using FridayDeploy.Web.Data;
using FridayDeploy.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.BackgroundServices;

public sealed class LogRetentionService(IServiceScopeFactory scopeFactory, ILogger<LogRetentionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunRetentionAsync(stoppingToken);
            var now = DateTimeOffset.Now;
            var nextRun = now.Date.AddDays(1).AddHours(2);
            await Task.Delay(nextRun - now, stoppingToken);
        }
    }

    private async Task RunRetentionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();
            var setting = await settingsService.GetAsync(cancellationToken);
            var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, setting.RetentionDays));
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Logs.Where(x => x.TimestampUtc < cutoff).ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Log retention failed.");
        }
    }
}
