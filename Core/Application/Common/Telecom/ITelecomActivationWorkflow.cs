using Application.Common.Integrations;
using Domain.Entities;

namespace Application.Common.Telecom;

public sealed record TelecomActivationWorkflowResult(
    TelecomOperationRequest Operation,
    BillingProvisionResult BillingResult,
    NetworkProvisionResult? NetworkResult,
    bool IdempotentReplay,
    string Message);

public interface ITelecomActivationWorkflow
{
    Task<TelecomActivationWorkflowResult> ConfirmActivationAsync(
        string operationId,
        string? actorUserId,
        CancellationToken cancellationToken);
}
