using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Reconnect;

public interface IReconnectCompletionService
{
    Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class ReconnectCompletionService : IReconnectCompletionService
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;
    private readonly ISmsGatewayIntegration _sms;
    private readonly IHLRLiveStatusService _hlr;
    private readonly IBillingSystemIntegration _billing;
    private readonly IUnitOfWork _unitOfWork;

    public ReconnectCompletionService(
        IQueryContext query,
        ICommandRepository<TelecomOperationAuditLog> auditRepository,
        ISmsGatewayIntegration sms,
        IHLRLiveStatusService hlr,
        IBillingSystemIntegration billing,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _auditRepository = auditRepository;
        _sms = sms;
        _hlr = hlr;
        _billing = billing;
        _unitOfWork = unitOfWork;
    }

    public async Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.Reconnect)
        {
            return;
        }

        operation.ProvisioningResult = "Completed";
        operation.ReactivationAtUtc ??= DateTime.UtcNow;

        var line = msisdn;
        if (string.IsNullOrEmpty(line) && !string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            line = await _query.MsisdnAsset.AsNoTracking()
                .Where(m => !m.IsDeleted && m.Id == operation.MsisdnAssetId)
                .Select(m => m.Msisdn)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var fieldChanges = System.Text.Json.JsonSerializer.Serialize(new
        {
            before = operation.PriorOperationalStatus,
            after = "Active",
            clearance = operation.ClearanceType,
            reason = operation.ReconnectReason,
            paymentRef = operation.PaymentReference,
            msisdn = line,
        });

        await _auditRepository.CreateAsync(
            new TelecomOperationAuditLog
            {
                TelecomOperationRequestId = operation.Id,
                FromStatus = TelecomOperationStatus.Provisioning,
                ToStatus = TelecomOperationStatus.Completed,
                ActorUserId = actorUserId,
                Note =
                    $"RCN completed|clearance={operation.ClearanceType}|reason={operation.ReconnectReason}|msisdn={line ?? "—"}",
                FieldChangesJson = fieldChanges,
                OccurredAtUtc = DateTime.UtcNow,
                CorrelationId = operation.CorrelationId,
                CreatedById = actorUserId,
            },
            cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        if (!string.IsNullOrEmpty(line))
        {
            await _hlr.MarkMockSubscriberActiveAsync(line, cancellationToken);

            // GLOBAL HARDENING: CBS Settlement for BDR Reconnect
            if (operation.ClearanceType == ReconnectWellKnown.Payment || !string.IsNullOrEmpty(operation.PaymentReference))
            {
                // Set Postpaid outstanding balance to 0 in CBS
                await _billing.AdjustBalanceAsync(line, 0, "BDR Settlement", null, cancellationToken);
            }

            var body =
                $"تم إعادة تفعيل خطك {line}. مرجع العملية: {operation.Number}.";
            await _sms.SendAsync(line, body, cancellationToken);
        }
    }
}
