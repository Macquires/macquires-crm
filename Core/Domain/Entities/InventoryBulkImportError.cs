using Domain.Common;

namespace Domain.Entities;

public class InventoryBulkImportError : BaseEntity
{
    public string JobId { get; set; } = null!;
    public InventoryBulkImportJob? Job { get; set; }
    public int RowNumber { get; set; }
    public string? Identifier { get; set; }
    public string? ErrorMessageAr { get; set; }
    public string? ErrorMessageEn { get; set; }
    public string? RawRowDataJson { get; set; }
}
