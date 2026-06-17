/**
 * Network & integration pulse — demo/mock CBS/HLR from BillingIntegrationLog.
 */
const ExecutiveNetworkPanel = (function () {
    function t(key) {
        const hit = typeof TelecomI18n !== 'undefined' && TelecomI18n.t
            ? TelecomI18n.t('executiveCommandCenter.network.' + key)
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

        const state = Vue.reactive({ logs: [], trend: [], loading: false });

        const computeTrend = (rows) => {
            const byDay = {};
            rows.forEach((r) => {
                const d = (r.createdAtUtc || '').slice(0, 10);
                if (!d) return;
                if (!byDay[d]) byDay[d] = { ok: 0, total: 0 };
                byDay[d].total++;
                if (r.success) byDay[d].ok++;
            });
            return Object.keys(byDay)
                .sort()
                .slice(-14)
                .map((d) => ({
                    label: d,
                    value: byDay[d].total > 0 ? Math.round((byDay[d].ok / byDay[d].total) * 100) : 0,
                }));
        };

        const load = async () => {
            state.loading = true;
            try {
                const res = await AxiosManager.get('/Telecom/GetBillingIntegrationLogList', {
                    params: { take: 100 },
                });
                const rows = (pick(res?.data?.content, 'data', 'Data') ?? []).map((r) => ({
                    target: pick(r, 'integrationTarget', 'IntegrationTarget'),
                    success: pick(r, 'success', 'Success') === true,
                    message: pick(r, 'message', 'Message'),
                    createdAtUtc: pick(r, 'createdAtUtc', 'CreatedAtUtc'),
                }));
                state.logs = rows.slice(0, 12);
                state.trend = computeTrend(rows);
            } catch (e) {
                console.error('Network panel load failed', e);
            } finally {
                state.loading = false;
            }
        };

        document.addEventListener(ExecutiveScopeBar?.EVENT || 'executive-scope-changed', load);

        const app = Vue.createApp({
            setup() {
                Vue.onMounted(load);
                const successRate = Vue.computed(() => {
                    if (!state.logs.length) return '—';
                    const ok = state.logs.filter((l) => l.success).length;
                    return `${Math.round((ok / state.logs.length) * 100)}%`;
                });
                return { state, t, successRate, load };
            },
            template: `
                <div>
                    <div class="d-flex align-items-center gap-2 mb-3">
                        <span class="badge bg-secondary">{{ t('demoBadge') }}</span>
                        <span class="small text-muted">{{ t('demoHint') }}</span>
                    </div>
                    <div v-if="state.loading" class="text-center py-3"><span class="spinner-border spinner-border-sm text-danger"></span></div>
                    <div v-else class="row g-3">
                        <div class="col-md-4">
                            <div class="strategic-bento-card h-100">
                                <p class="strategic-card-title">{{ t('successRate') }}</p>
                                <p class="strategic-metric-xl text-danger mb-0">{{ successRate }}</p>
                            </div>
                        </div>
                        <div class="col-md-8">
                            <div class="strategic-panel p-3 h-100">
                                <p class="strategic-panel-title">{{ t('trend7d') }}</p>
                                <ul class="list-unstyled small mb-0 executive-trend-list">
                                    <li v-for="p in state.trend" :key="p.label" class="executive-trend-row border-bottom py-2">
                                        <span class="executive-trend-date">{{ p.label }}</span>
                                        <strong class="executive-trend-value">{{ p.value }}%</strong>
                                    </li>
                                    <li v-if="!state.trend.length" class="text-muted">{{ t('noData') }}</li>
                                </ul>
                            </div>
                        </div>
                        <div class="col-12">
                            <div class="strategic-panel p-3">
                                <p class="strategic-panel-title mb-2">{{ t('recentLogs') }}</p>
                                <ul class="list-group list-group-flush executive-log-list">
                                    <li v-for="(log, i) in state.logs" :key="i"
                                        class="list-group-item executive-log-row bg-transparent px-0 py-2">
                                        <span class="executive-log-main">
                                            <span class="badge me-2" :class="log.success ? 'bg-success' : 'bg-danger'">{{ log.target }}</span>
                                            {{ log.message }}
                                        </span>
                                        <span class="executive-log-time text-muted small">{{ (log.createdAtUtc || '').slice(0, 16).replace('T', ' ') }}</span>
                                    </li>
                                </ul>
                            </div>
                        </div>
                    </div>
                </div>
            `,
        });

        app.mount(containerEl);
        return { load, app };
    }

    return { mount };
})();
