using FridayDeploy.Serilog;
using Serilog;
using Serilog.Context;

Serilog.Debugging.SelfLog.Enable(Console.Error);

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.FridayDeploy(options =>
    {
        options.ServerUrl = builder.Configuration["FridayDeploy:ServerUrl"] ?? "http://localhost:5246";
        options.ApiKey = builder.Configuration["FridayDeploy:ApiKey"] ?? "sample-app-dev-key";
        options.ApplicationName = "FridayDeploy.SampleApp";
        options.Environment = builder.Environment.EnvironmentName;
        options.Version = "1.0.0";
        options.BatchSizeLimit = 50;
        options.FlushInterval = TimeSpan.FromSeconds(2);
    })
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
    context.Response.Headers["X-Correlation-Id"] = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
    {
        await next();
    }
});

var villages = new[] { "Salem", "Erode", "Coimbatore", "Madurai", "Trichy" };
var browsers = new[] { "Chrome", "Firefox", "Edge", "Safari" };
var random = new Random();

app.MapGet("/", () => "FridayDeploy.SampleApp is running.");

app.MapGet("/loan/apply", (int userId, int loanId) =>
{
    Log.ForContext("UserId", userId)
        .ForContext("LoanId", loanId)
        .ForContext("District", villages[random.Next(villages.Length)])
        .ForContext("Village", villages[random.Next(villages.Length)])
        .ForContext("Browser", browsers[random.Next(browsers.Length)])
        .Information("Loan application {LoanId} submitted for user {UserId}", loanId, userId);

    return Results.Ok(new { loanId, status = "Submitted" });
});

app.MapGet("/loan/approve", (int userId, int loanId) =>
{
    Log.ForContext("UserId", userId)
        .ForContext("LoanId", loanId)
        .Warning("Loan {LoanId} for user {UserId} is pending manual review", loanId, userId);

    return Results.Ok(new { loanId, status = "PendingReview" });
});

app.MapGet("/loan/fail", (int userId, int loanId) =>
{
    try
    {
        throw new InvalidOperationException($"Credit check failed for loan {loanId}");
    }
    catch (Exception ex)
    {
        Log.ForContext("UserId", userId)
            .ForContext("LoanId", loanId)
            .Error(ex, "Loan {LoanId} processing failed for user {UserId}", loanId, userId);

        return Results.Problem("Loan processing failed.");
    }
});

app.MapGet("/system/crash", () =>
{
    Log.Fatal("Simulated fatal condition — sample app watchdog triggered");
    return Results.Ok(new { status = "FatalLogged" });
});

_ = Task.Run(async () =>
{
    var userId = 1000;
    while (true)
    {
        try
        {
            var loanId = random.Next(1, 10_000);
            Log.ForContext("UserId", userId)
                .ForContext("LoanId", loanId)
                .ForContext("District", villages[random.Next(villages.Length)])
                .Information("Background heartbeat: processed loan {LoanId} for user {UserId}", loanId, userId);
            userId++;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Background heartbeat failed");
        }

        await Task.Delay(TimeSpan.FromSeconds(10));
    }
});

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
