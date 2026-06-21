namespace Application.Common.Security;

/// <summary>Legacy telecom approve/execute keys that imply back-office queue and BDR workflow access.</summary>
public static class TelecomBackOfficeApprovalPermissionSets
{
    public static readonly IReadOnlyList<string> ExecuteAny =
    [
        PermissionCatalog.TelecomLineSimSwapApprove,
        PermissionCatalog.TelecomLineChangeNumberApprove,
        PermissionCatalog.TelecomLineTerminationApprove,
        PermissionCatalog.TelecomLineSuspensionApprove,
        PermissionCatalog.TelecomLineReconnectApprove,
        PermissionCatalog.TelecomLineRefundApprove,
        PermissionCatalog.TelecomLineCollectionApprove,
        PermissionCatalog.TelecomLineTransferOwnership,
        PermissionCatalog.TelecomDeviceInstallmentApprove,
        PermissionCatalog.TelecomLineChangeGsm,
        PermissionCatalog.TelecomLineChangeNumber,
        PermissionCatalog.TelecomLineTermination,
        PermissionCatalog.TelecomLineSuspension,
        PermissionCatalog.TelecomLineReconnect,
        PermissionCatalog.TelecomLineRefund,
        PermissionCatalog.TelecomLineCollection,
        PermissionCatalog.TelecomLineSimSwap,
        PermissionCatalog.TelecomLineCollectionManage,
    ];

    public static readonly IReadOnlyList<string> TechnicalSyncLegacyAny =
    [
        PermissionCatalog.TelecomTicketForceSync,
        PermissionCatalog.TelecomTicketHlrResync,
        PermissionCatalog.TelecomNetworkHlrResync,
    ];

    public static readonly IReadOnlyList<string> TechnicalViewLegacyAny =
    [
        PermissionCatalog.TelecomTicketEscalate,
        PermissionCatalog.TelecomTicketForceSync,
        PermissionCatalog.TelecomTicketHlrResync,
        PermissionCatalog.TelecomNetworkHlrResync,
    ];
}
