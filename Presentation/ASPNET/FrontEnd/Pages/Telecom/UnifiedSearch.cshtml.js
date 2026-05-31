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

const UnifiedSearchApp = {
    setup() {
        const state = Vue.reactive({
            searchTerm: '',
            searchBusy: false,
            results: [],
            selectedCustomerId: '',
            inlineAlert: '',
        });

        let searchModal = null;

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
                state.inlineAlert = 'هذا السجل غير مربوط بملف مشترك — اختر نتيجة تحتوي CustomerId.';
                state.selectedCustomerId = '';
                return;
            }
            state.selectedCustomerId = cid;
            state.inlineAlert = '';
        };

        const runSearch = async () => {
            state.inlineAlert = '';
            const term = (state.searchTerm || '').trim();
            if (term.length < 2) {
                state.inlineAlert = 'أدخل معرّفاً صالحاً (حرفان على الأقل).';
                state.results = [];
                return;
            }
            if (isNameOnlySearchTerm(term)) {
                state.inlineAlert = 'البحث بالاسم غير مسموح — استخدم الرقم الوطني أو السجل التجاري أو MSISDN.';
                state.results = [];
                return;
            }
            if (!isValidIdentifierTerm(term)) {
                state.inlineAlert = 'صيغة غير معتمدة — استخدم 10 أرقام (هوية/09…) أو سجلاً تجارياً.';
                state.results = [];
                return;
            }

            state.searchBusy = true;
            state.selectedCustomerId = '';
            try {
                const q = '/Telecom/GetTelecomUniversalSearch?term=' + encodeURIComponent(term);
                const res = await AxiosManager.get(q, {});
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
                showModal();
            } catch (e) {
                state.results = [];
                state.inlineAlert = e?.response?.data?.message || 'تعذّر تنفيذ البحث.';
            } finally {
                state.searchBusy = false;
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

        Vue.onMounted(async () => {
            try {
                if (!StorageManager.getAccessToken?.()) {
                    window.location.href = '/Accounts/Login';
                    return;
                }
                readQueryPrefill();
                document.getElementById('unifiedSearchInput')?.focus();
            } catch (e) {
                console.error('UnifiedSearch init:', e);
            } finally {
                if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
            }
        });

        return {
            state,
            runSearch,
            selectRow,
            redirectToFullProfile,
            rowKey,
            pickCustomerId,
            displayName,
        };
    },
};

Vue.createApp(UnifiedSearchApp).mount('#app');
