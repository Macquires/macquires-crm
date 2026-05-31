using Application.Common.Services.SecurityManager;

namespace Application.Common.Telecom;

/// <summary>Resolves the primary sidebar/dashboard persona from Identity roles (highest priority wins).</summary>
public static class TelecomPersonaResolver
{
    /// <summary>Must match <c>Infrastructure.SecurityManager.Roles.TelecomRoles</c>.</summary>
    /// <summary>SysAdmin before Executive so default admin (all roles) keeps full navigation.</summary>
    private static readonly (TelecomMenuPersona Persona, string Role)[] PriorityOrder =
    [
        (TelecomMenuPersona.SysAdmin, "TelecomAdmin"),
        (TelecomMenuPersona.Executive, "TelecomManagement"),
        (TelecomMenuPersona.BackOffice, "TelecomBackOffice"),
        (TelecomMenuPersona.CallCenter, "TelecomCallCenter"),
        (TelecomMenuPersona.Retail, "TelecomShowroom"),
    ];

    public static TelecomMenuPersona? ResolvePrimary(
        IReadOnlyList<string>? roleNames,
        TelecomMenuPersona? explicitPersona = null)
    {
        var fromRoles = ResolveFromRoles(roleNames);
        if (fromRoles.HasValue)
        {
            return fromRoles;
        }

        return explicitPersona;
    }

    public static TelecomMenuPersona? ResolveFromRoles(IReadOnlyList<string>? roleNames)
    {
        if (roleNames == null || roleNames.Count == 0)
        {
            return null;
        }

        var set = new HashSet<string>(roleNames, StringComparer.OrdinalIgnoreCase);
        foreach (var (persona, role) in PriorityOrder)
        {
            if (set.Contains(role))
            {
                return persona;
            }
        }

        return null;
    }

    public static bool HasAnyTelecomRole(IReadOnlyList<string>? roleNames) =>
        ResolvePrimary(roleNames) != null;
}
