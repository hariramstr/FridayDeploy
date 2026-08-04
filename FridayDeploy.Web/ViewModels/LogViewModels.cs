namespace FridayDeploy.Web.ViewModels;

public sealed record LogsPageViewModel(
    IReadOnlyList<LogGroupViewModel> Logs,
    string? Application,
    string? Query,
    int Page,
    int TotalPages,
    IReadOnlyList<string> Applications,
    IReadOnlyList<string> Environments,
    IReadOnlyList<string> Levels,
    IReadOnlyList<string> Machines,
    IReadOnlyList<string> PropertyNames,
    string GeneratedExpression,
    bool Live,
    IReadOnlyList<SavedSearchViewModel> SavedSearches,
    int SlowRequestThresholdMs);

public sealed record SavedSearchViewModel(int Id, string Name, string QueryString);

public sealed record LogCardViewModel(long Id, DateTime TimestampUtc, string Level, string Application, string? Environment, string? Machine, string? Source, string Message, string? CorrelationId, string? RequestId, double? Duration, bool IsSlow = false);

/// <summary>A displayed row on the Logs page: either a single event, or consecutive near-identical
/// events (same Application/Level/Message/Source) collapsed into one with a repeat count.</summary>
public sealed record LogGroupViewModel(LogCardViewModel Log, int RepeatCount);

public sealed record LogDetailsViewModel(
    long Id,
    DateTime TimestampUtc,
    string Application,
    string Level,
    string Message,
    string? Exception,
    string? StackTrace,
    string? RequestId,
    string? CorrelationId,
    string? Machine,
    string? Environment,
    string? Version,
    string? Source,
    string? IPAddress,
    string? ThreadId,
    double? Duration,
    string RawJson,
    IReadOnlyList<LogPropertyViewModel> Properties,
    IReadOnlyList<LogNoteViewModel> Notes,
    LogInvestigationViewModel? Investigation,
    IReadOnlyList<LogCardViewModel>? IncidentTimeline);

public sealed record LogPropertyViewModel(string Name, string? Value, string Type);

public sealed record LogNoteViewModel(int Id, string Author, string Body, DateTime CreatedUtc);
