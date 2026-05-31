namespace Infrastructure.SecurityManager.Roles;

public class RoleHelper
{
    /// <summary>Only the five standard Syriatel telecom roles (no legacy CRM navigation roles).</summary>
    public static IReadOnlyList<string> TelecomOperationalRoles { get; } = TelecomRoles.All;

    public static List<string> GetAdminRoles() => TelecomRoles.All.ToList();

    public static string GetProfileRole() => "Profiles";
}
