using Application.Common.Security;
using Infrastructure.SecurityManager.NavigationMenu;

namespace Application.Tests.Security;

/// <summary>
/// Ensures <see cref="PermissionCatalog.DefaultRoleGrants"/> align with portal navigation rules per role.
/// </summary>
public sealed class RolePermissionMatrixTests
{
    public static IEnumerable<object[]> StandardRoleCases() =>
        TelecomEnterpriseRoleMatrix.StandardRoles.Select(r => new object[] { r });

    [Theory]
    [MemberData(nameof(StandardRoleCases))]
    public void DefaultRoleGrants_MenuLeaves_AreAllowedByNavigationRules(string roleName)
    {
        var permissions = PermissionCatalog.DefaultRoleGrants[roleName]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nodes = NavigationTreeStructure.GetCompleteMenuNavigationTreeNode();
        var filtered = NavigationTreeStructure.ApplyTelecomWorkspaceMenuFilter(
            [roleName],
            permissions,
            nodes);

        var leaves = filtered
            .Where(n => !n.HasChild && !string.IsNullOrWhiteSpace(n.NavURL))
            .Select(n => n.NavURL!)
            .ToList();

        Assert.NotEmpty(leaves);

        foreach (var url in leaves)
        {
            Assert.True(
                NavigationPermissionRules.IsNavUrlAllowed(url, permissions),
                $"Role {roleName} sees nav item {url} but lacks navigation permission keys.");
        }
    }

    [Theory]
    [MemberData(nameof(StandardRoleCases))]
    public void DefaultRoleGrants_LandingPath_IsAllowedByNavigationRules(string roleName)
    {
        var permissions = PermissionCatalog.DefaultRoleGrants[roleName]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var landing = PermissionLandingResolver.Resolve(permissions);

        if (string.Equals(landing, PermissionLandingResolver.MyProfile, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Assert.True(
            NavigationPermissionRules.IsNavUrlAllowed(landing, permissions),
            $"Role {roleName} lands on {landing} but lacks navigation permission keys.");
    }

    [Fact]
    public void DefaultRoleGrants_AllRoles_UseKnownPermissionKeys()
    {
        foreach (var (roleName, keys) in PermissionCatalog.DefaultRoleGrants)
        {
            foreach (var key in keys)
            {
                Assert.True(
                    PermissionCatalog.IsKnownKey(key),
                    $"Role {roleName} grants unknown permission key: {key}");
            }
        }
    }

    [Fact]
    public void BackOffice_HasPendingQueueAndOperationalKpiSurfaces()
    {
        var keys = PermissionCatalog.DefaultRoleGrants[TelecomEnterpriseRoleMatrix.RoleBackOffice]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(BackOfficePermissionSets.PendingQueueViewAny.Any(keys.Contains));
        Assert.True(OperationalKpiPermissionSets.ReadAny.Any(keys.Contains));
        Assert.True(BackOfficePermissionSets.BdrExecuteAny.Any(keys.Contains));
        Assert.True(BackOfficePermissionSets.TechnicalSyncAny.Any(keys.Contains));
    }

    [Fact]
    public void Management_HasExecutiveOperationalSurfaces()
    {
        var keys = PermissionCatalog.DefaultRoleGrants[TelecomEnterpriseRoleMatrix.RoleManagement]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(PermissionCatalog.TelecomReportsMis, keys);
        Assert.True(OperationalKpiPermissionSets.ReadAny.Any(keys.Contains));
        Assert.True(DashboardPermissionSets.ReadAny.Any(keys.Contains));
    }

    [Fact]
    public void CallCenter_LandingPath_IsUnifiedSearch()
    {
        var permissions = PermissionCatalog.DefaultRoleGrants[TelecomEnterpriseRoleMatrix.RoleCallCenter]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(PermissionLandingResolver.UnifiedSearch, PermissionLandingResolver.Resolve(permissions));
    }

    [Fact]
    public void CallCenter_HasCustomerAndTicketSurfaces()
    {
        var keys = PermissionCatalog.DefaultRoleGrants[TelecomEnterpriseRoleMatrix.RoleCallCenter]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(CustomerPermissionSets.ViewAny.Any(keys.Contains));
        Assert.Contains(PermissionCatalog.TelecomTicketEscalate, keys);
    }

    [Fact]
    public void Showroom_HasHubAndCatalogSurfaces()
    {
        var keys = PermissionCatalog.DefaultRoleGrants[TelecomEnterpriseRoleMatrix.RoleShowroom]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(TelecomOperationPermissionSets.CreateAny.Any(keys.Contains));
        Assert.True(ProductCatalogPermissionSets.ReadAny.Any(keys.Contains));
        Assert.True(TelecomOperationPermissionSets.ReserveMsisdnAny.Any(keys.Contains));
    }
}
