# Security

## Reporting a vulnerability

Please open a private security advisory on GitHub (Security tab → "Report a vulnerability") rather than a public issue. Do not include exploit details in a public issue or PR.

## Authentication

- **Human users** authenticate with a cookie (`FridayDeploy.Auth`), issued after verifying a BCrypt-hashed password. There is a single administrator account today; multi-user/RBAC is on the [Roadmap](Roadmap.md).
- **Applications** authenticate ingestion with an API key (`X-Api-Key` header), scoped to exactly one application. Keys are stored as a SHA-256 hash, never in plaintext; only a display prefix is retained for identification in the UI. Disabling or deleting a key takes effect immediately.
- The single-event `POST /api/logs` endpoint is intentionally anonymous for quick manual testing — do not rely on it for production ingestion; use the API-key-gated batch endpoint via the Serilog sink instead.

## Transport and cookies

- HTTPS redirection and HSTS are enabled outside Development.
- The auth cookie is `HttpOnly`, `SameSite=Lax`, with sliding expiration; `Secure` is enforced outside Development (in Development it follows the request scheme, since local HTTP testing is common).

## Rate limiting

- Login (`POST /Auth/Login`) is limited to 10 attempts/minute per IP to slow down credential guessing.
- Log ingestion (`POST /api/logs`, `POST /api/logs/batch`) is limited to 300 requests/minute per IP — generous enough for normal application traffic, enough to blunt abuse. Exceeding either limit returns `429 Too Many Requests`.

## Secrets

- The default admin password in `appsettings.json` (`ChangeThisPassword123!`) is a development convenience only. **Change it before exposing the app**, ideally via the `AdminUser__Password` environment variable rather than editing the checked-in file. The app fails to start if no admin password is configured.
- API keys are shown once at creation time and never displayed again — store them in your application's secret configuration (environment variables, a secrets manager), not source control.

## Input handling

- All EF Core queries use parameterized LINQ — there is no raw SQL string concatenation anywhere in the codebase.
- Log properties, notes, and saved-search names are rendered through Razor's default HTML encoding, which mitigates stored XSS from ingested/user-supplied text.
- MVC state-changing actions (Settings, API Keys, Saved Searches, Bookmarks, Notes) use antiforgery tokens (`[ValidateAntiForgeryToken]`).

## Known gaps

- Multi-user accounts and role-based access control are v2 roadmap items — today, anyone who can log in has full access.
- The application targets `net10.0`; if you're building from source before .NET 10 is GA on your machine, use the matching preview SDK. Docker images (`mcr.microsoft.com/dotnet/*:10.0`) already track GA.

If you find something not listed here, please report it per the process above rather than assuming it's a known/accepted gap.
