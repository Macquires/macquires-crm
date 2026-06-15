using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>تجمع أرقام MSISDN ودورة حياتها (منفصل عن مخزون الشرائح).</summary>
public class MsisdnAsset : BaseEntity, IHasBranchId
{
    public string? BranchId { get; set; }
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

    /// <summary>Provisioning line type stamped at pool ingest (IN vs CBS routing) — required before sale/reservation.</summary>
    public string? IntendedSubscriptionTypeId { get; set; }
    public TelecomSubscriptionTypeLookup? IntendedSubscriptionTypeLookup { get; set; }

    private static readonly Dictionary<MsisdnPoolStatus, MsisdnPoolStatus[]> AllowedTransitions = new()
    {
        [MsisdnPoolStatus.Available] = [MsisdnPoolStatus.Reserved, MsisdnPoolStatus.Active],
        [MsisdnPoolStatus.Reserved] = [MsisdnPoolStatus.Active, MsisdnPoolStatus.Available],
        [MsisdnPoolStatus.Active] = [MsisdnPoolStatus.Suspended, MsisdnPoolStatus.Quarantined],
        [MsisdnPoolStatus.Suspended] = [MsisdnPoolStatus.Active, MsisdnPoolStatus.Quarantined],
        [MsisdnPoolStatus.Quarantined] = [MsisdnPoolStatus.Available],
    };

    public void TransitionTo(MsisdnPoolStatus newStatus, int? quarantineDays = null)
    {
        if (PoolStatus == newStatus) return;

        if (!AllowedTransitions.TryGetValue(PoolStatus, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new InvalidOperationException(
                $"انتقال غير مسموح لحالة الرقم: {PoolStatus} → {newStatus}. " +
                $"الانتقالات المتاحة: {string.Join(", ", allowed ?? [])}");
        }

        if (newStatus == MsisdnPoolStatus.Quarantined)
        {
            QuarantineEndsUtc = DateTime.UtcNow.AddDays(quarantineDays ?? 90);
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

    public void ReleaseReservation()
    {
        if (PoolStatus == MsisdnPoolStatus.Reserved)
        {
            TransitionTo(MsisdnPoolStatus.Available);
        }
    }

    public void ReleaseQuarantineIfExpired(DateTime utcNow)
    {
        if (PoolStatus != MsisdnPoolStatus.Quarantined || !QuarantineEndsUtc.HasValue || QuarantineEndsUtc > utcNow)
        {
            return;
        }

        QuarantineEndsUtc = null;
        TransitionTo(MsisdnPoolStatus.Available);
    }

    /// <summary>Oracle/demo ingest — returns asset to pool using only valid lifecycle transitions.</summary>
    public void ResetToAvailableForInventoryIngest()
    {
        SubscriberProfileId = null;
        PairedIccid = null;
        PairedImsi = null;
        ReservedForCustomerId = null;
        ReservedUntilUtc = null;

        if (PoolStatus == MsisdnPoolStatus.Available)
        {
            QuarantineEndsUtc = null;
            return;
        }

        var utcNow = DateTime.UtcNow;

        if (PoolStatus == MsisdnPoolStatus.Reserved)
        {
            TransitionTo(MsisdnPoolStatus.Available);
            QuarantineEndsUtc = null;
            return;
        }

        if (PoolStatus == MsisdnPoolStatus.Quarantined)
        {
            QuarantineEndsUtc = utcNow.AddSeconds(-1);
            ReleaseQuarantineIfExpired(utcNow);
            return;
        }

        // Active or Suspended → Quarantined (expired) → Available
        TransitionTo(MsisdnPoolStatus.Quarantined);
        QuarantineEndsUtc = utcNow.AddSeconds(-1);
        ReleaseQuarantineIfExpired(utcNow);
    }

    /// <summary>Rolls back a failed activation bind so the number can re-enter the pool.</summary>
    public void RollbackFailedActivationBinding(DateTime utcNow)
    {
        SubscriberProfileId = null;

        if (PoolStatus == MsisdnPoolStatus.Active)
        {
            TransitionTo(MsisdnPoolStatus.Quarantined);
            QuarantineEndsUtc = utcNow.AddSeconds(-1);
            ReleaseQuarantineIfExpired(utcNow);
            return;
        }

        if (PoolStatus == MsisdnPoolStatus.Reserved)
        {
            ReleaseReservation();
        }
    }
}
