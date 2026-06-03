using Domain.Enums;
using MediatR;

namespace Application.Features.TelecomManager.Events;

public sealed record TelecomOperationStatusChangedNotification(
    string TelecomOperationRequestId,
    TelecomOperationKind Kind,
    TelecomOperationStatus FromStatus,
    TelecomOperationStatus ToStatus,
    string? CorrelationId,
    string? Msisdn,
    string? ActorUserId) : INotification;
