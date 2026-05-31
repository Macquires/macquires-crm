/**
 * Floating admin tools — persona preview (TelecomAdmin only).
 */
const PortalAdminTools = (function () {
    const PREVIEW_KEY = PortalNavigation.PREVIEW_KEY;
    const PERSONA_ORDER = ['', 'Executive', 'CallCenter', 'Retail', 'BackOffice', 'SysAdmin'];

    function getLang() {
        return (document.documentElement.lang || '').toLowerCase().startsWith('en') ? 'en' : 'ar';
    }

    function canPreview() {
        return (StorageManager.getUserRoles() || []).some(
            (r) => r === 'TelecomAdmin' || r === 'TelecomManagement'
        );
    }

    function isDashboard() {
        return /\/Dashboards\/DefaultDashboard/i.test(window.location.pathname);
    }

    function buildSelectOptions() {
        const labels = PortalNavigation.PERSONA_LABELS;
        const ar = getLang() !== 'en';
        const defaultLabel = ar ? 'الدور الفعلي' : 'Actual role';
        let html = `<option value="">${defaultLabel}</option>`;
        PERSONA_ORDER.filter(Boolean).forEach((key) => {
            html += `<option value="${key}">${labels[key] || key}</option>`;
        });
        return html;
    }

    let remountTimer;

    async function applyPreview(value) {
        if (value) {
            sessionStorage.setItem(PREVIEW_KEY, value);
        } else {
            sessionStorage.removeItem(PREVIEW_KEY);
        }
        await PortalNavigation.reloadMenuForPreview(value || null);
        PortalNavigation.render('syrPortalNav');
        if (isDashboard() && typeof SyrBentoCockpit !== 'undefined') {
            clearTimeout(remountTimer);
            remountTimer = setTimeout(function () {
                SyrBentoCockpit.remount();
            }, 400);
        }
    }

    function ensureFab() {
        if (!canPreview()) {
            return;
        }
        if (document.getElementById('syrAdminFab')) {
            return;
        }

        const ar = getLang() !== 'en';
        const wrap = document.createElement('div');
        wrap.id = 'syrAdminFab';
        wrap.className = 'syr-admin-fab';
        wrap.innerHTML = `
            <button type="button" class="syr-admin-fab-btn" aria-label="${ar ? 'أدوات المسؤول' : 'Admin tools'}" aria-expanded="false">
                <i class="bi bi-person-gear"></i>
            </button>
            <div class="syr-admin-fab-panel" hidden>
                <p class="syr-admin-fab-title">${ar ? 'معاينة الدور' : 'Role preview'}</p>
                <select class="syr-admin-fab-select form-select form-select-sm" aria-label="${ar ? 'معاينة الدور' : 'Role preview'}">
                    ${buildSelectOptions()}
                </select>
                <p class="syr-admin-fab-hint">${ar ? 'للاختبار فقط — لا يغيّر صلاحياتك الفعلية' : 'Test only — does not change your real permissions'}</p>
            </div>`;
        document.body.appendChild(wrap);

        const btn = wrap.querySelector('.syr-admin-fab-btn');
        const panel = wrap.querySelector('.syr-admin-fab-panel');
        const select = wrap.querySelector('.syr-admin-fab-select');

        const saved = sessionStorage.getItem(PREVIEW_KEY) || '';
        select.value = saved;

        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            const open = panel.hasAttribute('hidden');
            if (open) {
                panel.removeAttribute('hidden');
                btn.setAttribute('aria-expanded', 'true');
                wrap.classList.add('is-open');
            } else {
                panel.setAttribute('hidden', '');
                btn.setAttribute('aria-expanded', 'false');
                wrap.classList.remove('is-open');
            }
        });

        select.addEventListener('change', function () {
            applyPreview(select.value);
        });

        document.addEventListener('click', function (e) {
            if (!wrap.contains(e.target)) {
                panel.setAttribute('hidden', '');
                btn.setAttribute('aria-expanded', 'false');
                wrap.classList.remove('is-open');
            }
        });
    }

    function init() {
        if (typeof PortalNavigation === 'undefined') {
            return;
        }
        ensureFab();
    }

    return { init, applyPreview, canPreview };
})();
