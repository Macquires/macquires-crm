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
                return {
                    canCreateOps: roles.some((x) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(x)),
                    canConfirmCbs: roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice'].includes(x)),
                    canApproveTakeOver: canTransferOwnership,
                    canApproveSimSwap,
                    canApproveChangeNumber,
                    canApproveTermination,
                    canApproveSuspension,
                    canApproveReconnect,
                    canApproveRefund,
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
                    cgtTargets: [],
                    cgtTargetsBusy: false,
                    cgtCurrentTypeLabel: '',
                    cgtTargetTypeId: '',
                    cgtMigrationReason: '',
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
                    vasCatalog: [],
                    vasCatalogBusy: false,
                    vasActivated: false,
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

            const kindToApiEnum = () => {
                const k = state.wizard.kind;
                if (k === 'activate') return 0;
                if (k === 'migrate') return 1;
                if (k === 'takeover') return 2;
                if (k === 'simswap') return 3;
                if (k === 'addpackage' || k === 'support') return 4;
                if (k === 'changeGsm') return 6;
                if (k === 'changeNumber') return 5;
                if (k === 'termination') return 7;
                if (k === 'suspension') return 8;
                if (k === 'reconnect') return 9;
                if (k === 'deviceSale') return 10;
                if (k === 'refund') return 11;
                return 0;
            };

            const isPremiumMsisdnCategory = (cat) => [1, 2, 3, 'Silver', 'Gold', 'Platinum'].includes(cat);

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

            const canWizardGoToStep3 = Vue.computed(() => {
                if (state.wizard.kind === 'addpackage') {
                    return !!state.wizard.vasActivated;
                }
                return !!state.wizard.createdOperationId && state.wizard.documentMarkedUploaded;
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
                state.wizard.selectedVasCode = '';
                state.wizard.vasCatalog = [];
                state.wizard.vasCatalogBusy = false;
                state.wizard.vasActivated = false;
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
                state.wizard.simReplacementReason = '';
                state.wizard.simLostOrStolen = false;
                state.wizard.cnTargetMsisdnAssetId = '';
                state.wizard.cnNumberChangeReason = '';
                state.wizard.cnPremiumFeeAmount = '';
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
                state.wizard.rcnFraudClearanceConfirmed = false;
                state.wizard.rcnRequiresBackOffice = false;
                state.wizard.rcnEligibilityBusy = false;
                state.wizard.rcnEligibility = null;
                state.wizard.rfdRefundType = 'Deposit';
                state.wizard.rfdRefundMethod = 'CreditNote';
                state.wizard.rfdRefundAmount = '';
                state.wizard.rfdRefundReason = '';
                state.wizard.rfdDepositSnapshot = null;
                state.wizard.rfdWalletSnapshot = null;
                state.wizard.rfdRequiresBackOffice = false;
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
                if (k === 6) return 'تحويل نوع الخط CGT';
                if (k === 5) return 'تغيير رقم CNR';
                if (k === 7) return 'إنهاء خط TRM';
                if (k === 8) return 'حظر مؤقت SUS';
                if (k === 9) return 'إعادة تفعيل RCN';
                if (k === 10) return 'بيع جهاز DEV';
                if (k === 11) return 'استرداد مالي RFD';
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

            const isSecureOpPendingReview = (o) =>
                isTakeOverPendingReview(o)
                || isSimSwapPendingReview(o)
                || isChangeNumberPremiumPending(o)
                || isTerminationBoPending(o)
                || isSuspensionBoPending(o)
                || isReconnectBoPending(o)
                || isRefundBoPending(o);

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

            const pendingSecureOpCount = Vue.computed(
                () =>
                    pendingTakeOverCount.value
                    + pendingSimSwapCount.value
                    + pendingChangeNumberCount.value
                    + pendingTerminationCount.value
                    + pendingSuspensionCount.value
                    + pendingReconnectCount.value
                    + pendingRefundCount.value
            );

            const secureOpApproveLabel = (o) => {
                if (Number(o?.kind) === 3) return t('telecom.ops.approveSimSwap');
                if (Number(o?.kind) === 5) return t('telecom.ops.approveChangeNumber');
                if (Number(o?.kind) === 7) return t('telecom.ops.approveTermination');
                if (Number(o?.kind) === 8) return t('telecom.ops.approveSuspension');
                if (Number(o?.kind) === 9) return t('telecom.ops.approveReconnect');
                if (Number(o?.kind) === 11) return t('telecom.ops.approveRefund');
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
                return t('telecom.takeOverModal.approve');
            });

            const onRefundTypeChange = () => {
                const amt = Number(state.wizard.rfdRefundAmount) || 0;
                const ty = (state.wizard.rfdRefundType || '').trim();
                const method = (state.wizard.rfdRefundMethod || '').trim();
                state.wizard.rfdRequiresBackOffice =
                    ty === 'SyriatelCash' || amt > 500000 || (method === 'Cash' && amt > 500000);
            };

            const onTerminationTypeChange = () => {
                const ty = (state.wizard.trmTerminationType || '').trim();
                state.wizard.trmRequiresBackOffice =
                    ty === 'Fraud' || ty === 'Regulatory' || ty === 'Collections';
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
                state.wizard.primaryMsisdn =
                    r.resultType === 'Msisdn' ? String(r.title || '').trim() : state.wizard.primaryMsisdn;
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
                if (state.wizard.kind === 'changeGsm') {
                    loadChangeGsmEligibleTargets();
                }
                if (state.wizard.kind === 'reconnect') {
                    loadReconnectEligibility();
                }
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
                loadReconnectEligibility();
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
                    if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: 'أدخل مبلغ الدفع ومرجع الدفع.' });
                    return;
                }
                await AxiosManager.post('/Telecom/RecordDeviceDownPayment', {
                    operationId: state.wizard.createdOperationId,
                    amountPaid: amount,
                    paymentChannel: Number(state.wizard.devPaymentChannel) || 0,
                    paymentReference: state.wizard.devPaymentReference.trim(),
                    updatedById: StorageManager.getUserId(),
                });
                state.wizard.documentMarkedUploaded = true;
                if (window.Swal) Swal.fire({ icon: 'success', title: 'تم تسجيل الدفع', timer: 1200, showConfirmButton: false });
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

            const openWizard = (kind) => {
                resetWizardState();
                state.wizard.visible = true;
                state.wizard.kind = kind;
                if (kind === 'deviceSale') loadDeviceWizardCatalog();
                if (kind === 'addpackage') loadWizardVasCatalog();
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

            const validateWizardBeforeCreateDraft = async () => {
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
                    if (state.wizard.kind === 'takeover' && !(state.wizard.tkoTransferReason || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.takeOver.transferReason'), text: t('telecom.takeOver.transferReasonPh') });
                        }
                        return false;
                    }
                }
                if (state.wizard.kind === 'deviceSale') {
                    if (!(state.wizard.devInventoryId || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: 'اختر جهازاً (IMEI) من المخزون.' });
                        return false;
                    }
                    if (state.wizard.devSaleType === 'Installment' && !(state.wizard.devInstallmentPlanId || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: 'اختر خطة التقسيط.' });
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
                            Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: 'ICCID (19 رقم)' });
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

                if (state.wizard.kind === 'refund') {
                    if (!(state.wizard.primaryMsisdnAssetId || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.refund.currentMsisdn') });
                        return false;
                    }
                    if (!(state.wizard.rfdRefundReason || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.refund.refundReason') });
                        return false;
                    }
                    const amt = Number(state.wizard.rfdRefundAmount);
                    if (!amt || amt <= 0) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.refund.refundAmount') });
                        return false;
                    }
                    onRefundTypeChange();
                }

                if (state.wizard.kind === 'suspension') {
                    if (!(state.wizard.primaryMsisdnAssetId || '').trim() || !(state.wizard.susSuspensionReason || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.suspension.suspensionReason') });
                        return false;
                    }
                    if (state.wizard.susAutoReconnectEnabled && !(state.wizard.susEndDateLocal || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.suspension.endDateRequired') });
                        return false;
                    }
                    state.wizard.susRequiresBackOffice =
                        ['Fraud', 'Regulatory'].includes(state.wizard.susSuspensionType);
                }

                if (state.wizard.kind === 'reconnect') {
                    if (!(state.wizard.primaryMsisdnAssetId || '').trim() || !(state.wizard.rcnReconnectReason || '').trim()) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.reconnect.reconnectReason') });
                        return false;
                    }
                    if (state.wizard.rcnClearanceType === 'Payment' && !(state.wizard.rcnPaymentReference || '').trim()) {
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

                if (state.wizard.kind === 'termination') {
                    if (!(state.wizard.primaryMsisdnAssetId || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.termination.currentMsisdn') });
                        }
                        return false;
                    }
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

                if (state.wizard.kind === 'changeNumber') {
                    if (!(state.wizard.primaryMsisdnAssetId || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: t('telecom.changeNumber.currentMsisdn') });
                        }
                        return false;
                    }
                    if (!(state.wizard.cnTargetMsisdnAssetId || '').trim()) {
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
                }
                if (state.wizard.kind === 'changeGsm') {
                    if (!(state.wizard.cgtTargetTypeId || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: 'اختر نوع الخط الجديد.' });
                        }
                        return false;
                    }
                    if (!(state.wizard.cgtMigrationReason || '').trim()) {
                        if (window.Swal) {
                            Swal.fire({ icon: 'warning', title: t('telecom.swal.incompleteTitle'), text: 'سبب التحويل مطلوب.' });
                        }
                        return false;
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
                                text: state.contentLang === 'ar' ? 'اختر مشتركاً من نتائج البحث (رقم خط).' : 'Select a subscriber with an MSISDN from search.',
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
                state.wizard.cnRequiresBackOffice = isPremiumMsisdnCategory(cat);
                if (!state.wizard.cnRequiresBackOffice) {
                    state.wizard.cnPremiumFeeAmount = '';
                }
            };

            const submitVasFromHub = async () => {
                const msisdn = (state.wizard.primaryMsisdn || '').trim();
                const code = (state.wizard.selectedVasCode || '').trim();
                if (!msisdn || !code) return;
                state.wizard.submitBusy = true;
                try {
                    const res = await AxiosManager.post('/Vas/ToggleSubscriberVasService', {
                        msisdn,
                        serviceCode: code,
                        action: 0,
                        actorUserId: StorageManager.getUserId(),
                    });
                    if (res?.data?.code === 200) {
                        state.wizard.vasActivated = true;
                        state.wizard.documentMarkedUploaded = true;
                        state.wizard.createdOperationNumber = res?.data?.content?.operationNumber ?? '';
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
                    } else {
                        throw Object.assign(new Error(res?.data?.message || 'fail'), { response: res });
                    }
                } catch (e) {
                    const errName = e?.response?.data?.error?.name;
                    const msg = pickHttpErrorMessage(e);
                    if (window.Swal) {
                        Swal.fire({
                            icon: errName === 'BusinessRuleViolationException' ? 'warning' : 'error',
                            title: 'VAL-11',
                            text: msg,
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
                if (state.wizard.kind === 'changeNumber' && state.wizard.cnTargetMsisdnAssetId) {
                    try {
                        await AxiosManager.post('/Telecom/ReserveMsisdnForCustomer', {
                            msisdnAssetId: state.wizard.cnTargetMsisdnAssetId,
                            customerId: state.wizard.customerId || state.wizard.primaryRowKey?.split('|')?.[0] || '',
                            reservedByUserId: StorageManager.getUserId(),
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
                            state.wizard.kind === 'activate' || state.wizard.kind === 'simswap'
                                ? (state.wizard.simIccid || '').trim() || null
                                : null,
                        replacementReason:
                            state.wizard.kind === 'simswap' ? (state.wizard.simReplacementReason || '').trim() || null : null,
                        isLostOrStolenReport: state.wizard.kind === 'simswap' ? !!state.wizard.simLostOrStolen : false,
                        createdById: StorageManager.getUserId(),
                        targetSubscriptionTypeId:
                            state.wizard.kind === 'changeGsm' ? state.wizard.cgtTargetTypeId || null : null,
                        gsmMigrationReason:
                            state.wizard.kind === 'changeGsm' ? (state.wizard.cgtMigrationReason || '').trim() || null : null,
                        transferReason:
                            state.wizard.kind === 'takeover' ? (state.wizard.tkoTransferReason || '').trim() || null : null,
                        depositTransferPolicy:
                            state.wizard.kind === 'takeover' ? Number(state.wizard.tkoDepositPolicy) : null,
                        targetMsisdnAssetId:
                            state.wizard.kind === 'changeNumber' ? state.wizard.cnTargetMsisdnAssetId || null : null,
                        numberChangeReason:
                            state.wizard.kind === 'changeNumber'
                                ? (state.wizard.cnNumberChangeReason || '').trim() || null
                                : null,
                        premiumFeeAmount:
                            state.wizard.kind === 'changeNumber' && state.wizard.cnPremiumFeeAmount
                                ? Number(state.wizard.cnPremiumFeeAmount)
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
                        suspensionType:
                            state.wizard.kind === 'suspension' ? (state.wizard.susSuspensionType || '').trim() || null : null,
                        suspensionReason:
                            state.wizard.kind === 'suspension' ? (state.wizard.susSuspensionReason || '').trim() || null : null,
                        barringLevel:
                            state.wizard.kind === 'suspension' ? (state.wizard.susBarringLevel || 'Full').trim() : null,
                        autoReconnectEnabled: state.wizard.kind === 'suspension' ? !!state.wizard.susAutoReconnectEnabled : false,
                        suspensionEndDateUtc:
                            state.wizard.kind === 'suspension' && state.wizard.susEndDateLocal
                                ? new Date(state.wizard.susEndDateLocal).toISOString()
                                : null,
                        reconnectReason:
                            state.wizard.kind === 'reconnect' ? (state.wizard.rcnReconnectReason || '').trim() || null : null,
                        clearanceType:
                            state.wizard.kind === 'reconnect' ? (state.wizard.rcnClearanceType || '').trim() || null : null,
                        paymentReference:
                            state.wizard.kind === 'reconnect' ? (state.wizard.rcnPaymentReference || '').trim() || null : null,
                        fraudClearanceConfirmed:
                            state.wizard.kind === 'reconnect' ? !!state.wizard.rcnFraudClearanceConfirmed : false,
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
                if (
                    (state.wizard.kind === 'takeover'
                        || (state.wizard.kind === 'simswap' && state.wizard.simLostOrStolen)
                        || (state.wizard.kind === 'changeNumber' && state.wizard.cnRequiresBackOffice)
                        || (state.wizard.kind === 'termination' && state.wizard.trmRequiresBackOffice)
                        || (state.wizard.kind === 'refund' && state.wizard.rfdRequiresBackOffice))
                    && state.wizard.identityFile
                ) {
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
                if (
                    (state.wizard.kind === 'takeover'
                        || (state.wizard.kind === 'simswap' && state.wizard.simLostOrStolen)
                        || (state.wizard.kind === 'changeNumber' && state.wizard.cnRequiresBackOffice)
                        || (state.wizard.kind === 'termination' && state.wizard.trmRequiresBackOffice)
                        || (state.wizard.kind === 'refund' && state.wizard.rfdRequiresBackOffice))
                    && !state.wizard.identityFile
                ) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: 'وثيقة مطلوبة',
                            text:
                                state.wizard.kind === 'termination'
                                    ? t('telecom.termination.identityRequired')
                                    : state.wizard.kind === 'changeNumber'
                                    ? t('telecom.changeNumber.paymentDocHint')
                                    : state.wizard.kind === 'simswap'
                                        ? 'ارفع إقرار / هوية المشترك قبل الإرسال.'
                                        : 'ارفع صورة أو PDF لهوية المالك الجديد قبل الإرسال.',
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
                            title: 'تعذّر تحميل الطلب',
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
                            text: 'صلاحية اعتماد SIM Swap (telecom.line.simswap_approve) مطلوبة.',
                        });
                    }
                    return;
                }
                if (kind === 5 && !canApproveSecureOp.value) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: 'صلاحية اعتماد تغيير الرقم (telecom.line.change_number_approve) مطلوبة.',
                        });
                    }
                    return;
                }
                if (kind === 7 && !canApproveSecureOp.value) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: 'صلاحية اعتماد الإنهاء (telecom.line.termination_approve) مطلوبة.',
                        });
                    }
                    return;
                }
                if (kind === 8 && !hubPermissions.value.canApproveSuspension) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: 'صلاحية اعتماد الحظر (telecom.line.suspension_approve) مطلوبة.',
                        });
                    }
                    return;
                }
                if (kind === 9 && !hubPermissions.value.canApproveReconnect) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: 'صلاحية اعتماد إعادة التفعيل (telecom.line.reconnect_approve) مطلوبة.',
                        });
                    }
                    return;
                }
                if (kind === 11 && !hubPermissions.value.canApproveRefund) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: 'صلاحية اعتماد الاسترداد (telecom.line.refund_approve) مطلوبة.',
                        });
                    }
                    return;
                }
                if (kind === 2 && !hubPermissions.value.canApproveTakeOver) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: t('telecom.swal.notAllowedTitle'),
                            text: 'صلاحية اعتماد نقل الملكية (telecom.line.transfer_ownership) مطلوبة.',
                        });
                    }
                    return;
                }
                const opId = state.takeOverApproval.operationId;
                state.takeOverApproval.approveBusy = true;
                try {
                    const res = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                        id: opId,
                        updatedById: StorageManager.getUserId(),
                    });
                    const content = res?.data?.content;
                    const br = content?.billingResult;
                    if (res?.data?.code === 200 && (br?.success || content?.idempotentReplay)) {
                        const hint =
                            content?.statusHintAr ??
                            content?.StatusHintAr ??
                            'تم تأكيد CBS؛ جاري تزويد الشبكة…';
                        const doneTitle =
                            kind === 3
                                ? 'اكتمل تبديل الشريحة'
                                : kind === 5
                                    ? 'اكتمل تغيير الرقم'
                                    : kind === 7
                                        ? 'اكتمل إنهاء الخط'
                                        : 'اكتمل نقل الملكية';
                        const failTitle =
                            kind === 3
                                ? 'فشل تبديل الشريحة'
                                : kind === 5
                                    ? 'فشل تغيير الرقم'
                                    : kind === 7
                                        ? 'فشل إنهاء الخط'
                                        : 'فشل أو تعذّر الإكمال';
                        const okTitle =
                            kind === 3
                                ? 'تم تبديل الشريحة'
                                : kind === 5
                                    ? 'تم تغيير الرقم'
                                    : kind === 7
                                        ? 'تم إنهاء الخط'
                                        : 'تم نقل الملكية';
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
                                Swal.fire({ icon: 'info', title: 'قيد التزويد', text: hint });
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

            const openHuaweiRechargeModal = async () => {
                if (!state.huaweiCbsData || !state.huaweiCbsData.msisdn) return;
                const msisdn = state.huaweiCbsData.msisdn;
                const customerId = state.subscriberDetail?.customerId;
                const subscriptionId = state.detailTelecomSubscriptionId;
                const ar = state.contentLang === 'ar';
                if (!customerId || !subscriptionId) {
                    Swal.fire({
                        icon: 'warning',
                        title: ar ? 'بيانات غير كافية' : 'Missing context',
                        text: ar
                            ? 'افتح ملف المشترك واختر خطاً نشطاً قبل الشحن عبر المحرك الموحد.'
                            : 'Open subscriber detail and select an active line first.',
                    });
                    return;
                }

                const { value: amountStr } = await Swal.fire({
                    title: ar ? 'شحن — المحرك المالي الموحد' : 'Recharge — Unified Payment',
                    text: ar
                        ? `CBS + PAY- للرقم ${msisdn}`
                        : `CBS + PAY- for ${msisdn}`,
                    input: 'number',
                    inputPlaceholder: ar ? '15000' : '15000',
                    showCancelButton: true,
                    confirmButtonText: ar ? 'التالي' : 'Next',
                    confirmButtonColor: '#c8102e',
                    inputValidator: (v) => {
                        const n = parseFloat(v);
                        if (!v || Number.isNaN(n) || n <= 0) {
                            return ar ? 'مبلغ غير صالح' : 'Invalid amount';
                        }
                    },
                });
                if (!amountStr) return;

                const { value: gatewayRef } = await Swal.fire({
                    title: ar ? 'مرجع الدفع' : 'Payment reference',
                    input: 'text',
                    showCancelButton: true,
                    confirmButtonText: ar ? 'شحن الآن' : 'Recharge now',
                    confirmButtonColor: '#c8102e',
                    inputValidator: (v) => (!v || !String(v).trim() ? (ar ? 'مرجع مطلوب' : 'Required') : undefined),
                });
                if (!gatewayRef) return;

                state.huaweiCbsBusy = true;
                try {
                    const createRes = await AxiosManager.post('/Telecom/CreatePaymentTransaction', {
                        type: 0,
                        customerId,
                        subscriptionId,
                        amount: parseFloat(amountStr),
                        paymentChannel: 1,
                        serviceChannel: 0,
                        createdById: StorageManager.getUserId(),
                    });
                    const draft = createRes?.data?.content ?? createRes?.data?.Content;
                    const paymentId = draft?.paymentId ?? draft?.PaymentId;
                    const confirmRes = await AxiosManager.post('/Telecom/ConfirmPaymentTransaction', {
                        paymentId,
                        gatewayReference: String(gatewayRef).trim(),
                        confirmedById: StorageManager.getUserId(),
                    });
                    const confirm = confirmRes?.data?.content ?? confirmRes?.data?.Content;
                    if (!(confirm?.success ?? confirm?.Success)) {
                        throw new Error(confirm?.messageAr || confirm?.MessageAr || 'Confirm failed');
                    }
                    await pollHubPaymentDetail(paymentId);
                    if (confirm?.newBalance != null) {
                        state.huaweiCbsData.balance = confirm.newBalance;
                    } else if (confirm?.NewBalance != null) {
                        state.huaweiCbsData.balance = confirm.NewBalance;
                    }
                    Swal.fire({
                        icon: 'success',
                        title: ar ? 'تم الشحن!' : 'Recharged',
                        text: confirm?.messageAr || confirm?.MessageAr || '',
                        confirmButtonColor: '#c8102e',
                    });
                } catch (err) {
                    Swal.fire({
                        icon: 'error',
                        title: ar ? 'فشل الشحن' : 'Failed',
                        text: err?.message || 'Error',
                    });
                } finally {
                    state.huaweiCbsBusy = false;
                }
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
                onDevicePicked,
                recordDeviceDownPayment,
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
                pendingSimSwapCount,
                pendingChangeNumberCount,
                pendingTerminationCount,
                pendingSecureOpCount,
                isSecureOpPendingReview,
                isSimSwapPendingReview,
                isChangeNumberPremiumPending,
                isTerminationBoPending,
                onTerminationTypeChange,
                onRefundTypeChange,
                loadReconnectEligibility,
                onRcnClearanceChange,
                loadChangeNumberPool,
                onChangeNumberTargetPicked,
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
