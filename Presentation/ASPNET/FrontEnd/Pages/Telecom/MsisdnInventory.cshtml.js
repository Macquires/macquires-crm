const MsisdnInventoryApp = {
    setup() {
        if (typeof SecurityManager !== 'undefined' && !SecurityManager.canAccessPortalPath(window.location.pathname)) {
            SecurityManager.denyPageAccess();
        }

        const isEn = () => document.documentElement.lang?.toLowerCase().startsWith('en');

        const t = (key, fallbackAr, fallbackEn) => {
            const fb = isEn() ? fallbackEn || fallbackAr : fallbackAr;
            try {
                const parts = key.split('.');
                let node = window.__telecomLocale?.telecom;
                for (const p of parts) {
                    node = node?.[p];
                }
                return node || fb;
            } catch {
                return fb;
            }
        };

        const state = Vue.reactive({
            loading: true,
            allRows: [],
            statusFilter: '',
            quickSearch: '',
            searchPlaceholder: isEn() ? 'MSISDN, ICCID…' : 'بحث MSISDN أو ICCID…',
            kpis: { total: 0, available: 0, reserved: 0, active: 0, quarantined: 0 },
            pageSize: 50,
        });

        const mainGridRef = Vue.ref(null);
        const mainGrid = { obj: null };

        const getGridBox = () => document.querySelector('.inv-grid-panel .grid-container');

        const computeGridHeight = () => {
            if (typeof computeTelecomGridHeight === 'function') {
                return computeTelecomGridHeight('.inv-grid-panel');
            }
            const box = getGridBox();
            const h = box?.clientHeight ?? 0;
            return h > 120 ? h : 400;
        };

        const pick = (o, ...keys) => {
            if (!o) return undefined;
            for (const k of keys) {
                if (o[k] !== undefined && o[k] !== null) return o[k];
            }
            return undefined;
        };

        const poolStatusFromEnum = (n) => {
            const names = ['Available', 'Reserved', 'Active', 'Suspended', 'Quarantined'];
            const idx = Number(n);
            return Number.isInteger(idx) && idx >= 0 && idx < names.length ? names[idx] : '';
        };

        const rowStatus = (row) => {
            const name = pick(row, 'poolStatusName', 'PoolStatusName');
            if (name) return String(name);
            return poolStatusFromEnum(pick(row, 'poolStatus', 'PoolStatus'));
        };

        const formatDt = (utc) => {
            if (!utc) return '';
            try {
                const loc = isEn() ? 'en-US' : 'ar-SY';
                return new Date(utc).toLocaleString(loc, { dateStyle: 'medium', timeStyle: 'short' });
            } catch {
                return String(utc);
            }
        };

        const statusLabel = (status) => {
            const ar = {
                Available: 'متاح',
                Reserved: 'محجوز',
                Active: 'نشط',
                Quarantined: 'حجر',
                Suspended: 'موقوف',
            };
            const en = {
                Available: 'Available',
                Reserved: 'Reserved',
                Active: 'Active',
                Quarantined: 'Quarantined',
                Suspended: 'Suspended',
            };
            const map = isEn() ? en : ar;
            return map[status] || status || '—';
        };

        const mapRow = (row) => {
            const status = rowStatus(row);
            const reserved = pick(row, 'reservedUntilUtc', 'ReservedUntilUtc');
            const quarantine = pick(row, 'quarantineEndsUtc', 'QuarantineEndsUtc');
            let releaseDisplay = '—';
            if (status === 'Reserved' && reserved) releaseDisplay = formatDt(reserved);
            else if (quarantine) releaseDisplay = formatDt(quarantine);

            const subscriberName = pick(row, 'subscriberName', 'SubscriberName');
            const msisdn = pick(row, 'msisdn', 'Msisdn') || '—';
            return {
                id: pick(row, 'id', 'Id') || msisdn,
                msisdn,
                iccid: pick(row, 'iccid', 'Iccid') || '—',
                imsi: pick(row, 'imsi', 'Imsi') || '—',
                poolStatusName: status,
                statusLabel: statusLabel(status),
                assignmentLabel: subscriberName
                    ? String(subscriberName)
                    : t('msisdnInventory.showroomStock', 'بضاعة في المعرض', 'Showroom stock'),
                productName: pick(row, 'productName', 'ProductName') || '—',
                releaseDisplay,
            };
        };

        const recomputeKpis = (rows) => {
            state.kpis.total = rows.length;
            state.kpis.available = rows.filter((r) => r.poolStatusName === 'Available').length;
            state.kpis.reserved = rows.filter((r) => r.poolStatusName === 'Reserved').length;
            state.kpis.active = rows.filter((r) => r.poolStatusName === 'Active').length;
            state.kpis.quarantined = rows.filter((r) => r.poolStatusName === 'Quarantined').length;
        };

        const filteredRows = () => {
            let rows = state.allRows;
            if (state.statusFilter) {
                rows = rows.filter((r) => r.poolStatusName === state.statusFilter);
            }
            const q = (state.quickSearch || '').trim().toLowerCase();
            if (!q) return rows;
            return rows.filter((r) => {
                const blob = [r.msisdn, r.iccid, r.imsi, r.assignmentLabel].join(' ').toLowerCase();
                return blob.includes(q);
            });
        };

        const poolStatusBadgeHtml = (row) => {
            if (!row || typeof row !== 'object') return '—';
            const html = window.TelecomUiBadges?.poolStatus(row.poolStatusName, row.statusLabel);
            if (typeof html === 'string' && html.length > 0) return html;
            return String(row.statusLabel || row.poolStatusName || '—');
        };

        const syncPageSizeToGrid = () => {
            if (!mainGrid.obj?.pageSettings) return;
            const size = Number(state.pageSize) || 50;
            mainGrid.obj.pageSettings.pageSize = size;
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

        const parsePoolList = (res) => {
            const fromHelper = StorageManager.apiList(res);
            if (fromHelper.length > 0) return fromHelper;
            const content = res?.data?.content ?? res?.data?.Content;
            const list = content?.data ?? content?.Data;
            return Array.isArray(list) ? list : [];
        };

        const methods = {
            load: async () => {
                state.loading = true;
                try {
                    const res = await AxiosManager.get('/Telecom/GetMsisdnAssetPoolList', {});
                    const raw = parsePoolList(res);
                    state.allRows = raw.map(mapRow);
                    recomputeKpis(state.allRows);
                } catch (e) {
                    state.allRows = [];
                    recomputeKpis([]);
                    const msg =
                        e?.response?.data?.message ||
                        e?.response?.data?.error?.message ||
                        e?.message ||
                        'تعذر تحميل مستودع الأرقام';
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({ icon: 'error', title: 'خطأ في التحميل', text: msg });
                    } else {
                        console.error('MsisdnInventory load failed', e);
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
            applyFilter: () => {
                bindGridData(true);
            },
            changePageSize: () => {
                if (!mainGrid.obj) return;
                syncPageSizeToGrid();
                mainGrid.obj.pageSettings.currentPage = 1;
                if (typeof mainGrid.obj.goToPage === 'function') {
                    mainGrid.obj.goToPage(1);
                }
                if (typeof mainGrid.obj.refresh === 'function') {
                    mainGrid.obj.refresh();
                } else if (typeof mainGrid.obj.dataBind === 'function') {
                    mainGrid.obj.dataBind();
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
                mainGridRef.value || document.querySelector('.inv-grid-panel .grid-container > div');
            if (!host || mainGrid.obj) return false;

            try {
                mainGrid.obj = new ej.grids.Grid({
                height: computeGridHeight(),
                dataSource: filteredRows(),
                width: '100%',
                allowPaging: true,
                allowSorting: true,
                allowResizing: true,
                gridLines: 'Horizontal',
                emptyRecordTemplate: '<div class="text-center py-4 text-muted">لا توجد أرقام مطابقة</div>',
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
                        args.cell.innerHTML = poolStatusBadgeHtml(args.data);
                    }
                },
                columns: [
                    {
                        field: 'msisdn',
                        headerText: t('msisdnInventory.columns.msisdn', 'رقم الموبايل', 'MSISDN'),
                        width: 130,
                        isPrimaryKey: true,
                    },
                    { field: 'iccid', headerText: t('msisdnInventory.columns.iccid', 'ICCID', 'ICCID'), width: 160 },
                    { field: 'imsi', headerText: t('msisdnInventory.columns.imsi', 'IMSI', 'IMSI'), width: 140 },
                    {
                        field: 'statusLabel',
                        headerText: t('msisdnInventory.columns.status', 'الحالة', 'Status'),
                        width: 130,
                    },
                    { field: 'assignmentLabel', headerText: t('msisdnInventory.columns.assignment', 'الارتباط', 'Assignment'), width: 180 },
                    { field: 'productName', headerText: t('msisdnInventory.columns.product', 'الباقة / المنتج', 'Package'), width: 180 },
                    { field: 'releaseDisplay', headerText: t('msisdnInventory.columns.releaseDate', 'فك الحجز / الحجر', 'Release / quarantine'), width: 160 },
                ],
            });
                mainGrid.obj.appendTo(host);
                return true;
            } catch (err) {
                console.error('MsisdnInventory grid init failed', err);
                mainGrid.obj = null;
                if (typeof Swal !== 'undefined') {
                    Swal.fire({ icon: 'error', title: 'تعذر عرض الجدول', text: String(err?.message || err) });
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
                        Swal.fire({ icon: 'error', title: 'مكتبة الجداول غير محمّلة' });
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

        const loadTelecomLocale = async () => {
            try {
                const lang = isEn() ? 'en' : 'ar';
                const res = await fetch(`/locales/telecom.${lang}.json`);
                if (res.ok) window.__telecomLocale = await res.json();
            } catch {
                /* optional */
            }
        };

        const onWindowResize = () => applyGridHeight();

        Vue.onMounted(async () => {
            try {
                if (typeof PortalNavigation !== 'undefined' && PortalNavigation.syncOperatorSession) {
                    await PortalNavigation.syncOperatorSession(true);
                }
            } catch (e) {
                console.warn('MsisdnInventory: session sync failed', e);
            }
            await loadTelecomLocale();
            await methods.load();
            await ensureGridReady();
            window.addEventListener('resize', onWindowResize);
            if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
        });

        Vue.onUnmounted(() => {
            window.removeEventListener('resize', onWindowResize);
        });

        const kpiBadge = (status, count) => {
            const label = `${count}`;
            return window.TelecomUiBadges?.poolStatus(status, label) || label;
        };

        return { state, handler, mainGridRef, kpiBadge };
    },
};

Vue.createApp(MsisdnInventoryApp).mount('#app');
