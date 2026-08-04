namespace FridayDeploy.Web.Models;

/// <summary>One hour-bucket of log volume for one application, rolled up by AppHourlyRollupService.
/// Backs uptime strips, sparklines, anomaly detection, and volume forecasting without re-scanning
/// the full Logs table for every dashboard load.</summary>
public sealed class AppHourlyStat
{
    public int Id { get; set; }
    public required string Application { get; set; }
    public DateTime HourBucketUtc { get; set; }
    public int LogCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
}
