using FridayDeploy.Web.Data;
using FridayDeploy.Web.Helpers;
using FridayDeploy.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

/// <summary>Composes "what should I look at next?" for a single log event from three independent
/// signals (has this exception happened before, what's the root cause of this chain, what changed
/// before it failed) so Log Details can answer it without three separate manual searches.</summary>
public sealed class LogInvestigationService(AppDbContext db, ExplorerService explorerService, LogService logService)
{
    public async Task<LogInvestigationViewModel?> GetAsync(long logId, CancellationToken cancellationToken)
    {
        var log = await db.Logs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == logId, cancellationToken);
        if (log is null)
        {
            return null;
        }

        var exceptionStatusTask = log.ExceptionFingerprintId is int fingerprintId
            ? GetExceptionStatusAsync(fingerprintId, cancellationToken)
            : Task.FromResult<ExceptionGroupViewModel?>(null);

        var timelineTask = !string.IsNullOrWhiteSpace(log.CorrelationId)
            ? explorerService.GetTimelineAsync("CorrelationId", log.CorrelationId, cancellationToken)
            : Task.FromResult<TimelineViewModel?>(null);

        var lastSuccessTask = LogLevelHelper.IsError(log.Level)
            ? logService.GetLastSuccessBeforeAsync(logId, "UserId", cancellationToken)
            : Task.FromResult<LastSuccessViewModel?>(null);

        await Task.WhenAll(exceptionStatusTask, timelineTask, lastSuccessTask);

        var rootCause = timelineTask.Result?.Events.FirstOrDefault(x => x.IsRootCauseCandidate);
        var isRootCause = rootCause is not null && rootCause.Id == logId;

        return new LogInvestigationViewModel(exceptionStatusTask.Result, rootCause, isRootCause, lastSuccessTask.Result);
    }

    private async Task<ExceptionGroupViewModel?> GetExceptionStatusAsync(int fingerprintId, CancellationToken cancellationToken)
    {
        var fingerprint = await db.ExceptionFingerprints.AsNoTracking().FirstOrDefaultAsync(x => x.Id == fingerprintId, cancellationToken);
        return fingerprint is null
            ? null
            : new ExceptionGroupViewModel(
                fingerprint.Id,
                fingerprint.SampleMessage,
                fingerprint.SampleStackTrace,
                fingerprint.Application,
                fingerprint.OccurrenceCount,
                fingerprint.FirstSeenUtc,
                fingerprint.LastSeenUtc,
                ExceptionStatusHelper.FromTimestamps(fingerprint.FirstSeenUtc, fingerprint.LastSeenUtc, DateTime.UtcNow));
    }
}
