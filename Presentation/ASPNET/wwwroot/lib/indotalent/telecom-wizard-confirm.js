/**
 * Shared CBS confirm / back-office routing for BSS wizards (Hub, C360, CustomerList).
 */
(function (global) {
    'use strict';

    const CONFIRM_CBS_PERMISSION_KEYS = [
        'telecom.hub.backoffice',
        'telecom.line.simswap',
        'telecom.line.activate',
        'telecom.line.migrate',
        'telecom.line.change_gsm',
        'telecom.line.transfer_ownership',
        'telecom.line.change_number',
        'telecom.line.termination',
        'telecom.line.suspension',
        'telecom.line.reconnect',
        'telecom.device.sell',
        'telecom.line.refund',
        'telecom.line.collection',
        'customer.update',
    ];

    const KYC_ALLOWED_EXT = new Set(['.pdf', '.png', '.jpg', '.jpeg', '.webp']);

    function requiresStep2IdentityUpload(wizard, options) {
        const k = wizard?.kind;
        if (!k || k === 'addpackage' || k === 'support' || k === 'activate') {
            return false;
        }
        if (k === 'suspension' && options?.susRequiresStep2Identity === false) {
            return false;
        }
        return true;
    }

    function validateIdentityFile(file) {
        if (!file) {
            return 'wizardUi.identityRequired';
        }
        const name = (file.name || '').toLowerCase();
        const ext = name.includes('.') ? name.slice(name.lastIndexOf('.')) : '';
        if (!KYC_ALLOWED_EXT.has(ext)) {
            return 'wizardUi.identityInvalid';
        }
        return '';
    }

    async function uploadOperationIdentityDocument(operationId, file) {
        const fileErr = validateIdentityFile(file);
        if (fileErr) {
            throw new Error(fileErr);
        }
        const opId = (operationId || '').trim();
        if (!opId) {
            throw new Error('wizardUi.identityRequired');
        }
        if (typeof AxiosManager === 'undefined') {
            throw new Error('wizardUi.identityUploadFailed');
        }
        const form = new FormData();
        form.append('id', opId);
        form.append('file', file);
        return AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
            headers: { 'Content-Type': 'multipart/form-data' },
        });
    }

    function hasConfirmCbsPermission() {
        if (typeof StorageManager === 'undefined') {
            return true;
        }
        const perms = StorageManager.getPermissions?.() || [];
        return StorageManager.hasAnyPermission?.(perms, CONFIRM_CBS_PERMISSION_KEYS) || false;
    }

    /** True when the wizard must stop at back-office approval (no POS Confirm CBS). */
    function awaitBackOffice(wizard) {
        if (!wizard) {
            return false;
        }
        const k = wizard.kind;
        if (k === 'takeover') {
            return true;
        }
        if (k === 'simswap' && wizard.simLostOrStolen) {
            return true;
        }
        if (k === 'changeNumber' && wizard.cnRequiresBackOffice) {
            return true;
        }
        if (k === 'termination' && wizard.trmRequiresBackOffice) {
            return true;
        }
        if (k === 'suspension' && wizard.susRequiresBackOffice) {
            return true;
        }
        if (k === 'reconnect' && wizard.rcnRequiresBackOffice) {
            return true;
        }
        if (k === 'refund' && wizard.rfdRequiresBackOffice) {
            return true;
        }
        if (k === 'badDebt' && wizard.bdrRequiresBackOffice) {
            return true;
        }
        if (k === 'deviceSale' && wizard.devRequiresFinance) {
            return true;
        }
        return false;
    }

    function showsConfirmCbs(wizard, options) {
        if (!wizard?.createdOperationId || !wizard.documentMarkedUploaded) {
            return false;
        }
        if (wizard.confirmed) {
            return false;
        }
        if (awaitBackOffice(wizard)) {
            return false;
        }
        if (wizard.kind === 'addpackage' || wizard.kind === 'support' || wizard.kind === 'takeover') {
            return false;
        }
        if (options?.requirePermission !== false && !hasConfirmCbsPermission()) {
            return false;
        }
        if (
            wizard.kind === 'activate'
            && options?.activateRequiredDeposit > 0
            && !wizard.paymentRecorded
        ) {
            return false;
        }
        if (
            wizard.kind === 'deviceSale'
            && wizard.devSaleType === 'Installment'
            && !wizard.documentMarkedUploaded
        ) {
            return false;
        }
        return true;
    }

    function canFinishStep2(wizard, options) {
        if (wizard?.kind === 'addpackage') {
            return !!wizard.vasActivated;
        }
        if (wizard?.kind === 'support') {
            return !!wizard.supportTicketCreated;
        }
        if (!wizard?.createdOperationId || !wizard.documentMarkedUploaded) {
            return false;
        }
        if (wizard.kind === 'changeGsm') {
            return !!wizard.confirmed;
        }
        if (wizard.kind === 'activate') {
            if (options?.activateRequiredDeposit > 0 && !wizard.paymentRecorded) {
                return false;
            }
            if (options?.requireActivateKyc && !(wizard.kycDocumentReferenceId || '').trim()) {
                return false;
            }
            if (options?.requireActivateConfirmed) {
                return !!wizard.confirmed;
            }
        }
        if (awaitBackOffice(wizard)) {
            return true;
        }
        if (options?.requirePermission !== false && !hasConfirmCbsPermission()) {
            return true;
        }
        return !!wizard.confirmed;
    }

    function shouldSkipConfirm(wizard) {
        return awaitBackOffice(wizard) || !hasConfirmCbsPermission();
    }

    function uploadSuccessIsBackOffice(wizard) {
        return awaitBackOffice(wizard);
    }

    function isCorporateCustomerKind(customerKind) {
        const kind = String(customerKind ?? '').toLowerCase();
        return kind === 'corporate' || kind === '1';
    }

    function isPremiumMsisdnCategory(cat) {
        return [1, 2, 3, 'Silver', 'Gold', 'Platinum'].includes(cat);
    }

    function resolveChangeNumberRequiresBackOffice(wizard) {
        if (!wizard) return false;
        if ((wizard.cnChangeMode || 'Internal').trim() === 'PortIn') return true;
        const targetId = (wizard.cnTargetMsisdnAssetId || '').trim();
        const row = (wizard.cnPoolNumbers || []).find((r) => String(r.id ?? r.Id) === targetId);
        const cat = row?.category ?? row?.Category;
        if (!isPremiumMsisdnCategory(cat)) return false;
        const fee = Number(wizard.cnPremiumFeeAmount || 0);
        const pay = (wizard.bssPaymentReference || '').trim();
        return !(fee > 0 || pay);
    }

    function syncBackOfficeFlagsFromEntity(wizard, entity) {
        if (!wizard || !entity) return;
        const bo =
            String(entity.approvalLevelRequired ?? entity.ApprovalLevelRequired ?? '').toLowerCase()
            === 'backoffice';
        const k = wizard.kind;
        if (k === 'reconnect') wizard.rcnRequiresBackOffice = bo || !!wizard.rcnRequiresBackOffice;
        if (k === 'suspension') wizard.susRequiresBackOffice = bo || !!wizard.susRequiresBackOffice;
        if (k === 'termination') wizard.trmRequiresBackOffice = bo || !!wizard.trmRequiresBackOffice;
        if (k === 'changeNumber') wizard.cnRequiresBackOffice = bo || !!wizard.cnRequiresBackOffice;
        if (k === 'refund') {
            wizard.rfdRequiresBackOffice =
                bo
                || !!entity.requiresDualApproval
                || !!entity.RequiresDualApproval
                || !!wizard.rfdRequiresBackOffice;
        }
        if (k === 'badDebt') wizard.bdrRequiresBackOffice = bo || !!wizard.bdrRequiresBackOffice;
        if (k === 'deviceSale') {
            wizard.devRequiresFinance =
                bo
                || !!entity.deviceApprovalLevelRequired
                || !!entity.DeviceApprovalLevelRequired
                || !!wizard.devRequiresFinance;
        }
    }

    function operationIdentityDocumentUrl(operationId) {
        const opId = (operationId || '').trim();
        if (!opId || typeof AxiosManager === 'undefined') return '';
        const base = (AxiosManager.getBaseUrl?.() || '/api').replace(/\/$/, '');
        const token = typeof StorageManager !== 'undefined' ? StorageManager.getAccessToken?.() || '' : '';
        const q = `id=${encodeURIComponent(opId)}${token ? `&access_token=${encodeURIComponent(token)}` : ''}`;
        return `${base}/Telecom/DownloadTelecomOperationIdentityDocument?${q}`;
    }

    /**
     * CustomerList / one-shot flows: skip CBS confirm when BO queue or persona lacks execute permission.
     * @returns {Promise<object|null>} confirm API response, or null when skipped
     */
    async function confirmOperationIfAllowed(opId, options) {
        if (!opId) {
            return null;
        }
        if (options?.requiresBackOffice) {
            return null;
        }
        if (!hasConfirmCbsPermission()) {
            return null;
        }
        if (typeof AxiosManager === 'undefined') {
            return null;
        }
        return AxiosManager.post('/Telecom/ConfirmTelecomOperation', { id: opId });
    }

    global.TelecomWizardConfirm = {
        CONFIRM_CBS_PERMISSION_KEYS,
        KYC_ALLOWED_EXT,
        hasConfirmCbsPermission,
        requiresStep2IdentityUpload,
        validateIdentityFile,
        uploadOperationIdentityDocument,
        awaitBackOffice,
        showsConfirmCbs,
        canFinishStep2,
        shouldSkipConfirm,
        uploadSuccessIsBackOffice,
        isCorporateCustomerKind,
        isPremiumMsisdnCategory,
        resolveChangeNumberRequiresBackOffice,
        syncBackOfficeFlagsFromEntity,
        operationIdentityDocumentUrl,
        confirmOperationIfAllowed,
    };
})(typeof window !== 'undefined' ? window : globalThis);
