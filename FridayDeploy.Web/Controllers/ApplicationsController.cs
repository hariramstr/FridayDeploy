using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers;

[Authorize]
public sealed class ApplicationsController(DashboardService dashboardService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await dashboardService.GetApplicationsAsync(cancellationToken));
}
