namespace FridayDeploy.Web.ViewModels;

public sealed record PropertyExplorerViewModel(IReadOnlyList<PropertySummaryViewModel> Properties);

public sealed record PropertySummaryViewModel(string Name, int UsageCount, int DistinctValues, DateTime LatestUsageUtc);

public sealed record PropertyDetailsViewModel(string Name, IReadOnlyList<PropertyValueSummaryViewModel> Values);

public sealed record PropertyValueSummaryViewModel(string? Value, int UsageCount, DateTime LatestUsageUtc);

public sealed record TimelineViewModel(string Title, string Field, string Value, IReadOnlyList<TimelineEventViewModel> Events, bool Live = false, int PollIntervalMs = 5000);

public sealed record TimelineEventViewModel(long Id, DateTime TimestampUtc, string Application, string Level, string Message, string? Source, double? Duration, double? GapMs, bool IsRootCauseCandidate);

public sealed record ExceptionSummaryViewModel(string Exception, int Count, DateTime LastSeenUtc, string Application);

public sealed record ChartPointViewModel(string Label, int Count);

public sealed record LastSuccessViewModel(long LogId, DateTime TimestampUtc, string Message, double MinutesBeforeFailure);

public sealed record LogInvestigationViewModel(ExceptionGroupViewModel? ExceptionStatus, TimelineEventViewModel? RootCause, bool IsRootCause, LastSuccessViewModel? LastSuccess);

/// <summary>F-37: which source/endpoint tends to fail at which hour of day, over the last 7 days.</summary>
public sealed record HeatmapViewModel(IReadOnlyList<HeatmapRowViewModel> Rows);

public sealed record HeatmapRowViewModel(string Source, IReadOnlyList<int> HourlyErrorCounts);

/// <summary>F-24: background/scheduled jobs, discovered by convention from a "JobName" property on
/// ingested logs — no separate job-registration API needed.</summary>
public sealed record JobViewModel(string JobName, DateTime LastRunUtc, string LastRunLevel, string LastRunMessage, int RunCount, int FailureCount);
