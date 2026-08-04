using FridayDeploy.Web.Services;
using FridayDeploy.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers;

[Authorize]
public sealed class CompareController(DashboardService dashboardService) : Controller
{
    private const int MaxSelected = 3;

    public async Task<IActionResult> Index(string[]? apps, CancellationToken cancellationToken)
    {
        var allCards = await dashboardService.GetApplicationsAsync(cancellationToken);
        var allNames = allCards.Select(x => x.Application).OrderBy(x => x).ToList();
        var selected = (apps is { Length: > 0 } ? apps : allNames.Take(2)).Take(MaxSelected).ToList();
        var cards = allCards.Where(x => selected.Contains(x.Application)).ToList();
        return View(new CompareViewModel(allNames, selected, cards));
    }
}
