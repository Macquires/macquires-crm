const App = {
    setup() {
        const state = Vue.reactive({
            cardsData: {},
            salesData: {},
            purchaseData: {},
            inventoryData: {},
        });

        const cardSalesQtyRef = Vue.ref(null);
        const cardSalesReturnQtyRef = Vue.ref(null);
        const cardPurchaseQtyRef = Vue.ref(null);
        const cardPurchaseReturnQtyRef = Vue.ref(null);
        const cardDeliveryOrderQtyRef = Vue.ref(null);
        const cardGoodsReceiveQtyRef = Vue.ref(null);
        const cardTransferOutQtyRef = Vue.ref(null);
        const cardTransferInQtyRef = Vue.ref(null);

        const salesOrderGridRef = Vue.ref(null);
        const inventoryTransactionGridRef = Vue.ref(null);
        const purchaseOrderGridRef = Vue.ref(null);
        const customerGroupChartRef = Vue.ref(null);
        const vendorGroupChartRef = Vue.ref(null);
        const customerCategoryChartRef = Vue.ref(null);
        const vendorCategoryChartRef = Vue.ref(null);
        const stockChartRef = Vue.ref(null);


        const leadPipelineFunnelRef = Vue.ref(null);
        const salesTeamLeadClosingRef = Vue.ref(null);
        const campaignByStatusRef = Vue.ref(null);
        const leadActivityByTypeRef = Vue.ref(null);

        const services = {
            getCardsData: async () => {
                try {
                    const response = await AxiosManager.get('/Dashboard/GetCardsDashboard', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSalesData: async () => {
                try {
                    const response = await AxiosManager.get('/Dashboard/GetSalesDashboard', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getPurchaseData: async () => {
                try {
                    const response = await AxiosManager.get('/Dashboard/GetPurchaseDashboard', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getInventoryData: async () => {
                try {
                    const response = await AxiosManager.get('/Dashboard/GetInventoryDashboard', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getLeadPipelineFunnelData: async () => {
                try {
                    const response = await AxiosManager.get('/Dashboard/GetLeadPipelineFunnel', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSalesTeamLeadClosingData: async () => {
                try {
                    const response = await AxiosManager.get('/Dashboard/GetSalesTeamLeadClosing', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getCampaignByStatusData: async () => {
                try {
                    const response = await AxiosManager.get('/Dashboard/GetCampaignByStatus', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getLeadActivityByTypeData: async () => {
                try {
                    const response = await AxiosManager.get('/Dashboard/GetLeadActivityByType', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            populateCardsData: async () => {
                const response = await services.getCardsData();
                state.cardsData = response?.data?.content?.data;
                methods.populateCards();
            },
            populateSalesData: async () => {
                const response = await services.getSalesData();
                state.salesData = response?.data?.content?.data;
                methods.populateSalesOrderGrid();
                methods.populateSalesByCustomerGroupChart();
                methods.populateSalesByCustomerCategoryChart();
            },
            populatePurchaseData: async () => {
                const response = await services.getPurchaseData();
                state.purchaseData = response?.data?.content?.data;
                methods.populatePurchaseOrderGrid();
                methods.populatePurchaseByVendorGroupChart();
                methods.populatePurchaseByVendorCategoryChart();
            },
            populateInventoryData: async () => {
                const response = await services.getInventoryData();
                state.inventoryData = response?.data?.content?.data;
                methods.populateInventoryTransactionGrid();
                methods.populateInventoryStockChart();
            },
            populateCards: () => {
                const cardsDashboard = state.cardsData?.cardsDashboard;

                if (cardsDashboard) {
                    cardSalesQtyRef.value.textContent = cardsDashboard.salesTotal || 0;
                    cardSalesReturnQtyRef.value.textContent = cardsDashboard.salesReturnTotal || 0;
                    cardPurchaseQtyRef.value.textContent = cardsDashboard.purchaseTotal || 0;
                    cardPurchaseReturnQtyRef.value.textContent = cardsDashboard.purchaseReturnTotal || 0;
                    cardDeliveryOrderQtyRef.value.textContent = cardsDashboard.deliveryOrderTotal || 0;
                    cardGoodsReceiveQtyRef.value.textContent = cardsDashboard.goodsReceiveTotal || 0;
                    cardTransferOutQtyRef.value.textContent = cardsDashboard.transferOutTotal || 0;
                    cardTransferInQtyRef.value.textContent = cardsDashboard.transferInTotal || 0;
                } else {
                    console.error('CardsDashboard data is not available.');
                }
            },
            populateSalesOrderGrid: () => {
                const salesOrderDashboard = state.salesData?.salesOrderDashboard ?? [];
                new ej.grids.Grid({
                    dataSource: salesOrderDashboard,
                    allowFiltering: false,
                    allowSorting: true,
                    allowSelection: false,
                    allowGrouping: false,
                    allowTextWrap: false,
                    allowResizing: false,
                    allowPaging: true,
                    allowExcelExport: false,
                    sortSettings: { columns: [{ field: 'orderDate', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 10 },
                    autoFit: false,
                    showColumnMenu: false,
                    gridLines: 'Horizontal',
                    columns: [
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'salesOrder.orderDate', headerText: 'Order Date', width: 70, type: 'dateTime', format: 'yyyy-MM-dd', textAlign: 'Left' },
                        { field: 'salesOrder.number', headerText: '#Number', width: 90 },
                        { field: 'product.name', headerText: 'Product', width: 150 },
                        { field: 'total', headerText: 'Total', width: 70, type: 'number', format: 'N2', textAlign: 'Right' },
                    ],
                }, salesOrderGridRef.value);
            },
            populateInventoryTransactionGrid: () => {
                const inventoryTransactionDashboard = state.inventoryData?.inventoryTransactionDashboard ?? [];
                new ej.grids.Grid({
                    dataSource: inventoryTransactionDashboard,
                    allowFiltering: false,
                    allowSorting: true,
                    allowSelection: false,
                    allowGrouping: false,
                    allowTextWrap: false,
                    allowResizing: false,
                    allowPaging: true,
                    allowExcelExport: false,
                    sortSettings: { columns: [{ field: 'movementDate', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 10 },
                    autoFit: false,
                    showColumnMenu: false,
                    gridLines: 'Horizontal',
                    columns: [
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'movementDate', headerText: 'Date', width: 150, format: 'yyyy-MM-dd', textAlign: 'Left', type: 'dateTime' },
                        { field: 'warehouse.name', headerText: 'Warehouse', width: 150 },
                        { field: 'product.name', headerText: 'Product', width: 150 },
                        { field: 'number', headerText: 'Number', width: 150 },
                        { field: 'stock', headerText: 'Movement', width: 100, type: 'number', format: '+0.00;-0.00;0.00', textAlign: 'Right' },
                        { field: 'moduleName', headerText: 'Module Name', width: 150 },
                        { field: 'moduleCode', headerText: 'Module Code', width: 150 },
                        { field: 'moduleNumber', headerText: 'Module Number', width: 150 },
                        { field: 'warehouseFrom.name', headerText: 'Warehouse From', width: 150 },
                        { field: 'warehouseTo.name', headerText: 'Warehouse To', width: 150 },
                    ],
                }, inventoryTransactionGridRef.value);
            },
            populatePurchaseOrderGrid: () => {
                const purchaseOrderDashboard = state.purchaseData?.purchaseOrderDashboard ?? [];
                new ej.grids.Grid({
                    dataSource: purchaseOrderDashboard,
                    allowFiltering: false,
                    allowSorting: true,
                    allowSelection: false,
                    allowGrouping: false,
                    allowTextWrap: false,
                    allowResizing: false,
                    allowPaging: true,
                    allowExcelExport: false,
                    sortSettings: { columns: [{ field: 'orderDate', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 10 },
                    autoFit: false,
                    showColumnMenu: false,
                    gridLines: 'Horizontal',
                    columns: [
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'purchaseOrder.orderDate', headerText: 'Order Date', width: 70, type: 'dateTime', format: 'yyyy-MM-dd', textAlign: 'Left' },
                        { field: 'purchaseOrder.number', headerText: '#Number', width: 90 },
                        { field: 'product.name', headerText: 'Product', width: 150 },
                        { field: 'total', headerText: 'Total', width: 70, type: 'number', format: 'N2', textAlign: 'Right' },
                    ],
                }, purchaseOrderGridRef.value);
            },
            populateSalesByCustomerGroupChart: () => {
                const salesByCustomerGroupDashboard = state.salesData?.salesByCustomerGroupDashboard ?? [];
                new ej.charts.Chart(
                    {
                        primaryXAxis: {
                            valueType: 'Category', interval: 1, majorGridLines: { width: 0 }, majorTickLines: { width: 0 }, labelIntersectAction: 'None', labelRotation: ej.base.Browser.isDevice ? -45 : 0, minorTickLines: { width: 0 }
                        },
                        chartArea: { border: { width: 0 } },
                        primaryYAxis: {
                            title: 'Quantity',
                            majorTickLines: { width: 0 }, lineStyle: { width: 0 },
                        },
                        series: salesByCustomerGroupDashboard,
                        title: 'Sales by Customer Group',
                        tooltip: { enable: true, header: "<b>${point.tooltip}</b>", shared: true },
                        legendSettings: { enableHighlight: true },
                        palettes: ["#E94649", "#F6B53F", "#009CFF", "#C4C24A"],
                    },
                    customerGroupChartRef.value);
            },
            populatePurchaseByVendorGroupChart: () => {
                const purchaseByVendorGroupDashboard = state.purchaseData?.purchaseByVendorGroupDashboard ?? [];
                new ej.charts.Chart(
                    {
                        primaryXAxis: {
                            valueType: 'Category', interval: 1, majorGridLines: { width: 0 }, majorTickLines: { width: 0 }, labelIntersectAction: 'None', labelRotation: ej.base.Browser.isDevice ? -45 : 0, minorTickLines: { width: 0 }
                        },
                        chartArea: { border: { width: 0 } },
                        primaryYAxis: {
                            title: 'Quantity',
                            majorTickLines: { width: 0 }, lineStyle: { width: 0 },
                        },
                        series: purchaseByVendorGroupDashboard,
                        title: 'Purchase by Vendor Group',
                        tooltip: { enable: true, header: "<b>${point.tooltip}</b>", shared: true },
                        legendSettings: { enableHighlight: true },
                        palettes: ["#E94649", "#F6B53F", "#009CFF", "#C4C24A"],
                    },
                    vendorGroupChartRef.value);
            },
            populateSalesByCustomerCategoryChart: () => {
                const salesByCustomerCategoryDashboard = state.salesData?.salesByCustomerCategoryDashboard ?? [];
                new ej.charts.Chart(
                    {
                        primaryXAxis: {
                            valueType: 'Category', interval: 1, majorGridLines: { width: 0 }, majorTickLines: { width: 0 }, labelIntersectAction: 'None', labelRotation: ej.base.Browser.isDevice ? -45 : 0, minorTickLines: { width: 0 }
                        },
                        chartArea: { border: { width: 0 } },
                        primaryYAxis: {
                            title: 'Quantity',
                            majorTickLines: { width: 0 }, lineStyle: { width: 0 },
                        },
                        series: salesByCustomerCategoryDashboard,
                        title: 'Sales by Customer Category',
                        tooltip: { enable: true, header: "<b>${point.tooltip}</b>", shared: true },
                        legendSettings: { enableHighlight: true },
                        palettes: ["#E94649", "#F6B53F", "#009CFF", "#C4C24A"],
                    },
                    customerCategoryChartRef.value);
            },
            populatePurchaseByVendorCategoryChart: () => {
                const purchaseByVendorCategoryDashboard = state.purchaseData?.purchaseByVendorCategoryDashboard ?? [];
                new ej.charts.Chart(
                    {
                        primaryXAxis: {
                            valueType: 'Category', interval: 1, majorGridLines: { width: 0 }, majorTickLines: { width: 0 }, labelIntersectAction: 'None', labelRotation: ej.base.Browser.isDevice ? -45 : 0, minorTickLines: { width: 0 }
                        },
                        chartArea: { border: { width: 0 } },
                        primaryYAxis: {
                            title: 'Quantity',
                            majorTickLines: { width: 0 }, lineStyle: { width: 0 },
                        },
                        series: purchaseByVendorCategoryDashboard,
                        title: 'Purchase by Vendor Category',
                        tooltip: { enable: true, header: "<b>${point.tooltip}</b>", shared: true },
                        legendSettings: { enableHighlight: true },
                        palettes: ["#E94649", "#F6B53F", "#009CFF", "#C4C24A"],
                    },
                    vendorCategoryChartRef.value);
            },
            populateInventoryStockChart: () => {
                const inventoryStockDashboard = state.inventoryData?.inventoryStockDashboard ?? [];
                new ej.charts.Chart(
                    {
                        primaryXAxis: {
                            valueType: 'Category', interval: 1, majorGridLines: { width: 0 }, majorTickLines: { width: 0 }, labelIntersectAction: 'None', labelRotation: -15, minorTickLines: { width: 0 }
                        },
                        chartArea: { border: { width: 0 } },
                        primaryYAxis: {
                            title: 'Quantity',
                            majorTickLines: { width: 0 }, lineStyle: { width: 0 },
                        },
                        series: inventoryStockDashboard,
                        title: 'Stock by Warehouse',
                        tooltip: { enable: true, header: "<b>${point.tooltip}</b>", shared: true },
                        legendSettings: { visible: true },
                        palettes: ["#E94649", "#F6B53F", "#009CFF", "#C4C24A"],
                    },
                    stockChartRef.value);
            },
            populateLeadPipelineFunnelData: async () => {
                methods.populateLeadPipelineFunnel();
            },
            populateLeadPipelineFunnel: async () => {
                const response = await services.getLeadPipelineFunnelData();
                const leadPipelineFunnelData = response?.data?.content?.data ?? [];
                
                new ej.charts.AccumulationChart({
                    series: [{
                        type: 'Funnel',
                        dataSource: leadPipelineFunnelData,
                        xName: 'x', yName: 'y',
                        dataLabel: { name: 'text', visible: true, position: 'Inside', font: { fontWeight: '600' }, connectorStyle: { length: '20px' } },
                        gapRatio: 0.03,
                        neckWidth: '50%', neckHeight: '30%',
                        width: '100%', height: '100%'
                    }
                    ],
                    legendSettings: { visible: false },
                    title: 'Lead Pipeline Funnel',
                }, leadPipelineFunnelRef.value);
            },
            populateSalesTeamLeadClosingPie: async () => {
                const response = await services.getSalesTeamLeadClosingData();
                const salesTeamLeadClosingData = response?.data?.content?.data ?? [];

                new ej.charts.AccumulationChart({
                    enableSmartLabels: true,
                    selectionMode: 'Point',
                    annotations: [{
                        content: '<div><strong>Closing</strong></div>',
                        region: "Series",
                        x: "52%",
                        y: "50%"
                    }],
                    series: [
                        {
                            dataSource: salesTeamLeadClosingData,
                            xName: 'x', yName: 'y', startAngle: 30,
                            innerRadius: '50%', radius: ej.base.Browser.isDevice ? '80%' : '85%',
                            dataLabel: {
                                visible: true, position: 'Inside',
                                font: { fontWeight: '600', color: '#ffffff' },
                            },
                        }
                    ],
                    legendSettings: {
                        visible: true, toggleVisibility: false,
                        position: 'Bottom',
                        maximumColumns: ej.base.Browser.isDevice ? 2 : 3,
                        fixedWidth: true
                    },
                    title: 'Sales Team Achievement',
                    enableBorderOnMouseMove: false,
                    textRender: function (args) {
                        args.series.dataLabel.font.size = '13px';
                        args.text = args.text + '%';
                    },
                    pointRender: function (args) {
                        var selectedTheme = location.hash.split('/')[1];
                        selectedTheme = selectedTheme ? selectedTheme : 'Fluent2';
                        if (selectedTheme === 'fluent2') {
                            args.fill = fluent2Colors[args.point.index % 10];
                        }
                    },
                }, salesTeamLeadClosingRef.value);
            },
            populateCampaignByStatus: async () => {
                const response = await services.getCampaignByStatusData();
                const campaignByStatusData = response?.data?.content?.data ?? [];

                new ej.charts.AccumulationChart({
                    series: [{
                        type: 'Pie',
                        dataSource: campaignByStatusData,
                        animation: { enable: true },
                        xName: 'x',
                        yName: 'y',
                        innerRadius: '50%',
                        dataLabel: {
                            visible: true,
                            position: 'Outside',
                            name: 'x',
                            connectorStyle: { width: 0 },
                        },
                        borderRadius: 8,
                        border: { width: 3 }
                    }],
                    tooltip: {
                        enable: true,
                        header: '<b>Campaign Status</b>', format: '${point.x}: <b>${point.y}%</b>',
                        enableHighlight: true
                    },
                    title: 'Campaign By Status',
                    enableSmartLabels: true,
                    enableBorderOnMouseMove: false,
                    legendSettings: {
                        visible: false
                    },
                    annotations: [{
                        content: '<div><strong>Status</strong></div>',
                        region: "Series",
                        x: "52%",
                        y: "50%"
                    }],
                    load: function (args) {
                        var selectedTheme = location.hash.split('/')[1];
                        selectedTheme = selectedTheme ? selectedTheme : 'Fluent2';
                        args.accumulation.theme = (selectedTheme.charAt(0).toUpperCase() +
                            selectedTheme.slice(1)).replace(/-dark/i, 'Dark').replace(/contrast/i, 'Contrast').replace(/-highContrast/i, 'HighContrast');
                    },
                    pointRender: function (args) {
                        var selectedTheme = location.hash.split('/')[1];
                        selectedTheme = selectedTheme ? selectedTheme : 'Fluent2';
                        if (selectedTheme.indexOf('dark') > -1) {
                            if (selectedTheme.indexOf('material') > -1) {
                                args.border.color = '#303030';
                            }
                            else if (selectedTheme.indexOf('bootstrap5') > -1) {
                                args.border.color = '#212529';
                            }
                            else if (selectedTheme.indexOf('bootstrap') > -1) {
                                args.border.color = '#1A1A1A';

                            }
                            else if (selectedTheme.indexOf('fabric') > -1) {
                                args.border.color = '#201f1f';

                            }
                            else if (selectedTheme.indexOf('fluent') > -1) {
                                args.border.color = '#252423';

                            }
                            else if (selectedTheme.indexOf('bootstrap') > -1) {
                                args.border.color = '#1A1A1A';

                            }
                            else if (selectedTheme.indexOf('tailwind') > -1) {
                                args.border.color = '#1F2937';

                            }
                            else {
                                args.border.color = '#222222';

                            }
                        }
                        else if (selectedTheme.indexOf('highcontrast') > -1) {
                            args.border.color = '#000000';
                        }
                        else {
                            args.border.color = '#FFFFFF';
                        }
                    }
                }, campaignByStatusRef.value);
            },
            populateLeadActivityByType: async () => {
                const response = await services.getLeadActivityByTypeData();
                const leadActivityByTypeData = response?.data?.content?.data ?? [];

                new ej.charts.AccumulationChart({
                    series: [{
                        type: 'Pyramid',
                        dataSource: leadActivityByTypeData,
                        xName: 'x', yName: 'y',
                        dataLabel: { name: 'text', visible: true, position: 'Inside', font: { fontWeight: '600' }, connectorStyle: { length: '20px' } },
                        gapRatio: 0.03,
                        neckWidth: '50%', neckHeight: '30%',
                        width: '100%', height: '100%'
                    }
                    ],
                    legendSettings: { visible: false },
                    title: 'Lead Activity By Type',
                }, leadActivityByTypeRef.value);
            },
        };


        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['Dashboards']);
                await SecurityManager.validateToken();

                await methods.populateLeadPipelineFunnel();
                await methods.populateSalesTeamLeadClosingPie();
                await methods.populateCampaignByStatus();
                await methods.populateLeadActivityByType();

                hideSpinnerAndShowContent();

                await methods.populateCardsData();
                await methods.populateSalesData();
                await methods.populatePurchaseData();
                await methods.populateInventoryData();

                setFormCardHeight();

            } catch (e) {
                console.error('page init error:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return {
            cardSalesQtyRef,
            cardSalesReturnQtyRef,
            cardPurchaseQtyRef,
            cardPurchaseReturnQtyRef,
            cardDeliveryOrderQtyRef,
            cardGoodsReceiveQtyRef,
            cardTransferOutQtyRef,
            cardTransferInQtyRef,
            salesOrderGridRef,
            inventoryTransactionGridRef,
            purchaseOrderGridRef,
            customerGroupChartRef,
            vendorGroupChartRef,
            customerCategoryChartRef,
            vendorCategoryChartRef,
            stockChartRef,
            state,
            methods,
            leadPipelineFunnelRef,
            salesTeamLeadClosingRef,
            campaignByStatusRef,
            leadActivityByTypeRef,
        };
    }
};

Vue.createApp(App).mount('#app');