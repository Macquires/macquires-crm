using Domain.Common;

namespace Domain.Entities;

/// <summary>Audit trail for primary-line MSISDN / subscription-type changes (BSS support & compliance).</summary>
public class TelecomMsisdnChangeLog : BaseEntity
{
    public string CustomerId { get; set; } = null!;
    public Customer? Customer { get; set; }

    public string SubscriberProfileId { get; set; } = null!;
    public string TelecomSubscriptionId { get; set; } = null!;
    public string MsisdnAssetId { get; set; } = null!;

    public string? OldMsisdn { get; set; }
    public string? NewMsisdn { get; set; }
    public string? OldSubscriptionType { get; set; }
    public string? NewSubscriptionType { get; set; }

    public bool? ExternalSyncSuccess { get; set; }
    public string? ExternalSyncMessage { get; set; }
}
