# Architecture

## Projects

```text
FridayDeploy.Web        ASP.NET Core MVC + Web API, EF Core/SQLite, the admin UI
FridayDeploy.Serilog    Reusable Serilog sink, packaged as a standalone NuGet package
FridayDeploy.SampleApp  Minimal ASP.NET Core app demonstrating the sink end-to-end
FridayDeploy.Tests      Unit, integration, and API tests
```

## FridayDeploy.Web layout

```text
Controllers/            MVC controllers for the human UI (Dashboard, Logs, Applications, ...)
Controllers/Api/        Thin [ApiController] classes — ingestion, search, dashboard, properties
Services/                Application logic; controllers delegate here, EF Core lives here
Data/AppDbContext.cs     EF Core DbContext and entity configuration (indexes, cascades, max lengths)
Models/                  EF Core entities
DTOs/                    Wire-format request/response types for the API
ViewModels/              Shapes purpose-built for Razor views
Helpers/                 Small static helpers (log level → CSS class, app health thresholds)
Authentication/          The custom X-Api-Key authentication scheme
BackgroundServices/      IHostedService implementations (retention sweep)
Extensions/              Startup helper extensions (DB init, SQLite directory creation)
wwwroot/                 Static assets (site.css, site.js)
```

## Data flow

1. **Ingestion.** A Serilog sink or a direct `curl` posts JSON to `POST /api/logs` (anonymous, single event) or `POST /api/logs/batch` (API-key authenticated, batched — what the sink uses). `LogService` builds a `Log` entity plus one `LogProperty` row per custom property and saves it.
2. **API key auth.** `ApiKeyAuthenticationHandler` validates the `X-Api-Key` header against a SHA-256 hash lookup, bumps `ApiKey.LastUsedUtc` and the owning `Application.LastSeenUtc`, and attaches the application's identity as claims. The batch endpoint uses that identity for every event in the payload — a key can never claim to be a different application.
3. **Search.** `LogSearchService.BuildQuery` turns a `LogSearchRequest` (structured filters + a small query-language string like `Level=Error AND District~Salem`) into an `IQueryable<Log>`, reused by search, export, and the correlation/request timelines.
4. **Presentation.** MVC controllers call services and render Razor views; `Controllers/Api/*` return the same data as JSON for programmatic access and Swagger.

## Design decisions

- **SQLite by default, no external infra.** The whole point of FridayDeploy is a log server you can run in under two minutes. `AsNoTracking()` + indexes on `TimestampUtc`/`Application`/`Level` keep read paths fast without a heavier database.
- **Two authentication schemes, not one.** Cookie auth (human UI) and API-key auth (machine ingestion) are registered side by side in `Program.cs` and selected per-endpoint with `[Authorize(AuthenticationSchemes = ...)]` — ingestion never needs a browser session, and the UI never needs an API key.
- **API keys are hashed with SHA-256, not BCrypt.** API keys are already high-entropy random secrets (unlike user passwords), so a fast deterministic hash enables an indexed database lookup instead of iterating and verifying every key on each request.
- **No heavy abstractions.** EF Core is used directly in services where it's clear and efficient; there's no repository/unit-of-work layer on top of `DbContext`. Application logic lives in small, single-purpose services (`LogService`, `ExportService`, `BookmarkService`, ...) rather than one large service class.
- **Applications are a real entity, not just distinct log values.** `Application`/`ApiKey` exist so a key can be scoped to one application and so First Seen / Last Seen / online-offline health can be tracked without re-scanning the `Logs` table on every request.
