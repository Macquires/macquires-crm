using Application.Common.Security;
using Application.Common.Services.SecurityManager;

namespace Application.Common.Telecom;

/// <summary>Maps UI persona to primary Identity telecom role (Syriatel workspace).</summary>
public static class TelecomPersonaRoleMapper
{
    private static readonly IReadOnlyDictionary<TelecomMenuPersona, string> PersonaToRole =
        new Dictionary<TelecomMenuPersona, string>
        {
            [TelecomMenuPersona.Executive] = TelecomEnterpriseRoleMatrix.RoleManagement,
            [TelecomMenuPersona.SysAdmin] = TelecomEnterpriseRoleMatrix.RoleAdmin,
            [TelecomMenuPersona.BackOffice] = TelecomEnterpriseRoleMatrix.RoleBackOffice,
            [TelecomMenuPersona.CallCenter] = TelecomEnterpriseRoleMatrix.RoleCallCenter,
            [TelecomMenuPersona.Retail] = TelecomEnterpriseRoleMatrix.RoleShowroom,
        };

    public static string? GetRoleForPersona(TelecomMenuPersona persona) =>
        PersonaToRole.TryGetValue(persona, out var role) ? role : null;

    public static TelecomMenuPersona? GetPersonaForRole(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return null;
        }

        foreach (var pair in PersonaToRole)
        {
            if (string.Equals(pair.Value, roleName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return pair.Key;
            }
        }

        return null;
    }

    public static IReadOnlyList<string> TelecomRolesToReplace { get; } = TelecomEnterpriseRoleMatrix.StandardRoles;
}
