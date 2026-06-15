/**
 * Canonical prepaid recharge flow for C360 / List / Hub.
 * CreatePaymentTransaction → ConfirmPaymentTransaction → poll detail.
 */
(function (global) {
    'use strict';

    const PaymentType = { Wallet: 0, Voucher: 1 };

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
        const { value } = await swal.fire({
            title: labels.methodTitle,
            input: 'radio',
            inputOptions: {
                wallet: labels.wallet,
                voucher: labels.voucher,
            },
            inputValue: 'wallet',
            showCancelButton: true,
            confirmButtonText: labels.continueBtn,
            cancelButtonText: labels.cancelBtn,
        });
        return value || null;
    }

    async function promptWallet(swal, labels) {
        const amountRes = await swal.fire({
            title: labels.amountTitle,
            input: 'number',
            inputPlaceholder: labels.amountPlaceholder || '15000',
            showCancelButton: true,
            confirmButtonText: labels.continueBtn,
            inputValidator: (v) => {
                const n = parseFloat(v);
                if (!v || Number.isNaN(n) || n <= 0) return labels.invalidAmount;
            },
        });
        if (!amountRes.value) return null;
        const refRes = await swal.fire({
            title: labels.paymentRefTitle,
            input: 'text',
            inputPlaceholder: labels.paymentRefPlaceholder || '',
            showCancelButton: true,
            confirmButtonText: labels.confirmBtn,
            inputValidator: (v) => (!v || !String(v).trim() ? labels.refRequired : undefined),
        });
        if (!refRes.value) return null;
        return {
            createBody: {
                type: PaymentType.Wallet,
                amount: parseFloat(amountRes.value),
                paymentChannel: 1,
                serviceChannel: 0,
            },
            gatewayRef: String(refRes.value).trim(),
        };
    }

    async function promptVoucher(axiosManager, swal, labels) {
        const codeRes = await swal.fire({
            title: labels.voucherCodeTitle,
            input: 'text',
            inputPlaceholder: labels.voucherPlaceholder || '',
            showCancelButton: true,
            confirmButtonText: labels.validateBtn,
            inputValidator: (v) => (!v || !String(v).trim() ? labels.voucherRequired : undefined),
        });
        if (!codeRes.value) return null;
        const voucherCode = String(codeRes.value).trim();
        const valRes = await axiosManager.post('/Telecom/ValidateVoucher', { voucherCode });
        const val = valRes?.data?.content ?? valRes?.data?.Content;
        if (!(val?.valid ?? val?.Valid)) {
            await swal.fire({
                icon: 'error',
                title: val?.messageAr || val?.MessageAr || labels.voucherInvalid,
            });
            return null;
        }
        if (labels.voucherConfirmTitle) {
            const ok = await swal.fire({
                icon: 'info',
                title: labels.voucherConfirmTitle,
                text: val?.messageAr || val?.MessageAr || '',
                showCancelButton: true,
                confirmButtonText: labels.voucherConfirmBtn,
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
        pollPaymentDetail,
        runFlow,
    };
})(typeof window !== 'undefined' ? window : globalThis);
