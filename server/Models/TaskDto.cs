namespace TaskPwa.Server.Models;

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
