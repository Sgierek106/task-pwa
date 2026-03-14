using System.ComponentModel.DataAnnotations;

namespace TaskPwa.Server.Models;

public sealed class TaskItem
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserKey { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public bool IsCompleted { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public int Version { get; set; }
}
