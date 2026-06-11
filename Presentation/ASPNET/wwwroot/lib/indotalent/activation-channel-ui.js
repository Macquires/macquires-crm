/**
 * Omnichannel activation UI — persona-based channel lock + labels from Global Settings.
 */
window.ActivationChannelUi = (function () {
    const SHOWROOM = 0;
    const DEALER = 1;
    const DIGITAL = 2;

    const BACK_OFFICE_ROLES = ['TelecomBackOffice', 'TelecomAdmin', 'TelecomManagement'];
    const BACK_OFFICE_PERSONAS = ['BackOffice', 'SysAdmin', 'Executive'];

    const FALLBACK = {
        showroom: { ar: 'نقطة البيع', en: 'POS' },
        dealer: { ar: 'موزع', en: 'Dealer' },
        digital: { ar: 'رقمي', en: 'Digital' },
    };

    let labels = null;
    let loadPromise = null;

    function resolveMode() {
        const roles = StorageManager.getUserRoles?.() || [];
        const persona =
            (typeof PortalNavigation !== 'undefined' && PortalNavigation.getEffectivePersona?.()) ||
            StorageManager.getPrimaryMenuPersona?.() ||
            '';
        const isAdminPortal =
            BACK_OFFICE_PERSONAS.includes(persona) ||
            roles.some((r) => BACK_OFFICE_ROLES.includes(r));
        return isAdminPortal ? 'admin' : 'showroom';
    }

    function normalizeApiLabels(content) {
        const data = content?.data ?? content?.Data ?? content ?? {};
        const showroom = data.showroom ?? data.Showroom ?? {};
        const dealer = data.dealer ?? data.Dealer ?? {};
        const digital = data.digital ?? data.Digital ?? {};
        return {
            showroom: {
                code: Number(showroom.code ?? showroom.Code ?? SHOWROOM),
                ar: (showroom.labelAr ?? showroom.LabelAr ?? FALLBACK.showroom.ar).trim(),
                en: (showroom.labelEn ?? showroom.LabelEn ?? FALLBACK.showroom.en).trim(),
            },
            dealer: {
                code: Number(dealer.code ?? dealer.Code ?? DEALER),
                ar: (dealer.labelAr ?? dealer.LabelAr ?? FALLBACK.dealer.ar).trim(),
                en: (dealer.labelEn ?? dealer.LabelEn ?? FALLBACK.dealer.en).trim(),
            },
            digital: {
                code: Number(digital.code ?? digital.Code ?? DIGITAL),
                ar: (digital.labelAr ?? digital.LabelAr ?? FALLBACK.digital.ar).trim(),
                en: (digital.labelEn ?? digital.LabelEn ?? FALLBACK.digital.en).trim(),
            },
        };
    }

    async function ensureLoaded(force) {
        if (labels && !force) {
            return labels;
        }
        if (loadPromise && !force) {
            return loadPromise;
        }
        loadPromise = (async () => {
            try {
                const res = await AxiosManager.get('/Telecom/GetActivationChannelLabels', {});
                const content = res?.data?.content ?? res?.data?.Content ?? {};
                labels = normalizeApiLabels(content);
            } catch {
                labels = normalizeApiLabels(null);
            }
            return labels;
        })();
        return loadPromise;
    }

    function entryForCode(code) {
        const c = Number(code);
        if (c === DEALER) return 'dealer';
        if (c === DIGITAL) return 'digital';
        return 'showroom';
    }

    function label(code, locale) {
        const bucket = labels || normalizeApiLabels(null);
        const key = entryForCode(code);
        const isAr = String(locale || '').toLowerCase().startsWith('ar');
        return isAr ? bucket[key].ar : bucket[key].en;
    }

    function applyDefaults(target) {
        if (!target || typeof target !== 'object') {
            return;
        }
        if (resolveMode() === 'showroom') {
            target.activationChannel = SHOWROOM;
            target.dealerCode = '';
        }
    }

    function lockedHint(locale) {
        const pos = label(SHOWROOM, locale);
        const isAr = String(locale || '').toLowerCase().startsWith('ar');
        return isAr
            ? `مساحة نقطة البيع — القناة مثبتة على «${pos}».`
            : `POS workspace — channel is fixed to «${pos}».`;
    }

    return {
        SHOWROOM,
        DEALER,
        DIGITAL,
        resolveMode,
        applyDefaults,
        ensureLoaded,
        label,
        lockedHint,
        labels: () => labels,
        isAdminPortal: () => resolveMode() === 'admin',
        isShowroomLocked: () => resolveMode() === 'showroom',
    };
})();
