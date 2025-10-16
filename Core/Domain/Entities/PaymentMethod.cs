using Domain.Common;

namespace Domain.Entities;

public class PaymentMethod : BaseEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
