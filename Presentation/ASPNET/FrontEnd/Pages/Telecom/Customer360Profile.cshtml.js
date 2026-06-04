const TABS = [
    { id: 'profile', label: 'البيانات والفوترة', icon: 'bi bi-person-vcard' },
    { id: 'services', label: 'الباقات والخدمات', icon: 'bi bi-reception-4' },
    { id: 'tickets', label: 'الشكاوى والدعم', icon: 'bi bi-ticket-detailed' },
];

const PERM = {
    cbs: 'telecom.ticket.forcesync',
    hlr: 'telecom.ticket.hlrresync',
    escalate: 'telecom.ticket.escalate',
    provisioning: 'telecom.customer.provisioning',
    provisioningLegacy: ['telecom.line.migrate', 'telecom.vas.toggle'],
    changeGsm: 'telecom.line.change_gsm',
    changeNumber: 'telecom.line.change_number_request',
    termination: 'telecom.line.termination_request',
    reconnect: 'telecom.line.reconnect_request',
    suspension: 'telecom.line.suspension_request',
    refund: 'telecom.line.refund_request',
    deviceSale: 'telecom.device.sell_request',
    collection: 'telecom.line.collection_request',
    network: 'telecom.network.hlrresync',
    networkLegacy: ['telecom.line.simswap_request', 'telecom.line.simswap', 'telecom.line.activate'],
    takeover: 'customer.update',
};

const PROV_SUCCESS_DEFAULT =
    'تم تسجيل العملية؛ جاري/اكتمل التزامن مع CBS وHLR';

const WIZARD_TITLES = {
    takeover: 'نقل الملكية',
    migrate: 'ترحيل باقة MGR',
    simswap: 'تبديل شريحة',
    addpackage: 'إضافة باقة / VAS',
    activate: 'تفعيل خط جديد',
    changeGsm: 'تحويل نوع الخط CGT',
    changeNumber: 'تغيير رقم الخط CNR',
    termination: 'إنهاء خط TRM',
    suspension: 'حظر مؤقت SUS',
    reconnect: 'إعادة تفعيل RCN',
    refund: 'استرداد RFD',
    badDebt: 'تحصيل BDR',
    deviceSale: 'بيع جهاز DEV',
};

const isPremiumMsisdnCategory = (cat) => [1, 2, 3, 'Silver', 'Gold', 'Platinum'].includes(cat);

const wizardKindToApi = (k) => {
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

const ISSUE = { 0: 'شبكة', 1: 'فوترة', 2: 'حظر شريحة', 3: 'تفعيل' };
const PRIORITY = { 0: 'منخفض', 1: 'متوسط', 2: 'عالي', 3: 'حرج' };
const STATUS = { 0: 'مفتوحة', 1: 'قيد المعالجة', 2: 'تم الحل', 3: 'مصعّدة' };
const RESOLVED = 2;

const pickHttpErrorMessage = (e) => {
    const d = e?.response?.data;
    if (!d) return e?.message ? String(e.message) : '';
    const inner = d.error?.innerException;
    let m = d.message || (typeof inner === 'string' ? inner : '') || '';
    m = String(m);
    if (m.startsWith('Exception: ')) return m.slice(11).trim();
    return m;
};

const formatDt = (utc) => {
    if (!utc) return '—';
    try {
        return new Date(utc).toLocaleString('ar-SY', { dateStyle: 'short', timeStyle: 'short' });
    } catch {
        return String(utc);
    }
};

const formatDateOnly = (val) => {
    if (!val) return '—';
    try {
        return new Date(val).toLocaleDateString('ar-SY', { dateStyle: 'medium' });
    } catch {
        return String(val);
    }
};

const OPERATIONAL_LABELS = {
    Pending: 'قيد الانتظار',
    Active: 'نشط',
    Suspended: 'موقوف',
    Terminated: 'منتهي',
    SuspendedInbound: 'موقوف وارد',
    SuspendedOutbound: 'موقوف صادر',
    Deactivated: 'معطّل',
};

const Customer360ProfileApp = {
    setup() {
        const state = Vue.reactive({
            customerId: '',
            profile: null,
            loading: true,
            loadError: null,
            activeTab: 'profile',
            cbsBusy: false,
            cbsData: null,
            lineWallets: {},
            lineWalletsBusy: {},
            rechargeBusy: '',
            hlrBusy: '',
            hlrBySub: {},
            aiSim: { msisdn: '', transcript: 'أخي شحنت كاش والنت واقف عندي بعد الشحن', busy: false },
            opBusy: null,
            isProvisioningInFlight: false,
            prov: {
                selectedLineKey: '',
            },
            wizard: {
                kind: '',
                step: 1,
                primarySubscriberProfileId: '',
                primaryMsisdnAssetId: '',
                primaryLabel: '',
                susSuspensionType: 'CustomerRequest',
                susSuspensionReason: '',
                susBarringLevel: 'Full',
                susAutoReconnectEnabled: false,
                susEndDateLocal: '',
                susRequiresBackOffice: false,
                rcnReconnectReason: '',
                rcnClearanceType: 'Customer',
                rcnPaymentReference: '',
                rcnFraudClearanceConfirmed: false,
                rcnRequiresBackOffice: false,
                rcnEligibility: null,
                rcnEligibilityBusy: false,
                rfdRefundType: 'Deposit',
                rfdRefundMethod: 'CreditNote',
                rfdRefundAmount: '',
                rfdRefundReason: '',
                rfdDepositSnapshot: null,
                rfdWalletSnapshot: null,
                rfdRequiresBackOffice: false,
                bdrCollectionAction: 'PaymentRecorded',
                bdrDunningStage: 'Reminder1',
                bdrCollectedAmount: '',
                bdrWriteOffAmount: '',
                bdrPaymentReference: '',
                bdrAgencyReference: '',
                bdrPaymentPlanMonths: '',
                bdrRequiresBackOffice: false,
                bdrEligibility: null,
                bdrEligibilityBusy: false,
                devInventoryId: '',
                devSaleType: 'Cash',
                devInstallmentPlanId: '',
                devDownPayment: '',
                devPaymentReference: '',
                devPaymentChannel: 0,
                devDevices: [],
                devPlans: [],
                devRequiresFinance: false,
                devFinancingPreview: '',
                takeoverSearchNationalId: '',
                takeoverSearchPhone: '',
                takeoverResults: [],
                takeoverTargetCustomerId: '',
                takeoverTargetProfileId: '',
                takeoverTargetLabel: '',
                takeoverTransferReason: '',
                takeoverDepositPolicy: 1,
                takeoverBusy: false,
                migrationOffers: [],
                migrationBusy: false,
                mgrCurrentPlan: '',
                cgtTargets: [],
                cgtTargetsBusy: false,
                cgtCurrentTypeLabel: '',
                cgtTargetTypeId: '',
                cgtMigrationReason: '',
                cgtOffers: [],
                cgtOffersBusy: false,
                selectedOfferingId: '',
                vasCatalog: [],
                catalogBusy: false,
                selectedVasCode: '',
                vasActivated: false,
                simIccid: '',
                simReplacementReason: '',
                simLostOrStolen: false,
                cnTargetMsisdnAssetId: '',
                cnNumberChangeReason: '',
                cnPremiumFeeAmount: '',
                cnPoolNumbers: [],
                cnPoolBusy: false,
                cnRequiresBackOffice: false,
                cnCurrentMsisdn: '',
                trmTerminationType: 'Voluntary',
                trmTerminationReason: '',
                trmRetentionOfferOutcome: 'Declined',
                trmRequiresBackOffice: false,
                poolNumbers: [],
                offerings: [],
                poolBusy: false,
                msisdnAssetId: '',
                selectedPoolImsi: '',
                selectedPoolMsisdn: '',
                confirmStatusHint: '',
                notes: '',
                createdOperationId: '',
                createdOperationNumber: '',
                documentMarkedUploaded: false,
                confirmed: false,
                submitBusy: false,
                uploadBusy: false,
                confirmBusy: false,
            },
        });

        let aiModal;
        let provWizardModal;

        const perms = Vue.computed(() => StorageManager.getPermissions?.() || []);
        const hasPerm = (key, legacy) =>
            StorageManager.hasAnyPermission(perms.value, [key, ...(legacy || [])]);

        const can = Vue.computed(() => ({
            cbs: StorageManager.hasAnyPermission(perms.value, [PERM.cbs]),
            hlr: StorageManager.hasAnyPermission(perms.value, [PERM.hlr]),
            escalate: StorageManager.hasAnyPermission(perms.value, [PERM.escalate]),
            provisioning: hasPerm(PERM.provisioning, PERM.provisioningLegacy),
            network: hasPerm(PERM.network, PERM.networkLegacy),
            takeover: StorageManager.hasAnyPermission(perms.value, [PERM.takeover]),
            changeGsm: StorageManager.hasAnyPermission(perms.value, [PERM.changeGsm]),
            changeNumber: StorageManager.hasAnyPermission(perms.value, [PERM.changeNumber]),
            termination: StorageManager.hasAnyPermission(perms.value, [PERM.termination]),
            reconnect: StorageManager.hasAnyPermission(perms.value, [
                PERM.reconnect,
                'telecom.line.reconnect',
            ]),
            collection: StorageManager.hasAnyPermission(perms.value, [
                PERM.collection,
                'telecom.line.collection',
                'telecom.line.collection_request',
            ]),
            suspension: StorageManager.hasAnyPermission(perms.value, [
                PERM.suspension,
                'telecom.line.suspension',
                'telecom.line.suspension_request',
            ]),
            refund: StorageManager.hasAnyPermission(perms.value, [
                PERM.refund,
                'telecom.line.refund',
                'telecom.line.refund_request',
            ]),
            deviceSale: StorageManager.hasAnyPermission(perms.value, [
                PERM.deviceSale,
                'telecom.device.sell',
                'telecom.device.sell_request',
            ]),
        }));

        const canRecharge = Vue.computed(() => {
            const roles = StorageManager.getUserRoles?.() || [];
            const perms = StorageManager.getUserPermissions?.() || [];
            return (
                StorageManager.hasAnyPermission?.(perms, ['telecom.line.recharge']) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(r))
            );
        });

        const normalizeSub = (s) => ({
            id: s.id ?? s.Id,
            subscriberProfileId: s.subscriberProfileId ?? s.SubscriberProfileId,
            msisdn: s.msisdn ?? s.Msisdn,
            msisdnAssetId: s.msisdnAssetId ?? s.MsisdnAssetId,
            isPrimaryLine: !!(s.isPrimaryLine ?? s.IsPrimaryLine),
            productId: s.productId ?? s.ProductId,
            productName: s.productName ?? s.ProductName,
            productOfferingId: s.productOfferingId ?? s.ProductOfferingId,
            productOfferingName: s.productOfferingName ?? s.ProductOfferingName,
            subscriptionTypeName: s.subscriptionTypeName ?? s.SubscriptionTypeName,
            subscriptionTypeCode: s.subscriptionTypeCode ?? s.SubscriptionTypeCode,
            simType: s.simType ?? s.SimType,
            iccid: s.iccid ?? s.Iccid,
            imsi: s.imsi ?? s.Imsi,
            profileOperationalStatus: s.profileOperationalStatus ?? s.ProfileOperationalStatus,
            documentStatusLabel: s.documentStatusLabel ?? s.DocumentStatusLabel,
            serviceLineTypeLabel: s.serviceLineTypeLabel ?? s.ServiceLineTypeLabel,
            languagePreferenceLabel: s.languagePreferenceLabel ?? s.LanguagePreferenceLabel,
            activationDateUtc: s.activationDateUtc ?? s.ActivationDateUtc,
            loyaltyPoints: s.loyaltyPoints ?? s.LoyaltyPoints,
            loyaltyTier: s.loyaltyTier ?? s.LoyaltyTier,
            prepaidBalance: s.prepaidBalance ?? s.PrepaidBalance,
            postpaidCreditLimit: s.postpaidCreditLimit ?? s.PostpaidCreditLimit,
            churnRiskScore: s.churnRiskScore ?? s.ChurnRiskScore,
            simStatus: s.simStatus ?? s.SimStatus,
            createdAtUtc: s.createdAtUtc ?? s.CreatedAtUtc,
        });

        const allSubscriptions = Vue.computed(() =>
            (state.profile?.core?.activeSubscriptions || state.profile?.core?.ActiveSubscriptions || [])
                .map(normalizeSub)
        );

        const selectedLineTerminated = Vue.computed(() => {
            const key = state.prov.selectedLineKey;
            const sub =
                allSubscriptions.value.find(
                    (s) => `${s.subscriberProfileId}|${s.msisdn}|${s.msisdnAssetId}` === key
                ) || allSubscriptions.value[0];
            return String(sub?.profileOperationalStatus || '').toLowerCase() === 'terminated';
        });

        const selectedLineSuspended = Vue.computed(() => {
            const key = state.prov.selectedLineKey;
            const sub =
                allSubscriptions.value.find(
                    (s) => `${s.subscriberProfileId}|${s.msisdn}|${s.msisdnAssetId}` === key
                ) || allSubscriptions.value[0];
            const st = String(sub?.profileOperationalStatus || '').toLowerCase();
            return st === 'suspended' || st === 'suspendedinbound' || st === 'suspendedoutbound';
        });

        const selectedLineActive = Vue.computed(
            () => !selectedLineTerminated.value && !selectedLineSuspended.value
        );

        const lineOptions = Vue.computed(() => {
            return allSubscriptions.value.map((s) => ({
                key: `${s.subscriberProfileId}|${s.msisdn}|${s.msisdnAssetId}`,
                label: `${s.msisdn || '—'} · ${s.productOfferingName || s.productName || 'خط'}`,
                subscriberProfileId: s.subscriberProfileId,
                msisdn: s.msisdn,
                msisdnAssetId: s.msisdnAssetId,
            }));
        });

        const parseSelectedLine = () => {
            const key = state.prov.selectedLineKey;
            const found = lineOptions.value.find((x) => x.key === key);
            if (found) return found;
            const subs = state.profile?.core?.activeSubscriptions || [];
            const s = subs[0];
            if (!s) return null;
            return {
                subscriberProfileId: s.subscriberProfileId,
                msisdn: s.msisdn,
                msisdnAssetId: s.msisdnAssetId,
            };
        };

        const selectedSubscription = Vue.computed(() => {
            const line = parseSelectedLine();
            if (!line) return null;
            return (
                allSubscriptions.value.find(
                    (s) =>
                        (line.msisdnAssetId && s.msisdnAssetId === line.msisdnAssetId) ||
                        (line.msisdn && s.msisdn === line.msisdn)
                ) || null
            );
        });

        const selectedLineOutstandingBalance = Vue.computed(() => {
            const sub = selectedSubscription.value;
            if (!sub?.id) return null;
            const w = state.lineWallets[sub.id];
            const raw = w?.outstandingBalance ?? w?.OutstandingBalance;
            if (raw === undefined || raw === null || raw === '') return null;
            const n = Number(raw);
            return Number.isFinite(n) ? n : null;
        });

        const selectedLineCollectionEligible = Vue.computed(() => {
            if (selectedLineTerminated.value) return false;
            const outstanding = selectedLineOutstandingBalance.value;
            if (outstanding != null && outstanding < 0) return true;
            return selectedLineSuspended.value;
        });

        const ensureLineSelected = () => {
            if (!state.prov.selectedLineKey && lineOptions.value.length) {
                state.prov.selectedLineKey = lineOptions.value[0].key;
            }
        };

        const getModal = (id, ref) => {
            if (!ref) ref = new bootstrap.Modal(document.getElementById(id));
            return ref;
        };

        const primaryMsisdn = () => {
            const subs = state.profile?.core?.activeSubscriptions || [];
            const primary = subs.find((s) => s.isPrimaryLine) || subs[0];
            return primary?.msisdn || state.profile?.core?.primaryPhone || '';
        };

        const activeVasCount = Vue.computed(() => {
            const list = state.profile?.activeVasServices || [];
            return list.filter((v) => String(v.status).toLowerCase() === 'active').length;
        });

        const vasSummary = Vue.computed(() => {
            const list = state.profile?.activeVasServices || [];
            let active = 0;
            let suspended = 0;
            list.forEach((v) => {
                const s = String(v.status || '').toLowerCase();
                if (s === 'active') active += 1;
                else if (s === 'suspended') suspended += 1;
            });
            return { total: list.length, active, suspended };
        });

        const groupedVasByLine = Vue.computed(() => {
            const list = state.profile?.activeVasServices || [];
            const map = new Map();
            list.forEach((v) => {
                const key = (v.msisdn || '—').trim();
                if (!map.has(key)) map.set(key, []);
                map.get(key).push(v);
            });
            return Array.from(map.entries())
                .sort((a, b) => a[0].localeCompare(b[0], 'ar'))
                .map(([msisdn, items]) => {
                    const sorted = [...items].sort((a, b) =>
                        (a.serviceNameAr || '').localeCompare(b.serviceNameAr || '', 'ar')
                    );
                    return {
                        msisdn,
                        items: sorted,
                        activeCount: sorted.filter((x) => String(x.status).toLowerCase() === 'active').length,
                    };
                });
        });

        const activeLinesCount = Vue.computed(() => allSubscriptions.value.length);

        const resolveCustomerKindLabel = (core) => {
            if (!core) return '—';
            const label = core.customerKindLabel ?? core.CustomerKindLabel;
            if (label) return label;
            const raw = core.customerKind ?? core.CustomerKind;
            const s = String(raw ?? '').toLowerCase();
            if (s === 'corporate' || raw === 1 || raw === '1') return 'شركة';
            if (s === 'individual' || raw === 0 || raw === '0') return 'فرد';
            return '—';
        };

        const customerKindLabel = Vue.computed(() => resolveCustomerKindLabel(state.profile?.core));

        const customerStatusLabel = Vue.computed(() => {
            const core = state.profile?.core;
            const label = core?.statusLabel ?? core?.StatusLabel;
            if (label) return label;
            const s = core?.status ?? core?.Status;
            if (s === 0 || s === 'Active') return 'نشط';
            if (s === 1 || s === 'Suspended') return 'موقوف';
            if (s === 2 || s === 'Closed') return 'مغلق';
            if (s === 3 || s === 'Blacklisted') return 'قائمة سوداء';
            return String(s ?? '—');
        });

        const coreField = (key, raw = false) => {
            const core = state.profile?.core;
            if (!core) return '—';
            const pascal = key.charAt(0).toUpperCase() + key.slice(1);
            const val = core[key] ?? core[pascal];
            if (val == null || val === '') return '—';
            if (raw) return val;
            return val;
        };

        const isIndividual = Vue.computed(() => {
            const core = state.profile?.core;
            const kind = String(core?.customerKind ?? core?.CustomerKind ?? '').toLowerCase();
            return kind === 'individual' || kind === '0';
        });

        const isCorporate = Vue.computed(() => {
            const core = state.profile?.core;
            const kind = String(core?.customerKind ?? core?.CustomerKind ?? '').toLowerCase();
            return kind === 'corporate' || kind === '1';
        });

        const statusBadgeClass = Vue.computed(() => {
            const s = String(state.profile?.core?.status ?? state.profile?.core?.Status ?? '').toLowerCase();
            if (s === '0' || s === 'active') return 'bg-success';
            if (s === '1' || s === 'suspended') return 'bg-warning text-dark';
            if (s === '3' || s === 'blacklisted') return 'bg-dark';
            return 'bg-secondary';
        });

        const operationalStatusLabel = (s) => {
            if (!s) return '—';
            const key = String(s).replace(/\s/g, '');
            return OPERATIONAL_LABELS[key] ?? s;
        };

        const mappedTickets = Vue.computed(() =>
            (state.profile?.supportTickets || []).map((r) => {
                const status = Number(r.status ?? r.Status ?? 0);
                const priority = Number(r.priority ?? r.Priority ?? 1);
                return {
                    id: r.id ?? r.Id,
                    ticketNumber: r.ticketNumber ?? r.TicketNumber,
                    msisdn: r.msisdn ?? r.Msisdn,
                    notes: r.notes ?? r.Notes,
                    status,
                    statusLabel: STATUS[status] ?? '—',
                    statusBadge:
                        status === RESOLVED
                            ? 'bg-success'
                            : status === 3
                              ? 'bg-dark'
                              : status === 1
                                ? 'bg-primary'
                                : 'bg-warning text-dark',
                    priorityLabel: PRIORITY[priority] ?? '—',
                    issueLabel: ISSUE[Number(r.issueType ?? r.IssueType)] ?? '—',
                    createdDisplay: formatDt(r.createdAtUtc ?? r.CreatedAtUtc),
                    canOperate: status !== RESOLVED,
                };
            })
        );

        const isTicketBusy = (ticketId, action) =>
            state.opBusy?.ticketId === ticketId && state.opBusy?.action === action;

        const setOpBusy = (ticketId, action) => {
            state.opBusy = action ? { ticketId, action } : null;
        };

        const toastSuccess = (title, html) => {
            if (typeof Swal !== 'undefined') {
                Swal.fire({ icon: 'success', title, html, timer: 2800, showConfirmButton: false });
            }
        };

        const toastError = (e, fallback) => {
            const msg = pickHttpErrorMessage(e) || fallback;
            if (typeof Swal !== 'undefined') {
                Swal.fire({ icon: 'error', title: msg });
            }
        };

        const normalizeCbsMsisdn = (raw) => {
            const m = String(raw || '').trim();
            if (m.length === 10 && m.startsWith('09')) return '093' + m.slice(2);
            return m;
        };

        const formatAmount = (val) => {
            if (val == null || val === '') return '—';
            const n = Number(val);
            if (Number.isNaN(n)) return String(val);
            return n.toLocaleString('ar-SY', { maximumFractionDigits: 2 });
        };

        const BUCKET_TYPE_LABELS = {
            0: 'دقائق',
            1: 'إنترنت',
            2: 'رسائل',
            Voice: 'دقائق',
            Data: 'إنترنت',
            Sms: 'رسائل',
        };

        const bucketLabel = (b) => {
            const label = b?.label ?? b?.Label;
            if (label) return label;
            const t = b?.componentType ?? b?.ComponentType;
            return BUCKET_TYPE_LABELS[t] ?? String(t ?? '—');
        };

        const lineWallet = (subscriptionId) => state.lineWallets[subscriptionId] ?? null;

        const loadLineWallets = async () => {
            if (!state.customerId) return;
            const subs = allSubscriptions.value;
            subs.forEach((s) => {
                state.lineWalletsBusy[s.id] = true;
            });
            try {
                const res = await AxiosManager.get(
                    '/Customer/GetCustomer360LineWallets?customerId=' + encodeURIComponent(state.customerId),
                    {}
                );
                const map =
                    res?.data?.content?.walletsBySubscriptionId ??
                    res?.data?.content?.WalletsBySubscriptionId ??
                    {};
                state.lineWallets = { ...map };
                const primary = subs.find((s) => s.isPrimaryLine) || subs[0];
                const primaryWallet = primary ? state.lineWallets[primary.id] : null;
                if (primaryWallet?.success) {
                    state.cbsData = {
                        success: true,
                        msisdn: primaryWallet.msisdn || primary.msisdn,
                        balance: primaryWallet.balance,
                        currency: primaryWallet.currency || 'SYP',
                    };
                }
            } catch {
                state.lineWallets = {};
            } finally {
                subs.forEach((s) => {
                    state.lineWalletsBusy[s.id] = false;
                });
            }
        };

        const loadCbs = async () => {
            await loadLineWallets();
        };

        const pollPaymentDetail = async (paymentId, maxAttempts = 12) => {
            for (let i = 0; i < maxAttempts; i++) {
                const res = await AxiosManager.get(
                    '/Telecom/GetPaymentTransactionDetail?id=' + encodeURIComponent(paymentId),
                    {}
                );
                const detail = res?.data?.content ?? res?.data?.Content;
                const status = detail?.status ?? detail?.Status;
                if (status === 2 || status === 'Completed') return detail;
                if (status === 3 || status === 'Failed') {
                    throw Object.assign(
                        new Error(detail?.failureReason || detail?.FailureReason || 'فشلت معاملة الدفع'),
                        { response: res }
                    );
                }
                await new Promise((r) => setTimeout(r, 500));
            }
            return null;
        };

        const executePaymentFlow = async (customerId, subscriptionId, busyKey, onSuccess) => {
            const { value: method } = await Swal.fire({
                title: 'طريقة الشحن',
                input: 'radio',
                inputOptions: { wallet: 'محفظة / نقد', voucher: 'قسيمة (كرت شحن)' },
                inputValue: 'wallet',
                showCancelButton: true,
                confirmButtonText: 'متابعة',
                cancelButtonText: 'إلغاء',
            });
            if (!method) return;

            let createBody;
            let gatewayRef;

            if (method === 'voucher') {
                const { value: voucherCode } = await Swal.fire({
                    title: 'رمز القسيمة',
                    input: 'text',
                    inputPlaceholder: 'مثال: VOUCH-DEMO-15000',
                    showCancelButton: true,
                    confirmButtonText: 'تحقق',
                    inputValidator: (v) => (!v || !String(v).trim() ? 'أدخل رمز القسيمة' : undefined),
                });
                if (!voucherCode) return;
                const valRes = await AxiosManager.post('/Telecom/ValidateVoucher', {
                    voucherCode: String(voucherCode).trim(),
                });
                const val = valRes?.data?.content ?? valRes?.data?.Content;
                if (!(val?.valid ?? val?.Valid)) {
                    Swal.fire({ icon: 'error', title: val?.messageAr || val?.MessageAr || 'قسيمة غير صالحة' });
                    return;
                }
                const ok = await Swal.fire({
                    icon: 'info',
                    title: 'قسيمة صالحة',
                    text: val?.messageAr || val?.MessageAr,
                    showCancelButton: true,
                    confirmButtonText: 'شحن بالقسيمة',
                });
                if (!ok.isConfirmed) return;
                createBody = {
                    type: 1,
                    customerId,
                    subscriptionId,
                    amount: val?.faceValue ?? val?.FaceValue ?? 0,
                    paymentChannel: 2,
                    serviceChannel: 0,
                    voucherCode: String(voucherCode).trim(),
                    createdById: StorageManager.getUserId(),
                };
                gatewayRef = `VCHR-${String(voucherCode).trim()}`;
            } else {
                const { value: amountStr } = await Swal.fire({
                    title: 'مبلغ الشحن',
                    input: 'number',
                    inputPlaceholder: '15000',
                    showCancelButton: true,
                    confirmButtonText: 'التالي',
                    inputValidator: (v) => {
                        const n = parseFloat(v);
                        if (!v || Number.isNaN(n) || n <= 0) return 'أدخل مبلغاً صحيحاً';
                    },
                });
                if (!amountStr) return;
                const { value: gw } = await Swal.fire({
                    title: 'مرجع الدفع',
                    input: 'text',
                    inputPlaceholder: 'WAL-2026-001234',
                    showCancelButton: true,
                    confirmButtonText: 'تأكيد الشحن',
                    inputValidator: (v) => (!v || !String(v).trim() ? 'أدخل مرجع الدفع' : undefined),
                });
                if (!gw) return;
                createBody = {
                    type: 0,
                    customerId,
                    subscriptionId,
                    amount: parseFloat(amountStr),
                    paymentChannel: 1,
                    serviceChannel: 0,
                    createdById: StorageManager.getUserId(),
                };
                gatewayRef = String(gw).trim();
            }

            state.rechargeBusy = busyKey;
            try {
                const createRes = await AxiosManager.post('/Telecom/CreatePaymentTransaction', createBody);
                const draft = createRes?.data?.content ?? createRes?.data?.Content;
                const paymentId = draft?.paymentId ?? draft?.PaymentId;
                if (!paymentId) throw new Error('لم يُنشأ مسودة الدفع');

                const confirmRes = await AxiosManager.post('/Telecom/ConfirmPaymentTransaction', {
                    paymentId,
                    gatewayReference: gatewayRef,
                    confirmedById: StorageManager.getUserId(),
                });
                const confirm = confirmRes?.data?.content ?? confirmRes?.data?.Content;
                if (!(confirm?.success ?? confirm?.Success)) {
                    throw new Error(confirm?.messageAr || confirm?.MessageAr || 'فشل تأكيد الدفع');
                }
                await pollPaymentDetail(paymentId);
                await onSuccess();
                Swal.fire({
                    icon: 'success',
                    title: 'إيصال الدفع',
                    html:
                        `<p>${confirm?.messageAr || confirm?.MessageAr || 'تم الشحن'}</p>` +
                        `<p class="small"><strong>PAY:</strong> ${confirm?.paymentNumber ?? confirm?.PaymentNumber}</p>` +
                        `<p class="small"><strong>إيصال:</strong> ${confirm?.receiptNumber ?? confirm?.ReceiptNumber}</p>` +
                        `<p class="small"><strong>CorrelationId:</strong> <span dir="ltr">${confirm?.correlationId ?? confirm?.CorrelationId}</span></p>` +
                        `<p class="small"><strong>الرصيد:</strong> ${formatAmount(confirm?.newBalance ?? confirm?.NewBalance)} ل.س</p>`,
                });
            } catch (e) {
                toastError(e, 'تعذّر تنفيذ الشحن');
            } finally {
                state.rechargeBusy = '';
            }
        };

        const openRechargeModal = async (sub) => {
            const wallet = lineWallet(sub.id);
            const msisdn = sub.msisdn || wallet?.msisdn || '';
            if (!msisdn) {
                Swal.fire({ icon: 'warning', title: 'لا يوجد رقم خط للشحن' });
                return;
            }
            await executePaymentFlow(state.customerId, sub.id, sub.id, () => loadProfile(true));
        };

        const loadProfile = async (silent = false) => {
            if (!state.customerId) {
                state.loadError = 'معرّف المشترك مفقود في الرابط.';
                state.loading = false;
                return;
            }
            if (!silent) {
                state.loading = true;
            }
            state.loadError = null;
            try {
                const res = await AxiosManager.get(
                    '/Customer/GetCustomer360Profile?customerId=' + encodeURIComponent(state.customerId),
                    {}
                );
                const content = res?.data?.content ?? res?.data?.Content ?? null;
                state.profile = content;
                if (!state.profile?.core) {
                    state.loadError = 'لم يُعثر على ملف المشترك.';
                } else {
                    state.aiSim.msisdn = primaryMsisdn() || state.aiSim.msisdn;
                    ensureLineSelected();
                    await loadCbs();
                }
            } catch (e) {
                state.profile = null;
                state.loadError = e?.response?.data?.message || e?.message || 'تعذّر تحميل Customer 360';
            } finally {
                if (!silent) {
                    state.loading = false;
                }
            }
        };

        const checkHlr = async (sub) => {
            const profileId = sub?.subscriberProfileId ?? sub?.SubscriberProfileId;
            if (!profileId) return;
            state.hlrBusy = sub.id;
            try {
                const res = await AxiosManager.get(
                    '/Telecom/QueryHlrLiveStatus?subscriberProfileId=' +
                        encodeURIComponent(profileId),
                    {}
                );
                const content = res?.data?.content ?? res?.data?.Content ?? res?.data;
                state.hlrBySub[sub.id] = {
                    hlrSubscriberState:
                        content?.hlrSubscriberState ?? content?.HlrSubscriberState ?? content?.message ?? '—',
                    differsFromCrm: !!(content?.differsFromCrm ?? content?.DiffersFromCrm),
                    isOnline: content?.isOnline ?? content?.IsOnline,
                };
            } catch (e) {
                state.hlrBySub[sub.id] = {
                    hlrSubscriberState: pickHttpErrorMessage(e) || 'Error',
                    differsFromCrm: false,
                };
            } finally {
                state.hlrBusy = '';
            }
        };

        const openAiWizard = () => {
            state.aiSim.msisdn = primaryMsisdn() || state.aiSim.msisdn;
            if (!aiModal) {
                aiModal = new bootstrap.Modal(document.getElementById('c360AiWizardModal'));
            }
            aiModal.show();
        };

        const simulateAiCall = async () => {
            const msisdn = (state.aiSim.msisdn || '').trim();
            const transcript = (state.aiSim.transcript || '').trim();
            if (!msisdn || transcript.length < 5) {
                Swal.fire({ icon: 'warning', title: 'أدخل الرقم ونص المكالمة (5 أحرف+)' });
                return;
            }
            state.aiSim.busy = true;
            try {
                const res = await AxiosManager.post('/TelecomBackOffice/SimulateVoiceAiIncomingCall', {
                    msisdn,
                    rawVoiceTranscript: transcript,
                });
                const body = res?.data?.content ?? res?.data?.Content;
                aiModal?.hide();
                state.activeTab = 'tickets';
                await loadProfile(true);
                toastSuccess(
                    'تذكرة AI جاهزة',
                    `<p class="small mb-0">${body?.ticketNumber ?? ''}<br/>${body?.summaryAr ?? ''}</p>`
                );
            } catch (e) {
                toastError(e, 'تعذر إنشاء التذكرة');
            } finally {
                state.aiSim.busy = false;
            }
        };

        const forceCbs = async (t) => {
            setOpBusy(t.id, 'cbs');
            try {
                const res = await AxiosManager.post('/TelecomBackOffice/ForceCbsSync', {
                    ticketId: t.id,
                });
                const body = res?.data?.content ?? res?.data?.Content;
                const msg = body?.message || body?.Message || 'تم دفع الشحنة وتسوية CBS';
                await loadProfile(true);
                await loadCbs();
                toastSuccess(msg);
            } catch (e) {
                toastError(e, 'تعذر ضغط الشحنة');
            } finally {
                setOpBusy(null);
            }
        };

        const hlrResync = async (t) => {
            setOpBusy(t.id, 'hlr');
            try {
                const res = await AxiosManager.post('/TelecomBackOffice/HlrResyncByMsisdn', {
                    msisdn: t.msisdn,
                    technicalTicketId: t.id,
                });
                const body = res?.data?.content ?? res?.data?.Content;
                const msg = body?.message || body?.Message || 'تمت مزامنة HLR';
                await loadProfile(true);
                toastSuccess(msg);
            } catch (e) {
                toastError(e, 'فشلت مزامنة HLR');
            } finally {
                setOpBusy(null);
            }
        };

        const escalateTicket = async (t) => {
            const { value: notes } = await Swal.fire({
                title: 'تصعيد للقسم الهندسي (Tier-3)',
                input: 'textarea',
                inputLabel: 'سبب التصعيد (10 أحرف على الأقل)',
                inputPlaceholder: 'وصف المشكلة التقنية...',
                showCancelButton: true,
                confirmButtonText: 'تصعيد',
                cancelButtonText: 'إلغاء',
                inputValidator: (v) =>
                    !v || v.trim().length < 10 ? 'أدخل 10 أحرف على الأقل' : undefined,
            });
            if (!notes) return;
            setOpBusy(t.id, 'escalate');
            try {
                await AxiosManager.post('/TelecomBackOffice/EscalateToTier3', {
                    ticketId: t.id,
                    escalationNotes: notes.trim(),
                });
                await loadProfile(true);
                toastSuccess('تم تصعيد التذكرة لفريق الشبكة الأساسية');
            } catch (e) {
                toastError(e, 'فشل التصعيد');
            } finally {
                setOpBusy(null);
            }
        };

        const operationalBadgeClass = (s) => {
            const v = String(s || '').toLowerCase();
            if (v.includes('active') || v.includes('نشط')) return 'bg-success';
            if (v.includes('suspend') || v.includes('موقوف')) return 'bg-warning text-dark';
            return 'bg-secondary';
        };

        const vasStatusLabel = (s) => {
            const v = String(s || '').toLowerCase();
            if (v === 'active') return 'نشطة';
            if (v === 'suspended') return 'موقوفة';
            return s || '—';
        };

        const vasBadgeClass = (s) => {
            const v = String(s || '').toLowerCase();
            if (v === 'active') return 'bg-success';
            if (v === 'suspended') return 'bg-warning text-dark';
            return 'bg-secondary';
        };

        const wizardTitle = Vue.computed(
            () => WIZARD_TITLES[state.wizard.kind] || 'معالج التشغيل'
        );

        const activateRequiredDeposit = Vue.computed(() => {
            if (state.wizard.kind !== 'activate') return 0;
            const o = (state.wizard.offerings || []).find(
                (x) => x.id === state.wizard.selectedOfferingId
            );
            return Number(o?.unitPrice ?? o?.UnitPrice ?? 0) || 0;
        });

        const canWizardFinishStep2 = Vue.computed(() => {
            if (state.wizard.kind === 'addpackage') {
                return !!state.wizard.vasActivated;
            }
            if (!state.wizard.createdOperationId || !state.wizard.documentMarkedUploaded) return false;
            if (state.wizard.kind === 'changeGsm') {
                return state.wizard.confirmed;
            }
            if (state.wizard.kind === 'activate') {
                if (activateRequiredDeposit.value > 0 && !state.wizard.paymentRecorded) return false;
            }
            if (state.wizard.kind === 'takeover') return true;
            if (state.wizard.kind === 'simswap' && state.wizard.simLostOrStolen) return true;
            if (state.wizard.kind === 'changeNumber' && state.wizard.cnRequiresBackOffice) return true;
            if (state.wizard.kind === 'termination' && state.wizard.trmRequiresBackOffice) return true;
            if (state.wizard.kind === 'suspension' && state.wizard.susRequiresBackOffice) return true;
            if (state.wizard.kind === 'reconnect' && state.wizard.rcnRequiresBackOffice) return true;
            if (state.wizard.kind === 'refund' && state.wizard.rfdRequiresBackOffice) return true;
            if (state.wizard.kind === 'badDebt' && state.wizard.bdrRequiresBackOffice) return true;
            if (state.wizard.kind === 'deviceSale' && state.wizard.devRequiresFinance) return true;
            return state.wizard.confirmed;
        });

        const onTerminationTypeChange = () => {
            const ty = (state.wizard.trmTerminationType || '').trim();
            state.wizard.trmRequiresBackOffice =
                ty === 'Fraud' || ty === 'Regulatory' || ty === 'Collections';
        };

        const resetWizardState = () => {
            const kind = state.wizard.kind;
            state.wizard = {
                kind,
                step: 1,
                primarySubscriberProfileId: '',
                primaryMsisdnAssetId: '',
                primaryLabel: '',
                takeoverSearchNationalId: '',
                takeoverSearchPhone: '',
                takeoverResults: [],
                takeoverTargetCustomerId: '',
                takeoverTargetProfileId: '',
                takeoverTargetLabel: '',
                takeoverTransferReason: '',
                takeoverDepositPolicy: 1,
                takeoverBusy: false,
                migrationOffers: [],
                migrationBusy: false,
                mgrCurrentPlan: '',
                cgtTargets: [],
                cgtTargetsBusy: false,
                cgtCurrentTypeLabel: '',
                cgtTargetTypeId: '',
                cgtMigrationReason: '',
                cgtOffers: [],
                cgtOffersBusy: false,
                selectedOfferingId: '',
                vasCatalog: [],
                catalogBusy: false,
                selectedVasCode: '',
                vasActivated: false,
                simIccid: '',
                simReplacementReason: '',
                simLostOrStolen: false,
                cnTargetMsisdnAssetId: '',
                cnNumberChangeReason: '',
                cnPremiumFeeAmount: '',
                cnPoolNumbers: [],
                cnPoolBusy: false,
                cnRequiresBackOffice: false,
                cnCurrentMsisdn: '',
                trmTerminationType: 'Voluntary',
                trmTerminationReason: '',
                trmRetentionOfferOutcome: 'Declined',
                trmRequiresBackOffice: false,
                susSuspensionType: 'CustomerRequest',
                susSuspensionReason: '',
                susBarringLevel: 'Full',
                susAutoReconnectEnabled: false,
                susEndDateLocal: '',
                susRequiresBackOffice: false,
                rcnReconnectReason: '',
                rcnClearanceType: 'Customer',
                rcnPaymentReference: '',
                rcnFraudClearanceConfirmed: false,
                rcnRequiresBackOffice: false,
                rcnEligibility: null,
                rcnEligibilityBusy: false,
                rfdRefundType: 'Deposit',
                rfdRefundMethod: 'CreditNote',
                rfdRefundAmount: '',
                rfdRefundReason: '',
                rfdDepositSnapshot: null,
                rfdWalletSnapshot: null,
                rfdRequiresBackOffice: false,
                bdrCollectionAction: 'PaymentRecorded',
                bdrDunningStage: 'Reminder1',
                bdrCollectedAmount: '',
                bdrWriteOffAmount: '',
                bdrPaymentReference: '',
                bdrAgencyReference: '',
                bdrPaymentPlanMonths: '',
                bdrRequiresBackOffice: false,
                bdrEligibility: null,
                bdrEligibilityBusy: false,
                devInventoryId: '',
                devSaleType: 'Cash',
                devInstallmentPlanId: '',
                devDownPayment: '',
                devPaymentReference: '',
                devPaymentChannel: 0,
                devDevices: [],
                devPlans: [],
                devRequiresFinance: false,
                devFinancingPreview: '',
                poolNumbers: [],
                offerings: [],
                poolBusy: false,
                msisdnAssetId: '',
                selectedPoolImsi: '',
                selectedPoolMsisdn: '',
                confirmStatusHint: '',
                notes: '',
                createdOperationId: '',
                createdOperationNumber: '',
                documentMarkedUploaded: false,
                identityFile: null,
                confirmed: false,
                submitBusy: false,
                uploadBusy: false,
                confirmBusy: false,
                activationChannel: 0,
                dealerCode: '',
                paymentReference: '',
                paymentChannel: 0,
                paymentAmount: '',
                paymentRecorded: false,
                paymentBusy: false,
                operationCorrelationId: '',
            };
        };

        const onSusTypeChange = () => {
            const ty = (state.wizard.susSuspensionType || '').trim();
            state.wizard.susRequiresBackOffice = ty === 'Fraud' || ty === 'Regulatory';
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
            } catch {
                state.wizard.rcnEligibility = null;
            } finally {
                state.wizard.rcnEligibilityBusy = false;
            }
        };

        const onRcnClearanceChange = () => {
            loadReconnectEligibility();
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
            } catch {
                state.wizard.bdrEligibility = null;
            } finally {
                state.wizard.bdrEligibilityBusy = false;
            }
        };

        const onBdrActionChange = () => {
            loadBadDebtEligibility();
        };

        const onRefundTypeChange = async () => {
            state.wizard.rfdRequiresBackOffice = state.wizard.rfdRefundType === 'SyriatelCash';
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
            const row = (state.wizard.devDevices || []).find(
                (d) => String(d.id ?? d.Id) === String(state.wizard.devInventoryId)
            );
            if (row) {
                state.wizard.devDownPayment = String(row.listPrice ?? row.ListPrice ?? '');
            }
        };

        const recordDeviceDownPaymentC360 = async () => {
            if (!state.wizard.createdOperationId) return;
            const amount = Number(state.wizard.devDownPayment);
            const ref = (state.wizard.devPaymentReference || '').trim();
            if (!amount || !ref) {
                Swal.fire({ icon: 'warning', title: 'أدخل مبلغ الدفع ومرجع الدفع' });
                return;
            }
            state.wizard.paymentBusy = true;
            try {
                const res = await AxiosManager.post('/Telecom/RecordDeviceDownPayment', {
                    operationId: state.wizard.createdOperationId,
                    amountPaid: amount,
                    paymentChannel: Number(state.wizard.devPaymentChannel) || 0,
                    paymentReference: ref,
                    updatedById: StorageManager.getUserId(),
                });
                if (res?.data?.code === 200) {
                    state.wizard.documentMarkedUploaded = true;
                    Swal.fire({ icon: 'success', title: 'تم تسجيل الدفع', timer: 1200, showConfirmButton: false });
                } else {
                    throw Object.assign(new Error(res?.data?.message || 'فشل'), { response: res });
                }
            } catch (e) {
                toastError(e, 'تعذّر تسجيل دفع الجهاز');
            } finally {
                state.wizard.paymentBusy = false;
            }
        };

        const onWizardIdentityFileChange = (ev) => {
            state.wizard.identityFile = ev?.target?.files?.[0] || null;
        };

        const bindPrimaryFromLine = () => {
            const line = parseSelectedLine();
            if (!line?.subscriberProfileId) return false;
            state.wizard.primarySubscriberProfileId = line.subscriberProfileId;
            state.wizard.primaryMsisdnAssetId = line.msisdnAssetId || '';
            state.wizard.primaryLabel =
                lineOptions.value.find((x) => x.key === state.prov.selectedLineKey)?.label ||
                line.msisdn ||
                '—';
            return true;
        };

        const loadWizardVasCatalog = async () => {
            const line = parseSelectedLine();
            if (!line?.subscriberProfileId) {
                state.wizard.vasCatalog = [];
                return;
            }
            state.wizard.catalogBusy = true;
            try {
                let url =
                    '/Product/GetEligibleVasOfferings?subscriberProfileId=' +
                    encodeURIComponent(line.subscriberProfileId);
                if (line.msisdnAssetId) {
                    url += '&msisdnAssetId=' + encodeURIComponent(line.msisdnAssetId);
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
                state.wizard.catalogBusy = false;
            }
        };

        const loadWizardMigrationOffers = async () => {
            const line = parseSelectedLine();
            if (!line?.subscriberProfileId) return;
            state.wizard.migrationBusy = true;
            try {
                let url =
                    '/Product/GetMigrationEligibleProducts?subscriberProfileId=' +
                    encodeURIComponent(line.subscriberProfileId);
                if (line.msisdnAssetId) {
                    url += '&msisdnAssetId=' + encodeURIComponent(line.msisdnAssetId);
                }
                const res = await AxiosManager.get(url, {});
                const content = res?.data?.content ?? res?.data?.Content ?? {};
                state.wizard.migrationOffers = (content.data || content.Data || []).map((o) => ({
                    id: o.id,
                    name: o.name,
                    serviceCode: o.serviceCode,
                }));
                state.wizard.mgrCurrentPlan = content.currentProductName || content.CurrentProductName || '';
            } catch {
                state.wizard.migrationOffers = [];
            } finally {
                state.wizard.migrationBusy = false;
            }
        };

        const loadWizardChangeNumberPool = async () => {
            const currentId = (state.wizard.primaryMsisdnAssetId || '').trim();
            state.wizard.cnCurrentMsisdn = state.wizard.primaryLabel || '';
            state.wizard.cnPoolBusy = true;
            try {
                const res = await AxiosManager.get('/Telecom/GetMsisdnAssetPoolList?status=Available', {});
                const rows = parseMsisdnPoolRows(res);
                state.wizard.cnPoolNumbers = rows.filter((r) => {
                    const id = r.id ?? r.Id;
                    if (currentId && String(id) === currentId) return false;
                    return isAvailableMsisdnPoolRow(r);
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

        const loadWizardChangeGsmTargets = async () => {
            const line = parseSelectedLine();
            if (!line?.subscriberProfileId) return;
            state.wizard.cgtTargetsBusy = true;
            try {
                let url =
                    '/Product/GetChangeGsmEligibleTargets?subscriberProfileId=' +
                    encodeURIComponent(line.subscriberProfileId);
                if (line.msisdnAssetId) {
                    url += '&msisdnAssetId=' + encodeURIComponent(line.msisdnAssetId);
                }
                const res = await AxiosManager.get(url, {});
                const content = res?.data?.content ?? {};
                state.wizard.cgtTargets = (content.data || []).map((t) => ({
                    id: t.id,
                    code: t.code,
                    nameAr: t.nameAr,
                    nameEn: t.nameEn,
                }));
                state.wizard.cgtCurrentTypeLabel =
                    content.currentSubscriptionTypeLabel || content.CurrentSubscriptionTypeLabel || '';
            } catch {
                state.wizard.cgtTargets = [];
            } finally {
                state.wizard.cgtTargetsBusy = false;
            }
        };

        const onCgtTargetTypeChanged = async () => {
            state.wizard.selectedOfferingId = '';
            const line = parseSelectedLine();
            const targetId = (state.wizard.cgtTargetTypeId || '').trim();
            if (!line?.subscriberProfileId || !targetId) {
                state.wizard.cgtOffers = [];
                return;
            }
            state.wizard.cgtOffersBusy = true;
            try {
                let url =
                    '/Product/GetChangeGsmEligibleProducts?subscriberProfileId=' +
                    encodeURIComponent(line.subscriberProfileId) +
                    '&targetSubscriptionTypeId=' +
                    encodeURIComponent(targetId);
                if (line.msisdnAssetId) {
                    url += '&msisdnAssetId=' + encodeURIComponent(line.msisdnAssetId);
                }
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

        const onActivateMsisdnChanged = () => {
            const id = (state.wizard.msisdnAssetId || '').trim();
            const row = (state.wizard.poolNumbers || []).find((a) => a.id === id);
            state.wizard.selectedPoolMsisdn = row?.msisdn ?? row?.Msisdn ?? '';
            state.wizard.selectedPoolImsi =
                row?.pairedImsi ?? row?.PairedImsi ?? row?.imsi ?? row?.Imsi ?? '';
        };

        const pollOperationAfterConfirm = async (operationId) => {
            const terminal = new Set([3, 4, 'Completed', 'Failed']);
            const slowKinds = [
                'changeGsm',
                'changeNumber',
                'termination',
                'suspension',
                'reconnect',
                'refund',
                'badDebt',
                'deviceSale',
            ];
            const intervalMs = slowKinds.includes(state.wizard.kind) ? 2000 : 1200;
            const maxPolls = slowKinds.includes(state.wizard.kind) ? 15 : 10;
            for (let i = 0; i < maxPolls; i++) {
                await new Promise((r) => setTimeout(r, intervalMs));
                try {
                    const res = await AxiosManager.get(
                        '/Telecom/GetTelecomOperationDetail?id=' + encodeURIComponent(operationId),
                        {}
                    );
                    const data = res?.data?.content?.data ?? res?.data?.content?.Data;
                    const st = data?.status ?? data?.Status;
                    state.wizard.operationCorrelationId =
                        data?.correlationId ?? data?.CorrelationId ?? '';
                    if (terminal.has(st)) {
                        return data?.statusLabelAr ?? data?.StatusLabelAr ?? '';
                    }
                } catch {
                    /* retry */
                }
            }
            return '';
        };

        const loadActivatePoolData = async () => {
            state.wizard.poolBusy = true;
            try {
                const [poolRes, offRes] = await Promise.all([
                    AxiosManager.get('/Telecom/GetMsisdnAssetPoolList?status=Available', {}),
                    AxiosManager.get('/ProductOffering/GetProductOfferingList', {}),
                ]);
                state.wizard.poolNumbers = parseMsisdnPoolRows(poolRes).filter(isAvailableMsisdnPoolRow);
                const offContent = offRes?.data?.content ?? offRes?.data?.Content;
                const allOfferings = offContent?.data ?? offContent?.Data ?? [];
                state.wizard.offerings = (Array.isArray(allOfferings) ? allOfferings : []).filter(
                    (o) => o.isActive !== false
                );
            } catch {
                state.wizard.poolNumbers = [];
                state.wizard.offerings = [];
            } finally {
                state.wizard.poolBusy = false;
            }
        };

        const requireLine = () => {
            if (lineOptions.value.length) return true;
            Swal.fire({ icon: 'warning', title: 'لا يوجد خط نشط لهذا المشترك لتنفيذ العملية.' });
            return false;
        };

        const onWizardLineChanged = async () => {
            bindPrimaryFromLine();
            if (state.wizard.kind === 'migrate') {
                state.wizard.selectedOfferingId = '';
                await loadWizardMigrationOffers();
            }
            if (state.wizard.kind === 'changeGsm') {
                state.wizard.cgtTargetTypeId = '';
                state.wizard.selectedOfferingId = '';
                await loadWizardChangeGsmTargets();
            }
            if (state.wizard.kind === 'changeNumber') {
                state.wizard.cnTargetMsisdnAssetId = '';
                state.wizard.cnNumberChangeReason = '';
                state.wizard.cnRequiresBackOffice = false;
                await loadWizardChangeNumberPool();
            }
            if (state.wizard.kind === 'reconnect') {
                await loadReconnectEligibility();
            }
            if (state.wizard.kind === 'badDebt') {
                await loadBadDebtEligibility();
            }
        };

        const openProvisioningWizard = async (kind) => {
            const permMap = {
                takeover: () => can.value.takeover,
                migrate: () => can.value.provisioning,
                changeGsm: () => can.value.changeGsm,
                changeNumber: () => can.value.changeNumber,
                termination: () => can.value.termination && !selectedLineTerminated.value,
                suspension: () => can.value.suspension && selectedLineActive.value,
                reconnect: () => can.value.reconnect && selectedLineSuspended.value,
                refund: () => can.value.refund && !selectedLineTerminated.value,
                badDebt: () => can.value.collection && selectedLineCollectionEligible.value,
                deviceSale: () => can.value.deviceSale,
                addpackage: () => can.value.provisioning,
                simswap: () => can.value.network,
                activate: () => can.value.network,
            };
            if (!permMap[kind]?.()) {
                Swal.fire({ icon: 'info', title: 'لا توجد صلاحية لهذه العملية' });
                return;
            }
            if (kind !== 'activate' && !requireLine()) return;
            ensureLineSelected();
            state.wizard.kind = kind;
            resetWizardState();
            if (kind !== 'activate' && !bindPrimaryFromLine()) {
                Swal.fire({ icon: 'warning', title: 'تعذّر ربط الاشتراك بالعملية' });
                return;
            }
            if (kind === 'activate') {
                const subs = allSubscriptions.value;
                const p = subs.find((s) => s.isPrimaryLine) || subs[0];
                if (!p?.subscriberProfileId) {
                    Swal.fire({ icon: 'warning', title: 'لا يوجد ملف مشترك لهذا العميل' });
                    return;
                }
                state.wizard.primarySubscriberProfileId = p.subscriberProfileId;
                state.wizard.primaryLabel = state.profile?.core?.displayName || 'مشترك حالي';
                await loadActivatePoolData();
            } else if (kind === 'migrate') {
                await loadWizardMigrationOffers();
            } else if (kind === 'changeGsm') {
                await loadWizardChangeGsmTargets();
            } else if (kind === 'changeNumber') {
                await loadWizardChangeNumberPool();
            } else if (kind === 'termination') {
                onTerminationTypeChange();
            } else if (kind === 'suspension') {
                onSusTypeChange();
            } else if (kind === 'reconnect') {
                await loadReconnectEligibility();
            } else if (kind === 'badDebt') {
                await loadBadDebtEligibility();
            } else if (kind === 'deviceSale') {
                await loadDeviceWizardCatalog();
            } else if (kind === 'addpackage') {
                await loadWizardVasCatalog();
            }
            provWizardModal = getModal('c360ProvWizardModal', provWizardModal);
            provWizardModal.show();
        };

        const closeProvisioningWizard = () => {
            provWizardModal?.hide();
            resetWizardState();
            state.wizard.kind = '';
        };

        const finishProvisioningWizard = async () => {
            closeProvisioningWizard();
            state.activeTab = 'services';
            await loadProfile(true);
            await loadCbs();
        };

        const searchTakeoverTarget = async () => {
            const nat = (state.wizard.takeoverSearchNationalId || '').trim();
            const ph = (state.wizard.takeoverSearchPhone || '').trim();
            if (nat.length < 2 && ph.length < 2) {
                Swal.fire({ icon: 'info', title: 'أدخل رقم وطني أو جوال (حرفين على الأقل)' });
                return;
            }
            state.wizard.takeoverBusy = true;
            try {
                const qs = new URLSearchParams();
                if (nat) qs.set('nationalId', nat);
                if (ph) qs.set('phone', ph);
                const res = await AxiosManager.get('/Customer/FindCustomerCandidates?' + qs.toString(), {});
                state.wizard.takeoverResults = res?.data?.content?.data ?? [];
                state.wizard.takeoverTargetCustomerId = '';
                state.wizard.takeoverTargetProfileId = '';
                state.wizard.takeoverTargetLabel = '';
            } catch (e) {
                state.wizard.takeoverResults = [];
                toastError(e, 'تعذّر البحث');
            } finally {
                state.wizard.takeoverBusy = false;
            }
        };

        const selectTakeoverTarget = async (c) => {
            if (!c?.id) return;
            state.wizard.takeoverTargetCustomerId = c.id;
            state.wizard.takeoverTargetProfileId = '';
            state.wizard.takeoverTargetLabel = '';
            state.wizard.takeoverBusy = true;
            try {
                const res = await AxiosManager.get(
                    '/Customer/GetCustomer360?customerId=' + encodeURIComponent(c.id),
                    {}
                );
                const subs = res?.data?.content?.activeSubscriptions || [];
                const pick = subs.find((s) => s.isPrimaryLine) || subs[0];
                const pid = (pick?.subscriberProfileId || '').trim();
                state.wizard.takeoverTargetProfileId = pid;
                if (!pid) {
                    Swal.fire({ icon: 'warning', title: 'لا يوجد ملف مشترك للعميل المختار' });
                } else {
                    state.wizard.takeoverTargetLabel = `جاهز: ${c.name || '—'} (${pid.slice(0, 8)}…)`;
                }
            } catch (e) {
                toastError(e, 'تعذّر تحميل ملف المالك الجديد');
            } finally {
                state.wizard.takeoverBusy = false;
            }
        };

        const validateWizardStep1 = () => {
            const k = state.wizard.kind;
            if (k !== 'activate' && !(state.wizard.primarySubscriberProfileId || '').trim()) {
                Swal.fire({ icon: 'warning', title: 'لا يوجد اشتراك مرتبط' });
                return false;
            }
            if (k === 'takeover' && !(state.wizard.takeoverTargetProfileId || '').trim()) {
                Swal.fire({ icon: 'warning', title: 'اختر المالك الجديد' });
                return false;
            }
            if (k === 'takeover' && !(state.wizard.takeoverTransferReason || '').trim()) {
                Swal.fire({ icon: 'warning', title: 'سبب نقل الملكية مطلوب' });
                return false;
            }
            if (k === 'migrate' && !(state.wizard.selectedOfferingId || '').trim()) {
                Swal.fire({ icon: 'warning', title: 'اختر الباقة الجديدة' });
                return false;
            }
            if (k === 'changeGsm') {
                if (!(state.wizard.cgtTargetTypeId || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'اختر نوع الخط الجديد' });
                    return false;
                }
                if (!(state.wizard.cgtMigrationReason || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'سبب التحويل مطلوب' });
                    return false;
                }
            }
            if (k === 'addpackage' && !(state.wizard.selectedVasCode || '').trim()) {
                Swal.fire({ icon: 'warning', title: 'اختر خدمة VAS' });
                return false;
            }
            if (k === 'changeNumber') {
                if (!(state.wizard.cnTargetMsisdnAssetId || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'اختر الرقم الجديد من المخزون' });
                    return false;
                }
                if (!(state.wizard.cnNumberChangeReason || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'اختر سبب التغيير' });
                    return false;
                }
                if (state.wizard.cnRequiresBackOffice && !state.wizard.identityFile) {
                    Swal.fire({ icon: 'warning', title: 'ارفع إيصال الدفع أو موافقة المشرف' });
                    return false;
                }
            }
            if (k === 'termination') {
                if (!(state.wizard.trmTerminationReason || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'سبب الإنهاء مطلوب' });
                    return false;
                }
                if (
                    state.wizard.trmTerminationType === 'Voluntary'
                    && !(state.wizard.trmRetentionOfferOutcome || '').trim()
                ) {
                    Swal.fire({ icon: 'warning', title: 'نتيجة عرض الاحتفاظ مطلوبة' });
                    return false;
                }
                if (state.wizard.trmRequiresBackOffice && !state.wizard.identityFile) {
                    Swal.fire({ icon: 'warning', title: 'ارفع الوثيقة / القرار الإداري' });
                    return false;
                }
            }
            if (k === 'simswap') {
                if (!(state.wizard.simReplacementReason || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'اختر سبب التبديل' });
                    return false;
                }
                if ((state.wizard.simIccid || '').trim().length < 19) {
                    Swal.fire({ icon: 'warning', title: 'أدخل ICCID (19 رقم)' });
                    return false;
                }
                if (state.wizard.simLostOrStolen && !state.wizard.identityFile) {
                    Swal.fire({ icon: 'warning', title: 'ارفع إقرار / هوية المشترك' });
                    return false;
                }
            }
            if (k === 'activate') {
                const assetId = (state.wizard.msisdnAssetId || '').trim();
                const offeringId = (state.wizard.selectedOfferingId || '').trim();
                const iccid = (state.wizard.simIccid || '').trim();
                if (!assetId || !offeringId || iccid.length < 19) {
                    Swal.fire({ icon: 'warning', title: 'أكمل الرقم والباقة وICCID' });
                    return false;
                }
            }
            if (k === 'suspension') {
                if (!(state.wizard.susSuspensionReason || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'سبب الحظر مطلوب' });
                    return false;
                }
                if (state.wizard.susAutoReconnectEnabled && !(state.wizard.susEndDateLocal || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'تاريخ انتهاء الحظر مطلوب' });
                    return false;
                }
                onSusTypeChange();
            }
            if (k === 'reconnect') {
                if (!(state.wizard.rcnReconnectReason || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'سبب إعادة التفعيل مطلوب' });
                    return false;
                }
                if (state.wizard.rcnClearanceType === 'Payment' && !(state.wizard.rcnPaymentReference || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'مرجع الدفع مطلوب' });
                    return false;
                }
                await loadReconnectEligibility();
                if (state.wizard.rcnEligibility && !state.wizard.rcnEligibility.allowed && !state.wizard.rcnEligibility.Allowed) {
                    const msg = state.wizard.rcnEligibility.messageAr || state.wizard.rcnEligibility.MessageAr || '';
                    Swal.fire({ icon: 'error', title: 'غير مسموح', text: msg });
                    return false;
                }
            }
            if (k === 'refund') {
                if (!(state.wizard.rfdRefundReason || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'سبب الاسترداد مطلوب' });
                    return false;
                }
                const amt = Number(state.wizard.rfdRefundAmount);
                if (!amt || amt <= 0) {
                    Swal.fire({ icon: 'warning', title: 'مبلغ الاسترداد مطلوب' });
                    return false;
                }
            }
            if (k === 'badDebt') {
                if (state.wizard.bdrCollectionAction === 'PaymentRecorded') {
                    if (!(state.wizard.bdrPaymentReference || '').trim()) {
                        Swal.fire({ icon: 'warning', title: 'مرجع الدفع مطلوب' });
                        return false;
                    }
                    const col = Number(state.wizard.bdrCollectedAmount);
                    if (!col || col <= 0) {
                        Swal.fire({ icon: 'warning', title: 'المبلغ المحصّل مطلوب' });
                        return false;
                    }
                }
                if (
                    (state.wizard.bdrCollectionAction === 'WriteOffPartial'
                        || state.wizard.bdrCollectionAction === 'WriteOffFull')
                    && !(Number(state.wizard.bdrWriteOffAmount) > 0)
                ) {
                    Swal.fire({ icon: 'warning', title: 'مبلغ الشطب مطلوب' });
                    return false;
                }
                await loadBadDebtEligibility();
                if (state.wizard.bdrEligibility && !state.wizard.bdrEligibility.allowed && !state.wizard.bdrEligibility.Allowed) {
                    const msg = state.wizard.bdrEligibility.messageAr || state.wizard.bdrEligibility.MessageAr || '';
                    Swal.fire({ icon: 'error', title: 'غير مسموح', text: msg });
                    return false;
                }
            }
            if (k === 'deviceSale') {
                if (!(state.wizard.devInventoryId || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'اختر جهازاً من المخزون' });
                    return false;
                }
                if (state.wizard.devSaleType === 'Installment' && !(state.wizard.devInstallmentPlanId || '').trim()) {
                    Swal.fire({ icon: 'warning', title: 'اختر خطة التقسيط' });
                    return false;
                }
            }
            return true;
        };

        const buildWizardCreateBody = () => {
            const k = state.wizard.kind;
            const line = parseSelectedLine();
            const msisdn = line?.msisdn || '';
            const notesBase = `Customer360|${k}|${msisdn || state.wizard.primaryLabel}`;
            const userNotes = (state.wizard.notes || '').trim();
            const notes = userNotes ? `${notesBase}|${userNotes}` : notesBase;

            const body = {
                kind: wizardKindToApi(k),
                subscriberProfileId: state.wizard.primarySubscriberProfileId,
                notes,
                createdById: StorageManager.getUserId(),
            };

            if (k === 'takeover') {
                body.secondarySubscriberProfileId = state.wizard.takeoverTargetProfileId;
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.transferReason = (state.wizard.takeoverTransferReason || '').trim();
                body.depositTransferPolicy = Number(state.wizard.takeoverDepositPolicy) || 1;
            } else if (k === 'migrate') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.productOfferingId = state.wizard.selectedOfferingId;
            } else if (k === 'changeGsm') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.targetSubscriptionTypeId = state.wizard.cgtTargetTypeId;
                body.gsmMigrationReason = (state.wizard.cgtMigrationReason || '').trim();
                const off = (state.wizard.selectedOfferingId || '').trim();
                if (off) body.changeGsmProductOfferingId = off;
            } else if (k === 'simswap') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.simIccid = (state.wizard.simIccid || '').trim();
                body.replacementReason = (state.wizard.simReplacementReason || '').trim();
                body.isLostOrStolenReport = !!state.wizard.simLostOrStolen;
            } else if (k === 'changeNumber') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.targetMsisdnAssetId = state.wizard.cnTargetMsisdnAssetId || null;
                body.numberChangeReason = (state.wizard.cnNumberChangeReason || '').trim();
                if (state.wizard.cnPremiumFeeAmount) {
                    body.premiumFeeAmount = Number(state.wizard.cnPremiumFeeAmount);
                }
            } else if (k === 'termination') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.terminationType = (state.wizard.trmTerminationType || '').trim();
                body.terminationReason = (state.wizard.trmTerminationReason || '').trim();
                if (state.wizard.trmTerminationType === 'Voluntary') {
                    body.retentionOfferOutcome = (state.wizard.trmRetentionOfferOutcome || '').trim();
                }
            } else if (k === 'addpackage') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                const vas = state.wizard.vasCatalog.find((v) => v.serviceCode === state.wizard.selectedVasCode);
                body.targetOfferName = vas?.nameAr || state.wizard.selectedVasCode;
                body.notes = `${notes}|VAS:${state.wizard.selectedVasCode}`;
            } else if (k === 'activate') {
                body.msisdnAssetId = state.wizard.msisdnAssetId;
                body.productOfferingId = state.wizard.selectedOfferingId;
                body.simIccid = (state.wizard.simIccid || '').trim();
                body.activationChannel = Number(state.wizard.activationChannel) || 0;
                const dc = (state.wizard.dealerCode || '').trim();
                if (dc) body.dealerCode = dc;
            } else if (k === 'suspension') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.suspensionType = (state.wizard.susSuspensionType || '').trim() || null;
                body.suspensionReason = (state.wizard.susSuspensionReason || '').trim() || null;
                body.barringLevel = (state.wizard.susBarringLevel || 'Full').trim();
                body.autoReconnectEnabled = !!state.wizard.susAutoReconnectEnabled;
                body.suspensionEndDateUtc =
                    state.wizard.susAutoReconnectEnabled && state.wizard.susEndDateLocal
                        ? new Date(state.wizard.susEndDateLocal).toISOString()
                        : null;
            } else if (k === 'reconnect') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.reconnectReason = (state.wizard.rcnReconnectReason || '').trim() || null;
                body.clearanceType = (state.wizard.rcnClearanceType || '').trim() || null;
                body.fraudClearanceConfirmed = !!state.wizard.rcnFraudClearanceConfirmed;
                body.paymentReference = (state.wizard.rcnPaymentReference || '').trim() || null;
            } else if (k === 'refund') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.refundType = (state.wizard.rfdRefundType || '').trim() || null;
                body.refundMethod = (state.wizard.rfdRefundMethod || '').trim() || null;
                body.refundReason = (state.wizard.rfdRefundReason || '').trim() || null;
                body.refundAmount = state.wizard.rfdRefundAmount ? Number(state.wizard.rfdRefundAmount) : null;
            } else if (k === 'badDebt') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.collectionAction = (state.wizard.bdrCollectionAction || '').trim() || null;
                body.dunningStage = (state.wizard.bdrDunningStage || '').trim() || null;
                body.collectedAmount = state.wizard.bdrCollectedAmount
                    ? Number(state.wizard.bdrCollectedAmount)
                    : null;
                body.writeOffAmount = state.wizard.bdrWriteOffAmount
                    ? Number(state.wizard.bdrWriteOffAmount)
                    : null;
                body.paymentReference = (state.wizard.bdrPaymentReference || '').trim() || null;
                body.agencyReference = (state.wizard.bdrAgencyReference || '').trim() || null;
                body.paymentPlanMonths = state.wizard.bdrPaymentPlanMonths
                    ? Number(state.wizard.bdrPaymentPlanMonths)
                    : null;
            } else if (k === 'deviceSale') {
                body.deviceInventoryId = state.wizard.devInventoryId || null;
                body.deviceSaleType = state.wizard.devSaleType === 'Installment' ? 1 : 0;
                if (state.wizard.devSaleType === 'Installment') {
                    body.deviceInstallmentPlanId = state.wizard.devInstallmentPlanId || null;
                }
            }
            return body;
        };

        const submitWizardRecordPayment = async () => {
            if (!state.wizard.createdOperationId) return;
            const ref = (state.wizard.paymentReference || '').trim();
            const amt = Number(state.wizard.paymentAmount);
            if (!ref || !(amt > 0)) {
                Swal.fire({ icon: 'warning', title: 'أدخل رقم مرجع الدفع والمبلغ' });
                return;
            }
            state.wizard.paymentBusy = true;
            try {
                const res = await AxiosManager.post('/Telecom/RecordSellingLinePayment', {
                    operationId: state.wizard.createdOperationId,
                    paymentReference: ref,
                    amountPaid: amt,
                    paymentChannel: Number(state.wizard.paymentChannel) || 0,
                    updatedById: StorageManager.getUserId(),
                });
                if (res?.data?.code === 200) {
                    state.wizard.paymentRecorded = true;
                    Swal.fire({ icon: 'success', title: 'تم تثبيت الدفع', timer: 1200, showConfirmButton: false });
                } else {
                    throw Object.assign(new Error(res?.data?.message || 'فشل'), { response: res });
                }
            } catch (e) {
                toastError(e, 'تعذّر تسجيل الدفع');
            } finally {
                state.wizard.paymentBusy = false;
            }
        };

        const submitWizardVasActivate = async () => {
            if (!validateWizardStep1()) return;
            const line = parseSelectedLine();
            const msisdn = (line?.msisdn || '').trim();
            const code = (state.wizard.selectedVasCode || '').trim();
            if (!msisdn || !code) return;
            state.wizard.submitBusy = true;
            state.isProvisioningInFlight = true;
            try {
                const res = await AxiosManager.post('/Vas/ToggleSubscriberVasService', {
                    msisdn,
                    serviceCode: code,
                    action: 0,
                    actorUserId: StorageManager.getUserId(),
                });
                if (res?.data?.code === 200) {
                    state.wizard.vasActivated = true;
                    state.wizard.createdOperationNumber = res?.data?.content?.operationNumber ?? '';
                    state.wizard.confirmStatusHint =
                        'تم تفعيل VAS بنجاح' +
                        (state.wizard.createdOperationNumber
                            ? ` (${state.wizard.createdOperationNumber})`
                            : '');
                    Swal.fire({
                        icon: 'success',
                        title: 'تم تفعيل الخدمة',
                        text: state.wizard.confirmStatusHint,
                        timer: 2500,
                        showConfirmButton: false,
                    });
                    state.wizard.step = 3;
                } else {
                    throw Object.assign(new Error(res?.data?.message || 'فشل'), { response: res });
                }
            } catch (e) {
                const errName = e?.response?.data?.error?.name;
                const msg =
                    e?.response?.data?.error?.message ??
                    e?.response?.data?.message ??
                    e?.message ??
                    '';
                Swal.fire({
                    icon: errName === 'BusinessRuleViolationException' ? 'warning' : 'error',
                    title: errName === 'BusinessRuleViolationException' ? 'VAL-11' : 'تعذّر التفعيل',
                    text: msg || 'تعذّر تفعيل الخدمة — تحقق من الأهلية (VAL-11)',
                });
            } finally {
                state.wizard.submitBusy = false;
                state.isProvisioningInFlight = false;
            }
        };

        const wizardStepNext = () => {
            if (state.wizard.step === 1) {
                if (!validateWizardStep1()) return;
                if (state.wizard.kind === 'addpackage') {
                    submitWizardVasActivate();
                    return;
                }
                state.wizard.step = 2;
                return;
            }
            if (state.wizard.step === 2) {
                if (!canWizardFinishStep2.value) {
                    Swal.fire({
                        icon: 'info',
                        title: 'أكمل التسجيل والوثيقة وتأكيد CBS',
                    });
                    return;
                }
                state.wizard.step = 3;
            }
        };

        const wizardStepPrev = () => {
            if (state.wizard.step > 1) state.wizard.step -= 1;
        };

        const submitWizardCreateDraft = async () => {
            if (!validateWizardStep1()) return;
            if (state.wizard.kind === 'changeNumber' && state.wizard.cnTargetMsisdnAssetId) {
                try {
                    await AxiosManager.post('/Telecom/ReserveMsisdnForCustomer', {
                        msisdnAssetId: state.wizard.cnTargetMsisdnAssetId,
                        customerId: state.customerId,
                        reservedByUserId: StorageManager.getUserId(),
                    });
                } catch (e) {
                    toastError(e, 'تعذّر حجز الرقم الجديد');
                    return;
                }
            }
            if (state.wizard.kind === 'activate') {
                try {
                    await AxiosManager.post('/Telecom/ReserveMsisdnForCustomer', {
                        msisdnAssetId: state.wizard.msisdnAssetId,
                        customerId: state.customerId,
                        reservedByUserId: StorageManager.getUserId(),
                    });
                } catch (e) {
                    toastError(e, 'تعذّر حجز الرقم');
                    return;
                }
            }
            state.wizard.submitBusy = true;
            state.isProvisioningInFlight = true;
            try {
                const res = await AxiosManager.post('/Telecom/CreateTelecomOperation', buildWizardCreateBody());
                const ok = res?.data?.code === 200;
                const entity = res?.data?.content?.data;
                if (ok && entity?.id) {
                    state.wizard.createdOperationId = entity.id;
                    state.wizard.createdOperationNumber = entity.number || '';
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
                    if (state.wizard.kind === 'refund') {
                        state.wizard.rfdRequiresBackOffice =
                            String(entity.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                            || !!entity.requiresDualApproval;
                        state.wizard.rfdDepositSnapshot =
                            entity.depositBalanceSnapshot ?? entity.DepositBalanceSnapshot ?? null;
                        state.wizard.rfdWalletSnapshot =
                            entity.walletBalanceSnapshot ?? entity.WalletBalanceSnapshot ?? null;
                    }
                    if (state.wizard.kind === 'badDebt') {
                        state.wizard.bdrRequiresBackOffice =
                            String(entity.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                            || state.wizard.bdrRequiresBackOffice;
                        if (state.wizard.bdrRequiresBackOffice) {
                            state.wizard.documentMarkedUploaded = true;
                        }
                    }
                    if (state.wizard.kind === 'deviceSale') {
                        state.wizard.devRequiresFinance =
                            !!entity.deviceApprovalLevelRequired || !!entity.approvalLevelRequired;
                        state.wizard.devFinancingPreview = entity.deviceFinancingNoteAr || entity.notes || '';
                        state.wizard.devDownPayment = String(
                            entity.deviceDownPaymentAmount ?? state.wizard.devDownPayment ?? ''
                        );
                        state.wizard.documentMarkedUploaded = state.wizard.devSaleType === 'Cash';
                    }
                    Swal.fire({
                        icon: 'success',
                        title: 'تم إنشاء المسودة',
                        html:
                            `<p class="mb-1">${state.wizard.createdOperationNumber}</p>` +
                            '<p class="small text-muted mb-0">ظهرت العملية في طابور قمرة العمليات الخلفية للمعالجة.</p>',
                        timer: 2800,
                        showConfirmButton: false,
                    });
                } else {
                    throw Object.assign(new Error(res?.data?.message || 'فشل إنشاء العملية'), { response: res });
                }
            } catch (e) {
                toastError(e, 'تعذّر إنشاء المسودة');
            } finally {
                state.wizard.submitBusy = false;
                state.isProvisioningInFlight = false;
            }
        };

        const submitWizardMarkDocument = async () => {
            if (!state.wizard.createdOperationId) return;
            if (state.wizard.kind === 'takeover' && !state.wizard.identityFile) {
                Swal.fire({ icon: 'warning', title: 'ارفع هوية المالك الجديد' });
                return;
            }
            if (state.wizard.kind === 'simswap' && state.wizard.simLostOrStolen && !state.wizard.identityFile) {
                Swal.fire({ icon: 'warning', title: 'ارفع إقرار / هوية المشترك' });
                return;
            }
            if (state.wizard.kind === 'changeNumber' && state.wizard.cnRequiresBackOffice && !state.wizard.identityFile) {
                Swal.fire({ icon: 'warning', title: 'ارفع إيصال الدفع أو موافقة المشرف' });
                return;
            }
            if (state.wizard.kind === 'termination' && state.wizard.trmRequiresBackOffice && !state.wizard.identityFile) {
                Swal.fire({ icon: 'warning', title: 'ارفع الوثيقة / القرار الإداري' });
                return;
            }
            if (state.wizard.kind === 'refund' && state.wizard.rfdRequiresBackOffice && !state.wizard.identityFile) {
                Swal.fire({ icon: 'warning', title: 'ارفع وثيقة الاعتماد' });
                return;
            }
            state.wizard.uploadBusy = true;
            try {
                const uid = StorageManager.getUserId();
                let res;
                if (
                    (state.wizard.kind === 'takeover'
                        || (state.wizard.kind === 'simswap' && state.wizard.simLostOrStolen)
                        || (state.wizard.kind === 'changeNumber' && state.wizard.cnRequiresBackOffice)
                        || (state.wizard.kind === 'termination' && state.wizard.trmRequiresBackOffice)
                        || (state.wizard.kind === 'refund' && state.wizard.rfdRequiresBackOffice))
                    && state.wizard.identityFile
                ) {
                    const form = new FormData();
                    form.append('id', state.wizard.createdOperationId);
                    form.append('updatedById', uid || '');
                    form.append('file', state.wizard.identityFile);
                    res = await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                } else {
                    res = await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', {
                        id: state.wizard.createdOperationId,
                        updatedById: uid,
                    });
                }
                if (res?.data?.code === 200) {
                    state.wizard.documentMarkedUploaded = true;
                    const title =
                        state.wizard.kind === 'takeover'
                        || (state.wizard.kind === 'simswap' && state.wizard.simLostOrStolen)
                        || (state.wizard.kind === 'changeNumber' && state.wizard.cnRequiresBackOffice)
                        || (state.wizard.kind === 'termination' && state.wizard.trmRequiresBackOffice)
                        || (state.wizard.kind === 'refund' && state.wizard.rfdRequiresBackOffice)
                            ? 'تم الإرسال للباك أوفيس'
                            : 'تم تسجيل الوثيقة';
                    Swal.fire({ icon: 'success', title, timer: 1400, showConfirmButton: false });
                } else {
                    throw Object.assign(new Error(res?.data?.message || 'فشل'), { response: res });
                }
            } catch (e) {
                toastError(e, 'تعذّر تسجيل الوثيقة');
            } finally {
                state.wizard.uploadBusy = false;
            }
        };

        const submitWizardConfirmCbs = async () => {
            if (!state.wizard.createdOperationId) return;
            state.wizard.confirmBusy = true;
            state.isProvisioningInFlight = true;
            try {
                const res = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                    id: state.wizard.createdOperationId,
                    updatedById: StorageManager.getUserId(),
                });
                if (res?.data?.code === 200) {
                    state.wizard.confirmed = true;
                    const content = res?.data?.content ?? res?.data?.Content ?? {};
                    const hint =
                        content.statusHintAr ??
                        content.StatusHintAr ??
                        PROV_SUCCESS_DEFAULT;
                    state.wizard.confirmStatusHint = hint;
                    toastSuccess(hint);
                    if (content.hlrCompletesAsynchronously ?? content.HlrCompletesAsynchronously) {
                        const polled = await pollOperationAfterConfirm(state.wizard.createdOperationId);
                        if (polled) {
                            state.wizard.confirmStatusHint = polled;
                            toastSuccess(polled);
                        }
                    }
                } else {
                    throw Object.assign(new Error(res?.data?.message || 'فشل التأكيد'), { response: res });
                }
            } catch (e) {
                toastError(e, 'تعذّر تأكيد CBS');
            } finally {
                state.wizard.confirmBusy = false;
                state.isProvisioningInFlight = false;
            }
        };

        Vue.onMounted(async () => {
            try {
                if (!StorageManager.getAccessToken?.()) {
                    window.location.href = '/Accounts/Login';
                    return;
                }
                if (typeof PortalNavigation !== 'undefined' && PortalNavigation.syncOperatorSession) {
                    await PortalNavigation.syncOperatorSession(false);
                }
                const ok = await SecurityManager.authorizeTelecomAccess({
                    roles: ['TelecomAdmin', 'TelecomManagement', 'TelecomBackOffice', 'TelecomCallCenter', 'TelecomShowroom'],
                    permissions: ['customer.view'],
                });
                if (!ok) {
                    SecurityManager.denyPageAccess?.();
                    return;
                }
                const params = new URLSearchParams(window.location.search);
                state.customerId = params.get('customerId') || '';
                await loadProfile();
                const deepWizard = (params.get('wizard') || '').trim();
                const deepLineKey = (params.get('lineKey') || '').trim();
                if (deepLineKey) {
                    state.prov.selectedLineKey = deepLineKey;
                }
                if (deepWizard) {
                    await openProvisioningWizard(deepWizard);
                }
            } catch (e) {
                console.error('Customer360Profile init:', e);
                state.loadError = 'غير مصرّح أو انتهت الجلسة.';
            } finally {
                if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
            }
        });

        return {
            state,
            tabs: TABS,
            can,
            selectedLineTerminated,
            selectedLineSuspended,
            selectedLineActive,
            selectedLineCollectionEligible,
            selectedLineOutstandingBalance,
            onTerminationTypeChange,
            onSusTypeChange,
            onRcnClearanceChange,
            onBdrActionChange,
            onRefundTypeChange,
            loadReconnectEligibility,
            loadBadDebtEligibility,
            onDevicePicked,
            recordDeviceDownPaymentC360,
            canRecharge,
            formatDt,
            formatDateOnly,
            formatAmount,
            lineWallet,
            bucketLabel,
            openRechargeModal,
            coreField,
            isIndividual,
            isCorporate,
            statusBadgeClass,
            allSubscriptions,
            operationalStatusLabel,
            checkHlr,
            loadProfile,
            openAiWizard,
            simulateAiCall,
            forceCbs,
            hlrResync,
            escalateTicket,
            isTicketBusy,
            mappedTickets,
            activeVasCount,
            vasSummary,
            groupedVasByLine,
            vasStatusLabel,
            activeLinesCount,
            customerKindLabel,
            customerStatusLabel,
            operationalBadgeClass,
            vasBadgeClass,
            lineOptions,
            wizardTitle,
            canWizardFinishStep2,
            activateRequiredDeposit,
            openProvisioningWizard,
            closeProvisioningWizard,
            finishProvisioningWizard,
            onWizardLineChanged,
            onCgtTargetTypeChanged,
            onChangeNumberTargetPicked,
            searchTakeoverTarget,
            selectTakeoverTarget,
            wizardStepNext,
            wizardStepPrev,
            submitWizardCreateDraft,
            submitWizardMarkDocument,
            submitWizardRecordPayment,
            submitWizardConfirmCbs,
            onWizardIdentityFileChange,
            onActivateMsisdnChanged,
        };
    },
};

Vue.createApp(Customer360ProfileApp).mount('#app');
