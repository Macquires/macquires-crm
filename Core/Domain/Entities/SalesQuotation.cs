using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class SalesQuotation : BaseEntity
{
    public string? Number { get; set; }
    public DateTime? QuotationDate { get; set; }
    public SalesQuotationStatus? QuotationStatus { get; set; }
    public string? Description { get; set; }
    public string? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string? TaxId { get; set; }
    public Tax? Tax { get; set; }
    public double? BeforeTaxAmount { get; set; }
    public double? TaxAmount { get; set; }
    public double? AfterTaxAmount { get; set; }
    public ICollection<SalesQuotationItem> SalesQuotationItemList { get; set; } = new List<SalesQuotationItem>();
}
