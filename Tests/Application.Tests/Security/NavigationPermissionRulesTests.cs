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
    public void TechnicalTicketList_RequiresEscalateOrTechnicalView_NotCustomerViewAlone()
    {
        Assert.False(NavigationPermissionRules.IsNavUrlAllowed(
            "/Telecom/TechnicalTicketList",
            new HashSet<string> { PermissionCatalog.CustomerView }));

        Assert.False(NavigationPermissionRules.IsNavUrlAllowed(
            "/Telecom/TechnicalTicketList",
            new HashSet<string> { PermissionCatalog.TelecomTicketEscalate }));
    }

    [Fact]
    public void TechnicalTicketList_AllowsTicketAdminNavKeys()
    {
        foreach (var key in CustomerSupportPermissionSets.TicketAdminListNavAny)
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
    public void TelecomHub_RequiresHubSurfacePermission_NotProvisioningAlone()
    {
        Assert.False(NavigationPermissionRules.IsNavUrlAllowed(
            "/Telecom/TelecomHub",
            new HashSet<string> { PermissionCatalog.TelecomCustomerProvisioning }));

        Assert.True(NavigationPermissionRules.IsNavUrlAllowed(
            "/Telecom/TelecomHub",
            new HashSet<string> { PermissionCatalog.TelecomHubFrontline }));
    }

    [Fact]
    public void MsisdnInventory_RequiresAssetOrActivatePermission()
    {
        Assert.False(NavigationPermissionRules.IsNavUrlAllowed(
            "/Telecom/MsisdnInventory",
            new HashSet<string> { PermissionCatalog.TelecomCustomerProvisioning }));

        Assert.True(NavigationPermissionRules.IsNavUrlAllowed(
            "/Telecom/MsisdnInventory",
            new HashSet<string> { PermissionCatalog.TelecomAssetManage }));
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
