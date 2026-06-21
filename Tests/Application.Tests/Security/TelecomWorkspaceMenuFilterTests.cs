using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Infrastructure.SecurityManager.NavigationMenu;

namespace Application.Tests.Security;

public class TelecomWorkspaceMenuFilterTests
{
    private static HashSet<string> LeafUrls(IReadOnlyList<MenuNavigationTreeNodeDto> nodes) =>
        nodes
            .Where(n => !n.HasChild && !string.IsNullOrWhiteSpace(n.NavURL))
            .Select(n => n.NavURL!.Split('?')[0])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Executive_menu_excludes_back_office_despite_bulk_import_monitor_permission()
    {
        var nodes = NavigationTreeStructure.GetCompleteMenuNavigationTreeNode();
        var executivePerms = PermissionCatalog.DefaultRoleGrants[TelecomEnterpriseRoleMatrix.RoleManagement]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filtered = NavigationTreeStructure.ApplyTelecomWorkspaceMenuFilter(
            [TelecomEnterpriseRoleMatrix.RoleManagement],
            executivePerms,
            nodes);

        var urls = LeafUrls(filtered);
        var fullUrls = filtered
            .Where(n => !n.HasChild && !string.IsNullOrWhiteSpace(n.NavURL))
            .Select(n => n.NavURL!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("/Dashboards/DefaultDashboard", urls);
        Assert.Contains("/Executive/CommandCenter", urls);
        Assert.Contains("/Executive/CommandCenter?tab=scorecard", fullUrls);
        Assert.Contains("/Executive/CommandCenter?tab=mis", fullUrls);
        Assert.Contains("/Executive/CommandCenter?tab=alerts", fullUrls);
        Assert.DoesNotContain("/Telecom/TelecomMisReports", urls);
        Assert.DoesNotContain("/Telecom/BackOfficeDashboard", urls);
        Assert.DoesNotContain("/Administration/AuditLogList", urls);
    }

    [Fact]
    public void CallCenter_menu_matches_persona_and_customer_view_permission()
    {
        var nodes = NavigationTreeStructure.GetCompleteMenuNavigationTreeNode();
        var perms = PermissionCatalog.DefaultRoleGrants[TelecomEnterpriseRoleMatrix.RoleCallCenter]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filtered = NavigationTreeStructure.ApplyTelecomWorkspaceMenuFilter(
            [TelecomEnterpriseRoleMatrix.RoleCallCenter],
            perms,
            nodes);

        var urls = LeafUrls(filtered);
        Assert.Contains("/Telecom/UnifiedSearch", urls);
        Assert.Contains("/Telecom/ProductCatalog", urls);
        Assert.DoesNotContain("/Telecom/TechnicalTicketList", urls);
        Assert.DoesNotContain("/Customers/CustomerList", urls);
        Assert.DoesNotContain("/Telecom/TelecomHub", urls);
        Assert.DoesNotContain("/Telecom/MsisdnInventory", urls);
        Assert.DoesNotContain("/Telecom/BackOfficeDashboard", urls);
        Assert.DoesNotContain("/Administration/UserList", urls);
    }

    [Fact]
    public void BackOffice_menu_includes_operations_dashboard_and_tickets()
    {
        var nodes = NavigationTreeStructure.GetCompleteMenuNavigationTreeNode();
        var perms = PermissionCatalog.DefaultRoleGrants[TelecomEnterpriseRoleMatrix.RoleBackOffice]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filtered = NavigationTreeStructure.ApplyTelecomWorkspaceMenuFilter(
            [TelecomEnterpriseRoleMatrix.RoleBackOffice],
            perms,
            nodes);

        var urls = LeafUrls(filtered);
        Assert.Contains("/Telecom/BackOfficeDashboard", urls);
        Assert.Contains("/Telecom/TechnicalTicketList", urls);
        Assert.Contains("/Customers/CustomerList", urls);
        Assert.NotEmpty(filtered.Where(n => n.HasChild));
    }

    [Fact]
    public void SysAdmin_sees_governance_module_with_full_permissions()
    {
        var nodes = NavigationTreeStructure.GetCompleteMenuNavigationTreeNode();
        var perms = PermissionCatalog.All.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filtered = NavigationTreeStructure.ApplyTelecomWorkspaceMenuFilter(
            [TelecomEnterpriseRoleMatrix.RoleAdmin],
            perms,
            nodes);

        var urls = LeafUrls(filtered);
        Assert.Contains("/Administration/UserList", urls);
        Assert.Contains("/Telecom/IntegrationMonitor", urls);
    }
}
