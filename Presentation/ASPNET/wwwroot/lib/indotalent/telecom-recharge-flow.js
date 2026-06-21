/**
 * Canonical prepaid recharge flow for C360 / List / Hub.
 * CreatePaymentTransaction → ConfirmPaymentTransaction → poll detail.
 */
(function (global) {
    'use strict';

    const PaymentType = { Wallet: 0, Voucher: 1 };
    const DEFAULT_AMOUNT_MAX = 1000000;

    function escapeHtml(value) {
        if (value == null) return '';
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    /** Arabic/Persian digits → Latin; strip spaces and grouping separators. */
    function normalizeDigits(value) {
        return String(value ?? '')
            .replace(/[٠-٩]/g, (ch) => String(ch.charCodeAt(0) - 0x0660))
            .replace(/[۰-۹]/g, (ch) => String(ch.charCodeAt(0) - 0x06f0))
            .replace(/[\s,٬،]/g, '')
            .trim();
    }

    function parsePositiveAmount(value) {
        const raw = normalizeDigits(value);
        if (!raw) return { ok: false, code: 'empty' };
        if (!/^\d+(\.\d+)?$/.test(raw)) return { ok: false, code: 'invalid' };
        const n = Number(raw);
        if (!Number.isFinite(n) || n <= 0) return { ok: false, code: 'invalid' };
        return { ok: true, value: n, raw };
    }

    function swalDefaults(labels) {
        const i18n = global.TelecomI18n?.swalLabels?.() || {};
        return {
            cancelButtonText: labels.cancelBtn || i18n.cancel || 'Cancel',
            confirmButtonColor: '#c8102e',
            customClass: {
                validationMessage: 'text-start fw-semibold text-danger',
                input: 'text-center fs-5',
            },
        };
    }

    function amountValidationMessage(result, labels, maxAmount) {
        const max = maxAmount || DEFAULT_AMOUNT_MAX;
        if (result.code === 'empty') {
            return labels.amountEmpty || labels.invalidAmount || 'Please enter the amount.';
        }
        if (result.code === 'invalid') {
            return labels.amountInvalid || labels.invalidAmount || 'Enter a valid amount greater than zero.';
        }
        if (result.code === 'max') {
            const tpl = labels.amountTooHigh || labels.invalidAmount || 'Maximum is {max} SYP.';
            return tpl.replace(/\{max\}/g, String(max));
        }
        return labels.invalidAmount || 'Invalid amount';
    }

    function validateAmountInput(value, labels) {
        const parsed = parsePositiveAmount(value);
        if (!parsed.ok) {
            return amountValidationMessage(parsed, labels, labels.amountMax || DEFAULT_AMOUNT_MAX);
        }
        const max = Number(labels.amountMax || DEFAULT_AMOUNT_MAX);
        if (parsed.value > max) {
            return amountValidationMessage({ code: 'max' }, labels, max);
        }
        return undefined;
    }

    function validateMsisdnInput(value, labels) {
        const digits = normalizeDigits(value).replace(/\D/g, '');
        if (digits.length < 8) {
            return labels.msisdnInvalid || labels.invalidMsisdn || 'Enter a valid line number (8+ digits).';
        }
        return undefined;
    }

    function validateRequiredText(value, labels) {
        if (!String(value ?? '').trim()) {
            return labels.refEmpty || labels.refRequired || labels.required || 'This field is required.';
        }
        return undefined;
    }

    async function pollPaymentDetail(axiosManager, paymentId, maxAttempts = 12) {
        for (let i = 0; i < maxAttempts; i++) {
            const res = await axiosManager.get(
                '/Telecom/GetPaymentTransactionDetail?id=' + encodeURIComponent(paymentId),
                {}
            );
            const detail = res?.data?.content ?? res?.data?.Content;
            const status = detail?.status ?? detail?.Status;
            if (status === 2 || status === 'Completed') return detail;
            if (status === 3 || status === 'Failed') {
                const reason = detail?.failureReason || detail?.FailureReason || 'Payment failed';
                throw Object.assign(new Error(reason), { response: res });
            }
            await new Promise((r) => setTimeout(r, 500));
        }
        return null;
    }

    /**
     * @param {object} swal window.Swal
     * @param {Record<string, string>} labels translated strings
     */
    async function promptMethod(swal, labels) {
        if (!swal) return null;
        const hint = labels.methodHint
            ? `<p class="small text-muted text-start mb-0">${escapeHtml(labels.methodHint)}</p>`
            : '';
        const { value } = await swal.fire({
            ...swalDefaults(labels),
            title: labels.methodTitle,
            html: hint || undefined,
            input: 'radio',
            inputOptions: {
                wallet: labels.wallet,
                voucher: labels.voucher,
            },
            inputValue: 'wallet',
            showCancelButton: true,
            confirmButtonText: labels.continueBtn,
        });
        return value || null;
    }

    async function promptWallet(swal, labels) {
        const amountRes = await swal.fire({
            ...swalDefaults(labels),
            title: labels.amountTitle,
            input: 'text',
            inputValue: '',
            inputAttributes: {
                inputmode: 'decimal',
                autocapitalize: 'off',
                autocorrect: 'off',
                placeholder: labels.amountPlaceholder || '15000',
                'aria-label': labels.amountTitle,
            },
            inputLabel: labels.amountHint || '',
            footer: labels.amountFooter
                ? `<span class="small text-muted">${escapeHtml(labels.amountFooter)}</span>`
                : undefined,
            showCancelButton: true,
            confirmButtonText: labels.continueBtn,
            inputValidator: (v) => validateAmountInput(v, labels),
        });
        if (!amountRes.value && amountRes.value !== 0) return null;

        const parsed = parsePositiveAmount(amountRes.value);
        if (!parsed.ok) return null;

        const refRes = await swal.fire({
            ...swalDefaults(labels),
            title: labels.paymentRefTitle,
            input: 'text',
            inputAttributes: {
                autocapitalize: 'off',
                placeholder: labels.paymentRefPlaceholder || 'RCPT-2026-XXXX',
                'aria-label': labels.paymentRefTitle,
            },
            inputLabel: labels.refHint || '',
            showCancelButton: true,
            confirmButtonText: labels.confirmBtn,
            inputValidator: (v) => validateRequiredText(v, labels),
        });
        if (!refRes.value) return null;

        return {
            createBody: {
                type: PaymentType.Wallet,
                amount: parsed.value,
                paymentChannel: 1,
                serviceChannel: 0,
            },
            gatewayRef: String(refRes.value).trim(),
        };
    }

    async function promptVoucher(axiosManager, swal, labels) {
        const codeRes = await swal.fire({
            ...swalDefaults(labels),
            title: labels.voucherCodeTitle,
            input: 'text',
            inputAttributes: {
                autocapitalize: 'off',
                placeholder: labels.voucherPlaceholder || '',
            },
            inputLabel: labels.voucherHint || '',
            showCancelButton: true,
            confirmButtonText: labels.validateBtn,
            inputValidator: (v) => validateRequiredText(v, {
                ...labels,
                refEmpty: labels.voucherRequired,
                refRequired: labels.voucherRequired,
            }),
        });
        if (!codeRes.value) return null;
        const voucherCode = String(codeRes.value).trim();
        const valRes = await axiosManager.post('/Telecom/ValidateVoucher', { voucherCode });
        const val = valRes?.data?.content ?? valRes?.data?.Content;
        if (!(val?.valid ?? val?.Valid)) {
            await swal.fire({
                icon: 'error',
                title: labels.voucherInvalidTitle || labels.voucherInvalid,
                text: val?.messageAr || val?.MessageAr || labels.voucherInvalid,
                confirmButtonColor: '#c8102e',
            });
            return null;
        }
        if (labels.voucherConfirmTitle) {
            const ok = await swal.fire({
                icon: 'info',
                title: labels.voucherConfirmTitle,
                text: val?.messageAr || val?.MessageAr || '',
                showCancelButton: true,
                cancelButtonText: labels.cancelBtn || 'Cancel',
                confirmButtonText: labels.voucherConfirmBtn,
                confirmButtonColor: '#c8102e',
            });
            if (!ok.isConfirmed) return null;
        }
        return {
            createBody: {
                type: PaymentType.Voucher,
                amount: val?.faceValue ?? val?.FaceValue ?? 0,
                paymentChannel: 2,
                serviceChannel: 0,
                voucherCode,
            },
            gatewayRef: `VCHR-${voucherCode}`,
        };
    }

    /**
     * @param {object} axiosManager AxiosManager
     * @param {object} swal window.Swal
     * @param {{ customerId: string, subscriptionId: string, labels: Record<string,string> }} opts
     * @returns {Promise<object|null>} confirm payload or null if cancelled
     */
    async function runFlow(axiosManager, swal, opts) {
        const customerId = (opts?.customerId || '').trim();
        const subscriptionId = (opts?.subscriptionId || '').trim();
        const labels = opts?.labels || {};
        if (!customerId || !subscriptionId) {
            throw new Error(labels.missingContext || 'Missing recharge context');
        }

        const method = await promptMethod(swal, labels);
        if (!method) return null;

        let draft;
        if (method === 'voucher') {
            draft = await promptVoucher(axiosManager, swal, labels);
        } else {
            draft = await promptWallet(swal, labels);
        }
        if (!draft) return null;

        const createRes = await axiosManager.post('/Telecom/CreatePaymentTransaction', {
            ...draft.createBody,
            customerId,
            subscriptionId,
        });
        const created = createRes?.data?.content ?? createRes?.data?.Content;
        const paymentId = created?.paymentId ?? created?.PaymentId;
        if (!paymentId) {
            throw new Error(labels.draftMissing || 'Payment draft missing');
        }

        const confirmRes = await axiosManager.post('/Telecom/ConfirmPaymentTransaction', {
            paymentId,
            gatewayReference: draft.gatewayRef,
        });
        const confirm = confirmRes?.data?.content ?? confirmRes?.data?.Content;
        if (!(confirm?.success ?? confirm?.Success)) {
            throw new Error(confirm?.messageAr || confirm?.MessageAr || labels.confirmFailed || 'Confirm failed');
        }
        await pollPaymentDetail(axiosManager, paymentId);
        return confirm;
    }

    global.TelecomRechargeFlow = {
        PaymentType,
        DEFAULT_AMOUNT_MAX,
        normalizeDigits,
        parsePositiveAmount,
        validateAmountInput,
        validateMsisdnInput,
        validateRequiredText,
        pollPaymentDetail,
        runFlow,
    };
})(typeof window !== 'undefined' ? window : globalThis);
