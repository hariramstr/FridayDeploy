using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers;

[Authorize]
public sealed class JobsController(ExplorerService explorerService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await explorerService.GetJobsAsync(cancellationToken));
}
