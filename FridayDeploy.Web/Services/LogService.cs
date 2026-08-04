using System.Text.Json;
using FridayDeploy.Web.Data;
using FridayDeploy.Web.DTOs;
using FridayDeploy.Web.Helpers;
using FridayDeploy.Web.Models;
using FridayDeploy.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

public sealed class LogService(AppDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<long> IngestAsync(LogIngestRequest request, CancellationToken cancellationToken)
    {
        var log = BuildLog(request);
        db.Logs.Add(log);
        var newFingerprints = new List<ExceptionFingerprint>();
        await ApplyExceptionFingerprintAsync(log, newFingerprints, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await IndexNewFingerprintsAsync(newFingerprints, cancellationToken);
        return log.Id;
    }

    public async Task<int> IngestBatchAsync(IReadOnlyCollection<LogIngestRequest> requests, CancellationToken cancellationToken)
    {
        var newFingerprints = new List<ExceptionFingerprint>();
        foreach (var request in requests)
        {
            var log = BuildLog(request);
            db.Logs.Add(log);
            await ApplyExceptionFingerprintAsync(log, newFingerprints, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await IndexNewFingerprintsAsync(newFingerprints, cancellationToken);
        return requests.Count;
    }

    /// <summary>Finds or creates the ExceptionFingerprint for this log's exception (if any) and links it.
    /// Checks already-tracked (not-yet-saved) entries first so two occurrences of a brand-new fingerprint
    /// arriving in the same batch collapse into one row instead of racing to create two.</summary>
    private async Task ApplyExceptionFingerprintAsync(Log log, List<ExceptionFingerprint> newFingerprints, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(log.Exception))
        {
            return;
        }

        var fingerprint = ExceptionFingerprintHelper.Compute(log.Exception, log.StackTrace);
        var existing = db.ExceptionFingerprints.Local.FirstOrDefault(x => x.Fingerprint == fingerprint)
            ?? await db.ExceptionFingerprints.FirstOrDefaultAsync(x => x.Fingerprint == fingerprint, cancellationToken);

        if (existing is null)
        {
            existing = new ExceptionFingerprint
            {
                Fingerprint = fingerprint,
                Application = log.Application,
                SampleMessage = log.Exception,
                SampleStackTrace = log.StackTrace,
                FirstSeenUtc = log.TimestampUtc,
                LastSeenUtc = log.TimestampUtc,
                OccurrenceCount = 0
            };
            db.ExceptionFingerprints.Add(existing);
            newFingerprints.Add(existing);
        }

        existing.OccurrenceCount++;
        if (log.TimestampUtc > existing.LastSeenUtc) existing.LastSeenUtc = log.TimestampUtc;
        if (log.TimestampUtc < existing.FirstSeenUtc) existing.FirstSeenUtc = log.TimestampUtc;
        log.ExceptionFingerprint = existing;
    }

    /// <summary>Keeps the FTS5 search index (used for "related exceptions") in sync. Only new fingerprints
    /// need indexing — repeat occurrences don't change the searchable content, only the counters.</summary>
    private async Task IndexNewFingerprintsAsync(List<ExceptionFingerprint> newFingerprints, CancellationToken cancellationToken)
    {
        foreach (var fingerprint in newFingerprints)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO ExceptionSearch(Fingerprint, Content) VALUES ({fingerprint.Fingerprint}, {fingerprint.SampleMessage + " " + fingerprint.SampleStackTrace})",
                cancellationToken);
        }
    }

    private static Log BuildLog(LogIngestRequest request)
    {
        var properties = request.Properties ?? [];
        var log = new Log
        {
            TimestampUtc = request.TimestampUtc?.ToUniversalTime() ?? DateTime.UtcNow,
            Application = request.Application.Trim(),
            Environment = request.Environment,
            Version = request.Version,
            Machine = request.Machine,
            Level = request.Level.Trim(),
            Message = request.Message,
            Exception = request.Exception,
            StackTrace = request.StackTrace,
            Source = request.Source,
            RequestId = request.RequestId,
            CorrelationId = request.CorrelationId,
            ThreadId = request.ThreadId,
            Duration = request.Duration,
            IPAddress = request.IPAddress,
            PropertiesJson = JsonSerializer.Serialize(properties, JsonOptions)
        };

        foreach (var property in properties)
        {
            log.Properties.Add(new LogProperty
            {
                PropertyName = property.Key,
                PropertyType = GetPropertyType(property.Value),
                PropertyValue = GetPropertyValue(property.Value)
            });
        }

        return log;
    }

    public async Task<PagedResponse<LogResponse>> GetPagedAsync(int page, int pageSize, string? application, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Logs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(application))
        {
            query = query.Where(x => x.Application == application);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LogResponse(x.Id, x.TimestampUtc, x.Application, x.Environment, x.Machine, x.Level, x.Message, x.Source, x.CorrelationId, x.RequestId, x.Duration))
            .ToListAsync(cancellationToken);

        return new PagedResponse<LogResponse>(items, page, pageSize, total);
    }

    public Task<Log?> GetByIdAsync(long id, CancellationToken cancellationToken) => db.Logs.AsNoTracking().Include(x => x.Properties).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <summary>"What happened right before this?" — every log from the same Application + Machine in a
    /// window around the anchor event, so the actual cause (a slow query, a retry storm, a config reload)
    /// shows up without a manual time-range search.</summary>
    public async Task<IReadOnlyList<LogCardViewModel>?> GetIncidentTimelineAsync(long logId, int beforeSeconds, int afterSeconds, CancellationToken cancellationToken)
    {
        var anchor = await db.Logs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == logId, cancellationToken);
        if (anchor is null)
        {
            return null;
        }

        var from = anchor.TimestampUtc.AddSeconds(-beforeSeconds);
        var to = anchor.TimestampUtc.AddSeconds(afterSeconds);

        return await db.Logs.AsNoTracking()
            .Where(x => x.Application == anchor.Application && x.Machine == anchor.Machine && x.TimestampUtc >= from && x.TimestampUtc <= to)
            .OrderBy(x => x.TimestampUtc)
            .Select(x => new LogCardViewModel(x.Id, x.TimestampUtc, x.Level, x.Application, x.Environment, x.Machine, x.Source, x.Message, x.CorrelationId, x.RequestId, x.Duration))
            .ToListAsync(cancellationToken);
    }

    /// <summary>"What changed?" — the most recent successful (Information-level) request sharing the
    /// failing log's own property value (UserId by default), before the failure happened.</summary>
    public async Task<LastSuccessViewModel?> GetLastSuccessBeforeAsync(long logId, string propertyName, CancellationToken cancellationToken)
    {
        var failing = await db.Logs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == logId, cancellationToken);
        if (failing is null)
        {
            return null;
        }

        var propertyValue = await db.LogProperties.AsNoTracking()
            .Where(p => p.LogId == logId && p.PropertyName == propertyName)
            .Select(p => p.PropertyValue)
            .FirstOrDefaultAsync(cancellationToken);

        if (propertyValue is null)
        {
            return null;
        }

        var lastSuccess = await db.Logs.AsNoTracking()
            .Where(x => x.Level == "Information" && x.TimestampUtc < failing.TimestampUtc && x.Properties.Any(p => p.PropertyName == propertyName && p.PropertyValue == propertyValue))
            .OrderByDescending(x => x.TimestampUtc)
            .Select(x => new { x.Id, x.TimestampUtc, x.Message })
            .FirstOrDefaultAsync(cancellationToken);

        return lastSuccess is null
            ? null
            : new LastSuccessViewModel(lastSuccess.Id, lastSuccess.TimestampUtc, lastSuccess.Message, (failing.TimestampUtc - lastSuccess.TimestampUtc).TotalMinutes);
    }

    // Keeps indexed LogProperty rows small — full nested data (e.g. Serilog.Exceptions' "ExceptionDetails")
    // is never lost since it's always preserved verbatim in Log.PropertiesJson.
    private const int MaxPropertyValueLength = 1000;

    private static string GetPropertyType(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => "String",
        JsonValueKind.Number => "Number",
        JsonValueKind.True or JsonValueKind.False => "Boolean",
        JsonValueKind.Object => "Object",
        JsonValueKind.Array => "Array",
        JsonValueKind.Null => "Null",
        _ => "Unknown"
    };

    private static string? GetPropertyValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => Truncate(element.GetString()),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Object => $"{{…{element.EnumerateObject().Count()} properties…}}",
        JsonValueKind.Array => $"[…{element.GetArrayLength()} items…]",
        _ => Truncate(element.GetRawText())
    };

    private static string? Truncate(string? value) =>
        value is { Length: > MaxPropertyValueLength } ? string.Concat(value.AsSpan(0, MaxPropertyValueLength), "…") : value;
}
