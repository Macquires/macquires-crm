using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.DeviceSales;

public sealed record DeviceSalesEligibilityResult(
    bool Allowed,
    string MessageAr,
    string? DeviceInventoryId = null,
    DeviceFinancingDecision? FinancingDecision = null,
    string? ApprovalLevelRequired = null,
    string? FinancingNoteAr = null,
    decimal? RequiredDownPayment = null,
    decimal? MonthlyInstallment = null,
    int? CreditScoreSnapshot = null,
    bool RequiresFinanceApproval = false);

public interface IDeviceSalesEligibilityChecker
{
    Task<DeviceSalesEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string deviceInventoryId,
        DeviceSaleType saleType,
        string? installmentPlanId,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default);

    Task<DeviceSalesEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}
