namespace Infrastructure.Security.FieldEncryption;

public class FieldEncryptionOptions
{
    public const string SectionName = "FieldEncryption";

    /// <summary>32-byte AES-256 key, Base64-encoded.</summary>
    public string KeyBase64 { get; set; } = "";

    /// <summary>HMAC key for searchable hashes, Base64-encoded.</summary>
    public string SearchHashKeyBase64 { get; set; } = "";
}
