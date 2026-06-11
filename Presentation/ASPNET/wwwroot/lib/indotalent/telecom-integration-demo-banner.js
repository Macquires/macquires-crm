/**
 * Integration environment indicator — reads System.IsDemoVersion via Global Settings API.
 */
window.TelecomIntegrationDemoBanner = (function () {
    let cached = null;

    const parseDemoFlag = (res) => {
        const content = res?.data?.content ?? res?.data?.Content;
        const data = content?.data ?? content?.Data ?? content;
        return !!(data?.isDemoVersion ?? data?.IsDemoVersion);
    };

    const t = (key) => {
        const hit = window.TelecomI18n?.t?.(`integrationDemoBanner.${key}`);
        return hit && hit !== `integrationDemoBanner.${key}` ? hit : key;
    };

    return {
        clearCache() {
            cached = null;
        },

        async isDemoVersion() {
            if (cached !== null) return cached;
            try {
                const res = await AxiosManager.get('/Telecom/GetIntegrationEnvironmentContext', {});
                cached = parseDemoFlag(res);
            } catch {
                cached = false;
            }
            return cached;
        },

        title() {
            return t('title');
        },

        message() {
            return t('message');
        },

        chip() {
            return t('chip');
        },
    };
})();
