using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaskPwa.Server.Data;
using TaskPwa.Server.Models;
using TaskPwa.Server.Models.Sync;
using TaskPwa.Server.Services;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace TaskPwa.Server.Tests;

[TestClass]
public sealed class SyncServiceTests
{
    [TestMethod]
    public async Task ProcessAsync_WhenMultipleOpsTargetSameTask_DoesNotThrowAndAppliesLatest()
    {
        // Arrange
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

        // Act
        var response = await service.ProcessAsync(userKey, request);

        // Assert
        response.AppliedOpIds.Should().HaveCount(2);
        response.Rejected.Should().BeEmpty();

        var saved = await db.Tasks.SingleAsync(t => t.Id == taskId && t.UserKey == userKey);
        saved.Title.Should().Be("Second");
        saved.Notes.Should().Be("Updated");
        saved.IsCompleted.Should().BeTrue();
        saved.Version.Should().Be(2);
    }

    [TestMethod]
    public async Task ProcessAsync_WhenIncomingWriteIsStale_RejectsWithConflict()
    {
        // Arrange
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

        // Act
        var response = await service.ProcessAsync(userKey, request);

        // Assert
        response.AppliedOpIds.Should().BeEmpty();
        response.Rejected.Should().ContainSingle();
        response.Rejected[0].OpId.Should().Be(opId);
        response.Rejected[0].Reason.Should().Be("conflict");

        var saved = await db.Tasks.SingleAsync(t => t.Id == taskId && t.UserKey == userKey);
        saved.Title.Should().Be("Server Copy");
        saved.IsCompleted.Should().BeFalse();
        saved.Version.Should().Be(3);
    }

    [TestMethod]
    public void Constructor_WhenDbContextIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        Action act = () => _ = new SyncService(db: null!);

        // Act + Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("db");
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
