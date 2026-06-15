namespace Application.Common.Security;

public static class BackOfficePermissionSets
{
    public static readonly IReadOnlyList<string> TechnicalViewAny =
    [
        PermissionCatalog.NetworkTechnicalView,
        PermissionCatalog.AdminSettingsManage,
    ];

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

    public static readonly IReadOnlyList<string> TechnicalEscalateAny =
    [
        PermissionCatalog.TelecomTicketEscalate,
        PermissionCatalog.AdminSettingsManage,
    ];

    public static readonly IReadOnlyList<string> TechnicalSyncAny =
    [
        PermissionCatalog.NetworkTechnicalSync,
        PermissionCatalog.AdminSettingsManage,
    ];

    public static readonly IReadOnlyList<string> BdrViewAny =
    [
        PermissionCatalog.FinanceBdrView,
        PermissionCatalog.FinanceBdrExecute,
        PermissionCatalog.NetworkTechnicalView,
    ];

    public static readonly IReadOnlyList<string> BdrExecuteAny =
    [
        PermissionCatalog.FinanceBdrExecute,
        PermissionCatalog.AdminSettingsManage,
    ];

    /// <summary>Matches <see cref="TelecomPermissionAttributes.RequireBackOfficeOperationsAttribute"/>.</summary>
    public static readonly IReadOnlyList<string> OperationsAny =
    [
        PermissionCatalog.FinanceBdrView,
        PermissionCatalog.FinanceBdrExecute,
        PermissionCatalog.NetworkTechnicalView,
    ];

    private static IReadOnlyList<string> Merge(params IEnumerable<string>[] sources) =>
        sources.SelectMany(s => s).Distinct(StringComparer.Ordinal).ToList();
}
