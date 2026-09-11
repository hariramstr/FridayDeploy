using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace FridayDeploy.Serilog;

/// <summary>
/// Formats a single <see cref="LogEvent"/> as an OpenObserve-compatible JSON object
/// (one line, no trailing newline). Used by <see cref="OpenObserveBatchFormatter"/> to
/// build the JSON array sent to the <c>/_json</c> ingestion endpoint.
/// </summary>
internal sealed class OpenObserveTextFormatter : ITextFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _applicationName;
    private readonly string? _environment;

    public OpenObserveTextFormatter(string applicationName, string? environment)
    {
        _applicationName = applicationName;
        _environment = environment;
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var record = new Dictionary<string, object?>
        {
            // Microseconds since Unix epoch — required by OpenObserve for time ordering.
            ["_timestamp"] = logEvent.Timestamp.ToUnixTimeMilliseconds() * 1_000L,
            ["timestamp"]  = logEvent.Timestamp.UtcDateTime.ToString("O"),
            ["level"]      = logEvent.Level.ToString(),
            ["message"]    = logEvent.RenderMessage(),
            ["application"] = _applicationName,
            ["environment"] = _environment ?? string.Empty,
        };

        if (logEvent.Exception is { } ex)
        {
            record["exception"]   = ex.Message;
            record["stack_trace"] = ex.StackTrace;
        }

        foreach (var (name, value) in logEvent.Properties)
        {
            // Avoid clobbering the fixed fields above.
            var key = name switch
            {
                "level" or "message" or "application" or "environment"
                    or "_timestamp" or "timestamp" => $"prop_{name}",
                _ => name,
            };
            record[key] = LogEventPropertyFlattener.Flatten(value);
        }

        output.Write(JsonSerializer.Serialize(record, JsonOptions));
    }
}
