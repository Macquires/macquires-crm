using Domain.Common;

namespace Domain.Entities;

public class BookingGroup : BaseEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}