/**
 * Financial payments snapshot — the single home for monetary operations metrics
 * (recharge, completed payments, failure rate, payment SLA). Lives in the MIS tab.
 */
const ExecutivePaymentsPanel = (function () {
    function t(key) {
        const hit = typeof TelecomI18n !== 'undefined' && TelecomI18n.t
            ? TelecomI18n.t('executiveCommandCenter.payments.' + key)
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
                const res = await AxiosManager.get('/Telecom/GetExecutiveOperationsSnapshot', { params });
                const c = res?.data?.content;
                state.data = c
                    ? {
                        recharge: pick(c, 'paymentRechargeToday', 'PaymentRechargeToday'),
                        completed: pick(c, 'paymentCompletedToday', 'PaymentCompletedToday'),
                        failureRate: pick(c, 'paymentFailureRatePercent', 'PaymentFailureRatePercent'),
                        sla: pick(c, 'paymentSlaPercent', 'PaymentSlaPercent'),
                    }
                    : null;
            } catch (e) {
                console.error('Payments snapshot failed', e);
            } finally {
                state.loading = false;
            }
        };

        document.addEventListener(ExecutiveScopeBar?.EVENT || 'executive-scope-changed', load);

        const app = Vue.createApp({
            setup() {
                Vue.onMounted(load);
                const failureClass = Vue.computed(() => {
                    const v = Number(state.data?.failureRate);
                    if (isNaN(v)) return 'text-muted';
                    if (v >= 10) return 'status-crimson';
                    if (v >= 3) return 'status-amber';
                    return 'status-emerald';
                });
                return { state, t, formatMoney, formatPct, failureClass, load };
            },
            template: `
                <section class="strategic-panel p-3 mb-4">
                    <h3 class="strategic-panel-title mb-3"><i class="bi bi-cash-coin me-1"></i>{{ t('title') }}</h3>
                    <div v-if="state.loading" class="text-center py-3"><span class="spinner-border spinner-border-sm text-danger"></span></div>
                    <div v-else class="row g-3 strategic-bento">
                        <div class="col-6 col-lg-3">
                            <div class="strategic-bento-card h-100">
                                <p class="strategic-card-title">{{ t('recharge') }}</p>
                                <p class="strategic-metric-lg text-danger mb-0">{{ formatMoney(state.data?.recharge) }}</p>
                            </div>
                        </div>
                        <div class="col-6 col-lg-3">
                            <div class="strategic-bento-card h-100">
                                <p class="strategic-card-title">{{ t('completed') }}</p>
                                <p class="strategic-metric-lg mb-0">{{ state.data?.completed ?? '—' }}</p>
                            </div>
                        </div>
                        <div class="col-6 col-lg-3">
                            <div class="strategic-bento-card h-100">
                                <p class="strategic-card-title">{{ t('failureRate') }}</p>
                                <p class="strategic-metric-lg mb-0" :class="failureClass">{{ formatPct(state.data?.failureRate) }}</p>
                            </div>
                        </div>
                        <div class="col-6 col-lg-3">
                            <div class="strategic-bento-card h-100">
                                <p class="strategic-card-title">{{ t('sla') }}</p>
                                <p class="strategic-metric-lg status-emerald mb-0">{{ formatPct(state.data?.sla) }}</p>
                            </div>
                        </div>
                    </div>
                </section>
            `,
        });

        app.mount(containerEl);
        return { load, app };
    }

    return { mount };
})();
