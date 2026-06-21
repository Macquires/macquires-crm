namespace Application.Common.Telecom;

/// <summary>Canonical demo MSISDNs — single source aligned with Oracle Fusion inventory catalog.</summary>
public static class TelecomDemoMsisdn
{
    /// <summary>Customer 360 tribulation showcase — all demo lines live under this party.</summary>
    public const string ShowcaseCustomerName = "سعدون الشامي";

    /// <summary>Healthy baseline — CRM Active + HLR ACTIVE (last digit 0).</summary>
    public const string ShowcaseHealthy = "0939000000";

    public const string Hero = "0939000001";
    public const string DebtSubscriber = "0939000002";

    /// <summary>Active line — HLR NOT_PROVISIONED (last digit 3) → reprovision demo.</summary>
    public const string ShowcaseNotProvisioned = "0939000003";

    /// <summary>Suspended Operational — HLR SUSPENDED aligned (last digit 5) → RCN Operational VAL-09-04.</summary>
    public const string ShowcaseOperationalSuspended = "0939000005";

    /// <summary>Secondary hero line — suspended (Fraud) for RCN §9 demo.</summary>
    public const string ReconnectFraudDemo = "0939000091";

    /// <summary>EF-translatable list for branch RLS bypass on national showcase lines.</summary>
    public static readonly string[] WellKnownNationalMsisdns =
    [
        ShowcaseHealthy,
        Hero,
        DebtSubscriber,
        ShowcaseNotProvisioned,
        ShowcaseOperationalSuspended,
        ReconnectFraudDemo,
    ];

    public const string DebtShowcaseCustomerName = "مازن المديون";

    public static bool IsWellKnown(string msisdn) =>
        WellKnownNationalMsisdns.Contains(msisdn);

    /// <summary>
    /// Demo parties/lines used in national call-center and wizard scenarios — must stay visible across branch RLS.
    /// </summary>
    public static bool IsNationalDemoAnchor(string? primaryPhone, string? displayName)
    {
        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(primaryPhone ?? "");
        if (msisdn != null && IsWellKnown(msisdn))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return false;
        }

        return displayName.Contains(ShowcaseCustomerName, StringComparison.Ordinal)
            || displayName.Contains(DebtShowcaseCustomerName, StringComparison.Ordinal);
    }

    /// <summary>Canonical technical tickets that must stay Open for back-office demo walkthrough.</summary>
    public static readonly string[] PreservedDemoTicketNotes =
    [
        "شحن كاش والنت واقف",
        "ضعف تغطية - قدسيا",
        "طلب تبديل شريحة — معلّق",
        "ترحيل باقة MGR — معلّق",
        "تفعيل خط معلّق",
        "إعادة تهيئة HLR — NOT_PROVISIONED",
    ];

    public static bool IsPreservedBackOfficeDemoTicket(string? msisdn, string? notes)
    {
        var canonical = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(msisdn ?? "");
        if (canonical != null && IsWellKnown(canonical))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(notes)
               && PreservedDemoTicketNotes.Contains(notes.Trim(), StringComparer.Ordinal);
    }
}
