using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class ProgramManager : BaseEntity
{
    public string? Title { get; set; }
    public string? Number { get; set; }
    public string? Summary { get; set; }
    public ProgramManagerStatus? Status { get; set; } = ProgramManagerStatus.Draft;
    public ProgramManagerPriority? Priority { get; set; } = ProgramManagerPriority.Low;
    public string? ProgramManagerResourceId { get; set; }
    public ProgramManagerResource? ProgramManagerResource { get; set; }
}
