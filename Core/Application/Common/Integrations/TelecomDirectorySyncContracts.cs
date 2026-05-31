namespace Application.Common.Integrations;

public sealed record TelecomDirectoryMsisdnChangeRequest(
    string CustomerId,
    string MsisdnAssetId,
    string OldMsisdn,
    string NewMsisdn,
    string? CorrelationId);

public sealed record TelecomDirectorySyncResult(bool Success, string Message);

/// <summary>External HLR / directory (e.g. Huawei) — mock in Infrastructure until real API is wired.</summary>
public interface ITelecomDirectorySync
{
    Task<TelecomDirectorySyncResult> NotifyMsisdnChangedAsync(
        TelecomDirectoryMsisdnChangeRequest request,
        CancellationToken cancellationToken = default);
}
