/**
 * Read-only operations overview for executive (GM) monitoring.
 */
const ExecutiveOperationsPanel = (function () {
    function t(key) {
        const hit = typeof TelecomI18n !== 'undefined' && TelecomI18n.t
            ? TelecomI18n.t('executiveCommandCenter.operations.' + key)
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
                        paymentRecharge: pick(c, 'paymentRechargeToday', 'PaymentRechargeToday'),
                        paymentCompleted: pick(c, 'paymentCompletedToday', 'PaymentCompletedToday'),
                        paymentFailure: pick(c, 'paymentFailureRatePercent', 'PaymentFailureRatePercent'),
                        paymentSla: pick(c, 'paymentSlaPercent', 'PaymentSlaPercent'),
                        activations: pick(c, 'activationsToday', 'ActivationsToday'),
                        pending: pick(c, 'pendingOperations', 'PendingOperations'),
                        overdue: pick(c, 'overdueTickets', 'OverdueTickets'),
                        pendingOps: pick(c, 'topPendingOperations', 'TopPendingOperations') ?? [],
                        overdueTickets: pick(c, 'topOverdueTickets', 'TopOverdueTickets') ?? [],
                        scopeLabelAr: pick(c, 'scopeLabelAr', 'ScopeLabelAr'),
                    }
                    : null;
            } catch (e) {
                console.error('Operations snapshot failed', e);
            } finally {
                state.loading = false;
            }
        };

        document.addEventListener(ExecutiveScopeBar?.EVENT || 'executive-scope-changed', load);

        const app = Vue.createApp({
            setup() {
                Vue.onMounted(load);
                return { state, t, formatMoney, load };
            },
            template: `
                <div>
                    <p class="small text-muted mb-3"><i class="bi bi-eye me-1"></i>{{ t('readOnlyHint') }}</p>
                    <div v-if="state.loading" class="text-center py-3"><span class="spinner-border spinner-border-sm text-danger"></span></div>
                    <div v-else>
                        <div class="row g-3 mb-4 strategic-bento">
                            <div class="col-6 col-lg-3" v-for="kpi in [
                                { l: t('recharge'), v: formatMoney(state.data?.paymentRecharge) },
                                { l: t('payments'), v: state.data?.paymentCompleted },
                                { l: t('activations'), v: state.data?.activations },
                                { l: t('pendingOps'), v: state.data?.pending },
                            ]" :key="kpi.l" class="col-6 col-lg-3">
                                <div class="strategic-bento-card h-100">
                                    <p class="strategic-card-title">{{ kpi.l }}</p>
                                    <p class="strategic-metric-lg mb-0">{{ kpi.v ?? '—' }}</p>
                                </div>
                            </div>
                        </div>
                        <div class="row g-3">
                            <div class="col-lg-6">
                                <div class="strategic-panel p-3">
                                    <h3 class="strategic-panel-title">{{ t('topPending') }}</h3>
                                    <ul class="list-group list-group-flush">
                                        <li v-for="op in state.data?.pendingOps || []" :key="op.operationId"
                                            class="list-group-item d-flex justify-content-between bg-transparent px-0">
                                            <span>{{ op.operationNumber }} <span class="text-muted small">({{ op.kind }})</span></span>
                                            <a v-if="op.actionUrl" :href="op.actionUrl" class="btn btn-sm btn-outline-danger">{{ t('open') }}</a>
                                        </li>
                                        <li v-if="!state.data?.pendingOps?.length" class="text-muted small">{{ t('none') }}</li>
                                    </ul>
                                </div>
                            </div>
                            <div class="col-lg-6">
                                <div class="strategic-panel p-3">
                                    <h3 class="strategic-panel-title">{{ t('topOverdue') }}</h3>
                                    <ul class="list-group list-group-flush">
                                        <li v-for="tk in state.data?.overdueTickets || []" :key="tk.ticketId"
                                            class="list-group-item d-flex justify-content-between bg-transparent px-0">
                                            <span>{{ tk.ticketNumber }} <span class="badge bg-danger ms-1">+{{ tk.overdueHours }}h</span></span>
                                            <a v-if="tk.actionUrl" :href="tk.actionUrl" class="btn btn-sm btn-outline-danger">{{ t('open') }}</a>
                                        </li>
                                        <li v-if="!state.data?.overdueTickets?.length" class="text-muted small">{{ t('none') }}</li>
                                    </ul>
                                </div>
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
