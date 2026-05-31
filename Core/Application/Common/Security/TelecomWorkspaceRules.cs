using Application.Common.Services.SecurityManager;
using Application.Common.Telecom;

namespace Application.Common.Security;

/// <summary>
/// Syriatel telecom workspace: strict roles use persona navigation + RBAC intersection.
/// </summary>
public static class TelecomWorkspaceRules
{
    public static bool IsStrictTelecomWorkspaceUser(IReadOnlyList<string>? roleNames)
    {
        if (roleNames == null || roleNames.Count == 0)
        {
            return false;
        }

        foreach (var role in roleNames)
        {
            if (!TelecomEnterpriseRoleMatrix.IsStandardRole(role))
            {
                return false;
            }
        }

        return true;
    }

    public static string ResolveSessionLandingPath(
        IReadOnlyList<string> roleNames,
        TelecomMenuPersona? primaryPersona,
        IReadOnlySet<string> permissions) =>
        IsStrictTelecomWorkspaceUser(roleNames) && primaryPersona.HasValue
            ? TelecomPersonaLanding.ResolvePath(primaryPersona)
            : PermissionLandingResolver.Resolve(permissions);
}
