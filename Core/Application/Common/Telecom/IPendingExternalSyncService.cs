namespace Application.Common.Telecom;

public sealed record PendingExternalSyncResult(
    int Processed,
    int Succeeded,
    int StillPending,
    int Failed,
    IReadOnlyList<string> Errors);

/// <summary>Replays operations left in PendingExternal when integrations are turned back on.</summary>
public interface IPendingExternalSyncService
{
    Task<PendingExternalSyncResult> FlushAsync(string? actorUserId = null, CancellationToken cancellationToken = default);
}
