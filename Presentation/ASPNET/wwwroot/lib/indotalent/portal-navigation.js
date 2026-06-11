/**
 * Syr-Tel custom sidebar — server-filtered menu, premium accordions.
 */
const PortalNavigation = (function () {
    const PERSONA_LABELS_AR = {
        Executive: 'الإدارة العليا',
        CallCenter: 'مركز الاتصال',
        Retail: 'نقطة البيع',
        BackOffice: 'العمليات',
        SysAdmin: 'الإدارة التقنية',
    };

    const PERSONA_LABELS_EN = {
        Executive: 'Executive',
        CallCenter: 'Call center',
        Retail: 'Retail POS',
        BackOffice: 'Back office',
        SysAdmin: 'Technical administration',
    };

    function personaLabels() {
        return getLang() === 'en' ? PERSONA_LABELS_EN : PERSONA_LABELS_AR;
    }

    const PREVIEW_KEY = 'syrPreviewPersona';
    const EXPANDED_KEY = 'syrNavExpandedModules';
    const SIDEBAR_SCROLL_KEY = 'syrSidebarScrollPosition';
    const LEGACY_SIDEBAR_SCROLL_KEY = 'sidebarScrollPosition';

    function getSidebarScrollEl() {
        return document.getElementById('sidebar');
    }

    function saveSidebarScroll() {
        const el = getSidebarScrollEl();
        if (!el) {
            return;
        }
        try {
            sessionStorage.setItem(SIDEBAR_SCROLL_KEY, String(el.scrollTop));
        } catch {
            /* ignore */
        }
    }

    function readSavedSidebarScroll() {
        try {
            let raw = sessionStorage.getItem(SIDEBAR_SCROLL_KEY);
            if (raw === null) {
                raw = sessionStorage.getItem(LEGACY_SIDEBAR_SCROLL_KEY);
            }
            if (raw === null) {
                return null;
            }
            const top = parseInt(raw, 10);
            return Number.isFinite(top) ? top : null;
        } catch {
            return null;
        }
    }

    function restoreSidebarScroll() {
        const el = getSidebarScrollEl();
        const top = readSavedSidebarScroll();
        if (!el || top === null) {
            return;
        }
        el.scrollTop = top;
    }

    function scrollActiveNavIntoViewIfNeeded() {
        const sidebar = getSidebarScrollEl();
        const active = document.querySelector('#syrPortalNav .syr-nav-link.active');
        if (!sidebar || !active) {
            return;
        }
        const sidebarRect = sidebar.getBoundingClientRect();
        const linkRect = active.getBoundingClientRect();
        if (linkRect.top >= sidebarRect.top && linkRect.bottom <= sidebarRect.bottom) {
            return;
        }
        active.scrollIntoView({ block: 'nearest', behavior: 'instant' });
        saveSidebarScroll();
    }

    function afterNavRenderScrollSync() {
        function syncOnce() {
            restoreSidebarScroll();
            scrollActiveNavIntoViewIfNeeded();
            saveSidebarScroll();
        }
        syncOnce();
        requestAnimationFrame(syncOnce);
        // Accordion expand uses ~280ms CSS transition; re-sync when layout height settles.
        window.setTimeout(syncOnce, 320);
    }

    function bindSidebarScrollPersistence() {
        const el = getSidebarScrollEl();
        if (!el || el.dataset.syrScrollBound === '1') {
            return;
        }
        el.dataset.syrScrollBound = '1';
        el.addEventListener('scroll', saveSidebarScroll, { passive: true });
        window.addEventListener('beforeunload', saveSidebarScroll);
        const navHost = document.getElementById('syrPortalNav');
        const quickHost = document.getElementById('syrQuickActions');
        const onNavClick = function (e) {
            const link = e.target.closest('a.syr-nav-link, a.syr-quick-action');
            if (!link || !link.getAttribute('href') || link.getAttribute('href') === '#') {
                return;
            }
            saveSidebarScroll();
        };
        if (navHost) {
            navHost.addEventListener('click', onNavClick);
        }
        if (quickHost) {
            quickHost.addEventListener('click', onNavClick);
        }
    }

    function normalizePath(url) {
        if (!url || url === '#') return '';
        const path = String(url).split('?')[0].split('#')[0].trim().toLowerCase();
        if (!path) return '';
        const withSlash = path.startsWith('/') ? path : '/' + path;
        return withSlash.replace(/\/+$/, '') || '/';
    }

    function getLang() {
        return (document.documentElement.lang || '').toLowerCase().startsWith('en') ? 'en' : 'ar';
    }

    function localizeRow(row) {
        const useEn = getLang() === 'en';
        const name = useEn ? (row.nameEn || row.name) : row.name;
        return Object.assign({}, row, { name: name || row.name });
    }

    function localizeRows(rows) {
        return (rows || []).map(localizeRow);
    }

    function getSavedExpandedSet() {
        try {
            const raw = sessionStorage.getItem(EXPANDED_KEY);
            return new Set(raw ? JSON.parse(raw) : []);
        } catch {
            return new Set();
        }
    }

    function saveExpandedSet(set) {
        try {
            sessionStorage.setItem(EXPANDED_KEY, JSON.stringify([...set]));
        } catch {
            /* ignore */
        }
    }

    function updateSelection(rows) {
        const parentMap = {};
        rows.forEach((item) => {
            parentMap[item.id] = item;
        });

        const currentPath = normalizePath(window.location.pathname);
        const savedExpanded = getSavedExpandedSet();

        rows.forEach((item) => {
            item.isSelected = false;
            if (!item.hasChild) {
                return;
            }
            item.expanded = savedExpanded.has(item.id);
        });

        rows.forEach((item) => {
            const url = item.navURL || item.navUrl;
            if (!url || url === '#') {
                return;
            }
            if (normalizePath(url) === currentPath) {
                item.isSelected = true;
                if (item.hasChild) {
                    item.expanded = true;
                    savedExpanded.add(item.id);
                }
                let parentId = item.pid;
                while (parentId) {
                    const parent = parentMap[parentId];
                    if (parent) {
                        parent.expanded = true;
                        savedExpanded.add(parent.id);
                        parentId = parent.pid;
                    } else {
                        break;
                    }
                }
            }
        });

        saveExpandedSet(savedExpanded);
        return rows;
    }

    function buildSections(rows) {
        const modules = rows
            .filter((r) => r.hasChild)
            .sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0));
        const leaves = rows.filter((r) => !r.hasChild && (r.navURL || r.navUrl));
        const sections = [];

        modules.forEach((mod) => {
            const children = leaves
                .filter((l) => l.pid === mod.id)
                .sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0));
            if (children.length) {
                sections.push({ module: localizeRow(mod), children: localizeRows(children) });
            }
        });

        return sections;
    }

    function getQuickActions(rows) {
        return localizeRows(
            rows.filter((r) => !r.hasChild && r.isQuickAction && (r.navURL || r.navUrl))
        ).slice(0, 2);
    }

    function getEffectivePersona() {
        const preview = sessionStorage.getItem(PREVIEW_KEY);
        const roles = StorageManager.getUserRoles() || [];
        const canPreview = roles.some((r) => r === 'TelecomAdmin' || r === 'TelecomManagement');
        if (preview && canPreview) {
            return preview;
        }
        return StorageManager.getPrimaryMenuPersona() || '';
    }

    function getPermissionPersonaLabel() {
        const labels = personaLabels();
        const perms = StorageManager.getPermissions?.() || [];
        if (!perms.length) {
            return '';
        }
        if (StorageManager.hasAnyPermission(perms, ['admin.users.manage', 'admin.settings.manage', 'admin.roles.manage'])) {
            return labels.SysAdmin;
        }
        if (StorageManager.hasAnyPermission(perms, ['bulk.import.upload', 'telecom.asset.manage'])) {
            return labels.BackOffice;
        }
        if (StorageManager.hasAnyPermission(perms, ['telecom.reports.mis'])) {
            return labels.Executive;
        }
        if (
            StorageManager.hasAnyPermission(perms, [
                'telecom.line.activate',
                'telecom.line.simswap',
                'telecom.line.simswap_request',
            ])
        ) {
            return labels.Retail;
        }
        if (StorageManager.hasAnyPermission(perms, ['customer.view'])) {
            return labels.CallCenter;
        }
        return '';
    }

    function redirectToPermissionLandingIfNeeded() {
        // Deprecated aggressive redirect removed: it sent admins from DefaultDashboard to UserList
        // even when the dashboard was in their menu. enforcePermissionLanding + persona redirect suffice.
        return false;
    }

    function renderNavLink(leaf, badges, currentPath) {
        const url = leaf.navURL || leaf.navUrl || '#';
        const path = normalizePath(url);
        const active = path && currentPath === path;
        const icon = leaf.icon || 'bi-circle';
        const badgeKey = leaf.badgeKey || leaf.BadgeKey;
        const raw = badgeKey && badges && Object.prototype.hasOwnProperty.call(badges, badgeKey)
            ? badges[badgeKey]
            : null;
        const count = raw === null || raw === undefined ? 0 : Math.floor(Number(raw));
        const badgeHtml =
            badgeKey && Number.isFinite(count) && count > 0
                ? `<span class="syr-nav-badge" title="${badgeKey}" aria-label="${count}">${count > 99 ? '99+' : count}</span>`
                : '';

        return `<a class="syr-nav-link${active ? ' active' : ''}" href="${url}">
            <i class="bi ${icon} syr-nav-icon"></i>
            <span class="flex-grow-1 text-truncate">${leaf.name || ''}</span>
            ${badgeHtml}
        </a>`;
    }

    function renderAccordionSection(sec, badges, currentPath) {
        const mod = sec.module;
        const children = sec.children;
        const modIcon = mod.icon || 'bi-folder2';
        const expanded = mod.expanded === true;
        const chevron = expanded ? 'bi-chevron-up' : 'bi-chevron-down';

        if (children.length === 1) {
            const leaf = Object.assign({}, children[0], { icon: children[0].icon || modIcon });
            return `<div class="syr-nav-section syr-nav-section--flat">${renderNavLink(leaf, badges, currentPath)}</div>`;
        }

        let bodyHtml = '';
        children.forEach((leaf) => {
            bodyHtml += renderNavLink(leaf, badges, currentPath);
        });

        const hubUrl = mod.navURL || mod.navUrl;
        const hasHub = hubUrl && hubUrl !== '#';
        const hubActive = hasHub && currentPath === normalizePath(hubUrl);

        const headerInner = hasHub
            ? `<a class="syr-nav-accordion-hub${hubActive ? ' active' : ''}" href="${hubUrl}">
                <i class="bi ${modIcon} syr-nav-module-icon syr-duotone-lite"></i>
                <span class="flex-grow-1 text-truncate">${mod.name || ''}</span>
            </a>
            <button type="button" class="syr-nav-accordion-toggle" data-module-id="${mod.id}" aria-expanded="${expanded}" aria-label="Toggle section">
                <i class="bi ${chevron} syr-nav-chevron"></i>
            </button>`
            : `<button type="button" class="syr-nav-accordion-header syr-nav-accordion-header--solo" data-module-id="${mod.id}" aria-expanded="${expanded}">
                <i class="bi ${modIcon} syr-nav-module-icon syr-duotone-lite"></i>
                <span class="flex-grow-1 text-truncate text-start">${mod.name || ''}</span>
                <i class="bi ${chevron} syr-nav-chevron"></i>
            </button>`;

        return `<div class="syr-nav-section syr-nav-accordion${hasHub ? ' syr-nav-accordion--hub' : ''}">
            <div class="syr-nav-accordion-header-row${hubActive ? ' is-hub-active' : ''}" aria-expanded="${expanded}">
                ${headerInner}
            </div>
            <div class="syr-nav-accordion-body${expanded ? ' is-open' : ''}">
                <div class="syr-nav-accordion-inner">${bodyHtml}</div>
            </div>
        </div>`;
    }

    function bindAccordionHandlers(container, containerId, options) {
        if (container.dataset.accordionBound) {
            return;
        }
        container.dataset.accordionBound = '1';
        container.addEventListener('click', function (e) {
            if (e.target.closest('a.syr-nav-accordion-hub')) {
                return;
            }
            const btn = e.target.closest('.syr-nav-accordion-toggle, .syr-nav-accordion-header--solo');
            if (!btn) {
                return;
            }
            e.preventDefault();
            const moduleId = btn.getAttribute('data-module-id');
            if (!moduleId) {
                return;
            }
            const set = getSavedExpandedSet();
            if (set.has(moduleId)) {
                set.delete(moduleId);
            } else {
                set.add(moduleId);
            }
            saveExpandedSet(set);
            render(containerId, options);
        });
    }

    function render(containerId, options) {
        const container = document.getElementById(containerId);
        if (!container) {
            return;
        }

        const opts = options || {};
        let rows = StorageManager.getMenuNavigation() || [];
        rows = updateSelection(rows);

        const badges = opts.badges || StorageManager.getMenuBadges() || {};
        const currentPath = normalizePath(window.location.pathname);
        const persona = getEffectivePersona();
        const previewActive =
            sessionStorage.getItem(PREVIEW_KEY) &&
            (StorageManager.getUserRoles() || []).some(
                (r) => r === 'TelecomAdmin' || r === 'TelecomManagement'
            );
        const labels = personaLabels();
        const personaLabel = previewActive
            ? labels[persona] || persona || ''
            : getPermissionPersonaLabel() || labels[persona] || persona || '';

        const chipEl = document.getElementById('syrPersonaChip');
        if (chipEl) {
            chipEl.textContent = personaLabel || (getLang() === 'en' ? 'Operator' : 'مشغّل');
        }

        document.body.setAttribute('data-persona', persona || '');
        document.body.classList.remove('syriatel-operator-console');
        if (typeof setFormCardHeight === 'function') {
            setFormCardHeight();
        }

        const quickHost = document.getElementById('syrQuickActions');
        if (quickHost) {
            const quick = getQuickActions(rows);
            quickHost.innerHTML = quick
                .map((q) => {
                    const icon = q.icon || 'bi-lightning';
                    return `<a class="syr-quick-action" href="${q.navURL || q.navUrl}" title="${q.name || ''}">
                        <i class="bi ${icon}"></i><span class="text-truncate">${q.name}</span>
                    </a>`;
                })
                .join('');
            quickHost.style.display = quick.length ? '' : 'none';
        }

        const sections = buildSections(rows);
        let html = '';
        sections.forEach((sec) => {
            html += renderAccordionSection(sec, badges, currentPath);
        });

        container.innerHTML =
            html ||
            `<p class="small text-white-50 px-3 py-2">${getLang() === 'en' ? 'No menu items' : 'لا عناصر في القائمة'}</p>`;

        bindAccordionHandlers(container, containerId, options);
        bindSidebarScrollPersistence();
        afterNavRenderScrollSync();
    }

    async function reloadMenuForPreview(previewPersona) {
        const roles = StorageManager.getUserRoles() || [];
        if (!roles.length) {
            return;
        }
        try {
            const res = await AxiosManager.post('/Security/GetPersonaMenuNavigation', {
                roles: roles,
                previewPersona: previewPersona || null,
            });
            const content = res?.data?.content ?? res?.data?.Content;
            const nodes = content?.data ?? content?.Data;
            if (Array.isArray(nodes)) {
                StorageManager.saveMenuNavigation(nodes);
            }
            if (!previewPersona) {
                const primary = content?.primaryMenuPersona ?? content?.PrimaryMenuPersona;
                if (primary) {
                    StorageManager.savePrimaryMenuPersona(primary);
                }
            }
        } catch (e) {
            console.warn('Persona menu preview failed', e);
        }
    }

    async function refreshBadges() {
        try {
            const res = await AxiosManager.get('/Security/GetMenuBadges', {});
            const dto = res?.data?.content?.data ?? res?.data?.content?.Data;
            if (dto) {
                const map = {
                    pendingOperations: Number(dto.pendingOperations ?? dto.PendingOperations ?? 0) || 0,
                    overdueTickets: Number(dto.overdueTickets ?? dto.OverdueTickets ?? 0) || 0,
                    openTechnicalTickets: Number(dto.openTechnicalTickets ?? dto.OpenTechnicalTickets ?? 0) || 0,
                    bulkImportActive: Number(dto.bulkImportActive ?? dto.BulkImportActive ?? 0) || 0,
                };
                StorageManager.saveMenuBadges(map);
            } else {
                StorageManager.saveMenuBadges({});
            }
        } catch (e) {
            console.warn('Menu badges fetch failed', e);
            StorageManager.saveMenuBadges({});
        }
        if (document.getElementById('syrPortalNav')) {
            render('syrPortalNav');
        }
    }

    let badgePollTimer = null;
    function startBadgePolling() {
        if (badgePollTimer) {
            return;
        }
        badgePollTimer = window.setInterval(function () {
            refreshBadges();
        }, 60000);
    }

    function redirectToPersonaLandingIfNeeded() {
        if (
            typeof StorageManager.isStrictSyriatelTelecomWorkspaceUser === 'function' &&
            StorageManager.isStrictSyriatelTelecomWorkspaceUser()
        ) {
            return false;
        }
        if (typeof StorageManager.isPathAllowedForCurrentMenu !== 'function') {
            return false;
        }
        if (StorageManager.isPathAllowedForCurrentMenu()) {
            return false;
        }
        const landing =
            typeof StorageManager.getLandingPath === 'function'
                ? StorageManager.getLandingPath()
                : StorageManager.getLandingPathForPersona(getEffectivePersona());
        const current = normalizePath(window.location.pathname);
        if (landing && normalizePath(landing) !== current) {
            window.location.replace(landing);
            return true;
        }
        return false;
    }

    async function syncOperatorSession(force) {
        if (!force && sessionStorage.getItem('syrSessionSynced') === '1') {
            return;
        }
        try {
            const res = await AxiosManager.get('/Security/GetOperatorSession');
            const session = res?.data?.content ?? res?.data?.Content;
            if (session) {
                const preview = sessionStorage.getItem(PREVIEW_KEY);
                const sessionRoles = session.roles ?? session.Roles ?? [];
                StorageManager.applyOperatorSession(session);
                const roles = StorageManager.getUserRoles() || sessionRoles;
                const canPreview = roles.some(
                    (r) => r === 'TelecomAdmin' || r === 'TelecomManagement'
                );
                if (preview && canPreview) {
                    await reloadMenuForPreview(preview);
                }
                sessionStorage.setItem('syrSessionSynced', '1');
            }
        } catch (e) {
            console.warn('GetOperatorSession failed', e);
        }
    }

    function enforcePermissionLanding() {
        if (typeof StorageManager.getLandingPath !== 'function') {
            return false;
        }
        const landing = StorageManager.getLandingPath();
        const current = normalizePath(window.location.pathname);
        if (
            landing &&
            normalizePath(landing) !== current &&
            current === '/dashboards/defaultdashboard'
        ) {
            if (typeof StorageManager.isPathAllowedForCurrentMenu === 'function'
                && StorageManager.isPathAllowedForCurrentMenu()) {
                return false;
            }
            const perms = StorageManager.getPermissions?.() || [];
            if (typeof StorageManager.hasAnyPermission === 'function'
                && StorageManager.hasAnyPermission(perms, [
                    'customer.view',
                    'telecom.reports.mis',
                    'admin.users.manage',
                    'admin.settings.manage',
                    'admin.roles.manage',
                ])) {
                return false;
            }
            window.location.replace(landing);
            return true;
        }
        return false;
    }

    async function init() {
        bindSidebarScrollPersistence();
        await syncOperatorSession(false);
        const savedPreview = sessionStorage.getItem(PREVIEW_KEY);
        const roles = StorageManager.getUserRoles() || [];
        const canPreview = roles.some(
            (r) => r === 'TelecomAdmin' || r === 'TelecomManagement'
        );
        if (savedPreview && canPreview) {
            await reloadMenuForPreview(savedPreview);
        }
        StorageManager.saveMenuBadges({});
        await refreshBadges();
        startBadgePolling();
        if (enforcePermissionLanding()) {
            return;
        }
        if (redirectToPermissionLandingIfNeeded()) {
            return;
        }
        if (redirectToPersonaLandingIfNeeded()) {
            return;
        }
        render('syrPortalNav');
        document.documentElement.addEventListener('syriatel-locale-changed', function () {
            render('syrPortalNav');
        });
    }

    return {
        init,
        render,
        refreshBadges,
        startBadgePolling,
        reloadMenuForPreview,
        syncOperatorSession,
        enforcePermissionLanding,
        getEffectivePersona,
        PERSONA_LABELS: personaLabels,
        personaLabels,
        PREVIEW_KEY,
    };
})();
