namespace Application.Common.Security;

public static class ReferenceDataPermissionSets
{
    public static readonly IReadOnlyList<string> ReadAny =
    [
        PermissionCatalog.CustomerView,
        PermissionCatalog.CustomerCreate,
        PermissionCatalog.TelecomCustomerProvisioning,
        PermissionCatalog.TelecomLineActivate,
        PermissionCatalog.AdminSettingsManage,
    ];

    public static readonly IReadOnlyList<string> ManageAny =
    [
        PermissionCatalog.AdminSettingsManage,
    ];

    /// <summary>Matches <see cref="TelecomPermissionAttributes.RequireTelecomLineTypeManageAttribute"/>.</summary>
    public static readonly IReadOnlyList<string> LineTypeManageAny =
    [
        PermissionCatalog.AdminSettingsManage,
        PermissionCatalog.TelecomCustomerProvisioning,
    ];
}
