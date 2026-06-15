const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            mainTitle: null,
            id: '',
            code: '',
            nameAr: '',
            nameEn: '',
            displayColor: '',
            sortOrder: 0,
            isActive: true,
            isDefault: false,
            errors: {
                code: '',
                nameAr: '',
                nameEn: '',
            },
            isSubmitting: false,
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);

        const services = {
            getMainData: async () => {
                const response = await AxiosManager.get(
                    '/TelecomSubscriptionType/GetTelecomSubscriptionTypeList?isDeleted=false&activeOnly=false',
                    {}
                );
                return response;
            },
            create: async (body) => {
                return await AxiosManager.post('/TelecomSubscriptionType/CreateTelecomSubscriptionType', body);
            },
            update: async (body) => {
                return await AxiosManager.post('/TelecomSubscriptionType/UpdateTelecomSubscriptionType', body);
            },
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = (response?.data?.content?.data || []).map((item) => ({
                    ...item,
                    createdAtUtc: item.createdAtUtc ? new Date(item.createdAtUtc) : null,
                }));
            },
        };

        const resetFormState = () => {
            state.id = '';
            state.code = '';
            state.nameAr = '';
            state.nameEn = '';
            state.displayColor = '';
            state.sortOrder = 0;
            state.isActive = true;
            state.isDefault = false;
            state.errors = { code: '', nameAr: '', nameEn: '' };
        };

        const handler = {
            handleSubmit: async function () {
                state.errors = { code: '', nameAr: '', nameEn: '' };
                let ok = true;
                if (!state.code?.trim()) {
                    state.errors.code = 'Required';
                    ok = false;
                }
                if (!state.nameAr?.trim()) {
                    state.errors.nameAr = 'Required';
                    ok = false;
                }
                if (!state.nameEn?.trim()) {
                    state.errors.nameEn = 'Required';
                    ok = false;
                }
                if (!ok) return;

                try {
                    state.isSubmitting = true;
                    const body =
                        state.id === ''
                            ? {
                                  code: state.code.trim(),
                                  nameAr: state.nameAr.trim(),
                                  nameEn: state.nameEn.trim(),
                                  displayColor: state.displayColor?.trim() || null,
                                  sortOrder: Number(state.sortOrder) || 0,
                                  isActive: state.isActive,
                                  isDefault: state.isDefault && state.isActive,
                              }
                            : {
                                  id: state.id,
                                  code: state.code.trim(),
                                  nameAr: state.nameAr.trim(),
                                  nameEn: state.nameEn.trim(),
                                  displayColor: state.displayColor?.trim() || null,
                                  sortOrder: Number(state.sortOrder) || 0,
                                  isActive: state.isActive,
                                  isDefault: state.isDefault && state.isActive,
                              };

                    const response = state.id === '' ? await services.create(body) : await services.update(body);

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();
                        Swal.fire({
                            icon: 'success',
                            title: 'Save Successful',
                            timer: 1600,
                            showConfirmButton: false,
                        });
                        mainModal.obj.hide();
                        resetFormState();
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Save Failed',
                            text: response.data.message ?? '',
                        });
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: error.response?.data?.message ?? error.message ?? '',
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
        };

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    id: 'MainGrid',
                    height: getDashminGridHeight(),
                    dataSource: dataSource,
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
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'code', headerText: 'Code', width: 120 },
                        { field: 'nameAr', headerText: 'Name AR', width: 160 },
                        { field: 'nameEn', headerText: 'Name EN', width: 140 },
                        {
                            field: 'displayColor',
                            headerText: 'Color',
                            width: 100,
                            template:
                                '<span style="color:${displayColor}">${displayColor}</span>',
                        },
                        { field: 'sortOrder', headerText: 'Order', width: 80 },
                        {
                            field: 'isActive',
                            headerText: 'Active',
                            width: 80,
                            displayAsCheckBox: true,
                            type: 'boolean',
                        },
                        {
                            field: 'isDefault',
                            headerText: 'Default',
                            width: 90,
                            displayAsCheckBox: true,
                            type: 'boolean',
                        },
                        { field: 'createdAtUtc', headerText: 'Created', width: 150, format: 'yyyy-MM-dd HH:mm' },
                    ],
                    toolbar: [
                        'ExcelExport',
                        'Search',
                        { type: 'Separator' },
                        { text: 'Add', tooltipText: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                    ],
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom'], false);
                        mainGrid.obj.autoFitColumns(['code', 'nameAr', 'nameEn', 'displayColor', 'sortOrder']);
                    },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length === 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom'], true);
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length !== 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom'], false);
                        }
                    },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id?.endsWith('_excelexport')) {
                            mainGrid.obj.excelExport();
                        }
                        if (args.item.id === 'AddCustom') {
                            state.mainTitle = 'Add subscription type';
                            resetFormState();
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'EditCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const r = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit subscription type';
                                state.id = r.id ?? '';
                                state.code = r.code ?? '';
                                state.nameAr = r.nameAr ?? '';
                                state.nameEn = r.nameEn ?? '';
                                state.displayColor = r.displayColor ?? '';
                                state.sortOrder = r.sortOrder ?? 0;
                                state.isActive = !!r.isActive;
                                state.isDefault = !!r.isDefault;
                                mainModal.obj.show();
                            }
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
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false,
                });
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['TelecomBackOffice', 'TelecomAdmin']);
                await SecurityManager.validateToken();
                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                mainModal.create();
            } catch (e) {
                console.error('page init error:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return {
            mainGridRef,
            mainModalRef,
            state,
            handler,
        };
    },
};

Vue.createApp(App).mount('#app');
