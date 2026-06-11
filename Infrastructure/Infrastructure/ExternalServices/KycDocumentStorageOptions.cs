namespace Infrastructure.ExternalServices;

public sealed class KycDocumentStorageOptions
{
    public const string SectionName = "KycDocumentStorage";

    /// <summary>Physical vault root (relative to content root or absolute). Default: wwwroot/secure_kyc_vault/</summary>
    public string VaultRootPath { get; set; } = "wwwroot/secure_kyc_vault";

    public int MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
}
