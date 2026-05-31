using Application.Common.BulkImport;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.TelecomIntegrations.BulkImport;

public sealed class PackageMigrationBulkImportProcessor : IBulkImportJobProcessor
{
    private readonly DataContext _db;

    public PackageMigrationBulkImportProcessor(DataContext db) => _db = db;

    public BulkImportJobType JobType => BulkImportJobType.PackageMigration;

    public IReadOnlyList<string> RequiredHeaders => BulkImportSchemas.PackageMigrationHeaders;

    public string TemplateFileName => "Template_PackageMigration.csv";

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
                var currentCode = row.Get("currentoffercode");
                var newCode = row.Get("newoffercode");

                if (string.IsNullOrWhiteSpace(msisdnRaw) || string.IsNullOrWhiteSpace(newCode))
                {
                    errors.Add(Err(rowNum, msisdnRaw, "MSISDN و NewOfferCode مطلوبان.",
                        "MSISDN and NewOfferCode are required.", row));
                    continue;
                }

                var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(msisdnRaw) ?? msisdnRaw.Trim();
                var subscription = await _db.TelecomSubscription
                    .Include(s => s.MsisdnAsset)
                    .Include(s => s.Product)
                    .Where(s => !s.IsDeleted && s.MsisdnAsset != null && s.MsisdnAsset.Msisdn == msisdn)
                    .OrderByDescending(s => s.IsPrimaryLine)
                    .FirstOrDefaultAsync(cancellationToken);

                if (subscription == null)
                {
                    errors.Add(Err(rowNum, msisdn, "لا يوجد اشتراك لهذا الرقم.", "No subscription found for MSISDN.", row));
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(currentCode) && subscription.Product != null)
                {
                    var currentProductCode = subscription.Product.Number ?? subscription.Product.Name;
                    if (!string.Equals(currentProductCode, currentCode.Trim(), StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(subscription.Product.Name, currentCode.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(Err(rowNum, msisdn,
                            $"الباقة الحالية لا تطابق CurrentOfferCode ({currentCode}).",
                            $"Current package does not match CurrentOfferCode ({currentCode}).", row));
                        continue;
                    }
                }

                var offering = await _db.ProductOffering.AsNoTracking()
                    .FirstOrDefaultAsync(
                        o => !o.IsDeleted && o.IsActive && o.Code == newCode.Trim(),
                        cancellationToken);

                if (offering == null)
                {
                    errors.Add(Err(rowNum, msisdn, $"رمز العرض الجديد غير موجود: {newCode}.",
                        $"New offer code not found: {newCode}.", row));
                    continue;
                }

                var resolvedProductId = (offering.ProductId ?? "").Trim();
                if (string.IsNullOrEmpty(resolvedProductId))
                {
                    errors.Add(Err(rowNum, msisdn, "العرض غير مرتبط بمنتج تقني.", "Offering has no linked product.", row));
                    continue;
                }

                subscription.ProductId = resolvedProductId;
                subscription.UpdatedById = job.CreatedById;
                subscription.UpdatedAtUtc = DateTime.UtcNow;
                success++;
            }
            catch (Exception ex)
            {
                errors.Add(Err(rowNum, row.Get("msisdn"), ex.Message, ex.Message, row));
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new BatchProcessResult { SuccessCount = success, ErrorCount = errors.Count, Errors = errors };
    }

    private static RowImportError Err(int row, string? id, string ar, string en, ParsedImportRow rowData) =>
        new() { RowNumber = row, Identifier = id, ErrorMessageAr = ar, ErrorMessageEn = en, Row = rowData };
}
