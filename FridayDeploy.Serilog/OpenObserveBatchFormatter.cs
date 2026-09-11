using Serilog.Sinks.Http;

namespace FridayDeploy.Serilog;

/// <summary>
/// Wraps a sequence of pre-formatted JSON objects (one per log event, produced by
/// <see cref="OpenObserveTextFormatter"/>) into the JSON array format required by
/// OpenObserve's <c>/_json</c> ingestion endpoint.
/// </summary>
internal sealed class OpenObserveBatchFormatter : IBatchFormatter
{
    public void Format(IEnumerable<string> logEvents, TextWriter output)
    {
        output.Write('[');
        bool first = true;
        foreach (var logEvent in logEvents)
        {
            if (!first) output.Write(',');
            // Each event is already a compact JSON object; just trim any trailing newline.
            output.Write(logEvent.TrimEnd('\r', '\n'));
            first = false;
        }
        output.Write(']');
    }
}
