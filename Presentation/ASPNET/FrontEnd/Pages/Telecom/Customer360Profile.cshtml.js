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

function formatMoneyOffer(n, lang) {
    if (n == null || isNaN(n)) return '—';
    try {
        return new Intl.NumberFormat(lang === 'ar' ? 'ar-SY' : 'en-US', { maximumFractionDigits: 0 }).format(n);
    } catch {
        return String(n);
    }
}

const telecomT = (key, fallback) => {
    try {
        if (window.TelecomI18n?.resolve) {
            return window.TelecomI18n.resolve(key, fallback, ['customer360Profile']);
        }
        const raw = String(key);
        const paths = [];
        if (!raw.includes('.')) {
            paths.push(`customer360Profile.${raw}`, raw);
        } else if (raw.startsWith('customer360Profile.')) {
            paths.push(raw);
        } else {
            paths.push(raw, `customer360Profile.${raw}`, `customerList.${raw}`);
        }
        for (const k of paths) {
            const hit = window.TelecomI18n?.t?.(k);
            if (hit) return hit;
        }
        return fallback;
    } catch {
        return fallback;
    }
};

const ticketEnumLabel = (group, code) =>
    telecomT(`ticketEnums.${group}.${code}`, '') || '—';

const TAB_DEFS = [
    { id: 'profile', key: 'tabs.profile', icon: 'bi bi-person-vcard' },
    { id: 'services', key: 'tabs.services', icon: 'bi bi-reception-4' },
    { id: 'timeline', key: 'tabs.timeline', icon: 'bi bi-clock-history' },
    { id: 'tickets', key: 'tabs.tickets', icon: 'bi bi-ticket-detailed' },
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

const isPremiumMsisdnCategory = (cat) => [1, 2, 3, 'Silver', 'Gold', 'Platinum'].includes(cat);

const contentLocale = () => {
    const fromI18n = window.TelecomI18n?.getLang?.();
    if (fromI18n) return fromI18n;
    const lang = (document.documentElement.lang || 'en').toLowerCase();
    return lang.startsWith('en') ? 'en' : 'ar';
};

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

const RESOLVED = 2;
const ESCALATED = 3;

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
        const loc = contentLocale() === 'en' ? 'en-US' : 'ar-SY';
        return new Date(utc).toLocaleString(loc, { dateStyle: 'short', timeStyle: 'short' });
    } catch {
        return String(utc);
    }
};

const formatDateOnly = (val) => {
    if (!val) return '—';
    try {
        const loc = contentLocale() === 'en' ? 'en-US' : 'ar-SY';
        return new Date(val).toLocaleDateString(loc, { dateStyle: 'medium' });
    } catch {
        return String(val);
    }
};

const Customer360ProfileApp = {
    setup() {
        const localeTick = Vue.ref(0);

        const t360 = (key, fallback = '') => {
            localeTick.value;
            return telecomT(key, fallback);
        };

        const uiLang = () => contentLocale();

        const ui = Vue.computed(() => {
            localeTick.value;
            const f = (k, fb = '') => telecomT(`fields.${k}`, fb);
            const s = (k, fb = '') => telecomT(`sections.${k}`, fb);
            const h = (k, fb = '') => telecomT(`hero.${k}`, fb);
            return {
                pageTitle: telecomT('pageTitle', 'Subscriber 360'),
                heroEyebrow: telecomT('heroEyebrow', 'Customer 360'),
                hero: {
                    activeLines: h('activeLines', '{count} lines'),
                    tickets: h('tickets', 'Tickets'),
                    vas: h('vas', 'VAS'),
                    aiAgent: h('aiAgent', 'AI agent'),
                    search: h('search', 'Search'),
                    crmRegistry: h('crmRegistry', 'CRM'),
                    opsHub: h('opsHub', 'Hub'),
                },
                tabs: {
                    profile: telecomT('tabs.profile', 'Profile'),
                    services: telecomT('tabs.services', 'Services'),
                    tickets: telecomT('tabs.tickets', 'Tickets'),
                    timeline: telecomT('tabs.timeline', 'Timeline'),
                },
                timeline: {
                    noEvents: telecomT('timeline.noEvents', 'No events yet.'),
                    open: telecomT('timeline.open', 'Open'),
                    loadMore: telecomT('timeline.loadMore', 'Load more'),
                },
                sections: {
                    fullInfo: s('fullInfo'),
                    identity: s('identity'),
                    individual: s('individual'),
                    corporate: s('corporate'),
                    contact: s('contact'),
                    address: s('address'),
                    contactsTable: s('contactsTable'),
                    audit: s('audit'),
                    allSubscriptions: s('allSubscriptions'),
                    liveAssets: s('liveAssets'),
                    operationsPipeline: s('operationsPipeline'),
                    supportTickets: s('supportTickets'),
                    vasByLine: s('vasByLine'),
                },
                actions: {
                    provisioningHub: telecomT('actions.provisioningHub', 'Provisioning hub'),
                    techSupport: telecomT('actions.techSupport', 'Support'),
                    techSupportSub: telecomT('actions.techSupportSub', 'AI'),
                    addPackage: telecomT('actions.addPackage', 'Add package / VAS'),
                    changeGsm: telecomT('actions.changeGsm', 'Change line type (CGT)'),
                    simSwap: telecomT('lineActions.simSwap', 'SIM swap'),
                    takeOver: telecomT('lineActions.takeOver', 'Transfer ownership'),
                    migrate: telecomT('lineActions.migrate', 'Migrate package'),
                    changeNumber: telecomT('lineActions.changeNumber', 'Change MSISDN (CNR)'),
                    terminate: telecomT('lineActions.terminate', 'Terminate line (TRM)'),
                    suspension: telecomT('lineActions.suspension', 'Temporary suspension (SUS)'),
                    reconnect: telecomT('lineActions.reconnect', 'Reconnect (RCN)'),
                    collection: telecomT('lineActions.collection', 'Collections (BDR)'),
                    refund: telecomT('lineActions.refund', 'Refund (RFD)'),
                    deviceSale: telecomT('lineActions.deviceSale', 'Device sale (DEV)'),
                    newLine: telecomT('lineActions.newLine', 'Activate new line'),
                    inFlight: telecomT('actions.inFlight', 'Processing…'),
                },
                grid: {
                    msisdn: telecomT('grid.msisdn', 'MSISDN'),
                    product: telecomT('grid.product', 'Plan'),
                    subscriptionType: telecomT('grid.subscriptionType', 'Type'),
                    status: telecomT('grid.status', 'Status'),
                    lineType: telecomT('grid.lineType', 'Line type'),
                    iccid: telecomT('grid.iccid', 'ICCID'),
                    imsi: telecomT('grid.imsi', 'IMSI'),
                    simType: telecomT('grid.simType', 'SIM type'),
                    simStatus: telecomT('grid.simStatus', 'SIM status'),
                    documents: telecomT('grid.documents', 'Documents'),
                    balanceLimit: telecomT('grid.balanceLimit', 'Balance'),
                    loyalty: telecomT('grid.loyalty', 'Loyalty'),
                    activation: telecomT('grid.activation', 'Activation'),
                    hlr: telecomT('grid.hlr', 'HLR'),
                },
                profileTab: {
                    legal: telecomT('profileTab.legal', 'Legal'),
                    billing: telecomT('profileTab.billing', 'Billing'),
                    noMsisdn: telecomT('profileTab.noMsisdn', ''),
                    billingLog: telecomT('profileTab.billingLog', 'Log'),
                    operation: telecomT('profileTab.operation', 'Op'),
                    result: telecomT('profileTab.result', 'Result'),
                    time: telecomT('profileTab.time', 'Time'),
                    noLogs: telecomT('profileTab.noLogs', 'Empty'),
                    logOk: telecomT('profileTab.logOk', 'OK'),
                    logRetry: telecomT('profileTab.logRetry', 'Retry'),
                },
                servicesTab: {
                    activePlans: telecomT('servicesTab.activePlans', 'Plans'),
                    noSubs: telecomT('servicesTab.noSubs', ''),
                    currentBalance: telecomT('servicesTab.currentBalance', 'Balance'),
                    walletLoadFailed: telecomT('servicesTab.walletLoadFailed', ''),
                    quotaTitle: telecomT('servicesTab.quotaTitle', 'Quotas'),
                    noBuckets: telecomT('servicesTab.noBuckets', ''),
                    noMsisdnForWallet: telecomT('servicesTab.noMsisdnForWallet', ''),
                    unlimited: telecomT('servicesTab.unlimited', 'Unlimited'),
                    vasTitle: telecomT('servicesTab.vasTitle', 'VAS'),
                    activateService: telecomT('servicesTab.activateService', 'Activate'),
                    noVas: telecomT('servicesTab.noVas', ''),
                    vasTotal: telecomT('servicesTab.vasTotal', 'Total'),
                    vasActive: telecomT('servicesTab.vasActive', 'Active'),
                    vasSuspended: telecomT('servicesTab.vasSuspended', 'Suspended'),
                    vasLines: telecomT('servicesTab.vasLines', 'Lines'),
                    vasPerLine: telecomT('servicesTab.vasPerLine', 'services'),
                    vasActiveBadge: telecomT('servicesTab.vasActiveBadge', 'active'),
                    service: telecomT('servicesTab.service', 'Service'),
                    status: telecomT('servicesTab.status', 'Status'),
                    activatedAt: telecomT('servicesTab.activatedAt', 'Activated'),
                    deactivatedAt: telecomT('servicesTab.deactivatedAt', 'Deactivated'),
                },
                ticketsTab: {
                    title: telecomT('ticketsTab.title', 'Tickets'),
                    archive: telecomT('ticketsTab.archive', 'Archive'),
                    empty: telecomT('ticketsTab.empty', ''),
                    forceCbs: telecomT('ticketsTab.forceCbs', 'CBS'),
                    hlrSync: telecomT('ticketsTab.hlrSync', 'HLR'),
                    escalate: telecomT('ticketsTab.escalate', 'Escalate'),
                    escalatedFollowUp: telecomT(
                        'ticketsTab.escalatedFollowUp',
                        'Escalated to Tier-3 — awaiting engineering. Track here and update the customer.'
                    ),
                    closed: telecomT('ticketsTab.closed', ''),
                    noRbac: telecomT('ticketsTab.noRbac', ''),
                },
                ai: {
                    title: telecomT('ai.title', 'AI'),
                    hint: telecomT('ai.hint', ''),
                    transcript: telecomT('ai.transcript', 'Transcript'),
                    transcriptPh: telecomT('ai.transcriptPh', ''),
                    cancel: telecomT('ai.cancel', 'Cancel'),
                    run: telecomT('ai.run', 'Run'),
                },
                wizardUi: {
                    closeAria: telecomT('wizardUi.closeAria', 'Close'),
                    stepData: telecomT('wizardUi.stepData', 'Data'),
                    stepReg: telecomT('wizardUi.stepReg', 'Reg'),
                    stepDone: telecomT('wizardUi.stepDone', 'Done'),
                    linkedLine: telecomT('wizardUi.linkedLine', 'Line:'),
                    pickSubscription: telecomT('wizardUi.pickSubscription', 'Line'),
                    notes: telecomT('wizardUi.notes', 'Notes'),
                    step2Help: telecomT('wizardUi.step2Help', ''),
                    createDraft: telecomT('wizardUi.createDraft', 'Draft'),
                    opNumber: telecomT('wizardUi.opNumber', '#:'),
                    markDocument: telecomT('wizardUi.markDocument', 'Document'),
                    documentDone: telecomT('wizardUi.documentDone', 'Done'),
                    paymentBlock: telecomT('wizardUi.paymentBlock', 'Payment'),
                    amountPh: telecomT('wizardUi.amountPh', 'Amount'),
                    paymentRefPh: telecomT('wizardUi.paymentRefPh', 'Ref'),
                    recordPayment: telecomT('wizardUi.recordPayment', 'Pay'),
                    fetchFromCashier: telecomT('wizardUi.fetchFromCashier', 'Fetch from cashier'),
                    cashierFetched: telecomT('wizardUi.cashierFetched', 'Loaded from cashier'),
                    paymentDone: telecomT('wizardUi.paymentDone', 'Paid'),
                    confirmCbs: telecomT('wizardUi.confirmCbs', 'CBS'),
                    awaitBo: telecomT('wizardUi.awaitBo', 'BO'),
                    confirmed: telecomT('wizardUi.confirmed', 'OK'),
                    scheduledConfirmed: telecomT('wizardUi.scheduledConfirmed', 'Scheduled'),
                    doneTitle: telecomT('wizardUi.doneTitle', 'Done'),
                    doneHint: telecomT('wizardUi.doneHint', ''),
                    prev: telecomT('wizardUi.prev', 'Back'),
                    nextReg: telecomT('wizardUi.nextReg', 'Next'),
                    nextVas: telecomT('wizardUi.nextVas', 'VAS'),
                    nextSummary: telecomT('wizardUi.nextSummary', 'Summary'),
                    close: telecomT('wizardUi.close', 'Close'),
                    additionalLine: telecomT('wizardUi.additionalLine', ''),
                    availableMsisdn: telecomT('wizardUi.availableMsisdn', 'MSISDN'),
                    imsiFromStock: telecomT('wizardUi.imsiFromStock', 'IMSI'),
                    package: telecomT('wizardUi.package', 'Package'),
                    iccid: telecomT('wizardUi.iccid', 'ICCID'),
                    activationChannel: telecomT('wizardUi.activationChannel', 'Channel'),
                    channelShowroom: telecomT('wizardUi.channelShowroom', 'POS'),
                    channelShowroomLockedHint: telecomT('wizardUi.channelShowroomLockedHint', ''),
                    channelDealer: telecomT('wizardUi.channelDealer', 'Dealer'),
                    channelDigital: telecomT('wizardUi.channelDigital', 'Digital'),
                    dealerCode: telecomT('wizardUi.dealerCode', 'Dealer'),
                    searchNewOwner: telecomT('wizardUi.searchNewOwner', 'Search'),
                    newOwnerId: telecomT('wizardUi.newOwnerId', 'ID'),
                    pointsUnit: telecomT('wizardUi.pointsUnit', 'pts'),
                },
                subscriptionBadge: telecomT('subscriptionBadge', '{count}'),
                loyaltyPoints: telecomT('loyaltyPoints', 'pts'),
                balancePrefix: telecomT('balancePrefix', 'Balance:'),
                limitPrefix: telecomT('limitPrefix', 'Limit:'),
                fields: {
                    displayName: f('displayName'),
                    accountNumber: f('accountNumber'),
                    customerKind: f('customerKind'),
                    status: f('status'),
                    statusReason: f('statusReason'),
                    statusNote: f('statusNote'),
                    group: f('group'),
                    category: f('category'),
                    description: f('description'),
                    nationalId: f('nationalId'),
                    dob: f('dob'),
                    nationality: f('nationality'),
                    gender: f('gender'),
                    occupation: f('occupation'),
                    commercialRegistry: f('commercialRegistry'),
                    taxNumber: f('taxNumber'),
                    authorizedSignatory: f('authorizedSignatory'),
                    legalStatus: f('legalStatus'),
                    billingMode: f('billingMode'),
                    parentCompany: f('parentCompany'),
                    primaryPhone: f('primaryPhone'),
                    email: f('email'),
                    fax: f('fax'),
                    website: f('website'),
                    whatsApp: f('whatsApp'),
                    street: f('street'),
                    city: f('city'),
                    state: f('state'),
                    zip: f('zip'),
                    country: f('country'),
                    contactName: f('contactName'),
                    contactTitle: f('contactTitle'),
                    contactPhone: f('contactPhone'),
                    contactEmail: f('contactEmail'),
                    contactNotes: f('contactNotes'),
                    linkedIn: f('linkedIn'),
                    facebook: f('facebook'),
                    instagram: f('instagram'),
                    twitterX: f('twitterX'),
                    tikTok: f('tikTok'),
                    createdAt: f('createdAt'),
                    updatedAt: f('updatedAt'),
                    customerId: f('customerId'),
                    viewDocument: f('viewDocument'),
                },
            };
        });

        const tabs = Vue.computed(() => {
            localeTick.value;
            return TAB_DEFS.map((t) => ({
                id: t.id,
                icon: t.icon,
                label: telecomT(t.key, t.id),
            }));
        });

        const state = Vue.reactive({
            customerId: '',
            profile: null,
            loading: true,
            loadError: null,
            activeTab: 'profile',
            timelineKindFilter: '',
            timelineSkip: 0,
            timelineHasMore: false,
            timelineLoading: false,
            cbsBusy: false,
            cbsData: null,
            lineWallets: {},
            lineWalletsBusy: {},
            telecomLineTypes: [],
            telecomLineTypesBusy: false,
            rechargeBusy: '',
            hlrBusy: '',
            hlrBySub: {},
            hlrRemediationBusy: '',
            payAndReconnectBusy: '',
            lastHlrLog: '',
            aiSim: { msisdn: '', transcript: '', busy: false },
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
                bssPaymentReference: '',
                bssSecurityTicketId: '',
                bssDocumentNumber: '',
                bssRegulatoryFile: null,
                bssIdentityFile: null,
                bssKycDocumentReferenceId: '',
                bssOriginalTransactionRef: '',
                bssPayoutDestination: '',
                cgtMigrationPath: 'Standard',
                cgtSourceTypeCode: '',
                cgtSourceTypeId: '',
                susAutoReconnectEnabled: false,
                susEndDateLocal: '',
                susEndDateValidationError: '',
                susRequiresBackOffice: false,
                rcnReconnectReason: '',
                rcnClearanceType: 'Customer',
                rcnPaymentReference: '',
                rcnSecurityTicketId: '',
                rcnDocumentNumber: '',
                rcnRegulatoryFile: null,
                rcnKycDocumentReferenceId: '',
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
                bdrSupervisorConfirmed: false,
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
                offerDetail: null,
                offerDetailBusy: false,
                vasCatalog: [],
                catalogBusy: false,
                selectedVasCode: '',
                vasAction: 'Activate',
                vasActivated: false,
                simIccid: '',
                simReplacementReason: '',
                simLostOrStolen: false,
                cnTargetMsisdnAssetId: '',
                cnNumberChangeReason: '',
                cnPremiumFeeAmount: '',
                cnChangeMode: 'Internal',
                cnPortInMsisdn: '',
                cnDonorOperatorCode: '',
                cnPortInReference: '',
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
                confirmScheduled: false,
                notes: '',
                createdOperationId: '',
                createdOperationNumber: '',
                documentMarkedUploaded: false,
                confirmed: false,
                submitBusy: false,
                uploadBusy: false,
                confirmBusy: false,
                kycDocumentReferenceId: '',
                activateIdentityFile: null,
                kycUploadBusy: false,
                kycUploadError: '',
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
            supportTicket: StorageManager.hasAnyPermission(perms.value, [PERM.escalate, 'customer.view']),
        }));

        const HUB_NAV_PERMS = [
            'telecom.hub.frontline',
            'telecom.hub.backoffice',
            'telecom.hub.supervisor',
            'telecom.line.activate',
            'telecom.asset.manage',
            'admin.settings.manage',
        ];

        const isSupportAgent = Vue.computed(() => {
            const p = perms.value;
            const hasSupport =
                StorageManager.hasAnyPermission(p, ['customer.view', PERM.escalate]);
            const hasOps = StorageManager.hasAnyPermission(p, HUB_NAV_PERMS);
            return hasSupport && !hasOps;
        });

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
            productOfferingNameEn: s.productOfferingNameEn ?? s.ProductOfferingNameEn,
            subscriptionTypeName: s.subscriptionTypeName ?? s.SubscriptionTypeName,
            subscriptionTypeNameEn: s.subscriptionTypeNameEn ?? s.SubscriptionTypeNameEn,
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
            documentOperationId: s.documentOperationId ?? s.DocumentOperationId ?? '',
            documentSource: s.documentSource ?? s.DocumentSource ?? '',
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
                label: `${s.msisdn || '—'} · ${productOfferingDisplayName(s) || t360('lineDefault', 'Line')}`,
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

        const selectedLineBdrStatus = Vue.computed(() => {
            const sub = selectedSubscription.value;
            if (!sub) return null;
            const ops = state.profile?.telecomOperations || [];
            const bdr = ops.find(o => o.kind === 'BadDebtRecovery' && (o.msisdnAssetId === sub.msisdnAssetId || o.msisdn === sub.msisdn));
            return bdr ? bdr.status : null;
        });

        const selectedLineBdrPending = Vue.computed(() => {
            const s = selectedLineBdrStatus.value;
            return s === 'Draft' || s === 'PendingDocuments' || s === 'Confirmed' || s === 'Provisioning';
        });

        const selectedLineBdrApproved = Vue.computed(() => {
            return selectedLineBdrStatus.value === 'Approved_Pending_Cash';
        });

        const selectedLineBdrPaidPending = Vue.computed(() => {
            const s = selectedLineBdrStatus.value;
            return s === 'Paid_Pending_BackOffice_Clearance';
        });

        const selectedLineBdrAwaitingAudit = Vue.computed(() => {
            return selectedLineBdrPaidPending.value;
        });

        const rcnShowsPaymentRef = Vue.computed(() => {
            const rcn = typeof TelecomReconnectClearance !== 'undefined' ? TelecomReconnectClearance : null;
            return rcn
                ? rcn.showsPaymentReference(state.wizard.rcnClearanceType, { bdrApproved: selectedLineBdrApproved.value })
                : state.wizard.rcnClearanceType === 'Payment' || selectedLineBdrApproved.value;
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

        const lineKey = (sub) =>
            `${sub.subscriberProfileId}|${sub.msisdn}|${sub.msisdnAssetId}`;

        const ensureLineSelected = () => {
            if (!state.prov.selectedLineKey && lineOptions.value.length) {
                state.prov.selectedLineKey = lineOptions.value[0].key;
            }
        };

        const isRowSelected = (sub) => state.prov.selectedLineKey === lineKey(sub);

        const normalizeHlrState = (raw) => String(raw || '').trim().toUpperCase();

        const isHlrRemediationState = (hlrState) => {
            const s = normalizeHlrState(hlrState);
            return ['INACTIVE', 'NOT_PROVISIONED', 'SUSPENDED', 'ERROR'].includes(s);
        };

        const hlrDataForSub = (sub) => state.hlrBySub[sub?.id] || null;

        const isRevenueLeakageDesync = (crmStatus, hlrState) => {
            const crm = String(crmStatus || '').trim().toLowerCase();
            return crm.includes('suspended') && normalizeHlrState(hlrState) === 'ACTIVE';
        };

        const isBillingSuspensionAligned = (crmStatus, hlrState) => {
            const crm = String(crmStatus || '').trim().toLowerCase();
            const hlr = normalizeHlrState(hlrState);
            return crm.includes('suspended') && (hlr === 'SUSPENDED' || hlr === 'INACTIVE');
        };

        const isHealthyActiveLine = (crmStatus, hlrState) => {
            const crm = String(crmStatus || '').trim().toLowerCase();
            return (crm === 'active' || crm === '1') && normalizeHlrState(hlrState) === 'ACTIVE';
        };

        const hlrNeedsRemediation = (sub) => {
            const data = hlrDataForSub(sub);
            if (!data) return false;
            const liveCrm = sub?.profileOperationalStatus ?? data.crmOperationalStatus;
            const hlr = data.hlrSubscriberState;
            if (isHealthyActiveLine(liveCrm, hlr)) return false;
            if (isBillingSuspensionAligned(liveCrm, hlr)) return false;
            if (isRevenueLeakageDesync(liveCrm, hlr)) return true;
            if (data.differsFromCrm) return true;
            return isHlrRemediationState(hlr);
        };

        const selectedHlrNeedsRemediation = Vue.computed(() => {
            const sub = selectedSubscription.value;
            return sub ? hlrNeedsRemediation(sub) : false;
        });

        const selectedHlrSnapshot = Vue.computed(() => {
            const sub = selectedSubscription.value;
            if (!sub) return null;
            return hlrDataForSub(sub);
        });

        const hlrStatusBtnClass = (sub) => {
            const data = hlrDataForSub(sub);
            if (!data) return 'btn-outline-secondary';
            const liveCrm = sub?.profileOperationalStatus ?? data.crmOperationalStatus;
            const hlr = data.hlrSubscriberState;
            if (isHealthyActiveLine(liveCrm, hlr)) return 'btn-outline-success';
            if (isBillingSuspensionAligned(liveCrm, hlr)) return 'btn-outline-success';
            if (isRevenueLeakageDesync(liveCrm, hlr) || data.differsFromCrm) return 'btn-outline-danger';
            const s = normalizeHlrState(hlr);
            if (s === 'ACTIVE') return 'btn-outline-success';
            if (isHlrRemediationState(s)) return 'btn-outline-danger';
            return 'btn-outline-warning';
        };

        const selectedHlrRemediationCrmStatus = Vue.computed(() => {
            const sub = selectedSubscription.value;
            const snap = selectedHlrSnapshot.value;
            return sub?.profileOperationalStatus ?? snap?.crmOperationalStatus ?? '';
        });

        const hlrRemediationCrmLabel = Vue.computed(() => {
            const st = selectedHlrRemediationCrmStatus.value;
            return `CRM: ${operationalStatusLabel(st)}`;
        });

        const hlrRemediationCrmBadgeClass = Vue.computed(() =>
            operationalBadgeClass(selectedHlrRemediationCrmStatus.value)
        );

        const hlrRemediationHlrBadgeClass = Vue.computed(() => {
            const s = normalizeHlrState(selectedHlrSnapshot.value?.hlrSubscriberState);
            if (s === 'ACTIVE') return 'bg-success';
            if (s === 'SUSPENDED') return 'bg-warning text-dark';
            return 'bg-danger';
        });

        const hlrRemediationSubtitle = Vue.computed(() => {
            const snap = selectedHlrSnapshot.value;
            if (!snap) return '';
            const crm = operationalStatusLabel(selectedHlrRemediationCrmStatus.value);
            const hlr = snap.hlrSubscriberState || '—';
            const tpl = t360(
                'hlrRemediation.subtitlePattern',
                'CRM shows {crm} but HLR reports {hlr} — choose a remediation action.'
            );
            return tpl.replace('{crm}', crm).replace('{hlr}', hlr);
        });

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
            localeTick.value;
            const raw = core.customerKind ?? core.CustomerKind;
            const s = String(raw ?? '').toLowerCase();
            if (s === 'corporate' || raw === 1 || raw === '1')
                return t360('enums.customerKind.corporate', 'Corporate');
            if (s === 'individual' || raw === 0 || raw === '0')
                return t360('enums.customerKind.individual', 'Individual');
            return '—';
        };

        const customerKindLabel = Vue.computed(() => resolveCustomerKindLabel(state.profile?.core));

        const customerStatusLabel = Vue.computed(() => {
            localeTick.value;
            const core = state.profile?.core;
            const s = core?.status ?? core?.Status;
            if (s === 0 || s === 'Active' || String(s).toLowerCase() === 'active')
                return t360('enums.customerStatus.active', 'Active');
            if (s === 1 || s === 'Suspended' || String(s).toLowerCase() === 'suspended')
                return t360('enums.customerStatus.suspended', 'Suspended');
            if (s === 2 || s === 'Closed' || String(s).toLowerCase() === 'closed')
                return t360('enums.customerStatus.closed', 'Closed');
            if (s === 3 || s === 'Blacklisted' || String(s).toLowerCase() === 'blacklisted')
                return t360('enums.customerStatus.blacklisted', 'Blacklisted');
            return String(s ?? '—');
        });

        const localizedGenderLabel = () => {
            localeTick.value;
            const core = state.profile?.core;
            const g = core?.gender ?? core?.Gender;
            if (g === 1 || g === 'Male') return t360('enums.gender.male', 'Male');
            if (g === 2 || g === 'Female') return t360('enums.gender.female', 'Female');
            return t360('enums.gender.unknown', '—');
        };

        const localizedLegalStatusLabel = () => {
            localeTick.value;
            const core = state.profile?.core;
            const ls = core?.legalStatus ?? core?.LegalStatus;
            const map = {
                0: 'unknown',
                1: 'soleProprietorship',
                2: 'partnership',
                3: 'limitedLiability',
                4: 'jointStock',
                5: 'government',
            };
            const key = map[Number(ls)];
            if (key) return t360(`enums.legalStatus.${key}`, core?.legalStatusLabel ?? '—');
            return core?.legalStatusLabel ?? core?.LegalStatusLabel ?? '—';
        };

        const localizedBillingModeLabel = () => {
            localeTick.value;
            const core = state.profile?.core;
            const mode = core?.billingConsolidationMode ?? core?.BillingConsolidationMode;
            if (mode === 1 || String(mode).toLowerCase() === 'unified')
                return t360('enums.billingMode.unified', 'Unified');
            if (mode === 0 || String(mode).toLowerCase() === 'separate')
                return t360('enums.billingMode.separate', 'Separate');
            return core?.billingConsolidationModeLabel ?? core?.BillingConsolidationModeLabel ?? '—';
        };

        const localizedStatusReasonLabel = () => {
            localeTick.value;
            const core = state.profile?.core;
            const code = core?.statusReasonCode ?? core?.StatusReasonCode;
            if (code === 1 || String(code).toLowerCase() === 'credit')
                return t360('enums.statusReason.credit', 'Credit');
            if (code === 2 || String(code).toLowerCase() === 'fraud')
                return t360('enums.statusReason.fraud', 'Fraud');
            if (code === 3 || String(code).toLowerCase() === 'regulatory')
                return t360('enums.statusReason.regulatory', 'Regulatory');
            return core?.statusReasonCodeLabel ?? core?.StatusReasonCodeLabel ?? '—';
        };

        const subscriptionTypeLabel = (sub) => {
            localeTick.value;
            if (!sub) return '—';
            const code = String(sub.subscriptionTypeCode || '').trim().toLowerCase();
            if (code) {
                const hit = t360(`enums.subscriptionType.${code}`, '');
                if (hit) return hit;
            }
            return uiLang() === 'en'
                ? sub.subscriptionTypeNameEn || sub.subscriptionTypeName || sub.subscriptionTypeCode || '—'
                : sub.subscriptionTypeName || sub.subscriptionTypeNameEn || sub.subscriptionTypeCode || '—';
        };

        const productOfferingDisplayName = (sub) => {
            if (!sub) return '—';
            localeTick.value;
            const ar = (sub.productOfferingName || sub.productName || '').trim();
            const en = (sub.productOfferingNameEn || '').trim();
            return uiLang() === 'en' ? en || ar || '—' : ar || en || '—';
        };

        const subscriptionMetaLine = (sub) => {
            localeTick.value;
            const type = subscriptionTypeLabel(sub);
            const sim = simTypeLabel(sub?.simType);
            return `${type} · ${sim}`;
        };

        const simTypeLabel = (raw) => {
            localeTick.value;
            const v = String(raw || '').trim().toLowerCase();
            if (!v) return t360('enums.simType.physical', 'Physical');
            if (v === 'esim' || v.includes('esim')) return t360('enums.simType.esim', 'eSIM');
            if (v === 'physical' || v.includes('فيزي')) return t360('enums.simType.physical', 'Physical');
            return raw;
        };

        const simStatusLabel = (raw) => {
            localeTick.value;
            const v = String(raw || '').trim().toLowerCase();
            const map = {
                available: 'available',
                reserved: 'reserved',
                active: 'active',
                suspended: 'suspended',
                quarantined: 'quarantined',
                متاحة: 'available',
                محجوزة: 'reserved',
                نشطة: 'active',
                موقوفة: 'suspended',
                حجر: 'quarantined',
            };
            const key = map[v];
            return key ? t360(`enums.simStatus.${key}`, raw) : raw || '—';
        };

        const documentStatusLabel = (sub) => {
            localeTick.value;
            const raw = String(sub?.documentStatusLabel || '').trim().toLowerCase();
            const map = {
                uploaded: 'uploaded',
                verified: 'verified',
                rejected: 'rejected',
                missing: 'missing',
                مرفوع: 'uploaded',
                'موثّق': 'verified',
                موثق: 'verified',
                مرفوض: 'rejected',
                ناقص: 'missing',
            };
            const key = map[raw];
            return key ? t360(`enums.documentStatus.${key}`, sub?.documentStatusLabel) : sub?.documentStatusLabel || '—';
        };

        const serviceLineTypeLabel = (sub) => {
            localeTick.value;
            const raw = String(sub?.serviceLineTypeLabel || '').trim().toLowerCase();
            if (!raw || raw === 'mobile' || raw === 'موبايل') {
                return t360('enums.serviceLineType.mobile', 'Mobile');
            }
            return sub?.serviceLineTypeLabel || t360('enums.serviceLineType.mobile', 'Mobile');
        };

        const apiBaseUrl = () => {
            const base = typeof AxiosManager !== 'undefined' && AxiosManager.getBaseUrl ? AxiosManager.getBaseUrl() : '/api';
            return (base || '/api').replace(/\/$/, '');
        };

        const subscriptionDocumentUrl = (sub) => {
            const opId = (sub?.documentOperationId || '').trim();
            if (!opId) return '';
            const source = String(sub?.documentSource || '').toLowerCase();
            const path =
                source === 'kyc'
                    ? '/Telecom/DownloadKycDocument'
                    : '/Telecom/DownloadTelecomOperationIdentityDocument';
            const token = StorageManager.getAccessToken?.();
            const q = `id=${encodeURIComponent(opId)}${token ? `&access_token=${encodeURIComponent(token)}` : ''}`;
            return `${apiBaseUrl()}${path}?${q}`;
        };

        const walletErrorMessage = (wallet) => {
            localeTick.value;
            if (!wallet) return '';
            const code = wallet.errorMessage ?? wallet.ErrorMessage;
            if (code === 'noMsisdnForWallet') return t360('servicesTab.noMsisdnForWallet', '');
            return code || t360('servicesTab.walletLoadFailed', '');
        };

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
            localeTick.value;
            if (!s) return '—';
            const key = String(s).replace(/\s/g, '');
            return t360(`enums.operational.${key}`, s);
        };

        const isGuidLike = (value) => /^[0-9a-f]{8}-[0-9a-f-]{27,}$/i.test(String(value || '').trim());

        const timelineKindMeta = (kind) => {
            localeTick.value;
            const k = Number(kind);
            const labels = {
                0: { label: telecomT('timeline.kinds.operation', 'Operation'), badge: 'bg-danger' },
                1: { label: telecomT('timeline.kinds.payment', 'Payment'), badge: 'bg-success' },
                2: { label: telecomT('timeline.kinds.ticket', 'Ticket'), badge: 'bg-warning text-dark' },
                3: { label: telecomT('timeline.kinds.billing', 'Billing'), badge: 'bg-info text-dark' },
                4: { label: telecomT('timeline.kinds.audit', 'Audit'), badge: 'bg-secondary' },
            };
            return labels[k] || { label: telecomT('timeline.kinds.other', 'Event'), badge: 'bg-light text-dark border' };
        };

        const mappedTimeline = Vue.computed(() => {
            localeTick.value;
            const ar = contentLang() === 'ar';
            return (state.profile?.timeline || []).map((r) => {
                const kind = r.kind ?? r.Kind;
                const rawRef = r.referenceId ?? r.ReferenceId;
                const reference = rawRef && !isGuidLike(rawRef) ? String(rawRef).trim() : '';
                const subtitle = (r.subtitle ?? r.Subtitle ?? '').trim();
                const status = (r.status ?? r.Status ?? '').trim();
                const kindMeta = timelineKindMeta(kind);
                const detailParts = [];
                if (subtitle) detailParts.push(subtitle);
                if (reference && !subtitle.includes(reference)) {
                    detailParts.push(
                        ar ? `المرجع ${reference}` : `Ref ${reference}`,
                    );
                }
                return {
                    occurredAtUtc: r.occurredAtUtc ?? r.OccurredAtUtc,
                    kind,
                    kindLabel: kindMeta.label,
                    kindBadge: kindMeta.badge,
                    title: ar
                        ? (r.titleAr ?? r.TitleAr ?? '—')
                        : (r.titleEn ?? r.TitleEn ?? r.titleAr ?? r.TitleAr ?? '—'),
                    detail: detailParts.join(' · ') || '—',
                    status: status || null,
                    actionUrl: r.actionUrl ?? r.ActionUrl,
                    display: formatDt(r.occurredAtUtc ?? r.OccurredAtUtc),
                };
            });
        });

        const timelineFilters = Vue.computed(() => [
            { id: '', label: telecomT('timeline.filters.all', 'All') },
            { id: '0', label: telecomT('timeline.filters.operations', 'Operations') },
            { id: '1', label: telecomT('timeline.filters.payments', 'Payments') },
            { id: '2', label: telecomT('timeline.filters.tickets', 'Tickets') },
            { id: '3', label: telecomT('timeline.filters.billing', 'Billing') },
            { id: '4', label: telecomT('timeline.filters.audit', 'Audit') },
        ]);

        const fetchTimeline = async (reset = false) => {
            if (!state.customerId) return;
            if (reset) {
                state.timelineSkip = 0;
                if (state.profile) state.profile.timeline = [];
            }
            state.timelineLoading = true;
            try {
                let url =
                    '/Customer/GetCustomer360Timeline?customerId=' +
                    encodeURIComponent(state.customerId) +
                    '&take=25&skip=' +
                    state.timelineSkip;
                if (state.timelineKindFilter) {
                    url += '&kinds=' + encodeURIComponent(state.timelineKindFilter);
                }
                const tlRes = await AxiosManager.get(url, {});
                const tl = tlRes?.data?.content ?? tlRes?.data?.Content ?? {};
                const items = tl?.items ?? tl?.Items ?? [];
                state.timelineHasMore = tl?.hasMore ?? tl?.HasMore ?? false;
                if (!state.profile) return;
                state.profile.timeline = reset
                    ? items
                    : [...(state.profile.timeline || []), ...items];
                state.timelineSkip += items.length;
            } catch {
                if (reset && state.profile) state.profile.timeline = [];
            } finally {
                state.timelineLoading = false;
            }
        };

        const setTimelineFilter = async (id) => {
            state.timelineKindFilter = id;
            await fetchTimeline(true);
        };

        const loadMoreTimeline = () => fetchTimeline(false);

        const mappedTickets = Vue.computed(() =>
            (state.profile?.supportTickets || []).map((r) => {
                const status = Number(r.status ?? r.Status ?? 0);
                const priority = Number(r.priority ?? r.Priority ?? 1);
                return {
                    id: r.id ?? r.Id,
                    ticketNumber: r.ticketNumber ?? r.TicketNumber,
                    msisdn: r.msisdn ?? r.Msisdn,
                    notes: r.notes ?? r.Notes,
                    resolutionNotes: r.resolutionNotes ?? r.ResolutionNotes,
                    status,
                    statusLabel: ticketEnumLabel('status', status),
                    statusBadge:
                        status === RESOLVED
                            ? 'bg-success'
                            : status === ESCALATED
                              ? 'bg-dark'
                              : status === 1
                                ? 'bg-primary'
                                : 'bg-warning text-dark',
                    priorityLabel: ticketEnumLabel('priority', priority),
                    issueLabel: ticketEnumLabel('issue', Number(r.issueType ?? r.IssueType)),
                    createdDisplay: formatDt(r.createdAtUtc ?? r.CreatedAtUtc),
                    canOperate: status !== RESOLVED,
                    canEscalate: status !== RESOLVED && status !== ESCALATED,
                    isEscalated: status === ESCALATED,
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
            const loc = uiLang() === 'en' ? 'en-US' : 'ar-SY';
            return n.toLocaleString(loc, { maximumFractionDigits: 2 });
        };

        const formatBucketUnit = (unit) => {
            localeTick.value;
            const u = String(unit || '').trim().toLowerCase();
            if (!u) return '';
            if (u === 'gb') return t360('enums.units.gb', 'GB');
            if (u === 'sms') return t360('enums.units.sms', 'SMS');
            if (u === 'minutes' || u === 'minute' || u === 'دقيقة' || u === 'دقائق')
                return t360('enums.units.minutes', 'min');
            return unit;
        };

        const bucketLabel = (b) => {
            localeTick.value;
            const t = b?.componentType ?? b?.ComponentType;
            const map = {
                0: t360('enums.bucket.voice', t360('customerList.c360.walletVoice', 'Voice')),
                1: t360('enums.bucket.data', t360('customerList.c360.walletData', 'Data')),
                2: t360('enums.bucket.sms', t360('customerList.c360.walletSms', 'SMS')),
                Voice: t360('enums.bucket.voice', 'Voice'),
                Data: t360('enums.bucket.data', 'Data'),
                Sms: t360('enums.bucket.sms', 'SMS'),
            };
            return map[t] ?? String(t ?? '—');
        };

        const lineWallet = (subscriptionId) => state.lineWallets[subscriptionId] ?? null;

        const subscriptionBalanceLine = (sub) => {
            if (!sub) return { busy: false, balance: null, currency: null, limit: null };
            if (state.lineWalletsBusy[sub.id]) return { busy: true, balance: null, currency: null, limit: null };
            const w = lineWallet(sub.id);
            if (w?.success) {
                return {
                    busy: false,
                    balance: w.balance ?? w.Balance,
                    currency: w.currency ?? w.Currency ?? 'SYP',
                    limit: w.outstandingBalance ?? w.OutstandingBalance ?? null,
                };
            }
            return {
                busy: false,
                balance: sub.prepaidBalance ?? null,
                currency: t360('common.currencySuffix', 'SYP'),
                limit: sub.postpaidCreditLimit ?? null,
            };
        };

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
            if (typeof TelecomRechargeFlow !== 'undefined') {
                return TelecomRechargeFlow.pollPaymentDetail(AxiosManager, paymentId, maxAttempts);
            }
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
                        new Error(detail?.failureReason || detail?.FailureReason || telecomT('swal.paymentConfirmFailed', 'Payment failed')),
                        { response: res }
                    );
                }
                await new Promise((r) => setTimeout(r, 500));
            }
            return null;
        };

        const rechargeFlowLabels = () => ({
            methodTitle: telecomT('swal.rechargeMethod', 'Recharge method'),
            methodHint: telecomT('swal.rechargeMethodHint', 'Wallet/cash: amount + receipt. Voucher: prepaid card code.'),
            wallet: telecomT('swal.walletCash', 'Wallet / cash'),
            voucher: telecomT('swal.voucher', 'Voucher'),
            continueBtn: telecomT('swal.continue', 'Continue'),
            cancelBtn: telecomT('swal.cancel', 'Cancel'),
            amountTitle: telecomT('swal.rechargeAmount', 'Amount'),
            amountPlaceholder: '15000',
            amountHint: telecomT('swal.rechargeAmountHint', 'Cash amount in SYP received from the customer.'),
            amountFooter: telecomT('swal.rechargeAmountFooter', 'Tip: typical demo amounts are 15,000 or 30,000 SYP.'),
            amountEmpty: telecomT('swal.rechargeAmountEmpty', 'Please enter the cash amount.'),
            amountInvalid: telecomT('swal.rechargeAmountInvalid', 'Use numbers only (greater than zero).'),
            invalidAmount: telecomT('swal.amountInvalid', 'Invalid amount'),
            amountTooHigh: telecomT('swal.rechargeAmountTooHigh', 'Maximum recharge amount is {max} SYP.'),
            amountMax: String(TelecomRechargeFlow?.DEFAULT_AMOUNT_MAX || 1000000),
            paymentRefTitle: telecomT('swal.paymentRef', 'Payment reference'),
            paymentRefPlaceholder: 'WAL-2026-001234',
            refHint: telecomT('swal.rechargeRefHint', 'Receipt or cashier reference — required for audit.'),
            refEmpty: telecomT('swal.rechargeRefEmpty', 'Payment reference is required.'),
            confirmBtn: telecomT('swal.confirmRecharge', 'Confirm'),
            refRequired: telecomT('swal.paymentRefRequired', 'Required'),
            voucherCodeTitle: telecomT('swal.voucherCode', 'Voucher code'),
            voucherPlaceholder: telecomT('swal.voucherPh', ''),
            voucherHint: telecomT('swal.rechargeVoucherHint', 'Enter the prepaid voucher code.'),
            validateBtn: telecomT('swal.validate', 'Validate'),
            voucherRequired: telecomT('swal.voucherRequired', 'Required'),
            voucherInvalid: telecomT('swal.voucherInvalid', 'Invalid'),
            voucherConfirmTitle: telecomT('swal.voucherOk', 'Valid'),
            voucherConfirmBtn: telecomT('swal.rechargeWithVoucher', 'Recharge'),
            draftMissing: telecomT('swal.paymentDraftMissing', 'No draft'),
            confirmFailed: telecomT('swal.paymentConfirmFailed', 'Failed'),
            missingContext: telecomT('swal.noMsisdnRecharge', 'No MSISDN'),
        });

        const executePaymentFlow = async (customerId, subscriptionId, busyKey, onSuccess) => {
            if (typeof TelecomRechargeFlow === 'undefined') {
                Swal.fire({ icon: 'error', title: telecomT('swal.rechargeFailed', 'Recharge failed') });
                return;
            }
            state.rechargeBusy = busyKey;
            try {
                const confirm = await TelecomRechargeFlow.runFlow(AxiosManager, Swal, {
                    customerId,
                    subscriptionId,
                    labels: rechargeFlowLabels(),
                });
                if (!confirm) return;
                await onSuccess();
                const currency = telecomT('common.currencySuffix', 'SYP');
                Swal.fire({
                    icon: 'success',
                    title: telecomT('swal.receipt', 'Receipt'),
                    html:
                        `<p>${confirm?.messageAr || confirm?.MessageAr || telecomT('swal.recharged', 'Done')}</p>` +
                        `<p class="small"><strong>PAY:</strong> ${confirm?.paymentNumber ?? confirm?.PaymentNumber}</p>` +
                        `<p class="small"><strong>${telecomT('swal.receiptNo', 'Receipt')}</strong> ${confirm?.receiptNumber ?? confirm?.ReceiptNumber}</p>` +
                        `<p class="small"><strong>CorrelationId:</strong> <span dir="ltr">${confirm?.correlationId ?? confirm?.CorrelationId}</span></p>` +
                        `<p class="small"><strong>${telecomT('swal.balanceAfter', 'Balance')}</strong> ${formatAmount(confirm?.newBalance ?? confirm?.NewBalance)} ${currency}</p>`,
                });
            } catch (e) {
                toastError(e, telecomT('swal.rechargeFailed', 'Recharge failed'));
            } finally {
                state.rechargeBusy = '';
            }
        };

        const openRechargeModal = async (sub) => {
            const wallet = lineWallet(sub.id);
            const msisdn = sub.msisdn || wallet?.msisdn || '';
            if (!msisdn) {
                Swal.fire({ icon: 'warning', title: telecomT('swal.noMsisdnRecharge', 'No MSISDN') });
                return;
            }
            await executePaymentFlow(state.customerId, sub.id, sub.id, () => loadProfile(true));
        };

        const loadProfileSupplements = async () => {
            if (!state.customerId || !state.profile?.core) return;
            try {
                const res = await AxiosManager.get(
                    '/Customer/GetCustomer360Supplements?customerId=' + encodeURIComponent(state.customerId),
                    {}
                );
                const sup = res?.data?.content ?? res?.data?.Content ?? null;
                if (!sup || !state.profile?.core) return;
                state.profile = {
                    ...state.profile,
                    activeVasServices: sup.activeVasServices ?? sup.ActiveVasServices ?? [],
                    supportTickets: sup.supportTickets ?? sup.SupportTickets ?? [],
                    billingLogs: sup.billingLogs ?? sup.BillingLogs ?? [],
                    activityLogs: sup.activityLogs ?? sup.ActivityLogs ?? [],
                    telecomOperations: sup.telecomOperations ?? sup.TelecomOperations ?? [],
                };
                await fetchTimeline(true);
            } catch {
                /* supplements are non-blocking */
            }
        };

        const loadSecondaryLineData = async () => {
            const selected = selectedSubscription.value;
            if (selected?.id) {
                delete state.hlrBySub[selected.id];
            }
            await Promise.all([
                loadCbs(),
                selected ? checkHlr(selected) : Promise.resolve(),
            ]);
        };

        const loadProfile = async (silent = false) => {
            if (!state.customerId) {
                state.loadError = telecomT('swal.customerIdMissing', 'Missing id');
                state.loading = false;
                return;
            }
            if (!silent) {
                state.loading = true;
            }
            state.loadError = null;
            try {
                const res = await AxiosManager.get(
                    '/Customer/GetCustomer360?customerId=' + encodeURIComponent(state.customerId),
                    {}
                );
                const core = res?.data?.content ?? res?.data?.Content ?? null;
                state.profile = core ? { core } : null;
                if (!state.profile?.core) {
                    state.loadError = telecomT('swal.profileNotFound', 'Not found');
                } else {
                    state.aiSim.msisdn = primaryMsisdn() || state.aiSim.msisdn;
                    ensureLineSelected();
                    void loadProfileSupplements();
                    void loadSecondaryLineData();
                }
            } catch (e) {
                state.profile = null;
                state.loadError = e?.response?.data?.message || e?.message || telecomT('swal.loadFailed', 'Load failed');
            } finally {
                if (!silent) {
                    state.loading = false;
                }
            }
        };

        const checkHlr = async (sub, { selectLine = false } = {}) => {
            const profileId = sub?.subscriberProfileId ?? sub?.SubscriberProfileId;
            const msisdn = (sub?.msisdn || '').trim();
            if (!profileId && !msisdn) return null;
            if (selectLine) {
                state.prov.selectedLineKey = lineKey(sub);
            }
            state.hlrBusy = sub.id;
            try {
                const q = msisdn
                    ? 'msisdn=' + encodeURIComponent(msisdn)
                    : 'subscriberProfileId=' + encodeURIComponent(profileId);
                const res = await AxiosManager.get('/Telecom/CheckHlrStatus?' + q, {});
                const content = res?.data?.content ?? res?.data?.Content ?? res?.data;
                const snapshot = {
                    hlrSubscriberState:
                        content?.hlrSubscriberState ?? content?.HlrSubscriberState ?? content?.message ?? '—',
                    differsFromCrm: !!(content?.differsFromCrm ?? content?.DiffersFromCrm),
                    isOnline: content?.isOnline ?? content?.IsOnline,
                    crmOperationalStatus:
                        content?.crmOperationalStatus ?? content?.CrmOperationalStatus ?? sub?.profileOperationalStatus,
                };
                state.hlrBySub[sub.id] = snapshot;
                return snapshot;
            } catch (e) {
                state.hlrBySub[sub.id] = {
                    hlrSubscriberState: pickHttpErrorMessage(e) || 'ERROR',
                    differsFromCrm: false,
                };
                return null;
            } finally {
                state.hlrBusy = '';
            }
        };

        const canPayAndReconnect = (sub) => {
            const status = sub?.profileOperationalStatus || sub?.operationalStatus || '';
            return (
                (status === 'SuspendedBilling' || status === 'SUSPENDED_BILLING' || status === 'Suspended' || status === 'SUSPENDED') &&
                StorageManager.hasAnyPermission(perms.value, ['telecom.line.reconnect_request', 'telecom.line.reconnect', 'telecom.hub.frontline'])
            );
        };

        const executePayAndReconnect = async (sub) => {
            const msisdn = (sub?.msisdn || '').trim();
            if (!msisdn) {
                Swal.fire({ icon: 'warning', title: telecomT('swal.noActiveLine', 'No line') });
                return;
            }
            state.payAndReconnectBusy = sub.id;
            try {
                const res = await AxiosManager.post('/Telecom/ExecutePayAndReconnect', {
                    customerId: state.customerId,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdn: msisdn,
                });
                const body = res?.data?.content ?? res?.data?.Content ?? res?.data;
                if (res?.data?.code === 200) {
                    const paymentRef = body?.paymentReference || body?.PaymentReference || '';
                    const reconnectRef = body?.reconnectReference || body?.ReconnectReference || body?.operationNumber || body?.OperationNumber || '';
                    await loadProfile(true);
                    await loadCbs();
                    await loadLineWallets();
                    const refreshed = allSubscriptions.value.find((s) => s.id === sub.id) || sub;
                    await checkHlr(refreshed);
                    toastSuccess(
                        telecomT('lineActions.payAndReconnectSuccess', 'Pay & Reconnect completed'),
                        `<div class="small">
                            <p class="mb-1"><strong>${telecomT('lineActions.paymentRef', 'Payment')}:</strong> ${paymentRef}</p>
                            <p class="mb-0"><strong>${telecomT('lineActions.reconnectRef', 'Reconnect')}:</strong> ${reconnectRef}</p>
                        </div>`
                    );
                } else {
                    throw Object.assign(new Error(body?.message || res?.data?.message || 'Failed'), { response: res });
                }
            } catch (e) {
                toastError(e, telecomT('lineActions.payAndReconnectFailed', 'Pay & Reconnect failed'));
            } finally {
                state.payAndReconnectBusy = '';
            }
        };

        const selectSubscriptionRow = async (sub) => {
            state.prov.selectedLineKey = lineKey(sub);
            await checkHlr(sub);
        };

        const reprovisionSelectedHlr = async () => {
            const sub = selectedSubscription.value;
            const profileId = (sub?.subscriberProfileId || '').trim();
            if (!profileId) {
                Swal.fire({ icon: 'warning', title: telecomT('swal.noActiveLine', 'No line') });
                return;
            }
            if (!can.value.network) {
                Swal.fire({ icon: 'info', title: telecomT('swal.noPermission', 'No permission') });
                return;
            }
            state.hlrRemediationBusy = 'reprovision';
            try {
                const res = await AxiosManager.post('/Telecom/ReprovisionSubscriberToHlr', {
                    subscriberProfileId: profileId,
                    msisdnAssetId: (sub?.msisdnAssetId || sub?.MsisdnAssetId || '').trim() || null,
                    msisdn: (sub?.msisdn || '').trim() || null,
                });
                const body = res?.data?.content ?? res?.data?.Content ?? {};
                if (body?.logEntry || body?.LogEntry) {
                    state.lastHlrLog = body.logEntry || body.LogEntry;
                }
                await checkHlr(sub);
                await loadCbs();
                toastSuccess(
                    body?.message || body?.Message || t360('hlrRemediation.reprovisionOk', 'HLR reprovisioned'),
                    state.lastHlrLog
                        ? `<p class="small font-monospace mb-0" dir="ltr">${state.lastHlrLog}</p>`
                        : ''
                );
            } catch (e) {
                toastError(e, telecomT('swal.hlrSyncFailed', 'HLR failed'));
            } finally {
                state.hlrRemediationBusy = '';
            }
        };

        const quickSimSwapRemediation = async () => {
            const sub = selectedSubscription.value;
            if (!sub) return;
            if (!can.value.network) {
                Swal.fire({ icon: 'info', title: telecomT('swal.noPermission', 'No permission') });
                return;
            }
            const { value: iccid, isConfirmed } = await Swal.fire({
                title: ui.value.actions.simSwap,
                input: 'text',
                inputLabel: telecomT('wizardUi.iccid', 'ICCID'),
                inputPlaceholder: '89963…',
                showCancelButton: true,
                confirmButtonText: telecomT('swal.confirm', 'Confirm'),
                cancelButtonText: telecomT('swal.cancel', 'Cancel'),
                inputValidator: (v) =>
                    !v || String(v).trim().length < 10
                        ? telecomT('wizardUi.iccid', 'ICCID required')
                        : undefined,
            });
            if (!isConfirmed || !iccid) return;
            state.hlrRemediationBusy = 'simswap';
            try {
                const res = await AxiosManager.post('/TelecomBackOffice/ExecuteTechnicalAction', {
                    customerId: state.customerId,
                    actionType: 'SIMSWAP',
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdn: sub.msisdn,
                    simIccid: String(iccid).trim(),
                    notes: 'Customer360 HLR remediation — SIM swap',
                });
                const body = res?.data?.content ?? res?.data?.Content ?? {};
                state.lastHlrLog =
                    body?.messageAr ||
                    body?.MessageAr ||
                    `SIMSWAP ${sub.msisdn} -> ICCID ${String(iccid).trim()}`;
                await loadProfile(true);
                await loadCbs();
                const refreshed = allSubscriptions.value.find((s) => s.id === sub.id) || sub;
                await checkHlr(refreshed);
                toastSuccess(body?.messageAr || body?.MessageAr || telecomT('lineActions.success', 'Done'));
            } catch (e) {
                toastError(e, telecomT('lineActions.businessError', 'Failed'));
            } finally {
                state.hlrRemediationBusy = '';
            }
        };

        const openRemediationSuspension = () => openProvisioningWizard('suspension');
        const openRemediationReconnect = () => openProvisioningWizard('reconnect');

        const openAiWizard = () => {
            state.aiSim.msisdn = primaryMsisdn() || state.aiSim.msisdn;
            if (!state.aiSim.transcript) {
                state.aiSim.transcript = telecomT('ai.transcriptPh', '');
            }
            if (!aiModal) {
                aiModal = new bootstrap.Modal(document.getElementById('c360AiWizardModal'));
            }
            aiModal.show();
        };

        const openSupportTicketC360 = () => openProvisioningWizard('support');

        const submitSupportFromC360 = async () => {
            const line = parseSelectedLine();
            const msisdn = (line?.msisdn || state.wizard.primaryLabel || primaryMsisdn() || '').trim();
            if (!msisdn) {
                Swal.fire({
                    icon: 'warning',
                    title: telecomT('customerList.supportTicket.noMsisdn', 'No MSISDN'),
                });
                return;
            }
            state.wizard.submitBusy = true;
            try {
                const res = await AxiosManager.post('/TelecomBackOffice/CreateTechnicalTicket', {
                    msisdn,
                    issueType: Number(state.wizard.supportIssueType) || 0,
                    priority: 1,
                    notes: (state.wizard.supportNotes || '').trim(),
                    customerId: state.customerId || null,
                    subscriberProfileId:
                        (state.wizard.primarySubscriberProfileId || line?.subscriberProfileId || '').trim() || null,
                });
                if (res?.data?.code !== 200) {
                    throw Object.assign(
                        new Error(
                            res?.data?.message || telecomT('customerList.supportTicket.createFail', 'Create failed')
                        ),
                        { response: res }
                    );
                }
                const ticket = res?.data?.content?.data ?? res?.data?.content?.Data;
                state.wizard.supportTicketCreated = true;
                state.wizard.supportTicketId = ticket?.id ?? ticket?.Id ?? '';
                state.wizard.supportTicketNumber = ticket?.ticketNumber ?? ticket?.TicketNumber ?? '';
                state.wizard.createdOperationNumber = state.wizard.supportTicketNumber;
                Swal.fire({
                    icon: 'success',
                    title: telecomT('customerList.supportTicket.createdOk', 'Ticket created'),
                    html: state.wizard.supportTicketNumber
                        ? `<p dir="ltr">${state.wizard.supportTicketNumber}</p>`
                        : undefined,
                    timer: 2200,
                    showConfirmButton: false,
                });
                state.wizard.step = 3;
            } catch (e) {
                Swal.fire({
                    icon: 'error',
                    title:
                        e?.response?.data?.message ||
                        telecomT('customerList.supportTicket.createFail', 'Create failed'),
                });
            } finally {
                state.wizard.submitBusy = false;
            }
        };

        const simulateAiCall = async () => {
            const msisdn = (state.aiSim.msisdn || '').trim();
            const transcript = (state.aiSim.transcript || '').trim();
            if (!msisdn || transcript.length < 5) {
                Swal.fire({ icon: 'warning', title: telecomT('swal.aiInputRequired', 'Input required') });
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
                    telecomT('swal.aiCreated', 'AI ticket ready'),
                    `<p class="small mb-0">${body?.ticketNumber ?? ''}<br/>${body?.summaryAr ?? ''}</p>`
                );
            } catch (e) {
                toastError(e, telecomT('swal.aiCreateFailed', 'Create failed'));
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
                const msg = body?.message || body?.Message || telecomT('swal.cbsForced', 'CBS applied');
                await loadProfile(true);
                await loadCbs();
                toastSuccess(msg);
            } catch (e) {
                toastError(e, telecomT('swal.cbsForceFailed', 'CBS failed'));
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
                const msg = body?.message || body?.Message || telecomT('swal.hlrSynced', 'HLR synced');
                await loadProfile(true);
                toastSuccess(msg);
            } catch (e) {
                toastError(e, telecomT('swal.hlrSyncFailed', 'HLR failed'));
            } finally {
                setOpBusy(null);
            }
        };

        const escalateTicket = async (t) => {
            const { value: notes } = await Swal.fire({
                title: telecomT('swal.escalateTitle', 'Escalate'),
                input: 'textarea',
                inputLabel: telecomT('swal.escalateLabel', 'Reason'),
                inputPlaceholder: telecomT('swal.escalatePh', ''),
                showCancelButton: true,
                confirmButtonText: telecomT('swal.escalate', 'Escalate'),
                cancelButtonText: telecomT('swal.cancel', 'Cancel'),
                inputValidator: (v) =>
                    !v || v.trim().length < 10 ? telecomT('swal.escalateMin', 'Min 10 chars') : undefined,
            });
            if (!notes) return;
            setOpBusy(t.id, 'escalate');
            try {
                await AxiosManager.post('/TelecomBackOffice/EscalateToTier3', {
                    ticketId: t.id,
                    escalationNotes: notes.trim(),
                });
                await loadProfile(true);
                toastSuccess(telecomT('swal.escalated', 'Escalated'));
            } catch (e) {
                toastError(e, telecomT('swal.escalateFailed', 'Failed'));
            } finally {
                setOpBusy(null);
            }
        };

        const operationalBadgeClass = (s) => {
            const v = String(s || '').toLowerCase();
            if (v.includes('active')) return 'bg-success';
            if (v.includes('suspend')) return 'bg-warning text-dark';
            return 'bg-secondary';
        };

        const vasStatusLabel = (s) => {
            localeTick.value;
            const v = String(s || '').toLowerCase();
            if (v === 'active') return t360('vasStatus.active', 'Active');
            if (v === 'suspended') return t360('vasStatus.suspended', 'Suspended');
            return s || '—';
        };

        const subscriptionBadgeText = Vue.computed(() => {
            localeTick.value;
            const tpl = telecomT('subscriptionBadge', '{count}');
            return tpl.replace('{count}', String(allSubscriptions.value.length));
        });

        const vasBadgeClass = (s) => {
            const v = String(s || '').toLowerCase();
            if (v === 'active') return 'bg-success';
            if (v === 'suspended') return 'bg-warning text-dark';
            return 'bg-secondary';
        };

        const wizardTitle = Vue.computed(() => {
            localeTick.value;
            const k = state.wizard.kind;
            if (!k) return '';
            const hit = window.TelecomI18n?.t?.(`wizard.titles.${k}`);
            return hit || k;
        });

        const contentLang = () => (document.documentElement.lang === 'ar' ? 'ar' : 'en');

        const activateRequiredDeposit = Vue.computed(() => {
            if (state.wizard.kind !== 'activate') return 0;
            const fromDetail = offerDefaultMonthlyPrice(state.wizard.offerDetail);
            if (fromDetail != null) return fromDetail;
            const o = (state.wizard.offerings || []).find(
                (x) => x.id === state.wizard.selectedOfferingId
            );
            return Number(o?.defaultPrice ?? o?.DefaultPrice ?? o?.unitPrice ?? o?.UnitPrice ?? 0) || 0;
        });

        const wizardOfferDisplayName = Vue.computed(() => {
            localeTick.value;
            return offerDetailDisplayName(state.wizard.offerDetail, contentLang());
        });

        const wizardOfferMonthlyPrice = Vue.computed(() => {
            localeTick.value;
            const p = offerDefaultMonthlyPrice(state.wizard.offerDetail);
            return p != null ? formatMoneyOffer(p, contentLang()) : null;
        });

        const wizardOfferVoiceLine = Vue.computed(() => {
            localeTick.value;
            const d = state.wizard.offerDetail;
            if (!d) return '';
            const c = offerComponentByType(d, 0);
            if (c) return formatOfferQuotaLine(c, contentLang());
            if (d.voiceMinutesLimit) {
                return contentLang() === 'ar'
                    ? `${d.voiceMinutesLimit} دقيقة`
                    : `${d.voiceMinutesLimit} min`;
            }
            return '';
        });

        const wizardOfferDataLine = Vue.computed(() => {
            localeTick.value;
            const d = state.wizard.offerDetail;
            if (!d) return '';
            const c = offerComponentByType(d, 1);
            if (c) return formatOfferQuotaLine(c, contentLang());
            if (d.speedQuotaLimitGb) {
                return contentLang() === 'ar'
                    ? `${d.speedQuotaLimitGb} جيجا`
                    : `${d.speedQuotaLimitGb} GB`;
            }
            return '';
        });

        const wizardOfferSmsLine = Vue.computed(() => {
            localeTick.value;
            const d = state.wizard.offerDetail;
            if (!d) return '';
            const c = offerComponentByType(d, 2);
            return c ? formatOfferQuotaLine(c, contentLang()) : '';
        });

        const wizardOfferSummaryText = Vue.computed(() => {
            localeTick.value;
            return offerDetailSummaryText(state.wizard.offerDetail, contentLang());
        });

        const wizardOfferPriceLabel = Vue.computed(() => {
            localeTick.value;
            const p = wizardOfferMonthlyPrice.value;
            if (!p) return '';
            const tpl = telecomT('wizard.migrationOffers.offerCard.priceLabel', 'Price: {price}');
            return tpl.replace('{price}', p);
        });

        const activateSimIccidLocked = Vue.computed(() => state.wizard.kind === 'activate');

        const loadWizardOfferingDetail = async (offerId) => {
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

        const onWizardOfferingChanged = async () => {
            await loadWizardOfferingDetail(state.wizard.selectedOfferingId);
            if (state.wizard.kind === 'migrate') {
                await loadMigrationProrationPreview();
            }
        };

        const lineTypeRowId = (row) => String(row?.id ?? row?.Id ?? '').trim();

        const lineTypeDisplayName = (row) => {
            if (!row) return '—';
            const ar = row.nameAr || row.NameAr || '';
            const en = row.nameEn || row.NameEn || '';
            if (localeTick.value && ar) return ar;
            if (en) return en;
            return ar || en || row.code || row.Code || '—';
        };

        const activationLineTypeOptions = Vue.computed(() =>
            (state.telecomLineTypes || [])
                .filter((x) => x.isActive !== false && x.IsActive !== false)
                .map((x) => ({
                    ...x,
                    id: lineTypeRowId(x),
                    sortOrder: x.sortOrder ?? x.SortOrder ?? 0,
                }))
                .filter((x) => x.id)
                .sort((a, b) => a.sortOrder - b.sortOrder)
        );

        const filteredActivatePoolNumbers = Vue.computed(() => {
            const lineTypeId = (state.wizard.activationLineTypeId || '').trim();
            let rows = state.wizard.poolNumbers || [];
            if (!lineTypeId) return [];
            return rows.filter((row) => {
                const compat = row?.compatibleSubscriptionTypeId ?? row?.CompatibleSubscriptionTypeId ?? '';
                return !compat || compat === lineTypeId;
            });
        });

        const loadTelecomLineTypes = async () => {
            if (state.telecomLineTypesBusy) return;
            state.telecomLineTypesBusy = true;
            try {
                const res = await AxiosManager.get(
                    '/TelecomSubscriptionType/GetTelecomSubscriptionTypeList?isDeleted=false&activeOnly=true',
                    {}
                );
                const content = res?.data?.content ?? res?.data?.Content;
                state.telecomLineTypes = content?.data ?? content?.Data ?? [];
            } catch {
                state.telecomLineTypes = [];
            } finally {
                state.telecomLineTypesBusy = false;
            }
        };

        const loadActivateOfferings = async () => {
            const sid = (state.wizard.primarySubscriberProfileId || '').trim();
            const lineTypeId = (state.wizard.activationLineTypeId || '').trim();
            if (!sid || !lineTypeId) {
                state.wizard.offerings = [];
                return;
            }
            try {
                const url =
                    '/Product/GetMigrationEligibleProducts?subscriberProfileId=' +
                    encodeURIComponent(sid) +
                    '&targetSubscriptionTypeId=' +
                    encodeURIComponent(lineTypeId);
                const res = await AxiosManager.get(url, {});
                const content = res?.data?.content ?? res?.data?.Content ?? {};
                state.wizard.offerings = (content.data || content.Data || []).map((o) => ({
                    id: o.id,
                    name: o.name,
                    nameEn: o.name,
                    serviceCode: o.serviceCode,
                }));
            } catch {
                state.wizard.offerings = [];
            }
        };

        const onActivationLineTypeChange = async () => {
            state.wizard.msisdnAssetId = '';
            state.wizard.simIccid = '';
            state.wizard.selectedOfferingId = '';
            state.wizard.offerDetail = null;
            await loadActivateOfferings();
        };

        const searchActivateSecondary = async () => {
            const nat = (state.wizard.activateSecondarySearchNationalId || '').trim();
            const ph = (state.wizard.activateSecondarySearchPhone || '').trim();
            if (nat.length < 2 && ph.length < 2) {
                Swal.fire({ icon: 'info', title: telecomT('swal.searchMinChars', 'Search hint') });
                return;
            }
            state.wizard.activateSecondaryBusy = true;
            try {
                const qs = new URLSearchParams();
                if (nat) qs.set('nationalId', nat);
                if (ph) qs.set('phone', ph);
                const res = await AxiosManager.get('/Customer/FindCustomerCandidates?' + qs.toString(), {});
                state.wizard.activateSecondaryResults = res?.data?.content?.data ?? [];
                state.wizard.activateSecondaryProfileId = '';
                state.wizard.activateSecondaryLabel = '';
            } catch (e) {
                state.wizard.activateSecondaryResults = [];
                toastError(e, telecomT('customerList.swal.searchFailed', 'Search failed'));
            } finally {
                state.wizard.activateSecondaryBusy = false;
            }
        };

        const selectActivateSecondary = async (c) => {
            if (!c?.id) return;
            state.wizard.activateSecondaryBusy = true;
            try {
                const res = await AxiosManager.get(
                    '/Customer/GetCustomer360?customerId=' + encodeURIComponent(c.id),
                    {}
                );
                const subs = res?.data?.content?.activeSubscriptions || [];
                const pick = subs.find((s) => s.isPrimaryLine) || subs[0];
                const pid = (pick?.subscriberProfileId || '').trim();
                state.wizard.activateSecondaryProfileId = pid;
                state.wizard.activateSecondaryLabel = pid ? `${c.name || '—'}` : '';
                if (!pid) {
                    Swal.fire({
                        icon: 'warning',
                        title: telecomT('customerList.swal.noSubscriberProfile', 'No profile'),
                    });
                }
            } catch (e) {
                toastError(e, telecomT('swal.loadFailed', 'Load failed'));
            } finally {
                state.wizard.activateSecondaryBusy = false;
            }
        };

        const loadMigrationProrationPreview = async () => {
            const offeringId = (state.wizard.selectedOfferingId || '').trim();
            const sid = (state.wizard.primarySubscriberProfileId || '').trim();
            const assetId = (state.wizard.primaryMsisdnAssetId || '').trim();
            if (!offeringId || !sid || !assetId || typeof TelecomBssWizardClearance === 'undefined') {
                state.wizard.mgrProrationPreview = null;
                return;
            }
            state.wizard.mgrProrationBusy = true;
            try {
                state.wizard.mgrProrationPreview = await TelecomBssWizardClearance.migration.loadPreview(
                    sid,
                    assetId,
                    offeringId
                );
            } catch {
                state.wizard.mgrProrationPreview = null;
            } finally {
                state.wizard.mgrProrationBusy = false;
            }
        };

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
            return t360('wizard.migrationOffers.daysRemaining', 'Days').replace('{days}', days).replace('{total}', total);
        });
        const mgrProrationSufficient = Vue.computed(
            () => state.wizard.mgrProrationPreview?.sufficientBalance ?? state.wizard.mgrProrationPreview?.SufficientBalance !== false
        );

        const wizardRequiresStep2Identity = Vue.computed(() =>
            typeof TelecomWizardConfirm !== 'undefined'
                ? TelecomWizardConfirm.requiresStep2IdentityUpload(state.wizard, {
                    susRequiresStep2Identity: susRequiresStep2Identity.value,
                })
                : false);

        const wizardAwaitBackOffice = Vue.computed(() =>
            typeof TelecomWizardConfirm !== 'undefined'
                ? TelecomWizardConfirm.awaitBackOffice(state.wizard)
                : false);

        const wizardShowsConfirmCbs = Vue.computed(() =>
            typeof TelecomWizardConfirm !== 'undefined'
                ? TelecomWizardConfirm.showsConfirmCbs(state.wizard, {
                    activateRequiredDeposit: activateRequiredDeposit.value,
                })
                : false);

        const canWizardFinishStep2 = Vue.computed(() =>
            typeof TelecomWizardConfirm !== 'undefined'
                ? TelecomWizardConfirm.canFinishStep2(state.wizard, {
                    activateRequiredDeposit: activateRequiredDeposit.value,
                })
                : false);

        const isCorporateProfile = () => {
            const core = state.profile?.core;
            return typeof TelecomWizardConfirm !== 'undefined'
                ? TelecomWizardConfirm.isCorporateCustomerKind(core?.customerKind ?? core?.CustomerKind)
                : String(core?.customerKind ?? core?.CustomerKind ?? '').toLowerCase() === 'corporate';
        };

        const onTerminationTypeChange = () => {
            const ty = (state.wizard.trmTerminationType || '').trim();
            state.wizard.trmRequiresBackOffice =
                ty === 'Fraud' || ty === 'Regulatory' || ty === 'Collections' || isCorporateProfile();
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.termination.reset(state.wizard, ty);
            }
        };

        const bssCgtOptions = () => ({
            outstandingBalance: selectedLineOutstandingBalance.value,
        });

        const bssTkoOptions = () => ({
            outstandingBalance: selectedLineOutstandingBalance.value,
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
        const susRequiresStep2Identity = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.suspension.requiresIdentityUpload(state.wizard.susSuspensionType)
                : false);

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
                : 'bss.payoutDestination');

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
            const bal = selectedLineOutstandingBalance.value;
            return bal != null && bal < 0;
        });

        const onSimLostOrStolenChange = () => {
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.simSwap.reset(state.wizard, state.wizard.simLostOrStolen);
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
        const onCgtPathChange = () => {
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.changeGsm.reset(state.wizard, state.wizard.cgtMigrationPath);
            }
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
                offerDetail: null,
                offerDetailBusy: false,
                vasCatalog: [],
                catalogBusy: false,
                selectedVasCode: '',
                vasAction: 'Activate',
                vasActivated: false,
                simIccid: '',
                simReplacementReason: '',
                simLostOrStolen: false,
                cnTargetMsisdnAssetId: '',
                cnNumberChangeReason: '',
                cnPremiumFeeAmount: '',
                cnChangeMode: 'Internal',
                cnPortInMsisdn: '',
                cnDonorOperatorCode: '',
                cnPortInReference: '',
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
                bssPaymentReference: '',
                bssSecurityTicketId: '',
                bssDocumentNumber: '',
                bssRegulatoryFile: null,
                bssIdentityFile: null,
                bssKycDocumentReferenceId: '',
                bssOriginalTransactionRef: '',
                bssPayoutDestination: '',
                cgtMigrationPath: 'Standard',
                cgtSourceTypeCode: '',
                cgtSourceTypeId: '',
                susAutoReconnectEnabled: false,
                susEndDateLocal: '',
                susEndDateValidationError: '',
                susRequiresBackOffice: false,
                rcnReconnectReason: '',
                rcnClearanceType: 'Customer',
                rcnPaymentReference: '',
                rcnSecurityTicketId: '',
                rcnDocumentNumber: '',
                rcnRegulatoryFile: null,
                rcnKycDocumentReferenceId: '',
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
                bdrSupervisorConfirmed: false,
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
                confirmScheduled: false,
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
                paymentCashierLocked: false,
                cashierFetchBusy: false,
                operationCorrelationId: '',
                falloutTicketId: '',
                falloutTicketNumber: '',
                kycDocumentReferenceId: '',
                activateIdentityFile: null,
                kycUploadBusy: false,
                kycUploadError: '',
                activationLineTypeId: '',
                activateSecondarySearchNationalId: '',
                activateSecondarySearchPhone: '',
                activateSecondaryResults: [],
                activateSecondaryProfileId: '',
                activateSecondaryLabel: '',
                activateSecondaryBusy: false,
                bssSupervisorConfirmed: false,
                bssEffectiveMode: 'immediate',
                bssEffectiveDateLocal: '',
                mgrProrationPreview: null,
                mgrProrationBusy: false,
                supportIssueType: 0,
                supportNotes: '',
                supportTicketCreated: false,
                supportTicketId: '',
                supportTicketNumber: '',
            };
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.effectiveDate.reset(state.wizard);
            }
            if (kind === 'activate' && typeof ActivationChannelUi !== 'undefined') {
                ActivationChannelUi.applyDefaults(state.wizard);
            }
        };

        const SUSPENSION_MAX_DAYS = 90;

        const suspensionStartDateLocal = () => {
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const iso = TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard);
                return iso.slice(0, 10);
            }
            const d = new Date();
            const y = d.getFullYear();
            const m = String(d.getMonth() + 1).padStart(2, '0');
            const day = String(d.getDate()).padStart(2, '0');
            return `${y}-${m}-${day}`;
        };

        const bssEffectiveTodayLocal = () =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.effectiveDate.todayLocal()
                : suspensionStartDateLocal();

        const wizardSupportsEffectiveDate = (kind) =>
            typeof TelecomBssWizardClearance !== 'undefined'
            && TelecomBssWizardClearance.effectiveDate.supportsWizardKind(kind);

        const suspensionMaxEndDateLocal = () =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.suspensionEndDate.maxEndDateLocal(suspensionStartDateLocal())
                : (() => {
                    const d = new Date();
                    d.setDate(d.getDate() + SUSPENSION_MAX_DAYS);
                    const y = d.getFullYear();
                    const m = String(d.getMonth() + 1).padStart(2, '0');
                    const day = String(d.getDate()).padStart(2, '0');
                    return `${y}-${m}-${day}`;
                })();

        const validateSuspensionEndDate = () => {
            state.wizard.susEndDateValidationError = '';
            if (!state.wizard.susAutoReconnectEnabled) return true;
            const end = (state.wizard.susEndDateLocal || '').trim();
            if (!end) {
                state.wizard.susEndDateValidationError = t360('suspension.endDateRequired', 'End date required');
                return false;
            }
            const start = suspensionStartDateLocal();
            const max = suspensionMaxEndDateLocal();
            if (end < start || end > max) {
                state.wizard.susEndDateValidationError = t360(
                    'suspension.endDateMaxExceeded',
                    'Maximum 90 days'
                );
                return false;
            }
            return true;
        };

        const onSusAutoReconnectChange = () => {
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.suspensionEndDate.onAutoReconnectToggled(
                    state.wizard,
                    suspensionStartDateLocal
                );
                return;
            }
            if (state.wizard.susAutoReconnectEnabled && !(state.wizard.susEndDateLocal || '').trim()) {
                const d = new Date();
                d.setDate(d.getDate() + 30);
                state.wizard.susEndDateLocal = d.toISOString().slice(0, 10);
            }
            if (!state.wizard.susAutoReconnectEnabled) {
                state.wizard.susEndDateLocal = '';
                state.wizard.susEndDateValidationError = '';
            }
        };

        const onSusTypeChange = () => {
            const ty = (state.wizard.susSuspensionType || '').trim();
            state.wizard.susRequiresBackOffice =
                ty === 'Fraud' || ty === 'Regulatory' || isCorporateProfile();
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.suspension.reset(state.wizard, ty);
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
            } catch {
                state.wizard.rcnEligibility = null;
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
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.refund.reset(
                    state.wizard,
                    state.wizard.rfdRefundType,
                    state.wizard.rfdRefundMethod
                );
            }
        };

        const onRefundMethodChange = () => {
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.refund.reset(
                    state.wizard,
                    state.wizard.rfdRefundType,
                    state.wizard.rfdRefundMethod
                );
            }
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
                Swal.fire({ icon: 'warning', title: telecomT('swal.paymentAmountRequired', 'Payment required') });
                return;
            }
            state.wizard.paymentBusy = true;
            try {
                const res = await AxiosManager.post('/Telecom/RecordDeviceDownPayment', {
                    operationId: state.wizard.createdOperationId,
                    amountPaid: amount,
                    paymentChannel: Number(state.wizard.devPaymentChannel) || 0,
                    paymentReference: ref,
                });
                if (res?.data?.code === 200) {
                    state.wizard.documentMarkedUploaded = true;
                    Swal.fire({
                        icon: 'success',
                        title: telecomT('swal.paymentRecorded', 'Recorded'),
                        timer: 1200,
                        showConfirmButton: false,
                    });
                } else {
                    throw Object.assign(
                        new Error(res?.data?.message || telecomT('swal.genericFailed', 'Failed')),
                        { response: res }
                    );
                }
            } catch (e) {
                toastError(e, telecomT('swal.devicePaymentFailed', 'Device payment failed'));
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
            state.wizard.cnRequiresBackOffice =
                typeof TelecomWizardConfirm !== 'undefined'
                    ? TelecomWizardConfirm.resolveChangeNumberRequiresBackOffice(state.wizard)
                    : (state.wizard.cnChangeMode || 'Internal') === 'PortIn'
                        || isPremiumMsisdnCategory(
                            (state.wizard.cnPoolNumbers || []).find(
                                (r) => String(r.id ?? r.Id) === (state.wizard.cnTargetMsisdnAssetId || '').trim()
                            )?.category
                        );
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.changeNumber.reset(
                    state.wizard,
                    state.wizard.cnRequiresBackOffice
                );
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
            if (!portIn) {
                loadChangeNumberPoolForWizard();
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
                state.wizard.cgtSourceTypeId =
                    content.currentSubscriptionTypeId || content.CurrentSubscriptionTypeId || '';
                state.wizard.cgtSourceTypeCode =
                    content.currentSubscriptionTypeCode || content.CurrentSubscriptionTypeCode || '';
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
            state.wizard.simIccid = String(
                row?.iccid ?? row?.Iccid ?? row?.pairedIccid ?? row?.PairedIccid ?? ''
            ).trim();
            state.wizard.kycDocumentReferenceId = '';
            state.wizard.kycUploadError = '';
        };

        const getActivatePoolMsisdn = () => {
            const msisdn = (state.wizard.selectedPoolMsisdn || '').trim();
            if (msisdn) return msisdn;
            const id = (state.wizard.msisdnAssetId || '').trim();
            const row = (state.wizard.poolNumbers || []).find((a) => a.id === id);
            return (row?.msisdn ?? row?.Msisdn ?? '').trim();
        };

        const uploadC360ActivateKyc = async (file, msisdn) => {
            if (typeof TelecomBssWizardClearance !== 'undefined' && TelecomBssWizardClearance.uploadKyc) {
                return TelecomBssWizardClearance.uploadKyc(msisdn, file);
            }
            if (typeof TelecomReconnectClearance !== 'undefined' && TelecomReconnectClearance.uploadRegulatoryAttachment) {
                return TelecomReconnectClearance.uploadRegulatoryAttachment(msisdn, file);
            }
            throw new Error('wizard.kycUpload.failed');
        };

        const onActivateKycFileChange = async (ev) => {
            const file = ev?.target?.files?.[0] || null;
            state.wizard.activateIdentityFile = file;
            state.wizard.kycDocumentReferenceId = '';
            state.wizard.kycUploadError = '';
            if (!file) return;
            const msisdn = getActivatePoolMsisdn();
            if (!msisdn) {
                state.wizard.kycUploadError = t360('wizard.kycUpload.msisdnRequired', 'Select MSISDN first');
                return;
            }
            state.wizard.kycUploadBusy = true;
            try {
                state.wizard.kycDocumentReferenceId = await uploadC360ActivateKyc(file, msisdn);
            } catch (e) {
                state.wizard.kycUploadError = t360(e?.message || 'wizard.kycUpload.failed', 'Upload failed');
            } finally {
                state.wizard.kycUploadBusy = false;
            }
        };

        const ensureC360ActivateKyc = async () => {
            if ((state.wizard.kycDocumentReferenceId || '').trim()) return true;
            const file = state.wizard.activateIdentityFile;
            if (!file) {
                Swal.fire({
                    icon: 'warning',
                    title: t360('wizard.kycUpload.title', 'KYC required'),
                    text: t360('wizard.kycUpload.required', 'Upload document'),
                });
                return false;
            }
            const msisdn = getActivatePoolMsisdn();
            if (!msisdn) {
                Swal.fire({ icon: 'warning', title: telecomT('customerList.swal.newLineIncomplete', 'Incomplete') });
                return false;
            }
            state.wizard.kycUploadBusy = true;
            try {
                state.wizard.kycDocumentReferenceId = await uploadC360ActivateKyc(file, msisdn);
                return true;
            } catch (e) {
                state.wizard.kycUploadError = t360(e?.message || 'wizard.kycUpload.failed', 'Upload failed');
                Swal.fire({ icon: 'warning', text: state.wizard.kycUploadError });
                return false;
            } finally {
                state.wizard.kycUploadBusy = false;
            }
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

        const loadActivatePoolData = async () => {
            state.wizard.poolBusy = true;
            try {
                const poolRes = await AxiosManager.get('/Telecom/GetMsisdnAssetPoolList?status=Available', {});
                state.wizard.poolNumbers = parseMsisdnPoolRows(poolRes).filter(isAvailableMsisdnPoolRow);
                state.wizard.offerings = [];
            } catch {
                state.wizard.poolNumbers = [];
                state.wizard.offerings = [];
            } finally {
                state.wizard.poolBusy = false;
            }
        };

        const requireLine = () => {
            if (lineOptions.value.length) return true;
            Swal.fire({ icon: 'warning', title: telecomT('swal.noActiveLine', 'No active line') });
            return false;
        };

        const onWizardLineChanged = async () => {
            bindPrimaryFromLine();
            const sub = selectedSubscription.value;
            if (sub) {
                await checkHlr(sub);
            }
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

        const openProvisioningWizard = async (kind, options = {}) => {
            const permMap = {
                takeover: () => can.value.takeover,
                migrate: () => can.value.provisioning,
                changeGsm: () => can.value.changeGsm,
                changeNumber: () => can.value.changeNumber,
                termination: () => can.value.termination && !selectedLineTerminated.value,
                suspension: () => can.value.suspension && selectedLineActive.value,
                reconnect: () => (can.value.reconnect || options.bypassToAdvance) && selectedLineSuspended.value,
                refund: () => can.value.refund && !selectedLineTerminated.value,
                badDebt: () => can.value.collection && selectedLineCollectionEligible.value,
                deviceSale: () => can.value.deviceSale,
                addpackage: () => can.value.provisioning,
                simswap: () => can.value.network,
                activate: () => can.value.network,
                support: () => can.value.supportTicket,
            };
            if (!permMap[kind]?.()) {
                Swal.fire({ icon: 'info', title: telecomT('swal.noPermission', 'No permission') });
                return;
            }
            if (kind !== 'activate' && kind !== 'support' && !requireLine()) return;
            ensureLineSelected();
            state.wizard.kind = kind;
            resetWizardState();
            state.wizard.bypassToAdvance = !!options.bypassToAdvance;
            if (kind === 'support') {
                if (!bindPrimaryFromLine()) {
                    const subs = allSubscriptions.value;
                    const p = subs.find((s) => s.isPrimaryLine) || subs[0];
                    if (!(p?.msisdn || '').trim()) {
                        Swal.fire({
                            icon: 'warning',
                            title: telecomT('customerList.supportTicket.noMsisdn', 'No MSISDN'),
                        });
                        return;
                    }
                    state.wizard.primarySubscriberProfileId = p.subscriberProfileId || '';
                    state.wizard.primaryMsisdnAssetId = p.msisdnAssetId || '';
                    state.wizard.primaryLabel = p.msisdn || '—';
                }
            } else if (kind !== 'activate' && !bindPrimaryFromLine()) {
                Swal.fire({ icon: 'warning', title: telecomT('swal.subscriptionLinkFailed', 'Link failed') });
                return;
            }
            if (kind === 'activate') {
                if (typeof ActivationChannelUi !== 'undefined') {
                    await ActivationChannelUi.ensureLoaded();
                    ActivationChannelUi.applyDefaults(state.wizard);
                }
                const subs = allSubscriptions.value;
                const p = subs.find((s) => s.isPrimaryLine) || subs[0];
                if (!p?.subscriberProfileId) {
                    Swal.fire({ icon: 'warning', title: telecomT('swal.noCustomerProfile', 'No profile') });
                    return;
                }
                state.wizard.primarySubscriberProfileId = p.subscriberProfileId;
                state.wizard.primaryLabel =
                    state.profile?.core?.displayName || telecomT('swal.currentSubscriber', 'Subscriber');
                await loadTelecomLineTypes();
                await loadActivatePoolData();
                const def = activationLineTypeOptions.value.find((x) => x.isDefault || x.IsDefault)
                    || activationLineTypeOptions.value[0];
                if (def?.id) {
                    state.wizard.activationLineTypeId = def.id;
                    await loadActivateOfferings();
                }
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
                const line = parseSelectedLine();
                const msisdn = (line?.msisdn || state.wizard.primaryLabel || '').trim();
                if (msisdn === '0939000002') {
                    state.wizard.rcnClearanceType = 'Payment';
                    state.wizard.rcnPaymentReference = 'RCPT-2002';
                    state.wizard.rcnReconnectReason = 'LateBillPayment';
                } else if (selectedLineBdrApproved.value) {
                    state.wizard.rcnClearanceType = 'Payment';
                }
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

        const releaseMsisdnReservation = async (assetId) => {
            const aid = (assetId || '').trim();
            const cid = (state.customerId || '').trim();
            if (!aid || !cid) return;
            try {
                await AxiosManager.post('/Telecom/ReleaseMsisdnReservation', {
                    msisdnAssetId: aid,
                    customerId: cid,
                });
            } catch {
                /* best-effort */
            }
        };

        const releaseWizardMsisdnReservationsIfAny = async () => {
            if ((state.wizard.createdOperationId || '').trim()) return;
            if (state.wizard.submitBusy || state.isProvisioningInFlight) return;
            if (state.wizard.kind === 'activate' && state.wizard.msisdnAssetId) {
                await releaseMsisdnReservation(state.wizard.msisdnAssetId);
            }
            if (state.wizard.kind === 'changeNumber' && state.wizard.cnTargetMsisdnAssetId) {
                await releaseMsisdnReservation(state.wizard.cnTargetMsisdnAssetId);
            }
        };

        const closeProvisioningWizard = async () => {
            await releaseWizardMsisdnReservationsIfAny();
            provWizardModal?.hide();
            resetWizardState();
            state.wizard.kind = '';
        };

        const finishProvisioningWizard = async () => {
            const wasReconnect = state.wizard.kind === 'reconnect' && state.wizard.confirmed;
            const wasSupport = state.wizard.kind === 'support' && state.wizard.supportTicketCreated;
            const reconnectSub = wasReconnect ? selectedSubscription.value : null;
            closeProvisioningWizard();
            if (wasSupport) {
                state.activeTab = 'tickets';
            } else {
                state.activeTab = 'services';
            }
            await loadProfile(true);
            await loadCbs();
            if (reconnectSub) {
                await checkHlr(reconnectSub);
            }
        };

        const searchTakeoverTarget = async () => {
            const nat = (state.wizard.takeoverSearchNationalId || '').trim();
            const ph = (state.wizard.takeoverSearchPhone || '').trim();
            if (nat.length < 2 && ph.length < 2) {
                Swal.fire({ icon: 'info', title: telecomT('swal.searchMinChars', 'Search hint') });
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
                toastError(e, telecomT('customerList.swal.searchFailed', 'Search failed'));
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
                    Swal.fire({
                        icon: 'warning',
                        title: telecomT('customerList.swal.noSubscriberProfile', 'No profile'),
                    });
                } else {
                    state.wizard.takeoverTargetLabel = `${c.name || '—'} (${pid.slice(0, 8)}…)`;
                }
            } catch (e) {
                toastError(e, telecomT('swal.loadFailed', 'Load failed'));
            } finally {
                state.wizard.takeoverBusy = false;
            }
        };

        const validateWizardStep1 = async () => {
            const k = state.wizard.kind;
            const warn = (key, fb) => {
                Swal.fire({
                    icon: 'warning',
                    title: telecomT('swal.incompleteTitle', 'Incomplete'),
                    text: telecomT(key, fb),
                });
                return false;
            };
            const deny = (msg) => {
                Swal.fire({
                    icon: 'error',
                    title: telecomT('reconnect.matrixDenied', 'Denied'),
                    text: msg,
                });
                return false;
            };
            if (k !== 'activate' && !(state.wizard.primarySubscriberProfileId || '').trim()) {
                return warn('swal.subscriptionLinkFailed', 'No subscription');
            }
            if (k === 'takeover' && !(state.wizard.takeoverTargetProfileId || '').trim()) {
                return warn('wizard.secondaryRequiredHint', 'Select new owner');
            }
            if (k === 'takeover') {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    const tkoErr = TelecomBssWizardClearance.takeOver.validate(state.wizard, bssTkoOptions());
                    if (tkoErr) return warn(tkoErr.key, telecomT(tkoErr.key, tkoErr.key));
                } else if (!(state.wizard.takeoverTransferReason || '').trim()) {
                    return warn('takeOver.transferReason', 'Transfer reason required');
                }
            }
            if (k === 'migrate' && !(state.wizard.selectedOfferingId || '').trim()) {
                return warn('wizard.targetOffer', 'Pick package');
            }
            if (k === 'migrate' && typeof TelecomBssWizardClearance !== 'undefined') {
                const prErr = TelecomBssWizardClearance.migration.validatePreview(
                    state.wizard.mgrProrationPreview
                );
                if (prErr) return warn(prErr.key, telecomT(prErr.key, telecomT('wizard.migrationOffers.prorationInsufficient', 'Insufficient balance')));
            }
            if (k === 'changeGsm') {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    const cgtErr = TelecomBssWizardClearance.changeGsm.validate(
                        state.wizard,
                        bssCgtOptions()
                    );
                    if (cgtErr) return warn(cgtErr.key, telecomT(cgtErr.key, cgtErr.key));
                } else {
                    if (!(state.wizard.cgtTargetTypeId || '').trim()) {
                        return warn('changeGsm.targetType', 'Pick line type');
                    }
                    if (!(state.wizard.cgtMigrationReason || '').trim()) {
                        return warn('changeGsm.migrationReason', 'Reason required');
                    }
                }
            }
            if (k === 'addpackage' && !(state.wizard.selectedVasCode || '').trim()) {
                return warn('wizard.vasPlaceholder', 'Pick VAS');
            }
            if (k === 'changeNumber') {
                if (cnShowsInternalPool.value && !(state.wizard.cnTargetMsisdnAssetId || '').trim()) {
                    return warn('changeNumber.targetMsisdn', 'Pick number');
                }
                if (!(state.wizard.cnNumberChangeReason || '').trim()) {
                    return warn('changeNumber.changeReason', 'Pick reason');
                }
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    const cnErr = TelecomBssWizardClearance.changeNumber.validate(state.wizard);
                    if (cnErr) return warn(cnErr.key, telecomT(cnErr.key, cnErr.key));
                }
                if (state.wizard.cnRequiresBackOffice && !state.wizard.identityFile) {
                    return warn('changeNumber.paymentDocHint', 'Upload receipt');
                }
            }
            if (k === 'termination') {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    const trmErr = TelecomBssWizardClearance.termination.validate(state.wizard);
                    if (trmErr) return warn(trmErr.key, telecomT(trmErr.key, trmErr.key));
                } else {
                    if (!(state.wizard.trmTerminationReason || '').trim()) {
                        return warn('customerList.swal.terminationIncomplete', 'Termination incomplete');
                    }
                }
                if (
                    trmRequiresLegacyIdentity.value
                    && state.wizard.trmRequiresBackOffice
                    && !state.wizard.identityFile
                ) {
                    return warn('termination.identityRequired', 'Document required');
                }
            }
            if (k === 'simswap') {
                if (!(state.wizard.simReplacementReason || '').trim()) {
                    return warn('customerList.swal.pickSimReason', 'Pick reason');
                }
                if ((state.wizard.simIccid || '').trim().length < 19) {
                    return warn('customerList.swal.enterNewIccid', 'ICCID required');
                }
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    const simErr = TelecomBssWizardClearance.simSwap.validate(state.wizard);
                    if (simErr) return warn(simErr.key, telecomT(simErr.key, simErr.key));
                } else if (state.wizard.simLostOrStolen && !state.wizard.identityFile) {
                    return warn('customerList.swal.uploadIdentity', 'Upload ID');
                }
            }
            if (k === 'activate') {
                const lineTypeId = (state.wizard.activationLineTypeId || '').trim();
                const assetId = (state.wizard.msisdnAssetId || '').trim();
                const offeringId = (state.wizard.selectedOfferingId || '').trim();
                const iccid = (state.wizard.simIccid || '').trim();
                if (!lineTypeId) {
                    return warn('wizard.lineType.required', 'Select line type');
                }
                if (!assetId || !offeringId || iccid.length < 19) {
                    return warn('customerList.swal.newLineIncomplete', 'Incomplete activation');
                }
                if (isCorporate.value && !(state.wizard.activateSecondaryProfileId || '').trim()) {
                    return warn('wizard.corporateSecondPartyRequired', 'Select second party');
                }
                if (!(state.wizard.kycDocumentReferenceId || '').trim() && !state.wizard.activateIdentityFile) {
                    return warn('wizard.kycUpload.required', 'KYC document required');
                }
            }
            if (wizardSupportsEffectiveDate(k)) {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    const effErr = TelecomBssWizardClearance.effectiveDate.validate(state.wizard);
                    if (effErr) return warn(effErr.key, telecomT(effErr.key, effErr.key));
                }
            }
            if (k === 'suspension') {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    const susErr = TelecomBssWizardClearance.suspension.validate(state.wizard);
                    if (susErr) return warn(susErr.key, telecomT(susErr.key, susErr.key));
                } else if (!(state.wizard.susSuspensionReason || '').trim()) {
                    return warn('suspension.suspensionReason', 'Reason required');
                }
                if (!validateSuspensionEndDate()) {
                    return warn(
                        'suspension.endDateMaxExceeded',
                        state.wizard.susEndDateValidationError || 'Invalid end date'
                    );
                }
                onSusTypeChange();
            }
            if (k === 'reconnect') {
                if (typeof TelecomReconnectClearance !== 'undefined') {
                    const rcnErr = TelecomReconnectClearance.validate(state.wizard, {
                        bdrApproved: selectedLineBdrApproved.value,
                    });
                    if (rcnErr) return warn(rcnErr.key, telecomT(rcnErr.key, rcnErr.key));
                } else {
                    if (selectedLineBdrApproved.value && !(state.wizard.rcnPaymentReference || '').trim()) {
                        return warn('reconnect.paymentReference', 'Receipt reference is mandatory for BDR clearance');
                    }
                    if (!(state.wizard.rcnReconnectReason || '').trim()) {
                        return warn('reconnect.reconnectReason', 'Reason required');
                    }
                    if (state.wizard.rcnClearanceType === 'Payment' && !(state.wizard.rcnPaymentReference || '').trim()) {
                        return warn('reconnect.paymentReference', 'Payment ref required');
                    }
                }
                await loadReconnectEligibility();
                if (
                    state.wizard.rcnEligibility
                    && !state.wizard.rcnEligibility.allowed
                    && !state.wizard.rcnEligibility.Allowed
                ) {
                    const msg =
                        state.wizard.rcnEligibility.messageAr || state.wizard.rcnEligibility.MessageAr || '';
                    return deny(msg);
                }
            }
            if (k === 'refund') {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    const rfdErr = TelecomBssWizardClearance.refund.validate(state.wizard);
                    if (rfdErr) return warn(rfdErr.key, telecomT(rfdErr.key, rfdErr.key));
                } else {
                    if (!(state.wizard.rfdRefundReason || '').trim()) {
                        return warn('refund.refundReason', 'Reason required');
                    }
                    const amt = Number(state.wizard.rfdRefundAmount);
                    if (!amt || amt <= 0) {
                        return warn('refund.refundAmount', 'Amount required');
                    }
                }
            }
            if (k === 'badDebt') {
                if (state.wizard.bdrCollectionAction === 'PaymentRecorded') {
                    if (!(state.wizard.bdrPaymentReference || '').trim()) {
                        return warn('badDebt.paymentReference', 'Payment ref required');
                    }
                    const col = Number(state.wizard.bdrCollectedAmount);
                    if (!col || col <= 0) {
                        return warn('badDebt.collectedAmount', 'Amount required');
                    }
                }
                if (
                    (state.wizard.bdrCollectionAction === 'WriteOffPartial'
                        || state.wizard.bdrCollectionAction === 'WriteOffFull')
                    && !(Number(state.wizard.bdrWriteOffAmount) > 0)
                ) {
                    return warn('badDebt.writeOffAmount', 'Write-off required');
                }
                await loadBadDebtEligibility();
                if (
                    state.wizard.bdrEligibility
                    && !state.wizard.bdrEligibility.allowed
                    && !state.wizard.bdrEligibility.Allowed
                ) {
                    const msg =
                        state.wizard.bdrEligibility.messageAr || state.wizard.bdrEligibility.MessageAr || '';
                    return deny(msg);
                }
            }
            if (k === 'deviceSale') {
                if (!(state.wizard.devInventoryId || '').trim()) {
                    return warn('deviceSale.deviceImei', 'Pick device');
                }
                if (state.wizard.devSaleType === 'Installment' && !(state.wizard.devInstallmentPlanId || '').trim()) {
                    return warn('deviceSale.installmentPlan', 'Pick plan');
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
            };

            if (k === 'takeover') {
                body.secondarySubscriberProfileId = state.wizard.takeoverTargetProfileId;
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.transferReason = (state.wizard.takeoverTransferReason || '').trim();
                body.depositTransferPolicy = Number(state.wizard.takeoverDepositPolicy) || 1;
                const tkoApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.takeOver.buildApi(state.wizard, bssTkoOptions())
                    : {};
                body.paymentReference = tkoApi.paymentReference ?? null;
                body.takeOverObligationStatus = tkoApi.takeOverObligationStatus ?? null;
                body.takeOverEffectiveDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                        : new Date().toISOString();
            } else if (k === 'migrate') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.productOfferingId = state.wizard.selectedOfferingId;
                body.migrationEffectiveDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                        : new Date().toISOString();
            } else if (k === 'changeGsm') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.targetSubscriptionTypeId = state.wizard.cgtTargetTypeId;
                body.gsmMigrationReason = (state.wizard.cgtMigrationReason || '').trim();
                const off = (state.wizard.selectedOfferingId || '').trim();
                if (off) body.changeGsmProductOfferingId = off;
                const cgtApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.changeGsm.buildApi(state.wizard, bssCgtOptions())
                    : {};
                body.paymentReference = cgtApi.paymentReference ?? null;
                body.collectionNote = cgtApi.collectionNote ?? null;
                body.kycDocumentReferenceId = cgtApi.kycDocumentReferenceId ?? null;
                body.gsmEffectiveDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                        : new Date().toISOString();
            } else if (k === 'simswap') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.simIccid = (state.wizard.simIccid || '').trim();
                body.replacementReason = (state.wizard.simReplacementReason || '').trim();
                body.isLostOrStolenReport = !!state.wizard.simLostOrStolen;
                const simApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.simSwap.buildApi(state.wizard)
                    : {};
                body.agencyReference = simApi.agencyReference ?? null;
                body.simSwapEffectiveDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                        : new Date().toISOString();
            } else if (k === 'changeNumber') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.numberChangeMode = state.wizard.cnChangeMode || 'Internal';
                if (cnShowsInternalPool.value) {
                    body.targetMsisdnAssetId = state.wizard.cnTargetMsisdnAssetId || null;
                } else {
                    body.portInMsisdn = (state.wizard.cnPortInMsisdn || '').trim() || null;
                    body.donorOperatorCode = (state.wizard.cnDonorOperatorCode || '').trim() || null;
                }
                body.numberChangeReason = (state.wizard.cnNumberChangeReason || '').trim();
                if (state.wizard.cnPremiumFeeAmount) {
                    body.premiumFeeAmount = Number(state.wizard.cnPremiumFeeAmount);
                }
                const cnApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.changeNumber.buildApi(state.wizard)
                    : {};
                body.paymentReference = cnApi.paymentReference ?? null;
                body.agencyReference = cnApi.agencyReference ?? null;
                body.numberChangeEffectiveDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                        : new Date().toISOString();
            } else if (k === 'termination') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.terminationType = (state.wizard.trmTerminationType || '').trim();
                body.terminationReason = (state.wizard.trmTerminationReason || '').trim();
                if (state.wizard.trmTerminationType === 'Voluntary') {
                    body.retentionOfferOutcome = (state.wizard.trmRetentionOfferOutcome || '').trim();
                }
                const trmApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.termination.buildApi(state.wizard)
                    : {};
                body.paymentReference = trmApi.paymentReference ?? null;
                body.agencyReference = trmApi.agencyReference ?? null;
                body.collectionNote = trmApi.collectionNote ?? null;
                body.kycDocumentReferenceId = trmApi.kycDocumentReferenceId ?? null;
                body.terminationEffectiveDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                        : new Date().toISOString();
            } else if (k === 'addpackage') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                const vas = state.wizard.vasCatalog.find((v) => v.serviceCode === state.wizard.selectedVasCode);
                body.targetOfferName = vas?.nameAr || state.wizard.selectedVasCode;
                body.notes = `${notes}|VAS:${state.wizard.selectedVasCode}`;
            } else if (k === 'activate') {
                body.msisdnAssetId = state.wizard.msisdnAssetId;
                body.productOfferingId = state.wizard.selectedOfferingId;
                body.targetSubscriptionTypeId = (state.wizard.activationLineTypeId || '').trim() || null;
                body.secondarySubscriberProfileId =
                    (state.wizard.activateSecondaryProfileId || '').trim() || null;
                body.simIccid = (state.wizard.simIccid || '').trim();
                body.kycDocumentReferenceId = (state.wizard.kycDocumentReferenceId || '').trim() || null;
                body.activationChannel = Number(state.wizard.activationChannel) || 0;
                const dc = (state.wizard.dealerCode || '').trim();
                if (dc) body.dealerCode = dc;
                body.activationEffectiveDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                        : new Date().toISOString();
            } else if (k === 'suspension') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.suspensionType = (state.wizard.susSuspensionType || '').trim() || null;
                body.suspensionReason = (state.wizard.susSuspensionReason || '').trim() || null;
                body.barringLevel = (state.wizard.susBarringLevel || 'Full').trim();
                body.autoReconnectEnabled = !!state.wizard.susAutoReconnectEnabled;
                body.suspensionStartDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                        : new Date(suspensionStartDateLocal()).toISOString();
                body.suspensionEndDateUtc =
                    state.wizard.susAutoReconnectEnabled && state.wizard.susEndDateLocal
                        ? new Date(state.wizard.susEndDateLocal + 'T23:59:59').toISOString()
                        : null;
                const susApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.suspension.buildApi(state.wizard)
                    : {};
                body.paymentReference = susApi.paymentReference ?? null;
                body.agencyReference = susApi.agencyReference ?? null;
                body.collectionNote = susApi.collectionNote ?? null;
                body.kycDocumentReferenceId = susApi.kycDocumentReferenceId ?? null;
                body.fraudClearanceConfirmed = susApi.fraudClearanceConfirmed ?? false;
            } else if (k === 'reconnect') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.reconnectReason = (state.wizard.rcnReconnectReason || '').trim() || null;
                body.clearanceType = (state.wizard.rcnClearanceType || '').trim() || null;
                const rcnFields = typeof TelecomReconnectClearance !== 'undefined'
                    ? TelecomReconnectClearance.buildApiFields(state.wizard, { bdrApproved: selectedLineBdrApproved.value })
                    : {
                        paymentReference: (state.wizard.rcnPaymentReference || '').trim() || null,
                        agencyReference: null,
                        collectionNote: null,
                        kycDocumentReferenceId: null,
                        fraudClearanceConfirmed: !!state.wizard.rcnFraudClearanceConfirmed,
                    };
                body.fraudClearanceConfirmed = rcnFields.fraudClearanceConfirmed;
                body.paymentReference = rcnFields.paymentReference;
                body.agencyReference = rcnFields.agencyReference;
                body.collectionNote = rcnFields.collectionNote;
                body.kycDocumentReferenceId = rcnFields.kycDocumentReferenceId;
                
                // GLOBAL HARDENING: Route B Bypass to Advance
                if (state.wizard.bypassToAdvance) {
                    body.notes = `${notes}|BypassToAdvance:true`;
                    body.status = 9; // Paid_Pending_BackOffice_Clearance
                }
                body.reconnectEffectiveDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                        : new Date().toISOString();
            } else if (k === 'refund') {
                body.msisdnAssetId = state.wizard.primaryMsisdnAssetId || null;
                body.refundType = (state.wizard.rfdRefundType || '').trim() || null;
                body.refundMethod = (state.wizard.rfdRefundMethod || '').trim() || null;
                body.refundReason = (state.wizard.rfdRefundReason || '').trim() || null;
                body.refundAmount = state.wizard.rfdRefundAmount ? Number(state.wizard.rfdRefundAmount) : null;
                const rfdApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.refund.buildApi(state.wizard)
                    : {};
                body.refundCbsReference = rfdApi.refundCbsReference ?? null;
                body.refundGatewayReference = rfdApi.refundGatewayReference ?? null;
                body.refundEffectiveDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                        : new Date().toISOString();
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
                body.collectionApprovalConfirmed = !!state.wizard.bdrSupervisorConfirmed;
                body.badDebtEffectiveDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                        : new Date().toISOString();
            } else if (k === 'deviceSale') {
                body.deviceInventoryId = state.wizard.devInventoryId || null;
                body.deviceSaleType = state.wizard.devSaleType === 'Installment' ? 1 : 0;
                if (state.wizard.devSaleType === 'Installment') {
                    body.deviceInstallmentPlanId = state.wizard.devInstallmentPlanId || null;
                }
                body.deviceSaleEffectiveDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(state.wizard)
                        : new Date().toISOString();
            }
            return body;
        };

        const fetchPaymentFromCashier = async () => {
            const ref = (state.wizard.paymentReference || '').trim();
            if (!ref) {
                Swal.fire({ icon: 'warning', title: telecomT('swal.paymentRefRequired', 'Payment ref required') });
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
                    toastSuccess(data.messageAr ?? data.MessageAr ?? telecomT('wizardUi.cashierFetched', 'Loaded'));
                } else {
                    throw Object.assign(
                        new Error(res?.data?.message || telecomT('swal.genericFailed', 'Failed')),
                        { response: res }
                    );
                }
            } catch (e) {
                toastError(e, telecomT('swal.genericFailed', 'Failed'));
            } finally {
                state.wizard.cashierFetchBusy = false;
            }
        };

        const submitWizardRecordPayment = async () => {
            if (!state.wizard.createdOperationId) return;
            const ref = (state.wizard.paymentReference || '').trim();
            const amt = Number(state.wizard.paymentAmount);
            if (!ref || !(amt > 0)) {
                Swal.fire({ icon: 'warning', title: telecomT('swal.paymentAmountRequired', 'Payment required') });
                return;
            }
            if (activateRequiredDeposit.value > 0 && !state.wizard.paymentCashierLocked) {
                Swal.fire({
                    icon: 'warning',
                    title: telecomT('wizardUi.fetchFromCashier', 'Fetch from cashier'),
                    text: telecomT('wizardUi.cashierFetched', 'Load payment from cashier first'),
                });
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
                    Swal.fire({
                        icon: 'success',
                        title: telecomT('swal.paymentRecorded', 'Recorded'),
                        timer: 1200,
                        showConfirmButton: false,
                    });
                } else {
                    throw Object.assign(
                        new Error(res?.data?.message || telecomT('swal.genericFailed', 'Failed')),
                        { response: res }
                    );
                }
            } catch (e) {
                toastError(e, telecomT('swal.devicePaymentFailed', 'Payment failed'));
            } finally {
                state.wizard.paymentBusy = false;
            }
        };

        const submitWizardVasActivate = async () => {
            if (!(await validateWizardStep1())) return;
            const line = parseSelectedLine();
            const msisdn = (line?.msisdn || '').trim();
            const code = (state.wizard.selectedVasCode || '').trim();
            if (!msisdn || !code) return;
            state.wizard.submitBusy = true;
            state.isProvisioningInFlight = true;
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
                    throw new Error(telecomT('swal.genericFailed', 'Failed'));
                }
                state.wizard.vasActivated = true;
                state.wizard.createdOperationNumber = result.operationNumber || '';
                const vasOk = telecomT('swal.vasActivated', 'VAS activated');
                state.wizard.confirmStatusHint =
                    vasOk +
                    (state.wizard.createdOperationNumber
                        ? ` (${state.wizard.createdOperationNumber})`
                        : '');
                Swal.fire({
                    icon: 'success',
                    title: telecomT('swal.vasActivateTitle', 'Activated'),
                    text: state.wizard.confirmStatusHint,
                    timer: 2500,
                    showConfirmButton: false,
                });
                state.wizard.step = 3;
            } catch (e) {
                const isBrv =
                    typeof TelecomVasToggle !== 'undefined'
                        ? TelecomVasToggle.isBusinessRuleViolation(e)
                        : e?.response?.data?.error?.name === 'BusinessRuleViolationException';
                const msg =
                    typeof TelecomVasToggle !== 'undefined'
                        ? TelecomVasToggle.pickError(e)
                        : e?.response?.data?.error?.message ??
                          e?.response?.data?.message ??
                          e?.message ??
                          '';
                Swal.fire({
                    icon: isBrv ? 'warning' : 'error',
                    title: isBrv ? 'VAL-11' : telecomT('swal.vasActivateFailed', 'Failed'),
                    text: msg || telecomT('swal.vasActivateFailedHint', 'VAL-11'),
                });
            } finally {
                state.wizard.submitBusy = false;
                state.isProvisioningInFlight = false;
            }
        };

        const wizardStepNext = async () => {
            if (state.wizard.step === 1) {
                if (!(await validateWizardStep1())) return;
                if (state.wizard.kind === 'addpackage') {
                    submitWizardVasActivate();
                    return;
                }
                if (state.wizard.kind === 'support') {
                    await submitSupportFromC360();
                    return;
                }
                state.wizard.step = 2;
                return;
            }
            if (state.wizard.step === 2) {
                if (!canWizardFinishStep2.value) {
                    Swal.fire({
                        icon: 'info',
                        title: telecomT('swal.completeRegDocCbs', 'Complete steps'),
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
            if (!(await validateWizardStep1())) return;
            if (state.wizard.kind === 'changeNumber' && state.wizard.cnTargetMsisdnAssetId) {
                try {
                    await AxiosManager.post('/Telecom/ReserveMsisdnForCustomer', {
                        msisdnAssetId: state.wizard.cnTargetMsisdnAssetId,
                        customerId: state.customerId,
                    });
                } catch (e) {
                    toastError(e, telecomT('swal.reserveNewNumberFailed', 'Reserve failed'));
                    return;
                }
            }
            if (state.wizard.kind === 'activate') {
                if (!(await ensureC360ActivateKyc())) return;
                try {
                    await AxiosManager.post('/Telecom/ReserveMsisdnForCustomer', {
                        msisdnAssetId: state.wizard.msisdnAssetId,
                        customerId: state.customerId,
                    });
                } catch (e) {
                    toastError(e, telecomT('swal.reserveMsisdnFailed', 'Reserve failed'));
                    return;
                }
            }
            state.wizard.submitBusy = true;
            state.isProvisioningInFlight = true;
            try {
                if (
                    state.wizard.kind === 'reconnect'
                    && typeof TelecomReconnectClearance !== 'undefined'
                ) {
                    const line = parseSelectedLine();
                    const msisdn = line?.msisdn || state.wizard.primaryLabel || '';
                    await TelecomReconnectClearance.ensureRegulatoryAttachmentUploaded(state.wizard, msisdn);
                }
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    const line = parseSelectedLine();
                    const msisdn = line?.msisdn || state.wizard.primaryLabel || '';
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
                const res = await AxiosManager.post('/Telecom/CreateTelecomOperation', buildWizardCreateBody());
                const ok = res?.data?.code === 200;
                const entity = res?.data?.content?.data;
                if (ok && entity?.id) {
                    state.wizard.createdOperationId = entity.id;
                    state.wizard.createdOperationNumber = entity.number || '';
                    if (typeof TelecomWizardConfirm !== 'undefined') {
                        TelecomWizardConfirm.syncBackOfficeFlagsFromEntity(state.wizard, entity);
                    }
                    if (state.wizard.kind === 'refund') {
                        state.wizard.rfdDepositSnapshot =
                            entity.depositBalanceSnapshot ?? entity.DepositBalanceSnapshot ?? null;
                        state.wizard.rfdWalletSnapshot =
                            entity.walletBalanceSnapshot ?? entity.WalletBalanceSnapshot ?? null;
                    }
                    if (state.wizard.kind === 'deviceSale') {
                        state.wizard.devFinancingPreview = entity.deviceFinancingNoteAr || entity.notes || '';
                        state.wizard.devDownPayment = String(
                            entity.deviceDownPaymentAmount ?? state.wizard.devDownPayment ?? ''
                        );
                    }
                    Swal.fire({
                        icon: 'success',
                        title: telecomT('swal.draftCreated', 'Draft created'),
                        html:
                            `<p class="mb-1">${state.wizard.createdOperationNumber}</p>` +
                            `<p class="small text-muted mb-0">${telecomT('swal.draftCreatedHint', '')}</p>`,
                        timer: 2800,
                        showConfirmButton: false,
                    });
                } else {
                    throw Object.assign(
                        new Error(res?.data?.message || telecomT('swal.draftCreateFailed', 'Create failed')),
                        { response: res }
                    );
                }
            } catch (e) {
                if (state.wizard.kind === 'activate' && state.wizard.msisdnAssetId) {
                    await releaseMsisdnReservation(state.wizard.msisdnAssetId);
                }
                if (state.wizard.kind === 'changeNumber' && state.wizard.cnTargetMsisdnAssetId) {
                    await releaseMsisdnReservation(state.wizard.cnTargetMsisdnAssetId);
                }
                toastError(e, telecomT('customerList.swal.operationFailed', 'Operation failed'));
            } finally {
                state.wizard.submitBusy = false;
                state.isProvisioningInFlight = false;
            }
        };

        const submitWizardMarkDocument = async () => {
            if (!state.wizard.createdOperationId) return;
            if (wizardRequiresStep2Identity.value && !state.wizard.identityFile) {
                Swal.fire({
                    icon: 'warning',
                    title: t360('suspension.kycRequired', 'Upload document'),
                    text: t360('suspension.kycDocumentHint', ''),
                });
                return;
            }
            state.wizard.uploadBusy = true;
            try {
                let res;
                if (
                    wizardRequiresStep2Identity.value
                    && typeof TelecomWizardConfirm !== 'undefined'
                ) {
                    res = await TelecomWizardConfirm.uploadOperationIdentityDocument(
                        state.wizard.createdOperationId,
                        state.wizard.identityFile
                    );
                } else if (wizardRequiresStep2Identity.value) {
                    const form = new FormData();
                    form.append('id', state.wizard.createdOperationId);
                    form.append('file', state.wizard.identityFile);
                    res = await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                } else {
                    res = await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', {
                        id: state.wizard.createdOperationId,
                    });
                }
                if (res?.data?.code === 200) {
                    state.wizard.documentMarkedUploaded = true;
                    const sentBo =
                        typeof TelecomWizardConfirm !== 'undefined'
                            ? TelecomWizardConfirm.uploadSuccessIsBackOffice(state.wizard)
                            : false;
                    const title = sentBo
                        ? telecomT('swal.sentToBackOffice', 'Sent to BO')
                        : telecomT('swal.documentRecorded', 'Document recorded');
                    Swal.fire({ icon: 'success', title, timer: 1400, showConfirmButton: false });
                } else {
                    throw Object.assign(
                        new Error(res?.data?.message || telecomT('swal.genericFailed', 'Failed')),
                        { response: res }
                    );
                }
            } catch (e) {
                toastError(e, telecomT('customerList.swal.operationFailed', 'Operation failed'));
            } finally {
                state.wizard.uploadBusy = false;
            }
        };

        const submitWizardConfirmCbs = async () => {
            if (!state.wizard.createdOperationId) return;
            if (
                typeof TelecomWizardConfirm !== 'undefined'
                && TelecomWizardConfirm.shouldSkipConfirm(state.wizard)
            ) {
                if (
                    !TelecomWizardConfirm.hasConfirmCbsPermission()
                    && typeof Swal !== 'undefined'
                ) {
                    Swal.fire({
                        icon: 'info',
                        title: telecomT('swal.notAllowed', 'Not allowed'),
                        text: telecomT('wizardUi.awaitBo', 'Awaiting back office'),
                    });
                }
                return;
            }
            state.wizard.confirmBusy = true;
            state.isProvisioningInFlight = true;
            try {
                const res = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                    id: state.wizard.createdOperationId,
                });
                if (res?.data?.code === 200) {
                    state.wizard.confirmed = true;
                    const content = res?.data?.content ?? res?.data?.Content ?? {};
                    const hint =
                        content.statusHintAr ??
                        content.StatusHintAr ??
                        telecomT('provSuccess', 'Operation recorded');
                    state.wizard.confirmStatusHint = hint;
                    const scheduled =
                        window.TelecomUiBadges?.isScheduledOperationStatus?.(
                            window.TelecomUiBadges?.operationStatusFromConfirm?.(content)
                        ) ?? false;
                    state.wizard.confirmScheduled = scheduled;
                    if (scheduled) {
                        if (typeof Swal !== 'undefined') {
                            Swal.fire({
                                icon: 'info',
                                title: telecomT('wizardUi.scheduledConfirmed', 'Scheduled'),
                                text: hint,
                                timer: 4200,
                                showConfirmButton: true,
                            });
                        }
                    } else {
                        toastSuccess(hint);
                    }
                    if (
                        !scheduled
                        && (content.hlrCompletesAsynchronously ?? content.HlrCompletesAsynchronously)
                    ) {
                        const polled = await pollOperationAfterConfirm(state.wizard.createdOperationId);
                        if (polled) {
                            state.wizard.confirmStatusHint = polled;
                            toastSuccess(polled);
                        }
                    }
                    if (state.wizard.kind === 'reconnect') {
                        const sub = selectedSubscription.value;
                        if (sub) {
                            await checkHlr(sub);
                        }
                    }
                } else {
                    throw Object.assign(
                        new Error(res?.data?.message || telecomT('customerList.swal.confirmFailed', 'Confirm failed')),
                        { response: res }
                    );
                }
            } catch (e) {
                toastError(e, telecomT('customerList.swal.confirmFailed', 'Confirm failed'));
            } finally {
                state.wizard.confirmBusy = false;
                state.isProvisioningInFlight = false;
            }
        };

        const refreshPageI18n = async () => {
            try {
                await window.TelecomI18n?.ensureLoaded?.();
                localeTick.value++;
                const title = telecomT('pageTitle', 'Subscriber 360');
                if (title) document.title = title;
                window.TelecomI18n?.refresh?.();
            } catch (_) { /* ignore */ }
        };

        const currentUiLang = () => window.TelecomI18n?.getLang?.() || 'ar';

        Vue.onMounted(async () => {
            document.documentElement.addEventListener('syriatel-locale-changed', refreshPageI18n);
            
            // GLOBAL HARDENING: BDR Real-time state polling
            const bdrPoll = setInterval(() => {
                if (selectedLineBdrPending.value) {
                    void loadProfileSupplements();
                }
            }, 10000);

            Vue.onUnmounted(() => {
                clearInterval(bdrPoll);
            });

            if (typeof ActivationChannelUi !== 'undefined') {
                await ActivationChannelUi.ensureLoaded();
            }
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
                if (!state.customerId) {
                    const missingMsg = telecomT(
                        'wizardUi.customer360OpenedWithoutId',
                        'تنبيه: تم فتح الصفحة بدون تحديد هوية العميل المستهدف.'
                    );
                    if (typeof Swal !== 'undefined') {
                        await Swal.fire({
                            icon: 'warning',
                            title: telecomT('swal.incompleteTitle', 'Incomplete'),
                            text: missingMsg,
                            confirmButtonColor: '#c8102e',
                        });
                    }
                    window.location.replace('/Telecom/TelecomHub');
                    return;
                }
                await loadProfile();
                await refreshPageI18n();
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
                state.loadError = telecomT('backOffice.dashboard.messages.accessDenied', 'Access denied');
            } finally {
                if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
            }
        });

        return {
            state,
            tabs,
            ui,
            telecomT,
            t360,
            localeTick,
            subscriptionTypeLabel,
            productOfferingDisplayName,
            subscriptionMetaLine,
            simTypeLabel,
            simStatusLabel,
            documentStatusLabel,
            subscriptionDocumentUrl,
            subscriptionBalanceLine,
            serviceLineTypeLabel,
            walletErrorMessage,
            formatBucketUnit,
            localizedGenderLabel,
            localizedLegalStatusLabel,
            localizedBillingModeLabel,
            localizedStatusReasonLabel,
            activeLinesLabel: Vue.computed(() => {
                localeTick.value;
                const tpl = telecomT('hero.activeLines', '{count} lines');
                return tpl.replace('{count}', String(activeLinesCount.value));
            }),
            can,
            isSupportAgent,
            selectedLineTerminated,
            selectedLineSuspended,
            selectedLineActive,
            selectedLineCollectionEligible,
            selectedLineBdrStatus,
            selectedLineBdrPending,
            selectedLineBdrApproved,
            selectedLineBdrAwaitingAudit,
            selectedLineOutstandingBalance,
            rcnShowsPaymentRef,
            rcnShowsFraudFields,
            rcnShowsRegulatoryFields,
            rcnShowsSimplePath,
            onTerminationTypeChange,
            onSusTypeChange,
            onSusAutoReconnectChange,
            onRefundMethodChange,
            onBssRegulatoryFileChange,
            onBssIdentityFileChange,
            onCgtPathChange,
            susShowsPayment,
            susShowsFraud,
            susShowsRegulatory,
            susShowsSimple,
            susRequiresStep2Identity,
            barringLevelOptions,
            trmShowsPayment,
            trmShowsFraud,
            trmShowsRegulatory,
            trmShowsVoluntary,
            trmRequiresLegacyIdentity,
            cgtShowsFinancial,
            cgtShowsRegulatory,
            rfdShowsOriginalTxRef,
            rfdShowsPayoutDestination,
            rfdPayoutLabelKey,
            simShowsLostStolenFields,
            cnShowsPremiumPayment,
            tkoShowsObligationSettlement,
            onSimLostOrStolenChange,
            suspensionStartDateLocal,
            suspensionMaxEndDateLocal,
            bssEffectiveTodayLocal,
            wizardSupportsEffectiveDate,
            activationLineTypeOptions,
            filteredActivatePoolNumbers,
            lineTypeDisplayName,
            onActivationLineTypeChange,
            searchActivateSecondary,
            selectActivateSecondary,
            mgrProrationPriceDifferenceLabel,
            mgrProrationAmountLabel,
            mgrProrationWalletLabel,
            mgrProrationDaysLabel,
            mgrProrationSufficient,
            validateSuspensionEndDate,
            onRcnClearanceChange,
            onRcnRegulatoryFileChange,
            onBdrActionChange,
            onRefundTypeChange,
            loadReconnectEligibility,
            loadBadDebtEligibility,
            onDevicePicked,
            recordDeviceDownPaymentC360,
            canRecharge,
            canPayAndReconnect,
            executePayAndReconnect,
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
            subscriptionBadgeText,
            operationalStatusLabel,
            checkHlr,
            selectSubscriptionRow,
            isRowSelected,
            hlrStatusBtnClass,
            hlrNeedsRemediation,
            selectedHlrNeedsRemediation,
            selectedHlrSnapshot,
            hlrRemediationSubtitle,
            hlrRemediationCrmLabel,
            hlrRemediationCrmBadgeClass,
            hlrRemediationHlrBadgeClass,
            reprovisionSelectedHlr,
            quickSimSwapRemediation,
            openRemediationSuspension,
            openRemediationReconnect,
            loadProfile,
            openAiWizard,
            openSupportTicketC360,
            simulateAiCall,
            forceCbs,
            hlrResync,
            escalateTicket,
            isTicketBusy,
            mappedTimeline,
            timelineFilters,
            setTimelineFilter,
            loadMoreTimeline,
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
            selectedSubscription,
            wizardTitle,
            canWizardFinishStep2,
            wizardAwaitBackOffice,
            wizardShowsConfirmCbs,
            wizardRequiresStep2Identity,
            activateRequiredDeposit,
            openProvisioningWizard,
            closeProvisioningWizard,
            finishProvisioningWizard,
            onWizardLineChanged,
            onCgtTargetTypeChanged,
            onChangeNumberTargetPicked,
            onCnChangeModeChange,
            cnShowsInternalPool,
            cnDonorOperators,
            searchTakeoverTarget,
            selectTakeoverTarget,
            wizardStepNext,
            wizardStepPrev,
            submitWizardCreateDraft,
            submitWizardMarkDocument,
            submitWizardRecordPayment,
            fetchPaymentFromCashier,
            submitWizardConfirmCbs,
            onWizardIdentityFileChange,
            onActivateMsisdnChanged,
            onActivateKycFileChange,
            onWizardOfferingChanged,
            wizardOfferDisplayName,
            wizardOfferMonthlyPrice,
            wizardOfferVoiceLine,
            wizardOfferDataLine,
            wizardOfferSmsLine,
            wizardOfferSummaryText,
            wizardOfferPriceLabel,
            activateSimIccidLocked,
            activationChannelUiMode: Vue.computed(() =>
                typeof ActivationChannelUi !== 'undefined' ? ActivationChannelUi.resolveMode() : 'showroom'),
            activationChannelLabels: Vue.computed(() => {
                localeTick.value;
                const loc = currentUiLang();
                if (typeof ActivationChannelUi === 'undefined') {
                    return { showroom: 'POS', dealer: 'Dealer' };
                }
                return {
                    showroom: ActivationChannelUi.label(0, loc),
                    dealer: ActivationChannelUi.label(1, loc),
                };
            }),
            activationChannelLockedHint: Vue.computed(() => {
                localeTick.value;
                if (typeof ActivationChannelUi === 'undefined') {
                    return telecomT('wizardUi.channelShowroomLockedHint', '');
                }
                return ActivationChannelUi.lockedHint(currentUiLang());
            }),
        };
    },
};

Vue.createApp(Customer360ProfileApp).mount('#app');
