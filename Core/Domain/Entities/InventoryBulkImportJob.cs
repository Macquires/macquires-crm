using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public enum InventoryBulkImportJobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    PartiallySucceeded = 4
}

/// <summary>Background bulk import job (up to 100k rows).</summary>
public class InventoryBulkImportJob : BaseEntity, IHasBranchId
{
    public string? BranchId { get; set; }
    public InventoryBulkImportJobStatus JobStatus { get; set; } = InventoryBulkImportJobStatus.Pending;
    public BulkImportJobType JobType { get; set; } = BulkImportJobType.MsisdnAsset;
    public string? FileName { get; set; }
    public string? StoredFilePath { get; set; }
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorSummary { get; set; }
    /// <summary>Import options/metadata only — not full row payload.</summary>
    public string? PayloadJson { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
