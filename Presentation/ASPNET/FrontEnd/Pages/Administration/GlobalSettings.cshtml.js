const App = {
    setup() {
        const state = Vue.reactive({
            settings: {
                maintenanceMode: false,
                maintenanceMessageAr: '',
                maintenanceMessageEn: '',
                maintenanceBypassIPs: '127.0.0.1',
                isStrictPersonaMode: true,
                jwtAccessTokenMinutes: 60,
                integrationHuaweiEnabled: true,
                integrationHlrEnabled: true,
                integrationSmsEnabled: true,
            },
            isSaving: false,
        });

        const services = {
            load: () => AxiosManager.get('/Security/GetGlobalSettings'),
            save: (body) => AxiosManager.post('/Security/UpdateGlobalSettings', body),
        };

        const handler = {
            save: async () => {
                state.isSaving = true;
                try {
                    const res = await services.save({
                        settings: { ...state.settings },
                        updatedById: StorageManager.getUserId(),
                    });
                    if (res?.data?.code === 200) {
                        const content = res?.data?.content;
                        const d = content?.data;
                        if (d) {
                            Object.assign(state.settings, d);
                        }
                        const sync = content?.pendingSync;
                        let text = 'تُطبَّق فوراً على الصيانة والتكاملات ووضع Persona.';
                        if (sync?.processed > 0) {
                            text += ` تمت مزامنة ${sync.succeeded} عملية معلقة فوراً (${sync.stillPending} لا تزال معلقة).`;
                        }
                        Swal.fire({
                            icon: 'success',
                            title: 'تم حفظ الإعدادات',
                            text,
                            timer: sync?.processed > 0 ? 4000 : 2200,
                            showConfirmButton: false,
                        });
                    }
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: 'فشل الحفظ',
                        text: e.response?.data?.message || e.message,
                    });
                } finally {
                    state.isSaving = false;
                }
            },
        };

        const integrationToggles = Vue.computed(() => [
            {
                id: 'Huawei',
                field: 'integrationHuaweiEnabled',
                title: 'Huawei CBS',
                hint: 'فوترة وتفعيل على نظام الفوترة المركزي.',
                icon: 'bi bi-hdd-network',
                iconClass: 'icon-cbs',
                enabled: state.settings.integrationHuaweiEnabled,
            },
            {
                id: 'Hlr',
                field: 'integrationHlrEnabled',
                title: 'HLR / الشبكة',
                hint: 'تفعيل، SIM Swap، Migration على الأبراج.',
                icon: 'bi bi-broadcast',
                iconClass: 'icon-hlr',
                enabled: state.settings.integrationHlrEnabled,
            },
            {
                id: 'Sms',
                field: 'integrationSmsEnabled',
                title: 'SMS Gateway',
                hint: 'رسائل ترحيب وتذاكر الدعم.',
                icon: 'bi bi-chat-dots',
                iconClass: 'icon-sms',
                enabled: state.settings.integrationSmsEnabled,
            },
        ]);

        const integrationStatusBadge = (enabled) => {
            const mode = enabled ? 'live' : 'fallback';
            const label = enabled ? 'تشغيل حي (ON)' : 'طوارئ Fallback (OFF)';
            return window.TelecomUiBadges?.integrationHealth(mode, label) || label;
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(['TelecomAdmin']);
                await SecurityManager.validateToken();
                const res = await services.load();
                const d = res?.data?.content?.data;
                if (d) {
                    Object.assign(state.settings, d);
                }
            } catch (e) {
                console.error('GlobalSettings init:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return { state, handler, integrationToggles, integrationStatusBadge };
    },
};

Vue.createApp(App).mount('#app');
