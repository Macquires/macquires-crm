using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Termination;

public interface ITerminationCompletionService
{
    Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class TerminationCompletionService : ITerminationCompletionService
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;
    private readonly ISmsGatewayIntegration _sms;
    private readonly IUnitOfWork _unitOfWork;

    public TerminationCompletionService(
        IQueryContext query,
        ICommandRepository<TelecomOperationAuditLog> auditRepository,
        ISmsGatewayIntegration sms,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _auditRepository = auditRepository;
        _sms = sms;
        _unitOfWork = unitOfWork;
    }

    public async Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (operation.Kind != TelecomOperationKind.Termination)
        {
            return;
        }

        operation.DeprovisionStatus = "Completed";
        operation.DepositSettlementStatus ??= "Settled";

        var line = msisdn;
        if (string.IsNullOrEmpty(line) && !string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            line = await _query.MsisdnAsset.AsNoTracking()
                .Where(m => !m.IsDeleted && m.Id == operation.MsisdnAssetId)
                .Select(m => m.Msisdn)
                .FirstOrDefaultAsync(cancellationToken);
        }

        await _auditRepository.CreateAsync(
            new TelecomOperationAuditLog
            {
                TelecomOperationRequestId = operation.Id,
                FromStatus = TelecomOperationStatus.Provisioning,
                ToStatus = TelecomOperationStatus.Completed,
                ActorUserId = actorUserId,
                Note =
                    $"TRM completed|type={operation.TerminationType}|reason={operation.TerminationReason}|finalBill={operation.FinalBillAmount}|msisdn={line ?? "—"}",
                OccurredAtUtc = DateTime.UtcNow,
                CorrelationId = operation.CorrelationId,
                CreatedById = actorUserId,
            },
            cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        if (!string.IsNullOrEmpty(line))
        {
            var body =
                $"تم إنهاء خطك {line} بنجاح. الفاتورة النهائية: {operation.FinalBillAmount ?? 0:N0} ل.س.";
            await _sms.SendAsync(line, body, cancellationToken);
        }
    }
}
