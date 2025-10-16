const App = {
    setup() {
        const state = Vue.reactive({
            bookingResourceLookupData: [],
            bookingGroupLookupData: [],
        });

        const scheduleRef = Vue.ref(null);

        const services = {
            getBookingResourceLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/BookingResource/GetBookingResourceList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getBookingGroupLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/BookingGroup/GetBookingGroupList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            populateBookingResourceLookupData: async () => {
                const response = await services.getBookingResourceLookupData();
                state.bookingResourceLookupData = response?.data?.content?.data?.map(item => ({
                    id: item.id,
                    name: item.name,
                    bookingGroupId: item.bookingGroupId
                }));
            },

            populateBookingGroupLookupData: async () => {
                const response = await services.getBookingGroupLookupData();
                state.bookingGroupLookupData = response?.data?.content?.data?.map(item => ({
                    id: item.id,
                    name: item.name
                }));
            },

            populateScheduler: () => {
                const accessToken = StorageManager.getAccessToken();

                var dataManager = new ej.data.DataManager({
                    url: '/api/Booking/SchedulerGet',
                    crudUrl: '/api/Booking/SchedulerCRUD',
                    adaptor: new ej.data.UrlAdaptor(),
                    headers: [
                        { Authorization: `Bearer ${accessToken}` }
                    ]
                });

                var scheduleObj = new ej.schedule.Schedule({
                    width: '100%',
                    height: getDashminSchedulerHeight(),
                    views: ['TimelineDay', 'TimelineWeek', 'TimelineWorkWeek', 'TimelineMonth', 'Agenda'],
                    currentView: 'TimelineMonth',
                    workDays: [0, 1, 2, 3, 4, 5],
                    eventSettings: {
                        dataSource: dataManager,
                        fields: {
                            id: 'id',
                            subject: { name: 'subject' },
                            location: { name: 'location' },
                            description: { name: 'description' },
                            startTime: { name: 'startTime' },
                            endTime: { name: 'endTime' },
                            recurrenceRule: { name: 'recurrenceRule' }
                        }
                    },
                    group: {
                        enableCompactView: false,
                        resources: ['BookingGroup', 'BookingResource']
                    },
                    resources: [
                        {
                            field: 'bookingGroupId',
                            name: 'BookingGroup',
                            title: 'Booking Group',
                            allowMultiple: false,
                            dataSource: state.bookingGroupLookupData,
                            textField: 'name', idField: 'id'
                        },
                        {
                            field: 'bookingResourceId',
                            name: 'BookingResource',
                            title: 'Booking Resource',
                            allowMultiple: false,
                            dataSource: state.bookingResourceLookupData,
                            textField: 'name', idField: 'id', groupIDField: 'bookingGroupId'
                        }
                    ],
                    popupOpen: (args) => {
                        if (args.type === 'Editor') {
                            if (!args.element.querySelector('.custom-field-row')) {
                                var row = new ej.base.createElement('div', { className: 'custom-field-row' });
                                var formElement = args.element.querySelector('.e-schedule-form');
                                formElement.firstChild.insertBefore(row, args.element.querySelector('.e-title-location-row'));
                                var container = new ej.base.createElement('div', { className: 'custom-field-container' });
                                var inputEle = new ej.base.createElement('input', {
                                    className: 'e-field', attrs: { name: 'Status' }
                                });
                                container.appendChild(inputEle);
                                row.appendChild(container);
                                var drowDownList = new ej.dropdowns.DropDownList({
                                    dataSource: [
                                        { text: 'Cancelled', value: 0 },
                                        { text: 'Draft', value: 1 },
                                        { text: 'Confirmed', value: 2 },
                                        { text: 'OnProgress', value: 3 },
                                        { text: 'Done', value: 4 }
                                    ],
                                    fields: { text: 'text', value: 'value' },
                                    value: args.data.status,
                                    floatLabelType: 'Always', placeholder: 'Status'
                                });
                                drowDownList.appendTo(inputEle);
                                inputEle.setAttribute('name', 'Status');
                            }

                            var dropdownList = args.element.querySelector('.e-field[name="Status"]');
                            if (dropdownList) {
                                var drowDownList = ej.base.getInstance(dropdownList, ej.dropdowns.DropDownList);
                                drowDownList.value = args.data.status;
                            }

                            let timezoneRow = args.element.querySelector('.e-time-zone-container');
                            if (timezoneRow) {
                                timezoneRow.style.display = 'none';
                            }
                        }
                    },
                    eventRendered: (args) => {
                        switch (args.data.status) {
                            case 0:
                                (args.element).style.backgroundColor = '#E94649';
                                break;
                            case 1:
                                (args.element).style.backgroundColor = '#F6B53F';
                                break;
                            case 2:
                                (args.element).style.backgroundColor = '#009CFF';
                                break;
                            case 3:
                                (args.element).style.backgroundColor = '#C4C24A';
                                break;
                            case 4:
                                (args.element).style.backgroundColor = '#8e24aa';
                                break;
                        }
                    },
                    actionFailure: function (args) {
                        if (args.error && args.error.length > 0) {
                            var errorMessage = '';
                            args.error.forEach(function (error) {
                                errorMessage += error.error.responseText + '<br>';
                            });

                            var toast = new ej.notifications.Toast({
                                position: { X: 'Right', Y: 'Bottom' },
                                timeOut: 5000,
                                cssClass: 'e-toast-danger'
                            });
                            toast.appendTo('#toast');
                            toast.show({ title: 'Error', content: errorMessage });
                        } else {
                            var toast = new ej.notifications.Toast({
                                position: { X: 'Right', Y: 'Bottom' },
                                timeOut: 5000,
                                cssClass: 'e-toast-danger'
                            });
                            toast.appendTo('#toast');
                            toast.show({ title: 'Error', content: 'Failed to load data. Please try again later.' });
                        }
                    },
                    actionBegin: (args) => {
                        if (args.requestType === 'eventChange' || args.requestType === 'eventCreate') {
                            const event = args.changedRecords[0] || args.addedRecords[0];
                            if (event) {
                                const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
                                event.StartTimezone = timezone;
                                event.EndTimezone = timezone;
                            }
                        }
                    },
                    readonly: false,
                });
                scheduleObj.appendTo(scheduleRef.value);

            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['BookingSchedulers']);
                await SecurityManager.validateToken();

                ej.base.L10n.load({
                    'en-US': {
                        'schedule': {
                            'newEvent': 'Add Booking',
                            'editEvent': 'Edit Booking',
                        },
                    }
                });

                await methods.populateBookingResourceLookupData();
                await methods.populateBookingGroupLookupData();
                methods.populateScheduler();

            } catch (e) {
                console.error('page init error:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return {
            scheduleRef,
        };
    }
};

Vue.createApp(App).mount('#app');