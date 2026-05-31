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
        Assert.Contains("/Dashboards/DefaultDashboard", urls);
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
        Assert.Contains("/Customers/CustomerList", urls);
        Assert.Contains("/Telecom/TechnicalTicketList", urls);
        Assert.DoesNotContain("/Telecom/BackOfficeDashboard", urls);
        Assert.DoesNotContain("/Administration/UserList", urls);
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
