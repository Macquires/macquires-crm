using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.OfferSubscription;

public interface IVasCompletionService
{
    Task WriteAuditAsync(
        string operationId,
        string serviceCode,
        bool activate,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class VasCompletionService : IVasCompletionService
{
    private readonly ICommandRepository<TelecomOperationAuditLog> _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VasCompletionService(
        ICommandRepository<TelecomOperationAuditLog> auditRepository,
        IUnitOfWork unitOfWork)
    {
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task WriteAuditAsync(
        string operationId,
        string serviceCode,
        bool activate,
        string? msisdn,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        await _auditRepository.CreateAsync(
            new TelecomOperationAuditLog
            {
                TelecomOperationRequestId = operationId,
                FromStatus = TelecomOperationStatus.Confirmed,
                ToStatus = TelecomOperationStatus.Completed,
                ActorUserId = actorUserId,
                Note = $"VAS {(activate ? "Activate" : "Deactivate")}|code={serviceCode}|msisdn={msisdn ?? "—"}",
                OccurredAtUtc = DateTime.UtcNow,
                CreatedById = actorUserId,
            },
            cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
