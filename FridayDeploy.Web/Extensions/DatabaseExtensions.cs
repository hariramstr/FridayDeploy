using FridayDeploy.Web.Data;
using FridayDeploy.Web.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

        if (!await db.Users.AnyAsync())
        {
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var username = config["AdminUser:Username"];
            var password = config["AdminUser:Password"];

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("AdminUser:Username and AdminUser:Password must be configured.");
            }

            db.Users.Add(new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsAdministrator = true,
                CreatedUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }
    }

    public static void EnsureSqliteDirectoryExists(this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=App_Data/fridaydeploy.db";
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var directory = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
