const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            programManagerResourceListLookupData: [],
            programManagerStatusListLookupData: [],
            programManagerPriorityListLookupData: [],
            mainTitle: null,
            id: '',
            number: '',
            title: '',
            summary: '',
            programManagerResourceId: null,
            status: null,
            priority: null,
            errors: {
                title: '',
                programManagerResourceId: '',
                status: '',
                priority: ''
            },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const titleRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const programManagerResourceIdRef = Vue.ref(null);
        const statusRef = Vue.ref(null);
        const priorityRef = Vue.ref(null);

        const titleText = {
            obj: null,
            create: () => {
                titleText.obj = new ej.inputs.TextBox({
                    placeholder: 'Enter Title',
                });
                titleText.obj.appendTo(titleRef.value);
            },
            refresh: () => {
                if (titleText.obj) {
                    titleText.obj.value = state.title;
                }
            }
        };

        const numberText = {
            obj: null,
            create: () => {
                numberText.obj = new ej.inputs.TextBox({
                    placeholder: '[auto]',
                    readonly: true
                });
                numberText.obj.appendTo(numberRef.value);
            },
            refresh: () => {
                if (numberText.obj) {
                    numberText.obj.value = state.number;
                }
            }
        };

        const programManagerResourceListLookup = {
            obj: null,
            create: () => {
                if (state.programManagerResourceListLookupData && Array.isArray(state.programManagerResourceListLookupData)) {
                    programManagerResourceListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.programManagerResourceListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select Resource',
                        change: (e) => {
                            state.programManagerResourceId = e.value;
                        }
                    });
                    programManagerResourceListLookup.obj.appendTo(programManagerResourceIdRef.value);
                } else {
                    console.error('Resource list lookup data is not available or invalid.');
                }
            },
            refresh: () => {
                if (programManagerResourceListLookup.obj) {
                    programManagerResourceListLookup.obj.value = state.programManagerResourceId;
                }
            },
        };

        const programManagerStatusListLookup = {
            obj: null,
            create: () => {
                if (state.programManagerStatusListLookupData && Array.isArray(state.programManagerStatusListLookupData)) {
                    programManagerStatusListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.programManagerStatusListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select Status',
                        change: (e) => {
                            state.status = e.value;
                        }
                    });
                    programManagerStatusListLookup.obj.appendTo(statusRef.value);
                } else {
                    console.error('Status list lookup data is not available or invalid.');
                }
            },
            refresh: () => {
                if (programManagerStatusListLookup.obj) {
                    programManagerStatusListLookup.obj.value = state.status;
                }
            },
        };

        const programManagerPriorityListLookup = {
            obj: null,
            create: () => {
                if (state.programManagerPriorityListLookupData && Array.isArray(state.programManagerPriorityListLookupData)) {
                    programManagerPriorityListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.programManagerPriorityListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select Priority',
                        change: (e) => {
                            state.priority = e.value;
                        }
                    });
                    programManagerPriorityListLookup.obj.appendTo(priorityRef.value);
                } else {
                    console.error('Priority list lookup data is not available or invalid.');
                }
            },
            refresh: () => {
                if (programManagerPriorityListLookup.obj) {
                    programManagerPriorityListLookup.obj.value = state.priority;
                }
            },
        };

        Vue.watch(
            () => state.title,
            (newVal, oldVal) => {
                state.errors.title = '';
                titleText.refresh();
            }
        );

        Vue.watch(
            () => state.number,
            (newVal, oldVal) => {
                numberText.refresh();
            }
        );

        Vue.watch(
            () => state.programManagerResourceId,
            (newVal, oldVal) => {
                state.errors.programManagerResourceId = '';
                programManagerResourceListLookup.refresh();
            }
        );

        Vue.watch(
            () => state.status,
            (newVal, oldVal) => {
                state.errors.status = '';
                programManagerStatusListLookup.refresh();
            }
        );

        Vue.watch(
            () => state.priority,
            (newVal, oldVal) => {
                state.errors.priority = '';
                programManagerPriorityListLookup.refresh();
            }
        );

        const validateForm = function () {
            state.errors.title = '';
            state.errors.programManagerResourceId = '';
            state.errors.status = '';
            state.errors.priority = '';

            let isValid = true;

            if (!state.title) {
                state.errors.title = 'Title is required.';
                isValid = false;
            }
            if (!state.programManagerResourceId) {
                state.errors.programManagerResourceId = 'Resource is required.';
                isValid = false;
            }
            if (!state.priority) {
                state.errors.priority = 'Priority is required.';
                isValid = false;
            }
            if (!state.status) {
                state.errors.status = 'Status is required.';
                isValid = false;
            }

            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.number = '';
            state.title = '';
            state.summary = '';
            state.programManagerResourceId = null;
            state.status = null;
            state.priority = null;
            state.errors = {
                title: '',
                programManagerResourceId: '',
                status: '',
                priority: ''
            };
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/ProgramManager/GetProgramManagerList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (title, status, priority, summary, programManagerResourceId, createdById) => {
                try {
                    const response = await AxiosManager.post('/ProgramManager/CreateProgramManager', {
                        title, status, priority, summary, programManagerResourceId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, title, status, priority, summary, programManagerResourceId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/ProgramManager/UpdateProgramManager', {
                        id, title, status, priority, summary, programManagerResourceId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/ProgramManager/DeleteProgramManager', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getProgramManagerResourceListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/ProgramManagerResource/GetProgramManagerResourceList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getProgramManagerStatusListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/ProgramManager/GetProgramManagerStatusList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getProgramManagerPriorityListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/ProgramManager/GetProgramManagerPriorityList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            populateProgramManagerResourceListLookupData: async () => {
                const response = await services.getProgramManagerResourceListLookupData();
                state.programManagerResourceListLookupData = response?.data?.content?.data;
            },
            populateProgramManagerStatusListLookupData: async () => {
                const response = await services.getProgramManagerStatusListLookupData();
                state.programManagerStatusListLookupData = response?.data?.content?.data;
            },
            populateProgramManagerPriorityListLookupData: async () => {
                const response = await services.getProgramManagerPriorityListLookupData();
                state.programManagerPriorityListLookupData = response?.data?.content?.data;
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
        };

        const handler = {
            handleSubmit: async function () {
                try {
                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 300));

                    if (!validateForm()) {
                        return;
                    }

                    const response = state.id === ''
                        ? await services.createMainData(state.title, state.status, state.priority, state.summary, state.programManagerResourceId, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.title, state.status, state.priority, state.summary, state.programManagerResourceId, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = 'Edit Program Manager';
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.title = response?.data?.content?.data.title ?? '';
                            state.summary = response?.data?.content?.data.summary ?? '';
                            state.programManagerResourceId = response?.data?.content?.data.programManagerResourceId ?? '';
                            state.status = response?.data?.content?.data.status ?? '';
                            state.priority = response?.data?.content?.data.priority ?? '';

                            Swal.fire({
                                icon: 'success',
                                title: state.deleteMode ? 'Delete Successful' : 'Save Successful',
                                text: 'Form will be closed...',
                                timer: 2000,
                                showConfirmButton: false
                            });
                            setTimeout(() => {
                                mainModal.obj.hide();
                            }, 2000);

                        } else {
                            Swal.fire({
                                icon: 'success',
                                title: 'Delete Successful',
                                text: 'Form will be closed...',
                                timer: 2000,
                                showConfirmButton: false
                            });
                            setTimeout(() => {
                                mainModal.obj.hide();
                                resetFormState();
                            }, 2000);
                        }

                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: state.deleteMode ? 'Delete Failed' : 'Save Failed',
                            text: response.data.message ?? 'Please check your data.',
                            confirmButtonText: 'Try Again'
                        });
                    }

                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'An Error Occurred',
                        text: error.response?.data?.message ?? 'Please try again.',
                        confirmButtonText: 'OK'
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['ProgramManagers']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                await methods.populateProgramManagerResourceListLookupData();
                programManagerResourceListLookup.create();
                await methods.populateProgramManagerStatusListLookupData();
                programManagerStatusListLookup.create();
                await methods.populateProgramManagerPriorityListLookupData();
                programManagerPriorityListLookup.create();

                titleText.create();
                numberText.create();

                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', () => {
                    resetFormState();
                });

            } catch (e) {
                console.error('page init error:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        Vue.onUnmounted(() => {
            mainModalRef.value?.removeEventListener('hidden.bs.modal', resetFormState);
        });

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: getDashminGridHeight(),
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowGrouping: true,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    sortSettings: { columns: [{ field: 'createdAtUtc', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'number', headerText: 'Number', width: 150, minWidth: 150 },
                        { field: 'title', headerText: 'Title', width: 150, minWidth: 150 },
                        { field: 'programManagerResourceName', headerText: 'Resource', width: 150, minWidth: 150 },
                        { field: 'statusName', headerText: 'Status', width: 150, minWidth: 150 },
                        { field: 'priorityName', headerText: 'Priority', width: 150, minWidth: 150 },
                        { field: 'createdAtUtc', headerText: 'Created At UTC', width: 150, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Add', tooltipText: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Delete', tooltipText: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' },
                        { type: 'Separator' },
                        { text: 'Kanban View', tooltipText: 'Kanban View', id: 'KanbanViewCustom' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        mainGrid.obj.autoFitColumns(['number', 'title', 'programManagerResourceName', 'statusName', 'priorityName', 'createdAtUtc']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        }
                    },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            mainGrid.obj.excelExport();
                        }

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = 'Add Program Manager';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Program Manager';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.title = selectedRecord.title ?? '';
                                state.summary = selectedRecord.summary ?? '';
                                state.programManagerResourceId = selectedRecord.programManagerResourceId ?? '';
                                state.status = String(selectedRecord.status ?? '');
                                state.priority = String(selectedRecord.priority ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Program Manager?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.title = selectedRecord.title ?? '';
                                state.summary = selectedRecord.summary ?? '';
                                state.programManagerResourceId = selectedRecord.programManagerResourceId ?? '';
                                state.status = String(selectedRecord.status ?? '');
                                state.priority = String(selectedRecord.priority ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'KanbanViewCustom') {
                            state.deleteMode = false;
                            window.open('/ProgramKanbans/ProgramKanbanList', '_blank');
                        }
                    }
                });

                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            }
        };

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        return {
            mainGridRef,
            mainModalRef,
            titleRef,
            numberRef,
            programManagerResourceIdRef,
            statusRef,
            priorityRef,
            state,
            handler,
        };
    }
};

Vue.createApp(App).mount('#app');