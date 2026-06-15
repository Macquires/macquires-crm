using Application.Common.Integrations;
using Domain.Entities;

namespace Application.Common.Telecom;

public sealed record TelecomActivationWorkflowResult(
    TelecomOperationRequest Operation,
    BillingProvisionResult BillingResult,
    NetworkProvisionResult? NetworkResult,
    bool IdempotentReplay,
    string Message,
    string? MessageAr = null,
    string? MessageEn = null);

public interface ITelecomActivationWorkflow
{
    Task<TelecomActivationWorkflowResult> ConfirmActivationAsync(
        string operationId,
        string? actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Runs CBS/HLR for operations in <see cref="TelecomOperationStatus.Scheduled"/> once due.</summary>
    Task<TelecomActivationWorkflowResult> ExecuteScheduledOperationAsync(
        string operationId,
        string? actorUserId,
        CancellationToken cancellationToken);
}
