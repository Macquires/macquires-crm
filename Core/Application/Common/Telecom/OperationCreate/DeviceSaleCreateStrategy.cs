using Application.Common.Exceptions;
using Application.Common.Telecom.DeviceSales;
using Domain.Enums;

namespace Application.Common.Telecom.OperationCreate;

public sealed class DeviceSaleCreateStrategy : IOperationCreateStrategy
{
    private readonly IDeviceSalesEligibilityChecker _eligibility;

    public DeviceSaleCreateStrategy(IDeviceSalesEligibilityChecker eligibility)
    {
        _eligibility = eligibility;
    }

    public TelecomOperationKind Kind => TelecomOperationKind.DeviceSale;

    public OperationCreatePostCreateFlags PostCreateFlags =>
        OperationCreatePostCreateFlags.ReserveDeviceInventory;

    public async Task ValidateForCreateAsync(OperationCreateContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;

        var result = await _eligibility.ValidateForCreateAsync(
            request.SubscriberProfileId,
            request.DeviceInventoryId!,
            request.DeviceSaleType!.Value,
            request.DeviceInstallmentPlanId,
            excludeOperationId: null,
            cancellationToken: cancellationToken);

        if (!result.Allowed)
        {
            throw new BusinessRuleViolationException(result.MessageAr);
        }

        context.DeviceSalesEligibility = result;
    }

    public Task ValidateCatalogAsync(OperationCreateContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ApplyToEntityAsync(OperationCreateBuildContext build, CancellationToken cancellationToken)
    {
        var eligibility = build.Validation.DeviceSalesEligibility;
        if (eligibility == null)
        {
            return Task.CompletedTask;
        }

        var request = build.Validation.Request;
        var entity = build.Entity;

        entity.DeviceInventoryId = eligibility.DeviceInventoryId ?? request.DeviceInventoryId!.Trim();
        entity.DeviceSaleType = request.DeviceSaleType;
        entity.InstallmentPlanId = request.DeviceInstallmentPlanId?.Trim();
        entity.DeviceFinancingDecision = eligibility.FinancingDecision;
        entity.DeviceApprovalLevelRequired = eligibility.ApprovalLevelRequired;
        entity.DeviceFinancingNoteAr = eligibility.FinancingNoteAr;
        entity.DeviceDownPaymentAmount = eligibility.RequiredDownPayment;
        entity.DeviceMonthlyInstallmentAmount = eligibility.MonthlyInstallment;
        entity.DeviceCreditScoreSnapshot = eligibility.CreditScoreSnapshot;
        entity.ProvisioningResult = "Pending";
        if (eligibility.RequiresFinanceApproval)
        {
            entity.ApprovalLevelRequired = eligibility.ApprovalLevelRequired;
        }

        entity.Notes = string.IsNullOrEmpty(entity.Notes)
            ? eligibility.MessageAr
            : $"{entity.Notes}\n{eligibility.MessageAr}";
        entity.DeviceSaleEffectiveDateUtc = request.DeviceSaleEffectiveDateUtc ?? DateTime.UtcNow;
        return Task.CompletedTask;
    }
}
