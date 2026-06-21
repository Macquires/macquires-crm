using Domain.Entities;

namespace Application.Common.Telecom.BackOffice;

public interface IBackOfficeRcnBdrSettlementService
{
    /// <summary>
    /// Scenario A: full payment at showroom — auto-close bad-debt ledger and supersede manual discount BDRs.
    /// </summary>
    Task EnsureAutoSettlementForPaidReconnectAsync(
        TelecomOperationRequest reconnectOperation,
        string actorUserId,
        CancellationToken cancellationToken = default);
}
