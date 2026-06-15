/**
 * Combined alerts + supervisor audit tab with unified scope filters.
 */
const ExecutiveAlertsPanel = (function () {
    function t(key) {
        const hit = typeof TelecomI18n !== 'undefined' && TelecomI18n.t
            ? TelecomI18n.t('executiveCommandCenter.alerts.' + key)
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

        const state = Vue.reactive({
            exceptions: null,
            digest: null,
            supervisor: null,
            loading: false,
            subTab: 'exceptions',
        });

        const severityClass = (s) => {
            const v = String(s || '').toLowerCase();
            if (v === 'critical' || v === '2') return 'bg-danger';
            if (v === 'warning' || v === '1') return 'bg-warning text-dark';
            return 'bg-secondary';
        };

        const load = async () => {
            state.loading = true;
            const params = typeof ExecutiveScopeBar !== 'undefined'
                ? ExecutiveScopeBar.getQueryParams()
                : {};
            const supParams = { ...params };
            if (!supParams.fromUtc) {
                const from = new Date();
                from.setDate(from.getDate() - 30);
                supParams.fromUtc = from.toISOString();
                supParams.toUtc = new Date().toISOString();
            }
            try {
                const [exRes, digRes, supRes] = await Promise.all([
                    AxiosManager.get('/Telecom/GetExecutiveExceptions', { params }),
                    AxiosManager.get('/Telecom/GetExecutiveWeeklyDigest', { params }),
                    AxiosManager.get('/Telecom/GetSupervisorInterventionAudit', { params: supParams }),
                ]);
                const ex = exRes?.data?.content;
                state.exceptions = ex
                    ? {
                        scopeLabelAr: pick(ex, 'scopeLabelAr', 'ScopeLabelAr'),
                        criticalCount: pick(ex, 'criticalCount', 'CriticalCount') ?? 0,
                        warningCount: pick(ex, 'warningCount', 'WarningCount') ?? 0,
                        items: pick(ex, 'items', 'Items') ?? [],
                    }
                    : { items: [] };
                const dig = digRes?.data?.content;
                state.digest = dig ? { summaryAr: pick(dig, 'summaryAr', 'SummaryAr') } : null;
                const sup = supRes?.data?.content;
                state.supervisor = sup
                    ? {
                        total: pick(sup, 'totalInterventions', 'TotalInterventions') ?? 0,
                        rows: pick(sup, 'rows', 'Rows') ?? [],
                    }
                    : { rows: [] };
            } catch (e) {
                console.error('Alerts panel failed', e);
            } finally {
                state.loading = false;
            }

            if (typeof ExecutiveExceptionsPanel !== 'undefined' && ExecutiveExceptionsPanel.connectAlerts) {
                ExecutiveExceptionsPanel.connectAlerts();
            }
        };

        document.addEventListener(ExecutiveScopeBar?.EVENT || 'executive-scope-changed', load);

        const app = Vue.createApp({
            setup() {
                Vue.onMounted(load);
                return { state, t, severityClass, load };
            },
            template: `
                <div>
                    <ul class="nav nav-tabs mb-3">
                        <li class="nav-item">
                            <button type="button" class="nav-link" :class="{ active: state.subTab === 'exceptions' }" @click="state.subTab = 'exceptions'">
                                {{ t('exceptions') }}
                                <span v-if="state.exceptions?.criticalCount" class="badge bg-danger ms-1">{{ state.exceptions.criticalCount }}</span>
                            </button>
                        </li>
                        <li class="nav-item">
                            <button type="button" class="nav-link" :class="{ active: state.subTab === 'supervisor' }" @click="state.subTab = 'supervisor'">{{ t('supervisor') }}</button>
                        </li>
                    </ul>
                    <div v-if="state.loading" class="text-center py-4"><span class="spinner-border text-danger"></span></div>
                    <div v-else-if="state.subTab === 'exceptions'">
                        <div v-if="state.digest?.summaryAr" class="alert alert-light border small mb-3">{{ state.digest.summaryAr }}</div>
                        <ul class="list-group list-group-flush strategic-panel p-0">
                            <li v-for="item in state.exceptions?.items || []" :key="item.code + (item.branchId || '')"
                                class="list-group-item d-flex justify-content-between bg-transparent py-3">
                                <div>
                                    <span class="badge me-2" :class="severityClass(item.severity)">{{ item.severity }}</span>
                                    <strong>{{ item.titleAr }}</strong>
                                    <p class="text-muted small mb-0">{{ item.detailAr }}</p>
                                </div>
                                <a v-if="item.actionUrl" :href="item.actionUrl" class="btn btn-sm btn-outline-danger">{{ t('open') }}</a>
                            </li>
                            <li v-if="!state.exceptions?.items?.length" class="list-group-item text-success bg-transparent">{{ t('allClear') }}</li>
                        </ul>
                    </div>
                    <div v-else>
                        <p class="small text-muted mb-2">{{ t('supervisorHint') }} ({{ state.supervisor?.total ?? 0 }})</p>
                        <ul class="list-group list-group-flush strategic-panel p-0">
                            <li v-for="row in state.supervisor?.rows || []" :key="row.operationId"
                                class="list-group-item bg-transparent py-2 small d-flex justify-content-between">
                                <span>{{ row.operationNumber }} — {{ row.interventionType }}</span>
                                <span class="text-muted">{{ row.branchName }}</span>
                            </li>
                            <li v-if="!state.supervisor?.rows?.length" class="list-group-item text-muted bg-transparent">{{ t('none') }}</li>
                        </ul>
                    </div>
                </div>
            `,
        });

        app.mount(containerEl);
        return { load, app };
    }

    return { mount };
})();
