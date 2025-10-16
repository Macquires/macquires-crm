const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
        });

        const mainGridRef = Vue.ref(null);

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/PurchaseReturn/GetPurchaseReturnReport', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    createdAtUtc: new Date(item.inventoryTransaction.createdAtUtc)
                }));
            },
        };

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
                    groupSettings: {
                        columns: ['purchaseReturn.number']
                    },
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    sortSettings: { columns: [{ field: 'purchaseReturn.number', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'inventoryTransaction.id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'purchaseReturn.number', headerText: 'PurchaseReturn', width: 200, minWidth: 200 },
                        { field: 'purchaseReturn.goodsReceive.number', headerText: 'GoodsReceive', width: 200, minWidth: 200 },
                        { field: 'purchaseReturn.goodsReceive.purchaseOrder.vendor.name', headerText: 'Vendor', width: 200, minWidth: 200 },
                        { field: 'inventoryTransaction.warehouse.name', headerText: 'Warehouse', width: 200, minWidth: 200 },
                        { field: 'inventoryTransaction.product.number', headerText: 'Product Number', width: 200, minWidth: 200 },
                        { field: 'inventoryTransaction.product.name', headerText: 'Product Name', width: 200, minWidth: 200 },
                        { field: 'inventoryTransaction.movement', headerText: 'Quantity', width: 150, minWidth: 150 },
                        { field: 'createdAtUtc', headerText: 'Created At UTC', width: 150, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    aggregates: [
                        {
                            columns: [
                                {
                                    type: 'Sum',
                                    field: 'inventoryTransaction.movement',
                                    groupCaptionTemplate: 'Total: ${Sum}',
                                    format: 'N2'
                                }
                            ]
                        }
                    ],
                    toolbar: [
                        'ExcelExport', 'Search',
                        { type: 'Separator' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.autoFitColumns(['purchaseReturn.number', 'purchaseReturn.goodsReceive.number', 'purchaseReturn.goodsReceive.purchaseOrder.vendor.name', 'inventoryTransaction.warehouse.name', 'inventoryTransaction.product.number', 'inventoryTransaction.product.name', 'inventoryTransaction.movement', 'createdAtUtc']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => { },
                    rowDeselected: () => { },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            mainGrid.obj.excelExport();
                        }
                    }
                });

                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            }
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['PurchaseReturnReports']);
                await SecurityManager.validateToken();
                await methods.populateMainData();
                await mainGrid.create(state.mainData);
            } catch (e) {
                console.error('page init error:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return {
            mainGridRef,
            state,
        };
    }
};

Vue.createApp(App).mount('#app');