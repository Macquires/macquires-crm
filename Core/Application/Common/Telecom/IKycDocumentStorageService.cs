namespace Application.Common.Telecom;

/// <summary>
/// Sovereign KYC document vault — streams files to secure storage (blob-compatible contract).
/// </summary>
public interface IKycDocumentStorageService
{
    Task<string> StoreKycDocumentAsync(
        string msisdn,
        string fileName,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> GetKycDocumentAsync(
        string documentReferenceId,
        CancellationToken cancellationToken = default);

    bool DocumentExists(string? documentReferenceId);

    /// <summary>Opens a stored KYC file when <paramref name="documentReferenceId"/> exists.</summary>
    Task<(Stream Stream, string ContentType)?> TryOpenKycDocumentAsync(
        string documentReferenceId,
        CancellationToken cancellationToken = default);
}
