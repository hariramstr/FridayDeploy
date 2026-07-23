using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers;

[Authorize]
public sealed class CorrelationController(ExplorerService explorerService) : Controller
{
    public async Task<IActionResult> Index(string id, CancellationToken cancellationToken)
    {
        var model = await explorerService.GetTimelineAsync("CorrelationId", id, cancellationToken);
        return model is null ? NotFound() : View("Timeline", model);
    }
}
