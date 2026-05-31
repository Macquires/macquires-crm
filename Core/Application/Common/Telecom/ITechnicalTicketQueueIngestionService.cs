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
}
