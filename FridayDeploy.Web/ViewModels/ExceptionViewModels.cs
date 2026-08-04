namespace FridayDeploy.Web.ViewModels;

public sealed record ExceptionGroupViewModel(
    int Id,
    string SampleMessage,
    string? SampleStackTrace,
    string Application,
    int OccurrenceCount,
    DateTime FirstSeenUtc,
    DateTime LastSeenUtc,
    string Status);

public sealed record ExceptionGroupDetailsViewModel(
    ExceptionGroupViewModel Group,
    IReadOnlyList<LogCardViewModel> RecentOccurrences,
    IReadOnlyList<ExceptionGroupViewModel> Related,
    IReadOnlyList<ChartPointViewModel> DailyTrend,
    IReadOnlyList<PropertyBreakdownItemViewModel> UserBreakdown);

public sealed record PropertyBreakdownItemViewModel(string Value, int Count);
