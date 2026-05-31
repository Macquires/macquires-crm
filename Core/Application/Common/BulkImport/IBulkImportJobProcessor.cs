using Domain.Entities;
using Domain.Enums;

namespace Application.Common.BulkImport;

public interface IBulkImportJobProcessor
{
    BulkImportJobType JobType { get; }

    /// <summary>Approved template columns (exact set required in file header row).</summary>
    IReadOnlyList<string> RequiredHeaders { get; }

    string TemplateFileName { get; }

    Task<BatchProcessResult> ProcessBatchAsync(
        InventoryBulkImportJob job,
        IReadOnlyList<ParsedImportRow> rows,
        int startRowNumber,
        CancellationToken cancellationToken);
}
