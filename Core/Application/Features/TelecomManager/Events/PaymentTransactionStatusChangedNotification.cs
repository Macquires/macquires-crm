using Domain.Enums;
using MediatR;

namespace Application.Features.TelecomManager.Events;

public sealed record PaymentTransactionStatusChangedNotification(
    string PaymentTransactionId,
    string PaymentNumber,
    PaymentTransactionType TransactionType,
    PaymentTransactionStatus FromStatus,
    PaymentTransactionStatus ToStatus,
    string? CorrelationId,
    string? Msisdn,
    decimal Amount,
    string? ActorUserId) : INotification;
