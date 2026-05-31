const ACTION_LABELS = {
    SubscriberSearched: 'بحث عن مشترك',
    CustomerViewed: 'اطلاع على ملف مشترك',
    NetworkCommandExecuted: 'أمر شبكة (HLR)',
    TicketResolved: 'إغلاق تذكرة',
    BulkImportStarted: 'بدء استيراد',
    BulkImportExecuted: 'تنفيذ استيراد',
    TelecomOperationConfirmed: 'تأكيد عملية BSS',
};

const ACTION_OPTIONS = Object.entries(ACTION_LABELS).map(([value, label]) => ({ value, label }));

const escapeHtml = (s) =>
    String(s ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');

const formatDt = (utc) => {
    if (!utc) return '';
    try {
        return new Date(utc).toLocaleString('ar-SY', { dateStyle: 'short', timeStyle: 'medium' });
    } catch {
        return String(utc);
    }
};

const plainRows = (rows) => (Array.isArray(rows) ? rows.map((r) => ({ ...r })) : []);

const parseAuditListResponse = (res) => {
    const content = res?.data?.content ?? res?.data?.Content;
    const rows =
        typeof StorageManager?.apiList === 'function'
            ? StorageManager.apiList(res)
            : content?.data ?? content?.Data ?? [];
    const summary = content?.summary ?? content?.Summary;
    return {
        rows: Array.isArray(rows) ? rows : [],
        totalCount: content?.totalCount ?? content?.TotalCount ?? (Array.isArray(rows) ? rows.length : 0),
        summary: {
            logsToday: summary?.logsToday ?? summary?.LogsToday ?? 0,
            networkCommandsToday: summary?.networkCommandsToday ?? summary?.NetworkCommandsToday ?? 0,
            totalMatching: summary?.totalMatching ?? summary?.TotalMatching ?? 0,
        },
    };
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

const formatPayloadPre = (row) => {
    if (row?.payloadJson) {
        try {
            return JSON.stringify(JSON.parse(row.payloadJson), null, 2);
        } catch {
            return String(row.payloadJson);
        }
    }
    return row?.summaryAr ? String(row.summaryAr) : '{}';
};

const BackOfficeAuditListApp = {
    setup() {
        const state = Vue.reactive({
            loading: false,
            filters: { searchTerm: '', actionType: '', fromDate: '', toDate: '' },
            summary: { logsToday: 0, networkCommandsToday: 0, totalMatching: 0 },
            rows: [],
            totalCount: 0,
            actionOptions: ACTION_OPTIONS,
        });

        const mainGridRef = Vue.ref(null);
        const mainGrid = { obj: null };
        let auditDrawer = null;

        const buildQuery = () => {
            const q = { take: 200, skip: 0 };
            const term = (state.filters.searchTerm || '').trim();
            if (term.length >= 2) q.searchTerm = term;
            if (state.filters.actionType) q.actionType = state.filters.actionType;
            if (state.filters.fromDate) {
                q.fromUtc = new Date(state.filters.fromDate + 'T00:00:00.000Z').toISOString();
            }
            if (state.filters.toDate) {
                q.toUtc = new Date(state.filters.toDate + 'T23:59:59.999Z').toISOString();
            }
            return q;
        };

        const mapRow = (r) => {
            const actionType = r.actionType ?? r.ActionType;
            const occurredAtUtc = r.occurredAtUtc ?? r.OccurredAtUtc;
            return {
                id: r.id ?? r.Id,
                userId: r.userId ?? r.UserId,
                actorUserId: r.actorUserId ?? r.ActorUserId,
                actionType,
                entityType: r.entityType ?? r.EntityType,
                entityId: r.entityId ?? r.EntityId,
                summaryAr: r.summaryAr ?? r.SummaryAr,
                payloadJson: r.payloadJson ?? r.PayloadJson,
                occurredAtUtc,
                ipAddress: r.ipAddress ?? r.IpAddress,
                actorDisplayName: r.actorDisplayName ?? r.ActorDisplayName,
                targetDisplayName: r.targetDisplayName ?? r.TargetDisplayName,
                actionLabel: ACTION_LABELS[actionType] || actionType,
                occurredDisplay: formatDt(occurredAtUtc),
            };
        };

        const methods = {
            load: async () => {
                state.loading = true;
                try {
                    const res = await AxiosManager.get('/TelecomBackOffice/GetBackOfficeAuditLogList', {
                        params: buildQuery(),
                    });
                    const parsed = parseAuditListResponse(res);
                    state.rows = parsed.rows.map(mapRow);
                    state.totalCount = parsed.totalCount ?? state.rows.length;
                    state.summary.logsToday = parsed.summary.logsToday;
                    state.summary.networkCommandsToday = parsed.summary.networkCommandsToday;
                    state.summary.totalMatching = parsed.summary.totalMatching || state.totalCount;
                } catch (e) {
                    console.error('BackOfficeAuditList load:', e);
                    state.rows = [];
                    state.totalCount = 0;
                } finally {
                    state.loading = false;
                }
            },
        };

        const openDetail = (row) => {
            if (!row) return;
            if (window.AuditDetailModal) {
                AuditDetailModal.show(row, { actionLabels: ACTION_LABELS });
                return;
            }
            const titleEl = document.getElementById('auditDrawerTitle');
            const timeEl = document.getElementById('auditDrawerTime');
            const preEl = document.getElementById('auditDrawerPre');
            if (titleEl) titleEl.textContent = row.actionLabel || row.actionType || '—';
            if (timeEl) timeEl.textContent = row.occurredAtUtc ? new Date(row.occurredAtUtc).toISOString() : '—';
            if (preEl) preEl.textContent = formatPayloadPre(row);
            if (!auditDrawer) {
                const el = document.getElementById('auditDetailDrawer');
                if (el) auditDrawer = new bootstrap.Offcanvas(el);
            }
            auditDrawer?.show();
        };

        const auditActionBadge = (row) =>
            window.TelecomUiBadges?.auditAction(row.actionType, row.actionLabel) || escapeHtml(row.actionLabel);

        const applyGridHeight = () => {
            if (!mainGrid.obj) return;
            const h =
                typeof computeTelecomGridHeight === 'function'
                    ? computeTelecomGridHeight('.bo-audit-grid-panel')
                    : 420;
            mainGrid.obj.height = h;
        };

        const createGrid = () => {
            if (!mainGridRef.value || mainGrid.obj || typeof ej === 'undefined' || !ej.grids) return;
            const gridHeight =
                typeof computeTelecomGridHeight === 'function'
                    ? computeTelecomGridHeight('.bo-audit-grid-panel')
                    : 420;
            mainGrid.obj = new ej.grids.Grid({
                id: 'BackOfficeAuditGrid',
                height: gridHeight,
                width: '100%',
                dataSource: plainRows(state.rows),
                allowPaging: true,
                allowSorting: true,
                allowFiltering: true,
                allowResizing: true,
                allowReordering: true,
                filterSettings: { type: 'Menu' },
                pageSettings: {
                    pageSize: 25,
                    pageSizes: [25, 50, 100, 200],
                },
                queryCellInfo: (args) => {
                    if (args?.column?.field === 'actionLabel' && args.cell) {
                        args.cell.innerHTML = auditActionBadge(args.data);
                    }
                },
                columns: [
                    { field: 'id', isPrimaryKey: true, visible: false },
                    { field: 'occurredDisplay', headerText: 'الوقت', width: 165 },
                    {
                        field: 'actionLabel',
                        headerText: 'العملية',
                        width: 175,
                        allowFiltering: false,
                    },
                    { field: 'summaryAr', headerText: 'الملخص', width: 300, minWidth: 140 },
                    { field: 'ipAddress', headerText: 'IP', width: 120 },
                ],
                recordDoubleClick: (args) => openDetail(args?.rowData),
            });
            mainGrid.obj.appendTo(mainGridRef.value);
        };

        const handler = {
            search: async () => {
                await methods.load();
                if (!mainGrid.obj) return;
                mainGrid.obj.dataSource = plainRows(state.rows);
                applyGridHeight();
                if (typeof mainGrid.obj.dataBind === 'function') mainGrid.obj.dataBind();
                else mainGrid.obj.refresh();
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['TelecomBackOffice', 'TelecomAdmin', 'TelecomManagement']);
                await SecurityManager.validateToken();
                await methods.load();
                await Vue.nextTick();
                await waitForSyncfusion();
                createGrid();
                requestAnimationFrame(() => {
                    applyGridHeight();
                    if (typeof mainGrid.obj?.dataBind === 'function') mainGrid.obj.dataBind();
                });
                const onResize = () => applyGridHeight();
                window.addEventListener('resize', onResize);
                Vue.onUnmounted(() => window.removeEventListener('resize', onResize));
            } catch (e) {
                console.error('BackOfficeAuditList init:', e);
            } finally {
                if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
            }
        });

        return { state, handler, mainGridRef };
    },
};

Vue.createApp(BackOfficeAuditListApp).mount('#app');
