const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            purchaseReturnListLookupData: [],
            debitNoteStatusListLookupData: [],
            secondaryData: [],
            mainTitle: null,
            id: '',
            number: '',
            debitNoteDate: '',
            description: '',
            purchaseReturnId: null,
            debitNoteStatus: null,
            errors: {
                debitNoteDate: '',
                purchaseReturnId: '',
                debitNoteStatus: '',
                description: ''
            },
            isSubmitting: false,
            purchaseReturnData: null,
            subTotalAmount: '0.00',
            taxAmount: '0.00',
            totalAmount: '0.00'
        });

        const mainGridRef = Vue.ref(null);
        const secondaryGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const debitNoteDateRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const purchaseReturnIdRef = Vue.ref(null);
        const debitNoteStatusRef = Vue.ref(null);


        const validateForm = function () {
            state.errors.debitNoteDate = '';
            state.errors.purchaseReturnId = '';
            state.errors.debitNoteStatus = '';

            let isValid = true;

            if (!state.debitNoteDate) {
                state.errors.debitNoteDate = 'Debit note date is required.';
                isValid = false;
            }
            if (!state.purchaseReturnId) {
                state.errors.purchaseReturnId = 'Purchase order is required.';
                isValid = false;
            }
            if (!state.debitNoteStatus) {
                state.errors.debitNoteStatus = 'Debit note status is required.';
                isValid = false;
            }

            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.number = '';
            state.debitNoteDate = '';
            state.description = '';
            state.purchaseReturnId = null;
            state.debitNoteStatus = null;
            state.errors = {
                debitNoteDate: '',
                purchaseReturnId: '',
                debitNoteStatus: '',
                description: ''
            };
            state.purchaseReturnData = null;
            state.secondaryData = [];
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/DebitNote/GetDebitNoteList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (debitNoteDate, description, debitNoteStatus, purchaseReturnId, createdById) => {
                try {
                    const response = await AxiosManager.post('/DebitNote/CreateDebitNote', {
                        debitNoteDate, description, debitNoteStatus, purchaseReturnId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, debitNoteDate, description, debitNoteStatus, purchaseReturnId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/DebitNote/UpdateDebitNote', {
                        id, debitNoteDate, description, debitNoteStatus, purchaseReturnId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/DebitNote/DeleteDebitNote', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPurchaseReturnListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/PurchaseReturn/GetPurchaseReturnList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPurchaseReturnSingleData: async (purchaseReturnId) => {
                try {
                    const response = await AxiosManager.get('/PurchaseReturn/GetPurchaseReturnSingle?id=' + purchaseReturnId, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getDebitNoteStatusListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/DebitNote/GetDebitNoteStatusList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            }
        };

        const methods = {
            populatePurchaseReturnListLookupData: async () => {
                const response = await services.getPurchaseReturnListLookupData();
                state.purchaseReturnListLookupData = response?.data?.content?.data;
            },
            populateDebitNoteStatusListLookupData: async () => {
                const response = await services.getDebitNoteStatusListLookupData();
                state.debitNoteStatusListLookupData = response?.data?.content?.data;
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    debitNoteDate: new Date(item.debitNoteDate),
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
            populatePurchaseReturnSingleData: async (purchaseReturnId) => {
                const response = await services.getPurchaseReturnSingleData(purchaseReturnId);
                state.purchaseReturnData = response?.data?.content?.data;
                state.secondaryData = response?.data?.content?.transactionList?.map(item => ({
                    ...item,
                    total: item.product.unitPrice * item.movement
                })) || [];
                state.subTotalAmount = NumberFormatManager.formatToLocale(response?.data?.content?.beforeTaxAmount ?? 0);
                state.taxAmount = NumberFormatManager.formatToLocale(response?.data?.content?.taxAmount ?? 0);
                state.totalAmount = NumberFormatManager.formatToLocale(response?.data?.content?.afterTaxAmount ?? 0);
            },
            handleFormSubmit: async () => {

                try {
                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 200));

                    if (!validateForm()) {
                        return;
                    }

                    const response = state.id === ''
                        ? await services.createMainData(state.debitNoteDate, state.description, state.debitNoteStatus, state.purchaseReturnId, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.debitNoteDate, state.description, state.debitNoteStatus, state.purchaseReturnId, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = 'Edit DebitNote';
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.debitNoteDate = response?.data?.content?.data.debitNoteDate ? new Date(response.data.content.data.debitNoteDate) : null;
                            state.description = response?.data?.content?.data.description ?? '';
                            state.purchaseReturnId = response?.data?.content?.data.purchaseReturnId ?? '';
                            state.debitNoteStatus = String(response?.data?.content?.data.debitNoteStatus ?? '');

                            Swal.fire({
                                icon: 'success',
                                title: 'Save Successful',
                                timer: 1000,
                                showConfirmButton: false
                            });
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
            onMainModalHidden: () => {
                state.errors.debitNoteDate = '';
                state.errors.purchaseReturnId = '';
                state.errors.debitNoteStatus = '';
            },
            refreshPaymentSummary: async () => {
            },
        };

        const purchaseReturnListLookup = {
            obj: null,
            create: () => {
                if (state.purchaseReturnListLookupData && Array.isArray(state.purchaseReturnListLookupData)) {
                    purchaseReturnListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.purchaseReturnListLookupData,
                        fields: { value: 'id', text: 'number' },
                        placeholder: 'Select Purchase Return',
                        filterBarPlaceholder: 'Search',
                        sortOrder: 'Ascending',
                        allowFiltering: true,
                        filtering: (e) => {
                            e.preventDefaultAction = true;
                            let query = new ej.data.Query();
                            if (e.text !== '') {
                                query = query.where('number', 'contains', e.text, true);
                            }
                            e.updateData(state.purchaseReturnListLookupData, query);
                        },
                        change: async (e) => {
                            state.purchaseReturnId = e.value;
                        }
                    });
                    purchaseReturnListLookup.obj.appendTo(purchaseReturnIdRef.value);
                }
            },
            refresh: () => {
                if (purchaseReturnListLookup.obj) {
                    purchaseReturnListLookup.obj.value = state.purchaseReturnId;
                }
            }
        };

        const debitNoteStatusListLookup = {
            obj: null,
            create: () => {
                if (state.debitNoteStatusListLookupData && Array.isArray(state.debitNoteStatusListLookupData)) {
                    debitNoteStatusListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.debitNoteStatusListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select DebitNote Status',
                        change: (e) => {
                            state.debitNoteStatus = e.value;
                        }
                    });
                    debitNoteStatusListLookup.obj.appendTo(debitNoteStatusRef.value);
                }
            },
            refresh: () => {
                if (debitNoteStatusListLookup.obj) {
                    debitNoteStatusListLookup.obj.value = state.debitNoteStatus;
                }
            }
        };

        const debitNoteDatePicker = {
            obj: null,
            create: () => {
                debitNoteDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'yyyy-MM-dd',
                    value: state.debitNoteDate ? new Date(state.debitNoteDate) : null,
                    change: (e) => {
                        state.debitNoteDate = e.value;
                    }
                });
                debitNoteDatePicker.obj.appendTo(debitNoteDateRef.value);
            },
            refresh: () => {
                if (debitNoteDatePicker.obj) {
                    debitNoteDatePicker.obj.value = state.debitNoteDate ? new Date(state.debitNoteDate) : null;
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
            }
        };

        Vue.watch(
            () => state.debitNoteDate,
            (newVal, oldVal) => {
                debitNoteDatePicker.refresh();
                state.errors.debitNoteDate = '';
            }
        );

        Vue.watch(
            () => state.purchaseReturnId,
            async (newVal, oldVal) => {
                purchaseReturnListLookup.refresh();
                state.errors.purchaseReturnId = '';
                await methods.populatePurchaseReturnSingleData(newVal);
            }
        );

        Vue.watch(
            () => state.purchaseReturnData,
            async (newVal, oldVal) => {
                await methods.refreshPaymentSummary();
                secondaryGrid.refresh();
            }
        );

        Vue.watch(
            () => state.debitNoteStatus,
            (newVal, oldVal) => {
                debitNoteStatusListLookup.refresh();
                state.errors.debitNoteStatus = '';
            }
        );


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
                        { field: 'debitNoteDate', headerText: 'DebitNote Date', width: 150, format: 'yyyy-MM-dd' },
                        { field: 'purchaseReturnNumber', headerText: 'Purchase Return', width: 200, minWidth: 200 },
                        { field: 'afterTaxAmount', headerText: 'Total Amount', width: 150, minWidth: 150, format: 'N2' },
                        { field: 'statusName', headerText: 'Status', width: 150, minWidth: 150 },
                        { field: 'createdAtUtc', headerText: 'Created At UTC', width: 150, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                        { text: 'Add', tooltipText: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: 'Delete', tooltipText: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' },
                        { type: 'Separator' },
                        { text: 'Print PDF', tooltipText: 'Print PDF', id: 'PrintPDFCustom' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], false);
                        mainGrid.obj.autoFitColumns(['number', 'debitNoteDate', 'purchaseReturnNumber', 'afterTaxAmount', 'statusName', 'createdAtUtc']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom', 'PrintPDFCustom'], false);
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
                            state.mainTitle = 'Add Debit Note';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Debit Note';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.debitNoteDate = selectedRecord.debitNoteDate ? new Date(selectedRecord.debitNoteDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.purchaseReturnId = selectedRecord.purchaseReturnId ?? '';
                                state.debitNoteStatus = String(selectedRecord.debitNoteStatus ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Debit Note?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.debitNoteDate = selectedRecord.debitNoteDate ? new Date(selectedRecord.debitNoteDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.purchaseReturnId = selectedRecord.purchaseReturnId ?? '';
                                state.debitNoteStatus = String(selectedRecord.debitNoteStatus ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'PrintPDFCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                window.open('/DebitNotes/DebitNotePdf?id=' + (selectedRecord.id ?? ''), '_blank');
                            }
                        }
                    }
                });

                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            }
        };


        const secondaryGrid = {
            obj: null,
            create: async (dataSource) => {
                secondaryGrid.obj = new ej.grids.Grid({
                    height: 200,
                    dataSource: dataSource,
                    editSettings: { allowEditing: false, allowAdding: false, allowDeleting: false, showDeleteConfirmDialog: true, mode: 'Normal', allowEditOnDblClick: true },
                    allowFiltering: false,
                    allowSorting: true,
                    allowSelection: true,
                    allowGrouping: false,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: false,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    sortSettings: { columns: [{ field: 'productName', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: false,
                    showColumnMenu: false,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'product.number', headerText: 'Number', width: 200, minWidth: 200 },
                        { field: 'product.name', headerText: 'Name', width: 300, minWidth: 300 },
                        { field: 'product.unitPrice', headerText: 'Unit Price', width: 200, minWidth: 200, type: 'number', format: 'N2', textAlign: 'Right' },
                        { field: 'movement', headerText: 'Quantity', width: 100, minWidth: 100, type: 'number', format: 'N2', textAlign: 'Right' },
                        { field: 'total', headerText: 'Total', width: 200, minWidth: 200, type: 'number', format: 'N2', textAlign: 'Right' },
                    ],
                    toolbar: [
                        'ExcelExport',
                        { type: 'Separator' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () { },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (secondaryGrid.obj.getSelectedRecords().length == 1) {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit'], true);
                        } else {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (secondaryGrid.obj.getSelectedRecords().length == 1) {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit'], true);
                        } else {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit'], false);
                        }
                    },
                    rowSelecting: () => {
                        if (secondaryGrid.obj.getSelectedRecords().length) {
                            secondaryGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: (args) => {
                        if (args.item.id === 'SecondaryGrid_excelexport') {
                            secondaryGrid.obj.excelExport();
                        }
                    },
                    actionComplete: async (args) => {
                    }
                });
                secondaryGrid.obj.appendTo(secondaryGridRef.value);
            },
            refresh: () => {
                secondaryGrid.obj.setProperties({ dataSource: state.secondaryData });
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

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['DebitNotes']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                await secondaryGrid.create(state.secondaryData);

                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', methods.onMainModalHidden);
                await methods.populatePurchaseReturnListLookupData();
                purchaseReturnListLookup.create();
                await methods.populateDebitNoteStatusListLookupData();
                debitNoteStatusListLookup.create();
                debitNoteDatePicker.create();
                numberText.create();
            } catch (e) {
                console.error('page init error:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        Vue.onUnmounted(() => {
            mainModalRef.value?.removeEventListener('hidden.bs.modal', methods.onMainModalHidden);
        });

        return {
            mainGridRef,
            secondaryGridRef,
            mainModalRef,
            debitNoteDateRef,
            numberRef,
            purchaseReturnIdRef,
            debitNoteStatusRef,
            state,
            methods,
            handler: {
                handleSubmit: methods.handleFormSubmit
            }
        };
    }
};

Vue.createApp(App).mount('#app');