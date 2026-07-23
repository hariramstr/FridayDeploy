using FridayDeploy.Web.Data;
using FridayDeploy.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

public sealed class BookmarkService(AppDbContext db)
{
    public Task<List<Bookmark>> GetAllAsync(int userId, CancellationToken cancellationToken) =>
        db.Bookmarks.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);

    public async Task AddLogBookmarkAsync(int userId, long logId, string? note, CancellationToken cancellationToken)
    {
        db.Bookmarks.Add(new Bookmark { UserId = userId, LogId = logId, Note = note, CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddCorrelationBookmarkAsync(int userId, string correlationId, string? note, CancellationToken cancellationToken)
    {
        db.Bookmarks.Add(new Bookmark { UserId = userId, CorrelationId = correlationId, Note = note, CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRequestBookmarkAsync(int userId, string requestId, string? note, CancellationToken cancellationToken)
    {
        db.Bookmarks.Add(new Bookmark { UserId = userId, RequestId = requestId, Note = note, CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int userId, int id, CancellationToken cancellationToken)
    {
        var bookmark = await db.Bookmarks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        if (bookmark is null)
        {
            return;
        }

        db.Bookmarks.Remove(bookmark);
        await db.SaveChangesAsync(cancellationToken);
    }
}
