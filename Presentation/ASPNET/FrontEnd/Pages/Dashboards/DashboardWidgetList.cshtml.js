const WIDGET_KIND_LABELS = { 0: 'Stat', 1: 'CTA', 2: 'OmniSearch', 3: 'StatusList' };

const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            providers: [],
            mainTitle: null,
            id: '',
            widgetKey: '',
            titleAr: '',
            titleEn: '',
            icon: '',
            providerKey: '',
            personasSelected: [],
            personaOptions: ['Executive', 'CallCenter', 'Retail', 'BackOffice', 'SysAdmin'],
            gridSize: 6,
            sortOrder: 0,
            widgetKind: 0,
            refreshIntervalSeconds: null,
            ctaUrl: '',
            ctaLabelAr: '',
            ctaLabelEn: '',
            isActive: true,
            isSubmitting: false,
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);

        const services = {
            getList: async () =>
                AxiosManager.get('/Dashboard/GetDashboardWidgetList?isDeleted=false&activeOnly=false', {}),
            getProviders: async () => AxiosManager.get('/Dashboard/GetRegisteredProviders', {}),
            create: async (body) => AxiosManager.post('/Dashboard/CreateDashboardWidget', body),
            update: async (body) => AxiosManager.post('/Dashboard/UpdateDashboardWidget', body),
            delete: async (body) => AxiosManager.post('/Dashboard/DeleteDashboardWidget', body),
            reorder: async (body) => AxiosManager.post('/Dashboard/ReorderDashboardWidgets', body),
        };

        function personasCsv() {
            return state.personasSelected.slice().sort().join(',');
        }

        function parsePersonas(csv) {
            if (!csv) return [];
            return csv
                .split(',')
                .map((p) => p.trim())
                .filter(Boolean);
        }

        const methods = {
            populateMainData: async () => {
                const response = await services.getList();
                state.mainData = (response?.data?.content?.data || []).map((item) => ({
                    ...item,
                    widgetKindLabel: WIDGET_KIND_LABELS[item.widgetKind] ?? item.widgetKind,
                    createdAtUtc: item.createdAtUtc ? new Date(item.createdAtUtc) : null,
                }));
            },
            loadProviders: async () => {
                try {
                    const res = await services.getProviders();
                    state.providers = res?.data?.content?.data ?? [];
                } catch (e) {
                    console.warn('providers', e);
                    state.providers = [];
                }
            },
        };

        const resetFormState = () => {
            state.id = '';
            state.widgetKey = '';
            state.titleAr = '';
            state.titleEn = '';
            state.icon = 'bi-grid';
            state.providerKey = '';
            state.personasSelected = [];
            state.gridSize = 6;
            state.sortOrder =
                state.mainData.length > 0
                    ? Math.max(...state.mainData.map((w) => w.sortOrder || 0)) + 10
                    : 10;
            state.widgetKind = 0;
            state.refreshIntervalSeconds = null;
            state.ctaUrl = '';
            state.ctaLabelAr = '';
            state.ctaLabelEn = '';
            state.isActive = true;
        };

        const fillFormFromRow = (r) => {
            state.id = r.id ?? '';
            state.widgetKey = r.widgetKey ?? '';
            state.titleAr = r.titleAr ?? '';
            state.titleEn = r.titleEn ?? '';
            state.icon = r.icon ?? '';
            state.providerKey = r.providerKey ?? '';
            state.personasSelected = parsePersonas(r.personasAllowed);
            state.gridSize = r.gridSize ?? 6;
            state.sortOrder = r.sortOrder ?? 0;
            state.widgetKind = r.widgetKind ?? 0;
            state.refreshIntervalSeconds = r.refreshIntervalSeconds ?? null;
            state.ctaUrl = r.ctaUrl ?? '';
            state.ctaLabelAr = r.ctaLabelAr ?? '';
            state.ctaLabelEn = r.ctaLabelEn ?? '';
            state.isActive = !!r.isActive;
        };

        const handler = {
            handleSubmit: async function () {
                if (!state.titleAr?.trim()) {
                    Swal.fire({ icon: 'warning', title: 'العنوان العربي مطلوب' });
                    return;
                }
                if (!state.personasSelected.length) {
                    Swal.fire({ icon: 'warning', title: 'اختر دوراً واحداً على الأقل' });
                    return;
                }

                const uid = StorageManager.getUserId();
                const personasAllowed = personasCsv();
                const refresh =
                    state.refreshIntervalSeconds === '' ||
                    state.refreshIntervalSeconds == null ||
                    isNaN(state.refreshIntervalSeconds)
                        ? null
                        : Number(state.refreshIntervalSeconds);

                try {
                    state.isSubmitting = true;
                    const body =
                        state.id === ''
                            ? {
                                  widgetKey: state.widgetKey.trim(),
                                  titleAr: state.titleAr.trim(),
                                  titleEn: state.titleEn?.trim() || null,
                                  icon: state.icon?.trim() || null,
                                  providerKey: state.providerKey?.trim() || null,
                                  personasAllowed,
                                  gridSize: Number(state.gridSize) || 6,
                                  sortOrder: Number(state.sortOrder) || 0,
                                  widgetKind: Number(state.widgetKind) || 0,
                                  refreshIntervalSeconds: refresh,
                                  ctaUrl: state.ctaUrl?.trim() || null,
                                  ctaLabelAr: state.ctaLabelAr?.trim() || null,
                                  ctaLabelEn: state.ctaLabelEn?.trim() || null,
                                  isActive: state.isActive,
                                  createdById: uid,
                              }
                            : {
                                  id: state.id,
                                  titleAr: state.titleAr.trim(),
                                  titleEn: state.titleEn?.trim() || null,
                                  icon: state.icon?.trim() || null,
                                  providerKey: state.providerKey?.trim() || null,
                                  personasAllowed,
                                  gridSize: Number(state.gridSize) || 6,
                                  sortOrder: Number(state.sortOrder) || 0,
                                  widgetKind: Number(state.widgetKind) || 0,
                                  refreshIntervalSeconds: refresh,
                                  ctaUrl: state.ctaUrl?.trim() || null,
                                  ctaLabelAr: state.ctaLabelAr?.trim() || null,
                                  ctaLabelEn: state.ctaLabelEn?.trim() || null,
                                  isActive: state.isActive,
                                  updatedById: uid,
                              };

                    const response = state.id === '' ? await services.create(body) : await services.update(body);

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();
                        Swal.fire({ icon: 'success', title: 'تم الحفظ', timer: 1400, showConfirmButton: false });
                        mainModal.obj.hide();
                        resetFormState();
                    } else {
                        Swal.fire({ icon: 'error', title: 'فشل الحفظ', text: response.data.message ?? '' });
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'خطأ',
                        text: error.response?.data?.message ?? error.message ?? '',
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
            handleDelete: async function () {
                const rows = mainGrid.obj.getSelectedRecords();
                if (!rows.length) return;
                const r = rows[0];
                const confirm = await Swal.fire({
                    icon: 'warning',
                    title: 'حذف الكرت؟',
                    text: r.widgetKey,
                    showCancelButton: true,
                    confirmButtonText: 'حذف',
                    cancelButtonText: 'إلغاء',
                });
                if (!confirm.isConfirmed) return;

                try {
                    const res = await services.delete({ id: r.id, deletedById: StorageManager.getUserId() });
                    if (res.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();
                        Swal.fire({ icon: 'success', title: 'تم الحذف', timer: 1200, showConfirmButton: false });
                    }
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'خطأ', text: error.response?.data?.message ?? error.message });
                }
            },
            moveSelected: async function (direction) {
                const rows = mainGrid.obj.getSelectedRecords();
                if (!rows.length) return;
                const sorted = [...state.mainData].sort((a, b) => (a.sortOrder || 0) - (b.sortOrder || 0));
                const idx = sorted.findIndex((x) => x.id === rows[0].id);
                const swapIdx = direction === 'up' ? idx - 1 : idx + 1;
                if (swapIdx < 0 || swapIdx >= sorted.length) return;

                const a = sorted[idx];
                const b = sorted[swapIdx];
                const aOrder = a.sortOrder;
                const bOrder = b.sortOrder;

                try {
                    await services.reorder({
                        items: [
                            { id: a.id, sortOrder: bOrder },
                            { id: b.id, sortOrder: aOrder },
                        ],
                        updatedById: StorageManager.getUserId(),
                    });
                    await methods.populateMainData();
                    mainGrid.refresh();
                } catch (error) {
                    Swal.fire({ icon: 'error', title: 'خطأ', text: error.response?.data?.message ?? error.message });
                }
            },
        };

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: getDashminGridHeight(),
                    dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    sortSettings: { columns: [{ field: 'sortOrder', direction: 'Ascending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ['10', '20', '50', '100'] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 50 },
                        { field: 'id', isPrimaryKey: true, visible: false },
                        { field: 'widgetKey', headerText: 'Key', width: 130 },
                        { field: 'titleAr', headerText: 'Title AR', width: 150 },
                        { field: 'personasAllowed', headerText: 'Personas', width: 180 },
                        { field: 'widgetKindLabel', headerText: 'Kind', width: 90 },
                        { field: 'providerKey', headerText: 'Provider', width: 130 },
                        { field: 'gridSize', headerText: 'Span', width: 70 },
                        { field: 'sortOrder', headerText: 'Order', width: 75 },
                        {
                            field: 'isActive',
                            headerText: 'Active',
                            width: 80,
                            displayAsCheckBox: true,
                            type: 'boolean',
                        },
                    ],
                    toolbar: [
                        'ExcelExport',
                        'Search',
                        { type: 'Separator' },
                        { text: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' },
                        { type: 'Separator' },
                        { text: 'Move up', prefixIcon: 'e-arrow-up', id: 'MoveUpCustom' },
                        { text: 'Move down', prefixIcon: 'e-arrow-down', id: 'MoveDownCustom' },
                    ],
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'MoveUpCustom', 'MoveDownCustom'], false);
                    },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length === 1) {
                            mainGrid.obj.toolbarModule.enableItems(
                                ['EditCustom', 'DeleteCustom', 'MoveUpCustom', 'MoveDownCustom'],
                                true
                            );
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length !== 1) {
                            mainGrid.obj.toolbarModule.enableItems(
                                ['EditCustom', 'DeleteCustom', 'MoveUpCustom', 'MoveDownCustom'],
                                false
                            );
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id?.endsWith('_excelexport')) {
                            mainGrid.obj.excelExport();
                        }
                        if (args.item.id === 'AddCustom') {
                            state.mainTitle = 'إضافة كرت لوحة';
                            resetFormState();
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'EditCustom' && mainGrid.obj.getSelectedRecords().length) {
                            state.mainTitle = 'تعديل كرت لوحة';
                            fillFormFromRow(mainGrid.obj.getSelectedRecords()[0]);
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'DeleteCustom') {
                            await handler.handleDelete();
                        }
                        if (args.item.id === 'MoveUpCustom') {
                            await handler.moveSelected('up');
                        }
                        if (args.item.id === 'MoveDownCustom') {
                            await handler.moveSelected('down');
                        }
                    },
                });
                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            },
        };

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, { backdrop: 'static', keyboard: false });
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['TelecomAdmin']);
                await SecurityManager.validateToken();
                await methods.loadProviders();
                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                mainModal.create();
            } catch (e) {
                console.error('DashboardWidgetList', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return { mainGridRef, mainModalRef, state, handler };
    },
};

Vue.createApp(App).mount('#app');
