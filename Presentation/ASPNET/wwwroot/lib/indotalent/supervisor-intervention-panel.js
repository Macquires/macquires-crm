/**
 * Supervisor intervention audit — GET /Telecom/GetSupervisorInterventionAudit
 */
const SupervisorInterventionPanel = (function () {
    const MIS_PERMISSION = 'telecom.reports.mis';

    function t(key) {
        return (typeof TelecomI18n !== 'undefined' && TelecomI18n.t
            ? TelecomI18n.t('defaultDashboard.supervisor.' + key)
            : null) || key;
    }

    function hasPermission() {
        const perms = StorageManager.getPermissions?.() || [];
        return StorageManager.hasAnyPermission?.(perms, [MIS_PERMISSION]);
    }

    function pick(o, ...keys) {
        for (const k of keys) {
            if (o?.[k] !== undefined && o?.[k] !== null) return o[k];
        }
        return undefined;
    }

    function mount(rootEl) {
        const state = Vue.reactive({ data: null, loading: false });

        const load = async () => {
            state.loading = true;
            try {
                const params = typeof ExecutiveScopeBar !== 'undefined'
                    ? ExecutiveScopeBar.getQueryParams()
                    : {};
                if (!params.fromUtc) {
                    const from = new Date();
                    from.setDate(from.getDate() - 30);
                    params.fromUtc = from.toISOString();
                    params.toUtc = new Date().toISOString();
                }
                const res = await AxiosManager.get('/Telecom/GetSupervisorInterventionAudit', { params });
                const c = res?.data?.content;
                state.data = {
                    scopeLabelAr: pick(c, 'scopeLabelAr', 'ScopeLabelAr') ?? '',
                    total: pick(c, 'totalInterventions', 'TotalInterventions') ?? 0,
                    bypass: pick(c, 'bypassCount', 'BypassCount') ?? 0,
                    approved: pick(c, 'approvedCount', 'ApprovedCount') ?? 0,
                    rows: (pick(c, 'rows', 'Rows') ?? []).map((r) => ({
                        operationNumber: pick(r, 'operationNumber', 'OperationNumber'),
                        kind: pick(r, 'kind', 'Kind'),
                        branchName: pick(r, 'branchName', 'BranchName'),
                        interventionType: pick(r, 'interventionType', 'InterventionType'),
                        overrideReasonCode: pick(r, 'overrideReasonCode', 'OverrideReasonCode'),
                        barringLevel: pick(r, 'barringLevel', 'BarringLevel'),
                        status: pick(r, 'status', 'Status'),
                    })),
                };
            } catch (e) {
                console.error('SupervisorInterventionAudit failed', e);
                state.data = { scopeLabelAr: '—', rows: [] };
            } finally {
                state.loading = false;
            }
        };

        const app = Vue.createApp({
            setup() {
                Vue.onMounted(load);
                return { ...Vue.toRefs(state), t, load };
            },
            template: `
            <div>
            <header class="strategic-exec-hero mb-3">
                <p class="strategic-exec-kicker mb-1">{{ t('kicker') }}</p>
                <h2 class="h5 fw-bold text-syriatel-red">{{ t('title') }}</h2>
                <p class="small text-muted mb-0">{{ t('scopePrefix') }} <strong>{{ data?.scopeLabelAr || '—' }}</strong></p>
            </header>
            <div class="d-flex flex-wrap gap-2 mb-3">
                <span class="badge bg-danger px-3 py-2">{{ t('total') }}: {{ data?.total ?? 0 }}</span>
                <span class="badge bg-warning text-dark px-3 py-2">{{ t('bypass') }}: {{ data?.bypass ?? 0 }}</span>
                <span class="badge bg-success px-3 py-2">{{ t('approved') }}: {{ data?.approved ?? 0 }}</span>
                <button type="button" class="btn btn-sm btn-outline-secondary ms-auto" @click="load" :disabled="loading">{{ t('refresh') }}</button>
            </div>
            <div class="table-responsive strategic-panel">
                <table class="table table-sm table-hover mb-0">
                    <thead><tr>
                        <th>{{ t('colOp') }}</th><th>{{ t('colBranch') }}</th><th>{{ t('colType') }}</th><th>{{ t('colReason') }}</th><th>{{ t('colStatus') }}</th>
                    </tr></thead>
                    <tbody>
                        <tr v-if="loading"><td colspan="5" class="text-center text-muted">{{ t('loading') }}</td></tr>
                        <tr v-else-if="!data?.rows?.length"><td colspan="5" class="text-center text-muted">{{ t('empty') }}</td></tr>
                        <tr v-for="(r, i) in data?.rows || []" :key="i">
                            <td class="font-monospace small">{{ r.operationNumber }}</td>
                            <td>{{ r.branchName || '—' }}</td>
                            <td>{{ r.interventionType }}</td>
                            <td>{{ r.overrideReasonCode || r.barringLevel || '—' }}</td>
                            <td>{{ r.status }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>
            </div>`,
        });
        app.mount(rootEl);
        return app;
    }

    async function initFromDashboard() {
        if (!hasPermission()) return null;
        const section = document.getElementById('supervisor-interventions');
        const root = document.getElementById('supervisorInterventionApp');
        if (!section || !root) return null;
        section.classList.remove('d-none');
        if (window.location.hash === '#supervisor-interventions') {
            section.scrollIntoView({ behavior: 'smooth' });
        }
        await SecurityManager.validateToken();
        return mount(root);
    }

    return { initFromDashboard, hasPermission };
})();
