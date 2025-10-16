using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Bill : BaseEntity
{
    public string? Number { get; set; }
    public DateTime? BillDate { get; set; }
    public BillStatus? BillStatus { get; set; }
    public string? Description { get; set; }
    public string? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}
