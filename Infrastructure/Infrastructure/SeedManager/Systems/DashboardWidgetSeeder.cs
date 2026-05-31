using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Systems;

/// <summary>
/// Seed Bento tiles. ProviderKey registry (C# DI):
/// subscriber_count → Customer + SubscriberProfile counts;
/// active_subscriptions → TelecomSubscription;
/// pending_operations → TelecomOperationRequest (PendingDocuments, Confirmed);
/// bulk_import_active → InventoryBulkImportJob;
/// msisdn_available → MsisdnAsset (Available);
/// operations_today → TelecomOperationRequest created today UTC;
/// network_pulse → BillingIntegrationLog success rate;
/// integration_health → BillingIntegrationLog by IntegrationTarget.
/// </summary>
public class DashboardWidgetSeeder
{
    private readonly DataContext _context;

    public DashboardWidgetSeeder(DataContext context) => _context = context;

    public async Task GenerateDataAsync()
    {
        if (await _context.DashboardWidget.AnyAsync())
        {
            return;
        }

        var widgets = new List<DashboardWidget>
        {
            // Executive
            W("exec_subscribers", "المشتركون", "Subscribers", "bi-people", "subscriber_count", "Executive", DashboardWidgetGridSize.Medium, 10, 60),
            W("exec_subscriptions", "خطوط نشطة", "Active lines", "bi-sim", "active_subscriptions", "Executive", DashboardWidgetGridSize.Medium, 11, 60),
            W("exec_network", "نبض الشبكة", "Network pulse", "bi-activity", "network_pulse", "Executive", DashboardWidgetGridSize.Large, 12, 30),
            W("exec_mis", "التحليلات الاستراتيجية", "Strategic analytics", "bi-graph-up-arrow", null, "Executive", DashboardWidgetGridSize.Medium, 20, kind: DashboardWidgetKind.Cta, ctaUrl: "/Dashboards/DefaultDashboard#strategic-analytics", ctaAr: "عرض التحليلات"),

            // CallCenter
            W("cc_search", "بحث عالمي", "Omni search", "bi-search", null, "CallCenter", DashboardWidgetGridSize.Full, 1, kind: DashboardWidgetKind.OmniSearch),
            W("cc_pending", "عمليات معلقة", "Pending ops", "bi-hourglass-split", "pending_operations", "CallCenter", DashboardWidgetGridSize.Medium, 10, 30),
            W("cc_network", "حالة التكامل", "Integration", "bi-broadcast", "network_pulse", "CallCenter", DashboardWidgetGridSize.Medium, 11, 60),
            W("cc_subscribers", "سجل المشتركين", "Registry", "bi-person-lines-fill", "subscriber_count", "CallCenter", DashboardWidgetGridSize.Medium, 12, 120),

            // Retail
            W("retail_ops_today", "عمليات اليوم", "Ops today", "bi-lightning-charge", "operations_today", "Retail", DashboardWidgetGridSize.Medium, 10, 60),
            W("retail_msisdn", "أرقام متاحة", "MSISDN pool", "bi-telephone", "msisdn_available", "Retail", DashboardWidgetGridSize.Medium, 11, 60),
            W("retail_activate", "تفعيل خط", "Activate", "bi-plus-circle", null, "Retail", DashboardWidgetGridSize.Medium, 5, kind: DashboardWidgetKind.Cta, ctaUrl: "/Telecom/TelecomHub", ctaAr: "معالج التفعيل"),
            W("retail_catalog", "كتالوج العروض", "Catalog", "bi-box-seam", null, "Retail", DashboardWidgetGridSize.Medium, 20, kind: DashboardWidgetKind.Cta, ctaUrl: "/Telecom/ProductCatalog", ctaAr: "الكتالوج"),

            // BackOffice
            W("bo_pending", "موافقات معلقة", "Approvals", "bi-clipboard-check", "pending_operations", "BackOffice", DashboardWidgetGridSize.Medium, 10, 30),
            W("bo_bulk", "استيراد نشط", "Bulk import", "bi-cloud-upload", "bulk_import_active", "BackOffice", DashboardWidgetGridSize.Medium, 11, 30),
            W("bo_hub", "مركز العمليات", "NOC", "bi-broadcast", null, "BackOffice", DashboardWidgetGridSize.Medium, 12, kind: DashboardWidgetKind.Cta, ctaUrl: "/Telecom/TelecomHub", ctaAr: "مركز العمليات"),

            // SysAdmin
            W("admin_integrations", "صحة التكاملات", "Integrations", "bi-hdd-network", "integration_health", "SysAdmin", DashboardWidgetGridSize.Large, 10, 60),
            W("admin_bulk", "مهام خلفية", "Background jobs", "bi-cpu", "bulk_import_active", "SysAdmin", DashboardWidgetGridSize.Medium, 11, 30),
            W("admin_subscribers", "المشتركون", "Subscribers", "bi-people", "subscriber_count", "SysAdmin", DashboardWidgetGridSize.Medium, 12, 120),
        };

        // Shared across all personas
        widgets.Add(W("all_customers", "إجمالي العملاء", "Customers", "bi-person-badge", "subscriber_count",
            "Executive,CallCenter,Retail,BackOffice,SysAdmin", DashboardWidgetGridSize.Small, 0, 120));

        await _context.DashboardWidget.AddRangeAsync(widgets);
        await _context.SaveChangesAsync();
    }

    private static DashboardWidget W(
        string key,
        string titleAr,
        string titleEn,
        string icon,
        string? providerKey,
        string personas,
        DashboardWidgetGridSize grid,
        int sort,
        int? refreshSec = null,
        DashboardWidgetKind kind = DashboardWidgetKind.Stat,
        string? ctaUrl = null,
        string? ctaAr = null) =>
        new()
        {
            WidgetKey = key,
            TitleAr = titleAr,
            TitleEn = titleEn,
            Icon = icon,
            ProviderKey = providerKey,
            PersonasAllowed = personas,
            GridSize = grid,
            SortOrder = sort,
            RefreshIntervalSeconds = refreshSec,
            WidgetKind = kind,
            CtaUrl = ctaUrl,
            CtaLabelAr = ctaAr,
            IsActive = true,
        };
}
