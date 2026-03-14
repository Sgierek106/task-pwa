using System.ComponentModel.DataAnnotations;

namespace TaskPwa.Server.Models;

/// <summary>
/// Persistence model for a single task row.
/// </summary>
public sealed class TaskItem
{
    [Key]
    /// <summary>
    /// Task identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User partition key that owns this task.
    /// </summary>
    public Guid UserKey { get; set; }

    [MaxLength(200)]
    /// <summary>
    /// Task title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional task notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Indicates whether the task is completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Optional due date.
    /// </summary>
    public DateTimeOffset? DueAt { get; set; }

    /// <summary>
    /// Last time this task was updated on the server.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Tombstone timestamp when soft-deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Monotonic task version for conflict signaling.
    /// </summary>
    public int Version { get; set; }
}
