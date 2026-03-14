using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskPwa.Server.Data;
using TaskPwa.Server.Models;

namespace TaskPwa.Server.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db)
    {
        _db = db;
    }

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
