document.addEventListener('DOMContentLoaded', async () => {
    const gate = document.getElementById('strategicAnalyticsGate');
    const appRoot = document.getElementById('strategicAnalyticsApp');

    if (typeof StrategicAnalyticsPanel === 'undefined') {
        console.error('StrategicAnalyticsPanel not loaded');
        return;
    }

    if (!StrategicAnalyticsPanel.hasPermission()) {
        if (gate) gate.classList.remove('d-none');
        if (appRoot) appRoot.classList.add('d-none');
        return;
    }

    try {
        await SecurityManager.validateToken();
    } catch {
        return;
    }

    if (typeof StrategicAnalyticsPanel.initStandalone === 'function') {
        await StrategicAnalyticsPanel.initStandalone();
    } else if (appRoot) {
        StrategicAnalyticsPanel.mount(appRoot);
    }
});
