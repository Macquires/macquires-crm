namespace Application.Common.Security;

/// <summary>
/// Resolves post-login landing routes from effective permission keys (priority matrix).
/// </summary>
public static class PermissionLandingResolver
{
    public const string MyProfile = "/Profiles/MyProfile";
    public const string AdministrationUsers = "/Administration/UserList";
    public const string BackOfficeDashboard = "/Telecom/BackOfficeDashboard";
    public const string TelecomHub = "/Telecom/TelecomHub";
    /// <summary>Legacy dashboard path; operators with customer.view land on <see cref="TelecomHub"/> instead.</summary>
    public const string DefaultCrmDashboard = TelecomHub;

    public static string Resolve(IReadOnlySet<string> permissions)
    {
        if (permissions == null || permissions.Count == 0)
        {
            return MyProfile;
        }

        if (HasAny(
                permissions,
                PermissionCatalog.AdminUsersManage,
                PermissionCatalog.AdminSettingsManage,
                PermissionCatalog.AdminRolesManage))
        {
            return AdministrationUsers;
        }

        if (HasAny(
                permissions,
                PermissionCatalog.BulkImportUpload,
                PermissionCatalog.BulkImportMonitor,
                PermissionCatalog.TelecomAssetManage))
        {
            return BackOfficeDashboard;
        }

        if (permissions.Contains(PermissionCatalog.CustomerView))
        {
            return DefaultCrmDashboard;
        }

        return MyProfile;
    }

    public static string Resolve(IEnumerable<string> permissions) =>
        Resolve(permissions.ToHashSet(StringComparer.OrdinalIgnoreCase));

    private static bool HasAny(IReadOnlySet<string> permissions, params string[] keys) =>
        keys.Any(k => permissions.Contains(k));
}
