using FridayDeploy.Web.Data;
using FridayDeploy.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

public sealed class SavedSearchService(AppDbContext db)
{
    public Task<List<SavedSearch>> GetAllAsync(int userId, CancellationToken cancellationToken) =>
        db.SavedSearches.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);

    public async Task SaveAsync(int userId, string name, string queryString, CancellationToken cancellationToken)
    {
        db.SavedSearches.Add(new SavedSearch
        {
            UserId = userId,
            Name = name.Trim(),
            QueryString = queryString,
            CreatedUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int userId, int id, CancellationToken cancellationToken)
    {
        var search = await db.SavedSearches.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        if (search is null)
        {
            return;
        }

        db.SavedSearches.Remove(search);
        await db.SaveChangesAsync(cancellationToken);
    }
}
