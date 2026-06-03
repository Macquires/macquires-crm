namespace Application.Common.Telecom.SellingLine;

/// <summary>Blueprint page 5 — capability ownership for Selling Line &amp; New Activation.</summary>
public static class SellingLineCapabilityOwnership
{
    public const string CapabilityDomain = "Retail & Customer Lifecycle";

    public const string CapabilityNameAr = "بيع خط / تفعيل جديد";

    public static readonly IReadOnlyList<CapabilityOwner> Owners =
    [
        new("Branch CSR", "TelecomShowroom", "telecom.line.activate", "Initiation, validation, fulfillment"),
        new("Dealer Agent", "TelecomShowroom", "telecom.line.activate", "Dealer channel (ActivationChannel.Dealer)"),
        new("Activation Officer", "TelecomBackOffice", "telecom.line.activate", "Override / high-risk approval"),
        new("Billing System", "Integration", null, "CBS account / deposit (IBillingSystemIntegration)"),
        new("Provisioning System", "Integration", null, "HLR create subscriber (INetworkProvisioningService)"),
    ];
}

public sealed record CapabilityOwner(
    string ActorLabel,
    string RoleOrSystem,
    string? PermissionKey,
    string Responsibility);
