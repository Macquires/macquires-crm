using System.ComponentModel;

namespace Domain.Enums;

public enum ProgramManagerPriority
{
    [Description("Low")]
    Low = 0,
    [Description("High")]
    High = 1,
    [Description("Normal")]
    Normal = 2,
    [Description("Critical")]
    Critical = 3
}
