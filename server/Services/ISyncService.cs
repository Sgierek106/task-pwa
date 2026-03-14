using TaskPwa.Server.Models.Sync;

namespace TaskPwa.Server.Services;

/// <summary>
/// Coordinates synchronization between client operations and server task state.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Applies the incoming client operations and returns changed tasks since the provided watermark.
    /// </summary>
    /// <param name="userKey">Tenant key used to scope task data.</param>
    /// <param name="request">Client sync payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A response containing server time, applied operations, conflicts, and changed tasks.</returns>
    Task<SyncResponse> ProcessAsync(Guid userKey, SyncRequest request, CancellationToken ct = default);
}
