using Domain.Enums;
using MediatR;

namespace Application.Common.Events;

/// <summary>Raised after local confirm + CBS; triggers HLR and optional SMS handlers for all lifecycle kinds.</summary>
public sealed record TelecomOperationProvisionedNotification(
    TelecomOperationKind Kind,
    string TelecomOperationRequestId,
    string? Msisdn,
    string? Iccid,
    string? CorrelationId,
    string? ActorUserId,
    string? CustomerId = null,
    string? SubscriberProfileId = null,
    string? TelecomSubscriptionId = null,
    string? MsisdnAssetId = null,
    string? SimInventoryId = null) : INotification;
