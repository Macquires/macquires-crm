(function () {
    const BackOfficeHistoricalLedgerApp = {
        setup() {
            const state = Vue.reactive({
                loading: false,
                filters: { searchTerm: '', domain: '', slaBreached: '', fromDate: '', toDate: '' },
                rows: [],
                totalCount: 0,
            });

            const mainGridRef = Vue.ref(null);
            const mainGrid = { obj: null };

            const buildQuery = () => {
                const q = { take: 100, skip: 0, includeResolved: true };
                if (state.filters.searchTerm) q.searchTerm = state.filters.searchTerm;
                if (state.filters.domain !== '') q.domain = parseInt(state.filters.domain);
                if (state.filters.slaBreached !== '') q.slaBreached = state.filters.slaBreached === 'true';
                // Note: Backend might need to be updated to support these extra filters in GetPendingRequests
                return q;
            };

            const methods = {
                hasPermission: (key) => {
                    const perms = StorageManager.getPermissions?.() || [];
                    return perms.includes(key);
                },
                load: async () => {
                    if (!methods.hasPermission('Permission.Finance.Bdr.View') && !methods.hasPermission('admin.audit.view')) {
                        Swal.fire({ icon: 'error', title: 'Security Violation: You do not possess the required compliance permissions to view the historical ledger.' });
                        return;
                    }
                    state.loading = true;
                    try {
                        const res = await AxiosManager.get('/TelecomBackOffice/GetPendingRequests', {
                            params: buildQuery(),
                        });
                        const content = res?.data?.content ?? res?.data?.Content;
                        state.rows = content?.data ?? content?.Data ?? [];
                        state.totalCount = content?.total ?? content?.Total ?? state.rows.length;
                    } catch (e) {
                        console.error('BackOfficeHistoricalLedger load:', e);
                    } finally {
                        state.loading = false;
                    }
                },
            };

            const formatDt = (utc) => {
                if (!utc) return '—';
                return new Date(utc).toLocaleString();
            };

            const createGrid = () => {
                if (!mainGridRef.value || mainGrid.obj) return;
                mainGrid.obj = new ej.grids.Grid({
                    id: 'BackOfficeHistoricalLedgerGrid',
                    height: 500,
                    width: '100%',
                    dataSource: state.rows,
                    allowPaging: true,
                    allowSorting: true,
                    allowFiltering: true,
                    pageSettings: { pageSize: 20 },
                    columns: [
                        { field: 'number', headerText: 'الطلب', width: 120 },
                        { field: 'kindNameAr', headerText: 'النوع', width: 150 },
                        { field: 'msisdn', headerText: 'الخط', width: 120 },
                        { field: 'subscriberName', headerText: 'المشترك', width: 180 },
                        { 
                            field: 'pipelineState', 
                            headerText: 'الحالة النهائية', 
                            width: 150,
                            template: (data) => {
                                const isSuccess = data.pipelineState === 'Completed' || data.pipelineState === 'Approved_Pending_Cash';
                                const cls = isSuccess ? 'bg-success' : 'bg-danger';
                                return `<span class="badge ${cls}">${data.pipelineState}</span>`;
                            }
                        },
                        { 
                            field: 'auditorDisplayName', 
                            headerText: 'المدقق', 
                            width: 150,
                            template: (data) => data.auditorDisplayName || data.updatedById || '—'
                        },
                        { 
                            field: 'updatedAtUtc', 
                            headerText: 'تاريخ البت', 
                            width: 160,
                            template: (data) => formatDt(data.updatedAtUtc)
                        },
                        { 
                            field: 'slaBreached', 
                            headerText: 'SLA', 
                            width: 100,
                            template: (data) => data.slaBreached ? '<span class="badge bg-danger">BREACHED</span>' : '<span class="badge bg-success">OK</span>'
                        }
                    ],
                });
                mainGrid.obj.appendTo(mainGridRef.value);
            };

            const handler = {
                search: async () => {
                    await methods.load();
                    if (mainGrid.obj) {
                        mainGrid.obj.dataSource = state.rows;
                        mainGrid.obj.refresh();
                    }
                },
            };

            Vue.onMounted(async () => {
                await methods.load();
                createGrid();
                if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
            });

            return { state, handler, mainGridRef };
        },
    };

    Vue.createApp(BackOfficeHistoricalLedgerApp).mount('#app');
})();
