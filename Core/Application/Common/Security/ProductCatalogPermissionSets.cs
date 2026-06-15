namespace Application.Common.Security;

public static class ProductCatalogPermissionSets
{
    public static readonly IReadOnlyList<string> ReadAny =
    [
        PermissionCatalog.CustomerView,
        PermissionCatalog.TelecomCustomerProvisioning,
        PermissionCatalog.TelecomLineActivate,
        PermissionCatalog.TelecomLineMigrate,
        PermissionCatalog.TelecomLineChangeGsm,
        PermissionCatalog.TelecomVasToggle,
        PermissionCatalog.AdminSettingsManage,
    ];

    public static readonly IReadOnlyList<string> ManageAny =
    [
        PermissionCatalog.AdminSettingsManage,
        PermissionCatalog.TelecomCustomerProvisioning,
    ];
}
