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
    IReadOnlyList<ChartPointViewModel> LastSevenDays);

public sealed record LatestLogViewModel(long Id, DateTime TimestampUtc, string Application, string Level, string Message);

public sealed record DeploymentActivityViewModel(string Application, DateTime FirstSeenUtc);
