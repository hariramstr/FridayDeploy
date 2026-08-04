using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers;

[Authorize]
public sealed class ExceptionsController(ExplorerService explorerService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await explorerService.GetExceptionGroupsAsync(cancellationToken));

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var details = await explorerService.GetExceptionGroupDetailsAsync(id, cancellationToken);
        return details is null ? NotFound() : View(details);
    }
}
