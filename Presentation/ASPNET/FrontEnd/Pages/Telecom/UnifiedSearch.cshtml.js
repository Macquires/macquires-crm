const usT = (key) => window.TelecomI18n?.t?.(`unifiedSearch.${key}`) || key;

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

const UnifiedSearchApp = {
    setup() {
        const state = Vue.reactive({
            searchTerm: '',
            searchBusy: false,
            results: [],
            selectedCustomerId: '',
            inlineAlert: '',
            modalError: '',
            searchPlaceholder: '',
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

            if (searchAbort) {
                searchAbort.abort();
            }
            searchAbort = new AbortController();
            const abortSignal = searchAbort.signal;
            const seq = ++searchSeq;

            state.searchBusy = true;
            state.results = [];
            state.selectedCustomerId = '';
            showModal();

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
                        window.location.href =
                            '/Telecom/Customer360Profile?customerId=' + encodeURIComponent(cid);
                        return;
                    }
                    selectRow(state.results[0]);
                }
            } catch (e) {
                if (isAbortError(e) || abortSignal.aborted) return;
                state.results = [];
                const msg = e?.response?.data?.message || usT('searchFailed');
                state.modalError = msg;
                state.inlineAlert = msg;
            } finally {
                if (seq === searchSeq) {
                    state.searchBusy = false;
                }
            }
        };

        const redirectToFullProfile = () => {
            if (!state.selectedCustomerId) return;
            window.location.href =
                '/Telecom/Customer360Profile?customerId=' + encodeURIComponent(state.selectedCustomerId);
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
            state.searchPlaceholder = usT('placeholder');
            const title = usT('pageTitle');
            if (title) document.title = title;
            window.TelecomI18n?.applyDom?.();
        };

        Vue.onMounted(async () => {
            await window.TelecomI18n?.ensureLoaded?.();
            applyLocaleUi();
            document.documentElement.addEventListener('syriatel-locale-changed', applyLocaleUi);
            readQueryPrefill();
            if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
        });

        Vue.onUnmounted(() => {
            if (searchAbort) searchAbort.abort();
            document.documentElement.removeEventListener('syriatel-locale-changed', applyLocaleUi);
        });

        return {
            state,
            runSearch,
            selectRow,
            rowKey,
            displayName,
            pickCustomerId,
            redirectToFullProfile,
            usT,
        };
    },
};

Vue.createApp(UnifiedSearchApp).mount('#app');
