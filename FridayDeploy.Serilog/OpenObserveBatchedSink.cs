using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace FridayDeploy.Serilog;

/// <summary>
/// Ships batches of Serilog events to an OpenObserve instance via its JSON ingestion API
/// (POST /api/{org}/{stream}/_json). Delivery is async, batched, and fault-isolated —
/// failures are logged to Serilog SelfLog and never propagate to the application.
/// </summary>
public sealed class OpenObserveBatchedSink : IBatchedLogEventSink, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OpenObserveOptions _options;
    private readonly HttpClient _httpClient;
    private readonly string _ingestUrl;
    private readonly string _applicationName;

    public OpenObserveBatchedSink(OpenObserveOptions options, HttpClient? httpClient = null)
    {
        _options = options;
        _ingestUrl = $"{options.Url.TrimEnd('/')}/api/{options.Organization}/{options.Stream}/_json";
        _applicationName = options.ApplicationName
            ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name
            ?? "UnknownApplication";

        _httpClient = httpClient ?? new HttpClient { Timeout = options.HttpTimeout };

        // Use the pre-generated token directly as the Authorization header value.
        // Expected format: "Basic <base64>" — stored verbatim from appsettings.
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", options.Token);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        var records = batch.Select(ToRecord).ToList();
        if (records.Count == 0)
            return;

        try
        {
            var json = JsonSerializer.Serialize(records, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(_ingestUrl, content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await TryReadBodyAsync(response).ConfigureAwait(false);
                SelfLog.WriteLine("OpenObserve sink: server returned {0}. Body: {1}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("OpenObserve sink: failed to send batch: {0}", ex);
        }
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;

    private Dictionary<string, object?> ToRecord(LogEvent logEvent)
    {
        var record = new Dictionary<string, object?>
        {
            // OpenObserve recognises _timestamp as microseconds since Unix epoch for time ordering.
            ["_timestamp"] = logEvent.Timestamp.UtcDateTime.Ticks / 10, // 100-ns ticks → microseconds
            ["timestamp"] = logEvent.Timestamp.UtcDateTime.ToString("O"),
            ["level"] = logEvent.Level.ToString(),
            ["message"] = logEvent.RenderMessage(),
            ["application"] = _applicationName,
            ["environment"] = _options.Environment ?? string.Empty,
        };

        if (logEvent.Exception is { } ex)
        {
            record["exception"] = ex.Message;
            record["stack_trace"] = ex.StackTrace;
        }

        foreach (var (name, value) in logEvent.Properties)
        {
            // Avoid overwriting the fixed fields above.
            var key = name switch
            {
                "level" or "message" or "application" or "environment"
                    or "_timestamp" or "timestamp" => $"prop_{name}",
                _ => name
            };
            record[key] = ToPlainValue(value);
        }

        return record;
    }

    private static object? ToPlainValue(LogEventPropertyValue value) => value switch
    {
        ScalarValue scalar => scalar.Value,
        SequenceValue seq  => seq.Elements.Select(ToPlainValue).ToList(),
        StructureValue str => str.Properties.ToDictionary(p => p.Name, p => ToPlainValue(p.Value)),
        DictionaryValue dict => dict.Elements.ToDictionary(kv => kv.Key.ToString(), kv => ToPlainValue(kv.Value)),
        _ => value.ToString()
    };

    private static async Task<string> TryReadBodyAsync(HttpResponseMessage response)
    {
        const int maxLength = 500;
        try
        {
            var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return text.Length > maxLength ? text[..maxLength] + "…" : text;
        }
        catch
        {
            return "<unreadable>";
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
