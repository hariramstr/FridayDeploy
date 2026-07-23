using System.Globalization;
using System.Text;
using System.Text.Json;
using FridayDeploy.Web.DTOs;
using FridayDeploy.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

public enum ExportFormat { Csv, Json, Txt }

public sealed class ExportService(LogSearchService logSearchService)
{
    private const int MaxRows = 10_000;

    public async Task WriteAsync(Stream destination, LogSearchRequest request, ExportFormat format, CancellationToken cancellationToken)
    {
        var logs = await logSearchService.BuildQuery(request).Take(MaxRows).ToListAsync(cancellationToken);
        await using var writer = new StreamWriter(destination, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);

        switch (format)
        {
            case ExportFormat.Csv:
                await WriteCsvAsync(writer, logs);
                break;
            case ExportFormat.Json:
                await WriteJsonAsync(writer, logs, cancellationToken);
                break;
            default:
                await WriteTxtAsync(writer, logs);
                break;
        }

        await writer.FlushAsync(cancellationToken);
    }

    private static async Task WriteCsvAsync(StreamWriter writer, IReadOnlyList<Log> logs)
    {
        await writer.WriteLineAsync("Id,TimestampUtc,Application,Environment,Machine,Level,Message,Source,CorrelationId,RequestId,Duration");
        foreach (var log in logs)
        {
            await writer.WriteLineAsync(string.Join(',',
                log.Id,
                log.TimestampUtc.ToString("O"),
                CsvField(log.Application),
                CsvField(log.Environment),
                CsvField(log.Machine),
                CsvField(log.Level),
                CsvField(log.Message),
                CsvField(log.Source),
                CsvField(log.CorrelationId),
                CsvField(log.RequestId),
                log.Duration?.ToString(CultureInfo.InvariantCulture) ?? ""));
        }
    }

    private static string CsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var needsQuoting = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        var escaped = value.Replace("\"", "\"\"");
        return needsQuoting ? $"\"{escaped}\"" : escaped;
    }

    private static async Task WriteJsonAsync(StreamWriter writer, IReadOnlyList<Log> logs, CancellationToken cancellationToken)
    {
        var payload = logs.Select(x => new LogResponse(x.Id, x.TimestampUtc, x.Application, x.Environment, x.Machine, x.Level, x.Message, x.Source, x.CorrelationId, x.RequestId, x.Duration));
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        await writer.WriteAsync(json.AsMemory(), cancellationToken);
    }

    private static async Task WriteTxtAsync(StreamWriter writer, IReadOnlyList<Log> logs)
    {
        foreach (var log in logs)
        {
            await writer.WriteLineAsync($"[{log.TimestampUtc:O}] {log.Level,-11} {log.Application} — {log.Message}");
        }
    }
}
