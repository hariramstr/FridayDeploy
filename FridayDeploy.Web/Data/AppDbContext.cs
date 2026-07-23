using FridayDeploy.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FridayDeploy.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Log> Logs => Set<Log>();
    public DbSet<LogProperty> LogProperties => Set<LogProperty>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<LogNote> LogNotes => Set<LogNote>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.Username).HasMaxLength(128);
            entity.Property(x => x.PasswordHash).HasMaxLength(256);
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.HasIndex(x => x.TimestampUtc);
            entity.HasIndex(x => x.Application);
            entity.HasIndex(x => x.Level);
            entity.Property(x => x.Application).HasMaxLength(200);
            entity.Property(x => x.Level).HasMaxLength(32);
            entity.Property(x => x.Message).HasMaxLength(4096);
            entity.HasMany(x => x.Properties).WithOne(x => x.Log).HasForeignKey(x => x.LogId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LogProperty>(entity =>
        {
            entity.HasIndex(x => x.LogId);
            entity.HasIndex(x => new { x.PropertyName, x.PropertyValue });
            entity.Property(x => x.PropertyName).HasMaxLength(256);
            entity.Property(x => x.PropertyType).HasMaxLength(64);
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(x => x.KeyHash).IsUnique();
            entity.Property(x => x.KeyHash).HasMaxLength(64);
            entity.Property(x => x.Prefix).HasMaxLength(32);
            entity.HasOne(x => x.Application).WithMany(x => x.ApiKeys).HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SavedSearch>(entity =>
        {
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.LogId);
            entity.HasIndex(x => x.CorrelationId);
        });

        modelBuilder.Entity<LogNote>(entity =>
        {
            entity.HasIndex(x => x.LogId);
            entity.Property(x => x.Author).HasMaxLength(128);
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.Property(x => x.Theme).HasMaxLength(16);
            entity.Property(x => x.BrandingName).HasMaxLength(128);
        });
    }
}
