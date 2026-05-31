const App = {
    setup() {
        const mainGridRef = Vue.ref(null);
        let mainGrid = null;

        const services = {
            load: () => AxiosManager.get('/NumberSequence/GetNumberSequenceList', {}),
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['TelecomAdmin']);
                await SecurityManager.validateToken();

                const res = await services.load();
                const rows = (res?.data?.content?.data || []).map((item) => ({
                    ...item,
                    createdAtUtc: item.createdAtUtc ? new Date(item.createdAtUtc) : null,
                }));

                mainGrid = new ej.grids.Grid({
                    height: getDashminGridHeight(),
                    dataSource: rows,
                    allowFiltering: true,
                    allowSorting: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    sortSettings: { columns: [{ field: 'entityName', direction: 'Ascending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ['10', '20', '50', '100', 'All'] },
                    gridLines: 'Horizontal',
                    columns: [
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'entityName', headerText: 'Entity', width: 180 },
                        { field: 'prefix', headerText: 'Prefix', width: 120 },
                        { field: 'suffix', headerText: 'Suffix', width: 120 },
                        { field: 'lastUsedCount', headerText: 'Last count', width: 120 },
                        { field: 'createdAtUtc', headerText: 'Created', width: 160, format: 'yyyy-MM-dd HH:mm' },
                    ],
                    toolbar: ['ExcelExport', 'Search'],
                });
                mainGrid.appendTo(mainGridRef.value);
            } catch (e) {
                console.error('NumberSequenceList init:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return { mainGridRef };
    },
};

Vue.createApp(App).mount('#app');
