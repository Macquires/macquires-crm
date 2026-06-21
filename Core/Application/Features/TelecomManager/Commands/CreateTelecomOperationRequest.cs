using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.ChangeNumber;
using Application.Common.Telecom.OperationCreate;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.Refund;
using Application.Common.Telecom.Suspension;
using Application.Common.Telecom.Termination;
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
public class CreateTelecomOperationRequest : IRequest<CreateTelecomOperationRequestResult>, IRequireAnyPermission
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

    /// <summary>§11 MGR — scheduled effective date (UTC).</summary>
    public DateTime? MigrationEffectiveDateUtc { get; init; }

    public string? SimInventoryId { get; init; }

    /// <summary>Resolves <see cref="SimInventoryId"/> when set (available SIM in pool).</summary>
    public string? SimIccid { get; init; }

    public ActivationChannel? ActivationChannel { get; set; }

    public string? DealerCode { get; init; }

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

    /// <summary>§5 Internal (CNR) vs Port-In (MNP).</summary>
    public string? NumberChangeMode { get; init; }

    /// <summary>§5 MNP — MSISDN to port from donor operator.</summary>
    public string? PortInMsisdn { get; init; }

    /// <summary>§5 MNP — donor operator code.</summary>
    public string? DonorOperatorCode { get; init; }

    /// <summary>§5 Premium fee (VAL-05-02).</summary>
    public decimal? PremiumFeeAmount { get; init; }

    public DateTime? NumberChangeEffectiveDateUtc { get; init; }

    public DateTime? SimSwapEffectiveDateUtc { get; init; }

    /// <summary>§10 Termination — Voluntary, Collections, Regulatory, Fraud.</summary>
    public string? TerminationType { get; init; }

    /// <summary>§10 Termination — reason.</summary>
    public string? TerminationReason { get; init; }

    public DateTime? TerminationEffectiveDateUtc { get; init; }

    public DateTime? ActivationEffectiveDateUtc { get; init; }

    public DateTime? ReconnectEffectiveDateUtc { get; init; }

    public DateTime? RefundEffectiveDateUtc { get; init; }

    public DateTime? BadDebtEffectiveDateUtc { get; init; }

    public DateTime? DeviceSaleEffectiveDateUtc { get; init; }

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

    public string? RefundCbsReference { get; init; }

    public string? RefundGatewayReference { get; init; }

    /// <summary>§16 BDR.</summary>
    public string? CollectionAction { get; init; }

    public string? DunningStage { get; init; }

    public decimal? CollectedAmount { get; init; }

    public decimal? WriteOffAmount { get; init; }

    public string? AgencyReference { get; init; }

    public int? PaymentPlanMonths { get; init; }

    public string? CollectionNote { get; init; }

    public bool CollectionApprovalConfirmed { get; init; }

    /// <summary>KYC vault document reference (required for new line activation).</summary>
    public string? KycDocumentReferenceId { get; init; }

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.CreateAny;
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
        RuleFor(x => x.KycDocumentReferenceId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.NewActivation)
            .WithMessage(TelecomUserMessages.ValAct12KycRequired.Ar);
        RuleFor(x => x.ActivationChannel)
            .NotNull()
            .When(x => x.Kind == TelecomOperationKind.NewActivation);
        RuleFor(x => x.DealerCode)
            .NotEmpty()
            .MaximumLength(64)
            .When(x => x.Kind == TelecomOperationKind.NewActivation
                       && (x.ActivationChannel ?? ActivationChannel.Showroom) == ActivationChannel.Dealer)
            .WithMessage("VAL-ACT-09: Dealer Code is missing for Dealer Channel.");
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.ChangeGsmType);
        RuleFor(x => x.TargetSubscriptionTypeId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.ChangeGsmType);
        RuleFor(x => x.TargetSubscriptionTypeId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.NewActivation);
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
        RuleFor(x => x.PaymentReference)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.TakeOver
                       && string.Equals(x.TakeOverObligationStatus, "Settled", StringComparison.OrdinalIgnoreCase));
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
        RuleFor(x => x.AgencyReference)
            .NotEmpty()
            .MaximumLength(128)
            .When(x => x.Kind == TelecomOperationKind.SimSwap && x.IsLostOrStolenReport);
        RuleFor(x => x.MsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.NumberPortability);
        RuleFor(x => x.TargetMsisdnAssetId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.NumberPortability
                       && !ChangeNumberWellKnown.IsPortInMode(x.NumberChangeMode));
        RuleFor(x => x.PortInMsisdn)
            .NotEmpty()
            .MaximumLength(32)
            .When(x => x.Kind == TelecomOperationKind.NumberPortability
                       && ChangeNumberWellKnown.IsPortInMode(x.NumberChangeMode));
        RuleFor(x => x.DonorOperatorCode)
            .NotEmpty()
            .MaximumLength(32)
            .When(x => x.Kind == TelecomOperationKind.NumberPortability
                       && ChangeNumberWellKnown.IsPortInMode(x.NumberChangeMode));
        RuleFor(x => x.AgencyReference)
            .NotEmpty()
            .MaximumLength(128)
            .When(x => x.Kind == TelecomOperationKind.NumberPortability
                       && ChangeNumberWellKnown.IsPortInMode(x.NumberChangeMode));
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
        RuleFor(x => x.FraudClearanceConfirmed)
            .Equal(true)
            .When(x => x.Kind == TelecomOperationKind.TemporarySuspension
                       && string.Equals(x.SuspensionType, SuspensionWellKnown.Fraud, StringComparison.OrdinalIgnoreCase))
            .WithMessage("تأكيد المشرف مطلوب لحظر الاحتيال.");
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
        RuleFor(x => x.PaymentReference)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.Reconnect
                       && string.Equals(x.ClearanceType, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.AgencyReference)
            .NotEmpty()
            .MaximumLength(128)
            .When(x => x.Kind == TelecomOperationKind.Reconnect
                       && string.Equals(x.ClearanceType, ReconnectWellKnown.Fraud, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.CollectionNote)
            .NotEmpty()
            .MaximumLength(512)
            .When(x => x.Kind == TelecomOperationKind.Reconnect
                       && string.Equals(x.ClearanceType, ReconnectWellKnown.Regulatory, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.KycDocumentReferenceId)
            .NotEmpty()
            .MaximumLength(50)
            .When(x => x.Kind == TelecomOperationKind.Reconnect
                       && string.Equals(x.ClearanceType, ReconnectWellKnown.Regulatory, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.PaymentReference)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.TemporarySuspension
                       && string.Equals(x.SuspensionType, SuspensionWellKnown.Billing, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.AgencyReference)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.TemporarySuspension
                       && string.Equals(x.SuspensionType, SuspensionWellKnown.Fraud, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.CollectionNote)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.TemporarySuspension
                       && string.Equals(x.SuspensionType, SuspensionWellKnown.Regulatory, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.KycDocumentReferenceId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.TemporarySuspension
                       && string.Equals(x.SuspensionType, SuspensionWellKnown.Regulatory, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.PaymentReference)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.Termination
                       && string.Equals(x.TerminationType, TerminationWellKnown.Collections, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.AgencyReference)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.Termination
                       && string.Equals(x.TerminationType, TerminationWellKnown.Fraud, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.CollectionNote)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.Termination
                       && string.Equals(x.TerminationType, TerminationWellKnown.Regulatory, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.KycDocumentReferenceId)
            .NotEmpty()
            .When(x => x.Kind == TelecomOperationKind.Termination
                       && string.Equals(x.TerminationType, TerminationWellKnown.Regulatory, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.RefundCbsReference)
            .NotEmpty()
            .MaximumLength(128)
            .When(x => x.Kind == TelecomOperationKind.DepositRefundSettlement
                       && (string.Equals(x.RefundType, RefundWellKnown.TypeDeposit, StringComparison.OrdinalIgnoreCase)
                           || string.Equals(x.RefundType, RefundWellKnown.TypeOverpayment, StringComparison.OrdinalIgnoreCase)));
        RuleFor(x => x.RefundGatewayReference)
            .NotEmpty()
            .MaximumLength(128)
            .When(x => x.Kind == TelecomOperationKind.DepositRefundSettlement
                       && (string.Equals(x.RefundMethod, RefundWellKnown.MethodBankTransfer, StringComparison.OrdinalIgnoreCase)
                           || string.Equals(x.RefundType, RefundWellKnown.TypeSyriatelCash, StringComparison.OrdinalIgnoreCase)));
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
    private readonly IOperationCreateStrategyRegistry _createStrategies;
    private readonly IOperationCreatePostCreateService _postCreate;
    private readonly IOperatorContext _operator;

    public CreateTelecomOperationRequestHandler(
        ICommandRepository<TelecomOperationRequest> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService,
        IQueryContext queryContext,
        IOperationCreateStrategyRegistry createStrategies,
        IOperationCreatePostCreateService postCreate,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
        _queryContext = queryContext;
        _createStrategies = createStrategies;
        _postCreate = postCreate;
        _operator = operatorContext;
    }

    public async Task<CreateTelecomOperationRequestResult> Handle(
        CreateTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _operator.UserId
            ?? throw new BusinessRuleViolationException("يجب تسجيل الدخول لإنشاء العملية.");
        var branchId = string.IsNullOrWhiteSpace(_operator.BranchId) ? null : _operator.BranchId.Trim();

        var createContext = new OperationCreateContext(request, actorUserId);
        var createStrategy = _createStrategies.Resolve(request.Kind);
        await createStrategy.ValidateForCreateAsync(createContext, cancellationToken);

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

        createContext.ResolvedOfferingId = resolvedOfferingId;
        createContext.ResolvedProductId = resolvedProductId;
        await createStrategy.ValidateCatalogAsync(createContext, cancellationToken);

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

        var (entityName, prefix) = TelecomNumberSequence.ForKind(request.Kind);
        var number = await _numberSequenceService.GenerateNumberAsync(entityName, prefix, "", useDate: false, cancellationToken: cancellationToken);

        var notes = BuildNotes(request);

        var channel = request.ActivationChannel ?? ActivationChannel.Showroom;
        var dealerCode = string.IsNullOrWhiteSpace(request.DealerCode) ? null : request.DealerCode.Trim();

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
            CreatedById = actorUserId,
            IsLostOrStolenReport = request.IsLostOrStolenReport,
            FraudClearanceConfirmed = false,
            AutoReconnectEnabled = false,
            NotificationSuppressed = false,
        };

        var buildContext = new OperationCreateBuildContext(createContext, entity, simInventoryId, branchId, actorUserId);
        await createStrategy.ApplyToEntityAsync(buildContext, cancellationToken);
        entity.SimInventoryId = string.IsNullOrEmpty(buildContext.SimInventoryId) ? null : buildContext.SimInventoryId;

        if (!string.IsNullOrWhiteSpace(entity.MsisdnAssetId))
        {
            var msisdn = await _queryContext.MsisdnAsset.AsNoTracking()
                .Where(m => !m.IsDeleted && m.Id == entity.MsisdnAssetId)
                .Select(m => m.Msisdn)
                .FirstOrDefaultAsync(cancellationToken);
            if (TelecomDemoMsisdn.IsWellKnown(msisdn ?? string.Empty))
            {
                // National showcase anchors must stay visible in every branch back-office queue.
                entity.BranchId = null;
            }
        }

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _postCreate.RunAsync(entity, actorUserId, cancellationToken);

        return new CreateTelecomOperationRequestResult { Data = entity };
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
}
