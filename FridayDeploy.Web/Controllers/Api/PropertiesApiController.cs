using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/properties")]
public sealed class PropertiesApiController(ExplorerService explorerService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => Ok(await explorerService.GetPropertiesAsync(cancellationToken));
}
