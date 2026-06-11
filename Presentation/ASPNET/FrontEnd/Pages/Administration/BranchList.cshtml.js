const I18N_PREFIX = 'administration.branchList';

const App = {
    setup() {
        const localeTick = Vue.ref(0);

        const t = (key, fallback = '') => {
            localeTick.value;
            const hit = window.TelecomI18n?.t?.(`${I18N_PREFIX}.${key}`);
            return hit && hit !== `${I18N_PREFIX}.${key}` ? hit : fallback || key;
        };

        const isEn = () => document.documentElement.lang?.toLowerCase().startsWith('en');

        const branchLabel = (row) => {
            if (!row) return '';
            const ar = (row.nameAr || '').trim();
            const en = (row.nameEn || '').trim();
            return isEn() ? en || ar : ar || en;
        };

        const state = Vue.reactive({
            mainData: [],
            managerOptions: [],
            mainTitle: '',
            id: '',
            nameAr: '',
            nameEn: '',
            parentId: '',
            managerUserId: '',
            isActive: true,
            isSubmitting: false,
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);

        const services = {
            getBranches: async () => AxiosManager.get('/Security/GetOrgUnitList'),
            getUsers: async () => AxiosManager.get('/Security/GetUserList'),
            createBranch: async (body) => AxiosManager.post('/Security/CreateOrgUnit', body),
            updateBranch: async (body) => AxiosManager.post('/Security/UpdateOrgUnit', body),
        };

        const parentOptions = Vue.computed(() => (state.mainData || []).filter((row) => row.id !== state.id));

        const buildManagerOptions = (rows) =>
            (rows || []).map((u) => ({
                id: u.id,
                label: `${u.firstName || ''} ${u.lastName || ''} (${u.email || ''})`.trim(),
            }));

        const mapBranchRow = (row) => ({
            ...row,
            activeLabel: row.isActive ? t('active.active') : t('active.inactive'),
            activeBadge: row.isActive ? 'bg-success' : 'bg-secondary',
            managerLabel: row.managerDisplayName || '—',
        });

        const methods = {
            populateMainData: async () => {
                const response = await services.getBranches();
                state.mainData = (response?.data?.content?.data || []).map(mapBranchRow);
            },
            populateManagers: async () => {
                const response = await services.getUsers();
                const rows = response?.data?.content?.data || [];
                state.managerOptions = buildManagerOptions(rows);
            },
        };

        const resetForm = () => {
            state.id = '';
            state.nameAr = '';
            state.nameEn = '';
            state.parentId = '';
            state.managerUserId = '';
            state.isActive = true;
        };

        const handler = {
            handleSubmit: async () => {
                if (!state.nameAr?.trim()) {
                    Swal.fire({ icon: 'warning', title: t('messages.nameRequired') });
                    return;
                }

                state.isSubmitting = true;
                const uid = StorageManager.getUserId();
                try {
                    const body = state.id
                        ? {
                              id: state.id,
                              nameAr: state.nameAr.trim(),
                              nameEn: state.nameEn?.trim() || null,
                              parentId: state.parentId || null,
                              managerUserId: state.managerUserId || null,
                              isActive: state.isActive,
                              updatedById: uid,
                          }
                        : {
                              nameAr: state.nameAr.trim(),
                              nameEn: state.nameEn?.trim() || null,
                              parentId: state.parentId || null,
                              managerUserId: state.managerUserId || null,
                              isActive: state.isActive,
                              createdById: uid,
                          };

                    const res = state.id ? await services.updateBranch(body) : await services.createBranch(body);

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
            { field: 'nameAr', headerText: t('grid.nameAr'), width: 180, minWidth: 140 },
            { field: 'nameEn', headerText: t('grid.nameEn'), width: 140, minWidth: 100 },
            { field: 'parentNameAr', headerText: t('grid.parent'), width: 160, minWidth: 120 },
            { field: 'managerLabel', headerText: t('grid.manager'), width: 180, minWidth: 140 },
            { field: 'managerEmail', headerText: t('grid.managerEmail'), width: 200, minWidth: 140 },
            { field: 'staffCount', headerText: t('grid.staff'), width: 90, minWidth: 70, textAlign: 'Center' },
            {
                field: 'activeLabel',
                headerText: t('grid.status'),
                width: 90,
                minWidth: 80,
                template: '<span class="badge ${activeBadge}">${activeLabel}</span>',
            },
        ];

        const gridToolbar = () => [
            { text: t('toolbar.add'), tooltipText: t('toolbar.add'), prefixIcon: 'e-add', id: 'AddCustom' },
            { text: t('toolbar.edit'), tooltipText: t('toolbar.edit'), prefixIcon: 'e-edit', id: 'EditCustom' },
            'Search',
            'ExcelExport',
        ];

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                localeTick.value;
                mainGrid.obj = new ej.grids.Grid({
                    id: 'BranchAdminGrid',
                    height: getDashminGridHeight(),
                    dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    pageSettings: { pageSize: 25, pageSizes: ['10', '25', '50'] },
                    selectionSettings: { type: 'Single' },
                    gridLines: 'Horizontal',
                    columns: gridColumns(),
                    toolbar: gridToolbar(),
                    toolbarClick: (args) => {
                        if (args.item.id === 'AddCustom') {
                            resetForm();
                            state.mainTitle = t('modal.addTitle');
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'EditCustom') {
                            const selected = mainGrid.obj.getSelectedRecords();
                            if (!selected?.length) {
                                Swal.fire({ icon: 'info', title: t('messages.selectBranch') });
                                return;
                            }
                            const row = selected[0];
                            state.id = row.id;
                            state.nameAr = row.nameAr || '';
                            state.nameEn = row.nameEn || '';
                            state.parentId = row.parentId || '';
                            state.managerUserId = row.managerUserId || '';
                            state.isActive = !!row.isActive;
                            state.mainTitle = t('modal.editTitle');
                            mainModal.obj.show();
                        }
                    },
                });
                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => {
                if (mainGrid.obj) {
                    mainGrid.obj.dataSource = state.mainData;
                }
            },
            rebuild: async () => {
                if (!mainGridRef.value) return;
                state.mainData = state.mainData.map(mapBranchRow);
                if (mainGrid.obj) {
                    mainGrid.obj.destroy();
                    mainGrid.obj = null;
                }
                await mainGrid.create(state.mainData);
            },
        };

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value);
            },
        };

        const onLocaleChanged = async () => {
            localeTick.value++;
            await window.TelecomI18n?.ensureLoaded?.();
            const title = t('pageTitle');
            if (title) document.title = title;
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
                mainModal.create();
                await methods.populateManagers();
                await methods.populateMainData();
                await mainGrid.create(state.mainData);
            } catch (e) {
                console.error('BranchList init:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        Vue.onUnmounted(() => {
            document.documentElement.removeEventListener('syriatel-locale-changed', onLocaleChanged);
        });

        return { state, handler, mainGridRef, mainModalRef, parentOptions, t, branchLabel };
    },
};

Vue.createApp(App).mount('#app');
