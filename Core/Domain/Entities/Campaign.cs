using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Campaign : BaseEntity
{
    public string? Number { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public double? TargetRevenueAmount { get; set; }
    public DateTime? CampaignDateStart { get; set; }
    public DateTime? CampaignDateFinish { get; set; }
    public CampaignStatus? Status { get; set; }
    public string? SalesTeamId { get; set; }
    public SalesTeam? SalesTeam { get; set; }
    public ICollection<Budget> CampaignBudgetList { get; set; } = new List<Budget>();
    public ICollection<Expense> CampaignExpenseList { get; set; } = new List<Expense>();
    public ICollection<Lead> CampaignLeadList { get; set; } = new List<Lead>();
}
