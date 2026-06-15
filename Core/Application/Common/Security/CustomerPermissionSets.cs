namespace Application.Common.Security;

public static class CustomerPermissionSets
{
    public static readonly IReadOnlyList<string> ViewAny =
    [
        PermissionCatalog.CustomerView,
        PermissionCatalog.TelecomCustomerProvisioning,
    ];

    public static readonly IReadOnlyList<string> ManageAny =
    [
        PermissionCatalog.CustomerUpdate,
        PermissionCatalog.TelecomCustomerProvisioning,
        PermissionCatalog.TelecomLineActivate,
    ];

    public static readonly IReadOnlyList<string> DeleteAny =
    [
        PermissionCatalog.CustomerUpdate,
        PermissionCatalog.TelecomCustomerProvisioning,
        PermissionCatalog.AdminUsersManage,
    ];
}
