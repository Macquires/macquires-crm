using Application.Common.Security;
using Application.Common.Services.SecurityManager;

namespace Application.Tests.Security;

public class TelecomWorkspaceRulesTests
{
    [Theory]
    [InlineData(new[] { "TelecomAdmin" }, true)]
    [InlineData(new[] { "TelecomManagement", "TelecomCallCenter" }, true)]
    [InlineData(new[] { "TelecomShowroom" }, true)]
    [InlineData(new[] { "TelecomShowroom", "Admin" }, false)]
    public void IsStrictTelecomWorkspaceUser_only_all_standard_roles(string[] roles, bool expected) =>
        Assert.Equal(expected, TelecomWorkspaceRules.IsStrictTelecomWorkspaceUser(roles));

    [Fact]
    public void ResolveSessionLandingPath_executive_uses_telecom_hub_not_back_office()
    {
        var permissions = PermissionCatalog.DefaultRoleGrants[TelecomEnterpriseRoleMatrix.RoleManagement]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            PermissionLandingResolver.BackOfficeDashboard,
            PermissionLandingResolver.Resolve(permissions));
        Assert.Equal(
            TelecomPersonaLanding.Executive,
            TelecomWorkspaceRules.ResolveSessionLandingPath(
                [TelecomEnterpriseRoleMatrix.RoleManagement],
                TelecomMenuPersona.Executive,
                permissions));
    }

    [Fact]
    public void ResolveSessionLandingPath_sysadmin_uses_telecom_hub_not_user_list()
    {
        var permissions = PermissionCatalog.DefaultRoleGrants[TelecomEnterpriseRoleMatrix.RoleAdmin]
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(
            PermissionLandingResolver.AdministrationUsers,
            PermissionLandingResolver.Resolve(permissions));
        Assert.Equal(
            TelecomPersonaLanding.SysAdmin,
            TelecomWorkspaceRules.ResolveSessionLandingPath(
                [TelecomEnterpriseRoleMatrix.RoleAdmin],
                TelecomMenuPersona.SysAdmin,
                permissions));
    }
}
