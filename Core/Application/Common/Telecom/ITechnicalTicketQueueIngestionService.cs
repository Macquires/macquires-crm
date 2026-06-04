using Domain.Entities;

namespace Application.Common.Telecom;

public interface ITechnicalTicketQueueIngestionService
{
    /// <summary>
    /// Enqueues an open back-office ticket from a Customer 360 / showroom telecom operation draft.
    /// Idempotent per operation id while a matching ticket is still open.
    /// </summary>
    Task<TelecomTechnicalTicket?> EnqueueFromTelecomOperationAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken = default);

    /// <summary>VAL-02-05 — fallout ticket after CBS/HLR failure (correlation id in payload).</summary>
    Task<TelecomTechnicalTicket?> EnqueueProvisioningFalloutAsync(
        TelecomOperationRequest operation,
        string failureMessage,
        string? actorUserId,
        CancellationToken cancellationToken = default);

    /// <summary>VAL-14-04 — device installment overdue → collections queue.</summary>
    Task<TelecomTechnicalTicket?> EnqueueDeviceInstallmentCollectionsAsync(
        TelecomOperationRequest operation,
        string contractNumber,
        string message,
        CancellationToken cancellationToken = default);
}
