const ACTION_LABELS = {
    UserLoggedIn: 'تسجيل دخول',
    UserLoginFailed: 'فشل دخول',
    UserLoggedOut: 'تسجيل خروج',
    UserCreated: 'إنشاء مستخدم',
    UserUpdated: 'تحديث مستخدم',
    UserRolesUpdated: 'تحديث أدوار',
    RolePermissionsUpdated: 'صلاحيات دور',
    RolePermissionsCloned: 'نسخ دور',
    GlobalSettingsUpdated: 'إعدادات عامة',
    IntegrationCircuitBreakerChanged: 'قاطع تكامل (طوارئ)',
    CustomerCreated: 'إنشاء مشترك',
    CustomerUpdated: 'تحديث مشترك',
    TelecomOperationConfirmed: 'تأكيد عملية BSS',
    SubscriberSearched: 'بحث عن مشترك',
    CustomerViewed: 'اطلاع على ملف مشترك',
    NetworkCommandExecuted: 'أمر شبكة / VAS',
    BulkImportStarted: 'بدء استيراد',
    BulkImportExecuted: 'تنفيذ استيراد',
    TicketResolved: 'إغلاق تذكرة',
};

const App = {
    setup() {
        const urlParams = new URLSearchParams(window.location.search);

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
            actionOptions: Object.entries(ACTION_LABELS).map(([value, label]) => ({ value, label })),
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

        const formatDt = (utc) => {
            if (!utc) return '';
            try {
                return new Date(utc).toLocaleString('ar-SY', { dateStyle: 'short', timeStyle: 'medium' });
            } catch {
                return utc;
            }
        };

        const methods = {
            load: async () => {
                const res = await AxiosManager.get('/Security/GetUserAuditLogList', { params: buildQuery() });
                state.rows = res?.data?.content?.data || [];
                state.totalCount = res?.data?.content?.totalCount ?? state.rows.length;
            },
        };

        const handler = {
            search: async () => {
                await methods.load();
                if (!mainGrid.obj) return;
                mainGrid.obj.dataSource = state.rows;
                mainGrid.obj.refresh();
            },
            showPayload: (row) => {
                if (window.AuditDetailModal) {
                    AuditDetailModal.show(row, { actionLabels: ACTION_LABELS });
                    return;
                }
                Swal.fire({ icon: 'info', title: 'تفاصيل السجل', text: row.summaryAr || '—' });
            },
        };

        const createGrid = () => {
            mainGrid.obj = new ej.grids.Grid({
                id: 'AuditLogGrid',
                height: getDashminGridHeight(),
                dataSource: state.rows,
                allowFiltering: true,
                allowSorting: true,
                allowResizing: true,
                allowPaging: true,
                pageSettings: { pageSize: 25 },
                columns: [
                    { field: 'id', isPrimaryKey: true, visible: false },
                    {
                        field: 'occurredAtUtc',
                        headerText: 'الوقت',
                        width: 160,
                        valueAccessor: (_, data) => formatDt(data.occurredAtUtc),
                    },
                    {
                        field: 'actionType',
                        headerText: 'العملية',
                        width: 140,
                        valueAccessor: (_, data) => ACTION_LABELS[data.actionType] || data.actionType,
                    },
                    { field: 'actorDisplayName', headerText: 'المنفّذ', width: 140 },
                    { field: 'targetDisplayName', headerText: 'الهدف', width: 120 },
                    { field: 'entityType', headerText: 'كيان', width: 90 },
                    { field: 'summaryAr', headerText: 'الملخص', width: 260, minWidth: 120 },
                    { field: 'ipAddress', headerText: 'IP', width: 120 },
                    {
                        field: '_details',
                        headerText: '',
                        width: 90,
                        allowSorting: false,
                        allowFiltering: false,
                        template:
                            '<button type="button" class="btn btn-sm btn-outline-danger audit-payload-btn"><i class="bi bi-eye me-1"></i>تفاصيل</button>',
                    },
                ],
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

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['TelecomAdmin']);
                await SecurityManager.validateToken();
                await methods.load();
                createGrid();
            } catch (e) {
                console.error('AuditLogList init:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return { mainGridRef, state, handler };
    },
};

Vue.createApp(App).mount('#app');
