using FridayDeploy.Web.Data;
using FridayDeploy.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.BackgroundServices;

/// <summary>Aggregates the most recently completed hour of logs into AppHourlyStat, one row per
/// application. Powers uptime strips, sparklines, anomaly detection, and forecasting without those
/// features re-scanning the full Logs table on every page load.</summary>
public sealed class AppHourlyRollupService(IServiceScopeFactory scopeFactory, ILogger<AppHourlyRollupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunRollupAsync(stoppingToken);
            var now = DateTime.UtcNow;
            var nextHour = now.Date.AddHours(now.Hour + 1).AddMinutes(1);
            var delay = nextHour - now;
            await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task RunRollupAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var currentHour = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour);
            var bucketStart = currentHour.AddHours(-1);

            var rows = await db.Logs.AsNoTracking()
                .Where(x => x.TimestampUtc >= bucketStart && x.TimestampUtc < currentHour)
                .GroupBy(x => x.Application)
                .Select(g => new
                {
                    Application = g.Key,
                    LogCount = g.Count(),
                    ErrorCount = g.Count(x => x.Level == "Error" || x.Level == "Fatal" || x.Level == "Critical"),
                    WarningCount = g.Count(x => x.Level == "Warning")
                })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                var existing = await db.AppHourlyStats.FirstOrDefaultAsync(
                    x => x.Application == row.Application && x.HourBucketUtc == bucketStart, cancellationToken);

                if (existing is null)
                {
                    db.AppHourlyStats.Add(new AppHourlyStat
                    {
                        Application = row.Application,
                        HourBucketUtc = bucketStart,
                        LogCount = row.LogCount,
                        ErrorCount = row.ErrorCount,
                        WarningCount = row.WarningCount
                    });
                }
                else
                {
                    existing.LogCount = row.LogCount;
                    existing.ErrorCount = row.ErrorCount;
                    existing.WarningCount = row.WarningCount;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Hourly stat rollup failed.");
        }
    }
}
