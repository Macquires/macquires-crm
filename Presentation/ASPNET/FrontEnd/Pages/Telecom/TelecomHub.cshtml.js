const TELECOM_ROLE_NAMES = new Set(['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice', 'TelecomCallCenter', 'TelecomManagement']);
const SUBSCRIBER_REGISTRY_ROLES = new Set([
    'Customers',
    'TelecomAdmin',
    'TelecomShowroom',
    'TelecomBackOffice',
    'TelecomCallCenter',
    'TelecomManagement',
]);

/** Best-effort message from Axios API error payloads (e.g. validation / exception handler). */
function pickHttpErrorMessage(e) {
    const d = e?.response?.data;
    if (!d) return e?.message ? String(e.message) : '';
    const inner = d.error?.innerException;
    let m = d.message || (typeof inner === 'string' ? inner : '') || '';
    m = String(m);
    if (m.startsWith('Exception: ')) return m.slice(11).trim();
    return m;
}

function formatMoneyIntl(value, locale) {
    if (value == null || isNaN(value)) return '—';
    const isAr = locale === 'ar';
    const loc = isAr ? 'ar-SY' : 'en-US';
    const opts = { maximumFractionDigits: 0 };
    if (isAr) {
        try {
            return new Intl.NumberFormat(loc, { ...opts, numberingSystem: 'arab' }).format(value);
        } catch {
            return new Intl.NumberFormat(loc, opts).format(value);
        }
    }
    return new Intl.NumberFormat(loc, opts).format(value);
}

function formatDateIntl(d, locale) {
    if (!d) return '';
    const dt = new Date(d);
    if (isNaN(dt.getTime())) return '';
    const isAr = locale === 'ar';
    const loc = isAr ? 'ar-SY' : 'en-US';
    const opts = isAr
        ? { dateStyle: 'medium', timeStyle: 'short' }
        : { dateStyle: 'medium', timeStyle: 'short' };
    try {
        return new Intl.DateTimeFormat(loc, opts).format(dt);
    } catch {
        return dt.toLocaleString(loc);
    }
}

/** Unwrap GetTelecomUniversalSearch rows from ApiSuccessResult. */
function parseUniversalSearchRows(res) {
    if (typeof StorageManager !== 'undefined' && typeof StorageManager.apiList === 'function') {
        const list = StorageManager.apiList(res);
        if (Array.isArray(list)) return list;
    }
    const content = res?.data?.content ?? res?.data?.Content;
    const rows = content?.data ?? content?.Data;
    return Array.isArray(rows) ? rows : [];
}

function pickCustomerIdFromSearchRow(row) {
    return row?.customerId || row?.CustomerId || '';
}

/** Mirrors server rule: name-only queries are rejected (national ID / registry / MSISDN only). */
function isNameOnlyTelecomSearchTerm(term) {
    const t = (term || '').trim();
    if (t.length < 2) return false;
    const digits = t.replace(/[٠-٩۰-۹]/g, (ch) => {
        const cp = ch.codePointAt(0);
        if (cp >= 0x0660 && cp <= 0x0669) return String(cp - 0x0660);
        if (cp >= 0x06f0 && cp <= 0x06f9) return String(cp - 0x06f0);
        return ch;
    }).replace(/\D/g, '');
    if (digits.length >= 2) return false;
    return /[\p{L}]/u.test(t);
}

function createTelecomApp() {
    const { useI18n } = VueI18n;

    return {
        setup() {
            const { t, locale } = useI18n();

            const hubPermissions = Vue.computed(() => {
                const roles = StorageManager.getUserRoles() || [];
                const perms = StorageManager.getPermissions?.() || [];
                return {
                    canCreateOps: roles.some((x) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(x)),
                    canConfirmCbs: roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice'].includes(x)),
                    canSeeBillingLog: roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice', 'TelecomManagement'].includes(x)),
                    showRetailShortcuts: roles.some((x) => !TELECOM_ROLE_NAMES.has(x)),
                    canOpenSubscriberRegistry: roles.some((x) => SUBSCRIBER_REGISTRY_ROLES.has(x)),
                    canMutatePrimaryLine: roles.some((x) =>
                        ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(x)),
                };
            });

            const state = Vue.reactive({
                kpis: { arpuDemo: 0, churnPercentDemo: 0, branchHeat: [] },
                operations: [],
                searchResults: [],
                searchTerm: '',
                searchBusy: false,
                hasSearched: false,
                loading: true,
                loadError: null,
                localeSkeleton: false,
                migrationEligibleProducts: [],
                migrationOffersBusy: false,
                migrationLineTypeHint: '',
                currentProductName: '',
                preloadedProductId: null,
                wizard: {
                    visible: false,
                    kind: '',
                    step: 1,
                    primarySearchTerm: '',
                    primaryResults: [],
                    primaryBusy: false,
                    primarySubscriberProfileId: '',
                    primaryMsisdnAssetId: '',
                    primaryLabel: '',
                    primaryRowKey: '',
                    secondarySearchTerm: '',
                    secondaryResults: [],
                    secondaryBusy: false,
                    secondarySubscriberProfileId: '',
                    secondaryMsisdnAssetId: '',
                    secondaryLabel: '',
                    notes: '',
                    createdOperationId: '',
                    createdOperationNumber: '',
                    documentMarkedUploaded: false,
                    identityFile: null,
                    submitBusy: false,
                    uploadBusy: false,
                    migrationTargetProductId: '',
                    targetOfferOtherText: '',
                    customerId: '',
                    simIccid: '',
                },
                searchModalVisible: false,
                searchModalView: 'list',
                subscriberDetail: null,
                subscriberDetailBusy: false,
                registryModalVisible: false,
                registryIframeSrc: '',
                detailTelecomSubscriptionId: '',
                detailTelecomMsisdn: '',
                detailTelecomTypeId: 'a0e0e0e0-0000-4000-8000-000000000001',
                detailLineSaveBusy: false,
                telecomLineTypes: [],
                huaweiCbsBusy: false,
                huaweiCbsData: null,
                hlrLiveBusy: false,
                hlrResyncBusy: false,
                hlrLiveData: null,
                takeOverApproval: {
                    visible: false,
                    operationId: '',
                    number: '',
                    busy: false,
                    approveBusy: false,
                    detail: null,
                    documentUrl: '',
                },
            });

            const contentDir = Vue.computed(() => (locale.value === 'ar' ? 'rtl' : 'ltr'));
            const contentLang = Vue.computed(() => (locale.value === 'ar' ? 'ar' : 'en'));

            const kindToApiEnum = () => {
                const k = state.wizard.kind;
                if (k === 'activate') return 0;
                if (k === 'migrate') return 1;
                if (k === 'takeover') return 2;
                if (k === 'simswap') return 3;
                if (k === 'addpackage' || k === 'support') return 4;
                return 0;
            };

            const wizardTitle = Vue.computed(() => {
                const k = state.wizard.kind;
                if (!k) return '';
                return t('telecom.wizard.titles.' + k);
            });

            const needsSecondaryParty = Vue.computed(() => state.wizard.kind === 'takeover' || state.wizard.kind === 'migrate' || state.wizard.kind === 'activate');

            const secondaryRequired = Vue.computed(() => state.wizard.kind === 'takeover' || state.wizard.kind === 'activate');

            const migrateNeedsOtherText = Vue.computed(
                () => state.wizard.kind === 'migrate' && state.wizard.migrationTargetProductId === 'OTHER'
            );

            const buildMigrateTargetOfferPayload = () => {
                const sel = (state.wizard.migrationTargetProductId || '').trim();
                if (!sel) return null;
                if (sel === 'OTHER') {
                    const txt = (state.wizard.targetOfferOtherText || '').trim();
                    return txt || null;
                }
                const row = state.migrationEligibleProducts.find((x) => x.id === sel);
                if (row && row.name) return String(row.name).trim();
                return null;
            };

            const canWizardGoToStep3 = Vue.computed(
                () => !!state.wizard.createdOperationId && state.wizard.documentMarkedUploaded
            );

            const rowKey = (r) => `${r.resultType}:${r.id}`;

            const resetWizardState = () => {
                state.wizard.step = 1;
                state.wizard.primarySearchTerm = '';
                state.wizard.primaryResults = [];
                state.wizard.primaryBusy = false;
                state.wizard.primarySubscriberProfileId = '';
                state.wizard.primaryMsisdnAssetId = '';
                state.wizard.primaryLabel = '';
                state.wizard.primaryRowKey = '';
                state.wizard.secondarySearchTerm = '';
                state.wizard.secondaryResults = [];
                state.wizard.secondaryBusy = false;
                state.wizard.secondarySubscriberProfileId = '';
                state.wizard.secondaryMsisdnAssetId = '';
                state.wizard.secondaryLabel = '';
                state.wizard.notes = '';
                state.wizard.createdOperationId = '';
                state.wizard.createdOperationNumber = '';
                state.wizard.documentMarkedUploaded = false;
                state.wizard.identityFile = null;
                state.wizard.submitBusy = false;
                state.wizard.uploadBusy = false;
                state.wizard.migrationTargetProductId = '';
                state.wizard.targetOfferOtherText = '';
                state.wizard.customerId = '';
                state.wizard.simIccid = '';
                state.migrationEligibleProducts = [];
                state.migrationOffersBusy = false;
                state.migrationLineTypeHint = '';
                state.currentProductName = '';
                state.preloadedProductId = null;
            };

            const formatMoney = (n) => formatMoneyIntl(n, locale.value);

            const branchLoadLabel = (revenueDemo) => {
                const v = Number(revenueDemo);
                if (isNaN(v)) return '—';
                if (v >= 80) return t('telecom.branchLoad.high');
                if (v >= 40) return t('telecom.branchLoad.mid');
                return t('telecom.branchLoad.low');
            };

            const branchLoadBadgeClass = (revenueDemo) => {
                const v = Number(revenueDemo);
                if (v >= 80) return 'bg-danger';
                if (v >= 40) return 'bg-warning text-dark';
                return 'bg-success';
            };

            const formatDate = (d) => formatDateIntl(d, locale.value);

            const statusBadge = (status) => {
                const s = Number(status);
                if (s === 3) return 'bg-success';
                if (s === 6) return 'bg-primary';
                if (s === 5) return 'bg-info text-dark';
                if (s === 2) return 'bg-warning text-dark';
                if (s === 4) return 'bg-danger';
                if (s === 1) return 'bg-secondary';
                return 'bg-secondary';
            };

            const operationConfirmable = (o) =>
                Number(o?.documentStatus) >= 1 && [0, 2, 5].includes(Number(o?.status));

            const kindLabelAr = (kind) => {
                const k = Number(kind);
                if (k === 2) return 'نقل ملكية';
                if (k === 1) return 'تحويل';
                if (k === 0) return 'تفعيل';
                if (k === 3) return 'تبديل شريحة';
                return String(kind ?? '—');
            };

            const statusLabelAr = (status) => {
                const s = Number(status);
                if (s === 5) return 'قيد التدقيق القانوني';
                if (s === 6) return 'تجهيز الشبكة';
                if (s === 3) return 'منجز';
                if (s === 4) return 'فشل';
                if (s === 1) return 'مؤكد';
                if (s === 0) return 'مسودة';
                return String(status ?? '—');
            };

            const isTakeOverPendingReview = (o) => Number(o?.kind) === 2 && Number(o?.status) === 5;

            const pendingTakeOverCount = Vue.computed(
                () => (state.operations || []).filter((o) => isTakeOverPendingReview(o)).length
            );

            const apiBase = () => {
                const base = typeof AxiosManager !== 'undefined' && AxiosManager.getBaseUrl ? AxiosManager.getBaseUrl() : '';
                return (base || '').replace(/\/$/, '');
            };

            const identityDocumentUrl = (operationId) => {
                if (!operationId) return '';
                const token = StorageManager.getAccessToken?.();
                const q = `id=${encodeURIComponent(operationId)}${token ? `&access_token=${encodeURIComponent(token)}` : ''}`;
                return `${apiBase()}/Telecom/DownloadTelecomOperationIdentityDocument?${q}`;
            };

            const pipelineStepsForOp = (status) => {
                const s = Number(status);
                const failed = s === 4;
                return [
                    { label: 'مسودة', done: s !== 4, active: s === 0, failed },
                    { label: 'وثائق', done: s >= 5 || s === 6 || s === 3 || s === 1, active: s === 5, failed },
                    { label: 'تجهيز', done: s === 3 || s === 1, active: s === 6, failed },
                    { label: 'منجز', done: s === 3, active: false, failed },
                ];
            };

            const loadKpis = async () => {
                try {
                    const res = await AxiosManager.get('/Telecom/GetTelecomDashboardKpis', {});
                    const c = res?.data?.content;
                    if (!c) return false;
                    state.kpis.arpuDemo = c.arpuDemo ?? 0;
                    state.kpis.churnPercentDemo = c.churnPercentDemo ?? 0;
                    state.kpis.branchHeat = c.branchHeat ?? [];
                    return true;
                } catch {
                    state.kpis.branchHeat = [];
                    return false;
                }
            };

            const loadOperations = async () => {
                try {
                    const res = await AxiosManager.get('/Telecom/GetTelecomOperationList', {});
                    state.operations = res?.data?.content?.data ?? [];
                    return true;
                } catch {
                    state.operations = [];
                    return false;
                }
            };

            const refreshAll = async () => {
                state.loading = true;
                state.loadError = null;
                try {
                    const results = await Promise.all([loadKpis(), loadOperations()]);
                    const okK = results[0];
                    const okO = results[1];
                    if (!okK || !okO) {
                        state.loadError = t('telecom.refreshFail');
                    }
                } finally {
                    state.loading = false;
                }
            };

            const runSearch = async () => {
                const term = (state.searchTerm || '').trim();
                if (term.length < 2) {
                    state.searchResults = [];
                    state.hasSearched = false;
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.searchTitle'),
                            text: t('telecom.swal.searchTooShort'),
                            timer: 2400,
                            showConfirmButton: true,
                        });
                    }
                    return;
                }
                if (isNameOnlyTelecomSearchTerm(term)) {
                    state.searchResults = [];
                    state.hasSearched = false;
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.searchTitle'),
                            text: t('telecom.swal.searchNameNotAllowed'),
                            showConfirmButton: true,
                        });
                    }
                    return;
                }
                state.searchBusy = true;
                state.hasSearched = true;
                try {
                    const searchUrl = '/Telecom/GetTelecomUniversalSearch?term=' + encodeURIComponent(term);
                    const res = await AxiosManager.get(searchUrl, {});
                    const rows = parseUniversalSearchRows(res);
                    if (!rows.length) {
                        state.searchResults = [];
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'info',
                                title: t('telecom.swal.searchTitle'),
                                text: t('telecom.swal.searchNoResults'),
                                showConfirmButton: true,
                            });
                        }
                        return;
                    }
                    const singleCid = rows.length === 1 ? pickCustomerIdFromSearchRow(rows[0]) : '';
                    if (singleCid) {
                        window.location.href =
                            '/Telecom/Customer360Profile?customerId=' + encodeURIComponent(singleCid);
                        return;
                    }
                    const encodedTerm = encodeURIComponent(term);
                    window.location.href = '/Telecom/UnifiedSearch?q=' + encodedTerm;
                    return;
                } catch {
                    state.searchResults = [];
                    if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.searchTitle'), text: t('telecom.swal.searchFail'), timer: 2000, showConfirmButton: false });
                    }
                } finally {
                    state.searchBusy = false;
                }
            };

            const closeSearchModal = () => {
                state.searchModalVisible = false;
                state.searchModalView = 'list';
                state.subscriberDetail = null;
                state.subscriberDetailBusy = false;
            };

            const closeRegistryModal = () => {
                state.registryModalVisible = false;
                state.registryIframeSrc = '';
            };

            const openRegistryModalForCustomer = (customerId) => {
                if (!customerId) return;
                state.registryIframeSrc = '/Customers/CustomerList?embed=1&customerId=' + encodeURIComponent(customerId);
                state.registryModalVisible = true;
            };

            const openRegistryModalNew = () => {
                window.location.href = '/Customers/CustomerList?action=new';
            };

            const searchModalBackToList = () => {
                state.searchModalView = 'list';
                state.subscriberDetail = null;
                state.subscriberDetailBusy = false;
            };

            const syncDetailLineEditFields = () => {
                const d = state.subscriberDetail;
                if (!d) {
                    state.detailTelecomSubscriptionId = '';
                    state.detailTelecomMsisdn = '';
                    state.detailTelecomTypeId = 'a0e0e0e0-0000-4000-8000-000000000001';
                    return;
                }
                const subs = d.subscriptions || [];
                const primary = subs.find((x) => x.isPrimaryLine) || subs[0];
                state.detailTelecomSubscriptionId = primary?.id || '';
                state.detailTelecomMsisdn = primary?.msisdn || '';
                const defId =
                    (state.telecomLineTypes || []).find((x) => x.isDefault)?.id ||
                    'a0e0e0e0-0000-4000-8000-000000000001';
                state.detailTelecomTypeId = primary?.subscriptionTypeId || defId;
            };

            const onDetailSubscriptionChange = () => {
                const d = state.subscriberDetail;
                if (!d) return;
                const subs = d.subscriptions || [];
                const found = subs.find((x) => x.id === state.detailTelecomSubscriptionId);
                if (found) {
                    state.detailTelecomMsisdn = found.msisdn || '';
                    state.detailTelecomTypeId = found.subscriptionTypeId || 'a0e0e0e0-0000-4000-8000-000000000001';
                }
            };

            const openSubscriberDetailFromRow = async (r) => {
                const pid = profileIdFromRow(r);
                if (!pid) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.noProfileTitle'),
                            text: t('telecom.swal.noProfileText'),
                        });
                    }
                    return;
                }
                state.searchModalView = 'detail';
                state.subscriberDetailBusy = true;
                state.subscriberDetail = null;
                state.hlrLiveData = null;
                try {
                    const res = await AxiosManager.get(
                        '/Telecom/GetTelecomSubscriberProfileDetail?subscriberProfileId=' + encodeURIComponent(pid),
                        {}
                    );
                    state.subscriberDetail = res?.data?.content ?? null;
                    syncDetailLineEditFields();
                    if (state.subscriberDetail) {
                        fetchHuaweiCbsData(state.detailTelecomMsisdn);
                    } else {
                        state.huaweiCbsData = null;
                    }
                    if (!state.subscriberDetail && window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.searchModal.detailMissingTitle'),
                            text: t('telecom.searchModal.detailMissingText'),
                        });
                        state.searchModalView = 'list';
                    }
                } catch {
                    state.subscriberDetail = null;
                    if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.searchTitle'), text: t('telecom.searchModal.detailLoadFail') });
                    }
                    state.searchModalView = 'list';
                } finally {
                    state.subscriberDetailBusy = false;
                }
            };

            const goToSubscriberRegistry = (r) => {
                if (!hubPermissions.value.canOpenSubscriberRegistry) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'info', title: t('telecom.searchModal.registryDeniedTitle'), text: t('telecom.searchModal.registryDeniedText') });
                    }
                    return;
                }
                const cid = r?.customerId;
                if (!cid) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'info', title: t('telecom.searchModal.noCustomerTitle'), text: t('telecom.searchModal.noCustomerText') });
                    }
                    return;
                }
                closeSearchModal();
                openRegistryModalForCustomer(cid);
            };

            const goToSubscriberRegistryFromDetail = () => {
                const cid = state.subscriberDetail?.customerId;
                if (!cid) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'info', title: t('telecom.searchModal.noCustomerTitle'), text: t('telecom.searchModal.noCustomerText') });
                    }
                    return;
                }
                if (!hubPermissions.value.canOpenSubscriberRegistry) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'info', title: t('telecom.searchModal.registryDeniedTitle'), text: t('telecom.searchModal.registryDeniedText') });
                    }
                    return;
                }
                closeSearchModal();
                openRegistryModalForCustomer(cid);
            };

            const saveDetailPrimaryLine = async () => {
                const cid = state.subscriberDetail?.customerId;
                if (!cid) {
                    return;
                }
                const subId = state.detailTelecomSubscriptionId;
                const st = state.detailTelecomTypeId;
                state.detailLineSaveBusy = true;
                try {
                    const res = await AxiosManager.post('/Telecom/UpdateCustomerPrimaryTelecomLine', {
                        customerId: cid,
                        subscriptionId: subId,
                        primaryMsisdn: null,
                        primarySubscriptionTypeId: st,
                        updatedById: StorageManager.getUserId(),
                    });
                    const ok = res?.data?.code === 200;
                    if (ok) {
                        const pid = state.subscriberDetail?.subscriberProfileId;
                        if (pid) {
                            state.subscriberDetailBusy = true;
                            try {
                                const res2 = await AxiosManager.get(
                                    '/Telecom/GetTelecomSubscriberProfileDetail?subscriberProfileId=' + encodeURIComponent(pid),
                                    {}
                                );
                                state.subscriberDetail = res2?.data?.content ?? state.subscriberDetail;
                                syncDetailLineEditFields();
                            } finally {
                                state.subscriberDetailBusy = false;
                            }
                        }
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'success',
                                title: t('telecom.searchModal.lineEditSaved'),
                                timer: 1600,
                                showConfirmButton: false,
                            });
                        }
                    } else if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.failTitle'), text: res?.data?.message ?? '' });
                    }
                } catch (e) {
                    const msg = e.response?.data?.message || e.message || '';
                    if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.errorTitle'), text: String(msg) });
                    }
                } finally {
                    state.detailLineSaveBusy = false;
                }
            };

            const wizardSearch = async (term, which) => {
                const tt = (term || '').trim();
                if (tt.length < 2) {
                    if (which === 'primary') state.wizard.primaryResults = [];
                    else state.wizard.secondaryResults = [];
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.searchTitle'),
                            text: t('telecom.swal.searchTooShort'),
                            timer: 2200,
                            showConfirmButton: true,
                        });
                    }
                    return;
                }
                if (isNameOnlyTelecomSearchTerm(tt)) {
                    if (which === 'primary') state.wizard.primaryResults = [];
                    else state.wizard.secondaryResults = [];
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.searchTitle'),
                            text: t('telecom.swal.searchNameNotAllowed'),
                            showConfirmButton: true,
                        });
                    }
                    return;
                }
                if (which === 'primary') state.wizard.primaryBusy = true;
                else state.wizard.secondaryBusy = true;
                try {
                    const q = '/Telecom/GetTelecomUniversalSearch?term=' + encodeURIComponent(tt) + 
                        (state.wizard.kind === 'activate' && which === 'secondary' ? '&availableOnly=true' : '') +
                        (state.wizard.kind === 'activate' && which === 'primary' ? '&profilesOnly=true' : '');
                    const res = await AxiosManager.get(q, {});
                    const rows = res?.data?.content?.data ?? [];
                    if (which === 'primary') state.wizard.primaryResults = rows;
                    else state.wizard.secondaryResults = rows;
                } catch {
                    if (which === 'primary') state.wizard.primaryResults = [];
                    else state.wizard.secondaryResults = [];
                    if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.searchTitle'), text: t('telecom.swal.searchFail'), timer: 1800, showConfirmButton: false });
                    }
                } finally {
                    if (which === 'primary') state.wizard.primaryBusy = false;
                    else state.wizard.secondaryBusy = false;
                }
            };

            const runWizardPrimarySearch = () => wizardSearch(state.wizard.primarySearchTerm, 'primary');
            const runWizardSecondarySearch = () => wizardSearch(state.wizard.secondarySearchTerm, 'secondary');

            const profileIdFromRow = (r) => {
                if (!r) return '';
                if (r.subscriberProfileId) return r.subscriberProfileId;
                if (r.resultType === 'SubscriberProfile' && r.id) return r.id;
                return '';
            };

            const loadMigrationEligibleProducts = async () => {
                if (state.wizard.kind !== 'migrate' && state.wizard.kind !== 'activate') return;
                const sid = (state.wizard.primarySubscriberProfileId || '').trim();
                if (!sid) {
                    state.migrationEligibleProducts = [];
                    state.migrationLineTypeHint = '';
                    return;
                }
                state.migrationOffersBusy = true;
                try {
                    const ms = (state.wizard.primaryMsisdnAssetId || '').trim();
                    let url = '/Product/GetMigrationEligibleProducts?subscriberProfileId=' + encodeURIComponent(sid);
                    if (ms) url += '&msisdnAssetId=' + encodeURIComponent(ms);
                    const res = await AxiosManager.get(url, {});
                    const c = res?.data?.content;
                    state.migrationEligibleProducts = Array.isArray(c?.data) ? c.data : [];
                    state.migrationLineTypeHint = c?.resolvedSubscriptionTypeLabel || c?.ResolvedSubscriptionTypeLabel || '';
                    state.currentProductName = c?.currentProductName || c?.CurrentProductName || '';
                    const resolvedTypeId = c?.resolvedSubscriptionTypeId || c?.ResolvedSubscriptionTypeId || '';

                    // Check compatibility of preloaded package if present
                    if (state.preloadedProductId) {
                        try {
                            const prodRes = await AxiosManager.get(`/ProductOffering/GetProductOfferingSingle?id=${state.preloadedProductId}`);
                            const product = prodRes?.data?.content;
                            if (product) {
                                const prodCompatId = product.compatibleSubscriptionTypeId;
                                if (prodCompatId && prodCompatId !== resolvedTypeId) {
                                    // Incompatible! Show Smart Alert
                                    const confirm = await Swal.fire({
                                        title: state.contentLang === 'ar' ? 'تنبيه عدم التوافق' : 'Incompatibility Warning',
                                        html: state.contentLang === 'ar'
                                            ? `نوع اشتراك المشترك الحالي لا يتوافق مع متطلبات باقة <strong>(${product.name})</strong>.<br/><br/>هل ترغب في <strong>تحويل نوع اشتراك المشترك</strong> تلقائياً للمتابعة؟`
                                            : `The subscriber's line type is not compatible with the plan <strong>(${product.nameEn})</strong>.<br/><br/>Do you want to <strong>migrate the subscription type</strong> to proceed?`,
                                        icon: 'warning',
                                        showCancelButton: true,
                                        confirmButtonColor: '#c8102e',
                                        confirmButtonText: state.contentLang === 'ar' ? 'نعم، تحويل ومتابعة' : 'Yes, Migrate',
                                        cancelButtonText: state.contentLang === 'ar' ? 'إلغاء' : 'Cancel'
                                    });

                                    if (confirm.isConfirmed) {
                                        // Push the offering to eligible list so it's selectable in UI dropdown
                                        if (!state.migrationEligibleProducts.some(p => p.id === product.id)) {
                                            state.migrationEligibleProducts.push({
                                                id: product.id,
                                                name: state.contentLang === 'ar' ? product.name : product.nameEn,
                                                serviceCode: product.code,
                                                compatibleSubscriptionTypeId: prodCompatId
                                            });
                                        }
                                        state.wizard.migrationTargetProductId = product.id;
                                        state.wizard.notes = state.contentLang === 'ar'
                                            ? `تحويل تلقائي لنوع الاشتراك وتفعيل باقة ${product.name}`
                                            : `Automated subscription migration to activate ${product.nameEn}`;
                                    } else {
                                        state.wizard.migrationTargetProductId = '';
                                    }
                                } else {
                                    // Compatible! Just ensure it's in eligible list and select it
                                    if (!state.migrationEligibleProducts.some(p => p.id === product.id)) {
                                        state.migrationEligibleProducts.push({
                                            id: product.id,
                                            name: state.contentLang === 'ar' ? product.name : product.nameEn,
                                            serviceCode: product.code,
                                            compatibleSubscriptionTypeId: prodCompatId
                                        });
                                    }
                                    state.wizard.migrationTargetProductId = product.id;
                                }
                            }
                        } catch (e) {
                            console.error("Error checking preloaded offering compatibility", e);
                        } finally {
                            state.preloadedProductId = null; // Reset
                        }
                    }
                } catch {
                    state.migrationEligibleProducts = [];
                    state.migrationLineTypeHint = '';
                    state.currentProductName = '';
                } finally {
                    state.migrationOffersBusy = false;
                }
            };

            const selectWizardPrimary = (r) => {
                const pid = profileIdFromRow(r);
                if (!pid) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.noProfileTitle'),
                            text: t('telecom.swal.noProfileText'),
                        });
                    }
                    return;
                }
                state.wizard.primarySubscriberProfileId = pid;
                state.wizard.primaryMsisdnAssetId = r.resultType === 'Msisdn' ? r.id : '';
                state.wizard.customerId = r.customerId || state.wizard.customerId || '';
                state.wizard.primaryLabel = `${r.title || '—'} (${r.resultType})`;
                state.wizard.primaryRowKey = rowKey(r);
                
                if (state.preloadedProductId) {
                    state.wizard.migrationTargetProductId = state.preloadedProductId;
                } else {
                    state.wizard.migrationTargetProductId = '';
                }
                
                state.wizard.targetOfferOtherText = '';
                if (state.wizard.kind === 'migrate' || state.wizard.kind === 'activate') {
                    loadMigrationEligibleProducts();
                }
            };

            const clearWizardPrimary = () => {
                state.wizard.primarySubscriberProfileId = '';
                state.wizard.primaryMsisdnAssetId = '';
                state.wizard.primaryLabel = '';
                state.wizard.primaryRowKey = '';
                state.migrationEligibleProducts = [];
                state.migrationLineTypeHint = '';
                state.currentProductName = '';
                state.wizard.migrationTargetProductId = '';
                state.wizard.targetOfferOtherText = '';
            };

            const reserveMsisdnForWizard = async () => {
                const assetId = (state.wizard.secondaryMsisdnAssetId || '').trim();
                const customerId = (state.wizard.customerId || '').trim();
                if (!assetId || !customerId) return;
                try {
                    await AxiosManager.post('/Telecom/ReserveMsisdnForCustomer', {
                        msisdnAssetId: assetId,
                        customerId,
                        reservedByUserId: StorageManager.getUserId(),
                    });
                } catch (e) {
                    const msg = pickHttpErrorMessage(e);
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: state.contentLang === 'ar' ? 'تعذّر حجز الرقم' : 'Reservation failed',
                            text: msg || (state.contentLang === 'ar' ? 'تأكد أن الرقم متاح.' : 'Ensure the number is available.'),
                        });
                    }
                }
            };

            const selectWizardSecondary = async (r) => {
                if (state.wizard.kind === 'activate') {
                    state.wizard.secondaryMsisdnAssetId = r.id;
                    state.wizard.secondaryLabel = `${r.title || '—'} (${r.resultType})`;
                    await reserveMsisdnForWizard();
                    return;
                }
                const pid = profileIdFromRow(r);
                if (!pid) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.noProfileTitle'),
                            text: t('telecom.swal.noProfileShort'),
                        });
                    }
                    return;
                }
                if (pid === state.wizard.primarySubscriberProfileId) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'info', title: t('telecom.swal.samePartyTitle'), text: t('telecom.swal.samePartyText') });
                    }
                    return;
                }
                state.wizard.secondarySubscriberProfileId = pid;
                state.wizard.secondaryLabel = `${r.title || '—'} (${r.resultType})`;
            };

            const clearWizardSecondary = () => {
                state.wizard.secondarySubscriberProfileId = '';
                state.wizard.secondaryMsisdnAssetId = '';
                state.wizard.secondaryLabel = '';
            };

            const openWizard = (kind) => {
                resetWizardState();
                state.wizard.visible = true;
                state.wizard.kind = kind;
            };

            const closeWizard = () => {
                state.wizard.visible = false;
                resetWizardState();
            };

            const finishWizard = async () => {
                state.wizard.visible = false;
                resetWizardState();
                await refreshAll();
            };

            const wizardSubmitBusy = Vue.computed(() => state.wizard.submitBusy);
            const wizardUploadBusy = Vue.computed(() => state.wizard.uploadBusy);

            const validateWizardBeforeCreateDraft = () => {
                if (!(state.wizard.primarySubscriberProfileId || '').trim()) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.swal.incompleteText') });
                    }
                    return false;
                }
                if (secondaryRequired.value) {
                    if (state.wizard.kind === 'activate' && !state.wizard.secondaryMsisdnAssetId) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: state.contentLang === 'ar' ? 'يجب اختيار الرقم المطلوب (المتاح) من نتائج البحث قبل المتابعة.' : 'Select an available number from the search results to proceed.' });
                        }
                        return false;
                    }
                    if (state.wizard.kind === 'activate' && !(state.wizard.simIccid || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.incompleteTitle'),
                                text: state.contentLang === 'ar' ? 'أدخل ICCID الشريحة للتفعيل الثلاثي.' : 'Enter SIM ICCID for triple binding.',
                            });
                        }
                        return false;
                    }
                    if (state.wizard.kind === 'takeover' && !(state.wizard.secondarySubscriberProfileId || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.swal.secondaryPartyTitle'), text: t('telecom.swal.secondaryPartyText') });
                        }
                        return false;
                    }
                }
                
                const notes = (state.wizard.notes || '').trim();
                if (notes && notes.length > 500) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'warning', title: 'تنبيه', text: 'الملاحظات يجب ألا تتجاوز 500 حرف.' });
                    }
                    return false;
                }

                if (state.wizard.kind === 'migrate' || state.wizard.kind === 'activate') {
                    const sel = (state.wizard.migrationTargetProductId || '').trim();
                    if (!sel) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.targetOfferTitle'),
                                text: t('telecom.swal.targetOfferText'),
                            });
                        }
                        return false;
                    }
                    if (sel === 'OTHER' && !(state.wizard.targetOfferOtherText || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.targetOfferOtherTitle'),
                                text: t('telecom.swal.targetOfferOtherText'),
                            });
                        }
                        return false;
                    }
                }
                return true;
            };

            const wizardStepNext = () => {
                if (state.wizard.step === 1) {
                    if (!validateWizardBeforeCreateDraft()) return;
                    state.wizard.step = 2;
                    return;
                }
                if (state.wizard.step === 2) {
                    if (!canWizardGoToStep3.value) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'info',
                                title: t('telecom.swal.completeStepsTitle'),
                                text: t('telecom.swal.completeStepsText'),
                            });
                        }
                        return;
                    }
                    state.wizard.step = 3;
                }
            };

            const wizardStepPrev = () => {
                if (state.wizard.step > 1) state.wizard.step -= 1;
            };

            const submitCreateOperation = async () => {
                if (!validateWizardBeforeCreateDraft()) return;
                state.wizard.submitBusy = true;
                try {
                    const body = {
                        kind: kindToApiEnum(),
                        subscriberProfileId: state.wizard.primarySubscriberProfileId,
                        secondarySubscriberProfileId: state.wizard.secondarySubscriberProfileId || null,
                        msisdnAssetId: state.wizard.kind === 'activate' ? (state.wizard.secondaryMsisdnAssetId || null) : (state.wizard.primaryMsisdnAssetId || null),
                        productId: null,
                        productOfferingId:
                            (state.wizard.kind === 'migrate' || state.wizard.kind === 'activate') &&
                            state.wizard.migrationTargetProductId &&
                            state.wizard.migrationTargetProductId !== 'OTHER'
                                ? state.wizard.migrationTargetProductId
                                : null,
                        notes: (state.wizard.notes || '').trim() || null,
                        targetOfferName: state.wizard.kind === 'migrate' || state.wizard.kind === 'activate' ? buildMigrateTargetOfferPayload() : null,
                        simIccid:
                            state.wizard.kind === 'activate' ? (state.wizard.simIccid || '').trim() || null : null,
                        createdById: StorageManager.getUserId(),
                    };
                    const res = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                    const ok = res?.data?.code === 200;
                    const entity = res?.data?.content?.data;
                    if (ok && entity?.id) {
                        state.wizard.createdOperationId = entity.id;
                        state.wizard.createdOperationNumber = entity.number || '';
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'success',
                                title: t('telecom.swal.draftOkTitle'),
                                text: state.wizard.createdOperationNumber,
                                timer: 1600,
                                showConfirmButton: false,
                            });
                        }
                    } else if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.serverErrTitle'), text: res?.data?.message || t('telecom.swal.serverErrText') });
                    }
                } catch (e) {
                    const msg = pickHttpErrorMessage(e);
                    if (window.Swal) Swal.fire({ icon: 'error', title: t('telecom.swal.createFailTitle'), text: msg || t('telecom.swal.serverErrText') });
                } finally {
                    state.wizard.submitBusy = false;
                }
            };

            const onWizardIdentityFileChange = (ev) => {
                const f = ev?.target?.files?.[0];
                state.wizard.identityFile = f || null;
            };

            const uploadWizardIdentityDocument = async (operationId) => {
                const uid = StorageManager.getUserId();
                if (state.wizard.kind === 'takeover' && state.wizard.identityFile) {
                    const form = new FormData();
                    form.append('id', operationId);
                    form.append('updatedById', uid || '');
                    form.append('file', state.wizard.identityFile);
                    return AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                }
                return AxiosManager.post('/Telecom/UploadTelecomOperationDocument', {
                    id: operationId,
                    updatedById: uid,
                });
            };

            const submitMarkDocumentUploaded = async () => {
                if (!state.wizard.createdOperationId) return;
                if (state.wizard.kind === 'takeover' && !state.wizard.identityFile) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: 'هوية مطلوبة',
                            text: 'ارفع صورة أو PDF لهوية المالك الجديد قبل الإرسال.',
                        });
                    }
                    return;
                }
                state.wizard.uploadBusy = true;
                try {
                    const res = await uploadWizardIdentityDocument(state.wizard.createdOperationId);
                    if (res?.data?.code === 200) {
                        state.wizard.documentMarkedUploaded = true;
                        if (window.Swal) {
                            const title =
                                state.wizard.kind === 'takeover'
                                    ? 'تم الإرسال للباك أوفيس'
                                    : t('telecom.swal.docOkTitle');
                            Swal.fire({ icon: 'success', title, timer: 1600, showConfirmButton: false });
                        }
                    } else if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.failTitle'), text: res?.data?.message ?? '' });
                    }
                } catch (e) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.errorTitle'), text: e.response?.data?.message ?? '' });
                    }
                } finally {
                    state.wizard.uploadBusy = false;
                }
            };

            let takeOverModalInstance = null;
            const getTakeOverModal = () => {
                if (!takeOverModalInstance && typeof bootstrap !== 'undefined') {
                    const el = document.getElementById('takeOverApprovalModal');
                    if (el) takeOverModalInstance = new bootstrap.Modal(el);
                }
                return takeOverModalInstance;
            };

            const closeTakeOverApproval = () => {
                state.takeOverApproval.visible = false;
                state.takeOverApproval.documentUrl = '';
                getTakeOverModal()?.hide();
            };

            const openTakeOverApproval = async (op) => {
                if (!op?.id) return;
                state.takeOverApproval.operationId = op.id;
                state.takeOverApproval.number = op.number || '';
                state.takeOverApproval.busy = true;
                state.takeOverApproval.detail = null;
                state.takeOverApproval.documentUrl = op.hasIdentityDocument ? identityDocumentUrl(op.id) : '';
                getTakeOverModal()?.show();
                try {
                    const res = await AxiosManager.get(
                        '/Telecom/GetTelecomOperationDetail?id=' + encodeURIComponent(op.id),
                        {}
                    );
                    state.takeOverApproval.detail = res?.data?.content?.data ?? null;
                    if (state.takeOverApproval.detail?.hasIdentityDocument) {
                        state.takeOverApproval.documentUrl = identityDocumentUrl(op.id);
                    }
                } catch (e) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'error',
                            title: 'تعذّر تحميل الطلب',
                            text: pickHttpErrorMessage(e),
                        });
                    }
                } finally {
                    state.takeOverApproval.busy = false;
                }
            };

            const approveTakeOverOnNetwork = async () => {
                if (!state.takeOverApproval.operationId) return;
                state.takeOverApproval.approveBusy = true;
                try {
                    const res = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                        id: state.takeOverApproval.operationId,
                        updatedById: StorageManager.getUserId(),
                    });
                    const content = res?.data?.content;
                    const br = content?.billingResult;
                    if (res?.data?.code === 200 && (br?.success || content?.idempotentReplay)) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'success',
                                title: 'تم نقل الملكية',
                                text: 'اكتمل التزويد على CBS وHLR في الخلفية.',
                                timer: 2200,
                                showConfirmButton: false,
                            });
                        }
                        closeTakeOverApproval();
                        await refreshAll();
                    } else if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: 'لم يكتمل التزويد',
                            text: br?.message || res?.data?.message || '',
                        });
                    }
                } catch (e) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'error', title: 'فشل الاعتماد', text: pickHttpErrorMessage(e) });
                    }
                } finally {
                    state.takeOverApproval.approveBusy = false;
                }
            };

            const confirmFirstReadyDraft = async () => {
                if (!hubPermissions.value.canConfirmCbs) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: t('telecom.swal.notAllowedText'),
                        });
                    }
                    return;
                }
                const ready = state.operations.find((o) => operationConfirmable(o));
                if (!ready?.id) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'info', title: t('telecom.swal.noDraftTitle'), text: t('telecom.swal.noDraftText') });
                    }
                    return;
                }
                try {
                    const res = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                        id: ready.id,
                        updatedById: StorageManager.getUserId(),
                    });
                    const content = res?.data?.content;
                    const br = content?.billingResult;
                    if (res?.data?.code === 200 && content?.idempotentReplay) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'info',
                                title: state.contentLang === 'ar' ? 'معالج مسبقاً' : 'Already processed',
                                text: state.contentLang === 'ar' ? 'الطلب معالج مسبقاً.' : 'This operation was already processed.',
                                timer: 2200,
                                showConfirmButton: false,
                            });
                        }
                        await refreshAll();
                    } else if (res?.data?.code === 200 && br?.success) {
                        if (window.Swal) Swal.fire({ icon: 'success', title: t('telecom.swal.confirmOkTitle'), timer: 1400, showConfirmButton: false });
                        await refreshAll();
                    } else if (res?.data?.code === 200 && window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.failTitle'),
                            text: br?.message || res?.data?.message || t('telecom.swal.serverErrText'),
                        });
                    } else if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.failTitle'), text: res?.data?.message ?? '' });
                    }
                } catch (e) {
                    if (window.Swal) Swal.fire({ icon: 'error', title: t('telecom.swal.errorTitle'), text: e.response?.data?.message || t('telecom.swal.sessionHint') });
                }
            };

            const onLocaleChanged = () => {
                const lang = document.documentElement.lang?.toLowerCase().startsWith('en') ? 'en' : 'ar';
                locale.value = lang;
                state.localeSkeleton = true;
                Vue.nextTick(() => {
                    requestAnimationFrame(() => {
                        window.setTimeout(() => {
                            state.localeSkeleton = false;
                        }, 220);
                    });
                });
            };

            Vue.watch(
                () => state.wizard.primaryMsisdnAssetId,
                () => {
                    if (state.wizard.kind !== 'migrate') return;
                    if (!(state.wizard.primarySubscriberProfileId || '').trim()) return;
                    state.wizard.migrationTargetProductId = '';
                    state.wizard.targetOfferOtherText = '';
                    loadMigrationEligibleProducts();
                }
            );

            const wizardPrimaryMultiProfileSameParty = Vue.computed(() => {
                const rows = state.wizard.primaryResults || [];
                const profileRows = rows.filter(
                    (r) => r && r.resultType === 'SubscriberProfile' && r.customerId
                );
                const counts = new Map();
                for (const r of profileRows) {
                    const k = String(r.customerId);
                    counts.set(k, (counts.get(k) || 0) + 1);
                }
                for (const c of counts.values()) {
                    if (c > 1) {
                        return true;
                    }
                }
                return false;
            });

            const fetchHuaweiCbsData = async (msisdn) => {
                const cleanM = (msisdn || '').trim();
                if (!cleanM) {
                    state.huaweiCbsData = null;
                    return;
                }
                state.huaweiCbsBusy = true;
                try {
                    const res = await AxiosManager.get('/huawei-cbs-mock/QueryBalance?msisdn=' + encodeURIComponent(cleanM));
                    state.huaweiCbsData = res?.data ?? null;
                } catch (e) {
                    state.huaweiCbsData = {
                        success: false,
                        message: e.message || 'Failed to query Huawei CBS'
                    };
                } finally {
                    state.huaweiCbsBusy = false;
                }
            };

            const openHuaweiRechargeModal = () => {
                if (!state.huaweiCbsData || !state.huaweiCbsData.msisdn) return;
                const msisdn = state.huaweiCbsData.msisdn;
                const title = state.contentLang === 'ar' ? 'شحن رصيد ذكي — Huawei CBS' : 'Quick Recharge — Huawei CBS';
                const text = state.contentLang === 'ar' 
                    ? `سيتم إرسال طلب الشحن لنظام هواوي CBS للرقم ${msisdn}. الرجاء إدخال القيمة بالليرة السورية:` 
                    : `Recharge request will be routed to Huawei CBS for MSISDN ${msisdn}. Please enter the amount in SYP:`;
                const placeholder = state.contentLang === 'ar' ? 'المبلغ الفعلي (مثال: 15000)' : 'Amount (e.g. 15000)';

                Swal.fire({
                    title: title,
                    text: text,
                    input: 'number',
                    inputPlaceholder: placeholder,
                    showCancelButton: true,
                    confirmButtonText: state.contentLang === 'ar' ? 'شحن الآن' : 'Recharge Now',
                    cancelButtonText: state.contentLang === 'ar' ? 'إلغاء' : 'Cancel',
                    confirmButtonColor: '#c8102e',
                    inputValidator: (value) => {
                        if (!value || isNaN(value) || parseFloat(value) <= 0) {
                            return state.contentLang === 'ar' ? 'يرجى إدخال قيمة صحيحة وموجبة!' : 'Please enter a valid positive amount!';
                        }
                    }
                }).then(async (result) => {
                    if (result.isConfirmed) {
                        const amount = parseFloat(result.value);
                        state.huaweiCbsBusy = true;
                        try {
                            const idempotencyKey = crypto.randomUUID();
                            const res = await AxiosManager.post('/huawei-cbs-mock/Recharge', {
                                msisdn: msisdn,
                                amount: amount
                            }, {
                                headers: {
                                    'X-Idempotency-Key': idempotencyKey
                                }
                            });
                            if (res.data && res.data.success) {
                                state.huaweiCbsData.balance = res.data.newBalance;
                                Swal.fire({
                                    icon: 'success',
                                    title: state.contentLang === 'ar' ? 'تم الشحن بنجاح!' : 'Recharged Successfully!',
                                    text: res.data.message,
                                    confirmButtonColor: '#c8102e'
                                });
                            } else {
                                Swal.fire({
                                    icon: 'error',
                                    title: state.contentLang === 'ar' ? 'فشل الشحن' : 'Recharge Failed',
                                    text: res.data.message || 'Error occurred.',
                                    confirmButtonColor: '#c8102e'
                                });
                            }
                        } catch (err) {
                            Swal.fire({
                                icon: 'error',
                                title: 'Error',
                                text: err.message
                            });
                        } finally {
                            state.huaweiCbsBusy = false;
                        }
                    }
                });
            };

            const queryHlrLiveStatus = async () => {
                const pid = (state.subscriberDetail?.subscriberProfileId || '').trim();
                if (!pid) return;
                state.hlrLiveBusy = true;
                state.hlrLiveData = null;
                try {
                    const res = await AxiosManager.get(
                        '/Telecom/QueryHlrLiveStatus?subscriberProfileId=' + encodeURIComponent(pid),
                        {}
                    );
                    state.hlrLiveData = res?.data?.content ?? null;
                } catch (e) {
                    state.hlrLiveData = { success: false, message: pickHttpErrorMessage(e) || 'HLR query failed' };
                } finally {
                    state.hlrLiveBusy = false;
                }
            };

            const resyncSubscriberFromHlr = async () => {
                const pid = (state.subscriberDetail?.subscriberProfileId || '').trim();
                if (!pid) return;
                state.hlrResyncBusy = true;
                try {
                    const res = await AxiosManager.post('/Telecom/ResyncSubscriberFromHlr', {
                        subscriberProfileId: pid,
                        actorUserId: StorageManager.getUserId(),
                    });
                    const body = res?.data?.content;
                    if (res?.data?.code === 200 && body?.success) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'success',
                                title: locale.value === 'ar' ? 'تمت المزامنة' : 'Synced',
                                text: body.message || '',
                                timer: 1800,
                                showConfirmButton: false,
                            });
                        }
                        const detailRes = await AxiosManager.get(
                            '/Telecom/GetTelecomSubscriberProfileDetail?subscriberProfileId=' + encodeURIComponent(pid),
                            {}
                        );
                        state.subscriberDetail = detailRes?.data?.content ?? state.subscriberDetail;
                        syncDetailLineEditFields();
                        await queryHlrLiveStatus();
                    } else if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: locale.value === 'ar' ? 'فشل المزامنة' : 'Sync failed',
                            text: body?.message || res?.data?.message || '',
                        });
                    }
                } catch (e) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'error', title: 'HLR', text: pickHttpErrorMessage(e) });
                    }
                } finally {
                    state.hlrResyncBusy = false;
                }
            };

            const loadTelecomLineTypes = async () => {
                try {
                    const res = await AxiosManager.get(
                        '/TelecomSubscriptionType/GetTelecomSubscriptionTypeList?isDeleted=false&activeOnly=true',
                        {}
                    );
                    state.telecomLineTypes = res?.data?.content?.data || [];
                } catch {
                    state.telecomLineTypes = [];
                }
            };

            Vue.onMounted(async () => {
                document.documentElement.addEventListener('syriatel-locale-changed', onLocaleChanged);
                try {
                    const params = new URLSearchParams(window.location.search);
                    if (params.get('openPool') === '1') {
                        window.location.replace('/Telecom/MsisdnInventory');
                        return;
                    }

                    await loadTelecomLineTypes();
                    await refreshAll();

                    const wizParam = params.get('wizard');
                    const prodParam = params.get('productId');
                    if (wizParam) {
                        openWizard(wizParam);
                        if (prodParam) {
                            state.wizard.migrationTargetProductId = prodParam;
                            state.preloadedProductId = prodParam;
                        }
                    }

                    if (params.get('entry') === 'subscriber') {
                        const input = document.getElementById('telecomSearchInput');
                        if (input) {
                            input.focus();
                        }
                    }

                    window.addEventListener('message', async (event) => {
                        if (event.data?.action === 'syriatel-customer-saved') {
                            closeRegistryModal();
                            await refreshAll();
                            if (window.Swal) {
                                Swal.fire({ icon: 'success', title: 'تم الحفظ', text: 'تم حفظ بيانات المشترك بنجاح.', timer: 2000, showConfirmButton: false });
                            }
                        } else if (event.data?.action === 'syriatel-customer-saved-activate') {
                            closeRegistryModal();
                            await refreshAll();
                            openWizard('activate');
                        }
                    });
                } finally {
                    if (typeof hideSpinnerAndShowContent === 'function') {
                        hideSpinnerAndShowContent();
                    }
                    if (typeof setFormCardHeight === 'function') {
                        setFormCardHeight();
                    }
                }
            });

            Vue.onUnmounted(() => {
                document.documentElement.removeEventListener('syriatel-locale-changed', onLocaleChanged);
            });

            return {
                ...Vue.toRefs(state),
                t,
                hubPermissions,
                wizardTitle,
                wizardSubmitBusy,
                wizardUploadBusy,
                contentDir,
                contentLang,
                needsSecondaryParty,
                secondaryRequired,
                migrateNeedsOtherText,
                wizardPrimaryMultiProfileSameParty,
                loadMigrationEligibleProducts,
                canWizardGoToStep3,
                rowKey,
                formatMoney,
                formatDate,
                statusBadge,
                branchLoadLabel,
                branchLoadBadgeClass,
                refreshAll,
                runSearch,
                closeSearchModal,
                searchModalBackToList,
                openSubscriberDetailFromRow,
                goToSubscriberRegistry,
                goToSubscriberRegistryFromDetail,
                closeRegistryModal,
                saveDetailPrimaryLine,
                onDetailSubscriptionChange,
                openWizard,
                closeWizard,
                wizardStepNext,
                wizardStepPrev,
                runWizardPrimarySearch,
                runWizardSecondarySearch,
                selectWizardPrimary,
                clearWizardPrimary,
                selectWizardSecondary,
                clearWizardSecondary,
                submitCreateOperation,
                submitMarkDocumentUploaded,
                finishWizard,
                confirmFirstReadyDraft,
                kindLabelAr,
                statusLabelAr,
                isTakeOverPendingReview,
                pendingTakeOverCount,
                openTakeOverApproval,
                closeTakeOverApproval,
                approveTakeOverOnNetwork,
                onWizardIdentityFileChange,
                fetchHuaweiCbsData,
                openHuaweiRechargeModal,
                openRegistryModalNew,
                operationConfirmable,
                queryHlrLiveStatus,
                resyncSubscriberFromHlr,
                pipelineStepsForOp,
            };
        },
    };
}

async function bootstrapTelecomHub() {
    if (typeof Vue === 'undefined' || typeof VueI18n === 'undefined' || !VueI18n.createI18n) {
        console.error('Vue / VueI18n failed to load');
        return;
    }
    const urls = window.__TELECOM_I18N_URLS;
    if (!urls?.ar || !urls?.en) {
        console.error('Telecom locale URLs missing');
        return;
    }
    const loadLocaleJson = async (url) => {
        const res = await fetch(url, { cache: 'no-store' });
        if (!res.ok) throw new Error(`Locale load failed: ${url}`);
        const text = new TextDecoder('utf-8').decode(await res.arrayBuffer());
        return JSON.parse(text);
    };
    const [ar, en] = await Promise.all([loadLocaleJson(urls.ar), loadLocaleJson(urls.en)]);
    const initialLang = document.documentElement.lang?.toLowerCase().startsWith('en') ? 'en' : 'ar';
    const i18n = VueI18n.createI18n({
        legacy: false,
        locale: initialLang,
        fallbackLocale: 'ar',
        messages: { ar, en },
        missingWarn: false,
        fallbackWarn: false,
    });
    window.__telecomHubI18n = i18n;

    const app = Vue.createApp(createTelecomApp());
    app.use(i18n);
    app.mount('#app');
}

bootstrapTelecomHub().catch((e) => {
    console.error(e);
});
