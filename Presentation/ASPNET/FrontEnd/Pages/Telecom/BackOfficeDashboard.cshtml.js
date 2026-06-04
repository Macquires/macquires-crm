/* Back Office dashboard — plain JS, premium UX */

(function () {
    const ISSUE = { 0: 'شبكة', 1: 'فوترة', 2: 'حظر شريحة', 3: 'تفعيل' };
    const CATEGORY = {
        0: 'Complaint',
        1: 'SimSwap',
        2: 'PackageMigration',
        3: 'OwnershipTransfer',
        4: 'LineActivation',
        5: 'VasActivation',
    };
    const PRIORITY = { 0: 'منخفض', 1: 'متوسط', 2: 'عالي', 3: 'حرج' };
    const STATUS = { 0: 'مفتوحة', 1: 'قيد المعالجة', 2: 'تم الحل', 3: 'مصعّدة' };
    const PREVIEW_MAX = 50;

    const BACK_OFFICE_ROLES = ['TelecomBackOffice', 'TelecomAdmin', 'TelecomManagement'];
    const BACK_OFFICE_PERMISSIONS = [
        'bulk.import.upload',
        'bulk.import.monitor',
        'telecom.asset.manage',
        'telecom.line.migrate',
        'telecom.line.activate',
        'telecom.reports.mis',
        'telecom.ticket.forcesync',
        'admin.users.manage',
        'admin.roles.manage',
        'admin.settings.manage',
        'admin.audit.view',
        'customer.view',
    ];

    const EMPTY_GRID_HTML = `
        <div class="text-center py-4 text-muted">
            <i class="bi bi-inbox display-6 d-block mb-2"></i>
            <span>لا توجد تذاكر في طابورك</span>
        </div>`;

    let tickets = [];
    let ticketPreview = [];
    let offerings = [];
    let ticketsGrid = null;
    let catalogGrid = null;
    let selectedTicket = null;
    let ticketDrawer = null;
    let isLoading = false;
    let isTelecomActionInFlight = false;
    let liveStatusCollapse = null;
    let tier3Collapse = null;

    const pick = (o, ...keys) => {
        if (!o) return undefined;
        for (const k of keys) {
            if (o[k] !== undefined && o[k] !== null) return o[k];
        }
        return undefined;
    };

    const t = (key, fallback) => {
        try {
            const loc = window.TelecomI18n?.t?.(key);
            return loc || fallback;
        } catch {
            return fallback;
        }
    };

    const showError = (msg) => {
        const el = document.getElementById('bo-boot-error');
        if (!el) return;
        el.textContent = msg || 'خطأ في التحميل';
        el.classList.remove('d-none');
    };

    const setLoading = (on) => {
        isLoading = on;
        const overlay = document.getElementById('boLoadingOverlay');
        const btn = document.getElementById('boReloadBtn');
        if (overlay) overlay.classList.toggle('d-none', !on);
        if (btn) btn.disabled = on;
    };

    const setLastRefresh = () => {
        const el = document.getElementById('boLastRefresh');
        if (!el) return;
        try {
            el.textContent = new Date().toLocaleString('ar-SY', { dateStyle: 'short', timeStyle: 'medium' });
        } catch {
            el.textContent = new Date().toISOString();
        }
    };

    const parseTicketList = (res) => {
        const fromHelper = StorageManager.apiList(res);
        if (fromHelper.length > 0) return fromHelper;
        const content = res?.data?.content ?? res?.data?.Content;
        const list = content?.data ?? content?.Data;
        return Array.isArray(list) ? list : [];
    };

    const escapeHtml = (s) =>
        String(s ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');

    const resolveCategoryKey = (raw) => {
        const cat = pick(raw, 'ticketCategory', 'TicketCategory');
        if (cat === undefined || cat === null || cat === '') return 'Complaint';
        const n = Number(cat);
        if (!Number.isNaN(n) && CATEGORY[n]) return CATEGORY[n];
        return String(cat);
    };

    const categoryLabelHtml = (categoryKey) =>
        window.TelecomUiBadges?.ticketCategory(categoryKey) ||
        escapeHtml(String(categoryKey || '—'));

    const normalizeTicket = (raw) => {
        const ticketCategory = resolveCategoryKey(raw);
        return {
            id: pick(raw, 'id', 'Id'),
            ticketNumber: pick(raw, 'ticketNumber', 'TicketNumber'),
            msisdn: pick(raw, 'msisdn', 'Msisdn'),
            issueType: pick(raw, 'issueType', 'IssueType'),
            ticketCategory,
            priority: pick(raw, 'priority', 'Priority'),
            status: pick(raw, 'status', 'Status'),
            notes: pick(raw, 'notes', 'Notes'),
            payloadJson: pick(raw, 'payloadJson', 'PayloadJson'),
            createdByChannel: pick(raw, 'createdByChannel', 'CreatedByChannel') || 'CallCenter_Agent',
            customerDisplayName: pick(raw, 'customerDisplayName', 'CustomerDisplayName'),
            resolutionNotes: pick(raw, 'resolutionNotes', 'ResolutionNotes'),
            issueLabel: ISSUE[Number(pick(raw, 'issueType', 'IssueType'))] ?? '—',
            categoryLabelHtml: categoryLabelHtml(ticketCategory),
            priorityLabel: PRIORITY[Number(pick(raw, 'priority', 'Priority'))] ?? '—',
            statusLabel: STATUS[Number(pick(raw, 'status', 'Status'))] ?? '—',
        };
    };

    const categoryNeedsHlr = (cat) => {
        const c = String(cat || '').toLowerCase();
        return c === 'simswap' || c === 'lineactivation' || cat === 1 || cat === 4;
    };

    const categoryNeedsCbs = (cat) => {
        const c = String(cat || '').toLowerCase();
        return c === 'packagemigration' || c === 'complaint' || c === 'vasactivation' || cat === 0 || cat === 2 || cat === 5;
    };

    function updateDrawerToolbar(category) {
        const hlrBtn = document.getElementById('boHlrBtn');
        const cbsBtn = document.getElementById('btnCbsForceSync');
        const pingBtn = document.getElementById('btnNetworkPing');
        const escBtn = document.getElementById('btnCoreEscalate');
        if (hlrBtn) hlrBtn.classList.toggle('d-none', !categoryNeedsHlr(category));
        if (cbsBtn) cbsBtn.classList.toggle('d-none', !categoryNeedsCbs(category));
        if (pingBtn) pingBtn.classList.remove('d-none');
        if (escBtn) escBtn.classList.remove('d-none');
    }

    const normalizeOffering = (raw) => ({
        id: pick(raw, 'id', 'Id'),
        name: pick(raw, 'name', 'Name'),
        code: pick(raw, 'code', 'Code'),
        defaultPrice: pick(raw, 'defaultPrice', 'DefaultPrice'),
        isActive: !!pick(raw, 'isActive', 'IsActive'),
    });

    const rowClass = (data) => {
        const p = Number(pick(data, 'priority', 'Priority'));
        if (p === 3) return 'bo-ticket-critical table-danger';
        if (p === 2) return 'table-warning';
        return '';
    };

    const priorityBadge = (row) =>
        window.TelecomUiBadges?.ticketPriority(row.priority, row.priorityLabel) ||
        escapeHtml(row.priorityLabel);

    const statusBadge = (row) =>
        window.TelecomUiBadges?.ticketStatus(row.status, row.statusLabel) || escapeHtml(row.statusLabel);

    const channelBadge = (row) =>
        window.TelecomUiBadges?.ticketChannel(row.createdByChannel) ||
        escapeHtml(row.createdByChannel);

    function rolesFromAccessToken() {
        try {
            const token = StorageManager.getAccessToken?.();
            if (!token) return [];
            const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
            const role =
                payload.role ??
                payload.roles ??
                payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
            if (!role) return [];
            return Array.isArray(role) ? role : [role];
        } catch {
            return [];
        }
    }

    function getEffectiveRoles() {
        const stored = StorageManager.getUserRoles() || [];
        if (stored.length > 0) return stored;
        return rolesFromAccessToken();
    }

    function hasBackOfficeAccess() {
        const userRoles = getEffectiveRoles();
        const userPerms = StorageManager.getPermissions?.() || [];
        if (BACK_OFFICE_ROLES.some((r) => userRoles.includes(r))) return true;
        if (StorageManager.hasAnyPermission(userPerms, BACK_OFFICE_PERMISSIONS)) return true;
        if (typeof SecurityManager !== 'undefined' && typeof SecurityManager.canAccessTelecom === 'function') {
            return SecurityManager.canAccessTelecom({
                roles: BACK_OFFICE_ROLES,
                permissions: BACK_OFFICE_PERMISSIONS,
            });
        }
        return false;
    }

    async function ensureAccess() {
        try {
            if (typeof PortalNavigation !== 'undefined' && PortalNavigation.syncOperatorSession) {
                await PortalNavigation.syncOperatorSession(true);
            }
        } catch (e) {
            console.warn('BackOffice: operator session sync failed', e);
        }
        return hasBackOfficeAccess();
    }

    function computeStats() {
        const open = tickets.filter((x) => Number(x.status) === 0).length;
        const inProgress = tickets.filter((x) => Number(x.status) === 1).length;
        const critical = tickets.filter(
            (x) => Number(x.priority) === 3 && (Number(x.status) === 0 || Number(x.status) === 1)
        ).length;
        return { open, inProgress, critical, active: open + inProgress };
    }

    function updateKpis(activeCountFromApi) {
        const stats = computeStats();
        const countEl = document.getElementById('boTicketCount');
        const inProgEl = document.getElementById('boInProgressCount');
        const critEl = document.getElementById('boCriticalCount');
        const critCard = document.getElementById('boCriticalKpiCard');
        const active =
            activeCountFromApi != null && !Number.isNaN(Number(activeCountFromApi))
                ? Number(activeCountFromApi)
                : stats.active;

        if (countEl) countEl.textContent = String(active);
        if (inProgEl) inProgEl.textContent = String(stats.inProgress);
        if (critEl) critEl.textContent = String(stats.critical);
        if (critCard) critCard.classList.toggle('bo-ticket-critical', stats.critical > 0);

        const banner = document.getElementById('boCriticalBanner');
        const bannerText = document.getElementById('boCriticalBannerText');
        if (banner && bannerText) {
            if (stats.critical > 0) {
                const msg = t(
                    'backOffice.dashboard.criticalAlert',
                    `تنبيه: لديك ${stats.critical} تذكرة ذات أولوية حرجة تحتاج معالجة فورية.`
                ).replace('{count}', String(stats.critical));
                bannerText.textContent = msg;
                banner.classList.remove('d-none');
            } else {
                banner.classList.add('d-none');
            }
        }
    }

    function updateEmptyState() {
        const empty = document.getElementById('boEmptyQueue');
        const gridPanel = document.querySelector('.bo-tickets-panel');
        const hasRows = ticketPreview.length > 0;
        if (empty) empty.classList.toggle('d-none', hasRows);
        if (gridPanel) gridPanel.classList.toggle('d-none', !hasRows);
    }

    async function loadTickets() {
        const res = await AxiosManager.get('/TelecomBackOffice/GetTechnicalTickets', {
            params: { activeQueueOnly: true },
        });
        const content = res?.data?.content ?? res?.data?.Content;
        tickets = parseTicketList(res).map(normalizeTicket);
        ticketPreview = tickets.slice(0, PREVIEW_MAX);
        const activeCount = content?.activeOpenCount ?? content?.ActiveOpenCount;
        updateKpis(activeCount);
        updateEmptyState();
    }

    async function loadOfferings() {
        const res = await AxiosManager.get('/ProductOffering/GetProductOfferingList', {});
        offerings = StorageManager.apiList(res).map(normalizeOffering);
        const countEl = document.getElementById('boOfferingCount');
        if (countEl) countEl.textContent = String(offerings.length);
    }

    function withTimeout(promise, ms, label) {
        return Promise.race([
            promise,
            new Promise((_, reject) =>
                setTimeout(() => reject(new Error(`${label || 'Request'} timeout`)), ms)
            ),
        ]);
    }

    async function waitForSyncfusion(maxMs = 8000) {
        const step = 100;
        let waited = 0;
        while (typeof ej === 'undefined' || !ej.grids) {
            if (waited >= maxMs) return false;
            await new Promise((r) => setTimeout(r, step));
            waited += step;
        }
        return true;
    }

    function bindTicketActionButtons() {
        const host = document.getElementById('boTicketsGrid');
        if (!host) return;
        host.querySelectorAll('.bo-open-ticket').forEach((btn) => {
            btn.onclick = (ev) => {
                ev.stopPropagation();
                const id = btn.getAttribute('data-id');
                const row = ticketPreview.find((x) => x.id === id);
                if (row) openTicket(row);
            };
        });
    }

    function applyRowHighlight(args) {
        if (!args?.row) return;
        const cls = rowClass(args.data);
        if (!cls) return;
        cls.split(/\s+/).filter(Boolean).forEach((c) => args.row.classList.add(c));
    }

    function paintTicketGridCells(args) {
        if (!args?.cell || !args?.data) return;
        const row = args.data;
        if (args.column.field === 'statusLabel') {
            args.cell.innerHTML = statusBadge(row);
        } else if (args.column.field === 'priorityLabel') {
            args.cell.innerHTML = priorityBadge(row);
        } else if (args.column.field === 'createdByChannel') {
            args.cell.innerHTML = channelBadge(row);
        } else if (args.column.field === 'categoryLabelHtml') {
            args.cell.innerHTML = row.categoryLabelHtml || categoryLabelHtml(row.ticketCategory);
        } else if (args.column.field === '_boAction') {
            const id = escapeHtml(row.id);
            args.cell.innerHTML = `<button type="button" class="btn btn-sm btn-danger bo-open-ticket" data-id="${id}">معالجة</button>`;
        }
    }

    function boTicketsGridHeight() {
        return typeof computeTelecomGridHeight === 'function'
            ? computeTelecomGridHeight('.bo-tickets-panel')
            : 360;
    }

    function applyBoTicketsGridHeight() {
        if (!ticketsGrid) return;
        ticketsGrid.height = boTicketsGridHeight();
    }

    function initTicketsGrid() {
        const ticketsHost = document.getElementById('boTicketsGrid');
        if (!ticketsHost || ticketsGrid) return true;

        ticketsHost.classList.remove('d-none');
        ticketsHost.closest('.telecom-grid-panel')?.classList.remove('d-none');
        ticketsGrid = new ej.grids.Grid({
            id: 'BackOfficeTicketsGrid',
            dataSource: ticketPreview.slice(),
            height: boTicketsGridHeight(),
            width: '100%',
            allowPaging: true,
            allowSorting: true,
            allowFiltering: true,
            filterSettings: { type: 'Menu' },
            pageSettings: { pageSize: 10, pageSizes: [10, 20, 50] },
            emptyRecordTemplate: EMPTY_GRID_HTML,
            recordDoubleClick: (args) => openTicket(normalizeTicket(args.rowData)),
            rowDataBound: applyRowHighlight,
            queryCellInfo: paintTicketGridCells,
            columns: [
                { field: 'id', isPrimaryKey: true, visible: false },
                { field: 'ticketNumber', headerText: 'رقم التذكرة', width: 115 },
                { field: 'customerDisplayName', headerText: 'المشترك', width: 130 },
                { field: 'msisdn', headerText: 'رقم الخط', width: 108 },
                { field: 'categoryLabelHtml', headerText: 'نوع العملية', width: 130, allowFiltering: false },
                { field: 'issueLabel', headerText: 'المشكلة', width: 88 },
                { field: 'statusLabel', headerText: 'الحالة', width: 118, allowFiltering: false },
                { field: 'priorityLabel', headerText: 'الأولوية', width: 105, allowFiltering: false },
                { field: 'createdByChannel', headerText: 'المصدر', width: 140, allowFiltering: false },
                { field: 'notes', headerText: 'الشكوى', width: 200, minWidth: 120 },
                {
                    field: '_boAction',
                    headerText: 'إجراء',
                    width: 95,
                    allowSorting: false,
                    allowFiltering: false,
                },
            ],
            dataBound: () => bindTicketActionButtons(),
        });
        ticketsGrid.appendTo(ticketsHost);
        applyBoTicketsGridHeight();
        return true;
    }

    function initCatalogGrid() {
        const catalogHost = document.getElementById('boCatalogGrid');
        if (!catalogHost || catalogGrid) return true;

        catalogGrid = new ej.grids.Grid({
            dataSource: offerings.slice(),
            height: 260,
            allowPaging: true,
            allowSorting: true,
            pageSettings: { pageSize: 8 },
            columns: [
                { field: 'name', headerText: 'العرض', width: 200 },
                { field: 'code', headerText: 'الكود', width: 100 },
                { field: 'defaultPrice', headerText: 'السعر', width: 90, format: 'N0' },
                {
                    headerText: 'الحالة',
                    width: 100,
                    template:
                        '<button type="button" class="btn btn-sm ${isActive ? "btn-success" : "btn-outline-secondary"} bo-toggle-offer" data-id="${id}">${isActive ? "مفعّل" : "متوقف"}</button>',
                },
            ],
            dataBound: () => {
                catalogHost.querySelectorAll('.bo-toggle-offer').forEach((btn) => {
                    btn.onclick = (ev) => {
                        ev.stopPropagation();
                        const id = btn.getAttribute('data-id');
                        const row = offerings.find((o) => o.id === id);
                        if (row) toggleOffering(row);
                    };
                });
            },
        });
        catalogGrid.appendTo(catalogHost);
        return true;
    }

    function refreshTicketsGrid() {
        if (!ticketsGrid) return;
        ticketsGrid.dataSource = ticketPreview.slice();
        applyBoTicketsGridHeight();
        if (typeof ticketsGrid.dataBind === 'function') ticketsGrid.dataBind();
        else ticketsGrid.refresh();
        bindTicketActionButtons();
        updateEmptyState();
    }

    function refreshCatalogGrid() {
        if (!catalogGrid) return;
        catalogGrid.dataSource = offerings.slice();
        if (typeof catalogGrid.dataBind === 'function') catalogGrid.dataBind();
        else catalogGrid.refresh();
    }

    function fillDrawer(row, detail) {
        const status = pick(detail, 'status', 'Status') ?? row.status ?? 0;
        document.getElementById('boDrawerTitle').textContent = pick(detail, 'ticketNumber', 'TicketNumber') || row.ticketNumber || '—';
        document.getElementById('boDrawerMsisdn').textContent = pick(detail, 'msisdn', 'Msisdn') || row.msisdn || '—';
        document.getElementById('boDrawerCustomer').textContent = row.customerDisplayName || '—';
        document.getElementById('boDrawerIssue').textContent =
            ISSUE[Number(pick(detail, 'issueType', 'IssueType') ?? row.issueType)] ?? '—';

        const categoryKey = resolveCategoryKey(detail ?? row);
        const catHost = document.getElementById('boDrawerCategory');
        if (catHost) catHost.innerHTML = categoryLabelHtml(categoryKey);
        updateDrawerToolbar(categoryKey);

        const priEl = document.getElementById('boDrawerPriority');
        if (priEl) {
            const pri = Number(pick(detail, 'priority', 'Priority') ?? row.priority);
            priEl.innerHTML = window.TelecomUiBadges?.ticketPriority(pri, PRIORITY[pri]) || PRIORITY[pri] || '—';
        }

        const badgeHost = document.getElementById('boDrawerStatusBadge');
        if (badgeHost) {
            badgeHost.innerHTML =
                window.TelecomUiBadges?.ticketStatus(status, STATUS[Number(status)]) ||
                STATUS[Number(status)] ||
                '';
        }

        const complaint = pick(detail, 'notes', 'Notes') || row.notes || '—';
        document.getElementById('boDrawerComplaint').textContent = complaint;

        const statusSel = document.getElementById('boTicketStatus');
        if (statusSel) statusSel.value = String(status);

        const pri = Number(pick(detail, 'priority', 'Priority') ?? row.priority ?? 1);
        const prioritySel = document.getElementById('boTicketPriority');
        if (prioritySel) prioritySel.value = String(pri);

        const notesEl = document.getElementById('boOperatorNotes');
        if (notesEl) notesEl.value = row.resolutionNotes || pick(detail, 'resolutionNotes', 'ResolutionNotes') || '';

        const payload =
            pick(detail, 'payloadJson', 'PayloadJson') || row.payloadJson || row.notes || '—';
        document.getElementById('boDrawerPayload').textContent = payload;
    }

    async function openTicket(row) {
        try {
            const res = await AxiosManager.get('/TelecomBackOffice/GetTechnicalTicketSingle', {
                params: { id: row.id },
            });
            const d = StorageManager.apiContent(res);
            if (!d) {
                Swal.fire({ icon: 'warning', title: 'تعذر تحميل تفاصيل التذكرة' });
                return;
            }

            selectedTicket = {
                id: pick(d, 'id', 'Id'),
                ticketNumber: pick(d, 'ticketNumber', 'TicketNumber'),
                msisdn: pick(d, 'msisdn', 'Msisdn'),
                subscriberProfileId: pick(d, 'subscriberProfileId', 'SubscriberProfileId'),
                issueType: pick(d, 'issueType', 'IssueType'),
                ticketCategory: resolveCategoryKey(d ?? row),
                priority: pick(d, 'priority', 'Priority'),
                status: pick(d, 'status', 'Status') ?? row.status,
                notes: pick(d, 'notes', 'Notes'),
                payloadJson: pick(d, 'payloadJson', 'PayloadJson'),
            };

            hideLiveStatusPanel();
            hideTier3Panel();
            fillDrawer(row, d);
            ticketDrawer?.show();
        } catch (e) {
            Swal.fire({ icon: 'error', title: e?.response?.data?.message || 'تعذر فتح التذكرة' });
        }
    }

    const TELECOM_ACTION_IDS = [
        'boSaveStatusBtn',
        'boResolveOpenBtn',
        'boHlrBtn',
        'btnCbsForceSync',
        'btnNetworkPing',
        'btnCoreEscalate',
    ];

    function setTelecomActionBusy(on) {
        isTelecomActionInFlight = on;
        TELECOM_ACTION_IDS.forEach((id) => {
            const el = document.getElementById(id);
            if (!el) return;
            el.disabled = on;
            el.classList.toggle('bo-action-busy', on);
        });
    }

    function hideLiveStatusPanel() {
        const panel = document.getElementById('boLiveStatusPanel');
        const body = document.getElementById('boLiveStatusBody');
        if (body) body.textContent = '—';
        if (liveStatusCollapse) liveStatusCollapse.hide();
        else if (panel) panel.classList.remove('show');
    }

    function hideTier3Panel() {
        const notes = document.getElementById('boTier3Notes');
        if (notes) notes.value = '';
        const panel = document.getElementById('boTier3Panel');
        if (tier3Collapse) tier3Collapse.hide();
        else if (panel) panel.classList.remove('show');
    }

    function showTier3Panel() {
        hideLiveStatusPanel();
        const panel = document.getElementById('boTier3Panel');
        const notes = document.getElementById('boTier3Notes');
        if (tier3Collapse) tier3Collapse.show();
        else if (panel) panel.classList.add('show');
        setTimeout(() => notes?.focus(), 150);
    }

    function releaseDrawerFocusTrap() {
        const drawerEl = document.getElementById('ticketDrawer');
        if (!drawerEl) return null;
        drawerEl.setAttribute('data-bs-focus', 'false');
        const inst = typeof bootstrap !== 'undefined' ? bootstrap.Offcanvas.getInstance(drawerEl) : null;
        const trap = inst?._focustrap;
        trap?.deactivate?.();
        return { drawerEl, trap };
    }

    function restoreDrawerFocusTrap(released) {
        if (!released?.drawerEl) return;
        released.drawerEl.setAttribute('data-bs-focus', 'true');
        released.trap?.activate?.();
    }

    function renderLiveStatus(data) {
        const body = document.getElementById('boLiveStatusBody');
        if (!body || !data) return;
        const lines = [
            `MSISDN: ${pick(data, 'msisdn', 'Msisdn') || '—'}`,
            `CBS: ${pick(data, 'cbsBalanceLabel', 'CbsBalanceLabel') || pick(data, 'cbsBalance', 'CbsBalance')}`,
            `HLR State: ${pick(data, 'hlrSubscriberState', 'HlrSubscriberState') || '—'}`,
            `HLR Online: ${pick(data, 'hlrIsOnline', 'HlrIsOnline') ? 'YES' : 'NO'}`,
            `HLR Site: ${pick(data, 'hlrLocation', 'HlrLocation') || '—'}`,
            `CRM Status: ${pick(data, 'crmOperationalStatus', 'CrmOperationalStatus') || '—'}`,
            `Package: ${pick(data, 'productOfferingName', 'ProductOfferingName') || '—'}`,
            `CRM≠HLR: ${pick(data, 'differsFromCrm', 'DiffersFromCrm') ? 'YES' : 'NO'}`,
        ];
        body.textContent = lines.join('\n');
        const panel = document.getElementById('boLiveStatusPanel');
        if (liveStatusCollapse) liveStatusCollapse.show();
        else if (panel) panel.classList.add('show');
    }

    async function withTelecomAction(fn) {
        if (!selectedTicket) return;
        if (isTelecomActionInFlight) return;
        setTelecomActionBusy(true);
        try {
            await fn();
        } finally {
            setTelecomActionBusy(false);
        }
    }

    function hideTicketDrawer() {
        hideLiveStatusPanel();
        hideTier3Panel();
        selectedTicket = null;
        const drawerEl = document.getElementById('ticketDrawer');
        if (ticketDrawer) {
            ticketDrawer.hide();
        } else if (drawerEl && typeof bootstrap !== 'undefined') {
            bootstrap.Offcanvas.getInstance(drawerEl)?.hide();
        }
    }

    async function refreshDashboardQueue() {
        await loadDashboardData(false);
        if (typeof PortalNavigation !== 'undefined' && PortalNavigation.refreshBadges) {
            try {
                await PortalNavigation.refreshBadges();
            } catch (e) {
                console.warn('BackOffice: menu badge refresh failed', e);
            }
        }
    }

    const QUEUE_REMOVED_SUFFIX = ' — تم إزالتها من طابور العمل الحي';

    function queueOutcomeSuffix(body) {
        const resolved = pick(body, 'ticketAutoResolved', 'TicketAutoResolved');
        if (resolved === true || resolved === 'true') {
            return QUEUE_REMOVED_SUFFIX;
        }
        const moved = pick(body, 'ticketMovedToInProgress', 'TicketMovedToInProgress');
        if (moved === true || moved === 'true') {
            return ' — تم تحديث الحالة إلى «قيد المعالجة»';
        }
        return '';
    }

    function pickActionMessage(res, fallback) {
        const body = StorageManager.apiContent(res);
        return pick(body, 'message', 'Message') || fallback;
    }

    function showTelecomActionError(e, fallback) {
        Swal.fire({ icon: 'error', title: e?.response?.data?.message || fallback });
    }

    async function finalizeTelecomActionSuccess(message, options = {}) {
        const { closeDrawer = true, refreshQueue = true, timer = 2800 } = options;
        const title = message || t('backOffice.dashboard.actionOk', 'تم تنفيذ الإجراء بنجاح');
        if (closeDrawer) hideTicketDrawer();
        if (refreshQueue) await refreshDashboardQueue();
        Swal.fire({ icon: 'success', title, timer, showConfirmButton: false });
    }

    async function finalizeTelecomActionInDrawer(message) {
        Swal.fire({
            icon: 'success',
            title: message,
            timer: 2200,
            showConfirmButton: false,
        });
    }

    async function hlrResync() {
        await withTelecomAction(async () => {
            const res = await AxiosManager.post('/TelecomBackOffice/HlrResyncByMsisdn', {
                msisdn: selectedTicket.msisdn,
                technicalTicketId: selectedTicket.id,
            });
            const body = StorageManager.apiContent(res);
            const defaultOk = t('backOffice.hlrOk', 'تمت مزامنة HLR بنجاح');
            await finalizeTelecomActionSuccess(pickActionMessage(res, defaultOk) + queueOutcomeSuffix(body));
        }).catch((e) => showTelecomActionError(e, 'فشلت مزامنة HLR'));
    }

    async function forceCbsSync() {
        await withTelecomAction(async () => {
            const res = await AxiosManager.post('/TelecomBackOffice/ForceCbsSync', {
                ticketId: selectedTicket.id,
            });
            const body = StorageManager.apiContent(res);
            const defaultOk = t(
                'backOffice.dashboard.cbsForceOk',
                'تم دفع الشحنة وتسوية الفوترة بنجاح على سيرفر هواوي CBS'
            );
            await finalizeTelecomActionSuccess(pickActionMessage(res, defaultOk) + queueOutcomeSuffix(body));
        }).catch((e) => showTelecomActionError(e, 'تعذر الاتصال بسيرفر CBS الطوارئ'));
    }

    async function queryLiveNetworkStatus() {
        await withTelecomAction(async () => {
            const res = await AxiosManager.get('/TelecomBackOffice/QueryLiveNetworkStatus', {
                params: { ticketId: selectedTicket.id },
            });
            const content = StorageManager.apiContent(res);
            const data = content?.data ?? content?.Data ?? content;
            if (!data || typeof data !== 'object') {
                Swal.fire({ icon: 'warning', title: 'لا توجد بيانات من الشبكة الحية' });
                return;
            }
            renderLiveStatus(data);
            await finalizeTelecomActionInDrawer(
                t('backOffice.dashboard.networkPingOk', 'تم استعلام حالة الشبكة الحية — راجع القياسات أدناه')
            );
            await refreshDashboardQueue();
        }).catch((e) => showTelecomActionError(e, 'فشل استعلام الشبكة'));
    }

    function openTier3EscalationPanel() {
        if (!selectedTicket) return;
        showTier3Panel();
    }

    async function submitTier3Escalation() {
        if (!selectedTicket) return;
        const notes = document.getElementById('boTier3Notes')?.value?.trim() || '';
        if (notes.length < 10) {
            Swal.fire({ icon: 'warning', title: 'أدخل سبب التصعيد (10 أحرف على الأقل)' });
            document.getElementById('boTier3Notes')?.focus();
            return;
        }

        await withTelecomAction(async () => {
            await AxiosManager.post('/TelecomBackOffice/EscalateToTier3', {
                ticketId: selectedTicket.id,
                escalationNotes: notes,
            });
            await finalizeTelecomActionSuccess(
                t('backOffice.dashboard.tier3Ok', 'تم تصعيد التذكرة لفريق الشبكة الأساسية') + QUEUE_REMOVED_SUFFIX
            );
        }).catch((e) => showTelecomActionError(e, 'فشل التصعيد'));
    }

    async function saveTicketStatus() {
        if (!selectedTicket) return;
        const statusSel = document.getElementById('boTicketStatus');
        const prioritySel = document.getElementById('boTicketPriority');
        const notesEl = document.getElementById('boOperatorNotes');
        const newStatus = Number(statusSel?.value ?? 0);
        const newPriority = Number(prioritySel?.value ?? 1);
        const notes = notesEl?.value?.trim() || '';

        if (newStatus === 2 && notes.length < 5) {
            Swal.fire({ icon: 'warning', title: 'أدخل ملاحظات الحل (5 أحرف على الأقل)' });
            notesEl?.focus();
            return;
        }

        await withTelecomAction(async () => {
            await AxiosManager.post('/TelecomBackOffice/UpdateTechnicalTicketStatus', {
                ticketId: selectedTicket.id,
                newStatus,
                newPriority,
                operatorNotesAr: notes,
            });
            let msg = t('backOffice.dashboard.statusUpdated', 'تم تحديث حالة التذكرة');
            if (newStatus === 2) {
                msg = t('backOffice.dashboard.resolvedQueue', 'تم إغلاق التذكرة') + QUEUE_REMOVED_SUFFIX;
            } else if (newStatus === 3) {
                msg = t('backOffice.dashboard.escalatedQueue', 'تم تصعيد التذكرة') + QUEUE_REMOVED_SUFFIX;
            } else if (newStatus === 1) {
                msg = t('backOffice.dashboard.inProgressQueue', 'التذكرة الآن قيد المعالجة في الطابور');
            }
            await finalizeTelecomActionSuccess(msg);
        }).catch((e) => showTelecomActionError(e, 'تعذر تحديث الحالة'));
    }

    function quickResolve() {
        const statusSel = document.getElementById('boTicketStatus');
        const notesEl = document.getElementById('boOperatorNotes');
        if (statusSel) statusSel.value = '2';
        if (notesEl) {
            notesEl.focus();
            if (!notesEl.value.trim()) {
                notesEl.placeholder = 'اشرح الإجراء الفني قبل الإغلاق السريع…';
            }
        }
        Swal.fire({
            icon: 'info',
            title: 'إغلاق سريع',
            text: 'اكتب ملاحظات الحل ثم اضغط «حفظ الحالة»',
            timer: 2800,
            showConfirmButton: false,
        });
    }

    async function toggleOffering(row) {
        try {
            const res = await AxiosManager.post('/TelecomBackOffice/ToggleProductOfferingActive', { id: row.id });
            const body = StorageManager.apiContent(res);
            row.isActive = pick(body, 'isActive', 'IsActive') ?? !row.isActive;
            if (catalogGrid) catalogGrid.refresh();
        } catch (e) {
            Swal.fire({ icon: 'error', title: e?.response?.data?.message || 'تعذر تغيير حالة العرض' });
        }
    }

    function canReversePayment() {
        const roles = StorageManager.getUserRoles?.() || [];
        return roles.some((r) => ['TelecomManagement', 'TelecomBackOffice', 'TelecomAdmin'].includes(r));
    }

    const paymentStatusLabel = (s) => {
        const map = { 0: 'مسودة', 1: 'بوابة', 2: 'مكتمل', 3: 'فاشل', 4: 'معكوس' };
        return map[s] ?? String(s);
    };

    async function loadPaymentServicesPanel() {
        const tbody = document.getElementById('boPaymentTxBody');
        try {
            const kpiRes = await AxiosManager.get('/Telecom/GetPaymentServicesKpis', {});
            const k = kpiRes?.data?.content ?? kpiRes?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('payKpiAmount', `${(pick(k, 'totalRechargedAmountToday', 'TotalRechargedAmountToday') ?? 0).toLocaleString('ar-SY')} ل.س`);
            set('payKpiCompleted', pick(k, 'completedCountToday', 'CompletedCountToday'));
            set('payKpiFailed', pick(k, 'failedCountToday', 'FailedCountToday'));
            set('payKpiFailRate', `${pick(k, 'failureRatePercent', 'FailureRatePercent') ?? 0}%`);
            set('payKpiSla', `${pick(k, 'slaCompliancePercent', 'SlaCompliancePercent') ?? 0}%`);
            set('payKpiReversed', pick(k, 'reversedCountToday', 'ReversedCountToday'));
            const refreshEl = document.getElementById('boPaymentKpiRefresh');
            if (refreshEl) refreshEl.textContent = new Date().toLocaleString('ar-SY');

            const listRes = await AxiosManager.get('/Telecom/GetPaymentTransactionList?take=25', {});
            const items = listRes?.data?.content?.items ?? listRes?.data?.Content?.Items ?? [];
            if (!tbody) return;
            if (!items.length) {
                tbody.innerHTML = '<tr><td colspan="5" class="text-muted text-center">لا معاملات</td></tr>';
                return;
            }
            tbody.innerHTML = items
                .map((row) => {
                    const id = row.id ?? row.Id;
                    const canRev = row.canReverse ?? row.CanReverse;
                    const btn =
                        canRev && canReversePayment()
                            ? `<button type="button" class="btn btn-outline-danger btn-sm py-0 btn-pay-reverse" data-id="${id}">عكس</button>`
                            : '';
                    return `<tr>
                        <td class="font-monospace small">${row.number ?? row.Number}</td>
                        <td dir="ltr" class="small">${row.msisdn ?? row.Msisdn ?? '—'}</td>
                        <td>${(row.amount ?? row.Amount ?? 0).toLocaleString('ar-SY')}</td>
                        <td>${paymentStatusLabel(row.status ?? row.Status)}</td>
                        <td>${btn}</td>
                    </tr>`;
                })
                .join('');
            tbody.querySelectorAll('.btn-pay-reverse').forEach((btn) => {
                btn.addEventListener('click', () => reversePayment(btn.getAttribute('data-id')));
            });
        } catch (e) {
            console.warn('Payment services panel', e);
            if (tbody) tbody.innerHTML = '<tr><td colspan="5" class="text-danger text-center">تعذّر التحميل</td></tr>';
        }
    }

    async function reversePayment(paymentId) {
        if (!paymentId) return;
        const { value: reason } = await Swal.fire({
            title: 'عكس العملية المالية',
            input: 'text',
            inputPlaceholder: 'سبب العكس (مثال: خطأ موظف)',
            showCancelButton: true,
            confirmButtonText: 'تأكيد العكس',
            confirmButtonColor: '#c8102e',
            inputValidator: (v) => (!v || !String(v).trim() ? 'السبب مطلوب' : undefined),
        });
        if (!reason) return;
        try {
            const res = await AxiosManager.post('/Telecom/ReversePaymentTransaction', {
                paymentId,
                reasonCode: String(reason).trim(),
                reversedById: StorageManager.getUserId(),
            });
            const body = res?.data?.content ?? res?.data?.Content;
            await loadPaymentServicesPanel();
            Swal.fire({ icon: 'success', title: body?.messageAr || body?.MessageAr || 'تم العكس' });
        } catch (e) {
            Swal.fire({ icon: 'error', title: e?.response?.data?.message || e?.message || 'تعذّر العكس' });
        }
    }

    async function loadDeviceSaleKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetDeviceSaleKpis', {});
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('devKpiVolume', pick(c, 'totalVolume', 'TotalVolume'));
            set('devKpiCompleted', pick(c, 'completedCount', 'CompletedCount'));
            set('devKpiFailed', pick(c, 'failedCount', 'FailedCount'));
            set('devKpiCompletion', `${pick(c, 'completionRatePercent', 'CompletionRatePercent') ?? 0}%`);
            set('devKpiFallout', `${pick(c, 'falloutRatePercent', 'FalloutRatePercent') ?? 0}%`);
            set('devKpiInstallment', pick(c, 'installmentCount', 'InstallmentCount'));
            set('devKpiCash', pick(c, 'cashCount', 'CashCount'));
            set('devKpiOverrides', pick(c, 'manualOverrideCount', 'ManualOverrideCount'));
            const rejections = c.rejectionReasons ?? c.RejectionReasons ?? [];
            set(
                'devKpiRejections',
                rejections.length
                    ? rejections.map((r) => `${r.reason ?? r.Reason} (${r.count ?? r.Count})`).join(' · ')
                    : '—'
            );
        } catch (e) {
            console.warn('DeviceSale KPIs', e);
        }
    }

    async function loadBadDebtKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetBadDebtKpis', {});
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('bdrKpiTotal', pick(c, 'totalToday', 'TotalToday'));
            set('bdrKpiCompleted', pick(c, 'completedToday', 'CompletedToday'));
            set('bdrKpiFailed', pick(c, 'failedToday', 'FailedToday'));
            set('bdrKpiPending', pick(c, 'pendingBackOffice', 'PendingBackOffice'));
            const col = pick(c, 'collectedAmountToday', 'CollectedAmountToday');
            const wo = pick(c, 'writeOffAmountToday', 'WriteOffAmountToday');
            set('bdrKpiCollected', col != null && col !== '' ? `${col} ل.س` : '—');
            set('bdrKpiWriteOff', wo != null && wo !== '' ? `${wo} ل.س` : '—');
            set('bdrKpiPlans', pick(c, 'paymentPlansToday', 'PaymentPlansToday'));
            set('bdrKpiFailRate', `${pick(c, 'failureRatePercent', 'FailureRatePercent') ?? 0}%`);
        } catch (e) {
            console.warn('BadDebt KPIs', e);
        }
    }

    async function loadRefundKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetRefundKpis', {});
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('rfdKpiTotal', pick(c, 'totalToday', 'TotalToday'));
            set('rfdKpiCompleted', pick(c, 'completedToday', 'CompletedToday'));
            set('rfdKpiFailed', pick(c, 'failedToday', 'FailedToday'));
            set('rfdKpiPending', pick(c, 'pendingBackOffice', 'PendingBackOffice'));
            const amt = pick(c, 'settledAmountToday', 'SettledAmountToday');
            set('rfdKpiSettledAmount', amt != null && amt !== '' ? `${amt} ل.س` : '—');
            set('rfdKpiDual', pick(c, 'dualApprovalToday', 'DualApprovalToday'));
            set('rfdKpiFailRate', `${pick(c, 'failureRatePercent', 'FailureRatePercent') ?? 0}%`);
            set('rfdKpiSla', `${pick(c, 'slaCompliancePercent', 'SlaCompliancePercent') ?? 0}%`);
            const rejections = c.rejectionReasons ?? c.RejectionReasons ?? [];
            set(
                'rfdKpiRejections',
                rejections.length
                    ? rejections.map((r) => `${r.reason ?? r.Reason} (${r.count ?? r.Count})`).join(' · ')
                    : '—'
            );
        } catch (e) {
            console.warn('Refund KPIs', e);
        }
    }

    async function loadSuspensionKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetSuspensionKpis', {});
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('susKpiTotal', pick(c, 'totalToday', 'TotalToday'));
            set('susKpiCompleted', pick(c, 'completedToday', 'CompletedToday'));
            set('susKpiFailed', pick(c, 'failedToday', 'FailedToday'));
            set('susKpiPending', pick(c, 'pendingBackOffice', 'PendingBackOffice'));
            set('susKpiFraud', pick(c, 'fraudToday', 'FraudToday'));
            set('susKpiAuto', pick(c, 'autoReconnectEnabledToday', 'AutoReconnectEnabledToday'));
            set('susKpiFailRate', `${pick(c, 'failureRatePercent', 'FailureRatePercent') ?? 0}%`);
            set('susKpiSla', `${pick(c, 'slaCompliancePercent', 'SlaCompliancePercent') ?? 0}%`);
            const reasons = c.topReasons ?? c.TopReasons ?? [];
            set(
                'susKpiReasons',
                reasons.length
                    ? reasons.map((r) => `${r.reason ?? r.Reason} (${r.count ?? r.Count})`).join(' · ')
                    : '—'
            );
        } catch (e) {
            console.warn('Suspension KPIs', e);
        }
    }

    async function loadReconnectKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetReconnectKpis', {});
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('rcnKpiTotal', pick(c, 'totalToday', 'TotalToday'));
            set('rcnKpiCompleted', pick(c, 'completedToday', 'CompletedToday'));
            set('rcnKpiFailed', pick(c, 'failedToday', 'FailedToday'));
            set('rcnKpiPending', pick(c, 'pendingBackOffice', 'PendingBackOffice'));
            set('rcnKpiPayment', pick(c, 'paymentClearedToday', 'PaymentClearedToday'));
            set('rcnKpiFraud', pick(c, 'fraudClearanceToday', 'FraudClearanceToday'));
            set('rcnKpiFailRate', `${pick(c, 'failureRatePercent', 'FailureRatePercent') ?? 0}%`);
            set('rcnKpiSla', `${pick(c, 'slaCompliancePercent', 'SlaCompliancePercent') ?? 0}%`);
            const reasons = c.topReasons ?? c.TopReasons ?? [];
            set(
                'rcnKpiReasons',
                reasons.length
                    ? reasons.map((r) => `${r.reason ?? r.Reason} (${r.count ?? r.Count})`).join(' · ')
                    : '—'
            );
        } catch (e) {
            console.warn('Reconnect KPIs', e);
        }
    }

    async function loadTerminationKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetTerminationKpis', {});
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('trmKpiTotal', pick(c, 'totalToday', 'TotalToday'));
            set('trmKpiCompleted', pick(c, 'completedToday', 'CompletedToday'));
            set('trmKpiFailed', pick(c, 'failedToday', 'FailedToday'));
            set('trmKpiPending', pick(c, 'pendingBackOffice', 'PendingBackOffice'));
            set('trmKpiVoluntary', pick(c, 'voluntaryToday', 'VoluntaryToday'));
            const bills = pick(c, 'finalBillTotalToday', 'FinalBillTotalToday');
            set('trmKpiFinalBills', bills != null && bills !== '' ? bills : '—');
            set('trmKpiFailRate', `${pick(c, 'failureRatePercent', 'FailureRatePercent') ?? 0}%`);
            set('trmKpiSla', `${pick(c, 'slaCompliancePercent', 'SlaCompliancePercent') ?? 0}%`);
            const reasons = c.topReasons ?? c.TopReasons ?? [];
            const txt = reasons.length
                ? reasons
                      .map((r) => {
                          const bo = r.backOfficeCount ?? r.BackOfficeCount ?? 0;
                          const suffix = bo > 0 ? ` · ${bo} BO` : '';
                          return `${r.reason ?? r.Reason} (${r.count ?? r.Count})${suffix}`;
                      })
                      .join(' · ')
                : '—';
            set('trmKpiReasons', txt);
        } catch (e) {
            console.warn('Termination KPIs', e);
        }
    }

    async function loadOfferSubscriptionKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetOfferSubscriptionKpis', {});
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('osKpiMgrTotal', pick(c, 'migrationTotalToday', 'MigrationTotalToday'));
            set('osKpiMgrCompleted', pick(c, 'migrationCompletedToday', 'MigrationCompletedToday'));
            set('osKpiMgrFailed', pick(c, 'migrationFailedToday', 'MigrationFailedToday'));
            set('osKpiVasActivate', pick(c, 'vasActivateToday', 'VasActivateToday'));
            set('osKpiVasDeactivate', pick(c, 'vasDeactivateToday', 'VasDeactivateToday'));
            set('osKpiFailRate', `${pick(c, 'failureRatePercent', 'FailureRatePercent') ?? 0}%`);
            set('osKpiSla', `${pick(c, 'slaCompliancePercent', 'SlaCompliancePercent') ?? 0}%`);
            const top = c.topMigratedOffers ?? c.TopMigratedOffers ?? [];
            const txt = top.length
                ? top.map((r) => `${r.label ?? r.Label} (${r.count ?? r.Count})`).join(' · ')
                : '—';
            set('osKpiTopOffers', txt);
        } catch (e) {
            console.warn('Offer subscription KPIs', e);
        }
    }

    async function loadChangeNumberKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetChangeNumberKpis', {});
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('cnrKpiTotal', pick(c, 'totalToday', 'TotalToday'));
            set('cnrKpiCompleted', pick(c, 'completedToday', 'CompletedToday'));
            set('cnrKpiFailed', pick(c, 'failedToday', 'FailedToday'));
            set('cnrKpiPending', pick(c, 'pendingBackOffice', 'PendingBackOffice'));
            set('cnrKpiPremium', pick(c, 'premiumToday', 'PremiumToday'));
            const fees = pick(c, 'premiumFeeTotalToday', 'PremiumFeeTotalToday');
            set('cnrKpiPremiumFees', fees != null && fees !== '' ? fees : '—');
            set('cnrKpiFailRate', `${pick(c, 'failureRatePercent', 'FailureRatePercent') ?? 0}%`);
            set('cnrKpiSla', `${pick(c, 'slaCompliancePercent', 'SlaCompliancePercent') ?? 0}%`);
            const reasons = c.topReasons ?? c.TopReasons ?? [];
            const txt = reasons.length
                ? reasons
                      .map((r) => {
                          const prem = r.premiumCount ?? r.PremiumCount ?? 0;
                          const suffix = prem > 0 ? ` · ${prem} مميز` : '';
                          return `${r.reason ?? r.Reason} (${r.count ?? r.Count})${suffix}`;
                      })
                      .join(' · ')
                : '—';
            set('cnrKpiReasons', txt);
        } catch (e) {
            console.warn('ChangeNumber KPIs', e);
        }
    }

    async function loadSimSwapKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetSimSwapKpis', {});
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('simKpiTotal', pick(c, 'totalToday', 'TotalToday'));
            set('simKpiCompleted', pick(c, 'completedToday', 'CompletedToday'));
            set('simKpiFailed', pick(c, 'failedToday', 'FailedToday'));
            set('simKpiPending', pick(c, 'pendingBackOffice', 'PendingBackOffice'));
            set('simKpiLost', pick(c, 'lostOrStolenToday', 'LostOrStolenToday'));
            set('simKpiFailRate', `${pick(c, 'failureRatePercent', 'FailureRatePercent') ?? 0}%`);
            set('simKpiSla', `${pick(c, 'slaCompliancePercent', 'SlaCompliancePercent') ?? 0}%`);
            const reasons = c.topReasons ?? c.TopReasons ?? [];
            const txt = reasons.length
                ? reasons
                      .map((r) => {
                          const lost = r.lostOrStolenCount ?? r.LostOrStolenCount ?? 0;
                          const suffix = lost > 0 ? ` · ${lost} سرقة/ضياع` : '';
                          return `${r.reason ?? r.Reason} (${r.count ?? r.Count})${suffix}`;
                      })
                      .join(' · ')
                : '—';
            set('simKpiReasons', txt);
        } catch (e) {
            console.warn('SimSwap KPIs', e);
        }
    }

    async function loadTakeOverKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetTakeOverOwnershipKpis', {});
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('tkoKpiTotal', pick(c, 'totalToday', 'TotalToday'));
            set('tkoKpiCompleted', pick(c, 'completedToday', 'CompletedToday'));
            set('tkoKpiFailed', pick(c, 'failedToday', 'FailedToday'));
            set('tkoKpiPending', pick(c, 'pendingBackOffice', 'PendingBackOffice'));
            set('tkoKpiFailRate', `${pick(c, 'failureRatePercent', 'FailureRatePercent') ?? 0}%`);
            set('tkoKpiSla', `${pick(c, 'slaCompliancePercent', 'SlaCompliancePercent') ?? 0}%`);
            const reasons = c.topReasons ?? c.TopReasons ?? [];
            const txt = reasons.length
                ? reasons.map((r) => `${r.reason ?? r.Reason} (${r.count ?? r.Count})`).join(' · ')
                : '—';
            set('tkoKpiReasons', txt);
        } catch (e) {
            console.warn('TakeOver KPIs', e);
        }
    }

    async function loadChangeGsmKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetChangeGsmTypeKpis', {});
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('cgtKpiTotal', pick(c, 'totalToday', 'TotalToday'));
            set('cgtKpiCompleted', pick(c, 'completedToday', 'CompletedToday'));
            set('cgtKpiFailed', pick(c, 'failedToday', 'FailedToday'));
            set('cgtKpiFailRate', `${pick(c, 'failureRatePercent', 'FailureRatePercent') ?? 0}%`);
            const reasons = c.topReasons ?? c.TopReasons ?? [];
            const txt = reasons.length
                ? reasons.map((r) => `${r.reason ?? r.Reason} (${r.count ?? r.Count})`).join(' · ')
                : '—';
            set('cgtKpiReasons', txt);
        } catch (e) {
            console.warn('ChangeGsm KPIs', e);
        }
    }

    async function loadSellingLineKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetSellingLineActivationKpis', {});
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('slKpiVolume', pick(c, 'totalVolume', 'TotalVolume'));
            set('slKpiCompletion', `${pick(c, 'completionRatePercent', 'CompletionRatePercent') ?? 0}%`);
            set('slKpiFallout', `${pick(c, 'falloutRatePercent', 'FalloutRatePercent') ?? 0}%`);
            set('slKpiSla', `${pick(c, 'slaCompliancePercent', 'SlaCompliancePercent') ?? 0}%`);
            set('slKpiAht', pick(c, 'avgHandlingTimeMinutes', 'AvgHandlingTimeMinutes'));
            set('slKpiOverride', pick(c, 'manualOverrideCount', 'ManualOverrideCount'));
        } catch (e) {
            console.warn('Selling line KPIs', e);
        }
    }

    async function loadDashboardData(showLoader = true) {
        if (showLoader) setLoading(true);
        document.getElementById('bo-boot-error')?.classList.add('d-none');

        try {
            loadChangeGsmKpis();
            loadChangeNumberKpis();
            loadOfferSubscriptionKpis();
            loadTerminationKpis();
            loadRefundKpis();
            loadDeviceSaleKpis();
            loadBadDebtKpis();
            loadSuspensionKpis();
            loadReconnectKpis();
            loadSimSwapKpis();
            loadTakeOverKpis();
            loadSellingLineKpis();
            loadPaymentServicesPanel();
            const ticketsPromise = loadTickets().catch((e) => {
                tickets = [];
                ticketPreview = [];
                updateKpis(0);
                updateEmptyState();
                showError(e?.response?.data?.message || e?.message || 'تعذر تحميل التذاكر');
            });

            const offeringsPromise = withTimeout(loadOfferings(), 12000, 'Catalog').catch(() => {
                offerings = [];
                const countEl = document.getElementById('boOfferingCount');
                if (countEl) countEl.textContent = '0';
            });

            await Promise.all([ticketsPromise, offeringsPromise]);

            const sfReady = await waitForSyncfusion();
            if (!sfReady) {
                showError('مكتبة الجداول (Syncfusion) غير محمّلة — أعد تحميل الصفحة');
                return;
            }

            if (ticketPreview.length > 0) {
                if (!ticketsGrid) {
                    try {
                        initTicketsGrid();
                    } catch (gridErr) {
                        console.error('BackOffice tickets grid init failed', gridErr);
                        showError('تعذر عرض جدول التذاكر — جرّب تحديث الصفحة');
                    }
                }
                refreshTicketsGrid();
            } else {
                updateEmptyState();
            }
            setLastRefresh();
        } finally {
            setLoading(false);
        }
    }

    function wireCatalogLazyInit() {
        const collapse = document.getElementById('boCatalogCollapse');
        if (!collapse) return;
        collapse.addEventListener('shown.bs.collapse', async () => {
            try {
                const sfReady = await waitForSyncfusion(3000);
                if (!sfReady) return;
                if (!catalogGrid) initCatalogGrid();
                refreshCatalogGrid();
            } catch (e) {
                console.warn('BackOffice catalog grid init failed', e);
            }
        });
    }

    async function boot() {
        if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();

        const drawerEl = document.getElementById('ticketDrawer');
        if (drawerEl) ticketDrawer = new bootstrap.Offcanvas(drawerEl);
        const livePanelEl = document.getElementById('boLiveStatusPanel');
        if (livePanelEl && typeof bootstrap !== 'undefined') {
            liveStatusCollapse = new bootstrap.Collapse(livePanelEl, { toggle: false });
        }
        const tier3PanelEl = document.getElementById('boTier3Panel');
        if (tier3PanelEl && typeof bootstrap !== 'undefined') {
            tier3Collapse = new bootstrap.Collapse(tier3PanelEl, { toggle: false });
        }
        document.getElementById('boLiveStatusClose')?.addEventListener('click', hideLiveStatusPanel);
        document.getElementById('boTier3Cancel')?.addEventListener('click', hideTier3Panel);
        document.getElementById('boTier3Submit')?.addEventListener('click', submitTier3Escalation);

        wireCatalogLazyInit();
        document.getElementById('boReloadBtn')?.addEventListener('click', () => loadDashboardData(true));
        document.getElementById('boHlrBtn')?.addEventListener('click', hlrResync);
        document.getElementById('btnCbsForceSync')?.addEventListener('click', forceCbsSync);
        document.getElementById('btnNetworkPing')?.addEventListener('click', queryLiveNetworkStatus);
        document.getElementById('btnCoreEscalate')?.addEventListener('click', openTier3EscalationPanel);
        document.getElementById('boResolveOpenBtn')?.addEventListener('click', quickResolve);
        document.getElementById('boSaveStatusBtn')?.addEventListener('click', saveTicketStatus);
        window.addEventListener('resize', () => applyBoTicketsGridHeight());

        const allowed = await ensureAccess();
        if (!allowed) {
            setLoading(false);
            showError(
                'عذراً، لا تملك الصلاحيات الكافية لفتح قمرة العمليات. سجّل الخروج ثم الدخول مجدداً لتحديث الصلاحيات.'
            );
            return;
        }

        await loadDashboardData(true);

        /** Post-execution refresh hook (CBS / automation handshake). */
        window.triggerSearch = () => refreshDashboardQueue();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }
})();
