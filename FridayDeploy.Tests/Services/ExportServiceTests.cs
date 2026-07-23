using FridayDeploy.Tests.TestSupport;
using FridayDeploy.Web.DTOs;
using FridayDeploy.Web.Services;

namespace FridayDeploy.Tests.Services;

public sealed class ExportServiceTests : IDisposable
{
    private readonly SqliteDbContextFactory _factory = new();

    private async Task<ExportService> SeedAsync()
    {
        var db = _factory.CreateContext();
        var logService = new LogService(db);
        await logService.IngestAsync(new LogIngestRequest { Application = "A", Level = "Error", Message = "fails, badly", CorrelationId = "c1" }, CancellationToken.None);
        return new ExportService(new LogSearchService(db));
    }

    [Fact]
    public async Task WriteAsync_csv_escapes_commas_and_includes_header()
    {
        var export = await SeedAsync();
        using var stream = new MemoryStream();

        await export.WriteAsync(stream, new LogSearchRequest(), ExportFormat.Csv, CancellationToken.None);

        var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.StartsWith("Id,TimestampUtc,Application", text);
        Assert.Contains("\"fails, badly\"", text);
        Assert.Contains("c1", text);
    }

    [Fact]
    public async Task WriteAsync_json_produces_a_parseable_array()
    {
        var export = await SeedAsync();
        using var stream = new MemoryStream();

        await export.WriteAsync(stream, new LogSearchRequest(), ExportFormat.Json, CancellationToken.None);

        var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        var parsed = System.Text.Json.JsonDocument.Parse(text);
        Assert.Equal(1, parsed.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task WriteAsync_txt_includes_level_and_message()
    {
        var export = await SeedAsync();
        using var stream = new MemoryStream();

        await export.WriteAsync(stream, new LogSearchRequest(), ExportFormat.Txt, CancellationToken.None);

        var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("Error", text);
        Assert.Contains("fails, badly", text);
    }

    public void Dispose() => _factory.Dispose();
}
