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

        // FTS5 virtual table isn't part of the EF Core model (see AddExceptionFingerprints migration) and
        // so isn't created by EnsureCreated() — mirror the migration's raw SQL here for test databases.
        db.Database.ExecuteSqlRaw("CREATE VIRTUAL TABLE IF NOT EXISTS ExceptionSearch USING fts5(Fingerprint UNINDEXED, Content);");
    }

    public AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
