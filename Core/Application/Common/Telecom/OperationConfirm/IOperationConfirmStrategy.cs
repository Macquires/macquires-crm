using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.OperationConfirm;

public interface IOperationConfirmStrategy
{
    TelecomOperationKind Kind { get; }

    Task<OperationConfirmValidationResult> ValidateForConfirmAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken);

    Task<OperationApplyResult?> ApplyLocalChangesAsync(
        TelecomOperationRequest entity,
        string? actorUserId,
        CancellationToken cancellationToken);
}

public sealed record OperationConfirmValidationResult(bool Allowed, string MessageAr);

public sealed record OperationApplyResult(
    string? Msisdn,
    string? Iccid,
    string? CustomerId,
    string? SubscriberProfileId,
    string? TelecomSubscriptionId,
    bool RequiresBilling = true);
