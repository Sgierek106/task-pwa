using TaskPwa.Server.Data;
using TaskPwa.Server.Models;
using TaskPwa.Server.Models.Sync;
using Microsoft.EntityFrameworkCore;

namespace TaskPwa.Server.Services;

public sealed class SyncService
{
    private readonly AppDbContext _db;

    public SyncService(AppDbContext db)
    {
        _db = db;
    }

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
                var existing = await _db.Tasks
                    .FirstOrDefaultAsync(t => t.Id == op.EntityId && t.UserKey == userKey, ct);

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
                var existing = await _db.Tasks
                    .FirstOrDefaultAsync(t => t.Id == op.EntityId && t.UserKey == userKey, ct);

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

        // Return all tasks changed since lastSyncAt for this user
        IQueryable<TaskItem> changedQuery = _db.Tasks.Where(t => t.UserKey == userKey);
        if (request.LastSyncAt.HasValue)
        {
            changedQuery = changedQuery.Where(t => t.UpdatedAt > request.LastSyncAt.Value);
        }

        var changedTasks = await changedQuery
            .OrderBy(t => t.UpdatedAt)
            .Select(t => MapToDto(t))
            .ToListAsync(ct);

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
}
