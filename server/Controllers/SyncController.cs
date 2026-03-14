using Microsoft.AspNetCore.Mvc;
using TaskPwa.Server.Models.Sync;
using TaskPwa.Server.Services;

namespace TaskPwa.Server.Controllers;

[ApiController]
[Route("api/sync")]
public sealed class SyncController : ControllerBase
{
    private readonly SyncService _syncService;

    public SyncController(SyncService syncService)
    {
        _syncService = syncService;
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
    /// Apply a batch of client operations and return server-side changes.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Sync([FromBody] SyncRequest request, CancellationToken ct)
    {
        var userKey = GetUserKey();
        if (userKey is null)
            return BadRequest(new { error = "X-User-Key header is required and must be a valid GUID." });

        var response = await _syncService.ProcessAsync(userKey.Value, request, ct);
        return Ok(response);
    }
}
