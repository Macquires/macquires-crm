namespace Application.Common.BulkImport;

/// <summary>Approved bulk-import CSV header templates per job type.</summary>
public static class BulkImportSchemas
{
    /// <summary>Retired — inventory is synced from Oracle Fusion SCM only.</summary>
    public static readonly string[] MsisdnAssetHeaders =
        ["MSISDN", "IMSI", "ICCID", "Pin1", "Puk1"];

    public static readonly string[] CustomerProfilesHeaders =
        ["CustomerCode", "FullNameAr", "FullNameEn", "NationalId", "CustomerType"];

    public static readonly string[] PackageMigrationHeaders =
        ["MSISDN", "CurrentOfferCode", "NewOfferCode"];
}
