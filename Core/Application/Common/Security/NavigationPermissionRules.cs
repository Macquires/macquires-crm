namespace Application.Common.Security;

/// <summary>Maps portal nav URLs to permission keys (user needs any listed key).</summary>
public static class NavigationPermissionRules
{
  private static readonly IReadOnlyDictionary<string, string[]> Rules =
      new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
      {
          ["/Dashboards/DefaultDashboard"] =
          [
              PermissionCatalog.CustomerView,
              PermissionCatalog.TelecomReportsMis,
              PermissionCatalog.AdminUsersManage,
              PermissionCatalog.AdminSettingsManage,
              PermissionCatalog.AdminRolesManage,
          ],
          ["/Dashboards/DashboardWidgetList"] = [PermissionCatalog.AdminSettingsManage],
          ["/Telecom/BackOfficeDashboard"] =
          [
              PermissionCatalog.BulkImportUpload,
              PermissionCatalog.BulkImportMonitor,
              PermissionCatalog.TelecomAssetManage,
          ],
          ["/Telecom/TelecomHub"] =
          [
              PermissionCatalog.CustomerView,
              PermissionCatalog.TelecomLineActivate,
              PermissionCatalog.TelecomLineSimSwap,
              PermissionCatalog.TelecomLineSimSwapRequest,
              PermissionCatalog.TelecomLineMigrate,
          ],
          ["/Telecom/BillingIntegration"] =
          [
              PermissionCatalog.CustomerView,
              PermissionCatalog.TelecomLineActivate,
              PermissionCatalog.TelecomLineSimSwap,
              PermissionCatalog.TelecomLineSimSwapRequest,
              PermissionCatalog.TelecomLineMigrate,
              PermissionCatalog.TelecomReportsMis,
          ],
          ["/Telecom/IntegrationMonitor"] = [PermissionCatalog.AdminIntegrationMonitor],
          ["/Telecom/BulkImportMonitor"] =
          [
              PermissionCatalog.BulkImportMonitor,
              PermissionCatalog.BulkImportUpload,
          ],
          ["/Telecom/MsisdnInventory"] = [PermissionCatalog.TelecomAssetManage],
          ["/Telecom/DeviceInventory"] = [PermissionCatalog.TelecomDeviceInventoryManage],
          ["/Telecom/TechnicalTicketList"] =
          [
              PermissionCatalog.CustomerView,
              PermissionCatalog.BulkImportUpload,
              PermissionCatalog.TelecomAssetManage,
          ],
          ["/Telecom/BackOfficeAuditList"] =
          [
              PermissionCatalog.BulkImportUpload,
              PermissionCatalog.TelecomAssetManage,
          ],
          ["/Telecom/UnifiedSearch"] =
          [
              PermissionCatalog.CustomerView,
              PermissionCatalog.TelecomLineActivate,
              PermissionCatalog.TelecomLineSimSwap,
              PermissionCatalog.TelecomLineSimSwapRequest,
              PermissionCatalog.TelecomLineMigrate,
          ],
          ["/Telecom/Customer360Profile"] =
          [
              PermissionCatalog.CustomerView,
              PermissionCatalog.TelecomLineActivate,
              PermissionCatalog.TelecomLineSimSwap,
              PermissionCatalog.TelecomLineSimSwapRequest,
              PermissionCatalog.TelecomLineMigrate,
          ],
          ["/Customers/CustomerList"] = [PermissionCatalog.CustomerView],
          ["/CustomerGroups/CustomerGroupList"] = [PermissionCatalog.TelecomAssetManage],
          ["/CustomerCategories/CustomerCategoryList"] = [PermissionCatalog.TelecomAssetManage],
          ["/CustomerContacts/CustomerContactList"] = [PermissionCatalog.CustomerView],
          ["/Telecom/ProductCatalog"] = [PermissionCatalog.TelecomLineActivate],
          ["/Products/ProductList"] = [PermissionCatalog.TelecomAssetManage],
          ["/Telecom/VasCatalogList"] = [PermissionCatalog.TelecomVasManage],
          ["/TelecomSubscriptionTypes/TelecomSubscriptionTypeList"] =
              [PermissionCatalog.TelecomLineActivate],
          ["/Telecom/StrategicAnalytics"] = [PermissionCatalog.TelecomReportsMis],
          ["/Administration/UserList"] = [PermissionCatalog.AdminUsersManage],
          ["/Administration/BranchList"] = [PermissionCatalog.AdminUsersManage],
          ["/Administration/RoleList"] = [PermissionCatalog.AdminRolesManage],
          ["/Administration/GlobalSettings"] = [PermissionCatalog.AdminSettingsManage],
          ["/Administration/AuditLogList"] = [PermissionCatalog.AdminAuditView],
          ["/Companies/MyCompany"] = [PermissionCatalog.AdminSettingsManage],
          ["/NumberSequences/NumberSequenceList"] = [PermissionCatalog.AdminSettingsManage],
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
}
