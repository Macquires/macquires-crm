namespace Application.Common.Security;

/// <summary>Telecom Hub / NOC surfaces — distinct from customer-only provisioning on Customer 360.</summary>
public static class HubPermissionSets
{
    /// <summary>Sidebar NOC entry and Hub page access (POS / back-office / supervisor buckets).</summary>
    public static readonly IReadOnlyList<string> NavAny =
    [
        PermissionCatalog.TelecomHubFrontline,
        PermissionCatalog.TelecomHubBackOffice,
        PermissionCatalog.TelecomHubSupervisor,
        PermissionCatalog.TelecomLineActivate,
        PermissionCatalog.TelecomAssetManage,
        PermissionCatalog.AdminSettingsManage,
    ];

    /// <summary>Operational KPI tiles on Hub (pending / completed / failed).</summary>
    public static readonly IReadOnlyList<string> OperationalKpiAny = OperationalKpiPermissionSets.ReadAny;

    /// <summary>Network map / branch geo monitoring widgets.</summary>
    public static readonly IReadOnlyList<string> NetworkMapAny =
    [
        PermissionCatalog.NetworkTechnicalView,
        PermissionCatalog.TelecomReportsMis,
        PermissionCatalog.AdminSettingsManage,
    ];

    /// <summary>Telecom operations queue table on Hub.</summary>
    public static readonly IReadOnlyList<string> OperationsQueueAny = Merge(
        NavAny,
        BackOfficePermissionSets.PendingQueueViewAny);

    private static IReadOnlyList<string> Merge(params IEnumerable<string>[] sources) =>
        sources.SelectMany(s => s).Distinct(StringComparer.Ordinal).ToList();
}
