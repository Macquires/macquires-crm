/**
 * Premium audit record detail modal (SweetAlert2).
 * @global AuditDetailModal
 */
const AuditDetailModal = (function () {
    function isEn() {
        return (document.documentElement?.lang || '').toLowerCase().startsWith('en');
    }

    function pickBi(ar, en) {
        return isEn() ? en : ar;
    }

    const DEFAULT_LABELS_AR = {
        UserLoggedIn: 'تسجيل دخول',
        UserLoginFailed: 'فشل دخول',
        UserLoggedOut: 'تسجيل خروج',
        UserCreated: 'إنشاء مستخدم',
        UserUpdated: 'تحديث مستخدم',
        UserRolesUpdated: 'تحديث أدوار',
        RolePermissionsUpdated: 'صلاحيات دور',
        RolePermissionsCloned: 'نسخ دور',
        GlobalSettingsUpdated: 'إعدادات عامة',
        IntegrationCircuitBreakerChanged: 'قاطع تكامل',
        CustomerCreated: 'إنشاء مشترك',
        CustomerUpdated: 'تحديث مشترك',
        TelecomOperationConfirmed: 'تأكيد عملية BSS',
        SubscriberSearched: 'بحث عن مشترك',
        CustomerViewed: 'اطلاع على ملف مشترك',
        NetworkCommandExecuted: 'أمر شبكة / VAS',
        BulkImportStarted: 'بدء استيراد',
        BulkImportExecuted: 'تنفيذ استيراد',
        TicketResolved: 'إغلاق تذكرة',
    };

    const DEFAULT_LABELS_EN = {
        UserLoggedIn: 'User login',
        UserLoginFailed: 'Login failed',
        UserLoggedOut: 'User logout',
        UserCreated: 'User created',
        UserUpdated: 'User updated',
        UserRolesUpdated: 'Roles updated',
        RolePermissionsUpdated: 'Role permissions updated',
        RolePermissionsCloned: 'Role cloned',
        GlobalSettingsUpdated: 'Global settings updated',
        IntegrationCircuitBreakerChanged: 'Integration circuit breaker',
        CustomerCreated: 'Customer created',
        CustomerUpdated: 'Customer updated',
        TelecomOperationConfirmed: 'BSS operation confirmed',
        SubscriberSearched: 'Subscriber search',
        CustomerViewed: 'Customer profile viewed',
        NetworkCommandExecuted: 'Network / VAS command',
        BulkImportStarted: 'Import started',
        BulkImportExecuted: 'Import executed',
        TicketResolved: 'Ticket closed',
    };

    function resolveDefaultLabels() {
        const src = isEn() ? DEFAULT_LABELS_EN : DEFAULT_LABELS_AR;
        return { ...src };
    }

    const DEFAULT_LABELS = DEFAULT_LABELS_AR;

    const PAYLOAD_FIELD_LABELS_AR = {
        narrativeAr: 'الوصف',
        channel: 'الشاشة',
        channelLabelAr: 'الشاشة',
        criteria: 'معيار البحث',
        matchCount: 'عدد النتائج',
        customerId: 'معرّف المشترك',
        displayName: 'اسم المشترك',
        primaryPhoneOrMsisdn: 'الخط / الجوال',
        msisdn: 'MSISDN',
        MSISDN: 'MSISDN',
        profileId: 'معرّف الملف (تقني)',
        profileDisplay: 'ملف المشترك',
        subscriberDisplay: 'ملف المشترك',
        technicalTicketId: 'معرّف التذكرة (تقني)',
        nameAr: 'الاسم',
        matches: 'النتائج',
        actorUserId: 'معرّف المنفّذ',
        ticketNumber: 'رقم التذكرة',
        integrationTarget: 'هدف التكامل',
        command: 'الأمر',
        success: 'النتيجة',
    };

    const PAYLOAD_FIELD_LABELS_EN = {
        narrativeAr: 'Description',
        channel: 'Screen',
        channelLabelAr: 'Screen',
        criteria: 'Search criteria',
        matchCount: 'Match count',
        customerId: 'Customer ID',
        displayName: 'Customer name',
        primaryPhoneOrMsisdn: 'Line / mobile',
        msisdn: 'MSISDN',
        MSISDN: 'MSISDN',
        profileId: 'Profile ID (technical)',
        profileDisplay: 'Subscriber profile',
        subscriberDisplay: 'Subscriber profile',
        technicalTicketId: 'Ticket ID (technical)',
        nameAr: 'Name',
        matches: 'Results',
        actorUserId: 'Actor ID',
        ticketNumber: 'Ticket number',
        integrationTarget: 'Integration target',
        command: 'Command',
        success: 'Result',
    };

    function payloadFieldLabel(key) {
        const map = isEn() ? PAYLOAD_FIELD_LABELS_EN : PAYLOAD_FIELD_LABELS_AR;
        return map[key] || key;
    }

    const PAYLOAD_FIELD_LABELS = PAYLOAD_FIELD_LABELS_AR;

    const COPYABLE_KEYS = new Set([
        'msisdn', 'MSISDN', 'profileId', 'customerId', 'technicalTicketId',
        'ticketNumber', 'actorUserId', 'entityId', 'correlationId',
    ]);

    /** Prefer human labels; hide raw GUID keys when a display field exists. */
    const FRIENDLY_RESOLVERS = [
        {
            displayKeys: ['profileDisplay', 'subscriberDisplay', 'subscriberName'],
            rawKey: 'profileId',
            labelAr: 'ملف المشترك',
            labelEn: 'Subscriber profile',
        },
        {
            displayKeys: ['ticketNumber', 'ticketDisplay'],
            rawKey: 'technicalTicketId',
            labelAr: 'التذكرة الفنية',
            labelEn: 'Technical ticket',
        },
        {
            displayKeys: ['displayName', 'customerDisplayName', 'nameAr', 'nameEn', 'NameEn'],
            rawKey: 'customerId',
            labelAr: 'المشترك',
            labelEn: 'Customer',
        },
    ];

    const PAYLOAD_DISPLAY_ORDER = [
        'msisdn', 'MSISDN', 'profileDisplay', 'ticketNumber', 'displayName', 'customerDisplayName',
        'serviceCode', 'activate', 'provision', 'executedCommand', 'operationNumber', 'command', 'success',
    ];

    const SKIP_PAYLOAD_KEYS = new Set(['narrativeAr', 'NarrativeAr', 'result']);

    const escapeHtml = (s) =>
        String(s ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');

    const pick = (obj, ...keys) => {
        if (!obj || typeof obj !== 'object') return undefined;
        for (const k of keys) {
            const v = obj[k];
            if (v != null && String(v).trim() !== '') return v;
        }
        return undefined;
    };

    const formatDt = (utc) => {
        if (!utc) return '—';
        try {
            const loc = isEn() ? 'en-US' : 'ar-SY';
            return new Date(utc).toLocaleString(loc, { dateStyle: 'medium', timeStyle: 'medium' });
        } catch {
            return String(utc);
        }
    };

    const actionIconClass = (actionType) => {
        if (actionType === 'NetworkCommandExecuted') return { icon: 'bi-lightning-charge-fill', cls: 'audit-detail-icon--network' };
        if (actionType === 'SubscriberSearched' || actionType === 'CustomerViewed') return { icon: 'bi-search', cls: 'audit-detail-icon--search' };
        if (/User|Role|Login|Permission/i.test(actionType || '')) return { icon: 'bi-shield-lock-fill', cls: 'audit-detail-icon--security' };
        return { icon: 'bi-journal-text', cls: 'audit-detail-icon--default' };
    };

    const bindCopyButtons = (root) => {
        if (!root) return;
        root.querySelectorAll('.audit-kv-copy').forEach((btn) => {
            btn.addEventListener('click', async () => {
                const text = btn.getAttribute('data-copy') || '';
                try {
                    await navigator.clipboard.writeText(text);
                    btn.classList.add('copied');
                    btn.innerHTML = '<i class="bi bi-check2"></i>';
                    setTimeout(() => {
                        btn.classList.remove('copied');
                        btn.innerHTML = '<i class="bi bi-clipboard"></i>';
                    }, 1600);
                } catch {
                    /* ignore */
                }
            });
        });
    };

    const kvRow = (label, value, copyable, friendly) => {
        const v = value == null || value === '' ? '—' : String(value);
        const copyTitle = pickBi('نسخ', 'Copy');
        const copyBtn =
            copyable && v !== '—'
                ? `<button type="button" class="audit-kv-copy" data-copy="${escapeHtml(v)}" title="${copyTitle}"><i class="bi bi-clipboard"></i></button>`
                : '<span></span>';
        const valueClass = friendly ? 'audit-kv-value audit-kv-value--friendly' : 'audit-kv-value';
        return `
            <div class="audit-kv-row">
                <span class="audit-kv-label">${escapeHtml(label)}</span>
                <span class="${valueClass}">${escapeHtml(v)}</span>
                ${copyBtn}
            </div>`;
    };

    const kvRowWithTechnicalId = (label, displayValue, technicalId) => {
        const display = displayValue == null || displayValue === '' ? null : String(displayValue);
        const raw = technicalId == null || technicalId === '' ? null : String(technicalId);
        if (!display && !raw) return kvRow(label, '—', false, true);
        if (!display) return kvRow(label, raw, true, false);
        const sub = raw
            ? `<div class="audit-kv-sub" title="${pickBi('المعرّف التقني', 'Technical ID')}">${escapeHtml(raw)}</div>`
            : '';
        const copyBtn = raw
            ? `<button type="button" class="audit-kv-copy" data-copy="${escapeHtml(raw)}" title="${pickBi('نسخ المعرّف', 'Copy ID')}"><i class="bi bi-clipboard"></i></button>`
            : '<span></span>';
        return `
            <div class="audit-kv-row">
                <span class="audit-kv-label">${escapeHtml(label)}</span>
                <span class="audit-kv-value audit-kv-value--friendly">${escapeHtml(display)}${sub}</span>
                ${copyBtn}
            </div>`;
    };

    const parsePayload = (row) => {
        if (!row?.payloadJson) return null;
        try {
            return JSON.parse(row.payloadJson);
        } catch {
            return null;
        }
    };

    const enrichPayload = async (payload) => {
        if (!payload || typeof payload !== 'object') return payload;
        const needsProfile = payload.profileId && !pick(payload, 'profileDisplay', 'subscriberDisplay');
        const needsTicket = payload.technicalTicketId && !pick(payload, 'ticketNumber', 'ticketDisplay');
        const needsCustomer = payload.customerId && !pick(payload, 'displayName', 'customerDisplayName');
        if (!needsProfile && !needsTicket && !needsCustomer) return payload;

        try {
            const resolveUrl =
                typeof window !== 'undefined' && window.location.pathname.includes('/Administration/')
                    ? '/Security/ResolveAuditDisplayNames'
                    : '/TelecomBackOffice/ResolveAuditDisplayNames';
            const res = await AxiosManager.get(resolveUrl, {
                params: {
                    profileId: payload.profileId || undefined,
                    technicalTicketId: payload.technicalTicketId || undefined,
                    customerId: payload.customerId || undefined,
                },
            });
            const c = res?.data?.content ?? {};
            return {
                ...payload,
                profileDisplay: pick(payload, 'profileDisplay', 'subscriberDisplay') || c.profileDisplay,
                ticketNumber: pick(payload, 'ticketNumber', 'ticketDisplay') || c.ticketNumber,
                customerDisplayName:
                    pick(payload, 'displayName', 'customerDisplayName') || c.customerDisplayName,
            };
        } catch {
            return payload;
        }
    };

    const buildStructuredPayloadRows = (payload) => {
        const hiddenRaw = new Set();
        const rows = [];

        for (const rule of FRIENDLY_RESOLVERS) {
            const display = pick(payload, ...rule.displayKeys);
            const raw = payload[rule.rawKey];
            const label = pickBi(rule.labelAr, rule.labelEn);
            if (display) {
                hiddenRaw.add(rule.rawKey);
                rows.push(kvRowWithTechnicalId(label, display, raw));
            } else if (raw) {
                rows.push(kvRowWithTechnicalId(label, null, raw));
            }
        }

        const usedKeys = new Set([...hiddenRaw]);
        FRIENDLY_RESOLVERS.forEach((r) => {
            r.displayKeys.forEach((k) => usedKeys.add(k));
            usedKeys.add(r.rawKey);
        });

        const remaining = Object.entries(payload).filter(([k, v]) => {
            if (SKIP_PAYLOAD_KEYS.has(k)) return false;
            if (usedKeys.has(k)) return false;
            if (hiddenRaw.has(k)) return false;
            if (v == null || typeof v === 'object') return false;
            return true;
        });

        remaining.sort((a, b) => {
            const ia = PAYLOAD_DISPLAY_ORDER.indexOf(a[0]);
            const ib = PAYLOAD_DISPLAY_ORDER.indexOf(b[0]);
            return (ia === -1 ? 999 : ia) - (ib === -1 ? 999 : ib) || a[0].localeCompare(b[0]);
        });

        for (const [k, v] of remaining) {
            const label = payloadFieldLabel(k);
            const friendly = !/id$/i.test(k) && k !== 'msisdn' && k !== 'MSISDN';
            const copyable = COPYABLE_KEYS.has(k) || /id$/i.test(k) || k === 'msisdn';
            rows.push(kvRow(label, v, copyable, friendly));
        }

        return rows.join('');
    };

    const formatMatchLine = (m) => {
        const display = pick(m, 'displayLine', 'DisplayLine');
        if (display) return String(display);
        const name = pick(m, isEn() ? 'nameEn' : 'nameAr', 'NameEn', 'NameAr', 'name', 'Name');
        const phone = pick(m, 'msisdn', 'Msisdn', 'phoneOrMsisdn', 'PhoneOrMsisdn');
        if (name && phone) return `${name} — ${phone}`;
        if (name) return String(name);
        if (phone) return String(phone);
        const cid = pick(m, 'customerId', 'CustomerId');
        return cid ? String(cid) : '—';
    };

    const buildEventDetails = (payload, row) => {
        const narrative = pick(
            payload,
            isEn() ? 'narrativeEn' : 'narrativeAr',
            'narrativeAr',
            'NarrativeAr',
            'NarrativeEn'
        );
        const matches = payload?.matches ?? payload?.Matches;
        const criteria = pick(payload, 'criteria', 'Criteria');
        const channelLabel = pick(
            payload,
            isEn() ? 'channelLabelEn' : 'channelLabelAr',
            'channelLabelAr',
            'ChannelLabelAr',
            'channel',
            'Channel'
        );
        const matchCount = payload?.matchCount ?? payload?.MatchCount;

        if (narrative) {
            const lines = String(narrative)
                .split('\n')
                .filter((l) => l.trim())
                .map((line) => `<div class="audit-detail-line">${escapeHtml(line)}</div>`)
                .join('');
            return `<div class="audit-detail-narrative border rounded">${lines}</div>`;
        }

        if (matches && Array.isArray(matches) && matches.length) {
            const header =
                criteria || channelLabel
                    ? `<p class="small text-muted mb-2">${escapeHtml([channelLabel, criteria].filter(Boolean).join(' · '))}</p>`
                    : '';
            const items = matches
                .map((m, i) => `<li><strong>${i + 1}.</strong> ${escapeHtml(formatMatchLine(m))}</li>`)
                .join('');
            return `${header}<ul class="audit-match-list">${items}</ul>`;
        }

        if (payload && typeof payload === 'object') {
            const rows = buildStructuredPayloadRows(payload);
            if (rows) {
                return `<div class="audit-kv-list">${rows}</div>`;
            }
        }

        if (row.payloadJson) {
            try {
                const pretty = JSON.stringify(JSON.parse(row.payloadJson), null, 2);
                return `<pre class="audit-json-fallback mb-0">${escapeHtml(pretty)}</pre>`;
            } catch {
                return `<p class="audit-empty-hint">${pickBi('لا توجد تفاصيل قابلة للعرض.', 'No displayable details.')}</p>`;
            }
        }

        return `<p class="audit-empty-hint">${pickBi('لا توجد تفاصيل إضافية لهذا السجل.', 'No additional details for this record.')}</p>`;
    };

    const buildHtml = (row, labels, payloadOverride) => {
        const payload = payloadOverride ?? parsePayload(row);

        const actionLabel = labels[row.actionType] || row.actionType || '—';
        const { icon, cls } = actionIconClass(row.actionType);
        const summary = (isEn() ? row.summaryEn : row.summaryAr) || row.summaryAr || actionLabel;
        const timeStr = formatDt(row.occurredAtUtc);
        const dir = isEn() ? 'ltr' : 'rtl';

        const metaCards = [
            { icon: 'bi-person-badge', label: pickBi('المنفّذ', 'Actor'), value: row.actorDisplayName || '—' },
            { icon: 'bi-diagram-3', label: pickBi('العملية', 'Action'), value: actionLabel },
            { icon: 'bi-box', label: pickBi('الكيان', 'Entity'), value: row.entityType || '—' },
            { icon: 'bi-bullseye', label: pickBi('الهدف', 'Target'), value: row.targetDisplayName || '—' },
            { icon: 'bi-globe2', label: pickBi('عنوان IP', 'IP address'), value: row.ipAddress || '—', mono: true },
            { icon: 'bi-clock-history', label: pickBi('الوقت', 'Time'), value: timeStr, mono: true },
        ];

        const metaHtml = metaCards
            .map(
                (c) => `
            <div class="audit-meta-card">
                <div class="audit-meta-card-label"><i class="bi ${c.icon}"></i>${escapeHtml(c.label)}</div>
                <div class="audit-meta-card-value${c.mono ? ' mono' : ''}">${escapeHtml(c.value)}</div>
            </div>`
            )
            .join('');

        const eventHtml = buildEventDetails(payload, row);

        return `
            <div class="audit-detail-premium" dir="${dir}">
                <header class="audit-detail-hero">
                    <div class="audit-detail-hero-top">
                        <div>
                            <span class="audit-detail-badge">${escapeHtml(actionLabel)}</span>
                            <p class="audit-detail-summary">${escapeHtml(summary)}</p>
                            <span class="audit-detail-time"><i class="bi bi-clock me-1"></i>${escapeHtml(timeStr)}</span>
                        </div>
                        <div class="audit-detail-icon ${cls}" aria-hidden="true"><i class="bi ${icon}"></i></div>
                    </div>
                </header>
                <div class="audit-detail-body">
                    <div class="audit-meta-grid">${metaHtml}</div>
                    <section class="audit-event-section">
                        <div class="audit-event-section-title"><i class="bi bi-braces"></i> ${pickBi('تفاصيل الحدث', 'Event details')}</div>
                        ${eventHtml}
                    </section>
                </div>
            </div>`;
    };

    async function show(row, options) {
        if (!row || typeof Swal === 'undefined') return;
        const labels = { ...resolveDefaultLabels(), ...(options?.actionLabels || {}) };

        Swal.fire({
            title: pickBi('تفاصيل السجل', 'Audit record details'),
            html: `<div class="text-center py-4"><span class="spinner-border text-danger"></span><p class="small text-muted mt-2 mb-0">${pickBi('جاري تحميل التفاصيل…', 'Loading details…')}</p></div>`,
            width: 720,
            padding: 0,
            showConfirmButton: false,
            showCloseButton: true,
            allowOutsideClick: false,
            customClass: { popup: 'audit-swal-popup' },
        });

        let payload = parsePayload(row);
        payload = await enrichPayload(payload);
        const html = buildHtml(row, labels, payload);

        Swal.update({
            html,
            showConfirmButton: true,
            confirmButtonText: pickBi('إغلاق', 'Close'),
            customClass: {
                popup: 'audit-swal-popup',
                htmlContainer: 'audit-swal-html',
            },
        });

        const container = Swal.getHtmlContainer();
        bindCopyButtons(container);
    }

    return { show, buildHtml, formatDt, DEFAULT_LABELS };
})();

if (typeof window !== 'undefined') {
    window.AuditDetailModal = AuditDetailModal;
}
