using Domain.Entities;

namespace Application.Common.Telecom.BackOffice;

public static class BackOfficeBdrLedgerModes
{
    public const string ManualDiscount = "ManualDiscount";
    public const string FullPaymentSettlement = "FullPaymentSettlement";
    public const string StandardCollection = "StandardCollection";
}

public sealed record BackOfficeBdrLedgerView(
    decimal OutstandingDebt,
    decimal WriteOffWaiver,
    decimal CashCollection,
    string LedgerMode,
    string? NoteAr);

public interface IBackOfficeBdrLedgerResolver
{
    Task<BackOfficeBdrLedgerView> ResolveAsync(TelecomOperationRequest operation, CancellationToken cancellationToken = default);

    Task<bool> ShouldSuppressManualBdrInQueueAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}
