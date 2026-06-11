const MsisdnInventoryApp = {
    setup() {
        const isEn = () => document.documentElement.lang?.toLowerCase().startsWith('en');

        const t = (key) => window.TelecomI18n?.t?.(`msisdnInventory.${key}`) || key;

        const state = Vue.reactive({
            loading: true,
            showDemoBanner: false,
            allRows: [],
            statusFilter: '',
            lineTypeFilter: '',
            lineTypes: [],
            quickSearch: '',
            searchPlaceholder: '',
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
            const map = {
                Available: 'available',
                Reserved: 'reserved',
                Active: 'active',
                Quarantined: 'quarantined',
                Suspended: 'suspended',
            };
            const fk = map[status];
            if (fk) {
                const hit = t(`filter.${fk}`);
                if (hit && hit !== `filter.${fk}`) return hit;
            }
            return status || '—';
        };

        const lineTypeLabel = (lt) => {
            if (!lt) return '—';
            if (isEn()) return lt.nameEn || lt.NameEn || lt.nameAr || lt.NameAr || lt.code || lt.Code || '—';
            return lt.nameAr || lt.NameAr || lt.nameEn || lt.NameEn || lt.code || lt.Code || '—';
        };

        const exposesPoolPairing = (status) =>
            status === 'Active' || status === 'Reserved' || status === 'Suspended';

        const unboundCellLabel = () => t('unboundCell');

        const resolveLineTypeLabel = (row) => {
            const typeId = pick(row, 'compatibleSubscriptionTypeId', 'CompatibleSubscriptionTypeId');
            const typeAr = pick(row, 'compatibleSubscriptionTypeNameAr', 'CompatibleSubscriptionTypeNameAr');
            const typeEn = pick(row, 'compatibleSubscriptionTypeNameEn', 'CompatibleSubscriptionTypeNameEn');
            const typeCode = pick(row, 'compatibleSubscriptionTypeCode', 'CompatibleSubscriptionTypeCode');
            if (!typeId && !typeAr && !typeEn && !typeCode) return '';
            if (isEn()) {
                return String(typeEn || typeAr || typeCode || '').trim();
            }
            return String(typeAr || typeEn || typeCode || '').trim();
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
            const lineTypeId = pick(row, 'compatibleSubscriptionTypeId', 'CompatibleSubscriptionTypeId') || '';
            const lineTypeText = resolveLineTypeLabel(row);
            const paired = exposesPoolPairing(status);
            const unbound = unboundCellLabel();
            const rawIccid = String(pick(row, 'iccid', 'Iccid') || '').trim();
            const rawImsi = String(pick(row, 'imsi', 'Imsi') || '').trim();
            const rawProduct = String(pick(row, 'productName', 'ProductName') || '').trim();
            return {
                id: pick(row, 'id', 'Id') || msisdn,
                msisdn,
                iccid: paired && rawIccid ? rawIccid : unbound,
                imsi: paired && rawImsi ? rawImsi : unbound,
                poolStatusName: status,
                statusLabel: statusLabel(status),
                pairingExposed: paired,
                assignmentLabel: subscriberName
                    ? String(subscriberName)
                    : paired
                      ? '—'
                      : t('showroomStock'),
                lineTypeLabel: lineTypeText || '—',
                lineTypeId,
                lineTypeCode: pick(row, 'compatibleSubscriptionTypeCode', 'CompatibleSubscriptionTypeCode') || '',
                productName: paired && rawProduct ? rawProduct : unbound,
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
            if (state.lineTypeFilter) {
                rows = rows.filter((r) => r.lineTypeId === state.lineTypeFilter);
            }
            const q = (state.quickSearch || '').trim().toLowerCase();
            if (!q) return rows;
            return rows.filter((r) => {
                const blob = [r.msisdn, r.iccid, r.imsi, r.assignmentLabel, r.lineTypeLabel, r.lineTypeCode]
                    .join(' ')
                    .toLowerCase();
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
            const fromHelper =
                typeof StorageManager !== 'undefined' && StorageManager.apiList
                    ? StorageManager.apiList(res)
                    : [];
            if (fromHelper.length > 0) return fromHelper;
            const content = res?.data?.content ?? res?.data?.Content;
            const list = content?.data ?? content?.Data;
            return Array.isArray(list) ? list : [];
        };

        const buildPoolListUrl = () => {
            let url = '/Telecom/GetMsisdnAssetPoolList';
            const params = [];
            if (state.lineTypeFilter) {
                params.push('subscriptionTypeId=' + encodeURIComponent(state.lineTypeFilter));
            }
            if (params.length) url += '?' + params.join('&');
            return url;
        };

        const methods = {
            loadLineTypes: async () => {
                try {
                    const res = await AxiosManager.get(
                        '/TelecomSubscriptionType/GetTelecomSubscriptionTypeList?isDeleted=false&activeOnly=true',
                        {}
                    );
                    state.lineTypes = res?.data?.content?.data || [];
                } catch {
                    state.lineTypes = [];
                }
            },
            load: async () => {
                state.loading = true;
                try {
                    const res = await AxiosManager.get(buildPoolListUrl(), {});
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
                        t('messages.loadFailed');
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({ icon: 'error', title: t('messages.loadErrorTitle'), text: msg });
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
            applyLineTypeFilter: async () => {
                await methods.load();
                await ensureGridReady();
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
                        args.cell.innerHTML = poolStatusBadgeHtml(args.data);
                    }
                    const unboundFields = ['iccid', 'imsi', 'productName'];
                    if (
                        unboundFields.includes(args.column?.field) &&
                        args.cell &&
                        args.data &&
                        !args.data.pairingExposed
                    ) {
                        args.cell.innerHTML = `<span class="inv-cell-unbound text-muted fst-italic small">${escapeHtml(
                            unboundCellLabel()
                        )}</span>`;
                    }
                    if (args.column?.field === 'lineTypeLabel' && args.cell && args.data) {
                        const label = args.data.lineTypeLabel;
                        if (!label || label === '—') {
                            args.cell.textContent = '—';
                            args.cell.classList.add('text-muted');
                        } else {
                            const code = String(args.data.lineTypeCode || '').toUpperCase();
                            let cls = 'badge bg-secondary';
                            if (code === 'PREPAID') cls = 'badge bg-success';
                            else if (code === 'POSTPAID') cls = 'badge bg-primary';
                            else if (code === 'HYBRID') cls = 'badge bg-warning text-dark';
                            args.cell.innerHTML = `<span class="${cls}">${escapeHtml(label)}</span>`;
                        }
                    }
                },
                columns: [
                    {
                        field: 'msisdn',
                        headerText: t('columns.msisdn'),
                        width: 130,
                        isPrimaryKey: true,
                    },
                    { field: 'iccid', headerText: t('columns.iccid'), width: 160 },
                    { field: 'imsi', headerText: t('columns.imsi'), width: 140 },
                    {
                        field: 'statusLabel',
                        headerText: t('columns.status'),
                        width: 130,
                    },
                    { field: 'assignmentLabel', headerText: t('columns.assignment'), width: 180 },
                    {
                        field: 'lineTypeLabel',
                        headerText: t('columns.lineType'),
                        width: 120,
                    },
                    { field: 'productName', headerText: t('columns.product'), width: 180 },
                    { field: 'releaseDisplay', headerText: t('columns.releaseDate'), width: 160 },
                ],
            });
                mainGrid.obj.appendTo(host);
                return true;
            } catch (err) {
                console.error('MsisdnInventory grid init failed', err);
                mainGrid.obj = null;
                if (typeof Swal !== 'undefined') {
                    Swal.fire({ icon: 'error', title: t('messages.gridInitFailed'), text: String(err?.message || err) });
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

        const applyLocaleUi = () => {
            state.searchPlaceholder = t('searchPh');
            const title = window.TelecomI18n?.t?.('msisdnInventory.title');
            if (title) document.title = title;
            window.TelecomI18n?.applyDom?.();
        };

        const onWindowResize = () => applyGridHeight();

        const ensurePageAccess = async () => {
            if (!StorageManager.getAccessToken?.()) {
                window.location.href = '/Accounts/Login';
                return false;
            }
            if (typeof PortalNavigation !== 'undefined' && PortalNavigation.syncOperatorSession) {
                await PortalNavigation.syncOperatorSession(true);
            }
            // Align with GetMsisdnAssetPoolList API (RolesReadTelecom): showroom/call-center may view the pool.
            const ok = await SecurityManager.authorizeTelecomAccess({
                roles: SecurityManager.TELECOM_ROLES,
                permissions: ['telecom.asset.manage', 'telecom.line.activate'],
            });
            if (!ok) {
                SecurityManager.denyPageAccess();
                return false;
            }
            return true;
        };

        Vue.onMounted(async () => {
            try {
                const allowed = await ensurePageAccess();
                if (!allowed) return;
            } catch (e) {
                console.warn('MsisdnInventory: access check failed', e);
                SecurityManager.denyPageAccess?.();
                return;
            }
            await window.TelecomI18n?.ensureLoaded?.();
            applyLocaleUi();
            state.showDemoBanner = !!(await window.TelecomIntegrationDemoBanner?.isDemoVersion?.());
            document.documentElement.addEventListener('syriatel-locale-changed', () => {
                applyLocaleUi();
                if (mainGrid.obj) {
                    mainGrid.obj.destroy();
                    mainGrid.obj = null;
                    createGrid();
                    bindGridData(false);
                }
            });
            await methods.loadLineTypes();
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

        const demoBannerTitle = Vue.computed(
            () => window.TelecomIntegrationDemoBanner?.title?.() || ''
        );
        const demoBannerMessage = Vue.computed(
            () => window.TelecomIntegrationDemoBanner?.message?.() || ''
        );
        const demoBannerChip = Vue.computed(
            () => window.TelecomIntegrationDemoBanner?.chip?.() || ''
        );

        return {
            state,
            handler,
            mainGridRef,
            t,
            kpiBadge,
            lineTypeLabel,
            demoBannerTitle,
            demoBannerMessage,
            demoBannerChip,
        };
    },
};

Vue.createApp(MsisdnInventoryApp).mount('#app');
