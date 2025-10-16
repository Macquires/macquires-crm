using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class PaymentDisburse : BaseEntity
{
    public string? BillId { get; set; }
    public Bill? Bill { get; set; }
    public string? Number { get; set; }
    public string? Description { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public double? PaymentAmount { get; set; }
    public PaymentDisburseStatus? Status { get; set; }
}
