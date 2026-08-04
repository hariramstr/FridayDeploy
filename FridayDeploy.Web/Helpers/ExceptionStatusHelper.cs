namespace FridayDeploy.Web.Helpers;

public static class ExceptionStatusHelper
{
    private static readonly TimeSpan ActiveThreshold = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan QuietThreshold = TimeSpan.FromHours(24);
    private static readonly TimeSpan NewThreshold = TimeSpan.FromHours(24);

    /// <summary>New = first seen recently and still firing. Active = still firing, seen it before.
    /// Quiet = cooling off. Resolved = hasn't recurred in a long time.</summary>
    public static string FromTimestamps(DateTime firstSeenUtc, DateTime lastSeenUtc, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var sinceLastSeen = now - lastSeenUtc;

        if (sinceLastSeen > QuietThreshold)
        {
            return "Resolved";
        }

        if (sinceLastSeen > ActiveThreshold)
        {
            return "Quiet";
        }

        return (now - firstSeenUtc) <= NewThreshold ? "New" : "Active";
    }

    public static string CssClass(string status) => status switch
    {
        "New" => "level-error",
        "Active" => "level-warning",
        "Quiet" => "level-info",
        _ => "level-success"
    };
}
