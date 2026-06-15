namespace Application.Common.Security;

public static class BulkImportPermissionSets
{
    /// <summary>Monitor jobs, download templates, export errors (matches <c>RequireBulkImportMonitor</c>).</summary>
    public static readonly IReadOnlyList<string> MonitorAny =
    [
        PermissionCatalog.BulkImportMonitor,
        PermissionCatalog.BulkImportUpload,
        PermissionCatalog.AdminSettingsManage,
    ];

    /// <summary>Upload CSV jobs (matches inventory bulk-import upload paths).</summary>
    public static readonly IReadOnlyList<string> UploadAny =
    [
        PermissionCatalog.BulkImportUpload,
        PermissionCatalog.TelecomAssetManage,
        PermissionCatalog.AdminSettingsManage,
    ];
}
