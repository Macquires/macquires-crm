using System.ComponentModel;

namespace Domain.Enums;

public enum CampaignStatus
{
    [Description("Draft")]
    Draft = 0,
    [Description("Cancelled")]
    Cancelled = 1,
    [Description("Confirmed")]
    Confirmed = 2,
    [Description("OnProgress")]
    OnProgress = 3,
    [Description("OnHold")]
    OnHold = 4,
    [Description("Finished")]
    Finished = 5,
    [Description("Archived")]
    Archived = 6
}
