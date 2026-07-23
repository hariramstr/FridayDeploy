using FridayDeploy.Tests.TestSupport;
using FridayDeploy.Web.Services;

namespace FridayDeploy.Tests.Services;

public sealed class ApiKeyServiceTests : IDisposable
{
    private readonly SqliteDbContextFactory _factory = new();

    [Fact]
    public async Task CreateAsync_creates_application_and_returns_usable_key()
    {
        await using var db = _factory.CreateContext();
        var service = new ApiKeyService(db);

        var created = await service.CreateAsync("MyApp", CancellationToken.None);

        Assert.Equal("MyApp", created.ApplicationName);
        Assert.StartsWith("fd_", created.RawKey);
        Assert.NotNull(await service.ValidateAndTouchAsync(created.RawKey, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_reuses_existing_application_with_same_name()
    {
        await using var db = _factory.CreateContext();
        var service = new ApiKeyService(db);

        await service.CreateAsync("Shared", CancellationToken.None);
        await service.CreateAsync("Shared", CancellationToken.None);

        var keys = await service.GetAllAsync(CancellationToken.None);
        Assert.Equal(2, keys.Count);
        Assert.All(keys, k => Assert.Equal("Shared", k.ApplicationName));
    }

    [Fact]
    public async Task ValidateAndTouchAsync_rejects_unknown_key()
    {
        await using var db = _factory.CreateContext();
        var service = new ApiKeyService(db);

        Assert.Null(await service.ValidateAndTouchAsync("fd_does_not_exist", CancellationToken.None));
    }

    [Fact]
    public async Task SetDisabledAsync_true_blocks_subsequent_validation()
    {
        await using var db = _factory.CreateContext();
        var service = new ApiKeyService(db);
        var created = await service.CreateAsync("App", CancellationToken.None);

        await service.SetDisabledAsync(created.Id, true, CancellationToken.None);

        Assert.Null(await service.ValidateAndTouchAsync(created.RawKey, CancellationToken.None));
    }

    [Fact]
    public async Task RotateAsync_invalidates_old_key_and_issues_a_new_one()
    {
        await using var db = _factory.CreateContext();
        var service = new ApiKeyService(db);
        var created = await service.CreateAsync("App", CancellationToken.None);

        var rotated = await service.RotateAsync(created.Id, CancellationToken.None);

        Assert.NotNull(rotated);
        Assert.NotEqual(created.RawKey, rotated!.RawKey);
        Assert.Null(await service.ValidateAndTouchAsync(created.RawKey, CancellationToken.None));
        Assert.NotNull(await service.ValidateAndTouchAsync(rotated.RawKey, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_removes_the_key()
    {
        await using var db = _factory.CreateContext();
        var service = new ApiKeyService(db);
        var created = await service.CreateAsync("App", CancellationToken.None);

        await service.DeleteAsync(created.Id, CancellationToken.None);

        Assert.Empty(await service.GetAllAsync(CancellationToken.None));
    }

    public void Dispose() => _factory.Dispose();
}
