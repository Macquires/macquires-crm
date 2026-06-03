using Application.Common.CQS.Queries;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.OfferSubscription;

public interface IMigrationCompletionService
{
    Task NotifyAndAuditAsync(
        TelecomOperationRequest operation,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class MigrationCompletionService : IMigrationCompletionService
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;
    private readonly ISmsGatewayIntegration _sms;
    private readonly IUnitOfWork _unitOfWork;

    public MigrationCompletionService(
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
        if (operation.Kind != TelecomOperationKind.Migration)
        {
            return;
        }

        var offerName = operation.TargetOfferName;
        if (string.IsNullOrEmpty(offerName) && !string.IsNullOrEmpty(operation.ProductOfferingId))
        {
            offerName = await _query.ProductOffering.AsNoTracking()
                .Where(o => !o.IsDeleted && o.Id == operation.ProductOfferingId)
                .Select(o => o.Name)
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
                    $"MGR completed|offer={offerName ?? "—"}|priorProduct={operation.PriorProductId}|priorOffering={operation.PriorProductOfferingId}|msisdn={msisdn ?? "—"}",
                OccurredAtUtc = DateTime.UtcNow,
                CorrelationId = operation.CorrelationId,
                CreatedById = actorUserId,
            },
            cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        if (!string.IsNullOrEmpty(msisdn))
        {
            var body =
                $"سيريتل: تم ترحيل باقتك على {msisdn} إلى {offerName ?? "الباقة الجديدة"} بنجاح.";
            await _sms.SendAsync(msisdn, body, cancellationToken);
        }
    }
}
