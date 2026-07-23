using FridayDeploy.Web.Data;
using FridayDeploy.Web.Helpers;
using FridayDeploy.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

public sealed class DashboardService(AppDbContext db)
{
    public async Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var sevenDaysAgo = today.AddDays(-6);
        var latest = await db.Logs.AsNoTracking().OrderByDescending(x => x.TimestampUtc).Select(x => new LatestLogViewModel(x.Id, x.TimestampUtc, x.Application, x.Level, x.Message)).FirstOrDefaultAsync(cancellationToken);
        var latestError = await db.Logs.AsNoTracking().Where(x => x.Level == "Error" || x.Level == "Fatal" || x.Level == "Critical").OrderByDescending(x => x.TimestampUtc).Select(x => new LatestLogViewModel(x.Id, x.TimestampUtc, x.Application, x.Level, x.Message)).FirstOrDefaultAsync(cancellationToken);
        var topApplicationsRaw = await db.Logs.AsNoTracking().Where(x => x.TimestampUtc >= today).GroupBy(x => x.Application).Select(g => new { Label = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).Take(5).ToListAsync(cancellationToken);
        var topApplications = topApplicationsRaw.Select(x => new ChartPointViewModel(x.Label, x.Count)).ToList();
        var topExceptionsRaw = await db.Logs.AsNoTracking().Where(x => x.Exception != null && x.Exception != "").GroupBy(x => x.Exception!).Select(g => new { Exception = g.Key, Count = g.Count(), LastSeenUtc = g.Max(x => x.TimestampUtc), Application = g.OrderByDescending(x => x.TimestampUtc).Select(x => x.Application).First() }).OrderByDescending(x => x.Count).Take(5).ToListAsync(cancellationToken);
        var topExceptions = topExceptionsRaw.Select(x => new ExceptionSummaryViewModel(x.Exception, x.Count, x.LastSeenUtc, x.Application)).ToList();
        var logsPerHourRaw = await db.Logs.AsNoTracking().Where(x => x.TimestampUtc >= today).GroupBy(x => x.TimestampUtc.Hour).Select(g => new { Label = g.Key, Count = g.Count() }).OrderBy(x => x.Label).ToListAsync(cancellationToken);
        var logsPerHour = logsPerHourRaw.Select(x => new ChartPointViewModel(x.Label.ToString(), x.Count)).ToList();
        var errorsPerHourRaw = await db.Logs.AsNoTracking().Where(x => x.TimestampUtc >= today && (x.Level == "Error" || x.Level == "Fatal" || x.Level == "Critical")).GroupBy(x => x.TimestampUtc.Hour).Select(g => new { Label = g.Key, Count = g.Count() }).OrderBy(x => x.Label).ToListAsync(cancellationToken);
        var errorsPerHour = errorsPerHourRaw.Select(x => new ChartPointViewModel(x.Label.ToString(), x.Count)).ToList();
        var lastSevenDaysRaw = await db.Logs.AsNoTracking().Where(x => x.TimestampUtc >= sevenDaysAgo).GroupBy(x => x.TimestampUtc.Date).Select(g => new { Label = g.Key, Count = g.Count() }).OrderBy(x => x.Label).ToListAsync(cancellationToken);
        var lastSevenDays = lastSevenDaysRaw.Select(x => new ChartPointViewModel(x.Label.ToString("MM-dd"), x.Count)).ToList();

        var applicationLastSeen = await GetApplicationLastSeenAsync(cancellationToken);
        var healthCounts = applicationLastSeen
            .Select(x => ApplicationHealthHelper.FromLastSeen(x.LastSeenUtc, now))
            .GroupBy(x => x)
            .ToDictionary(g => g.Key, g => g.Count());

        var latestDeployment = await db.Applications.AsNoTracking()
            .OrderByDescending(x => x.FirstSeenUtc)
            .Select(x => new DeploymentActivityViewModel(x.Name, x.FirstSeenUtc))
            .FirstOrDefaultAsync(cancellationToken);

        var todaysLogs = await db.Logs.CountAsync(x => x.TimestampUtc >= today, cancellationToken);
        var minutesElapsedToday = Math.Max(1, (now - today).TotalMinutes);

        return new DashboardViewModel(
            todaysLogs,
            await db.Logs.CountAsync(x => x.TimestampUtc >= today && (x.Level == "Error" || x.Level == "Fatal" || x.Level == "Critical"), cancellationToken),
            await db.Logs.CountAsync(x => x.TimestampUtc >= today && (x.Level == "Warning" || x.Level == "Warn"), cancellationToken),
            applicationLastSeen.Count,
            healthCounts.GetValueOrDefault("Online"),
            healthCounts.GetValueOrDefault("Warning"),
            healthCounts.GetValueOrDefault("Offline"),
            Math.Round(todaysLogs / minutesElapsedToday, 2),
            latest,
            latestError,
            latestDeployment,
            topApplications,
            topExceptions,
            logsPerHour,
            errorsPerHour,
            lastSevenDays);
    }

    public async Task<IReadOnlyList<ApplicationCardViewModel>> GetApplicationsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var logs = await db.Logs.AsNoTracking()
            .GroupBy(x => x.Application)
            .Select(g => new
            {
                Application = g.Key,
                Environment = g.OrderByDescending(x => x.TimestampUtc).Select(x => x.Environment).FirstOrDefault(),
                MachineCount = g.Select(x => x.Machine).Where(x => x != null).Distinct().Count(),
                LogsToday = g.Count(x => x.TimestampUtc >= today),
                LogCount = g.Count(),
                ErrorCount = g.Count(x => x.Level == "Error" || x.Level == "Fatal" || x.Level == "Critical"),
                WarningCount = g.Count(x => x.Level == "Warning" || x.Level == "Warn"),
                InformationCount = g.Count(x => x.Level == "Information" || x.Level == "Info"),
                LastSeenUtc = g.Max(x => x.TimestampUtc),
                LastErrorUtc = g.Where(x => x.Level == "Error" || x.Level == "Fatal" || x.Level == "Critical").Max(x => (DateTime?)x.TimestampUtc),
                Machine = g.OrderByDescending(x => x.TimestampUtc).Select(x => x.Machine).FirstOrDefault()
            })
            .OrderByDescending(x => x.LastSeenUtc)
            .ToListAsync(cancellationToken);

        var registrations = await db.Applications.AsNoTracking()
            .Select(x => new { x.Name, x.FirstSeenUtc, HasActiveApiKey = x.ApiKeys.Any(k => !k.IsDisabled) })
            .ToDictionaryAsync(x => x.Name, cancellationToken);

        return logs.Select(x =>
        {
            registrations.TryGetValue(x.Application, out var registration);
            return new ApplicationCardViewModel(x.Application, x.Environment, x.MachineCount, x.LogsToday, x.LogCount, x.ErrorCount, x.WarningCount, x.InformationCount, x.LastSeenUtc, x.LastErrorUtc, x.Machine, Math.Round(x.LogsToday / Math.Max(1, (now - today).TotalHours), 1), x.ErrorCount > 0 ? "Needs attention" : "Healthy", registration?.FirstSeenUtc, registration?.HasActiveApiKey ?? false, ApplicationHealthHelper.FromLastSeen(x.LastSeenUtc, now));
        }).ToList();
    }

    public async Task<(IReadOnlyList<string> Applications, IReadOnlyList<string> Environments, IReadOnlyList<string> Levels, IReadOnlyList<string> Machines)> GetFilterOptionsAsync(CancellationToken cancellationToken)
    {
        var applications = await db.Logs.AsNoTracking().Select(x => x.Application).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var environments = await db.Logs.AsNoTracking().Where(x => x.Environment != null).Select(x => x.Environment!).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var levels = await db.Logs.AsNoTracking().Select(x => x.Level).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var machines = await db.Logs.AsNoTracking().Where(x => x.Machine != null).Select(x => x.Machine!).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        return (applications, environments, levels, machines);
    }

    private async Task<List<(string Application, DateTime LastSeenUtc)>> GetApplicationLastSeenAsync(CancellationToken cancellationToken)
    {
        var raw = await db.Logs.AsNoTracking()
            .GroupBy(x => x.Application)
            .Select(g => new { Application = g.Key, LastSeenUtc = g.Max(x => x.TimestampUtc) })
            .ToListAsync(cancellationToken);
        return raw.Select(x => (x.Application, x.LastSeenUtc)).ToList();
    }
}
