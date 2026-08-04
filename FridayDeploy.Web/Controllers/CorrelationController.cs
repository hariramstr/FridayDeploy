using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers;

[Authorize]
public sealed class CorrelationController(ExplorerService explorerService, SettingsService settingsService) : Controller
{
    public async Task<IActionResult> Index(string id, bool live, CancellationToken cancellationToken)
    {
        var model = await explorerService.GetTimelineAsync("CorrelationId", id, cancellationToken);
        if (model is null) return NotFound();
        var pollMs = (await settingsService.GetAsync(cancellationToken)).PollingIntervalSeconds * 1000;
        return View("Timeline", model with { Live = live, PollIntervalMs = pollMs });
    }
}
