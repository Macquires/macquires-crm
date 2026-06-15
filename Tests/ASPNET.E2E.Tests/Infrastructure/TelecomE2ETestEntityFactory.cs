using Domain.Entities;

namespace ASPNET.E2E.Tests.Infrastructure;

/// <summary>E2E entity factory aligned with Application.Tests TelecomTestEntityFactory.</summary>
public static class TelecomE2ETestEntityFactory
{
    public static readonly byte[] DefaultRowVersion = [0, 0, 0, 0, 0, 0, 0, 1];

    public static TelecomOperationRequest Operation(Action<TelecomOperationRequest>? configure = null)
    {
        var op = new TelecomOperationRequest
        {
            Id = Guid.NewGuid().ToString(),
            RowVersion = DefaultRowVersion,
            BranchId = E2EAuthHelper.BranchA,
            IsDeleted = false,
        };
        configure?.Invoke(op);
        return op;
    }

    public static TelecomTechnicalTicket TechnicalTicket(Action<TelecomTechnicalTicket>? configure = null)
    {
        var ticket = new TelecomTechnicalTicket
        {
            Id = Guid.NewGuid().ToString(),
            RowVersion = DefaultRowVersion,
            BranchId = E2EAuthHelper.BranchA,
            IsDeleted = false,
        };
        configure?.Invoke(ticket);
        return ticket;
    }
}
