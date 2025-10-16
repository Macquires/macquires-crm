using System.ComponentModel;

namespace Domain.Enums;

public enum BookingStatus
{
    [Description("Cancelled")]
    Cancelled = 0,
    [Description("Draft")]
    Draft = 1,
    [Description("Confirmed")]
    Confirmed = 2,
    [Description("OnProgress")]
    OnProgress = 3,
    [Description("Done")]
    Done = 4
}
