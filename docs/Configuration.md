# Configuration

## First-run configuration (`appsettings.json` / environment variables)

These values only matter on the very first run — after the database exists, most of them are edited from the **Settings** page and persisted in SQLite instead.

| Key | Env var | Default | Notes |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | `Data Source=App_Data/fridaydeploy.db` | SQLite file path. Always effective (not moved to Settings — changing the DB location is a deploy-time decision). |
| `AdminUser:Username` | `AdminUser__Username` | `admin` | First-run admin account. |
| `AdminUser:Password` | `AdminUser__Password` | — | **Required.** The app fails to start if unset. Change this before exposing the app. |
| `FridayDeploy:RetentionDays` | `FridayDeploy__RetentionDays` | `7` | Seed value for the Settings page; edit retention there afterward. |
| `FridayDeploy:RefreshIntervalSeconds` | `FridayDeploy__RefreshIntervalSeconds` | `30` | Dashboard auto-refresh. |
| `FridayDeploy:PollingIntervalSeconds` | `FridayDeploy__PollingIntervalSeconds` | `10` | Logs page "LIVE" tail polling interval. |
| `FridayDeploy:PageSize` | `FridayDeploy__PageSize` | `25` | Logs page rows per page. |
| `FridayDeploy:Theme` | `FridayDeploy__Theme` | `Dark` | `Dark`, `Light`, or `System`. |
| `FridayDeploy:BrandingName` | `FridayDeploy__BrandingName` | _(none)_ | Overrides "FridayDeploy" in the sidebar/title, useful for white-labeling internally. |

## Settings page (persisted, editable at runtime)

Retention Days, Refresh Interval, Polling Interval, Page Size, Theme, and Branding Name are stored in the `AppSettings` table and take effect immediately (Theme/Branding on next page load; retention on the next nightly sweep).

## Docker environment variables

```bash
AdminUser__Username=admin
AdminUser__Password=your-strong-password
ConnectionStrings__DefaultConnection="Data Source=/app/App_Data/fridaydeploy.db"
```

See [docker-compose.yml](../docker-compose.yml) for the full set already wired up, plus healthcheck, volume, and restart policy.

## FridayDeploy.Serilog sink options

| Option | Default | Notes |
|---|---|---|
| `ServerUrl` | — | **Required.** Base URL of the FridayDeploy server. |
| `ApiKey` | — | **Required.** Created from the API Keys page. |
| `ApplicationName` | entry assembly name | Sent with every event; also determines auto-registration display name if `Application` claim isn't overridden server-side (the API key's bound application always wins). |
| `Environment`, `Version` | — | Reported with every event. |
| `MinimumLevel` | `Verbose` | Minimum Serilog level shipped to the server. |
| `BatchSizeLimit` | `100` | Max events per HTTP batch. |
| `FlushInterval` | `5s` | Max time before a partial batch is flushed. |
| `UseGzipCompression` | `true` | Gzip-compresses batch payloads (the server auto-decompresses). |
| `MaxRetryAttempts` | `5` | Retries per batch before spooling to disk. |
| `RetryBaseDelay` / `RetryMaxDelay` | `1s` / `30s` | Exponential backoff bounds. |
| `SpoolDirectory` | `%TEMP%/fridaydeploy-spool` | Where undelivered batches are persisted and resent from. |
| `MaxSpoolFiles` | `500` | Oldest spooled batches are dropped past this limit. |
| `HttpTimeout` | `30s` | Per-attempt HTTP timeout. |
