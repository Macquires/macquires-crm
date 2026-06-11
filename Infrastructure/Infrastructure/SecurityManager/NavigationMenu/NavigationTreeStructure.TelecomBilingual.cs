namespace Infrastructure.SecurityManager.NavigationMenu;

/// <summary>
/// Arabic + English sidebar labels for BSS telecom navigation (Syriatel operator matrix).
/// </summary>
public static partial class NavigationTreeStructure
{
    private static readonly Dictionary<string, (string Ar, string En)> TelecomLeafLabels =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["/dashboards/defaultdashboard"] = ("لوحة القيادة الرئيسية", "Executive dashboard"),
            ["/telecom/telecommisreports"] = ("تقارير ضمان الخدمة (MIS)", "Service assurance reports (MIS)"),
            ["/telecom/telecomhub"] = ("مركز عمليات الشبكة (NOC)", "Network operations center (NOC)"),
            ["/telecom/unifiedsearch"] = ("سجل الخطوط والاشتراكات", "Subscriber lines & assets"),
            ["/telecom/msisdninventory"] = ("مستودع الأرقام والشرائح", "SIM & MSISDN resource pool"),
            ["/telecom/backofficedashboard"] = ("مركز العمليات الخلفية", "Back-office operations center"),
            ["/telecom/technicalticketlist"] = ("إدارة التذاكر الفنية", "Technical ticketing suite"),
            ["/telecom/bulkimportmonitor"] = ("مراقبة الاستيراد الضخم", "Bulk provisioning monitor"),
            ["/telecom/billingintegration"] = ("التكامل مع نظام الفوترة", "Billing integration"),
            ["/telecom/inintegration"] = ("التكامل مع الشبكة الذكية (IN)", "IN integration"),
            ["/telecom/hlrprovisioning"] = ("تزويد HLR / HSS", "HLR provisioning"),
            ["/telecom/deviceinventory"] = ("مخزون الأجهزة (IMEI)", "Device inventory (IMEI)"),
            ["/telecom/integrationmonitor"] = ("سجل ربط الشبكة والتكاملات", "Core network integration logs"),
            ["/customers/customerlist"] = ("سجل المشتركين (BSS)", "Subscriber registry (BSS)"),
            ["/customergroups/customergrouplist"] = ("المجموعات والحسابات", "Accounts & corporate groups"),
            ["/customercategories/customercategorylist"] = ("شرائح وتصنيفات العملاء", "Subscriber segments"),
            ["/customercontacts/customercontactlist"] = ("جهات اتصال المشتركين", "Subscriber contacts"),
            ["/telecom/productcatalog"] = ("العروض والخدمات التجارية", "Commercial offerings catalog"),
            ["/products/productlist"] = ("مواصفات المنتجات التقنية", "Technical product specs"),
            ["/telecom/vascataloglist"] = ("كتالوج الخدمات المضافة (VAS)", "Value-added services (VAS) catalog"),
            ["/telecomsubscriptiontypes/telecomsubscriptiontypelist"] = ("أنواع خطوط الاشتراك", "Subscription line types"),
            ["/administration"] = ("الإدارة والنظام", "Governance & system"),
            ["/administration/userlist"] = ("إدارة المستخدمين", "User management"),
            ["/administration/branchlist"] = ("إدارة الفروع", "Branch management"),
            ["/administration/rolelist"] = ("الصلاحيات والأدوار (RBAC)", "Roles & permissions matrix"),
            ["/administration/globalsettings"] = ("الإعدادات العامة للشبكة", "Core platform settings"),
            ["/administration/auditloglist"] = ("سجل الرقابة (الصندوق الأسود)", "System audit trail (black box)"),
            ["/dashboards/dashboardwidgetlist"] = ("عناصر لوحة القيادة", "Dashboard widgets (Bento)"),
            ["/companies/mycompany"] = ("بيانات المشغّل", "Operator company profile"),
            ["/numbersequences/numbersequencelist"] = ("تسلسل الأرقام التشغيلي", "Operational numbering"),
            ["/profiles/myprofile"] = ("ملفي التشغيلي", "My operator profile"),
        };

    /// <summary>Module headers: key = exact <c>Name</c> from JSON before mutation.</summary>
    private static readonly Dictionary<string, (string Ar, string En)> TelecomModuleLabels =
        new(StringComparer.Ordinal)
        {
            ["لوحات القيادة"] = ("لوحات القيادة", "Dashboards"),
            ["العمليات التشغيلية"] = ("العمليات التشغيلية", "Operations"),
            ["إدارة المشتركين"] = ("إدارة المشتركين", "Customer management"),
            ["كتالوج المنتجات"] = ("كتالوج المنتجات", "Product catalog"),
            ["الحوكمة والنظام"] = ("الحوكمة والنظام", "Governance & system"),
            ["حسابي"] = ("حسابي", "My account"),
        };

    private static string NormalizeNavPath(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url == "#")
        {
            return string.Empty;
        }

        var pathPart = url.Trim().Split('#')[0].TrimEnd('/');
        if (pathPart.Length == 0)
        {
            return string.Empty;
        }

        var lower = pathPart.ToLowerInvariant();
        return lower.StartsWith('/') ? lower : "/" + lower;
    }

    private static void ApplyBilingualTelecomLabels(List<JsonStructureItem>? roots)
    {
        if (roots == null)
        {
            return;
        }

        foreach (var item in roots)
        {
            ApplyBilingualToItem(item);
        }
    }

    private static void ApplyBilingualToItem(JsonStructureItem item)
    {
        if (item.Children is { Count: > 0 })
        {
            foreach (var ch in item.Children)
            {
                ApplyBilingualToItem(ch);
            }
        }

        if (!item.IsModule && !string.IsNullOrWhiteSpace(item.URL) && item.URL != "#")
        {
            var key = NormalizeNavPath(item.URL);
            if (TelecomLeafLabels.TryGetValue(key, out var leaf))
            {
                item.Name = leaf.Ar;
                item.NameEn = leaf.En;
                return;
            }
        }

        if (item.IsModule && !string.IsNullOrEmpty(item.Name) && TelecomModuleLabels.TryGetValue(item.Name, out var mod))
        {
            item.Name = mod.Ar;
            item.NameEn = mod.En;
            return;
        }

        item.NameEn ??= item.Name ?? string.Empty;
    }
}
