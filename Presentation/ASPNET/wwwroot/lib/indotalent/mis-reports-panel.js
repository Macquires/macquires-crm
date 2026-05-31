/**
 * MIS KPI strip — ARPU, churn, branch workload from GET /Telecom/GetTelecomDashboardKpis.
 * Merged from legacy TelecomMisReports page into DefaultDashboard (#mis-reports).
 */
const MisReportsPanel = (function () {
    const MIS_PERMISSION = 'telecom.reports.mis';

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

    function mount(rootEl) {
        if (!rootEl || typeof Vue === 'undefined') {
            return null;
        }

        const state = Vue.reactive({
            kpis: { arpuDemo: 0, churnPercentDemo: 0, branchHeat: [] },
            loading: false,
        });

        const formatMoney = (n) => {
            if (n == null || isNaN(n)) return '—';
            return new Intl.NumberFormat('ar-SY', { maximumFractionDigits: 0 }).format(n);
        };

        const formatWorkload = (n) => {
            if (n == null || isNaN(n)) return '—';
            const v = Math.round(Number(n));
            if (v >= 80) return 'مرتفع';
            if (v >= 40) return 'متوسط';
            return 'منخفض';
        };

        const loadKpis = async () => {
            state.loading = true;
            try {
                const res = await AxiosManager.get('/Telecom/GetTelecomDashboardKpis', {});
                const c = res?.data?.content;
                if (c) {
                    state.kpis.arpuDemo = c.arpuDemo ?? c.ArpuDemo ?? 0;
                    state.kpis.churnPercentDemo = c.churnPercentDemo ?? c.ChurnPercentDemo ?? 0;
                    state.kpis.branchHeat = c.branchHeat ?? c.BranchHeat ?? [];
                }
            } catch (err) {
                console.error('GetTelecomDashboardKpis failed', err);
                state.kpis.branchHeat = [];
            } finally {
                state.loading = false;
            }
        };

        const app = Vue.createApp({
            setup() {
                Vue.onMounted(() => {
                    loadKpis().then(scrollToPanel);
                });

                return {
                    ...Vue.toRefs(state),
                    formatMoney,
                    formatWorkload,
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
