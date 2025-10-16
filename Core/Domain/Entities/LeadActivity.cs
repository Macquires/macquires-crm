using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class LeadActivity : BaseEntity
{
    public string? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public string? Number { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public LeadActivityType? Type { get; set; }
    public string? AttachmentName { get; set; }
}
