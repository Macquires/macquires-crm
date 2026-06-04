const App = {
    setup() {
        if (typeof SecurityManager !== 'undefined' && !SecurityManager.canAccessPortalPath(window.location.pathname)) {
            SecurityManager.denyPageAccess();
        }

        const localeTick = Vue.ref(0);
        const ti = (key) => {
            localeTick.value;
            return window.TelecomI18n?.t?.(`integrationMonitor.${key}`) || key;
        };

        const state = Vue.reactive({
            rows: [],
            totalCount: 0,
            healthItems: [],
            filters: {
                msisdn: '',
                integrationSystem: '',
                fromDate: '',
                toDate: '',
            },
        });

        const mainGridRef = Vue.ref(null);
        const mainGrid = { obj: null };

        const contentLang = () =>
            document.documentElement.lang?.toLowerCase().startsWith('en') ? 'en' : 'ar';

        const formatDt = (utc) => {
            if (!utc) return '';
            try {
                const loc = contentLang() === 'ar' ? 'ar-SY' : 'en-US';
                return new Date(utc).toLocaleString(loc, { dateStyle: 'short', timeStyle: 'medium' });
            } catch {
                return utc;
            }
        };

        const buildQuery = () => {
            const q = { take: 200, skip: 0 };
            if (state.filters.msisdn?.trim()) q.msisdn = state.filters.msisdn.trim();
            if (state.filters.integrationSystem) q.integrationSystem = state.filters.integrationSystem;
            if (state.filters.fromDate) q.fromUtc = new Date(state.filters.fromDate + 'T00:00:00Z').toISOString();
            if (state.filters.toDate) q.toUtc = new Date(state.filters.toDate + 'T23:59:59Z').toISOString();
            return q;
        };

        const statusBadge = (code) =>
            window.TelecomUiBadges?.responseStatus(code) || code || '—';

        const healthBadge = (item) => {
            const short =
                item.mode === 'live'
                    ? 'Live'
                    : 'Fallback';
            return window.TelecomUiBadges?.integrationHealth(item.mode, short) || short;
        };

        const methods = {
            loadHealth: async () => {
                try {
                    const res = await AxiosManager.get('/Telecom/GetIntegrationHealthStatus');
                    state.healthItems = res?.data?.content?.items || [];
                } catch {
                    state.healthItems = [];
                }
            },
            load: async () => {
                const res = await AxiosManager.get('/Telecom/GetIntegrationLogList', { params: buildQuery() });
                state.rows = res?.data?.content?.data || [];
                state.totalCount = res?.data?.content?.totalCount ?? state.rows.length;
            },
        };

        const handler = {
            search: async () => {
                await Promise.all([methods.loadHealth(), methods.load()]);
                if (!mainGrid.obj) return;
                mainGrid.obj.dataSource = state.rows;
                mainGrid.obj.refresh();
            },
            showPayload: (row) => {
                const req = row.requestPayload || '—';
                const res = row.responsePayload || '—';
                Swal.fire({
                    title: row.operationName || ti('payloadTitle'),
                    html: `<div class="text-start small" dir="ltr"><p class="fw-semibold">${escapeHtml(ti('request'))}</p><pre class="bg-light border rounded p-2" style="max-height:160px;overflow:auto;white-space:pre-wrap">${escapeHtml(req)}</pre><p class="fw-semibold mt-2">${escapeHtml(ti('response'))}</p><pre class="bg-light border rounded p-2" style="max-height:160px;overflow:auto;white-space:pre-wrap">${escapeHtml(res)}</pre></div>`,
                    width: 720,
                    confirmButtonText: ti('close'),
                });
            },
        };

        const escapeHtml = (s) =>
            String(s)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;');

        const createGrid = () => {
            mainGrid.obj = new ej.grids.Grid({
                id: 'IntegrationLogGrid',
                height: getDashminGridHeight(),
                dataSource: state.rows,
                allowPaging: true,
                allowSorting: true,
                allowResizing: true,
                pageSettings: { pageSize: 25 },
                columns: [
                    {
                        field: 'occurredAtUtc',
                        headerText: ti('grid.time'),
                        width: 160,
                        template: (d) => formatDt(d.occurredAtUtc),
                    },
                    { field: 'integrationSystem', headerText: ti('grid.system'), width: 120 },
                    { field: 'operationName', headerText: ti('grid.operation'), width: 140 },
                    { field: 'msisdn', headerText: 'MSISDN', width: 120 },
                    {
                        field: 'isSuccess',
                        headerText: ti('grid.result'),
                        width: 90,
                        template: (d) =>
                            window.TelecomUiBadges?.resultSuccess(d.isSuccess) ||
                            (d.isSuccess ? ti('grid.success') : ti('grid.fail')),
                    },
                    { field: 'executionTimeMs', headerText: 'ms', width: 70 },
                    {
                        field: 'responseStatusCode',
                        headerText: 'Status',
                        width: 130,
                        template: (d) => statusBadge(d.responseStatusCode),
                    },
                    {
                        headerText: ti('grid.details'),
                        width: 90,
                        template: () =>
                            `<button type="button" class="btn btn-sm btn-outline-telecom integration-payload-btn">${escapeHtml(ti('grid.view'))}</button>`,
                    },
                ],
                recordClick: (args) => {
                    if (args?.target?.classList?.contains('integration-payload-btn')) {
                        handler.showPayload(args.rowData);
                    }
                },
            });
            mainGrid.obj.appendTo(mainGridRef.value);
        };

        const onLocaleChanged = () => {
            localeTick.value++;
            window.TelecomI18n?.applyDom?.();
            if (!mainGrid.obj) return;
            const rows = state.rows;
            mainGrid.obj.destroy();
            mainGrid.obj = null;
            createGrid();
            mainGrid.obj.dataSource = rows;
            mainGrid.obj.refresh();
        };

        Vue.onMounted(async () => {
            await window.TelecomI18n?.ensureLoaded?.();
            const title = window.TelecomI18n?.t?.('integrationMonitor.pageTitle');
            if (title) document.title = title;
            window.TelecomI18n?.applyDom?.();
            document.documentElement.addEventListener('syriatel-locale-changed', onLocaleChanged);
            await Promise.all([methods.loadHealth(), methods.load()]);
            createGrid();
            if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
        });

        Vue.onUnmounted(() => {
            document.documentElement.removeEventListener('syriatel-locale-changed', onLocaleChanged);
        });

        return { state, handler, mainGridRef, healthBadge, ti };
    },
};

Vue.createApp(App).mount('#app');
