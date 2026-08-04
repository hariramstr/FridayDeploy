namespace FridayDeploy.Web.ViewModels;

/// <summary>F-45: side-by-side application comparison — reuses the same per-app metrics already
/// computed for the Applications page, just filtered to the chosen 2-3 apps.</summary>
public sealed record CompareViewModel(
    IReadOnlyList<string> AllApplications,
    IReadOnlyList<string> SelectedApplications,
    IReadOnlyList<ApplicationCardViewModel> Cards);
