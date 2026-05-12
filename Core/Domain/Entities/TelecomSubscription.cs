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

    public TelecomSubscriptionType SubscriptionType { get; set; } = TelecomSubscriptionType.Prepaid;

    public TelecomDocumentStatus DocumentStatus { get; set; } = TelecomDocumentStatus.Missing;

    public bool IsPrimaryLine { get; set; } = true;
}
