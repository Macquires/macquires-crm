using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.PaymentServices;

public interface IPaymentServicesAuditWriter
{
    Task WriteAsync(
        TelecomPaymentTransaction payment,
        string action,
        PaymentTransactionStatus? fromStatus,
        PaymentTransactionStatus? toStatus,
        decimal? balanceBefore,
        decimal? balanceAfter,
        string? actorUserId,
        string? reasonCode,
        string? note,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentServicesAuditWriter : IPaymentServicesAuditWriter
{
    private readonly ICommandRepository<TelecomPaymentAuditLog> _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentServicesAuditWriter(
        ICommandRepository<TelecomPaymentAuditLog> auditRepository,
        IUnitOfWork unitOfWork)
    {
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task WriteAsync(
        TelecomPaymentTransaction payment,
        string action,
        PaymentTransactionStatus? fromStatus,
        PaymentTransactionStatus? toStatus,
        decimal? balanceBefore,
        decimal? balanceAfter,
        string? actorUserId,
        string? reasonCode,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var row = new TelecomPaymentAuditLog
        {
            TelecomPaymentTransactionId = payment.Id,
            Action = action,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            GatewayReference = payment.GatewayReference,
            ActorUserId = actorUserId,
            ReasonCode = reasonCode,
            Note = note,
            OccurredAtUtc = DateTime.UtcNow,
            CreatedById = actorUserId,
        };

        await _auditRepository.CreateAsync(row, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
