using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Telecom 360° view linked to CRM <see cref="Customer"/> (SID-aligned party/subscriber split).
/// </summary>
public class SubscriberProfile : BaseEntity
{
    public string CustomerId { get; set; } = null!;
    public Customer? Customer { get; set; }

    /// <summary>Optional national ID for universal search (demo — treat as PII).</summary>
    public string? NationalId { get; set; }

    public int LoyaltyPoints { get; set; }
    public string? LoyaltyTier { get; set; }

    public decimal? PostpaidCreditLimit { get; set; }
    public decimal? PrepaidBalance { get; set; }

    /// <summary>0–100 demo risk score for churn placeholder UI.</summary>
    public int? ChurnRiskScore { get; set; }

    public string? MasterSubscriberProfileId { get; set; }
    public SubscriberProfile? MasterSubscriberProfile { get; set; }
    public ICollection<SubscriberProfile> ChildProfiles { get; set; } = new List<SubscriberProfile>();

    public ICollection<TelecomSubscription> Subscriptions { get; set; } = new List<TelecomSubscription>();
    public ICollection<MsisdnAsset> MsisdnAssets { get; set; } = new List<MsisdnAsset>();
    public ICollection<TelecomOperationRequest> OperationRequests { get; set; } = new List<TelecomOperationRequest>();
}
