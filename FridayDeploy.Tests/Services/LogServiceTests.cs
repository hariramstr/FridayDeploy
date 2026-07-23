using System.Text.Json;
using FridayDeploy.Tests.TestSupport;
using FridayDeploy.Web.DTOs;
using FridayDeploy.Web.Services;

namespace FridayDeploy.Tests.Services;

public sealed class LogServiceTests : IDisposable
{
    private readonly SqliteDbContextFactory _factory = new();

    [Fact]
    public async Task IngestAsync_persists_log_and_custom_properties()
    {
        await using var db = _factory.CreateContext();
        var service = new LogService(db);

        var request = new LogIngestRequest
        {
            Application = "TestApp",
            Level = "Error",
            Message = "boom",
            Properties = new Dictionary<string, JsonElement>
            {
                ["UserId"] = JsonSerializer.SerializeToElement(42),
                ["District"] = JsonSerializer.SerializeToElement("Salem")
            }
        };

        var id = await service.IngestAsync(request, CancellationToken.None);

        var log = await service.GetByIdAsync(id, CancellationToken.None);
        Assert.NotNull(log);
        Assert.Equal("TestApp", log!.Application);
        Assert.Equal(2, log.Properties.Count);
        Assert.Contains(log.Properties, p => p.PropertyName == "UserId" && p.PropertyValue == "42");
        Assert.Contains(log.Properties, p => p.PropertyName == "District" && p.PropertyValue == "Salem");
    }

    [Fact]
    public async Task IngestBatchAsync_persists_every_event_in_one_call()
    {
        await using var db = _factory.CreateContext();
        var service = new LogService(db);

        var requests = new List<LogIngestRequest>
        {
            new() { Application = "TestApp", Level = "Information", Message = "one" },
            new() { Application = "TestApp", Level = "Warning", Message = "two" }
        };

        var count = await service.IngestBatchAsync(requests, CancellationToken.None);

        Assert.Equal(2, count);
        var page = await service.GetPagedAsync(1, 10, "TestApp", CancellationToken.None);
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_filters_by_application_and_paginates()
    {
        await using var db = _factory.CreateContext();
        var service = new LogService(db);
        await service.IngestBatchAsync(
        [
            new LogIngestRequest { Application = "A", Level = "Information", Message = "1" },
            new LogIngestRequest { Application = "A", Level = "Information", Message = "2" },
            new LogIngestRequest { Application = "B", Level = "Information", Message = "3" }
        ], CancellationToken.None);

        var pageA = await service.GetPagedAsync(1, 1, "A", CancellationToken.None);

        Assert.Equal(2, pageA.TotalCount);
        Assert.Single(pageA.Items);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        await using var db = _factory.CreateContext();
        var service = new LogService(db);

        Assert.Null(await service.GetByIdAsync(999, CancellationToken.None));
    }

    public void Dispose() => _factory.Dispose();
}
