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

## Troubleshooting: sink gets 403 Forbidden but curl works

If `curl` can POST to `/api/logs` or `/api/logs/batch` successfully but the Serilog sink consistently gets `Forbidden` on every attempt (visible via `Serilog.Debugging.SelfLog.Enable(Console.Error)` — see [FridayDeploy.SampleApp/Program.cs](../FridayDeploy.SampleApp/Program.cs) for an example), the response body logged alongside the status (added specifically for this) will usually reveal the cause. A **bare, unbranded 403 page** (e.g. a generic `nginx` error page, not your app or a custom error document) means something in front of the app is blocking the request before it arrives — not FridayDeploy itself, since the app's own API-key handler only ever returns 401, never 403.

The most common cause on shared-hosting control panels (Webuzo, cPanel, Plesk) is a **bundled WAF/bot-protection add-on (ModSecurity, Imunify360, etc.)** flagging the request. These often fingerprint the TLS handshake and client behavior, not just headers, so a legitimate request from .NET's `HttpClient` can get blocked while an identical-looking `curl` request passes. If you hit this:

1. Check the panel's WAF/security section (e.g. Security → ModSecurity → Audit Log) for the specific rule ID that fired, rather than disabling the WAF entirely for the domain.
2. Add a scoped exception for the ingestion path (`/api/logs`, `/api/logs/batch`) instead of turning ModSecurity off for the whole domain — disabling it site-wide removes protection for everything else hosted there too.
3. If you can't find the audit log, temporarily disabling the WAF to confirm it's the cause (as opposed to a routing/proxy config issue) is a reasonable diagnostic step — just re-enable it with a targeted rule exception afterward rather than leaving it off.
