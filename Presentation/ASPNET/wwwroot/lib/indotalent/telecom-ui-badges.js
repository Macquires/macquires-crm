/**
 * Duotone status badges for telecom premium UI (pool, integration, log codes).
 */
(function (global) {
    const escapeHtml = (s) =>
        String(s ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');

    const poolStatusClass = (status) => {
        switch (status) {
            case 'Available':
            case 'Active':
                return 'duotone-success';
            case 'Reserved':
                return 'duotone-warning';
            case 'Quarantined':
            case 'Suspended':
                return 'duotone-danger';
            default:
                return 'duotone-neutral';
        }
    };

    const integrationModeClass = (mode) => (mode === 'live' ? 'duotone-success' : 'duotone-warning');

    const responseStatusClass = (code) => {
        const c = String(code || '').toUpperCase();
        if (c === 'FALLBACK_MODE' || c === 'CIRCUIT_OPEN') return 'duotone-warning';
        if (c === '200' || c === 'SYNC_REPLAY' || c === 'OK') return 'duotone-success';
        if (c === 'ERROR' || c === 'DISABLED') return 'duotone-danger';
        return 'duotone-neutral';
    };

    const withPulse = (cls) =>
        cls === 'duotone-success' ||
        cls === 'duotone-warning' ||
        cls === 'duotone-info' ||
        cls === 'duotone-danger';

    const ticketPriorityClass = (priority) => {
        const p = Number(priority);
        if (p === 3) return 'duotone-danger';
        if (p === 2) return 'duotone-warning';
        if (p === 1) return 'duotone-info';
        return 'duotone-neutral';
    };

    const ticketStatusClass = (status) => {
        const s = Number(status);
        if (s === 2) return 'duotone-success';
        if (s === 1) return 'duotone-warning';
        if (s === 3) return 'duotone-danger';
        return 'duotone-info';
    };

    const badgeT = (key, fallback) => {
        try {
            const hit = global.TelecomI18n?.t?.(key);
            return hit || fallback;
        } catch {
            return fallback;
        }
    };

    const ticketStatusLabel = (status) => {
        const n = Number(status);
        const fallbacks = {
            0: 'مفتوحة',
            1: 'قيد المعالجة',
            2: 'تم الحل',
            3: 'مصعّدة',
        };
        return badgeT(`backOffice.dashboard.enums.status.${n}`, fallbacks[n]) || '—';
    };

    const ticketChannelClass = (channel) => {
        const c = String(channel || '');
        if (c === 'Customer_Care_Voice_AI') return 'duotone-ai';
        if (c === 'Self_Care_App') return 'duotone-info';
        if (c === 'Showroom_Agent') return 'duotone-neutral';
        return 'duotone-neutral';
    };

    const ticketCategoryClass = (category) => {
        const c = String(category ?? '').toLowerCase();
        const n = Number(category);
        if (c === 'simswap' || n === 1) return 'duotone-info';
        if (c === 'packagemigration' || n === 2) return 'duotone-warning';
        if (c === 'ownershiptransfer' || n === 3) return 'duotone-neutral';
        if (c === 'lineactivation' || n === 4) return 'duotone-success';
        if (c === 'vasactivation' || n === 5) return 'duotone-warning';
        return 'duotone-ai';
    };

    const ticketCategoryLabel = (category) => {
        const c = String(category ?? '').toLowerCase();
        const n = Number(category);
        const keyByCat = {
            simswap: 'simswap',
            packagemigration: 'packagemigration',
            ownershiptransfer: 'ownershiptransfer',
            lineactivation: 'lineactivation',
            vasactivation: 'vasactivation',
            complaint: 'complaint',
        };
        let key = keyByCat[c];
        if (!key && n === 1) key = 'simswap';
        if (!key && n === 2) key = 'packagemigration';
        if (!key && n === 3) key = 'ownershiptransfer';
        if (!key && n === 4) key = 'lineactivation';
        if (!key && n === 5) key = 'vasactivation';
        if (!key && n === 0) key = 'complaint';
        if (!key) return '—';
        const fallbacks = {
            simswap: 'تبديل شريحة',
            packagemigration: 'ترحيل باقة',
            ownershiptransfer: 'نقل ملكية',
            lineactivation: 'تفعيل خط',
            vasactivation: 'خدمة VAS',
            complaint: 'شكوى',
        };
        return badgeT(`backOffice.dashboard.badges.category.${key}`, fallbacks[key]);
    };

    const ticketChannelLabel = (channel) => {
        const c = String(channel || '');
        const fallbacks = {
            Customer_Care_Voice_AI: 'ذكاء اصطناعي — كول سنتر',
            Self_Care_App: 'تطبيق العميل',
            Showroom_Agent: 'نقطة البيع',
            CallCenter_Agent: 'كول سنتر',
        };
        if (fallbacks[c]) return badgeT(`backOffice.dashboard.badges.channel.${c}`, fallbacks[c]);
        return c || '—';
    };

    const auditActionClass = (actionType) => {
        const t = String(actionType || '');
        if (t === 'NetworkCommandExecuted') return 'duotone-danger';
        if (t.startsWith('BulkImport')) return 'duotone-warning';
        if (t === 'CustomerViewed' || t === 'TicketResolved' || t === 'SubscriberSearched') {
            return 'duotone-info';
        }
        if (t === 'TelecomOperationConfirmed') return 'duotone-success';
        return 'duotone-neutral';
    };

    const render = (label, toneClass, showPulse = true) => {
        const pulse =
            showPulse && withPulse(toneClass)
                ? '<span class="pulse-dot" aria-hidden="true"></span>'
                : '';
        return `<span class="telecom-duotone-badge ${toneClass}">${pulse}${escapeHtml(label)}</span>`;
    };

    const renderIcon = (biIcon, label, toneClass, showPulse = true) => {
        const pulse =
            showPulse && withPulse(toneClass)
                ? '<span class="pulse-dot" aria-hidden="true"></span>'
                : '';
        const icon = biIcon ? `<i class="bi ${biIcon}" aria-hidden="true"></i>` : '';
        return `<span class="telecom-duotone-badge ${toneClass}">${pulse}${icon}<span class="telecom-duotone-badge__label">${escapeHtml(label)}</span></span>`;
    };

    const isCorporateSubscriberKind = (kind) => {
        const raw = kind;
        const s = String(raw ?? '').trim().toLowerCase();
        return s === 'corporate' || raw === 1 || raw === '1';
    };

    global.TelecomUiBadges = {
        poolStatus(status, label) {
            return render(label || status, poolStatusClass(status));
        },
        integrationHealth(mode, labelAr) {
            return render(labelAr || mode, integrationModeClass(mode));
        },
        responseStatus(code) {
            return render(code || '—', responseStatusClass(code), false);
        },
        resultSuccess(isSuccess) {
            return render(
                isSuccess
                    ? badgeT('backOffice.dashboard.badges.resultSuccess', 'نجاح')
                    : badgeT('backOffice.dashboard.badges.resultFailed', 'فشل'),
                isSuccess ? 'duotone-success' : 'duotone-danger'
            );
        },
        auditAction(actionType, label) {
            const cls = auditActionClass(actionType);
            const pulse =
                cls === 'duotone-danger' ||
                cls === 'duotone-warning' ||
                withPulse(cls);
            return render(label || actionType, cls, pulse);
        },
        ticketPriority(priority, label) {
            const cls = ticketPriorityClass(priority);
            const pulse = cls === 'duotone-danger' || cls === 'duotone-warning';
            return render(label || '—', cls, pulse);
        },
        ticketChannel(channel, labelOverride) {
            const cls = ticketChannelClass(channel);
            const label = labelOverride || ticketChannelLabel(channel);
            const pulse = cls === 'duotone-ai';
            return render(label, cls, pulse);
        },
        ticketCategory(category, labelOverride) {
            const cls = ticketCategoryClass(category);
            const label = labelOverride || ticketCategoryLabel(category);
            const pulse = cls === 'duotone-info' || cls === 'duotone-warning';
            return render(label, cls, pulse);
        },
        ticketStatus(status, labelOverride) {
            const cls = ticketStatusClass(status);
            const label = labelOverride || ticketStatusLabel(status);
            const pulse = Number(status) === 0;
            return render(label, cls, pulse);
        },
        /** B2C / B2B subscriber kind (Customer list Type column). */
        subscriberKind(kind, labelOverride) {
            const isCorporate = isCorporateSubscriberKind(kind);
            const label =
                labelOverride ??
                (isCorporate
                    ? badgeT('customerList.onboarding.corporate', 'Corporate')
                    : badgeT('customerList.onboarding.individual', 'Individual'));
            return isCorporate
                ? renderIcon('bi-building', label, 'duotone-warning', true)
                : renderIcon('bi-person-fill', label, 'duotone-info', false);
        },
    };
})(typeof window !== 'undefined' ? window : globalThis);
