using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskPwa.Server.Data;
using TaskPwa.Server.Models;

namespace TaskPwa.Server.Controllers;

[ApiController]
[Route("api/tasks")]
/// <summary>
/// Exposes task query endpoints scoped to the current user key.
/// </summary>
/// <param name="db">Application database context.</param>
public sealed class TasksController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

    private Guid? GetUserKey()
    {
        if (Request.Headers.TryGetValue("X-User-Key", out var values) &&
            Guid.TryParse(values.FirstOrDefault(), out var userKey))
        {
            return userKey;
        }
        return null;
    }

    /// <summary>
    /// Returns all tasks for the authenticated user changed after <paramref name="since"/>.
    /// </summary>
    /// <param name="since">Optional watermark used to fetch only changed tasks.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of task DTOs changed after the provided watermark.</returns>
    [HttpGet("changes")]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetChanges([FromQuery] DateTimeOffset? since, CancellationToken ct)
    {
        var userKey = GetUserKey();
        if (userKey is null)
            return BadRequest(new { error = "X-User-Key header is required and must be a valid GUID." });

        IQueryable<TaskItem> query = _db.Tasks.Where(t => t.UserKey == userKey.Value);
        if (since.HasValue)
            query = query.Where(t => t.UpdatedAt > since.Value);

        var tasks = await query
            .OrderBy(t => t.UpdatedAt)
            .Select(t => new TaskDto(t.Id, t.Title, t.Notes, t.IsCompleted, t.DueAt, t.UpdatedAt, t.DeletedAt, t.Version))
            .ToListAsync(ct);

        return Ok(tasks);
    }
}
