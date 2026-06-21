namespace Application.Common.Security;

/// <summary>Call-center / customer-care surfaces — search, Customer 360, SUP tickets (not NOC/inventory).</summary>
public static class CustomerSupportPermissionSets
{
    public static readonly IReadOnlyList<string> ExecuteAny =
    [
        PermissionCatalog.CustomerView,
        PermissionCatalog.TelecomTicketEscalate,
    ];

    /// <summary>Personal open-ticket queue on unified search landing.</summary>
    public static readonly IReadOnlyList<string> MyTicketsQueueAny =
    [
        PermissionCatalog.TelecomTicketEscalate,
        PermissionCatalog.CustomerView,
    ];

    /// <summary>Full technical ticket admin list (sidebar) — not the call-center personal queue.</summary>
    public static readonly IReadOnlyList<string> TicketAdminListNavAny = Merge(
        [PermissionCatalog.NetworkTechnicalView, PermissionCatalog.AdminSettingsManage],
        CustomerPermissionSets.ManageAny);

    /// <summary>
    /// Support-only agent: can view customers and open SUP tickets, but lacks hub/line/inventory surfaces.
    /// </summary>
    public static bool IsSupportOnlyAgent(IReadOnlySet<string> permissions)
    {
        if (permissions == null || permissions.Count == 0)
        {
            return false;
        }

        var hasSupport = ExecuteAny.Any(permissions.Contains);
        if (!hasSupport)
        {
            return false;
        }

        return !HubPermissionSets.NavAny.Any(permissions.Contains)
            && !permissions.Contains(PermissionCatalog.TelecomLineActivate)
            && !permissions.Contains(PermissionCatalog.TelecomAssetManage)
            && !permissions.Contains(PermissionCatalog.NetworkTechnicalView)
            && !permissions.Contains(PermissionCatalog.AdminSettingsManage);
    }

    private static IReadOnlyList<string> Merge(params IEnumerable<string>[] sources) =>
        sources.SelectMany(s => s).Distinct(StringComparer.Ordinal).ToList();
}
