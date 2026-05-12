using Infrastructure.SecurityManager.NavigationMenu;

namespace Infrastructure.SecurityManager.Roles;

public class RoleHelper
{
    /// <summary>
    /// Explicit telecom workflow roles (Syriatel demo). Seeded alongside navigation-derived roles.
    /// Map Showroom → draft-only APIs; BackOffice → confirm / billing integration.
    /// </summary>
    public static IReadOnlyList<string> TelecomOperationalRoles { get; } =
    [
        TelecomRoles.Admin,
        TelecomRoles.Showroom,
        TelecomRoles.BackOffice,
        TelecomRoles.CallCenter,
        TelecomRoles.Management,
    ];

    public static List<string> GetAdminRoles()
    {
        var roles = NavigationTreeStructure.GetCompleteFirstMenuNavigationSegment();
        foreach (var role in TelecomOperationalRoles)
        {
            if (!roles.Contains(role))
            {
                roles.Add(role);
            }
        }

        return roles;
    }

    //make sure or cross check with NavigationTreeStructure
    public static string GetProfileRole()
    {
        return "Profiles";
    }
}
