using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers;

[Authorize]
public sealed class PropertiesController(ExplorerService explorerService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await explorerService.GetPropertiesAsync(cancellationToken));

    public async Task<IActionResult> Details(string name, CancellationToken cancellationToken)
    {
        var model = await explorerService.GetPropertyAsync(name, cancellationToken);
        return model is null ? NotFound() : View(model);
    }
}
