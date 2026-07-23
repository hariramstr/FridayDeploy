# Roadmap

## Shipped

- Structured log ingestion (single event and batched), `FridayDeploy.Serilog` sink with batching/retry/gzip/spooling
- API keys and automatic application registration, with Online/Warning/Offline health
- Dashboard analytics, correlation/request timelines, exception explorer, property explorer
- CSV/JSON/TXT export, saved searches, bookmarks, per-log notes
- Dark/Light/System theming, persisted Settings, About page
- Swagger/OpenAPI documentation
- Docker/Docker Compose deployment

## v1.1

- **Alerts** — email and webhook notifications on error-rate thresholds or specific exception patterns
- **Saved dashboards** — user-customizable dashboard layouts, not just the built-in one
- **Advanced query language** — boolean grouping (`OR`, parentheses), negation, and range queries beyond the current `AND`-only expression syntax
- A dedicated REST surface for export/saved-searches/bookmarks/notes/API-key management (currently MVC form actions only — see [API.md](API.md))

## v2

- **OpenTelemetry** ingestion, alongside the existing Serilog sink
- **NLog** and **`Microsoft.Extensions.Logging`** provider packages, so non-Serilog apps can ship logs without a bridge
- **Plugin system** for custom sinks, exporters, and dashboard widgets
- **PostgreSQL and SQL Server** support as alternatives to SQLite for teams that outgrow a single-file database
- **Multi-user accounts and RBAC** — today FridayDeploy is single-admin only

Have a request that isn't here? Open an issue using the feature request template.
