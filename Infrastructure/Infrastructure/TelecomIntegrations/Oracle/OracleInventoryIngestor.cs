using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations.Oracle;

/// <summary>Idempotent Oracle → CRM inventory upsert with optional demo state reset.</summary>
public sealed class OracleInventoryIngestor : IOracleInventoryIngestor
{
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OracleInventoryIngestor> _logger;

    public OracleInventoryIngestor(
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<SimInventory> simRepository,
        IUnitOfWork unitOfWork,
        ILogger<OracleInventoryIngestor> logger)
    {
        _msisdnRepository = msisdnRepository;
        _simRepository = simRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<OracleInventoryIngestResult> UpsertStandardCatalogAsync(
        bool resetExistingForDemo = true,
        CancellationToken cancellationToken = default) =>
        UpsertFeedAsync(OracleFusionInventoryCatalog.BuildStandardPull(), resetExistingForDemo, cancellationToken);

    public async Task<OracleInventoryIngestResult> UpsertFeedAsync(
        OracleInventoryPullResult pull,
        bool resetExistingForDemo = true,
        CancellationToken cancellationToken = default)
    {
        if (!pull.Success)
        {
            return new OracleInventoryIngestResult(0, 0, 0, 0, 0, 0, pull.Message);
        }

        var msisdnInserted = 0;
        var msisdnReset = 0;
        var msisdnSkipped = 0;
        var simInserted = 0;
        var simReset = 0;
        var simSkipped = 0;

        foreach (var item in pull.Msisdns)
        {
            if (!MsisdnValidator.TryValidate(item.Msisdn, out var normalized, out _))
            {
                msisdnSkipped++;
                continue;
            }

            var existing = await _msisdnRepository.GetQuery()
                .FirstOrDefaultAsync(m => !m.IsDeleted && m.Msisdn == normalized, cancellationToken);

            if (existing == null)
            {
                await _msisdnRepository.CreateAsync(new MsisdnAsset
                {
                    Msisdn = normalized,
                    CountryCode = item.CountryCode ?? "963",
                    Prefix = item.Prefix,
                    Category = ParseCategory(item.Category),
                    PoolStatus = MsisdnPoolStatus.Available,
                }, cancellationToken);
                msisdnInserted++;
                continue;
            }

            if (!resetExistingForDemo || IsMsisdnBoundToLiveLine(existing))
            {
                msisdnSkipped++;
                continue;
            }

            ResetMsisdnForDemo(existing);
            existing.UpdatedAtUtc = DateTime.UtcNow;
            _msisdnRepository.Update(existing);
            msisdnReset++;
        }

        foreach (var item in pull.Sims)
        {
            if (!IccidValidator.TryValidate(item.Iccid, out var iccid, out _))
            {
                simSkipped++;
                continue;
            }

            var existing = await _simRepository.GetQuery()
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.Iccid == iccid, cancellationToken);

            if (existing == null)
            {
                var sim = SimInventory.Create(
                    iccid,
                    item.Imsi,
                    item.Pin1,
                    item.Puk1,
                    simType: item.IsESim ? SimType.ESim : SimType.Physical);
                await _simRepository.CreateAsync(sim, cancellationToken);
                simInserted++;
                continue;
            }

            if (!resetExistingForDemo || IsSimBoundToLiveLine(existing))
            {
                simSkipped++;
                continue;
            }

            ResetSimForDemo(existing);
            existing.UpdatedAtUtc = DateTime.UtcNow;
            _simRepository.Update(existing);
            simReset++;
        }

        if (msisdnInserted + msisdnReset + simInserted + simReset > 0)
        {
            await _unitOfWork.SaveAsync(cancellationToken);
        }

        var message =
            $"Oracle ingest: MSISDN +{msisdnInserted}/reset {msisdnReset}/skip {msisdnSkipped}, " +
            $"SIM +{simInserted}/reset {simReset}/skip {simSkipped}.";
        _logger.LogInformation(message);

        return new OracleInventoryIngestResult(
            msisdnInserted,
            msisdnReset,
            msisdnSkipped,
            simInserted,
            simReset,
            simSkipped,
            message);
    }

    private static bool IsMsisdnBoundToLiveLine(MsisdnAsset asset) =>
        asset.PoolStatus == MsisdnPoolStatus.Active
        && !string.IsNullOrEmpty(asset.SubscriberProfileId);

    private static bool IsSimBoundToLiveLine(SimInventory sim) =>
        sim.Status == SimStatus.Active
        && !string.IsNullOrEmpty(sim.SubscriberProfileId);

    private static void ResetMsisdnForDemo(MsisdnAsset asset) =>
        asset.ResetToAvailableForInventoryIngest();

    private static void ResetSimForDemo(SimInventory sim) =>
        sim.ResetToAvailableForInventoryIngest();

    private static MsisdnCategory ParseCategory(string? raw) =>
        Enum.TryParse<MsisdnCategory>(raw, true, out var cat) ? cat : MsisdnCategory.Normal;
}
