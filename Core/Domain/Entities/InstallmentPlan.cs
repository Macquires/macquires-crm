using Domain.Common;

namespace Domain.Entities;

/// <summary>كتالوج خطط التقسيط (§14).</summary>
public class InstallmentPlan : BaseEntity
{
    public string Code { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public int Months { get; set; }
    public decimal MinDownPaymentPercent { get; set; }
    public decimal InterestRatePercent { get; set; }
    public int MinCreditScore { get; set; }
    public bool IsActive { get; set; } = true;
}
