using Application.Common.Exceptions;
using Domain.Enums;

namespace Application.Common.BulkImport;

/// <summary>Inventory bulk import is retired — legacy CRM migration only (customers / packages).</summary>
public static class BulkImportLegacyMigrationGuard
{
    public const string InventoryRetiredMessageAr =
        "استيراد مخزون MSISDN/SIM عبر الملفات متوقف. المخزون يُزامَن حصرياً من Oracle Fusion SCM.";

    public const string InventoryRetiredMessageEn =
        "MSISDN/SIM inventory file import is disabled. Inventory is synced exclusively from Oracle Fusion SCM.";

    public static void EnsureLegacyMigrationJobType(BulkImportJobType jobType)
    {
        if (jobType == BulkImportJobType.MsisdnAsset)
        {
            throw new BusinessRuleViolationException(InventoryRetiredMessageAr);
        }
    }
}
