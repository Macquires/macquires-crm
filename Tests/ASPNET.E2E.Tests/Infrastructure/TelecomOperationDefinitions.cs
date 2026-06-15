using Domain.Enums;

namespace ASPNET.E2E.Tests.Infrastructure;

public static class TelecomOperationDefinitions
{
    public static IEnumerable<object[]> AllOperations =>
        AllKinds.Select(k => new object[] { k });

    public static IEnumerable<object[]> NetworkOperations =>
        AllKinds.Where(k => E2ETelecomOperationSeeds.RequiresNetworkProvision(k))
            .Select(k => new object[] { k });

    public static IEnumerable<object[]> NonNetworkOperations =>
        AllKinds.Where(k => !E2ETelecomOperationSeeds.RequiresNetworkProvision(k))
            .Select(k => new object[] { k });

    public static string DisplayName(TelecomOperationKind kind) => kind switch
    {
        TelecomOperationKind.NewActivation => "NewActivation",
        TelecomOperationKind.Migration => "Migration",
        TelecomOperationKind.TakeOver => "TakeOver",
        TelecomOperationKind.SimSwap => "SimSwap",
        TelecomOperationKind.ServiceModification => "ServiceModification",
        TelecomOperationKind.NumberPortability => "ChangeNumber",
        TelecomOperationKind.ChangeGsmType => "ChangeGsm",
        TelecomOperationKind.Termination => "Termination",
        TelecomOperationKind.TemporarySuspension => "Suspension",
        TelecomOperationKind.Reconnect => "Reconnect",
        TelecomOperationKind.DeviceSale => "DeviceSale",
        TelecomOperationKind.DepositRefundSettlement => "Refund",
        TelecomOperationKind.BadDebtRecovery => "BadDebt",
        _ => kind.ToString(),
    };

    private static readonly TelecomOperationKind[] AllKinds =
    [
        TelecomOperationKind.NewActivation,
        TelecomOperationKind.Migration,
        TelecomOperationKind.TakeOver,
        TelecomOperationKind.SimSwap,
        TelecomOperationKind.ServiceModification,
        TelecomOperationKind.NumberPortability,
        TelecomOperationKind.ChangeGsmType,
        TelecomOperationKind.Termination,
        TelecomOperationKind.TemporarySuspension,
        TelecomOperationKind.Reconnect,
        TelecomOperationKind.DeviceSale,
        TelecomOperationKind.DepositRefundSettlement,
        TelecomOperationKind.BadDebtRecovery,
    ];
}
