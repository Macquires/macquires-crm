using System.ComponentModel;

namespace Domain.Enums;

public enum InvoiceStatus
{
    [Description("Draft")]
    Draft = 0,
    [Description("Cancelled")]
    Cancelled = 1,
    [Description("Confirmed")]
    Confirmed = 2,
    [Description("Partial Paid")]
    PartialPaid = 3,
    [Description("Full Paid")]
    FullPaid = 4
}