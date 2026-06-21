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

    /// <summary>Read org-unit / branch lookup — admin screens plus operational filters (device inventory, asset pool).</summary>
    public static readonly IReadOnlyList<string> OrgUnitReadAny = Merge(
        UsersManageAny,
        [
            PermissionCatalog.TelecomDeviceInventoryManage,
            PermissionCatalog.TelecomAssetManage,
            PermissionCatalog.BulkImportUpload,
            PermissionCatalog.BulkImportMonitor,
            PermissionCatalog.TelecomReportsMis,
        ]);

    private static IReadOnlyList<string> Merge(params IEnumerable<string>[] sources) =>
        sources.SelectMany(s => s).Distinct(StringComparer.Ordinal).ToList();
}
