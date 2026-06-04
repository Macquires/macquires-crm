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
    public const string TelecomLineSimSwapRequest = "telecom.line.simswap_request";
    public const string TelecomLineSimSwapApprove = "telecom.line.simswap_approve";
    public const string TelecomLineMigrate = "telecom.line.migrate";
    public const string TelecomLineChangeGsm = "telecom.line.change_gsm";
    public const string TelecomLineTransferOwnership = "telecom.line.transfer_ownership";
    public const string TelecomLineTransferRequest = "telecom.line.transfer_request";
    public const string TelecomLineChangeNumber = "telecom.line.change_number";
    public const string TelecomLineChangeNumberRequest = "telecom.line.change_number_request";
    public const string TelecomLineChangeNumberApprove = "telecom.line.change_number_approve";
    public const string TelecomLineTermination = "telecom.line.termination";
    public const string TelecomLineTerminationRequest = "telecom.line.termination_request";
    public const string TelecomLineTerminationApprove = "telecom.line.termination_approve";
    public const string TelecomLineSuspension = "telecom.line.suspension";
    public const string TelecomLineSuspensionRequest = "telecom.line.suspension_request";
    public const string TelecomLineSuspensionApprove = "telecom.line.suspension_approve";
    public const string TelecomLineReconnect = "telecom.line.reconnect";
    public const string TelecomLineReconnectRequest = "telecom.line.reconnect_request";
    public const string TelecomLineReconnectApprove = "telecom.line.reconnect_approve";
    public const string TelecomVasManage = "telecom.vas.manage";
    public const string TelecomVasToggle = "telecom.vas.toggle";
    public const string TelecomReportsMis = "telecom.reports.mis";
    public const string TelecomTicketForceSync = "telecom.ticket.forcesync";
    public const string TelecomTicketHlrResync = "telecom.ticket.hlrresync";
    public const string TelecomTicketEscalate = "telecom.ticket.escalate";
    public const string TelecomCustomerProvisioning = "telecom.customer.provisioning";
    public const string TelecomNetworkHlrResync = "telecom.network.hlrresync";
    public const string TelecomDeviceInventoryManage = "telecom.device.inventory_manage";
    public const string TelecomDeviceSell = "telecom.device.sell";
    public const string TelecomDeviceSellRequest = "telecom.device.sell_request";
    public const string TelecomDeviceInstallmentApprove = "telecom.device.installment_approve";
    public const string TelecomLineRefund = "telecom.line.refund";
    public const string TelecomLineRefundRequest = "telecom.line.refund_request";
    public const string TelecomLineRefundApprove = "telecom.line.refund_approve";
    public const string TelecomLineCollection = "telecom.line.collection";
    public const string TelecomLineCollectionRequest = "telecom.line.collection_request";
    public const string TelecomLineCollectionApprove = "telecom.line.collection_approve";
    public const string TelecomLineCollectionManage = "telecom.line.collection_manage";
    public const string TelecomLineRecharge = "telecom.line.recharge";

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
        new(TelecomLineSimSwap, "Telecom", "تبديل شريحة SIM (تنفيذ كامل)", "Perform and confirm SIM swap"),
        new(TelecomLineSimSwapRequest, "Telecom", "طلب تبديل شريحة (معرض)", "Submit SIM swap request from showroom"),
        new(TelecomLineSimSwapApprove, "Telecom", "اعتماد تبديل شريحة (باك أوفيس)", "Approve lost/stolen SIM swap"),
        new(TelecomLineMigrate, "Telecom", "ترحيل باقات وعروض المشتركين", "Migrate packages and offerings"),
        new(TelecomLineChangeGsm, "Telecom", "تحويل نوع الخط (مسبق/فاتورة/هجين) CGT", "Change GSM subscription type (CGT)"),
        new(TelecomLineTransferOwnership, "Telecom", "اعتماد نقل ملكية الخط (TKO)", "Approve line ownership transfer (TKO)"),
        new(TelecomLineTransferRequest, "Telecom", "طلب نقل ملكية من المعرض (TKO)", "Submit ownership transfer request (TKO)"),
        new(TelecomLineChangeNumber, "Telecom", "تغيير رقم خط (CNR) — تنفيذ كامل", "Perform internal MSISDN change (CNR)"),
        new(TelecomLineChangeNumberRequest, "Telecom", "طلب تغيير رقم (معرض)", "Submit change-number request from showroom"),
        new(TelecomLineChangeNumberApprove, "Telecom", "اعتماد تغيير رقم مميز (باك أوفيس)", "Approve premium change-number (CNR)"),
        new(TelecomLineTermination, "Telecom", "إنهاء خط (TRM) — تنفيذ كامل", "Perform line termination (TRM)"),
        new(TelecomLineTerminationRequest, "Telecom", "طلب إنهاء خط (معرض)", "Submit line termination from showroom"),
        new(TelecomLineTerminationApprove, "Telecom", "اعتماد إنهاء خط (باك أوفيس)", "Approve fraud/regulatory/collections termination"),
        new(TelecomLineSuspension, "Telecom", "حظر خط مؤقت (SUS) — تنفيذ كامل", "Perform line suspension (SUS)"),
        new(TelecomLineSuspensionRequest, "Telecom", "طلب حظر خط (معرض)", "Submit line suspension from showroom"),
        new(TelecomLineSuspensionApprove, "Telecom", "اعتماد حظر خط (باك أوفيس)", "Approve fraud/regulatory suspension"),
        new(TelecomLineReconnect, "Telecom", "إعادة تفعيل خط (RCN) — تنفيذ كامل", "Perform line reconnect (RCN)"),
        new(TelecomLineReconnectRequest, "Telecom", "طلب إعادة تفعيل (معرض)", "Submit reconnect from showroom"),
        new(TelecomLineReconnectApprove, "Telecom", "اعتماد إعادة تفعيل (باك أوفيس)", "Approve fraud/regulatory reconnect"),
        new(TelecomVasManage, "Telecom", "إدارة كتالوج الخدمات المضافة (VAS)", "Manage VAS catalog"),
        new(TelecomVasToggle, "Telecom", "تفعيل وتعطيل الخدمات المضافة على الخط", "Toggle VAS on subscriber lines"),
        new(TelecomReportsMis, "Telecom", "تقارير MIS التشغيلية والمالية", "MIS operational and financial reports"),
        new(TelecomTicketForceSync, "Telecom", "إعادة ضغط الشحنة وتسوية CBS من التذكرة", "Force CBS charge sync from technical ticket"),
        new(TelecomTicketHlrResync, "Telecom", "مزامنة HLR من تذكرة الدعم", "HLR resync from technical ticket"),
        new(TelecomTicketEscalate, "Telecom", "تصعيد التذكرة للقسم الهندسي", "Escalate technical ticket to Tier-3"),
        new(TelecomCustomerProvisioning, "Telecom", "تفعيل VAS وترحيل الباقات (Customer 360)", "VAS activation and package migration from subscriber profile"),
        new(TelecomNetworkHlrResync, "Telecom", "تبديل شريحة وتفعيل خط (HLR/CBS)", "SIM swap and line activation on core network"),
        new(TelecomDeviceInventoryManage, "Telecom", "إدارة مخزون أجهزة IMEI", "Manage device IMEI inventory"),
        new(TelecomDeviceSell, "Telecom", "بيع جهاز — تنفيذ كامل (DEV-)", "Complete device sale (DEV-)"),
        new(TelecomDeviceSellRequest, "Telecom", "طلب بيع جهاز من المعرض", "Submit device sale from showroom"),
        new(TelecomDeviceInstallmentApprove, "Telecom", "اعتماد تقسيط الأجهزة (فايننس)", "Approve device installment financing"),
        new(TelecomLineRefund, "Telecom", "استرداد تأمين/محفظة (RFD) — تنفيذ كامل", "Complete deposit/wallet refund (RFD)"),
        new(TelecomLineRefundRequest, "Telecom", "طلب استرداد مالي (معرض)", "Submit refund request from showroom"),
        new(TelecomLineRefundApprove, "Telecom", "اعتماد استرداد مبالغ عالية (باك أوفيس)", "Approve high-value / Syriatel Cash refund"),
        new(TelecomLineCollection, "Telecom", "تحصيل وديون معدومة (BDR) — تنفيذ كامل", "Complete collections / bad debt (BDR)"),
        new(TelecomLineCollectionRequest, "Telecom", "طلب تحصيل من المعرض", "Submit collection request from showroom"),
        new(TelecomLineCollectionApprove, "Telecom", "اعتماد شطب/وكالة تحصيل (باك أوفيس)", "Approve write-off / agency referral"),
        new(TelecomLineCollectionManage, "Telecom", "إدارة محفظة التحصيل والـ KPIs", "Manage collections portfolio and KPIs"),
        new(TelecomLineRecharge, "Telecom", "شحن رصيد وقسائم (PAY-)", "Recharge prepaid balance and redeem vouchers"),
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
                TelecomLineRecharge,
                BulkImportMonitor,
                AdminAuditView,
            ],
            [TelecomEnterpriseRoleMatrix.RoleBackOffice] =
            [
                BulkImportUpload,
                BulkImportMonitor,
                TelecomAssetManage,
                TelecomLineMigrate,
                TelecomLineChangeGsm,
                TelecomLineTransferOwnership,
                TelecomLineTransferRequest,
                TelecomLineSimSwapApprove,
                TelecomLineChangeNumber,
                TelecomLineChangeNumberApprove,
                TelecomLineTermination,
                TelecomLineTerminationApprove,
                TelecomLineSuspension,
                TelecomLineSuspensionApprove,
                TelecomLineReconnect,
                TelecomLineReconnectApprove,
                TelecomLineActivate,
                TelecomVasManage,
                TelecomVasToggle,
                TelecomTicketForceSync,
                TelecomTicketHlrResync,
                TelecomTicketEscalate,
                TelecomCustomerProvisioning,
                TelecomNetworkHlrResync,
                TelecomDeviceInventoryManage,
                TelecomDeviceSell,
                TelecomDeviceInstallmentApprove,
                TelecomLineRefund,
                TelecomLineRefundApprove,
                TelecomLineCollection,
                TelecomLineCollectionApprove,
                TelecomLineCollectionManage,
                TelecomLineRecharge,
                CustomerView,
            ],
            [TelecomEnterpriseRoleMatrix.RoleShowroom] =
            [
                CustomerView,
                CustomerCreate,
                CustomerUpdate,
                TelecomLineActivate,
                TelecomLineSimSwap,
                TelecomLineSimSwapRequest,
                TelecomVasToggle,
                TelecomCustomerProvisioning,
                TelecomLineChangeGsm,
                TelecomLineTransferRequest,
                TelecomLineChangeNumber,
                TelecomLineChangeNumberRequest,
                TelecomLineTermination,
                TelecomLineTerminationRequest,
                TelecomLineSuspension,
                TelecomLineSuspensionRequest,
                TelecomLineReconnect,
                TelecomLineReconnectRequest,
                TelecomNetworkHlrResync,
                TelecomDeviceSellRequest,
                TelecomLineRefundRequest,
                TelecomLineCollectionRequest,
                TelecomLineRecharge,
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
