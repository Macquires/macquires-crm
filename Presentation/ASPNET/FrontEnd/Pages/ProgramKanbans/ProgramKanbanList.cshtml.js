const App = {
    setup() {
        const state = Vue.reactive({
            programManagerResourceLookupData: [],
            programManagerStatusLookupData: [],
            programManagerPriorityLookupData: [],
        });

        const kanbanRef = Vue.ref(null);

        const services = {
            getProgramManagerResourceLookupData: async () => {
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
            populateProgramManagerResourceLookupData: async () => {
                const response = await services.getProgramManagerResourceLookupData();
                state.programManagerResourceLookupData = response?.data?.content?.data?.map(item => ({
                    id: item.id,
                    name: item.name
                }));
            },
            populateProgramManagerStatusListLookupData: async () => {
                const response = await services.getProgramManagerStatusListLookupData();
                state.programManagerStatusLookupData = response?.data?.content?.data;
            },
            populateProgramManagerPriorityListLookupData: async () => {
                const response = await services.getProgramManagerPriorityListLookupData();
                state.programManagerPriorityLookupData = response?.data?.content?.data;
            },
            populateKanban: () => {
                const statusData = state.programManagerStatusLookupData.map(item => item.name);
                const priorityData = state.programManagerPriorityLookupData.map(item => item.name);
                const resourceData = state.programManagerResourceLookupData.map(item => ({
                    programResourceId: item.id,
                    programResource: item.name
                }));

                const accessToken = StorageManager.getAccessToken();
                var dataManager = new ej.data.DataManager({
                    url: '/api/ProgramManager/KanbanGet',
                    updateUrl: '/api/ProgramManager/KanbanUpdate',
                    removeUrl: '/api/ProgramManager/KanbanDelete',
                    adaptor: new ej.data.UrlAdaptor(),
                    headers: [
                        { Authorization: `Bearer ${accessToken}` }
                    ]
                });

                var kanbanObj = new ej.kanban.Kanban({
                    dataSource: dataManager,
                    width: 'auto',
                    height: 'auto',
                    enableTooltip: true,
                    keyField: 'status',
                    columns: [
                        { headerText: 'Draft', keyField: 'Draft', template: '#headerTemplate', allowToggle: true },
                        { headerText: 'Confirmed', keyField: 'Confirmed', template: '#headerTemplate', allowToggle: true },
                        { headerText: 'OnProgress', keyField: 'OnProgress', template: '#headerTemplate', allowToggle: true },
                        { headerText: 'Done', keyField: 'Done', template: '#headerTemplate', allowToggle: true },
                        { headerText: 'Cancelled', keyField: 'Cancelled', template: '#headerTemplate', allowToggle: true, isExpanded: false }
                    ],
                    cardSettings: {
                        headerField: 'title',
                        template: '#cardTemplate',
                        selectionType: 'Multiple',
                        contentField: 'priority',
                    },
                    swimlaneSettings: {
                        keyField: 'programManagerResource',
                    },
                    allowDragAndDrop: true,
                    sortSettings: {
                        sortBy: 'Custom',
                        field: 'number',
                        direction: 'Descending'
                    },
                    dialogSettings: {
                        template: '#dialogTemplate'
                    },
                    cardRendered: function (args) {
                        ej.base.addClass([args.element], args.data.priority);
                    },
                    dialogOpen: (args) => {
                        var curData = args.data;

                        var filledTextBox = new ej.inputs.TextBox({});
                        filledTextBox.appendTo(args.element.querySelector('#title'));

                        var resourceDropObj = new ej.dropdowns.DropDownList({
                            value: curData.programManagerResource, popupHeight: '300px',
                            dataSource: resourceData, fields: { text: 'programResource', value: 'programResource' }, placeholder: 'Resource'
                        });
                        resourceDropObj.appendTo(args.element.querySelector('#programManagerResource'));

                        var statusDropObj = new ej.dropdowns.DropDownList({
                            value: curData.status, popupHeight: '300px',
                            dataSource: statusData, fields: { text: 'status', value: 'status' }, placeholder: 'Status'
                        });
                        statusDropObj.appendTo(args.element.querySelector('#status'));

                        var priorityObj = new ej.dropdowns.DropDownList({
                            value: curData.priority, popupHeight: '300px',
                            dataSource: priorityData, fields: { text: 'priority', value: 'priority' }, placeholder: 'Priority'
                        });
                        priorityObj.appendTo(args.element.querySelector('#priority'));

                        var textareaObj = new ej.inputs.TextBox({
                            placeholder: 'Summary',
                            multiline: true
                        });
                        textareaObj.appendTo(args.element.querySelector('#summary'));

                    }
                });
                kanbanObj.appendTo(kanbanRef.value);
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['ProgramKanbans']);
                await SecurityManager.validateToken();

                setFormCardHeight();

                await methods.populateProgramManagerResourceLookupData();
                await methods.populateProgramManagerStatusListLookupData();
                await methods.populateProgramManagerPriorityListLookupData();

                methods.populateKanban();

            } catch (e) {
                console.error('page init error:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return {
            kanbanRef,
        };
    }
};

Vue.createApp(App).mount('#app');