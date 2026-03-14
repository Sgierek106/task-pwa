using Microsoft.EntityFrameworkCore;
using TaskPwa.Server.Models;

namespace TaskPwa.Server.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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
