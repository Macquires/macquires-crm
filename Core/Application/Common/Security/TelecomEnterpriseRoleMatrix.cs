namespace Application.Common.Security;

/// <summary>
/// Global telecom RBAC matrix — five standard Identity roles and their default personas.
/// Keep in sync with <c>Infrastructure.SecurityManager.Roles.TelecomRoles</c>.
/// </summary>
public static class TelecomEnterpriseRoleMatrix
{
    public const string RoleAdmin = "TelecomAdmin";
    public const string RoleManagement = "TelecomManagement";
    public const string RoleBackOffice = "TelecomBackOffice";
    public const string RoleShowroom = "TelecomShowroom";
    public const string RoleCallCenter = "TelecomCallCenter";
    public const string RoleFinancialSupervisor = "Financial_Supervisor";
    public const string RoleNetworkTechnicalAdmin = "Network_Technical_Admin";
    public const string RoleOperationsManager = "Operations_Manager";

    public static readonly string[] StandardRoles =
    [
        RoleAdmin,
        RoleManagement,
        RoleBackOffice,
        RoleShowroom,
        RoleCallCenter,
    ];

    public static readonly IReadOnlyDictionary<string, RoleDefinition> Definitions =
        new Dictionary<string, RoleDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [RoleAdmin] = new(
                RoleAdmin,
                "SysAdmin",
                "مدير النظام الأعلى",
                "System superuser (full access)",
                "مدير النظام الأعلى — صلاحيات كاملة على المنصة وسجل الرقابة."),
            [RoleManagement] = new(
                RoleManagement,
                "Executive",
                "الإدارة العليا",
                "Executive / branch management",
                "الإدارة العليا — تقارير MIS ومراقبة الاستيراد وسجل الرقابة."),
            [RoleBackOffice] = new(
                RoleBackOffice,
                "BackOffice",
                "العمليات الخلفية",
                "Back-office / inventory operations",
                "العمليات الخلفية — استيراد ضخم، مستودع أرقام، وترحيل الاشتراكات."),
            [RoleShowroom] = new(
                RoleShowroom,
                "Retail",
                "نقطة البيع / خدمة العملاء",
                "Retail POS (front desk)",
                "موظفو نقاط البيع — بحث، تعديل أساسي، تفعيل خطوط وتبديل SIM."),
            [RoleCallCenter] = new(
                RoleCallCenter,
                "CallCenter",
                "مركز الاتصال",
                "Call center (111)",
                "مركز الاتصال — بحث ومشاهدة فقط بدون تعديل حساس."),
        };

    public static bool IsStandardRole(string? roleName) =>
        !string.IsNullOrWhiteSpace(roleName)
        && Definitions.ContainsKey(roleName.Trim());

    public static string? GetDefaultPersonaName(string? roleName) =>
        roleName != null && Definitions.TryGetValue(roleName.Trim(), out var def)
            ? def.DefaultPersona
            : null;
}

public sealed record RoleDefinition(
    string RoleName,
    string DefaultPersona,
    string LabelAr,
    string LabelEn,
    string DescriptionAr);
