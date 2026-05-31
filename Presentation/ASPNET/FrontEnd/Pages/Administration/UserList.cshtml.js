const TELECOM_ROLES = [
    'TelecomAdmin',
    'TelecomManagement',
    'TelecomBackOffice',
    'TelecomCallCenter',
    'TelecomShowroom',
];

const PERSONA_OPTIONS = ['Executive', 'CallCenter', 'Retail', 'BackOffice', 'SysAdmin'];

const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            orgUnits: [],
            managerOptions: [],
            mainTitle: '',
            userId: '',
            email: '',
            password: '',
            confirmPassword: '',
            firstName: '',
            lastName: '',
            primaryMenuPersona: 'Retail',
            managerUserId: '',
            orgUnitId: '',
            isBlocked: false,
            emailConfirmed: true,
            selectedRoles: [],
            telecomRoles: TELECOM_ROLES,
            personaOptions: PERSONA_OPTIONS,
            isSubmitting: false,
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);

        const services = {
            getUsers: async () => AxiosManager.get('/Security/GetUserList'),
            getOrgUnits: async () => AxiosManager.get('/Security/GetOrgUnitList'),
            getRoles: async (userId) =>
                AxiosManager.post('/Security/GetUserRoles', { userId }),
            createUser: async (body) => AxiosManager.post('/Security/CreateUser', body),
            updateUser: async (body) => AxiosManager.post('/Security/UpdateUser', body),
            updateUserRole: async (body) => AxiosManager.post('/Security/UpdateUserRole', body),
        };

        const buildManagerOptions = (rows) =>
            (rows || [])
                .filter((u) => u.id !== state.userId)
                .map((u) => ({
                    id: u.id,
                    label: `${u.firstName || ''} ${u.lastName || ''} (${u.email || ''})`.trim(),
                }));

        const methods = {
            populateMainData: async () => {
                const response = await services.getUsers();
                const rows = response?.data?.content?.data || [];
                state.mainData = rows.map((u) => {
                    const isOnline = !!u.isOnline;
                    return {
                        ...u,
                        fullName: `${u.firstName || ''} ${u.lastName || ''}`.trim(),
                        lastLoginAtUtc: u.lastLoginAtUtc ? new Date(u.lastLoginAtUtc) : null,
                        lastActivityAtUtc: u.lastActivityAtUtc ? new Date(u.lastActivityAtUtc) : null,
                        isOnline,
                        onlineStatus: isOnline ? 'متصل' : 'غير متصل',
                        onlineStatusBadge: isOnline ? 'bg-success' : 'bg-secondary',
                    };
                });
                state.managerOptions = buildManagerOptions(state.mainData);
            },
            populateOrgUnits: async () => {
                const response = await services.getOrgUnits();
                state.orgUnits = response?.data?.content?.data || [];
            },
        };

        const resetForm = () => {
            state.userId = '';
            state.email = '';
            state.password = '';
            state.confirmPassword = '';
            state.firstName = '';
            state.lastName = '';
            state.primaryMenuPersona = 'Retail';
            state.managerUserId = '';
            state.orgUnitId = '';
            state.isBlocked = false;
            state.emailConfirmed = true;
            state.selectedRoles = [];
        };

        const handler = {
            toggleRole: async (roleName, granted) => {
                if (!state.userId) return;
                try {
                    const res = await services.updateUserRole({
                        userId: state.userId,
                        roleName,
                        accessGranted: granted,
                        updatedById: StorageManager.getUserId(),
                    });
                    state.selectedRoles = res?.data?.content?.data || [];
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: 'فشل تحديث الدور',
                        text: e.response?.data?.message || e.message,
                    });
                }
            },
            handleSubmit: async () => {
                if (!state.firstName?.trim() || !state.lastName?.trim()) {
                    Swal.fire({ icon: 'warning', title: 'الاسم مطلوب' });
                    return;
                }
                if (!state.userId && (!state.email?.trim() || !state.password)) {
                    Swal.fire({ icon: 'warning', title: 'البريد وكلمة المرور مطلوبان' });
                    return;
                }

                state.isSubmitting = true;
                const uid = StorageManager.getUserId();
                try {
                    const body = state.userId
                        ? {
                              userId: state.userId,
                              firstName: state.firstName.trim(),
                              lastName: state.lastName.trim(),
                              emailConfirmed: state.emailConfirmed,
                              isBlocked: state.isBlocked,
                              isDeleted: false,
                              updatedById: uid,
                              primaryMenuPersona: state.primaryMenuPersona,
                              managerUserId: state.managerUserId || null,
                              orgUnitId: state.orgUnitId || null,
                              syncTelecomRoleFromPersona: true,
                          }
                        : {
                              email: state.email.trim(),
                              password: state.password,
                              confirmPassword: state.confirmPassword || state.password,
                              firstName: state.firstName.trim(),
                              lastName: state.lastName.trim(),
                              emailConfirmed: state.emailConfirmed,
                              isBlocked: state.isBlocked,
                              isDeleted: false,
                              createdById: uid,
                              primaryMenuPersona: state.primaryMenuPersona,
                              managerUserId: state.managerUserId || null,
                              orgUnitId: state.orgUnitId || null,
                              syncTelecomRoleFromPersona: true,
                          };

                    const res = state.userId
                        ? await services.updateUser(body)
                        : await services.createUser(body);

                    if (res?.data?.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();
                        mainModal.obj.hide();
                        resetForm();
                        Swal.fire({ icon: 'success', title: 'تم الحفظ', timer: 1400, showConfirmButton: false });
                    } else {
                        Swal.fire({ icon: 'error', title: 'فشل الحفظ', text: res?.data?.message || '' });
                    }
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: 'خطأ',
                        text: e.response?.data?.message || e.message,
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
        };

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    id: 'UserAdminGrid',
                    height: getDashminGridHeight(),
                    dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    pageSettings: { pageSize: 25, pageSizes: ['10', '25', '50', '100'] },
                    selectionSettings: { type: 'Single' },
                    gridLines: 'Horizontal',
                    columns: [
                        { field: 'id', isPrimaryKey: true, visible: false },
                        { field: 'fullName', headerText: 'الاسم', width: 160, minWidth: 120 },
                        { field: 'email', headerText: 'البريد', width: 180, minWidth: 140 },
                        { field: 'primaryMenuPersona', headerText: 'Persona', width: 100, minWidth: 90 },
                        { field: 'rolesDisplay', headerText: 'الأدوار', width: 200, minWidth: 120 },
                        { field: 'managerDisplayName', headerText: 'المدير', width: 140, minWidth: 100 },
                        { field: 'orgUnitNameAr', headerText: 'الوحدة', width: 120, minWidth: 90 },
                        {
                            field: 'onlineStatus',
                            headerText: 'الحالة',
                            width: 90,
                            minWidth: 80,
                            template: '<span class="badge ${onlineStatusBadge}">${onlineStatus}</span>',
                        },
                        {
                            field: 'lastLoginAtUtc',
                            headerText: 'آخر دخول',
                            width: 150,
                            minWidth: 120,
                            format: 'yyyy-MM-dd HH:mm',
                            type: 'dateTime',
                        },
                        {
                            field: 'lastActivityAtUtc',
                            headerText: 'آخر نشاط',
                            width: 150,
                            minWidth: 120,
                            format: 'yyyy-MM-dd HH:mm',
                            type: 'dateTime',
                        },
                        {
                            field: 'isBlocked',
                            headerText: 'موقوف',
                            width: 80,
                            minWidth: 70,
                            displayAsCheckBox: true,
                            type: 'boolean',
                        },
                    ],
                    toolbar: [
                        'ExcelExport',
                        'Search',
                        { type: 'Separator' },
                        { text: 'إضافة', prefixIcon: 'e-add', id: 'AddUser' },
                        { text: 'تعديل', prefixIcon: 'e-edit', id: 'EditUser' },
                    ],
                    dataBound: function () {
                        mainGrid.obj.toolbarModule.enableItems(['EditUser'], false);
                    },
                    rowSelected: () => {
                        if (mainGrid.obj.getSelectedRecords().length === 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditUser'], true);
                        }
                    },
                    rowDeselected: () => {
                        if (mainGrid.obj.getSelectedRecords().length !== 1) {
                            mainGrid.obj.toolbarModule.enableItems(['EditUser'], false);
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id?.endsWith('_excelexport')) {
                            mainGrid.obj.excelExport();
                        }
                        if (args.item.id === 'AddUser') {
                            resetForm();
                            state.mainTitle = 'إضافة مستخدم';
                            state.managerOptions = buildManagerOptions(state.mainData);
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'EditUser' && mainGrid.obj.getSelectedRecords().length) {
                            const r = mainGrid.obj.getSelectedRecords()[0];
                            state.mainTitle = 'تعديل مستخدم';
                            state.userId = r.id;
                            state.firstName = r.firstName || '';
                            state.lastName = r.lastName || '';
                            state.primaryMenuPersona = r.primaryMenuPersona || 'Retail';
                            state.managerUserId = r.managerUserId || '';
                            state.orgUnitId = r.orgUnitId || '';
                            state.isBlocked = !!r.isBlocked;
                            state.emailConfirmed = r.emailConfirmed !== false;
                            state.managerOptions = buildManagerOptions(state.mainData);
                            try {
                                const rolesRes = await services.getRoles(r.id);
                                state.selectedRoles = rolesRes?.data?.content?.data || [];
                            } catch {
                                state.selectedRoles = r.roles || [];
                            }
                            mainModal.obj.show();
                        }
                    },
                });
                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => mainGrid.obj?.setProperties({ dataSource: state.mainData }),
        };

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false,
                });
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['TelecomAdmin']);
                await SecurityManager.validateToken();
                await methods.populateOrgUnits();
                await methods.populateMainData();
                await mainGrid.create(state.mainData);
                mainModal.create();
            } catch (e) {
                console.error('UserList init:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return { mainGridRef, mainModalRef, state, handler };
    },
};

Vue.createApp(App).mount('#app');
