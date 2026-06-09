namespace Application.Common.Integrations;

public sealed record HlrLiveStatusResult(
    bool Success,
    string Message,
    bool IsOnline,
    string? Location,
    string? ActiveImsi,
    string? HlrSubscriberState,
    bool DiffersFromCrm,
    string? CrmOperationalStatus);

public sealed record HlrResyncRequest(
    string SubscriberProfileId,
    string Msisdn,
    string? CorrelationId,
    string? ActorUserId);

public sealed record HlrResyncResult(bool Success, string Message);

public sealed record HlrReprovisionResult(
    bool Success,
    string Message,
    string? LogEntry,
    string? HlrSubscriberState);

public sealed record HlrReprovisionRequest(
    string SubscriberProfileId,
    string Msisdn,
    string? Imsi,
    string? Iccid,
    string? ActorUserId);

public interface IHLRLiveStatusService
{
    Task<HlrLiveStatusResult> QueryLiveStatusAsync(string msisdn, string? crmOperationalStatus, CancellationToken cancellationToken = default);
    Task<HlrResyncResult> ResyncFromHlrAsync(HlrResyncRequest request, CancellationToken cancellationToken = default);
    Task<HlrReprovisionResult> ReprovisionSubscriberAsync(HlrReprovisionRequest request, CancellationToken cancellationToken = default);
}
