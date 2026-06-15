namespace Application.Common.Security;

public static class FileAttachmentPermissionSets
{
    /// <summary>Upload identity documents and telecom operation attachments.</summary>
    public static readonly IReadOnlyList<string> UploadAny =
    [
        PermissionCatalog.CustomerUpdate,
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
        PermissionCatalog.AdminSettingsManage,
    ];

    /// <summary>Download uploaded documents (customer file + telecom workflow readers).</summary>
    public static readonly IReadOnlyList<string> ReadAny =
    [
        PermissionCatalog.CustomerView,
        PermissionCatalog.TelecomCustomerProvisioning,
        PermissionCatalog.TelecomReportsMis,
        PermissionCatalog.TelecomLineRecharge,
        PermissionCatalog.TelecomAssetManage,
        PermissionCatalog.TelecomLineActivate,
        PermissionCatalog.AdminSettingsManage,
        PermissionCatalog.AdminAuditView,
    ];
}
