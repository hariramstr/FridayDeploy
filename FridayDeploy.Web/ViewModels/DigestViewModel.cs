namespace FridayDeploy.Web.ViewModels;

/// <summary>F-42: a weekly composite of "what happened" — top recurring bugs, top failing endpoints,
/// per-app volume, and anything brand-new — so a Monday catch-up doesn't require piecing it together
/// from several separate pages.</summary>
public sealed record DigestViewModel(
    IReadOnlyList<ExceptionGroupViewModel> TopExceptions,
    IReadOnlyList<TopFailingSourceViewModel> TopFailingSources,
    IReadOnlyList<AppWeeklyTotalViewModel> AppTotals,
    IReadOnlyList<ExceptionGroupViewModel> NewExceptionsThisWeek,
    IReadOnlyList<ForecastViewModel> Forecasts);

public sealed record AppWeeklyTotalViewModel(string Application, int LogCount, int ErrorCount);

/// <summary>F-43: a simple least-squares linear trend over the last 14 daily totals, so "is traffic
/// growing?" has a number instead of a guess. No statistics library — plain arithmetic.</summary>
public sealed record ForecastViewModel(string Application, double DailyAverage, double TrendSlopePerDay, double PredictedNextDayCount);
