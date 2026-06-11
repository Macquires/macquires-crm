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

    public static bool IsWellKnown(string msisdn) =>
        msisdn == Hero
        || msisdn == DebtSubscriber
        || msisdn == ReconnectFraudDemo
        || msisdn == ShowcaseHealthy
        || msisdn == ShowcaseNotProvisioned
        || msisdn == ShowcaseOperationalSuspended;
}
