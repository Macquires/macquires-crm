namespace Application.Common.Security;

/// <summary>Production telecom permission keys for RBAC matrix (seed + MediatR + UI).</summary>
public static class PermissionCatalog
{
    // Administration & governance
    public const string AdminUsersManage = "admin.users.manage";
    public const string AdminRolesManage = "admin.roles.manage";
    public const string AdminSettingsManage = "admin.settings.manage";
    public const string AdminAuditView = "admin.audit.view";
    public const string AdminIntegrationMonitor = "admin.integration.monitor";

    // CRM
    public const string CustomerView = "customer.view";
    public const string CustomerViewPii = "customer.view_pii";
    public const string CustomerCreate = "customer.create";
    public const string CustomerUpdate = "customer.update";

    // BSS / telecom operations
    public const string TelecomAssetManage = "telecom.asset.manage";
    public const string TelecomLineActivate = "telecom.line.activate";
    public const string TelecomLineSimSwap = "telecom.line.simswap";
    public const string TelecomLineMigrate = "telecom.line.migrate";
    public const string TelecomVasManage = "telecom.vas.manage";
    public const string TelecomVasToggle = "telecom.vas.toggle";
    public const string TelecomReportsMis = "telecom.reports.mis";
    public const string TelecomTicketForceSync = "telecom.ticket.forcesync";
    public const string TelecomTicketHlrResync = "telecom.ticket.hlrresync";
    public const string TelecomTicketEscalate = "telecom.ticket.escalate";
    public const string TelecomCustomerProvisioning = "telecom.customer.provisioning";
    public const string TelecomNetworkHlrResync = "telecom.network.hlrresync";

    // Bulk engine
    public const string BulkImportUpload = "bulk.import.upload";
    public const string BulkImportMonitor = "bulk.import.monitor";

    public static IReadOnlyList<PermissionDefinitionDto> All { get; } =
    [
        new(AdminUsersManage, "Administration", "إدارة المستخدمين والهيكل التنظيمي", "Manage users and org structure"),
        new(AdminRolesManage, "Administration", "إدارة مصفوفة الأدوار والصلاحيات", "Manage roles and permission matrix"),
        new(AdminSettingsManage, "Administration", "لوحة التحكم المركزية", "Global settings and integrations"),
        new(AdminAuditView, "Administration", "استعراض سجل الرقابة", "View audit trail"),
        new(AdminIntegrationMonitor, "Administration", "مراقبة تكاملات الشبكة (HLR/CBS/SMS)", "Monitor core network integration logs"),
        new(CustomerView, "Customers", "البحث عن المشتركين وفتح الملفات", "Search and open subscriber profiles"),
        new(CustomerViewPii, "Customers", "الاطلاع على البيانات الحساسة (PII)", "View decrypted national ID and PII"),
        new(CustomerCreate, "Customers", "إنشاء مشترك جديد", "Create new subscriber"),
        new(CustomerUpdate, "Customers", "تعديل بيانات المشترك", "Update subscriber profile"),
        new(TelecomAssetManage, "Telecom", "إدارة مستودع الأرقام وشرائح SIM", "Manage MSISDN pool and SIM inventory"),
        new(TelecomLineActivate, "Telecom", "تفعيل خط أو حزمة جديدة", "Activate line or package"),
        new(TelecomLineSimSwap, "Telecom", "تبديل شريحة SIM (SIM Swap)", "Perform SIM swap"),
        new(TelecomLineMigrate, "Telecom", "ترحيل باقات وعروض المشتركين", "Migrate packages and offerings"),
        new(TelecomVasManage, "Telecom", "إدارة كتالوج الخدمات المضافة (VAS)", "Manage VAS catalog"),
        new(TelecomVasToggle, "Telecom", "تفعيل وتعطيل الخدمات المضافة على الخط", "Toggle VAS on subscriber lines"),
        new(TelecomReportsMis, "Telecom", "تقارير MIS التشغيلية والمالية", "MIS operational and financial reports"),
        new(TelecomTicketForceSync, "Telecom", "إعادة ضغط الشحنة وتسوية CBS من التذكرة", "Force CBS charge sync from technical ticket"),
        new(TelecomTicketHlrResync, "Telecom", "مزامنة HLR من تذكرة الدعم", "HLR resync from technical ticket"),
        new(TelecomTicketEscalate, "Telecom", "تصعيد التذكرة للقسم الهندسي", "Escalate technical ticket to Tier-3"),
        new(TelecomCustomerProvisioning, "Telecom", "تفعيل VAS وترحيل الباقات (Customer 360)", "VAS activation and package migration from subscriber profile"),
        new(TelecomNetworkHlrResync, "Telecom", "تبديل شريحة وتفعيل خط (HLR/CBS)", "SIM swap and line activation on core network"),
        new(BulkImportUpload, "BulkImport", "رفع ملفات الاستيراد الضخم", "Upload bulk import files"),
        new(BulkImportMonitor, "BulkImport", "مراقبة الاستيراد الضخم", "Monitor bulk import jobs and counters"),
    ];

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultRoleGrants { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [TelecomEnterpriseRoleMatrix.RoleAdmin] = All.Select(p => p.Key).ToList(),
            [TelecomEnterpriseRoleMatrix.RoleManagement] =
            [
                CustomerView,
                TelecomReportsMis,
                BulkImportMonitor,
                AdminAuditView,
            ],
            [TelecomEnterpriseRoleMatrix.RoleBackOffice] =
            [
                BulkImportUpload,
                BulkImportMonitor,
                TelecomAssetManage,
                TelecomLineMigrate,
                TelecomLineActivate,
                TelecomVasManage,
                TelecomVasToggle,
                TelecomTicketForceSync,
                TelecomTicketHlrResync,
                TelecomTicketEscalate,
                TelecomCustomerProvisioning,
                TelecomNetworkHlrResync,
                CustomerView,
            ],
            [TelecomEnterpriseRoleMatrix.RoleShowroom] =
            [
                CustomerView,
                CustomerCreate,
                CustomerUpdate,
                TelecomLineActivate,
                TelecomLineSimSwap,
                TelecomVasToggle,
                TelecomCustomerProvisioning,
                TelecomNetworkHlrResync,
                BulkImportMonitor,
            ],
            [TelecomEnterpriseRoleMatrix.RoleCallCenter] =
            [
                CustomerView,
                TelecomVasToggle,
                TelecomCustomerProvisioning,
            ],
        };

    public static bool IsKnownKey(string? key) =>
        !string.IsNullOrWhiteSpace(key)
        && All.Any(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
}

public sealed record PermissionDefinitionDto(
    string Key,
    string Module,
    string LabelAr,
    string LabelEn);
