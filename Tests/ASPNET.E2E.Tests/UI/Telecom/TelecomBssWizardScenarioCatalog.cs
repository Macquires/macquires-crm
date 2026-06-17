namespace ASPNET.E2E.Tests.UI.Telecom;

/// <summary>Canonical BSS wizard coverage map — Hub tiles, C360 deep-links, List line actions.</summary>
public static class TelecomBssWizardScenarioCatalog
{
    public sealed record WizardScenario(
        string Code,
        string HubTile,
        string C360WizardKind,
        string HubAnchorTestId,
        string C360AnchorTestId,
        string? ListLineAction,
        string? ListAnchorTestId,
        string? ListModalId);

    public static IReadOnlyList<WizardScenario> All { get; } =
    [
        new("ACT", "activate", "activate", "hub-act-line-type", "c360-act-line-type", null, "list-act-effective-mode", "#C360NewLineModal"),
        new("MGR", "migrate", "migrate", "hub-mgr-offering-select", "c360-mgr-offering-select", "Migrate", "list-mgr-offering-select", "#C360MigrateModal"),
        new("CGT", "changeGsm", "changeGsm", "hub-cgt-wizard", "c360-cgt-wizard", "Change line type", "list-cgt-wizard", "#C360ChangeGsmModal"),
        new("TKO", "takeover", "takeover", "hub-tko-wizard", "c360-tko-wizard", "Transfer", "list-tko-wizard", "#C360TakeOverModal"),
        new("SIM", "simswap", "simswap", "hub-sim-replacement-reason", "c360-sim-replacement-reason", "SIM swap", "list-sim-replacement-reason", "#C360SimSwapModal"),
        new("CNR", "changeNumber", "changeNumber", "telecom-cn-change-mode", "c360-cn-change-mode", "Change number", "list-cn-wizard", "#C360ChangeNumberModal"),
        new("TRM", "termination", "termination", "hub-trm-termination-type", "c360-trm-termination-type", "Terminate", "list-trm-termination-type", "#C360TerminationModal"),
        new("SUS", "suspension", "suspension", "hub-sus-suspension-type", "c360-sus-suspension-type", "Suspend", "list-sus-wizard", "#C360SuspensionModal"),
        new("RCN", "reconnect", "reconnect", "hub-rcn-clearance-type", "c360-rcn-clearance-type", "Reconnect", "list-rcn-wizard", "#C360ReconnectModal"),
        new("RFD", "refund", "refund", "hub-effective-mode", "c360-effective-date-block", "Refund", "list-rfd-wizard", "#C360RefundModal"),
        new("BDR", "badDebt", "badDebt", "hub-bdr-collection-action", "c360-bdr-collection-action", "Collection", "list-bdr-wizard", "#C360BadDebtModal"),
        new("DEV", "deviceSale", "deviceSale", "hub-dev-wizard", "c360-dev-wizard", "Device sale", "list-dev-wizard", "#C360DeviceSaleModal"),
        new("VAS", "addpackage", "addpackage", "hub-vas-wizard", "c360-vas-wizard", "VAS", "list-vas-wizard", "#C360VasModal"),
        new("SUP", "support", "support", "hub-support-issue-type", "c360-support-wizard", null, "list-support-issue-type", "#C360SupportTicketModal"),
    ];

    public static readonly string[] TimelineFilterLabels =
    [
        "Operations",
        "Payments",
        "Tickets",
        "Billing",
        "Audit",
    ];

    public static readonly (string Label, string Msisdn)[] DemoLines =
    [
        ("Healthy", Application.Common.Telecom.TelecomDemoMsisdn.ShowcaseHealthy),
        ("NotProvisioned", Application.Common.Telecom.TelecomDemoMsisdn.ShowcaseNotProvisioned),
        ("OperationalSuspended", Application.Common.Telecom.TelecomDemoMsisdn.ShowcaseOperationalSuspended),
        ("Debt", Application.Common.Telecom.TelecomDemoMsisdn.DebtSubscriber),
        ("FraudReconnect", Application.Common.Telecom.TelecomDemoMsisdn.ReconnectFraudDemo),
    ];
}
