document.addEventListener('DOMContentLoaded', async () => {
    await window.TelecomI18n?.ensureLoaded?.();
    window.TelecomI18n?.applyDom?.();

    const title = window.TelecomI18n?.t?.('executiveCommandCenter.pageTitle');
    if (title) document.title = title;

    const gate = document.getElementById('commandCenterGate');
    const appRoot = document.getElementById('commandCenterApp');

    if (!ExecutiveScopeBar?.hasPermission?.()) {
        if (gate) gate.classList.remove('d-none');
        if (appRoot) appRoot.classList.add('d-none');
        return;
    }

    appRoot?.removeAttribute('v-cloak');

    try {
        await SecurityManager.validateToken();
    } catch {
        return;
    }

    const TAB_MAP = {
        scorecard: '#cc-scorecard',
        network: '#cc-network',
        mis: '#cc-mis',
        operations: '#cc-operations',
        workforce: '#cc-workforce',
        alerts: '#cc-alerts',
    };

    const activateTab = (tabKey, updateUrl) => {
        const key = TAB_MAP[tabKey] ? tabKey : 'scorecard';
        const target = TAB_MAP[key];
        const btn = document.querySelector(`#ccTabs [data-bs-target="${target}"]`);
        if (btn && typeof bootstrap !== 'undefined') {
            bootstrap.Tab.getOrCreateInstance(btn).show();
        }
        if (updateUrl !== false) {
            const qs = key === 'scorecard' ? '' : `?tab=${encodeURIComponent(key)}`;
            const next = `${window.location.pathname}${qs}`;
            if (`${window.location.pathname}${window.location.search}` !== next) {
                window.history.replaceState(null, '', next);
            }
        }
        if (typeof PortalNavigation !== 'undefined' && PortalNavigation.render) {
            PortalNavigation.render('syrPortalNav');
        }
    };

    const params = new URLSearchParams(window.location.search);
    activateTab(params.get('tab') || 'scorecard', false);

    document.getElementById('ccTabs')?.addEventListener('shown.bs.tab', (e) => {
        const target = e.target?.getAttribute?.('data-bs-target') || '';
        const entry = Object.entries(TAB_MAP).find(([, sel]) => sel === target);
        if (entry) {
            activateTab(entry[0]);
        }
    });

    ExecutiveScopeBar.mount(document.getElementById('executiveScopeBarHost'));
    ExecutiveScorecardPanel.mount(document.getElementById('executiveScorecardHost'));
    ExecutiveNetworkPanel.mount(document.getElementById('executiveNetworkHost'));
    ExecutiveOperationsPanel.mount(document.getElementById('executiveOperationsHost'));
    ExecutiveWorkforcePanel.mount(document.getElementById('executiveWorkforceHost'));
    ExecutiveAlertsPanel.mount(document.getElementById('executiveAlertsHost'));

    const misHost = document.getElementById('executiveMisHost');
    if (misHost && typeof StrategicAnalyticsPanel !== 'undefined') {
        StrategicAnalyticsPanel.mount(misHost, { hideFilters: true, externalScope: true });
    }

    const geoHost = document.getElementById('executiveGeoHost');
    if (geoHost && typeof BranchGeoHeatmapPanel?.mount === 'function') {
        BranchGeoHeatmapPanel.mount(geoHost);
    }

    if (typeof ExecutiveExceptionsPanel !== 'undefined') {
        ExecutiveExceptionsPanel.connectAlerts?.();
    }
});
