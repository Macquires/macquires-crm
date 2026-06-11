namespace Application.Common.Integrations;

public sealed record OracleInventoryIngestResult(
    int MsisdnsInserted,
    int MsisdnsReset,
    int MsisdnsSkipped,
    int SimsInserted,
    int SimsReset,
    int SimsSkipped,
    string Message);

public interface IOracleInventoryIngestor
{
    Task<OracleInventoryIngestResult> UpsertFeedAsync(
        OracleInventoryPullResult pull,
        bool resetExistingForDemo = true,
        CancellationToken cancellationToken = default);

    Task<OracleInventoryIngestResult> UpsertStandardCatalogAsync(
        bool resetExistingForDemo = true,
        CancellationToken cancellationToken = default);
}
