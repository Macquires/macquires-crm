namespace Application.Common.Telecom;

/// <summary>Persists identity scans attached to telecom operations (take-over legal gate).</summary>
public interface ITelecomOperationDocumentStore
{
    Task<string> SaveIdentityDocumentAsync(
        string operationId,
        Stream content,
        string originalFileName,
        CancellationToken cancellationToken = default);

    string GetAbsolutePath(string storageKey);

    bool Exists(string? storageKey);
}
