using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace FridayDeploy.Serilog;

/// <summary>
/// Ships batches of log events to a FridayDeploy server over HTTP, with gzip compression, exponential-backoff
/// retries, and local spooling of batches that could not be delivered.
/// </summary>
public class FridayDeployBatchedSink : IBatchedLogEventSink, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly FridayDeploySinkOptions _options;
    private readonly HttpClient _httpClient;
    private readonly BatchSpool _spool;
    private readonly string _applicationName;
    private readonly string _machineName;
    private readonly string _processId;

    public FridayDeployBatchedSink(FridayDeploySinkOptions options, HttpClient? httpClient = null)
    {
        _options = options;
        _httpClient = httpClient ?? new HttpClient { Timeout = options.HttpTimeout };
        _httpClient.BaseAddress ??= new Uri(options.ServerUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);

        // Some reverse proxies/WAFs reject requests with no User-Agent (HttpClient sends none by default).
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            var sinkVersion = typeof(FridayDeployBatchedSink).Assembly.GetName().Version?.ToString() ?? "1.0.0";
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FridayDeploy.Serilog", sinkVersion));
        }
        _spool = new BatchSpool(options.SpoolDirectory, options.MaxSpoolFiles);
        _applicationName = options.ApplicationName ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "UnknownApplication";
        _machineName = Environment.MachineName;
        _processId = Environment.ProcessId.ToString();
    }

    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        await ResendSpooledAsync().ConfigureAwait(false);

        var payloads = batch.Select(ToPayload).ToList();
        if (payloads.Count == 0)
        {
            return;
        }

        await SendWithRetryAsync(payloads).ConfigureAwait(false);
    }

    public Task OnEmptyBatchAsync() => ResendSpooledAsync();

    private async Task SendWithRetryAsync(List<LogEventPayload> payloads)
    {
        for (var attempt = 0; attempt <= _options.MaxRetryAttempts; attempt++)
        {
            try
            {
                var response = await PostAsync(payloads).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                var body = await ReadBodyForDiagnosticsAsync(response).ConfigureAwait(false);
                SelfLog.WriteLine("FridayDeploy sink: server returned {0} on attempt {1}. Body: {2}", response.StatusCode, attempt, body);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("FridayDeploy sink: send failed on attempt {0}: {1}", attempt, ex);
            }

            if (attempt < _options.MaxRetryAttempts)
            {
                var delay = ComputeBackoff(attempt);
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        SelfLog.WriteLine("FridayDeploy sink: exhausted retries, spooling {0} events", payloads.Count);
        _spool.Save(payloads);
    }

    private async Task<HttpResponseMessage> PostAsync(List<LogEventPayload> payloads)
    {
        var json = JsonSerializer.Serialize(payloads, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/logs/batch");

        if (_options.UseGzipCompression)
        {
            using var compressed = new MemoryStream();
            await using (var gzip = new GZipStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
            {
                await gzip.WriteAsync(bytes).ConfigureAwait(false);
            }

            request.Content = new ByteArrayContent(compressed.ToArray());
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content.Headers.ContentEncoding.Add("gzip");
        }
        else
        {
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        return await _httpClient.SendAsync(request).ConfigureAwait(false);
    }

    private async Task ResendSpooledAsync()
    {
        List<(string Path, IReadOnlyList<LogEventPayload> Events)> pending;
        try
        {
            pending = _spool.LoadPending().ToList();
        }
        catch
        {
            return;
        }

        foreach (var (path, events) in pending)
        {
            try
            {
                var response = await PostAsync(events.ToList()).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    _spool.Delete(path);
                }
                else
                {
                    // Leave it spooled; will retry on the next flush.
                    break;
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("FridayDeploy sink: failed to resend spooled batch {0}: {1}", path, ex);
                break;
            }
        }
    }

    /// <summary>Best-effort read of a failed response's body, truncated, so reverse-proxy/WAF block pages
    /// (which usually name themselves) show up in SelfLog instead of just a bare status code.</summary>
    private static async Task<string> ReadBodyForDiagnosticsAsync(HttpResponseMessage response)
    {
        const int maxLength = 500;
        try
        {
            var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return text.Length > maxLength ? text[..maxLength] + "…" : text;
        }
        catch (Exception ex)
        {
            return $"<failed to read body: {ex.Message}>";
        }
    }

    private TimeSpan ComputeBackoff(int attempt)
    {
        var delay = _options.RetryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt);
        var capped = Math.Min(delay, _options.RetryMaxDelay.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(capped);
    }

    private LogEventPayload ToPayload(LogEvent logEvent)
    {
        var properties = new Dictionary<string, object?>();
        string? requestId = null, correlationId = null, source = null, userId = null, threadId = null, machineName = null, processId = null;

        // Recognized names are pulled onto dedicated fields rather than left in the generic bag, so an
        // enricher-provided value (e.g. Serilog.Enrichers.Environment/Process) always wins over the sink's
        // own best-effort capture of the same information.
        foreach (var (name, value) in logEvent.Properties)
        {
            var plain = ToPlainValue(value);
            switch (name)
            {
                case "RequestId": requestId = plain?.ToString(); break;
                case "CorrelationId": correlationId = plain?.ToString(); break;
                case "SourceContext": source = plain?.ToString(); break;
                case "UserId": userId = plain?.ToString(); break;
                case "ThreadId": threadId = plain?.ToString(); break;
                case "MachineName": machineName = plain?.ToString(); break;
                case "ProcessId": processId = plain?.ToString(); break;
                default:
                    properties[name] = plain;
                    break;
            }
        }

        if (userId is not null)
        {
            properties["UserId"] = userId;
        }

        properties["ProcessId"] = processId ?? _processId;

        return new LogEventPayload
        {
            TimestampUtc = logEvent.Timestamp.UtcDateTime,
            Application = _applicationName,
            Environment = _options.Environment,
            Version = _options.Version,
            Machine = machineName ?? _machineName,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            Exception = logEvent.Exception?.Message,
            StackTrace = logEvent.Exception?.StackTrace,
            Source = source,
            RequestId = requestId,
            CorrelationId = correlationId,
            ThreadId = threadId ?? Environment.CurrentManagedThreadId.ToString(),
            Properties = properties
        };
    }

    private static object? ToPlainValue(LogEventPropertyValue value) =>
        LogEventPropertyFlattener.Flatten(value);

    public void Dispose() => _httpClient.Dispose();
}
