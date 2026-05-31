using Application.Common.BulkImport;
using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.TelecomIntegrations.BulkImport;

public sealed class CustomerProfilesBulkImportProcessor : IBulkImportJobProcessor
{
    private readonly DataContext _db;
    private readonly ICommandRepository<Customer> _customerRepo;
    private readonly IFieldEncryptionService _encryption;

    public CustomerProfilesBulkImportProcessor(
        DataContext db,
        ICommandRepository<Customer> customerRepo,
        IFieldEncryptionService encryption)
    {
        _db = db;
        _customerRepo = customerRepo;
        _encryption = encryption;
    }

    public BulkImportJobType JobType => BulkImportJobType.CustomerProfiles;

    public IReadOnlyList<string> RequiredHeaders => BulkImportSchemas.CustomerProfilesHeaders;

    public string TemplateFileName => "Template_CustomerProfiles.csv";

    public async Task<BatchProcessResult> ProcessBatchAsync(
        InventoryBulkImportJob job,
        IReadOnlyList<ParsedImportRow> rows,
        int startRowNumber,
        CancellationToken cancellationToken)
    {
        var errors = new List<RowImportError>();
        var success = 0;

        var groupId = await _db.CustomerGroup.AsNoTracking()
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.CreatedAtUtc)
            .Select(g => g.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var categoryId = await _db.CustomerCategory.AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(categoryId))
        {
            return new BatchProcessResult
            {
                ErrorCount = rows.Count,
                Errors =
                [
                    new RowImportError
                    {
                        RowNumber = startRowNumber,
                        ErrorMessageAr = "لا يوجد مجموعة أو فئة عملاء في النظام.",
                        ErrorMessageEn = "No customer group or category configured in the system."
                    }
                ]
            };
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var rowNum = startRowNumber + i;
            var row = rows[i];
            try
            {
                var code = row.Get("customercode");
                var nameAr = row.Get("fullnamear");
                var nameEn = row.Get("fullnameen");
                var nationalId = row.Get("nationalid");
                var typeRaw = row.Get("customertype");

                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(nameAr)
                    || string.IsNullOrWhiteSpace(nationalId) || string.IsNullOrWhiteSpace(typeRaw))
                {
                    errors.Add(Err(rowNum, code, "جميع أعمدة القالب مطلوبة بقيم غير فارغة.",
                        "All template columns require non-empty values.", row));
                    continue;
                }

                var isCorporate = string.Equals(typeRaw, "corporate", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(typeRaw, "2", StringComparison.Ordinal)
                    || string.Equals(typeRaw, "company", StringComparison.OrdinalIgnoreCase);

                var displayName = string.IsNullOrWhiteSpace(nameEn) ? nameAr : $"{nameAr} / {nameEn}";
                var address = new PostalAddress("-", "Damascus", "Damascus", "00000", "SY");
                var phoneSuffix = new string(code.Where(char.IsDigit).TakeLast(8).ToArray()).PadLeft(8, '0');
                var phone = "09" + phoneSuffix;

                if (isCorporate)
                {
                    var dup = await _db.Customer.AnyAsync(
                        c => !c.IsDeleted && c.AccountNumber == code.Trim(),
                        cancellationToken);
                    if (dup)
                    {
                        errors.Add(Err(rowNum, code, "رمز العميل مسجّل مسبقاً.", "Customer code already exists.", row));
                        continue;
                    }

                    var corp = CorporateCustomer.Create(
                        displayName,
                        code.Trim(),
                        nationalId.Trim(),
                        address,
                        $"{code.Trim()}@bulk-import.local",
                        phone,
                        groupId,
                        categoryId);
                    corp.CreatedById = job.CreatedById;
                    await _customerRepo.CreateAsync(corp, cancellationToken);
                }
                else
                {
                    if (nationalId.Trim().Length != 10)
                    {
                        errors.Add(Err(rowNum, code, "الرقم الوطني يجب أن يكون 10 خانات للأفراد.",
                            "National ID must be 10 digits for individuals.", row));
                        continue;
                    }

                    var hash = _encryption.ComputeSearchHash(nationalId.Trim());
                    var dupNat = await _db.Customer.OfType<IndividualCustomer>()
                        .AnyAsync(c => c.NationalIdSearchHash == hash, cancellationToken);
                    if (dupNat)
                    {
                        errors.Add(Err(rowNum, code, "الرقم الوطني مسجّل مسبقاً.", "National ID already registered.", row));
                        continue;
                    }

                    var dupCode = await _db.Customer.AnyAsync(
                        c => !c.IsDeleted && c.AccountNumber == code.Trim(),
                        cancellationToken);
                    if (dupCode)
                    {
                        errors.Add(Err(rowNum, code, "رمز العميل مسجّل مسبقاً.", "Customer code already exists.", row));
                        continue;
                    }

                    var individual = IndividualCustomer.Create(
                        nameAr.Trim(),
                        code.Trim(),
                        nationalId.Trim(),
                        address,
                        $"{code.Trim()}@bulk-import.local",
                        phone,
                        groupId,
                        categoryId,
                        null);
                    individual.CreatedById = job.CreatedById;
                    individual.SetNationalIdSearchHash(hash);
                    await _customerRepo.CreateAsync(individual, cancellationToken);
                }

                success++;
            }
            catch (Exception ex)
            {
                errors.Add(Err(rowNum, row.Get("customercode"), ex.Message, ex.Message, row));
            }
        }

        return new BatchProcessResult { SuccessCount = success, ErrorCount = errors.Count, Errors = errors };
    }

    private static RowImportError Err(int row, string? id, string ar, string en, ParsedImportRow rowData) =>
        new() { RowNumber = row, Identifier = id, ErrorMessageAr = ar, ErrorMessageEn = en, Row = rowData };
}
