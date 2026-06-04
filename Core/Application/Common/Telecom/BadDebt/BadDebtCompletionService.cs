using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.BadDebt;

public sealed class BadDebtCompletionService : IBadDebtCompletionService
{
    private readonly IQueryContext _query;
    private readonly IBillingSystemIntegration _billing;
    private readonly ISmsGatewayIntegration _sms;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;

    public BadDebtCompletionService(
        IQueryContext query,
        IBillingSystemIntegration billing,
        ISmsGatewayIntegration sms,
        ICommandRepository<TelecomOperationRequest> operationRepository)
    {
        _query = query;
        _billing = billing;
        _sms = sms;
        _operationRepository = operationRepository;
    }

    public async Task FulfillAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.BadDebtRecovery)
        {
            return;
        }

        var msisdn = await ResolveMsisdnAsync(operation, cancellationToken);
        var action = (operation.CollectionAction ?? string.Empty).Trim();
        operation.CollectionSettlementStatus = BadDebtWellKnown.SettlementPending;

        var cbsCode = action switch
        {
            _ when string.Equals(action, BadDebtWellKnown.PaymentRecorded, StringComparison.OrdinalIgnoreCase)
                => TelecomBssOperations.CbsPostCollectionPayment,
            _ when string.Equals(action, BadDebtWellKnown.WriteOffPartial, StringComparison.OrdinalIgnoreCase)
                || string.Equals(action, BadDebtWellKnown.WriteOffFull, StringComparison.OrdinalIgnoreCase)
                => TelecomBssOperations.CbsPostWriteOff,
            _ when string.Equals(action, BadDebtWellKnown.DunningEscalation, StringComparison.OrdinalIgnoreCase)
                => TelecomBssOperations.CbsDunningNotify,
            _ when string.Equals(action, BadDebtWellKnown.AgencyReferral, StringComparison.OrdinalIgnoreCase)
                => TelecomBssOperations.CbsDunningNotify,
            _ => TelecomBssOperations.CbsPostCollectionPayment,
        };

        var amount = string.Equals(action, BadDebtWellKnown.WriteOffPartial, StringComparison.OrdinalIgnoreCase)
                     || string.Equals(action, BadDebtWellKnown.WriteOffFull, StringComparison.OrdinalIgnoreCase)
            ? operation.WriteOffAmount ?? 0m
            : operation.CollectedAmount ?? 0m;

        var cbsResult = await _billing.ProvisionAsync(
            new BillingProvisionRequest(
                operation.Id,
                operation.Number,
                msisdn,
                TelecomOperationKind.BadDebtRecovery,
                operation.CorrelationId,
                amount > 0 ? amount : null,
                ProductServiceCode: cbsCode),
            cancellationToken);

        if (!cbsResult.Success)
        {
            operation.CollectionSettlementStatus = BadDebtWellKnown.SettlementFailed;
            throw new InvalidOperationException(cbsResult.Message ?? "فشل ترحيل التحصيل إلى CBS.");
        }

        operation.CollectionSettlementStatus = BadDebtWellKnown.SettlementCompleted;
        operation.ProvisioningResult = cbsResult.Message ?? $"BDR-CBS-{operation.Number}";

        if (string.Equals(action, BadDebtWellKnown.DunningEscalation, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(msisdn))
        {
            var stage = operation.DunningStage ?? BadDebtWellKnown.Reminder1;
            await _sms.SendAsync(
                msisdn,
                $"تذكير تحصيل ({stage}) — ذمة {-operation.OutstandingBalanceSnapshot:N0} ل.س — {operation.Number}",
                cancellationToken);
        }

        if (string.Equals(action, BadDebtWellKnown.WriteOffPartial, StringComparison.OrdinalIgnoreCase)
            || string.Equals(action, BadDebtWellKnown.WriteOffFull, StringComparison.OrdinalIgnoreCase))
        {
            operation.PriorDunningStage = operation.DunningStage;
            operation.DunningStage = BadDebtWellKnown.Settled;
        }

        operation.UpdatedById = actorUserId;
        _operationRepository.Update(operation);
    }

    public Task CompensateOnFailureAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.BadDebtRecovery)
        {
            return Task.CompletedTask;
        }

        operation.CollectionSettlementStatus = BadDebtWellKnown.SettlementFailed;
        operation.ProvisioningResult = "Compensated";
        _operationRepository.Update(operation);
        return Task.CompletedTask;
    }

    private async Task<string> ResolveMsisdnAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            var msisdn = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
                .Where(m => m.Id == operation.MsisdnAssetId)
                .Select(m => m.Msisdn)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(msisdn))
            {
                return msisdn;
            }
        }

        throw new InvalidOperationException("تعذر تحديد MSISDN لعملية التحصيل.");
    }
}
