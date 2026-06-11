namespace Application.Common.Integrations;

public sealed record OracleMsisdnFeedItem(
    string Msisdn,
    string? CountryCode,
    string? Prefix,
    string? Category);

public sealed record OracleSimFeedItem(
    string Iccid,
    string? Imsi,
    string? Pin1,
    string? Puk1,
    bool IsESim = false);

public sealed record OracleInventoryPullResult(
    bool Success,
    string Message,
    IReadOnlyList<OracleMsisdnFeedItem> Msisdns,
    IReadOnlyList<OracleSimFeedItem> Sims);

public interface IOracleFusionInventoryClient
{
    Task<OracleInventoryPullResult> PullNewInventoryAsync(
        DateTime? sinceUtc,
        CancellationToken cancellationToken = default);
}

public sealed record OracleInventorySyncResult(
    int MsisdnsInserted,
    int MsisdnsSkipped,
    int SimsInserted,
    int SimsSkipped,
    string Message);

public interface IOracleInventorySyncService
{
    Task<OracleInventorySyncResult> SyncAsync(CancellationToken cancellationToken = default);
}
