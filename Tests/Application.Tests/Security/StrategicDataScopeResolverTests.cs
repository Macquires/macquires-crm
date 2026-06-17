using Application.Common.Security;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tests.Security;

public class StrategicDataScopeResolverTests
{
    private static readonly List<OrgUnit> DemoTree = BuildDemoTree();

    private static List<OrgUnit> BuildDemoTree()
    {
        var hq = new OrgUnit { Id = "hq", NameAr = "المقر", Kind = OrgUnitKind.Headquarters, IsActive = true };
        var north = new OrgUnit { Id = "reg-north", ParentId = "hq", NameAr = "المنطقة الشمالية", Kind = OrgUnitKind.Region, IsActive = true };
        var coast = new OrgUnit { Id = "reg-coast", ParentId = "hq", NameAr = "المنطقة الساحلية", Kind = OrgUnitKind.Region, IsActive = true };
        var aleppo = new OrgUnit { Id = "br-aleppo", ParentId = "reg-north", NameAr = "فرع حلب", Kind = OrgUnitKind.Branch, IsActive = true };
        var tartus = new OrgUnit { Id = "br-tartus", ParentId = "reg-coast", NameAr = "فرع طرطوس", Kind = OrgUnitKind.Branch, IsActive = true };
        return [hq, north, coast, aleppo, tartus];
    }

    [Fact]
    public void GeneralManager_national_scope_returns_all_branches()
    {
        var scope = StrategicDataScopeResolver.Resolve(
            StrategicAccessLevel.GeneralManager,
            DemoTree,
            null,
            null,
            null,
            null);

        Assert.True(scope.CanUseFilters);
        Assert.Equal(2, scope.EffectiveBranchIds.Count);
    }

    [Fact]
    public void GeneralManager_region_drill_down_limits_branches()
    {
        var scope = StrategicDataScopeResolver.Resolve(
            StrategicAccessLevel.GeneralManager,
            DemoTree,
            null,
            null,
            "reg-north",
            null);

        Assert.Single(scope.EffectiveBranchIds);
        Assert.Equal("br-aleppo", scope.EffectiveBranchIds[0]);
    }

    [Fact]
    public void BranchManager_ignores_foreign_branch_request()
    {
        var scope = StrategicDataScopeResolver.Resolve(
            StrategicAccessLevel.BranchManager,
            DemoTree,
            null,
            "br-tartus",
            "reg-north",
            "br-aleppo");

        Assert.Equal("br-tartus", scope.EffectiveBranchId);
        Assert.Single(scope.EffectiveBranchIds);
        Assert.False(scope.CanUseFilters);
    }

    [Fact]
    public void RegionalDirector_rejects_foreign_region_in_service_layer()
    {
        var scope = StrategicDataScopeResolver.Resolve(
            StrategicAccessLevel.RegionalDirector,
            DemoTree,
            "reg-coast",
            null,
            null,
            "br-tartus");

        Assert.Equal("reg-coast", scope.EffectiveRegionId);
        Assert.Single(scope.EffectiveBranchIds);
        Assert.Equal("br-tartus", scope.EffectiveBranchIds[0]);
    }

    [Fact]
    public void RegionalDirector_foreign_branch_throws()
    {
        Assert.Throws<UnauthorizedAccessException>(() =>
            StrategicDataScopeResolver.Resolve(
                StrategicAccessLevel.RegionalDirector,
                DemoTree,
                "reg-coast",
                null,
                null,
                "br-aleppo"));
    }

    [Fact]
    public void DeriveAccessLevel_branch_user_with_org_unit()
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PermissionCatalog.CustomerView,
            PermissionCatalog.TelecomLineActivate,
        };

        var level = StrategicDataScopeResolver.DeriveAccessLevel(
            permissions,
            "br-tartus",
            null,
            null,
            OrgUnitKind.Branch);

        Assert.Equal(StrategicAccessLevel.BranchManager, level);
    }

    [Fact]
    public void DeriveAccessLevel_mis_permission_without_org_unit_is_general_manager()
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PermissionCatalog.TelecomReportsMis,
        };

        var level = StrategicDataScopeResolver.DeriveAccessLevel(
            permissions,
            null,
            null,
            null,
            null);

        Assert.Equal(StrategicAccessLevel.GeneralManager, level);
    }

    [Fact]
    public void DeriveAccessLevel_admin_settings_is_general_manager()
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PermissionCatalog.AdminSettingsManage,
        };

        var level = StrategicDataScopeResolver.DeriveAccessLevel(
            permissions,
            "br-tartus",
            null,
            null,
            OrgUnitKind.Branch);

        Assert.Equal(StrategicAccessLevel.GeneralManager, level);
    }
}
