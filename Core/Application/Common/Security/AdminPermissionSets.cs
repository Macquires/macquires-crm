namespace Application.Common.Security;

public static class AdminPermissionSets
{
    public static readonly IReadOnlyList<string> UsersManageAny =
    [
        PermissionCatalog.AdminUsersManage,
        PermissionCatalog.AdminSettingsManage,
    ];

    public static readonly IReadOnlyList<string> RolesManageAny =
    [
        PermissionCatalog.AdminRolesManage,
        PermissionCatalog.AdminSettingsManage,
    ];

    public static readonly IReadOnlyList<string> SettingsManageAny =
    [
        PermissionCatalog.AdminSettingsManage,
        PermissionCatalog.AdminUsersManage,
    ];

    public static readonly IReadOnlyList<string> AuditViewAny =
    [
        PermissionCatalog.AdminAuditView,
        PermissionCatalog.AdminSettingsManage,
        PermissionCatalog.AdminUsersManage,
    ];
}
