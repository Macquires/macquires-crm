const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            salesOrderListLookupData: [],
            invoiceStatusListLookupData: [],
            secondaryData: [],
            mainTitle: null,
            id: '',
            number: '',
            invoiceDate: '',
            description: '',
            salesOrderId: null,
            invoiceStatus: null,
            errors: {
                invoiceDate: '',
                salesOrderId: '',
                invoiceStatus: '',
                description: ''
            },
            isSubmitting: false,
            salesOrderData: null,
            subTotalAmount: '0.00',
            taxAmount: '0.00',
            totalAmount: '0.00'
        });

        const mainGridRef = Vue.ref(null);
        const secondaryGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const invoiceDateRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const salesOrderIdRef = Vue.ref(null);
        const invoiceStatusRef = Vue.ref(null);


        const validateForm = function () {
            state.errors.invoiceDate = '';
            state.errors.salesOrderId = '';
            state.errors.invoiceStatus = '';

            let isValid = true;

            if (!state.invoiceDate) {
                state.errors.invoiceDate = 'Invoice date is required.';
                isValid = false;
            }
            if (!state.salesOrderId) {
                state.errors.salesOrderId = 'Sales order is required.';
                isValid = false;
            }
            if (!state.invoiceStatus) {
                state.errors.invoiceStatus = 'Invoice status is required.';
                isValid = false;
            }

            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.number = '';
            state.invoiceDate = '';
            state.description = '';
            state.salesOrderId = null;
            state.invoiceStatus = null;
            state.errors = {
                invoiceDate: '',
                salesOrderId: '',
                invoiceStatus: '',
                description: ''
            };
            state.salesOrderData = null;
            state.secondaryData = [];
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/Invoice/GetInvoiceList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (invoiceDate, description, invoiceStatus, salesOrderId, createdById) => {
                try {
                    const response = await AxiosManager.post('/Invoice/CreateInvoice', {
                        invoiceDate, description, invoiceStatus, salesOrderId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, invoiceDate, description, invoiceStatus, salesOrderId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/Invoice/UpdateInvoice', {
                        id, invoiceDate, description, invoiceStatus, salesOrderId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/Invoice/DeleteInvoice', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSalesOrderListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/SalesOrder/GetSalesOrderList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSalesOrderSingleData: async (salesOrderId) => {
                try {
                    const response = await AxiosManager.get('/SalesOrder/GetSalesOrderSingle?id=' + salesOrderId, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getInvoiceStatusListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Invoice/GetInvoiceStatusList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            }
        };

        const methods = {
            populateSalesOrderListLookupData: async () => {
                const response = await services.getSalesOrderListLookupData();
                state.salesOrderListLookupData = response?.data?.content?.data;
            },
            populateInvoiceStatusListLookupData: async () => {
                const response = await services.getInvoiceStatusListLookupData();
                state.invoiceStatusListLookupData = response?.data?.content?.data;
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    invoiceDate: new Date(item.invoiceDate),
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
            populateSalesOrderSingleData: async (salesOrderId) => {
                const response = await services.getSalesOrderSingleData(salesOrderId);
                state.salesOrderData = response?.data?.content?.data;
                state.secondaryData = response?.data?.content?.data?.salesOrderItemList;
            },
            handleFormSubmit: async () => {

                try {
                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 200));

                    if (!validateForm()) {
                        return;
                    }

                    const response = state.id === ''
                        ? await services.createMainData(state.invoiceDate, state.description, state.invoiceStatus, state.salesOrderId, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.invoiceDate, state.description, state.invoiceStatus, state.salesOrderId, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = 'Edit Invoice';
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.invoiceDate = response?.data?.content?.data.invoiceDate ? new Date(response.data.content.data.invoiceDate) : null;
                            state.description = response?.data?.content?.data.description ?? '';
                            state.salesOrderId = response?.data?.content?.data.salesOrderId ?? '';
                            state.invoiceStatus = String(response?.data?.content?.data.invoiceStatus ?? '');

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
                state.errors.invoiceDate = '';
                state.errors.salesOrderId = '';
                state.errors.invoiceStatus = '';
            },
            refreshPaymentSummary: async () => {
                state.subTotalAmount = NumberFormatManager.formatToLocale(state?.salesOrderData?.beforeTaxAmount ?? 0);
                state.taxAmount = NumberFormatManager.formatToLocale(state?.salesOrderData?.taxAmount ?? 0);
                state.totalAmount = NumberFormatManager.formatToLocale(state?.salesOrderData?.afterTaxAmount ?? 0);
            },
        };

        const salesOrderListLookup = {
            obj: null,
            create: () => {
                if (state.salesOrderListLookupData && Array.isArray(state.salesOrderListLookupData)) {
                    salesOrderListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.salesOrderListLookupData,
                        fields: { value: 'id', text: 'number' },
                        placeholder: 'Select Sales Order',
                        filterBarPlaceholder: 'Search',
                        sortOrder: 'Ascending',
                        allowFiltering: true,
                        filtering: (e) => {
                            e.preventDefaultAction = true;
                            let query = new ej.data.Query();
                            if (e.text !== '') {
                                query = query.where('number', 'contains', e.text, true);
                            }
                            e.updateData(state.salesOrderListLookupData, query);
                        },
                        change: async (e) => {
                            state.salesOrderId = e.value;
                        }
                    });
                    salesOrderListLookup.obj.appendTo(salesOrderIdRef.value);
                }
            },
            refresh: () => {
                if (salesOrderListLookup.obj) {
                    salesOrderListLookup.obj.value = state.salesOrderId;
                }
            }
        };

        const invoiceStatusListLookup = {
            obj: null,
            create: () => {
                if (state.invoiceStatusListLookupData && Array.isArray(state.invoiceStatusListLookupData)) {
                    invoiceStatusListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.invoiceStatusListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select Invoice Status',
                        change: (e) => {
                            state.invoiceStatus = e.value;
                        }
                    });
                    invoiceStatusListLookup.obj.appendTo(invoiceStatusRef.value);
                }
            },
            refresh: () => {
                if (invoiceStatusListLookup.obj) {
                    invoiceStatusListLookup.obj.value = state.invoiceStatus;
                }
            }
        };

        const invoiceDatePicker = {
            obj: null,
            create: () => {
                invoiceDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'yyyy-MM-dd',
                    value: state.invoiceDate ? new Date(state.invoiceDate) : null,
                    change: (e) => {
                        state.invoiceDate = e.value;
                    }
                });
                invoiceDatePicker.obj.appendTo(invoiceDateRef.value);
            },
            refresh: () => {
                if (invoiceDatePicker.obj) {
                    invoiceDatePicker.obj.value = state.invoiceDate ? new Date(state.invoiceDate) : null;
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
            () => state.invoiceDate,
            (newVal, oldVal) => {
                invoiceDatePicker.refresh();
                state.errors.invoiceDate = '';
            }
        );

        Vue.watch(
            () => state.salesOrderId,
            async (newVal, oldVal) => {
                salesOrderListLookup.refresh();
                state.errors.salesOrderId = '';
                await methods.populateSalesOrderSingleData(newVal);
            }
        );

        Vue.watch(
            () => state.salesOrderData,
            async (newVal, oldVal) => {
                await methods.refreshPaymentSummary();
                secondaryGrid.refresh();
            }
        );

        Vue.watch(
            () => state.invoiceStatus,
            (newVal, oldVal) => {
                invoiceStatusListLookup.refresh();
                state.errors.invoiceStatus = '';
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
                        { field: 'invoiceDate', headerText: 'Invoice Date', width: 150, format: 'yyyy-MM-dd' },
                        { field: 'salesOrderNumber', headerText: 'Sales Order', width: 200, minWidth: 200 },
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
                        mainGrid.obj.autoFitColumns(['number', 'invoiceDate', 'salesOrderNumber', 'afterTaxAmount', 'statusName', 'createdAtUtc']);
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
                            state.mainTitle = 'Add Invoice';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Invoice';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.invoiceDate = selectedRecord.invoiceDate ? new Date(selectedRecord.invoiceDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.salesOrderId = selectedRecord.salesOrderId ?? '';
                                state.invoiceStatus = String(selectedRecord.invoiceStatus ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Invoice?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.invoiceDate = selectedRecord.invoiceDate ? new Date(selectedRecord.invoiceDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.salesOrderId = selectedRecord.salesOrderId ?? '';
                                state.invoiceStatus = String(selectedRecord.invoiceStatus ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'PrintPDFCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                window.open('/Invoices/InvoicePdf?id=' + (selectedRecord.id ?? ''), '_blank');
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
                        { field: 'quantity', headerText: 'Quantity', width: 100, minWidth: 100, type: 'number', format: 'N2', textAlign: 'Right' },
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
                await SecurityManager.authorizePage(['Invoices']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                await secondaryGrid.create(state.secondaryData);

                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', methods.onMainModalHidden);
                await methods.populateSalesOrderListLookupData();
                salesOrderListLookup.create();
                await methods.populateInvoiceStatusListLookupData();
                invoiceStatusListLookup.create();
                invoiceDatePicker.create();
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
            invoiceDateRef,
            numberRef,
            salesOrderIdRef,
            invoiceStatusRef,
            state,
            methods,
            handler: {
                handleSubmit: methods.handleFormSubmit
            }
        };
    }
};

Vue.createApp(App).mount('#app');