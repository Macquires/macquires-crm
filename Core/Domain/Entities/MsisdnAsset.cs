using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>SIM / MSISDN inventory and assignment (pool + lifecycle).</summary>
public class MsisdnAsset : BaseEntity
{
    public string Msisdn { get; set; } = null!;
    public string? Iccid { get; set; }
    public string? Imsi { get; set; }
    public string? Puk1 { get; set; }
    public string? Puk2 { get; set; }

    public MsisdnPoolStatus PoolStatus { get; set; } = MsisdnPoolStatus.Available;

    public DateTime? QuarantineEndsUtc { get; set; }

    public string? SubscriberProfileId { get; set; }
    public SubscriberProfile? SubscriberProfile { get; set; }

    public string? ProductId { get; set; }
    public Product? Product { get; set; }
}
