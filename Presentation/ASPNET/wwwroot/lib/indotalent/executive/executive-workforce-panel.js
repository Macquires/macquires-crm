/**
 * Workforce performance table with role filter and employee drill-down.
 */
const ExecutiveWorkforcePanel = (function () {
    function t(key) {
        const hit = typeof TelecomI18n !== 'undefined' && TelecomI18n.t
            ? TelecomI18n.t('executiveCommandCenter.workforce.' + key)
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
            rows: [],
            scopeLabelAr: '—',
            roleFilter: 'All',
            loading: false,
            detail: null,
            detailLoading: false,
        });

        const roleOptions = [
            { value: 'All', label: t('allRoles') },
            { value: 'Showroom', label: t('showroom') },
            { value: 'BackOffice', label: t('backOffice') },
            { value: 'CallCenter', label: t('callCenter') },
        ];

        const load = async () => {
            state.loading = true;
            state.detail = null;
            try {
                const params = {
                    ...(typeof ExecutiveScopeBar !== 'undefined' ? ExecutiveScopeBar.getQueryParams() : {}),
                };
                if (state.roleFilter && state.roleFilter !== 'All') {
                    params.roleFilter = state.roleFilter;
                }
                const res = await AxiosManager.get('/Telecom/GetWorkforcePerformanceReport', { params });
                const c = res?.data?.content;
                state.scopeLabelAr = pick(c, 'scopeLabelAr', 'ScopeLabelAr') ?? '—';
                state.rows = (pick(c, 'rows', 'Rows') ?? []).map((r) => ({
                    userId: pick(r, 'userId', 'UserId'),
                    displayName: pick(r, 'displayName', 'DisplayName'),
                    orgUnitName: pick(r, 'orgUnitName', 'OrgUnitName'),
                    roles: pick(r, 'roles', 'Roles') ?? [],
                    isOnline: pick(r, 'isOnline', 'IsOnline') === true,
                    operationsCreated: pick(r, 'operationsCreated', 'OperationsCreated') ?? 0,
                    operationsConfirmed: pick(r, 'operationsConfirmed', 'OperationsConfirmed') ?? 0,
                    paymentsCompleted: pick(r, 'paymentsCompleted', 'PaymentsCompleted') ?? 0,
                    ticketsResolved: pick(r, 'ticketsResolved', 'TicketsResolved') ?? 0,
                    productivityScore: pick(r, 'productivityScore', 'ProductivityScore') ?? 0,
                }));
            } catch (e) {
                console.error('Workforce report failed', e);
            } finally {
                state.loading = false;
            }
        };

        const openDetail = async (userId) => {
            state.detailLoading = true;
            try {
                const params = {
                    userId,
                    ...(typeof ExecutiveScopeBar !== 'undefined' ? ExecutiveScopeBar.getQueryParams() : {}),
                };
                const res = await AxiosManager.get('/Telecom/GetEmployeePerformanceDetail', { params });
                const c = res?.data?.content;
                state.detail = c
                    ? {
                        displayName: pick(c, 'displayName', 'DisplayName'),
                        orgUnitName: pick(c, 'orgUnitName', 'OrgUnitName'),
                        isOnline: pick(c, 'isOnline', 'IsOnline'),
                        kpis: pick(c, 'kpis', 'Kpis'),
                        timeline: pick(c, 'timeline', 'Timeline') ?? [],
                    }
                    : null;
            } finally {
                state.detailLoading = false;
            }
        };

        const exportCsv = () => {
            if (!state.rows.length) return;
            const header = ['Name', 'Branch', 'Score', 'Ops', 'Tickets'];
            const lines = [header.join(',')].concat(
                state.rows.map((r) =>
                    [r.displayName, r.orgUnitName, r.productivityScore, r.operationsCreated, r.ticketsResolved]
                        .map((c) => `"${String(c ?? '').replace(/"/g, '""')}"`)
                        .join(',')
                )
            );
            const blob = new Blob(['\uFEFF' + lines.join('\n')], { type: 'text/csv;charset=utf-8;' });
            const a = document.createElement('a');
            a.href = URL.createObjectURL(blob);
            a.download = `workforce-${new Date().toISOString().slice(0, 10)}.csv`;
            a.click();
        };

        document.addEventListener(ExecutiveScopeBar?.EVENT || 'executive-scope-changed', load);

        const app = Vue.createApp({
            setup() {
                Vue.onMounted(load);
                return { state, t, roleOptions, load, openDetail, exportCsv };
            },
            template: `
                <div>
                    <div class="d-flex flex-wrap gap-2 mb-3 align-items-center">
                        <select class="form-select form-select-sm strategic-select w-auto" v-model="state.roleFilter" @change="load">
                            <option v-for="o in roleOptions" :key="o.value" :value="o.value">{{ o.label }}</option>
                        </select>
                        <button type="button" class="btn btn-sm btn-outline-secondary" @click="load" :disabled="state.loading">{{ t('refresh') }}</button>
                        <button type="button" class="btn btn-sm btn-outline-success" @click="exportCsv" :disabled="!state.rows.length">{{ t('exportCsv') }}</button>
                        <span class="small text-muted ms-auto">{{ t('scopePrefix') }} {{ state.scopeLabelAr }}</span>
                    </div>
                    <div v-if="state.loading" class="text-center py-4"><span class="spinner-border text-danger"></span></div>
                    <div v-else class="table-responsive strategic-panel p-0">
                        <table class="table table-sm table-hover mb-0 align-middle">
                            <thead class="table-light">
                                <tr>
                                    <th>{{ t('employee') }}</th>
                                    <th>{{ t('branch') }}</th>
                                    <th class="text-end">{{ t('score') }}</th>
                                    <th class="text-end">{{ t('ops') }}</th>
                                    <th class="text-end">{{ t('tickets') }}</th>
                                    <th></th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="r in state.rows" :key="r.userId">
                                    <td>
                                        <span class="badge me-1" :class="r.isOnline ? 'bg-success' : 'bg-secondary'">●</span>
                                        {{ r.displayName }}
                                    </td>
                                    <td class="small text-muted">{{ r.orgUnitName || '—' }}</td>
                                    <td class="text-end fw-bold text-danger">{{ r.productivityScore }}</td>
                                    <td class="text-end">{{ r.operationsCreated }}</td>
                                    <td class="text-end">{{ r.ticketsResolved }}</td>
                                    <td class="text-end">
                                        <button type="button" class="btn btn-sm btn-outline-danger" @click="openDetail(r.userId)">{{ t('detail') }}</button>
                                    </td>
                                </tr>
                                <tr v-if="!state.rows.length"><td colspan="6" class="text-muted text-center py-4">{{ t('noEmployees') }}</td></tr>
                            </tbody>
                        </table>
                    </div>
                    <div v-if="state.detail || state.detailLoading" class="strategic-panel p-3 mt-3">
                        <div v-if="state.detailLoading" class="text-center py-3"><span class="spinner-border spinner-border-sm"></span></div>
                        <div v-else-if="state.detail">
                            <h4 class="h6 mb-2">{{ state.detail.displayName }} — {{ state.detail.orgUnitName }}</h4>
                            <div class="d-flex flex-wrap gap-2 mb-3 small">
                                <span class="badge bg-light text-dark border">{{ t('ops7d') }}: {{ state.detail.kpis?.operationsCreated7d }}</span>
                                <span class="badge bg-light text-dark border">{{ t('ops30d') }}: {{ state.detail.kpis?.operationsCreated30d }}</span>
                                <span class="badge bg-light text-dark border">{{ t('score30d') }}: {{ state.detail.kpis?.productivityScore30d }}</span>
                            </div>
                            <ul class="list-group list-group-flush small">
                                <li v-for="(ev, i) in state.detail.timeline" :key="i"
                                    class="list-group-item bg-transparent px-0 d-flex justify-content-between">
                                    <span>{{ ev.titleAr }}</span>
                                    <span class="text-muted">{{ (ev.occurredAtUtc || '').slice(0, 16).replace('T', ' ') }}</span>
                                </li>
                            </ul>
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
