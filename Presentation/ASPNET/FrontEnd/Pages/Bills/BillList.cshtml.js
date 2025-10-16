const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            purchaseOrderListLookupData: [],
            billStatusListLookupData: [],
            secondaryData: [],
            mainTitle: null,
            id: '',
            number: '',
            billDate: '',
            description: '',
            purchaseOrderId: null,
            billStatus: null,
            errors: {
                billDate: '',
                purchaseOrderId: '',
                billStatus: '',
                description: ''
            },
            isSubmitting: false,
            purchaseOrderData: null,
            subTotalAmount: '0.00',
            taxAmount: '0.00',
            totalAmount: '0.00'
        });

        const mainGridRef = Vue.ref(null);
        const secondaryGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const billDateRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const purchaseOrderIdRef = Vue.ref(null);
        const billStatusRef = Vue.ref(null);


        const validateForm = function () {
            state.errors.billDate = '';
            state.errors.purchaseOrderId = '';
            state.errors.billStatus = '';

            let isValid = true;

            if (!state.billDate) {
                state.errors.billDate = 'Bill date is required.';
                isValid = false;
            }
            if (!state.purchaseOrderId) {
                state.errors.purchaseOrderId = 'Purchase order is required.';
                isValid = false;
            }
            if (!state.billStatus) {
                state.errors.billStatus = 'Bill status is required.';
                isValid = false;
            }

            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.number = '';
            state.billDate = '';
            state.description = '';
            state.purchaseOrderId = null;
            state.billStatus = null;
            state.errors = {
                billDate: '',
                purchaseOrderId: '',
                billStatus: '',
                description: ''
            };
            state.purchaseOrderData = null;
            state.secondaryData = [];
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/Bill/GetBillList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (billDate, description, billStatus, purchaseOrderId, createdById) => {
                try {
                    const response = await AxiosManager.post('/Bill/CreateBill', {
                        billDate, description, billStatus, purchaseOrderId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, billDate, description, billStatus, purchaseOrderId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/Bill/UpdateBill', {
                        id, billDate, description, billStatus, purchaseOrderId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/Bill/DeleteBill', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPurchaseOrderListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/PurchaseOrder/GetPurchaseOrderList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPurchaseOrderSingleData: async (purchaseOrderId) => {
                try {
                    const response = await AxiosManager.get('/PurchaseOrder/GetPurchaseOrderSingle?id=' + purchaseOrderId, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getBillStatusListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Bill/GetBillStatusList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            }
        };

        const methods = {
            populatePurchaseOrderListLookupData: async () => {
                const response = await services.getPurchaseOrderListLookupData();
                state.purchaseOrderListLookupData = response?.data?.content?.data;
            },
            populateBillStatusListLookupData: async () => {
                const response = await services.getBillStatusListLookupData();
                state.billStatusListLookupData = response?.data?.content?.data;
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    billDate: new Date(item.billDate),
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
            populatePurchaseOrderSingleData: async (purchaseOrderId) => {
                const response = await services.getPurchaseOrderSingleData(purchaseOrderId);
                state.purchaseOrderData = response?.data?.content?.data;
                state.secondaryData = response?.data?.content?.data?.purchaseOrderItemList;
            },
            handleFormSubmit: async () => {

                try {
                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 200));

                    if (!validateForm()) {
                        return;
                    }

                    const response = state.id === ''
                        ? await services.createMainData(state.billDate, state.description, state.billStatus, state.purchaseOrderId, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.billDate, state.description, state.billStatus, state.purchaseOrderId, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = 'Edit Bill';
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.billDate = response?.data?.content?.data.billDate ? new Date(response.data.content.data.billDate) : null;
                            state.description = response?.data?.content?.data.description ?? '';
                            state.purchaseOrderId = response?.data?.content?.data.purchaseOrderId ?? '';
                            state.billStatus = String(response?.data?.content?.data.billStatus ?? '');

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
                state.errors.billDate = '';
                state.errors.purchaseOrderId = '';
                state.errors.billStatus = '';
            },
            refreshPaymentSummary: async () => {
                state.subTotalAmount = NumberFormatManager.formatToLocale(state?.purchaseOrderData?.beforeTaxAmount ?? 0);
                state.taxAmount = NumberFormatManager.formatToLocale(state?.purchaseOrderData?.taxAmount ?? 0);
                state.totalAmount = NumberFormatManager.formatToLocale(state?.purchaseOrderData?.afterTaxAmount ?? 0);
            },
        };

        const purchaseOrderListLookup = {
            obj: null,
            create: () => {
                if (state.purchaseOrderListLookupData && Array.isArray(state.purchaseOrderListLookupData)) {
                    purchaseOrderListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.purchaseOrderListLookupData,
                        fields: { value: 'id', text: 'number' },
                        placeholder: 'Select Purchase Order',
                        filterBarPlaceholder: 'Search',
                        sortOrder: 'Ascending',
                        allowFiltering: true,
                        filtering: (e) => {
                            e.preventDefaultAction = true;
                            let query = new ej.data.Query();
                            if (e.text !== '') {
                                query = query.where('number', 'contains', e.text, true);
                            }
                            e.updateData(state.purchaseOrderListLookupData, query);
                        },
                        change: async (e) => {
                            state.purchaseOrderId = e.value;
                        }
                    });
                    purchaseOrderListLookup.obj.appendTo(purchaseOrderIdRef.value);
                }
            },
            refresh: () => {
                if (purchaseOrderListLookup.obj) {
                    purchaseOrderListLookup.obj.value = state.purchaseOrderId;
                }
            }
        };

        const billStatusListLookup = {
            obj: null,
            create: () => {
                if (state.billStatusListLookupData && Array.isArray(state.billStatusListLookupData)) {
                    billStatusListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.billStatusListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select Bill Status',
                        change: (e) => {
                            state.billStatus = e.value;
                        }
                    });
                    billStatusListLookup.obj.appendTo(billStatusRef.value);
                }
            },
            refresh: () => {
                if (billStatusListLookup.obj) {
                    billStatusListLookup.obj.value = state.billStatus;
                }
            }
        };

        const billDatePicker = {
            obj: null,
            create: () => {
                billDatePicker.obj = new ej.calendars.DatePicker({
                    format: 'yyyy-MM-dd',
                    value: state.billDate ? new Date(state.billDate) : null,
                    change: (e) => {
                        state.billDate = e.value;
                    }
                });
                billDatePicker.obj.appendTo(billDateRef.value);
            },
            refresh: () => {
                if (billDatePicker.obj) {
                    billDatePicker.obj.value = state.billDate ? new Date(state.billDate) : null;
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
            () => state.billDate,
            (newVal, oldVal) => {
                billDatePicker.refresh();
                state.errors.billDate = '';
            }
        );

        Vue.watch(
            () => state.purchaseOrderId,
            async (newVal, oldVal) => {
                purchaseOrderListLookup.refresh();
                state.errors.purchaseOrderId = '';
                await methods.populatePurchaseOrderSingleData(newVal);
            }
        );

        Vue.watch(
            () => state.purchaseOrderData,
            async (newVal, oldVal) => {
                await methods.refreshPaymentSummary();
                secondaryGrid.refresh();
            }
        );

        Vue.watch(
            () => state.billStatus,
            (newVal, oldVal) => {
                billStatusListLookup.refresh();
                state.errors.billStatus = '';
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
                        { field: 'billDate', headerText: 'Bill Date', width: 150, format: 'yyyy-MM-dd' },
                        { field: 'purchaseOrderNumber', headerText: 'Purchase Order', width: 200, minWidth: 200 },
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
                        mainGrid.obj.autoFitColumns(['number', 'billDate', 'purchaseOrderNumber', 'afterTaxAmount', 'statusName', 'createdAtUtc']);
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
                            state.mainTitle = 'Add Bill';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Bill';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.billDate = selectedRecord.billDate ? new Date(selectedRecord.billDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.purchaseOrderId = selectedRecord.purchaseOrderId ?? '';
                                state.billStatus = String(selectedRecord.billStatus ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Bill?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.billDate = selectedRecord.billDate ? new Date(selectedRecord.billDate) : null;
                                state.description = selectedRecord.description ?? '';
                                state.purchaseOrderId = selectedRecord.purchaseOrderId ?? '';
                                state.billStatus = String(selectedRecord.billStatus ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'PrintPDFCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                window.open('/Bills/BillPdf?id=' + (selectedRecord.id ?? ''), '_blank');
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
                await SecurityManager.authorizePage(['Bills']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                await secondaryGrid.create(state.secondaryData);

                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', methods.onMainModalHidden);
                await methods.populatePurchaseOrderListLookupData();
                purchaseOrderListLookup.create();
                await methods.populateBillStatusListLookupData();
                billStatusListLookup.create();
                billDatePicker.create();
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
            billDateRef,
            numberRef,
            purchaseOrderIdRef,
            billStatusRef,
            state,
            methods,
            handler: {
                handleSubmit: methods.handleFormSubmit
            }
        };
    }
};

Vue.createApp(App).mount('#app');