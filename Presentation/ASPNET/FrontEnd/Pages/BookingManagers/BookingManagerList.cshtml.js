const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            bookingResourceListLookupData: [],
            bookingStatusListLookupData: [],
            mainTitle: null,
            id: '',
            number: '',
            subject: '',
            location: '',
            startTime: null,
            endTime: null,
            isAllDay: false,
            isReadOnly: false,
            isBlock: false,
            description: '',
            bookingResourceId: null,
            status: null,
            errors: {
                subject: '',
                location: '',
                startTime: '',
                endTime: '',
                bookingResourceId: '',
                status: ''
            },
            isSubmitting: false
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const subjectRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const locationRef = Vue.ref(null);
        const startTimeRef = Vue.ref(null);
        const endTimeRef = Vue.ref(null);
        const bookingResourceIdRef = Vue.ref(null);
        const statusRef = Vue.ref(null);

        const validateForm = function () {
            state.errors.subject = '';
            state.errors.location = '';
            state.errors.startTime = '';
            state.errors.endTime = '';
            state.errors.bookingResourceId = '';
            state.errors.status = '';

            let isValid = true;

            if (!state.subject) {
                state.errors.subject = 'Subject is required.';
                isValid = false;
            }
            if (!state.location) {
                state.errors.location = 'Location is required.';
                isValid = false;
            }
            if (!state.startTime) {
                state.errors.startTime = 'Start time is required.';
                isValid = false;
            }
            if (!state.endTime) {
                state.errors.endTime = 'End time is required.';
                isValid = false;
            }
            if (!state.bookingResourceId) {
                state.errors.bookingResourceId = 'BookingResource is required.';
                isValid = false;
            }
            if (!state.status) {
                state.errors.status = 'Status is required.';
                isValid = false;
            }

            return isValid;
        };

        const resetFormState = () => {
            state.id = '';
            state.number = '';
            state.subject = '';
            state.location = '';
            state.startTime = null;
            state.endTime = null;
            state.isAllDay = false;
            state.isReadOnly = false;
            state.isBlock = false;
            state.description = '';
            state.bookingResourceId = null;
            state.status = null;
            state.errors = {
                subject: '',
                location: '',
                startTime: '',
                endTime: '',
                bookingResourceId: '',
                status: ''
            };
        };

        const services = {
            getMainData: async () => {
                try {
                    const response = await AxiosManager.get('/Booking/GetBookingList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (subject, location, startTime, endTime, isAllDay, isReadOnly, isBlock, description, status, bookingResourceId, createdById) => {
                try {
                    const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
                    const response = await AxiosManager.post('/Booking/CreateBooking', {
                        subject, location, startTime, endTime, isAllDay, isReadOnly, isBlock, description, status, bookingResourceId, createdById, startTimezone : timezone, endTimezone : timezone
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, subject, location, startTime, endTime, isAllDay, isReadOnly, isBlock, description, status, bookingResourceId, updatedById) => {
                try {
                    const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;                    
                    const response = await AxiosManager.post('/Booking/UpdateBooking', {
                        id, subject, location, startTime, endTime, isAllDay, isReadOnly, isBlock, description, status, bookingResourceId, updatedById, startTimezone : timezone, endTimezone : timezone
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/Booking/DeleteBooking', {
                        id, deletedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getBookingResourceListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/BookingResource/GetBookingResourceList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getBookingStatusListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/Booking/GetBookingStatusList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            populateBookingResourceListLookupData: async () => {
                const response = await services.getBookingResourceListLookupData();
                state.bookingResourceListLookupData = response?.data?.content?.data;
            },

            populateBookingStatusListLookupData: async () => {
                const response = await services.getBookingStatusListLookupData();
                state.bookingStatusListLookupData = response?.data?.content?.data;
            },

            populateMainData: async () => {
                const response = await services.getMainData();
                state.mainData = response?.data?.content?.data.map(item => ({
                    ...item,
                    startTime: new Date(item.startTime),
                    endTime: new Date(item.endTime),
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
        };

        const subjectText = {
            obj: null,
            create: () => {
                subjectText.obj = new ej.inputs.TextBox({
                    placeholder: 'Enter Subject',
                });
                subjectText.obj.appendTo(subjectRef.value);
            },
            refresh: () => {
                if (subjectText.obj) {
                    subjectText.obj.value = state.subject;
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
            },
            refresh: () => {
                if (numberText.obj) {
                    numberText.obj.value = state.number;
                }
            }
        };

        const locationText = {
            obj: null,
            create: () => {
                locationText.obj = new ej.inputs.TextBox({
                    placeholder: 'Enter Location',
                });
                locationText.obj.appendTo(locationRef.value);
            },
            refresh: () => {
                if (locationText.obj) {
                    locationText.obj.value = state.location;
                }
            }
        };

        const startTimePicker = {
            obj: null,
            create: () => {
                startTimePicker.obj = new ej.calendars.DateTimePicker({
                    placeholder: 'Select Start Time',
                    format: 'yyyy-MM-dd HH:mm',
                    timeFormat: 'HH:mm',
                    change: (e) => {
                        state.startTime = e.value;
                    }
                });
                startTimePicker.obj.appendTo(startTimeRef.value);
            },
            refresh: () => {
                if (startTimePicker.obj) {
                    startTimePicker.obj.value = state.startTime ? new Date(state.startTime) : null;
                }
            }
        };

        const endTimePicker = {
            obj: null,
            create: () => {
                endTimePicker.obj = new ej.calendars.DateTimePicker({
                    placeholder: 'Select End Time',
                    format: 'yyyy-MM-dd HH:mm',
                    timeFormat: 'HH:mm',
                    value: state.endTime ? new Date(state.endTime) : null,
                    change: (e) => {
                        state.endTime = e.value;
                    }
                });
                endTimePicker.obj.appendTo(endTimeRef.value);
            },
            refresh: () => {
                if (endTimePicker.obj) {
                    endTimePicker.obj.value = state.endTime ? new Date(state.endTime) : null;
                }
            }
        };

        const bookingResourceListLookup = {
            obj: null,
            create: () => {
                if (state.bookingResourceListLookupData && Array.isArray(state.bookingResourceListLookupData)) {
                    bookingResourceListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.bookingResourceListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select Booking Resource',
                        change: (e) => {
                            state.bookingResourceId = e.value;
                        }
                    });
                    bookingResourceListLookup.obj.appendTo(bookingResourceIdRef.value);
                } else {
                    console.error('Booking Resource list lookup data is not available or invalid.');
                }
            },
            refresh: () => {
                if (bookingResourceListLookup.obj) {
                    bookingResourceListLookup.obj.value = state.bookingResourceId;
                }
            },
        };

        const bookingStatusListLookup = {
            obj: null,
            create: () => {
                if (state.bookingStatusListLookupData && Array.isArray(state.bookingStatusListLookupData)) {
                    bookingStatusListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.bookingStatusListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: 'Select Status',
                        change: (e) => {
                            state.status = e.value;
                        }
                    });
                    bookingStatusListLookup.obj.appendTo(statusRef.value);
                } else {
                    console.error('Booking Status list lookup data is not available or invalid.');
                }
            },
            refresh: () => {
                if (bookingStatusListLookup.obj) {
                    bookingStatusListLookup.obj.value = state.status;
                }
            },
        };

        Vue.watch(
            () => state.subject,
            (newVal, oldVal) => {
                state.errors.subject = '';
                subjectText.refresh();
            }
        );

        Vue.watch(
            () => state.location,
            (newVal, oldVal) => {
                state.errors.location = '';
                locationText.refresh();
            }
        );

        Vue.watch(
            () => state.startTime,
            (newVal, oldVal) => {
                state.errors.startTime = '';
                startTimePicker.refresh();
            }
        );

        Vue.watch(
            () => state.endTime,
            (newVal, oldVal) => {
                state.errors.endTime = '';
                endTimePicker.refresh();
            }
        );

        Vue.watch(
            () => state.bookingResourceId,
            (newVal, oldVal) => {
                state.errors.bookingResourceId = '';
                bookingResourceListLookup.refresh();
            }
        );

        Vue.watch(
            () => state.status,
            (newVal, oldVal) => {
                state.errors.status = '';
                bookingStatusListLookup.refresh();
            }
        );

        const handler = {
            handleSubmit: async function () {
                try {

                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 300));

                    if (!validateForm()) {
                        return;
                    }

                    const response = state.id === ''
                        ? await services.createMainData(state.subject, state.location, state.startTime, state.endTime, state.isAllDay, state.isReadOnly, state.isBlock, state.description, state.status, state.bookingResourceId, StorageManager.getUserId())
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.subject, state.location, state.startTime, state.endTime, state.isAllDay, state.isReadOnly, state.isBlock, state.description, state.status, state.bookingResourceId, StorageManager.getUserId());

                    if (response.data.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = 'Edit Booking';
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.subject = response?.data?.content?.data.subject ?? '';
                            state.location = response?.data?.content?.data.location ?? '';
                            state.startTime = response?.data?.content?.data.startTime ? new Date(response.data.content.data.startTime) : null;
                            state.endTime = response?.data?.content?.data.endTime ? new Date(response.data.content.data.endTime) : null;
                            state.isAllDay = response?.data?.content?.data.isAllDay ?? false;
                            state.isReadOnly = response?.data?.content?.data.isReadOnly ?? false;
                            state.isBlock = response?.data?.content?.data.isBlock ?? false;
                            state.description = response?.data?.content?.data.description ?? '';
                            state.bookingResourceId = response?.data?.content?.data.bookingResourceId ?? '';
                            state.status = String(response?.data?.content?.data.status ?? '');

                            Swal.fire({
                                icon: 'success',
                                title: state.deleteMode ? 'Delete Successful' : 'Save Successful',
                                text: 'Form will be closed...',
                                timer: 2000,
                                showConfirmButton: false
                            });
                            setTimeout(() => {
                                mainModal.obj.hide();
                            }, 2000);

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
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['BookingManagers']);
                await SecurityManager.validateToken();

                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                await methods.populateBookingResourceListLookupData();
                bookingResourceListLookup.create();
                await methods.populateBookingStatusListLookupData();
                bookingStatusListLookup.create();
                subjectText.create();
                numberText.create();
                locationText.create();
                startTimePicker.create();
                endTimePicker.create();

                mainModal.create();
                mainModalRef.value?.addEventListener('hidden.bs.modal', () => {
                    resetFormState();
                });

            } catch (e) {
                console.error('page init error:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        Vue.onUnmounted(() => {
            mainModalRef.value?.removeEventListener('hidden.bs.modal', resetFormState);
        });

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
                        columns: ['bookingGroupName']
                    },
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
                        { field: 'bookingResourceName', headerText: 'Resource', width: 150, minWidth: 150 },
                        { field: 'number', headerText: 'Number', width: 150, minWidth: 150 },
                        { field: 'subject', headerText: 'Subject', width: 150, minWidth: 150 },
                        { field: 'location', headerText: 'Location', width: 150, minWidth: 150 },
                        { field: 'startTime', headerText: 'Start Time', width: 150, format: 'yyyy-MM-dd HH:mm' },
                        { field: 'endTime', headerText: 'End Time', width: 150, format: 'yyyy-MM-dd HH:mm' },
                        { field: 'bookingGroupName', headerText: 'Group', width: 150, minWidth: 150 },
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
                        { text: 'Scheduler View', tooltipText: 'Scheduler View', id: 'SchedulerViewCustom' },
                    ],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        mainGrid.obj.autoFitColumns(['number', 'subject', 'location', 'startTime', 'endTime', 'bookingResourceName', 'bookingGroupName', 'statusName', 'createdAtUtc']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
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
                            state.mainTitle = 'Add Booking';
                            resetFormState();
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Edit Booking';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.subject = selectedRecord.subject ?? '';
                                state.location = selectedRecord.location ?? '';
                                state.startTime = selectedRecord.startTime ? new Date(selectedRecord.startTime) : null;
                                state.endTime = selectedRecord.endTime ? new Date(selectedRecord.endTime) : null;
                                state.isAllDay = selectedRecord.isAllDay ?? false;
                                state.isReadOnly = selectedRecord.isReadOnly ?? false;
                                state.isBlock = selectedRecord.isBlock ?? false;
                                state.description = selectedRecord.description ?? '';
                                state.bookingResourceId = selectedRecord.bookingResourceId ?? '';
                                state.status = String(selectedRecord.status ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = 'Delete Booking?';
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.subject = selectedRecord.subject ?? '';
                                state.location = selectedRecord.location ?? '';
                                state.startTime = selectedRecord.startTime ? new Date(selectedRecord.startTime) : null;
                                state.endTime = selectedRecord.endTime ? new Date(selectedRecord.endTime) : null;
                                state.isAllDay = selectedRecord.isAllDay ?? false;
                                state.isReadOnly = selectedRecord.isReadOnly ?? false;
                                state.isBlock = selectedRecord.isBlock ?? false;
                                state.description = selectedRecord.description ?? '';
                                state.bookingResourceId = selectedRecord.bookingResourceId ?? '';
                                state.status = String(selectedRecord.status ?? '');
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'SchedulerViewCustom') {
                            window.open('/BookingSchedulers/BookingSchedulerList', '_blank');
                        }
                    }
                });

                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
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

        return {
            mainGridRef,
            mainModalRef,
            subjectRef,
            numberRef,
            locationRef,
            startTimeRef,
            endTimeRef,
            bookingResourceIdRef,
            statusRef,
            state,
            handler,
        };
    }
};

Vue.createApp(App).mount('#app');