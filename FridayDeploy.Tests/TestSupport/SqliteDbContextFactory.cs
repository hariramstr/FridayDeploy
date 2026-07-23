using FridayDeploy.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Tests.TestSupport;

/// <summary>Creates an isolated in-memory SQLite database per test, kept alive for the test's lifetime via
/// an open connection (SQLite ":memory:" databases are destroyed when the last connection closes).</summary>
public sealed class SqliteDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDbContextFactory()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    public AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
