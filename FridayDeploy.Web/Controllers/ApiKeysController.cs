using FridayDeploy.Web.Services;
using FridayDeploy.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers;

[Authorize]
public sealed class ApiKeysController(ApiKeyService apiKeyService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new ApiKeysViewModel(await apiKeyService.GetAllAsync(cancellationToken), null));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string applicationName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            ModelState.AddModelError(string.Empty, "Application name is required.");
            return View(nameof(Index), new ApiKeysViewModel(await apiKeyService.GetAllAsync(cancellationToken), null));
        }

        var created = await apiKeyService.CreateAsync(applicationName, cancellationToken);
        return View(nameof(Index), new ApiKeysViewModel(await apiKeyService.GetAllAsync(cancellationToken), created));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rotate(int id, CancellationToken cancellationToken)
    {
        var created = await apiKeyService.RotateAsync(id, cancellationToken);
        return View(nameof(Index), new ApiKeysViewModel(await apiKeyService.GetAllAsync(cancellationToken), created));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(int id, CancellationToken cancellationToken)
    {
        await apiKeyService.SetDisabledAsync(id, true, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(int id, CancellationToken cancellationToken)
    {
        await apiKeyService.SetDisabledAsync(id, false, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await apiKeyService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
