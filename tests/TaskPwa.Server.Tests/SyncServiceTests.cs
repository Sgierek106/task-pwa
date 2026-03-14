using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskPwa.Server.Data;
using TaskPwa.Server.Models;
using TaskPwa.Server.Models.Sync;
using TaskPwa.Server.Services;
using Xunit;

namespace TaskPwa.Server.Tests;

public sealed class SyncServiceTests
{
    [Fact]
    public async Task ProcessAsync_WhenMultipleOpsTargetSameTask_DoesNotThrowAndAppliesLatest()
    {
        using var db = await CreateDbContextAsync();
        var service = new SyncService(db);
        var userKey = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var baseTime = DateTimeOffset.UtcNow;

        var request = new SyncRequest(
            ClientId: Guid.NewGuid(),
            LastSyncAt: null,
            Operations:
            [
                new SyncOperation(
                    OpId: Guid.NewGuid(),
                    Type: "upsert",
                    EntityId: taskId,
                    CreatedAt: baseTime,
                    Task: new TaskUpsertPayload(
                        Id: taskId,
                        Title: "First",
                        Notes: null,
                        IsCompleted: false,
                        DueAt: null,
                        UpdatedAt: baseTime,
                        DeletedAt: null,
                        BaseVersion: null
                    )
                ),
                new SyncOperation(
                    OpId: Guid.NewGuid(),
                    Type: "upsert",
                    EntityId: taskId,
                    CreatedAt: baseTime.AddSeconds(1),
                    Task: new TaskUpsertPayload(
                        Id: taskId,
                        Title: "Second",
                        Notes: "Updated",
                        IsCompleted: true,
                        DueAt: null,
                        UpdatedAt: baseTime.AddSeconds(1),
                        DeletedAt: null,
                        BaseVersion: null
                    )
                ),
            ]
        );

        var response = await service.ProcessAsync(userKey, request);

        Assert.Equal(2, response.AppliedOpIds.Count);
        Assert.Empty(response.Rejected);

        var saved = await db.Tasks.SingleAsync(t => t.Id == taskId && t.UserKey == userKey);
        Assert.Equal("Second", saved.Title);
        Assert.Equal("Updated", saved.Notes);
        Assert.True(saved.IsCompleted);
        Assert.Equal(2, saved.Version);
    }

    [Fact]
    public async Task ProcessAsync_WhenIncomingWriteIsStale_RejectsWithConflict()
    {
        using var db = await CreateDbContextAsync();
        var userKey = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var serverTimestamp = DateTimeOffset.UtcNow;

        db.Tasks.Add(new TaskItem
        {
            Id = taskId,
            UserKey = userKey,
            Title = "Server Copy",
            Notes = null,
            IsCompleted = false,
            DueAt = null,
            UpdatedAt = serverTimestamp,
            DeletedAt = null,
            Version = 3,
        });
        await db.SaveChangesAsync();

        var service = new SyncService(db);
        var staleClientTimestamp = serverTimestamp.AddMinutes(-5);
        var opId = Guid.NewGuid();
        var request = new SyncRequest(
            ClientId: Guid.NewGuid(),
            LastSyncAt: null,
            Operations:
            [
                new SyncOperation(
                    OpId: opId,
                    Type: "upsert",
                    EntityId: taskId,
                    CreatedAt: staleClientTimestamp,
                    Task: new TaskUpsertPayload(
                        Id: taskId,
                        Title: "Stale Change",
                        Notes: "should be rejected",
                        IsCompleted: true,
                        DueAt: null,
                        UpdatedAt: staleClientTimestamp,
                        DeletedAt: null,
                        BaseVersion: 2
                    )
                ),
            ]
        );

        var response = await service.ProcessAsync(userKey, request);

        Assert.Empty(response.AppliedOpIds);
        Assert.Single(response.Rejected);
        Assert.Equal(opId, response.Rejected[0].OpId);
        Assert.Equal("conflict", response.Rejected[0].Reason);

        var saved = await db.Tasks.SingleAsync(t => t.Id == taskId && t.UserKey == userKey);
        Assert.Equal("Server Copy", saved.Title);
        Assert.False(saved.IsCompleted);
        Assert.Equal(3, saved.Version);
    }

    private static async Task<AppDbContext> CreateDbContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}
