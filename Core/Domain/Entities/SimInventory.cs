using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>مخزون الشرائح (ICCID) — منفصل عن تجمع الأرقام MSISDN.</summary>
public class SimInventory : BaseEntity, IHasBranchId
{
    public string? BranchId { get; set; }
    public string Iccid { get; private set; } = null!;
    public SimType SimType { get; private set; } = SimType.Physical;
    public bool IsESim => SimType == SimType.ESim;
    public string? Eid { get; private set; }
    public string? ActivationCode { get; private set; }
    public string? Imsi { get; private set; }
    public string? Pin1 { get; private set; }
    public string? Puk1 { get; private set; }
    public string? Pin2 { get; private set; }
    public string? Puk2 { get; private set; }
    public SimStatus Status { get; private set; } = SimStatus.Available;
    public DateTime? QuarantineEndsUtc { get; private set; }
    public byte[]? RowVersion { get; set; }

    public string? SubscriberProfileId { get; private set; }
    public SubscriberProfile? SubscriberProfile { get; private set; }

    private static readonly Dictionary<SimStatus, SimStatus[]> AllowedTransitions = new()
    {
        [SimStatus.Available] = [SimStatus.Reserved, SimStatus.Active],
        [SimStatus.Reserved] = [SimStatus.Active, SimStatus.Available],
        [SimStatus.Active] = [SimStatus.Suspended, SimStatus.Quarantined, SimStatus.Burned],
        [SimStatus.Suspended] = [SimStatus.Active, SimStatus.Quarantined, SimStatus.Burned],
        [SimStatus.Quarantined] = [SimStatus.Available],
    };

    public static SimInventory Create(
        string iccid,
        string? imsi = null,
        string? pin1 = null,
        string? puk1 = null,
        string? pin2 = null,
        string? puk2 = null,
        SimType simType = SimType.Physical,
        string? eid = null,
        string? activationCode = null)
    {
        return new SimInventory
        {
            Iccid = iccid,
            Imsi = imsi,
            Pin1 = pin1,
            Puk1 = puk1,
            Pin2 = pin2,
            Puk2 = puk2,
            SimType = simType,
            Eid = eid,
            ActivationCode = activationCode
        };
    }

    public void TransitionTo(SimStatus newStatus, int? quarantineDays = null)
    {
        if (Status == newStatus) return;

        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new InvalidOperationException(
                $"انتقال غير مسموح لحالة الشريحة: {Status} → {newStatus}.");
        }

        if (newStatus == SimStatus.Quarantined)
        {
            QuarantineEndsUtc = DateTime.UtcNow.AddDays(quarantineDays ?? 90);
        }

        Status = newStatus;
    }

    /// <summary>Permanent death — used by MSISDN recycling (Case C).</summary>
    public void MarkBurned(DateTime utcNow)
    {
        if (Status == SimStatus.Burned)
        {
            return;
        }

        QuarantineEndsUtc = null;
        Status = SimStatus.Burned;
    }

    public void AssignToProfile(string? subscriberProfileId) => SubscriberProfileId = subscriberProfileId;

    public void ReleaseQuarantineIfExpired(DateTime utcNow)
    {
        if (Status != SimStatus.Quarantined || !QuarantineEndsUtc.HasValue || QuarantineEndsUtc > utcNow)
        {
            return;
        }

        QuarantineEndsUtc = null;
        TransitionTo(SimStatus.Available);
    }

    /// <summary>Oracle/demo ingest — returns SIM to pool using only valid lifecycle transitions.</summary>
    public void ResetToAvailableForInventoryIngest()
    {
        AssignToProfile(null);

        if (Status == SimStatus.Burned)
        {
            return;
        }

        if (Status == SimStatus.Available)
        {
            QuarantineEndsUtc = null;
            return;
        }

        var utcNow = DateTime.UtcNow;

        if (Status == SimStatus.Reserved)
        {
            TransitionTo(SimStatus.Available);
            QuarantineEndsUtc = null;
            return;
        }

        if (Status == SimStatus.Quarantined)
        {
            QuarantineEndsUtc = utcNow.AddSeconds(-1);
            ReleaseQuarantineIfExpired(utcNow);
            return;
        }

        // Active or Suspended → Quarantined (expired) → Available
        TransitionTo(SimStatus.Quarantined);
        QuarantineEndsUtc = utcNow.AddSeconds(-1);
        ReleaseQuarantineIfExpired(utcNow);
    }

    /// <summary>Rolls back a failed activation bind so the SIM can re-enter the warehouse.</summary>
    public void RollbackFailedActivationBinding(DateTime utcNow)
    {
        AssignToProfile(null);

        if (Status == SimStatus.Active)
        {
            TransitionTo(SimStatus.Quarantined);
            QuarantineEndsUtc = utcNow.AddSeconds(-1);
            ReleaseQuarantineIfExpired(utcNow);
            return;
        }

        if (Status == SimStatus.Reserved)
        {
            TransitionTo(SimStatus.Available);
        }
    }

    internal void SetIccidForImport(string iccid) => Iccid = iccid;
    internal void SetImsiForImport(string? imsi) => Imsi = imsi;
    internal void SetPinsForImport(string? pin1, string? puk1, string? pin2, string? puk2)
    {
        Pin1 = pin1;
        Puk1 = puk1;
        Pin2 = pin2;
        Puk2 = puk2;
    }

    public void SetActivationCodeFromDpPlus(string activationCode) => ActivationCode = activationCode;
}
