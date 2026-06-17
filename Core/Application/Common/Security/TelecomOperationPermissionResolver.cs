using Domain.Enums;

namespace Application.Common.Security;

public static class TelecomOperationPermissionResolver
{
    public static string PermissionKeyForKind(TelecomOperationKind kind) => kind switch
    {
        TelecomOperationKind.Migration => PermissionCatalog.TelecomLineMigrate,
        TelecomOperationKind.SimSwap => PermissionCatalog.TelecomLineSimSwap,
        TelecomOperationKind.NewActivation => PermissionCatalog.TelecomLineActivate,
        TelecomOperationKind.TakeOver => PermissionCatalog.TelecomLineTransferOwnership,
        TelecomOperationKind.NumberPortability => PermissionCatalog.TelecomLineChangeNumber,
        TelecomOperationKind.ChangeGsmType => PermissionCatalog.TelecomLineChangeGsm,
        TelecomOperationKind.Termination => PermissionCatalog.TelecomLineTermination,
        TelecomOperationKind.TemporarySuspension => PermissionCatalog.TelecomLineSuspension,
        TelecomOperationKind.Reconnect => PermissionCatalog.TelecomLineReconnect,
        TelecomOperationKind.DeviceSale => PermissionCatalog.TelecomDeviceSell,
        TelecomOperationKind.DepositRefundSettlement => PermissionCatalog.TelecomLineRefund,
        TelecomOperationKind.BadDebtRecovery => PermissionCatalog.TelecomLineCollection,
        TelecomOperationKind.ServiceModification => PermissionCatalog.TelecomVasToggle,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported telecom operation kind for permission resolution."),
    };
}
