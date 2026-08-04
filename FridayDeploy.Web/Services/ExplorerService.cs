using FridayDeploy.Web.Data;
using FridayDeploy.Web.Helpers;
using FridayDeploy.Web.Models;
using FridayDeploy.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

public sealed class ExplorerService(AppDbContext db)
{
    public async Task<PropertyExplorerViewModel> GetPropertiesAsync(CancellationToken cancellationToken)
    {
        var rawProperties = await db.LogProperties.AsNoTracking()
            .GroupBy(x => x.PropertyName)
            .Select(g => new
            {
                Name = g.Key,
                UsageCount = g.Count(),
                DistinctValues = g.Select(x => x.PropertyValue).Distinct().Count(),
                LatestUsageUtc = g.Max(x => x.Log.TimestampUtc)
            })
            .OrderByDescending(x => x.UsageCount)
            .ToListAsync(cancellationToken);

        var properties = rawProperties.Select(x => new PropertySummaryViewModel(x.Name, x.UsageCount, x.DistinctValues, x.LatestUsageUtc)).ToList();
        return new PropertyExplorerViewModel(properties);
    }

    public async Task<PropertyDetailsViewModel?> GetPropertyAsync(string name, CancellationToken cancellationToken)
    {
        var rawValues = await db.LogProperties.AsNoTracking()
            .Where(x => x.PropertyName == name)
            .GroupBy(x => x.PropertyValue)
            .Select(g => new { Value = g.Key, UsageCount = g.Count(), LatestUsageUtc = g.Max(x => x.Log.TimestampUtc) })
            .OrderByDescending(x => x.UsageCount)
            .Take(200)
            .ToListAsync(cancellationToken);

        var values = rawValues.Select(x => new PropertyValueSummaryViewModel(x.Value, x.UsageCount, x.LatestUsageUtc)).ToList();
        return values.Count == 0 ? null : new PropertyDetailsViewModel(name, values);
    }

    public async Task<TimelineViewModel?> GetTimelineAsync(string field, string value, CancellationToken cancellationToken)
    {
        var query = field.Equals("RequestId", StringComparison.OrdinalIgnoreCase)
            ? db.Logs.AsNoTracking().Where(x => x.RequestId == value)
            : db.Logs.AsNoTracking().Where(x => x.CorrelationId == value);

        var logs = await query.OrderBy(x => x.TimestampUtc)
            .Select(x => new { x.Id, x.TimestampUtc, x.Application, x.Level, x.Message, x.Source, x.Duration })
            .ToListAsync(cancellationToken);

        if (logs.Count == 0) return null;

        // The earliest Error-or-worse entry is usually the actual cause; everything chronologically after
        // it in the same chain (retries, cascading failures) is downstream noise, not new information.
        var rootCauseId = logs.Where(x => LogLevelHelper.IsError(x.Level)).Select(x => (long?)x.Id).FirstOrDefault();

        var events = logs.Select((x, index) => new TimelineEventViewModel(
            x.Id,
            x.TimestampUtc,
            x.Application,
            x.Level,
            x.Message,
            x.Source,
            x.Duration,
            index == 0 ? null : (x.TimestampUtc - logs[index - 1].TimestampUtc).TotalMilliseconds,
            x.Id == rootCauseId)).ToList();

        return new TimelineViewModel($"{field} Timeline", field, value, events);
    }

    public async Task<IReadOnlyList<string>> GetPropertyNamesAsync(CancellationToken cancellationToken) => await db.LogProperties.AsNoTracking().Select(x => x.PropertyName).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);

    /// <summary>"Which endpoint fails, and when?" — error counts for the top 10 sources by volume,
    /// bucketed by hour-of-day over the last 7 days, so a recurring nightly-batch failure or a
    /// business-hours-only endpoint issue is visible without slicing the logs manually.</summary>
    public async Task<HeatmapViewModel> GetSourceHeatmapAsync(CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var raw = await db.Logs.AsNoTracking()
            .Where(x => x.TimestampUtc >= since && x.Source != null && x.Source != "" && (x.Level == "Error" || x.Level == "Fatal" || x.Level == "Critical"))
            .GroupBy(x => new { x.Source, Hour = x.TimestampUtc.Hour })
            .Select(g => new { g.Key.Source, g.Key.Hour, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var topSources = raw.GroupBy(x => x.Source)
            .Select(g => new { Source = g.Key!, Total = g.Sum(x => x.Count) })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .Select(x => x.Source)
            .ToList();

        var rows = topSources.Select(source =>
        {
            var bySource = raw.Where(x => x.Source == source).ToDictionary(x => x.Hour, x => x.Count);
            var hourly = Enumerable.Range(0, 24).Select(hour => bySource.GetValueOrDefault(hour)).ToList();
            return new HeatmapRowViewModel(source, hourly);
        }).ToList();

        return new HeatmapViewModel(rows);
    }

    /// <summary>F-24: discovers scheduled/background jobs by convention — any log carrying a "JobName"
    /// property — and shows each job's last run outcome. No separate job-registration step required;
    /// a job just needs to log with a JobName property to show up here.</summary>
    public async Task<IReadOnlyList<JobViewModel>> GetJobsAsync(CancellationToken cancellationToken)
    {
        var jobNames = await db.LogProperties.AsNoTracking()
            .Where(p => p.PropertyName == "JobName")
            .Select(p => p.PropertyValue)
            .Distinct()
            .ToListAsync(cancellationToken);

        var jobs = new List<JobViewModel>();
        foreach (var jobName in jobNames)
        {
            if (jobName is null) continue;

            var runs = await db.LogProperties.AsNoTracking()
                .Where(p => p.PropertyName == "JobName" && p.PropertyValue == jobName)
                .Select(p => new { p.Log.TimestampUtc, p.Log.Level, p.Log.Message })
                .ToListAsync(cancellationToken);

            if (runs.Count == 0) continue;

            var lastRun = runs.OrderByDescending(x => x.TimestampUtc).First();
            var failureCount = runs.Count(x => x.Level == "Error" || x.Level == "Fatal" || x.Level == "Critical");
            jobs.Add(new JobViewModel(jobName, lastRun.TimestampUtc, lastRun.Level, lastRun.Message, runs.Count, failureCount));
        }

        return jobs.OrderByDescending(x => x.LastRunUtc).ToList();
    }

    /// <summary>"Has this happened before?" — the same bug collapsed into one row instead of hundreds of
    /// near-identical exception log lines, ranked by how recently it last fired.</summary>
    public async Task<IReadOnlyList<ExceptionGroupViewModel>> GetExceptionGroupsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var raw = await db.ExceptionFingerprints.AsNoTracking()
            .OrderByDescending(x => x.LastSeenUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return raw.Select(x => ToGroupViewModel(x, now)).ToList();
    }

    public async Task<ExceptionGroupDetailsViewModel?> GetExceptionGroupDetailsAsync(int id, CancellationToken cancellationToken)
    {
        var fingerprint = await db.ExceptionFingerprints.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (fingerprint is null)
        {
            return null;
        }

        var occurrencesRaw = await db.Logs.AsNoTracking()
            .Where(x => x.ExceptionFingerprintId == id)
            .OrderByDescending(x => x.TimestampUtc)
            .Take(20)
            .Select(x => new LogCardViewModel(x.Id, x.TimestampUtc, x.Level, x.Application, x.Environment, x.Machine, x.Source, x.Message, x.CorrelationId, x.RequestId, x.Duration))
            .ToListAsync(cancellationToken);

        var trend = await GetExceptionTrendAsync(id, cancellationToken);
        var related = await GetRelatedExceptionsAsync(fingerprint, cancellationToken);
        var breakdown = await GetUserBreakdownAsync(id, cancellationToken);

        return new ExceptionGroupDetailsViewModel(ToGroupViewModel(fingerprint, DateTime.UtcNow), occurrencesRaw, related, trend, breakdown);
    }

    /// <summary>Daily occurrence count for the last 14 days, missing days filled with zero so the sparkline
    /// doesn't visually compress when a bug has been quiet for a stretch.</summary>
    private async Task<IReadOnlyList<ChartPointViewModel>> GetExceptionTrendAsync(int fingerprintId, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.Date.AddDays(-13);
        var raw = await db.Logs.AsNoTracking()
            .Where(x => x.ExceptionFingerprintId == fingerprintId && x.TimestampUtc >= since)
            .GroupBy(x => x.TimestampUtc.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byDate = raw.ToDictionary(x => x.Date, x => x.Count);
        return Enumerable.Range(0, 14)
            .Select(offset => since.AddDays(offset))
            .Select(date => new ChartPointViewModel(date.ToString("MM-dd"), byDate.GetValueOrDefault(date)))
            .ToList();
    }

    /// <summary>Uses the SQLite FTS5 index (kept in sync by LogService on ingest) to find fingerprints with
    /// similar exception text — catches the same bug surfacing from a slightly different call site.</summary>
    private async Task<IReadOnlyList<ExceptionGroupViewModel>> GetRelatedExceptionsAsync(ExceptionFingerprint fingerprint, CancellationToken cancellationToken)
    {
        var ftsQuery = BuildFtsQuery(fingerprint.SampleMessage + " " + fingerprint.SampleStackTrace);
        if (ftsQuery is null)
        {
            return [];
        }

        var matchingFingerprints = await db.Database.SqlQuery<string>(
                $"SELECT Fingerprint AS \"Value\" FROM ExceptionSearch WHERE ExceptionSearch MATCH {ftsQuery} AND Fingerprint != {fingerprint.Fingerprint} ORDER BY bm25(ExceptionSearch) LIMIT 5")
            .ToListAsync(cancellationToken);

        if (matchingFingerprints.Count == 0)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var groups = await db.ExceptionFingerprints.AsNoTracking()
            .Where(x => matchingFingerprints.Contains(x.Fingerprint))
            .ToListAsync(cancellationToken);

        return groups.Select(x => ToGroupViewModel(x, now)).ToList();
    }

    /// <summary>"Is it one customer or everyone?" for a specific exception group.</summary>
    private async Task<IReadOnlyList<PropertyBreakdownItemViewModel>> GetUserBreakdownAsync(int fingerprintId, CancellationToken cancellationToken)
    {
        var raw = await db.LogProperties.AsNoTracking()
            .Where(p => p.PropertyName == "UserId" && p.Log.ExceptionFingerprintId == fingerprintId)
            .GroupBy(p => p.PropertyValue)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(cancellationToken);

        return raw.Select(x => new PropertyBreakdownItemViewModel(x.Value ?? "(none)", x.Count)).ToList();
    }

    private static ExceptionGroupViewModel ToGroupViewModel(ExceptionFingerprint fingerprint, DateTime now) => new(
        fingerprint.Id,
        fingerprint.SampleMessage,
        fingerprint.SampleStackTrace,
        fingerprint.Application,
        fingerprint.OccurrenceCount,
        fingerprint.FirstSeenUtc,
        fingerprint.LastSeenUtc,
        ExceptionStatusHelper.FromTimestamps(fingerprint.FirstSeenUtc, fingerprint.LastSeenUtc, now));

    /// <summary>Builds a safe FTS5 MATCH expression from free text by extracting plain word tokens and
    /// quoting each as a literal phrase — avoids FTS5 syntax errors from punctuation in stack traces.</summary>
    private static string? BuildFtsQuery(string text)
    {
        var tokens = System.Text.RegularExpressions.Regex.Matches(text, @"[A-Za-z][A-Za-z0-9_]{2,}")
            .Select(m => m.Value)
            .Distinct()
            .Take(12)
            .ToList();

        return tokens.Count == 0 ? null : string.Join(" OR ", tokens.Select(t => $"\"{t}\""));
    }
}
