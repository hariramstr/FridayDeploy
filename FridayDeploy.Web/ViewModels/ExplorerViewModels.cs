namespace FridayDeploy.Web.ViewModels;

public sealed record PropertyExplorerViewModel(IReadOnlyList<PropertySummaryViewModel> Properties);

public sealed record PropertySummaryViewModel(string Name, int UsageCount, int DistinctValues, DateTime LatestUsageUtc);

public sealed record PropertyDetailsViewModel(string Name, IReadOnlyList<PropertyValueSummaryViewModel> Values);

public sealed record PropertyValueSummaryViewModel(string? Value, int UsageCount, DateTime LatestUsageUtc);

public sealed record TimelineViewModel(string Title, string Field, string Value, IReadOnlyList<TimelineEventViewModel> Events);

public sealed record TimelineEventViewModel(long Id, DateTime TimestampUtc, string Application, string Level, string Message, string? Source, double? Duration, double? GapMs);

public sealed record ExceptionSummaryViewModel(string Exception, int Count, DateTime LastSeenUtc, string Application);

public sealed record ChartPointViewModel(string Label, int Count);
