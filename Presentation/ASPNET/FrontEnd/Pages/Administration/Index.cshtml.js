const ADMIN_HUB_LINKS = [
    { href: '/Administration/UserList', icon: 'bi-people-fill', i18nKey: 'users' },
    { href: '/Administration/BranchList', icon: 'bi-diagram-3-fill', i18nKey: 'branches' },
    { href: '/Administration/RoleList', icon: 'bi-shield-lock-fill', i18nKey: 'roles' },
    { href: '/Administration/GlobalSettings', icon: 'bi-sliders', i18nKey: 'globalSettings' },
    { href: '/Administration/AuditLogList', icon: 'bi-journal-text', i18nKey: 'auditLog' },
    { href: '/Telecom/IntegrationMonitor', icon: 'bi-hdd-network', i18nKey: 'integrationMonitor' },
    { href: '/Dashboards/DashboardWidgetList', icon: 'bi-layout-three-columns', i18nKey: 'dashboardWidgets' },
    { href: '/Companies/MyCompany', icon: 'bi-building', i18nKey: 'operatorCompany' },
    { href: '/NumberSequences/NumberSequenceList', icon: 'bi-123', i18nKey: 'numberSequences' },
];

const App = {
    setup() {
        const localeTick = Vue.ref(0);

        const t = (key, fallback = '') => {
            localeTick.value;
            const hit = window.TelecomI18n?.t?.(`administration.index.${key}`);
            return hit && hit !== `administration.index.${key}` ? hit : fallback;
        };

        const hubLinks = Vue.computed(() =>
            ADMIN_HUB_LINKS.map((item) => ({
                ...item,
                label: t(item.i18nKey, item.i18nKey),
            }))
        );

        const onLocaleChanged = async () => {
            localeTick.value++;
            await window.TelecomI18n?.ensureLoaded?.();
            const title = t('pageTitle');
            if (title) document.title = title;
        };

        Vue.onMounted(async () => {
            document.documentElement.addEventListener('syriatel-locale-changed', onLocaleChanged);
            try {
                await window.TelecomI18n?.ensureLoaded?.();
                await SecurityManager.authorizePage(['TelecomAdmin']);
                await SecurityManager.validateToken?.();
                const title = t('pageTitle');
                if (title) document.title = title;
            } catch (e) {
                console.error('Administration hub init:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        Vue.onUnmounted(() => {
            document.documentElement.removeEventListener('syriatel-locale-changed', onLocaleChanged);
        });

        return { t, hubLinks };
    },
};

try {
    Vue.createApp(App).mount('#app');
} catch (mountErr) {
    console.error('Administration hub mount failed:', mountErr);
    document.getElementById('app')?.removeAttribute('v-cloak');
    if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
}
