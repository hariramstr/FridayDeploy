using Serilog.Events;

namespace FridayDeploy.Serilog;

/// <summary>Configuration for the OpenObserve Serilog sink.</summary>
public sealed class OpenObserveOptions
{
    /// <summary>Base URL of the OpenObserve instance, e.g. "https://logs.example.com".</summary>
    public required string Url { get; set; }

    /// <summary>Pre-generated Authorization header value, e.g. "Basic &lt;base64&gt;".</summary>
    public required string Token { get; set; }

    /// <summary>OpenObserve organization name.</summary>
    public string Organization { get; set; } = "default";

    /// <summary>OpenObserve stream name. Each application should use a distinct stream.</summary>
    public required string Stream { get; set; }

    /// <summary>Application name enriched onto every log record.</summary>
    public string? ApplicationName { get; set; }

    /// <summary>Environment name enriched onto every log record (e.g. "Production").</summary>
    public string? Environment { get; set; }

    /// <summary>Minimum log level forwarded to OpenObserve.</summary>
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

    /// <summary>Maximum events per HTTP batch.</summary>
    public int BatchSizeLimit { get; set; } = 100;

    /// <summary>Maximum time between flushes.</summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>HTTP timeout per batch send attempt.</summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
