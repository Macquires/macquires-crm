const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            mainTitle: null,
            id: '',
            serviceCode: '',
            nameAr: '',
            nameEn: '',
            description: '',
            monthlyFee: 0,
            isActive: true,
            hlrCommandTemplate: '',
            sortOrder: 0,
            errors: { serviceCode: '', nameAr: '', hlrCommandTemplate: '' },
            isSubmitting: false,
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);

        const services = {
            getMainData: async () => {
                return await AxiosManager.get('/Vas/GetValueAddedServiceList?isDeleted=false&activeOnly=false', {});
            },
            create: async (body) => AxiosManager.post('/Vas/CreateValueAddedService', body),
            update: async (body) => AxiosManager.post('/Vas/UpdateValueAddedService', body),
            remove: async (body) => AxiosManager.post('/Vas/DeleteValueAddedService', body),
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
            state.serviceCode = '';
            state.nameAr = '';
            state.nameEn = '';
            state.description = '';
            state.monthlyFee = 0;
            state.isActive = true;
            state.hlrCommandTemplate = '';
            state.sortOrder = 0;
            state.errors = { serviceCode: '', nameAr: '', hlrCommandTemplate: '' };
        };

        const handler = {
            handleSubmit: async function () {
                state.errors = { serviceCode: '', nameAr: '', hlrCommandTemplate: '' };
                let ok = true;
                if (!state.serviceCode?.trim() && !state.id) {
                    state.errors.serviceCode = 'Required';
                    ok = false;
                }
                if (!state.nameAr?.trim()) {
                    state.errors.nameAr = 'Required';
                    ok = false;
                }
                if (!state.hlrCommandTemplate?.trim()) {
                    state.errors.hlrCommandTemplate = 'Required';
                    ok = false;
                }
                if (!ok) return;

                try {
                    state.isSubmitting = true;
                    const body =
                        state.id === ''
                            ? {
                                  serviceCode: state.serviceCode.trim(),
                                  nameAr: state.nameAr.trim(),
                                  nameEn: state.nameEn?.trim() || null,
                                  description: state.description?.trim() || null,
                                  monthlyFee: Number(state.monthlyFee) || 0,
                                  isActive: state.isActive,
                                  hlrCommandTemplate: state.hlrCommandTemplate.trim(),
                                  sortOrder: Number(state.sortOrder) || 0,
                              }
                            : {
                                  id: state.id,
                                  nameAr: state.nameAr.trim(),
                                  nameEn: state.nameEn?.trim() || null,
                                  description: state.description?.trim() || null,
                                  monthlyFee: Number(state.monthlyFee) || 0,
                                  isActive: state.isActive,
                                  hlrCommandTemplate: state.hlrCommandTemplate.trim(),
                                  sortOrder: Number(state.sortOrder) || 0,
                              };

                    const response = state.id === '' ? await services.create(body) : await services.update(body);

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();
                        Swal.fire({ icon: 'success', title: 'Saved', timer: 1600, showConfirmButton: false });
                        mainModal.obj.hide();
                        resetFormState();
                    } else {
                        Swal.fire({ icon: 'error', title: 'Save failed', text: response.data.message ?? '' });
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: error.response?.data?.error?.message ?? error.response?.data?.message ?? error.message ?? '',
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
            handleDelete: async function () {
                if (!mainGrid.obj?.getSelectedRecords()?.length) return;
                const r = mainGrid.obj.getSelectedRecords()[0];
                const confirm = await Swal.fire({
                    icon: 'warning',
                    title: 'Delete VAS service?',
                    text: r.serviceCode,
                    showCancelButton: true,
                });
                if (!confirm.isConfirmed) return;
                try {
                    await services.remove({ id: r.id });
                    await methods.populateMainData();
                    mainGrid.refresh();
                    Swal.fire({ icon: 'success', title: 'Deleted', timer: 1200, showConfirmButton: false });
                } catch (e) {
                    Swal.fire({ icon: 'error', title: 'Delete failed', text: e.message });
                }
            },
        };

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    id: 'VasCatalogGrid',
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
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'serviceCode', headerText: 'Service code', width: 140 },
                        { field: 'nameAr', headerText: 'Name AR', width: 180 },
                        { field: 'nameEn', headerText: 'Name EN', width: 140 },
                        { field: 'monthlyFee', headerText: 'Fee (SYP)', width: 100, format: 'N2' },
                        { field: 'hlrCommandTemplate', headerText: 'HLR template', width: 220 },
                        { field: 'sortOrder', headerText: 'Order', width: 70 },
                        { field: 'isActive', headerText: 'Active', width: 80, displayAsCheckBox: true, type: 'boolean' },
                        { field: 'createdAtUtc', headerText: 'Created', width: 150, format: 'yyyy-MM-dd HH:mm' },
                    ],
                    toolbar: [
                        'ExcelExport',
                        'Search',
                        { type: 'Separator' },
                        { text: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' },
                    ],
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                    },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length === 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], true);
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length !== 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
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
                            state.mainTitle = 'Add VAS service';
                            resetFormState();
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'EditCustom' && mainGrid.obj.getSelectedRecords().length) {
                            const r = mainGrid.obj.getSelectedRecords()[0];
                            state.mainTitle = 'Edit VAS service';
                            state.id = r.id ?? '';
                            state.serviceCode = r.serviceCode ?? '';
                            state.nameAr = r.nameAr ?? '';
                            state.nameEn = r.nameEn ?? '';
                            state.description = r.description ?? '';
                            state.monthlyFee = r.monthlyFee ?? 0;
                            state.isActive = !!r.isActive;
                            state.hlrCommandTemplate = r.hlrCommandTemplate ?? '';
                            state.sortOrder = r.sortOrder ?? 0;
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'DeleteCustom') {
                            await handler.handleDelete();
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
                console.error('VasCatalogList init:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return { mainGridRef, mainModalRef, state, handler };
    },
};

Vue.createApp(App).mount('#app');
