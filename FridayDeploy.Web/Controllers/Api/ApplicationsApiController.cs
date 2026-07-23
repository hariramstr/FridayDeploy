using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/applications")]
public sealed class ApplicationsApiController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => Ok(await dashboardService.GetApplicationsAsync(cancellationToken));
}
