using FridayDeploy.Web.Services;

namespace FridayDeploy.Web.ViewModels;

public sealed record ApiKeysViewModel(IReadOnlyList<ApiKeySummary> Keys, ApiKeyCreatedResult? JustCreated);
