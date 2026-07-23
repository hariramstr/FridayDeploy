using FridayDeploy.Web.Data;
using FridayDeploy.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

public sealed class NoteService(AppDbContext db)
{
    public Task<List<LogNote>> GetForLogAsync(long logId, CancellationToken cancellationToken) =>
        db.LogNotes.AsNoTracking().Where(x => x.LogId == logId).OrderBy(x => x.CreatedUtc).ToListAsync(cancellationToken);

    public async Task AddAsync(long logId, string author, string body, CancellationToken cancellationToken)
    {
        db.LogNotes.Add(new LogNote { LogId = logId, Author = author, Body = body.Trim(), CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var note = await db.LogNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (note is null)
        {
            return;
        }

        db.LogNotes.Remove(note);
        await db.SaveChangesAsync(cancellationToken);
    }
}
