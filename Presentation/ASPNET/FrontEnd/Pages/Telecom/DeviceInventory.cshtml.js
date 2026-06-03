const { createApp, reactive, onMounted } = Vue;

createApp({
    setup() {
        const state = reactive({ rows: [], loading: false, busy: false, form: { imei: '', model: '', listPrice: 0, branchId: '' } });
        const load = async () => {
            state.loading = true;
            try {
                const res = await AxiosManager.get('/Telecom/GetDeviceInventoryList', {});
                state.rows = res?.data?.content?.data ?? res?.data?.content?.Data ?? [];
            } finally {
                state.loading = false;
            }
        };
        const createDevice = async () => {
            state.busy = true;
            try {
                await AxiosManager.post('/Telecom/CreateDeviceInventory', {
                    imei: state.form.imei.trim(),
                    model: state.form.model.trim(),
                    listPrice: state.form.listPrice,
                    branchId: state.form.branchId || null,
                    createdById: StorageManager.getUserId(),
                });
                state.form.imei = '';
                state.form.model = '';
                await load();
            } catch (e) {
                if (window.Swal) Swal.fire({ icon: 'error', text: e?.response?.data?.message || e.message });
            } finally {
                state.busy = false;
            }
        };
        onMounted(load);
        return { ...Vue.toRefs(state), load, createDevice };
    },
}).mount('#app');
