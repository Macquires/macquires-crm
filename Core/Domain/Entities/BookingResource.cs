using Domain.Common;

namespace Domain.Entities;

public class BookingResource : BaseEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? BookingGroupId { get; set; }
    public BookingGroup? BookingGroup { get; set; }
}
