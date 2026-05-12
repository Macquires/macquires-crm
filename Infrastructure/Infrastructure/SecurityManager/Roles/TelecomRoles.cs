using System;

namespace Infrastructure.SecurityManager.Roles;

/// <summary>Identity role names for Syriatel Macquires CRM workflows (seeded + used in [Authorize]).</summary>
public static class TelecomRoles
{
    public const string Admin = "TelecomAdmin";
    public const string Showroom = "TelecomShowroom";
    public const string BackOffice = "TelecomBackOffice";
    public const string CallCenter = "TelecomCallCenter";
    public const string Management = "TelecomManagement";

    public static readonly string[] All =
    [
        Admin,
        Showroom,
        BackOffice,
        CallCenter,
        Management,
    ];

    public static bool IsTelecomOnlyRole(string roleName) =>
        All.Contains(roleName, StringComparer.OrdinalIgnoreCase);

    /// <summary>Comma-separated for [Authorize(Roles = "...")].</summary>
    public const string RolesCreateOperation = $"{Showroom},{BackOffice},{Admin}";

    public const string RolesUploadDocument = $"{Showroom},{BackOffice},{Admin}";

    public const string RolesConfirmOperation = $"{BackOffice},{Admin}";

    public const string RolesReadTelecom = $"{Showroom},{BackOffice},{CallCenter},{Management},{Admin}";

    public const string RolesImportSim = $"{BackOffice},{Admin}";

    public const string RolesDemoIntegrations = $"{Admin}";
}
