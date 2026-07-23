namespace FridayDeploy.Web.Helpers;

public static class LogLevelHelper
{
    public static string CssClass(string level) => level.ToLowerInvariant() switch
    {
        "error" => "level-error",
        "fatal" or "critical" => "level-fatal",
        "warning" or "warn" => "level-warning",
        "information" or "info" => "level-info",
        "debug" or "trace" => "level-muted",
        _ => "level-success"
    };

    public static bool IsError(string level) => string.Equals(level, "Error", StringComparison.OrdinalIgnoreCase) || string.Equals(level, "Fatal", StringComparison.OrdinalIgnoreCase) || string.Equals(level, "Critical", StringComparison.OrdinalIgnoreCase);

    public static bool IsWarning(string level) => string.Equals(level, "Warning", StringComparison.OrdinalIgnoreCase) || string.Equals(level, "Warn", StringComparison.OrdinalIgnoreCase);
}
