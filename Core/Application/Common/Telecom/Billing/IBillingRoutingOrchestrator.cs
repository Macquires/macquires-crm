using Application.Common.Integrations;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.Billing;

/// <summary>
/// Convergent charging router — enforces IN (prepaid real-time) vs CBS (postpaid ledger)
/// and synchronized hybrid orchestration with CorrelationId-backed idempotency.
/// </summary>
public interface IBillingRoutingOrchestrator
{
    BillingRoutingDecision ResolveProvisionRouting(
        TelecomOperationKind kind,
        string? subscriptionTypeCode,
        string? sourceSubscriptionTypeCode = null,
        string? targetSubscriptionTypeCode = null);

    bool RoutesPrimaryThroughIn(TelecomOperationKind kind, string? subscriptionTypeCode);

    string IntegrationFalloutSource(TelecomOperationKind kind, string? subscriptionTypeCode);

    Task<BillingProvisionResult> ProvisionAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        BillingProvisionRequest billingRequest,
        CancellationToken cancellationToken = default);

    Task<BillingProvisionResult> CompensateProvisionAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        BillingProvisionRequest billingRequest,
        CancellationToken cancellationToken = default);

    Task<BillingRechargeResult> RechargeAsync(
        string? subscriptionTypeCode,
        BillingRechargeRequest request,
        CancellationToken cancellationToken = default);

    Task<BillingRechargeResult> ReverseRechargeAsync(
        string? subscriptionTypeCode,
        BillingReverseRechargeRequest request,
        CancellationToken cancellationToken = default);
}
