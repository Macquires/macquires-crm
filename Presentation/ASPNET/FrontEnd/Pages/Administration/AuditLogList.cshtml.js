const ACTION_KEYS = [
    'UserLoggedIn',
    'UserLoginFailed',
    'UserLoggedOut',
    'UserCreated',
    'UserUpdated',
    'UserRolesUpdated',
    'RolePermissionsUpdated',
    'RolePermissionsCloned',
    'GlobalSettingsUpdated',
    'IntegrationCircuitBreakerChanged',
    'CustomerCreated',
    'CustomerUpdated',
    'TelecomOperationConfirmed',
    'SubscriberSearched',
    'CustomerViewed',
    'NetworkCommandExecuted',
    'BulkImportStarted',
    'BulkImportExecuted',
    'TicketResolved',
    'StrategicReportViewed',
    'PaymentServicesReverse',
];

const auditT = (key) => window.TelecomI18n?.t?.(`administration.auditLog.actions.${key}`) || key;
const gridT = (key) => window.TelecomI18n?.t?.(`administration.auditLog.grid.${key}`) || key;

const actionLabelsMap = () => Object.fromEntries(ACTION_KEYS.map((k) => [k, auditT(k)]));
const actionOptions = () => ACTION_KEYS.map((value) => ({ value, label: auditT(value) }));

const escapeHtml = (s) =>
    String(s ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');

const contentLang = () => (document.documentElement.lang?.toLowerCase().startsWith('en') ? 'en' : 'ar');

const formatDt = (utc) => {
    if (!utc) return '';
    try {
        const loc = contentLang() === 'ar' ? 'ar-SY' : 'en-US';
        return new Date(utc).toLocaleString(loc, { dateStyle: 'short', timeStyle: 'medium' });
    } catch {
        return String(utc);
    }
};

const plainRows = (rows) => (Array.isArray(rows) ? rows.map((r) => ({ ...r })) : []);

const App = {
    setup() {
        const urlParams = new URLSearchParams(window.location.search);
        const localeTick = Vue.ref(0);

        const state = Vue.reactive({
            rows: [],
            totalCount: 0,
            filters: {
                actionType: '',
                userId: urlParams.get('userId') || '',
                customerId: urlParams.get('customerId') || '',
                fromDate: '',
                toDate: '',
            },
            actionOptions: actionOptions(),
        });

        const mainGridRef = Vue.ref(null);
        const mainGrid = { obj: null };

        const buildQuery = () => {
            const q = { take: 200, skip: 0 };
            if (state.filters.actionType) q.actionType = state.filters.actionType;
            if (state.filters.userId?.trim()) q.userId = state.filters.userId.trim();
            if (state.filters.customerId?.trim()) q.customerId = state.filters.customerId.trim();
            if (state.filters.fromDate) q.fromUtc = new Date(state.filters.fromDate + 'T00:00:00Z').toISOString();
            if (state.filters.toDate) q.toUtc = new Date(state.filters.toDate + 'T23:59:59Z').toISOString();
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
                actionLabel: auditT(actionType) !== actionType ? auditT(actionType) : actionType,
                occurredDisplay: formatDt(occurredAtUtc),
            };
        };

        const methods = {
            load: async () => {
                const res = await AxiosManager.get('/Security/GetUserAuditLogList', { params: buildQuery() });
                const content = res?.data?.content ?? res?.data?.Content;
                const rows =
                    typeof StorageManager?.apiList === 'function'
                        ? StorageManager.apiList(res)
                        : content?.data ?? content?.Data ?? [];
                state.rows = (Array.isArray(rows) ? rows : []).map(mapRow);
                state.totalCount = content?.totalCount ?? content?.TotalCount ?? state.rows.length;
            },
        };

        const auditActionBadge = (row) =>
            window.TelecomUiBadges?.auditAction(row.actionType, row.actionLabel) || escapeHtml(row.actionLabel);

        const handler = {
            search: async () => {
                await methods.load();
                if (!mainGrid.obj) return;
                mainGrid.obj.dataSource = plainRows(state.rows);
                if (typeof mainGrid.obj.dataBind === 'function') mainGrid.obj.dataBind();
                else mainGrid.obj.refresh();
            },
            showPayload: (row) => {
                localeTick.value;
                if (window.AuditDetailModal) {
                    AuditDetailModal.show(row, { actionLabels: actionLabelsMap() });
                    return;
                }
                Swal.fire({
                    icon: 'info',
                    title: window.TelecomI18n?.t?.('administration.auditLog.modal.detailsTitle') || 'تفاصيل السجل',
                    text: row.summaryAr || '—',
                });
            },
        };

        const createGrid = () => {
            localeTick.value;
            if (!mainGridRef.value || mainGrid.obj) return;
            mainGrid.obj = new ej.grids.Grid({
                id: 'AuditLogGrid',
                height: getDashminGridHeight(),
                width: '100%',
                dataSource: plainRows(state.rows),
                allowFiltering: true,
                allowSorting: true,
                allowResizing: true,
                allowPaging: true,
                filterSettings: { type: 'CheckBox' },
                pageSettings: { pageSize: 25, pageSizes: ['10', '25', '50', '100'] },
                gridLines: 'Horizontal',
                toolbar: ['Search'],
                columns: [
                    { field: 'id', isPrimaryKey: true, visible: false },
                    { field: 'occurredDisplay', headerText: gridT('time'), width: 165, minWidth: 140 },
                    {
                        field: 'actionLabel',
                        headerText: gridT('action'),
                        width: 175,
                        minWidth: 120,
                        allowFiltering: false,
                    },
                    { field: 'actorDisplayName', headerText: gridT('actor'), width: 140, minWidth: 110 },
                    { field: 'targetDisplayName', headerText: gridT('target'), width: 120, minWidth: 100 },
                    { field: 'entityType', headerText: gridT('entity'), width: 90, minWidth: 70 },
                    { field: 'summaryAr', headerText: gridT('summary'), width: 260, minWidth: 140 },
                    { field: 'ipAddress', headerText: 'IP', width: 120, minWidth: 90 },
                    {
                        field: '_details',
                        headerText: gridT('details'),
                        width: 100,
                        minWidth: 90,
                        allowSorting: false,
                        allowFiltering: false,
                        template: `<button type="button" class="btn btn-sm btn-outline-danger audit-payload-btn"><i class="bi bi-eye me-1"></i>${escapeHtml(gridT('details'))}</button>`,
                    },
                ],
                queryCellInfo: (args) => {
                    if (args?.column?.field === 'actionLabel' && args.cell) {
                        args.cell.innerHTML = auditActionBadge(args.data);
                    }
                },
                recordClick: (args) => {
                    if (args?.target?.classList?.contains('audit-payload-btn')) {
                        handler.showPayload(args.rowData);
                    }
                },
                recordDoubleClick: (args) => {
                    if (args?.rowData) handler.showPayload(args.rowData);
                },
            });
            mainGrid.obj.appendTo(mainGridRef.value);
        };

        const remapRowsForLocale = () => {
            state.rows = state.rows.map((r) => ({
                ...r,
                actionLabel: auditT(r.actionType) !== r.actionType ? auditT(r.actionType) : r.actionType,
                occurredDisplay: formatDt(r.occurredAtUtc),
            }));
        };

        const refreshGridLocale = () => {
            if (!mainGrid.obj) return;
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

        const onLocaleChanged = () => {
            localeTick.value++;
            state.actionOptions = actionOptions();
            window.TelecomI18n?.applyDom?.();
            remapRowsForLocale();
            refreshGridLocale();
        };

        Vue.onMounted(async () => {
            try {
                await window.TelecomI18n?.ensureLoaded?.();
                window.TelecomI18n?.applyDom?.();
                const pageTitle = window.TelecomI18n?.t?.('administration.auditLog.pageTitle');
                if (pageTitle) document.title = pageTitle;
                document.documentElement.addEventListener('syriatel-locale-changed', onLocaleChanged);

                await SecurityManager.authorizePage(['TelecomAdmin']);
                await SecurityManager.validateToken();
                await methods.load();
                await Vue.nextTick();
                createGrid();
            } catch (e) {
                console.error('AuditLogList init:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        Vue.onUnmounted(() => {
            document.documentElement.removeEventListener('syriatel-locale-changed', onLocaleChanged);
        });

        return { mainGridRef, state, handler };
    },
};

Vue.createApp(App).mount('#app');
