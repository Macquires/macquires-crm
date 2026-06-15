const TELECOM_ROLES = [
    'TelecomAdmin',
    'TelecomManagement',
    'TelecomBackOffice',
    'TelecomCallCenter',
    'TelecomShowroom',
];

const PERSONA_OPTIONS = ['Executive', 'CallCenter', 'Retail', 'BackOffice', 'SysAdmin'];

const I18N_PREFIX = 'administration.userList';

const App = {
    setup() {
        const localeTick = Vue.ref(0);

        const t = (key, fallback = '') => {
            localeTick.value;
            const hit = window.TelecomI18n?.t?.(`${I18N_PREFIX}.${key}`);
            return hit && hit !== `${I18N_PREFIX}.${key}` ? hit : fallback || key;
        };

        const isEn = () => document.documentElement.lang?.toLowerCase().startsWith('en');

        const orgUnitLabel = (o) => {
            if (!o) return '';
            const ar = (o.nameAr || '').trim();
            const en = (o.nameEn || '').trim();
            return isEn() ? en || ar : ar || en;
        };

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
            getRoles: async (userId) => AxiosManager.post('/Security/GetUserRoles', { userId }),
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

        const mapUserRow = (u) => {
            const isOnline = !!u.isOnline;
            return {
                ...u,
                fullName: `${u.firstName || ''} ${u.lastName || ''}`.trim(),
                lastLoginAtUtc: u.lastLoginAtUtc ? new Date(u.lastLoginAtUtc) : null,
                lastActivityAtUtc: u.lastActivityAtUtc ? new Date(u.lastActivityAtUtc) : null,
                isOnline,
                onlineStatus: isOnline ? t('online.connected') : t('online.disconnected'),
                onlineStatusBadge: isOnline ? 'bg-success' : 'bg-secondary',
            };
        };

        const methods = {
            populateMainData: async () => {
                const response = await services.getUsers();
                const rows = response?.data?.content?.data || [];
                state.mainData = rows.map(mapUserRow);
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
                    });
                    state.selectedRoles = res?.data?.content?.data || [];
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: t('messages.roleUpdateFailed'),
                        text: e.response?.data?.message || e.message,
                    });
                }
            },
            handleSubmit: async () => {
                if (!state.firstName?.trim() || !state.lastName?.trim()) {
                    Swal.fire({ icon: 'warning', title: t('messages.nameRequired') });
                    return;
                }
                if (!state.userId && (!state.email?.trim() || !state.password)) {
                    Swal.fire({ icon: 'warning', title: t('messages.emailPasswordRequired') });
                    return;
                }

                state.isSubmitting = true;
                try {
                    const body = state.userId
                        ? {
                              userId: state.userId,
                              firstName: state.firstName.trim(),
                              lastName: state.lastName.trim(),
                              emailConfirmed: state.emailConfirmed,
                              isBlocked: state.isBlocked,
                              isDeleted: false,
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
                              primaryMenuPersona: state.primaryMenuPersona,
                              managerUserId: state.managerUserId || null,
                              orgUnitId: state.orgUnitId || null,
                              syncTelecomRoleFromPersona: true,
                          };

                    const res = state.userId ? await services.updateUser(body) : await services.createUser(body);

                    if (res?.data?.code === 200) {
                        await methods.populateMainData();
                        mainGrid.refresh();
                        mainModal.obj.hide();
                        resetForm();
                        Swal.fire({
                            icon: 'success',
                            title: t('messages.saved'),
                            timer: 1400,
                            showConfirmButton: false,
                        });
                    } else {
                        Swal.fire({ icon: 'error', title: t('messages.saveFailed'), text: res?.data?.message || '' });
                    }
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: t('messages.error'),
                        text: e.response?.data?.message || e.message,
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
        };

        const gridColumns = () => [
            { field: 'id', isPrimaryKey: true, visible: false },
            { field: 'fullName', headerText: t('grid.fullName'), width: 160, minWidth: 120 },
            { field: 'email', headerText: t('grid.email'), width: 180, minWidth: 140 },
            { field: 'primaryMenuPersona', headerText: t('grid.persona'), width: 100, minWidth: 90 },
            { field: 'rolesDisplay', headerText: t('grid.roles'), width: 200, minWidth: 120 },
            { field: 'managerDisplayName', headerText: t('grid.manager'), width: 140, minWidth: 100 },
            { field: 'orgUnitNameAr', headerText: t('grid.orgUnit'), width: 120, minWidth: 90 },
            {
                field: 'onlineStatus',
                headerText: t('grid.status'),
                width: 90,
                minWidth: 80,
                template: '<span class="badge ${onlineStatusBadge}">${onlineStatus}</span>',
            },
            {
                field: 'lastLoginAtUtc',
                headerText: t('grid.lastLogin'),
                width: 150,
                minWidth: 120,
                format: 'yyyy-MM-dd HH:mm',
                type: 'dateTime',
            },
            {
                field: 'lastActivityAtUtc',
                headerText: t('grid.lastActivity'),
                width: 150,
                minWidth: 120,
                format: 'yyyy-MM-dd HH:mm',
                type: 'dateTime',
            },
            {
                field: 'isBlocked',
                headerText: t('grid.blocked'),
                width: 80,
                minWidth: 70,
                displayAsCheckBox: true,
                type: 'boolean',
            },
        ];

        const gridToolbar = () => [
            'ExcelExport',
            'Search',
            { type: 'Separator' },
            { text: t('toolbar.add'), prefixIcon: 'e-add', id: 'AddUser' },
            { text: t('toolbar.edit'), prefixIcon: 'e-edit', id: 'EditUser' },
        ];

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                localeTick.value;
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
                    columns: gridColumns(),
                    toolbar: gridToolbar(),
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
                            state.mainTitle = t('modal.addTitle');
                            state.managerOptions = buildManagerOptions(state.mainData);
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'EditUser' && mainGrid.obj.getSelectedRecords().length) {
                            const r = mainGrid.obj.getSelectedRecords()[0];
                            state.mainTitle = t('modal.editTitle');
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
            refresh: () => {
                if (!mainGrid.obj) return;
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            },
            rebuild: async () => {
                if (!mainGridRef.value) return;
                const data = state.mainData.map(mapUserRow);
                state.mainData = data;
                if (mainGrid.obj) {
                    mainGrid.obj.destroy();
                    mainGrid.obj = null;
                }
                await mainGrid.create(data);
            },
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

        const onLocaleChanged = async () => {
            localeTick.value++;
            await window.TelecomI18n?.ensureLoaded?.();
            const title = t('pageTitle');
            if (title) document.title = title;
            state.mainData = state.mainData.map(mapUserRow);
            await mainGrid.rebuild();
        };

        Vue.onMounted(async () => {
            document.documentElement.addEventListener('syriatel-locale-changed', onLocaleChanged);
            try {
                await window.TelecomI18n?.ensureLoaded?.();
                const title = t('pageTitle');
                if (title) document.title = title;
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

        Vue.onUnmounted(() => {
            document.documentElement.removeEventListener('syriatel-locale-changed', onLocaleChanged);
        });

        return { mainGridRef, mainModalRef, state, handler, t, orgUnitLabel };
    },
};

Vue.createApp(App).mount('#app');
