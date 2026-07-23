using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardApiController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => Ok(await dashboardService.GetDashboardAsync(cancellationToken));
}
