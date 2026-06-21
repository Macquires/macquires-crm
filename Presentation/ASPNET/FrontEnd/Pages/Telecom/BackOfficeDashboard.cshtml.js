/* Back Office dashboard — plain JS, premium UX */

(function () {
    function hasPermission(key) {
        const perms = StorageManager.getPermissions?.() || [];
        return perms.includes(key);
    }

    function isOperationsManager() {
        // Operations manager has access to everything, usually mapped to admin.audit.view or similar
        return hasPermission('admin.audit.view') || hasPermission('admin.settings.manage');
    }

    function isFinancialSupervisor() {
        return hasPermission('Permission.Finance.Bdr.View') || isOperationsManager();
    }

    function isNetworkAdmin() {
        return hasPermission('Permission.Network.Technical.View') || isOperationsManager();
    }

    function applySecurityUIGovernance() {
        const canViewFin = hasPermission('Permission.Finance.Bdr.View') || isOperationsManager();
        const canViewNet = hasPermission('Permission.Network.Technical.View') || isOperationsManager();
        const canSyncNet = hasPermission('Permission.Network.Technical.Sync') || isOperationsManager();

        // 1. Tab Visibility
        const activeTab = document.getElementById('active-queue-tab');
        const techTab = document.getElementById('tech-ops-tab');

        if (activeTab && !canViewFin) {
            activeTab.closest('li').classList.add('d-none');
            if (currentView === 'active') {
                const nextTab = document.getElementById('tech-ops-tab');
                if (nextTab) nextTab.click();
            }
        }

        if (techTab && !canViewNet) {
            techTab.closest('li').classList.add('d-none');
        }

        // 2. Action Button Visibility in Drawer (Technical Actions)
        const hlrBtn = document.getElementById('boHlrBtn');
        const forceHlrBtn = document.getElementById('boForceHlrBtn');
        const cbsBtn = document.getElementById('btnCbsForceSync');
        const pingBtn = document.getElementById('btnNetworkPing');
        const escBtn = document.getElementById('btnCoreEscalate');

        if (!canSyncNet) {
            [hlrBtn, forceHlrBtn, cbsBtn, pingBtn, escBtn].forEach(btn => btn?.classList.add('d-none', 'security-blocked'));
        }
    }

    const CATEGORY = {
        0: 'Complaint',
        1: 'SimSwap',
        2: 'PackageMigration',
        3: 'OwnershipTransfer',
        4: 'LineActivation',
        5: 'VasActivation',
    };
    const PREVIEW_MAX = 50;

    const ENUM_FALLBACK_EN = {
        issue: { 0: 'Network', 1: 'Billing', 2: 'SIM bar', 3: 'Activation' },
        priority: { 0: 'Low', 1: 'Medium', 2: 'High', 3: 'Critical' },
        status: { 0: 'Open', 1: 'In progress', 2: 'Resolved', 3: 'Escalated' },
        paymentStatus: { 0: 'Draft', 1: 'Gateway', 2: 'Completed', 3: 'Failed', 4: 'Reversed' },
    };
    const ENUM_FALLBACK_AR = {
        issue: { 0: 'شبكة', 1: 'فوترة', 2: 'حظر شريحة', 3: 'تفعيل' },
        priority: { 0: 'منخفض', 1: 'متوسط', 2: 'عالي', 3: 'حرج' },
        status: { 0: 'مفتوحة', 1: 'قيد المعالجة', 2: 'تم الحل', 3: 'مصعّدة' },
        paymentStatus: { 0: 'مسودة', 1: 'بوابة', 2: 'مكتمل', 3: 'فاشل', 4: 'معكوس' },
    };

    const BACK_OFFICE_PERMISSIONS = [
        'bulk.import.upload',
        'bulk.import.monitor',
        'telecom.asset.manage',
        'admin.settings.manage',
        'customer.view',
    ];

    const PENDING_QUEUE_VIEW_PERMISSIONS = [
        'Permission.Finance.Bdr.View',
        'Permission.Finance.Bdr.Execute',
        'Permission.Network.Technical.View',
        ...BACK_OFFICE_PERMISSIONS,
    ];

    const emptyGridHtml = () => `
        <div class="text-center py-4 text-muted">
            <i class="bi bi-inbox display-6 d-block mb-2"></i>
            <span>${escapeHtml(t('backOffice.dashboard.grid.emptyInbox'))}</span>
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
    let currentView = 'active'; // 'active', 'tech', 'historical'
    const HISTORICAL_LEDGER_STATUSES = new Set([3, 4, 8]); // Completed, Failed, Approved_Pending_Cash
    const HISTORICAL_LEDGER_PIPELINE_STATES = new Set(['Completed', 'Failed', 'Approved_Pending_Cash']);
    const scopeState = { regionId: null, branchId: null, regions: [], branches: [], canFilter: false };
    let playbookState = { steps: [], currentIndex: 0, completed: new Set() };
    let lastLiveStatusData = null;
    let lastDrawerRow = null;
    let lastDrawerDetail = null;

    function scopeParams() {
        const p = {};
        if (scopeState.regionId) p.regionId = scopeState.regionId;
        if (scopeState.branchId) p.branchId = scopeState.branchId;
        return p;
    }

    function renderScopeSelects() {
        const regionSel = document.getElementById('boScopeRegion');
        const branchSel = document.getElementById('boScopeBranch');
        if (!regionSel || !branchSel || !scopeState.canFilter) return;

        const savedRegion = regionSel.value || scopeState.regionId || '';
        const savedBranch = branchSel.value || scopeState.branchId || '';
        const allRegions = t('backOffice.dashboard.allRegions', 'All regions');
        const allBranches = t('backOffice.dashboard.allBranches', 'All branches');
        const isEn = (document.documentElement.lang || 'en').toLowerCase().startsWith('en');
        const regionName = (r) => {
            if (isEn) return r.nameEn ?? r.NameEn ?? r.nameAr ?? r.NameAr ?? r.id ?? r.Id;
            return r.nameAr ?? r.NameAr ?? r.nameEn ?? r.NameEn ?? r.id ?? r.Id;
        };
        const branchName = (b) => {
            if (isEn) return b.nameEn ?? b.NameEn ?? b.nameAr ?? b.NameAr ?? b.id ?? b.Id;
            return b.nameAr ?? b.NameAr ?? b.nameEn ?? b.NameEn ?? b.id ?? b.Id;
        };

        regionSel.innerHTML =
            `<option value="">${escapeHtml(allRegions)}</option>` +
            scopeState.regions
                .map((r) => {
                    const id = r.id ?? r.Id;
                    return `<option value="${id}">${escapeHtml(regionName(r))}</option>`;
                })
                .join('');
        if (savedRegion) regionSel.value = savedRegion;

        const rid = regionSel.value || null;
        const list = rid
            ? scopeState.branches.filter((b) => (b.regionId ?? b.RegionId) === rid)
            : scopeState.branches;
        branchSel.innerHTML =
            `<option value="">${escapeHtml(allBranches)}</option>` +
            list
                .map((b) => {
                    const id = b.id ?? b.Id;
                    return `<option value="${id}">${escapeHtml(branchName(b))}</option>`;
                })
                .join('');
        if (savedBranch) branchSel.value = savedBranch;
    }

    async function initScopeFilters() {
        const wrap = document.getElementById('boScopeFilters');
        const regionSel = document.getElementById('boScopeRegion');
        const branchSel = document.getElementById('boScopeBranch');
        const applyBtn = document.getElementById('boScopeApply');
        if (!wrap || !regionSel || !branchSel) return;

        try {
            const res = await AxiosManager.get('/Telecom/GetOperationalAnalyticsScope', {});
            const m = res?.data?.content ?? res?.data?.Content ?? {};
            scopeState.canFilter = m.canUseFilters ?? m.CanUseFilters ?? false;
            scopeState.regions = m.regions ?? m.Regions ?? [];
            scopeState.branches = m.branches ?? m.Branches ?? [];
            if (!scopeState.canFilter) return;

            wrap.classList.remove('d-none');
            renderScopeSelects();

            const fillBranches = () => {
                const rid = regionSel.value || null;
                const savedBranch = branchSel.value || '';
                const allBranches = t('backOffice.dashboard.allBranches', 'All branches');
                const isEn = (document.documentElement.lang || 'en').toLowerCase().startsWith('en');
                const branchName = (b) => {
                    if (isEn) return b.nameEn ?? b.NameEn ?? b.nameAr ?? b.NameAr ?? b.id ?? b.Id;
                    return b.nameAr ?? b.NameAr ?? b.nameEn ?? b.NameEn ?? b.id ?? b.Id;
                };
                const list = rid
                    ? scopeState.branches.filter((b) => (b.regionId ?? b.RegionId) === rid)
                    : scopeState.branches;
                branchSel.innerHTML =
                    `<option value="">${escapeHtml(allBranches)}</option>` +
                    list
                        .map((b) => {
                            const id = b.id ?? b.Id;
                            return `<option value="${id}">${escapeHtml(branchName(b))}</option>`;
                        })
                        .join('');
                if (savedBranch) branchSel.value = savedBranch;
            };
            if (!regionSel.dataset.scopeBound) {
                regionSel.dataset.scopeBound = '1';
                regionSel.addEventListener('change', () => {
                    branchSel.value = '';
                    fillBranches();
                });
            }
            if (!applyBtn?.dataset.scopeBound) {
                applyBtn.dataset.scopeBound = '1';
                applyBtn?.addEventListener('click', async () => {
                    scopeState.regionId = regionSel.value || null;
                    scopeState.branchId = branchSel.value || null;
                    await loadDashboardData(true);
                });
            }
        } catch (e) {
            console.warn('BO scope filters unavailable', e);
        }
    }

    const pickHttpErrorMessage = (e) => {
        const data = e?.response?.data;
        if (e?.response?.status === 403) {
            return t('backOffice.dashboard.messages.forbidden', data?.messageAr || data?.MessageAr || data?.message || data?.Message);
        }
        if (typeof data === 'string') return data;
        if (data?.message) return data.message;
        if (data?.Message) return data.Message;
        if (e?.message) return e.message;
        return null;
    };

    const toastSuccess = (title, html) => {
        if (typeof Swal !== 'undefined') {
            Swal.fire({ icon: 'success', title, html, timer: 2800, showConfirmButton: false });
        }
    };

    const toastError = (e, fallback) => {
        const msg = pickHttpErrorMessage(e) || fallback;
        if (typeof Swal !== 'undefined') {
            Swal.fire({ icon: 'error', title: msg });
        }
    };

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

    const isEnUi = () => document.documentElement.lang?.toLowerCase().startsWith('en');

    const pipelineLabel = (state) => {
        if (!state) return '—';
        return t(`backOffice.dashboard.pipeline.${state}`, state);
    };

    const kindLabel = (row) =>
        isEnUi()
            ? pick(row, 'kindNameEn', 'KindNameEn') || pick(row, 'kindNameAr', 'KindNameAr') || '—'
            : pick(row, 'kindNameAr', 'KindNameAr') || pick(row, 'kindNameEn', 'KindNameEn') || '—';

    const clearanceLabel = (value) => {
        if (!value || value === '—') return '—';
        return t(`backOffice.dashboard.clearance.${value}`, value);
    };

    const accountTypeLabel = (value) => {
        const map = {
            'Postpaid Individual Account': 'backOffice.dashboard.accountTypes.postpaidIndividual',
            'Postpaid Corporate Account': 'backOffice.dashboard.accountTypes.postpaidCorporate',
        };
        return t(map[value] || '', value || t('backOffice.dashboard.accountTypes.postpaidIndividual', 'Postpaid Account'));
    };

    const boIdentityDocumentUrl = (operationId) => {
        if (typeof TelecomWizardConfirm !== 'undefined') {
            return TelecomWizardConfirm.operationIdentityDocumentUrl(operationId);
        }
        const opId = (operationId || '').trim();
        if (!opId) return '';
        const base = (typeof AxiosManager !== 'undefined' && AxiosManager.getBaseUrl
            ? AxiosManager.getBaseUrl()
            : '/api').replace(/\/$/, '');
        const token = typeof StorageManager !== 'undefined' ? StorageManager.getAccessToken?.() || '' : '';
        const q = `id=${encodeURIComponent(opId)}${token ? `&access_token=${encodeURIComponent(token)}` : ''}`;
        return `${base}/Telecom/DownloadTelecomOperationIdentityDocument?${q}`;
    };

    const blockReasonLabel = (row) => {
        const code = pick(row, 'blockReasonCode', 'BlockReasonCode');
        if (code) {
            const hit = t(`backOffice.dashboard.validation.${code}`);
            if (hit) return hit;
        }
        return isEnUi()
            ? pick(row, 'blockReasonEn', 'BlockReasonEn') || pick(row, 'blockReasonAr', 'BlockReasonAr') || ''
            : pick(row, 'blockReasonAr', 'BlockReasonAr') || pick(row, 'blockReasonEn', 'BlockReasonEn') || '';
    };

    const formatTpl = (tpl, vars) => {
        let out = tpl || '';
        Object.entries(vars).forEach(([k, v]) => {
            out = out.replaceAll(`{${k}}`, String(v ?? ''));
        });
        return out;
    };

    const enumFallback = () => (isEnUi() ? ENUM_FALLBACK_EN : ENUM_FALLBACK_AR);

    const enumLabel = (group, n) =>
        t(`backOffice.dashboard.enums.${group}.${n}`, enumFallback()[group]?.[n] ?? '—');

    const getUiLocale = () =>
        document.documentElement.lang?.toLowerCase().startsWith('en') ? 'en-GB' : 'ar-SY';

    const formatDateTime = (d) => {
        try {
            return new Date(d).toLocaleString(getUiLocale(), { dateStyle: 'short', timeStyle: 'medium' });
        } catch {
            return d instanceof Date ? d.toISOString() : String(d ?? '—');
        }
    };

    const formatMoney = (n) => {
        const val = Number(n) || 0;
        const suffix = t('backOffice.dashboard.kpi.currencySyp');
        return `${val.toLocaleString(getUiLocale())} ${suffix}`;
    };

    const showError = (msg) => {
        const el = document.getElementById('bo-boot-error');
        if (!el) return;
        el.textContent = msg || t('backOffice.dashboard.messages.loadError');
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
        el.textContent = formatDateTime(new Date());
    };

    const remapTicketLabels = () => {
        const mapRow = (row) => ({
            ...row,
            issueLabel: enumLabel('issue', Number(row.issueType)),
            priorityLabel: enumLabel('priority', Number(row.priority)),
            statusLabel: enumLabel('status', Number(row.status)),
            categoryLabelHtml: categoryLabelHtml(row.ticketCategory),
        });
        tickets = tickets.map(mapRow);
        ticketPreview = tickets.slice(0, PREVIEW_MAX).map(mapRow);
    };

    const applyPageI18n = () => {
        const title = t('backOffice.title');
        if (title) document.title = title;
        window.TelecomI18n?.refresh?.(document.getElementById('bo-dashboard-page'));
        window.TelecomI18n?.refresh?.(document.getElementById('ticketDrawer'));
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
            createdAtUtc: pick(raw, 'createdAtUtc', 'CreatedAtUtc'),
            updatedAtUtc: pick(raw, 'updatedAtUtc', 'UpdatedAtUtc'),
            issueLabel: enumLabel('issue', Number(pick(raw, 'issueType', 'IssueType'))),
            categoryLabelHtml: categoryLabelHtml(ticketCategory),
            priorityLabel: enumLabel('priority', Number(pick(raw, 'priority', 'Priority'))),
            statusLabel: enumLabel('status', Number(pick(raw, 'status', 'Status'))),
            slaExpirationTimeUtc: pick(raw, 'slaExpirationTimeUtc', 'SlaExpirationTimeUtc'),
            claimedByUserId: pick(raw, 'claimedByUserId', 'ClaimedByUserId'),
            outstandingBalanceSnapshot: pick(raw, 'outstandingBalanceSnapshot', 'OutstandingBalanceSnapshot'),
            writeOffAmount: pick(raw, 'writeOffAmount', 'WriteOffAmount'),
            collectedAmount: pick(raw, 'collectedAmount', 'CollectedAmount'),
            accountType: pick(raw, 'accountType', 'AccountType'),
            blockReasonAr: pick(raw, 'blockReasonAr', 'BlockReasonAr'),
            paymentReferenceValidated: pick(raw, 'paymentReferenceValidated', 'PaymentReferenceValidated'),
        };
    };

    const categoryNeedsHlr = (cat) => {
        const c = String(cat || '').toLowerCase();
        return c === 'simswap' || c === 'lineactivation' || cat === 1 || cat === 4;
    };

    const categoryNeedsForceHlr = (cat) => {
        const c = String(cat || '').toLowerCase();
        return c === 'lineactivation' || cat === 4;
    };

    const categoryNeedsCbs = (cat) => {
        const c = String(cat || '').toLowerCase();
        return c === 'packagemigration' || c === 'complaint' || c === 'vasactivation' || cat === 0 || cat === 2 || cat === 5;
    };

    function playbookActionIds(detail, row) {
        return new Set(
            buildPlaybookSteps(detail, row)
                .filter((s) => s.kind === 'action' && s.actionId)
                .map((s) => s.actionId)
        );
    }

    function updateDrawerToolbar(category, detail, row) {
        const hlrBtn = document.getElementById('boHlrBtn');
        const forceHlrBtn = document.getElementById('boForceHlrBtn');
        const cbsBtn = document.getElementById('btnCbsForceSync');
        const pingBtn = document.getElementById('btnNetworkPing');
        const escBtn = document.getElementById('btnCoreEscalate');
        const actions = detail || row ? playbookActionIds(detail ?? row, row ?? detail) : null;

        if (actions) {
            if (hlrBtn) hlrBtn.classList.toggle('d-none', !actions.has('boHlrBtn'));
            if (forceHlrBtn) forceHlrBtn.classList.toggle('d-none', !actions.has('boForceHlrBtn'));
            if (cbsBtn) cbsBtn.classList.toggle('d-none', !actions.has('btnCbsForceSync'));
        } else {
            if (hlrBtn) hlrBtn.classList.toggle('d-none', !categoryNeedsHlr(category));
            if (forceHlrBtn) forceHlrBtn.classList.toggle('d-none', !categoryNeedsForceHlr(category));
            if (cbsBtn) cbsBtn.classList.toggle('d-none', !categoryNeedsCbs(category));
        }
        if (pingBtn) pingBtn.classList.remove('d-none');
        if (escBtn) escBtn.classList.remove('d-none');
    }

    function playbookText(key) {
        return t(`backOffice.dashboard.playbook.${key}`, key);
    }

    function liveStatusLabel(key) {
        return t(`backOffice.dashboard.liveStatus.${key}`, key);
    }

    function yesNoLabel(value) {
        return value ? liveStatusLabel('yes') : liveStatusLabel('no');
    }

    function detectNotProvisioned(detail, row) {
        const blob = [
            pick(detail, 'notes', 'Notes'),
            pick(detail, 'payloadJson', 'PayloadJson'),
            row?.notes,
            row?.payloadJson,
        ]
            .filter(Boolean)
            .join(' ')
            .toUpperCase();
        return /NOT[_\s-]?PROVISIONED|REPROVISION|إعادة تهيئة HLR/.test(blob);
    }

    function buildPlaybookSteps(detail, row) {
        const issue = Number(pick(detail, 'issueType', 'IssueType') ?? row?.issueType ?? 0);
        const status = Number(pick(detail, 'status', 'Status') ?? row?.status ?? 0);
        const category = resolveCategoryKey(detail ?? row);
        const notProv = detectNotProvisioned(detail, row);

        const steps = [{ id: 'review', kind: 'info' }];
        if (status === 3) steps.push({ id: 'tier3Context', kind: 'info' });
        steps.push({ id: 'ping', kind: 'action', actionId: 'btnNetworkPing' });

        if (issue === 1 || (categoryNeedsCbs(category) && !notProv && !categoryNeedsForceHlr(category))) {
            steps.push({ id: 'cbs', kind: 'action', actionId: 'btnCbsForceSync' });
        } else if (notProv || categoryNeedsForceHlr(category)) {
            steps.push({ id: 'forceHlr', kind: 'action', actionId: 'boForceHlrBtn' });
        } else if (issue === 0 || issue === 2 || categoryNeedsHlr(category)) {
            steps.push({ id: 'hlr', kind: 'action', actionId: 'boHlrBtn' });
        }

        steps.push({ id: 'resolve', kind: 'action', actionId: 'boSaveStatusBtn' });
        return steps;
    }

    function getPlaybookIntro(detail, row) {
        const issue = Number(pick(detail, 'issueType', 'IssueType') ?? row?.issueType ?? 0);
        const status = Number(pick(detail, 'status', 'Status') ?? row?.status ?? 0);
        if (status === 3) return playbookText('introEscalated');
        if (detectNotProvisioned(detail, row)) return playbookText('introNotProvisioned');
        if (issue === 1) return playbookText('introBilling');
        if (issue === 0) return playbookText('introNetwork');
        if (issue === 2) return playbookText('introSim');
        return playbookText('introDefault');
    }

    function stepLabel(step) {
        return playbookText(`steps.${step.id}`, step.id);
    }

    function clearPlaybookHighlights() {
        document.querySelectorAll('.bo-playbook-action-target').forEach((el) => {
            el.classList.remove('bo-playbook-action-target');
        });
    }

    function highlightPlaybookAction(actionId) {
        clearPlaybookHighlights();
        if (!actionId) return;
        const btn = document.getElementById(actionId);
        btn?.classList.add('bo-playbook-action-target');
        btn?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
    }

    function findPlaybookStepIndex(stepId) {
        return playbookState.steps.findIndex((s) => s.id === stepId);
    }

    function refreshPlaybookStepClasses() {
        const stepsEl = document.getElementById('boPlaybookSteps');
        if (!stepsEl) return;
        stepsEl.querySelectorAll('li').forEach((li, i) => {
            const sid = li.getAttribute('data-playbook-step');
            const done = playbookState.completed.has(sid);
            li.classList.toggle('bo-step-done', done);
            li.classList.toggle('bo-step-current', i === playbookState.currentIndex && !done);
        });
    }

    function updatePlaybookHint() {
        const hintEl = document.getElementById('boPlaybookHint');
        if (!hintEl || !playbookState.steps.length) return;

        if (playbookState.currentIndex >= playbookState.steps.length) {
            hintEl.innerHTML = `<span class="text-success"><i class="bi bi-check-circle me-1"></i>${escapeHtml(
                playbookText('allDone')
            )}</span>`;
            clearPlaybookHighlights();
            return;
        }

        const current = playbookState.steps[playbookState.currentIndex];
        if (!current || current.kind === 'info') return;

        const hintKey = `hints.${current.id}`;
        const hintIcon = isEnUi() ? 'bi-arrow-right-circle' : 'bi-arrow-left-circle';
        hintEl.innerHTML = `<i class="bi ${hintIcon} me-1"></i>${escapeHtml(
            playbookText(hintKey) || stepLabel(current)
        )}`;
        highlightPlaybookAction(current.actionId);
    }

    function advancePlaybookStep(stepId) {
        if (!playbookState.steps.length) return;
        const idx = findPlaybookStepIndex(stepId);
        if (idx < 0) return;
        playbookState.completed.add(stepId);

        let nextIdx = idx + 1;
        while (nextIdx < playbookState.steps.length && playbookState.steps[nextIdx].kind === 'info') {
            playbookState.completed.add(playbookState.steps[nextIdx].id);
            nextIdx++;
        }
        playbookState.currentIndex = nextIdx;
        refreshPlaybookStepClasses();
        updatePlaybookHint();
    }

    function renderTicketPlaybook(row, detail) {
        const panel = document.getElementById('boPlaybookPanel');
        const introEl = document.getElementById('boPlaybookIntro');
        const stepsEl = document.getElementById('boPlaybookSteps');
        if (!panel || !stepsEl) return;

        playbookState.steps = buildPlaybookSteps(detail, row);
        playbookState.completed = new Set();
        playbookState.steps.forEach((step) => {
            if (step.kind === 'info') playbookState.completed.add(step.id);
        });

        playbookState.currentIndex = playbookState.steps.findIndex(
            (s) => s.kind === 'action' && !playbookState.completed.has(s.id)
        );
        if (playbookState.currentIndex < 0) playbookState.currentIndex = playbookState.steps.length;

        panel.classList.remove('d-none');
        if (introEl) introEl.textContent = getPlaybookIntro(detail, row);

        stepsEl.innerHTML = playbookState.steps
            .map((step, idx) => {
                const done = playbookState.completed.has(step.id);
                const current = idx === playbookState.currentIndex && !done;
                const cls = [done ? 'bo-step-done' : '', current ? 'bo-step-current' : '']
                    .filter(Boolean)
                    .join(' ');
                return `<li class="${cls}" data-playbook-step="${step.id}" data-step-idx="${idx}">${escapeHtml(
                    stepLabel(step)
                )}</li>`;
            })
            .join('');

        updatePlaybookHint();
    }

    function hideTicketPlaybook() {
        clearPlaybookHighlights();
        document.getElementById('boPlaybookPanel')?.classList.add('d-none');
        playbookState = { steps: [], currentIndex: 0, completed: new Set() };
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
        const userPerms = StorageManager.getPermissions?.() || [];
        if (StorageManager.hasAnyPermission(userPerms, BACK_OFFICE_PERMISSIONS)) return true;
        if (typeof SecurityManager !== 'undefined' && typeof SecurityManager.canAccessTelecom === 'function') {
            return SecurityManager.canAccessTelecom({ permissions: BACK_OFFICE_PERMISSIONS });
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
        const escalated = tickets.filter((x) => Number(x.status) === 3).length;
        const critical = tickets.filter(
            (x) =>
                Number(x.priority) === 3
                && [0, 1, 3].includes(Number(x.status))
        ).length;
        return { open, inProgress, escalated, critical, active: open + inProgress + escalated };
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
                const msg = t('backOffice.dashboard.criticalAlert').replace('{count}', String(stats.critical));
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

    function sortTicketsForQueue(rows) {
        return rows.slice().sort((a, b) => {
            const sa = Number(a.status);
            const sb = Number(b.status);
            if (sa === 3 && sb !== 3) return -1;
            if (sb === 3 && sa !== 3) return 1;
            const pa = Number(a.priority);
            const pb = Number(b.priority);
            if (pa === 3 && pb !== 3) return -1;
            if (pb === 3 && pa !== 3) return 1;
            const ta = new Date(a.updatedAtUtc || a.createdAtUtc || 0).getTime();
            const tb = new Date(b.updatedAtUtc || b.createdAtUtc || 0).getTime();
            return tb - ta;
        });
    }

    function updateEscalatedBanner() {
        const banner = document.getElementById('boEscalatedBanner');
        const text = document.getElementById('boEscalatedBannerText');
        if (!banner || !text) return;
        const escalated = tickets.filter((x) => Number(x.status) === 3);
        if (escalated.length === 0) {
            banner.classList.add('d-none');
            return;
        }
        const tpl = t(
            'backOffice.dashboard.escalatedAlert',
            '{count} escalated Tier-3 ticket(s) need engineering review — shown first in the queue below.'
        );
        text.textContent = tpl.replace('{count}', String(escalated.length));
        banner.classList.remove('d-none');
    }

    async function loadTickets() {
        const res = await AxiosManager.get('/TelecomBackOffice/GetTechnicalTickets', {
            params: { activeQueueOnly: currentView !== 'historical' },
        });
        const content = res?.data?.content ?? res?.data?.Content;
        tickets = sortTicketsForQueue(parseTicketList(res).map(normalizeTicket));
        ticketPreview = tickets.slice(0, PREVIEW_MAX);
        const activeCount = content?.activeOpenCount ?? content?.ActiveOpenCount;
        updateKpis(activeCount);
        updateEscalatedBanner();
        updateEmptyState();
        if (ticketsGrid) {
            ticketsGrid.dataSource = ticketPreview.slice();
            ticketsGrid.refresh();
        }
        
        const techCounter = document.getElementById('boTechCounter');
        if (techCounter) techCounter.textContent = String(activeCount);
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
            const treatLabel = escapeHtml(t('backOffice.dashboard.grid.treat'));
            args.cell.innerHTML = `<button type="button" class="btn btn-sm btn-danger bo-open-ticket" data-id="${id}">${treatLabel}</button>`;
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

    function getBoTicketColumns() {
        return [
            { field: 'id', isPrimaryKey: true, visible: false },
            { field: 'ticketNumber', headerText: t('backOffice.dashboard.grid.ticketNumber'), width: 115 },
            { field: 'customerDisplayName', headerText: t('backOffice.dashboard.grid.subscriber'), width: 130 },
            { field: 'msisdn', headerText: t('backOffice.dashboard.grid.msisdn'), width: 108 },
            {
                field: 'categoryLabelHtml',
                headerText: t('backOffice.dashboard.grid.category'),
                width: 130,
                allowFiltering: false,
            },
            { field: 'issueLabel', headerText: t('backOffice.dashboard.grid.issue'), width: 88 },
            { field: 'statusLabel', headerText: t('backOffice.dashboard.grid.status'), width: 118, allowFiltering: false },
            { field: 'priorityLabel', headerText: t('backOffice.dashboard.grid.priority'), width: 105, allowFiltering: false },
            { field: 'createdByChannel', headerText: t('backOffice.dashboard.grid.source'), width: 140, allowFiltering: false },
            { field: 'notes', headerText: t('backOffice.dashboard.grid.complaint'), width: 200, minWidth: 120 },
            {
                field: '_boAction',
                headerText: t('backOffice.dashboard.grid.action'),
                width: 95,
                allowSorting: false,
                allowFiltering: false,
            },
        ];
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
            emptyRecordTemplate: emptyGridHtml(),
            recordDoubleClick: (args) => openTicket(normalizeTicket(args.rowData)),
            rowDataBound: applyRowHighlight,
            queryCellInfo: paintTicketGridCells,
            columns: getBoTicketColumns(),
            dataBound: () => bindTicketActionButtons(),
        });
        ticketsGrid.appendTo(ticketsHost);
        applyBoTicketsGridHeight();
        return true;
    }

    function refreshTicketsGridI18n() {
        if (!ticketsGrid) return;
        ticketsGrid.columns = getBoTicketColumns();
        ticketsGrid.emptyRecordTemplate = emptyGridHtml();
        refreshTicketsGrid();
    }

    function catalogToggleTemplate() {
        const active = t('backOffice.dashboard.grid.active');
        const inactive = t('backOffice.dashboard.grid.inactive');
        return `<button type="button" class="btn btn-sm \${isActive ? "btn-success" : "btn-outline-secondary"} bo-toggle-offer" data-id="\${id}">\${isActive ? "${active}" : "${inactive}"}</button>`;
    }

    function getCatalogColumns() {
        return [
            { field: 'name', headerText: t('backOffice.dashboard.grid.offer'), width: 200 },
            { field: 'code', headerText: t('backOffice.dashboard.grid.code'), width: 100 },
            { field: 'defaultPrice', headerText: t('backOffice.dashboard.grid.price'), width: 90, format: 'N0' },
            {
                headerText: t('backOffice.dashboard.grid.catalogStatus'),
                width: 100,
                template: catalogToggleTemplate(),
            },
        ];
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
            columns: getCatalogColumns(),
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
        lastDrawerRow = row;
        lastDrawerDetail = detail;
        window.TelecomI18n?.applyDomI18n?.(document.getElementById('ticketDrawer'));

        const status = pick(detail, 'status', 'Status') ?? row.status ?? 0;
        document.getElementById('boDrawerTitle').textContent = pick(detail, 'ticketNumber', 'TicketNumber') || row.ticketNumber || '—';
        document.getElementById('boDrawerMsisdn').textContent = pick(detail, 'msisdn', 'Msisdn') || row.msisdn || '—';
        document.getElementById('boDrawerCustomer').textContent = row.customerDisplayName || '—';
        document.getElementById('boDrawerIssue').textContent =
            enumLabel('issue', Number(pick(detail, 'issueType', 'IssueType') ?? row.issueType));

        const categoryKey = resolveCategoryKey(detail ?? row);
        const catHost = document.getElementById('boDrawerCategory');
        if (catHost) catHost.innerHTML = categoryLabelHtml(categoryKey);
        updateDrawerToolbar(categoryKey, detail, row);

        const priEl = document.getElementById('boDrawerPriority');
        if (priEl) {
            const pri = Number(pick(detail, 'priority', 'Priority') ?? row.priority);
            priEl.innerHTML =
                window.TelecomUiBadges?.ticketPriority(pri, enumLabel('priority', pri)) || enumLabel('priority', pri);
        }

        const badgeHost = document.getElementById('boDrawerStatusBadge');
        if (badgeHost) {
            badgeHost.innerHTML =
                window.TelecomUiBadges?.ticketStatus(status, enumLabel('status', Number(status))) ||
                enumLabel('status', Number(status));
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
        renderTicketPlaybook(row, detail);
    }

    async function openTicket(row) {
        try {
            const res = await AxiosManager.get('/TelecomBackOffice/GetTechnicalTicketSingle', {
                params: { id: row.id },
            });
            const d = StorageManager.apiContent(res);
            if (!d) {
                Swal.fire({
                    icon: 'warning',
                    title: t('backOffice.dashboard.messages.ticketDetailFailed'),
                });
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
            Swal.fire({
                icon: 'error',
                title: e?.response?.data?.message || t('backOffice.dashboard.messages.ticketOpenFailed'),
            });
        }
    }

    const TELECOM_ACTION_IDS = [
        'boSaveStatusBtn',
        'boResolveOpenBtn',
        'boHlrBtn',
        'boForceHlrBtn',
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
        // Also handle dynamic queue buttons
        document.querySelectorAll('.bo-approve-op, .bo-reject-op').forEach((el) => {
            el.disabled = on;
            el.classList.toggle('bo-action-busy', on);
        });
    }

    function hideLiveStatusPanel() {
        lastLiveStatusData = null;
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
        lastLiveStatusData = data;
        const lines = [
            `${liveStatusLabel('msisdn')}: ${pick(data, 'msisdn', 'Msisdn') || '—'}`,
            `${liveStatusLabel('cbs')}: ${pick(data, 'cbsBalanceLabel', 'CbsBalanceLabel') || pick(data, 'cbsBalance', 'CbsBalance')}`,
            `${liveStatusLabel('hlrState')}: ${pick(data, 'hlrSubscriberState', 'HlrSubscriberState') || '—'}`,
            `${liveStatusLabel('hlrOnline')}: ${yesNoLabel(!!pick(data, 'hlrIsOnline', 'HlrIsOnline'))}`,
            `${liveStatusLabel('hlrSite')}: ${pick(data, 'hlrLocation', 'HlrLocation') || '—'}`,
            `${liveStatusLabel('crmStatus')}: ${pick(data, 'crmOperationalStatus', 'CrmOperationalStatus') || '—'}`,
            `${liveStatusLabel('package')}: ${pick(data, 'productOfferingName', 'ProductOfferingName') || '—'}`,
            `${liveStatusLabel('crmHlrDiff')}: ${yesNoLabel(!!pick(data, 'differsFromCrm', 'DiffersFromCrm'))}`,
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
        hideTicketPlaybook();
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

    function queueRemovedSuffix() {
        return t('backOffice.dashboard.queueRemovedSuffix');
    }

    function queueOutcomeSuffix(body) {
        const resolved = pick(body, 'ticketAutoResolved', 'TicketAutoResolved');
        if (resolved === true || resolved === 'true') {
            return queueRemovedSuffix();
        }
        const moved = pick(body, 'ticketMovedToInProgress', 'TicketMovedToInProgress');
        if (moved === true || moved === 'true') {
            return t('backOffice.dashboard.movedInProgressSuffix');
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
        const title = message || t('backOffice.dashboard.actionOk');
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
            const defaultOk = t('backOffice.hlrOk');
            advancePlaybookStep('hlr');
            await finalizeTelecomActionSuccess(pickActionMessage(res, defaultOk) + queueOutcomeSuffix(body));
        }).catch((e) => showTelecomActionError(e, t('backOffice.dashboard.messages.hlrFailed')));
    }

    async function forceHlrSync() {
        await withTelecomAction(async () => {
            const res = await AxiosManager.post('/TelecomBackOffice/ForceHlrSync', {
                ticketId: selectedTicket.id,
            });
            const body = StorageManager.apiContent(res);
            const defaultOk = t('backOffice.dashboard.forceHlrOk');
            advancePlaybookStep('forceHlr');
            await finalizeTelecomActionSuccess(pickActionMessage(res, defaultOk) + queueOutcomeSuffix(body));
        }).catch((e) => showTelecomActionError(e, t('backOffice.dashboard.messages.hlrFailed')));
    }

    async function forceCbsSync() {
        await withTelecomAction(async () => {
            const res = await AxiosManager.post('/TelecomBackOffice/ForceCbsSync', {
                ticketId: selectedTicket.id,
            });
            const body = StorageManager.apiContent(res);
            const defaultOk = t('backOffice.dashboard.cbsForceOk');
            advancePlaybookStep('cbs');
            await finalizeTelecomActionSuccess(pickActionMessage(res, defaultOk) + queueOutcomeSuffix(body));
        }).catch((e) =>
            showTelecomActionError(e, t('backOffice.dashboard.messages.cbsFailed'))
        );
    }

    async function queryLiveNetworkStatus() {
        await withTelecomAction(async () => {
            const res = await AxiosManager.get('/TelecomBackOffice/QueryLiveNetworkStatus', {
                params: { ticketId: selectedTicket.id },
            });
            const content = StorageManager.apiContent(res);
            const data = content?.data ?? content?.Data ?? content;
            if (!data || typeof data !== 'object') {
                Swal.fire({
                    icon: 'warning',
                    title: t('backOffice.dashboard.messages.noLiveData'),
                });
                return;
            }
            renderLiveStatus(data);
            advancePlaybookStep('ping');
            await finalizeTelecomActionInDrawer(
                t('backOffice.dashboard.networkPingOk')
            );
            await refreshDashboardQueue();
        }).catch((e) => showTelecomActionError(e, t('backOffice.dashboard.messages.networkFailed')));
    }

    function openTier3EscalationPanel() {
        if (!selectedTicket) return;
        showTier3Panel();
    }

    async function submitTier3Escalation() {
        if (!selectedTicket) return;
        const notes = document.getElementById('boTier3Notes')?.value?.trim() || '';
        if (notes.length < 10) {
            Swal.fire({
                icon: 'warning',
                title: t('backOffice.dashboard.messages.tier3NotesMin'),
            });
            document.getElementById('boTier3Notes')?.focus();
            return;
        }

        await withTelecomAction(async () => {
            await AxiosManager.post('/TelecomBackOffice/EscalateToTier3', {
                ticketId: selectedTicket.id,
                escalationNotes: notes,
            });
            await finalizeTelecomActionSuccess(
                t('backOffice.dashboard.tier3Ok') + queueRemovedSuffix()
            );
        }).catch((e) => showTelecomActionError(e, t('backOffice.dashboard.messages.escalationFailed')));
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
            Swal.fire({
                icon: 'warning',
                title: t('backOffice.dashboard.messages.resolutionNotesMin'),
            });
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
            let msg = t('backOffice.dashboard.statusUpdated');
            if (newStatus === 2) {
                advancePlaybookStep('resolve');
                msg = t('backOffice.dashboard.resolvedQueue') + queueRemovedSuffix();
            } else if (newStatus === 3) {
                msg = t('backOffice.dashboard.escalatedQueue') + queueRemovedSuffix();
            } else if (newStatus === 1) {
                msg = t('backOffice.dashboard.inProgressQueue');
            }
            await finalizeTelecomActionSuccess(msg);
        }).catch((e) =>
            showTelecomActionError(e, t('backOffice.dashboard.messages.statusUpdateFailed'))
        );
    }

    function quickResolve() {
        const statusSel = document.getElementById('boTicketStatus');
        const notesEl = document.getElementById('boOperatorNotes');
        if (statusSel) statusSel.value = '2';
        if (notesEl) {
            notesEl.focus();
            if (!notesEl.value.trim()) {
                notesEl.placeholder = t('backOffice.dashboard.resolutionPlaceholder');
            }
        }
        Swal.fire({
            icon: 'info',
            title: t('backOffice.dashboard.quickResolveTitle'),
            text: t('backOffice.dashboard.quickResolveText'),
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
            Swal.fire({
                icon: 'error',
                title:
                    e?.response?.data?.message ||
                    t('backOffice.dashboard.messages.offeringToggleFailed'),
            });
        }
    }

    function canReversePayment() {
        const roles = StorageManager.getUserRoles?.() || [];
        return roles.some((r) => ['TelecomManagement', 'TelecomBackOffice', 'TelecomAdmin'].includes(r));
    }

    const paymentStatusLabel = (s) => enumLabel('paymentStatus', Number(s));

    async function loadPaymentServicesPanel() {
        const tbody = document.getElementById('boPaymentTxBody');
        try {
            const kpiRes = await AxiosManager.get('/Telecom/GetPaymentServicesKpis', { params: scopeParams() });
            const k = kpiRes?.data?.content ?? kpiRes?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set(
                'payKpiAmount',
                formatMoney(pick(k, 'totalRechargedAmountToday', 'TotalRechargedAmountToday') ?? 0)
            );
            set('payKpiCompleted', pick(k, 'completedCountToday', 'CompletedCountToday'));
            set('payKpiFailed', pick(k, 'failedCountToday', 'FailedCountToday'));
            set('payKpiFailRate');
            set('payKpiSla');
            set('payKpiReversed', pick(k, 'reversedCountToday', 'ReversedCountToday'));
            const refreshEl = document.getElementById('boPaymentKpiRefresh');
            if (refreshEl) refreshEl.textContent = formatDateTime(new Date());

            const listRes = await AxiosManager.get('/Telecom/GetPaymentTransactionList?take=25', {});
            const items = listRes?.data?.content?.items ?? listRes?.data?.Content?.Items ?? [];
            if (!tbody) return;
            if (!items.length) {
                tbody.innerHTML = `<tr><td colspan="5" class="text-muted text-center">${escapeHtml(
                    t('backOffice.dashboard.kpi.noTransactions')
                )}</td></tr>`;
                return;
            }
            tbody.innerHTML = items
                .map((row) => {
                    const id = row.id ?? row.Id;
                    const canRev = row.canReverse ?? row.CanReverse;
                    const btn =
                        canRev && canReversePayment()
                            ? `<button type="button" class="btn btn-outline-danger btn-sm py-0 btn-pay-reverse" data-id="${id}">${escapeHtml(
                                  t('backOffice.dashboard.kpi.reverse')
                              )}</button>`
                            : '';
                    return `<tr>
                        <td class="font-monospace small">${row.number ?? row.Number}</td>
                        <td dir="ltr" class="small">${row.msisdn ?? row.Msisdn ?? '—'}</td>
                        <td>${formatMoney(row.amount ?? row.Amount ?? 0)}</td>
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
            if (tbody) {
                tbody.innerHTML = `<tr><td colspan="5" class="text-danger text-center">${escapeHtml(
                    t('backOffice.dashboard.kpi.loadFailed')
                )}</td></tr>`;
            }
        }
    }

    async function reversePayment(paymentId) {
        if (!paymentId) return;
        const { value: reason } = await Swal.fire({
            title: t('backOffice.dashboard.messages.reverseTitle'),
            input: 'text',
            inputPlaceholder: t('backOffice.dashboard.messages.reversePlaceholder'),
            showCancelButton: true,
            confirmButtonText: t('backOffice.dashboard.messages.reverseConfirm'),
            confirmButtonColor: '#c8102e',
            inputValidator: (v) =>
                !v || !String(v).trim()
                    ? t('backOffice.dashboard.messages.reasonRequired')
                    : undefined,
        });
        if (!reason) return;
        try {
            const res = await AxiosManager.post('/Telecom/ReversePaymentTransaction', {
                paymentId,
                reasonCode: String(reason).trim(),
            });
            const body = res?.data?.content ?? res?.data?.Content;
            await loadPaymentServicesPanel();
            Swal.fire({
                icon: 'success',
                title:
                    body?.messageAr ||
                    body?.MessageAr ||
                    t('backOffice.dashboard.messages.reverseDone'),
            });
        } catch (e) {
            Swal.fire({
                icon: 'error',
                title:
                    e?.response?.data?.message ||
                    e?.message ||
                    t('backOffice.dashboard.messages.reverseFailed'),
            });
        }
    }

    async function loadDeviceSaleKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetDeviceSaleKpis', { params: scopeParams() });
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('devKpiVolume', pick(c, 'totalVolume', 'TotalVolume'));
            set('devKpiCompleted', pick(c, 'completedCount', 'CompletedCount'));
            set('devKpiFailed', pick(c, 'failedCount', 'FailedCount'));
            set('devKpiCompletion');
            set('devKpiFallout');
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
            const res = await AxiosManager.get('/Telecom/GetBadDebtKpis', { params: scopeParams() });
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
            set('bdrKpiCollected', col != null && col !== '' ? formatMoney(col) : '—');
            set('bdrKpiWriteOff', wo != null && wo !== '' ? formatMoney(wo) : '—');
            set('bdrKpiPlans', pick(c, 'paymentPlansToday', 'PaymentPlansToday'));
            set('bdrKpiFailRate');
        } catch (e) {
            console.warn('BadDebt KPIs', e);
        }
    }

    async function loadRefundKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetRefundKpis', { params: scopeParams() });
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
            set('rfdKpiSettledAmount', amt != null && amt !== '' ? formatMoney(amt) : '—');
            set('rfdKpiDual', pick(c, 'dualApprovalToday', 'DualApprovalToday'));
            set('rfdKpiFailRate');
            set('rfdKpiSla');
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

    function isHistoricalLedgerRow(row) {
        const status = Number(pick(row, 'status', 'Status'));
        if (!Number.isNaN(status) && HISTORICAL_LEDGER_STATUSES.has(status)) return true;
        const pipeline = pick(row, 'pipelineState', 'PipelineState') || '';
        return HISTORICAL_LEDGER_PIPELINE_STATES.has(pipeline);
    }

    function filterHistoricalLedgerRows(rows) {
        return (rows || []).filter(isHistoricalLedgerRow);
    }

    function findTelecomQueueRow(operationId) {
        return document.querySelector(`.bo-queue-row[onclick*="${operationId}"]`)
            || document.querySelector(`button[onclick*="${operationId}"]`)?.closest('tr');
    }

    function fadeTelecomQueueRow(operationId) {
        const row = findTelecomQueueRow(operationId);
        if (!row) return;
        row.classList.add('bo-row-fade-out');
        const details = row.nextElementSibling;
        if (details?.classList.contains('collapse-row')) {
            details.classList.add('bo-row-fade-out');
        }
    }

    function removeFromActiveTelecomQueue(operationId) {
        const before = pendingTelecomOps.length;
        pendingTelecomOps = pendingTelecomOps.filter((row) => {
            const id = pick(row, 'id', 'Id');
            return id !== operationId;
        });
        if (pendingTelecomOps.length !== before) {
            renderTelecomQueue();
            if (currentView === 'active') {
                updateTabCounters(pendingTelecomOps.length);
            }
        }
    }

    function extractBackOfficeActionMessage(res, fallback) {
        const content = StorageManager.apiContent(res) ?? {};
        return pick(content, 'messageAr', 'MessageAr')
            || pick(content?.confirmResult ?? content?.ConfirmResult ?? {}, 'userMessageAr', 'UserMessageAr')
            || pick(content, 'message', 'Message')
            || fallback;
    }

    async function finalizeTelecomQueueAction(operationId, res, fallbackMessage) {
        fadeTelecomQueueRow(operationId);
        removeFromActiveTelecomQueue(operationId);
        toastSuccess(extractBackOfficeActionMessage(res, fallbackMessage));
        await loadPendingTelecomRequests();
        loadSuspensionKpis();
        loadReconnectKpis();
        loadBadDebtKpis();
    }

    let pendingTelecomOps = [];

    const updateSlaTimers = () => {
        const timers = document.querySelectorAll('.sla-timer');
        const now = new Date().getTime();
        timers.forEach(el => {
            const expiry = new Date(el.dataset.expiry).getTime();
            const diff = expiry - now;
            const row = el.closest('tr');
            
            if (diff <= 0) {
                el.textContent = t('backOffice.dashboard.telecomQueue.slaBreached', 'SLA breached');
                el.className = 'sla-timer badge bo-sla-breached';
                if (row) row.classList.add('table-danger');
            } else {
                const totalSeconds = Math.floor(diff / 1000);
                const mins = Math.floor(totalSeconds / 60);
                const secs = totalSeconds % 60;
                const timeStr = `${mins}:${secs.toString().padStart(2, '0')}`;
                el.textContent = formatTpl(
                    t('backOffice.dashboard.telecomQueue.slaLeft', '{time} left'),
                    { time: timeStr }
                );
                
                if (totalSeconds < 60) {
                    el.className = 'sla-timer badge bo-sla-breached';
                    if (row) row.classList.add('table-danger');
                } else {
                    el.className = 'sla-timer badge bo-sla-healthy';
                    if (row) row.classList.remove('table-danger');
                }
            }
        });
    };

    setInterval(updateSlaTimers, 1000);

    const renderTelecomQueue = () => {
        const canExecuteFin = hasPermission('Permission.Finance.Bdr.Execute') || isOperationsManager();
        const isOps = isOperationsManager();
        
        const body = document.getElementById('boTelecomQueueBody');
        const countEl = document.getElementById('boTelecomQueueCount');
        if (countEl) countEl.textContent = String(pendingTelecomOps.length);
        if (!body) return;

        if (!pendingTelecomOps.length) {
            body.innerHTML = `<tr><td colspan="8" class="text-muted text-center py-3">${escapeHtml(t('backOffice.dashboard.telecomQueue.empty'))}</td></tr>`;
            return;
        }

        body.innerHTML = pendingTelecomOps
            .map((row) => {
                const id = pick(row, 'id', 'Id');
                const number = pick(row, 'number', 'Number') || id;
                const kindName = kindLabel(row);
                const msisdn = pick(row, 'msisdn', 'Msisdn') || '—';
                const clearanceRaw = pick(row, 'clearanceType', 'ClearanceType') || pick(row, 'suspensionType', 'SuspensionType') || '—';
                const clearance = clearanceLabel(clearanceRaw);
                const paymentRef = pick(row, 'paymentReference', 'PaymentReference') || '—';
                const paymentOk = !!(pick(row, 'paymentReferenceValidated', 'PaymentReferenceValidated'));
                const pipelineRaw = pick(row, 'pipelineState', 'PipelineState') || 'Pending_BackOffice_Approval';
                const pipeline = pipelineLabel(pipelineRaw);
                const canApprove = !!(pick(row, 'canApprove', 'CanApprove'));
                const blockReason = blockReasonLabel(row);
                const slaExpiry = pick(row, 'slaExpirationTimeUtc', 'SlaExpirationTimeUtc');
                const isBdr = row.kind === 12 || row.Kind === 12;
                const isPaidBypass = row.status === 9 || row.Status === 9;
                const isFinalState = row.status === 3 || row.status === 4 || row.status === 8; // Completed, Failed, Approved_Pending_Cash

                let slaHtml = '—';
                if (slaExpiry && !isFinalState) {
                    slaHtml = `<span class="sla-timer font-monospace" data-expiry="${slaExpiry}">...</span>`;
                } else if (isFinalState) {
                    slaHtml = `<span class="badge bg-light text-muted border">${escapeHtml(t('backOffice.dashboard.telecomQueue.archived', 'Archived'))}</span>`;
                }

                const pipelineBadge = isPaidBypass 
                    ? `<span class="badge bg-warning-subtle text-danger border border-danger-subtle"><i class="bi bi-cash-stack me-1"></i>${escapeHtml(t('backOffice.dashboard.telecomQueue.paidPendingAudit', 'Paid: pending audit'))}</span>`
                    : isFinalState 
                        ? (row.status === 3 || row.status === 8 
                            ? `<span class="badge bg-success text-white"><i class="bi bi-check-circle me-1"></i>${escapeHtml(t('backOffice.dashboard.telecomQueue.approvedBadge', 'Approved'))}</span>`
                            : `<span class="badge bg-danger text-white"><i class="bi bi-x-circle me-1"></i>${escapeHtml(t('backOffice.dashboard.telecomQueue.rejectedBadge', 'Rejected'))}</span>`)
                        : `<span class="badge bg-danger-subtle text-danger border border-danger-subtle">${escapeHtml(pipeline)}</span>`;

                const paymentBadge = paymentOk
                    ? `<span class="badge bg-success-subtle text-success border border-success-subtle"><i class="bi bi-check-circle-fill me-1"></i>${escapeHtml(t('backOffice.dashboard.telecomQueue.paymentOk'))}</span>`
                    : `<span class="badge bg-warning-subtle text-warning border border-warning-subtle"><i class="bi bi-hourglass-split me-1"></i>${escapeHtml(t('backOffice.dashboard.telecomQueue.paymentPending'))}</span>`;

                let bdrLedgerHtml = '';
                if (isBdr) {
                    const balance = pick(row, 'outstandingBalanceSnapshot', 'OutstandingBalanceSnapshot') || 0;
                    const writeOff = pick(row, 'writeOffAmount', 'WriteOffAmount') || 0;
                    const cashTarget = pick(row, 'collectedAmount', 'CollectedAmount') || 0;
                    const ledgerMode = pick(row, 'bdrLedgerMode', 'BdrLedgerMode');
                    const ledgerNote = pick(row, 'bdrLedgerNoteAr', 'BdrLedgerNoteAr');
                    const isFullSettlement = ledgerMode === 'FullPaymentSettlement';
                    const accountType = accountTypeLabel(pick(row, 'accountType', 'AccountType'));
                    const debtDisplay = isFullSettlement ? 0 : Math.abs(Number(balance) || 0);
                    const writeOffDisplay = isFullSettlement ? 0 : writeOff;
                    const ledgerBadge = isFullSettlement
                        ? t('backOffice.dashboard.telecomQueue.bdrFullSettlement', 'Full payment settlement')
                        : t('backOffice.dashboard.telecomQueue.bdrVerified', 'Verified & audited');

                    bdrLedgerHtml = `
                        <div class="p-3 bg-white rounded border bo-bdr-ledger-card mb-2">
                            <div class="d-flex justify-content-between align-items-center mb-3 pb-2 border-bottom">
                                <h6 class="mb-0 text-danger fw-bold"><i class="bi bi-bank me-2"></i>${escapeHtml(t('backOffice.dashboard.telecomQueue.bdrLedgerTitle', 'Financial ledger (BDR audit)'))}</h6>
                                <span class="badge ${isFullSettlement ? 'bg-success-subtle text-success' : 'bg-primary-subtle text-primary'}">${escapeHtml(ledgerBadge)}</span>
                            </div>
                            ${ledgerNote ? `<p class="small text-muted mb-3">${escapeHtml(ledgerNote)}</p>` : ''}
                            <div class="row g-3">
                                <div class="col-md-3">
                                    <div class="bo-finance-block">
                                        <label class="small text-muted d-block mb-1">${escapeHtml(t('backOffice.dashboard.telecomQueue.accountType', 'Account type'))}</label>
                                        <span class="fw-semibold text-dark">${escapeHtml(accountType)}</span>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="bo-debt-highlight ${isFullSettlement ? 'bg-success-subtle border-success-subtle' : ''}">
                                        <label class="small d-block mb-1 opacity-75">${escapeHtml(t('backOffice.dashboard.telecomQueue.outstandingDebt', 'Outstanding debt'))}</label>
                                        <span class="fw-bold fs-5 ${isFullSettlement ? 'text-success' : ''}">${isFullSettlement ? '0' : `-${debtDisplay.toLocaleString(getUiLocale())}`} ${escapeHtml(t('backOffice.dashboard.kpi.currencySyp'))}</span>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="bo-finance-block" style="background: #f0fdf4; border-color: #dcfce7;">
                                        <label class="small text-success d-block mb-1">${escapeHtml(t('backOffice.dashboard.telecomQueue.writeOffWaiver', 'Write-off waiver'))}</label>
                                        <span class="fw-bold text-success fs-5">${isFullSettlement ? '—' : `${writeOffDisplay.toLocaleString(getUiLocale())} ${escapeHtml(t('backOffice.dashboard.kpi.currencySyp'))}`}</span>
                                    </div>
                                </div>
                                <div class="col-md-3">
                                    <div class="bo-finance-block">
                                        <label class="small text-muted d-block mb-1">${escapeHtml(t('backOffice.dashboard.telecomQueue.cashCollection', 'Cash collection'))}</label>
                                        <span class="fw-bold text-dark fs-5">${cashTarget.toLocaleString(getUiLocale())} ${escapeHtml(t('backOffice.dashboard.kpi.currencySyp'))}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    `;
                }

                const collapseId = `details_${id.replace(/-/g, '_')}`;
                const auditorName = pick(row, 'auditorDisplayName', 'AuditorDisplayName')
                    || t('backOffice.dashboard.telecomQueue.auditor', 'Auditor');
                const rejectionReason = pick(row, 'rejectionReasonAr', 'RejectionReasonAr');
                const isRejected = row.status === 4 || row.Status === 4;
                const hasIdentity = !!(pick(row, 'hasIdentityDocument', 'HasIdentityDocument'));
                const identityDocUrl = hasIdentity ? boIdentityDocumentUrl(id) : '';
                const identityReviewHtml = hasIdentity
                    ? `<div class="p-3 bg-white rounded border mb-3">
                            <div class="d-flex justify-content-between align-items-center mb-2">
                                <h6 class="mb-0 text-dark fw-bold"><i class="bi bi-person-vcard me-2"></i>${escapeHtml(t('backOffice.dashboard.telecomQueue.identityDocTitle', 'Identity document'))}</h6>
                                <span class="badge bg-success-subtle text-success border border-success-subtle">${escapeHtml(t('backOffice.dashboard.telecomQueue.identityUploaded', 'Uploaded'))}</span>
                            </div>
                            <div class="d-flex flex-wrap gap-2">
                                <a class="btn btn-sm btn-outline-primary" href="${escapeHtml(identityDocUrl)}" target="_blank" rel="noopener" onclick="event.stopPropagation()">
                                    <i class="bi bi-eye me-1"></i>${escapeHtml(t('backOffice.dashboard.telecomQueue.viewIdentity', 'View document'))}
                                </a>
                            </div>
                            <div class="mt-2 border rounded overflow-hidden bg-light" style="max-height: 220px;">
                                <iframe src="${escapeHtml(identityDocUrl)}" title="identity" class="w-100" style="height: 220px; border: 0;" onclick="event.stopPropagation()"></iframe>
                            </div>
                       </div>`
                    : `<div class="alert alert-warning py-2 small mb-3">
                            <i class="bi bi-exclamation-triangle me-1"></i>${escapeHtml(blockReason || t('backOffice.dashboard.telecomQueue.identityMissing', 'Identity document not uploaded yet.'))}
                       </div>`;

                return `
                <tr class="align-middle bo-queue-row" style="cursor: pointer;" onclick="if(!event.target.closest('button')) document.getElementById('btn_toggle_${collapseId}').click()">
                    <td class="ps-3">
                        <div class="d-flex align-items-center gap-2">
                            <button class="btn btn-link btn-sm p-0 text-danger" type="button" id="btn_toggle_${collapseId}" 
                                    data-bs-toggle="collapse" data-bs-target="#${collapseId}" aria-expanded="false" 
                                    onclick="event.stopPropagation()">
                                <i class="bi bi-plus-square fs-5"></i>
                            </button>
                            <strong class="text-dark" dir="ltr">${escapeHtml(number)}</strong>
                        </div>
                    </td>
                    <td><span class="fw-semibold text-secondary">${escapeHtml(kindName)}</span></td>
                    <td dir="ltr" class="fw-bold text-danger">${escapeHtml(msisdn)}</td>
                    <td><span class="badge bg-light text-dark border">${escapeHtml(clearance)}</span></td>
                    <td>${slaHtml}</td>
                    <td>
                        <div class="d-flex flex-column gap-1">
                            <span class="small font-monospace text-muted">${escapeHtml(paymentRef)}</span>
                            ${paymentRef !== '—' ? paymentBadge : ''}
                        </div>
                    </td>
                    <td>${pipelineBadge}</td>
                    <td class="text-end pe-3">
                        <i class="bi bi-chevron-expand text-muted"></i>
                    </td>
                </tr>
                <tr class="collapse-row border-0">
                    <td colspan="8" class="p-0 border-0">
                        <div class="collapse" id="${collapseId}">
                            <div class="px-4 py-3 bg-light-subtle border-start border-end border-danger border-4 border-top-0 border-bottom-0">
                                ${bdrLedgerHtml}
                                ${identityReviewHtml}
                                <div class="d-flex justify-content-between align-items-center mt-3 pt-2 border-top">
                                    <div class="small text-muted">
                                        <div class="mb-1">${escapeHtml(t('backOffice.dashboard.telecomQueue.recordCreated', 'Created'))}: ${formatDateTime(pick(row, 'createdAtUtc', 'CreatedAtUtc'))}</div>
                                    </div>
                                    <div class="btn-group shadow-sm">
                                        ${isFinalState ? 
                                            `<div class="alert alert-light border small mb-0 py-2 px-3">
                                                <i class="bi bi-info-circle me-2"></i>
                                                ${escapeHtml(formatTpl(
                                                    row.status === 3 || row.status === 8
                                                        ? t('backOffice.dashboard.telecomQueue.approvedBy', 'Approved by {user} at {time}')
                                                        : t('backOffice.dashboard.telecomQueue.rejectedBy', 'Rejected by {user} at {time}'),
                                                    {
                                                        user: auditorName,
                                                        time: formatDateTime(row.updatedAtUtc || new Date()),
                                                    }
                                                ))}
                                                ${isRejected && rejectionReason ? `
                                                    <div class="mt-2 pt-2 border-top text-danger">
                                                        <strong>${escapeHtml(t('backOffice.dashboard.telecomQueue.rejectionReason', 'Rejection reason'))}:</strong>
                                                        ${escapeHtml(rejectionReason)}
                                                    </div>` : ''}
                                            </div>` : 
                                            (canExecuteFin ? 
                                                `<button type="button" class="btn btn-success px-4 bo-approve-op" 
                                                        onclick="event.stopPropagation(); approveTelecomOperation('${escapeHtml(id)}')"
                                                        ${canApprove ? '' : 'disabled data-bs-toggle="tooltip" title="' + escapeHtml(blockReason) + '"'}>
                                                    <i class="bi bi-check-lg me-2"></i>${escapeHtml(t('backOffice.dashboard.telecomQueue.approve'))}
                                                </button>
                                                <button type="button" class="btn btn-outline-danger bo-reject-op" 
                                                        onclick="event.stopPropagation(); rejectTelecomOperation('${escapeHtml(id)}')">
                                                    <i class="bi bi-x-lg me-2"></i>${escapeHtml(t('backOffice.dashboard.telecomQueue.reject'))}
                                                </button>` : 
                                                `<div class="badge bg-light text-muted border p-2"><i class="bi bi-shield-lock me-1"></i>${escapeHtml(t('backOffice.dashboard.telecomQueue.financialAuditRequired', 'Financial audit permission required'))}</div>`)
                                        }
                                    </div>
                                </div>
                            </div>
                        </div>
                    </td>
                </tr>`;
            })
            .join('');
        
        // Initialize tooltips for disabled buttons
        if (typeof bootstrap !== 'undefined') {
            const tooltips = body.querySelectorAll('[data-bs-toggle="tooltip"]');
            tooltips.forEach(el => new bootstrap.Tooltip(el));
        }
    };

    async function loadPendingTelecomRequests() {
        const userPerms = StorageManager.getPermissions?.() || [];
        if (!StorageManager.hasAnyPermission(userPerms, PENDING_QUEUE_VIEW_PERMISSIONS)) {
            pendingTelecomOps = [];
            renderTelecomQueue();
            return;
        }
        try {
            const params = { take: 100 };
            if (currentView === 'historical') {
                params.includeResolved = true;
            } else if (currentView === 'tech') {
                params.domain = 1; // NetworkAndTechnical
            } else {
                // Active queue: default behavior (Pending/In_Progress)
            }

            const res = await AxiosManager.get('/TelecomBackOffice/GetPendingRequests', { params });
            const content = res?.data?.content ?? res?.data?.Content ?? {};
            let rows = content.data ?? content.Data ?? [];
            if (currentView === 'historical') {
                rows = filterHistoricalLedgerRows(rows);
            }
            pendingTelecomOps = rows;
            
            if (currentView === 'tech') {
                await loadTickets();
            }

            renderTelecomQueue();
            updateTabCounters(currentView === 'historical' ? pendingTelecomOps.length : (content.total ?? content.Total ?? pendingTelecomOps.length));
        } catch (e) {
            console.warn('Pending telecom queue', e);
            pendingTelecomOps = [];
            renderTelecomQueue();
        }
    }

    function updateTabCounters(count) {
        if (currentView === 'active') {
            const el = document.getElementById('boActiveCounter');
            if (el) el.textContent = String(count);
        } else if (currentView === 'tech') {
            const el = document.getElementById('boTechCounter');
            if (el) el.textContent = String(count);
        }
    }

    async function approveTelecomOperation(operationId) {
        if (!operationId || isTelecomActionInFlight) return;

        setTelecomActionBusy(true);
        try {
            const res = await AxiosManager.post('/TelecomBackOffice/ApproveRequest', {
                operationId,
            });
            await finalizeTelecomQueueAction(
                operationId,
                res,
                t('backOffice.dashboard.telecomQueue.approved')
            );
        } catch (e) {
            console.error('Approval failed:', e);
            toastError(e, t('backOffice.dashboard.messages.loadError'));
        } finally {
            setTelecomActionBusy(false);
        }
    }

    async function rejectTelecomOperation(operationId) {
        if (!operationId || isTelecomActionInFlight) return;
        if (typeof Swal === 'undefined') return;

        const result = await Swal.fire({
            icon: 'warning',
            title: t('backOffice.dashboard.telecomQueue.rejectTitle'),
            input: 'textarea',
            inputPlaceholder: t('backOffice.dashboard.telecomQueue.rejectPlaceholder'),
            inputAttributes: { maxlength: 500 },
            showCancelButton: true,
            confirmButtonText: t('backOffice.dashboard.telecomQueue.reject'),
            cancelButtonText: t('backOffice.cancel'),
            customClass: { container: 'bo-swal-over-offcanvas' },
            preConfirm: (value) => {
                const reason = (value || '').trim();
                if (reason.length < 5) {
                    Swal.showValidationMessage(t('backOffice.dashboard.resolutionHint'));
                    return false;
                }
                return reason;
            },
        });

        if (!result.isConfirmed || !result.value) return;

        setTelecomActionBusy(true);
        try {
            const res = await AxiosManager.post('/TelecomBackOffice/RejectRequest', {
                operationId,
                rejectionReason: result.value,
            });
            await finalizeTelecomQueueAction(
                operationId,
                res,
                t('backOffice.dashboard.telecomQueue.rejected')
            );
        } catch (e) {
            console.error('Rejection failed:', e);
            toastError(e, t('backOffice.dashboard.messages.loadError'));
        } finally {
            setTelecomActionBusy(false);
        }
    }

    function wireTelecomQueueActions() {
        const panel = document.getElementById('boTelecomQueuePanel');
        if (!panel || panel.dataset.wired === '1') return;
        panel.dataset.wired = '1';
        
        // Tab switching logic
        const tabs = document.querySelectorAll('#boQueueTabs button[data-bs-toggle="pill"]');
        tabs.forEach(tab => {
            tab.addEventListener('shown.bs.tab', async (e) => {
                const view = e.target.dataset.view;
                currentView = view;
                await loadPendingTelecomRequests();
                if (view !== 'historical') {
                    await loadTickets();
                }
            });
        });
    }

    async function loadSuspensionKpis() {
        try {
            const res = await AxiosManager.get('/Telecom/GetSuspensionKpis', { params: scopeParams() });
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
            set('susKpiFailRate');
            set('susKpiSla');
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
            const res = await AxiosManager.get('/Telecom/GetReconnectKpis', { params: scopeParams() });
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
            set('rcnKpiFailRate');
            set('rcnKpiSla');
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
            const res = await AxiosManager.get('/Telecom/GetTerminationKpis', { params: scopeParams() });
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
            set('trmKpiFailRate');
            set('trmKpiSla');
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
            const res = await AxiosManager.get('/Telecom/GetOfferSubscriptionKpis', { params: scopeParams() });
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
            set('osKpiFailRate');
            set('osKpiSla');
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
            const res = await AxiosManager.get('/Telecom/GetChangeNumberKpis', { params: scopeParams() });
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
            set('cnrKpiFailRate');
            set('cnrKpiSla');
            const reasons = c.topReasons ?? c.TopReasons ?? [];
            const txt = reasons.length
                ? reasons
                      .map((r) => {
                          const prem = r.premiumCount ?? r.PremiumCount ?? 0;
                          const suffix =
                              prem > 0
                                  ? ` · ${prem} ${t('backOffice.dashboard.kpi.suffixPremium')}`
                                  : '';
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
            const res = await AxiosManager.get('/Telecom/GetSimSwapKpis', { params: scopeParams() });
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
            set('simKpiFailRate');
            set('simKpiSla');
            const reasons = c.topReasons ?? c.TopReasons ?? [];
            const txt = reasons.length
                ? reasons
                      .map((r) => {
                          const lost = r.lostOrStolenCount ?? r.LostOrStolenCount ?? 0;
                          const suffix =
                              lost > 0
                                  ? ` · ${lost} ${t('backOffice.dashboard.kpi.suffixLostStolen')}`
                                  : '';
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
            const res = await AxiosManager.get('/Telecom/GetTakeOverOwnershipKpis', { params: scopeParams() });
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('tkoKpiTotal', pick(c, 'totalToday', 'TotalToday'));
            set('tkoKpiCompleted', pick(c, 'completedToday', 'CompletedToday'));
            set('tkoKpiFailed', pick(c, 'failedToday', 'FailedToday'));
            set('tkoKpiPending', pick(c, 'pendingBackOffice', 'PendingBackOffice'));
            set('tkoKpiFailRate');
            set('tkoKpiSla');
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
            const res = await AxiosManager.get('/Telecom/GetChangeGsmTypeKpis', { params: scopeParams() });
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('cgtKpiTotal', pick(c, 'totalToday', 'TotalToday'));
            set('cgtKpiCompleted', pick(c, 'completedToday', 'CompletedToday'));
            set('cgtKpiFailed', pick(c, 'failedToday', 'FailedToday'));
            set('cgtKpiFailRate');
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
            const res = await AxiosManager.get('/Telecom/GetSellingLineActivationKpis', { params: scopeParams() });
            const c = res?.data?.content ?? res?.data?.Content ?? {};
            const set = (id, v) => {
                const el = document.getElementById(id);
                if (el) el.textContent = v ?? '—';
            };
            set('slKpiVolume', pick(c, 'totalVolume', 'TotalVolume'));
            const completion = pick(c, 'completionRatePercent', 'CompletionRatePercent');
            const fallout = pick(c, 'falloutRatePercent', 'FalloutRatePercent');
            const sla = pick(c, 'slaCompliancePercent', 'SlaCompliancePercent');
            set('slKpiCompletion', completion != null ? `${completion}%` : '—');
            set('slKpiFallout', fallout != null ? `${fallout}%` : '—');
            set('slKpiSla', sla != null ? `${sla}%` : '—');
            set('slKpiAht', pick(c, 'avgHandlingTimeMinutes', 'AvgHandlingTimeMinutes'));
            set('slKpiOverride', pick(c, 'manualOverrideCount', 'ManualOverrideCount'));
            const reasons = c.rejectionReasons ?? c.RejectionReasons ?? [];
            const reasonsTxt = reasons.length
                ? reasons.map((r) => `${r.reason ?? r.Reason} (${r.count ?? r.Count})`).join(' · ')
                : '—';
            set('slKpiReasons', reasonsTxt);
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
            loadPendingTelecomRequests();
            const ticketsPromise = loadTickets().catch((e) => {
                tickets = [];
                ticketPreview = [];
                updateKpis(0);
                updateEmptyState();
                showError(
                    e?.response?.data?.message ||
                        e?.message ||
                        t('backOffice.dashboard.messages.ticketsLoadFailed')
                );
            });

            const offeringsPromise = withTimeout(loadOfferings(), 12000, 'Catalog').catch(() => {
                offerings = [];
                const countEl = document.getElementById('boOfferingCount');
                if (countEl) countEl.textContent = '0';
            });

            await Promise.all([ticketsPromise, offeringsPromise]);

            const sfReady = await waitForSyncfusion();
            if (!sfReady) {
                showError(
                    t('backOffice.dashboard.messages.syncfusionMissing')
                );
                return;
            }

            if (ticketPreview.length > 0) {
                if (!ticketsGrid) {
                    try {
                        initTicketsGrid();
                    } catch (gridErr) {
                        console.error('BackOffice tickets grid init failed', gridErr);
                        showError(
                            t('backOffice.dashboard.messages.gridInitFailed')
                        );
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
        wireTelecomQueueActions();
        document.getElementById('boReloadBtn')?.addEventListener('click', () => loadDashboardData(true));
        document.getElementById('boHlrBtn')?.addEventListener('click', hlrResync);
        document.getElementById('boForceHlrBtn')?.addEventListener('click', forceHlrSync);
        document.getElementById('btnCbsForceSync')?.addEventListener('click', forceCbsSync);
        document.getElementById('btnNetworkPing')?.addEventListener('click', queryLiveNetworkStatus);
        document.getElementById('btnCoreEscalate')?.addEventListener('click', openTier3EscalationPanel);
        document.getElementById('boResolveOpenBtn')?.addEventListener('click', quickResolve);
        document.getElementById('boSaveStatusBtn')?.addEventListener('click', saveTicketStatus);
        window.addEventListener('resize', () => applyBoTicketsGridHeight());
        document.documentElement.addEventListener('syriatel-locale-changed', async () => {
            try {
                await window.TelecomI18n?.ensureLoaded?.();
                applyPageI18n();
                renderScopeSelects();
                remapTicketLabels();
                refreshTicketsGridI18n();
                if (catalogGrid) {
                    catalogGrid.columns = getCatalogColumns();
                    catalogGrid.refresh();
                }
                if (selectedTicket && lastDrawerRow && lastDrawerDetail) {
                    fillDrawer(lastDrawerRow, lastDrawerDetail);
                } else if (selectedTicket) {
                    const row = ticketPreview.find((x) => x.id === selectedTicket.id) || selectedTicket;
                    fillDrawer(row, selectedTicket);
                }
                if (lastLiveStatusData) renderLiveStatus(lastLiveStatusData);
                await loadPaymentServicesPanel();
                renderTelecomQueue();
                updateSlaTimers();
                setLastRefresh();
            } catch (e) {
                console.warn('BackOffice locale refresh failed', e);
            }
        });

        try {
            await window.TelecomI18n?.ensureLoaded?.();
            applyPageI18n();
        } catch (e) {
            console.warn('BackOffice i18n preload failed', e);
        }

        const allowed = await ensureAccess();
        if (!allowed) {
            setLoading(false);
            showError(
                t('backOffice.dashboard.messages.accessDenied')
            );
            return;
        }

        await initScopeFilters();
        await loadDashboardData(true);
        applySecurityUIGovernance();

        /** Post-execution refresh hook (CBS / automation handshake). */
        window.triggerSearch = () => refreshDashboardQueue();
        window.approveTelecomOperation = approveTelecomOperation;
        window.rejectTelecomOperation = rejectTelecomOperation;
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }
})();
