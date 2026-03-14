namespace TaskPwa.Server.Models.Sync;

public sealed record SyncRequest(
    Guid ClientId,
    DateTimeOffset? LastSyncAt,
    IReadOnlyList<SyncOperation> Operations
);

public sealed record SyncOperation(
    Guid OpId,
    string Type,
    Guid EntityId,
    DateTimeOffset CreatedAt,
    TaskUpsertPayload? Task
);

public sealed record TaskUpsertPayload(
    Guid Id,
    string Title,
    string? Notes,
    bool IsCompleted,
    DateTimeOffset? DueAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt,
    int? BaseVersion
);

public sealed record SyncResponse(
    DateTimeOffset ServerTime,
    IReadOnlyList<Guid> AppliedOpIds,
    IReadOnlyList<RejectedOperation> Rejected,
    IReadOnlyList<TaskDto> ChangedTasks
);

public sealed record RejectedOperation(
    Guid OpId,
    string Reason,
    TaskDto? ServerTask
);
