using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Integrations;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Common.Telecom.Billing;

public sealed class BillingRoutingOrchestrator : IBillingRoutingOrchestrator
{
    private readonly IBillingSystemIntegration _billing;
    private readonly IIntelligentNetworkService _intelligentNetwork;
    private readonly IQueryContext _query;
    private readonly ILogger<BillingRoutingOrchestrator> _logger;

    public BillingRoutingOrchestrator(
        IBillingSystemIntegration billing,
        IIntelligentNetworkService intelligentNetwork,
        IQueryContext query,
        ILogger<BillingRoutingOrchestrator> logger)
    {
        _billing = billing;
        _intelligentNetwork = intelligentNetwork;
        _query = query;
        _logger = logger;
    }

    public BillingRoutingDecision ResolveProvisionRouting(
        TelecomOperationKind kind,
        string? subscriptionTypeCode,
        string? sourceSubscriptionTypeCode = null,
        string? targetSubscriptionTypeCode = null) =>
        BillingLineTypeResolver.ResolveProvisionRouting(
            kind,
            subscriptionTypeCode,
            sourceSubscriptionTypeCode,
            targetSubscriptionTypeCode);

    public bool RoutesPrimaryThroughIn(TelecomOperationKind kind, string? subscriptionTypeCode)
    {
        var decision = ResolveProvisionRouting(kind, subscriptionTypeCode);
        return decision.UsesIn && !decision.UsesCbs;
    }

    public string IntegrationFalloutSource(TelecomOperationKind kind, string? subscriptionTypeCode)
    {
        var decision = ResolveProvisionRouting(kind, subscriptionTypeCode);
        return BillingLineTypeResolver.IntegrationFalloutSource(decision);
    }

    public async Task<BillingProvisionResult> ProvisionAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        BillingProvisionRequest billingRequest,
        CancellationToken cancellationToken = default)
    {
        operation.CorrelationId ??= Guid.CreateVersion7().ToString();
        billingRequest = billingRequest with { CorrelationId = operation.CorrelationId };

        var decision = ResolveProvisionRouting(
            operation.Kind,
            lineContext.SubscriptionTypeCode,
            lineContext.SourceSubscriptionTypeCode,
            lineContext.TargetSubscriptionTypeCode);

        _logger.LogInformation(
            "Billing routing {Kind} MSISDN={Msisdn} line={LineType} channels={Channels} correlation={CorrelationId}",
            operation.Kind,
            lineContext.Msisdn,
            decision.LineType,
            decision.Channels,
            operation.CorrelationId);

        return operation.Kind switch
        {
            TelecomOperationKind.NewActivation => await ExecuteActivationAsync(
                operation,
                lineContext,
                billingRequest,
                decision,
                cancellationToken),
            TelecomOperationKind.Migration => await ExecuteMigrationAsync(
                operation,
                lineContext,
                billingRequest,
                decision,
                cancellationToken),
            TelecomOperationKind.ChangeGsmType => await ExecuteChangeGsmTypeAsync(
                operation,
                lineContext,
                billingRequest,
                decision,
                cancellationToken),
            TelecomOperationKind.Termination => await ExecuteTerminationAsync(
                lineContext,
                billingRequest,
                decision,
                cancellationToken),
            _ => await ExecuteCbsOnlyAsync(billingRequest, cancellationToken),
        };
    }

    public async Task<BillingProvisionResult> CompensateProvisionAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        BillingProvisionRequest billingRequest,
        CancellationToken cancellationToken = default)
    {
        billingRequest = billingRequest with
        {
            CorrelationId = operation.CorrelationId,
            Phase = TelecomBillingProvisionPhase.Reverse,
        };

        var decision = ResolveProvisionRouting(
            operation.Kind,
            lineContext.SubscriptionTypeCode,
            lineContext.SourceSubscriptionTypeCode,
            lineContext.TargetSubscriptionTypeCode);

        if (decision.UsesIn && !string.IsNullOrEmpty(lineContext.Msisdn))
        {
            var inResult = await _intelligentNetwork.UpdatePrepaidLifecycleStateAsync(
                lineContext.Msisdn,
                "Quarantined",
                cancellationToken);

            if (!decision.UsesCbs)
            {
                return inResult.Success || inResult.QueuedForSync
                    ? new BillingProvisionResult(true, inResult.Message, inResult.QueuedForSync)
                    : new BillingProvisionResult(false, inResult.Message);
            }
        }

        if (decision.UsesCbs)
        {
            return await _billing.ReverseProvisionAsync(billingRequest, cancellationToken);
        }

        return new BillingProvisionResult(true, "No billing compensation required.");
    }

    public async Task<BillingRechargeResult> RechargeAsync(
        string? subscriptionTypeCode,
        BillingRechargeRequest request,
        CancellationToken cancellationToken = default)
    {
        var lineType = BillingLineTypeResolver.FromCode(subscriptionTypeCode);

        return lineType switch
        {
            BillingLineType.Prepaid => await RechargeViaInAsync(request, cancellationToken),
            BillingLineType.Postpaid => await _billing.RechargeAsync(request, cancellationToken),
            BillingLineType.Hybrid => await RechargeHybridAsync(request, cancellationToken),
            _ => await RechargeViaInAsync(request, cancellationToken),
        };
    }

    public async Task<BillingRechargeResult> ReverseRechargeAsync(
        string? subscriptionTypeCode,
        BillingReverseRechargeRequest request,
        CancellationToken cancellationToken = default)
    {
        var lineType = BillingLineTypeResolver.FromCode(subscriptionTypeCode);

        return lineType switch
        {
            BillingLineType.Prepaid => await ReverseRechargeViaInAsync(request, cancellationToken),
            BillingLineType.Postpaid => await _billing.ReverseRechargeAsync(request, cancellationToken),
            BillingLineType.Hybrid => await ReverseRechargeHybridAsync(request, cancellationToken),
            _ => await ReverseRechargeViaInAsync(request, cancellationToken),
        };
    }

    private async Task<BillingProvisionResult> ExecuteActivationAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        BillingProvisionRequest billingRequest,
        BillingRoutingDecision decision,
        CancellationToken cancellationToken)
    {
        if (decision.LineType == BillingLineType.Prepaid)
        {
            return await ExecuteInActivationAsync(operation, lineContext, cancellationToken);
        }

        if (decision.LineType == BillingLineType.Hybrid)
        {
            return await ExecuteHybridActivationAsync(operation, lineContext, billingRequest, cancellationToken);
        }

        return await ExecuteCbsOnlyAsync(billingRequest, cancellationToken);
    }

    private async Task<BillingProvisionResult> ExecuteHybridActivationAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        BillingProvisionRequest billingRequest,
        CancellationToken cancellationToken)
    {
        var cbsResult = await _billing.ProvisionAsync(billingRequest, cancellationToken);
        if (!cbsResult.Success)
        {
            return cbsResult;
        }

        var inResult = await ExecuteInActivationAsync(operation, lineContext, cancellationToken);
        if (!inResult.Success)
        {
            await _billing.ReverseProvisionAsync(
                billingRequest with { Phase = TelecomBillingProvisionPhase.Reverse },
                cancellationToken);
            return inResult;
        }

        return new BillingProvisionResult(
            true,
            $"Hybrid sync: CBS + IN ({inResult.Message})",
            cbsResult.QueuedForSync || inResult.QueuedForSync);
    }

    private async Task<BillingProvisionResult> ExecuteInActivationAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        CancellationToken cancellationToken)
    {
        var msisdn = lineContext.Msisdn
            ?? throw new BusinessRuleViolationException("رقم MSISDN مطلوب لتفعيل خط الشحن عبر IN.");

        var imsi = lineContext.Imsi ?? string.Empty;
        var profileId = lineContext.ProductServiceCode ?? "PREPAID_DEFAULT";

        var provision = await _intelligentNetwork.ProvisionPrepaidSubscriberAsync(
            msisdn,
            imsi,
            profileId,
            cancellationToken);

        if (!provision.Success)
        {
            return new BillingProvisionResult(false, provision.Message, provision.QueuedForSync);
        }

        if (lineContext.InitialDeposit is > 0)
        {
            var credit = await _intelligentNetwork.CreditPrepaidBalanceAsync(
                msisdn,
                lineContext.InitialDeposit.Value,
                operation.PaymentReference ?? operation.CorrelationId ?? operation.Id,
                cancellationToken);

            if (!credit.Success)
            {
                await _intelligentNetwork.UpdatePrepaidLifecycleStateAsync(msisdn, "Quarantined", cancellationToken);
                return new BillingProvisionResult(false, credit.Message);
            }
        }

        return new BillingProvisionResult(true, provision.Message, provision.QueuedForSync);
    }

    private async Task<BillingProvisionResult> ExecuteMigrationAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        BillingProvisionRequest billingRequest,
        BillingRoutingDecision decision,
        CancellationToken cancellationToken)
    {
        var fee = await ResolveMigrationFeeAsync(operation, cancellationToken);

        if (decision.LineType == BillingLineType.Prepaid)
        {
            if (fee > 0 && !string.IsNullOrEmpty(lineContext.Msisdn))
            {
                var debit = await _intelligentNetwork.DebitPrepaidBalanceAsync(
                    lineContext.Msisdn,
                    fee,
                    $"MGR_{operation.CorrelationId}",
                    cancellationToken);

                if (!debit.Success)
                {
                    return new BillingProvisionResult(false, debit.Message);
                }
            }

            return new BillingProvisionResult(true, "Prepaid migration fee charged via IN.");
        }

        if (decision.LineType == BillingLineType.Hybrid)
        {
            var cbsResult = await _billing.ProvisionAsync(billingRequest, cancellationToken);
            if (!cbsResult.Success)
            {
                return cbsResult;
            }

            if (!string.IsNullOrEmpty(lineContext.Msisdn))
            {
                var sync = await _intelligentNetwork.ProvisionPrepaidSubscriberAsync(
                    lineContext.Msisdn,
                    lineContext.Imsi ?? string.Empty,
                    lineContext.ProductServiceCode ?? "HYBRID_MIGRATION",
                    cancellationToken);

                if (!sync.Success && !sync.QueuedForSync)
                {
                    await _billing.ReverseProvisionAsync(
                        billingRequest with { Phase = TelecomBillingProvisionPhase.Reverse },
                        cancellationToken);
                    return new BillingProvisionResult(false, sync.Message);
                }
            }

            return cbsResult;
        }

        return await ExecuteCbsOnlyAsync(billingRequest, cancellationToken);
    }

    private async Task<BillingProvisionResult> ExecuteChangeGsmTypeAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        BillingProvisionRequest billingRequest,
        BillingRoutingDecision decision,
        CancellationToken cancellationToken)
    {
        if (BillingLineTypeResolver.IsPrepaidToPostpaid(
                lineContext.SourceSubscriptionTypeCode,
                lineContext.TargetSubscriptionTypeCode)
            && !string.IsNullOrEmpty(lineContext.Msisdn))
        {
            await LiquidateInWalletAsync(lineContext.Msisdn, operation.CorrelationId ?? operation.Id, cancellationToken);
        }

        var cbsResult = await _billing.ProvisionAsync(billingRequest, cancellationToken);
        if (!cbsResult.Success)
        {
            return cbsResult;
        }

        if (decision.UsesIn
            && BillingLineTypeResolver.IsPostpaid(lineContext.TargetSubscriptionTypeCode) == false
            && BillingLineTypeResolver.IsHybrid(lineContext.TargetSubscriptionTypeCode)
            && !string.IsNullOrEmpty(lineContext.Msisdn))
        {
            var inSync = await _intelligentNetwork.ProvisionPrepaidSubscriberAsync(
                lineContext.Msisdn,
                lineContext.Imsi ?? string.Empty,
                lineContext.ProductServiceCode ?? "HYBRID_DEFAULT",
                cancellationToken);

            if (!inSync.Success && !inSync.QueuedForSync)
            {
                await _billing.ReverseProvisionAsync(
                    billingRequest with { Phase = TelecomBillingProvisionPhase.Reverse },
                    cancellationToken);
                return new BillingProvisionResult(false, inSync.Message);
            }
        }

        return cbsResult;
    }

    private async Task<BillingProvisionResult> ExecuteTerminationAsync(
        TelecomLineProvisionContext lineContext,
        BillingProvisionRequest billingRequest,
        BillingRoutingDecision decision,
        CancellationToken cancellationToken)
    {
        if (decision.LineType == BillingLineType.Prepaid)
        {
            return await HaltInRatingAsync(lineContext.Msisdn, cancellationToken);
        }

        if (decision.LineType == BillingLineType.Hybrid)
        {
            var cbsResult = await _billing.ProvisionAsync(billingRequest, cancellationToken);
            if (!cbsResult.Success)
            {
                return cbsResult;
            }

            var inResult = await HaltInRatingAsync(lineContext.Msisdn, cancellationToken);
            if (!inResult.Success && !inResult.QueuedForSync)
            {
                await _billing.ReverseProvisionAsync(
                    billingRequest with { Phase = TelecomBillingProvisionPhase.Reverse },
                    cancellationToken);
            }

            return inResult;
        }

        return await _billing.ProvisionAsync(billingRequest, cancellationToken);
    }

    private async Task<BillingProvisionResult> HaltInRatingAsync(
        string? msisdn,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(msisdn))
        {
            return new BillingProvisionResult(true, "IN lifecycle halt skipped — no MSISDN.");
        }

        var lifecycle = await _intelligentNetwork.UpdatePrepaidLifecycleStateAsync(
            msisdn,
            "Quarantined",
            cancellationToken);

        return lifecycle.Success || lifecycle.QueuedForSync
            ? new BillingProvisionResult(true, lifecycle.Message, lifecycle.QueuedForSync)
            : new BillingProvisionResult(false, lifecycle.Message);
    }

    private async Task LiquidateInWalletAsync(
        string msisdn,
        string transactionId,
        CancellationToken cancellationToken)
    {
        var balance = await _intelligentNetwork.GetPrepaidBalanceAsync(msisdn, cancellationToken);
        if (balance.Success && balance.BalanceAfter is > 0)
        {
            await _intelligentNetwork.DebitPrepaidBalanceAsync(
                msisdn,
                balance.BalanceAfter.Value,
                $"CGT_LIQUIDATE_{transactionId}",
                cancellationToken);
        }

        await _intelligentNetwork.UpdatePrepaidLifecycleStateAsync(
            msisdn,
            "MigratedToPostpaid",
            cancellationToken);
    }

    private Task<BillingProvisionResult> ExecuteCbsOnlyAsync(
        BillingProvisionRequest billingRequest,
        CancellationToken cancellationToken) =>
        _billing.ProvisionAsync(billingRequest, cancellationToken);

    private async Task<decimal> ResolveMigrationFeeAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(operation.ProductId))
        {
            return 0m;
        }

        var unitPrice = await _query.Product.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == operation.ProductId)
            .Select(p => p.UnitPrice)
            .FirstOrDefaultAsync(cancellationToken);

        return unitPrice is > 0 ? (decimal)unitPrice : 0m;
    }

    private async Task<BillingRechargeResult> RechargeViaInAsync(
        BillingRechargeRequest request,
        CancellationToken cancellationToken)
    {
        var credit = await _intelligentNetwork.CreditPrepaidBalanceAsync(
            request.Msisdn,
            request.Amount,
            request.CorrelationId ?? request.PaymentTransactionId,
            cancellationToken);

        return credit.Success
            ? new BillingRechargeResult(true, credit.Message, credit.BalanceAfter)
            : new BillingRechargeResult(false, credit.Message);
    }

    private async Task<BillingRechargeResult> RechargeHybridAsync(
        BillingRechargeRequest request,
        CancellationToken cancellationToken)
    {
        var cbsResult = await _billing.RechargeAsync(request, cancellationToken);
        if (!cbsResult.Success)
        {
            return cbsResult;
        }

        var inResult = await RechargeViaInAsync(request, cancellationToken);
        if (!inResult.Success)
        {
            await _billing.ReverseRechargeAsync(
                new BillingReverseRechargeRequest(
                    request.PaymentTransactionId,
                    request.PaymentNumber,
                    request.Msisdn,
                    request.Amount,
                    request.CorrelationId),
                cancellationToken);
            return inResult;
        }

        return new BillingRechargeResult(
            true,
            $"Hybrid recharge: CBS + IN ({inResult.Message})",
            inResult.NewBalance ?? cbsResult.NewBalance);
    }

    private async Task<BillingRechargeResult> ReverseRechargeViaInAsync(
        BillingReverseRechargeRequest request,
        CancellationToken cancellationToken)
    {
        var debit = await _intelligentNetwork.DebitPrepaidBalanceAsync(
            request.Msisdn,
            request.Amount,
            $"REVERSE_{request.CorrelationId ?? request.PaymentTransactionId}",
            cancellationToken);

        return debit.Success
            ? new BillingRechargeResult(true, debit.Message, debit.BalanceAfter)
            : new BillingRechargeResult(false, debit.Message);
    }

    private async Task<BillingRechargeResult> ReverseRechargeHybridAsync(
        BillingReverseRechargeRequest request,
        CancellationToken cancellationToken)
    {
        var inReverse = await ReverseRechargeViaInAsync(request, cancellationToken);
        if (!inReverse.Success)
        {
            return inReverse;
        }

        return await _billing.ReverseRechargeAsync(request, cancellationToken);
    }
}
