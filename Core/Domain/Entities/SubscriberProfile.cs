using Domain.Common;
using Domain.Enums;
using Domain.Events;

namespace Domain.Entities;

/// <summary>
/// الرابط التشغيلي بين العميل (TPH) والخدمة/الخط — ليس تخزين الهوية القانونية.
/// </summary>
public class SubscriberProfile : BaseEntity
{
    public string CustomerId { get; set; } = null!;
    public Customer? Customer { get; set; }

    public ServiceLineType ServiceLineType { get; set; } = ServiceLineType.Mobile;
    public LanguagePreference LanguagePreference { get; set; } = LanguagePreference.Arabic;
    public SubscriberOperationalStatus OperationalStatus { get; set; } = SubscriberOperationalStatus.Pending;
    public DateTime? ActivationDateUtc { get; set; }

    public int LoyaltyPoints { get; set; }
    public string? LoyaltyTier { get; set; }
    public decimal? PostpaidCreditLimit { get; set; }
    public decimal? PrepaidBalance { get; set; }
    public int? ChurnRiskScore { get; set; }

    public string? MasterSubscriberProfileId { get; set; }
    public SubscriberProfile? MasterSubscriberProfile { get; set; }
    public ICollection<SubscriberProfile> ChildProfiles { get; set; } = new List<SubscriberProfile>();

    public ICollection<TelecomSubscription> Subscriptions { get; set; } = new List<TelecomSubscription>();
    public ICollection<MsisdnAsset> MsisdnAssets { get; set; } = new List<MsisdnAsset>();
    public ICollection<SimInventory> SimInventories { get; set; } = new List<SimInventory>();
    public ICollection<TelecomOperationRequest> OperationRequests { get; set; } = new List<TelecomOperationRequest>();

    public void Activate()
    {
        OperationalStatus = SubscriberOperationalStatus.Active;
        ActivationDateUtc ??= DateTime.UtcNow;
    }

    public void Suspend(string msisdn)
    {
        OperationalStatus = SubscriberOperationalStatus.Suspended;
        AddDomainEvent(new SubscriberSuspendedEvent(Id, msisdn, CustomerId, OperationalStatus));
    }

    public void SuspendInbound(string msisdn)
    {
        OperationalStatus = SubscriberOperationalStatus.SuspendedInbound;
        AddDomainEvent(new SubscriberSuspendedEvent(Id, msisdn, CustomerId, OperationalStatus));
    }

    public void SuspendOutbound(string msisdn)
    {
        OperationalStatus = SubscriberOperationalStatus.SuspendedOutbound;
        AddDomainEvent(new SubscriberSuspendedEvent(Id, msisdn, CustomerId, OperationalStatus));
    }

    public void Deactivate() => OperationalStatus = SubscriberOperationalStatus.Deactivated;

    public void Terminate() => OperationalStatus = SubscriberOperationalStatus.Terminated;
}
