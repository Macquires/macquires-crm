document.addEventListener('DOMContentLoaded', async () => {
    await window.TelecomI18n?.ensureLoaded?.();
    window.TelecomI18n?.applyDom?.();
    const title = window.TelecomI18n?.t?.('strategicAnalytics.pageTitle');
    if (title) document.title = title;

    const gate = document.getElementById('strategicAnalyticsGate');
    const appRoot = document.getElementById('strategicAnalyticsApp');

    if (typeof StrategicAnalyticsPanel === 'undefined') {
        console.error('StrategicAnalyticsPanel not loaded');
        return;
    }

    if (!StrategicAnalyticsPanel.hasPermission()) {
        if (gate) gate.classList.remove('d-none');
        if (appRoot) appRoot.classList.add('d-none');
        document.documentElement.addEventListener('syriatel-locale-changed', () => {
            window.TelecomI18n?.applyDom?.();
        });
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
