# FridayDeploy.Serilog

A Serilog sink for [FridayDeploy](https://github.com/hariramstr/FridayDeploy) — async, batched, gzip-compressed, retrying HTTP log shipping with local spooling so no logs are lost.

## Usage

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.FridayDeploy(options =>
    {
        options.ServerUrl = "https://logs.company.com";
        options.ApiKey = "YOUR_API_KEY";
    })
    .CreateLogger();
```

## Features

- Non-blocking, async logging via an in-process background queue
- Automatic batching by size and time (`BatchSizeLimit`, `FlushInterval`)
- Gzip-compressed payloads
- Exponential backoff retries (`MaxRetryAttempts`, `RetryBaseDelay`, `RetryMaxDelay`)
- Local disk spooling of undelivered batches, resent automatically once the server is reachable again
- Graceful shutdown — in-flight and queued events are flushed on `Log.CloseAndFlush()`
- Automatic capture of Application, Environment, Version, Machine, ThreadId, ProcessId, plus `RequestId`, `CorrelationId`, `SourceContext`, `UserId`, and any custom `ForContext(...)` properties

## Works alongside other Serilog enrichers

The sink is a plain `ILogEventSink` — it composes with any other enricher in your pipeline (`Serilog.Enrichers.Environment`, `.Process`, `.Thread`, `.AssemblyName`, `Serilog.Exceptions`, etc.):

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentUserName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .Enrich.WithExceptionDetails()
    .Enrich.WithProperty("Service", "MyApp.Api")
    .WriteTo.FridayDeploy(options => { /* ... */ })
    .CreateLogger();
```

A few things worth knowing:

- If an enricher sets `MachineName` or `ProcessId`, the sink uses that value instead of its own best-effort capture — enricher wins.
- Every other enriched property (e.g. `ProcessName`, `ThreadName`, `AssemblyVersion`, custom `ForContext`/`WithProperty` values) is sent through as-is and is fully searchable in the FridayDeploy UI's property explorer and query language.
- `Serilog.Exceptions`' `WithExceptionDetails()` adds a large nested `ExceptionDetails` object (and similar deeply-nested/array properties are handled the same way) — FridayDeploy stores the full structure in the log's raw JSON (visible on the Log Details page) but summarizes it in the flat property list (e.g. `{…8 properties…}`) rather than indexing the whole blob as a single filterable value.
- `WithDemystifiedStackTraces()` needs no special handling — the sink reads `Exception.StackTrace` directly, so it already reflects the demystified version.
