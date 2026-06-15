namespace Application.Common.Security;

/// <summary>Maps portal nav URLs to permission keys (user needs any listed key).</summary>
public static class NavigationPermissionRules
{
    private static readonly IReadOnlyDictionary<string, string[]> Rules =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["/Dashboards/DefaultDashboard"] = Merge(
                DashboardPermissionSets.ReadAny,
                DashboardPermissionSets.AdminAny,
                [PermissionCatalog.AdminRolesManage]),
            ["/Dashboards/DashboardWidgetList"] = Keys(DashboardPermissionSets.AdminAny),
            ["/Telecom/BackOfficeDashboard"] = Merge(
                BulkImportPermissionSets.MonitorAny,
                [PermissionCatalog.TelecomAssetManage]),
            ["/Telecom/TelecomHub"] = Keys(TelecomOperationPermissionSets.CreateAny),
            ["/Telecom/BillingIntegration"] = Merge(
                DashboardPermissionSets.ReadAny,
                [PermissionCatalog.TelecomLineSimSwap, PermissionCatalog.TelecomLineMigrate]),
            ["/Telecom/InIntegration"] = Merge(
                DashboardPermissionSets.ReadAny,
                [PermissionCatalog.TelecomLineSimSwap, PermissionCatalog.TelecomLineMigrate]),
            ["/Telecom/HlrProvisioning"] = Merge(
                DashboardPermissionSets.ReadAny,
                [PermissionCatalog.TelecomLineSimSwap, PermissionCatalog.TelecomLineMigrate]),
            ["/Telecom/IntegrationMonitor"] = [PermissionCatalog.AdminIntegrationMonitor],
            ["/Telecom/BulkImportMonitor"] = Keys(BulkImportPermissionSets.MonitorAny),
            ["/Telecom/MsisdnInventory"] = Keys(TelecomOperationPermissionSets.ReserveMsisdnAny),
            ["/Telecom/DeviceInventory"] = [PermissionCatalog.TelecomDeviceInventoryManage],
            ["/Telecom/TechnicalTicketList"] = Keys(BackOfficePermissionSets.TechnicalTicketListAny),
            ["/Telecom/BackOfficeAuditList"] = Keys(BackOfficePermissionSets.OperationsAny),
            ["/Telecom/UnifiedSearch"] = [PermissionCatalog.TelecomReportsMis],
            ["/Telecom/Customer360Profile"] = Keys(CustomerPermissionSets.ViewAny),
            ["/Customers/CustomerList"] = Keys(CustomerPermissionSets.ViewAny),
            ["/CustomerGroups/CustomerGroupList"] = Keys(ReferenceDataPermissionSets.ReadAny),
            ["/CustomerCategories/CustomerCategoryList"] = Keys(ReferenceDataPermissionSets.ReadAny),
            ["/CustomerContacts/CustomerContactList"] = Keys(CustomerPermissionSets.ViewAny),
            ["/Telecom/ProductCatalog"] = Keys(ProductCatalogPermissionSets.ReadAny),
            ["/Products/ProductList"] = Keys(ProductCatalogPermissionSets.ReadAny),
            ["/Telecom/VasCatalogList"] = [PermissionCatalog.TelecomVasManage],
            ["/TelecomSubscriptionTypes/TelecomSubscriptionTypeList"] = Keys(ReferenceDataPermissionSets.ReadAny),
            ["/Telecom/StrategicAnalytics"] = [PermissionCatalog.TelecomReportsMis],
            ["/Administration/UserList"] = Keys(AdminPermissionSets.UsersManageAny),
            ["/Administration/BranchList"] = Keys(AdminPermissionSets.UsersManageAny),
            ["/Administration/RoleList"] = Keys(AdminPermissionSets.RolesManageAny),
            ["/Administration/GlobalSettings"] = Keys(AdminPermissionSets.SettingsManageAny),
            ["/Administration/CityList"] = Keys(ReferenceDataPermissionSets.ManageAny),
            ["/Administration/AuditLogList"] = Keys(AdminPermissionSets.AuditViewAny),
            ["/Companies/MyCompany"] = Keys(DashboardPermissionSets.AdminAny),
            ["/NumberSequences/NumberSequenceList"] = Keys(DashboardPermissionSets.AdminAny),
        };

    public static bool IsNavUrlAllowed(string? navUrl, IReadOnlySet<string> userPermissions)
    {
        if (string.IsNullOrWhiteSpace(navUrl) || navUrl == "#")
        {
            return false;
        }

        var raw = navUrl.Trim();
        var path = raw.Split('?')[0].TrimEnd('/');
        if (path.Length == 0)
        {
            return false;
        }

        if (string.Equals(path, PermissionLandingResolver.MyProfile, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Rules.TryGetValue(path, out var required) || required.Length == 0)
        {
            return true;
        }

        return required.Any(userPermissions.Contains);
    }

    private static string[] Keys(IReadOnlyList<string> permissions) => permissions.ToArray();

    private static string[] Merge(params IEnumerable<string>[] sources) =>
        sources.SelectMany(s => s).Distinct(StringComparer.Ordinal).ToArray();
}
