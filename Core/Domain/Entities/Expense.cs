using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Expense : BaseEntity
{
    public string? Number { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? ExpenseDate { get; set; }
    public ExpenseStatus? Status { get; set; }
    public double? Amount { get; set; }
    public string? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

}
