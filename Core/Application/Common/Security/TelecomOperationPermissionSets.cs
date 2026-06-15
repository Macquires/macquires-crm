namespace Application.Common.Security;

public static class TelecomOperationPermissionSets
{
    public static readonly IReadOnlyList<string> CreateAny =
    [
        PermissionCatalog.TelecomCustomerProvisioning,
        PermissionCatalog.TelecomLineActivate,
        PermissionCatalog.TelecomLineSimSwapRequest,
        PermissionCatalog.TelecomLineMigrate,
        PermissionCatalog.TelecomLineChangeGsm,
        PermissionCatalog.TelecomLineTransferRequest,
        PermissionCatalog.TelecomLineChangeNumberRequest,
        PermissionCatalog.TelecomLineTerminationRequest,
        PermissionCatalog.TelecomLineSuspensionRequest,
        PermissionCatalog.TelecomLineReconnectRequest,
        PermissionCatalog.TelecomDeviceSellRequest,
        PermissionCatalog.TelecomLineRefundRequest,
        PermissionCatalog.TelecomLineCollectionRequest,
        PermissionCatalog.CustomerUpdate,
    ];

    public static readonly IReadOnlyList<string> ConfirmAny =
    [
        PermissionCatalog.TelecomCustomerProvisioning,
        PermissionCatalog.TelecomLineActivate,
        PermissionCatalog.TelecomLineSimSwap,
        PermissionCatalog.TelecomLineMigrate,
        PermissionCatalog.TelecomLineChangeGsm,
        PermissionCatalog.TelecomLineTransferOwnership,
        PermissionCatalog.TelecomLineChangeNumber,
        PermissionCatalog.TelecomLineTermination,
        PermissionCatalog.TelecomLineSuspension,
        PermissionCatalog.TelecomLineReconnect,
        PermissionCatalog.TelecomDeviceSell,
        PermissionCatalog.TelecomLineRefund,
        PermissionCatalog.TelecomLineCollection,
        PermissionCatalog.CustomerUpdate,
    ];

    public static readonly IReadOnlyList<string> ReserveMsisdnAny =
    [
        PermissionCatalog.TelecomAssetManage,
        PermissionCatalog.TelecomLineActivate,
        PermissionCatalog.TelecomCustomerProvisioning,
    ];

    public static readonly IReadOnlyList<string> InventoryManageAny =
    [
        PermissionCatalog.TelecomAssetManage,
        PermissionCatalog.TelecomDeviceInventoryManage,
        PermissionCatalog.TelecomCustomerProvisioning,
    ];

    public static readonly IReadOnlyList<string> NetworkHlrAny =
    [
        PermissionCatalog.TelecomNetworkHlrResync,
        PermissionCatalog.TelecomTicketHlrResync,
        PermissionCatalog.TelecomCustomerProvisioning,
    ];

    public static readonly IReadOnlyList<string> PaymentAny =
    [
        PermissionCatalog.TelecomLineRecharge,
        PermissionCatalog.TelecomLineActivate,
        PermissionCatalog.TelecomCustomerProvisioning,
        PermissionCatalog.CustomerUpdate,
    ];

    public static readonly IReadOnlyList<string> CustomerProvisioningAny =
    [
        PermissionCatalog.TelecomCustomerProvisioning,
        PermissionCatalog.CustomerUpdate,
        PermissionCatalog.CustomerView,
    ];

    public static readonly IReadOnlyList<string> OperationsViewAny =
    [
        PermissionCatalog.CustomerView,
        PermissionCatalog.TelecomCustomerProvisioning,
        PermissionCatalog.TelecomLineActivate,
        PermissionCatalog.TelecomLineSimSwap,
        PermissionCatalog.TelecomLineSimSwapRequest,
        PermissionCatalog.TelecomLineMigrate,
        PermissionCatalog.TelecomLineChangeGsm,
        PermissionCatalog.TelecomLineTransferOwnership,
        PermissionCatalog.TelecomLineTransferRequest,
        PermissionCatalog.TelecomLineChangeNumber,
        PermissionCatalog.TelecomLineChangeNumberRequest,
        PermissionCatalog.TelecomLineTermination,
        PermissionCatalog.TelecomLineTerminationRequest,
        PermissionCatalog.TelecomLineSuspension,
        PermissionCatalog.TelecomLineSuspensionRequest,
        PermissionCatalog.TelecomLineReconnect,
        PermissionCatalog.TelecomLineReconnectRequest,
        PermissionCatalog.TelecomDeviceSell,
        PermissionCatalog.TelecomDeviceSellRequest,
        PermissionCatalog.TelecomLineRefund,
        PermissionCatalog.TelecomLineRefundRequest,
        PermissionCatalog.TelecomLineCollection,
        PermissionCatalog.TelecomLineCollectionRequest,
    ];
}
