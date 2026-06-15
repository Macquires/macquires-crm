/**
 * Shared product-offering detail helpers for telecom wizards.
 */
(function (global) {
    'use strict';

    function parseProductOfferingDetail(res) {
        const c = res?.data?.content ?? res?.content ?? {};
        return {
            id: c.id ?? c.Id ?? '',
            name: c.name ?? c.Name ?? '',
            nameEn: c.nameEn ?? c.NameEn ?? '',
            code: c.code ?? c.Code ?? c.serviceIdSocCode ?? c.ServiceIdSocCode ?? '',
            description: c.description ?? c.Description ?? '',
            shortDescription: c.shortDescription ?? c.ShortDescription ?? '',
            voiceMinutesLimit: c.voiceMinutesLimit ?? c.VoiceMinutesLimit,
            speedQuotaLimitGb: c.speedQuotaLimitGb ?? c.SpeedQuotaLimitGb,
            iconClass: c.iconClass ?? c.IconClass ?? 'bi-box-seam',
            badgeColor: c.badgeColor ?? c.BadgeColor ?? 'primary',
            components: Array.isArray(c.components) ? c.components : c.Components ?? [],
            pricePlans: Array.isArray(c.pricePlans) ? c.pricePlans : c.PricePlans ?? [],
        };
    }

    function displayName(detail, lang) {
        if (!detail) return '';
        const ar = (detail.name || '').trim();
        const en = (detail.nameEn || '').trim();
        return lang === 'ar' ? ar || en : en || ar;
    }

    function defaultMonthlyPrice(detail) {
        const plans = detail?.pricePlans || [];
        const def = plans.find((p) => p.isDefault || p.IsDefault) || plans[0];
        if (!def) return null;
        const price = Number(def.price ?? def.Price);
        return Number.isFinite(price) && price > 0 ? price : null;
    }

    function componentByType(detail, type) {
        return (detail?.components || []).find((c) => (c.componentType ?? c.ComponentType) === type);
    }

    function formatQuotaLine(comp, lang) {
        if (!comp) return '';
        if (comp.isUnlimited || comp.IsUnlimited) return lang === 'ar' ? 'بلا حدود' : 'Unlimited';
        const q = comp.quota ?? comp.Quota;
        const u = String(comp.quotaUnit ?? comp.QuotaUnit ?? '').toLowerCase();
        const type = comp.componentType ?? comp.ComponentType;
        if (lang === 'en') {
            if (type === 0 || u === 'minutes') return `${q} local minutes`;
            if (type === 1 || u === 'gb') return `${q} GB`;
            if (type === 2 || u === 'sms') return `${q} SMS`;
            const label = comp.label ?? comp.Label;
            return label || `${q ?? ''} ${u}`.trim();
        }
        const label = comp.label ?? comp.Label;
        if (label) return label;
        if (type === 0 || u === 'minutes') return `${q} دقيقة محلية`;
        if (type === 1 || u === 'gb') return `${q} جيجا`;
        if (type === 2 || u === 'sms') return `${q} رسالة`;
        return `${q ?? ''} ${u}`.trim();
    }

    function summaryText(detail, lang) {
        if (!detail) return '';
        if (lang === 'ar') {
            return (detail.shortDescription || detail.description || '').trim();
        }
        return (detail.shortDescription || detail.description || '').trim();
    }

    async function loadById(offerId) {
        const id = (offerId || '').trim();
        if (!id) return null;
        const res = await AxiosManager.get(
            `/ProductOffering/GetProductOfferingSingle?id=${encodeURIComponent(id)}`,
            {}
        );
        return parseProductOfferingDetail(res);
    }

    global.TelecomOfferDetail = {
        parseProductOfferingDetail,
        displayName,
        defaultMonthlyPrice,
        componentByType,
        formatQuotaLine,
        summaryText,
        loadById,
    };
})(typeof window !== 'undefined' ? window : globalThis);
