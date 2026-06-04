const DeviceInventoryApp = {
    setup() {
        if (typeof SecurityManager !== 'undefined' && !SecurityManager.canAccessPortalPath(window.location.pathname)) {
            SecurityManager.denyPageAccess();
        }

        const isEn = () => document.documentElement.lang?.toLowerCase().startsWith('en');
        const t = (key) => window.TelecomI18n?.t?.(`deviceInventory.${key}`) || key;

        const state = Vue.reactive({
            loading: true,
            busy: false,
            allRows: [],
            statusFilter: '',
            quickSearch: '',
            searchPlaceholder: '',
            pageSize: 50,
            form: { imei: '', model: '', listPrice: 0, branchId: '' },
            kpis: { total: 0, available: 0, reserved: 0, sold: 0, quarantined: 0 },
        });

        const mainGridRef = Vue.ref(null);
        const mainGrid = { obj: null };

        const escapeHtml = (s) =>
            String(s ?? '')
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;');

        const pick = (o, ...keys) => {
            if (!o) return undefined;
            for (const k of keys) {
                if (o[k] !== undefined && o[k] !== null) return o[k];
            }
            return undefined;
        };

        const computeGridHeight = () => {
            if (typeof computeTelecomGridHeight === 'function') {
                return computeTelecomGridHeight('.dev-grid-panel');
            }
            const box = document.querySelector('.dev-grid-panel .grid-container');
            const h = box?.clientHeight ?? 0;
            return h > 120 ? h : 400;
        };

        const formatMoney = (n) => {
            if (n == null || isNaN(n)) return '—';
            const loc = isEn() ? 'en-US' : 'ar-SY';
            return new Intl.NumberFormat(loc, { maximumFractionDigits: 0 }).format(Number(n));
        };

        const statusLabel = (status) => {
            const key = `status.${status}`;
            const hit = t(key);
            return hit && hit !== key ? hit : status || '—';
        };

        const mapRow = (row) => {
            const status = String(pick(row, 'status', 'Status') || '');
            const branch = pick(row, 'branchId', 'BranchId');
            return {
                id: pick(row, 'id', 'Id') || pick(row, 'imei', 'Imei'),
                imei: pick(row, 'imei', 'Imei') || '—',
                model: pick(row, 'model', 'Model') || '—',
                sku: pick(row, 'sku', 'Sku') || '—',
                listPrice: pick(row, 'listPrice', 'ListPrice'),
                listPriceDisplay: formatMoney(pick(row, 'listPrice', 'ListPrice')),
                statusName: status,
                statusLabel: statusLabel(status),
                branchDisplay: branch ? String(branch) : '—',
            };
        };

        const recomputeKpis = (rows) => {
            state.kpis.total = rows.length;
            state.kpis.available = rows.filter((r) => r.statusName === 'Available').length;
            state.kpis.reserved = rows.filter((r) => r.statusName === 'Reserved').length;
            state.kpis.sold = rows.filter((r) => r.statusName === 'Sold').length;
            state.kpis.quarantined = rows.filter((r) => r.statusName === 'Quarantined').length;
        };

        const filteredRows = () => {
            let rows = state.allRows;
            if (state.statusFilter) {
                rows = rows.filter((r) => r.statusName === state.statusFilter);
            }
            const q = (state.quickSearch || '').trim().toLowerCase();
            if (!q) return rows;
            return rows.filter((r) => {
                const blob = [r.imei, r.model, r.sku, r.branchDisplay].join(' ').toLowerCase();
                return blob.includes(q);
            });
        };

        const statusBadgeHtml = (row) => {
            if (!row) return '—';
            const html = window.TelecomUiBadges?.poolStatus(row.statusName, row.statusLabel);
            if (typeof html === 'string' && html.length > 0) return html;
            return escapeHtml(row.statusLabel);
        };

        const syncPageSizeToGrid = () => {
            if (!mainGrid.obj?.pageSettings) return;
            mainGrid.obj.pageSettings.pageSize = Number(state.pageSize) || 50;
        };

        const bindGridData = (resetPage = false) => {
            if (!mainGrid.obj) return;
            const rows = filteredRows();
            syncPageSizeToGrid();
            mainGrid.obj.dataSource = rows;
            if (resetPage && mainGrid.obj.pageSettings) {
                mainGrid.obj.pageSettings.currentPage = 1;
                if (typeof mainGrid.obj.goToPage === 'function') {
                    mainGrid.obj.goToPage(1);
                }
            }
            if (typeof mainGrid.obj.dataBind === 'function') {
                mainGrid.obj.dataBind();
            } else if (typeof mainGrid.obj.refresh === 'function') {
                mainGrid.obj.refresh();
            }
        };

        const applyGridHeight = () => {
            if (!mainGrid.obj) return;
            mainGrid.obj.height = computeGridHeight();
        };

        const parseList = (res) => {
            const fromHelper = typeof StorageManager !== 'undefined' && StorageManager.apiList
                ? StorageManager.apiList(res)
                : [];
            if (fromHelper.length > 0) return fromHelper;
            const content = res?.data?.content ?? res?.data?.Content;
            const list = content?.data ?? content?.Data;
            return Array.isArray(list) ? list : [];
        };

        const getGridColumns = () => [
            { field: 'imei', headerText: t('columns.imei'), width: 150, isPrimaryKey: true },
            { field: 'model', headerText: t('columns.model'), width: 160, minWidth: 100 },
            { field: 'sku', headerText: t('columns.sku'), width: 100, minWidth: 80 },
            { field: 'listPriceDisplay', headerText: t('columns.listPrice'), width: 110, textAlign: 'Right' },
            { field: 'statusLabel', headerText: t('columns.status'), width: 120, minWidth: 100 },
            { field: 'branchDisplay', headerText: t('columns.branch'), width: 120, minWidth: 90 },
        ];

        const methods = {
            load: async () => {
                state.loading = true;
                try {
                    const res = await AxiosManager.get('/Telecom/GetDeviceInventoryList', {});
                    const raw = parseList(res);
                    state.allRows = raw.map(mapRow);
                    recomputeKpis(state.allRows);
                } catch (e) {
                    state.allRows = [];
                    recomputeKpis([]);
                    const msg =
                        e?.response?.data?.message ||
                        e?.response?.data?.error?.message ||
                        e?.message ||
                        t('messages.loadFailed');
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({ icon: 'error', title: t('messages.loadErrorTitle'), text: msg });
                    }
                } finally {
                    state.loading = false;
                }
            },
        };

        const handler = {
            refresh: async () => {
                await methods.load();
                await ensureGridReady();
            },
            applyFilter: () => bindGridData(true),
            changePageSize: () => {
                if (!mainGrid.obj) return;
                syncPageSizeToGrid();
                mainGrid.obj.pageSettings.currentPage = 1;
                if (typeof mainGrid.obj.goToPage === 'function') {
                    mainGrid.obj.goToPage(1);
                }
                mainGrid.obj.refresh?.() || mainGrid.obj.dataBind?.();
            },
            createDevice: async () => {
                const imei = (state.form.imei || '').trim();
                const model = (state.form.model || '').trim();
                if (!imei) {
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({ icon: 'warning', text: t('messages.imeiRequired') });
                    }
                    return;
                }
                if (!model) {
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({ icon: 'warning', text: t('messages.modelRequired') });
                    }
                    return;
                }
                state.busy = true;
                try {
                    await AxiosManager.post('/Telecom/CreateDeviceInventory', {
                        imei,
                        model,
                        listPrice: state.form.listPrice,
                        branchId: state.form.branchId?.trim() || null,
                        createdById: StorageManager.getUserId(),
                    });
                    state.form.imei = '';
                    state.form.model = '';
                    state.form.branchId = '';
                    state.form.listPrice = 0;
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'success',
                            title: t('messages.createOk'),
                            timer: 1800,
                            showConfirmButton: false,
                        });
                    }
                    await methods.load();
                    bindGridData(true);
                } catch (e) {
                    const msg = e?.response?.data?.message || e?.message;
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({ icon: 'error', text: msg });
                    }
                } finally {
                    state.busy = false;
                }
            },
        };

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

        const createGrid = () => {
            const host =
                mainGridRef.value || document.querySelector('.dev-grid-panel .grid-container > div');
            if (!host || mainGrid.obj) return false;

            try {
                mainGrid.obj = new ej.grids.Grid({
                    id: 'DeviceInventoryGrid',
                    height: computeGridHeight(),
                    dataSource: filteredRows(),
                    width: '100%',
                    allowPaging: true,
                    allowSorting: true,
                    allowResizing: true,
                    allowTextWrap: true,
                    gridLines: 'Horizontal',
                    emptyRecordTemplate: `<div class="text-center py-4 text-muted">${escapeHtml(t('messages.emptyGrid'))}</div>`,
                    pageSettings: {
                        currentPage: 1,
                        pageSize: state.pageSize,
                        pageSizes: ['25', '50', '100', '200'],
                    },
                    actionComplete: (args) => {
                        if (args.requestType === 'paging' && mainGrid.obj?.pageSettings?.pageSize) {
                            const size = Number(mainGrid.obj.pageSettings.pageSize);
                            if (size > 0 && size !== state.pageSize) {
                                state.pageSize = size;
                            }
                        }
                    },
                    queryCellInfo: (args) => {
                        if (args.column?.field === 'statusLabel' && args.cell && args.data) {
                            args.cell.innerHTML = statusBadgeHtml(args.data);
                        }
                    },
                    columns: getGridColumns(),
                });
                mainGrid.obj.appendTo(host);
                return true;
            } catch (err) {
                console.error('DeviceInventory grid init failed', err);
                mainGrid.obj = null;
                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        icon: 'error',
                        title: t('messages.gridInitFailed'),
                        text: String(err?.message || err),
                    });
                }
                return false;
            }
        };

        const ensureGridReady = async () => {
            await Vue.nextTick();
            let attempts = 0;
            while (!mainGridRef.value && attempts < 10) {
                await new Promise((r) => setTimeout(r, 50));
                attempts += 1;
            }
            if (!mainGrid.obj) {
                const ok = await waitForSyncfusion();
                if (!ok) {
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({ icon: 'error', title: t('messages.syncfusionMissing') });
                    }
                    return;
                }
                createGrid();
            }
            requestAnimationFrame(() => {
                applyGridHeight();
                bindGridData(false);
            });
        };

        const refreshGridI18n = () => {
            if (!mainGrid.obj) return;
            mainGrid.obj.columns = getGridColumns();
            mainGrid.obj.emptyRecordTemplate = `<div class="text-center py-4 text-muted">${escapeHtml(t('messages.emptyGrid'))}</div>`;
            bindGridData(false);
        };

        const applyLocaleUi = () => {
            state.searchPlaceholder = t('searchPh');
            const title = t('pageTitle');
            if (title && title !== 'pageTitle') document.title = title;
            window.TelecomI18n?.applyDom?.();
        };

        const onWindowResize = () => applyGridHeight();

        Vue.onMounted(async () => {
            await window.TelecomI18n?.ensureLoaded?.();
            applyLocaleUi();
            document.documentElement.addEventListener('syriatel-locale-changed', () => {
                applyLocaleUi();
                state.allRows = state.allRows.map((r) => {
                    const status = r.statusName;
                    return {
                        ...r,
                        statusLabel: statusLabel(status),
                        listPriceDisplay: formatMoney(r.listPrice),
                    };
                });
                if (mainGrid.obj) {
                    refreshGridI18n();
                } else {
                    ensureGridReady();
                }
            });
            await methods.load();
            await ensureGridReady();
            window.addEventListener('resize', onWindowResize);
            if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
        });

        Vue.onUnmounted(() => {
            window.removeEventListener('resize', onWindowResize);
            if (mainGrid.obj) {
                mainGrid.obj.destroy();
                mainGrid.obj = null;
            }
        });

        const kpiBadge = (status, count) => {
            const label = `${count}`;
            return window.TelecomUiBadges?.poolStatus(status, label) || label;
        };

        return { state, handler, mainGridRef, kpiBadge };
    },
};

Vue.createApp(DeviceInventoryApp).mount('#app');
