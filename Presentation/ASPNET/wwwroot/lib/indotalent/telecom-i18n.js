/**
 * Shared telecom locale loader for Razor pages (data-telecom-i18n) and scripts without Vue I18n.
 */
const TelecomI18n = (function () {
    const URLS = {
        ar: '/locales/telecom.ar.json',
        en: '/locales/telecom.en.json',
    };

    const RECHARGE_SWAL_KEYS = [
        'rechargeMethodHint',
        'rechargeAmountEmpty',
        'rechargeAmountInvalid',
        'rechargeAmountTooHigh',
        'rechargeAmountHint',
        'rechargeAmountFooter',
        'rechargeInvalidMsisdn',
        'rechargeRefHint',
        'rechargeRefEmpty',
        'rechargeVoucherHint',
        'rechargeInvalidAmount',
    ];

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

    /** Mirror wizardUi + shared swal keys so Hub / List / C360 resolve the same paths. */
    function wireTelecomLocaleAliases(telecomRoot) {
        if (!telecomRoot || typeof telecomRoot !== 'object') return telecomRoot;

        if (!telecomRoot.wizardUi && telecomRoot.customer360Profile?.wizardUi) {
            telecomRoot.wizardUi = telecomRoot.customer360Profile.wizardUi;
        }

        telecomRoot.customerList = telecomRoot.customerList || {};
        if (!telecomRoot.customerList.wizardUi && telecomRoot.wizardUi) {
            telecomRoot.customerList.wizardUi = telecomRoot.wizardUi;
        }

        if (telecomRoot.swal && telecomRoot.customerList) {
            telecomRoot.customerList.swal = telecomRoot.customerList.swal || {};
            RECHARGE_SWAL_KEYS.forEach((k) => {
                if (telecomRoot.swal[k] && !telecomRoot.customerList.swal[k]) {
                    telecomRoot.customerList.swal[k] = telecomRoot.swal[k];
                }
            });
            ['ok', 'confirm', 'yes', 'no', 'cancel', 'continue'].forEach((k) => {
                if (telecomRoot.swal[k] && !telecomRoot.customerList.swal[k]) {
                    telecomRoot.customerList.swal[k] = telecomRoot.swal[k];
                }
            });
        }

        if (telecomRoot.customer360Profile?.swal) {
            const c360Swal = telecomRoot.customer360Profile.swal;
            if (!c360Swal.confirm) {
                c360Swal.confirm =
                    c360Swal.confirmRecharge
                    || telecomRoot.swal?.confirm
                    || telecomRoot.common?.confirm
                    || 'Confirm';
            }
            if (!c360Swal.ok && telecomRoot.swal?.ok) c360Swal.ok = telecomRoot.swal.ok;
            if (!c360Swal.cancel && telecomRoot.common?.cancel) c360Swal.cancel = telecomRoot.common.cancel;
        }

        return telecomRoot;
    }

    function wireLocaleBundle(bundle) {
        if (bundle?.telecom) wireTelecomLocaleAliases(bundle.telecom);
        return bundle;
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

    function buildResolvePaths(key, preferredScopes) {
        const raw = String(key);
        const paths = [];
        const add = (p) => {
            if (p && !paths.includes(p)) paths.push(p);
        };

        add(raw);
        (preferredScopes || []).forEach((scope) => add(`${scope}.${raw}`));

        if (raw.startsWith('wizardUi.')) {
            add(`customer360Profile.${raw}`);
            add(`customerList.${raw}`);
        }
        if (raw.startsWith('swal.') || raw.startsWith('common.')) {
            add(`customer360Profile.${raw}`);
            add(`customerList.${raw}`);
        }

        return paths;
    }

    function resolve(key, fallback, preferredScopes) {
        for (const path of buildResolvePaths(key, preferredScopes)) {
            const hit = t(path);
            if (hit) return hit;
        }
        return fallback;
    }

    function swalLabels(lang) {
        const pick = (key, fb) => t(key, lang) || fb;
        return {
            ok: pick('swal.ok', pick('common.ok', 'OK')),
            confirm: pick('swal.confirm', pick('common.confirm', 'Confirm')),
            cancel: pick('common.cancel', 'Cancel'),
            continue: pick('common.continue', 'Continue'),
            yes: pick('swal.yes', pick('common.yes', 'Yes')),
            no: pick('swal.no', pick('common.no', 'No')),
            close: pick('common.close', pick('swal.close', 'Close')),
            incompleteTitle: pick('swal.incompleteTitle', 'Missing details'),
        };
    }

    function swalDefaults(extra, lang) {
        const labels = swalLabels(lang);
        const merged = { ...(extra || {}) };
        if (!merged.confirmButtonText) {
            if (merged.showCancelButton && merged.input) {
                merged.confirmButtonText = labels.continue;
            } else if (merged.showCancelButton && merged.icon === 'question') {
                merged.confirmButtonText = labels.yes;
            } else if (merged.showCancelButton) {
                merged.confirmButtonText = labels.confirm;
            } else {
                merged.confirmButtonText = labels.ok;
            }
        }
        if (merged.showCancelButton && !merged.cancelButtonText) {
            merged.cancelButtonText = labels.cancel;
        }
        if (merged.showDenyButton && !merged.denyButtonText) {
            merged.denyButtonText = labels.no;
        }
        return merged;
    }

    async function ensureLoaded() {
        if (messages.ar && messages.en) return;
        if (!loadPromise) {
            loadPromise = Promise.all(
                Object.entries(URLS).map(async ([lang, url]) => {
                    const res = await fetch(url, { cache: 'no-store' });
                    if (!res.ok) throw new Error('Locale load failed: ' + url);
                    const text = new TextDecoder('utf-8').decode(await res.arrayBuffer());
                    messages[lang] = wireLocaleBundle(JSON.parse(text));
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

    return {
        t,
        resolve,
        swalLabels,
        swalDefaults,
        wireLocaleBundle,
        wireTelecomLocaleAliases,
        init,
        refresh,
        getLang,
        ensureLoaded,
        applyDomI18n,
        applyDom: applyDomI18n,
    };
})();

window.TelecomI18n = TelecomI18n;
