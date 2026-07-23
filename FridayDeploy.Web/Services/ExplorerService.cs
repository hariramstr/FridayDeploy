using FridayDeploy.Web.Data;
using FridayDeploy.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

public sealed class ExplorerService(AppDbContext db)
{
    public async Task<PropertyExplorerViewModel> GetPropertiesAsync(CancellationToken cancellationToken)
    {
        var rawProperties = await db.LogProperties.AsNoTracking()
            .GroupBy(x => x.PropertyName)
            .Select(g => new
            {
                Name = g.Key,
                UsageCount = g.Count(),
                DistinctValues = g.Select(x => x.PropertyValue).Distinct().Count(),
                LatestUsageUtc = g.Max(x => x.Log.TimestampUtc)
            })
            .OrderByDescending(x => x.UsageCount)
            .ToListAsync(cancellationToken);

        var properties = rawProperties.Select(x => new PropertySummaryViewModel(x.Name, x.UsageCount, x.DistinctValues, x.LatestUsageUtc)).ToList();
        return new PropertyExplorerViewModel(properties);
    }

    public async Task<PropertyDetailsViewModel?> GetPropertyAsync(string name, CancellationToken cancellationToken)
    {
        var rawValues = await db.LogProperties.AsNoTracking()
            .Where(x => x.PropertyName == name)
            .GroupBy(x => x.PropertyValue)
            .Select(g => new { Value = g.Key, UsageCount = g.Count(), LatestUsageUtc = g.Max(x => x.Log.TimestampUtc) })
            .OrderByDescending(x => x.UsageCount)
            .Take(200)
            .ToListAsync(cancellationToken);

        var values = rawValues.Select(x => new PropertyValueSummaryViewModel(x.Value, x.UsageCount, x.LatestUsageUtc)).ToList();
        return values.Count == 0 ? null : new PropertyDetailsViewModel(name, values);
    }

    public async Task<TimelineViewModel?> GetTimelineAsync(string field, string value, CancellationToken cancellationToken)
    {
        var query = field.Equals("RequestId", StringComparison.OrdinalIgnoreCase)
            ? db.Logs.AsNoTracking().Where(x => x.RequestId == value)
            : db.Logs.AsNoTracking().Where(x => x.CorrelationId == value);

        var logs = await query.OrderBy(x => x.TimestampUtc)
            .Select(x => new { x.Id, x.TimestampUtc, x.Application, x.Level, x.Message, x.Source, x.Duration })
            .ToListAsync(cancellationToken);

        if (logs.Count == 0) return null;

        var events = logs.Select((x, index) => new TimelineEventViewModel(
            x.Id,
            x.TimestampUtc,
            x.Application,
            x.Level,
            x.Message,
            x.Source,
            x.Duration,
            index == 0 ? null : (x.TimestampUtc - logs[index - 1].TimestampUtc).TotalMilliseconds)).ToList();

        return new TimelineViewModel($"{field} Timeline", field, value, events);
    }

    public async Task<IReadOnlyList<ExceptionSummaryViewModel>> GetExceptionsAsync(CancellationToken cancellationToken)
    {
        var raw = await db.Logs.AsNoTracking()
            .Where(x => x.Exception != null && x.Exception != "")
            .GroupBy(x => x.Exception!)
            .Select(g => new { Exception = g.Key, Count = g.Count(), LastSeenUtc = g.Max(x => x.TimestampUtc), Application = g.OrderByDescending(x => x.TimestampUtc).Select(x => x.Application).First() })
            .OrderByDescending(x => x.Count)
            .Take(100)
            .ToListAsync(cancellationToken);

        return raw.Select(x => new ExceptionSummaryViewModel(x.Exception, x.Count, x.LastSeenUtc, x.Application)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetPropertyNamesAsync(CancellationToken cancellationToken) => await db.LogProperties.AsNoTracking().Select(x => x.PropertyName).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
}
