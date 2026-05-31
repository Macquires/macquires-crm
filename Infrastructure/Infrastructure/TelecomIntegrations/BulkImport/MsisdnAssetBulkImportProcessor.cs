using Application.Common.BulkImport;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.TelecomIntegrations.BulkImport;

public sealed class MsisdnAssetBulkImportProcessor : IBulkImportJobProcessor
{
    private readonly DataContext _db;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepo;
    private readonly ICommandRepository<SimInventory> _simRepo;

    public MsisdnAssetBulkImportProcessor(
        DataContext db,
        ICommandRepository<MsisdnAsset> msisdnRepo,
        ICommandRepository<SimInventory> simRepo)
    {
        _db = db;
        _msisdnRepo = msisdnRepo;
        _simRepo = simRepo;
    }

    public BulkImportJobType JobType => BulkImportJobType.MsisdnAsset;

    public IReadOnlyList<string> RequiredHeaders => BulkImportSchemas.MsisdnAssetHeaders;

    public string TemplateFileName => "Template_MsisdnAsset.csv";

    public async Task<BatchProcessResult> ProcessBatchAsync(
        InventoryBulkImportJob job,
        IReadOnlyList<ParsedImportRow> rows,
        int startRowNumber,
        CancellationToken cancellationToken)
    {
        var errors = new List<RowImportError>();
        var success = 0;

        for (var i = 0; i < rows.Count; i++)
        {
            var rowNum = startRowNumber + i;
            var row = rows[i];
            try
            {
                var msisdnRaw = row.Get("msisdn");
                if (string.IsNullOrWhiteSpace(msisdnRaw))
                {
                    errors.Add(Err(rowNum, null, "MSISDN مطلوب.", "MSISDN is required.", row));
                    continue;
                }

                var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(msisdnRaw) ?? msisdnRaw.Trim();
                var imsiRaw = row.Get("imsi", "IMSI");
                string? pairedIccid = null;
                string? pairedImsi = string.IsNullOrWhiteSpace(imsiRaw) ? null : imsiRaw.Trim();

                var iccidRaw = row.Get("iccid", "ICCID");
                if (!string.IsNullOrWhiteSpace(iccidRaw))
                {
                    if (!IccidValidator.TryValidate(iccidRaw, out var iccid, out var iccidErr))
                    {
                        errors.Add(Err(rowNum, msisdn, iccidErr ?? "ICCID غير صالح.", iccidErr ?? "Invalid ICCID.", row));
                        continue;
                    }

                    pairedIccid = iccid;
                    var iccidTaken = await _db.SimInventory.AnyAsync(s => !s.IsDeleted && s.Iccid == iccid, cancellationToken);
                    if (!iccidTaken)
                    {
                        var sim = SimInventory.Create(
                            iccid,
                            imsi: pairedImsi,
                            puk1: row.Get("puk1", "Puk1"),
                            puk2: row.Get("puk2", "Puk2"),
                            simType: SimType.Physical,
                            eid: null);
                        sim.CreatedById = job.CreatedById;
                        await _simRepo.CreateAsync(sim, cancellationToken);
                    }
                }

                var asset = await _db.MsisdnAsset
                    .FirstOrDefaultAsync(m => !m.IsDeleted && m.Msisdn == msisdn, cancellationToken);
                if (asset == null)
                {
                    await _msisdnRepo.CreateAsync(new MsisdnAsset
                    {
                        Msisdn = msisdn,
                        PairedIccid = pairedIccid,
                        PairedImsi = pairedImsi,
                        PoolStatus = MsisdnPoolStatus.Available,
                        CountryCode = "963",
                        CreatedById = job.CreatedById
                    }, cancellationToken);
                }
                else if (!string.IsNullOrWhiteSpace(pairedIccid) || !string.IsNullOrWhiteSpace(pairedImsi))
                {
                    if (!string.IsNullOrWhiteSpace(pairedIccid))
                    {
                        asset.PairedIccid = pairedIccid;
                    }

                    if (!string.IsNullOrWhiteSpace(pairedImsi))
                    {
                        asset.PairedImsi = pairedImsi;
                    }

                    await _db.SaveChangesAsync(cancellationToken);
                }

                success++;
            }
            catch (Exception ex)
            {
                errors.Add(Err(rowNum, row.Get("msisdn"), ex.Message, ex.Message, row));
            }
        }

        return new BatchProcessResult
        {
            SuccessCount = success,
            ErrorCount = errors.Count,
            Errors = errors
        };
    }

    private static RowImportError Err(int row, string? id, string ar, string en, ParsedImportRow rowData) =>
        new()
        {
            RowNumber = row,
            Identifier = id,
            ErrorMessageAr = ar,
            ErrorMessageEn = en,
            Row = rowData
        };
}
