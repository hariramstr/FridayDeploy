namespace FridayDeploy.Web.Helpers;

public static class ApplicationHealthHelper
{
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan WarningThreshold = TimeSpan.FromMinutes(10);

    /// <summary>Online/Warning/Offline based on how long ago the application last sent a log.</summary>
    public static string FromLastSeen(DateTime lastSeenUtc, DateTime? nowUtc = null)
    {
        var age = (nowUtc ?? DateTime.UtcNow) - lastSeenUtc;
        return age switch
        {
            _ when age <= OnlineThreshold => "Online",
            _ when age <= WarningThreshold => "Warning",
            _ => "Offline"
        };
    }

    public static string CssClass(string health) => health switch
    {
        "Online" => "level-success",
        "Warning" => "level-warning",
        _ => "level-muted"
    };
}
