namespace Application.Common.Security;

/// <summary>Permission-based scope and RLS rules (role-name agnostic).</summary>
public static class PermissionScopeRules
{
    public static readonly IReadOnlyList<string> NationalDataScopeAny =
    [
        PermissionCatalog.AdminSettingsManage,
        PermissionCatalog.AdminUsersManage,
        PermissionCatalog.TelecomReportsMis,
    ];

    public static readonly IReadOnlyList<string> PersonaPreviewAny =
    [
        PermissionCatalog.AdminSettingsManage,
        PermissionCatalog.AdminUsersManage,
        PermissionCatalog.TelecomReportsMis,
    ];

    public static bool CanBypassBranchFilter(IEnumerable<string> permissions) =>
        HasAny(permissions, NationalDataScopeAny);

    public static bool HasNationalAdminScope(IEnumerable<string> permissions) =>
        HasAny(
            permissions,
            PermissionCatalog.AdminSettingsManage,
            PermissionCatalog.AdminUsersManage);

    public static bool HasExecutiveMisScope(IEnumerable<string> permissions) =>
        permissions.Any(p => string.Equals(p, PermissionCatalog.TelecomReportsMis, StringComparison.OrdinalIgnoreCase));

    public static bool CanPreviewPersona(IEnumerable<string> permissions) =>
        HasAny(permissions, PersonaPreviewAny);

    private static bool HasAny(IEnumerable<string> permissions, params string[] keys)
    {
        var set = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return keys.Any(set.Contains);
    }

    private static bool HasAny(IEnumerable<string> permissions, IReadOnlyList<string> keys) =>
        HasAny(permissions, keys.ToArray());
}
