using System.Security.Claims;
using FridayDeploy.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers;

[Authorize]
public sealed class BookmarksController(BookmarkService bookmarkService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await bookmarkService.GetAllAsync(CurrentUserId, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCorrelation(string correlationId, string? note, CancellationToken cancellationToken)
    {
        await bookmarkService.AddCorrelationBookmarkAsync(CurrentUserId, correlationId, note, cancellationToken);
        return RedirectToAction("Index", "Correlation", new { id = correlationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRequest(string requestId, string? note, CancellationToken cancellationToken)
    {
        await bookmarkService.AddRequestBookmarkAsync(CurrentUserId, requestId, note, cancellationToken);
        return RedirectToAction("Index", "Requests", new { id = requestId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await bookmarkService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
