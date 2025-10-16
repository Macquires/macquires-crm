using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class PurchaseRequisition : BaseEntity
{
    public string? Number { get; set; }
    public DateTime? RequisitionDate { get; set; }
    public PurchaseRequisitionStatus? RequisitionStatus { get; set; }
    public string? Description { get; set; }
    public string? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string? TaxId { get; set; }
    public Tax? Tax { get; set; }
    public double? BeforeTaxAmount { get; set; }
    public double? TaxAmount { get; set; }
    public double? AfterTaxAmount { get; set; }
    public ICollection<PurchaseRequisitionItem> PurchaseRequisitionItemList { get; set; } = new List<PurchaseRequisitionItem>();
}
