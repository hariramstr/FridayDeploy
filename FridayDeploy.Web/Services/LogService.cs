using System.Text.Json;
using FridayDeploy.Web.Data;
using FridayDeploy.Web.DTOs;
using FridayDeploy.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

public sealed class LogService(AppDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<long> IngestAsync(LogIngestRequest request, CancellationToken cancellationToken)
    {
        var log = BuildLog(request);
        db.Logs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
        return log.Id;
    }

    public async Task<int> IngestBatchAsync(IReadOnlyCollection<LogIngestRequest> requests, CancellationToken cancellationToken)
    {
        foreach (var request in requests)
        {
            db.Logs.Add(BuildLog(request));
        }

        await db.SaveChangesAsync(cancellationToken);
        return requests.Count;
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
