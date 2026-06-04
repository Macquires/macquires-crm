using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Settings;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.ChangeGsm;
using Application.Common.Telecom.SellingLine;
using Application.Common.Telecom.ChangeNumber;
using Application.Common.Telecom.Termination;
using Application.Common.Telecom.OfferSubscription;
using Application.Common.Telecom.Suspension;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.SimSwap;
using Application.Common.Telecom.TakeOver;
using Application.Common.Telecom.DeviceSales;
using Application.Common.Telecom.Refund;
using Application.Common.Telecom.BadDebt;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public static class TelecomNumberSequence
{
    public static (string EntityName, string Prefix) ForKind(TelecomOperationKind kind) => kind switch
    {
        TelecomOperationKind.Migration => ("TelecomOp_Migration", "MGR-"),
        TelecomOperationKind.TakeOver => ("TelecomOp_TakeOver", "TKO-"),
        TelecomOperationKind.NewActivation => ("TelecomOp_Activation", "ACT-"),
        TelecomOperationKind.SimSwap => ("TelecomOp_SimSwap", "SIM-"),
        TelecomOperationKind.ServiceModification => ("TelecomOp_Vas", "VAS-"),
        TelecomOperationKind.NumberPortability => ("TelecomOp_ChangeNumber", "CNR-"),
        TelecomOperationKind.ChangeGsmType => ("TelecomOp_ChangeGsm", "CGT-"),
        TelecomOperationKind.Termination => ("TelecomOp_Termination", "TRM-"),
        TelecomOperationKind.TemporarySuspension => ("TelecomOp_Suspension", "SUS-"),
        TelecomOperationKind.Reconnect => ("TelecomOp_Reconnect", "RCN-"),
        TelecomOperationKind.DeviceSale => ("TelecomOp_DeviceSale", "DEV-"),
        TelecomOperationKind.DepositRefundSettlement => ("TelecomOp_Refund", "RFD-"),
        TelecomOperationKind.BadDebtRecovery => ("TelecomOp_BadDebt", "BDR-"),
        _ => ("TelecomOp_Generic", "TEL-")
    };
}

public class CreateTelecomOperationRequestResult
{
    public TelecomOperationRequest? Data { get; set; }
}
public class CreateTelecomOperationRequest : IRequest<CreateTelecomOperationRequestResult>
{
    public TelecomOperationKind Kind { get; init; }
    public string SubscriberProfileId { get; init; } = null!;
    public string? SecondarySubscriberProfileId { get; init; }
    public string? MsisdnAssetId { get; init; }

    /// <summary>Commercial catalog selection (preferred). Resolved to technical <see cref="Product"/> id via the linked offering.</summary>
    public string? ProductOfferingId { get; init; }

    /// <summary>Legacy technical product id (optional when <see cref="ProductOfferingId"/> is set).</summary>
    public string? ProductId { get; init; }
    public string? Notes { get; init; }
    public string? TargetOfferName { get; init; }
    public string? SimInventoryId { get; init; }

    /// <summary>Resolves <see cref="SimInventoryId"/> when set (available SIM in pool).</summary>
    public string? SimIccid { get; init; }
    public string? CreatedById { get; init; }

    public ActivationChannel? ActivationChannel { get; init; }

    public string? DealerCode { get; init; }

    public string? BranchId { get; init; }

    /// <summary>§6 Change GSM — target subscription type lookup id.</summary>
    public string? TargetSubscriptionTypeId { get; init; }

    public string? GsmMigrationReason { get; init; }

    public DateTime? GsmEffectiveDateUtc { get; init; }

    /// <summary>Optional catalog offering after type change.</summary>
    public string? ChangeGsmProductOfferingId { get; init; }

    /// <summary>§7 Take-over — legal reason.</summary>
    public string? TransferReason { get; init; }

    public DepositTransferPolicy? DepositTransferPolicy { get; init; }

    public DateTime? TakeOverEffectiveDateUtc { get; init; }

    /// <summary>§7 B.5 obligation snapshot (demo: Unknown, Clear, …).</summary>
    public string? TakeOverObligationStatus { get; init; }

    /// <summary>§4 SIM Swap — replacement reason.</summary>
    public string? ReplacementReason { get; init; }

    /// <summary>§4 Lost/Stolen secure path (BackOffice approval).</summary>
    public bool IsLostOrStolenReport { get; init; }

    /// <summary>§5 Change Number — new MSISDN pool asset id.</summary>
    public string? TargetMsisdnAssetId { get; init; }

    /// <summary>§5 Change Number — reason.</summary>
    public string? NumberChangeReason { get; init; }

    /// <summary>§5 Premium fee (VAL-05-02).</summary>
    public decimal? PremiumFeeAmount { get; init; }

    /// <summary>§10 Termination — Voluntary, Collections, Regulatory, Fraud.</summary>
    public string? TerminationType { get; init; }

    /// <summary>§10 Termination — reason.</summary>
    public string? TerminationReason { get; init; }

    public DateTime? TerminationEffectiveDateUtc { get; init; }

    /// <summary>§10 VAL-10-05 — retention offer outcome (voluntary).</summary>
    public string? RetentionOfferOutcome { get; init; }

    /// <summary>§8 Suspension type.</summary>
    public string? SuspensionType { get; init; }

    public string? SuspensionReason { get; init; }

    public DateTime? SuspensionStartDateUtc { get; init; }

    public DateTime? SuspensionEndDateUtc { get; init; }

    public string? BarringLevel { get; init; }

    public bool AutoReconnectEnabled { get; init; }

    public bool NotificationSuppressed { get; init; }

    /// <summary>§9 Reconnect.</summary>
    public string? ReconnectReason { get; init; }

    public string? ClearanceType { get; init; }

    public string? SourceSuspensionOperationId { get; init; }

    public bool FraudClearanceConfirmed { get; init; }

    public string? PaymentReference { get; init; }

    /// <summary>§14 Device sale.</summary>
    public string? DeviceInventoryId { get; init; }

    public DeviceSaleType? DeviceSaleType { get; init; }

    public string? DeviceInstallmentPlanId { get; init; }

    /// <summary>§15 RFD — Deposit, WalletBalance, Overpayment, SyriatelCash.</summary>
    public string? RefundType { get; init; }

    public string? RefundReason { get; init; }

    public decimal? RefundAmount { get; init; }

    public string? RefundMethod { get; init; }

    /// <summary>§16 BDR.</summary>
    public string? CollectionAction { get; init; }

    public string? DunningStage { get; init; }

    public decimal? CollectedAmount { get; init; }

    public decimal? WriteOffAmount { get; init; }

    public string? AgencyReference { get; init; }

    public int? PaymentPlanMonths { get; init; }

    public string? CollectionNote { get; init; }

    public bool CollectionApprovalConfirmed { get; init; }
}

public class CreateTelecomOperationRequestValidator : AbstractValidator<CreateTelecomOperationRequest>
{
    public CreateTelecomOperationRequestValidator()
    {
        RuleFor(x => x.SubscriberProfileId).NotEmpty();
        RuleFor(x => x.SecondarySubscriberProfileId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.TakeOver);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.TargetOfferName).MaximumLength(200);
        RuleFor(x => x.ProductOfferingId).MaximumLength(450);
        RuleFor(x => x.SimIccid)
            .Must(iccid => IccidValidator.TryValidate(iccid, out _, out _))
            .When(x => x.Kind == TelecomOperationKind.NewActivation && !string.IsNullOrWhiteSpace(x.SimIccid))
            .WithMessage("ICCID غير صالح (19–20 رقماً).");
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.NewActivation);
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.ChangeGsmType);
        RuleFor(x => x.TargetSubscriptionTypeId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.ChangeGsmType);
        RuleFor(x => x.GsmMigrationReason)
            .NotEmpty()
            .MaximumLength(256)
            .When(x => x.Kind == TelecomOperationKind.ChangeGsmType);
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.TakeOver);
        RuleFor(x => x.TransferReason)
            .NotEmpty()
            .MaximumLength(256)
            .When(x => x.Kind == TelecomOperationKind.TakeOver);
        RuleFor(x => x.ReplacementReason)
            .NotEmpty()
            .MaximumLength(256)
            .When(x => x.Kind == TelecomOperationKind.SimSwap);
        RuleFor(x => x.SimIccid)
            .Must(iccid => IccidValidator.TryValidate(iccid, out _, out _))
            .When(x => x.Kind == TelecomOperationKind.SimSwap && !string.IsNullOrWhiteSpace(x.SimIccid))
            .WithMessage("ICCID غير صالح (19–20 رقماً).");
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.SimInventoryId) || !string.IsNullOrWhiteSpace(x.SimIccid))
            .When(x => x.Kind == TelecomOperationKind.SimSwap)
            .WithMessage("يجب تحديد الشريحة الجديدة (ICCID أو معرّف المخزون).");
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.NumberPortability);
        RuleFor(x => x.TargetMsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.NumberPortability);
        RuleFor(x => x.NumberChangeReason)
            .NotEmpty()
            .MaximumLength(256)
            .When(x => x.Kind == TelecomOperationKind.NumberPortability);
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.Termination);
        RuleFor(x => x.TerminationType)
            .NotEmpty()
            .MaximumLength(32)
            .When(x => x.Kind == TelecomOperationKind.Termination);
        RuleFor(x => x.TerminationReason)
            .NotEmpty()
            .MaximumLength(256)
            .When(x => x.Kind == TelecomOperationKind.Termination);
        RuleFor(x => x.RetentionOfferOutcome)
            .NotEmpty()
            .MaximumLength(128)
            .When(x => x.Kind == TelecomOperationKind.Termination
                       && string.Equals(x.TerminationType, TerminationWellKnown.Voluntary, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.TemporarySuspension);
        RuleFor(x => x.SuspensionType)
            .NotEmpty()
            .MaximumLength(32)
            .When(x => x.Kind == TelecomOperationKind.TemporarySuspension);
        RuleFor(x => x.SuspensionReason)
            .NotEmpty()
            .MaximumLength(256)
            .When(x => x.Kind == TelecomOperationKind.TemporarySuspension);
        RuleFor(x => x.BarringLevel)
            .NotEmpty()
            .MaximumLength(32)
            .When(x => x.Kind == TelecomOperationKind.TemporarySuspension);
        RuleFor(x => x.SuspensionEndDateUtc)
            .NotNull()
            .When(x => x.Kind == TelecomOperationKind.TemporarySuspension && x.AutoReconnectEnabled);
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.Reconnect);
        RuleFor(x => x.ReconnectReason)
            .NotEmpty()
            .MaximumLength(256)
            .When(x => x.Kind == TelecomOperationKind.Reconnect);
        RuleFor(x => x.ClearanceType)
            .NotEmpty()
            .MaximumLength(32)
            .When(x => x.Kind == TelecomOperationKind.Reconnect);
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.Migration);
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.ProductOfferingId) || !string.IsNullOrWhiteSpace(x.ProductId))
            .When(x => x.Kind == TelecomOperationKind.Migration)
            .WithMessage("يجب اختيار عرض تجاري أو منتج للترحيل.");
        RuleFor(x => x.DeviceInventoryId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.DeviceSale);
        RuleFor(x => x.DeviceSaleType)
            .NotNull()
            .When(x => x.Kind == TelecomOperationKind.DeviceSale);
        RuleFor(x => x.DeviceInstallmentPlanId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.DeviceSale
                       && x.DeviceSaleType == Domain.Enums.DeviceSaleType.Installment);
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.DepositRefundSettlement);
        RuleFor(x => x.RefundType)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.DepositRefundSettlement);
        RuleFor(x => x.RefundMethod)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.DepositRefundSettlement);
        RuleFor(x => x.RefundReason)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.DepositRefundSettlement);
        RuleFor(x => x.RefundAmount)
            .GreaterThan(0)
            .When(x => x.Kind == TelecomOperationKind.DepositRefundSettlement);
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.BadDebtRecovery);
        RuleFor(x => x.CollectionAction)
            .NotEmpty()
            .MaximumLength(32)
            .When(x => x.Kind == TelecomOperationKind.BadDebtRecovery);
        RuleFor(x => x.PaymentReference)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.BadDebtRecovery
                       && string.Equals(x.CollectionAction, BadDebtWellKnown.PaymentRecorded, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.CollectedAmount)
            .GreaterThan(0)
            .When(x => x.Kind == TelecomOperationKind.BadDebtRecovery
                       && string.Equals(x.CollectionAction, BadDebtWellKnown.PaymentRecorded, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.WriteOffAmount)
            .GreaterThan(0)
            .When(x => x.Kind == TelecomOperationKind.BadDebtRecovery
                       && (string.Equals(x.CollectionAction, BadDebtWellKnown.WriteOffPartial, StringComparison.OrdinalIgnoreCase)
                           || string.Equals(x.CollectionAction, BadDebtWellKnown.WriteOffFull, StringComparison.OrdinalIgnoreCase)));
    }
}

public class CreateTelecomOperationRequestHandler : IRequestHandler<CreateTelecomOperationRequest, CreateTelecomOperationRequestResult>
{
    private readonly ICommandRepository<TelecomOperationRequest> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IQueryContext _queryContext;
    private readonly IBillingSystemIntegration _billing;
    private readonly ITechnicalTicketQueueIngestionService _ticketQueue;
    private readonly IGlobalSettingsProvider _globalSettings;
    private readonly ISellingLineEligibilityChecker _sellingLineEligibility;
    private readonly IChangeGsmEligibilityChecker _changeGsmEligibility;
    private readonly ITakeOverEligibilityChecker _takeOverEligibility;
    private readonly ISimSwapEligibilityChecker _simSwapEligibility;
    private readonly IChangeNumberEligibilityChecker _changeNumberEligibility;
    private readonly ITerminationEligibilityChecker _terminationEligibility;
    private readonly ISuspensionEligibilityChecker _suspensionEligibility;
    private readonly IReconnectEligibilityChecker _reconnectEligibility;
    private readonly IOfferSubscriptionEligibilityChecker _offerSubscriptionEligibility;
    private readonly IPermissionEvaluator _permissions;
    private readonly IDeviceSalesEligibilityChecker _deviceSalesEligibility;
    private readonly IRefundEligibilityChecker _refundEligibility;
    private readonly IBadDebtEligibilityChecker _badDebtEligibility;
    private readonly ICommandRepository<DeviceInventory> _deviceInventoryRepository;

    public CreateTelecomOperationRequestHandler(
        ICommandRepository<TelecomOperationRequest> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        IQueryContext queryContext,
        IBillingSystemIntegration billing,
        ITechnicalTicketQueueIngestionService ticketQueue,
        IGlobalSettingsProvider globalSettings,
        ISellingLineEligibilityChecker sellingLineEligibility,
        IChangeGsmEligibilityChecker changeGsmEligibility,
        ITakeOverEligibilityChecker takeOverEligibility,
        ISimSwapEligibilityChecker simSwapEligibility,
        IChangeNumberEligibilityChecker changeNumberEligibility,
        ITerminationEligibilityChecker terminationEligibility,
        ISuspensionEligibilityChecker suspensionEligibility,
        IReconnectEligibilityChecker reconnectEligibility,
        IOfferSubscriptionEligibilityChecker offerSubscriptionEligibility,
        IPermissionEvaluator permissions,
        IDeviceSalesEligibilityChecker deviceSalesEligibility,
        IRefundEligibilityChecker refundEligibility,
        IBadDebtEligibilityChecker badDebtEligibility,
        ICommandRepository<DeviceInventory> deviceInventoryRepository)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _queryContext = queryContext;
        _billing = billing;
        _ticketQueue = ticketQueue;
        _globalSettings = globalSettings;
        _sellingLineEligibility = sellingLineEligibility;
        _changeGsmEligibility = changeGsmEligibility;
        _takeOverEligibility = takeOverEligibility;
        _simSwapEligibility = simSwapEligibility;
        _changeNumberEligibility = changeNumberEligibility;
        _terminationEligibility = terminationEligibility;
        _suspensionEligibility = suspensionEligibility;
        _reconnectEligibility = reconnectEligibility;
        _offerSubscriptionEligibility = offerSubscriptionEligibility;
        _permissions = permissions;
        _deviceSalesEligibility = deviceSalesEligibility;
        _refundEligibility = refundEligibility;
        _badDebtEligibility = badDebtEligibility;
        _deviceInventoryRepository = deviceInventoryRepository;
    }

    public async Task<CreateTelecomOperationRequestResult> Handle(
        CreateTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        TakeOverEligibilityResult? takeOverEligibility = null;
        if (request.Kind == TelecomOperationKind.TakeOver)
        {
            await EnsureTakeOverCreatePermissionAsync(request.CreatedById, cancellationToken);

            takeOverEligibility = await _takeOverEligibility.ValidateForCreateAsync(
                request.SubscriberProfileId,
                request.SecondarySubscriberProfileId!,
                request.MsisdnAssetId,
                request.TransferReason!,
                excludeOperationId: null,
                cancellationToken: cancellationToken);

            if (!takeOverEligibility.Allowed)
            {
                throw new BusinessRuleViolationException(takeOverEligibility.MessageAr);
            }
        }

        if (request.Kind == TelecomOperationKind.NewActivation)
        {
            await ValidateNewActivationLineCapAsync(request, cancellationToken);
        }

        SimSwapEligibilityResult? simSwapEligibility = null;
        if (request.Kind == TelecomOperationKind.SimSwap)
        {
            await EnsureSimSwapCreatePermissionAsync(request.CreatedById, cancellationToken);

            simSwapEligibility = await _simSwapEligibility.ValidateForCreateAsync(
                request.SubscriberProfileId,
                request.MsisdnAssetId,
                string.IsNullOrEmpty((request.SimInventoryId ?? string.Empty).Trim()) ? null : request.SimInventoryId,
                request.SimIccid,
                request.ReplacementReason!,
                request.IsLostOrStolenReport,
                excludeOperationId: null,
                cancellationToken: cancellationToken);

            if (!simSwapEligibility.Allowed)
            {
                throw new BusinessRuleViolationException(simSwapEligibility.MessageAr);
            }
        }

        ChangeNumberEligibilityResult? changeNumberEligibility = null;
        if (request.Kind == TelecomOperationKind.NumberPortability)
        {
            await EnsureChangeNumberCreatePermissionAsync(request.CreatedById, cancellationToken);

            changeNumberEligibility = await _changeNumberEligibility.ValidateForCreateAsync(
                request.SubscriberProfileId,
                request.MsisdnAssetId!,
                request.TargetMsisdnAssetId!,
                request.NumberChangeReason!,
                request.PremiumFeeAmount,
                excludeOperationId: null,
                cancellationToken: cancellationToken);

            if (!changeNumberEligibility.Allowed)
            {
                throw new BusinessRuleViolationException(changeNumberEligibility.MessageAr);
            }
        }

        TerminationEligibilityResult? terminationEligibility = null;
        if (request.Kind == TelecomOperationKind.Termination)
        {
            await EnsureTerminationCreatePermissionAsync(request.CreatedById, cancellationToken);

            terminationEligibility = await _terminationEligibility.ValidateForCreateAsync(
                request.SubscriberProfileId,
                request.MsisdnAssetId!,
                request.TerminationType!,
                request.TerminationReason!,
                request.RetentionOfferOutcome,
                excludeOperationId: null,
                cancellationToken: cancellationToken);

            if (!terminationEligibility.Allowed)
            {
                throw new BusinessRuleViolationException(terminationEligibility.MessageAr);
            }
        }

        SuspensionEligibilityResult? suspensionEligibility = null;
        if (request.Kind == TelecomOperationKind.TemporarySuspension)
        {
            await EnsureSuspensionCreatePermissionAsync(request.CreatedById, cancellationToken);

            suspensionEligibility = await _suspensionEligibility.ValidateForCreateAsync(
                request.SubscriberProfileId,
                request.MsisdnAssetId!,
                request.SuspensionType!,
                request.SuspensionReason!,
                request.BarringLevel ?? SuspensionWellKnown.BarringFull,
                request.AutoReconnectEnabled,
                request.SuspensionEndDateUtc,
                excludeOperationId: null,
                cancellationToken: cancellationToken);

            if (!suspensionEligibility.Allowed)
            {
                throw new BusinessRuleViolationException(suspensionEligibility.MessageAr);
            }
        }

        ReconnectEligibilityResult? reconnectEligibility = null;
        if (request.Kind == TelecomOperationKind.Reconnect)
        {
            await EnsureReconnectCreatePermissionAsync(request.CreatedById, cancellationToken);

            reconnectEligibility = await _reconnectEligibility.ValidateForCreateAsync(
                request.SubscriberProfileId,
                request.MsisdnAssetId!,
                request.ReconnectReason!,
                request.ClearanceType ?? ReconnectWellKnown.Customer,
                request.PaymentReference,
                request.FraudClearanceConfirmed,
                request.SourceSuspensionOperationId,
                excludeOperationId: null,
                cancellationToken: cancellationToken);

            if (!reconnectEligibility.Allowed)
            {
                throw new BusinessRuleViolationException(reconnectEligibility.MessageAr);
            }
        }

        DeviceSalesEligibilityResult? deviceSalesEligibility = null;
        if (request.Kind == TelecomOperationKind.DeviceSale)
        {
            deviceSalesEligibility = await _deviceSalesEligibility.ValidateForCreateAsync(
                request.SubscriberProfileId,
                request.DeviceInventoryId!,
                request.DeviceSaleType!.Value,
                request.DeviceInstallmentPlanId,
                excludeOperationId: null,
                cancellationToken: cancellationToken);

            if (!deviceSalesEligibility.Allowed)
            {
                throw new BusinessRuleViolationException(deviceSalesEligibility.MessageAr);
            }
        }

        BadDebtEligibilityResult? badDebtEligibility = null;
        if (request.Kind == TelecomOperationKind.BadDebtRecovery)
        {
            await EnsureBadDebtCreatePermissionAsync(request.CreatedById, cancellationToken);

            badDebtEligibility = await _badDebtEligibility.ValidateForCreateAsync(
                request.SubscriberProfileId,
                request.MsisdnAssetId!,
                request.CollectionAction!,
                request.DunningStage,
                request.PaymentReference,
                request.CollectedAmount,
                request.WriteOffAmount,
                request.CollectionApprovalConfirmed,
                excludeOperationId: null,
                cancellationToken: cancellationToken);

            if (!badDebtEligibility.Allowed)
            {
                throw new BusinessRuleViolationException(badDebtEligibility.MessageAr);
            }
        }

        RefundEligibilityResult? refundEligibility = null;
        if (request.Kind == TelecomOperationKind.DepositRefundSettlement)
        {
            await EnsureRefundCreatePermissionAsync(request.CreatedById, cancellationToken);

            refundEligibility = await _refundEligibility.ValidateForCreateAsync(
                request.SubscriberProfileId,
                request.MsisdnAssetId!,
                request.RefundType!,
                request.RefundMethod!,
                request.RefundAmount!.Value,
                request.RefundReason!,
                excludeOperationId: null,
                cancellationToken: cancellationToken);

            if (!refundEligibility.Allowed)
            {
                throw new BusinessRuleViolationException(refundEligibility.MessageAr);
            }
        }

        ChangeGsmEligibilityResult? changeGsmEligibility = null;
        if (request.Kind == TelecomOperationKind.ChangeGsmType)
        {
            changeGsmEligibility = await _changeGsmEligibility.ValidateForCreateAsync(
                request.SubscriberProfileId,
                request.MsisdnAssetId,
                request.TargetSubscriptionTypeId!,
                request.GsmMigrationReason,
                cancellationToken);

            if (!changeGsmEligibility.Allowed)
            {
                throw new BusinessRuleViolationException(changeGsmEligibility.MessageAr);
            }
        }

        var offeringIdInput = (request.ProductOfferingId ?? string.Empty).Trim();
        if (request.Kind == TelecomOperationKind.ChangeGsmType
            && string.IsNullOrEmpty(offeringIdInput)
            && !string.IsNullOrWhiteSpace(request.ChangeGsmProductOfferingId))
        {
            offeringIdInput = request.ChangeGsmProductOfferingId.Trim();
        }
        var productIdInput = (request.ProductId ?? string.Empty).Trim();

        string? resolvedOfferingId = null;
        string? resolvedProductId = null;

        if (!string.IsNullOrEmpty(offeringIdInput))
        {
            var offeringEntity = await _queryContext.ProductOffering
                .AsNoTracking()
                .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == offeringIdInput, cancellationToken)
                ?? throw new BusinessRuleViolationException("العرض التجاري المختار غير موجود.");

            var linked = (offeringEntity.ProductId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(linked))
            {
                throw new BusinessRuleViolationException(
                    "هذا العرض غير مربوط بمنتج تقني للتفعيل. يرجى ربط منتج (CBS) من كتالوج الباقات.");
            }

            resolvedOfferingId = offeringEntity.Id;
            resolvedProductId = linked;
        }
        else if (!string.IsNullOrEmpty(productIdInput))
        {
            resolvedProductId = productIdInput;
        }

        OfferSubscriptionEligibilityResult? migrationEligibility = null;
        if (request.Kind == TelecomOperationKind.Migration)
        {
            if (string.IsNullOrEmpty(resolvedProductId) || string.IsNullOrEmpty(resolvedOfferingId))
            {
                throw new BusinessRuleViolationException(
                    "ترحيل الباقة يتطلب اختيار عرض تجاري مربوط بمنتج تقني (CBS).");
            }

            migrationEligibility = await _offerSubscriptionEligibility.ValidateForMigrationCreateAsync(
                request.SubscriberProfileId,
                request.MsisdnAssetId!,
                resolvedOfferingId,
                resolvedProductId,
                excludeOperationId: null,
                cancellationToken);

            if (!migrationEligibility.Allowed)
            {
                throw new BusinessRuleViolationException(migrationEligibility.MessageAr);
            }
        }
        else if (request.Kind == TelecomOperationKind.NewActivation
                 && !string.IsNullOrEmpty(resolvedProductId))
        {
            await _sellingLineEligibility.ValidateCatalogForCreateAsync(
                request, resolvedProductId, resolvedOfferingId, cancellationToken);
            await _sellingLineEligibility.ValidateForCreateAsync(request, cancellationToken);
        }
        else if (request.Kind == TelecomOperationKind.ChangeGsmType
                 && !string.IsNullOrEmpty(resolvedProductId)
                 && changeGsmEligibility != null)
        {
            await ValidateCatalogSelectionForChangeGsmAsync(
                changeGsmEligibility.SourceSubscriptionTypeId!,
                request.TargetSubscriptionTypeId!,
                resolvedProductId,
                resolvedOfferingId,
                cancellationToken);
        }
        else if (request.Kind == TelecomOperationKind.NewActivation)
        {
            await _sellingLineEligibility.ValidateForCreateAsync(request, cancellationToken);
        }

        var simInventoryId = (request.SimInventoryId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(simInventoryId) && !string.IsNullOrWhiteSpace(request.SimIccid))
        {
            if (!IccidValidator.TryValidate(request.SimIccid, out var normalizedIccid, out var iccidErr))
            {
                throw new BusinessRuleViolationException(iccidErr ?? "ICCID غير صالح.");
            }

            var sim = await _queryContext.SimInventory
                .AsNoTracking()
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.Iccid == normalizedIccid, cancellationToken)
                ?? throw new BusinessRuleViolationException("الشريحة (ICCID) غير موجودة في المستودع.");
            simInventoryId = sim.Id;
        }

        if (request.Kind == TelecomOperationKind.SimSwap
            && simSwapEligibility != null
            && !string.IsNullOrEmpty(simSwapEligibility.NewSimInventoryId))
        {
            simInventoryId = simSwapEligibility.NewSimInventoryId;
        }

        var (entityName, prefix) = TelecomNumberSequence.ForKind(request.Kind);
        var number = _numberSequenceService.GenerateNumber(entityName, prefix, "", useDate: false);

        var notes = BuildNotes(request);

        var channel = request.ActivationChannel ?? ActivationChannel.Showroom;
        var dealerCode = string.IsNullOrWhiteSpace(request.DealerCode) ? null : request.DealerCode.Trim();
        var branchId = string.IsNullOrWhiteSpace(request.BranchId) ? null : request.BranchId.Trim();

        var entity = new TelecomOperationRequest
        {
            Kind = request.Kind,
            Number = number,
            CorrelationId = Guid.CreateVersion7().ToString(),
            Status = TelecomOperationStatus.Draft,
            DocumentStatus = TelecomDocumentStatus.Missing,
            SubscriberProfileId = request.SubscriberProfileId,
            SecondarySubscriberProfileId = request.SecondarySubscriberProfileId,
            MsisdnAssetId = request.MsisdnAssetId,
            SimInventoryId = string.IsNullOrEmpty(simInventoryId) ? null : simInventoryId,
            ProductId = resolvedProductId,
            ProductOfferingId = resolvedOfferingId,
            Notes = notes,
            TargetOfferName = request.TargetOfferName,
            ActivationChannel = channel,
            DealerCode = dealerCode,
            BranchId = branchId,
            CreatedById = request.CreatedById,
            IsLostOrStolenReport = request.IsLostOrStolenReport,
            FraudClearanceConfirmed = false,
            AutoReconnectEnabled = false,
            NotificationSuppressed = false,
        };

        if (request.Kind == TelecomOperationKind.ChangeGsmType && changeGsmEligibility != null)
        {
            entity.SourceSubscriptionTypeId = changeGsmEligibility.SourceSubscriptionTypeId;
            entity.TargetSubscriptionTypeId = request.TargetSubscriptionTypeId!.Trim();
            entity.GsmMigrationReason = request.GsmMigrationReason!.Trim();
            entity.GsmEffectiveDateUtc = request.GsmEffectiveDateUtc ?? DateTime.UtcNow;
            entity.GsmCompatibilityStatus = changeGsmEligibility.CompatibilityStatus;
            entity.Notes = AppendChangeGsmAudit(entity.Notes, changeGsmEligibility);
        }

        if (request.Kind == TelecomOperationKind.TakeOver && takeOverEligibility != null)
        {
            entity.TransferReason = request.TransferReason!.Trim();
            entity.DepositTransferPolicy = request.DepositTransferPolicy ?? DepositTransferPolicy.TransferToNewOwner;
            entity.TakeOverEffectiveDateUtc = request.TakeOverEffectiveDateUtc ?? DateTime.UtcNow;
            entity.TakeOverObligationStatus = string.IsNullOrWhiteSpace(request.TakeOverObligationStatus)
                ? "Unknown"
                : request.TakeOverObligationStatus.Trim();
            entity.ApprovalLevelRequired = "BackOffice";
            entity.OldCustomerId = takeOverEligibility.OldCustomerId;
            entity.NewCustomerId = takeOverEligibility.NewCustomerId;
            entity.PriorSubscriberProfileId = request.SubscriberProfileId;
            entity.Notes = AppendTakeOverAudit(entity.Notes, takeOverEligibility);
        }

        if (request.Kind == TelecomOperationKind.SimSwap && simSwapEligibility != null)
        {
            entity.ReplacementReason = request.ReplacementReason!.Trim();
            entity.IsLostOrStolenReport = request.IsLostOrStolenReport;
            entity.PriorSimInventoryId = simSwapEligibility.PriorSimInventoryId;
            if (request.IsLostOrStolenReport)
            {
                entity.ApprovalLevelRequired = "BackOffice";
            }

            entity.Notes = AppendSimSwapAudit(entity.Notes, simSwapEligibility);
        }

        if (request.Kind == TelecomOperationKind.NumberPortability && changeNumberEligibility != null)
        {
            entity.PriorMsisdnAssetId = changeNumberEligibility.PriorMsisdnAssetId ?? request.MsisdnAssetId;
            entity.TargetMsisdnAssetId = changeNumberEligibility.TargetMsisdnAssetId ?? request.TargetMsisdnAssetId;
            entity.NumberChangeReason = request.NumberChangeReason!.Trim();
            entity.NumberChangeMode = ChangeNumberModes.Internal;
            entity.PremiumFeeAmount = request.PremiumFeeAmount;
            if (changeNumberEligibility.RequiresBackOfficeApproval)
            {
                entity.ApprovalLevelRequired = "BackOffice";
            }

            entity.Notes = AppendChangeNumberAudit(entity.Notes, changeNumberEligibility);
        }

        if (request.Kind == TelecomOperationKind.Termination && terminationEligibility != null)
        {
            entity.TerminationType = request.TerminationType!.Trim();
            entity.TerminationReason = request.TerminationReason!.Trim();
            entity.TerminationEffectiveDateUtc = request.TerminationEffectiveDateUtc ?? DateTime.UtcNow;
            entity.RetentionOfferOutcome = (request.RetentionOfferOutcome ?? string.Empty).Trim();
            entity.PriorMsisdnAssetId = terminationEligibility.MsisdnAssetId ?? request.MsisdnAssetId;
            entity.PriorSimInventoryId = terminationEligibility.PriorSimInventoryId;
            entity.DeprovisionStatus = "Pending";
            if (terminationEligibility.RequiresBackOfficeApproval)
            {
                entity.ApprovalLevelRequired = "BackOffice";
            }

            entity.Notes = AppendTerminationAudit(entity.Notes, terminationEligibility);
        }

        if (request.Kind == TelecomOperationKind.Migration && migrationEligibility != null)
        {
            entity.PriorProductId = migrationEligibility.PriorProductId;
            entity.PriorProductOfferingId = migrationEligibility.PriorProductOfferingId;
            entity.Notes = AppendOfferMigrationAudit(entity.Notes, migrationEligibility);
        }

        if (request.Kind == TelecomOperationKind.TemporarySuspension && suspensionEligibility != null)
        {
            entity.SuspensionType = request.SuspensionType!.Trim();
            entity.SuspensionReason = request.SuspensionReason!.Trim();
            entity.BarringLevel = (request.BarringLevel ?? SuspensionWellKnown.BarringFull).Trim();
            entity.SuspensionStartDateUtc = request.SuspensionStartDateUtc ?? DateTime.UtcNow;
            entity.SuspensionEndDateUtc = request.SuspensionEndDateUtc;
            entity.AutoReconnectEnabled = request.AutoReconnectEnabled;
            entity.NotificationSuppressed = request.NotificationSuppressed;
            entity.BarStatus = "Pending";
            if (suspensionEligibility.RequiresBackOfficeApproval)
            {
                entity.ApprovalLevelRequired = "BackOffice";
                entity.Status = TelecomOperationStatus.PendingDocuments;
            }

            entity.Notes = AppendSuspensionAudit(entity.Notes, suspensionEligibility);
        }

        if (request.Kind == TelecomOperationKind.DeviceSale && deviceSalesEligibility != null)
        {
            entity.DeviceInventoryId = deviceSalesEligibility.DeviceInventoryId ?? request.DeviceInventoryId!.Trim();
            entity.DeviceSaleType = request.DeviceSaleType;
            entity.InstallmentPlanId = request.DeviceInstallmentPlanId?.Trim();
            entity.DeviceFinancingDecision = deviceSalesEligibility.FinancingDecision;
            entity.DeviceApprovalLevelRequired = deviceSalesEligibility.ApprovalLevelRequired;
            entity.DeviceFinancingNoteAr = deviceSalesEligibility.FinancingNoteAr;
            entity.DeviceDownPaymentAmount = deviceSalesEligibility.RequiredDownPayment;
            entity.DeviceMonthlyInstallmentAmount = deviceSalesEligibility.MonthlyInstallment;
            entity.DeviceCreditScoreSnapshot = deviceSalesEligibility.CreditScoreSnapshot;
            entity.ProvisioningResult = "Pending";
            if (deviceSalesEligibility.RequiresFinanceApproval)
            {
                entity.ApprovalLevelRequired = deviceSalesEligibility.ApprovalLevelRequired;
            }

            entity.Notes = string.IsNullOrEmpty(entity.Notes)
                ? deviceSalesEligibility.MessageAr
                : $"{entity.Notes}\n{deviceSalesEligibility.MessageAr}";
        }

        if (request.Kind == TelecomOperationKind.DepositRefundSettlement && refundEligibility != null)
        {
            entity.RefundType = request.RefundType!.Trim();
            entity.RefundMethod = request.RefundMethod!.Trim();
            entity.RefundReason = request.RefundReason!.Trim();
            entity.RefundAmount = request.RefundAmount;
            entity.DepositBalanceSnapshot = refundEligibility.DepositBalanceSnapshot;
            entity.WalletBalanceSnapshot = refundEligibility.WalletBalanceSnapshot;
            entity.RefundSettlementStatus = RefundWellKnown.SettlementPending;
            entity.RequiresDualApproval = refundEligibility.RequiresDualApproval;
            entity.ProvisioningResult = "Pending";
            if (refundEligibility.RequiresBackOfficeApproval)
            {
                entity.ApprovalLevelRequired = "BackOffice";
                entity.Status = TelecomOperationStatus.PendingDocuments;
            }

            entity.Notes = AppendRefundAudit(entity.Notes, refundEligibility);
        }

        if (request.Kind == TelecomOperationKind.BadDebtRecovery && badDebtEligibility != null)
        {
            entity.CollectionAction = request.CollectionAction!.Trim();
            entity.DunningStage = string.IsNullOrWhiteSpace(request.DunningStage)
                ? BadDebtWellKnown.Reminder1
                : request.DunningStage.Trim();
            entity.PriorDunningStage = entity.DunningStage;
            entity.OutstandingBalanceSnapshot = badDebtEligibility.OutstandingBalanceSnapshot;
            entity.CollectedAmount = request.CollectedAmount;
            entity.WriteOffAmount = request.WriteOffAmount;
            entity.AgencyReference = string.IsNullOrWhiteSpace(request.AgencyReference)
                ? null
                : request.AgencyReference.Trim();
            entity.PaymentPlanMonths = request.PaymentPlanMonths;
            entity.CollectionNote = string.IsNullOrWhiteSpace(request.CollectionNote)
                ? null
                : request.CollectionNote.Trim();
            entity.PaymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
                ? null
                : request.PaymentReference.Trim();
            entity.FraudClearanceConfirmed = request.CollectionApprovalConfirmed;
            entity.CollectionSettlementStatus = BadDebtWellKnown.SettlementPending;
            entity.ProvisioningResult = "Pending";
            if (request.PaymentPlanMonths is > 0)
            {
                entity.NextDunningDueUtc = DateTime.UtcNow.AddMonths(request.PaymentPlanMonths.Value);
            }

            if (badDebtEligibility.RequiresBackOfficeApproval)
            {
                entity.ApprovalLevelRequired = "BackOffice";
                entity.Status = TelecomOperationStatus.PendingDocuments;
            }

            entity.Notes = AppendBadDebtAudit(entity.Notes, badDebtEligibility);
        }

        if (request.Kind == TelecomOperationKind.Reconnect && reconnectEligibility != null)
        {
            entity.ReconnectReason = request.ReconnectReason!.Trim();
            entity.ClearanceType = (request.ClearanceType ?? ReconnectWellKnown.Customer).Trim();
            entity.PaymentReference = string.IsNullOrWhiteSpace(request.PaymentReference)
                ? null
                : request.PaymentReference.Trim();
            entity.SourceSuspensionOperationId = reconnectEligibility.SourceSuspensionOperationId;
            entity.FraudClearanceConfirmed = request.FraudClearanceConfirmed;
            entity.FraudClearanceByUserId = request.FraudClearanceConfirmed ? request.CreatedById : null;
            entity.IsLostOrStolenReport = false;
            entity.AutoReconnectEnabled = false;
            entity.NotificationSuppressed = false;
            if (reconnectEligibility.RequiresBackOfficeApproval)
            {
                entity.ApprovalLevelRequired = "BackOffice";
                entity.Status = TelecomOperationStatus.PendingDocuments;
            }

            entity.Notes = AppendReconnectAudit(entity.Notes, reconnectEligibility);
        }

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        if (entity.Kind == TelecomOperationKind.DeviceSale && !string.IsNullOrEmpty(entity.DeviceInventoryId))
        {
            var device = await _deviceInventoryRepository.GetAsync(entity.DeviceInventoryId, cancellationToken);
            if (device != null && device.Status == DeviceInventoryStatus.Available)
            {
                device.ReserveForOperation(entity.Id);
                device.UpdatedById = request.CreatedById;
                _deviceInventoryRepository.Update(device);
                await _unitOfWork.SaveAsync(cancellationToken);
            }
        }

        if (entity.Kind is not TelecomOperationKind.NewActivation
            and not TelecomOperationKind.DeviceSale
            and not TelecomOperationKind.DepositRefundSettlement)
        {
            await _ticketQueue.EnqueueFromTelecomOperationAsync(entity, request.CreatedById, cancellationToken);
        }

        return new CreateTelecomOperationRequestResult { Data = entity };
    }

    private async Task ValidateCatalogSelectionAgainstSubscriptionAsync(
        CreateTelecomOperationRequest request,
        string resolvedProductId,
        string? resolvedOfferingId,
        CancellationToken cancellationToken)
    {
        var subs = await _queryContext.TelecomSubscription
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.SubscriberProfileId == request.SubscriberProfileId)
            .Include(s => s.SubscriptionTypeLookup)
            .ToListAsync(cancellationToken);

        if (subs.Count == 0)
        {
            throw new BusinessRuleViolationException(
                "لا يوجد اشتراك مرتبط بملف المشترك؛ لا يمكن التحقق من نوع الخط لتطبيق قواعد توافق الباقة.");
        }

        var msisdnId = (request.MsisdnAssetId ?? string.Empty).Trim();
        TelecomSubscription? chosen = null;
        if (request.Kind == TelecomOperationKind.NewActivation && !string.IsNullOrEmpty(msisdnId))
        {
            var poolAsset = await _queryContext.MsisdnAsset
                .AsNoTracking()
                .FirstOrDefaultAsync(m => !m.IsDeleted && m.Id == msisdnId, cancellationToken);
            if (poolAsset?.PoolStatus == MsisdnPoolStatus.Available)
            {
                chosen = subs.FirstOrDefault(s => s.IsPrimaryLine) ?? subs.First();
            }
        }

        if (chosen == null && !string.IsNullOrEmpty(msisdnId))
        {
            chosen = subs.FirstOrDefault(s => s.MsisdnAssetId == msisdnId);
        }

        chosen ??= subs.FirstOrDefault(s => s.IsPrimaryLine) ?? subs.First();

        var lineTypeId = chosen.SubscriptionTypeId;

        var product = await _queryContext.Product
            .AsNoTracking()
            .FirstOrDefaultAsync(p => !p.IsDeleted && p.Id == resolvedProductId, cancellationToken)
            ?? throw new BusinessRuleViolationException("المنتج التقني المرتبط غير موجود.");

        if (!string.IsNullOrEmpty(resolvedOfferingId))
        {
            var offering = await _queryContext.ProductOffering
                .AsNoTracking()
                .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == resolvedOfferingId, cancellationToken);

            if (offering != null
                && !string.IsNullOrEmpty(offering.CompatibleSubscriptionTypeId)
                && offering.CompatibleSubscriptionTypeId != lineTypeId)
            {
                throw new BusinessRuleViolationException(
                    $"نوع الخط الحالي ({chosen.SubscriptionTypeLookup?.NameAr ?? "غير معروف"}) غير متوافق مع العرض التجاري المختار.");
            }
        }

        if (!string.IsNullOrEmpty(product.CompatibleSubscriptionTypeId)
            && product.CompatibleSubscriptionTypeId != lineTypeId)
        {
            throw new BusinessRuleViolationException(
                $"نوع الخط الأساسي ({chosen.SubscriptionTypeLookup?.NameAr ?? "غير معروف"}) غير متوافق مع الباقة المطلوبة.");
        }
    }

    private async Task ValidateCatalogSelectionForChangeGsmAsync(
        string sourceTypeId,
        string targetTypeId,
        string resolvedProductId,
        string? resolvedOfferingId,
        CancellationToken cancellationToken)
    {
        var product = await _queryContext.Product
            .AsNoTracking()
            .FirstOrDefaultAsync(p => !p.IsDeleted && p.Id == resolvedProductId, cancellationToken)
            ?? throw new BusinessRuleViolationException("المنتج التقني المرتبط غير موجود.");

        if (!string.IsNullOrEmpty(resolvedOfferingId))
        {
            var offering = await _queryContext.ProductOffering
                .AsNoTracking()
                .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == resolvedOfferingId, cancellationToken);

            if (offering != null
                && !string.IsNullOrEmpty(offering.CompatibleSubscriptionTypeId)
                && offering.CompatibleSubscriptionTypeId != targetTypeId)
            {
                throw new BusinessRuleViolationException(
                    "العرض التجاري المختار غير متوافق مع نوع الخط الهدف بعد التحويل.");
            }
        }

        if (!string.IsNullOrEmpty(product.CompatibleSubscriptionTypeId)
            && product.CompatibleSubscriptionTypeId != targetTypeId)
        {
            throw new BusinessRuleViolationException(
                "الباقة المختارة غير متوافقة مع نوع الخط الهدف (CGT).");
        }
    }

    private static string? AppendChangeGsmAudit(string? notes, ChangeGsmEligibilityResult eligibility)
    {
        var stamp =
            $"CGT|src={eligibility.SourceTypeCode ?? eligibility.SourceSubscriptionTypeId}|tgt={eligibility.TargetTypeCode}|status={eligibility.CompatibilityStatus}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    private async Task EnsureSimSwapCreatePermissionAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لإنشاء طلب تبديل الشريحة.");
        }

        string[] keys =
        [
            PermissionCatalog.TelecomLineSimSwapRequest,
            PermissionCatalog.TelecomLineSimSwap,
            PermissionCatalog.TelecomLineSimSwapApprove,
            PermissionCatalog.CustomerUpdate,
            PermissionCatalog.TelecomCustomerProvisioning,
        ];

        foreach (var key in keys)
        {
            if (await _permissions.HasPermissionAsync(userId, key, cancellationToken))
            {
                return;
            }
        }

        throw new BusinessRuleViolationException(
            "ليس لديك صلاحية إنشاء طلب تبديل الشريحة (telecom.line.simswap_request).");
    }

    private async Task EnsureTakeOverCreatePermissionAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لإنشاء طلب نقل الملكية.");
        }

        string[] keys =
        [
            PermissionCatalog.TelecomLineTransferRequest,
            PermissionCatalog.TelecomLineTransferOwnership,
            PermissionCatalog.CustomerUpdate,
            PermissionCatalog.TelecomCustomerProvisioning,
        ];

        foreach (var key in keys)
        {
            if (await _permissions.HasPermissionAsync(userId, key, cancellationToken))
            {
                return;
            }
        }

        throw new BusinessRuleViolationException(
            "ليس لديك صلاحية إنشاء طلب نقل الملكية (telecom.line.transfer_request).");
    }

    private static string? AppendTakeOverAudit(string? notes, TakeOverEligibilityResult eligibility)
    {
        var stamp = $"TKO|msisdn={eligibility.Msisdn ?? "—"}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    private static string? AppendSimSwapAudit(string? notes, SimSwapEligibilityResult eligibility)
    {
        var stamp = $"SIM|msisdn={eligibility.Msisdn ?? "—"}|prior={eligibility.PriorSimInventoryId ?? "—"}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    private static string? AppendChangeNumberAudit(string? notes, ChangeNumberEligibilityResult eligibility)
    {
        var stamp =
            $"CNR|cur={eligibility.CurrentMsisdn ?? "—"}|tgt={eligibility.TargetMsisdn ?? "—"}|bo={eligibility.RequiresBackOfficeApproval}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    private async Task EnsureTerminationCreatePermissionAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لإنشاء طلب إنهاء الخط.");
        }

        string[] keys =
        [
            PermissionCatalog.TelecomLineTerminationRequest,
            PermissionCatalog.TelecomLineTermination,
            PermissionCatalog.TelecomLineTerminationApprove,
            PermissionCatalog.CustomerUpdate,
            PermissionCatalog.TelecomCustomerProvisioning,
        ];

        foreach (var key in keys)
        {
            if (await _permissions.HasPermissionAsync(userId, key, cancellationToken))
            {
                return;
            }
        }

        throw new BusinessRuleViolationException(
            "ليس لديك صلاحية إنشاء طلب إنهاء الخط (telecom.line.termination_request).");
    }

    private static string? AppendTerminationAudit(string? notes, TerminationEligibilityResult eligibility)
    {
        var stamp =
            $"TRM|msisdn={eligibility.Msisdn ?? "—"}|bo={eligibility.RequiresBackOfficeApproval}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    private static string? AppendOfferMigrationAudit(string? notes, OfferSubscriptionEligibilityResult eligibility)
    {
        var stamp =
            $"MGR|msisdn={eligibility.Msisdn ?? "—"}|priorProduct={eligibility.PriorProductId ?? "—"}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    private async Task EnsureSuspensionCreatePermissionAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لإنشاء طلب حظر الخط.");
        }

        string[] keys =
        [
            PermissionCatalog.TelecomLineSuspensionRequest,
            PermissionCatalog.TelecomLineSuspension,
            PermissionCatalog.TelecomLineSuspensionApprove,
            PermissionCatalog.CustomerUpdate,
            PermissionCatalog.TelecomCustomerProvisioning,
        ];

        foreach (var key in keys)
        {
            if (await _permissions.HasPermissionAsync(userId, key, cancellationToken))
            {
                return;
            }
        }

        throw new BusinessRuleViolationException(
            "ليس لديك صلاحية إنشاء طلب حظر الخط (telecom.line.suspension_request).");
    }

    private async Task EnsureRefundCreatePermissionAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لإنشاء طلب استرداد مالي.");
        }

        string[] keys =
        [
            PermissionCatalog.TelecomLineRefundRequest,
            PermissionCatalog.TelecomLineRefund,
            PermissionCatalog.TelecomLineRefundApprove,
            PermissionCatalog.CustomerUpdate,
            PermissionCatalog.TelecomCustomerProvisioning,
        ];

        foreach (var key in keys)
        {
            if (await _permissions.HasPermissionAsync(userId, key, cancellationToken))
            {
                return;
            }
        }

        throw new BusinessRuleViolationException(
            "ليس لديك صلاحية إنشاء طلب الاسترداد (telecom.line.refund_request).");
    }

    private async Task EnsureReconnectCreatePermissionAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لإنشاء طلب إعادة التفعيل.");
        }

        string[] keys =
        [
            PermissionCatalog.TelecomLineReconnectRequest,
            PermissionCatalog.TelecomLineReconnect,
            PermissionCatalog.TelecomLineReconnectApprove,
            PermissionCatalog.CustomerUpdate,
            PermissionCatalog.TelecomCustomerProvisioning,
        ];

        foreach (var key in keys)
        {
            if (await _permissions.HasPermissionAsync(userId, key, cancellationToken))
            {
                return;
            }
        }

        throw new BusinessRuleViolationException(
            "ليس لديك صلاحية إنشاء طلب إعادة التفعيل (telecom.line.reconnect_request).");
    }

    private static string? AppendRefundAudit(string? notes, RefundEligibilityResult eligibility)
    {
        var stamp =
            $"RFD|msisdn={eligibility.Msisdn ?? "—"}|deposit={eligibility.DepositBalanceSnapshot:N0}|wallet={eligibility.WalletBalanceSnapshot:N0}|dual={eligibility.RequiresDualApproval}|bo={eligibility.RequiresBackOfficeApproval}|code={eligibility.OutcomeCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    private static string? AppendSuspensionAudit(string? notes, SuspensionEligibilityResult eligibility)
    {
        var stamp =
            $"SUS|msisdn={eligibility.Msisdn ?? "—"}|bo={eligibility.RequiresBackOfficeApproval}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    private static string? AppendReconnectAudit(string? notes, ReconnectEligibilityResult eligibility)
    {
        var stamp =
            $"RCN|msisdn={eligibility.Msisdn ?? "—"}|bo={eligibility.RequiresBackOfficeApproval}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    private async Task EnsureBadDebtCreatePermissionAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لإنشاء طلب التحصيل.");
        }

        string[] keys =
        [
            PermissionCatalog.TelecomLineCollectionRequest,
            PermissionCatalog.TelecomLineCollection,
            PermissionCatalog.TelecomLineCollectionApprove,
            PermissionCatalog.TelecomLineCollectionManage,
            PermissionCatalog.CustomerUpdate,
            PermissionCatalog.TelecomCustomerProvisioning,
        ];

        foreach (var key in keys)
        {
            if (await _permissions.HasPermissionAsync(userId, key, cancellationToken))
            {
                return;
            }
        }

        throw new BusinessRuleViolationException(
            "ليس لديك صلاحية إنشاء طلب التحصيل (telecom.line.collection_request).");
    }

    private static string? AppendBadDebtAudit(string? notes, BadDebtEligibilityResult eligibility)
    {
        var stamp =
            $"BDR|msisdn={eligibility.Msisdn ?? "—"}|balance={eligibility.OutstandingBalanceSnapshot:N0}|bo={eligibility.RequiresBackOfficeApproval}|code={eligibility.ValidationCode}";
        return string.IsNullOrWhiteSpace(notes) ? stamp : $"{notes} | {stamp}";
    }

    private async Task EnsureChangeNumberCreatePermissionAsync(string? userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لإنشاء طلب تغيير الرقم.");
        }

        string[] keys =
        [
            PermissionCatalog.TelecomLineChangeNumberRequest,
            PermissionCatalog.TelecomLineChangeNumber,
            PermissionCatalog.TelecomLineChangeNumberApprove,
            PermissionCatalog.CustomerUpdate,
            PermissionCatalog.TelecomCustomerProvisioning,
        ];

        foreach (var key in keys)
        {
            if (await _permissions.HasPermissionAsync(userId, key, cancellationToken))
            {
                return;
            }
        }

        throw new BusinessRuleViolationException(
            "ليس لديك صلاحية إنشاء طلب تغيير الرقم (telecom.line.change_number_request).");
    }

    private static string? BuildNotes(CreateTelecomOperationRequest request)
    {
        var existing = (request.Notes ?? string.Empty).Trim();
        if (existing.StartsWith("Customer360|", StringComparison.OrdinalIgnoreCase))
        {
            return existing.Length > 0 ? existing : null;
        }

        string? msisdnLabel = null;
        if (!string.IsNullOrWhiteSpace(request.MsisdnAssetId))
        {
            msisdnLabel = request.MsisdnAssetId.Trim();
        }

        var auditPrefix = $"Customer360|{request.Kind}|{msisdnLabel ?? "—"}";
        if (string.IsNullOrEmpty(existing))
        {
            return auditPrefix;
        }

        return $"{auditPrefix} | {existing}";
    }

    private async Task ValidateNewActivationLineCapAsync(
        CreateTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var maxLines = await _globalSettings.GetIntAsync(
            GlobalSettingKeys.TelecomMaxActiveLinesPerIndividual,
            defaultValue: 5,
            min: 1,
            max: 20,
            cancellationToken);

        var customerId = await _queryContext.SubscriberProfile.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == request.SubscriberProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(customerId))
        {
            return;
        }

        var isIndividual = await _queryContext.Customer.AsNoTracking()
            .Where(c => !c.IsDeleted && c.Id == customerId)
            .Select(c => c.CustomerKind)
            .FirstOrDefaultAsync(cancellationToken) == CustomerKind.Individual;

        if (!isIndividual)
        {
            return;
        }

        var activeLineCount = await (
            from s in _queryContext.TelecomSubscription.AsNoTracking()
            join m in _queryContext.MsisdnAsset.AsNoTracking() on s.MsisdnAssetId equals m.Id
            join p in _queryContext.SubscriberProfile.AsNoTracking() on s.SubscriberProfileId equals p.Id
            where !s.IsDeleted && !m.IsDeleted && p.CustomerId == customerId
                  && m.PoolStatus == MsisdnPoolStatus.Active
            select s.Id
        ).CountAsync(cancellationToken);

        if (activeLineCount >= maxLines)
        {
            throw new BusinessRuleViolationException(
                $"تجاوز الحد التنظيمي للخطوط النشطة ({maxLines}) لهذا العميل.");
        }
    }
}
