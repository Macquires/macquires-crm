using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class PaymentReceive : BaseEntity
{
    public string? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public string? Number { get; set; }
    public string? Description { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public double? PaymentAmount { get; set; }
    public PaymentReceiveStatus? Status { get; set; }
}
