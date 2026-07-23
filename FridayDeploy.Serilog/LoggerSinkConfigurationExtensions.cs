using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.PeriodicBatching;

namespace FridayDeploy.Serilog;

/// <summary>Adds the <c>WriteTo.FridayDeploy(...)</c> sink to a Serilog <see cref="LoggerConfiguration"/>.</summary>
public static class LoggerSinkConfigurationExtensions
{
    /// <summary>
    /// Ships log events to a FridayDeploy server. Events are queued in-process, batched by size and time,
    /// gzip-compressed, and sent asynchronously so application threads are never blocked on I/O. Batches that
    /// fail after retrying with exponential backoff are spooled to disk and resent on a later flush.
    /// </summary>
    public static LoggerConfiguration FridayDeploy(this LoggerSinkConfiguration sinkConfiguration, Action<FridayDeploySinkOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(sinkConfiguration);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new FridayDeploySinkOptions { ServerUrl = "", ApiKey = "" };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.ServerUrl))
        {
            throw new ArgumentException("FridayDeploySinkOptions.ServerUrl must be set.", nameof(configure));
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException("FridayDeploySinkOptions.ApiKey must be set.", nameof(configure));
        }

        var batchedSink = new FridayDeployBatchedSink(options);
        var periodicBatchingSink = new PeriodicBatchingSink(batchedSink, new PeriodicBatchingSinkOptions
        {
            BatchSizeLimit = options.BatchSizeLimit,
            Period = options.FlushInterval,
            EagerlyEmitFirstEvent = true
        });

        return sinkConfiguration.Sink(periodicBatchingSink, options.MinimumLevel);
    }
}
