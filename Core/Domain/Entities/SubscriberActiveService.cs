using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>Per-line VAS activation state (keyed by <see cref="TelecomSubscriptionId"/> / MSISDN).</summary>
public class SubscriberActiveService : BaseEntity
{
    public string TelecomSubscriptionId { get; set; } = null!;
    public TelecomSubscription? TelecomSubscription { get; set; }

    public string TelecomValueAddedServiceId { get; set; } = null!;
    public TelecomValueAddedService? TelecomValueAddedService { get; set; }

    /// <summary>Denormalized for fast MSISDN lookups and audit payloads.</summary>
    public string Msisdn { get; set; } = null!;

    public SubscriberVasStatus Status { get; set; } = SubscriberVasStatus.Active;
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? DeactivatedAtUtc { get; set; }
}
