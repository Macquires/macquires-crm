import re
from pathlib import Path

root = Path(r"c:\Users\HP\source\repos\macquires-crm\Presentation\ASPNET\FrontEnd\Pages")

SHELL_DEFAULT = (
    '@{ ViewData["PortalPageVcloak"] = true; }\n'
    '@await Html.PartialAsync("~/FrontEnd/Pages/Shared/AdminTemplate/_PortalPageShell.cshtml")\n\n'
)
SHELL_INNER_FALSE = (
    '@{ ViewData["PortalPageVcloak"] = true; ViewData["PortalPageInner"] = false; }\n'
    '@await Html.PartialAsync("~/FrontEnd/Pages/Shared/AdminTemplate/_PortalPageShell.cshtml")\n\n'
)
SHELL_VUE = (
    '@{ ViewData["PortalPageVcloak"] = true; ViewData["PortalPageInner"] = false; '
    'ViewData["PortalPageAttrs"] = ":dir=\\"contentDir\\" :lang=\\"contentLang\\""; }\n'
    '@await Html.PartialAsync("~/FrontEnd/Pages/Shared/AdminTemplate/_PortalPageShell.cshtml")\n\n'
)
SHELL_END = (
    '\n@await Html.PartialAsync("~/FrontEnd/Pages/Shared/AdminTemplate/_PortalPageShellEnd.cshtml")'
)


def add_end(content: str) -> str:
    if "_PortalPageShellEnd" in content:
        return content
    return re.sub(r"\n</div>\s*\n(@section\b)", SHELL_END + r"\n\n\1", content, count=1, flags=re.I)


def process(path: Path, opener: str, replacement: str) -> bool:
    if not path.exists():
        return False
    content = path.read_text(encoding="utf-8")
    if "_PortalPageShell.cshtml" in content:
        return False
    if opener not in content:
        return False
    content = content.replace(opener, replacement, 1)
    content = add_end(content)
    path.write_text(content, encoding="utf-8", newline="\r\n")
    return True


def main() -> None:
    updated: list[str] = []

    simple = [
        "Administration/UserList.cshtml",
        "Administration/RoleList.cshtml",
        "Administration/BranchList.cshtml",
        "Administration/AuditLogList.cshtml",
        "CustomerContacts/CustomerContactList.cshtml",
        "CustomerGroups/CustomerGroupList.cshtml",
        "CustomerCategories/CustomerCategoryList.cshtml",
        "Products/ProductList.cshtml",
        "NumberSequences/NumberSequenceList.cshtml",
        "Companies/MyCompany.cshtml",
        "Profiles/MyProfile.cshtml",
        "Dashboards/DashboardWidgetList.cshtml",
        "TelecomSubscriptionTypes/TelecomSubscriptionTypeList.cshtml",
        "Telecom/VasCatalogList.cshtml",
    ]
    for rel in simple:
        if process(root / rel, '<div id="app" v-cloak>\n', SHELL_DEFAULT):
            updated.append(rel)

    premium = [
        "Telecom/MsisdnInventory.cshtml",
        "Telecom/DeviceInventory.cshtml",
        "Telecom/BillingIntegration.cshtml",
        "Telecom/HlrProvisioning.cshtml",
        "Telecom/InIntegration.cshtml",
        "Telecom/BackOfficeHistoricalLedger.cshtml",
        "Telecom/BackOfficeAuditList.cshtml",
        "Telecom/UnifiedSearch.cshtml",
        "Telecom/Customer360Profile.cshtml",
    ]
    opener = '<div id="app" v-cloak class="telecom-premium-page">\n'
    for rel in premium:
        if process(root / rel, opener, SHELL_INNER_FALSE):
            updated.append(rel)

    container = {
        "Telecom/TechnicalTicketList.cshtml": '<div id="app" v-cloak class="telecom-premium-page container-fluid py-4">\n',
        "Telecom/IntegrationMonitor.cshtml": '<div id="app" v-cloak class="telecom-premium-page container-fluid">\n',
        "Administration/GlobalSettings.cshtml": '<div id="app" v-cloak class="telecom-premium-page container-fluid">\n',
        "Administration/Index.cshtml": '<div id="app" v-cloak class="telecom-premium-page container py-4">\n',
    }
    for rel, op in container.items():
        if process(root / rel, op, SHELL_DEFAULT):
            updated.append(rel)

    if process(root / "Telecom/BulkImportMonitor.cshtml", '<div id="app" v-cloak>\n', SHELL_INNER_FALSE):
        updated.append("Telecom/BulkImportMonitor.cshtml")

    vue_openers = [
        '<div id="app" v-cloak class="telecom-premium-page" :dir="contentDir" :lang="contentLang">\n',
        '<div id="app" v-cloak class="telecom-premium-page syr-portal-page" :dir="contentDir" :lang="contentLang">\n',
    ]
    for rel in ["Telecom/ProductCatalog.cshtml", "Telecom/TelecomHub.cshtml"]:
        path = root / rel
        for op in vue_openers:
            if process(path, op, SHELL_VUE):
                updated.append(rel)
                break

    print(f"Updated {len(updated)} files:")
    for item in updated:
        print(f" - {item}")


if __name__ == "__main__":
    main()
