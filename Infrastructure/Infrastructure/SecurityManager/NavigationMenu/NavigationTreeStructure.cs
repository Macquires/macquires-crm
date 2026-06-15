using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Application.Common.Telecom;
using Infrastructure.SecurityManager.Roles;
using System.Text.Json;

namespace Infrastructure.SecurityManager.NavigationMenu;

public class JsonStructureItem
{
    public string? URL { get; set; }
    public string? Name { get; set; }
    /// <summary>Optional English label from JSON; usually filled by <see cref="ApplyBilingualTelecomLabels"/>.</summary>
    public string? NameEn { get; set; }
    public bool IsModule { get; set; }
    public List<string>? Personas { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public string? BadgeKey { get; set; }
    public bool IsQuickAction { get; set; }
    public List<JsonStructureItem> Children { get; set; } = new List<JsonStructureItem>();
}

public static partial class NavigationTreeStructure
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static readonly string JsonStructure = """
    [
        {
            "URL": "#",
            "Name": "لوحات القيادة",
            "IsModule": true,
            "Icon": "bi-grid-1x2",
            "SortOrder": 1,
            "Children": [
                { "URL": "/Dashboards/DefaultDashboard", "Name": "لوحة القيادة الرئيسية", "IsModule": false, "Personas": ["Executive","CallCenter","Retail","BackOffice","SysAdmin"], "Icon": "bi-grid-1x2", "SortOrder": 1 }
            ]
        },
        {
            "URL": "#",
            "Name": "مركز قيادة الإدارة العليا",
            "IsModule": true,
            "Personas": ["Executive","SysAdmin"],
            "Icon": "bi-command",
            "SortOrder": 1,
            "Children": [
                { "URL": "/Executive/CommandCenter", "Name": "لوحة القيادة التنفيذية", "IsModule": false, "Personas": ["Executive","SysAdmin"], "Icon": "bi-speedometer2", "SortOrder": 1, "BadgeKey": "criticalExecutiveExceptions", "IsQuickAction": true },
                { "URL": "/Telecom/StrategicAnalytics", "Name": "التحليلات الاستراتيجية", "IsModule": false, "Personas": ["Executive","SysAdmin"], "Icon": "bi-graph-up-arrow", "SortOrder": 2 },
                { "URL": "/Dashboards/DefaultDashboard#strategic-analytics", "Name": "MIS — لوحة موحّدة", "IsModule": false, "Personas": ["Executive","SysAdmin"], "Icon": "bi-pie-chart", "SortOrder": 3 },
                { "URL": "/Dashboards/DefaultDashboard#mis-reports", "Name": "تقارير MIS السريعة", "IsModule": false, "Personas": ["Executive","SysAdmin"], "Icon": "bi-speedometer", "SortOrder": 4 },
                { "URL": "/Dashboards/DefaultDashboard#executive-exceptions", "Name": "تنبيهات الإدارة العليا", "IsModule": false, "Personas": ["Executive","SysAdmin"], "Icon": "bi-exclamation-triangle", "SortOrder": 5, "BadgeKey": "criticalExecutiveExceptions" },
                { "URL": "/Telecom/ExecutiveExceptions", "Name": "مركز الاستثناءات التشغيلية", "IsModule": false, "Personas": ["Executive","SysAdmin"], "Icon": "bi-shield-exclamation", "SortOrder": 6, "BadgeKey": "criticalExecutiveExceptions" },
                { "URL": "/Dashboards/DefaultDashboard#supervisor-interventions", "Name": "تدقيق تدخلات المشرف", "IsModule": false, "Personas": ["Executive","SysAdmin"], "Icon": "bi-person-check", "SortOrder": 7 },
                { "URL": "/Dashboards/DefaultDashboard#branch-geo-heatmap", "Name": "خريطة حرارة الفروع", "IsModule": false, "Personas": ["Executive","SysAdmin"], "Icon": "bi-geo-alt", "SortOrder": 8 }
            ]
        },
        {
            "URL": "#",
            "Name": "العمليات التشغيلية",
            "IsModule": true,
            "Icon": "bi-heart-pulse",
            "SortOrder": 2,
            "Children": [
                { "URL": "/Telecom/TelecomHub", "Name": "مركز عمليات الشبكة", "IsModule": false, "Personas": ["Executive","CallCenter","Retail","BackOffice","SysAdmin"], "Icon": "bi-broadcast", "SortOrder": 1, "IsQuickAction": true, "BadgeKey": "pendingOperations" },
                { "URL": "/Telecom/BackOfficeDashboard", "Name": "مركز العمليات الخلفية", "IsModule": false, "Personas": ["BackOffice","SysAdmin"], "Icon": "bi-speedometer2", "SortOrder": 2, "IsQuickAction": true, "BadgeKey": "overdueTickets" },
                { "URL": "/Telecom/TechnicalTicketList", "Name": "إدارة التذاكر الفنية", "IsModule": false, "Personas": ["CallCenter","BackOffice","SysAdmin"], "Icon": "bi-ticket-detailed", "SortOrder": 3, "BadgeKey": "openTechnicalTickets" },
                { "URL": "/Telecom/BulkImportMonitor", "Name": "مراقبة الاستيراد الضخم", "IsModule": false, "Personas": ["BackOffice","SysAdmin"], "Icon": "bi-cloud-upload", "SortOrder": 4, "BadgeKey": "bulkImportActive" },
                { "URL": "/Telecom/MsisdnInventory", "Name": "مستودع الأرقام والشرائح", "IsModule": false, "Personas": ["BackOffice","Showroom","CallCenter","SysAdmin"], "Icon": "bi-boxes", "SortOrder": 5 },
                { "URL": "/Telecom/DeviceInventory", "Name": "مخزون الأجهزة (IMEI)", "IsModule": false, "Personas": ["BackOffice","SysAdmin"], "Icon": "bi-phone", "SortOrder": 6 },
                { "URL": "/Telecom/BillingIntegration", "Name": "التكامل مع نظام الفوترة", "IsModule": false, "Personas": ["Executive","BackOffice","SysAdmin"], "Icon": "bi-receipt-cutoff", "SortOrder": 7 },
                { "URL": "/Telecom/InIntegration", "Name": "التكامل مع الشبكة الذكية (IN)", "IsModule": false, "Personas": ["Executive","BackOffice","SysAdmin"], "Icon": "bi-cpu", "SortOrder": 8 },
                { "URL": "/Telecom/HlrProvisioning", "Name": "تزويد HLR / HSS", "IsModule": false, "Personas": ["Executive","BackOffice","SysAdmin"], "Icon": "bi-hdd-network", "SortOrder": 9 }
            ]
        },
        {
            "URL": "#",
            "Name": "إدارة المشتركين",
            "IsModule": true,
            "Icon": "bi-people",
            "SortOrder": 3,
            "Children": [
                { "URL": "/Customers/CustomerList", "Name": "سجل المشتركين", "IsModule": false, "Personas": ["Executive","CallCenter","Retail","BackOffice","SysAdmin"], "Icon": "bi-person-lines-fill", "SortOrder": 1, "IsQuickAction": true },
                { "URL": "/Telecom/UnifiedSearch", "Name": "سجل الخطوط والاشتراكات", "IsModule": false, "Personas": ["Executive","CallCenter","Retail","BackOffice","SysAdmin"], "Icon": "bi-search", "SortOrder": 2, "IsQuickAction": true },
                { "URL": "/CustomerGroups/CustomerGroupList", "Name": "المجموعات والحسابات", "IsModule": false, "Personas": ["BackOffice","SysAdmin"], "Icon": "bi-diagram-3", "SortOrder": 3 },
                { "URL": "/CustomerCategories/CustomerCategoryList", "Name": "شرائح وتصنيفات العملاء", "IsModule": false, "Personas": ["BackOffice","SysAdmin"], "Icon": "bi-tags", "SortOrder": 4 },
                { "URL": "/CustomerContacts/CustomerContactList", "Name": "جهات اتصال المشتركين", "IsModule": false, "Personas": ["CallCenter","BackOffice","SysAdmin"], "Icon": "bi-telephone", "SortOrder": 5 }
            ]
        },
        {
            "URL": "#",
            "Name": "كتالوج المنتجات",
            "IsModule": true,
            "Icon": "bi-box-seam",
            "SortOrder": 4,
            "Children": [
                { "URL": "/Telecom/ProductCatalog", "Name": "العروض والخدمات التجارية", "IsModule": false, "Personas": ["Retail","BackOffice","SysAdmin"], "Icon": "bi-box-seam", "SortOrder": 1 },
                { "URL": "/Products/ProductList", "Name": "مواصفات المنتجات التقنية", "IsModule": false, "Personas": ["BackOffice","SysAdmin"], "Icon": "bi-hdd-stack", "SortOrder": 2 },
                { "URL": "/Telecom/VasCatalogList", "Name": "كتالوج الخدمات المضافة (VAS)", "IsModule": false, "Personas": ["BackOffice","SysAdmin"], "Icon": "bi-toggle-on", "SortOrder": 3 },
                { "URL": "/TelecomSubscriptionTypes/TelecomSubscriptionTypeList", "Name": "أنواع خطوط الاشتراك", "IsModule": false, "Personas": ["Retail","BackOffice","SysAdmin"], "Icon": "bi-sim", "SortOrder": 4 }
            ]
        },
        {
            "URL": "/Administration",
            "Name": "الحوكمة والنظام",
            "IsModule": true,
            "Personas": ["SysAdmin"],
            "Icon": "bi-building-gear",
            "SortOrder": 90,
            "Children": [
                { "URL": "/Administration/UserList", "Name": "إدارة المستخدمين", "IsModule": false, "Personas": ["SysAdmin"], "Icon": "bi-people-fill", "SortOrder": 1 },
                { "URL": "/Administration/BranchList", "Name": "إدارة الفروع", "IsModule": false, "Personas": ["SysAdmin"], "Icon": "bi-diagram-3-fill", "SortOrder": 2 },
                { "URL": "/Administration/RoleList", "Name": "الصلاحيات والأدوار", "IsModule": false, "Personas": ["SysAdmin"], "Icon": "bi-shield-lock-fill", "SortOrder": 3 },
                { "URL": "/Administration/GlobalSettings", "Name": "الإعدادات العامة للشبكة", "IsModule": false, "Personas": ["SysAdmin"], "Icon": "bi-sliders", "SortOrder": 4 },
                { "URL": "/Administration/AuditLogList", "Name": "سجل الرقابة", "IsModule": false, "Personas": ["SysAdmin"], "Icon": "bi-journal-text", "SortOrder": 5 },
                { "URL": "/Telecom/IntegrationMonitor", "Name": "سجل ربط الشبكة والتكاملات", "IsModule": false, "Personas": ["SysAdmin"], "Icon": "bi-hdd-network", "SortOrder": 6 },
                { "URL": "/Dashboards/DashboardWidgetList", "Name": "عناصر لوحة القيادة", "IsModule": false, "Personas": ["SysAdmin"], "Icon": "bi-layout-three-columns", "SortOrder": 7 },
                { "URL": "/Companies/MyCompany", "Name": "بيانات المشغّل", "IsModule": false, "Personas": ["SysAdmin"], "Icon": "bi-building", "SortOrder": 8 },
                { "URL": "/NumberSequences/NumberSequenceList", "Name": "تسلسل الأرقام التشغيلي", "IsModule": false, "Personas": ["SysAdmin"], "Icon": "bi-123", "SortOrder": 9 }
            ]
        },
        {
            "URL": "#",
            "Name": "حسابي",
            "IsModule": true,
            "Icon": "bi-person-circle",
            "SortOrder": 95,
            "Children": [
                { "URL": "/Profiles/MyProfile", "Name": "ملفي التشغيلي", "IsModule": false, "Personas": ["Executive","CallCenter","Retail","BackOffice","SysAdmin"], "Icon": "bi-person", "SortOrder": 1 }
            ]
        }
    ]
    """;

    public static List<MenuNavigationTreeNodeDto> GetCompleteMenuNavigationTreeNode()
    {
        var menus = JsonSerializer.Deserialize<List<JsonStructureItem>>(JsonStructure, JsonOptions);
        ApplyBilingualTelecomLabels(menus);

        List<MenuNavigationTreeNodeDto> nodes = new List<MenuNavigationTreeNodeDto>();

        var index = 1;
        void AddNodes(List<JsonStructureItem> menuItems, string? parentId = null)
        {
            foreach (var item in menuItems)
            {
                var nodeId = index.ToString();
                if (item.IsModule)
                {
                    var moduleUrl = string.IsNullOrWhiteSpace(item.URL) || item.URL == "#" ? null : item.URL;
                    nodes.Add(new MenuNavigationTreeNodeDto(
                        nodeId,
                        item.Name ?? "",
                        param_navURL: moduleUrl,
                        param_hasChild: true,
                        param_expanded: false,
                        param_nameEn: item.NameEn ?? item.Name,
                        param_personas: item.Personas,
                        param_icon: item.Icon,
                        param_sortOrder: item.SortOrder));
                }
                else
                {
                    nodes.Add(new MenuNavigationTreeNodeDto(
                        nodeId,
                        item.Name ?? "",
                        parentId,
                        item.URL,
                        param_nameEn: item.NameEn ?? item.Name,
                        param_personas: item.Personas,
                        param_icon: item.Icon,
                        param_sortOrder: item.SortOrder,
                        param_badgeKey: item.BadgeKey,
                        param_isQuickAction: item.IsQuickAction));
                }

                index++;

                if (item.Children is { Count: > 0 })
                {
                    AddNodes(item.Children, nodeId);
                }
            }
        }

        if (menus != null)
        {
            AddNodes(menus);
        }

        return nodes;
    }

    public static string GetFirstSegmentFromUrlPath(string? path)
    {
        var result = string.Empty;
        if (path != null && path.Contains("/"))
        {
            string[] parts = path.Split("/");
            if (parts.Length > 2)
            {
                result = parts[1];
            }
        }
        return result;
    }

    public static List<string> GetCompleteFirstMenuNavigationSegment()
    {
        var menus = JsonSerializer.Deserialize<List<JsonStructureItem>>(JsonStructure, JsonOptions);
        var result = new List<string>();

        if (menus != null)
        {
            foreach (var item in menus)
            {
                ProcessMenuItem(item, result);
            }
        }

        return result;
    }

    private static void ProcessMenuItem(JsonStructureItem item, List<string> result)
    {
        if (!string.IsNullOrEmpty(item.URL) && item.URL != "#")
        {
            var segment = GetFirstSegmentFromUrlPath(item.URL);
            if (!string.IsNullOrEmpty(segment) && !result.Contains(segment))
            {
                result.Add(segment);
            }
        }

        if (item.Children != null)
        {
            foreach (var child in item.Children)
            {
                ProcessMenuItem(child, result);
            }
        }
    }

    /// <summary>True when the user has only Syriatel telecom roles (no legacy CRM navigation roles).</summary>
    public static bool IsStrictTelecomWorkspaceUser(IReadOnlyList<string> roleNames) =>
        TelecomWorkspaceRules.IsStrictTelecomWorkspaceUser(roleNames);

    /// <summary>
    /// Strict telecom: persona menu first, then RBAC permission intersection.
    /// Non-strict: permission filter when keys exist, otherwise persona.
    /// </summary>
    public static List<MenuNavigationTreeNodeDto> ApplyTelecomWorkspaceMenuFilter(
        IReadOnlyList<string> roleNames,
        IReadOnlySet<string> userPermissions,
        List<MenuNavigationTreeNodeDto> nodes,
        TelecomMenuPersona? previewPersona = null)
    {
        if (!IsStrictTelecomWorkspaceUser(roleNames))
        {
            if (userPermissions.Count > 0)
            {
                return ApplyPermissionMenuFilter(userPermissions, nodes);
            }

            return ApplyPersonaMenuFilter(roleNames, nodes, previewPersona);
        }

        var personaFiltered = ApplyPersonaMenuFilter(roleNames, nodes, previewPersona);
        if (userPermissions.Count == 0)
        {
            return personaFiltered;
        }

        return ApplyPermissionMenuFilter(userPermissions, personaFiltered);
    }

    /// <summary>
    /// Server-side menu filter for strict telecom workspace users.
    /// Only leaves tagged with the resolved <see cref="TelecomMenuPersona"/> are returned (with ancestor modules).
    /// </summary>
    public static List<MenuNavigationTreeNodeDto> ApplyPersonaMenuFilter(
        IReadOnlyList<string> roleNames,
        List<MenuNavigationTreeNodeDto> nodes,
        TelecomMenuPersona? previewPersona = null)
    {
        if (nodes.Count == 0)
        {
            return nodes;
        }

        var persona = previewPersona ?? TelecomPersonaResolver.ResolvePrimary(roleNames);
        if (persona == null)
        {
            return nodes;
        }

        if (!previewPersona.HasValue && !IsStrictTelecomWorkspaceUser(roleNames))
        {
            return nodes;
        }

        var personaName = persona.Value.ToString();
        var keep = new HashSet<string>(StringComparer.Ordinal);

        foreach (var leaf in nodes.Where(n => !n.HasChild && !string.IsNullOrWhiteSpace(n.NavURL)))
        {
            if (!LeafAllowedForPersona(leaf, personaName))
            {
                continue;
            }

            string? cursor = leaf.Id;
            while (!string.IsNullOrEmpty(cursor))
            {
                keep.Add(cursor);
                cursor = nodes.FirstOrDefault(n => n.Id == cursor)?.Pid;
            }
        }

        return nodes.Where(n => keep.Contains(n.Id)).ToList();
    }

    private static bool LeafAllowedForPersona(MenuNavigationTreeNodeDto leaf, string personaKey) =>
        leaf.Personas != null
        && leaf.Personas.Count > 0
        && leaf.Personas.Contains(personaKey, StringComparer.OrdinalIgnoreCase);

    /// <summary>Filters strict telecom menu leaves by effective RBAC permission keys.</summary>
    public static List<MenuNavigationTreeNodeDto> ApplyPermissionMenuFilter(
        IReadOnlySet<string> userPermissions,
        List<MenuNavigationTreeNodeDto> nodes)
    {
        if (nodes.Count == 0 || userPermissions.Count == 0)
        {
            return nodes;
        }

        var keep = new HashSet<string>(StringComparer.Ordinal);

        foreach (var leaf in nodes.Where(n => !n.HasChild && !string.IsNullOrWhiteSpace(n.NavURL)))
        {
            if (!NavigationPermissionRules.IsNavUrlAllowed(leaf.NavURL, userPermissions))
            {
                continue;
            }

            string? cursor = leaf.Id;
            while (!string.IsNullOrEmpty(cursor))
            {
                keep.Add(cursor);
                cursor = nodes.FirstOrDefault(n => n.Id == cursor)?.Pid;
            }
        }

        return nodes.Where(n => keep.Contains(n.Id)).ToList();
    }

    [Obsolete("Use ApplyPersonaMenuFilter. Kept for backward compatibility during migration.")]
    public static List<MenuNavigationTreeNodeDto> ApplyStrictTelecomMenuFilter(
        IReadOnlyList<string> roleNames,
        List<MenuNavigationTreeNodeDto> nodes) =>
        ApplyPersonaMenuFilter(roleNames, nodes);
}
