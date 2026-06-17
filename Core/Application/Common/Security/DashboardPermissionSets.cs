namespace Application.Common.Security;

public static class DashboardPermissionSets
{
    /// <summary>Read dashboard widgets and provider data (matches <c>RequireTelecomRead</c>).</summary>
    public static readonly IReadOnlyList<string> ReadAny =
    [
        PermissionCatalog.CustomerView,
        PermissionCatalog.TelecomCustomerProvisioning,
        PermissionCatalog.TelecomReportsMis,
        PermissionCatalog.TelecomLineRecharge,
        PermissionCatalog.TelecomAssetManage,
        PermissionCatalog.TelecomLineActivate,
    ];

    /// <summary>Subscriber registry / unified search — same surface as <c>RequireTelecomRead</c>.</summary>
    public static readonly IReadOnlyList<string> UnifiedSearchAny = ReadAny;

    /// <summary>Configure widget catalog (matches <c>RequireDashboardAdmin</c>).</summary>
    public static readonly IReadOnlyList<string> AdminAny =
    [
        PermissionCatalog.AdminSettingsManage,
        PermissionCatalog.AdminUsersManage,
    ];
}
