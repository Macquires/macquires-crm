using Domain.Entities;

namespace Application.Tests.TestSupport;

/// <summary>Required concurrency tokens for in-memory EF tests.</summary>
public static class TelecomTestEntityFactory
{
    public static readonly byte[] DefaultRowVersion = [0, 0, 0, 0, 0, 0, 0, 1];

    public static TelecomOperationRequest Operation(Action<TelecomOperationRequest>? configure = null)
    {
        var op = new TelecomOperationRequest
        {
            Id = Guid.NewGuid().ToString(),
            RowVersion = DefaultRowVersion,
            BranchId = TestOperatorContext.DefaultBranchId,
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
            BranchId = TestOperatorContext.DefaultBranchId,
            IsDeleted = false,
        };
        configure?.Invoke(ticket);
        return ticket;
    }

    public static void ApplyRowVersion(TelecomOperationRequest entity) =>
        entity.RowVersion = DefaultRowVersion;

    public static void ApplyRowVersion(TelecomTechnicalTicket entity) =>
        entity.RowVersion = DefaultRowVersion;
}
