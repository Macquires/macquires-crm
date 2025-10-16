const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            salesReturnListLookupData: [],
            creditNoteStatusListLookupData: [],
            secondaryData: [],
            mainTitle: null,
            id: '',
            number: '',
            creditNoteDate: '',
            description: '',
            salesReturnId: null,
            creditNoteStatus: null,
            errors: {
                creditNoteDate: '',
                salesReturnId: '',
                creditNoteStatus: '',
                description: ''
            },
            isSubmitting: false,
            salesReturnData: null,
            subTotalAmount: '0.00',
            taxAmount: '0.00',
            totalAmount: '0.00'
        });

        const mainGridRef = Vue.ref(null);
        const secondaryGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const creditNoteDateRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const salesReturnIdRef = Vue.ref(null);
        const creditNoteStatusRef = Vue.ref(null);


        const validateForm = function () {
            state.errors.creditNoteDate = '';
            state.errors.salesReturnId = '';
            state.errors.creditNoteStatus = '';

            let isValid = true;

            if (!state.creditNoteDate) {
                state.errors.creditNoteDate = 'CreditNote date is required.';
                isValid = false;
            }
            if (!state.salesReturnId) {
                state.errors.salesReturnId = 'Sales order is required.';
                isValid = false;
            }
            if (!state.creditNoteStatus) {
                state.errors.creditNoteStatus = 'CreditNote status is required.';
                isValid = false;
            }

            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.number = '';
            state.creditNoteDate = '';
            state.description = '';
            state.salesReturnId = null;
            state.creditNoteStatus = null;
            state.errors = {
                creditNoteDate: '',
                salesReturnId: '',
                creditNoteStatus: '',
                description: ''
            };
            state.salesReturnData = null;
            state.secondaryData = [];
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/CreditNote/GetCreditNoteList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (creditNoteDate, description, creditNoteStatus, salesReturnId, createdById) => {
                try {
                    const response = await AxiosManager.post('/CreditNote/CreateCreditNote', {
                        creditNoteDate, description, creditNoteStatus, salesReturnId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, creditNoteDate, description, creditNoteStatus, salesReturnId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/CreditNote/UpdateCreditNote', {
                        id, creditNoteDate, description, creditNoteStatus, salesReturnId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/CreditNote/DeleteCreditNote', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSalesReturnListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/SalesReturn/GetSalesReturnList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSalesReturnSingleData: async (salesReturnId) => {
                try {
                    const response = await AxiosManager.get('/SalesReturn/GetSalesReturnSingle?id=' + salesReturnId, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getCreditNoteStatusListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/CreditNote/GetCreditNoteStatusList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            }
        };

        const methods = {
            populateSalesReturnListLookupData: async () => {
                const response = await services.getSalesReturnListLookupData();
                state.salesReturnListLookupData = response?.data?.content?.data;
            },
            populateCreditNoteStatusListLookupData: async () => {
                const response = await services.getCreditNoteStatusListLookupData();
                state.creditNoteStatusListLookupData = response?.data?.content?.data;
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    creditNoteDate: new Date(item.creditNoteDate),
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
            populateSalesReturnSingleData: async (salesReturnId) => {
                const response = await services.getSalesReturnSingleData(salesReturnId);
                state.salesReturnData = response?.data?.content?.data;
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
                        ? await services.createMainData(state.creditNoteDate, state.description, state.creditNoteStatus, state.salesReturnId, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.creditNoteDate, state.description, state.creditNoteStatus, state.salesReturnId, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = 'Edit CreditNote';
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.creditNoteDate = response?.data?.content?.data.creditNoteDate ? new Date(response.data.content.data.creditNoteDate) : null;
                            state.description = response?.data?.content?.data.description ?? '';
                            state.salesReturnId = response?.data?.content?.data.salesReturnId ?? '';
                            state.creditNoteStatus = String(response?.data?.content?.data.creditNoteStatus ?? '');

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
                state.errors.creditNoteDate = '';
                state.errors.salesReturnId = '';
                state.errors.creditNoteStatus = '';
            },
            refreshPaymentSummary: async () => {
            },
        };

        const salesReturnListLookup = {
            obj: null,
            create: () => {
                if (state.salesReturnListLookupData && Array.isArray(state.salesReturnListLookupData)) {
                    salesReturnListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.salesReturnListLookupData,
                        fields: { value: 'id', text: 'number' },
                        placeholder: 'Select Sales Return',
                        filterBarPlaceholder: 'Search',
                        sortOrder: 'Ascending',
                        allowFiltering: true,
                        filtering: (e) => {
                            e.preventDefaultAction = true;
                            let query = new ej.data.Query();
                            if (e.text !== '') {
                                query = query.where('number', 'contains', e.text, true);
                            }
                            e.updateData(state.salesReturnListLookupData, query);
                        },
                        change: async (e) => {
                            state.salesReturnId = e.value;
                        }
                    });
                    salesReturnListLookup.obj.appendTo(salesReturnIdRef.value);
                }
            },
            refresh: () => {
                if (salesReturnListLookup.obj) {
                    salesReturnListLookup.obj.value = state.salesReturnId;
                }
            }
        };

        const creditNoteStatusListLookup = {
            obj: null,
            create: () => {
                if (state.creditNoteStatusListLookupData && Array.isArray(state.creditNoteStatusListLookupData)) {
                    creditNoteStatusListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.creditNoteStatusListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select CreditNote Status',
                        change: (e) => {
                            state.creditNoteStatus = e.value;
                        }
                    });
                    creditNoteStatusListLookup.obj.appendTo(creditNoteStatusRef.value);
                }
            },
            refresh: () => {
                if (creditNoteStatusListLookup.obj) {
                    creditNoteStatusListLookup.obj.value = state.creditNoteStatus;
                }
            }
        };

        const creditNoteDatePicker = {
            obj: null,
            create: () => {
                creditNoteDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'yyyy-MM-dd',
                    value: state.creditNoteDate ? new Date(state.creditNoteDate) : null,
                    change: (e) => {
                        state.creditNoteDate = e.value;
                    }
                });
                creditNoteDatePicker.obj.appendTo(creditNoteDateRef.value);
            },
            refresh: () => {
                if (creditNoteDatePicker.obj) {
                    creditNoteDatePicker.obj.value = state.creditNoteDate ? new Date(state.creditNoteDate) : null;
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
            () => state.creditNoteDate,
            (newVal, oldVal) => {
                creditNoteDatePicker.refresh();
                state.errors.creditNoteDate = '';
            }
        );

        Vue.watch(
            () => state.salesReturnId,
            async (newVal, oldVal) => {
                salesReturnListLookup.refresh();
                state.errors.salesReturnId = '';
                await methods.populateSalesReturnSingleData(newVal);
            }
        );

        Vue.watch(
            () => state.salesReturnData,
            async (newVal, oldVal) => {
                await methods.refreshPaymentSummary();
                secondaryGrid.refresh();
            }
        );

        Vue.watch(
            () => state.creditNoteStatus,
            (newVal, oldVal) => {
                creditNoteStatusListLookup.refresh();
                state.errors.creditNoteStatus = '';
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
                        { field: 'creditNoteDate', headerText: 'CreditNote Date', width: 150, format: 'yyyy-MM-dd' },
                        { field: 'salesReturnNumber', headerText: 'Sales Return', width: 200, minWidth: 200 },
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
                        mainGrid.obj.autoFitColumns(['number', 'creditNoteDate', 'salesReturnNumber', 'afterTaxAmount', 'statusName', 'createdAtUtc']);
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
                            state.mainTitle = 'Add CreditNote';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Credit Note';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.creditNoteDate = selectedRecord.creditNoteDate ? new Date(selectedRecord.creditNoteDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.salesReturnId = selectedRecord.salesReturnId ?? '';
                                state.creditNoteStatus = String(selectedRecord.creditNoteStatus ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Credit Note?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.creditNoteDate = selectedRecord.creditNoteDate ? new Date(selectedRecord.creditNoteDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.salesReturnId = selectedRecord.salesReturnId ?? '';
                                state.creditNoteStatus = String(selectedRecord.creditNoteStatus ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'PrintPDFCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                window.open('/CreditNotes/CreditNotePdf?id=' + (selectedRecord.id ?? ''), '_blank');
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
                await SecurityManager.authorizePage(['CreditNotes']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                await secondaryGrid.create(state.secondaryData);

                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', methods.onMainModalHidden);
                await methods.populateSalesReturnListLookupData();
                salesReturnListLookup.create();
                await methods.populateCreditNoteStatusListLookupData();
                creditNoteStatusListLookup.create();
                creditNoteDatePicker.create();
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
            creditNoteDateRef,
            numberRef,
            salesReturnIdRef,
            creditNoteStatusRef,
            state,
            methods,
            handler: {
                handleSubmit: methods.handleFormSubmit
            }
        };
    }
};

Vue.createApp(App).mount('#app');