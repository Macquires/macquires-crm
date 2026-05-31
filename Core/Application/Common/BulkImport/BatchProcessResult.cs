namespace Application.Common.BulkImport;

public sealed class BatchProcessResult
{
    public int SuccessCount { get; init; }
    public int ErrorCount { get; init; }
    public IReadOnlyList<RowImportError> Errors { get; init; } = Array.Empty<RowImportError>();
}
