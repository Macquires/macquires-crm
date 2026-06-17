namespace Application.Common.Security;

public static class IntegrationMonitorPermissionSets
{
    public static readonly IReadOnlyList<string> MonitorAny =
    [
        PermissionCatalog.AdminIntegrationMonitor,
        PermissionCatalog.AdminSettingsManage,
        PermissionCatalog.TelecomReportsMis,
    ];
}
