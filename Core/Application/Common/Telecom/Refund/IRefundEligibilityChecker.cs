using Domain.Entities;

namespace Application.Common.Telecom.Refund;

public sealed record RefundEligibilityResult(
    bool Allowed,
    string MessageAr,
    string? CustomerId,
    string? Msisdn,
    string? MsisdnAssetId,
    decimal DepositBalanceSnapshot,
    decimal WalletBalanceSnapshot,
    bool RequiresDualApproval,
    bool RequiresBackOfficeApproval,
    string OutcomeCode);

public interface IRefundEligibilityChecker
{
    Task<RefundEligibilityResult> ValidateForCreateAsync(
        string subscriberProfileId,
        string msisdnAssetId,
        string refundType,
        string refundMethod,
        decimal refundAmount,
        string refundReason,
        string? excludeOperationId = null,
        CancellationToken cancellationToken = default);

    Task<RefundEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}
