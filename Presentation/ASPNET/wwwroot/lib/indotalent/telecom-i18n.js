/**
 * Shared telecom locale loader for Razor pages (data-telecom-i18n) and scripts without Vue I18n.
 */
const TelecomI18n = (function () {
    const URLS = {
        ar: '/locales/telecom.ar.json',
        en: '/locales/telecom.en.json',
    };

    let messages = { ar: null, en: null };
    let loadPromise = null;

    function getLang() {
        const lang = (document.documentElement.lang || 'en').toLowerCase();
        return lang.startsWith('en') ? 'en' : 'ar';
    }

    function resolveKey(root, key) {
        if (!root || !key) return null;
        let cur = root;
        for (const part of String(key).split('.')) {
            cur = cur?.[part];
            if (cur == null) return null;
        }
        return typeof cur === 'string' ? cur : null;
    }

    function t(key, lang) {
        const lg = lang || getLang();
        const root = messages[lg]?.telecom;
        const hit = resolveKey(root, key);
        if (hit) return hit;
        if (lg !== 'en') {
            const enHit = resolveKey(messages.en?.telecom, key);
            if (enHit) return enHit;
        }
        return resolveKey(messages.ar?.telecom, key);
    }

    async function ensureLoaded() {
        if (messages.ar && messages.en) return;
        if (!loadPromise) {
            loadPromise = Promise.all(
                Object.entries(URLS).map(async ([lang, url]) => {
                    const res = await fetch(url, { cache: 'no-store' });
                    if (!res.ok) throw new Error('Locale load failed: ' + url);
                    const text = new TextDecoder('utf-8').decode(await res.arrayBuffer());
                    messages[lang] = JSON.parse(text);
                })
            );
        }
        await loadPromise;
    }

    function applyDomI18n(root) {
        const scope = root || document;
        scope.querySelectorAll('[data-telecom-i18n]').forEach((el) => {
            const key = el.getAttribute('data-telecom-i18n');
            const val = t(key);
            if (val) el.textContent = val;
        });
        scope.querySelectorAll('[data-telecom-i18n-placeholder]').forEach((el) => {
            const key = el.getAttribute('data-telecom-i18n-placeholder');
            const val = t(key);
            if (val) el.setAttribute('placeholder', val);
        });
        scope.querySelectorAll('[data-telecom-i18n-title]').forEach((el) => {
            const key = el.getAttribute('data-telecom-i18n-title');
            const val = t(key);
            if (val) el.setAttribute('title', val);
        });
        scope.querySelectorAll('option[data-telecom-i18n]').forEach((el) => {
            const key = el.getAttribute('data-telecom-i18n');
            const val = t(key);
            if (val) el.textContent = val;
        });
    }

    async function init(root) {
        await ensureLoaded();
        applyDomI18n(root);
    }

    async function refresh(root) {
        await ensureLoaded();
        applyDomI18n(root);
    }

    document.documentElement.addEventListener('syriatel-locale-changed', () => {
        refresh().catch(() => {});
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            init().catch(() => {});
        });
    } else {
        init().catch(() => {});
    }

    return { t, init, refresh, getLang, ensureLoaded };
})();

window.TelecomI18n = TelecomI18n;
