namespace Application.Common.Security;

public static class BackOfficeDashboardPermissionSets
{
    /// <summary>Matches <see cref="NavigationPermissionRules"/> for <c>/Telecom/BackOfficeDashboard</c>.</summary>
    public static readonly IReadOnlyList<string> AccessAny =
    [
        PermissionCatalog.BulkImportMonitor,
        PermissionCatalog.BulkImportUpload,
        PermissionCatalog.TelecomAssetManage,
        PermissionCatalog.AdminSettingsManage,
    ];
}
