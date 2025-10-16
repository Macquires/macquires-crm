using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;


public class Booking : BaseEntity
{
    public string? Subject { get; set; }
    public string? Number { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? StartTimezone { get; set; }
    public string? EndTimezone { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public bool? IsAllDay { get; set; }
    public bool? IsReadOnly { get; set; }
    public bool? IsBlock { get; set; }
    public string? RecurrenceRule { get; set; }
    public string? RecurrenceID { get; set; }
    public string? FollowingID { get; set; }
    public string? RecurrenceException { get; set; }
    public BookingStatus? Status { get; set; } = BookingStatus.Draft;
    public string? BookingResourceId { get; set; }
    public BookingResource? BookingResource { get; set; }
}
