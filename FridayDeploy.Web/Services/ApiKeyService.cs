using System.Security.Cryptography;
using System.Text;
using FridayDeploy.Web.Data;
using FridayDeploy.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Services;

public sealed record ApiKeyCreatedResult(int Id, string ApplicationName, string RawKey);
public sealed record ApiKeySummary(int Id, string ApplicationName, string Prefix, DateTime CreatedUtc, DateTime? LastUsedUtc, bool IsDisabled);

public sealed class ApiKeyService(AppDbContext db)
{
    private const string Prefix = "fd_";

    public async Task<ApiKeyCreatedResult> CreateAsync(string applicationName, CancellationToken cancellationToken)
    {
        applicationName = applicationName.Trim();
        var application = await db.Applications.FirstOrDefaultAsync(x => x.Name == applicationName, cancellationToken);
        if (application is null)
        {
            application = new Application { Name = applicationName, FirstSeenUtc = DateTime.UtcNow, LastSeenUtc = DateTime.UtcNow };
            db.Applications.Add(application);
        }

        var rawKey = GenerateRawKey();
        var apiKey = new ApiKey
        {
            Application = application,
            KeyHash = Hash(rawKey),
            Prefix = rawKey[..12],
            CreatedUtc = DateTime.UtcNow
        };
        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync(cancellationToken);

        return new ApiKeyCreatedResult(apiKey.Id, application.Name, rawKey);
    }

    public async Task<IReadOnlyList<ApiKeySummary>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.ApiKeys.AsNoTracking()
            .Include(x => x.Application)
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => new ApiKeySummary(x.Id, x.Application.Name, x.Prefix, x.CreatedUtc, x.LastUsedUtc, x.IsDisabled))
            .ToListAsync(cancellationToken);

    public async Task SetDisabledAsync(int id, bool isDisabled, CancellationToken cancellationToken)
    {
        var apiKey = await db.ApiKeys.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (apiKey is null)
        {
            return;
        }

        apiKey.IsDisabled = isDisabled;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var apiKey = await db.ApiKeys.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (apiKey is null)
        {
            return;
        }

        db.ApiKeys.Remove(apiKey);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ApiKeyCreatedResult?> RotateAsync(int id, CancellationToken cancellationToken)
    {
        var apiKey = await db.ApiKeys.Include(x => x.Application).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (apiKey is null)
        {
            return null;
        }

        var rawKey = GenerateRawKey();
        apiKey.KeyHash = Hash(rawKey);
        apiKey.Prefix = rawKey[..12];
        apiKey.IsDisabled = false;
        await db.SaveChangesAsync(cancellationToken);

        return new ApiKeyCreatedResult(apiKey.Id, apiKey.Application.Name, rawKey);
    }

    /// <summary>Validates a raw API key and, if valid, bumps its LastUsedUtc and the owning Application's
    /// LastSeenUtc (auto-registering the application's activity without a separate write).</summary>
    public async Task<Application?> ValidateAndTouchAsync(string rawKey, CancellationToken cancellationToken)
    {
        var hash = Hash(rawKey);
        var apiKey = await db.ApiKeys.Include(x => x.Application).FirstOrDefaultAsync(x => x.KeyHash == hash && !x.IsDisabled, cancellationToken);
        if (apiKey is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        apiKey.LastUsedUtc = now;
        apiKey.Application.LastSeenUtc = now;
        await db.SaveChangesAsync(cancellationToken);

        return apiKey.Application;
    }

    private static string GenerateRawKey() => Prefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

    private static string Hash(string rawKey) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey))).ToLowerInvariant();
}
