using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>تجمع أرقام MSISDN ودورة حياتها (منفصل عن مخزون الشرائح).</summary>
public class MsisdnAsset : BaseEntity
{
    public string Msisdn { get; set; } = null!;
    /// <summary>شريحة مُدخلة مع الرقم (استيراد/مستودع) قبل ربط ملف المشترك.</summary>
    public string? PairedIccid { get; set; }
    public string? PairedImsi { get; set; }
    public string? CountryCode { get; set; }
    public string? Prefix { get; set; }
    public MsisdnCategory Category { get; set; } = MsisdnCategory.Normal;
    public MsisdnPoolStatus PoolStatus { get; set; } = MsisdnPoolStatus.Available;
    public DateTime? ReservedUntilUtc { get; set; }
    public string? ReservedForCustomerId { get; set; }
    public DateTime? QuarantineEndsUtc { get; set; }
    public byte[]? RowVersion { get; set; }

    public string? SubscriberProfileId { get; set; }
    public SubscriberProfile? SubscriberProfile { get; set; }

    public string? ProductId { get; set; }
    public Product? Product { get; set; }

    private static readonly Dictionary<MsisdnPoolStatus, MsisdnPoolStatus[]> AllowedTransitions = new()
    {
        [MsisdnPoolStatus.Available] = [MsisdnPoolStatus.Reserved, MsisdnPoolStatus.Active],
        [MsisdnPoolStatus.Reserved] = [MsisdnPoolStatus.Active, MsisdnPoolStatus.Available],
        [MsisdnPoolStatus.Active] = [MsisdnPoolStatus.Suspended, MsisdnPoolStatus.Quarantined],
        [MsisdnPoolStatus.Suspended] = [MsisdnPoolStatus.Active, MsisdnPoolStatus.Quarantined],
        [MsisdnPoolStatus.Quarantined] = [MsisdnPoolStatus.Available],
    };

    public void TransitionTo(MsisdnPoolStatus newStatus)
    {
        if (PoolStatus == newStatus) return;

        if (!AllowedTransitions.TryGetValue(PoolStatus, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new InvalidOperationException(
                $"انتقال غير مسموح لحالة الرقم: {PoolStatus} → {newStatus}. " +
                $"الانتقالات المتاحة: {string.Join(", ", allowed ?? [])}");
        }

        if (newStatus == MsisdnPoolStatus.Quarantined && QuarantineEndsUtc == null)
        {
            QuarantineEndsUtc = DateTime.UtcNow.AddDays(90);
        }

        if (newStatus is MsisdnPoolStatus.Available or MsisdnPoolStatus.Active)
        {
            ReservedUntilUtc = null;
            ReservedForCustomerId = null;
        }

        PoolStatus = newStatus;
    }

    public void ReserveForCustomer(string customerId, DateTime utcNow, TimeSpan? duration = null)
    {
        TransitionTo(MsisdnPoolStatus.Reserved);
        ReservedForCustomerId = customerId;
        ReservedUntilUtc = utcNow.Add(duration ?? TimeSpan.FromHours(24));
    }

    public void ReleaseReservationIfExpired(DateTime utcNow)
    {
        if (PoolStatus != MsisdnPoolStatus.Reserved || !ReservedUntilUtc.HasValue || ReservedUntilUtc > utcNow)
        {
            return;
        }

        TransitionTo(MsisdnPoolStatus.Available);
    }
}
