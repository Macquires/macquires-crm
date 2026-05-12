using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>Unified telecom operation (Activation, MGR, TKO, SIM swap, …).</summary>
public class TelecomOperationRequest : BaseEntity
{
    public TelecomOperationKind Kind { get; set; }

    public string Number { get; set; } = null!;

    public TelecomOperationStatus Status { get; set; } = TelecomOperationStatus.Draft;

    public TelecomDocumentStatus DocumentStatus { get; set; } = TelecomDocumentStatus.Missing;

    public string SubscriberProfileId { get; set; } = null!;
    public SubscriberProfile? SubscriberProfile { get; set; }

    /// <summary>Take-over target or second party.</summary>
    public string? SecondarySubscriberProfileId { get; set; }
    public SubscriberProfile? SecondarySubscriberProfile { get; set; }

    public string? MsisdnAssetId { get; set; }
    public MsisdnAsset? MsisdnAsset { get; set; }

    public string? ProductId { get; set; }
    public Product? Product { get; set; }

    public string? Notes { get; set; }

    /// <summary>Target plan / offer label for migrations (e.g. «سيريتل ميكس») — demo field.</summary>
    public string? TargetOfferName { get; set; }

    public DateTime? ConfirmedAtUtc { get; set; }
}
