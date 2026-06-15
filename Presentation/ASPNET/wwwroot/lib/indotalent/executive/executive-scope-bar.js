/**
 * Unified scope bar for Executive Command Center — single source of filter state.
 */
const ExecutiveScopeBar = (function () {
    const MIS_PERMISSION = 'telecom.reports.mis';
    const EVENT = 'executive-scope-changed';

    const state = {
        regionId: null,
        branchId: null,
        fromUtc: null,
        toUtc: null,
        canUseFilters: false,
        scopeLabelAr: '—',
        accessLevel: '',
        regions: [],
        branches: [],
        loading: false,
        initialized: false,
    };

    function hasPermission() {
        const perms = typeof StorageManager !== 'undefined' && StorageManager.getPermissions
            ? StorageManager.getPermissions() || []
            : [];
        return (
            typeof StorageManager !== 'undefined' &&
            typeof StorageManager.hasAnyPermission === 'function' &&
            StorageManager.hasAnyPermission(perms, [MIS_PERMISSION])
        );
    }

    function pick(o, ...keys) {
        if (!o) return undefined;
        for (const k of keys) {
            if (o[k] !== undefined && o[k] !== null) return o[k];
        }
        return undefined;
    }

    function defaultDateRange() {
        const to = new Date();
        const from = new Date();
        from.setDate(from.getDate() - 30);
        return { from, to };
    }

    function toIso(d) {
        return d ? new Date(d).toISOString() : null;
    }

    function getQueryParams() {
        const params = {};
        if (state.regionId) params.regionId = state.regionId;
        if (state.branchId) params.branchId = state.branchId;
        if (state.fromUtc) params.fromUtc = state.fromUtc;
        if (state.toUtc) params.toUtc = state.toUtc;
        return params;
    }

    function emitChange() {
        document.dispatchEvent(new CustomEvent(EVENT, { detail: { ...getScope() } }));
    }

    function applySummary(content) {
        if (!content) return;
        state.canUseFilters = pick(content, 'canUseFilters', 'CanUseFilters') === true;
        state.scopeLabelAr = pick(content, 'scopeLabelAr', 'ScopeLabelAr') ?? '—';
        state.accessLevel = pick(content, 'accessLevel', 'AccessLevel') ?? '';
        state.regionId = pick(content, 'effectiveRegionId', 'EffectiveRegionId') ?? state.regionId;
        state.branchId = pick(content, 'effectiveBranchId', 'EffectiveBranchId') ?? state.branchId;
        state.regions = (pick(content, 'regions', 'Regions') ?? []).map((r) => ({
            id: pick(r, 'id', 'Id'),
            nameAr: pick(r, 'nameAr', 'NameAr'),
        }));
        state.branches = (pick(content, 'branches', 'Branches') ?? []).map((b) => ({
            id: pick(b, 'id', 'Id'),
            nameAr: pick(b, 'nameAr', 'NameAr'),
            regionId: pick(b, 'regionId', 'RegionId'),
        }));
    }

    async function loadInitial() {
        state.loading = true;
        const range = defaultDateRange();
        state.fromUtc = toIso(range.from);
        state.toUtc = toIso(range.to);
        try {
            const res = await AxiosManager.get('/Telecom/GetExecutiveCommandCenterSummary', {
                params: { fromUtc: state.fromUtc, toUtc: state.toUtc },
            });
            applySummary(res?.data?.content);
            state.initialized = true;
            emitChange();
        } catch (e) {
            console.error('Executive scope init failed', e);
        } finally {
            state.loading = false;
        }
    }

    function getScope() {
        return {
            regionId: state.regionId,
            branchId: state.branchId,
            fromUtc: state.fromUtc,
            toUtc: state.toUtc,
            canUseFilters: state.canUseFilters,
            scopeLabelAr: state.scopeLabelAr,
            accessLevel: state.accessLevel,
            regions: state.regions,
            branches: state.branches,
        };
    }

    function branchOptions() {
        if (!state.regionId) return state.branches;
        return state.branches.filter((b) => b.regionId === state.regionId);
    }

    function mount(containerEl) {
        if (!containerEl || typeof Vue === 'undefined') return null;

        const ui = Vue.reactive({
            regionId: state.regionId,
            branchId: state.branchId,
            fromDate: state.fromUtc ? state.fromUtc.slice(0, 10) : '',
            toDate: state.toUtc ? state.toUtc.slice(0, 10) : '',
            loading: false,
            scopeLabelAr: state.scopeLabelAr,
            canUseFilters: state.canUseFilters,
            regions: state.regions,
            branches: state.branches,
        });

        const t = (key) => {
            const hit = typeof TelecomI18n !== 'undefined' && TelecomI18n.t
                ? TelecomI18n.t('executiveCommandCenter.scope.' + key)
                : null;
            return hit || key;
        };

        const filteredBranches = Vue.computed(() => {
            if (!ui.regionId) return ui.branches;
            return ui.branches.filter((b) => b.regionId === ui.regionId);
        });

        const syncFromState = () => {
            ui.regionId = state.regionId;
            ui.branchId = state.branchId;
            ui.fromDate = state.fromUtc ? state.fromUtc.slice(0, 10) : '';
            ui.toDate = state.toUtc ? state.toUtc.slice(0, 10) : '';
            ui.scopeLabelAr = state.scopeLabelAr;
            ui.canUseFilters = state.canUseFilters;
            ui.regions = state.regions;
            ui.branches = state.branches;
        };

        const applyAndEmit = async () => {
            state.regionId = ui.regionId || null;
            state.branchId = ui.branchId || null;
            if (ui.fromDate) state.fromUtc = new Date(ui.fromDate + 'T00:00:00Z').toISOString();
            if (ui.toDate) state.toUtc = new Date(ui.toDate + 'T23:59:59Z').toISOString();
            ui.loading = true;
            try {
                const res = await AxiosManager.get('/Telecom/GetExecutiveCommandCenterSummary', {
                    params: getQueryParams(),
                });
                applySummary(res?.data?.content);
                syncFromState();
                emitChange();
            } finally {
                ui.loading = false;
            }
        };

        const onRegionChange = () => {
            ui.branchId = null;
        };

        const app = Vue.createApp({
            setup() {
                Vue.onMounted(async () => {
                    await loadInitial();
                    syncFromState();
                });
                return {
                    ui,
                    t,
                    filteredBranches,
                    applyAndEmit,
                    onRegionChange,
                };
            },
            template: `
                <section class="executive-scope-bar strategic-filter-bar mb-4 p-3 rounded-3">
                    <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3">
                        <div>
                            <span class="strategic-exec-kicker">{{ t('kicker') }}</span>
                            <p class="mb-0 small text-muted">{{ t('scopePrefix') }} <strong>{{ ui.scopeLabelAr }}</strong></p>
                        </div>
                        <span v-if="ui.loading" class="spinner-border spinner-border-sm text-danger"></span>
                    </div>
                    <div v-if="ui.canUseFilters" class="row g-3 align-items-end">
                        <div class="col-md-3">
                            <label class="form-label strategic-label">{{ t('region') }}</label>
                            <select class="form-select strategic-select" v-model="ui.regionId" @change="onRegionChange">
                                <option :value="null">{{ t('allSyria') }}</option>
                                <option v-for="r in ui.regions" :key="r.id" :value="r.id">{{ r.nameAr }}</option>
                            </select>
                        </div>
                        <div class="col-md-3">
                            <label class="form-label strategic-label">{{ t('branch') }}</label>
                            <select class="form-select strategic-select" v-model="ui.branchId">
                                <option :value="null">{{ t('allBranches') }}</option>
                                <option v-for="b in filteredBranches" :key="b.id" :value="b.id">{{ b.nameAr }}</option>
                            </select>
                        </div>
                        <div class="col-md-2">
                            <label class="form-label strategic-label">{{ t('from') }}</label>
                            <input type="date" class="form-control strategic-select" v-model="ui.fromDate" />
                        </div>
                        <div class="col-md-2">
                            <label class="form-label strategic-label">{{ t('to') }}</label>
                            <input type="date" class="form-control strategic-select" v-model="ui.toDate" />
                        </div>
                        <div class="col-md-2">
                            <button type="button" class="btn btn-strategic-gold w-100" @click="applyAndEmit" :disabled="ui.loading">{{ t('apply') }}</button>
                        </div>
                    </div>
                    <div v-else class="alert alert-secondary py-2 mb-0 small">
                        <i class="bi bi-lock me-1"></i>{{ t('lockedScope') }}: <strong>{{ ui.scopeLabelAr }}</strong>
                    </div>
                </section>
            `,
        });

        app.mount(containerEl);
        return app;
    }

    return {
        EVENT,
        hasPermission,
        mount,
        loadInitial,
        getScope,
        getQueryParams,
        applySummary,
    };
})();
