using FridayDeploy.Tests.TestSupport;
using FridayDeploy.Web.DTOs;
using FridayDeploy.Web.Services;

namespace FridayDeploy.Tests.Services;

public sealed class LogSearchServiceTests : IDisposable
{
    private readonly SqliteDbContextFactory _factory = new();

    [Theory]
    [InlineData("Level=Error", "Level", "=", "Error")]
    [InlineData("District~Salem", "District", "~", "Salem")]
    [InlineData("just text", "*", "~", "just text")]
    public void Parse_reads_a_single_filter(string expression, string field, string op, string value)
    {
        var filters = LogSearchService.Parse(expression);

        var filter = Assert.Single(filters);
        Assert.Equal(field, filter.Field);
        Assert.Equal(op, filter.Operator);
        Assert.Equal(value, filter.Value);
    }

    [Fact]
    public void Parse_reads_multiple_AND_ed_filters()
    {
        var filters = LogSearchService.Parse("Level=Error AND District~Salem");

        Assert.Equal(2, filters.Count);
        Assert.Equal("Level", filters[0].Field);
        Assert.Equal("District", filters[1].Field);
    }

    [Fact]
    public void Parse_returns_empty_for_blank_expression()
    {
        Assert.Empty(LogSearchService.Parse(null));
        Assert.Empty(LogSearchService.Parse("   "));
    }

    [Fact]
    public async Task SearchAsync_filters_by_application_and_level()
    {
        await using var db = _factory.CreateContext();
        var logService = new LogService(db);
        await logService.IngestBatchAsync(
        [
            new LogIngestRequest { Application = "A", Level = "Error", Message = "fails" },
            new LogIngestRequest { Application = "A", Level = "Information", Message = "ok" },
            new LogIngestRequest { Application = "B", Level = "Error", Message = "also fails" }
        ], CancellationToken.None);

        var searchService = new LogSearchService(db);
        var result = await searchService.SearchAsync(new LogSearchRequest { Application = "A", Level = "Error" }, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("fails", item.Message);
    }

    [Fact]
    public async Task SearchAsync_supports_property_based_query_expression()
    {
        await using var db = _factory.CreateContext();
        var logService = new LogService(db);
        await logService.IngestAsync(new LogIngestRequest
        {
            Application = "A",
            Level = "Error",
            Message = "loan failed",
            Properties = new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["District"] = System.Text.Json.JsonSerializer.SerializeToElement("Salem")
            }
        }, CancellationToken.None);
        await logService.IngestAsync(new LogIngestRequest { Application = "A", Level = "Error", Message = "unrelated" }, CancellationToken.None);

        var searchService = new LogSearchService(db);
        var result = await searchService.SearchAsync(new LogSearchRequest { Query = "District=Salem" }, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("loan failed", item.Message);
    }

    public void Dispose() => _factory.Dispose();
}
