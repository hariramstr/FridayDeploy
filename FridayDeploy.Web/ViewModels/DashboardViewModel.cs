namespace FridayDeploy.Web.ViewModels;

public sealed record DashboardViewModel(
    int TodaysLogs,
    int Errors,
    int Warnings,
    int Applications,
    int OnlineApplications,
    int WarningApplications,
    int OfflineApplications,
    double AverageLogsPerMinute,
    LatestLogViewModel? LatestLog,
    LatestLogViewModel? LatestError,
    DeploymentActivityViewModel? LatestDeploymentActivity,
    IReadOnlyList<ChartPointViewModel> TopApplications,
    IReadOnlyList<ExceptionSummaryViewModel> TopExceptions,
    IReadOnlyList<ChartPointViewModel> LogsPerHour,
    IReadOnlyList<ChartPointViewModel> ErrorsPerHour,
    IReadOnlyList<ChartPointViewModel> LastSevenDays,
    IReadOnlyList<TopFailingSourceViewModel> TopFailingSources,
    IReadOnlyList<AnomalyViewModel> Anomalies);

public sealed record LatestLogViewModel(long Id, DateTime TimestampUtc, string Application, string Level, string Message);

public sealed record DeploymentActivityViewModel(string Application, DateTime FirstSeenUtc);

/// <summary>F-07: the endpoints/sources generating the most errors today, so support can see "what's
/// actually broken" without manually grouping the logs list by Source.</summary>
public sealed record TopFailingSourceViewModel(string Source, string Application, int Count);

/// <summary>F-08: this hour's volume for an application compared to its average for this same hour
/// over the last 7 days — flags a spike without needing a dedicated statistics library.</summary>
public sealed record AnomalyViewModel(string Application, int CurrentHourCount, double BaselineHourCount, bool IsSpike);
