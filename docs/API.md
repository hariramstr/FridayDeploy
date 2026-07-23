# API Reference

Interactive docs with schemas and "Try it out": run the app and open `/swagger`.

Two authentication modes:

- **Cookie session** — used by the browser UI and by `GET` endpoints below. Log in via `/Auth/Login`.
- **API key** (`X-Api-Key` header) — used only by `POST /api/logs/batch`, i.e. log ingestion from applications. Create keys from the **API Keys** page.

`POST /api/logs` (single event) is anonymous, intended for quick manual testing — not for production ingestion volume.

## Logs

### `POST /api/logs`

Ingest one log event. Anonymous.

```json
{
  "application": "MyApp",
  "level": "Error",
  "message": "Unable to save member",
  "timestampUtc": "2026-01-01T12:00:00Z",
  "environment": "Production",
  "version": "1.2.3",
  "machine": "web-01",
  "exception": "System.InvalidOperationException: ...",
  "stackTrace": "...",
  "source": "MembersController",
  "requestId": "REQ789",
  "correlationId": "ABC123",
  "threadId": "11",
  "duration": 42.5,
  "properties": { "UserId": 123, "District": "Salem", "LoanId": 55 }
}
```

Only `application`, `level`, and `message` are required. Returns `201 Created` with `{ "id": 42 }`.

### `POST /api/logs/batch`

Ingest a batch of log events. **Requires `X-Api-Key`.** This is what `FridayDeploy.Serilog` posts to. Body is a JSON array of the same shape as above, minus `application` (the API key determines the application). Returns `200 OK` with `{ "ingested": 2 }`.

### `GET /api/logs`

Search logs. Requires a session cookie. Query parameters mirror the Logs page filters:

| Parameter | Type | Notes |
|---|---|---|
| `Query` | string | Query-language expression, e.g. `Level=Error AND District~Salem` |
| `Application`, `Environment`, `Level`, `Machine`, `CorrelationId`, `RequestId`, `Source` | string | Exact/contains filters |
| `ContainsText` | string | Full-text search across message, application, exception, etc. |
| `PropertyName`, `PropertyValue` | string | Filter on a custom property |
| `FromUtc`, `ToUtc` | datetime | Time range |
| `MinDuration`, `MaxDuration` | number | Duration range (ms) |
| `Page`, `PageSize` | int | Paging (page size capped at 100) |
| `Sort` | string | `timestamp_desc` (default) or `timestamp_asc` |

Returns `{ "items": [...], "page": 1, "pageSize": 50, "totalCount": 231 }`.

### `GET /api/logs/{id}`

Fetch a single log event with its structured properties. `404` if it doesn't exist.

## Applications

### `GET /api/applications`

Returns discovered applications with health, First Seen/Last Seen, machine count, and log volume today.

## Dashboard

### `GET /api/dashboard`

Returns the same aggregate metrics shown on the Dashboard page: today's logs/errors/warnings, average logs/minute, application health counts, top applications, top exceptions, latest deployment activity, and hourly/7-day chart series.

## Properties

### `GET /api/properties`

Returns the set of distinct custom property names seen across all logs, for building filters/query expressions.

## UI-only actions (not REST endpoints yet)

Export, saved searches, bookmarks, notes, API key management, and settings are currently form-posted MVC actions (`/Logs/Export`, `/Logs/SaveSearch`, `/Bookmarks/*`, `/ApiKeys/*`, `/Settings/Save`, ...) rather than JSON API endpoints. They work from the browser today; a dedicated REST surface for them is tracked on the [Roadmap](Roadmap.md).

## Health check

### `GET /health`

Anonymous. Returns `{ "status": "Healthy", "service": "FridayDeploy", "utc": "..." }`. Used by the Docker healthcheck.
