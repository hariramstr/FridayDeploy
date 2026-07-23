namespace FridayDeploy.Web.Models;

/// <summary>Single-row table (Id is always 1) holding the runtime-editable Settings page values.</summary>
public sealed class AppSetting
{
    public int Id { get; set; }
    public int RetentionDays { get; set; }
    public int RefreshIntervalSeconds { get; set; }
    public int PollingIntervalSeconds { get; set; }
    public int PageSize { get; set; }
    public required string Theme { get; set; }
    public string? BrandingName { get; set; }
}
