using System.Reflection;
using FridayDeploy.Web.Services;
using FridayDeploy.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace FridayDeploy.Web.Controllers;

[Authorize]
public sealed class AboutController(SettingsService settingsService, IConfiguration configuration) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var setting = await settingsService.GetAsync(cancellationToken);
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=App_Data/fridaydeploy.db";
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var databasePath = Path.GetFullPath(dataSource);
        var databaseSize = System.IO.File.Exists(databasePath) ? new FileInfo(databasePath).Length : 0;

        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";

        return View(new AboutViewModel(
            assembly.GetName().Version?.ToString() ?? "1.0.0.0",
            informationalVersion,
            databasePath,
            databaseSize,
            setting.RetentionDays,
            "MIT",
            "https://github.com/hariramstr/FridayDeploy"));
    }
}
