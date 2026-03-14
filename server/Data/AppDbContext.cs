using Microsoft.EntityFrameworkCore;
using TaskPwa.Server.Models;

namespace TaskPwa.Server.Data;

/// <summary>
/// Entity Framework database context for Task PWA server data.
/// </summary>
/// <param name="options">Database context options.</param>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options ?? throw new ArgumentNullException(nameof(options)))
{
    /// <summary>
    /// Tasks persisted for all users.
    /// </summary>
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.HasIndex(t => new { t.UserKey, t.UpdatedAt });
        });
    }
}
