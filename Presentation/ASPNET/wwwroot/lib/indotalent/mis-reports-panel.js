/**
 * MIS KPI strip — live CBS payment ledger via GET /Telecom/GetTelecomDashboardKpis.
 */
const MisReportsPanel = (function () {
    const MIS_PERMISSION = 'telecom.reports.mis';

    function t(key) {
        const hit = typeof TelecomI18n !== 'undefined' && TelecomI18n.t ? TelecomI18n.t('defaultDashboard.mis.' + key) : null;
        return hit || (typeof TelecomI18n !== 'undefined' && TelecomI18n.t ? TelecomI18n.t('defaultDashboard.strategic.' + key) : null) || key;
    }

    function numberLocale() {
        return typeof TelecomI18n !== 'undefined' && TelecomI18n.getLang?.() === 'en' ? 'en-US' : 'ar-SY';
    }

    function hasPermission() {
        const perms =
            typeof StorageManager !== 'undefined' && StorageManager.getPermissions
                ? StorageManager.getPermissions() || []
                : [];
        return (
            typeof StorageManager !== 'undefined' &&
            typeof StorageManager.hasAnyPermission === 'function' &&
            StorageManager.hasAnyPermission(perms, [MIS_PERMISSION])
        );
    }

    function revealEmbed() {
        const embed = document.getElementById('mis-reports');
        if (embed) {
            embed.classList.remove('d-none');
        }
    }

    function scrollToPanel() {
        const embed = document.getElementById('mis-reports');
        if (embed && window.location.hash === '#mis-reports') {
            embed.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    function pick(o, ...keys) {
        if (!o) return undefined;
        for (const k of keys) {
            if (o[k] !== undefined && o[k] !== null) return o[k];
        }
        return undefined;
    }

    function mount(rootEl) {
        if (!rootEl || typeof Vue === 'undefined') {
            return null;
        }

        const state = Vue.reactive({
            filters: { regionId: null, branchId: null },
            kpis: {
                arpu: 0,
                churnPercent30: 0,
                totalRevenue: 0,
                revenueChangePercent: 0,
                branchHeat: [],
                scopeLabelAr: '',
                canUseFilters: false,
                regions: [],
                branches: [],
            },
            loading: false,
        });
        const localeVersion = Vue.ref(0);

        const branchOptions = Vue.computed(() => {
            if (!state.kpis.branches?.length) return [];
            if (!state.filters.regionId) return state.kpis.branches;
            return state.kpis.branches.filter((b) => b.regionId === state.filters.regionId);
        });

        const formatMoney = (n) => {
            if (n == null || isNaN(n)) return '—';
            return new Intl.NumberFormat(numberLocale(), { maximumFractionDigits: 0 }).format(n);
        };

        const formatPercent = (n) => {
            if (n == null || isNaN(n)) return '—';
            const v = Number(n);
            return `${v >= 0 ? '+' : ''}${v.toFixed(1)}%`;
        };

        const tBound = (key) => {
            void localeVersion.value;
            return t(key);
        };

        const normalize = (c) => {
            if (!c) return null;
            return {
                arpu: pick(c, 'arpu', 'Arpu', 'arpuDemo', 'ArpuDemo') ?? 0,
                churnPercent30: pick(c, 'churnPercent30', 'ChurnPercent30', 'churnPercentDemo', 'ChurnPercentDemo') ?? 0,
                totalRevenue: pick(c, 'totalRevenue', 'TotalRevenue') ?? 0,
                revenueChangePercent: pick(c, 'revenueChangePercent', 'RevenueChangePercent') ?? 0,
                scopeLabelAr: pick(c, 'scopeLabelAr', 'ScopeLabelAr') ?? '',
                canUseFilters: pick(c, 'canUseFilters', 'CanUseFilters') === true,
                regions: (pick(c, 'regions', 'Regions') ?? []).map((r) => ({
                    id: pick(r, 'id', 'Id'),
                    nameAr: pick(r, 'nameAr', 'NameAr'),
                })),
                branches: (pick(c, 'branches', 'Branches') ?? []).map((b) => ({
                    id: pick(b, 'id', 'Id'),
                    nameAr: pick(b, 'nameAr', 'NameAr'),
                    regionId: pick(b, 'regionId', 'RegionId'),
                })),
                branchHeat: (pick(c, 'branchHeat', 'BranchHeat') ?? []).map((b) => ({
                    branchId: pick(b, 'branchId', 'BranchId'),
                    branchName: pick(b, 'branchName', 'BranchName'),
                    revenue: pick(b, 'revenue', 'Revenue', 'revenueDemo', 'RevenueDemo') ?? 0,
                })),
            };
        };

        const loadKpis = async () => {
            state.loading = true;
            try {
                const params = {};
                if (state.kpis.canUseFilters) {
                    if (state.filters.regionId) params.regionId = state.filters.regionId;
                    if (state.filters.branchId) params.branchId = state.filters.branchId;
                }
                const res = await AxiosManager.get('/Telecom/GetTelecomDashboardKpis', { params });
                const m = normalize(res?.data?.content);
                if (m) {
                    state.kpis = m;
                    if (m.canUseFilters) {
                        state.filters.regionId = pick(res?.data?.content, 'effectiveRegionId', 'EffectiveRegionId') ?? state.filters.regionId;
                        state.filters.branchId = pick(res?.data?.content, 'effectiveBranchId', 'EffectiveBranchId') ?? state.filters.branchId;
                    }
                }
            } catch (err) {
                console.error('GetTelecomDashboardKpis failed', err);
                state.kpis.branchHeat = [];
            } finally {
                state.loading = false;
            }
        };

        const onRegionChange = () => {
            state.filters.branchId = null;
            loadKpis();
        };

        const app = Vue.createApp({
            setup() {
                Vue.onMounted(() => {
                    loadKpis().then(scrollToPanel);
                    document.documentElement.addEventListener('syriatel-locale-changed', () => {
                        localeVersion.value++;
                    });
                });

                return {
                    ...Vue.toRefs(state),
                    branchOptions,
                    t: tBound,
                    formatMoney,
                    formatPercent,
                    loadKpis,
                    onRegionChange,
                };
            },
        });

        app.mount(rootEl);
        return app;
    }

    async function initFromDashboard() {
        if (!hasPermission()) {
            return null;
        }
        revealEmbed();
        const root = document.getElementById('misReportsApp');
        if (!root) {
            return null;
        }
        try {
            await SecurityManager.validateToken();
        } catch {
            /* redirect handled by SecurityManager */
        }
        return mount(root);
    }

    return {
        hasPermission,
        mount,
        initFromDashboard,
        revealEmbed,
        scrollToPanel,
    };
})();
