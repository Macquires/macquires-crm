const App = {
    setup() {
        const state = Vue.reactive({
            id: '',
            name: '',
            description: '',
            currency: '',
            street: '',
            city: '',
            state: '',
            zipCode: '',
            country: '',
            phoneNumber: '',
            faxNumber: '',
            emailAddress: '',
            website: '',
            isSaving: false,
        });

        const services = {
            loadList: () => AxiosManager.get('/Company/GetCompanyList', {}),
            loadSingle: (id) => AxiosManager.get('/Company/GetCompanySingle', { params: { id } }),
            save: (body) => AxiosManager.post('/Company/UpdateCompany', body),
        };

        const applyDto = (dto) => {
            if (!dto) return;
            state.id = dto.id || '';
            state.name = dto.name || '';
            state.description = dto.description || '';
            state.currency = dto.currency || '';
            state.street = dto.street || '';
            state.city = dto.city || '';
            state.state = dto.state || '';
            state.zipCode = dto.zipCode || '';
            state.country = dto.country || '';
            state.phoneNumber = dto.phoneNumber || '';
            state.faxNumber = dto.faxNumber || '';
            state.emailAddress = dto.emailAddress || '';
            state.website = dto.website || '';
        };

        const handler = {
            save: async () => {
                if (!state.id) {
                    Swal.fire({ icon: 'error', title: 'No company record loaded.' });
                    return;
                }
                state.isSaving = true;
                try {
                    const res = await services.save({
                        id: state.id,
                        name: state.name,
                        description: state.description,
                        currency: state.currency,
                        street: state.street,
                        city: state.city,
                        state: state.state,
                        zipCode: state.zipCode,
                        country: state.country,
                        phoneNumber: state.phoneNumber,
                        faxNumber: state.faxNumber,
                        emailAddress: state.emailAddress,
                        website: state.website,
                    });
                    if (res?.data?.code === 200) {
                        const cached = StorageManager.getCompany() || {};
                        StorageManager.saveCompany({ ...cached, ...state });
                        Swal.fire({ icon: 'success', title: 'Company saved', timer: 1800, showConfirmButton: false });
                    }
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Save failed',
                        text: e.response?.data?.message || e.message,
                    });
                } finally {
                    state.isSaving = false;
                }
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['TelecomAdmin']);
                await SecurityManager.validateToken();

                let companyId = StorageManager.getCompany()?.id;
                if (!companyId) {
                    const listRes = await services.loadList();
                    const first = listRes?.data?.content?.data?.[0];
                    companyId = first?.id;
                }
                if (!companyId) {
                    Swal.fire({ icon: 'warning', title: 'No company found in database.' });
                    return;
                }
                const singleRes = await services.loadSingle(companyId);
                applyDto(singleRes?.data?.content?.data);
            } catch (e) {
                console.error('MyCompany init:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return { state, handler };
    },
};

Vue.createApp(App).mount('#app');
