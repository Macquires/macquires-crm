/**
 * Shared reconnect (RCN) clearance UI helpers — dynamic fields, validation, API payload binding.
 */
(function (global) {
    'use strict';

    const PAYMENT_REF_RE = /^[A-Za-z0-9][A-Za-z0-9._-]{2,63}$/;
    const SECURITY_TICKET_RE = /^(TT|TKT|RA|SEC)-[A-Za-z0-9][A-Za-z0-9._-]{2,48}$/i;
    const KYC_ALLOWED_EXT = new Set(['.pdf', '.png', '.jpg', '.jpeg']);
    const KYC_MAX_BYTES = 5 * 1024 * 1024;

    function normalizeClearance(clearanceType) {
        return (clearanceType || 'Customer').trim();
    }

    function resetConditionalFields(target, clearanceType) {
        if (!target) return;
        const c = normalizeClearance(clearanceType);
        if (c !== 'Payment') {
            target.rcnPaymentReference = '';
        }
        if (c !== 'Fraud') {
            target.rcnSecurityTicketId = '';
            target.rcnFraudClearanceConfirmed = false;
        }
        if (c !== 'Regulatory') {
            target.rcnDocumentNumber = '';
            target.rcnRegulatoryFile = null;
            target.rcnKycDocumentReferenceId = '';
        }
    }

    function showsPaymentReference(clearanceType, options) {
        if (options?.bdrApproved) return true;
        return normalizeClearance(clearanceType) === 'Payment';
    }

    function showsFraudFields(clearanceType) {
        return normalizeClearance(clearanceType) === 'Fraud';
    }

    function showsRegulatoryFields(clearanceType) {
        return normalizeClearance(clearanceType) === 'Regulatory';
    }

    function showsSimplePath(clearanceType) {
        const c = normalizeClearance(clearanceType);
        return c === 'Customer' || c === 'Operational';
    }

    function validateKycFile(file) {
        if (!file) return 'reconnect.regulatoryAttachment';
        const name = (file.name || '').toLowerCase();
        const ext = name.includes('.') ? name.slice(name.lastIndexOf('.')) : '';
        if (!KYC_ALLOWED_EXT.has(ext)) return 'reconnect.regulatoryAttachmentInvalid';
        if (file.size > KYC_MAX_BYTES) return 'reconnect.regulatoryAttachmentTooLarge';
        return '';
    }

    function validate(target, options) {
        if (!target) return { key: 'reconnect.reconnectReason' };
        const c = normalizeClearance(target.rcnClearanceType);
        const reason = (target.rcnReconnectReason || '').trim();
        if (!reason) return { key: 'reconnect.reconnectReason' };

        if (options?.bdrApproved) {
            const pay = (target.rcnPaymentReference || '').trim();
            if (!pay) return { key: 'reconnect.paymentReference' };
            if (!PAYMENT_REF_RE.test(pay)) return { key: 'reconnect.paymentRefInvalid' };
        }

        if (c === 'Payment') {
            const pay = (target.rcnPaymentReference || '').trim();
            if (!pay) return { key: 'reconnect.paymentReference' };
            if (!PAYMENT_REF_RE.test(pay)) return { key: 'reconnect.paymentRefInvalid' };
        }

        if (c === 'Fraud') {
            const ticketId = (target.rcnSecurityTicketId || '').trim();
            if (!ticketId) return { key: 'reconnect.securityTicketId' };
            if (!SECURITY_TICKET_RE.test(ticketId)) return { key: 'reconnect.securityTicketInvalid' };
        }

        if (c === 'Regulatory') {
            const docNo = (target.rcnDocumentNumber || '').trim();
            if (!docNo) return { key: 'reconnect.regulatoryDocumentNumber' };
            if (!target.rcnKycDocumentReferenceId && !target.rcnRegulatoryFile) {
                return { key: 'reconnect.regulatoryAttachment' };
            }
            if (target.rcnRegulatoryFile) {
                const fileErr = validateKycFile(target.rcnRegulatoryFile);
                if (fileErr) return { key: fileErr };
            }
        }

        return null;
    }

    function buildApiFields(target, options) {
        const c = normalizeClearance(target?.rcnClearanceType);
        const fields = {
            paymentReference: null,
            agencyReference: null,
            collectionNote: null,
            kycDocumentReferenceId: null,
            fraudClearanceConfirmed: false,
        };

        if (options?.bdrApproved || c === 'Payment') {
            const pay = (target?.rcnPaymentReference || '').trim();
            fields.paymentReference = pay || null;
        }

        if (c === 'Fraud') {
            fields.agencyReference = (target?.rcnSecurityTicketId || '').trim() || null;
            fields.fraudClearanceConfirmed = !!target?.rcnFraudClearanceConfirmed;
        } else if (c === 'Regulatory') {
            fields.collectionNote = (target?.rcnDocumentNumber || '').trim() || null;
            fields.kycDocumentReferenceId = (target?.rcnKycDocumentReferenceId || '').trim() || null;
            fields.fraudClearanceConfirmed = !!target?.rcnFraudClearanceConfirmed;
        }

        return fields;
    }

    async function uploadRegulatoryAttachment(msisdn, file) {
        const fileErr = validateKycFile(file);
        if (fileErr) {
            throw new Error(fileErr);
        }
        const msisdnValue = (msisdn || '').trim();
        if (!msisdnValue) {
            throw new Error('reconnect.regulatoryMsisdnRequired');
        }

        const form = new FormData();
        form.append('msisdn', msisdnValue);
        form.append('file', file);

        const token = typeof StorageManager !== 'undefined' ? StorageManager.getAccessToken?.() || '' : '';
        const baseUrl = (typeof AxiosManager !== 'undefined' && AxiosManager.getBaseUrl
            ? AxiosManager.getBaseUrl()
            : '/api').replace(/\/$/, '');
        const url = `${baseUrl}/Telecom/UploadKycDocument`;

        const payload = await new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();
            xhr.open('POST', url, true);
            if (token) xhr.setRequestHeader('Authorization', `Bearer ${token}`);
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
                    reject(data || { message: xhr.statusText });
                }
            };
            xhr.onerror = () => reject(new Error('upload failed'));
            xhr.send(form);
        });

        const ref =
            payload?.documentReferenceId
            ?? payload?.DocumentReferenceId
            ?? payload?.content?.documentReferenceId
            ?? payload?.content?.DocumentReferenceId
            ?? '';
        if (!ref) {
            throw new Error('reconnect.regulatoryUploadFailed');
        }
        return ref;
    }

    async function ensureRegulatoryAttachmentUploaded(target, msisdn) {
        if (!target || normalizeClearance(target.rcnClearanceType) !== 'Regulatory') {
            return;
        }
        if ((target.rcnKycDocumentReferenceId || '').trim()) {
            return;
        }
        if (!target.rcnRegulatoryFile) {
            throw new Error('reconnect.regulatoryAttachment');
        }
        const ref = await uploadRegulatoryAttachment(msisdn, target.rcnRegulatoryFile);
        target.rcnKycDocumentReferenceId = ref;
    }

    const BDR_KIND_NUM = 12;

    function isBadDebtOperation(op) {
        const kind = op?.kind ?? op?.Kind;
        return kind === BDR_KIND_NUM || kind === 'BadDebtRecovery';
    }

    function resolveBdrStatus(operations, sub) {
        if (!operations?.length || !sub) return null;
        const assetId = String(sub.msisdnAssetId ?? sub.MsisdnAssetId ?? '').trim();
        const msisdn = String(sub.msisdn ?? sub.Msisdn ?? '').trim();
        const bdr = operations.find((o) => {
            if (!isBadDebtOperation(o)) return false;
            const oAsset = String(o.msisdnAssetId ?? o.MsisdnAssetId ?? '').trim();
            const oMsisdn = String(o.msisdn ?? o.Msisdn ?? '').trim();
            return (assetId && oAsset === assetId) || (msisdn && oMsisdn === msisdn);
        });
        if (!bdr) return null;
        const st = bdr.status ?? bdr.Status;
        return st != null ? String(st) : null;
    }

    function isBdrApprovedForReconnect(status) {
        return String(status || '') === 'Approved_Pending_Cash';
    }

    function bdrReconnectOptions(sub, operations) {
        const status = resolveBdrStatus(operations, sub);
        return { bdrStatus: status, bdrApproved: isBdrApprovedForReconnect(status) };
    }

    global.TelecomReconnectClearance = {
        PAYMENT_REF_RE,
        SECURITY_TICKET_RE,
        resetConditionalFields,
        showsPaymentReference,
        showsFraudFields,
        showsRegulatoryFields,
        showsSimplePath,
        validate,
        validateKycFile,
        buildApiFields,
        uploadRegulatoryAttachment,
        ensureRegulatoryAttachmentUploaded,
        resolveBdrStatus,
        isBdrApprovedForReconnect,
        bdrReconnectOptions,
    };
})(typeof window !== 'undefined' ? window : globalThis);
