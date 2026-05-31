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

    const ticketStatusLabel = (status) => {
        const labels = {
            0: 'مفتوحة',
            1: 'قيد المعالجة',
            2: 'تم الحل',
            3: 'مصعّدة',
        };
        return labels[Number(status)] || '—';
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
        if (c === 'simswap' || n === 1) return '🌐 تبديل شريحة';
        if (c === 'packagemigration' || n === 2) return '📦 ترحيل باقة';
        if (c === 'ownershiptransfer' || n === 3) return '👤 نقل ملكية';
        if (c === 'lineactivation' || n === 4) return '📡 تفعيل خط';
        if (c === 'vasactivation' || n === 5) return '➕ خدمة VAS';
        if (c === 'complaint' || n === 0) return '📞 شكوى';
        return '—';
    };

    const ticketChannelLabel = (channel) => {
        const c = String(channel || '');
        if (c === 'Customer_Care_Voice_AI') return 'ذكاء اصطناعي — كول سنتر';
        if (c === 'Self_Care_App') return 'تطبيق العميل';
        if (c === 'Showroom_Agent') return 'معرض';
        if (c === 'CallCenter_Agent') return 'كول سنتر';
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
                isSuccess ? 'نجاح' : 'فشل',
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
    };
})(typeof window !== 'undefined' ? window : globalThis);
