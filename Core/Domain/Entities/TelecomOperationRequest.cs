using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>Unified telecom operation (Activation, MGR, TKO, SIM swap, …).</summary>
public class TelecomOperationRequest : BaseEntity
{
    public TelecomOperationKind Kind { get; set; }

    public string Number { get; set; } = null!;

    /// <summary>End-to-end correlation for provisioning and audit (GUID v7).</summary>
    public string? CorrelationId { get; set; }

    public TelecomOperationStatus Status { get; set; } = TelecomOperationStatus.Draft;

    public TelecomDocumentStatus DocumentStatus { get; set; } = TelecomDocumentStatus.Missing;

    public string SubscriberProfileId { get; set; } = null!;
    public SubscriberProfile? SubscriberProfile { get; set; }

    /// <summary>Take-over target or second party.</summary>
    public string? SecondarySubscriberProfileId { get; set; }
    public SubscriberProfile? SecondarySubscriberProfile { get; set; }

    public string? MsisdnAssetId { get; set; }
    public MsisdnAsset? MsisdnAsset { get; set; }

    public string? SimInventoryId { get; set; }
    public SimInventory? SimInventory { get; set; }

    public string? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Commercial catalog selection for migrate/activate (optional; <see cref="ProductId"/> is resolved from it).</summary>
    public string? ProductOfferingId { get; set; }
    public ProductOffering? ProductOffering { get; set; }

    /// <summary>§11 MGR rollback — technical product before package change.</summary>
    public string? PriorProductId { get; set; }

    /// <summary>§11 MGR rollback — commercial offering before package change.</summary>
    public string? PriorProductOfferingId { get; set; }

    public string? Notes { get; set; }

    /// <summary>Target plan / offer label for migrations (e.g. «سيريتل ميكس») — demo field.</summary>
    public string? TargetOfferName { get; set; }

    public DateTime? ConfirmedAtUtc { get; set; }

    /// <summary>Stored identity scan for legal review (relative path under telecom-ops upload root).</summary>
    public string? IdentityDocumentStorageKey { get; set; }

    public ActivationChannel ActivationChannel { get; set; } = ActivationChannel.Showroom;

    public string? DealerCode { get; set; }

    public string? BranchId { get; set; }

    public string? PaymentReference { get; set; }

    public decimal? InitialDepositAmount { get; set; }

    public string? OverrideReasonCode { get; set; }

    public DateTime? KycVerifiedAtUtc { get; set; }

    /// <summary>§6 Change GSM — current subscription type at initiation.</summary>
    public string? SourceSubscriptionTypeId { get; set; }

    /// <summary>§6 Change GSM — target subscription type.</summary>
    public string? TargetSubscriptionTypeId { get; set; }

    public string? GsmMigrationReason { get; set; }

    public DateTime? GsmEffectiveDateUtc { get; set; }

    /// <summary>Compatibility outcome label (e.g. Allowed, Blocked).</summary>
    public string? GsmCompatibilityStatus { get; set; }

    /// <summary>§7 Take-over — legal reason (sale, inheritance, court order, …).</summary>
    public string? TransferReason { get; set; }

    /// <summary>§7 B.5 obligation snapshot label (e.g. Unknown, Clear, PendingSettlement).</summary>
    public string? TakeOverObligationStatus { get; set; }

    public DepositTransferPolicy? DepositTransferPolicy { get; set; }

    /// <summary>Required approver tier (Showroom drafts only; BackOffice confirms).</summary>
    public string? ApprovalLevelRequired { get; set; }

    public DateTime? TakeOverEffectiveDateUtc { get; set; }

    public string? OldCustomerId { get; set; }

    public string? NewCustomerId { get; set; }

    /// <summary>Prior owner profile id captured before CBS/HLR swap (rollback anchor).</summary>
    public string? PriorSubscriberProfileId { get; set; }

    /// <summary>§4 SIM Swap — reason (damaged, upgrade, lost/stolen report, …).</summary>
    public string? ReplacementReason { get; set; }

    /// <summary>§4 Lost/Stolen path — requires BackOffice approval (no showroom confirm).</summary>
    public bool IsLostOrStolenReport { get; set; }

    /// <summary>Active SIM id captured at initiation (quarantine + HLR rollback anchor).</summary>
    public string? PriorSimInventoryId { get; set; }

    /// <summary>§5 Change Number — current MSISDN asset before swap (rollback anchor).</summary>
    public string? PriorMsisdnAssetId { get; set; }

    /// <summary>§5 Change Number — new MSISDN from pool.</summary>
    public string? TargetMsisdnAssetId { get; set; }

    /// <summary>§5 Change Number — business reason.</summary>
    public string? NumberChangeReason { get; set; }

    /// <summary>§5 Premium number fee (VAL-05-02).</summary>
    public decimal? PremiumFeeAmount { get; set; }

    /// <summary>§5 Internal vs Port-In (demo: Internal).</summary>
    public string? NumberChangeMode { get; set; }

    /// <summary>§10 Termination — Voluntary, Collections, Regulatory, Fraud.</summary>
    public string? TerminationType { get; set; }

    /// <summary>§10 Termination — business reason.</summary>
    public string? TerminationReason { get; set; }

    public DateTime? TerminationEffectiveDateUtc { get; set; }

    /// <summary>§10 VAL-10-02 — final bill snapshot from CBS.</summary>
    public decimal? FinalBillAmount { get; set; }

    public decimal? DepositSettlementAmount { get; set; }

    public string? DepositSettlementStatus { get; set; }

    /// <summary>§10 VAL-10-05 — retention offer logged before voluntary completion.</summary>
    public string? RetentionOfferOutcome { get; set; }

    public string? DeprovisionStatus { get; set; }

    /// <summary>§8 Suspension — CustomerRequest, Billing, Fraud, Regulatory, Operational.</summary>
    public string? SuspensionType { get; set; }

    public string? SuspensionReason { get; set; }

    public DateTime? SuspensionStartDateUtc { get; set; }

    public DateTime? SuspensionEndDateUtc { get; set; }

    /// <summary>§8 — Full, InboundOnly, OutboundOnly.</summary>
    public string? BarringLevel { get; set; }

    public bool AutoReconnectEnabled { get; set; }

    public bool NotificationSuppressed { get; set; }

    public string? BarStatus { get; set; }

    /// <summary>Snapshot for HLR rollback (SubscriberOperationalStatus name).</summary>
    public string? PriorOperationalStatus { get; set; }

    /// <summary>§9 Reconnect — reason narrative.</summary>
    public string? ReconnectReason { get; set; }

    /// <summary>§9 — Payment, Fraud, Regulatory, Customer, Operational.</summary>
    public string? ClearanceType { get; set; }

    /// <summary>§9 — link to completed SUS operation.</summary>
    public string? SourceSuspensionOperationId { get; set; }

    public bool FraudClearanceConfirmed { get; set; }

    public string? FraudClearanceByUserId { get; set; }

    public DateTime? ReactivationAtUtc { get; set; }

    public string? ProvisioningResult { get; set; }

    /// <summary>§14 Device sale — IMEI inventory row.</summary>
    public string? DeviceInventoryId { get; set; }
    public DeviceInventory? DeviceInventory { get; set; }

    public DeviceSaleType? DeviceSaleType { get; set; }

    public string? InstallmentPlanId { get; set; }
    public InstallmentPlan? InstallmentPlan { get; set; }

    public decimal? DeviceDownPaymentAmount { get; set; }

    public decimal? DeviceMonthlyInstallmentAmount { get; set; }

    public int? DeviceCreditScoreSnapshot { get; set; }

    public string? DeviceInstallmentContractId { get; set; }
    public DeviceInstallmentContract? DeviceInstallmentContract { get; set; }

    public DeviceFinancingDecision? DeviceFinancingDecision { get; set; }

    public string? DeviceOverrideReasonCode { get; set; }

    public string? DeviceApprovalLevelRequired { get; set; }

    public string? DeviceFinancingNoteAr { get; set; }

    public DateTime? DeviceWarrantyStartsAtUtc { get; set; }

    /// <summary>§15 RFD — Deposit, WalletBalance, Overpayment, SyriatelCash.</summary>
    public string? RefundType { get; set; }

    public string? RefundReason { get; set; }

    public decimal? RefundAmount { get; set; }

    /// <summary>§15 — Cash, BankTransfer, WalletCredit, CreditNote.</summary>
    public string? RefundMethod { get; set; }

    public decimal? DepositBalanceSnapshot { get; set; }

    public decimal? WalletBalanceSnapshot { get; set; }

    public string? RefundSettlementStatus { get; set; }

    public string? RefundCbsReference { get; set; }

    public string? RefundGatewayReference { get; set; }

    public bool RequiresDualApproval { get; set; }

    public ICollection<TelecomOperationAuditLog> AuditLogs { get; set; } = new List<TelecomOperationAuditLog>();
}
