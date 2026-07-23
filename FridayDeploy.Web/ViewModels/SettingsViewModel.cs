namespace FridayDeploy.Web.ViewModels;

public sealed record SettingsViewModel(
    int RetentionDays,
    int RefreshIntervalSeconds,
    int PollingIntervalSeconds,
    int PageSize,
    string Theme,
    string? BrandingName,
    string AdminUsername,
    bool Saved,
    bool Truncated);
