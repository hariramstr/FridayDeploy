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
