using Domain.Entities;

namespace Application.Common.Telecom.OperationConfirm;

public interface IOperationConfirmProvisionContextBuilder
{
    Task<OperationProvisionContext> BuildAsync(
        TelecomOperationRequest entity,
        CancellationToken cancellationToken,
        string? iccidOverride = null);

    Task<OperationProvisionContext> BuildForMsisdnChangeAsync(
        TelecomOperationRequest entity,
        string? msisdn,
        string? priorMsisdn,
        CancellationToken cancellationToken);
}
