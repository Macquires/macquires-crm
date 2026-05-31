const App = {
    setup() {
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

        const parentOptions = Vue.computed(() =>
            (state.mainData || []).filter((row) => row.id !== state.id)
        );

        const buildManagerOptions = (rows) =>
            (rows || []).map((u) => ({
                id: u.id,
                label: `${u.firstName || ''} ${u.lastName || ''} (${u.email || ''})`.trim(),
            }));

        const methods = {
            populateMainData: async () => {
                const response = await services.getBranches();
                state.mainData = (response?.data?.content?.data || []).map((row) => ({
                    ...row,
                    activeLabel: row.isActive ? 'نشط' : 'موقوف',
                    activeBadge: row.isActive ? 'bg-success' : 'bg-secondary',
                    managerLabel: row.managerDisplayName || '—',
                }));
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
                    Swal.fire({ icon: 'warning', title: 'اسم الفرع مطلوب' });
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

                    const res = state.id
                        ? await services.updateBranch(body)
                        : await services.createBranch(body);

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
                    columns: [
                        { field: 'id', isPrimaryKey: true, visible: false },
                        { field: 'nameAr', headerText: 'الفرع', width: 180, minWidth: 140 },
                        { field: 'nameEn', headerText: 'EN', width: 140, minWidth: 100 },
                        { field: 'parentNameAr', headerText: 'الوحدة الأب', width: 160, minWidth: 120 },
                        { field: 'managerLabel', headerText: 'مدير الفرع', width: 180, minWidth: 140 },
                        { field: 'managerEmail', headerText: 'بريد المدير', width: 200, minWidth: 140 },
                        { field: 'staffCount', headerText: 'الموظفون', width: 90, minWidth: 70, textAlign: 'Center' },
                        {
                            field: 'activeLabel',
                            headerText: 'الحالة',
                            width: 90,
                            minWidth: 80,
                            template: '<span class="badge ${activeBadge}">${activeLabel}</span>',
                        },
                    ],
                    toolbar: [
                        { text: 'إضافة فرع', tooltipText: 'إضافة', prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: 'تعديل', tooltipText: 'تعديل', prefixIcon: 'e-edit', id: 'EditCustom' },
                        'Search',
                        'ExcelExport',
                    ],
                    toolbarClick: (args) => {
                        if (args.item.id === 'AddCustom') {
                            resetForm();
                            state.mainTitle = 'إضافة فرع';
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'EditCustom') {
                            const selected = mainGrid.obj.getSelectedRecords();
                            if (!selected?.length) {
                                Swal.fire({ icon: 'info', title: 'اختر فرعاً من الجدول' });
                                return;
                            }
                            const row = selected[0];
                            state.id = row.id;
                            state.nameAr = row.nameAr || '';
                            state.nameEn = row.nameEn || '';
                            state.parentId = row.parentId || '';
                            state.managerUserId = row.managerUserId || '';
                            state.isActive = !!row.isActive;
                            state.mainTitle = 'تعديل فرع';
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
        };

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value);
            },
        };

        Vue.onMounted(async () => {
            mainModal.create();
            await methods.populateManagers();
            await methods.populateMainData();
            await mainGrid.create(state.mainData);
        });

        return { state, handler, mainGridRef, mainModalRef, parentOptions };
    },
};

Vue.createApp(App).mount('#app');
