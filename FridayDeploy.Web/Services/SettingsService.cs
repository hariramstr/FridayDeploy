using FridayDeploy.Web.Data;
using FridayDeploy.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

public sealed class SettingsService(AppDbContext db, IConfiguration configuration)
{
    private const int SingletonId = 1;

    public async Task<AppSetting> GetAsync(CancellationToken cancellationToken)
    {
        var setting = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == SingletonId, cancellationToken);
        if (setting is not null)
        {
            return setting;
        }

        // First run: seed from appsettings.json so existing deployments keep their configured defaults.
        setting = new AppSetting
        {
            Id = SingletonId,
            RetentionDays = configuration.GetValue("FridayDeploy:RetentionDays", 7),
            RefreshIntervalSeconds = configuration.GetValue("FridayDeploy:RefreshIntervalSeconds", 30),
            PollingIntervalSeconds = configuration.GetValue("FridayDeploy:PollingIntervalSeconds", 10),
            PageSize = configuration.GetValue("FridayDeploy:PageSize", 25),
            Theme = configuration["FridayDeploy:Theme"] is "Light" or "System" ? configuration["FridayDeploy:Theme"]! : "Dark",
            BrandingName = configuration["FridayDeploy:BrandingName"]
        };
        db.AppSettings.Add(setting);
        await db.SaveChangesAsync(cancellationToken);
        return setting;
    }

    public async Task SaveAsync(AppSetting updated, CancellationToken cancellationToken)
    {
        var setting = await db.AppSettings.FirstOrDefaultAsync(x => x.Id == SingletonId, cancellationToken);
        if (setting is null)
        {
            updated.Id = SingletonId;
            db.AppSettings.Add(updated);
        }
        else
        {
            setting.RetentionDays = updated.RetentionDays;
            setting.RefreshIntervalSeconds = updated.RefreshIntervalSeconds;
            setting.PollingIntervalSeconds = updated.PollingIntervalSeconds;
            setting.PageSize = updated.PageSize;
            setting.Theme = updated.Theme;
            setting.BrandingName = updated.BrandingName;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Deletes all logs (and their properties, via cascade), bookmarks, notes, and saved searches.
    /// Users, Applications, and ApiKeys are left untouched so existing integrations keep working.</summary>
    public async Task TruncateLogDataAsync(CancellationToken cancellationToken)
    {
        await db.Logs.ExecuteDeleteAsync(cancellationToken);
        await db.Bookmarks.ExecuteDeleteAsync(cancellationToken);
        await db.LogNotes.ExecuteDeleteAsync(cancellationToken);
        await db.SavedSearches.ExecuteDeleteAsync(cancellationToken);
    }
}
