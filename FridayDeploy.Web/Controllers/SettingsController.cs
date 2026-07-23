using FridayDeploy.Web.Models;
using FridayDeploy.Web.Services;
using FridayDeploy.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers;

[Authorize]
public sealed class SettingsController(SettingsService settingsService, IConfiguration configuration) : Controller
{
    public async Task<IActionResult> Index(bool saved = false, CancellationToken cancellationToken = default)
    {
        var setting = await settingsService.GetAsync(cancellationToken);
        return View(ToViewModel(setting, saved));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SettingsViewModel model, CancellationToken cancellationToken)
    {
        var setting = new AppSetting
        {
            RetentionDays = Math.Clamp(model.RetentionDays, 1, 365),
            RefreshIntervalSeconds = Math.Clamp(model.RefreshIntervalSeconds, 5, 3600),
            PollingIntervalSeconds = Math.Clamp(model.PollingIntervalSeconds, 5, 3600),
            PageSize = Math.Clamp(model.PageSize, 10, 200),
            Theme = model.Theme is "Light" or "System" ? model.Theme : "Dark",
            BrandingName = string.IsNullOrWhiteSpace(model.BrandingName) ? null : model.BrandingName.Trim()
        };
        await settingsService.SaveAsync(setting, cancellationToken);
        return RedirectToAction(nameof(Index), new { saved = true });
    }

    private SettingsViewModel ToViewModel(AppSetting setting, bool saved) => new(
        setting.RetentionDays,
        setting.RefreshIntervalSeconds,
        setting.PollingIntervalSeconds,
        setting.PageSize,
        setting.Theme,
        setting.BrandingName,
        configuration["AdminUser:Username"] ?? "admin",
        saved);
}
