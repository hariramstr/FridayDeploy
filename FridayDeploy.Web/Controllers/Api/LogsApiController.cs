using FridayDeploy.Web.Authentication;
using FridayDeploy.Web.DTOs;
using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FridayDeploy.Web.Controllers.Api;

[ApiController]
[Route("api/logs")]
[Produces("application/json")]
public sealed class LogsApiController(LogService logService, LogSearchService logSearchService) : ControllerBase
{
    /// <summary>Ingests a single structured log event. Anonymous — intended for quick manual testing (curl,
    /// scripts). Applications sending real traffic should use <see cref="PostBatch"/> with an API key.</summary>
    /// <response code="201">The event was stored; the response body contains its id.</response>
    /// <response code="400">application, level, or message was missing.</response>
    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Ingestion)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(LogIngestRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Application) || string.IsNullOrWhiteSpace(request.Level) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("application, level and message are required.");
        }

        var id = await logService.IngestAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Ingests a batch of structured log events under a single API key. This is the endpoint the
    /// FridayDeploy.Serilog sink posts to. The application identity is taken from the API key, not the
    /// payload, so a key can never attribute logs to a different application.</summary>
    /// <response code="200">Returns the number of events ingested.</response>
    /// <response code="400">The batch was empty, or level/message was missing on an event.</response>
    /// <response code="401">The X-Api-Key header was missing, invalid, or disabled.</response>
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.Scheme)]
    [HttpPost("batch")]
    [EnableRateLimiting(RateLimitPolicies.Ingestion)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PostBatch(List<LogIngestRequest> requests, CancellationToken cancellationToken)
    {
        if (requests is not { Count: > 0 })
        {
            return BadRequest("At least one log event is required.");
        }

        foreach (var request in requests)
        {
            if (string.IsNullOrWhiteSpace(request.Level) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("level and message are required for every event.");
            }

            // The API key already identifies the application; the payload's Application field is ignored
            // so one key cannot attribute logs to a different application.
            request.Application = User.FindFirst("ApplicationName")!.Value;
        }

        var count = await logService.IngestBatchAsync(requests, cancellationToken);
        return Ok(new { ingested = count });
    }

    /// <summary>Searches logs with the same filters and query language as the Logs UI. Requires a signed-in session.</summary>
    /// <response code="200">A page of matching logs.</response>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedResponse<LogResponse>> Get([FromQuery] LogSearchRequest request, CancellationToken cancellationToken = default) => logSearchService.SearchAsync(request, cancellationToken);

    /// <summary>Fetches a single log event with its full structured properties.</summary>
    /// <response code="200">The log event.</response>
    /// <response code="404">No log with that id exists.</response>
    [Authorize]
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var log = await logService.GetByIdAsync(id, cancellationToken);
        return log is null ? NotFound() : Ok(log);
    }
}
