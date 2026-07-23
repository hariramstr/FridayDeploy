namespace FridayDeploy.Web.ViewModels;

public sealed record ApplicationCardViewModel(
    string Application,
    string? Environment,
    int MachineCount,
    int LogsToday,
    int LogCount,
    int ErrorCount,
    int WarningCount,
    int InformationCount,
    DateTime LastSeenUtc,
    DateTime? LastErrorUtc,
    string? Machine,
    double AverageLogsPerHour,
    string Status,
    DateTime? FirstSeenUtc,
    bool HasActiveApiKey,
    string HealthStatus);
