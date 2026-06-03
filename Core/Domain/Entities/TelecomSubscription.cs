using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class TelecomSubscription : BaseEntity
{
    public string SubscriberProfileId { get; set; } = null!;
    public SubscriberProfile? SubscriberProfile { get; set; }

    public string MsisdnAssetId { get; set; } = null!;
    public MsisdnAsset? MsisdnAsset { get; set; }

    public string? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Commercial TM Forum offering bound to this line (catalog price, components, SOC).</summary>
    public string? ProductOfferingId { get; set; }
    public ProductOffering? ProductOffering { get; set; }

    public string SubscriptionTypeId { get; set; } = TelecomSubscriptionTypeWellKnownIds.Prepaid;

    public TelecomSubscriptionTypeLookup? SubscriptionTypeLookup { get; set; }

    public TelecomDocumentStatus DocumentStatus { get; set; } = TelecomDocumentStatus.Missing;

    public bool IsPrimaryLine { get; set; } = true;
}
