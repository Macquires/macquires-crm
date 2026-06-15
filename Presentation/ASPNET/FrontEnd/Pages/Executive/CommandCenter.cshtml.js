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

    try {
        await SecurityManager.validateToken();
    } catch {
        return;
    }

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
    if (geoHost && typeof BranchGeoHeatmapPanel !== 'undefined') {
        BranchGeoHeatmapPanel.mount(geoHost);
    }

    if (typeof ExecutiveExceptionsPanel !== 'undefined') {
        ExecutiveExceptionsPanel.connectAlerts?.();
    }
});
