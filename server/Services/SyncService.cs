using TaskPwa.Server.Data;
using TaskPwa.Server.Models;
using TaskPwa.Server.Models.Sync;
using Microsoft.EntityFrameworkCore;

namespace TaskPwa.Server.Services;

/// <summary>
/// Applies sync operations and computes changed task projections for a user partition.
/// </summary>
/// <param name="db">Application database context.</param>
public sealed class SyncService(AppDbContext db) : ISyncService
{
    private readonly AppDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

    /// <summary>
    /// Processes incoming sync operations and returns the resulting delta from the server.
    /// </summary>
    /// <param name="userKey">Tenant key used to scope task data.</param>
    /// <param name="request">Incoming sync payload from client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Sync response containing applied operation IDs, rejected operations, and changed tasks.</returns>
    public async Task<SyncResponse> ProcessAsync(Guid userKey, SyncRequest request, CancellationToken ct = default)
    {
        var appliedOpIds = new List<Guid>();
        var rejected = new List<RejectedOperation>();
        var serverNow = DateTimeOffset.UtcNow;

        foreach (var op in request.Operations)
        {
            if (op.Type == "upsert" && op.Task is not null)
            {
                var payload = op.Task;
                var existing = await FindTaskForUserAsync(op.EntityId, userKey, ct);

                if (existing is null)
                {
                    _db.Tasks.Add(new TaskItem
                    {
                        Id = op.EntityId,
                        UserKey = userKey,
                        Title = payload.Title,
                        Notes = payload.Notes,
                        IsCompleted = payload.IsCompleted,
                        DueAt = payload.DueAt,
                        UpdatedAt = serverNow,
                        DeletedAt = payload.DeletedAt,
                        Version = 1
                    });
                    appliedOpIds.Add(op.OpId);
                }
                else
                {
                    // last-write-wins: apply if incoming client time is newer
                    if (payload.UpdatedAt > existing.UpdatedAt)
                    {
                        existing.Title = payload.Title;
                        existing.Notes = payload.Notes;
                        existing.IsCompleted = payload.IsCompleted;
                        existing.DueAt = payload.DueAt;
                        existing.DeletedAt = payload.DeletedAt;
                        existing.UpdatedAt = serverNow;
                        existing.Version += 1;
                        appliedOpIds.Add(op.OpId);
                    }
                    else
                    {
                        // stale write - report conflict so client can reconcile
                        rejected.Add(new RejectedOperation(
                            op.OpId,
                            "conflict",
                            MapToDto(existing)
                        ));
                    }
                }
            }
            else if (op.Type == "delete")
            {
                var existing = await FindTaskForUserAsync(op.EntityId, userKey, ct);

                if (existing is null)
                {
                    appliedOpIds.Add(op.OpId);
                }
                else if (existing.DeletedAt is null)
                {
                    existing.DeletedAt = serverNow;
                    existing.UpdatedAt = serverNow;
                    existing.Version += 1;
                    appliedOpIds.Add(op.OpId);
                }
                else
                {
                    // already deleted - idempotent
                    appliedOpIds.Add(op.OpId);
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        // Keep user scoping in SQL, then do DateTimeOffset filtering/sorting in memory for SQLite compatibility.
        var userTasks = await _db.Tasks
            .Where(t => t.UserKey == userKey)
            .Select(t => MapToDto(t))
            .ToListAsync(ct);

        var changedTasks = userTasks
            .Where(t => !request.LastSyncAt.HasValue || t.UpdatedAt > request.LastSyncAt.Value)
            .OrderBy(t => t.UpdatedAt)
            .ToList();

        return new SyncResponse(serverNow, appliedOpIds, rejected, changedTasks);
    }

    private static TaskDto MapToDto(TaskItem t) => new(
        t.Id,
        t.Title,
        t.Notes,
        t.IsCompleted,
        t.DueAt,
        t.UpdatedAt,
        t.DeletedAt,
        t.Version
    );

    private async Task<TaskItem?> FindTaskForUserAsync(Guid taskId, Guid userKey, CancellationToken ct)
    {
        // Avoid tracking conflicts when multiple operations in one request reference the same task.
        var tracked = _db.Tasks.Local.FirstOrDefault(t => t.Id == taskId && t.UserKey == userKey);
        if (tracked is not null)
        {
            return tracked;
        }

        return await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserKey == userKey, ct);
    }
}
