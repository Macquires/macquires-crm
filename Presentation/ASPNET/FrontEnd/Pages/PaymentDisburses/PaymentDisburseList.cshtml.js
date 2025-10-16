const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            paymentAmount: '',
            billListLookupData: [],
            statusListLookupData: [],
            secondaryData: [],
            mainTitle: null,
            id: '',
            number: '',
            paymentDate: '',
            description: '',
            billId: null,
            status: null,
            errors: {
                paymentDate: '',
                billId: '',
                status: '',
                paymentAmount: '',
                paymentMethodId: '',
                description: ''
            },
            isSubmitting: false,
            billData: null,
            subTotalAmount: '0.00',
            taxAmount: '0.00',
            totalAmount: '0.00'
        });

        const mainGridRef = Vue.ref(null);
        const secondaryGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const paymentDateRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const billIdRef = Vue.ref(null);
        const statusRef = Vue.ref(null);
        const paymentAmountRef = Vue.ref(null);
        const paymentMethodIdRef = Vue.ref(null);


        const validateForm = function () {
            state.errors.paymentDate = '';
            state.errors.billId = '';
            state.errors.status = '';
            state.errors.paymentAmount = '';
            state.errors.paymentMethodId = '';

            let isValid = true;

            if (!state.paymentDate) {
                state.errors.paymentDate = 'Payment disburse date is required.';
                isValid = false;
            }
            if (!state.billId) {
                state.errors.billId = 'Bill is required.';
                isValid = false;
            }
            if (!state.status) {
                state.errors.status = 'Payment disburse status is required.';
                isValid = false;
            }
            if (!state.paymentMethodId) {
                state.errors.paymentMethodId = 'Payment method is required.';
                isValid = false;
            }
            if (state.paymentAmount === null || state.paymentAmount === '' || isNaN(state.paymentAmount)) {
                state.errors.paymentAmount = 'Payment amount is required.';
                isValid = false;
            }

            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.number = '';
            state.paymentDate = '';
            state.description = '';
            state.billId = null;
            state.status = null;
            state.paymentAmount = '';
            state.paymentMethodId = null;
            state.errors = {
                paymentDate: '',
                billId: '',
                status: '',
                paymentAmount: '',
                paymentMethodId: '',
                description: ''
            };
            state.billData = null;
            state.secondaryData = [];
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/PaymentDisburse/GetPaymentDisburseList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (paymentAmount, paymentMethodId, paymentDate, description, status, billId, createdById) => {
                try {
                    const response = await AxiosManager.post('/PaymentDisburse/CreatePaymentDisburse', {
                        paymentAmount, paymentMethodId, paymentDate, description, status, billId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, paymentAmount, paymentMethodId, paymentDate, description, status, billId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/PaymentDisburse/UpdatePaymentDisburse', {
                        id, paymentAmount, paymentMethodId, paymentDate, description, status, billId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/PaymentDisburse/DeletePaymentDisburse', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getBillListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Bill/GetBillList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPaymentMethodListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/PaymentMethod/GetPaymentMethodList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getBillSingleData: async (billId) => {
                try {
                    const response = await AxiosManager.get('/Bill/GetBillSingle?id=' + billId, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getStatusListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/PaymentDisburse/GetPaymentDisburseStatusList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            }
        };

        const methods = {
            populatePaymentMethodListLookupData: async () => {
                const response = await services.getPaymentMethodListLookupData();
                state.paymentMethodListLookupData = response?.data?.content?.data;
            },
            populateBillListLookupData: async () => {
                const response = await services.getBillListLookupData();
                state.billListLookupData = response?.data?.content?.data;
            },
            populateStatusListLookupData: async () => {
                const response = await services.getStatusListLookupData();
                state.statusListLookupData = response?.data?.content?.data;
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    paymentDate: new Date(item.paymentDate),
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
            populateBillSingleData: async (billId) => {
                const response = await services.getBillSingleData(billId);
                const selectedRecord = response?.data?.content?.data;
                state.billData = selectedRecord;
                state.secondaryData = selectedRecord?.purchaseOrder?.purchaseOrderItemList;
                state.subTotalAmount = NumberFormatManager.formatToLocale(selectedRecord?.purchaseOrder?.beforeTaxAmount ?? 0);
                state.taxAmount = NumberFormatManager.formatToLocale(selectedRecord?.purchaseOrder?.taxAmount ?? 0);
                state.totalAmount = NumberFormatManager.formatToLocale(selectedRecord?.purchaseOrder?.afterTaxAmount ?? 0);
            },
            handleFormSubmit: async () => {

                try {
                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 200));

                    if (!validateForm()) {
                        return;
                    }

                    const response = state.id === ''
                        ? await services.createMainData(state.paymentAmount, state.paymentMethodId, state.paymentDate, state.description, state.status, state.billId, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.paymentAmount, state.paymentMethodId, state.paymentDate, state.description, state.status, state.billId, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = 'Edit Payment Disburse';
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.paymentDate = response?.data?.content?.data.paymentDate ? new Date(response.data.content.data.paymentDate) : null;
                            state.description = response?.data?.content?.data.description ?? '';
                            state.billId = response?.data?.content?.data.billId ?? '';
                            state.paymentMethodId = response?.data?.content?.data.paymentMethodId ?? '';
                            state.paymentAmount = response?.data?.content?.data.paymentAmount ?? '';
                            state.status = String(response?.data?.content?.data.status ?? '');

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
                state.errors.paymentDate = '';
                state.errors.billId = '';
                state.errors.status = '';
                state.errors.paymentMethodId = '';
                state.errors.paymentAmount = '';
            },
            refreshPaymentSummary: async () => {
            },
        };

        const paymentMethodListLookup = {
            obj: null,
            create: () => {
                if (state.paymentMethodListLookupData && Array.isArray(state.paymentMethodListLookupData)) {
                    paymentMethodListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.paymentMethodListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select Payment Method',
                        filterBarPlaceholder: 'Search',
                        sortOrder: 'Ascending',
                        change: (e) => {
                            state.paymentMethodId = e.value;
                        }
                    });
                    paymentMethodListLookup.obj.appendTo(paymentMethodIdRef.value);
                }
            },

            refresh: () => {
                if (paymentMethodListLookup.obj) {
                    paymentMethodListLookup.obj.value = state.paymentMethodId
                }
            },
        };

        Vue.watch(
            () => state.paymentMethodId,
            (newVal, oldVal) => {
                paymentMethodListLookup.refresh();
                state.errors.paymentMethodId = '';
            }
        );

        const billListLookup = {
            obj: null,
            create: () => {
                if (state.billListLookupData && Array.isArray(state.billListLookupData)) {
                    billListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.billListLookupData,
                        fields: { value: 'id', text: 'number' },
                        placeholder: 'Select Bill',
                        filterBarPlaceholder: 'Search',
                        sortOrder: 'Ascending',
                        allowFiltering: true,
                        filtering: (e) => {
                            e.preventDefaultAction = true;
                            let query = new ej.data.Query();
                            if (e.text !== '') {
                                query = query.where('number', 'contains', e.text, true);
                            }
                            e.updateData(state.billListLookupData, query);
                        },
                        change: async (e) => {
                            state.billId = e.value;
                        }
                    });
                    billListLookup.obj.appendTo(billIdRef.value);
                }
            },
            refresh: () => {
                if (billListLookup.obj) {
                    billListLookup.obj.value = state.billId;
                }
            }
        };

        const statusListLookup = {
            obj: null,
            create: () => {
                if (state.statusListLookupData && Array.isArray(state.statusListLookupData)) {
                    statusListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.statusListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select Payment Disburse Status',
                        change: (e) => {
                            state.status = e.value;
                        }
                    });
                    statusListLookup.obj.appendTo(statusRef.value);
                }
            },
            refresh: () => {
                if (statusListLookup.obj) {
                    statusListLookup.obj.value = state.status;
                }
            }
        };

        const paymentDatePicker = {
            obj: null,
            create: () => {
                paymentDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'yyyy-MM-dd',
                    value: state.paymentDate ? new Date(state.paymentDate) : null,
                    change: (e) => {
                        state.paymentDate = e.value;
                    }
                });
                paymentDatePicker.obj.appendTo(paymentDateRef.value);
            },
            refresh: () => {
                if (paymentDatePicker.obj) {
                    paymentDatePicker.obj.value = state.paymentDate ? new Date(state.paymentDate) : null;
                }
            }
        };

        const paymentAmountText = {
            obj: null,
            create: () => {
                paymentAmountText.obj = new ej.inputs.NumericTextBox({
                    placeholder: 'Enter Payment Amount',
                    format: 'n2',
                    min: 0,
                });
                paymentAmountText.obj.appendTo(paymentAmountRef.value);
            },
            refresh: () => {
                if (paymentAmountText.obj) {
                    paymentAmountText.obj.value = parseFloat(state.paymentAmount) || 0;
                }
            }
        };

        Vue.watch(
            () => state.paymentAmount,
            (newVal, oldVal) => {
                paymentAmountText.refresh();
                state.errors.paymentAmount = '';
            }
        );

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
            () => state.paymentDate,
            (newVal, oldVal) => {
                paymentDatePicker.refresh();
                state.errors.paymentDate = '';
            }
        );

        Vue.watch(
            () => state.billId,
            async (newVal, oldVal) => {
                billListLookup.refresh();
                state.errors.billId = '';
                await methods.populateBillSingleData(newVal);
            }
        );

        Vue.watch(
            () => state.billData,
            async (newVal, oldVal) => {
                await methods.refreshPaymentSummary();
                secondaryGrid.refresh();
            }
        );

        Vue.watch(
            () => state.status,
            (newVal, oldVal) => {
                statusListLookup.refresh();
                state.errors.status = '';
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
                        { field: 'paymentDate', headerText: 'Payment Date', width: 150, format: 'yyyy-MM-dd' },
                        { field: 'billNumber', headerText: 'Bill', width: 200, minWidth: 200 },
                        { field: 'paymentAmount', headerText: 'Payment Amount', width: 150, minWidth: 150, format: 'N2' },
                        { field: 'paymentMethodName', headerText: 'Payment Method', width: 200, minWidth: 200 },
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
                        mainGrid.obj.autoFitColumns(['number', 'paymentDate', 'billNumber', 'paymentAmount', 'paymentMethodName', 'statusName', 'createdAtUtc']);
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
                            state.mainTitle = 'Add Payment Disburse';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Payment Disburse';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.paymentDate = selectedRecord.paymentDate ? new Date(selectedRecord.paymentDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.billId = selectedRecord.billId ?? '';
                                state.paymentMethodId = selectedRecord.paymentMethodId ?? '';
                                state.paymentAmount = selectedRecord.paymentAmount ?? '';
                                state.status = String(selectedRecord.status ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Payment Disburse?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.paymentDate = selectedRecord.paymentDate ? new Date(selectedRecord.paymentDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.billId = selectedRecord.billId ?? '';
                                state.paymentMethodId = selectedRecord.paymentMethodId ?? '';
                                state.paymentAmount = selectedRecord.paymentAmount ?? '';
                                state.status = String(selectedRecord.status ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'PrintPDFCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                window.open('/PaymentDisburses/PaymentDisbursePdf?id=' + (selectedRecord.id ?? ''), '_blank');
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
                await SecurityManager.authorizePage(['PaymentDisburses']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                await secondaryGrid.create(state.secondaryData);

                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', methods.onMainModalHidden);
                await methods.populateBillListLookupData();
                billListLookup.create();
                await methods.populatePaymentMethodListLookupData();
                paymentMethodListLookup.create();
                await methods.populateStatusListLookupData();
                statusListLookup.create();
                paymentDatePicker.create();
                numberText.create();
                paymentAmountText.create();
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
            paymentDateRef,
            numberRef,
            billIdRef,
            paymentMethodIdRef,
            paymentAmountRef,
            statusRef,
            state,
            methods,
            handler: {
                handleSubmit: methods.handleFormSubmit
            }
        };
    }
};

Vue.createApp(App).mount('#app');