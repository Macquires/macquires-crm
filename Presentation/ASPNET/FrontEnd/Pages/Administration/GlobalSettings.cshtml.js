/** GeoCity CRUD — inlined (static assets only publish *.cshtml.js next to Razor pages). */
window.GeoCityAdmin = {
    create(Vue, t) {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            mainTitle: '',
            id: '',
            name: '',
            governorate: '',
            isActive: true,
            sortOrder: 0,
            errors: { name: '', governorate: '' },
            isSubmitting: false,
            initialized: false,
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);

        const services = {
            getMainData: () => AxiosManager.get('/GeoCity/GetGeoCityList', {}),
            createMainData: (payload) => AxiosManager.post('/GeoCity/CreateGeoCity', payload),
            updateMainData: (payload) => AxiosManager.post('/GeoCity/UpdateGeoCity', payload),
            deleteMainData: (payload) => AxiosManager.post('/GeoCity/DeleteGeoCity', payload),
        };

        const resetFormState = () => {
            state.id = '';
            state.name = '';
            state.governorate = '';
            state.isActive = true;
            state.sortOrder = 0;
            state.errors = { name: '', governorate: '' };
        };

        const populateMainData = async () => {
            const response = await services.getMainData();
            state.mainData = (response?.data?.content?.data ?? []).map((item) => ({
                ...item,
                createdAtUtc: item.createdAtUtc ? new Date(item.createdAtUtc) : null,
            }));
        };

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: getDashminGridHeight(),
                    dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    sortSettings: { columns: [{ field: 'sortOrder', direction: 'Ascending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ['10', '20', '50', '100', '200', 'All'] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        { field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false },
                        { field: 'name', headerText: t('colCity'), width: 180 },
                        { field: 'governorate', headerText: t('colGovernorate'), width: 180 },
                        { field: 'sortOrder', headerText: t('colSort'), width: 90, textAlign: 'Center' },
                        { field: 'isActive', headerText: t('colActive'), width: 90, displayAsCheckBox: true, textAlign: 'Center' },
                        { field: 'createdAtUtc', headerText: t('colCreated'), width: 150, format: 'yyyy-MM-dd HH:mm' },
                    ],
                    toolbar: [
                        'ExcelExport',
                        'Search',
                        { type: 'Separator' },
                        { text: t('add'), prefixIcon: 'e-add', id: 'AddCustom' },
                        { text: t('edit'), prefixIcon: 'e-edit', id: 'EditCustom' },
                        { text: t('delete'), prefixIcon: 'e-delete', id: 'DeleteCustom' },
                    ],
                    dataBound() {
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], false);
                    },
                    rowSelected() {
                        const one = mainGrid.obj.getSelectedRecords().length === 1;
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], one);
                    },
                    rowDeselected() {
                        const one = mainGrid.obj.getSelectedRecords().length === 1;
                        mainGrid.obj.toolbarModule.enableItems(['EditCustom', 'DeleteCustom'], one);
                    },
                    rowSelecting() {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id?.endsWith('_excelexport')) {
                            mainGrid.obj.excelExport();
                        }
                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = t('modalAdd');
                            resetFormState();
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'EditCustom' && mainGrid.obj.getSelectedRecords().length) {
                            state.deleteMode = false;
                            const row = mainGrid.obj.getSelectedRecords()[0];
                            state.mainTitle = t('modalEdit');
                            state.id = row.id ?? '';
                            state.name = row.name ?? '';
                            state.governorate = row.governorate ?? '';
                            state.isActive = row.isActive !== false;
                            state.sortOrder = row.sortOrder ?? 0;
                            mainModal.obj.show();
                        }
                        if (args.item.id === 'DeleteCustom' && mainGrid.obj.getSelectedRecords().length) {
                            state.deleteMode = true;
                            const row = mainGrid.obj.getSelectedRecords()[0];
                            state.mainTitle = t('modalDelete');
                            state.id = row.id ?? '';
                            state.name = row.name ?? '';
                            state.governorate = row.governorate ?? '';
                            state.isActive = row.isActive !== false;
                            state.sortOrder = row.sortOrder ?? 0;
                            mainModal.obj.show();
                        }
                    },
                });
                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh() {
                if (mainGrid.obj) {
                    mainGrid.obj.setProperties({ dataSource: state.mainData });
                }
            },
        };

        const mainModal = {
            obj: null,
            create() {
                if (!mainModalRef.value) return;
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false,
                });
            },
        };

        const handler = {
            handleSubmit: async () => {
                try {
                    state.isSubmitting = true;
                    state.errors.name = '';
                    state.errors.governorate = '';
                    let isValid = true;
                    if (!state.name?.trim()) {
                        state.errors.name = t('errName');
                        isValid = false;
                    }
                    if (!state.governorate?.trim()) {
                        state.errors.governorate = t('errGovernorate');
                        isValid = false;
                    }
                    if (!isValid) return;

                    const uid = StorageManager.getUserId();
                    let response;
                    if (state.id === '') {
                        response = await services.createMainData({
                            name: state.name.trim(),
                            governorate: state.governorate.trim(),
                            isActive: state.isActive,
                            sortOrder: state.sortOrder || 0,
                            createdById: uid,
                        });
                    } else if (state.deleteMode) {
                        response = await services.deleteMainData({ id: state.id, deletedById: uid });
                    } else {
                        response = await services.updateMainData({
                            id: state.id,
                            name: state.name.trim(),
                            governorate: state.governorate.trim(),
                            isActive: state.isActive,
                            sortOrder: state.sortOrder || 0,
                            updatedById: uid,
                        });
                    }

                    if (response.data.code === 200) {
                        await populateMainData();
                        mainGrid.refresh();
                        Swal.fire({
                            icon: 'success',
                            title: state.deleteMode ? t('deleted') : t('saved'),
                            timer: 1800,
                            showConfirmButton: false,
                        });
                        mainModal.obj.hide();
                        resetFormState();
                    } else {
                        Swal.fire({ icon: 'error', title: t('saveFailed'), text: response.data.message });
                    }
                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: t('saveFailed'),
                        text: error.response?.data?.message ?? error.message,
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
        };

        const init = async () => {
            if (state.initialized) return;
            await populateMainData();
            if (mainGridRef.value && !mainGrid.obj) {
                await mainGrid.create(state.mainData);
            }
            mainModal.create();
            state.initialized = true;
        };

        const rebuildGrid = async () => {
            if (!mainGridRef.value) return;
            const data = state.mainData.slice();
            if (mainGrid.obj) {
                mainGrid.obj.destroy();
                mainGrid.obj = null;
            }
            await mainGrid.create(data);
        };

        return {
            cityState: state,
            cityHandler: handler,
            cityGridRef: mainGridRef,
            cityModalRef: mainModalRef,
            initCities: init,
            rebuildGrid,
        };
    },
};

const TELECOM_SETTINGS_DEFAULTS = () => ({
    inventory: {
        msisdnQuarantineDays: 90,
        simQuarantineDays: 90,
        msisdnReservationMinutes: 15,
        msisdnReservationHours: 24,
    },
    activation: {
        maxLinesIndividual: 5,
        maxLinesCorporate: 50,
        dealerCodeRequired: true,
        requireKycBeforeConfirm: true,
        requirePaymentReferenceWhenDepositDue: true,
        channelShowroomLabelAr: 'نقطة البيع',
        channelShowroomLabelEn: 'POS',
        channelDealerLabelAr: 'موزع',
        channelDealerLabelEn: 'Dealer',
        channelDigitalLabelAr: 'رقمي',
        channelDigitalLabelEn: 'Digital',
    },
    termination: {
        defaultFinalBillAmountSyp: 12500,
        backOfficeTypes: 'Fraud,Regulatory,Collections',
        corporateRequiresBackOffice: true,
        voluntaryRequiresRetentionOutcome: true,
    },
    refund: {
        dualApprovalThresholdSyp: 500000,
        highRiskMethods: 'SyriatelCash',
    },
    suspension: {
        longReviewDays: 90,
        backOfficeTypes: 'Fraud,Regulatory',
        corporateRequiresBackOffice: true,
    },
    simSwap: {
        lostStolenRequiresBackOffice: true,
    },
    changeNumber: {
        premiumCategories: 'Silver,Gold,Platinum',
        blockQuarantinedTarget: true,
    },
    takeOver: {
        alwaysRequiresBackOffice: true,
        defaultDepositTransferPolicy: 'TransferToNewOwner',
    },
    deviceSales: {
        creditScoreLowThreshold: 550,
        creditScoreGoodThreshold: 650,
        downPaymentPercentReject: 100,
        downPaymentPercentVip: 10,
        downPaymentPercentLow: 20,
        downPaymentPercentGood: 15,
    },
});

const mergeTelecomSettings = (target, source) => {
    if (!source || typeof source !== 'object') return;
    Object.keys(target).forEach((section) => {
        if (source[section] && typeof source[section] === 'object') {
            Object.assign(target[section], source[section]);
        }
    });
};

const applySettingsFromApi = (stateSettings, data) => {
    if (!data) return;
    const { telecom, ...rest } = data;
    Object.assign(stateSettings, rest);
    if (!stateSettings.telecom) {
        stateSettings.telecom = TELECOM_SETTINGS_DEFAULTS();
    }
    mergeTelecomSettings(stateSettings.telecom, telecom);
};

const ADMIN_SETTINGS_TABS = [
    { id: 'cities', icon: 'bi bi-geo-alt', i18nKey: 'tabs.cities', saveMode: 'embedded' },
    { id: 'security', icon: 'bi bi-shield-lock', i18nKey: 'tabs.security', saveMode: 'settings' },
    { id: 'maintenance', icon: 'bi bi-cone-striped', i18nKey: 'tabs.maintenance', saveMode: 'settings' },
    { id: 'notifications', icon: 'bi bi-bell', i18nKey: 'tabs.notifications', saveMode: 'settings' },
    { id: 'integrations', icon: 'bi bi-plug', i18nKey: 'tabs.integrations', saveMode: 'settings', flushOnSave: true },
    { id: 'telecom-lifecycle', icon: 'bi bi-arrow-repeat', i18nKey: 'tabs.telecomLifecycle', saveMode: 'settings', isTelecom: true },
    { id: 'telecom-activation', icon: 'bi bi-phone', i18nKey: 'tabs.telecomActivation', saveMode: 'settings', isTelecom: true },
    { id: 'telecom-financial', icon: 'bi bi-cash-stack', i18nKey: 'tabs.telecomFinancial', saveMode: 'settings', isTelecom: true },
    { id: 'telecom-operations', icon: 'bi bi-sliders', i18nKey: 'tabs.telecomOperations', saveMode: 'settings', isTelecom: true },
];

const VALID_TAB_IDS = new Set(ADMIN_SETTINGS_TABS.map((x) => x.id));

const App = {
    setup() {
        const localeTick = Vue.ref(0);

        const state = Vue.reactive({
            activeTab: 'cities',
            settings: {
                maintenanceMode: false,
                maintenanceMessageAr: '',
                maintenanceMessageEn: '',
                maintenanceBypassIPs: '127.0.0.1',
                isStrictPersonaMode: true,
                isDemoVersion: true,
                jwtAccessTokenMinutes: 60,
                integrationHuaweiEnabled: true,
                integrationHlrEnabled: true,
                integrationSmsEnabled: true,
                integrationInEnabled: true,
                integrationInGatewayUrl: 'https://in-gateway.syriatel.local/api/v1',
                integrationInTimeoutMilliseconds: 5000,
                integrationInSimulateRealTimeDeduction: true,
                notificationSmsCustomerOpsEnabled: true,
                notificationSmsWelcomeEnabled: true,
                notificationSmsWelcomeTemplateAr: '',
                notificationSmsWelcomeTemplateEn: '',
                telecom: TELECOM_SETTINGS_DEFAULTS(),
            },
            savingTab: '',
            settingsLoaded: false,
        });

        const t = (key, fallback = '') => {
            localeTick.value;
            const hit = window.TelecomI18n?.t?.(`admin.globalSettings.${key}`);
            return hit && hit !== `admin.globalSettings.${key}` ? hit : fallback;
        };

        const cityT = (key) => {
            const common = ['saveFailed'];
            if (common.includes(key)) return t(key, key);
            return t(`cities.${key}`, key);
        };
        const cityAdmin = window.GeoCityAdmin.create(Vue, cityT);

        const services = {
            load: () => AxiosManager.get('/Security/GetGlobalSettings'),
            save: (body) => AxiosManager.post('/Security/UpdateGlobalSettings', body),
        };

        const resolveTabFromUrl = () => {
            const params = new URLSearchParams(window.location.search);
            const tab = (params.get('tab') || '').trim().toLowerCase();
            if (VALID_TAB_IDS.has(tab)) {
                return tab;
            }
            const hash = (window.location.hash || '').replace('#', '').trim().toLowerCase();
            if (VALID_TAB_IDS.has(hash)) {
                return hash;
            }
            return 'cities';
        };

        const setActiveTab = async (tabId) => {
            if (!VALID_TAB_IDS.has(tabId)) return;
            state.activeTab = tabId;
            const url = new URL(window.location.href);
            url.searchParams.set('tab', tabId);
            window.history.replaceState({}, '', url.toString());
            if (tabId === 'cities') {
                await cityAdmin.initCities();
            }
        };

        const handler = {
            saveTab: async (tabId) => {
                state.savingTab = tabId;
                try {
                    const res = await services.save({
                        settings: {
                            ...state.settings,
                            telecom: state.settings.telecom,
                        },
                        updatedById: StorageManager.getUserId(),
                    });
                    if (res?.data?.code === 200) {
                        const content = res?.data?.content;
                        const d = content?.data;
                        if (d) {
                            applySettingsFromApi(state.settings, d);
                        }
                        const sync = content?.pendingSync;
                        let text = t('saveSuccessHint', 'تُطبَّق فوراً على الجلسات النشطة.');
                        if (tabId?.startsWith('telecom-')) {
                            text = t('telecom.saveHint', text);
                        }
                        if (tabId === 'integrations' && sync?.processed > 0) {
                            text += ` ${t('syncSummary', 'مزامنة')}: ${sync.succeeded}/${sync.processed}`;
                        }
                        Swal.fire({
                            icon: 'success',
                            title: t('saveSuccess', 'تم الحفظ'),
                            text,
                            timer: sync?.processed > 0 ? 4000 : 2200,
                            showConfirmButton: false,
                        });
                    }
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: t('saveFailed', 'فشل الحفظ'),
                        text: e.response?.data?.message || e.message,
                    });
                } finally {
                    state.savingTab = '';
                }
            },
        };

        const tabs = Vue.computed(() =>
            ADMIN_SETTINGS_TABS.map((tab) => ({
                ...tab,
                label: t(tab.i18nKey, tab.id),
            }))
        );

        const integrationToggles = Vue.computed(() => [
            {
                id: 'Huawei',
                field: 'integrationHuaweiEnabled',
                title: t('integrations.huaweiTitle', 'Huawei CBS'),
                hint: t('integrations.huaweiHint', ''),
                icon: 'bi bi-hdd-network',
                iconClass: 'icon-cbs',
                enabled: state.settings.integrationHuaweiEnabled,
            },
            {
                id: 'In',
                field: 'integrationInEnabled',
                title: t('integrations.inTitle', 'Huawei IN'),
                hint: t('integrations.inHint', ''),
                icon: 'bi bi-lightning-charge',
                iconClass: 'icon-in',
                enabled: state.settings.integrationInEnabled,
            },
            {
                id: 'Hlr',
                field: 'integrationHlrEnabled',
                title: t('integrations.hlrTitle', 'HLR'),
                hint: t('integrations.hlrHint', ''),
                icon: 'bi bi-broadcast',
                iconClass: 'icon-hlr',
                enabled: state.settings.integrationHlrEnabled,
            },
            {
                id: 'Sms',
                field: 'integrationSmsEnabled',
                title: t('integrations.smsTitle', 'SMS Gateway'),
                hint: t('integrations.smsHint', ''),
                icon: 'bi bi-chat-dots',
                iconClass: 'icon-sms',
                enabled: state.settings.integrationSmsEnabled,
            },
        ]);

        const integrationStatusBadge = (enabled) => {
            localeTick.value;
            const mode = enabled ? 'live' : 'fallback';
            const label = enabled ? t('integrations.badgeOn', 'ON') : t('integrations.badgeOff', 'OFF');
            return window.TelecomUiBadges?.integrationHealth(mode, label) || label;
        };

        const onLocaleChanged = async () => {
            localeTick.value++;
            await window.TelecomI18n?.ensureLoaded?.();
            const title = t('pageTitle');
            if (title) document.title = title;
            if (state.activeTab === 'cities') {
                try {
                    await cityAdmin.rebuildGrid?.();
                } catch (e) {
                    console.warn('GlobalSettings: city grid locale refresh failed', e);
                }
            }
        };

        Vue.watch(
            () => state.activeTab,
            async (tab) => {
                if (tab === 'cities') {
                    await cityAdmin.initCities();
                }
            }
        );

        Vue.onMounted(async () => {
            document.documentElement.addEventListener('syriatel-locale-changed', onLocaleChanged);
            try {
                await window.TelecomI18n?.ensureLoaded?.();
                const title = t('pageTitle');
                if (title) document.title = title;
                await SecurityManager.authorizePage(['TelecomAdmin']);
                await SecurityManager.validateToken();
                const res = await services.load();
                const d = res?.data?.content?.data;
                if (d) {
                    applySettingsFromApi(state.settings, d);
                }
                state.settingsLoaded = true;
                state.activeTab = resolveTabFromUrl();
                const url = new URL(window.location.href);
                if (!url.searchParams.get('tab') && VALID_TAB_IDS.has(state.activeTab)) {
                    url.searchParams.set('tab', state.activeTab);
                    window.history.replaceState({}, '', url.toString());
                }
                if (state.activeTab === 'cities') {
                    await cityAdmin.initCities();
                }
            } catch (e) {
                console.error('GlobalSettings init:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        Vue.onUnmounted(() => {
            document.documentElement.removeEventListener('syriatel-locale-changed', onLocaleChanged);
        });

        return {
            state,
            handler,
            tabs,
            t,
            setActiveTab,
            integrationToggles,
            integrationStatusBadge,
            cityState: cityAdmin.cityState,
            cityHandler: cityAdmin.cityHandler,
            cityGridRef: cityAdmin.cityGridRef,
            cityModalRef: cityAdmin.cityModalRef,
        };
    },
};

try {
    Vue.createApp(App).mount('#app');
} catch (mountErr) {
    console.error('GlobalSettings mount failed:', mountErr);
    document.getElementById('app')?.removeAttribute('v-cloak');
    if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
}
