const App = {
    setup() {
        const state = Vue.reactive({
            roles: [],
            catalog: [],
            permissionGroups: [],
            selectedRole: null,
            selectedPermissionKeys: [],
            isSaving: false,
        });

        const roleGridRef = Vue.ref(null);
        let roleGrid = null;

        const services = {
            getRoles: () => AxiosManager.get('/Security/GetRoleList'),
            getCatalog: () => AxiosManager.get('/Security/GetPermissionCatalog'),
            getRolePermissions: (roleName) =>
                AxiosManager.get('/Security/GetRolePermissions', { params: { roleName } }),
            updateRolePermissions: (body) =>
                AxiosManager.post('/Security/UpdateRolePermissions', body),
            cloneRole: (body) => AxiosManager.post('/Security/CloneRolePermissions', body),
        };

        const uiLang = () =>
            (typeof getUiLang === 'function' ? getUiLang() : document.documentElement.lang?.startsWith('en') ? 'en' : 'ar');

        const permKey = (p) => p?.key ?? p?.Key ?? '';
        const permModule = (p) => p?.module ?? p?.Module ?? '';
        const permLabel = (p) => {
            const key = permKey(p);
            return uiLang() === 'en'
                ? p?.labelEn ?? p?.LabelEn ?? key
                : p?.labelAr ?? p?.LabelAr ?? key;
        };

        const buildGroups = (catalog) => {
            const map = new Map();
            for (const p of catalog) {
                const module = permModule(p);
                if (!map.has(module)) {
                    map.set(module, []);
                }
                map.get(module).push(p);
            }
            return Array.from(map.entries()).map(([module, items]) => ({ module, items }));
        };

        const roleDisplayName = (row) => (row?.name ?? row?.Name ?? '').trim();

        const normalizeRoleRow = (row) => ({
            id: row?.id ?? row?.Id ?? '',
            name: roleDisplayName(row),
        });

        const methods = {
            load: async () => {
                const [rolesRes, catRes] = await Promise.all([
                    services.getRoles(),
                    services.getCatalog(),
                ]);
                state.roles = (rolesRes?.data?.content?.data || []).map(normalizeRoleRow);
                state.catalog = catRes?.data?.content?.data || [];
                state.permissionGroups = buildGroups(state.catalog);
            },
            loadRolePermissions: async (roleName) => {
                const name = (roleName ?? '').trim();
                if (!name) {
                    state.selectedPermissionKeys = [];
                    return;
                }
                const res = await services.getRolePermissions(name);
                const keys =
                    res?.data?.content?.permissionKeys ??
                    res?.data?.content?.PermissionKeys ??
                    [];
                state.selectedPermissionKeys = [...keys];
            },
        };

        const handler = {
            togglePermission: (key, checked) => {
                const set = new Set(state.selectedPermissionKeys);
                if (checked) {
                    set.add(key);
                } else {
                    set.delete(key);
                }
                state.selectedPermissionKeys = Array.from(set);
            },
            cloneRole: async () => {
                if (!state.selectedRole) return;
                const { value: newName } = await Swal.fire({
                    title: uiLang() === 'en' ? 'Clone role' : 'نسخ الدور',
                    input: 'text',
                    inputLabel: uiLang() === 'en' ? 'New role name' : 'اسم الدور الجديد',
                    inputValue: state.selectedRole + '_Copy',
                    showCancelButton: true,
                });
                if (!newName?.trim()) return;
                try {
                    const res = await services.cloneRole({
                        sourceRoleName: state.selectedRole,
                        newRoleName: newName.trim(),
                        createdById: StorageManager.getUserId(),
                    });
                    if (res?.data?.code === 200) {
                        await methods.load();
                        roleGrid.dataSource = state.roles;
                        roleGrid.refresh();
                        Swal.fire({ icon: 'success', title: uiLang() === 'en' ? 'Role cloned' : 'تم نسخ الدور', timer: 1400, showConfirmButton: false });
                    }
                } catch (e) {
                    Swal.fire({ icon: 'error', text: e.response?.data?.message || e.message });
                }
            },
            savePermissions: async () => {
                if (!state.selectedRole) return;
                state.isSaving = true;
                try {
                    const res = await services.updateRolePermissions({
                        roleName: state.selectedRole,
                        permissionKeys: state.selectedPermissionKeys,
                        updatedById: StorageManager.getUserId(),
                    });
                    if (res?.data?.code === 200) {
                        Swal.fire({ icon: 'success', title: 'تم حفظ الصلاحيات', timer: 1400, showConfirmButton: false });
                    } else {
                        Swal.fire({ icon: 'error', title: 'فشل الحفظ', text: res?.data?.message || '' });
                    }
                } catch (e) {
                    Swal.fire({ icon: 'error', title: 'خطأ', text: e.response?.data?.message || e.message });
                } finally {
                    state.isSaving = false;
                }
            },
        };

        const createRoleGrid = () => {
            roleGrid = new ej.grids.Grid({
                id: 'RoleGrid',
                height: getDashminGridHeight(),
                dataSource: state.roles,
                allowFiltering: true,
                allowSorting: true,
                allowSelection: true,
                selectionSettings: { type: 'Single' },
                columns: [
                    { field: 'id', isPrimaryKey: true, visible: false },
                    { field: 'name', headerText: 'الدور', width: 200 },
                ],
                rowSelected: async () => {
                    const row = roleGrid.getSelectedRecords()[0];
                    if (!row) return;
                    const roleName = roleDisplayName(row);
                    if (!roleName) return;
                    state.selectedRole = roleName;
                    await methods.loadRolePermissions(roleName);
                },
            });
            roleGrid.appendTo(roleGridRef.value);
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['TelecomAdmin']);
                await SecurityManager.validateToken();
                await methods.load();
                createRoleGrid();
            } catch (e) {
                console.error('RoleList init:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return { roleGridRef, state, handler, permLabel, permKey };
    },
};

Vue.createApp(App).mount('#app');
