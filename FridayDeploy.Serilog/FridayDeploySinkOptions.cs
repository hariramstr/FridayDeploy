using Serilog.Events;

namespace FridayDeploy.Serilog;

/// <summary>Configuration for the FridayDeploy Serilog sink.</summary>
public sealed class FridayDeploySinkOptions
{
    /// <summary>Base URL of the FridayDeploy server, e.g. "https://logs.company.com".</summary>
    public required string ServerUrl { get; set; }

    /// <summary>API key issued to this application in the FridayDeploy admin UI.</summary>
    public required string ApiKey { get; set; }

    /// <summary>Application name reported with every log event. Defaults to the entry assembly name.</summary>
    public string? ApplicationName { get; set; }

    /// <summary>Environment name reported with every log event (e.g. "Production").</summary>
    public string? Environment { get; set; }

    /// <summary>Application version reported with every log event.</summary>
    public string? Version { get; set; }

    /// <summary>Minimum level for events sent to FridayDeploy.</summary>
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Verbose;

    /// <summary>Maximum number of events sent per HTTP batch.</summary>
    public int BatchSizeLimit { get; set; } = 100;

    /// <summary>Maximum time to wait before flushing a partial batch.</summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Compress batch payloads with gzip before sending.</summary>
    public bool UseGzipCompression { get; set; } = true;

    /// <summary>Maximum retry attempts per batch before it is spooled to disk.</summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>Base delay for exponential backoff between retries.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Maximum delay between retries, regardless of attempt count.</summary>
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Directory used to persist batches that could not be delivered after <see cref="MaxRetryAttempts"/>
    /// attempts, and to resend them on a later flush. Defaults to "%TEMP%/fridaydeploy-spool".
    /// </summary>
    public string SpoolDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "fridaydeploy-spool");

    /// <summary>Maximum number of spooled batch files kept on disk before the oldest are dropped.</summary>
    public int MaxSpoolFiles { get; set; } = 500;

    /// <summary>HTTP request timeout for a single batch send attempt.</summary>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
