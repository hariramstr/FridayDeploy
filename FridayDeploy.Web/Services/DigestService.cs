using FridayDeploy.Web.Data;
using FridayDeploy.Web.Helpers;
using FridayDeploy.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

/// <summary>Composes the F-42 weekly digest and F-43 volume forecast from data already produced by
/// exception fingerprinting (M9) and the AppHourlyStat rollup — no new ingestion or heavy queries.</summary>
public sealed class DigestService(AppDbContext db, DashboardService dashboardService)
{
    public async Task<DigestViewModel> GetAsync(CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-7);

        var topExceptionsRaw = await db.ExceptionFingerprints.AsNoTracking()
            .Where(x => x.LastSeenUtc >= since)
            .OrderByDescending(x => x.OccurrenceCount)
            .Take(5)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var topExceptions = topExceptionsRaw.Select(x => ToGroupViewModel(x, now)).ToList();

        var newExceptionsRaw = await db.ExceptionFingerprints.AsNoTracking()
            .Where(x => x.FirstSeenUtc >= since)
            .OrderByDescending(x => x.FirstSeenUtc)
            .Take(10)
            .ToListAsync(cancellationToken);
        var newExceptions = newExceptionsRaw.Select(x => ToGroupViewModel(x, now)).ToList();

        var topFailingSources = await dashboardService.GetTopFailingSourcesAsync(since, cancellationToken);

        var appTotalsRaw = await db.Logs.AsNoTracking()
            .Where(x => x.TimestampUtc >= since)
            .GroupBy(x => x.Application)
            .Select(g => new
            {
                Application = g.Key,
                LogCount = g.Count(),
                ErrorCount = g.Count(x => x.Level == "Error" || x.Level == "Fatal" || x.Level == "Critical")
            })
            .OrderByDescending(x => x.LogCount)
            .ToListAsync(cancellationToken);
        var appTotals = appTotalsRaw.Select(x => new AppWeeklyTotalViewModel(x.Application, x.LogCount, x.ErrorCount)).ToList();

        var forecasts = await GetForecastsAsync(cancellationToken);

        return new DigestViewModel(topExceptions, topFailingSources, appTotals, newExceptions, forecasts);
    }

    /// <summary>F-43: fits a simple least-squares line through each application's last 14 daily totals
    /// (from AppHourlyStat, rolled up to per-day) and projects one day forward. Plain arithmetic —
    /// intentionally not a statistics library, since the input is 14 points at most.</summary>
    private async Task<IReadOnlyList<ForecastViewModel>> GetForecastsAsync(CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.Date.AddDays(-13);
        var raw = await db.AppHourlyStats.AsNoTracking()
            .Where(x => x.HourBucketUtc >= since)
            .GroupBy(x => new { x.Application, Day = x.HourBucketUtc.Date })
            .Select(g => new { g.Key.Application, g.Key.Day, Count = g.Sum(x => x.LogCount) })
            .ToListAsync(cancellationToken);

        return raw.GroupBy(x => x.Application)
            .Where(g => g.Count() >= 2)
            .Select(g =>
            {
                var points = g.Select(x => (X: (x.Day - since).TotalDays, Y: (double)x.Count)).ToList();
                var (slope, intercept) = LeastSquares(points);
                var average = points.Average(p => p.Y);
                var nextX = points.Max(p => p.X) + 1;
                var predicted = Math.Max(0, slope * nextX + intercept);
                return new ForecastViewModel(g.Key, Math.Round(average, 1), Math.Round(slope, 2), Math.Round(predicted, 1));
            })
            .OrderByDescending(x => x.DailyAverage)
            .ToList();
    }

    private static (double Slope, double Intercept) LeastSquares(IReadOnlyList<(double X, double Y)> points)
    {
        var n = points.Count;
        var sumX = points.Sum(p => p.X);
        var sumY = points.Sum(p => p.Y);
        var sumXY = points.Sum(p => p.X * p.Y);
        var sumXX = points.Sum(p => p.X * p.X);

        var denominator = n * sumXX - sumX * sumX;
        if (denominator == 0)
        {
            return (0, sumY / n);
        }

        var slope = (n * sumXY - sumX * sumY) / denominator;
        var intercept = (sumY - slope * sumX) / n;
        return (slope, intercept);
    }

    private static ExceptionGroupViewModel ToGroupViewModel(Models.ExceptionFingerprint fingerprint, DateTime now) => new(
        fingerprint.Id,
        fingerprint.SampleMessage,
        fingerprint.SampleStackTrace,
        fingerprint.Application,
        fingerprint.OccurrenceCount,
        fingerprint.FirstSeenUtc,
        fingerprint.LastSeenUtc,
        ExceptionStatusHelper.FromTimestamps(fingerprint.FirstSeenUtc, fingerprint.LastSeenUtc, now));
}
