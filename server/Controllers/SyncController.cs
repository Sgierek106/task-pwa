using Microsoft.AspNetCore.Mvc;
using TaskPwa.Server.Models.Sync;
using TaskPwa.Server.Services;

namespace TaskPwa.Server.Controllers;

[ApiController]
[Route("api/sync")]
/// <summary>
/// Handles synchronization requests for task data.
/// </summary>
/// <param name="syncService">Synchronization service.</param>
public sealed class SyncController(ISyncService syncService) : ControllerBase
{
    private readonly ISyncService _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));

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
    /// Applies a batch of client operations and returns server-side changes.
    /// </summary>
    /// <param name="request">Incoming sync request payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The sync response with applied operations, conflicts, and changed tasks.</returns>
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
