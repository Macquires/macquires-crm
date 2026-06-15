/**
 * Executive Command Center — scorecard tab (30-second KPI overview).
 */
const ExecutiveScorecardPanel = (function () {
    function t(key) {
        const hit = typeof TelecomI18n !== 'undefined' && TelecomI18n.t
            ? TelecomI18n.t('executiveCommandCenter.scorecard.' + key)
            : null;
        return hit || key;
    }

    function pick(o, ...keys) {
        if (!o) return undefined;
        for (const k of keys) {
            if (o[k] !== undefined && o[k] !== null) return o[k];
        }
        return undefined;
    }

    function mount(containerEl) {
        if (!containerEl || typeof Vue === 'undefined') return null;

        const state = Vue.reactive({ data: null, loading: false });

        const formatMoney = (n) => {
            if (n == null || isNaN(n)) return '—';
            const loc = typeof TelecomI18n !== 'undefined' && TelecomI18n.getLang?.() === 'en' ? 'en-US' : 'ar-SY';
            return new Intl.NumberFormat(loc, { maximumFractionDigits: 0 }).format(n);
        };

        const formatPct = (n) => (n == null || isNaN(n) ? '—' : `${Number(n).toFixed(1)}%`);

        const load = async () => {
            state.loading = true;
            try {
                const params = typeof ExecutiveScopeBar !== 'undefined'
                    ? ExecutiveScopeBar.getQueryParams()
                    : {};
                const res = await AxiosManager.get('/Telecom/GetExecutiveCommandCenterSummary', { params });
                const c = res?.data?.content;
                state.data = c
                    ? {
                        totalRevenue: pick(c, 'totalRevenue', 'TotalRevenue'),
                        revenueChangePercent: pick(c, 'revenueChangePercent', 'RevenueChangePercent'),
                        arpu: pick(c, 'arpu', 'Arpu'),
                        churn30: pick(c, 'churnPercent30', 'ChurnPercent30'),
                        churn60: pick(c, 'churnPercent60', 'ChurnPercent60'),
                        operationsToday: pick(c, 'operationsToday', 'OperationsToday'),
                        openTickets: pick(c, 'openTickets', 'OpenTickets'),
                        sla: pick(c, 'slaCompliancePercent', 'SlaCompliancePercent'),
                        criticalAlerts: pick(c, 'criticalAlerts', 'CriticalAlerts'),
                        warningAlerts: pick(c, 'warningAlerts', 'WarningAlerts'),
                        onlineEmployees: pick(c, 'onlineEmployees', 'OnlineEmployees'),
                        bestBranch: pick(c, 'bestBranch', 'BestBranch'),
                        weakestBranch: pick(c, 'weakestBranch', 'WeakestBranch'),
                        digest: pick(c, 'weeklyDigestSummaryAr', 'WeeklyDigestSummaryAr'),
                    }
                    : null;
            } catch (e) {
                console.error('Scorecard load failed', e);
            } finally {
                state.loading = false;
            }
        };

        document.addEventListener(ExecutiveScopeBar?.EVENT || 'executive-scope-changed', load);

        const app = Vue.createApp({
            setup() {
                Vue.onMounted(load);
                return { state, t, formatMoney, formatPct, load };
            },
            template: `
                <div>
                    <div v-if="state.loading" class="text-center py-4"><span class="spinner-border text-danger"></span></div>
                    <div v-else class="row g-3 strategic-bento">
                        <div class="col-6 col-lg-3" v-for="card in [
                            { label: t('revenue'), value: formatMoney(state.data?.totalRevenue), sub: formatPct(state.data?.revenueChangePercent) },
                            { label: t('arpu'), value: formatMoney(state.data?.arpu), sub: '' },
                            { label: t('churn30'), value: formatPct(state.data?.churn30), sub: t('churn60') + ': ' + formatPct(state.data?.churn60) },
                            { label: t('operations'), value: state.data?.operationsToday ?? '—', sub: '' },
                            { label: t('openTickets'), value: state.data?.openTickets ?? '—', sub: t('sla') + ': ' + formatPct(state.data?.sla) },
                            { label: t('criticalAlerts'), value: state.data?.criticalAlerts ?? 0, sub: t('warnings') + ': ' + (state.data?.warningAlerts ?? 0) },
                            { label: t('onlineNow'), value: state.data?.onlineEmployees ?? 0, sub: '' },
                            { label: t('bestBranch'), value: state.data?.bestBranch?.branchName || '—', sub: formatMoney(state.data?.bestBranch?.revenue) },
                        ]" :key="card.label">
                            <div class="strategic-bento-card h-100">
                                <p class="strategic-card-title mb-1">{{ card.label }}</p>
                                <p class="strategic-metric-xl text-danger mb-0">{{ card.value }}</p>
                                <p v-if="card.sub" class="strategic-metric-sub mb-0">{{ card.sub }}</p>
                            </div>
                        </div>
                    </div>
                    <div v-if="state.data?.weakestBranch" class="alert alert-warning mt-3 py-2 small mb-0">
                        <i class="bi bi-exclamation-triangle me-1"></i>
                        {{ t('weakestBranch') }}: <strong>{{ state.data.weakestBranch.branchName }}</strong>
                        ({{ formatMoney(state.data.weakestBranch.revenue) }})
                    </div>
                    <div v-if="state.data?.digest" class="alert alert-light border mt-3 py-3 small mb-0">
                        <i class="bi bi-journal-text me-1"></i>{{ state.data.digest }}
                    </div>
                </div>
            `,
        });

        app.mount(containerEl);
        return { load, app };
    }

    return { mount };
})();
