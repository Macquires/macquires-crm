using System.ComponentModel;

namespace Domain.Enums;

public enum ClosingStatus
{
    [Description("ClosedLost")]
    ClosedLost = 0,
    [Description("ClosedWon")]
    ClosedWon = 1,
    [Description("OnProgress")]
    OnProgress = 2
}
