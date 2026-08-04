using FridayDeploy.Tests.TestSupport;
using FridayDeploy.Web.DTOs;
using FridayDeploy.Web.Helpers;
using FridayDeploy.Web.Services;

namespace FridayDeploy.Tests.Services;

public sealed class ExceptionFingerprintTests : IDisposable
{
    private readonly SqliteDbContextFactory _factory = new();

    [Fact]
    public void Compute_ignores_varying_ids_and_line_numbers()
    {
        var fingerprintA = ExceptionFingerprintHelper.Compute(
            "Order 4821 failed for user 55231",
            "   at GreatWave.CMS.PageService.Save() in Program.cs:line 42");
        var fingerprintB = ExceptionFingerprintHelper.Compute(
            "Order 9910 failed for user 12",
            "   at GreatWave.CMS.PageService.Save() in Program.cs:line 88");

        Assert.Equal(fingerprintA, fingerprintB);
    }

    [Fact]
    public void Compute_distinguishes_different_stack_frames()
    {
        var fingerprintA = ExceptionFingerprintHelper.Compute("boom", "   at A.B.Save()");
        var fingerprintB = ExceptionFingerprintHelper.Compute("boom", "   at X.Y.Load()");

        Assert.NotEqual(fingerprintA, fingerprintB);
    }

    [Fact]
    public async Task IngestBatchAsync_groups_repeated_occurrences_into_one_fingerprint()
    {
        await using var db = _factory.CreateContext();
        var service = new LogService(db);

        await service.IngestBatchAsync(
        [
            new LogIngestRequest { Application = "A", Level = "Error", Message = "m1", Exception = "Order 1 failed", StackTrace = "   at Svc.Save() in F.cs:line 10" },
            new LogIngestRequest { Application = "A", Level = "Error", Message = "m2", Exception = "Order 2 failed", StackTrace = "   at Svc.Save() in F.cs:line 20" },
            new LogIngestRequest { Application = "A", Level = "Information", Message = "no exception here" }
        ], CancellationToken.None);

        var fingerprints = db.ExceptionFingerprints.ToList();
        var fingerprint = Assert.Single(fingerprints);
        Assert.Equal(2, fingerprint.OccurrenceCount);

        var logsWithFingerprint = db.Logs.Where(x => x.ExceptionFingerprintId != null).ToList();
        Assert.Equal(2, logsWithFingerprint.Count);
    }

    [Fact]
    public async Task GetExceptionGroupsAsync_reflects_status_and_occurrence_count()
    {
        await using var db = _factory.CreateContext();
        var logService = new LogService(db);
        await logService.IngestBatchAsync(
        [
            new LogIngestRequest { Application = "A", Level = "Error", Message = "m1", Exception = "Db unavailable", StackTrace = "   at Data.Connect()" },
            new LogIngestRequest { Application = "A", Level = "Error", Message = "m2", Exception = "Db unavailable", StackTrace = "   at Data.Connect()" }
        ], CancellationToken.None);

        var explorer = new ExplorerService(db);
        var groups = await explorer.GetExceptionGroupsAsync(CancellationToken.None);

        var group = Assert.Single(groups);
        Assert.Equal(2, group.OccurrenceCount);
        Assert.Equal("New", group.Status);
    }

    public void Dispose() => _factory.Dispose();
}
