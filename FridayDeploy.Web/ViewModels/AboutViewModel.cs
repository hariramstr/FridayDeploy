namespace FridayDeploy.Web.ViewModels;

public sealed record AboutViewModel(
    string Version,
    string Build,
    string DatabasePath,
    long DatabaseSizeBytes,
    int RetentionDays,
    string License,
    string GitHubUrl);
