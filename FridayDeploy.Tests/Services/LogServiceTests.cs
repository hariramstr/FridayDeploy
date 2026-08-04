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
    public async Task IngestAsync_summarizes_large_nested_properties_instead_of_indexing_raw_json()
    {
        // Mirrors what Serilog.Exceptions' WithExceptionDetails() enricher produces: a deeply nested
        // object property. It must not end up as a giant indexed LogProperty value — the full data still
        // lives in PropertiesJson, but the per-property row should stay small and summarized.
        await using var db = _factory.CreateContext();
        var service = new LogService(db);

        var exceptionDetails = JsonSerializer.SerializeToElement(new
        {
            Type = "System.InvalidOperationException",
            Message = "boom",
            Data = new Dictionary<string, string>(),
            InnerException = (object?)null
        });
        var tags = JsonSerializer.SerializeToElement(new[] { "a", "b", "c" });

        var request = new LogIngestRequest
        {
            Application = "TestApp",
            Level = "Error",
            Message = "boom",
            Properties = new Dictionary<string, JsonElement>
            {
                ["ExceptionDetails"] = exceptionDetails,
                ["Tags"] = tags
            }
        };

        var id = await service.IngestAsync(request, CancellationToken.None);

        var log = await service.GetByIdAsync(id, CancellationToken.None);
        Assert.NotNull(log);
        var exceptionProp = log!.Properties.Single(p => p.PropertyName == "ExceptionDetails");
        Assert.Equal("Object", exceptionProp.PropertyType);
        Assert.DoesNotContain("InvalidOperationException", exceptionProp.PropertyValue);
        Assert.Contains("properties", exceptionProp.PropertyValue);

        var tagsProp = log.Properties.Single(p => p.PropertyName == "Tags");
        Assert.Equal("Array", tagsProp.PropertyType);
        Assert.Contains("3 items", tagsProp.PropertyValue);

        // Nothing is lost — the full structure is still in the raw properties JSON on the log itself.
        Assert.Contains("InvalidOperationException", log.PropertiesJson);
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
