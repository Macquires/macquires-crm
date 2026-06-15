using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.SellingLine;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.Confirm;

public static class TelecomOperationConfirmPermissionResolver
{
    public static IEnumerable<string> ResolvePermissionKeys(TelecomOperationRequest operation)
    {
        if (operation.Kind == TelecomOperationKind.SimSwap && operation.IsLostOrStolenReport)
        {
            yield return PermissionCatalog.TelecomLineSimSwapApprove;
            yield return PermissionCatalog.TelecomLineSimSwap;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.SimSwap)
        {
            yield return PermissionCatalog.TelecomLineSimSwap;
            yield return PermissionCatalog.TelecomLineSimSwapApprove;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.NumberPortability
            && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))
        {
            yield return PermissionCatalog.TelecomLineChangeNumberApprove;
            yield return PermissionCatalog.TelecomLineChangeNumber;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.NumberPortability)
        {
            yield return PermissionCatalog.TelecomLineChangeNumber;
            yield return PermissionCatalog.TelecomLineChangeNumberApprove;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.Termination
            && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))
        {
            yield return PermissionCatalog.TelecomLineTerminationApprove;
            yield return PermissionCatalog.TelecomLineTermination;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.Termination)
        {
            yield return PermissionCatalog.TelecomLineTermination;
            yield return PermissionCatalog.TelecomLineTerminationApprove;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.TemporarySuspension
            && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))
        {
            yield return PermissionCatalog.TelecomLineSuspensionApprove;
            yield return PermissionCatalog.TelecomLineSuspension;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.TemporarySuspension)
        {
            yield return PermissionCatalog.TelecomLineSuspension;
            yield return PermissionCatalog.TelecomLineSuspensionApprove;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.Reconnect
            && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))
        {
            yield return PermissionCatalog.TelecomLineReconnectApprove;
            yield return PermissionCatalog.TelecomLineReconnect;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.Reconnect)
        {
            yield return PermissionCatalog.TelecomLineReconnect;
            yield return PermissionCatalog.TelecomLineReconnectApprove;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.DepositRefundSettlement
            && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))
        {
            yield return PermissionCatalog.TelecomLineRefundApprove;
            yield return PermissionCatalog.TelecomLineRefund;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.DepositRefundSettlement)
        {
            yield return PermissionCatalog.TelecomLineRefund;
            yield return PermissionCatalog.TelecomLineRefundApprove;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.BadDebtRecovery
            && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.Ordinal))
        {
            yield return PermissionCatalog.TelecomLineCollectionApprove;
            yield return PermissionCatalog.TelecomLineCollection;
            yield break;
        }

        if (operation.Kind == TelecomOperationKind.BadDebtRecovery)
        {
            yield return PermissionCatalog.TelecomLineCollection;
            yield return PermissionCatalog.TelecomLineCollectionApprove;
            yield break;
        }

        yield return TelecomOperationPermissionResolver.PermissionKeyForKind(operation.Kind);
    }
}

public interface IConfirmTelecomOperationPermissionGate
{
    Task EnsureCanConfirmAsync(TelecomOperationRequest operation, CancellationToken cancellationToken);
}

public sealed class ConfirmTelecomOperationPermissionGate : IConfirmTelecomOperationPermissionGate
{
    private readonly IOperatorContext _operator;
    private readonly IPermissionEvaluator _permissions;

    public ConfirmTelecomOperationPermissionGate(IOperatorContext operatorContext, IPermissionEvaluator permissions)
    {
        _operator = operatorContext;
        _permissions = permissions;
    }

    public async Task EnsureCanConfirmAsync(TelecomOperationRequest operation, CancellationToken cancellationToken)
    {
        if (!_operator.IsAuthenticated || string.IsNullOrEmpty(_operator.UserId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لتنفيذ هذه العملية.");
        }

        foreach (var key in TelecomOperationConfirmPermissionResolver.ResolvePermissionKeys(operation))
        {
            if (await _permissions.HasPermissionAsync(_operator.UserId, key, cancellationToken))
            {
                return;
            }
        }

        throw new BusinessRuleViolationException("ليس لديك صلاحية لتنفيذ هذه العملية.");
    }
}

public interface IConfirmTelecomOperationSellingLineOverrideApplier
{
    Task ApplyAsync(
        TelecomOperationRequest operation,
        string operationId,
        string overrideReasonCode,
        CancellationToken cancellationToken);
}

public sealed class ConfirmTelecomOperationSellingLineOverrideApplier : IConfirmTelecomOperationSellingLineOverrideApplier
{
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public ConfirmTelecomOperationSellingLineOverrideApplier(
        ICommandRepository<TelecomOperationRequest> operationRepository,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _operationRepository = operationRepository;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task ApplyAsync(
        TelecomOperationRequest operation,
        string operationId,
        string overrideReasonCode,
        CancellationToken cancellationToken)
    {
        if (operation.Kind != TelecomOperationKind.NewActivation)
        {
            throw new BusinessRuleViolationException("سبب الاستثناء متاح لعمليات تفعيل خط جديد فقط.");
        }

        var canOverride = _operator.Roles.Any(r =>
            r.Equals("TelecomBackOffice", StringComparison.OrdinalIgnoreCase)
            || r.Equals("TelecomAdmin", StringComparison.OrdinalIgnoreCase));
        if (!canOverride)
        {
            throw new BusinessRuleViolationException("تسجيل سبب الاستثناء متاح لموظفي الباك أوفيس فقط.");
        }

        var tracked = await _operationRepository.GetAsync(operationId, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        SellingLineOperationMutationGuard.EnsureEditableInventoryFields(
            tracked, tracked.MsisdnAssetId, tracked.SimInventoryId, tracked.ProductOfferingId, tracked.ProductId);

        tracked.OverrideReasonCode = overrideReasonCode.Trim();
        tracked.UpdatedById = OperatorActor.RequireUserId(_operator);
        _operationRepository.Update(tracked);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}

public static class ConfirmTelecomOperationStatusHints
{
    public static (string HintAr, string HintEn) Map(
        TelecomActivationWorkflowResult result,
        TelecomOperationStatus status)
    {
        var (hintAr, hintEn) = status switch
        {
            TelecomOperationStatus.Completed => (
                "تم تسجيل العملية واكتمل التزامن مع CBS وHLR.",
                "Operation recorded; CBS and HLR sync completed."),
            TelecomOperationStatus.PendingExternal => (
                "تم تسجيل العملية؛ المزامنة مع CBS/HLR قيد الانتظار (شبكة أو إعدادات تكامل).",
                "Operation recorded; CBS/HLR sync is pending (network or integration settings)."),
            TelecomOperationStatus.Provisioning => (
                "تم تأكيد CBS؛ جاري تزويد الشبكة (HLR) في الخلفية.",
                "CBS confirmed; network provisioning (HLR) is running in the background."),
            TelecomOperationStatus.Scheduled => (
                result.MessageAr ?? "تم جدولة العملية للتنفيذ في تاريخ السريان.",
                result.MessageEn ?? "Operation scheduled for the effective date."),
            TelecomOperationStatus.Failed => (
                result.MessageAr ?? result.Message,
                result.MessageEn ?? result.Message),
            _ => ("تم استلام الطلب.", "Request received."),
        };

        if (status == TelecomOperationStatus.Failed
            && string.IsNullOrWhiteSpace(result.MessageAr)
            && string.IsNullOrWhiteSpace(result.MessageEn))
        {
            hintAr = "فشلت العملية — راجع سجل التكامل أو أعد المحاولة من الباك أوفيس.";
            hintEn = "Operation failed — review the integration log or retry from back office.";
        }

        return (hintAr, hintEn);
    }
}
