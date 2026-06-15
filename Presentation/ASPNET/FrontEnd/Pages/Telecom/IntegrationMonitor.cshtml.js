const waitForSyncfusion = async (maxMs = 8000) => {
    const step = 100;
    let waited = 0;
    while (typeof ej === 'undefined' || !ej.grids) {
        if (waited >= maxMs) return false;
        await new Promise((r) => setTimeout(r, step));
        waited += step;
    }
    return true;
};

const plainRows = (rows) => (Array.isArray(rows) ? rows.map((r) => ({ ...r })) : []);

const App = {
    setup() {
        const localeTick = Vue.ref(0);
        const ti = (key) => {
            localeTick.value;
            return window.TelecomI18n?.t?.(`integrationMonitor.${key}`) || key;
        };

        const state = Vue.reactive({
            rows: [],
            totalCount: 0,
            healthItems: [],
            liveEvents: [],
            filters: {
                msisdn: '',
                integrationSystem: '',
                fromDate: '',
                toDate: '',
            },
        });

        const mainGridRef = Vue.ref(null);
        const liveConnected = Vue.ref(false);
        let liveConnection = null;
        const mainGrid = { obj: null };

        const contentLang = () =>
            document.documentElement.lang?.toLowerCase().startsWith('en') ? 'en' : 'ar';

        const formatDt = (utc) => {
            if (!utc) return '—';
            try {
                const loc = contentLang() === 'ar' ? 'ar-SY' : 'en-US';
                return new Date(utc).toLocaleString(loc, { dateStyle: 'short', timeStyle: 'medium' });
            } catch {
                return String(utc);
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

        const mapRow = (r) => {
            const occurredAtUtc = r.occurredAtUtc ?? r.OccurredAtUtc;
            const isSuccess = r.isSuccess ?? r.IsSuccess;
            return {
                id: r.id ?? r.Id,
                msisdn: r.msisdn ?? r.Msisdn,
                integrationSystem: r.integrationSystem ?? r.IntegrationSystem,
                operationName: r.operationName ?? r.OperationName,
                requestPayload: r.requestPayload ?? r.RequestPayload,
                responsePayload: r.responsePayload ?? r.ResponsePayload,
                executionTimeMs: r.executionTimeMs ?? r.ExecutionTimeMs,
                isSuccess,
                responseStatusCode: r.responseStatusCode ?? r.ResponseStatusCode,
                occurredAtUtc,
                occurredDisplay: formatDt(occurredAtUtc),
                resultLabel: isSuccess ? ti('grid.success') : ti('grid.fail'),
            };
        };

        const parseRows = (res) => {
            const fromHelper =
                typeof StorageManager?.apiList === 'function' ? StorageManager.apiList(res) : [];
            if (fromHelper.length) return fromHelper.map(mapRow);
            const content = res?.data?.content ?? res?.data?.Content;
            const raw = content?.data ?? content?.Data ?? [];
            return (Array.isArray(raw) ? raw : []).map(mapRow);
        };

        const parseTotal = (res, rowCount) => {
            const content = res?.data?.content ?? res?.data?.Content;
            return content?.totalCount ?? content?.TotalCount ?? rowCount;
        };

        const healthBadge = (item) => {
            const short = item.mode === 'live' ? 'Live' : 'Fallback';
            return window.TelecomUiBadges?.integrationHealth(item.mode, short) || short;
        };

        const methods = {
            loadHealth: async () => {
                try {
                    const res = await AxiosManager.get('/Telecom/GetIntegrationHealthStatus');
                    state.healthItems = res?.data?.content?.items ?? res?.data?.content?.Items ?? [];
                } catch {
                    state.healthItems = [];
                }
            },
            load: async () => {
                try {
                    const res = await AxiosManager.get('/Telecom/GetIntegrationLogList', { params: buildQuery() });
                    state.rows = parseRows(res);
                    state.totalCount = parseTotal(res, state.rows.length);
                } catch (e) {
                    console.error('IntegrationMonitor load:', e);
                    state.rows = [];
                    state.totalCount = 0;
                }
            },
        };

        const escapeHtml = (s) =>
            String(s)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;');

        const bindGrid = () => {
            if (!mainGrid.obj) return;
            const rows = plainRows(state.rows);
            mainGrid.obj.dataSource = rows;
            applyGridHeight();
            if (typeof mainGrid.obj.dataBind === 'function') mainGrid.obj.dataBind();
            else mainGrid.obj.refresh();
        };

        const handler = {
            search: async () => {
                await Promise.all([methods.loadHealth(), methods.load()]);
                bindGrid();
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

        const applyGridHeight = () => {
            if (!mainGrid.obj) return;
            const h =
                typeof computeTelecomGridHeight === 'function'
                    ? computeTelecomGridHeight('.integration-grid-panel')
                    : Math.max(320, Math.min(480, (window.innerHeight || 800) - 360));
            mainGrid.obj.height = h;
        };

        const paintCells = (args) => {
            if (!args?.cell || !args?.data) return;
            const row = args.data;
            if (args.column.field === 'resultLabel') {
                args.cell.innerHTML =
                    window.TelecomUiBadges?.resultSuccess(row.isSuccess) ||
                    escapeHtml(row.resultLabel || '—');
            } else if (args.column.field === 'responseStatusCode') {
                args.cell.innerHTML =
                    window.TelecomUiBadges?.responseStatus(row.responseStatusCode) ||
                    escapeHtml(row.responseStatusCode || '—');
            }
        };

        const resolveGridHost = () => mainGridRef.value || document.querySelector('.integration-grid-panel .grid-container > div');

        const createGrid = () => {
            localeTick.value;
            const host = resolveGridHost();
            if (!host || mainGrid.obj) return;
            const gridHeight =
                typeof computeTelecomGridHeight === 'function'
                    ? computeTelecomGridHeight('.integration-grid-panel')
                    : Math.max(320, Math.min(480, (window.innerHeight || 800) - 360));
            mainGrid.obj = new ej.grids.Grid({
                id: 'IntegrationLogGrid',
                height: gridHeight,
                width: '100%',
                dataSource: plainRows(state.rows),
                allowPaging: true,
                allowSorting: true,
                allowResizing: true,
                gridLines: 'Horizontal',
                pageSettings: { pageSize: 25, pageSizes: ['10', '25', '50', '100'] },
                columns: [
                    { field: 'id', isPrimaryKey: true, visible: false },
                    { field: 'occurredDisplay', headerText: ti('grid.time'), width: 165, minWidth: 140 },
                    { field: 'integrationSystem', headerText: ti('grid.system'), width: 130, minWidth: 100 },
                    { field: 'operationName', headerText: ti('grid.operation'), width: 150, minWidth: 110 },
                    { field: 'msisdn', headerText: 'MSISDN', width: 120, minWidth: 100 },
                    { field: 'resultLabel', headerText: ti('grid.result'), width: 100, minWidth: 80, allowSorting: false },
                    { field: 'executionTimeMs', headerText: 'ms', width: 70, minWidth: 60, textAlign: 'Right' },
                    { field: 'responseStatusCode', headerText: 'Status', width: 130, minWidth: 100, allowSorting: false },
                    {
                        field: '_details',
                        headerText: ti('grid.details'),
                        width: 95,
                        minWidth: 85,
                        allowSorting: false,
                        template: `<button type="button" class="btn btn-sm btn-outline-telecom integration-payload-btn">${escapeHtml(ti('grid.view'))}</button>`,
                    },
                ],
                queryCellInfo: paintCells,
                recordClick: (args) => {
                    if (args?.target?.classList?.contains('integration-payload-btn')) {
                        handler.showPayload(args.rowData);
                    }
                },
            });
            mainGrid.obj.appendTo(host);
        };

        const connectLiveFeed = async () => {
            if (typeof signalR === 'undefined') return;
            try {
                liveConnection = new signalR.HubConnectionBuilder()
                    .withUrl('/hubs/integration-live')
                    .withAutomaticReconnect()
                    .build();

                liveConnection.on('integrationEvent', (ev) => {
                    const atUtc = ev.atUtc ?? ev.AtUtc ?? new Date().toISOString();
                    state.liveEvents.unshift({
                        atUtc,
                        atDisplay: formatDt(atUtc),
                        system: ev.system ?? ev.System ?? '—',
                        operation: ev.operation ?? ev.Operation ?? '—',
                        msisdn: ev.msisdn ?? ev.Msisdn,
                        message: ev.message ?? ev.Message ?? '',
                        success: ev.success ?? ev.Success ?? true,
                    });
                    if (state.liveEvents.length > 50) state.liveEvents.pop();
                });

                liveConnection.onreconnected(() => { liveConnected.value = true; });
                liveConnection.onclose(() => { liveConnected.value = false; });

                await liveConnection.start();
                await liveConnection.invoke('JoinIntegrationConsole');
                liveConnected.value = true;
            } catch (e) {
                console.warn('IntegrationMonitor: SignalR live feed unavailable', e);
                liveConnected.value = false;
            }
        };

        const onLocaleChanged = () => {
            localeTick.value++;
            window.TelecomI18n?.applyDom?.();
            if (!mainGrid.obj) return;
            state.rows = state.rows.map((r) => ({
                ...r,
                occurredDisplay: formatDt(r.occurredAtUtc),
                resultLabel: r.isSuccess ? ti('grid.success') : ti('grid.fail'),
            }));
            const rows = plainRows(state.rows);
            mainGrid.obj.destroy();
            mainGrid.obj = null;
            createGrid();
            if (mainGrid.obj) {
                mainGrid.obj.dataSource = rows;
                if (typeof mainGrid.obj.dataBind === 'function') mainGrid.obj.dataBind();
                else mainGrid.obj.refresh();
            }
        };

        Vue.onMounted(async () => {
            try {
                if (typeof PortalNavigation !== 'undefined' && PortalNavigation.syncOperatorSession) {
                    await PortalNavigation.syncOperatorSession(true);
                }
            } catch (e) {
                console.warn('IntegrationMonitor: session sync failed', e);
            }

            try {
                await window.TelecomI18n?.ensureLoaded?.();
                const title = window.TelecomI18n?.t?.('integrationMonitor.pageTitle');
                if (title) document.title = title;
                window.TelecomI18n?.applyDom?.();
                document.documentElement.addEventListener('syriatel-locale-changed', onLocaleChanged);

                await SecurityManager.authorizePage(['TelecomAdmin', 'TelecomManagement', 'TelecomBackOffice']);
                await SecurityManager.validateToken?.();

                await Promise.all([methods.loadHealth(), methods.load(), connectLiveFeed()]);

                const sfReady = await waitForSyncfusion();
                if (!sfReady) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Grid library not ready',
                        text: 'Reload the page or check Syncfusion scripts.',
                    });
                    return;
                }

                await Vue.nextTick();
                createGrid();
                bindGrid();
                requestAnimationFrame(() => applyGridHeight());
                const onResize = () => applyGridHeight();
                window.addEventListener('resize', onResize);
                Vue.onUnmounted(() => window.removeEventListener('resize', onResize));
            } catch (e) {
                console.error('IntegrationMonitor init:', e);
            } finally {
                if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
            }
        });

        Vue.onUnmounted(() => {
            document.documentElement.removeEventListener('syriatel-locale-changed', onLocaleChanged);
            if (liveConnection) liveConnection.stop();
        });

        return { state, handler, mainGridRef, healthBadge, ti, liveConnected };
    },
};

try {
    Vue.createApp(App).mount('#app');
} catch (mountErr) {
    console.error('IntegrationMonitor mount failed:', mountErr);
    document.getElementById('app')?.removeAttribute('v-cloak');
    if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
}
