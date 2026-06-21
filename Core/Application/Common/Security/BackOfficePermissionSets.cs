namespace Application.Common.Security;

public static class BackOfficePermissionSets
{
    public static readonly IReadOnlyList<string> TechnicalViewAny = Merge(
        [PermissionCatalog.NetworkTechnicalView, PermissionCatalog.AdminSettingsManage],
        TelecomBackOfficeApprovalPermissionSets.TechnicalViewLegacyAny,
        CustomerPermissionSets.ViewAny);

    /// <summary>Ticket list/archive — matches <see cref="NavigationPermissionRules"/> Call Center nav.</summary>
    public static readonly IReadOnlyList<string> TechnicalTicketListAny = Merge(
        TechnicalViewAny,
        CustomerPermissionSets.ViewAny);

    /// <summary>Open ticket from call center or back-office (same surface as list nav).</summary>
    public static readonly IReadOnlyList<string> TechnicalTicketCreateAny = TechnicalTicketListAny;

    /// <summary>Update status, resolve, claim — provisioning staff without network tab access.</summary>
    public static readonly IReadOnlyList<string> TechnicalTicketManageAny = Merge(
        TechnicalViewAny,
        CustomerPermissionSets.ManageAny,
        [PermissionCatalog.TelecomTicketEscalate]);

    public static readonly IReadOnlyList<string> TechnicalEscalateAny = Merge(
        [PermissionCatalog.TelecomTicketEscalate, PermissionCatalog.AdminSettingsManage],
        TelecomBackOfficeApprovalPermissionSets.TechnicalViewLegacyAny);

    public static readonly IReadOnlyList<string> TechnicalSyncAny = Merge(
        [PermissionCatalog.NetworkTechnicalSync, PermissionCatalog.AdminSettingsManage],
        TelecomBackOfficeApprovalPermissionSets.TechnicalSyncLegacyAny);

    public static readonly IReadOnlyList<string> BdrViewAny = Merge(
        [
            PermissionCatalog.FinanceBdrView,
            PermissionCatalog.FinanceBdrExecute,
            PermissionCatalog.NetworkTechnicalView,
        ],
        TelecomBackOfficeApprovalPermissionSets.ExecuteAny);

    public static readonly IReadOnlyList<string> BdrExecuteAny = Merge(
        [PermissionCatalog.FinanceBdrExecute, PermissionCatalog.AdminSettingsManage],
        TelecomBackOfficeApprovalPermissionSets.ExecuteAny);

    /// <summary>View pending telecom approval queue — matches <c>GetPendingRequests</c>.</summary>
    public static readonly IReadOnlyList<string> PendingQueueViewAny = Merge(
        BdrViewAny,
        BackOfficeDashboardPermissionSets.AccessAny);

    /// <summary>Matches <see cref="TelecomPermissionAttributes.RequireBackOfficeOperationsAttribute"/>.</summary>
    public static readonly IReadOnlyList<string> OperationsAny = Merge(
        BdrViewAny,
        BackOfficeDashboardPermissionSets.AccessAny);

    private static IReadOnlyList<string> Merge(params IEnumerable<string>[] sources) =>
        sources.SelectMany(s => s).Distinct(StringComparer.Ordinal).ToList();
}
