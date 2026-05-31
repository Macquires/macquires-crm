using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>مخزون الشرائح (ICCID) — منفصل عن تجمع الأرقام MSISDN.</summary>
public class SimInventory : BaseEntity
{
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
        [SimStatus.Active] = [SimStatus.Suspended, SimStatus.Quarantined],
        [SimStatus.Suspended] = [SimStatus.Active, SimStatus.Quarantined],
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

    public void TransitionTo(SimStatus newStatus)
    {
        if (Status == newStatus) return;

        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new InvalidOperationException(
                $"انتقال غير مسموح لحالة الشريحة: {Status} → {newStatus}.");
        }

        if (newStatus == SimStatus.Quarantined && QuarantineEndsUtc == null)
        {
            QuarantineEndsUtc = DateTime.UtcNow.AddDays(90);
        }

        Status = newStatus;
    }

    public void AssignToProfile(string? subscriberProfileId) => SubscriberProfileId = subscriberProfileId;

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
