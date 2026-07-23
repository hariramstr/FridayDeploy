# FridayDeploy.SampleApp

A minimal ASP.NET Core app demonstrating `FridayDeploy.Serilog`. It emits Information/Warning/Error/Fatal logs with correlation IDs and custom properties (UserId, LoanId, District, Village, Browser) on a few endpoints, plus a background heartbeat.

## Run it

1. Start `FridayDeploy.Web` (the log server).
2. Log in to the FridayDeploy UI and open **API Keys** → create a key for application name `FridayDeploy.SampleApp`.
3. Paste the generated key into `appsettings.json` → `FridayDeploy:ApiKey` (or set the `FridayDeploy__ApiKey` environment variable).
4. `dotnet run` and hit `GET /loan/apply?userId=1&loanId=100`, `/loan/approve`, `/loan/fail`, or `/system/crash` — or just let the background heartbeat run.

Logs appear in the FridayDeploy dashboard within a few seconds (default flush interval is 2s for this sample).
