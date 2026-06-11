using System;

namespace Infrastructure.SecurityManager.Roles;

/// <summary>Identity role names for Syriatel CRM telecom workflows (seeded + used in [Authorize]).</summary>
public static class TelecomRoles
{
    public const string Admin = "TelecomAdmin";
    public const string Showroom = "TelecomShowroom";
    public const string BackOffice = "TelecomBackOffice";
    public const string CallCenter = "TelecomCallCenter";
    public const string Management = "TelecomManagement";

    // GLOBAL HARDENING: Separation of Duties Roles
    public const string FinancialSupervisor = "Financial_Supervisor";
    public const string NetworkTechnicalAdmin = "Network_Technical_Admin";
    public const string OperationsManager = "Operations_Manager";

    public static readonly string[] All =
    [
        Admin,
        Showroom,
        BackOffice,
        CallCenter,
        Management,
        FinancialSupervisor,
        NetworkTechnicalAdmin,
        OperationsManager,
    ];

    public static bool IsTelecomOnlyRole(string roleName) =>
        All.Contains(roleName, StringComparer.OrdinalIgnoreCase);

    /// <summary>Comma-separated for [Authorize(Roles = "...")].</summary>
    public const string RolesCreateOperation = $"{Showroom},{BackOffice},{Admin}";

    public const string RolesUploadDocument = $"{Showroom},{BackOffice},{Admin}";

    public const string RolesConfirmOperation = $"{Showroom},{BackOffice},{Admin}";

    public const string RolesReadTelecom = $"{Showroom},{BackOffice},{CallCenter},{Management},{Admin}";

    public const string RolesImportSim = $"{BackOffice},{Admin}";

    /// <summary>Primary MSISDN / line type correction (BSS) — showroom/back-office/admin.</summary>
    public const string RolesMutateCustomerPrimaryLine = $"{Showroom},{BackOffice},{Admin}";

    /// <summary>Reference data: telecom line / subscription types (Arabic + English labels).</summary>
    public const string RolesManageTelecomLineTypes = $"{BackOffice},{Admin}";

    public const string RolesDemoIntegrations = $"{Admin}";

    /// <summary>Reveal decrypted national ID and other sensitive PII in UI.</summary>
    public const string RolesViewDecryptedPII = $"{BackOffice},{Management},{Admin}";

    public const string RolesViewBulkImportMonitor = $"{BackOffice},{Admin}";

    public const string RolesManageTechnicalTickets = $"{CallCenter},{BackOffice},{Admin}";

    public const string RolesResolveTechnicalTickets = $"{BackOffice},{Admin},{NetworkTechnicalAdmin},{OperationsManager}";

    /// <summary>عكس معاملات الدفع (Finance / Management tier).</summary>
    public const string RolesReversePayment = $"{Management},{BackOffice},{Admin},{FinancialSupervisor},{OperationsManager}";

    public const string RolesFinancialAudit = $"{FinancialSupervisor},{OperationsManager},{Admin}";
    public const string RolesNetworkTechnicalAudit = $"{NetworkTechnicalAdmin},{OperationsManager},{Admin}";
}
