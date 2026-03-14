namespace TaskPwa.Server.Models.Sync;

/// <summary>
/// Client sync request payload.
/// </summary>
/// <param name="ClientId">Client device identifier.</param>
/// <param name="LastSyncAt">Optional last sync watermark.</param>
/// <param name="Operations">Operations pending from client outbox.</param>
public sealed record SyncRequest(
    Guid ClientId,
    DateTimeOffset? LastSyncAt,
    IReadOnlyList<SyncOperation> Operations
);

/// <summary>
/// One synchronization operation emitted by the client outbox.
/// </summary>
/// <param name="OpId">Operation identifier for idempotency.</param>
/// <param name="Type">Operation type (upsert/delete).</param>
/// <param name="EntityId">Task identifier targeted by the operation.</param>
/// <param name="CreatedAt">Client-side operation creation timestamp.</param>
/// <param name="Task">Task payload for upsert operations.</param>
public sealed record SyncOperation(
    Guid OpId,
    string Type,
    Guid EntityId,
    DateTimeOffset CreatedAt,
    TaskUpsertPayload? Task
);

/// <summary>
/// Payload for an upsert operation.
/// </summary>
/// <param name="Id">Task identifier.</param>
/// <param name="Title">Task title.</param>
/// <param name="Notes">Optional notes.</param>
/// <param name="IsCompleted">Completion flag.</param>
/// <param name="DueAt">Optional due date.</param>
/// <param name="UpdatedAt">Client-side updated timestamp.</param>
/// <param name="DeletedAt">Optional soft-delete timestamp.</param>
/// <param name="BaseVersion">Optional client-known version.</param>
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

/// <summary>
/// Synchronization response payload sent to the client.
/// </summary>
/// <param name="ServerTime">Server time to persist as next watermark.</param>
/// <param name="AppliedOpIds">Operation IDs successfully applied.</param>
/// <param name="Rejected">Rejected operations and conflict context.</param>
/// <param name="ChangedTasks">Tasks changed since the requested watermark.</param>
public sealed record SyncResponse(
    DateTimeOffset ServerTime,
    IReadOnlyList<Guid> AppliedOpIds,
    IReadOnlyList<RejectedOperation> Rejected,
    IReadOnlyList<TaskDto> ChangedTasks
);

/// <summary>
/// Represents an operation rejected during synchronization.
/// </summary>
/// <param name="OpId">Rejected operation identifier.</param>
/// <param name="Reason">Reason code for rejection.</param>
/// <param name="ServerTask">Current server-side task snapshot when relevant.</param>
public sealed record RejectedOperation(
    Guid OpId,
    string Reason,
    TaskDto? ServerTask
);
