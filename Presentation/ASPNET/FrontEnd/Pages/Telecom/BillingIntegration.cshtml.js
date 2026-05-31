const BILLING_LOG_VIEWER_ROLES = new Set(['TelecomAdmin', 'TelecomBackOffice', 'TelecomManagement']);

function formatDateAr(d) {
    if (!d) return '—';
    try {
        const dt = new Date(d);
        if (Number.isNaN(dt.getTime())) return '—';
        return new Intl.DateTimeFormat('ar-SY', {
            dateStyle: 'short',
            timeStyle: 'short',
        }).format(dt);
    } catch {
        return '—';
    }
}

function buildPageNumbers(current, total) {
    if (total <= 1) return [];
    const windowSize = 5;
    let start = Math.max(1, current - Math.floor(windowSize / 2));
    let end = Math.min(total, start + windowSize - 1);
    start = Math.max(1, end - windowSize + 1);
    const pages = [];
    for (let p = start; p <= end; p += 1) {
        pages.push(p);
    }
    return pages;
}

const BillingIntegrationApp = {
    setup() {
        const roles = StorageManager.getUserRoles() || [];
        const canAccess = roles.some((r) => BILLING_LOG_VIEWER_ROLES.has(r));

        const state = Vue.reactive({
            loading: false,
            rows: [],
            totalCount: 0,
            page: 1,
            pageSize: 25,
            totalPages: 1,
            pageNumbers: [],
            filters: {
                operationNumber: '',
                success: '',
            },
        });

        const loadPage = async () => {
            if (!canAccess) return;
            state.loading = true;
            try {
                const skip = (state.page - 1) * state.pageSize;
                const params = {
                    skip,
                    take: state.pageSize,
                };
                const opNum = (state.filters.operationNumber || '').trim();
                if (opNum) params.operationNumber = opNum;
                if (state.filters.success === 'true') params.success = true;
                if (state.filters.success === 'false') params.success = false;

                const res = await AxiosManager.get('/Telecom/GetBillingIntegrationLogList', { params });
                const content = res?.data?.content;
                state.rows = content?.data ?? [];
                state.totalCount = content?.totalCount ?? state.rows.length;
                state.totalPages = Math.max(1, Math.ceil(state.totalCount / state.pageSize));
                if (state.page > state.totalPages) {
                    state.page = state.totalPages;
                }
                state.pageNumbers = buildPageNumbers(state.page, state.totalPages);
            } catch (e) {
                console.error(e);
                state.rows = [];
                state.totalCount = 0;
                state.totalPages = 1;
                state.pageNumbers = [];
            } finally {
                state.loading = false;
            }
        };

        const handler = {
            search: async () => {
                state.page = 1;
                await loadPage();
            },
            refresh: async () => {
                await loadPage();
            },
            goToPage: async (page) => {
                const p = Number(page);
                if (!p || p < 1 || p > state.totalPages || p === state.page || state.loading) return;
                state.page = p;
                await loadPage();
            },
        };

        Vue.onMounted(async () => {
            if (typeof hideSpinnerAndShowContent === 'function') {
                hideSpinnerAndShowContent();
            }
            if (canAccess) {
                await loadPage();
            }
        });

        return {
            state,
            canAccess,
            handler,
            formatDate: formatDateAr,
        };
    },
};

if (typeof Vue !== 'undefined') {
    Vue.createApp(BillingIntegrationApp).mount('#app');
} else {
    console.error('Vue failed to load');
}
