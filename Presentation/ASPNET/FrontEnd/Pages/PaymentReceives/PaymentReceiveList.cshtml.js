const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            paymentAmount: '',
            invoiceListLookupData: [],
            statusListLookupData: [],
            secondaryData: [],
            mainTitle: null,
            id: '',
            number: '',
            paymentDate: '',
            description: '',
            invoiceId: null,
            status: null,
            errors: {
                paymentDate: '',
                invoiceId: '',
                status: '',
                paymentAmount: '',
                paymentMethodId: '',
                description: ''
            },
            isSubmitting: false,
            invoiceData: null,
            subTotalAmount: '0.00',
            taxAmount: '0.00',
            totalAmount: '0.00'
        });

        const mainGridRef = Vue.ref(null);
        const secondaryGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const paymentDateRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const invoiceIdRef = Vue.ref(null);
        const statusRef = Vue.ref(null);
        const paymentAmountRef = Vue.ref(null);
        const paymentMethodIdRef = Vue.ref(null);


        const validateForm = function () {
            state.errors.paymentDate = '';
            state.errors.invoiceId = '';
            state.errors.status = '';
            state.errors.paymentAmount = '';
            state.errors.paymentMethodId = '';

            let isValid = true;

            if (!state.paymentDate) {
                state.errors.paymentDate = 'Payment receive date is required.';
                isValid = false;
            }
            if (!state.invoiceId) {
                state.errors.invoiceId = 'Invoice is required.';
                isValid = false;
            }
            if (!state.status) {
                state.errors.status = 'Payment receive status is required.';
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
            state.invoiceId = null;
            state.status = null;
            state.paymentAmount = '';
            state.paymentMethodId = null;
            state.errors = {
                paymentDate: '',
                invoiceId: '',
                status: '',
                paymentAmount: '',
                paymentMethodId: '',
                description: ''
            };
            state.invoiceData = null;
            state.secondaryData = [];
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/PaymentReceive/GetPaymentReceiveList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (paymentAmount, paymentMethodId, paymentDate, description, status, invoiceId, createdById) => {
                try {
                    const response = await AxiosManager.post('/PaymentReceive/CreatePaymentReceive', {
                        paymentAmount, paymentMethodId, paymentDate, description, status, invoiceId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, paymentAmount, paymentMethodId, paymentDate, description, status, invoiceId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/PaymentReceive/UpdatePaymentReceive', {
                        id, paymentAmount, paymentMethodId, paymentDate, description, status, invoiceId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/PaymentReceive/DeletePaymentReceive', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getInvoiceListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Invoice/GetInvoiceList', {});
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
            getInvoiceSingleData: async (invoiceId) => {
                try {
                    const response = await AxiosManager.get('/Invoice/GetInvoiceSingle?id=' + invoiceId, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getStatusListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/PaymentReceive/GetPaymentReceiveStatusList', {});
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
            populateInvoiceListLookupData: async () => {
                const response = await services.getInvoiceListLookupData();
                state.invoiceListLookupData = response?.data?.content?.data;
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
            populateInvoiceSingleData: async (invoiceId) => {
                const response = await services.getInvoiceSingleData(invoiceId);
                const selectedRecord = response?.data?.content?.data;
                state.invoiceData = selectedRecord;
                state.secondaryData = selectedRecord?.salesOrder?.salesOrderItemList;
                state.subTotalAmount = NumberFormatManager.formatToLocale(selectedRecord?.salesOrder?.beforeTaxAmount ?? 0);
                state.taxAmount = NumberFormatManager.formatToLocale(selectedRecord?.salesOrder?.taxAmount ?? 0);
                state.totalAmount = NumberFormatManager.formatToLocale(selectedRecord?.salesOrder?.afterTaxAmount ?? 0);
            },
            handleFormSubmit: async () => {

                try {
                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 200));

                    if (!validateForm()) {
                        return;
                    }

                    const response = state.id === ''
                        ? await services.createMainData(state.paymentAmount, state.paymentMethodId, state.paymentDate, state.description, state.status, state.invoiceId, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.paymentAmount, state.paymentMethodId, state.paymentDate, state.description, state.status, state.invoiceId, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = 'Edit Payment Receive';
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.paymentDate = response?.data?.content?.data.paymentDate ? new Date(response.data.content.data.paymentDate) : null;
                            state.description = response?.data?.content?.data.description ?? '';
                            state.invoiceId = response?.data?.content?.data.invoiceId ?? '';
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
                state.errors.invoiceId = '';
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

        const invoiceListLookup = {
            obj: null,
            create: () => {
                if (state.invoiceListLookupData && Array.isArray(state.invoiceListLookupData)) {
                    invoiceListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.invoiceListLookupData,
                        fields: { value: 'id', text: 'number' },
                        placeholder: 'Select Invoice',
                        filterBarPlaceholder: 'Search',
                        sortOrder: 'Ascending',
                        allowFiltering: true,
                        filtering: (e) => {
                            e.preventDefaultAction = true;
                            let query = new ej.data.Query();
                            if (e.text !== '') {
                                query = query.where('number', 'contains', e.text, true);
                            }
                            e.updateData(state.invoiceListLookupData, query);
                        },
                        change: async (e) => {
                            state.invoiceId = e.value;
                        }
                    });
                    invoiceListLookup.obj.appendTo(invoiceIdRef.value);
                }
            },
            refresh: () => {
                if (invoiceListLookup.obj) {
                    invoiceListLookup.obj.value = state.invoiceId;
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
                        placeholder: 'Select Payment Receive Status',
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
            () => state.invoiceId,
            async (newVal, oldVal) => {
                invoiceListLookup.refresh();
                state.errors.invoiceId = '';
                await methods.populateInvoiceSingleData(newVal);
            }
        );

        Vue.watch(
            () => state.invoiceData,
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
                        { field: 'invoiceNumber', headerText: 'Invoice', width: 200, minWidth: 200 },
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
                        mainGrid.obj.autoFitColumns(['number', 'paymentDate', 'invoiceNumber', 'paymentAmount', 'paymentMethodName', 'statusName', 'createdAtUtc']);
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
                            state.mainTitle = 'Add Payment Receive';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Payment Receive';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.paymentDate = selectedRecord.paymentDate ? new Date(selectedRecord.paymentDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.invoiceId = selectedRecord.invoiceId ?? '';
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
                                state.mainTitle = 'Delete Payment Receive?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.paymentDate = selectedRecord.paymentDate ? new Date(selectedRecord.paymentDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.invoiceId = selectedRecord.invoiceId ?? '';
                                state.paymentMethodId = selectedRecord.paymentMethodId ?? '';
                                state.paymentAmount = selectedRecord.paymentAmount ?? '';
                                state.status = String(selectedRecord.status ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'PrintPDFCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                window.open('/PaymentReceives/PaymentReceivePdf?id=' + (selectedRecord.id ?? ''), '_blank');
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
                await SecurityManager.authorizePage(['PaymentReceives']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                await secondaryGrid.create(state.secondaryData);

                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', methods.onMainModalHidden);
                await methods.populateInvoiceListLookupData();
                invoiceListLookup.create();
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
            invoiceIdRef,
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