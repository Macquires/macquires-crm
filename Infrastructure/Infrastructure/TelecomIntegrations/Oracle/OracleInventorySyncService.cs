using Application.Common.Integrations;

using Microsoft.Extensions.Logging;



namespace Infrastructure.TelecomIntegrations.Oracle;



/// <summary>Scheduled Oracle → CRM inventory sync (delegates to idempotent ingestor).</summary>

public sealed class OracleInventorySyncService : IOracleInventorySyncService

{

    private readonly IOracleFusionInventoryClient _oracle;

    private readonly IOracleInventoryIngestor _ingestor;

    private readonly ILogger<OracleInventorySyncService> _logger;

    private DateTime? _lastSyncUtc;



    public OracleInventorySyncService(

        IOracleFusionInventoryClient oracle,

        IOracleInventoryIngestor ingestor,

        ILogger<OracleInventorySyncService> logger)

    {

        _oracle = oracle;

        _ingestor = ingestor;

        _logger = logger;

    }



    public async Task<OracleInventorySyncResult> SyncAsync(CancellationToken cancellationToken = default)

    {

        var pull = await _oracle.PullNewInventoryAsync(_lastSyncUtc, cancellationToken);

        if (!pull.Success)

        {

            return new OracleInventorySyncResult(0, 0, 0, 0, pull.Message);

        }



        var ingest = await _ingestor.UpsertFeedAsync(pull, resetExistingForDemo: false, cancellationToken);

        _lastSyncUtc = DateTime.UtcNow;



        var message =

            $"Oracle sync: MSISDN +{ingest.MsisdnsInserted}/reset {ingest.MsisdnsReset}, " +

            $"SIM +{ingest.SimsInserted}/reset {ingest.SimsReset}.";

        _logger.LogInformation(message);



        return new OracleInventorySyncResult(

            ingest.MsisdnsInserted,

            ingest.MsisdnsSkipped,

            ingest.SimsInserted,

            ingest.SimsSkipped,

            message);

    }

}


