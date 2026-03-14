namespace TaskPwa.Server.Models;

/// <summary>
/// Task payload returned to clients.
/// </summary>
/// <param name="Id">Task identifier.</param>
/// <param name="Title">Task title.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="IsCompleted">Completion flag.</param>
/// <param name="DueAt">Optional due date.</param>
/// <param name="UpdatedAt">Last update timestamp.</param>
/// <param name="DeletedAt">Optional soft-delete timestamp.</param>
/// <param name="Version">Task version used for synchronization.</param>
public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Notes,
    bool IsCompleted,
    DateTimeOffset? DueAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt,
    int Version
);
