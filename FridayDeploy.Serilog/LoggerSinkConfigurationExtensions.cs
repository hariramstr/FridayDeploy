using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.PeriodicBatching;

namespace FridayDeploy.Serilog;

/// <summary>Adds the <c>WriteTo.FridayDeploy(...)</c> and <c>WriteTo.OpenObserve(...)</c> sinks to a Serilog <see cref="LoggerConfiguration"/>.</summary>
public static class LoggerSinkConfigurationExtensions
{
    /// <summary>
    /// Ships log events to a self-hosted OpenObserve instance via its JSON ingestion API
    /// (POST /api/{org}/{stream}/_json) using <c>Serilog.Sinks.Http</c> as the transport.
    /// Events are buffered in memory, batched, and flushed asynchronously; HTTP failures
    /// are reported to Serilog SelfLog and never block the application.
    /// <para>
    /// <b>Shutdown:</b> call <c>await Log.CloseAndFlushAsync()</c> (or
    /// <c>Log.CloseAndFlush()</c>) before the process exits to ensure the last
    /// batch is delivered. Without it, events buffered since the previous flush
    /// window are silently dropped.
    /// </para>
    /// </summary>
    public static LoggerConfiguration OpenObserve(
        this LoggerSinkConfiguration sinkConfiguration,
        Action<OpenObserveOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(sinkConfiguration);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new OpenObserveOptions { Url = "", Token = "", Stream = "" };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.Url))
            throw new ArgumentException("OpenObserveOptions.Url must be set.", nameof(configure));
        if (string.IsNullOrWhiteSpace(options.Token))
            throw new ArgumentException("OpenObserveOptions.Token must be set.", nameof(configure));
        if (string.IsNullOrWhiteSpace(options.Stream))
            throw new ArgumentException("OpenObserveOptions.Stream must be set.", nameof(configure));

        var appName = options.ApplicationName
            ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name
            ?? "UnknownApplication";

        var ingestUrl = $"{options.Url.TrimEnd('/')}/api/{options.Organization}/{options.Stream}/_json";

        return sinkConfiguration.Http(
            requestUri: ingestUrl,
            queueLimitBytes: null,
            logEventsInBatchLimit: options.BatchSizeLimit,
            batchSizeLimitBytes: null,
            period: options.FlushInterval,
            textFormatter: new OpenObserveTextFormatter(appName, options.Environment),
            batchFormatter: new OpenObserveBatchFormatter(),
            httpClient: new OpenObserveHttpClient(options.Token, options.HttpTimeout),
            restrictedToMinimumLevel: options.MinimumLevel);
    }


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
