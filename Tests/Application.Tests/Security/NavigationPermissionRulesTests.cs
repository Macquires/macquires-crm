using Application.Common.Security;

namespace Application.Tests.Security;

public sealed class NavigationPermissionRulesTests
{
    [Fact]
    public void BulkImportMonitor_AllowsMonitorPermission()
    {
        var allowed = NavigationPermissionRules.IsNavUrlAllowed(
            "/Telecom/BulkImportMonitor",
            new HashSet<string> { PermissionCatalog.BulkImportMonitor });

        Assert.True(allowed);
    }

    [Fact]
    public void CustomerGroupList_RequiresReferenceDataRead()
    {
        var denied = NavigationPermissionRules.IsNavUrlAllowed(
            "/CustomerGroups/CustomerGroupList",
            new HashSet<string> { PermissionCatalog.TelecomAssetManage });

        var allowed = NavigationPermissionRules.IsNavUrlAllowed(
            "/CustomerGroups/CustomerGroupList",
            new HashSet<string> { PermissionCatalog.CustomerView });

        Assert.False(denied);
        Assert.True(allowed);
    }

    [Fact]
    public void TechnicalTicketList_AllowsCustomerView_ForCallCenterNav()
    {
        var allowed = NavigationPermissionRules.IsNavUrlAllowed(
            "/Telecom/TechnicalTicketList",
            new HashSet<string> { PermissionCatalog.CustomerView });

        Assert.True(allowed);
    }

    [Fact]
    public void TechnicalTicketList_MatchesBackOfficePermissionSet()
    {
        foreach (var key in BackOfficePermissionSets.TechnicalTicketListAny)
        {
            Assert.True(
                NavigationPermissionRules.IsNavUrlAllowed(
                    "/Telecom/TechnicalTicketList",
                    new HashSet<string> { key }),
                $"Expected nav allow for {key}");
        }
    }

    [Fact]
    public void TechnicalTicketList_AllowsNetworkTechnicalView()
    {
        var allowed = NavigationPermissionRules.IsNavUrlAllowed(
            "/Telecom/TechnicalTicketList",
            new HashSet<string> { PermissionCatalog.NetworkTechnicalView });

        Assert.True(allowed);
    }

    [Fact]
    public void DefaultDashboard_AllowsDashboardReadPermission()
    {
        var allowed = NavigationPermissionRules.IsNavUrlAllowed(
            "/Dashboards/DefaultDashboard",
            new HashSet<string> { PermissionCatalog.TelecomReportsMis });

        Assert.True(allowed);
    }

    [Fact]
    public void MyProfile_IsAlwaysAllowed()
    {
        var allowed = NavigationPermissionRules.IsNavUrlAllowed(
            PermissionLandingResolver.MyProfile,
            new HashSet<string>());

        Assert.True(allowed);
    }

    [Fact]
    public void UnifiedSearch_AllowsCustomerView_ForCallCenterNav()
    {
        var allowed = NavigationPermissionRules.IsNavUrlAllowed(
            "/Telecom/UnifiedSearch",
            new HashSet<string> { PermissionCatalog.CustomerView });

        Assert.True(allowed);
    }

    [Fact]
    public void UnifiedSearch_MatchesUnifiedSearchPermissionSet()
    {
        foreach (var key in DashboardPermissionSets.UnifiedSearchAny)
        {
            Assert.True(
                NavigationPermissionRules.IsNavUrlAllowed(
                    "/Telecom/UnifiedSearch",
                    new HashSet<string> { key }),
                $"Expected nav allow for {key}");
        }

        Assert.False(
            NavigationPermissionRules.IsNavUrlAllowed(
                "/Telecom/UnifiedSearch",
                new HashSet<string> { PermissionCatalog.AdminRolesManage }));
    }
}
