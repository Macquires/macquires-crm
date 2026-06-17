const TELECOM_ROLE_NAMES = new Set(['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice', 'TelecomCallCenter', 'TelecomManagement']);
const SUBSCRIBER_REGISTRY_ROLES = new Set([
    'Customers',
    'TelecomAdmin',
    'TelecomShowroom',
    'TelecomBackOffice',
    'TelecomCallCenter',
    'TelecomManagement',
]);

/** Best-effort bilingual message from API payloads (errors or structured responses). */
function pickApiUserMessage(data, locale) {
    if (!data) return '';
    const isAr = String(locale || '').toLowerCase().startsWith('ar');
    const ar = data.messageAr || data.MessageAr || '';
    const en = data.messageEn || data.MessageEn || '';
    if (ar || en) return isAr ? (ar || en) : (en || ar);
    const inner = data.error?.innerException;
    let m = data.message || data.Message || (typeof inner === 'string' ? inner : '') || '';
    m = String(m);
    if (m.startsWith('Exception: ')) return m.slice(11).trim();
    return m;
}

/** Best-effort message from Axios API error payloads (e.g. validation / exception handler). */
function pickHttpErrorMessage(e, locale) {
    const d = e?.response?.data;
    if (!d) return e?.message ? String(e.message) : '';
    return pickApiUserMessage(d, locale);
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

function resolveWizardCustomerId(wizard) {
    const direct = (wizard?.customerId || '').trim();
    if (direct) {
        return direct;
    }

    const rowKey = (wizard?.primaryRowKey || '').trim();
    if (rowKey.startsWith('Customer:')) {
        return rowKey.slice('Customer:'.length).trim();
    }

    return '';
}

/** Same list as /TelecomSubscriptionTypes/TelecomSubscriptionTypeList (GetTelecomSubscriptionTypeList API). */
function parseTelecomSubscriptionTypeList(res) {
    if (typeof StorageManager !== 'undefined' && typeof StorageManager.apiList === 'function') {
        const list = StorageManager.apiList(res);
        if (Array.isArray(list)) return list;
    }
    const content = res?.data?.content ?? res?.data?.Content;
    const rows = content?.data ?? content?.Data;
    return Array.isArray(rows) ? rows : [];
}

function lineTypeRowId(row) {
    return (row?.id ?? row?.Id ?? '').trim();
}

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
        billingCycle: c.billingCycle ?? c.BillingCycle ?? '',
        iconClass: c.iconClass ?? c.IconClass ?? 'bi-box-seam',
        badgeColor: c.badgeColor ?? c.BadgeColor ?? 'primary',
        components: Array.isArray(c.components) ? c.components : c.Components ?? [],
        pricePlans: Array.isArray(c.pricePlans) ? c.pricePlans : c.PricePlans ?? [],
    };
}

function offerDetailDisplayName(detail, lang) {
    if (!detail) return '';
    const ar = (detail.name || '').trim();
    const en = (detail.nameEn || '').trim();
    return lang === 'ar' ? ar || en : en || ar;
}

function offerDefaultMonthlyPrice(detail) {
    const plans = detail?.pricePlans || [];
    const def = plans.find((p) => p.isDefault || p.IsDefault) || plans[0];
    if (!def) return null;
    const price = Number(def.price ?? def.Price);
    return Number.isFinite(price) && price > 0 ? price : null;
}

function defaultCountryLabel() {
    const hit = window.TelecomI18n?.t?.('common.country.syria');
    if (hit) return hit;
    const lang = (document.documentElement.lang || 'en').toLowerCase();
    return lang.startsWith('en') ? 'Syria' : 'سوريا';
}

function offerComponentByType(detail, type) {
    return (detail?.components || []).find((c) => (c.componentType ?? c.ComponentType) === type);
}

function formatOfferQuotaLine(comp, lang) {
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

function offerDetailSummaryText(detail, lang) {
    if (!detail) return '';
    if (lang === 'ar') {
        return (detail.shortDescription || detail.description || '').trim();
    }
    return (detail.shortDescriptionEn || detail.descriptionEn || detail.shortDescription || detail.description || '').trim();
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

            const secureOpKindKey = (kind) =>
                ({ 3: 'simSwap', 5: 'changeNumber', 7: 'termination' }[kind] || 'takeover');
            const secureOpTitle = (kind, phase) =>
                t(`telecom.swal.secureOp.${secureOpKindKey(kind)}.${phase}`);

            const hubPermissions = Vue.computed(() => {
                const roles = StorageManager.getUserRoles() || [];
                const perms = StorageManager.getPermissions?.() || [];
                const canTransferOwnership =
                    StorageManager.hasAnyPermission?.(perms, ['telecom.line.transfer_ownership']) ||
                    roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice'].includes(x));
                const canApproveSimSwap =
                    StorageManager.hasAnyPermission?.(perms, ['telecom.line.simswap_approve']) ||
                    roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice'].includes(x));
                const canApproveChangeNumber =
                    StorageManager.hasAnyPermission?.(perms, ['telecom.line.change_number_approve']) ||
                    roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice'].includes(x));
                const canApproveTermination =
                    StorageManager.hasAnyPermission?.(perms, ['telecom.line.termination_approve']) ||
                    roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice'].includes(x));
                const canApproveSuspension =
                    StorageManager.hasAnyPermission?.(perms, ['telecom.line.suspension_approve']) ||
                    roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice'].includes(x));
                const canApproveReconnect =
                    StorageManager.hasAnyPermission?.(perms, ['telecom.line.reconnect_approve']) ||
                    roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice'].includes(x));
                const canApproveRefund =
                    StorageManager.hasAnyPermission?.(perms, ['telecom.line.refund_approve']) ||
                    roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice'].includes(x));
                const canApproveCollection =
                    StorageManager.hasAnyPermission?.(perms, ['telecom.line.collection_approve']) ||
                    roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice'].includes(x));
                return {
                    canCreateOps: roles.some((x) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(x)),
                    canConfirmCbs: roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice', 'TelecomShowroom'].includes(x)),
                    canApproveTakeOver: canTransferOwnership,
                    canApproveSimSwap,
                    canApproveChangeNumber,
                    canApproveTermination,
                    canApproveSuspension,
                    canApproveReconnect,
                    canApproveRefund,
                    canApproveCollection,
                    canSeeBillingLog: roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice', 'TelecomManagement'].includes(x)),
                    showRetailShortcuts: roles.some((x) => !TELECOM_ROLE_NAMES.has(x)),
                    canOpenSubscriberRegistry: roles.some((x) => SUBSCRIBER_REGISTRY_ROLES.has(x)),
                    canMutatePrimaryLine: roles.some((x) =>
                        ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(x)),
                    canRecharge:
                        StorageManager.hasAnyPermission?.(perms, ['telecom.line.recharge']) ||
                        roles.some((x) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(x)),
                };
            });

            const state = Vue.reactive({
                kpis: { arpuDemo: 0, churnPercentDemo: 0, branchHeat: [] },
                operations: [],
                searchResults: [],
                searchTerm: '',
                searchBusy: false,
                hasSearched: false,
                rechargeBusy: false,
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
                    primaryCustomerKind: '',
                    activationLineTypeId: '',
                    activateMsisdnSearchTerm: '',
                    activateMsisdnPoolRows: [],
                    activateMsisdnPoolBusy: false,
                    activateMsisdnLabel: '',
                    activateMsisdnCategoryTab: 'all',
                    activateMsisdnModalPrefix: '',
                    activateMsisdnModalOpen: false,
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
                    kycDocumentReferenceId: '',
                    kycUploadFileName: '',
                    kycUploadProgress: 0,
                    kycUploadLocked: false,
                    kycUploadBusy: false,
                    kycUploadError: '',
                    kycDropHover: false,
                    kycThumbnailUrl: '',
                    submitBusy: false,
                    uploadBusy: false,
                    migrationTargetProductId: '',
                    offerDetail: null,
                    offerDetailBusy: false,
                    targetOfferOtherText: '',
                    customerId: '',
                    simIccid: '',
                    cgtTargets: [],
                    cgtTargetsBusy: false,
                    cgtCurrentTypeLabel: '',
                    cgtTargetTypeId: '',
                    cgtMigrationReason: '',
                    cgtMigrationPath: 'Standard',
                    cgtSourceTypeCode: '',
                    cgtOffers: [],
                    cgtOffersBusy: false,
                    cgtProductOfferingId: '',
                    bdrSupervisorConfirmed: false,
                    bssPaymentReference: '',
                    bssSecurityTicketId: '',
                    bssDocumentNumber: '',
                    bssRegulatoryFile: null,
                    bssIdentityFile: null,
                    bssKycDocumentReferenceId: '',
                    bssOriginalTransactionRef: '',
                    bssPayoutDestination: '',
                    bssSupervisorConfirmed: false,
                    bssEffectiveMode: 'immediate',
                    bssEffectiveDateLocal: '',
                    mgrProrationPreview: null,
                    mgrProrationBusy: false,
                    primaryOutstandingBalance: null,
                    tkoTransferReason: '',
                    tkoDepositPolicy: 1,
                    simReplacementReason: '',
                    simLostOrStolen: false,
                    devInventoryId: '',
                    devSaleType: 'Cash',
                    devInstallmentPlanId: '',
                    devDevices: [],
                    devPlans: [],
                    devFinancingPreview: '',
                    devDownPayment: '',
                    devPaymentReference: '',
                    devPaymentChannel: 0,
                    devRequiresFinance: false,
                    primaryMsisdn: '',
                    selectedVasCode: '',
                    vasAction: 'Activate',
                    vasCatalog: [],
                    vasCatalogBusy: false,
                    vasActivated: false,
                    supportIssueType: 0,
                    supportNotes: '',
                    supportTicketCreated: false,
                    supportTicketNumber: '',
                    supportTicketId: '',
                    activationChannel: 0,
                    dealerCode: '',
                    paymentReference: '',
                    paymentAmount: '',
                    paymentChannel: 0,
                    paymentRecorded: false,
                    paymentBusy: false,
                    paymentCashierLocked: false,
                    cashierFetchBusy: false,
                    confirmBusy: false,
                    confirmed: false,
                    confirmStatusHint: '',
                    confirmScheduled: false,
                    operationCorrelationId: '',
                    falloutTicketId: '',
                    falloutTicketNumber: '',
                },
                customerWizard: {
                    visible: false,
                    step: 1,
                    searchNationalId: '',
                    searchPhone: '',
                    searchBusy: false,
                    searchAttempted: false,
                    searchResults: [],
                    lookupsLoaded: false,
                    groupOptions: [],
                    categoryOptions: [],
                    cityOptions: [],
                    cityId: '',
                    subscriberType: 0,
                    name: '',
                    nationalId: '',
                    commercialRegistration: '',
                    taxNumber: '',
                    authorizedSignatory: '',
                    dateOfBirth: '',
                    description: '',
                    customerGroupId: '',
                    customerCategoryId: '',
                    street: '',
                    city: '',
                    addressState: '',
                    zipCode: '',
                    country: defaultCountryLabel(),
                    phoneNumber: '',
                    emailAddress: '',
                    nationality: '',
                    gender: '',
                    occupation: '',
                    faxNumber: '',
                    website: '',
                    whatsApp: '',
                    linkedIn: '',
                    facebook: '',
                    instagram: '',
                    twitterX: '',
                    tikTok: '',
                    contactRows: [
                        { name: '', jobTitle: '', phoneNumber: '', emailAddress: '', description: '' },
                        { name: '', jobTitle: '', phoneNumber: '', emailAddress: '', description: '' },
                        { name: '', jobTitle: '', phoneNumber: '', emailAddress: '', description: '' },
                    ],
                    errors: {},
                    saveBusy: false,
                    handoffBusy: false,
                    savedCustomerId: '',
                    savedCustomerNumber: '',
                    savedCustomerName: '',
                    savedSubscriberProfileId: '',
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
                telecomLineTypesBusy: false,
                huaweiCbsBusy: false,
                huaweiCbsData: null,
                hlrLiveBusy: false,
                hlrResyncBusy: false,
                hlrLiveData: null,
                takeOverApproval: {
                    visible: false,
                    kind: 0,
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

            const productDisplayName = (p) => {
                if (!p) return '';
                const ar = (p.name || '').trim();
                const en = (p.nameEn || '').trim();
                return contentLang.value === 'ar' ? ar || en : en || ar;
            };

            const kindToApiEnum = () => {
                const k = state.wizard.kind;
                if (k === 'activate') return 0;
                if (k === 'migrate') return 1;
                if (k === 'takeover') return 2;
                if (k === 'simswap') return 3;
                if (k === 'addpackage') return 4;
                if (k === 'changeGsm') return 6;
                if (k === 'changeNumber') return 5;
                if (k === 'termination') return 7;
                if (k === 'suspension') return 8;
                if (k === 'reconnect') return 9;
                if (k === 'deviceSale') return 10;
                if (k === 'refund') return 11;
                if (k === 'badDebt') return 12;
                return 0;
            };

            const isPremiumMsisdnCategory = (cat) => [1, 2, 3, 'Silver', 'Gold', 'Platinum'].includes(cat);

            const wizardTitle = Vue.computed(() => {
                const k = state.wizard.kind;
                if (!k) return '';
                return t('telecom.wizard.titles.' + k);
            });

            const isCorporateKind = (kind) => {
                const k = String(kind ?? '').toLowerCase();
                return k === 'corporate' || k === '1';
            };

            const isCorporatePrimary = Vue.computed(() => isCorporateKind(state.wizard.primaryCustomerKind));

            const MSISDN_CATEGORY_TABS = [
                { id: 'all', i18n: 'all' },
                { id: 'Normal', i18n: 'normal' },
                { id: 'Silver', i18n: 'silver' },
                { id: 'Gold', i18n: 'gold' },
                { id: 'Platinum', i18n: 'platinum' },
            ];

            const parseMsisdnPoolRows = (res) => {
                if (typeof StorageManager?.apiList === 'function') {
                    const list = StorageManager.apiList(res);
                    if (Array.isArray(list) && list.length > 0) return list;
                }
                const content = res?.data?.content ?? res?.data?.Content;
                const rows = content?.data ?? content?.Data;
                return Array.isArray(rows) ? rows : [];
            };

            const isAvailableMsisdnPoolRow = (row) => {
                if (!row) return false;
                const name = String(row.poolStatusName ?? row.PoolStatusName ?? '').toLowerCase();
                if (name === 'available') return true;
                const st = row.poolStatus ?? row.PoolStatus;
                return st === 0 || st === '0' || st === 'Available';
            };

            const resolveMsisdnCategory = (row) => {
                const cat = row?.category ?? row?.Category;
                if (typeof cat === 'string' && cat) return cat;
                const names = ['Normal', 'Silver', 'Gold', 'Platinum'];
                const n = Number(cat);
                return names[n] ?? 'Normal';
            };

            const msisdnPrefixDigits = (term) =>
                String(term || '')
                    .replace(/[\u0660-\u0669\u06F0-\u06F9]/g, (ch) => {
                        const code = ch.charCodeAt(0);
                        if (code >= 0x0660 && code <= 0x0669) return String(code - 0x0660);
                        return String(code - 0x06f0);
                    })
                    .replace(/\D/g, '');

            const activationLineTypeOptions = Vue.computed(() =>
                (state.telecomLineTypes || [])
                    .filter((x) => x.isActive !== false && x.IsActive !== false)
                    .map((x) => ({
                        ...x,
                        id: lineTypeRowId(x),
                        code: x.code || x.Code || '',
                        nameAr: x.nameAr || x.NameAr || '',
                        nameEn: x.nameEn || x.NameEn || '',
                        sortOrder: x.sortOrder ?? x.SortOrder ?? 0,
                        isDefault: x.isDefault ?? x.IsDefault ?? false,
                    }))
                    .filter((x) => x.id)
                    .sort((a, b) => a.sortOrder - b.sortOrder)
            );

            const defaultTelecomLineTypeId = () => {
                const rows = activationLineTypeOptions.value;
                const def = rows.find((x) => x.isDefault);
                return def?.id || rows[0]?.id || '';
            };

            const activationLineTypeLabel = Vue.computed(() => {
                const id = (state.wizard.activationLineTypeId || '').trim();
                if (!id) return '';
                const row = activationLineTypeOptions.value.find((x) => x.id === id);
                return row ? lineTypeDisplayName(row) : '';
            });

            const canPickActivateMsisdn = Vue.computed(
                () => !!(state.wizard.activationLineTypeId || '').trim()
            );

            const lineTypeDisplayName = (row) => {
                if (!row) return '—';
                const ar = row.nameAr || row.NameAr || '';
                const en = row.nameEn || row.NameEn || '';
                if (locale.value === 'ar' && ar) return ar;
                if (en) return en;
                return ar || en || row.code || row.Code || '—';
            };

            const msisdnMatchesActivationLineType = (row) => {
                const lineTypeId = (state.wizard.activationLineTypeId || '').trim();
                if (!lineTypeId) return false;
                const compat = row?.compatibleSubscriptionTypeId ?? row?.CompatibleSubscriptionTypeId ?? '';
                return !compat || compat === lineTypeId;
            };

            const filteredActivateMsisdnPool = Vue.computed(() => {
                const tab = state.wizard.activateMsisdnCategoryTab || 'all';
                const prefix = msisdnPrefixDigits(state.wizard.activateMsisdnModalPrefix);
                let rows = state.wizard.activateMsisdnPoolRows || [];
                if (state.wizard.kind === 'activate' && (state.wizard.activationLineTypeId || '').trim()) {
                    rows = rows.filter(msisdnMatchesActivationLineType);
                }
                if (prefix.length >= 2) {
                    rows = rows.filter((r) => {
                        const m = String(r.msisdn ?? r.Msisdn ?? '');
                        return m.startsWith(prefix) || m.includes(prefix);
                    });
                }
                if (tab !== 'all') {
                    rows = rows.filter((r) => resolveMsisdnCategory(r) === tab);
                }
                return rows;
            });

            const activateMsisdnPoolDisplay = Vue.computed(() => filteredActivateMsisdnPool.value.slice(0, 100));

            const activateMsisdnPoolTruncated = Vue.computed(
                () => filteredActivateMsisdnPool.value.length > 100
            );

            const activateMsisdnPoolUnfilteredCount = Vue.computed(() => {
                const tab = state.wizard.activateMsisdnCategoryTab || 'all';
                const prefix = msisdnPrefixDigits(state.wizard.activateMsisdnModalPrefix);
                let rows = state.wizard.activateMsisdnPoolRows || [];
                if (prefix.length >= 2) {
                    rows = rows.filter((r) => {
                        const m = String(r.msisdn ?? r.Msisdn ?? '');
                        return m.startsWith(prefix) || m.includes(prefix);
                    });
                }
                if (tab !== 'all') {
                    rows = rows.filter((r) => resolveMsisdnCategory(r) === tab);
                }
                return rows.length;
            });

            const activateMsisdnPoolEmptyDueToLineType = Vue.computed(
                () =>
                    state.wizard.kind === 'activate' &&
                    !!(state.wizard.activationLineTypeId || '').trim() &&
                    !state.wizard.activateMsisdnPoolBusy &&
                    filteredActivateMsisdnPool.value.length === 0 &&
                    activateMsisdnPoolUnfilteredCount.value > 0
            );

            const msisdnCategoryLabel = (cat) => t(`telecom.wizard.msisdnPicker.categories.${String(cat).toLowerCase()}`, cat);

            const msisdnCategoryBadgeClass = (cat) => {
                const c = String(cat || 'Normal');
                if (c === 'Platinum') return 'msisdn-tier-platinum';
                if (c === 'Gold') return 'msisdn-tier-gold';
                if (c === 'Silver') return 'msisdn-tier-silver';
                return 'msisdn-tier-normal';
            };

            const needsActivateMsisdn = Vue.computed(() => state.wizard.kind === 'activate');

            const needsSecondaryParty = Vue.computed(() => {
                const k = state.wizard.kind;
                if (k === 'takeover' || k === 'migrate') return true;
                if (k === 'activate' && isCorporatePrimary.value) return true;
                return false;
            });

            const secondaryRequired = Vue.computed(() => {
                const k = state.wizard.kind;
                if (k === 'takeover') return true;
                if (k === 'activate' && isCorporatePrimary.value) return true;
                return false;
            });

            const buildMigrateTargetOfferPayload = () => {
                const sel = (state.wizard.migrationTargetProductId || '').trim();
                if (!sel) return null;
                const row = state.migrationEligibleProducts.find((x) => x.id === sel);
                if (row && row.name) return String(row.name).trim();
                return null;
            };

            const activateRequiredDeposit = Vue.computed(() => {
                if (state.wizard.kind !== 'activate') return 0;
                const fromDetail = offerDefaultMonthlyPrice(state.wizard.offerDetail);
                if (fromDetail != null) return fromDetail;
                const sel = (state.wizard.migrationTargetProductId || '').trim();
                if (!sel) return 0;
                const row = state.migrationEligibleProducts.find((x) => x.id === sel);
                return Number(row?.unitPrice ?? row?.UnitPrice ?? 0) || 0;
            });

            const wizardOfferDisplayName = Vue.computed(() =>
                offerDetailDisplayName(state.wizard.offerDetail, locale.value)
            );

            const wizardOfferMonthlyPrice = Vue.computed(() => {
                const p = offerDefaultMonthlyPrice(state.wizard.offerDetail);
                return p != null ? formatMoneyIntl(p, locale.value) : null;
            });

            const wizardOfferVoiceLine = Vue.computed(() => {
                const d = state.wizard.offerDetail;
                if (!d) return '';
                const c = offerComponentByType(d, 0);
                if (c) return formatOfferQuotaLine(c, locale.value);
                if (d.voiceMinutesLimit) {
                    return locale.value === 'ar'
                        ? `${d.voiceMinutesLimit} دقيقة`
                        : `${d.voiceMinutesLimit} min`;
                }
                return '';
            });

            const wizardOfferDataLine = Vue.computed(() => {
                const d = state.wizard.offerDetail;
                if (!d) return '';
                const c = offerComponentByType(d, 1);
                if (c) return formatOfferQuotaLine(c, locale.value);
                if (d.speedQuotaLimitGb) {
                    return locale.value === 'ar'
                        ? `${d.speedQuotaLimitGb} جيجا`
                        : `${d.speedQuotaLimitGb} GB`;
                }
                return '';
            });

            const wizardOfferSmsLine = Vue.computed(() => {
                const d = state.wizard.offerDetail;
                if (!d) return '';
                const c = offerComponentByType(d, 2);
                return c ? formatOfferQuotaLine(c, locale.value) : '';
            });

            const wizardOfferSummaryText = Vue.computed(() => {
                locale.value;
                return offerDetailSummaryText(state.wizard.offerDetail, locale.value);
            });

            const hubBssEffectiveToday = () =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.effectiveDate.todayLocal()
                    : new Date().toISOString().slice(0, 10);

            const wizardSupportsEffectiveDate = (kind) =>
                typeof TelecomBssWizardClearance !== 'undefined'
                && TelecomBssWizardClearance.effectiveDate.supportsWizardKind(kind);

            const hubSuspensionStartDateLocal = () => {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    return TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard).slice(0, 10);
                }
                return hubBssEffectiveToday();
            };

            const hubSuspensionMaxEndDateLocal = () =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.suspensionEndDate.maxEndDateLocal(hubSuspensionStartDateLocal())
                    : (() => {
                        const d = new Date();
                        d.setDate(d.getDate() + 90);
                        return d.toISOString().slice(0, 10);
                    })();

            const mgrProrationPriceDifferenceLabel = Vue.computed(() => {
                const p = state.wizard.mgrProrationPreview;
                if (!p || typeof TelecomBssWizardClearance === 'undefined') return '—';
                return TelecomBssWizardClearance.migration.formatAmount(
                    p.priceDifference ?? p.PriceDifference,
                    p.currencyCode ?? p.CurrencyCode
                );
            });
            const mgrProrationAmountLabel = Vue.computed(() => {
                const p = state.wizard.mgrProrationPreview;
                if (!p || typeof TelecomBssWizardClearance === 'undefined') return '—';
                return TelecomBssWizardClearance.migration.formatAmount(
                    p.proratedAmount ?? p.ProratedAmount,
                    p.currencyCode ?? p.CurrencyCode
                );
            });
            const mgrProrationWalletLabel = Vue.computed(() => {
                const p = state.wizard.mgrProrationPreview;
                if (!p || typeof TelecomBssWizardClearance === 'undefined') return '—';
                return TelecomBssWizardClearance.migration.formatAmount(
                    p.walletBalance ?? p.WalletBalance,
                    p.currencyCode ?? p.CurrencyCode
                );
            });
            const mgrProrationDaysLabel = Vue.computed(() => {
                const p = state.wizard.mgrProrationPreview;
                if (!p) return '';
                const days = p.daysRemainingInCycle ?? p.DaysRemainingInCycle ?? 0;
                const total = p.daysInBillingCycle ?? p.DaysInBillingCycle ?? 0;
                return t('telecom.wizard.migrationOffers.daysRemaining', 'Days')
                    .replace('{days}', days)
                    .replace('{total}', total);
            });
            const mgrProrationSufficient = Vue.computed(
                () =>
                    state.wizard.mgrProrationPreview?.sufficientBalance ??
                    state.wizard.mgrProrationPreview?.SufficientBalance !== false
            );

            const activateSimIccidLocked = Vue.computed(() => state.wizard.kind === 'activate');

            const loadWizardOfferDetail = async (offerId) => {
                const id = (offerId || '').trim();
                if (!id) {
                    state.wizard.offerDetail = null;
                    state.wizard.offerDetailBusy = false;
                    return;
                }
                state.wizard.offerDetailBusy = true;
                try {
                    const res = await AxiosManager.get(
                        `/ProductOffering/GetProductOfferingSingle?id=${encodeURIComponent(id)}`,
                        {}
                    );
                    state.wizard.offerDetail = parseProductOfferingDetail(res);
                } catch {
                    state.wizard.offerDetail = null;
                } finally {
                    state.wizard.offerDetailBusy = false;
                }
            };

            const onWizardOfferChanged = async () => {
                await loadWizardOfferDetail(state.wizard.migrationTargetProductId);
                if (state.wizard.kind === 'migrate' && typeof TelecomBssWizardClearance !== 'undefined') {
                    const sid = (state.wizard.primarySubscriberProfileId || '').trim();
                    const aid = (state.wizard.primaryMsisdnAssetId || '').trim();
                    const oid = (state.wizard.migrationTargetProductId || '').trim();
                    if (!sid || !aid || !oid) {
                        state.wizard.mgrProrationPreview = null;
                        return;
                    }
                    state.wizard.mgrProrationBusy = true;
                    try {
                        state.wizard.mgrProrationPreview = await TelecomBssWizardClearance.migration.loadPreview(
                            sid,
                            aid,
                            oid
                        );
                    } catch {
                        state.wizard.mgrProrationPreview = null;
                    } finally {
                        state.wizard.mgrProrationBusy = false;
                    }
                }
            };

            const wizardAwaitBackOffice = Vue.computed(() => {
                const k = state.wizard.kind;
                if (k === 'takeover') return true;
                if (k === 'simswap' && state.wizard.simLostOrStolen) return true;
                if (k === 'changeNumber' && state.wizard.cnRequiresBackOffice) return true;
                if (k === 'termination' && state.wizard.trmRequiresBackOffice) return true;
                if (k === 'suspension' && state.wizard.susRequiresBackOffice) return true;
                if (k === 'reconnect' && state.wizard.rcnRequiresBackOffice) return true;
                if (k === 'refund' && state.wizard.rfdRequiresBackOffice) return true;
                if (k === 'badDebt' && state.wizard.bdrRequiresBackOffice) return true;
                if (k === 'deviceSale' && state.wizard.devRequiresFinance) return true;
                return false;
            });

            const susRequiresStep2Identity = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.suspension.requiresIdentityUpload(state.wizard.susSuspensionType)
                    : ['Fraud', 'Regulatory'].includes(state.wizard.susSuspensionType));

            const wizardNeedsIdentityUpload = Vue.computed(() => {
                const k = state.wizard.kind;
                return (
                    k === 'takeover'
                    || (k === 'simswap' && state.wizard.simLostOrStolen)
                    || (k === 'changeNumber' && state.wizard.cnRequiresBackOffice)
                    || (k === 'termination' && state.wizard.trmRequiresBackOffice)
                    || (k === 'refund' && state.wizard.rfdRequiresBackOffice)
                    || (k === 'suspension' && susRequiresStep2Identity.value && state.wizard.susRequiresBackOffice)
                    || (k === 'badDebt' && state.wizard.bdrRequiresBackOffice)
                );
            });

            const wizardShowsConfirmCbs = Vue.computed(() => {
                if (!state.wizard.createdOperationId || !state.wizard.documentMarkedUploaded) return false;
                if (state.wizard.confirmed) return false;
                if (wizardAwaitBackOffice.value) return false;
                if (state.wizard.kind === 'addpackage' || state.wizard.kind === 'support') return false;
                if (
                    state.wizard.kind === 'activate'
                    && activateRequiredDeposit.value > 0
                    && !state.wizard.paymentRecorded
                ) {
                    return false;
                }
                if (
                    state.wizard.kind === 'deviceSale'
                    && state.wizard.devSaleType === 'Installment'
                    && !state.wizard.documentMarkedUploaded
                ) {
                    return false;
                }
                return true;
            });

            const canWizardGoToStep3 = Vue.computed(() => {
                if (state.wizard.kind === 'addpackage') {
                    return !!state.wizard.vasActivated;
                }
                if (state.wizard.kind === 'support') {
                    return !!state.wizard.supportTicketCreated;
                }
                if (!state.wizard.createdOperationId || !state.wizard.documentMarkedUploaded) return false;
                if (state.wizard.kind === 'changeGsm') {
                    return !!state.wizard.confirmed;
                }
                if (state.wizard.kind === 'activate') {
                    if (!state.wizard.kycDocumentReferenceId) return false;
                    if (activateRequiredDeposit.value > 0 && !state.wizard.paymentRecorded) return false;
                    return !!state.wizard.confirmed;
                }
                if (wizardAwaitBackOffice.value) return true;
                return !!state.wizard.confirmed;
            });

            const rowKey = (r) => `${r.resultType}:${r.id}`;

            const resetWizardState = () => {
                state.wizard.step = 1;
                state.wizard.primarySearchTerm = '';
                state.wizard.primaryResults = [];
                state.wizard.primaryBusy = false;
                state.wizard.primarySubscriberProfileId = '';
                state.wizard.primaryMsisdnAssetId = '';
                state.wizard.primaryMsisdn = '';
                state.wizard.primaryLabel = '';
                state.wizard.primaryRowKey = '';
                state.wizard.primaryCustomerKind = '';
                state.wizard.activationLineTypeId = '';
                state.wizard.activateMsisdnSearchTerm = '';
                state.wizard.activateMsisdnPoolRows = [];
                state.wizard.activateMsisdnPoolBusy = false;
                state.wizard.activateMsisdnLabel = '';
                state.wizard.activateMsisdnCategoryTab = 'all';
                state.wizard.activateMsisdnModalPrefix = '';
                state.wizard.activateMsisdnModalOpen = false;
                unlockActivateMsisdnBodyScroll();
                state.wizard.selectedVasCode = '';
                state.wizard.vasCatalog = [];
                state.wizard.vasCatalogBusy = false;
                state.wizard.vasActivated = false;
                state.wizard.supportIssueType = 0;
                state.wizard.supportNotes = '';
                state.wizard.supportTicketCreated = false;
                state.wizard.supportTicketNumber = '';
                state.wizard.supportTicketId = '';
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
                state.wizard.kycDocumentReferenceId = '';
                state.wizard.kycUploadFileName = '';
                state.wizard.kycUploadProgress = 0;
                state.wizard.kycUploadLocked = false;
                state.wizard.kycUploadBusy = false;
                state.wizard.kycUploadError = '';
                state.wizard.kycDropHover = false;
                if (state.wizard.kycThumbnailUrl) {
                    URL.revokeObjectURL(state.wizard.kycThumbnailUrl);
                }
                state.wizard.kycThumbnailUrl = '';
                state.wizard.submitBusy = false;
                state.wizard.uploadBusy = false;
                state.wizard.migrationTargetProductId = '';
                state.wizard.offerDetail = null;
                state.wizard.offerDetailBusy = false;
                state.wizard.targetOfferOtherText = '';
                state.wizard.customerId = '';
                state.wizard.simIccid = '';
                state.wizard.simReplacementReason = '';
                state.wizard.simLostOrStolen = false;
                state.wizard.cnTargetMsisdnAssetId = '';
                state.wizard.cnNumberChangeReason = '';
                state.wizard.cnPremiumFeeAmount = '';
                state.wizard.cnChangeMode = 'Internal';
                state.wizard.cnPortInMsisdn = '';
                state.wizard.cnDonorOperatorCode = '';
                state.wizard.cnPortInReference = '';
                state.wizard.cnPoolNumbers = [];
                state.wizard.cnPoolBusy = false;
                state.wizard.cnRequiresBackOffice = false;
                state.wizard.cnCurrentMsisdn = '';
                state.wizard.trmTerminationType = 'Voluntary';
                state.wizard.trmTerminationReason = '';
                state.wizard.trmRetentionOfferOutcome = 'Declined';
                state.wizard.trmRequiresBackOffice = false;
                state.wizard.susSuspensionType = 'CustomerRequest';
                state.wizard.susSuspensionReason = '';
                state.wizard.susBarringLevel = 'Full';
                state.wizard.susAutoReconnectEnabled = false;
                state.wizard.susEndDateLocal = '';
                state.wizard.susRequiresBackOffice = false;
                state.wizard.rcnClearanceType = 'Customer';
                state.wizard.rcnReconnectReason = '';
                state.wizard.rcnPaymentReference = '';
                state.wizard.rcnSecurityTicketId = '';
                state.wizard.rcnDocumentNumber = '';
                state.wizard.rcnRegulatoryFile = null;
                state.wizard.rcnKycDocumentReferenceId = '';
                state.wizard.rcnFraudClearanceConfirmed = false;
                state.wizard.rcnRequiresBackOffice = false;
                state.wizard.rcnEligibilityBusy = false;
                state.wizard.rcnEligibility = null;
                state.wizard.rcnBdrApproved = false;
                state.wizard.rfdRefundType = 'Deposit';
                state.wizard.rfdRefundMethod = 'CreditNote';
                state.wizard.rfdRefundAmount = '';
                state.wizard.rfdRefundReason = '';
                state.wizard.rfdDepositSnapshot = null;
                state.wizard.rfdWalletSnapshot = null;
                state.wizard.rfdRequiresBackOffice = false;
                state.wizard.bssPaymentReference = '';
                state.wizard.bssSecurityTicketId = '';
                state.wizard.bssDocumentNumber = '';
                state.wizard.bssRegulatoryFile = null;
                state.wizard.bssIdentityFile = null;
                state.wizard.bssKycDocumentReferenceId = '';
                state.wizard.bssOriginalTransactionRef = '';
                state.wizard.bssPayoutDestination = '';
                state.wizard.primaryOutstandingBalance = null;
                state.wizard.cgtMigrationPath = 'Standard';
                state.wizard.cgtSourceTypeCode = '';
                state.wizard.bdrCollectionAction = 'PaymentRecorded';
                state.wizard.bdrDunningStage = 'Reminder1';
                state.wizard.bdrCollectedAmount = '';
                state.wizard.bdrWriteOffAmount = '';
                state.wizard.bdrPaymentReference = '';
                state.wizard.bdrAgencyReference = '';
                state.wizard.bdrPaymentPlanMonths = '';
                state.wizard.bdrSupervisorConfirmed = false;
                state.wizard.bdrRequiresBackOffice = false;
                state.wizard.bdrEligibilityBusy = false;
                state.wizard.bdrEligibility = null;
                state.wizard.activationChannel = 0;
                state.wizard.dealerCode = '';
                if (typeof ActivationChannelUi !== 'undefined') {
                    ActivationChannelUi.applyDefaults(state.wizard);
                }
                state.wizard.paymentReference = '';
                state.wizard.paymentAmount = '';
                state.wizard.paymentChannel = 0;
                state.wizard.paymentRecorded = false;
                state.wizard.paymentBusy = false;
                state.wizard.paymentCashierLocked = false;
                state.wizard.cashierFetchBusy = false;
                state.wizard.confirmBusy = false;
                state.wizard.confirmed = false;
                state.wizard.confirmStatusHint = '';
                state.wizard.confirmScheduled = false;
                state.wizard.operationCorrelationId = '';
                state.wizard.falloutTicketId = '';
                state.wizard.falloutTicketNumber = '';
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

            const statusBadge = (status) =>
                window.TelecomUiBadges?.operationStatusBootstrapClass?.(status)
                ?? (() => {
                    const s = Number(status);
                    if (s === 3) return 'bg-success';
                    if (s === 6) return 'bg-primary';
                    if (s === 5) return 'bg-info text-dark';
                    if (s === 2) return 'bg-warning text-dark';
                    if (s === 4) return 'bg-danger';
                    if (s === 11) return 'bg-info text-dark telecom-op-status-scheduled';
                    if (s === 1) return 'bg-secondary';
                    return 'bg-secondary';
                })();

            const operationConfirmable = (o) =>
                Number(o?.documentStatus) >= 1 && [0, 2, 5].includes(Number(o?.status));

            const kindLabel = (kind) => {
                const n = Number(kind);
                if (n === 4) return t('telecom.ops.kindLabels.serviceModification');
                const map = {
                    0: 'activate',
                    1: 'migrate',
                    2: 'takeover',
                    3: 'simswap',
                    5: 'changeNumber',
                    6: 'changeGsm',
                    7: 'termination',
                    8: 'suspension',
                    9: 'reconnect',
                    10: 'deviceSale',
                    11: 'refund',
                    12: 'badDebt',
                };
                const id = map[n];
                return id ? t('telecom.ops.kindLabels.' + id) : String(kind ?? '—');
            };

            const statusLabel = (status) => {
                const s = Number(status);
                const key = 'telecom.ops.statusLabels.' + s;
                const lbl = t(key);
                return lbl !== key ? lbl : String(status ?? '—');
            };

            const pickOpEffectiveUtc = (op) =>
                window.TelecomUiBadges?.pickScheduledEffectiveDate?.(op) ??
                op?.scheduledEffectiveDateUtc ??
                op?.ScheduledEffectiveDateUtc ??
                null;

            const formatOpEffectiveDate = (op) => {
                const utc = pickOpEffectiveUtc(op);
                if (!utc) return '';
                return (
                    window.TelecomUiBadges?.formatScheduledEffectiveDate?.(utc, locale.value) ||
                    formatDateIntl(utc, locale.value)
                );
            };

            const isScheduledOp = (op) =>
                Number(op?.status) === 11
                || window.TelecomUiBadges?.isScheduledOperationStatus?.(op?.status);

            const isTakeOverPendingReview = (o) => Number(o?.kind) === 2 && Number(o?.status) === 5;

            const isSimSwapPendingReview = (o) =>
                Number(o?.kind) === 3 && Number(o?.status) === 5 && !!o?.isLostOrStolenReport;

            const isChangeNumberPremiumPending = (o) =>
                Number(o?.kind) === 5
                && Number(o?.status) === 5
                && String(o?.approvalLevelRequired || '').toLowerCase() === 'backoffice';

            const isTerminationBoPending = (o) =>
                Number(o?.kind) === 7
                && Number(o?.status) === 5
                && String(o?.approvalLevelRequired || '').toLowerCase() === 'backoffice';

            const isSuspensionBoPending = (o) =>
                Number(o?.kind) === 8
                && Number(o?.status) === 5
                && String(o?.approvalLevelRequired || '').toLowerCase() === 'backoffice';

            const isReconnectBoPending = (o) =>
                Number(o?.kind) === 9
                && Number(o?.status) === 5
                && String(o?.approvalLevelRequired || '').toLowerCase() === 'backoffice';

            const isRefundBoPending = (o) =>
                Number(o?.kind) === 11
                && Number(o?.status) === 5
                && String(o?.approvalLevelRequired || '').toLowerCase() === 'backoffice';

            const isBadDebtBoPending = (o) =>
                Number(o?.kind) === 12
                && Number(o?.status) === 5
                && String(o?.approvalLevelRequired || '').toLowerCase() === 'backoffice';

            const isSecureOpPendingReview = (o) =>
                isTakeOverPendingReview(o)
                || isSimSwapPendingReview(o)
                || isChangeNumberPremiumPending(o)
                || isTerminationBoPending(o)
                || isSuspensionBoPending(o)
                || isReconnectBoPending(o)
                || isRefundBoPending(o)
                || isBadDebtBoPending(o);

            const operationKpis = Vue.computed(() => {
                const ops = state.operations || [];
                let pending = 0;
                let completed = 0;
                let failed = 0;
                for (const o of ops) {
                    const s = Number(o.status ?? o.Status ?? -1);
                    if (s === 3 || s === 1) completed++;
                    else if (s === 4) failed++;
                    else if ([0, 2, 5, 6].includes(s)) pending++;
                }
                return { pending, completed, failed };
            });

            const pendingTakeOverCount = Vue.computed(
                () => (state.operations || []).filter((o) => isTakeOverPendingReview(o)).length
            );

            const pendingSimSwapCount = Vue.computed(
                () => (state.operations || []).filter((o) => isSimSwapPendingReview(o)).length
            );

            const pendingChangeNumberCount = Vue.computed(
                () => (state.operations || []).filter((o) => isChangeNumberPremiumPending(o)).length
            );

            const pendingTerminationCount = Vue.computed(
                () => (state.operations || []).filter((o) => isTerminationBoPending(o)).length
            );

            const pendingSuspensionCount = Vue.computed(
                () => (state.operations || []).filter((o) => isSuspensionBoPending(o)).length
            );

            const pendingReconnectCount = Vue.computed(
                () => (state.operations || []).filter((o) => isReconnectBoPending(o)).length
            );

            const pendingRefundCount = Vue.computed(
                () => (state.operations || []).filter((o) => isRefundBoPending(o)).length
            );

            const pendingBadDebtCount = Vue.computed(
                () => (state.operations || []).filter((o) => isBadDebtBoPending(o)).length
            );

            const pendingSecureOpCount = Vue.computed(
                () =>
                    pendingTakeOverCount.value
                    + pendingSimSwapCount.value
                    + pendingChangeNumberCount.value
                    + pendingTerminationCount.value
                    + pendingSuspensionCount.value
                    + pendingReconnectCount.value
                    + pendingRefundCount.value
                    + pendingBadDebtCount.value
            );

            const secureOpApproveLabel = (o) => {
                if (Number(o?.kind) === 3) return t('telecom.ops.approveSimSwap');
                if (Number(o?.kind) === 5) return t('telecom.ops.approveChangeNumber');
                if (Number(o?.kind) === 7) return t('telecom.ops.approveTermination');
                if (Number(o?.kind) === 8) return t('telecom.ops.approveSuspension');
                if (Number(o?.kind) === 9) return t('telecom.ops.approveReconnect');
                if (Number(o?.kind) === 11) return t('telecom.ops.approveRefund');
                if (Number(o?.kind) === 12) return t('telecom.ops.approveCollection');
                return t('telecom.ops.approveTakeOver');
            };

            const canApproveSecureOp = Vue.computed(() => {
                const k = Number(state.takeOverApproval.kind);
                if (k === 3) {
                    return (
                        hubPermissions.value.canApproveSimSwap ||
                        hubPermissions.value.canApproveTakeOver
                    );
                }
                if (k === 5) {
                    return (
                        hubPermissions.value.canApproveChangeNumber ||
                        hubPermissions.value.canApproveSimSwap
                    );
                }
                if (k === 7) {
                    return hubPermissions.value.canApproveTermination;
                }
                if (k === 8) {
                    return hubPermissions.value.canApproveSuspension;
                }
                if (k === 9) {
                    return hubPermissions.value.canApproveReconnect;
                }
                if (k === 11) {
                    return hubPermissions.value.canApproveRefund;
                }
                if (k === 12) {
                    return hubPermissions.value.canApproveCollection;
                }
                return hubPermissions.value.canApproveTakeOver;
            });

            const secureApprovalModalTitle = Vue.computed(() => {
                if (Number(state.takeOverApproval.kind) === 3) {
                    return t('telecom.simSwapModal.title');
                }
                if (Number(state.takeOverApproval.kind) === 5) {
                    return t('telecom.changeNumberModal.title');
                }
                if (Number(state.takeOverApproval.kind) === 7) {
                    return t('telecom.terminationModal.title');
                }
                if (Number(state.takeOverApproval.kind) === 8) {
                    return t('telecom.suspensionModal.title');
                }
                if (Number(state.takeOverApproval.kind) === 9) {
                    return t('telecom.reconnectModal.title');
                }
                if (Number(state.takeOverApproval.kind) === 11) {
                    return t('telecom.refundModal.title');
                }
                if (Number(state.takeOverApproval.kind) === 12) {
                    return t('telecom.badDebtModal.title');
                }
                return t('telecom.takeOverModal.title');
            });

            const secureApprovalApproveLabel = Vue.computed(() => {
                if (Number(state.takeOverApproval.kind) === 3) {
                    return t('telecom.simSwapModal.approve');
                }
                if (Number(state.takeOverApproval.kind) === 5) {
                    return t('telecom.changeNumberModal.approve');
                }
                if (Number(state.takeOverApproval.kind) === 7) {
                    return t('telecom.terminationModal.approve');
                }
                if (Number(state.takeOverApproval.kind) === 8) {
                    return t('telecom.suspensionModal.approve');
                }
                if (Number(state.takeOverApproval.kind) === 9) {
                    return t('telecom.reconnectModal.approve');
                }
                if (Number(state.takeOverApproval.kind) === 11) {
                    return t('telecom.refundModal.approve');
                }
                if (Number(state.takeOverApproval.kind) === 12) {
                    return t('telecom.badDebtModal.approve');
                }
                return t('telecom.takeOverModal.approve');
            });

            const onRefundTypeChange = () => {
                const amt = Number(state.wizard.rfdRefundAmount) || 0;
                const ty = (state.wizard.rfdRefundType || '').trim();
                const method = (state.wizard.rfdRefundMethod || '').trim();
                state.wizard.rfdRequiresBackOffice =
                    ty === 'SyriatelCash' || amt > 500000 || (method === 'Cash' && amt > 500000);
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    TelecomBssWizardClearance.refund.reset(state.wizard, ty, method);
                }
            };

            const onRefundMethodChange = () => {
                onRefundTypeChange();
            };

            const onTerminationTypeChange = () => {
                const ty = (state.wizard.trmTerminationType || '').trim();
                state.wizard.trmRequiresBackOffice =
                    ty === 'Fraud' || ty === 'Regulatory' || ty === 'Collections';
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    TelecomBssWizardClearance.termination.reset(state.wizard, ty);
                }
            };

            const onSusTypeChange = () => {
                const ty = (state.wizard.susSuspensionType || '').trim();
                state.wizard.susRequiresBackOffice = ty === 'Fraud' || ty === 'Regulatory';
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    TelecomBssWizardClearance.suspension.reset(state.wizard, ty);
                }
            };

            const onSusAutoReconnectChange = () => {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    TelecomBssWizardClearance.suspensionEndDate.onAutoReconnectToggled(
                        state.wizard,
                        hubSuspensionStartDateLocal
                    );
                }
            };

            const onCgtPathChange = () => {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    TelecomBssWizardClearance.changeGsm.reset(state.wizard, state.wizard.cgtMigrationPath);
                }
            };

            const onCgtTargetTypeChanged = async () => {
                state.wizard.cgtProductOfferingId = '';
                const sid = (state.wizard.primarySubscriberProfileId || '').trim();
                const targetId = (state.wizard.cgtTargetTypeId || '').trim();
                if (!sid || !targetId) {
                    state.wizard.cgtOffers = [];
                    return;
                }
                state.wizard.cgtOffersBusy = true;
                try {
                    let url =
                        '/Product/GetChangeGsmEligibleProducts?subscriberProfileId=' +
                        encodeURIComponent(sid) +
                        '&targetSubscriptionTypeId=' +
                        encodeURIComponent(targetId);
                    const ms = (state.wizard.primaryMsisdnAssetId || '').trim();
                    if (ms) url += '&msisdnAssetId=' + encodeURIComponent(ms);
                    const res = await AxiosManager.get(url, {});
                    const content = res?.data?.content ?? {};
                    state.wizard.cgtOffers = (content.data || []).map((o) => ({
                        id: o.id,
                        name: o.name,
                        serviceCode: o.serviceCode,
                    }));
                } catch {
                    state.wizard.cgtOffers = [];
                } finally {
                    state.wizard.cgtOffersBusy = false;
                }
            };

            const onBssRegulatoryFileChange = (ev) => {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    TelecomBssWizardClearance.onRegulatoryFileChange(state.wizard, ev);
                }
            };

            const onBssIdentityFileChange = (ev) => {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    TelecomBssWizardClearance.onIdentityFileChange(state.wizard, ev);
                }
            };

            const bssCgtOptions = () => ({
                outstandingBalance: state.wizard.primaryOutstandingBalance,
            });

            const bssTkoOptions = () => ({
                outstandingBalance: state.wizard.primaryOutstandingBalance,
            });

            const loadPrimaryOutstandingBalance = async () => {
                state.wizard.primaryOutstandingBalance = null;
                const cid = resolveWizardCustomerId(state.wizard);
                const needle = String(state.wizard.primaryMsisdn || state.wizard.primaryLabel || '')
                    .replace(/\D/g, '');
                if (!cid || needle.length < 6) return;
                try {
                    const res = await AxiosManager.get(
                        '/Customer/GetCustomer360LineWallets?customerId=' + encodeURIComponent(cid),
                        {}
                    );
                    const map =
                        res?.data?.content?.walletsBySubscriptionId
                        ?? res?.data?.content?.WalletsBySubscriptionId
                        ?? {};
                    for (const w of Object.values(map)) {
                        const m = String(w?.msisdn ?? w?.Msisdn ?? '').replace(/\D/g, '');
                        if (!m || !needle.endsWith(m.slice(-Math.min(9, m.length)))) continue;
                        const raw = w?.outstandingBalance ?? w?.OutstandingBalance;
                        state.wizard.primaryOutstandingBalance =
                            raw === undefined || raw === null || raw === '' ? null : Number(raw);
                        return;
                    }
                } catch {
                    state.wizard.primaryOutstandingBalance = null;
                }
            };

            const onSimLostOrStolenChange = () => {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    TelecomBssWizardClearance.simSwap.reset(state.wizard, state.wizard.simLostOrStolen);
                }
            };

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
                if (window.TelecomUiBadges?.pipelineStepsForOperationStatus) {
                    return window.TelecomUiBadges.pipelineStepsForOperationStatus(status, (k, fb) => {
                        const hit = t('telecom.' + k);
                        return hit !== 'telecom.' + k ? hit : fb;
                    });
                }
                const s = Number(status);
                const failed = s === 4;
                return [
                    { label: t('telecom.ops.pipeline.draft'), done: s !== 4, active: s === 0, failed },
                    {
                        label: t('telecom.ops.pipeline.docs'),
                        done: s >= 5 || s === 6 || s === 3 || s === 1 || s === 11,
                        active: s === 5,
                        failed,
                    },
                    {
                        label: t('telecom.ops.pipeline.scheduled'),
                        done: s === 3 || s === 1 || s === 6,
                        active: s === 11,
                        failed,
                    },
                    {
                        label: t('telecom.ops.pipeline.provisioning'),
                        done: s === 3 || s === 1,
                        active: s === 6,
                        failed,
                    },
                    { label: t('telecom.ops.pipeline.done'), done: s === 3, active: false, failed },
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
                    const okO = results[1];
                    if (!okO) {
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
                    state.detailTelecomTypeId = defaultTelecomLineTypeId();
                    return;
                }
                const subs = d.subscriptions || [];
                const primary = subs.find((x) => x.isPrimaryLine) || subs[0];
                state.detailTelecomSubscriptionId = primary?.id || '';
                state.detailTelecomMsisdn = primary?.msisdn || '';
                state.detailTelecomTypeId = primary?.subscriptionTypeId || defaultTelecomLineTypeId();
            };

            const onDetailSubscriptionChange = () => {
                const d = state.subscriberDetail;
                if (!d) return;
                const subs = d.subscriptions || [];
                const found = subs.find((x) => x.id === state.detailTelecomSubscriptionId);
                if (found) {
                    state.detailTelecomMsisdn = found.msisdn || '';
                    state.detailTelecomTypeId = found.subscriptionTypeId || defaultTelecomLineTypeId();
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
                    const q =
                        '/Telecom/GetTelecomUniversalSearch?term=' +
                        encodeURIComponent(tt) +
                        (which === 'primary' || (state.wizard.kind === 'activate' && which === 'secondary')
                            ? '&profilesOnly=true'
                            : '');
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

            const loadActivateMsisdnPool = async () => {
                state.wizard.activateMsisdnPoolBusy = true;
                try {
                    let url = '/Telecom/GetMsisdnAssetPoolList?status=Available';
                    const lineTypeId = (state.wizard.activationLineTypeId || '').trim();
                    if (lineTypeId) {
                        url += '&subscriptionTypeId=' + encodeURIComponent(lineTypeId);
                    }
                    const res = await AxiosManager.get(url, {});
                    state.wizard.activateMsisdnPoolRows = parseMsisdnPoolRows(res).filter(isAvailableMsisdnPoolRow);
                } catch (e) {
                    console.warn('Activate MSISDN pool load failed', e);
                    state.wizard.activateMsisdnPoolRows = [];
                } finally {
                    state.wizard.activateMsisdnPoolBusy = false;
                }
            };

            const unlockActivateMsisdnBodyScroll = () => {
                document.body.classList.remove('modal-open');
                document.body.style.removeProperty('overflow');
                document.body.style.removeProperty('padding-right');
            };

            const closeActivateMsisdnModal = () => {
                state.wizard.activateMsisdnModalOpen = false;
                unlockActivateMsisdnBodyScroll();
            };

            const setActivateMsisdnCategoryTab = (tabId) => {
                state.wizard.activateMsisdnCategoryTab = tabId;
            };

            const openActivateMsisdnModal = async () => {
                if (!canPickActivateMsisdn.value) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.incompleteTitle'),
                            text: t('telecom.wizard.lineType.required'),
                        });
                    }
                    return;
                }
                state.wizard.activateMsisdnModalPrefix = (state.wizard.activateMsisdnSearchTerm || '').trim();
                state.wizard.activateMsisdnCategoryTab = 'all';
                state.wizard.activateMsisdnModalOpen = true;
                document.body.classList.add('modal-open');
                document.body.style.overflow = 'hidden';
                await loadActivateMsisdnPool();
            };

            const runWizardActivateMsisdnSearch = async (ev) => {
                if (ev?.preventDefault) ev.preventDefault();
                await openActivateMsisdnModal();
            };

            const onActivationLineTypeChange = async () => {
                await clearWizardActivateMsisdn();
                state.wizard.migrationTargetProductId = '';
                state.migrationEligibleProducts = [];
                state.migrationLineTypeHint = activationLineTypeLabel.value;
                const sid = (state.wizard.primarySubscriberProfileId || '').trim();
                if (sid && canPickActivateMsisdn.value) {
                    await loadMigrationEligibleProducts();
                }
            };

            const resolvePrimaryCustomerKind = async (profileId) => {
                const pid = (profileId || '').trim();
                if (!pid) return '';
                try {
                    const res = await AxiosManager.get(
                        '/Telecom/GetTelecomSubscriberProfileDetail?subscriberProfileId=' + encodeURIComponent(pid),
                        {}
                    );
                    const d = res?.data?.content ?? {};
                    return d.subscriberType ?? d.SubscriberType ?? d.data?.subscriberType ?? '';
                } catch {
                    return '';
                }
            };

            const subscriberTypeToCustomerKind = (subscriberType) => {
                const n = Number(subscriberType);
                if (n === 1) return 'Corporate';
                if (n === 0) return 'Individual';
                return '';
            };

            const profileIdFromRow = (r) => {
                if (!r) return '';
                if (r.subscriberProfileId) return r.subscriberProfileId;
                if (r.resultType === 'SubscriberProfile' && r.id) return r.id;
                return '';
            };

            const loadChangeGsmEligibleTargets = async () => {
                if (state.wizard.kind !== 'changeGsm') return;
                const sid = (state.wizard.primarySubscriberProfileId || '').trim();
                if (!sid) {
                    state.wizard.cgtTargets = [];
                    return;
                }
                state.wizard.cgtTargetsBusy = true;
                try {
                    const ms = (state.wizard.primaryMsisdnAssetId || '').trim();
                    let url = '/Product/GetChangeGsmEligibleTargets?subscriberProfileId=' + encodeURIComponent(sid);
                    if (ms) url += '&msisdnAssetId=' + encodeURIComponent(ms);
                    const res = await AxiosManager.get(url, {});
                    const c = res?.data?.content ?? {};
                    state.wizard.cgtTargets = Array.isArray(c?.data) ? c.data : [];
                    state.wizard.cgtCurrentTypeLabel = c?.currentSubscriptionTypeLabel || '';
                    state.wizard.cgtSourceTypeCode =
                        c?.currentSubscriptionTypeCode || c?.CurrentSubscriptionTypeCode || '';
                } catch {
                    state.wizard.cgtTargets = [];
                } finally {
                    state.wizard.cgtTargetsBusy = false;
                }
            };

            const loadMigrationEligibleProducts = async () => {
                if (state.wizard.kind !== 'migrate' && state.wizard.kind !== 'activate') return;
                const sid = (state.wizard.primarySubscriberProfileId || '').trim();
                if (!sid) {
                    state.migrationEligibleProducts = [];
                    state.migrationLineTypeHint = '';
                    return;
                }
                if (state.wizard.kind === 'activate') {
                    const lineTypeId = (state.wizard.activationLineTypeId || '').trim();
                    if (!lineTypeId) {
                        state.migrationEligibleProducts = [];
                        state.migrationLineTypeHint = '';
                        return;
                    }
                }
                state.migrationOffersBusy = true;
                try {
                    let url = '/Product/GetMigrationEligibleProducts?subscriberProfileId=' + encodeURIComponent(sid);
                    if (state.wizard.kind === 'activate') {
                        const lineTypeId = (state.wizard.activationLineTypeId || '').trim();
                        if (lineTypeId) {
                            url += '&targetSubscriptionTypeId=' + encodeURIComponent(lineTypeId);
                        }
                    } else {
                        const ms = (state.wizard.primaryMsisdnAssetId || '').trim();
                        if (ms) url += '&msisdnAssetId=' + encodeURIComponent(ms);
                    }
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
                                    const planLabel = productDisplayName(product);
                                    const confirm = await Swal.fire({
                                        title: t('telecom.swal.incompatTitle'),
                                        html: t('telecom.swal.incompatHtml', { plan: planLabel }),
                                        icon: 'warning',
                                        showCancelButton: true,
                                        confirmButtonColor: '#c8102e',
                                        confirmButtonText: t('telecom.swal.incompatConfirm'),
                                        cancelButtonText: t('telecom.wizard.cancel'),
                                    });

                                    if (confirm.isConfirmed) {
                                        // Push the offering to eligible list so it's selectable in UI dropdown
                                        if (!state.migrationEligibleProducts.some(p => p.id === product.id)) {
                                            state.migrationEligibleProducts.push({
                                                id: product.id,
                                                name: product.name,
                                                nameEn: product.nameEn,
                                                serviceCode: product.code,
                                                compatibleSubscriptionTypeId: prodCompatId
                                            });
                                        }
                                        state.wizard.migrationTargetProductId = product.id;
                                        await loadWizardOfferDetail(product.id);
                                        state.wizard.notes = t('telecom.swal.autoMigrateNote', {
                                            plan: productDisplayName(product),
                                        });
                                    } else {
                                        state.wizard.migrationTargetProductId = '';
                                        state.wizard.offerDetail = null;
                                    }
                                } else {
                                    // Compatible! Just ensure it's in eligible list and select it
                                    if (!state.migrationEligibleProducts.some(p => p.id === product.id)) {
                                        state.migrationEligibleProducts.push({
                                            id: product.id,
                                            name: product.name,
                                            nameEn: product.nameEn,
                                            serviceCode: product.code,
                                            compatibleSubscriptionTypeId: prodCompatId
                                        });
                                    }
                                    state.wizard.migrationTargetProductId = product.id;
                                    await loadWizardOfferDetail(product.id);
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
                    const selOffer = (state.wizard.migrationTargetProductId || '').trim();
                    if (selOffer) {
                        await loadWizardOfferDetail(selOffer);
                    }
                    state.migrationOffersBusy = false;
                }
            };

            const selectWizardPrimary = async (r) => {
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
                const prevCorporate = isCorporatePrimary.value;
                state.wizard.primarySubscriberProfileId = pid;
                state.wizard.primaryMsisdnAssetId = r.resultType === 'Msisdn' ? r.id : '';
                state.wizard.primaryMsisdn =
                    r.resultType === 'Msisdn' ? String(r.title || '').trim() : state.wizard.primaryMsisdn;
                state.wizard.customerId = r.customerId || state.wizard.customerId || '';
                state.wizard.primaryLabel = `${r.title || '—'} (${r.resultType})`;
                state.wizard.primaryRowKey = rowKey(r);
                state.wizard.primaryCustomerKind = await resolvePrimaryCustomerKind(pid);
                if (prevCorporate && !isCorporateKind(state.wizard.primaryCustomerKind)) {
                    clearWizardSecondary();
                }

                if (state.preloadedProductId) {
                    state.wizard.migrationTargetProductId = state.preloadedProductId;
                } else {
                    state.wizard.migrationTargetProductId = '';
                }
                
                state.wizard.targetOfferOtherText = '';
                if (state.wizard.kind === 'migrate') {
                    loadMigrationEligibleProducts();
                } else if (state.wizard.kind === 'activate' && (state.wizard.activationLineTypeId || '').trim()) {
                    loadMigrationEligibleProducts();
                }
                if (state.wizard.kind === 'changeGsm') {
                    loadChangeGsmEligibleTargets();
                }
                if (state.wizard.kind === 'reconnect') {
                    loadReconnectEligibility();
                }
                if (state.wizard.kind === 'badDebt') {
                    loadBadDebtEligibility();
                }
                await loadPrimaryOutstandingBalance();
            };

            const loadBadDebtEligibility = async () => {
                const pid = (state.wizard.primarySubscriberProfileId || '').trim();
                const assetId = (state.wizard.primaryMsisdnAssetId || '').trim();
                if (!pid || !assetId) {
                    state.wizard.bdrEligibility = null;
                    return;
                }
                state.wizard.bdrEligibilityBusy = true;
                try {
                    const qs = new URLSearchParams({
                        subscriberProfileId: pid,
                        msisdnAssetId: assetId,
                        collectionAction: (state.wizard.bdrCollectionAction || 'PaymentRecorded').trim(),
                        dunningStage: (state.wizard.bdrDunningStage || 'Reminder1').trim(),
                        collectionApprovalConfirmed: state.wizard.bdrSupervisorConfirmed ? 'true' : 'false',
                    });
                    const pay = (state.wizard.bdrPaymentReference || '').trim();
                    if (pay) qs.set('paymentReference', pay);
                    const col = Number(state.wizard.bdrCollectedAmount);
                    if (col > 0) qs.set('collectedAmount', String(col));
                    const wo = Number(state.wizard.bdrWriteOffAmount);
                    if (wo > 0) qs.set('writeOffAmount', String(wo));
                    const res = await AxiosManager.get('/Telecom/GetBadDebtEligibility?' + qs.toString(), {});
                    const d = res?.data?.content?.data ?? res?.data?.content?.Data ?? null;
                    state.wizard.bdrEligibility = d;
                    if (d) {
                        state.wizard.bdrRequiresBackOffice =
                            !!d.requiresBackOfficeApproval || !!d.RequiresBackOfficeApproval;
                    }
                } catch (e) {
                    state.wizard.bdrEligibility = null;
                    console.warn('BadDebt eligibility', e);
                } finally {
                    state.wizard.bdrEligibilityBusy = false;
                }
            };

            const onBdrActionChange = () => {
                loadBadDebtEligibility();
            };

            const loadReconnectEligibility = async () => {
                const pid = (state.wizard.primarySubscriberProfileId || '').trim();
                const assetId = (state.wizard.primaryMsisdnAssetId || '').trim();
                if (!pid || !assetId) {
                    state.wizard.rcnEligibility = null;
                    return;
                }
                state.wizard.rcnEligibilityBusy = true;
                try {
                    const qs = new URLSearchParams({
                        subscriberProfileId: pid,
                        msisdnAssetId: assetId,
                        reconnectReason: (state.wizard.rcnReconnectReason || 'CustomerRequest').trim(),
                        clearanceType: (state.wizard.rcnClearanceType || 'Customer').trim(),
                        fraudClearanceConfirmed: String(!!state.wizard.rcnFraudClearanceConfirmed),
                    });
                    const pay = (state.wizard.rcnPaymentReference || '').trim();
                    if (pay) qs.set('paymentReference', pay);
                    const res = await AxiosManager.get('/Telecom/GetReconnectEligibility?' + qs.toString(), {});
                    const d = res?.data?.content?.data ?? res?.data?.content?.Data ?? null;
                    state.wizard.rcnEligibility = d;
                    if (d) {
                        state.wizard.rcnRequiresBackOffice =
                            !!d.requiresBackOfficeApproval || !!d.RequiresBackOfficeApproval;
                    }
                } catch (e) {
                    state.wizard.rcnEligibility = null;
                    console.warn('Reconnect eligibility', e);
                } finally {
                    state.wizard.rcnEligibilityBusy = false;
                }
            };

            const onRcnClearanceChange = () => {
                if (typeof TelecomReconnectClearance !== 'undefined') {
                    TelecomReconnectClearance.resetConditionalFields(state.wizard, state.wizard.rcnClearanceType);
                }
                loadReconnectEligibility();
            };

            const onRcnRegulatoryFileChange = (ev) => {
                const file = ev?.target?.files?.[0] || null;
                state.wizard.rcnRegulatoryFile = file;
                state.wizard.rcnKycDocumentReferenceId = '';
            };

            const rcnShowsPaymentRef = Vue.computed(() => {
                const rcn = typeof TelecomReconnectClearance !== 'undefined' ? TelecomReconnectClearance : null;
                return rcn
                    ? rcn.showsPaymentReference(state.wizard.rcnClearanceType, { bdrApproved: !!state.wizard.rcnBdrApproved })
                    : state.wizard.rcnClearanceType === 'Payment' || !!state.wizard.rcnBdrApproved;
            });
            const rcnShowsFraudFields = Vue.computed(() => {
                const rcn = typeof TelecomReconnectClearance !== 'undefined' ? TelecomReconnectClearance : null;
                return rcn ? rcn.showsFraudFields(state.wizard.rcnClearanceType) : state.wizard.rcnClearanceType === 'Fraud';
            });
            const rcnShowsRegulatoryFields = Vue.computed(() => {
                const rcn = typeof TelecomReconnectClearance !== 'undefined' ? TelecomReconnectClearance : null;
                return rcn ? rcn.showsRegulatoryFields(state.wizard.rcnClearanceType) : state.wizard.rcnClearanceType === 'Regulatory';
            });
            const rcnShowsSimplePath = Vue.computed(() => {
                const rcn = typeof TelecomReconnectClearance !== 'undefined' ? TelecomReconnectClearance : null;
                return rcn ? rcn.showsSimplePath(state.wizard.rcnClearanceType) : ['Customer', 'Operational'].includes(state.wizard.rcnClearanceType);
            });

            const susShowsPayment = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.suspension.showsPayment(state.wizard.susSuspensionType)
                    : state.wizard.susSuspensionType === 'Billing');
            const susShowsFraud = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.suspension.showsFraud(state.wizard.susSuspensionType)
                    : state.wizard.susSuspensionType === 'Fraud');
            const susShowsRegulatory = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.suspension.showsRegulatory(state.wizard.susSuspensionType)
                    : state.wizard.susSuspensionType === 'Regulatory');
            const susShowsSimple = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.suspension.showsSimple(state.wizard.susSuspensionType)
                    : ['CustomerRequest', 'Operational'].includes(state.wizard.susSuspensionType));

            const barringLevelOptions = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.BARRING_LEVELS
                    : [
                        { value: 'Full', key: 'suspension.barringFull' },
                        { value: 'InboundOnly', key: 'suspension.barringInbound' },
                        { value: 'OutboundOnly', key: 'suspension.barringOutbound' },
                        { value: 'DataOnly', key: 'suspension.barringDataOnly' },
                    ]);

            const trmShowsPayment = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.termination.showsPayment(state.wizard.trmTerminationType)
                    : state.wizard.trmTerminationType === 'Collections');
            const trmShowsFraud = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.termination.showsFraud(state.wizard.trmTerminationType)
                    : state.wizard.trmTerminationType === 'Fraud');
            const trmShowsRegulatory = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.termination.showsRegulatory(state.wizard.trmTerminationType)
                    : state.wizard.trmTerminationType === 'Regulatory');
            const trmShowsVoluntary = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.termination.showsVoluntary(state.wizard.trmTerminationType)
                    : state.wizard.trmTerminationType === 'Voluntary');
            const trmRequiresLegacyIdentity = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.termination.requiresLegacyIdentity(state.wizard.trmTerminationType)
                        && !trmShowsRegulatory.value
                    : state.wizard.trmRequiresBackOffice && state.wizard.trmTerminationType !== 'Regulatory');

            const cgtShowsFinancial = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.changeGsm.showsFinancial(state.wizard, bssCgtOptions())
                    : false);
            const cgtShowsRegulatory = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.changeGsm.showsRegulatory(state.wizard)
                    : state.wizard.cgtMigrationPath === 'Regulatory');

            const rfdShowsOriginalTxRef = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.refund.showsOriginalTxRef(state.wizard.rfdRefundType)
                    : ['Deposit', 'Overpayment'].includes(state.wizard.rfdRefundType));
            const rfdShowsPayoutDestination = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.refund.showsPayoutDestination(
                        state.wizard.rfdRefundType,
                        state.wizard.rfdRefundMethod
                    )
                    : state.wizard.rfdRefundMethod === 'BankTransfer'
                        || state.wizard.rfdRefundType === 'SyriatelCash');
            const rfdPayoutLabelKey = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.refund.payoutLabel(
                        state.wizard.rfdRefundType,
                        state.wizard.rfdRefundMethod
                    )
                    : 'telecom.bss.payoutDestination');

            const simShowsLostStolenFields = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.simSwap.showsLostStolenFields(state.wizard.simLostOrStolen)
                    : !!state.wizard.simLostOrStolen);
            const cnShowsPremiumPayment = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.changeNumber.showsPremiumPayment(state.wizard)
                    : !!state.wizard.cnRequiresBackOffice);
            const cnShowsInternalPool = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.changeNumber.showsInternalPool(state.wizard)
                    : (state.wizard.cnChangeMode || 'Internal') !== 'PortIn');
            const cnDonorOperators = Vue.computed(() =>
                typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.changeNumber.donorOperators
                    : ['MTN', 'AFRICELL', 'OTHER']);
            const tkoShowsObligationSettlement = Vue.computed(() => {
                const bal = state.wizard.primaryOutstandingBalance;
                return bal != null && Number.isFinite(Number(bal)) && Number(bal) < 0;
            });

            const clearWizardPrimary = async () => {
                await releaseWizardActivateMsisdnIfAny();
                state.wizard.primarySubscriberProfileId = '';
                state.wizard.primaryMsisdnAssetId = '';
                state.wizard.primaryLabel = '';
                state.wizard.primaryRowKey = '';
                state.wizard.primaryCustomerKind = '';
                clearWizardSecondary();
                state.migrationEligibleProducts = [];
                state.migrationLineTypeHint = '';
                state.currentProductName = '';
                state.wizard.migrationTargetProductId = '';
                state.wizard.offerDetail = null;
                state.wizard.offerDetailBusy = false;
                state.wizard.targetOfferOtherText = '';
            };

            const wizardReservationCustomerId = () => resolveWizardCustomerId(state.wizard);

            const wizardCustomer360ProfileUrl = Vue.computed(() => {
                const customerId = resolveWizardCustomerId(state.wizard);
                if (!customerId) {
                    return '';
                }
                return '/Telecom/Customer360Profile?customerId=' + encodeURIComponent(customerId);
            });

            const openWizardCustomer360 = () => {
                const url = wizardCustomer360ProfileUrl.value;
                if (url) {
                    window.location.href = url;
                    return;
                }
                if (window.Swal) {
                    Swal.fire({
                        icon: 'warning',
                        title: t('telecom.swal.incompleteTitle'),
                        text: t('telecom.wizardUi.customer360IdMissing', 'Select a customer before opening Customer 360.'),
                    });
                }
            };

            const releaseMsisdnForWizard = async (assetId, customerId) => {
                const aid = (assetId || '').trim();
                const cid = (customerId || '').trim();
                if (!aid || !cid) return;
                try {
                    await AxiosManager.post('/Telecom/ReleaseMsisdnReservation', {
                        msisdnAssetId: aid,
                        customerId: cid,
                    });
                } catch {
                    /* best-effort; background cleanup is safety net */
                }
            };

            const releaseWizardActivateMsisdnIfAny = async () => {
                if ((state.wizard.createdOperationId || '').trim()) return;
                if (state.wizard.submitBusy) return;
                const assetId = (state.wizard.secondaryMsisdnAssetId || '').trim();
                if (!assetId) return;
                await releaseMsisdnForWizard(assetId, wizardReservationCustomerId());
            };

            const reserveMsisdnForWizard = async () => {
                const assetId = (state.wizard.secondaryMsisdnAssetId || '').trim();
                const customerId = wizardReservationCustomerId();
                if (!assetId || !customerId) return;
                try {
                    await AxiosManager.post('/Telecom/ReserveMsisdnForCustomer', {
                        msisdnAssetId: assetId,
                        customerId,
                    });
                } catch (e) {
                    const msg = pickHttpErrorMessage(e);
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.reserveFailedTitle'),
                            text: msg || t('telecom.swal.reserveFailedText'),
                        });
                    }
                }
            };

            const iccidFromMsisdnPoolRow = (row) =>
                String(row?.iccid ?? row?.Iccid ?? row?.pairedIccid ?? row?.PairedIccid ?? '').trim();

            const selectWizardActivateMsisdn = async (row) => {
                const id = row?.id ?? row?.Id;
                const msisdn = row?.msisdn ?? row?.Msisdn ?? row?.title;
                if (!id) return;
                const previousAssetId = (state.wizard.secondaryMsisdnAssetId || '').trim();
                if (previousAssetId && previousAssetId !== id) {
                    await releaseMsisdnForWizard(previousAssetId, wizardReservationCustomerId());
                }
                state.wizard.secondaryMsisdnAssetId = id;
                state.wizard.activateMsisdnLabel = msisdn || '—';
                state.wizard.activateMsisdnSearchTerm = msisdn || '';
                state.wizard.simIccid = iccidFromMsisdnPoolRow(row);
                closeActivateMsisdnModal();
                await reserveMsisdnForWizard();
            };

            const clearWizardActivateMsisdn = async () => {
                await releaseWizardActivateMsisdnIfAny();
                state.wizard.secondaryMsisdnAssetId = '';
                state.wizard.activateMsisdnLabel = '';
                state.wizard.simIccid = '';
            };

            const selectWizardSecondary = async (r) => {
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
                state.wizard.secondaryLabel = '';
                state.wizard.secondarySearchTerm = '';
                state.wizard.secondaryResults = [];
            };

            const loadDeviceWizardCatalog = async () => {
                try {
                    const [devRes, planRes] = await Promise.all([
                        AxiosManager.get('/Telecom/GetDeviceInventoryList?status=Available', {}),
                        AxiosManager.get('/Telecom/GetInstallmentPlanList', {}),
                    ]);
                    state.wizard.devDevices = devRes?.data?.content?.data ?? devRes?.data?.content?.Data ?? [];
                    state.wizard.devPlans = planRes?.data?.content?.data ?? planRes?.data?.content?.Data ?? [];
                } catch {
                    state.wizard.devDevices = [];
                    state.wizard.devPlans = [];
                }
            };

            const onDevicePicked = () => {
                const row = (state.wizard.devDevices || []).find((d) => String(d.id) === String(state.wizard.devInventoryId));
                if (row) {
                    state.wizard.devDownPayment = String(row.listPrice ?? row.ListPrice ?? '');
                    state.wizard.devFinancingPreview = '';
                }
            };

            const recordDeviceDownPayment = async () => {
                if (!state.wizard.createdOperationId) return;
                const amount = Number(state.wizard.devDownPayment);
                if (!amount || !state.wizard.devPaymentReference?.trim()) {
                    if (window.Swal)
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.incompleteTitle'),
                            text: t('telecom.swal.paymentDownRequired'),
                        });
                    return;
                }
                await AxiosManager.post('/Telecom/RecordDeviceDownPayment', {
                    operationId: state.wizard.createdOperationId,
                    amountPaid: amount,
                    paymentChannel: Number(state.wizard.devPaymentChannel) || 0,
                    paymentReference: state.wizard.devPaymentReference.trim(),
                });
                state.wizard.documentMarkedUploaded = true;
                if (window.Swal)
                    Swal.fire({
                        icon: 'success',
                        title: t('telecom.swal.paymentRecordedOk'),
                        timer: 1200,
                        showConfirmButton: false,
                    });
            };

            const loadWizardVasCatalog = async () => {
                const sid = (state.wizard.primarySubscriberProfileId || '').trim();
                if (!sid) {
                    state.wizard.vasCatalog = [];
                    return;
                }
                state.wizard.vasCatalogBusy = true;
                try {
                    const ms = (state.wizard.primaryMsisdnAssetId || '').trim();
                    let url =
                        '/Product/GetEligibleVasOfferings?subscriberProfileId=' + encodeURIComponent(sid);
                    if (ms) {
                        url += '&msisdnAssetId=' + encodeURIComponent(ms);
                    }
                    const res = await AxiosManager.get(url, {});
                    const content = res?.data?.content ?? res?.data?.Content ?? {};
                    const list = content.data || content.Data || [];
                    state.wizard.vasCatalog = (Array.isArray(list) ? list : [])
                        .map((v) => ({
                            serviceCode: v.serviceCode || v.ServiceCode,
                            nameAr:
                                v.nameAr ||
                                v.NameAr ||
                                v.catalogComponentLabel ||
                                v.CatalogComponentLabel ||
                                v.serviceCode ||
                                v.ServiceCode,
                            productOfferingId: v.productOfferingId || v.ProductOfferingId,
                        }))
                        .filter((v) => v.serviceCode);
                } catch {
                    state.wizard.vasCatalog = [];
                } finally {
                    state.wizard.vasCatalogBusy = false;
                }
            };

            const scrollToHubWizard = async (headingId) => {
                await Vue.nextTick();
                requestAnimationFrame(() => {
                    const el = document.getElementById(headingId);
                    if (!el) return;
                    const stickyHeader = document.querySelector('.navbar.fixed-top, .main-header, header.fixed-top');
                    const offset = (stickyHeader?.offsetHeight ?? 0) + 16;
                    const top = el.getBoundingClientRect().top + window.scrollY - offset;
                    window.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });
                });
            };

            const loadHubReconnectBdrContext = async () => {
                state.wizard.rcnBdrApproved = false;
                const customerId = (state.wizard.customerId || resolveWizardCustomerId(state.wizard) || '').trim();
                const sub = {
                    msisdnAssetId: state.wizard.primaryMsisdnAssetId,
                    msisdn: state.wizard.primaryMsisdn,
                };
                if (!customerId || !(sub.msisdnAssetId || sub.msisdn)) return;
                try {
                    const res = await AxiosManager.get(
                        '/Customer/GetCustomer360?customerId=' + encodeURIComponent(customerId),
                        {}
                    );
                    const ops =
                        res?.data?.content?.recentOperations ??
                        res?.data?.content?.RecentOperations ??
                        [];
                    if (typeof TelecomReconnectClearance !== 'undefined') {
                        state.wizard.rcnBdrApproved = TelecomReconnectClearance.isBdrApprovedForReconnect(
                            TelecomReconnectClearance.resolveBdrStatus(ops, sub)
                        );
                    }
                } catch {
                    state.wizard.rcnBdrApproved = false;
                }
            };

            const openWizard = async (kind) => {
                if (state.customerWizard.visible) {
                    closeCustomerWizard();
                }
                resetWizardState();
                state.wizard.visible = true;
                state.wizard.kind = kind;
                if (kind === 'activate') {
                    if (typeof ActivationChannelUi !== 'undefined') {
                        await ActivationChannelUi.ensureLoaded();
                        ActivationChannelUi.applyDefaults(state.wizard);
                    }
                    await loadTelecomLineTypes();
                }
                if (kind === 'deviceSale') loadDeviceWizardCatalog();
                if (kind === 'addpackage') loadWizardVasCatalog();
                if (kind === 'reconnect') await loadHubReconnectBdrContext();
                await scrollToHubWizard('wizardHeading');
            };

            const resetCustomerWizard = () => {
                state.customerWizard.step = 1;
                state.customerWizard.searchNationalId = '';
                state.customerWizard.searchPhone = '';
                state.customerWizard.searchBusy = false;
                state.customerWizard.searchAttempted = false;
                state.customerWizard.searchResults = [];
                state.customerWizard.subscriberType = 0;
                state.customerWizard.name = '';
                state.customerWizard.nationalId = '';
                state.customerWizard.commercialRegistration = '';
                state.customerWizard.taxNumber = '';
                state.customerWizard.authorizedSignatory = '';
                state.customerWizard.dateOfBirth = '';
                state.customerWizard.description = '';
                state.customerWizard.customerGroupId = '';
                state.customerWizard.customerCategoryId = '';
                state.customerWizard.street = '';
                state.customerWizard.cityId = '';
                state.customerWizard.city = '';
                state.customerWizard.addressState = '';
                state.customerWizard.zipCode = '';
                state.customerWizard.country = defaultCountryLabel();
                state.customerWizard.phoneNumber = '';
                state.customerWizard.emailAddress = '';
                state.customerWizard.nationality = '';
                state.customerWizard.gender = '';
                state.customerWizard.occupation = '';
                state.customerWizard.faxNumber = '';
                state.customerWizard.website = '';
                state.customerWizard.whatsApp = '';
                state.customerWizard.linkedIn = '';
                state.customerWizard.facebook = '';
                state.customerWizard.instagram = '';
                state.customerWizard.twitterX = '';
                state.customerWizard.tikTok = '';
                state.customerWizard.contactRows = [
                    { name: '', jobTitle: '', phoneNumber: '', emailAddress: '', description: '' },
                    { name: '', jobTitle: '', phoneNumber: '', emailAddress: '', description: '' },
                    { name: '', jobTitle: '', phoneNumber: '', emailAddress: '', description: '' },
                ];
                state.customerWizard.errors = {};
                state.customerWizard.saveBusy = false;
                state.customerWizard.handoffBusy = false;
                state.customerWizard.savedCustomerId = '';
                state.customerWizard.savedCustomerNumber = '';
                state.customerWizard.savedCustomerName = '';
                state.customerWizard.savedSubscriberProfileId = '';
            };

            const loadCustomerWizardLookups = async () => {
                if (state.customerWizard.lookupsLoaded) return;
                try {
                    const [groupRes, categoryRes, cityRes] = await Promise.all([
                        AxiosManager.get('/CustomerGroup/GetCustomerGroupList', {}),
                        AxiosManager.get('/CustomerCategory/GetCustomerCategoryList', {}),
                        AxiosManager.get('/GeoCity/GetGeoCityList', { params: { activeOnly: true } }),
                    ]);
                    state.customerWizard.groupOptions = groupRes?.data?.content?.data ?? [];
                    state.customerWizard.categoryOptions = categoryRes?.data?.content?.data ?? [];
                    state.customerWizard.cityOptions = cityRes?.data?.content?.data ?? [];
                    state.customerWizard.lookupsLoaded = true;
                } catch {
                    state.customerWizard.groupOptions = [];
                    state.customerWizard.categoryOptions = [];
                    state.customerWizard.cityOptions = [];
                }
            };

            const onCustomerWizardCityChange = () => {
                const w = state.customerWizard;
                const selected = w.cityOptions.find((c) => c.id === w.cityId);
                if (selected) {
                    w.city = selected.name || '';
                    w.addressState = selected.governorate || '';
                } else {
                    w.city = '';
                    w.addressState = '';
                }
            };

            const applyCustomerWizardDefaultLookups = () => {
                const w = state.customerWizard;
                if (!w.customerGroupId && w.groupOptions.length) {
                    const preferred =
                        w.subscriberType === 1
                            ? w.groupOptions.find((g) => (g.name || '').includes('شركات'))
                            : w.groupOptions.find((g) => (g.name || '').includes('أفراد'));
                    w.customerGroupId = preferred?.id ?? w.groupOptions[0]?.id ?? '';
                }
                if (!w.customerCategoryId && w.categoryOptions.length) {
                    const preferred =
                        w.subscriberType === 1
                            ? w.categoryOptions.find((c) => (c.name || '').includes('شركات'))
                            : w.categoryOptions.find((c) => (c.name || '').includes('أفراد'));
                    w.customerCategoryId = preferred?.id ?? w.categoryOptions[0]?.id ?? '';
                }
            };

            const openCustomerWizard = async () => {
                if (!hubPermissions.value.canOpenSubscriberRegistry) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.searchModal.registryDeniedTitle'),
                            text: t('telecom.searchModal.registryDeniedText'),
                        });
                    }
                    return;
                }
                if (state.wizard.visible) {
                    closeWizard();
                }
                resetCustomerWizard();
                state.customerWizard.visible = true;
                await loadCustomerWizardLookups();
                await scrollToHubWizard('customerWizardHeading');
            };

            const closeCustomerWizard = () => {
                state.customerWizard.visible = false;
                resetCustomerWizard();
            };

            const runCustomerWizardSearch = async () => {
                const nat = (state.customerWizard.searchNationalId || '').trim();
                const ph = (state.customerWizard.searchPhone || '').trim();
                if (nat.length < 2 && ph.length < 2) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.searchTitle'),
                            text: t('telecom.customerWizard.searchHintMin'),
                        });
                    }
                    return;
                }
                state.customerWizard.searchBusy = true;
                state.customerWizard.searchAttempted = true;
                try {
                    const qs = new URLSearchParams();
                    if (nat) qs.set('nationalId', nat);
                    if (ph) qs.set('phone', ph);
                    const res = await AxiosManager.get('/Customer/FindCustomerCandidates?' + qs.toString(), {});
                    if (res?.data?.code !== 200) {
                        state.customerWizard.searchResults = [];
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.searchTitle'),
                                text: res?.data?.message || t('telecom.swal.searchFail'),
                            });
                        }
                        return;
                    }
                    const list = res?.data?.content?.data;
                    state.customerWizard.searchResults = Array.isArray(list) ? list : [];
                } catch (e) {
                    state.customerWizard.searchResults = [];
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'error',
                            title: t('telecom.swal.searchTitle'),
                            text: pickHttpErrorMessage(e) || t('telecom.swal.searchFail'),
                        });
                    }
                } finally {
                    state.customerWizard.searchBusy = false;
                }
            };

            const proceedCustomerWizardNew = async () => {
                await loadCustomerWizardLookups();
                state.customerWizard.step = 2;
                const nat = (state.customerWizard.searchNationalId || '').trim();
                const ph = (state.customerWizard.searchPhone || '').trim();
                if (nat) {
                    if (state.customerWizard.subscriberType === 0 && nat.length === 10) {
                        state.customerWizard.nationalId = nat;
                    } else if (state.customerWizard.subscriberType === 1 && !state.customerWizard.commercialRegistration) {
                        state.customerWizard.commercialRegistration = nat;
                    }
                }
                if (ph && !state.customerWizard.phoneNumber) {
                    state.customerWizard.phoneNumber = ph;
                }
                applyCustomerWizardDefaultLookups();
            };

            const goBackCustomerWizardSearch = () => {
                state.customerWizard.step = 1;
                state.customerWizard.errors = {};
            };

            const onCustomerWizardSubscriberTypeChange = () => {
                state.customerWizard.customerGroupId = '';
                state.customerWizard.customerCategoryId = '';
                applyCustomerWizardDefaultLookups();
            };

            const validateCustomerWizardForm = () => {
                const w = state.customerWizard;
                const errors = {};
                const trim = (v) => (v || '').trim();

                if (!trim(w.name)) errors.name = t('telecom.customerWizard.errName');
                if (!trim(w.customerGroupId)) errors.customerGroupId = t('telecom.customerWizard.errGroup');
                if (!trim(w.customerCategoryId)) errors.customerCategoryId = t('telecom.customerWizard.errCategory');
                if (!trim(w.street)) errors.street = t('telecom.customerWizard.errStreet');
                if (!trim(w.cityId)) errors.city = t('telecom.customerWizard.errCity');
                if (!trim(w.addressState)) errors.addressState = t('telecom.customerWizard.errState');
                if (!trim(w.zipCode)) errors.zipCode = t('telecom.customerWizard.errZip');
                if (w.subscriberType === 0) {
                    const nid = trim(w.nationalId);
                    if (!nid || nid.length !== 10) {
                        errors.nationalId = t('telecom.customerWizard.errNationalId');
                    }
                } else {
                    const reg = trim(w.commercialRegistration);
                    if (!reg || reg.length < 4) {
                        errors.commercialRegistration = t('telecom.customerWizard.errCommercialReg');
                    }
                }

                w.errors = errors;
                return Object.keys(errors).length === 0;
            };

            const ensureSubscriberProfileForCustomerId = async (customerId) => {
                const res = await AxiosManager.post('/Telecom/EnsureSubscriberProfileForCustomer', {
                    customerId,
                });
                if (res?.data?.code !== 200) {
                    throw Object.assign(new Error(res?.data?.message || t('telecom.customerWizard.profileFail')), {
                        response: res,
                    });
                }
                const content = res?.data?.content ?? res?.data?.Content;
                return (
                    content?.subscriberProfileId ??
                    content?.SubscriberProfileId ??
                    content?.data?.subscriberProfileId ??
                    ''
                );
            };

            const prefillActivateWizardForCustomer = async (
                customerId,
                customerName,
                subscriberProfileId,
                customerKindHint = ''
            ) => {
                resetWizardState();
                state.wizard.visible = true;
                state.wizard.kind = 'activate';
                state.wizard.step = 1;
                state.wizard.customerId = customerId;
                state.wizard.primarySubscriberProfileId = subscriberProfileId;
                state.wizard.primaryLabel = customerName || customerId;
                state.wizard.primaryRowKey = `Customer:${customerId}`;
                state.wizard.primaryCustomerKind =
                    customerKindHint || (await resolvePrimaryCustomerKind(subscriberProfileId));
                await scrollToHubWizard('wizardHeading');
            };

            const trimOrNull = (v) => {
                const s = (v || '').trim();
                return s || null;
            };

            const isCustomerWizardContactRowComplete = (row) => {
                const name = (row?.name || '').trim();
                const jobTitle = (row?.jobTitle || '').trim();
                const phoneNumber = (row?.phoneNumber || '').trim();
                const emailAddress = (row?.emailAddress || '').trim();
                return !!(name && jobTitle && phoneNumber && emailAddress);
            };

            const createCustomerWizardContacts = async (customerId, uid) => {
                const rows = state.customerWizard.contactRows || [];
                for (const row of rows) {
                    if (!isCustomerWizardContactRowComplete(row)) continue;
                    const contactRes = await AxiosManager.post('/CustomerContact/CreateCustomerContact', {
                        name: row.name.trim(),
                        jobTitle: row.jobTitle.trim(),
                        phoneNumber: row.phoneNumber.trim(),
                        emailAddress: row.emailAddress.trim(),
                        description: trimOrNull(row.description),
                        customerId,
                    });
                    if (contactRes?.data?.code !== 200) {
                        throw Object.assign(new Error(contactRes?.data?.message || t('telecom.swal.failTitle')), {
                            response: contactRes,
                        });
                    }
                }
            };

            const submitCustomerWizardSave = async () => {
                if (!validateCustomerWizardForm()) return;
                state.customerWizard.saveBusy = true;
                try {
                    const w = state.customerWizard;
                    const nid = (w.nationalId || '').trim();
                    const genderVal = w.gender === '' || w.gender == null ? null : Number(w.gender);
                    const res = await AxiosManager.post('/Customer/CreateCustomer', {
                        name: w.name.trim(),
                        customerGroupId: w.customerGroupId,
                        customerCategoryId: w.customerCategoryId,
                        description: trimOrNull(w.description),
                        street: w.street.trim(),
                        city: w.city.trim(),
                        state: w.addressState.trim(),
                        zipCode: w.zipCode.trim(),
                        country: w.country.trim(),
                        phoneNumber: trimOrNull(w.phoneNumber),
                        faxNumber: trimOrNull(w.faxNumber),
                        emailAddress: trimOrNull(w.emailAddress),
                        website: trimOrNull(w.website),
                        whatsApp: trimOrNull(w.whatsApp),
                        linkedIn: trimOrNull(w.linkedIn),
                        facebook: trimOrNull(w.facebook),
                        instagram: trimOrNull(w.instagram),
                        twitterX: trimOrNull(w.twitterX),
                        tikTok: trimOrNull(w.tikTok),
                        subscriberType: w.subscriberType,
                        nationalId: w.subscriberType === 0 ? nid : '',
                        dateOfBirth: w.dateOfBirth ? new Date(w.dateOfBirth).toISOString() : null,
                        nationality: w.subscriberType === 0 ? trimOrNull(w.nationality) : null,
                        gender: w.subscriberType === 0 ? genderVal : null,
                        occupation: w.subscriberType === 0 ? trimOrNull(w.occupation) : null,
                        commercialRegistration: w.subscriberType === 1 ? (w.commercialRegistration || '').trim() : '',
                        taxNumber: (w.taxNumber || '').trim(),
                        authorizedSignatory: (w.authorizedSignatory || '').trim(),
                    });
                    if (res?.data?.code !== 200) {
                        throw Object.assign(new Error(res?.data?.message || t('telecom.swal.failTitle')), { response: res });
                    }
                    const row = res?.data?.content?.data ?? res?.data?.content?.Data ?? {};
                    const customerId = row?.id ?? row?.Id ?? '';
                    if (!customerId) {
                        throw new Error(t('telecom.customerWizard.saveNoId'));
                    }
                    await createCustomerWizardContacts(customerId, uid);
                    const profileId = await ensureSubscriberProfileForCustomerId(customerId);
                    state.customerWizard.savedCustomerId = customerId;
                    state.customerWizard.savedCustomerNumber = row?.number ?? row?.Number ?? '';
                    state.customerWizard.savedCustomerName = row?.name ?? row?.Name ?? state.customerWizard.name;
                    state.customerWizard.savedSubscriberProfileId = profileId;
                    state.customerWizard.step = 3;
                    await refreshAll();
                } catch (e) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'error',
                            title: t('telecom.swal.errorTitle'),
                            text: pickHttpErrorMessage(e) || t('telecom.swal.failTitle'),
                        });
                    }
                } finally {
                    state.customerWizard.saveBusy = false;
                }
            };

            const finishCustomerWizard = async () => {
                closeCustomerWizard();
                await refreshAll();
            };

            const handoffSavedCustomerToActivate = async () => {
                const cid = (state.customerWizard.savedCustomerId || '').trim();
                let pid = (state.customerWizard.savedSubscriberProfileId || '').trim();
                if (!cid) return;
                if (!hubPermissions.value.canCreateOps) return;
                state.customerWizard.handoffBusy = true;
                try {
                    if (!pid) {
                        pid = await ensureSubscriberProfileForCustomerId(cid);
                    }
                    const name = state.customerWizard.savedCustomerName;
                    closeCustomerWizard();
                    await prefillActivateWizardForCustomer(
                        cid,
                        name,
                        pid,
                        subscriberTypeToCustomerKind(state.customerWizard.subscriberType)
                    );
                } catch (e) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'error',
                            title: t('telecom.swal.errorTitle'),
                            text: pickHttpErrorMessage(e),
                        });
                    }
                } finally {
                    state.customerWizard.handoffBusy = false;
                }
            };

            const handoffExistingCustomerToActivate = async (c) => {
                if (!c?.id || !hubPermissions.value.canCreateOps) return;
                state.customerWizard.handoffBusy = true;
                try {
                    const profileId = await ensureSubscriberProfileForCustomerId(c.id);
                    closeCustomerWizard();
                    await prefillActivateWizardForCustomer(c.id, c.name, profileId);
                } catch (e) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'error',
                            title: t('telecom.swal.errorTitle'),
                            text: pickHttpErrorMessage(e),
                        });
                    }
                } finally {
                    state.customerWizard.handoffBusy = false;
                }
            };

            const closeWizard = async () => {
                await releaseWizardActivateMsisdnIfAny();
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

            const validateWizardBeforeCreateDraft = async () => {
                if (!(state.wizard.primarySubscriberProfileId || '').trim()) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.swal.incompleteText') });
                    }
                    return false;
                }
                if (state.wizard.kind === 'activate') {
                    if (!(state.wizard.activationLineTypeId || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.incompleteTitle'),
                                text: t('telecom.wizard.lineType.required'),
                            });
                        }
                        return false;
                    }
                    if (!state.wizard.secondaryMsisdnAssetId) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.incompleteTitle'),
                                text: t('telecom.wizard.activateMsisdnRequired'),
                            });
                        }
                        return false;
                    }
                    if (!(state.wizard.simIccid || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.incompleteTitle'),
                                text: t('telecom.wizard.iccidRequired'),
                            });
                        }
                        return false;
                    }
                    if (
                        isCorporatePrimary.value &&
                        !(state.wizard.secondarySubscriberProfileId || '').trim()
                    ) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.secondaryPartyTitle'),
                                text: t('telecom.wizard.corporateSecondPartyRequired'),
                            });
                        }
                        return false;
                    }
                }
                if (state.wizard.kind === 'migrate') {
                    if (
                        isCorporatePrimary.value &&
                        !(state.wizard.secondarySubscriberProfileId || '').trim()
                    ) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.secondaryPartyTitle'),
                                text: t('telecom.wizard.corporateSecondPartyRequired'),
                            });
                        }
                        return false;
                    }
                }
                if (secondaryRequired.value) {
                    if (state.wizard.kind === 'takeover' && !(state.wizard.secondarySubscriberProfileId || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.swal.secondaryPartyTitle'), text: t('telecom.swal.secondaryPartyText') });
                        }
                        return false;
                    }
                    if (state.wizard.kind === 'takeover') {
                        if (typeof TelecomBssWizardClearance !== 'undefined') {
                            const tkoErr = TelecomBssWizardClearance.takeOver.validate(state.wizard, bssTkoOptions());
                            if (tkoErr) {
                                if (window.Swal) {
                                    Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.' + tkoErr.key) });
                                }
                                return false;
                            }
                        } else if (!(state.wizard.tkoTransferReason || '').trim()) {
                            if (window.Swal) {
                                Swal.fire({ icon: 'warning', title: t('telecom.takeOver.transferReason'), text: t('telecom.takeOver.transferReasonPh') });
                            }
                            return false;
                        }
                    }
                }
                if (state.wizard.kind === 'deviceSale') {
                    if (!(state.wizard.devInventoryId || '').trim()) {
                        if (window.Swal)
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.incompleteTitle'),
                                text: t('telecom.swal.deviceImeiRequired'),
                            });
                        return false;
                    }
                    if (state.wizard.devSaleType === 'Installment' && !(state.wizard.devInstallmentPlanId || '').trim()) {
                        if (window.Swal)
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.incompleteTitle'),
                                text: t('telecom.swal.installmentPlanRequired'),
                            });
                        return false;
                    }
                }
                if (state.wizard.kind === 'simswap') {
                    if (!(state.wizard.simReplacementReason || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.simSwap.replacementReason'), text: t('telecom.simSwap.reasonDamaged') });
                        }
                        return false;
                    }
                    if ((state.wizard.simIccid || '').trim().length < 19) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.incompleteTitle'),
                                text: t('telecom.wizard.iccidInvalid'),
                            });
                        }
                        return false;
                    }
                    if (typeof TelecomBssWizardClearance !== 'undefined') {
                        const simErr = TelecomBssWizardClearance.simSwap.validate(state.wizard);
                        if (simErr) {
                            if (window.Swal) {
                                Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.' + simErr.key) });
                            }
                            return false;
                        }
                    } else if (state.wizard.simLostOrStolen && !state.wizard.identityFile) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.simSwap.identityRequired') });
                        }
                        return false;
                    }
                }

                const notes = (state.wizard.notes || '').trim();
                if (notes && notes.length > 500) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.notesMaxTitle'),
                            text: t('telecom.swal.notesMaxText'),
                        });
                    }
                    return false;
                }

                if (state.wizard.kind === 'refund') {
                    if (!(state.wizard.primaryMsisdnAssetId || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.refund.currentMsisdn') });
                        return false;
                    }
                    if (typeof TelecomBssWizardClearance !== 'undefined') {
                        const rfdErr = TelecomBssWizardClearance.refund.validate(state.wizard);
                        if (rfdErr) {
                            if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.' + rfdErr.key) });
                            return false;
                        }
                    } else {
                        if (!(state.wizard.rfdRefundReason || '').trim()) {
                            if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.refund.refundReason') });
                            return false;
                        }
                        const amt = Number(state.wizard.rfdRefundAmount);
                        if (!amt || amt <= 0) {
                            if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.refund.refundAmount') });
                            return false;
                        }
                    }
                    const amt = Number(state.wizard.rfdRefundAmount) || 0;
                    const rfdTy = (state.wizard.rfdRefundType || '').trim();
                    const rfdMethod = (state.wizard.rfdRefundMethod || '').trim();
                    state.wizard.rfdRequiresBackOffice =
                        rfdTy === 'SyriatelCash' || amt > 500000 || (rfdMethod === 'Cash' && amt > 500000);
                }

                if (state.wizard.kind === 'suspension') {
                    if (!(state.wizard.primaryMsisdnAssetId || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.suspension.suspensionReason') });
                        return false;
                    }
                    if (typeof TelecomBssWizardClearance !== 'undefined') {
                        const susErr = TelecomBssWizardClearance.suspension.validate(state.wizard);
                        if (susErr) {
                            if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.' + susErr.key) });
                            return false;
                        }
                    } else if (!(state.wizard.susSuspensionReason || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.suspension.suspensionReason') });
                        return false;
                    }
                    if (state.wizard.susAutoReconnectEnabled && !(state.wizard.susEndDateLocal || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.suspension.endDateRequired') });
                        return false;
                    }
                    if (state.wizard.susAutoReconnectEnabled) {
                        const endErr =
                            typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.suspensionEndDate.validateEndDate(
                                    state.wizard,
                                    hubSuspensionStartDateLocal
                                )
                                : null;
                        if (endErr) {
                            if (window.Swal) {
                                Swal.fire({
                                    icon: 'warning',
                                    title: t('telecom.swal.incompleteTitle'),
                                    text: t('telecom.' + endErr.key),
                                });
                            }
                            return false;
                        }
                    }
                    state.wizard.susRequiresBackOffice =
                        ['Fraud', 'Regulatory'].includes(state.wizard.susSuspensionType);
                }

                if (state.wizard.kind === 'reconnect') {
                    if (!(state.wizard.primaryMsisdnAssetId || '').trim() || !(state.wizard.rcnReconnectReason || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.reconnect.reconnectReason') });
                        return false;
                    }
                    if (typeof TelecomReconnectClearance !== 'undefined') {
                        const rcnErr = TelecomReconnectClearance.validate(state.wizard, {
                            bdrApproved: !!state.wizard.rcnBdrApproved,
                        });
                        if (rcnErr) {
                            if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.' + rcnErr.key) });
                            return false;
                        }
                    } else if (state.wizard.rcnClearanceType === 'Payment' && !(state.wizard.rcnPaymentReference || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.reconnect.paymentReference') });
                        return false;
                    }
                    await loadReconnectEligibility();
                    if (state.wizard.rcnEligibility && !state.wizard.rcnEligibility.allowed && !state.wizard.rcnEligibility.Allowed) {
                        const msg = state.wizard.rcnEligibility.messageAr || state.wizard.rcnEligibility.MessageAr || '';
                        if (window.Swal) Swal.fire({ icon: 'error', title: t('telecom.swal.notAllowedTitle'), text: msg });
                        return false;
                    }
                }

                if (state.wizard.kind === 'badDebt') {
                    if (!(state.wizard.primaryMsisdnAssetId || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.badDebt.currentMsisdn') });
                        return false;
                    }
                    if (state.wizard.bdrCollectionAction === 'PaymentRecorded') {
                        if (!(state.wizard.bdrPaymentReference || '').trim()) {
                            if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.badDebt.paymentReference') });
                            return false;
                        }
                        const col = Number(state.wizard.bdrCollectedAmount);
                        if (!col || col <= 0) {
                            if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.badDebt.collectedAmount') });
                            return false;
                        }
                    }
                    if ((state.wizard.bdrCollectionAction === 'WriteOffPartial' || state.wizard.bdrCollectionAction === 'WriteOffFull')) {
                        const wo = Number(state.wizard.bdrWriteOffAmount);
                        if (!wo || wo <= 0) {
                            if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.badDebt.writeOffAmount') });
                            return false;
                        }
                    }
                    await loadBadDebtEligibility();
                    if (state.wizard.bdrEligibility && !state.wizard.bdrEligibility.allowed && !state.wizard.bdrEligibility.Allowed) {
                        const msg = state.wizard.bdrEligibility.messageAr || state.wizard.bdrEligibility.MessageAr || '';
                        if (window.Swal) Swal.fire({ icon: 'error', title: t('telecom.swal.notAllowedTitle'), text: msg });
                        return false;
                    }
                }

                if (state.wizard.kind === 'termination') {
                    if (!(state.wizard.primaryMsisdnAssetId || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.termination.currentMsisdn') });
                        }
                        return false;
                    }
                    if (typeof TelecomBssWizardClearance !== 'undefined') {
                        const trmErr = TelecomBssWizardClearance.termination.validate(state.wizard);
                        if (trmErr) {
                            if (window.Swal) {
                                Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.' + trmErr.key) });
                            }
                            return false;
                        }
                    } else {
                        if (!(state.wizard.trmTerminationReason || '').trim()) {
                            if (window.Swal) {
                                Swal.fire({ icon: 'warning', title: t('telecom.termination.terminationReason'), text: t('telecom.termination.reasonPh') });
                            }
                            return false;
                        }
                        if (state.wizard.trmTerminationType === 'Voluntary' && !(state.wizard.trmRetentionOfferOutcome || '').trim()) {
                            if (window.Swal) {
                                Swal.fire({ icon: 'warning', title: t('telecom.termination.retentionOutcome'), text: t('telecom.termination.retentionDeclined') });
                            }
                            return false;
                        }
                    }
                }

                if (state.wizard.kind === 'changeNumber') {
                    if (!(state.wizard.primaryMsisdnAssetId || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.changeNumber.currentMsisdn') });
                        }
                        return false;
                    }
                    if (!(state.wizard.cnTargetMsisdnAssetId || '').trim()
                        && cnShowsInternalPool.value) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.changeNumber.targetMsisdn') });
                        }
                        return false;
                    }
                    if (!(state.wizard.cnNumberChangeReason || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.changeNumber.changeReason'), text: t('telecom.changeNumber.reasonCustomer') });
                        }
                        return false;
                    }
                    if (typeof TelecomBssWizardClearance !== 'undefined') {
                        const cnErr = TelecomBssWizardClearance.changeNumber.validate(state.wizard);
                        if (cnErr) {
                            if (window.Swal) {
                                Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.' + cnErr.key) });
                            }
                            return false;
                        }
                    }
                }
                if (state.wizard.kind === 'changeGsm') {
                    if (!(state.wizard.cgtTargetTypeId || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.incompleteTitle'),
                                text: t('telecom.swal.changeGsmTargetTypeRequired'),
                            });
                        }
                        return false;
                    }
                    if (!(state.wizard.cgtMigrationReason || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.incompleteTitle'),
                                text: t('telecom.swal.changeGsmReasonRequired'),
                            });
                        }
                        return false;
                    }
                    if (typeof TelecomBssWizardClearance !== 'undefined') {
                        const cgtErr = TelecomBssWizardClearance.changeGsm.validate(state.wizard, bssCgtOptions());
                        if (cgtErr) {
                            if (window.Swal) {
                                Swal.fire({
                                    icon: 'warning',
                                    title: t('telecom.swal.incompleteTitle'),
                                    text: t('telecom.' + cgtErr.key),
                                });
                            }
                            return false;
                        }
                    }
                }
                if (state.wizard.kind === 'addpackage') {
                    if (!(state.wizard.selectedVasCode || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.incompleteTitle'),
                                text: t('telecom.offerSubscription.vasBlocked'),
                            });
                        }
                        return false;
                    }
                    if (!(state.wizard.primaryMsisdn || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.incompleteTitle'),
                                text: t('telecom.swal.selectSubscriberMsisdn'),
                            });
                        }
                        return false;
                    }
                }
                if (state.wizard.kind === 'support') {
                    if (!(state.wizard.primaryMsisdn || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.customerList.supportTicket.noMsisdn'),
                            });
                        }
                        return false;
                    }
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
                    const hasOffering = (state.migrationEligibleProducts || []).some((p) => p?.id === sel);
                    if (!hasOffering) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.targetOfferTitle'),
                                text: t('telecom.swal.targetOfferText'),
                            });
                        }
                        return false;
                    }
                    if (state.wizard.kind === 'migrate' && typeof TelecomBssWizardClearance !== 'undefined') {
                        const prErr = TelecomBssWizardClearance.migration.validatePreview(
                            state.wizard.mgrProrationPreview
                        );
                        if (prErr) {
                            if (window.Swal) {
                                Swal.fire({
                                    icon: 'warning',
                                    title: t('telecom.swal.incompleteTitle'),
                                    text: t('telecom.' + prErr.key),
                                });
                            }
                            return false;
                        }
                    }
                }
                if (wizardSupportsEffectiveDate(state.wizard.kind)
                    && typeof TelecomBssWizardClearance !== 'undefined') {
                    const effErr = TelecomBssWizardClearance.effectiveDate.validate(state.wizard);
                    if (effErr) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: t('telecom.swal.incompleteTitle'),
                                text: t('telecom.' + effErr.key),
                            });
                        }
                        return false;
                    }
                }
                return true;
            };

            const wizardStepNext = async () => {
                if (state.wizard.step === 1) {
                    if (!(await validateWizardBeforeCreateDraft())) return;
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

            const loadChangeNumberPool = async () => {
                state.wizard.cnPoolBusy = true;
                try {
                    const res = await AxiosManager.get('/Telecom/GetMsisdnAssetPoolList?status=Available', {});
                    const rows = res?.data?.content?.data ?? res?.data?.content?.Data ?? [];
                    const currentId = (state.wizard.primaryMsisdnAssetId || '').trim();
                    state.wizard.cnPoolNumbers = (Array.isArray(rows) ? rows : []).filter((r) => {
                        const id = r.id ?? r.Id;
                        if (currentId && String(id) === currentId) return false;
                        const name = String(r.poolStatusName ?? r.PoolStatusName ?? '').toLowerCase();
                        const st = r.poolStatus ?? r.PoolStatus;
                        return name === 'available' || st === 0 || st === '0' || st === 'Available';
                    });
                } catch {
                    state.wizard.cnPoolNumbers = [];
                } finally {
                    state.wizard.cnPoolBusy = false;
                }
            };

            const onChangeNumberTargetPicked = () => {
                const id = (state.wizard.cnTargetMsisdnAssetId || '').trim();
                const row = (state.wizard.cnPoolNumbers || []).find((r) => String(r.id ?? r.Id) === id);
                const cat = row?.category ?? row?.Category;
                state.wizard.cnRequiresBackOffice =
                    (state.wizard.cnChangeMode || 'Internal') === 'PortIn' || isPremiumMsisdnCategory(cat);
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    TelecomBssWizardClearance.changeNumber.reset(state.wizard, state.wizard.cnRequiresBackOffice);
                } else if (!state.wizard.cnRequiresBackOffice) {
                    state.wizard.cnPremiumFeeAmount = '';
                }
            };

            const onCnChangeModeChange = () => {
                const portIn = (state.wizard.cnChangeMode || 'Internal') === 'PortIn';
                state.wizard.cnRequiresBackOffice = portIn;
                state.wizard.cnTargetMsisdnAssetId = '';
                state.wizard.cnPremiumFeeAmount = '';
                if (portIn) {
                    state.wizard.cnNumberChangeReason = state.wizard.cnNumberChangeReason || 'PortIn';
                }
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    TelecomBssWizardClearance.changeNumber.reset(state.wizard, state.wizard.cnRequiresBackOffice);
                }
                if (!portIn && (state.wizard.primaryMsisdnAssetId || '').trim()) {
                    loadChangeNumberPool();
                }
            };

            const submitVasFromHub = async () => {
                const msisdn = (state.wizard.primaryMsisdn || '').trim();
                const code = (state.wizard.selectedVasCode || '').trim();
                if (!msisdn || !code) return;
                state.wizard.submitBusy = true;
                try {
                    const result =
                        typeof TelecomVasToggle !== 'undefined'
                            ? await TelecomVasToggle.toggle(AxiosManager, {
                                  msisdn,
                                  serviceCode: code,
                                  activate: (state.wizard.vasAction || 'Activate') !== 'Deactivate',
                              })
                            : null;
                    if (!result?.ok) {
                        throw new Error(t('telecom.swal.failTitle'));
                    }
                    state.wizard.vasActivated = true;
                    state.wizard.documentMarkedUploaded = true;
                    state.wizard.createdOperationNumber = result.operationNumber || '';
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: t('telecom.swal.createOkTitle'),
                            html: state.wizard.createdOperationNumber
                                ? `<p>${state.wizard.createdOperationNumber}</p>`
                                : undefined,
                            timer: 2200,
                            showConfirmButton: false,
                        });
                    }
                    state.wizard.step = 3;
                } catch (e) {
                    const errName = e?.response?.data?.error?.name;
                    const msg =
                        typeof TelecomVasToggle !== 'undefined'
                            ? TelecomVasToggle.pickError(e)
                            : pickHttpErrorMessage(e, locale.value);
                    if (window.Swal) {
                        Swal.fire({
                            icon:
                                errName === 'BusinessRuleViolationException' ||
                                (typeof TelecomVasToggle !== 'undefined' && TelecomVasToggle.isBusinessRuleViolation(e))
                                    ? 'warning'
                                    : 'error',
                            title: 'VAL-11',
                            text: msg,
                        });
                    }
                } finally {
                    state.wizard.submitBusy = false;
                }
            };

            const submitSupportFromHub = async () => {
                const msisdn = (state.wizard.primaryMsisdn || '').trim();
                if (!msisdn) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.customerList.supportTicket.noMsisdn'),
                        });
                    }
                    return;
                }
                state.wizard.submitBusy = true;
                try {
                    const res = await AxiosManager.post('/TelecomBackOffice/CreateTechnicalTicket', {
                        msisdn,
                        issueType: Number(state.wizard.supportIssueType) || 0,
                        priority: 1,
                        notes: (state.wizard.supportNotes || '').trim(),
                        customerId: state.wizard.customerId || resolveWizardCustomerId(state.wizard) || null,
                        subscriberProfileId: (state.wizard.primarySubscriberProfileId || '').trim() || null,
                    });
                    if (res?.data?.code !== 200) {
                        throw Object.assign(new Error(res?.data?.message || t('telecom.customerList.supportTicket.createFail')), {
                            response: res,
                        });
                    }
                    const ticket = res?.data?.content?.data ?? res?.data?.content?.Data;
                    state.wizard.supportTicketCreated = true;
                    state.wizard.supportTicketId = ticket?.id ?? ticket?.Id ?? '';
                    state.wizard.supportTicketNumber = ticket?.ticketNumber ?? ticket?.TicketNumber ?? '';
                    state.wizard.createdOperationNumber = state.wizard.supportTicketNumber;
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: t('telecom.customerList.supportTicket.createdOk'),
                            html: state.wizard.supportTicketNumber
                                ? `<p dir="ltr">${state.wizard.supportTicketNumber}</p>`
                                : undefined,
                            timer: 2200,
                            showConfirmButton: false,
                        });
                    }
                    state.wizard.step = 3;
                } catch (e) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'error',
                            title: pickHttpErrorMessage(e, locale.value) || t('telecom.customerList.supportTicket.createFail'),
                        });
                    }
                } finally {
                    state.wizard.submitBusy = false;
                }
            };

            const submitCreateOperation = async () => {
                if (!(await validateWizardBeforeCreateDraft())) return;
                if (state.wizard.kind === 'addpackage') {
                    await submitVasFromHub();
                    return;
                }
                if (state.wizard.kind === 'support') {
                    await submitSupportFromHub();
                    return;
                }
                if (state.wizard.kind === 'changeNumber' && state.wizard.cnTargetMsisdnAssetId) {
                    try {
                        await AxiosManager.post('/Telecom/ReserveMsisdnForCustomer', {
                            msisdnAssetId: state.wizard.cnTargetMsisdnAssetId,
                            customerId: state.wizard.customerId || state.wizard.primaryRowKey?.split('|')?.[0] || '',
                        });
                    } catch (e) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'error', title: t('telecom.swal.createFailTitle'), text: pickHttpErrorMessage(e) });
                        }
                        return;
                    }
                }
                state.wizard.submitBusy = true;
                try {
                    if (
                        state.wizard.kind === 'reconnect'
                        && typeof TelecomReconnectClearance !== 'undefined'
                    ) {
                        await TelecomReconnectClearance.ensureRegulatoryAttachmentUploaded(
                            state.wizard,
                            state.wizard.primaryLabel || ''
                        );
                    }
                    if (typeof TelecomBssWizardClearance !== 'undefined') {
                        const msisdn = state.wizard.primaryLabel || '';
                        if (state.wizard.kind === 'suspension') {
                            await TelecomBssWizardClearance.suspension.ensureUploads(state.wizard, msisdn);
                        }
                        if (state.wizard.kind === 'termination') {
                            await TelecomBssWizardClearance.termination.ensureUploads(state.wizard, msisdn);
                        }
                        if (state.wizard.kind === 'changeGsm') {
                            await TelecomBssWizardClearance.changeGsm.ensureUploads(
                                state.wizard,
                                msisdn,
                                bssCgtOptions()
                            );
                        }
                    }
                    const rcnApiFields = state.wizard.kind === 'reconnect' && typeof TelecomReconnectClearance !== 'undefined'
                        ? TelecomReconnectClearance.buildApiFields(state.wizard, { bdrApproved: !!state.wizard.rcnBdrApproved })
                        : null;
                    const susApi = state.wizard.kind === 'suspension' && typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.suspension.buildApi(state.wizard)
                        : {};
                    const trmApi = state.wizard.kind === 'termination' && typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.termination.buildApi(state.wizard)
                        : {};
                    const cgtApi = state.wizard.kind === 'changeGsm' && typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.changeGsm.buildApi(state.wizard, bssCgtOptions())
                        : {};
                    const rfdApi = state.wizard.kind === 'refund' && typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.refund.buildApi(state.wizard)
                        : {};
                    const simApi = state.wizard.kind === 'simswap' && typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.simSwap.buildApi(state.wizard)
                        : {};
                    const cnApi = state.wizard.kind === 'changeNumber' && typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.changeNumber.buildApi(state.wizard)
                        : {};
                    const tkoApi = state.wizard.kind === 'takeover' && typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.takeOver.buildApi(state.wizard, bssTkoOptions())
                        : {};
                    const body = {
                        kind: kindToApiEnum(),
                        subscriberProfileId: state.wizard.primarySubscriberProfileId,
                        secondarySubscriberProfileId: state.wizard.secondarySubscriberProfileId || null,
                        msisdnAssetId: state.wizard.kind === 'activate' ? (state.wizard.secondaryMsisdnAssetId || null) : (state.wizard.primaryMsisdnAssetId || null),
                        productId: null,
                        productOfferingId:
                            (state.wizard.kind === 'migrate' || state.wizard.kind === 'activate') &&
                            (state.migrationEligibleProducts || []).some((p) => p?.id === state.wizard.migrationTargetProductId)
                                ? state.wizard.migrationTargetProductId
                                : null,
                        notes: (state.wizard.notes || '').trim() || null,
                        targetOfferName: state.wizard.kind === 'migrate' || state.wizard.kind === 'activate' ? buildMigrateTargetOfferPayload() : null,
                        simIccid:
                            state.wizard.kind === 'activate' || state.wizard.kind === 'simswap'
                                ? (state.wizard.simIccid || '').trim() || null
                                : null,
                        activationEffectiveDateUtc:
                            state.wizard.kind === 'activate' && typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                                : state.wizard.kind === 'activate'
                                    ? new Date().toISOString()
                                    : null,
                        replacementReason:
                            state.wizard.kind === 'simswap' ? (state.wizard.simReplacementReason || '').trim() || null : null,
                        isLostOrStolenReport: state.wizard.kind === 'simswap' ? !!state.wizard.simLostOrStolen : false,
                        simSwapEffectiveDateUtc:
                            state.wizard.kind === 'simswap' && typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                                : state.wizard.kind === 'simswap'
                                    ? new Date().toISOString()
                                    : null,
                        targetSubscriptionTypeId:
                            state.wizard.kind === 'changeGsm'
                                ? state.wizard.cgtTargetTypeId || null
                                : state.wizard.kind === 'activate'
                                  ? (state.wizard.activationLineTypeId || '').trim() || null
                                  : null,
                        gsmMigrationReason:
                            state.wizard.kind === 'changeGsm' ? (state.wizard.cgtMigrationReason || '').trim() || null : null,
                        changeGsmProductOfferingId:
                            state.wizard.kind === 'changeGsm' && (state.wizard.cgtProductOfferingId || '').trim()
                                ? state.wizard.cgtProductOfferingId.trim()
                                : null,
                        transferReason:
                            state.wizard.kind === 'takeover' ? (state.wizard.tkoTransferReason || '').trim() || null : null,
                        depositTransferPolicy:
                            state.wizard.kind === 'takeover' ? Number(state.wizard.tkoDepositPolicy) : null,
                        takeOverObligationStatus:
                            state.wizard.kind === 'takeover' ? (tkoApi.takeOverObligationStatus ?? null) : null,
                        takeOverEffectiveDateUtc:
                            state.wizard.kind === 'takeover' && typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                                : state.wizard.kind === 'takeover'
                                    ? new Date().toISOString()
                                    : null,
                        gsmEffectiveDateUtc:
                            state.wizard.kind === 'changeGsm' && typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                                : state.wizard.kind === 'changeGsm'
                                    ? new Date().toISOString()
                                    : null,
                        migrationEffectiveDateUtc:
                            state.wizard.kind === 'migrate' && typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                                : state.wizard.kind === 'migrate'
                                    ? new Date().toISOString()
                                    : null,
                        targetMsisdnAssetId:
                            state.wizard.kind === 'changeNumber' && cnShowsInternalPool.value
                                ? state.wizard.cnTargetMsisdnAssetId || null
                                : null,
                        numberChangeMode:
                            state.wizard.kind === 'changeNumber'
                                ? (state.wizard.cnChangeMode || 'Internal')
                                : null,
                        portInMsisdn:
                            state.wizard.kind === 'changeNumber' && !cnShowsInternalPool.value
                                ? (state.wizard.cnPortInMsisdn || '').trim() || null
                                : null,
                        donorOperatorCode:
                            state.wizard.kind === 'changeNumber' && !cnShowsInternalPool.value
                                ? (state.wizard.cnDonorOperatorCode || '').trim() || null
                                : null,
                        numberChangeReason:
                            state.wizard.kind === 'changeNumber'
                                ? (state.wizard.cnNumberChangeReason || '').trim() || null
                                : null,
                        premiumFeeAmount:
                            state.wizard.kind === 'changeNumber' && state.wizard.cnPremiumFeeAmount
                                ? Number(state.wizard.cnPremiumFeeAmount)
                                : null,
                        numberChangeEffectiveDateUtc:
                            state.wizard.kind === 'changeNumber' && typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                                : state.wizard.kind === 'changeNumber'
                                    ? new Date().toISOString()
                                    : null,
                        terminationType:
                            state.wizard.kind === 'termination' ? (state.wizard.trmTerminationType || '').trim() || null : null,
                        terminationReason:
                            state.wizard.kind === 'termination'
                                ? (state.wizard.trmTerminationReason || '').trim() || null
                                : null,
                        retentionOfferOutcome:
                            state.wizard.kind === 'termination' && state.wizard.trmTerminationType === 'Voluntary'
                                ? (state.wizard.trmRetentionOfferOutcome || '').trim() || null
                                : null,
                        terminationEffectiveDateUtc:
                            state.wizard.kind === 'termination' && typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                                : state.wizard.kind === 'termination'
                                    ? new Date().toISOString()
                                    : null,
                        suspensionType:
                            state.wizard.kind === 'suspension' ? (state.wizard.susSuspensionType || '').trim() || null : null,
                        suspensionReason:
                            state.wizard.kind === 'suspension' ? (state.wizard.susSuspensionReason || '').trim() || null : null,
                        barringLevel:
                            state.wizard.kind === 'suspension' ? (state.wizard.susBarringLevel || 'Full').trim() : null,
                        autoReconnectEnabled: state.wizard.kind === 'suspension' ? !!state.wizard.susAutoReconnectEnabled : false,
                        suspensionStartDateUtc:
                            state.wizard.kind === 'suspension' && typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                                : state.wizard.kind === 'suspension'
                                    ? new Date().toISOString()
                                    : null,
                        suspensionEndDateUtc:
                            state.wizard.kind === 'suspension' && state.wizard.susEndDateLocal
                                ? new Date(state.wizard.susEndDateLocal + 'T23:59:59').toISOString()
                                : null,
                        reconnectReason:
                            state.wizard.kind === 'reconnect' ? (state.wizard.rcnReconnectReason || '').trim() || null : null,
                        reconnectEffectiveDateUtc:
                            state.wizard.kind === 'reconnect' && typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                                : state.wizard.kind === 'reconnect'
                                    ? new Date().toISOString()
                                    : null,
                        clearanceType:
                            state.wizard.kind === 'reconnect' ? (state.wizard.rcnClearanceType || '').trim() || null : null,
                        fraudClearanceConfirmed:
                            state.wizard.kind === 'reconnect'
                                ? (rcnApiFields?.fraudClearanceConfirmed ?? !!state.wizard.rcnFraudClearanceConfirmed)
                                : state.wizard.kind === 'suspension'
                                    ? (susApi.fraudClearanceConfirmed ?? false)
                                    : false,
                        deviceInventoryId:
                            state.wizard.kind === 'deviceSale' ? state.wizard.devInventoryId || null : null,
                        deviceSaleType:
                            state.wizard.kind === 'deviceSale'
                                ? state.wizard.devSaleType === 'Installment' ? 1 : 0
                                : null,
                        deviceInstallmentPlanId:
                            state.wizard.kind === 'deviceSale' && state.wizard.devSaleType === 'Installment'
                                ? state.wizard.devInstallmentPlanId || null
                                : null,
                        deviceSaleEffectiveDateUtc:
                            state.wizard.kind === 'deviceSale' && typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                                : state.wizard.kind === 'deviceSale'
                                    ? new Date().toISOString()
                                    : null,
                        refundType:
                            state.wizard.kind === 'refund' ? (state.wizard.rfdRefundType || '').trim() || null : null,
                        refundMethod:
                            state.wizard.kind === 'refund' ? (state.wizard.rfdRefundMethod || '').trim() || null : null,
                        refundReason:
                            state.wizard.kind === 'refund' ? (state.wizard.rfdRefundReason || '').trim() || null : null,
                        refundAmount:
                            state.wizard.kind === 'refund' && state.wizard.rfdRefundAmount
                                ? Number(state.wizard.rfdRefundAmount)
                                : null,
                        refundCbsReference:
                            state.wizard.kind === 'refund' ? (rfdApi.refundCbsReference ?? null) : null,
                        refundGatewayReference:
                            state.wizard.kind === 'refund' ? (rfdApi.refundGatewayReference ?? null) : null,
                        refundEffectiveDateUtc:
                            state.wizard.kind === 'refund' && typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                                : state.wizard.kind === 'refund'
                                    ? new Date().toISOString()
                                    : null,
                        collectionAction:
                            state.wizard.kind === 'badDebt' ? (state.wizard.bdrCollectionAction || '').trim() || null : null,
                        dunningStage:
                            state.wizard.kind === 'badDebt' ? (state.wizard.bdrDunningStage || '').trim() || null : null,
                        collectedAmount:
                            state.wizard.kind === 'badDebt' && state.wizard.bdrCollectedAmount
                                ? Number(state.wizard.bdrCollectedAmount)
                                : null,
                        writeOffAmount:
                            state.wizard.kind === 'badDebt' && state.wizard.bdrWriteOffAmount
                                ? Number(state.wizard.bdrWriteOffAmount)
                                : null,
                        paymentReference:
                            state.wizard.kind === 'badDebt'
                                ? (state.wizard.bdrPaymentReference || '').trim() || null
                                : state.wizard.kind === 'reconnect'
                                    ? (rcnApiFields?.paymentReference ?? ((state.wizard.rcnPaymentReference || '').trim() || null))
                                    : state.wizard.kind === 'termination'
                                        ? (trmApi.paymentReference ?? null)
                                        : state.wizard.kind === 'suspension'
                                            ? (susApi.paymentReference ?? null)
                                            : state.wizard.kind === 'changeGsm'
                                                ? (cgtApi.paymentReference ?? null)
                                                : state.wizard.kind === 'changeNumber'
                                                    ? (cnApi.paymentReference ?? null)
                                                    : state.wizard.kind === 'takeover'
                                                        ? (tkoApi.paymentReference ?? null)
                                                        : null,
                        agencyReference:
                            state.wizard.kind === 'badDebt'
                                ? (state.wizard.bdrAgencyReference || '').trim() || null
                                : state.wizard.kind === 'reconnect'
                                    ? (rcnApiFields?.agencyReference ?? null)
                                    : state.wizard.kind === 'termination'
                                        ? (trmApi.agencyReference ?? null)
                                        : state.wizard.kind === 'suspension'
                                            ? (susApi.agencyReference ?? null)
                                            : state.wizard.kind === 'simswap'
                                                ? (simApi.agencyReference ?? null)
                                                : state.wizard.kind === 'changeNumber'
                                                    ? (cnApi.agencyReference ?? null)
                                                    : null,
                        collectionNote:
                            state.wizard.kind === 'reconnect'
                                ? (rcnApiFields?.collectionNote ?? null)
                                : state.wizard.kind === 'termination'
                                    ? (trmApi.collectionNote ?? null)
                                    : state.wizard.kind === 'suspension'
                                        ? (susApi.collectionNote ?? null)
                                        : state.wizard.kind === 'changeGsm'
                                            ? (cgtApi.collectionNote ?? null)
                                            : null,
                        paymentPlanMonths:
                            state.wizard.kind === 'badDebt' && state.wizard.bdrPaymentPlanMonths
                                ? Number(state.wizard.bdrPaymentPlanMonths)
                                : null,
                        badDebtEffectiveDateUtc:
                            state.wizard.kind === 'badDebt' && typeof TelecomBssWizardClearance !== 'undefined'
                                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                                : state.wizard.kind === 'badDebt'
                                    ? new Date().toISOString()
                                    : null,
                        collectionApprovalConfirmed: !!state.wizard.bdrSupervisorConfirmed,
                        activationChannel:
                            state.wizard.kind === 'activate' ? Number(state.wizard.activationChannel) || 0 : null,
                        dealerCode:
                            state.wizard.kind === 'activate'
                                ? (state.wizard.dealerCode || '').trim() || null
                                : null,
                        kycDocumentReferenceId:
                            state.wizard.kind === 'activate'
                                ? (state.wizard.kycDocumentReferenceId || '').trim() || null
                                : state.wizard.kind === 'reconnect'
                                    ? (rcnApiFields?.kycDocumentReferenceId ?? null)
                                    : state.wizard.kind === 'termination'
                                        ? (trmApi.kycDocumentReferenceId ?? null)
                                        : state.wizard.kind === 'suspension'
                                            ? (susApi.kycDocumentReferenceId ?? null)
                                            : state.wizard.kind === 'changeGsm'
                                                ? (cgtApi.kycDocumentReferenceId ?? null)
                                                : null,
                    };
                    const res = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                    const ok = res?.data?.code === 200;
                    const entity = res?.data?.content?.data;
                    if (ok && entity?.id) {
                        state.wizard.createdOperationId = entity.id;
                        state.wizard.createdOperationNumber = entity.number || '';
                        state.wizard.cnRequiresBackOffice =
                            String(entity.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                            || state.wizard.cnRequiresBackOffice;
                        if (state.wizard.kind === 'termination') {
                            state.wizard.trmRequiresBackOffice =
                                String(entity.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                                || state.wizard.trmRequiresBackOffice;
                        }
                        if (state.wizard.kind === 'suspension') {
                            state.wizard.susRequiresBackOffice =
                                String(entity.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                                || state.wizard.susRequiresBackOffice;
                        }
                        if (state.wizard.kind === 'reconnect') {
                            state.wizard.rcnRequiresBackOffice =
                                String(entity.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                                || state.wizard.rcnRequiresBackOffice;
                        }
                        if (state.wizard.kind === 'deviceSale') {
                            state.wizard.devRequiresFinance =
                                !!entity.deviceApprovalLevelRequired || !!entity.approvalLevelRequired;
                            state.wizard.devFinancingPreview = entity.deviceFinancingNoteAr || entity.notes || '';
                            state.wizard.devDownPayment = String(entity.deviceDownPaymentAmount ?? state.wizard.devDownPayment ?? '');
                            state.wizard.documentMarkedUploaded = state.wizard.devSaleType === 'Cash';
                        }
                        if (state.wizard.kind === 'refund') {
                            state.wizard.rfdRequiresBackOffice =
                                String(entity.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                                || !!entity.requiresDualApproval;
                            state.wizard.rfdDepositSnapshot = entity.depositBalanceSnapshot ?? entity.DepositBalanceSnapshot ?? null;
                            state.wizard.rfdWalletSnapshot = entity.walletBalanceSnapshot ?? entity.WalletBalanceSnapshot ?? null;
                        }
                        if (state.wizard.kind === 'badDebt') {
                            state.wizard.bdrRequiresBackOffice =
                                String(entity.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                                || state.wizard.bdrRequiresBackOffice;
                        }
                        if (state.wizard.kind === 'activate' && state.wizard.kycDocumentReferenceId) {
                            state.wizard.documentMarkedUploaded = true;
                        }
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

            const KYC_ALLOWED_EXT = new Set(['.pdf', '.png', '.jpg', '.jpeg']);
            const KYC_MAX_BYTES = 5 * 1024 * 1024;

            const validateKycFile = (file) => {
                if (!file) return t('telecom.wizard.kycUpload.required');
                const name = (file.name || '').toLowerCase();
                const ext = name.includes('.') ? name.slice(name.lastIndexOf('.')) : '';
                if (!KYC_ALLOWED_EXT.has(ext)) {
                    return t('telecom.wizard.kycUpload.badType');
                }
                if (file.size > KYC_MAX_BYTES) {
                    return t('telecom.wizard.kycUpload.tooLarge');
                }
                return '';
            };

            const setKycThumbnailPreview = (file) => {
                if (state.wizard.kycThumbnailUrl) {
                    URL.revokeObjectURL(state.wizard.kycThumbnailUrl);
                    state.wizard.kycThumbnailUrl = '';
                }
                const name = (file?.name || '').toLowerCase();
                if (file && (name.endsWith('.png') || name.endsWith('.jpg') || name.endsWith('.jpeg'))) {
                    state.wizard.kycThumbnailUrl = URL.createObjectURL(file);
                }
            };

            const uploadKycDocument = async (file) => {
                const validationError = validateKycFile(file);
                if (validationError) {
                    state.wizard.kycUploadError = validationError;
                    return;
                }

                const msisdn = (state.wizard.activateMsisdnLabel || '').trim();
                if (!msisdn) {
                    state.wizard.kycUploadError = t('telecom.wizard.kycUpload.msisdnRequired');
                    return;
                }

                state.wizard.kycUploadError = '';
                state.wizard.kycUploadBusy = true;
                state.wizard.kycUploadProgress = 0;
                state.wizard.kycUploadFileName = file.name;

                const form = new FormData();
                form.append('msisdn', msisdn);
                form.append('file', file);

                try {
                    const token = StorageManager.getAccessToken?.() || '';
                    const baseUrl = (typeof AxiosManager !== 'undefined' && AxiosManager.getBaseUrl
                        ? AxiosManager.getBaseUrl()
                        : '/api').replace(/\/$/, '');
                    const url = `${baseUrl}/Telecom/UploadKycDocument`;

                    const payload = await new Promise((resolve, reject) => {
                        const xhr = new XMLHttpRequest();
                        xhr.open('POST', url, true);
                        if (token) xhr.setRequestHeader('Authorization', `Bearer ${token}`);
                        xhr.upload.onprogress = (ev) => {
                            if (ev.lengthComputable) {
                                state.wizard.kycUploadProgress = Math.min(100, Math.round((ev.loaded / ev.total) * 100));
                            }
                        };
                        xhr.onload = () => {
                            let data = null;
                            try {
                                data = JSON.parse(xhr.responseText || '{}');
                            } catch {
                                data = null;
                            }
                            if (xhr.status >= 200 && xhr.status < 300) {
                                resolve(data);
                            } else {
                                reject(new Error(data?.message || data?.Message || xhr.statusText || 'Upload failed'));
                            }
                        };
                        xhr.onerror = () => reject(new Error(t('telecom.wizard.kycUpload.network')));
                        xhr.send(form);
                    });

                    const refId = payload?.documentReferenceId || payload?.DocumentReferenceId || '';
                    const success = payload?.success ?? payload?.Success;
                    if (!success || !refId) {
                        throw new Error(pickApiUserMessage(payload, locale.value) || t('telecom.wizard.kycUpload.failed'));
                    }

                    state.wizard.kycDocumentReferenceId = refId;
                    state.wizard.kycUploadLocked = true;
                    state.wizard.kycUploadProgress = 100;
                    setKycThumbnailPreview(file);
                } catch (e) {
                    state.wizard.kycUploadLocked = false;
                    state.wizard.kycDocumentReferenceId = '';
                    state.wizard.kycUploadError = e?.message || t('telecom.wizard.kycUpload.failed');
                } finally {
                    state.wizard.kycUploadBusy = false;
                }
            };

            const onKycFileSelected = async (ev) => {
                const file = ev?.target?.files?.[0];
                if (!file || state.wizard.kycUploadLocked) return;
                await uploadKycDocument(file);
                if (ev?.target) ev.target.value = '';
            };

            const onKycDragEnter = () => {
                if (!state.wizard.kycUploadLocked && !state.wizard.kycUploadBusy) state.wizard.kycDropHover = true;
            };
            const onKycDragOver = () => {
                if (!state.wizard.kycUploadLocked && !state.wizard.kycUploadBusy) state.wizard.kycDropHover = true;
            };
            const onKycDragLeave = () => {
                state.wizard.kycDropHover = false;
            };
            const onKycDrop = async (ev) => {
                state.wizard.kycDropHover = false;
                if (state.wizard.kycUploadLocked || state.wizard.kycUploadBusy) return;
                const file = ev?.dataTransfer?.files?.[0];
                if (file) await uploadKycDocument(file);
            };

            const uploadWizardIdentityDocument = async (operationId) => {
                if (
                    (state.wizard.kind === 'takeover'
                        || (state.wizard.kind === 'simswap' && state.wizard.simLostOrStolen)
                        || (state.wizard.kind === 'changeNumber' && state.wizard.cnRequiresBackOffice)
                        || (state.wizard.kind === 'termination' && state.wizard.trmRequiresBackOffice)
                        || (state.wizard.kind === 'refund' && state.wizard.rfdRequiresBackOffice)
                        || (state.wizard.kind === 'suspension' && susRequiresStep2Identity.value && state.wizard.susRequiresBackOffice)
                        || (state.wizard.kind === 'badDebt' && state.wizard.bdrRequiresBackOffice))
                    && state.wizard.identityFile
                ) {
                    const form = new FormData();
                    form.append('id', operationId);
                    form.append('file', state.wizard.identityFile);
                    return AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                }
                return AxiosManager.post('/Telecom/UploadTelecomOperationDocument', {
                    id: operationId,
                });
            };

            const submitMarkDocumentUploaded = async () => {
                if (!state.wizard.createdOperationId) return;
                if (
                    (state.wizard.kind === 'takeover'
                        || (state.wizard.kind === 'simswap' && state.wizard.simLostOrStolen)
                        || (state.wizard.kind === 'changeNumber' && state.wizard.cnRequiresBackOffice)
                        || (state.wizard.kind === 'termination' && state.wizard.trmRequiresBackOffice)
                        || (state.wizard.kind === 'refund' && state.wizard.rfdRequiresBackOffice)
                        || (state.wizard.kind === 'suspension' && susRequiresStep2Identity.value && state.wizard.susRequiresBackOffice)
                        || (state.wizard.kind === 'badDebt' && state.wizard.bdrRequiresBackOffice))
                    && !state.wizard.identityFile
                ) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.docRequiredTitle'),
                            text:
                                state.wizard.kind === 'termination'
                                    ? t('telecom.termination.identityRequired')
                                    : state.wizard.kind === 'changeNumber'
                                    ? t('telecom.changeNumber.paymentDocHint')
                                    : state.wizard.kind === 'refund'
                                    ? t('telecom.refund.identityRequired', 'هوية المشترك / وثيقة الاسترداد')
                                    : state.wizard.kind === 'simswap'
                                        ? t('telecom.swal.docRequiredSubscriberIdentity')
                                        : state.wizard.kind === 'suspension'
                                            ? t('telecom.suspension.kycDocument')
                                            : state.wizard.kind === 'badDebt'
                                                ? t('telecom.badDebt.identityRequired', 'وثيقة التحصيل / الهوية')
                                                : t('telecom.swal.docRequiredNewOwnerIdentity'),
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
                                || (state.wizard.kind === 'simswap' && state.wizard.simLostOrStolen)
                                || (state.wizard.kind === 'changeNumber' && state.wizard.cnRequiresBackOffice)
                                || (state.wizard.kind === 'termination' && state.wizard.trmRequiresBackOffice)
                                || (state.wizard.kind === 'refund' && state.wizard.rfdRequiresBackOffice)
                                    ? t('telecom.swal.sentToBackOffice')
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

            const pollActivateOperationAfterConfirm = async (operationId) => {
                const terminal = new Set([3, 4, 'Completed', 'Failed']);
                for (let i = 0; i < 12; i++) {
                    await new Promise((r) => setTimeout(r, 1500));
                    try {
                        const res = await AxiosManager.get(
                            '/Telecom/GetTelecomOperationDetail?id=' + encodeURIComponent(operationId),
                            {}
                        );
                        const data = res?.data?.content?.data ?? res?.data?.content?.Data;
                        const st = data?.status ?? data?.Status;
                        state.wizard.operationCorrelationId =
                            data?.correlationId ?? data?.CorrelationId ?? '';
                        state.wizard.falloutTicketId =
                            data?.technicalTicketId ?? data?.TechnicalTicketId ?? '';
                        state.wizard.falloutTicketNumber =
                            data?.technicalTicketNumber ?? data?.TechnicalTicketNumber ?? '';
                        if (terminal.has(st)) {
                            return data?.statusLabelAr ?? data?.StatusLabelAr ?? '';
                        }
                    } catch {
                        /* retry */
                    }
                }
                return '';
            };

            const fetchPaymentFromCashier = async () => {
                const ref = (state.wizard.paymentReference || '').trim();
                if (!ref) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.incompleteTitle'),
                            text: t('telecom.wizardUi.paymentRefPh', 'Payment reference'),
                        });
                    }
                    return;
                }
                state.wizard.cashierFetchBusy = true;
                try {
                    const res = await AxiosManager.post('/Telecom/FetchCashierPayment', {
                        paymentReference: ref,
                        operationId: state.wizard.createdOperationId || null,
                        expectedAmount: activateRequiredDeposit.value > 0 ? activateRequiredDeposit.value : null,
                    });
                    const data = res?.data?.content?.data ?? res?.data?.content?.Data;
                    if (res?.data?.code === 200 && data) {
                        const amt = data.amountPaid ?? data.AmountPaid;
                        if (amt != null) state.wizard.paymentAmount = String(amt);
                        const ch = data.paymentChannel ?? data.PaymentChannel;
                        if (ch != null) state.wizard.paymentChannel = Number(ch);
                        state.wizard.paymentReference = data.paymentReference ?? data.PaymentReference ?? ref;
                        state.wizard.paymentCashierLocked = true;
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'success',
                                title: t('telecom.wizardUi.cashierFetched', 'Loaded from cashier'),
                                timer: 1200,
                                showConfirmButton: false,
                            });
                        }
                    } else {
                        throw Object.assign(new Error(res?.data?.message || t('telecom.swal.failTitle')), { response: res });
                    }
                } catch (e) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.failTitle'), text: pickHttpErrorMessage(e) });
                    }
                } finally {
                    state.wizard.cashierFetchBusy = false;
                }
            };

            const submitWizardRecordPayment = async () => {
                if (!state.wizard.createdOperationId || state.wizard.kind !== 'activate') return;
                const ref = (state.wizard.paymentReference || '').trim();
                const amt = Number(state.wizard.paymentAmount);
                if (!ref || !(amt > 0)) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.incompleteTitle'),
                            text: t('telecom.swal.paymentDownRequired'),
                        });
                    }
                    return;
                }
                if (activateRequiredDeposit.value > 0 && !state.wizard.paymentCashierLocked) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.wizardUi.fetchFromCashier', 'Fetch from cashier'),
                        });
                    }
                    return;
                }
                state.wizard.paymentBusy = true;
                try {
                    const res = await AxiosManager.post('/Telecom/RecordSellingLinePayment', {
                        operationId: state.wizard.createdOperationId,
                        paymentReference: ref,
                        amountPaid: amt,
                        paymentChannel: Number(state.wizard.paymentChannel) || 0,
                    });
                    if (res?.data?.code === 200) {
                        state.wizard.paymentRecorded = true;
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'success',
                                title: t('telecom.swal.paymentRecordedOk'),
                                timer: 1200,
                                showConfirmButton: false,
                            });
                        }
                    } else {
                        throw Object.assign(new Error(res?.data?.message || t('telecom.swal.failTitle')), { response: res });
                    }
                } catch (e) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.failTitle'), text: pickHttpErrorMessage(e) });
                    }
                } finally {
                    state.wizard.paymentBusy = false;
                }
            };

            const submitWizardConfirmCbs = async () => {
                if (!state.wizard.createdOperationId) return;
                if (!hubPermissions.value.canConfirmCbs) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: t('telecom.swal.permConfirmCbs'),
                        });
                    }
                    return;
                }
                if (
                    state.wizard.kind === 'activate'
                    && activateRequiredDeposit.value > 0
                    && !state.wizard.paymentRecorded
                ) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.incompleteTitle'),
                            text: t('telecom.swal.paymentDownRequired'),
                        });
                    }
                    return;
                }
                state.wizard.confirmBusy = true;
                try {
                    const res = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                        id: state.wizard.createdOperationId,
                    });
                    if (res?.data?.code === 200) {
                        state.wizard.confirmed = true;
                        const content = res?.data?.content ?? res?.data?.Content ?? {};
                        const hint =
                            (locale.value === 'ar'
                                ? (content.statusHintAr ?? content.StatusHintAr)
                                : (content.statusHintEn ?? content.StatusHintEn))
                            ?? content.userMessageAr
                            ?? content.UserMessageAr
                            ?? content.userMessageEn
                            ?? content.UserMessageEn
                            ?? t('telecom.swal.cbsConfirmHint');
                        state.wizard.confirmStatusHint = hint;
                        const scheduled =
                            window.TelecomUiBadges?.isScheduledOperationStatus?.(
                                window.TelecomUiBadges?.operationStatusFromConfirm?.(content)
                            ) ?? false;
                        state.wizard.confirmScheduled = scheduled;
                        if ((content.hlrCompletesAsynchronously ?? content.HlrCompletesAsynchronously) && !scheduled) {
                            const polled = await pollActivateOperationAfterConfirm(state.wizard.createdOperationId);
                            if (polled) state.wizard.confirmStatusHint = polled;
                        }
                        if (window.Swal) {
                            if (window.TelecomUiBadges?.showConfirmToast) {
                                window.TelecomUiBadges.showConfirmToast(res, {
                                    locale: locale.value,
                                    successTitle: t('telecom.swal.cbsConfirmHint'),
                                });
                            } else {
                                Swal.fire({
                                    icon: scheduled ? 'info' : 'success',
                                    title: t('telecom.swal.cbsConfirmHint'),
                                    text: state.wizard.confirmStatusHint,
                                    timer: scheduled ? 4200 : 2200,
                                    showConfirmButton: scheduled,
                                });
                            }
                        }
                        await refreshAll();
                    } else if (window.Swal) {
                        const content = res?.data?.content ?? res?.data?.Content ?? res?.data ?? {};
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.provisioningIncomplete'),
                            text: pickApiUserMessage(content, locale.value) || pickApiUserMessage(res?.data, locale.value) || '',
                        });
                    }
                } catch (e) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.failTitle'), text: pickHttpErrorMessage(e, locale.value) });
                    }
                } finally {
                    state.wizard.confirmBusy = false;
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

            const openSecureOpApproval = async (op) => {
                if (!op?.id) return;
                state.takeOverApproval.kind = Number(op.kind) || 0;
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
                            title: t('telecom.swal.loadOpDetailFailed'),
                            text: pickHttpErrorMessage(e),
                        });
                    }
                } finally {
                    state.takeOverApproval.busy = false;
                }
            };

            const pollTakeOverAfterApprove = async (operationId) => {
                const terminal = new Set([3, 4, 'Completed', 'Failed']);
                for (let i = 0; i < 15; i++) {
                    await new Promise((r) => setTimeout(r, 2000));
                    try {
                        const res = await AxiosManager.get(
                            '/Telecom/GetTelecomOperationDetail?id=' + encodeURIComponent(operationId),
                            {}
                        );
                        const data = res?.data?.content?.data ?? res?.data?.content?.Data;
                        const st = data?.status ?? data?.Status;
                        if (terminal.has(st)) {
                            return {
                                done: true,
                                ok: st === 3 || st === 'Completed',
                                label: data?.statusLabelAr ?? data?.StatusLabelAr ?? '',
                            };
                        }
                    } catch {
                        /* retry */
                    }
                }
                return { done: false, ok: false, label: '' };
            };

            const approveSecureOpOnNetwork = async () => {
                if (!state.takeOverApproval.operationId) return;
                const kind = Number(state.takeOverApproval.kind);
                if (kind === 3 && !canApproveSecureOp.value) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: t('telecom.swal.permSimSwapApprove'),
                        });
                    }
                    return;
                }
                if (kind === 5 && !canApproveSecureOp.value) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: t('telecom.swal.permChangeNumberApprove'),
                        });
                    }
                    return;
                }
                if (kind === 7 && !canApproveSecureOp.value) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: t('telecom.swal.permTerminationApprove'),
                        });
                    }
                    return;
                }
                if (kind === 8 && !hubPermissions.value.canApproveSuspension) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: t('telecom.swal.permSuspensionApprove'),
                        });
                    }
                    return;
                }
                if (kind === 9 && !hubPermissions.value.canApproveReconnect) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: t('telecom.swal.permReconnectApprove'),
                        });
                    }
                    return;
                }
                if (kind === 11 && !hubPermissions.value.canApproveRefund) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: t('telecom.swal.permRefundApprove'),
                        });
                    }
                    return;
                }
                if (kind === 12 && !hubPermissions.value.canApproveCollection) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: t('telecom.swal.permCollectionApprove'),
                        });
                    }
                    return;
                }
                if (kind === 2 && !hubPermissions.value.canApproveTakeOver) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: t('telecom.swal.permTakeoverApprove'),
                        });
                    }
                    return;
                }
                const opId = state.takeOverApproval.operationId;
                state.takeOverApproval.approveBusy = true;
                try {
                    const res = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                        id: opId,
                    });
                    const content = res?.data?.content;
                    const br = content?.billingResult;
                    if (res?.data?.code === 200 && (br?.success || content?.idempotentReplay)) {
                        const hint =
                            content?.statusHintAr ??
                            content?.StatusHintAr ??
                            t('telecom.swal.cbsConfirmHint');
                        const doneTitle = secureOpTitle(kind, 'done');
                        const failTitle = secureOpTitle(kind, 'fail');
                        const okTitle = secureOpTitle(kind, 'ok');
                        if (content?.hlrCompletesAsynchronously ?? content?.HlrCompletesAsynchronously) {
                            const polled = await pollTakeOverAfterApprove(opId);
                            if (polled.done) {
                                if (window.Swal) {
                                    Swal.fire({
                                        icon: polled.ok ? 'success' : 'warning',
                                        title: polled.ok ? doneTitle : failTitle,
                                        text: polled.label || hint,
                                        timer: polled.ok ? 2400 : undefined,
                                        showConfirmButton: !polled.ok,
                                    });
                                }
                            } else if (window.Swal) {
                                Swal.fire({ icon: 'info', title: t('telecom.swal.provisioningPending'), text: hint });
                            }
                        } else if (window.Swal) {
                            Swal.fire({
                                icon: 'success',
                                title: okTitle,
                                text: hint,
                                timer: 2200,
                                showConfirmButton: false,
                            });
                        }
                        closeTakeOverApproval();
                        await refreshAll();
                    } else if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.provisioningIncomplete'),
                            text: br?.message || res?.data?.message || '',
                        });
                    }
                } catch (e) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'error', title: t('telecom.swal.approvalFailed'), text: pickHttpErrorMessage(e) });
                    }
                } finally {
                    state.takeOverApproval.approveBusy = false;
                }
            };

            const openTakeOverApproval = openSecureOpApproval;
            const approveTakeOverOnNetwork = approveSecureOpOnNetwork;

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
                    });
                    const content = res?.data?.content;
                    const br = content?.billingResult;
                    if (res?.data?.code === 200 && content?.idempotentReplay) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'info',
                                title: t('telecom.swal.alreadyProcessedTitle'),
                                text: t('telecom.swal.alreadyProcessedText'),
                                timer: 2200,
                                showConfirmButton: false,
                            });
                        }
                        await refreshAll();
                    } else if (res?.data?.code === 200 && br?.success) {
                        const scheduled =
                            window.TelecomUiBadges?.isScheduledOperationStatus?.(
                                window.TelecomUiBadges?.operationStatusFromConfirm?.(content)
                            ) ?? false;
                        if (window.TelecomUiBadges?.showConfirmToast) {
                            window.TelecomUiBadges.showConfirmToast(res, {
                                locale: locale.value,
                                successTitle: t('telecom.swal.confirmOkTitle'),
                            });
                        } else if (window.Swal) {
                            Swal.fire({
                                icon: scheduled ? 'info' : 'success',
                                title: scheduled
                                    ? (content?.statusHintAr ?? content?.StatusHintAr ?? t('telecom.ops.statusLabels.11'))
                                    : t('telecom.swal.confirmOkTitle'),
                                text: scheduled
                                    ? (content?.statusHintAr ?? content?.StatusHintAr ?? '')
                                    : undefined,
                                timer: scheduled ? 4200 : 1400,
                                showConfirmButton: scheduled,
                            });
                        }
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
                    if (state.wizard.kind === 'migrate') {
                        if (!(state.wizard.primarySubscriberProfileId || '').trim()) return;
                        state.wizard.migrationTargetProductId = '';
                        state.wizard.targetOfferOtherText = '';
                        loadMigrationEligibleProducts();
                    }
                    if (state.wizard.kind === 'changeNumber') {
                        const row = (state.wizard.primaryResults || []).find(
                            (r) => r.msisdnAssetId === state.wizard.primaryMsisdnAssetId
                        );
                        state.wizard.cnCurrentMsisdn = row?.msisdn || state.wizard.primaryLabel || '';
                        state.wizard.cnTargetMsisdnAssetId = '';
                        loadChangeNumberPool();
                    }
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

            const pollHubPaymentDetail = async (paymentId) => {
                if (typeof TelecomRechargeFlow !== 'undefined') {
                    return TelecomRechargeFlow.pollPaymentDetail(AxiosManager, paymentId, 12);
                }
                for (let i = 0; i < 12; i++) {
                    const res = await AxiosManager.get(
                        '/Telecom/GetPaymentTransactionDetail?id=' + encodeURIComponent(paymentId)
                    );
                    const detail = res?.data?.content ?? res?.data?.Content;
                    const status = detail?.status ?? detail?.Status;
                    if (status === 2 || status === 'Completed') return detail;
                    if (status === 3 || status === 'Failed') {
                        throw new Error(detail?.failureReason || detail?.FailureReason || 'Payment failed');
                    }
                    await new Promise((r) => setTimeout(r, 500));
                }
                return null;
            };

            const normalizeMsisdnDigits = (m) => String(m || '').replace(/\D/g, '');

            const hubRechargeFlowLabels = () => ({
                methodTitle: t('telecom.customerList.swal.rechargeMethod'),
                wallet: t('telecom.customerList.swal.rechargeWallet'),
                voucher: t('telecom.customerList.swal.rechargeVoucher'),
                continueBtn: t('telecom.customerList.swal.continueBtn'),
                cancelBtn: t('telecom.common.cancel'),
                amountTitle: t('telecom.swal.rechargeTitle'),
                amountPlaceholder: '15000',
                invalidAmount: t('telecom.swal.rechargeInvalidAmount'),
                paymentRefTitle: t('telecom.swal.rechargePaymentRefTitle'),
                confirmBtn: t('telecom.swal.rechargeNow'),
                refRequired: t('telecom.swal.rechargeRefRequired'),
                voucherCodeTitle: t('telecom.customerList.swal.voucherCodeTitle'),
                validateBtn: t('telecom.customerList.swal.validate', 'Validate'),
                voucherRequired: t('telecom.customerList.swal.enterVoucherCode'),
                voucherInvalid: t('telecom.customerList.swal.invalidVoucher'),
                draftMissing: t('telecom.swal.rechargeFail'),
                confirmFailed: t('telecom.swal.rechargeFail'),
                missingContext: t('telecom.swal.rechargeMissingContextText'),
            });

            const resolveHubRechargeContext = async (msisdnInput) => {
                const term = (msisdnInput || '').trim();
                if (!term) return null;
                const digits = normalizeMsisdnDigits(term);

                if (state.subscriberDetail?.customerId && state.detailTelecomSubscriptionId) {
                    const subs = state.subscriberDetail.subscriptions || [];
                    const sub =
                        subs.find((s) => normalizeMsisdnDigits(s.msisdn) === digits) ||
                        subs.find((s) => s.id === state.detailTelecomSubscriptionId) ||
                        subs[0];
                    if (sub?.id) {
                        return {
                            customerId: state.subscriberDetail.customerId,
                            subscriptionId: sub.id,
                            msisdn: sub.msisdn || term,
                        };
                    }
                }

                const searchRes = await AxiosManager.get(
                    '/Telecom/GetTelecomUniversalSearch?term=' + encodeURIComponent(term),
                    {}
                );
                const rows = parseUniversalSearchRows(searchRes);
                let customerId = '';
                for (const row of rows) {
                    customerId = pickCustomerIdFromSearchRow(row);
                    if (customerId) break;
                }
                if (!customerId) return null;

                const c360Res = await AxiosManager.get(
                    '/Customer/GetCustomer360?customerId=' + encodeURIComponent(customerId),
                    {}
                );
                const core = c360Res?.data?.content;
                const subs = core?.activeSubscriptions || [];
                const sub =
                    subs.find((s) => normalizeMsisdnDigits(s.msisdn) === digits) || subs[0];
                if (!sub?.id) return null;
                return {
                    customerId,
                    subscriptionId: sub.id,
                    msisdn: sub.msisdn || term,
                };
            };

            const openHubRecharge = async () => {
                if (!hubPermissions.value.canRecharge || state.rechargeBusy) return;
                if (typeof TelecomRechargeFlow === 'undefined') {
                    Swal.fire({ icon: 'error', title: t('telecom.swal.rechargeFail') });
                    return;
                }

                let msisdn = (state.searchTerm || '').trim();
                const looksLikeMsisdn = normalizeMsisdnDigits(msisdn).length >= 8;
                if (!looksLikeMsisdn) {
                    const { value } = await Swal.fire({
                        title: t('telecom.swal.rechargeTitle'),
                        input: 'text',
                        inputPlaceholder: t('telecom.search.placeholder'),
                        showCancelButton: true,
                        confirmButtonText: t('telecom.swal.rechargeNext'),
                        confirmButtonColor: '#c8102e',
                        inputValidator: (v) =>
                            normalizeMsisdnDigits(v).length < 8 ? t('telecom.swal.rechargeInvalidAmount') : undefined,
                    });
                    if (!value) return;
                    msisdn = String(value).trim();
                }

                state.rechargeBusy = true;
                try {
                    const ctx = await resolveHubRechargeContext(msisdn);
                    if (!ctx) {
                        Swal.fire({
                            icon: 'warning',
                            title: t('telecom.swal.rechargeMissingContextTitle'),
                            text: t('telecom.swal.rechargeMissingContextText'),
                        });
                        return;
                    }

                    const confirm = await TelecomRechargeFlow.runFlow(AxiosManager, Swal, {
                        customerId: ctx.customerId,
                        subscriptionId: ctx.subscriptionId,
                        labels: hubRechargeFlowLabels(),
                    });
                    if (!confirm) return;

                    if (state.huaweiCbsData?.msisdn) {
                        fetchHuaweiCbsData(ctx.msisdn);
                    }
                    Swal.fire({
                        icon: 'success',
                        title: t('telecom.swal.rechargeOk'),
                        text: confirm?.messageAr || confirm?.MessageAr || '',
                        confirmButtonColor: '#c8102e',
                    });
                } catch (err) {
                    Swal.fire({
                        icon: 'error',
                        title: t('telecom.swal.rechargeFail'),
                        text: err?.message || 'Error',
                    });
                } finally {
                    state.rechargeBusy = false;
                }
            };

            const openHuaweiRechargeModal = openHubRecharge;

            const queryHlrLiveStatus = async () => {
                const pid = (state.subscriberDetail?.subscriberProfileId || '').trim();
                const msisdn = (state.subscriberDetail?.msisdn || '').trim();
                if (!pid && !msisdn) return;
                state.hlrLiveBusy = true;
                state.hlrLiveData = null;
                try {
                    const q = msisdn
                        ? 'msisdn=' + encodeURIComponent(msisdn)
                        : 'subscriberProfileId=' + encodeURIComponent(pid);
                    const res = await AxiosManager.get(
                        '/Telecom/QueryHlrLiveStatus?' + q,
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
                const msisdn = (state.subscriberDetail?.msisdn || '').trim();
                const msisdnAssetId = (state.subscriberDetail?.msisdnAssetId || '').trim();
                if (!pid) return;
                state.hlrResyncBusy = true;
                try {
                    const res = await AxiosManager.post('/Telecom/ResyncSubscriberFromHlr', {
                        subscriberProfileId: pid,
                        msisdnAssetId: msisdnAssetId || null,
                        msisdn: msisdn || null,
                    });
                    const body = res?.data?.content;
                    if (res?.data?.code === 200 && body?.success) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'success',
                                title: t('telecom.swal.hlrSyncedTitle'),
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
                            title: t('telecom.swal.syncFailed'),
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
                if (state.telecomLineTypesBusy) return;
                state.telecomLineTypesBusy = true;
                try {
                    const res = await AxiosManager.get(
                        '/TelecomSubscriptionType/GetTelecomSubscriptionTypeList?isDeleted=false&activeOnly=true',
                        {}
                    );
                    state.telecomLineTypes = parseTelecomSubscriptionTypeList(res);
                } catch {
                    state.telecomLineTypes = [];
                } finally {
                    state.telecomLineTypesBusy = false;
                }
            };

            Vue.onMounted(async () => {
                document.documentElement.addEventListener('syriatel-locale-changed', onLocaleChanged);
                if (typeof ActivationChannelUi !== 'undefined') {
                    await ActivationChannelUi.ensureLoaded();
                }
                try {
                    const params = new URLSearchParams(window.location.search);
                    if (params.get('entry') === 'subscriber') {
                        const q = params.get('q') || params.get('term') || '';
                        const target = q
                            ? '/Telecom/UnifiedSearch?q=' + encodeURIComponent(q)
                            : '/Telecom/UnifiedSearch';
                        window.location.replace(target);
                        return;
                    }
                    if (params.get('openPool') === '1') {
                        window.location.replace('/Telecom/MsisdnInventory');
                        return;
                    }

                    await loadTelecomLineTypes();
                    await refreshAll();

                    const wizParam = params.get('wizard');
                    const prodParam = params.get('productId');
                    if (wizParam) {
                        await openWizard(wizParam);
                        if (prodParam) {
                            state.wizard.migrationTargetProductId = prodParam;
                            state.preloadedProductId = prodParam;
                        }
                        const deepProfile = params.get('subscriberProfileId');
                        const deepAsset = params.get('msisdnAssetId');
                        const deepMsisdn = params.get('msisdn');
                        const deepCustomer = params.get('customerId');
                        if (deepProfile && deepAsset) {
                            state.wizard.primarySubscriberProfileId = deepProfile;
                            state.wizard.primaryMsisdnAssetId = deepAsset;
                            state.wizard.customerId = deepCustomer || state.wizard.customerId || '';
                            state.wizard.primaryLabel = deepMsisdn
                                ? `${deepMsisdn} (Msisdn)`
                                : state.wizard.primaryLabel;
                            if (wizParam === 'reconnect') {
                                await loadReconnectEligibility();
                            }
                            if (wizParam === 'badDebt') {
                                await loadBadDebtEligibility();
                            }
                        }
                    }

                    window.addEventListener('message', async (event) => {
                        if (event.data?.action === 'syriatel-customer-saved') {
                            closeRegistryModal();
                            await refreshAll();
                            if (window.Swal) {
                                Swal.fire({
                                    icon: 'success',
                                    title: t('telecom.swal.savedOk'),
                                    text: t('telecom.swal.savedSubscriberText'),
                                    timer: 2000,
                                    showConfirmButton: false,
                                });
                            }
                        } else if (event.data?.action === 'syriatel-customer-saved-activate') {
                            closeRegistryModal();
                            await refreshAll();
                            await openWizard('activate');
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
                closeActivateMsisdnModal();
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
                productDisplayName,
                needsSecondaryParty,
                secondaryRequired,
                needsActivateMsisdn,
                isCorporatePrimary,
                MSISDN_CATEGORY_TABS,
                activateMsisdnPoolDisplay,
                activateMsisdnPoolTruncated,
                activateMsisdnPoolEmptyDueToLineType,
                filteredActivateMsisdnPool,
                msisdnCategoryLabel,
                msisdnCategoryBadgeClass,
                resolveMsisdnCategory,
                openActivateMsisdnModal,
                closeActivateMsisdnModal,
                setActivateMsisdnCategoryTab,
                loadActivateMsisdnPool,
                telecomLineTypesBusy: Vue.computed(() => state.telecomLineTypesBusy),
                activationLineTypeOptions,
                activationLineTypeLabel,
                canPickActivateMsisdn,
                lineTypeDisplayName,
                onActivationLineTypeChange,
                wizardPrimaryMultiProfileSameParty,
                loadMigrationEligibleProducts,
                onWizardOfferChanged,
                mgrProrationPriceDifferenceLabel,
                mgrProrationAmountLabel,
                mgrProrationWalletLabel,
                mgrProrationDaysLabel,
                mgrProrationSufficient,
                hubBssEffectiveToday,
                wizardSupportsEffectiveDate,
                hubSuspensionStartDateLocal,
                hubSuspensionMaxEndDateLocal,
                wizardOfferDisplayName,
                wizardOfferMonthlyPrice,
                wizardOfferVoiceLine,
                wizardOfferDataLine,
                wizardOfferSmsLine,
                wizardOfferSummaryText,
                activateSimIccidLocked,
                activationChannelUiMode: Vue.computed(() =>
                    typeof ActivationChannelUi !== 'undefined' ? ActivationChannelUi.resolveMode() : 'showroom'),
                activationChannelLabels: Vue.computed(() => {
                    const loc = locale.value || 'ar';
                    if (typeof ActivationChannelUi === 'undefined') {
                        return {
                            showroom: t('telecom.wizardUi.channelShowroom', 'POS'),
                            dealer: t('telecom.wizardUi.channelDealer', 'Dealer'),
                        };
                    }
                    return {
                        showroom: ActivationChannelUi.label(0, loc),
                        dealer: ActivationChannelUi.label(1, loc),
                    };
                }),
                activationChannelLockedHint: Vue.computed(() => {
                    if (typeof ActivationChannelUi === 'undefined') {
                        return t('telecom.wizardUi.channelShowroomLockedHint', '');
                    }
                    return ActivationChannelUi.lockedHint(locale.value || 'ar');
                }),
                canWizardGoToStep3,
                wizardAwaitBackOffice,
                wizardNeedsIdentityUpload,
                wizardShowsConfirmCbs,
                activateRequiredDeposit,
                wizardCustomer360ProfileUrl,
                openWizardCustomer360,
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
                openCustomerWizard,
                closeCustomerWizard,
                runCustomerWizardSearch,
                proceedCustomerWizardNew,
                goBackCustomerWizardSearch,
                onCustomerWizardSubscriberTypeChange,
                onCustomerWizardCityChange,
                submitCustomerWizardSave,
                finishCustomerWizard,
                handoffSavedCustomerToActivate,
                handoffExistingCustomerToActivate,
                customerWizard: state.customerWizard,
                onDevicePicked,
                recordDeviceDownPayment,
                closeWizard,
                wizardStepNext,
                wizardStepPrev,
                runWizardPrimarySearch,
                runWizardSecondarySearch,
                runWizardActivateMsisdnSearch,
                selectWizardPrimary,
                clearWizardPrimary,
                selectWizardActivateMsisdn,
                clearWizardActivateMsisdn,
                selectWizardSecondary,
                clearWizardSecondary,
                submitCreateOperation,
                submitMarkDocumentUploaded,
                submitWizardRecordPayment,
                fetchPaymentFromCashier,
                submitWizardConfirmCbs,
                finishWizard,
                confirmFirstReadyDraft,
                kindLabel,
                statusLabel,
                kindLabelAr: kindLabel,
                statusLabelAr: statusLabel,
                formatOpEffectiveDate,
                isScheduledOp,
                isTakeOverPendingReview,
                operationKpis,
                pendingTakeOverCount,
                pendingSimSwapCount,
                pendingChangeNumberCount,
                pendingTerminationCount,
                pendingSecureOpCount,
                isSecureOpPendingReview,
                isSimSwapPendingReview,
                isChangeNumberPremiumPending,
                isTerminationBoPending,
                onTerminationTypeChange,
                onSusTypeChange,
                onSusAutoReconnectChange,
                susShowsSimple,
                barringLevelOptions,
                trmRequiresLegacyIdentity,
                onRefundTypeChange,
                onRefundMethodChange,
                onSimLostOrStolenChange,
                simShowsLostStolenFields,
                cnShowsPremiumPayment,
                tkoShowsObligationSettlement,
                onCgtPathChange,
                onCgtTargetTypeChanged,
                onBssRegulatoryFileChange,
                onBssIdentityFileChange,
                susShowsPayment,
                susShowsFraud,
                susRequiresStep2Identity,
                susShowsRegulatory,
                trmShowsPayment,
                trmShowsFraud,
                trmShowsRegulatory,
                trmShowsVoluntary,
                cgtShowsFinancial,
                cgtShowsRegulatory,
                rfdShowsOriginalTxRef,
                rfdShowsPayoutDestination,
                rfdPayoutLabelKey,
                loadReconnectEligibility,
                onRcnClearanceChange,
                onRcnRegulatoryFileChange,
                rcnShowsPaymentRef,
                rcnShowsFraudFields,
                rcnShowsRegulatoryFields,
                rcnShowsSimplePath,
                loadBadDebtEligibility,
                onBdrActionChange,
                pendingBadDebtCount,
                loadChangeNumberPool,
                onChangeNumberTargetPicked,
                onCnChangeModeChange,
                cnShowsInternalPool,
                cnDonorOperators,
                secureOpApproveLabel,
                canApproveSecureOp,
                secureApprovalModalTitle,
                secureApprovalApproveLabel,
                openSecureOpApproval,
                approveSecureOpOnNetwork,
                openTakeOverApproval,
                closeTakeOverApproval,
                approveTakeOverOnNetwork,
                onWizardIdentityFileChange,
                onKycFileSelected,
                onKycDragEnter,
                onKycDragOver,
                onKycDragLeave,
                onKycDrop,
                uploadKycDocument,
                fetchHuaweiCbsData,
                openHuaweiRechargeModal,
                openHubRecharge,
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
        fallbackLocale: 'en',
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
