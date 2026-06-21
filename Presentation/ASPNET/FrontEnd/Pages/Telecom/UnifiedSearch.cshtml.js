const US_FALLBACKS_EN = {
    kicker: 'Syria Telecom BSS',
    title: 'Smart unified search',
    callCenterTitle: 'Syriatel Customer Care Portal',
    subtitle: 'Unified subscriber search & active support desk',
    placeholder: 'Search by MSISDN (09x), national ID, or commercial registry…',
    search: 'Search',
    hintEnter: 'Press Enter — opens Customer 360 when one match is found',
    hubLink: 'Operations hub',
    modalTitle: 'Quick search results',
    close: 'Close',
    loading: 'Querying…',
    noResults: 'No matches for this identifier.',
    colSubscriber: 'Subscriber name',
    colId: 'Identifier',
    colMsisdn: 'MSISDN',
    colStatus: 'Status',
    openProfile: 'Open Customer 360 profile',
    myTicketsTitle: 'My tickets — open & escalated follow-up',
    myTicketsEmpty: 'No open or escalated tickets you opened — search for a subscriber above.',
    ticketsLoading: 'Loading your ticket queue…',
    refreshTickets: 'Refresh',
    colTicket: 'Ticket ID',
    colIssue: 'Issue type',
    colOpened: 'Opened',
    subscriberFallback: 'Postpaid subscriber — profile linked',
    ticketIssue0: 'Network',
    ticketIssue1: 'Billing',
    ticketIssue2: 'SIM block',
    ticketIssue3: 'Activation',
    ticketIssueOther: 'Other',
    ticketStatus0: 'Open',
    ticketStatus1: 'In progress',
    ticketStatus2: 'Resolved',
    ticketStatus3: 'Escalated (Tier-3)',
    ticketStatusOther: 'Unknown',
    pageTitle: 'Unified search — Customer 360',
    termTooShort: 'Enter a valid identifier (at least 2 characters).',
    nameNotAllowed: 'Name search is not allowed — use national ID, commercial registry, or MSISDN.',
    invalidFormat: 'Invalid format — use 10 digits (ID/09…) or a commercial registry number.',
    searchFailed: 'Search could not be completed.',
    noCustomerId: 'This row is not linked to a subscriber profile.',
};

const US_FALLBACKS_AR = {
    kicker: 'Syria Telecom BSS',
    title: 'البحث الموحّد الذكي',
    callCenterTitle: 'بوابة خدمة العملاء الموحدة | مركز الاتصال',
    subtitle: 'البحث السريع وعلاج تذاكر المشتركين — مصدر الحقيقة الواحد',
    placeholder: 'أدخل رقم المشترك (09x)، الرقم الوطني، أو السجل التجاري…',
    search: 'بحث',
    hintEnter: 'اضغط Enter — يفتح Customer 360 مباشرة عند وجود نتيجة واحدة',
    hubLink: 'مركز العمليات',
    modalTitle: 'نتائج البحث السريعة',
    close: 'إغلاق',
    loading: 'جاري الاستعلام…',
    noResults: 'لا توجد مطابقات لهذا المعرّف.',
    colSubscriber: 'اسم المشترك',
    colId: 'المعرّف',
    colMsisdn: 'MSISDN',
    colStatus: 'الحالة',
    openProfile: 'فتح ملف Customer 360',
    myTicketsTitle: 'طابور تذاكري — مفتوحة ومصعّدة للمتابعة',
    myTicketsEmpty: 'لا توجد تذاكر مفتوحة أو مصعّدة — ابحث عن مشترك أعلاه.',
    ticketsLoading: 'جاري تحميل طابور التذاكر…',
    refreshTickets: 'تحديث',
    colTicket: 'رقم التذكرة',
    colIssue: 'نوع الشكوى',
    colOpened: 'وقت الفتح',
    subscriberFallback: 'مشترك — باقة مفعّلة',
    ticketIssue0: 'شبكة',
    ticketIssue1: 'فوترة',
    ticketIssue2: 'حظر SIM',
    ticketIssue3: 'تفعيل',
    ticketIssueOther: 'أخرى',
    ticketStatus0: 'مفتوحة',
    ticketStatus1: 'قيد المعالجة',
    ticketStatus2: 'مغلقة',
    ticketStatus3: 'مصعّدة (Tier-3)',
    ticketStatusOther: 'غير معروف',
    pageTitle: 'البحث الموحّد — Customer 360',
    termTooShort: 'أدخل معرّفاً صالحاً (حرفان على الأقل).',
    nameNotAllowed: 'البحث بالاسم غير مسموح — استخدم الرقم الوطني أو MSISDN.',
    invalidFormat: 'صيغة غير معتمدة — استخدم 10 أرقام أو سجلاً تجارياً.',
    searchFailed: 'تعذّر تنفيذ البحث.',
    noCustomerId: 'هذا السجل غير مربوط بملف مشترك.',
};

function usFallbacks() {
    const lang = window.TelecomI18n?.getLang?.() || 'en';
    return lang === 'ar' ? US_FALLBACKS_AR : US_FALLBACKS_EN;
}

function usT(key) {
    const fullKey = `unifiedSearch.${key}`;
    const hit = window.TelecomI18n?.t?.(fullKey);
    if (hit) return hit;
    const fb = usFallbacks();
    return fb[key] || key;
}

const HUB_NAV_PERMS = [
    'telecom.hub.frontline',
    'telecom.hub.backoffice',
    'telecom.hub.supervisor',
    'telecom.line.activate',
    'telecom.asset.manage',
    'admin.settings.manage',
];

const MY_TICKETS_PERMS = ['telecom.ticket.escalate', 'customer.view'];

const SEARCH_TIMEOUT_MS = 20000;

function normalizeIndicDigits(term) {
    return String(term || '').replace(/[٠-٩۰-۹]/g, (ch) => {
        const cp = ch.codePointAt(0);
        if (cp >= 0x0660 && cp <= 0x0669) return String(cp - 0x0660);
        if (cp >= 0x06f0 && cp <= 0x06f9) return String(cp - 0x06f0);
        return ch;
    });
}

function digitsOnly(term) {
    return normalizeIndicDigits(term).replace(/\D/g, '');
}

function isNameOnlySearchTerm(term) {
    const t = (term || '').trim();
    if (t.length < 2) return false;
    const d = digitsOnly(t);
    if (d.length >= 2) return false;
    return /[\p{L}]/u.test(t);
}

function isValidIdentifierTerm(term) {
    const t = normalizeIndicDigits(term).trim();
    const d = digitsOnly(t);
    if (d.length === 10 && d.startsWith('09')) return true;
    if (d.length === 10 && !d.startsWith('09')) return true;
    if (t.length >= 3 && /[A-Za-z\u0600-\u06FF0-9]/.test(t)) return true;
    return false;
}

function pickCustomerId(row) {
    return row?.customerId || row?.CustomerId || '';
}

function displayName(row) {
    return row?.customerNameAr || row?.CustomerNameAr || row?.title || row?.Title || '—';
}

function isAbortError(e) {
    const code = e?.code || e?.name || '';
    return code === 'ERR_CANCELED' || code === 'CanceledError' || code === 'AbortError';
}

function hasAnyPerm(keys) {
    const perms = StorageManager.getPermissions?.() || [];
    return StorageManager.hasAnyPermission?.(perms, keys) || false;
}

function normalizeTicket(raw) {
    return {
        id: raw?.id ?? raw?.Id ?? '',
        ticketNumber: raw?.ticketNumber ?? raw?.TicketNumber ?? '',
        msisdn: raw?.msisdn ?? raw?.Msisdn ?? '',
        customerId: raw?.customerId ?? raw?.CustomerId ?? '',
        customerDisplayName: raw?.customerDisplayName ?? raw?.CustomerDisplayName ?? '',
        issueType: raw?.issueType ?? raw?.IssueType ?? 0,
        status: raw?.status ?? raw?.Status ?? 0,
        createdAtUtc: raw?.createdAtUtc ?? raw?.CreatedAtUtc ?? null,
        updatedAtUtc: raw?.updatedAtUtc ?? raw?.UpdatedAtUtc ?? null,
    };
}

const UnifiedSearchApp = {
    setup() {
        const localeTick = Vue.ref(0);

        const state = Vue.reactive({
            searchTerm: '',
            searchBusy: false,
            results: [],
            selectedCustomerId: '',
            inlineAlert: '',
            modalError: '',
            myTickets: [],
            ticketsBusy: false,
        });

        const ui = Vue.computed(() => {
            localeTick.value;
            const isCallCenterLanding = hasAnyPerm(MY_TICKETS_PERMS) && !hasAnyPerm(HUB_NAV_PERMS);
            return {
                kicker: usT('kicker'),
                title: isCallCenterLanding ? usT('callCenterTitle') : usT('title'),
                subtitle: usT('subtitle'),
                placeholder: usT('placeholder'),
                search: usT('search'),
                hintEnter: usT('hintEnter'),
                hubLink: usT('hubLink'),
                modalTitle: usT('modalTitle'),
                close: usT('close'),
                loading: usT('loading'),
                noResults: usT('noResults'),
                colSubscriber: usT('colSubscriber'),
                colId: usT('colId'),
                colMsisdn: usT('colMsisdn'),
                colStatus: usT('colStatus'),
                openProfile: usT('openProfile'),
                showHubLink: hasAnyPerm(HUB_NAV_PERMS),
                showMyTickets: hasAnyPerm(MY_TICKETS_PERMS),
                myTicketsTitle: usT('myTicketsTitle'),
                myTicketsEmpty: usT('myTicketsEmpty'),
                ticketsLoading: usT('ticketsLoading'),
                refreshTickets: usT('refreshTickets'),
                colTicket: usT('colTicket'),
                colIssue: usT('colIssue'),
                colOpened: usT('colOpened'),
                subscriberFallback: usT('subscriberFallback'),
            };
        });

        let searchModal = null;
        let searchAbort = null;
        let searchSeq = 0;

        const rowKey = (row) => `${pickCustomerId(row)}:${row?.msisdn || row?.Msisdn || row?.id || row?.Id}`;

        const showModal = () => {
            const el = document.getElementById('searchResultModal');
            if (!el || typeof bootstrap === 'undefined') return;
            searchModal = bootstrap.Modal.getOrCreateInstance(el);
            searchModal.show();
        };

        const goToProfile = (customerId) => {
            if (!customerId) return;
            window.location.href = '/Telecom/Customer360Profile?customerId=' + encodeURIComponent(customerId);
        };

        const selectRow = (row) => {
            const cid = pickCustomerId(row);
            if (!cid) {
                state.modalError = usT('noCustomerId');
                state.selectedCustomerId = '';
                return;
            }
            state.selectedCustomerId = cid;
            state.modalError = '';
        };

        const openProfileRow = (row) => {
            const cid = pickCustomerId(row);
            if (cid) goToProfile(cid);
        };

        const runSearch = async () => {
            state.inlineAlert = '';
            state.modalError = '';
            const term = (state.searchTerm || '').trim();
            if (term.length < 2) {
                state.inlineAlert = usT('termTooShort');
                state.results = [];
                return;
            }
            if (isNameOnlySearchTerm(term)) {
                state.inlineAlert = usT('nameNotAllowed');
                state.results = [];
                return;
            }
            if (!isValidIdentifierTerm(term)) {
                state.inlineAlert = usT('invalidFormat');
                state.results = [];
                return;
            }

            if (searchAbort) searchAbort.abort();
            searchAbort = new AbortController();
            const abortSignal = searchAbort.signal;
            const seq = ++searchSeq;

            state.searchBusy = true;
            state.results = [];
            state.selectedCustomerId = '';

            try {
                const q = '/Telecom/GetTelecomUniversalSearch?term=' + encodeURIComponent(term);
                const res = await AxiosManager.get(q, {
                    signal: abortSignal,
                    timeout: SEARCH_TIMEOUT_MS,
                });
                if (seq !== searchSeq || abortSignal.aborted) return;

                const rows =
                    typeof StorageManager !== 'undefined' && typeof StorageManager.apiList === 'function'
                        ? StorageManager.apiList(res)
                        : res?.data?.content?.data ?? res?.data?.content?.Data ?? [];
                state.results = Array.isArray(rows) ? rows : [];

                if (state.results.length === 1) {
                    const cid = pickCustomerId(state.results[0]);
                    if (cid) {
                        goToProfile(cid);
                        return;
                    }
                    selectRow(state.results[0]);
                }

                if (state.results.length > 1) {
                    showModal();
                }
            } catch (e) {
                if (isAbortError(e) || abortSignal.aborted) return;
                state.results = [];
                const msg = e?.response?.data?.message || usT('searchFailed');
                state.modalError = msg;
                state.inlineAlert = msg;
                showModal();
            } finally {
                if (seq === searchSeq) {
                    state.searchBusy = false;
                }
            }
        };

        const redirectToFullProfile = () => {
            if (!state.selectedCustomerId) return;
            goToProfile(state.selectedCustomerId);
        };

        const loadMyTickets = async () => {
            if (!hasAnyPerm(MY_TICKETS_PERMS)) return;
            state.ticketsBusy = true;
            try {
                const res = await AxiosManager.get('/TelecomBackOffice/GetTechnicalTickets', {
                    params: { openedByCurrentUserOnly: true, activeQueueOnly: true },
                });
                const rows = StorageManager.apiList(res).map(normalizeTicket);
                rows.sort((a, b) => {
                    const sa = Number(a.status);
                    const sb = Number(b.status);
                    if (sa === 3 && sb !== 3) return -1;
                    if (sb === 3 && sa !== 3) return 1;
                    const ta = new Date(a.updatedAtUtc || a.createdAtUtc || 0).getTime();
                    const tb = new Date(b.updatedAtUtc || b.createdAtUtc || 0).getTime();
                    return tb - ta;
                });
                state.myTickets = rows.slice(0, 12);
            } catch (e) {
                console.error('loadMyTickets failed', e);
                state.myTickets = [];
            } finally {
                state.ticketsBusy = false;
            }
        };

        const ticketIssueLabel = (issueType) => {
            const n = Number(issueType);
            return usT(`ticketIssue${n}`) !== `ticketIssue${n}`
                ? usT(`ticketIssue${n}`)
                : usT('ticketIssueOther');
        };

        const ticketStatusLabel = (status) => {
            const n = Number(status);
            return usT(`ticketStatus${n}`) !== `ticketStatus${n}`
                ? usT(`ticketStatus${n}`)
                : usT('ticketStatusOther');
        };

        const ticketStatusBadge = (status) => {
            const s = Number(status);
            if (s === 1) return 'bg-primary';
            if (s === 2) return 'bg-success';
            if (s === 3) return 'bg-dark';
            return 'bg-warning text-dark';
        };

        const formatTicketDate = (value) => {
            if (!value) return '—';
            try {
                const loc = window.TelecomI18n?.getLang?.() === 'en' ? 'en-US' : 'ar-SY';
                return new Intl.DateTimeFormat(loc, {
                    dateStyle: 'short',
                    timeStyle: 'short',
                }).format(new Date(value));
            } catch {
                return String(value);
            }
        };

        const displaySubscriber = (ticket) => {
            const name = (ticket.customerDisplayName || '').trim();
            if (name) return name;
            return ui.value.subscriberFallback;
        };

        const openTicketCustomer = async (ticket) => {
            if (ticket.customerId) {
                goToProfile(ticket.customerId);
                return;
            }
            if (ticket.msisdn) {
                state.searchTerm = ticket.msisdn;
                await runSearch();
            }
        };

        const readQueryPrefill = () => {
            const params = new URLSearchParams(window.location.search);
            const q = params.get('q') || params.get('term') || '';
            if (q) {
                state.searchTerm = q;
                Vue.nextTick(() => runSearch());
            }
        };

        const applyLocaleUi = () => {
            localeTick.value += 1;
            document.title = usT('pageTitle');
        };

        Vue.onMounted(async () => {
            await window.TelecomI18n?.ensureLoaded?.();
            applyLocaleUi();
            document.documentElement.addEventListener('syriatel-locale-changed', applyLocaleUi);
            readQueryPrefill();
            await loadMyTickets();
            if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
        });

        Vue.onUnmounted(() => {
            if (searchAbort) searchAbort.abort();
            document.documentElement.removeEventListener('syriatel-locale-changed', applyLocaleUi);
        });

        return {
            state,
            ui,
            runSearch,
            selectRow,
            openProfileRow,
            rowKey,
            displayName,
            displaySubscriber,
            pickCustomerId,
            redirectToFullProfile,
            loadMyTickets,
            ticketIssueLabel,
            ticketStatusLabel,
            ticketStatusBadge,
            formatTicketDate,
            openTicketCustomer,
            usT,
        };
    },
};

(async function bootUnifiedSearch() {
    try {
        await window.TelecomI18n?.ensureLoaded?.();
    } catch (e) {
        console.warn('TelecomI18n preload failed', e);
    }
    Vue.createApp(UnifiedSearchApp).mount('#app');
})();
