namespace Application.Common.BulkImport;

public static class BulkImportLimits
{
    public const int MaxRowsPerJob = 100_000;
    public const int MaxInlineEnqueueRows = 500;
    public const int DefaultBatchSize = 1000;
}
