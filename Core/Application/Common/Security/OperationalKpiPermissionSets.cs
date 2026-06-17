namespace Application.Common.Security;

public static class OperationalKpiPermissionSets
{
    /// <summary>
    /// Operational KPI read surface — MIS/executive analytics plus back-office dashboard operators.
    /// </summary>
    public static readonly IReadOnlyList<string> ReadAny = Merge(
        [PermissionCatalog.TelecomReportsMis, PermissionCatalog.AdminSettingsManage],
        BackOfficeDashboardPermissionSets.AccessAny);

    private static IReadOnlyList<string> Merge(params IEnumerable<string>[] sources) =>
        sources.SelectMany(s => s).Distinct(StringComparer.Ordinal).ToList();
}
