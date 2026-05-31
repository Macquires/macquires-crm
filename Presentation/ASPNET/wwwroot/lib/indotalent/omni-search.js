/**
 * Syr-Tel Omni-Search (Ctrl+K) — universal MSISDN / customer lookup.
 */
const OmniSearch = (function () {
    let modalInstance = null;
    let debounceTimer = null;

    function ensureModal() {
        if (document.getElementById('syrOmniModal')) return;
        const rtl = (document.documentElement.getAttribute('dir') || 'ltr') === 'rtl';
        const closeLabel = rtl ? 'إغلاق' : 'Close';
        const placeholder = rtl
            ? 'رقم الاشتراك، MSISDN، الرقم الوطني، أو السجل التجاري…'
            : 'Subscription, MSISDN, national ID, or commercial registry…';
        const html = `
<div class="modal fade syr-omni-modal" id="syrOmniModal" tabindex="-1" aria-hidden="true" aria-labelledby="syrOmniTitle">
  <div class="modal-dialog modal-dialog-centered syr-omni-dialog">
    <div class="modal-content syr-omni-panel" dir="${rtl ? 'rtl' : 'ltr'}">
      <div class="syr-omni-top">
        <div class="syr-omni-search-wrap">
          <i class="bi bi-search syr-omni-search-icon" aria-hidden="true"></i>
          <input type="search" class="syr-omni-input" id="syrOmniInput"
            placeholder="${placeholder}"
            autocomplete="off"
            enterkeyhint="search"
            aria-labelledby="syrOmniTitle" />
        </div>
        <button type="button" class="btn-close syr-omni-close" data-bs-dismiss="modal" aria-label="${closeLabel}"></button>
      </div>
      <p id="syrOmniTitle" class="visually-hidden">${rtl ? 'بحث سريع' : 'Quick search'}</p>
      <div class="syr-omni-results" id="syrOmniResults" role="listbox" aria-live="polite"></div>
      <div class="syr-omni-foot">
        <span><kbd>Ctrl</kbd>+<kbd>K</kbd> ${rtl ? 'لفتح البحث' : 'to open'}</span>
        <span>${rtl ? 'رقم أو هوية أو سجل — حرفان على الأقل' : 'ID or registry — 2+ characters'}</span>
      </div>
    </div>
  </div>
</div>`;
        document.body.insertAdjacentHTML('beforeend', html);
    }

    function setResultsMessage(kind, text) {
        const host = document.getElementById('syrOmniResults');
        if (!host) return;
        const icons = {
            hint: 'bi-keyboard',
            loading: 'bi-arrow-repeat syr-omni-spin',
            empty: 'bi-inbox',
            error: 'bi-exclamation-circle',
            blocked: 'bi-slash-circle',
        };
        const icon = icons[kind] || icons.hint;
        host.innerHTML = `<div class="syr-omni-empty syr-omni-empty--${kind}">
          <i class="bi ${icon}" aria-hidden="true"></i>
          <p>${text}</p>
        </div>`;
    }

    function open() {
        ensureModal();
        const el = document.getElementById('syrOmniModal');
        if (!el || typeof bootstrap === 'undefined') return;
        modalInstance = bootstrap.Modal.getOrCreateInstance(el);
        modalInstance.show();
        setTimeout(() => document.getElementById('syrOmniInput')?.focus(), 150);
        runSearch('');
    }

    function navigateForHit(hit) {
        if (!hit) return;
        const customerId = hit.customerId || hit.CustomerId;
        if (customerId) {
            window.location.href = '/Telecom/Customer360Profile?customerId=' + encodeURIComponent(customerId);
            return;
        }
        const term = hit.msisdn || hit.Msisdn || hit.title || hit.Title || '';
        if (term) {
            window.location.href = '/Telecom/UnifiedSearch?q=' + encodeURIComponent(term);
        }
    }

    function escapeHtml(s) {
        return String(s ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function resultIcon(hit) {
        const t = String(hit.resultType || hit.ResultType || '').toLowerCase();
        if (t.includes('msisdn') || t.includes('line')) return 'bi-telephone';
        if (t.includes('customer') || t.includes('profile')) return 'bi-person-badge';
        if (t.includes('ticket')) return 'bi-ticket';
        return 'bi-search';
    }

    function renderResults(items) {
        const host = document.getElementById('syrOmniResults');
        if (!host) return;
        if (!items || !items.length) {
            setResultsMessage('empty', 'لا نتائج — جرّب رقم اشتراك أو هوية أو سجل تجاري.');
            return;
        }
        host.innerHTML = items
            .slice(0, 12)
            .map((hit, i) => {
                const title = escapeHtml(hit.title || hit.Title || '—');
                const sub = escapeHtml(hit.subtitle || hit.Subtitle || hit.resultType || hit.ResultType || '');
                const icon = resultIcon(hit);
                return `<a href="#" class="syr-omni-result" data-idx="${i}" role="option">
                    <span class="syr-omni-result-icon" aria-hidden="true"><i class="bi ${icon}"></i></span>
                    <span class="syr-omni-result-body">
                      <strong>${title}</strong>
                      ${sub ? `<small>${sub}</small>` : ''}
                    </span>
                    <i class="bi bi-chevron-left syr-omni-result-chevron" aria-hidden="true"></i>
                </a>`;
            })
            .join('');

        host.querySelectorAll('.syr-omni-result').forEach((a) => {
            a.addEventListener('click', function (ev) {
                ev.preventDefault();
                const idx = parseInt(a.getAttribute('data-idx'), 10);
                navigateForHit(items[idx]);
                modalInstance?.hide();
            });
        });
    }

    async function runSearch(term) {
        if (!term || term.length < 2) {
            setResultsMessage('hint', 'اكتب حرفين على الأقل للبحث.');
            return;
        }
        setResultsMessage('loading', 'جاري البحث…');
        const digits = term.replace(/[٠-٩۰-۹]/g, (ch) => {
            const cp = ch.codePointAt(0);
            if (cp >= 0x0660 && cp <= 0x0669) return String(cp - 0x0660);
            if (cp >= 0x06f0 && cp <= 0x06f9) return String(cp - 0x06f0);
            return ch;
        }).replace(/\D/g, '');
        if (digits.length < 2 && /[\p{L}]/u.test(term)) {
            setResultsMessage('blocked', 'البحث بالاسم غير مسموح — استخدم رقماً أو سجلاً تجارياً.');
            return;
        }
        try {
            const q =
                '/Telecom/GetTelecomUniversalSearch?term=' +
                encodeURIComponent(term) +
                '&profilesOnly=false';
            const res = await AxiosManager.get(q, {});
            const data =
                typeof StorageManager !== 'undefined' && typeof StorageManager.apiList === 'function'
                    ? StorageManager.apiList(res)
                    : res?.data?.content?.data || res?.data?.content?.Data || [];
            renderResults(Array.isArray(data) ? data : []);
        } catch (e) {
            console.error('OmniSearch', e);
            setResultsMessage('error', 'تعذّر البحث — تحقق من الاتصال وحاول مجدداً.');
        }
    }

    function bindKeys() {
        document.addEventListener('keydown', function (ev) {
            if ((ev.ctrlKey || ev.metaKey) && ev.key === 'k') {
                ev.preventDefault();
                open();
            }
        });
    }

    function bindUi() {
        ensureModal();
        const input = document.getElementById('syrOmniInput');
        if (input && !input.dataset.bound) {
            input.dataset.bound = '1';
            input.addEventListener('input', function () {
                clearTimeout(debounceTimer);
                const v = input.value.trim();
                debounceTimer = setTimeout(() => runSearch(v), 280);
            });
        }
        document.querySelectorAll('[data-syr-omni-open]').forEach((btn) => {
            btn.addEventListener('click', function (ev) {
                ev.preventDefault();
                open();
            });
        });
    }

    function init() {
        bindKeys();
        bindUi();
    }

    return { init, open };
})();
