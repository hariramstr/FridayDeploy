using FridayDeploy.Web.Data;
using FridayDeploy.Web.DTOs;
using FridayDeploy.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

public sealed class LogSearchService(AppDbContext db)
{
    private static readonly HashSet<string> LogFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Application", "Environment", "Version", "Machine", "Level", "Message", "Exception", "Source", "RequestId", "CorrelationId", "ThreadId", "IPAddress"
    };

    public IQueryable<Log> BuildQuery(LogSearchRequest request)
    {
        var query = db.Logs.AsNoTracking();

        query = ApplyEquals(query, nameof(Log.Application), request.Application);
        query = ApplyEquals(query, nameof(Log.Environment), request.Environment);
        query = ApplyEquals(query, nameof(Log.Level), request.Level);
        query = ApplyEquals(query, nameof(Log.Machine), request.Machine);
        query = ApplyEquals(query, nameof(Log.CorrelationId), request.CorrelationId);
        query = ApplyEquals(query, nameof(Log.RequestId), request.RequestId);
        query = ApplyContains(query, nameof(Log.Source), request.Source);

        if (request.FromUtc is not null) query = query.Where(x => x.TimestampUtc >= request.FromUtc.Value.ToUniversalTime());
        if (request.ToUtc is not null) query = query.Where(x => x.TimestampUtc <= request.ToUtc.Value.ToUniversalTime());
        if (request.MinDuration is not null) query = query.Where(x => x.Duration >= request.MinDuration);
        if (request.MaxDuration is not null) query = query.Where(x => x.Duration <= request.MaxDuration);
        if (!string.IsNullOrWhiteSpace(request.PropertyName)) query = query.Where(x => x.Properties.Any(p => p.PropertyName == request.PropertyName));
        if (!string.IsNullOrWhiteSpace(request.PropertyValue)) query = query.Where(x => x.Properties.Any(p => p.PropertyValue != null && p.PropertyValue.Contains(request.PropertyValue)));
        if (!string.IsNullOrWhiteSpace(request.ContainsText)) query = ApplyGlobalText(query, request.ContainsText);

        foreach (var filter in Parse(request.Query))
        {
            query = ApplyFilter(query, filter);
        }

        return request.Sort.Equals("timestamp_asc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.TimestampUtc)
            : query.OrderByDescending(x => x.TimestampUtc);
    }

    public async Task<PagedResponse<LogResponse>> SearchAsync(LogSearchRequest request, CancellationToken cancellationToken)
    {
        request.Page = Math.Max(1, request.Page);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = BuildQuery(request);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new LogResponse(x.Id, x.TimestampUtc, x.Application, x.Environment, x.Machine, x.Level, x.Message, x.Source, x.CorrelationId, x.RequestId, x.Duration))
            .ToListAsync(cancellationToken);
        return new PagedResponse<LogResponse>(items, request.Page, request.PageSize, total);
    }

    public static IReadOnlyList<SearchFilter> Parse(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return [];
        var parts = expression.Split(" AND ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Select(ParseFilter).Where(x => x is not null).Cast<SearchFilter>().ToList();
    }

    private static SearchFilter? ParseFilter(string part)
    {
        var op = part.Contains('~') ? "~" : part.Contains('=') ? "=" : string.Empty;
        if (op.Length == 0) return new SearchFilter("*", "~", part.Trim());
        var pieces = part.Split(op, 2, StringSplitOptions.TrimEntries);
        return pieces.Length == 2 && pieces[0].Length > 0 ? new SearchFilter(pieces[0], op, pieces[1]) : null;
    }

    private static IQueryable<Log> ApplyFilter(IQueryable<Log> query, SearchFilter filter)
    {
        if (filter.Field == "*") return ApplyGlobalText(query, filter.Value);
        if (!LogFields.Contains(filter.Field))
        {
            return filter.Operator == "="
                ? query.Where(x => x.Properties.Any(p => p.PropertyName == filter.Field && p.PropertyValue == filter.Value))
                : query.Where(x => x.Properties.Any(p => p.PropertyName == filter.Field && p.PropertyValue != null && p.PropertyValue.Contains(filter.Value)));
        }

        return filter.Operator == "=" ? ApplyEquals(query, filter.Field, filter.Value) : ApplyContains(query, filter.Field, filter.Value);
    }

    private static IQueryable<Log> ApplyEquals(IQueryable<Log> query, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return query;
        return field.ToLowerInvariant() switch
        {
            "application" => query.Where(x => x.Application == value),
            "environment" => query.Where(x => x.Environment == value),
            "version" => query.Where(x => x.Version == value),
            "machine" => query.Where(x => x.Machine == value),
            "level" => query.Where(x => x.Level == value),
            "message" => query.Where(x => x.Message == value),
            "exception" => query.Where(x => x.Exception == value),
            "source" => query.Where(x => x.Source == value),
            "requestid" => query.Where(x => x.RequestId == value),
            "correlationid" => query.Where(x => x.CorrelationId == value),
            "threadid" => query.Where(x => x.ThreadId == value),
            "ipaddress" => query.Where(x => x.IPAddress == value),
            _ => query
        };
    }

    private static IQueryable<Log> ApplyContains(IQueryable<Log> query, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return query;
        return field.ToLowerInvariant() switch
        {
            "application" => query.Where(x => x.Application.Contains(value)),
            "environment" => query.Where(x => x.Environment != null && x.Environment.Contains(value)),
            "version" => query.Where(x => x.Version != null && x.Version.Contains(value)),
            "machine" => query.Where(x => x.Machine != null && x.Machine.Contains(value)),
            "level" => query.Where(x => x.Level.Contains(value)),
            "message" => query.Where(x => x.Message.Contains(value)),
            "exception" => query.Where(x => x.Exception != null && x.Exception.Contains(value)),
            "source" => query.Where(x => x.Source != null && x.Source.Contains(value)),
            "requestid" => query.Where(x => x.RequestId != null && x.RequestId.Contains(value)),
            "correlationid" => query.Where(x => x.CorrelationId != null && x.CorrelationId.Contains(value)),
            "threadid" => query.Where(x => x.ThreadId != null && x.ThreadId.Contains(value)),
            "ipaddress" => query.Where(x => x.IPAddress != null && x.IPAddress.Contains(value)),
            _ => query
        };
    }

    private static IQueryable<Log> ApplyGlobalText(IQueryable<Log> query, string value) => query.Where(x =>
        x.Message.Contains(value) ||
        x.Application.Contains(value) ||
        x.Level.Contains(value) ||
        (x.Exception != null && x.Exception.Contains(value)) ||
        (x.Machine != null && x.Machine.Contains(value)) ||
        (x.Source != null && x.Source.Contains(value)) ||
        (x.Environment != null && x.Environment.Contains(value)) ||
        (x.Version != null && x.Version.Contains(value)) ||
        (x.RequestId != null && x.RequestId.Contains(value)) ||
        (x.CorrelationId != null && x.CorrelationId.Contains(value)) ||
        x.Properties.Any(p => p.PropertyName.Contains(value) || (p.PropertyValue != null && p.PropertyValue.Contains(value))));
}
