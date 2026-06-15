/**
 * Canonical VAS toggle for C360 / List / Hub.
 * Backend `/Vas/ToggleSubscriberVasService` creates a ServiceModification audit trail internally.
 */
(function (global) {
    'use strict';

    const Action = { Activate: 0, Deactivate: 1 };

    function pickError(e) {
        const d = e?.response?.data;
        if (!d) return e?.message ? String(e.message) : '';
        const inner = d.error?.innerException;
        const m = d.error?.message ?? d.message ?? (typeof inner === 'string' ? inner : '') ?? '';
        return String(m);
    }

    function isBusinessRuleViolation(e) {
        return e?.response?.data?.error?.name === 'BusinessRuleViolationException';
    }

    /**
     * @param {object} axiosManager AxiosManager
     * @param {{ msisdn: string, serviceCode: string, action?: number, activate?: boolean }} opts
     */
    async function toggle(axiosManager, opts) {
        const msisdn = (opts?.msisdn || '').trim();
        const serviceCode = (opts?.serviceCode || '').trim();
        if (!msisdn || !serviceCode) {
            return { ok: false, code: 'MISSING_INPUT' };
        }
        let action = opts?.action;
        if (action == null) {
            action = opts?.activate === false ? Action.Deactivate : Action.Activate;
        }
        const res = await axiosManager.post('/Vas/ToggleSubscriberVasService', {
            msisdn,
            serviceCode,
            action,
        });
        if (res?.data?.code === 200) {
            const content = res?.data?.content ?? {};
            return {
                ok: true,
                isActive: !!(content.isActive ?? content.IsActive),
                operationNumber: String(content.operationNumber ?? content.OperationNumber ?? '').trim(),
            };
        }
        throw Object.assign(new Error(res?.data?.message || 'VAS toggle failed'), { response: res });
    }

    global.TelecomVasToggle = {
        Action,
        toggle,
        pickError,
        isBusinessRuleViolation,
    };
})(typeof window !== 'undefined' ? window : globalThis);
