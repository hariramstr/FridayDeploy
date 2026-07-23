using System.Security.Claims;
using System.Text.Json;
using FridayDeploy.Web.DTOs;
using FridayDeploy.Web.Helpers;
using FridayDeploy.Web.Services;
using FridayDeploy.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FridayDeploy.Web.Controllers;

[Authorize]
public sealed class LogsController(
    LogService logService,
    LogSearchService logSearchService,
    DashboardService dashboardService,
    ExplorerService explorerService,
    ExportService exportService,
    SavedSearchService savedSearchService,
    BookmarkService bookmarkService,
    NoteService noteService,
    SettingsService settingsService) : Controller
{
    public async Task<IActionResult> Index(LogSearchRequest request, bool live = false, CancellationToken cancellationToken = default)
    {
        var pageSize = (await settingsService.GetAsync(cancellationToken)).PageSize;
        request.PageSize = pageSize;
        var result = await logSearchService.SearchAsync(request, cancellationToken);
        var filters = await dashboardService.GetFilterOptionsAsync(cancellationToken);
        var propertyNames = await explorerService.GetPropertyNamesAsync(cancellationToken);
        var savedSearches = await savedSearchService.GetAllAsync(CurrentUserId, cancellationToken);
        var generated = BuildExpression(request);
        var vm = new LogsPageViewModel(
            result.Items.Select(x => new LogCardViewModel(x.Id, x.TimestampUtc, x.Level, x.Application, x.Environment, x.Machine, x.Source, x.Message, x.CorrelationId, x.RequestId, x.Duration)).ToList(),
            request.Application,
            request.Query,
            result.Page,
            Math.Max(1, (int)Math.Ceiling(result.TotalCount / (double)pageSize)),
            filters.Applications,
            filters.Environments,
            filters.Levels,
            filters.Machines,
            propertyNames,
            generated,
            live,
            savedSearches.Select(x => new SavedSearchViewModel(x.Id, x.Name, x.QueryString)).ToList());
        return View(vm);
    }

    public async Task<IActionResult> Export(LogSearchRequest request, ExportFormat format, CancellationToken cancellationToken)
    {
        request.PageSize = int.MaxValue;
        using var memory = new MemoryStream();
        await exportService.WriteAsync(memory, request, format, cancellationToken);
        memory.Position = 0;
        var (contentType, extension) = format switch
        {
            ExportFormat.Csv => ("text/csv", "csv"),
            ExportFormat.Json => ("application/json", "json"),
            _ => ("text/plain", "txt")
        };
        return File(memory.ToArray(), contentType, $"fridaydeploy-logs-{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSearch(string name, string queryString, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            await savedSearchService.SaveAsync(CurrentUserId, name, queryString, cancellationToken);
        }

        return Redirect(string.IsNullOrWhiteSpace(queryString) ? Url.Action(nameof(Index))! : $"{Url.Action(nameof(Index))}{queryString}");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSavedSearch(int id, CancellationToken cancellationToken)
    {
        await savedSearchService.DeleteAsync(CurrentUserId, id, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bookmark(long id, string? note, CancellationToken cancellationToken)
    {
        await bookmarkService.AddLogBookmarkAsync(CurrentUserId, id, note, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(long id, string body, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            await noteService.AddAsync(id, User.Identity?.Name ?? "unknown", body, cancellationToken);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var log = await logService.GetByIdAsync(id, cancellationToken);
        if (log is null)
        {
            return NotFound();
        }

        var raw = JsonSerializer.Serialize(log, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        var notes = await noteService.GetForLogAsync(id, cancellationToken);
        return View(new LogDetailsViewModel(
            log.Id,
            log.TimestampUtc,
            log.Application,
            log.Level,
            log.Message,
            log.Exception,
            log.StackTrace,
            log.RequestId,
            log.CorrelationId,
            log.Machine,
            log.Environment,
            log.Version,
            log.Source,
            log.IPAddress,
            log.ThreadId,
            log.Duration,
            raw,
            log.Properties.Select(x => new LogPropertyViewModel(x.PropertyName, x.PropertyValue, x.PropertyType)).ToList(),
            notes.Select(x => new LogNoteViewModel(x.Id, x.Author, x.Body, x.CreatedUtc)).ToList()));
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static string BuildExpression(LogSearchRequest request)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Application)) parts.Add($"Application={request.Application}");
        if (!string.IsNullOrWhiteSpace(request.Level)) parts.Add($"Level={request.Level}");
        if (!string.IsNullOrWhiteSpace(request.Environment)) parts.Add($"Environment={request.Environment}");
        if (!string.IsNullOrWhiteSpace(request.Machine)) parts.Add($"Machine={request.Machine}");
        if (!string.IsNullOrWhiteSpace(request.CorrelationId)) parts.Add($"CorrelationId={request.CorrelationId}");
        if (!string.IsNullOrWhiteSpace(request.RequestId)) parts.Add($"RequestId={request.RequestId}");
        if (!string.IsNullOrWhiteSpace(request.PropertyName) && !string.IsNullOrWhiteSpace(request.PropertyValue)) parts.Add($"{request.PropertyName}={request.PropertyValue}");
        if (!string.IsNullOrWhiteSpace(request.ContainsText)) parts.Add($"Message~{request.ContainsText}");
        return string.IsNullOrWhiteSpace(request.Query) ? string.Join(" AND ", parts) : request.Query;
    }
}
