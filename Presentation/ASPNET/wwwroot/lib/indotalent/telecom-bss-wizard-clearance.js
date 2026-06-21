/**
 * BSS Golden Standard — dynamic fields for SUS, CGT, TRM, RFD wizards.
 * Mirrors telecom-reconnect-clearance.js patterns.
 */
(function (global) {
    'use strict';

    const PAYMENT_REF_RE = /^[A-Za-z0-9][A-Za-z0-9._-]{2,63}$/;
    const SECURITY_TICKET_RE = /^(TT|TKT|RA|SEC)-[A-Za-z0-9][A-Za-z0-9._-]{2,48}$/i;
    const IBAN_RE = /^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$/i;
    const SYRIATEL_MOBILE_RE = /^(09|\+9639|9639)[0-9]{8}$/;
    const KYC_ALLOWED_EXT = new Set(['.pdf', '.png', '.jpg', '.jpeg']);
    const KYC_MAX_BYTES = 5 * 1024 * 1024;

    function clearBssFields(target, keys) {
        if (!target) return;
        keys.forEach((k) => {
            if (k.endsWith('File')) target[k] = null;
            else if (k === 'bssKycDocumentReferenceId') target[k] = '';
            else target[k] = '';
        });
    }

    function validateKycFile(file) {
        if (!file) return 'bss.regulatoryAttachment';
        const name = (file.name || '').toLowerCase();
        const ext = name.includes('.') ? name.slice(name.lastIndexOf('.')) : '';
        if (!KYC_ALLOWED_EXT.has(ext)) return 'bss.regulatoryAttachmentInvalid';
        if (file.size > KYC_MAX_BYTES) return 'bss.regulatoryAttachmentTooLarge';
        return '';
    }

    async function uploadKyc(msisdn, file) {
        if (global.TelecomReconnectClearance?.uploadRegulatoryAttachment) {
            return global.TelecomReconnectClearance.uploadRegulatoryAttachment(msisdn, file);
        }
        const fileErr = validateKycFile(file);
        if (fileErr) throw new Error(fileErr);
        const form = new FormData();
        form.append('msisdn', (msisdn || '').trim());
        form.append('file', file);
        const token = typeof StorageManager !== 'undefined' ? StorageManager.getAccessToken?.() || '' : '';
        const baseUrl = (typeof AxiosManager !== 'undefined' && AxiosManager.getBaseUrl
            ? AxiosManager.getBaseUrl()
            : '/api').replace(/\/$/, '');
        const payload = await new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();
            xhr.open('POST', `${baseUrl}/Telecom/UploadKycDocument`, true);
            if (token) xhr.setRequestHeader('Authorization', `Bearer ${token}`);
            xhr.onload = () => {
                try { resolve(JSON.parse(xhr.responseText || '{}')); }
                catch { reject(new Error('upload failed')); }
            };
            xhr.onerror = () => reject(new Error('upload failed'));
            xhr.send(form);
        });
        const ref = payload?.documentReferenceId ?? payload?.DocumentReferenceId ?? '';
        if (!ref) throw new Error('bss.regulatoryUploadFailed');
        return ref;
    }

    async function ensureKycUploaded(target, msisdn, fileKey, refKey) {
        if ((target[refKey] || '').trim()) return;
        const file = target[fileKey];
        if (!file) throw new Error('bss.regulatoryAttachment');
        target[refKey] = await uploadKyc(msisdn, file);
    }

    function isPrepaidCode(code) {
        return /prepaid|pre-paid|مسبق/i.test(code || '');
    }

    function isPostpaidCode(code) {
        return /postpaid|post-paid|فاتورة/i.test(code || '');
    }

    // ——— SUSPENSION (SUS) ———
    const suspension = {
        reset(target, suspensionType) {
            const t = (suspensionType || 'CustomerRequest').trim();
            clearBssFields(target, [
                'bssPaymentReference', 'bssSecurityTicketId', 'bssDocumentNumber',
                'bssRegulatoryFile', 'bssKycDocumentReferenceId',
            ]);
            if (t !== 'Fraud') target.bssSupervisorConfirmed = false;
        },
        showsPayment(type) { return (type || '').trim() === 'Billing'; },
        showsFraud(type) { return (type || '').trim() === 'Fraud'; },
        showsRegulatory(type) { return (type || '').trim() === 'Regulatory'; },
        showsSimple(type) {
            const t = (type || '').trim();
            return t === 'CustomerRequest' || t === 'Operational';
        },
        requiresIdentityUpload(type) {
            return suspension.showsRegulatory(type) || suspension.showsFraud(type);
        },
        validate(target) {
            const t = (target.susSuspensionType || '').trim();
            const reason = (target.susSuspensionReason || '').trim();
            if (!reason) return { key: 'suspension.suspensionReason' };
            if (suspension.showsPayment(t)) {
                const pay = (target.bssPaymentReference || '').trim();
                if (!pay) return { key: 'bss.paymentReference' };
                if (!PAYMENT_REF_RE.test(pay)) return { key: 'bss.paymentRefInvalid' };
            }
            if (suspension.showsFraud(t)) {
                const tid = (target.bssSecurityTicketId || '').trim();
                if (!tid) return { key: 'bss.securityTicketId' };
                if (!SECURITY_TICKET_RE.test(tid)) return { key: 'bss.securityTicketInvalid' };
                if (!target.bssSupervisorConfirmed) return { key: 'bss.supervisorConfirmation' };
            }
            if (suspension.showsRegulatory(t)) {
                const doc = (target.bssDocumentNumber || '').trim();
                if (!doc) return { key: 'bss.regulatoryDocumentNumber' };
                if (!target.bssKycDocumentReferenceId && !target.bssRegulatoryFile) {
                    return { key: 'bss.regulatoryAttachment' };
                }
            }
            return null;
        },
        buildApi(target) {
            const t = (target.susSuspensionType || '').trim();
            const api = {
                paymentReference: null,
                agencyReference: null,
                collectionNote: null,
                kycDocumentReferenceId: null,
            };
            if (suspension.showsPayment(t)) {
                api.paymentReference = (target.bssPaymentReference || '').trim() || null;
            } else if (suspension.showsFraud(t)) {
                api.agencyReference = (target.bssSecurityTicketId || '').trim() || null;
                api.fraudClearanceConfirmed = !!target.bssSupervisorConfirmed;
            } else if (suspension.showsRegulatory(t)) {
                api.collectionNote = (target.bssDocumentNumber || '').trim() || null;
                api.kycDocumentReferenceId = (target.bssKycDocumentReferenceId || '').trim() || null;
            }
            return api;
        },
        async ensureUploads(target, msisdn) {
            if (suspension.showsRegulatory(target.susSuspensionType)) {
                await ensureKycUploaded(target, msisdn, 'bssRegulatoryFile', 'bssKycDocumentReferenceId');
            }
        },
    };

    // ——— TERMINATION (TRM) ———
    const termination = {
        reset(target, terminationType) {
            const t = (terminationType || 'Voluntary').trim();
            clearBssFields(target, [
                'bssPaymentReference', 'bssSecurityTicketId', 'bssDocumentNumber',
                'bssRegulatoryFile', 'bssKycDocumentReferenceId',
            ]);
            if (t === 'Voluntary') {
                target.trmRetentionOfferOutcome = target.trmRetentionOfferOutcome || 'Declined';
            }
        },
        showsPayment(type) { return (type || '').trim() === 'Collections'; },
        showsFraud(type) { return (type || '').trim() === 'Fraud'; },
        showsRegulatory(type) { return (type || '').trim() === 'Regulatory'; },
        showsVoluntary(type) { return (type || '').trim() === 'Voluntary'; },
        requiresLegacyIdentity(type) {
            const t = (type || '').trim();
            return t === 'Fraud' || t === 'Regulatory' || t === 'Collections';
        },
        validate(target) {
            const t = (target.trmTerminationType || '').trim();
            const reason = (target.trmTerminationReason || '').trim();
            if (!reason) return { key: 'termination.terminationReason' };
            if (termination.showsVoluntary(t) && !(target.trmRetentionOfferOutcome || '').trim()) {
                return { key: 'termination.retentionOutcome' };
            }
            if (termination.showsPayment(t)) {
                const pay = (target.bssPaymentReference || '').trim();
                if (!pay) return { key: 'bss.paymentReference' };
                if (!PAYMENT_REF_RE.test(pay)) return { key: 'bss.paymentRefInvalid' };
            }
            if (termination.showsFraud(t)) {
                const tid = (target.bssSecurityTicketId || '').trim();
                if (!tid) return { key: 'bss.securityTicketId' };
                if (!SECURITY_TICKET_RE.test(tid)) return { key: 'bss.securityTicketInvalid' };
            }
            if (termination.showsRegulatory(t)) {
                const doc = (target.bssDocumentNumber || '').trim();
                if (!doc) return { key: 'bss.regulatoryDocumentNumber' };
                if (!target.bssKycDocumentReferenceId && !target.bssRegulatoryFile) {
                    return { key: 'bss.regulatoryAttachment' };
                }
            }
            return null;
        },
        buildApi(target) {
            const t = (target.trmTerminationType || '').trim();
            const api = {
                paymentReference: null,
                agencyReference: null,
                collectionNote: null,
                kycDocumentReferenceId: null,
            };
            if (termination.showsPayment(t)) {
                api.paymentReference = (target.bssPaymentReference || '').trim() || null;
            } else if (termination.showsFraud(t)) {
                api.agencyReference = (target.bssSecurityTicketId || '').trim() || null;
            } else if (termination.showsRegulatory(t)) {
                api.collectionNote = (target.bssDocumentNumber || '').trim() || null;
                api.kycDocumentReferenceId = (target.bssKycDocumentReferenceId || '').trim() || null;
            }
            return api;
        },
        async ensureUploads(target, msisdn) {
            if (termination.showsRegulatory(target.trmTerminationType)) {
                await ensureKycUploaded(target, msisdn, 'bssRegulatoryFile', 'bssKycDocumentReferenceId');
            }
        },
    };

    // ——— CHANGE GSM (CGT) ———
    const changeGsm = {
        reset(target, migrationPath) {
            clearBssFields(target, [
                'bssPaymentReference', 'bssDocumentNumber',
                'bssRegulatoryFile', 'bssKycDocumentReferenceId', 'bssIdentityFile',
            ]);
            if ((migrationPath || 'Standard') !== 'Regulatory') {
                target.cgtMigrationPath = target.cgtMigrationPath || 'Standard';
            }
        },
        needsFinancialClearance(target, options) {
            const sourceCode = target.cgtSourceTypeCode || '';
            const targetRow = (target.cgtTargets || []).find(
                (x) => String(x.id) === String(target.cgtTargetTypeId)
            );
            const targetCode = targetRow?.code || '';
            const preToPost = isPrepaidCode(sourceCode) && isPostpaidCode(targetCode);
            const debt = Number(options?.outstandingBalance ?? 0) < 0;
            return preToPost || debt;
        },
        showsFinancial(target, options) {
            return changeGsm.needsFinancialClearance(target, options);
        },
        showsRegulatory(target) {
            return (target.cgtMigrationPath || 'Standard').trim() === 'Regulatory';
        },
        showsKycUpload(target, options) {
            return changeGsm.showsFinancial(target, options) || changeGsm.showsRegulatory(target);
        },
        validate(target, options) {
            const targetId = (target.cgtTargetTypeId || '').trim();
            const reason = (target.cgtMigrationReason || '').trim();
            if (!targetId) return { key: 'changeGsm.targetType' };
            if (!reason) return { key: 'changeGsm.migrationReason' };
            if (changeGsm.showsFinancial(target, options)) {
                const pay = (target.bssPaymentReference || '').trim();
                if (!pay) return { key: 'bss.paymentReference' };
                if (!PAYMENT_REF_RE.test(pay)) return { key: 'bss.paymentRefInvalid' };
                if (!target.bssKycDocumentReferenceId && !target.bssIdentityFile && !target.bssRegulatoryFile) {
                    return { key: 'bss.identityReverifyRequired' };
                }
            }
            if (changeGsm.showsRegulatory(target)) {
                const doc = (target.bssDocumentNumber || '').trim();
                if (!doc) return { key: 'bss.regulatoryDocumentNumber' };
                if (!target.bssKycDocumentReferenceId && !target.bssRegulatoryFile) {
                    return { key: 'bss.regulatoryAttachment' };
                }
            }
            return null;
        },
        buildApi(target, options) {
            const api = {
                paymentReference: null,
                agencyReference: null,
                collectionNote: null,
                kycDocumentReferenceId: null,
            };
            if (changeGsm.showsFinancial(target, options)) {
                api.paymentReference = (target.bssPaymentReference || '').trim() || null;
            }
            if (changeGsm.showsRegulatory(target)) {
                api.collectionNote = (target.bssDocumentNumber || '').trim() || null;
            }
            const kyc = (target.bssKycDocumentReferenceId || '').trim();
            if (kyc) api.kycDocumentReferenceId = kyc;
            return api;
        },
        async ensureUploads(target, msisdn, options) {
            const file = target.bssRegulatoryFile || target.bssIdentityFile;
            if (file && !target.bssKycDocumentReferenceId) {
                target.bssKycDocumentReferenceId = await uploadKyc(msisdn, file);
            }
        },
    };

    // ——— SIM SWAP ———
    const POLICE_REPORT_RE = /^(POL|TT|TKT|RA|SEC)-[A-Za-z0-9][A-Za-z0-9._-]{2,48}$/i;

    const simSwap = {
        reset(target, lostOrStolen) {
            if (!lostOrStolen) {
                clearBssFields(target, ['bssSecurityTicketId', 'bssIdentityFile', 'bssKycDocumentReferenceId']);
                target.identityFile = null;
                target.simIdentityFile = null;
            }
        },
        showsLostStolenFields(lostOrStolen) {
            return !!lostOrStolen;
        },
        validate(target) {
            if (!target.simLostOrStolen) return null;
            const report = (target.bssSecurityTicketId || '').trim();
            if (!report) return { key: 'bss.policeReportNumber' };
            if (!POLICE_REPORT_RE.test(report) && !SECURITY_TICKET_RE.test(report)) {
                return { key: 'bss.policeReportInvalid' };
            }
            const hasId = target.identityFile || target.simIdentityFile || target.bssIdentityFile
                || (target.bssKycDocumentReferenceId || '').trim();
            if (!hasId) return { key: 'bss.identityReverifyRequired' };
            return null;
        },
        buildApi(target) {
            if (!target.simLostOrStolen) return { agencyReference: null };
            return { agencyReference: (target.bssSecurityTicketId || '').trim() || null };
        },
    };

    // ——— CHANGE NUMBER (CNR / MNP) ———
    const DONOR_OPERATORS = ['MTN', 'AFRICELL', 'OTHER'];

    const changeNumber = {
        donorOperators: DONOR_OPERATORS,
        reset(target, requiresPremium) {
            if (!requiresPremium) target.bssPaymentReference = '';
            if ((target.cnChangeMode || 'Internal') !== 'PortIn') {
                target.cnPortInMsisdn = '';
                target.cnDonorOperatorCode = '';
                target.cnPortInReference = '';
            }
        },
        isPortIn(target) {
            return (target?.cnChangeMode || 'Internal').trim() === 'PortIn';
        },
        showsInternalPool(target) {
            return !changeNumber.isPortIn(target);
        },
        showsPortInFields(target) {
            return changeNumber.isPortIn(target);
        },
        showsPremiumPayment(target) {
            if (changeNumber.isPortIn(target)) return false;
            return !!target.cnRequiresBackOffice;
        },
        validate(target) {
            if (changeNumber.isPortIn(target)) {
                const portIn = (target.cnPortInMsisdn || '').trim();
                if (!portIn) return { key: 'changeNumber.portInMsisdn' };
                const donor = (target.cnDonorOperatorCode || '').trim();
                if (!donor) return { key: 'changeNumber.donorOperator' };
                const ref = (target.cnPortInReference || '').trim();
                if (!ref) return { key: 'changeNumber.portInReference' };
                if (!/^(MNP|PORT|CNR)-[A-Za-z0-9][A-Za-z0-9._-]{2,48}$/i.test(ref)) {
                    return { key: 'changeNumber.portInReferenceInvalid' };
                }
                const reason = (target.cnNumberChangeReason || '').trim();
                if (!reason) return { key: 'changeNumber.changeReason' };
                return null;
            }
            if (!target.cnRequiresBackOffice) return null;
            const pay = (target.bssPaymentReference || '').trim();
            if (!pay) return { key: 'bss.paymentReference' };
            if (!PAYMENT_REF_RE.test(pay)) return { key: 'bss.paymentRefInvalid' };
            return null;
        },
        buildApi(target) {
            if (changeNumber.isPortIn(target)) {
                return {
                    paymentReference: null,
                    agencyReference: (target.cnPortInReference || '').trim() || null,
                };
            }
            if (!target.cnRequiresBackOffice) return { paymentReference: null };
            return { paymentReference: (target.bssPaymentReference || '').trim() || null };
        },
    };

    // ——— TAKE OVER (TKO) ———
    const takeOver = {
        showsObligationSettlement(target, options) {
            const bal = Number(options?.outstandingBalance);
            if (Number.isFinite(bal) && bal < 0) return true;
            return !!target.tkoObligationBlocked;
        },
        reset(target, options) {
            if (!takeOver.showsObligationSettlement(target, options)) {
                target.bssPaymentReference = '';
            }
        },
        validate(target, options) {
            const reason = (target.takeoverTransferReason || target.tkoTransferReason || '').trim();
            if (!reason) return { key: 'takeOver.transferReason' };
            if (!(target.takeoverTargetProfileId || target.secondarySubscriberProfileId || '').trim()) {
                return { key: 'takeOver.newOwnerRequired' };
            }
            if (takeOver.showsObligationSettlement(target, options)) {
                const pay = (target.bssPaymentReference || '').trim();
                if (!pay) return { key: 'bss.obligationSettlementRef' };
                if (!PAYMENT_REF_RE.test(pay)) return { key: 'bss.paymentRefInvalid' };
            }
            return null;
        },
        buildApi(target, options) {
            if (!takeOver.showsObligationSettlement(target, options)) {
                return { paymentReference: null, takeOverObligationStatus: null };
            }
            return {
                paymentReference: (target.bssPaymentReference || '').trim() || null,
                takeOverObligationStatus: 'Settled',
            };
        },
    };

    // ——— REFUND (RFD) ———
    const refund = {
        reset(target, refundType, refundMethod) {
            clearBssFields(target, [
                'bssOriginalTransactionRef', 'bssPayoutDestination',
            ]);
            const rt = (refundType || '').trim();
            const rm = (refundMethod || '').trim();
            if (rt !== 'Deposit' && rt !== 'Overpayment') target.bssOriginalTransactionRef = '';
            if (rm !== 'BankTransfer' && rt !== 'SyriatelCash') target.bssPayoutDestination = '';
        },
        showsOriginalTxRef(type) {
            const t = (type || '').trim();
            return t === 'Deposit' || t === 'Overpayment';
        },
        showsPayoutDestination(type, method) {
            const t = (type || '').trim();
            const m = (method || '').trim();
            return m === 'BankTransfer' || t === 'SyriatelCash';
        },
        payoutLabel(type, method) {
            const t = (type || '').trim();
            const m = (method || '').trim();
            if (m === 'BankTransfer') return 'bss.iban';
            if (t === 'SyriatelCash') return 'bss.walletMobile';
            return 'bss.payoutDestination';
        },
        validate(target) {
            const t = (target.rfdRefundType || '').trim();
            const m = (target.rfdRefundMethod || '').trim();
            const reason = (target.rfdRefundReason || '').trim();
            const amt = Number(target.rfdRefundAmount);
            if (!reason) return { key: 'refund.refundReason' };
            if (!amt || amt <= 0) return { key: 'refund.refundAmount' };
            if (refund.showsOriginalTxRef(t)) {
                const ref = (target.bssOriginalTransactionRef || '').trim();
                if (!ref) return { key: 'bss.originalTransactionRef' };
                if (!PAYMENT_REF_RE.test(ref)) return { key: 'bss.originalTransactionRefInvalid' };
            }
            if (refund.showsPayoutDestination(t, m)) {
                const dest = (target.bssPayoutDestination || '').trim();
                if (!dest) return { key: refund.payoutLabel(t, m) };
                if (m === 'BankTransfer' && !IBAN_RE.test(dest.replace(/\s/g, ''))) {
                    return { key: 'bss.ibanInvalid' };
                }
                if (t === 'SyriatelCash' && !SYRIATEL_MOBILE_RE.test(dest.replace(/\s/g, ''))) {
                    return { key: 'bss.walletMobileInvalid' };
                }
            }
            return null;
        },
        buildApi(target) {
            const t = (target.rfdRefundType || '').trim();
            const m = (target.rfdRefundMethod || '').trim();
            return {
                refundCbsReference: refund.showsOriginalTxRef(t)
                    ? (target.bssOriginalTransactionRef || '').trim() || null
                    : null,
                refundGatewayReference: refund.showsPayoutDestination(t, m)
                    ? (target.bssPayoutDestination || '').trim().replace(/\s/g, '') || null
                    : null,
            };
        },
    };

    function onRegulatoryFileChange(target, ev) {
        const file = ev?.target?.files?.[0] || null;
        target.bssRegulatoryFile = file;
        target.bssKycDocumentReferenceId = '';
    }

    function onIdentityFileChange(target, ev) {
        const file = ev?.target?.files?.[0] || null;
        target.bssIdentityFile = file;
        target.bssKycDocumentReferenceId = '';
    }

    const effectiveDate = {
        reset(target) {
            if (!target) return;
            target.bssEffectiveMode = 'immediate';
            target.bssEffectiveDateLocal = '';
        },
        todayLocal() {
            const d = new Date();
            const y = d.getFullYear();
            const m = String(d.getMonth() + 1).padStart(2, '0');
            const day = String(d.getDate()).padStart(2, '0');
            return `${y}-${m}-${day}`;
        },
        validate(target) {
            if (!target || (target.bssEffectiveMode || 'immediate') !== 'scheduled') return null;
            const picked = (target.bssEffectiveDateLocal || '').trim();
            if (!picked) return { key: 'bss.effectiveDateRequired' };
            if (picked < effectiveDate.todayLocal()) return { key: 'bss.effectiveDatePast' };
            return null;
        },
        resolveUtcIso(target) {
            if (!target || (target.bssEffectiveMode || 'immediate') === 'immediate') {
                return new Date().toISOString();
            }
            const picked = (target.bssEffectiveDateLocal || '').trim();
            if (!picked) return new Date().toISOString();
            return new Date(`${picked}T00:00:00`).toISOString();
        },
        supportsWizardKind(kind) {
            const k = (kind || '').trim();
            return [
                'activate',
                'reconnect',
                'refund',
                'badDebt',
                'deviceSale',
                'suspension',
                'changeGsm',
                'takeover',
                'migrate',
                'termination',
                'changeNumber',
                'simswap',
            ].includes(k);
        },
    };

    const migration = {
        async loadPreview(subscriberProfileId, msisdnAssetId, productOfferingId) {
            const sid = (subscriberProfileId || '').trim();
            const aid = (msisdnAssetId || '').trim();
            const oid = (productOfferingId || '').trim();
            if (!sid || !aid || !oid) return null;
            const url =
                '/Product/GetMigrationProrationPreview?subscriberProfileId=' +
                encodeURIComponent(sid) +
                '&msisdnAssetId=' +
                encodeURIComponent(aid) +
                '&productOfferingId=' +
                encodeURIComponent(oid);
            const res = await AxiosManager.get(url, {});
            return res?.data?.content ?? res?.data?.Content ?? null;
        },
        validatePreview(preview) {
            if (!preview) return null;
            const sufficient = preview.sufficientBalance ?? preview.SufficientBalance;
            if (sufficient === false) return { key: 'wizard.migrationOffers.prorationInsufficient' };
            return null;
        },
        formatAmount(value, currency) {
            const n = Number(value);
            if (!Number.isFinite(n)) return '—';
            const cur = currency || 'SYP';
            try {
                return new Intl.NumberFormat(undefined, { style: 'currency', currency: cur, maximumFractionDigits: 0 }).format(n);
            } catch {
                return `${n.toLocaleString()} ${cur}`;
            }
        },
        previewLabels(preview, translate) {
            const t = typeof translate === 'function' ? translate : (k, fb) => fb || k;
            if (!preview) {
                return {
                    priceDifference: '—',
                    proratedAmount: '—',
                    walletBalance: '—',
                    daysRemaining: '',
                    sufficient: true,
                };
            }
            const cur = preview.currencyCode ?? preview.CurrencyCode;
            const days = preview.daysRemainingInCycle ?? preview.DaysRemainingInCycle ?? 0;
            const total = preview.daysInBillingCycle ?? preview.DaysInBillingCycle ?? 0;
            const sufficient = preview.sufficientBalance ?? preview.SufficientBalance !== false;
            return {
                priceDifference: migration.formatAmount(preview.priceDifference ?? preview.PriceDifference, cur),
                proratedAmount: migration.formatAmount(preview.proratedAmount ?? preview.ProratedAmount, cur),
                walletBalance: migration.formatAmount(preview.walletBalance ?? preview.WalletBalance, cur),
                daysRemaining: t('wizard.migrationOffers.daysRemaining', 'Days')
                    .replace('{days}', days)
                    .replace('{total}', total),
                sufficient,
            };
        },
    };

    const BARRING_LEVELS = [
        { value: 'Full', key: 'suspension.barringFull' },
        { value: 'InboundOnly', key: 'suspension.barringInbound' },
        { value: 'OutboundOnly', key: 'suspension.barringOutbound' },
        { value: 'DataOnly', key: 'suspension.barringDataOnly' },
    ];

    const SUSPENSION_MAX_DAYS = 90;
    const SUSPENSION_DEFAULT_END_OFFSET_DAYS = 30;

    function toLocalIsoDate(d) {
        const y = d.getFullYear();
        const m = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        return `${y}-${m}-${day}`;
    }

    const suspensionEndDate = {
        maxDays: SUSPENSION_MAX_DAYS,
        defaultOffsetDays: SUSPENSION_DEFAULT_END_OFFSET_DAYS,
        maxEndDateLocal(startLocal) {
            const base = (startLocal || effectiveDate.todayLocal()).trim();
            const d = new Date(`${base}T12:00:00`);
            d.setDate(d.getDate() + SUSPENSION_MAX_DAYS);
            return toLocalIsoDate(d);
        },
        onAutoReconnectToggled(target, getStartLocal) {
            if (!target) return;
            if (target.susAutoReconnectEnabled && !(target.susEndDateLocal || '').trim()) {
                const start = typeof getStartLocal === 'function' ? getStartLocal() : effectiveDate.todayLocal();
                const d = new Date(`${start}T12:00:00`);
                d.setDate(d.getDate() + SUSPENSION_DEFAULT_END_OFFSET_DAYS);
                target.susEndDateLocal = toLocalIsoDate(d);
            }
            if (!target.susAutoReconnectEnabled) {
                target.susEndDateLocal = '';
                if ('susEndDateValidationError' in target) target.susEndDateValidationError = '';
            }
        },
        validateEndDate(target, getStartLocal) {
            if (!target?.susAutoReconnectEnabled) return null;
            const end = (target.susEndDateLocal || '').trim();
            if (!end) return { key: 'suspension.endDateRequired' };
            const start = typeof getStartLocal === 'function' ? getStartLocal() : effectiveDate.todayLocal();
            const max = suspensionEndDate.maxEndDateLocal(start);
            if (end < start || end > max) return { key: 'suspension.endDateMaxExceeded' };
            return null;
        },
    };

    const ui = {
        incompleteSwal(Swal, translate, key, fallback) {
            if (!Swal || !key) return;
            const t = typeof translate === 'function' ? translate : (k, fb) => fb || k;
            const labels = global.TelecomI18n?.swalLabels?.() || {};
            Swal.fire({
                icon: 'warning',
                title: t('swal.incompleteTitle', labels.incompleteTitle || 'Missing details'),
                text: t(key, fallback || key),
                confirmButtonText: labels.ok || 'OK',
            });
        },
    };

    global.TelecomBssWizardClearance = {
        PAYMENT_REF_RE,
        SECURITY_TICKET_RE,
        IBAN_RE,
        BARRING_LEVELS,
        suspensionEndDate,
        ui,
        suspension,
        termination,
        changeGsm,
        simSwap,
        changeNumber,
        takeOver,
        refund,
        effectiveDate,
        migration,
        onRegulatoryFileChange,
        onIdentityFileChange,
        uploadKyc,
        validateKycFile,
    };
})(typeof window !== 'undefined' ? window : globalThis);
