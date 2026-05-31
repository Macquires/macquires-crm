namespace Application.Common.BulkImport;

public interface IBulkImportFileStore
{
    Task<string> SaveUploadAsync(string jobId, Stream content, string originalFileName, CancellationToken cancellationToken);

    string GetAbsolutePath(string storedRelativePath);

    Task<int> CountDataRowsAsync(string storedRelativePath, CancellationToken cancellationToken);

    IAsyncEnumerable<ParsedImportRow> ReadRowsAsync(string storedRelativePath, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ReadHeaderColumnsAsync(string storedRelativePath, CancellationToken cancellationToken);
}
