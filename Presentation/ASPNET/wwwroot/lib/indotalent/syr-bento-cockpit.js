/**
 * Data-driven Bento dashboard — widgets from GET /Dashboard/GetMyWidgets, values from providers.
 */
const SyrBentoCockpit = (function () {
    const PREVIEW_KEY = 'syrPreviewPersona';
    const WIDGET_KIND = { Stat: 0, Cta: 1, OmniSearch: 2, StatusList: 3 };

    const PERSONA_SUBTITLE_KEYS = {
        Executive: 'personaExecutive',
        CallCenter: 'personaCallCenter',
        Retail: 'personaRetail',
        BackOffice: 'personaBackOffice',
        SysAdmin: 'personaSysAdmin',
    };

    function bdT(key) {
        const hit = typeof TelecomI18n !== 'undefined' && TelecomI18n.t ? TelecomI18n.t('defaultDashboard.cockpit.' + key) : null;
        return hit || key;
    }

    const pollTimers = new Map();
    let mountedWidgets = [];
    let mountGeneration = 0;
    let widgetsLoadPromise = null;
    let widgetBatchPromise = null;

    function getLang() {
        return (document.documentElement.lang || '').toLowerCase().startsWith('en') ? 'en' : 'ar';
    }

    function getPreviewPersona() {
        return sessionStorage.getItem(PREVIEW_KEY) || '';
    }

    function getPersona() {
        return getPreviewPersona() || StorageManager.getPrimaryMenuPersona() || '';
    }

    function clearPollers() {
        pollTimers.forEach((t) => clearInterval(t));
        pollTimers.clear();
    }

    function escapeHtml(s) {
        if (s == null) return '';
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function gridSpan(w) {
        const g = w.gridSize ?? w.GridSize ?? 6;
        const n = typeof g === 'number' ? g : parseInt(g, 10);
        if (n === 4 || n === 6 || n === 8 || n === 12) return n;
        return 6;
    }

    function widgetKind(w) {
        const k = w.widgetKind ?? w.WidgetKind;
        if (typeof k === 'number') return k;
        if (k === 'OmniSearch') return WIDGET_KIND.OmniSearch;
        if (k === 'Cta') return WIDGET_KIND.Cta;
        if (k === 'StatusList') return WIDGET_KIND.StatusList;
        return WIDGET_KIND.Stat;
    }

    function localizedTitle(w) {
        const useEn = getLang() === 'en';
        return escapeHtml(useEn ? w.titleEn || w.TitleEn || w.titleAr || w.TitleAr : w.titleAr || w.TitleAr);
    }

    function localizedCtaLabel(w) {
        const useEn = getLang() === 'en';
        return escapeHtml(
            useEn
                ? w.ctaLabelEn || w.CtaLabelEn || w.ctaLabelAr || w.CtaLabelAr
                : w.ctaLabelAr || w.CtaLabelAr || bdT('ctaOpen')
        );
    }

    async function fetchWidgets() {
        if (widgetsLoadPromise) {
            return widgetsLoadPromise;
        }
        widgetsLoadPromise = (async function () {
            const preview = getPreviewPersona();
            let url = '/Dashboard/GetMyWidgets';
            if (preview) {
                url += '?previewPersona=' + encodeURIComponent(preview);
            }
            const res = await AxiosManager.get(url, {});
            const content = res?.data?.content ?? {};
            const list = content.data ?? content.Data ?? [];
            const primary = content.primaryMenuPersona ?? content.PrimaryMenuPersona;
            if (primary && !preview) {
                StorageManager.savePrimaryMenuPersona(primary);
            }
            return Array.isArray(list) ? list : [];
        })().finally(function () {
            widgetsLoadPromise = null;
        });
        return widgetsLoadPromise;
    }

    async function fetchWidgetData(providerKey) {
        if (!providerKey) return null;
        const preview = getPreviewPersona();
        let url = '/Dashboard/GetWidgetData?providerKey=' + encodeURIComponent(providerKey);
        if (preview) {
            url += '&previewPersona=' + encodeURIComponent(preview);
        }
        const res = await AxiosManager.get(url, {});
        return res?.data?.content?.data ?? res?.data?.content?.Data ?? null;
    }

    async function fetchWidgetDataBatch(providerKeys) {
        const keys = [...new Set((providerKeys || []).filter(Boolean))].sort();
        if (!keys.length) return {};
        const cacheKey = keys.join('|') + '|' + (getPreviewPersona() || '');
        if (widgetBatchPromise && widgetBatchPromise.key === cacheKey) {
            return widgetBatchPromise.promise;
        }
        const preview = getPreviewPersona();
        const promise = AxiosManager.post('/Dashboard/GetWidgetDataBatch', {
            providerKeys: keys,
            previewPersona: preview || null,
        })
            .then(function (res) {
                return res?.data?.content?.data ?? res?.data?.content?.Data ?? {};
            })
            .finally(function () {
                if (widgetBatchPromise && widgetBatchPromise.key === cacheKey) {
                    widgetBatchPromise = null;
                }
            });
        widgetBatchPromise = { key: cacheKey, promise: promise };
        return promise;
    }

    function statusClass(status) {
        if (status === 'error') return 'syr-bento-status-error';
        if (status === 'warn') return 'syr-bento-status-warn';
        return 'syr-bento-status-ok';
    }

    function renderValueBody(w, data) {
        const kind = widgetKind(w);
        if (kind === WIDGET_KIND.OmniSearch) {
            return `<input type="search" class="syr-bento-search-lg syr-bento-omni-inline" placeholder="${bdT('omniPlaceholder')}" autocomplete="off" />`;
        }
        if (kind === WIDGET_KIND.Cta) {
            const url = w.ctaUrl || w.CtaUrl || '#';
            const label = localizedCtaLabel(w);
            const val = data?.valueText ?? data?.ValueText;
            const sub = data?.subtitle ?? data?.Subtitle;
            return `
                ${val ? `<div class="syr-bento-value ${statusClass(data?.status ?? data?.Status)}">${escapeHtml(val)}</div>` : ''}
                ${sub ? `<p class="small text-muted mb-2">${escapeHtml(sub)}</p>` : ''}
                <a class="syr-bento-cta" href="${escapeHtml(url)}">${label}</a>`;
        }
        if (kind === WIDGET_KIND.StatusList && data) {
            const items = data.items ?? data.Items ?? [];
            if (!items.length) {
                return `<p class="text-muted small mb-0">${bdT('na')}</p>`;
            }
            return `<ul class="list-unstyled mb-0 syr-bento-status-list">
                ${items
                    .map(
                        (it) =>
                            `<li class="d-flex justify-content-between py-1 border-bottom border-light-subtle">
                                <span>${escapeHtml(it.label ?? it.Label)}</span>
                                <span class="${statusClass(it.status ?? it.Status)}">${escapeHtml(it.value ?? it.Value)}</span>
                            </li>`
                    )
                    .join('')}
            </ul>`;
        }
        if (!data) {
            return `<p class="syr-bento-value text-muted mb-0"><span class="spinner-border spinner-border-sm"></span></p>`;
        }
        const val = data.valueText ?? data.ValueText ?? bdT('na');
        const sub = data.subtitle ?? data.Subtitle;
        return `
            <p class="syr-bento-value mb-0 ${statusClass(data.status ?? data.Status)}">${escapeHtml(val)}</p>
            ${sub ? `<p class="small text-muted mt-1 mb-0">${escapeHtml(sub)}</p>` : ''}`;
    }

    function buildCardShell(w) {
        const span = gridSpan(w);
        const icon = w.icon || w.Icon || 'bi-grid';
        const pk = w.providerKey || w.ProviderKey || '';
        return `<div class="syr-bento-card span-${span}" data-widget-id="${escapeHtml(w.id ?? w.Id)}"
            data-provider-key="${escapeHtml(pk)}" data-widget-kind="${widgetKind(w)}"
            data-refresh-sec="${w.refreshIntervalSeconds ?? w.RefreshIntervalSeconds ?? ''}">
            <h2><i class="bi ${escapeHtml(icon)} me-1"></i>${localizedTitle(w)}</h2>
            <div class="syr-bento-card-body">${renderValueBody(w, null)}</div>
        </div>`;
    }

    function updateCardBody(cardEl, w, data) {
        const body = cardEl.querySelector('.syr-bento-card-body');
        if (body) {
            body.innerHTML = renderValueBody(w, data);
        }
    }

    async function refreshCard(cardEl, w) {
        const pk = w.providerKey || w.ProviderKey;
        const kind = widgetKind(w);
        if (!pk) {
            if (kind === WIDGET_KIND.Cta || kind === WIDGET_KIND.OmniSearch) {
                updateCardBody(cardEl, w, {});
            }
            return;
        }
        try {
            const data = await fetchWidgetData(pk);
            updateCardBody(cardEl, w, data);
        } catch (e) {
            console.warn('Widget data', pk, e);
            updateCardBody(cardEl, w, {
                valueText: bdT('na'),
                status: 'error',
                subtitle: bdT('dataLoadFailed'),
            });
        }
    }

    function schedulePoll(cardEl, w) {
        const pk = w.providerKey || w.ProviderKey;
        const sec = w.refreshIntervalSeconds ?? w.RefreshIntervalSeconds;
        const id = w.id ?? w.Id;
        if (!pk || !sec || sec < 5) return;
        if (pollTimers.has(id)) {
            clearInterval(pollTimers.get(id));
        }
        const timer = setInterval(() => refreshCard(cardEl, w), sec * 1000);
        pollTimers.set(id, timer);
    }

    function bindOmniInputs(container) {
        container.querySelectorAll('.syr-bento-omni-inline').forEach((inp) => {
            if (inp.dataset.bound) return;
            inp.dataset.bound = '1';
            inp.addEventListener('keydown', function (ev) {
                if (ev.key === 'Enter' && inp.value.trim().length >= 2 && typeof OmniSearch !== 'undefined') {
                    OmniSearch.open();
                    const modalInp = document.getElementById('syrOmniInput');
                    if (modalInp) modalInp.value = inp.value.trim();
                }
            });
        });
    }

    async function renderGrid(containerEl, widgets) {
        clearPollers();
        mountedWidgets = widgets;
        if (!widgets.length) {
            containerEl.innerHTML = '<p class="text-muted col-12">' + escapeHtml(bdT('noWidgets')) + '</p>';
            return;
        }
        containerEl.innerHTML = widgets.map(buildCardShell).join('');
        bindOmniInputs(containerEl);

        const providerKeys = widgets.map((w) => w.providerKey || w.ProviderKey).filter(Boolean);
        let batchData = {};
        try {
            batchData = await fetchWidgetDataBatch(providerKeys);
        } catch (e) {
            console.warn('Widget batch load', e);
        }

        const cards = containerEl.querySelectorAll('.syr-bento-card');
        const tasks = [];
        cards.forEach((cardEl, idx) => {
            const w = widgets[idx];
            if (!w) return;
            const pk = w.providerKey || w.ProviderKey;
            if (pk && batchData[pk]) {
                updateCardBody(cardEl, w, batchData[pk]);
                schedulePoll(cardEl, w);
            } else {
                tasks.push(refreshCard(cardEl, w).then(() => schedulePoll(cardEl, w)));
            }
        });
        await Promise.allSettled(tasks);
    }

    function updateSubtitle(persona) {
        const subtitle = document.getElementById('syrBentoSubtitle');
        if (!subtitle) {
            return;
        }
        const key = PERSONA_SUBTITLE_KEYS[persona];
        subtitle.textContent = key ? bdT(key) : bdT('dynamicSubtitle');
        subtitle.removeAttribute('data-telecom-i18n');
    }

    function applyPageChromeI18n() {
        const slim = document.getElementById('telecomOperatorDashSlim');
        if (slim && typeof TelecomI18n !== 'undefined') {
            slim.setAttribute('dir', document.documentElement.getAttribute('dir') || 'rtl');
            slim.setAttribute('lang', document.documentElement.getAttribute('lang') || 'ar');
            if (TelecomI18n.applyDomI18n) {
                TelecomI18n.applyDomI18n(slim);
            }
        }
        const title = typeof TelecomI18n !== 'undefined' && TelecomI18n.t ? TelecomI18n.t('defaultDashboard.pageTitle') : null;
        if (title) {
            document.title = title;
        }
        updateSubtitle(getPersona());
    }

    async function mount(containerEl) {
        if (!containerEl) return;
        const gen = ++mountGeneration;
        const persona = getPersona();
        updateSubtitle(persona);
        containerEl.setAttribute('data-persona', persona);
        document.body.setAttribute('data-persona', persona);

        applyPageChromeI18n();
        containerEl.innerHTML =
            '<p class="text-muted col-12"><span class="spinner-border spinner-border-sm"></span> ' +
            escapeHtml(bdT('loadingPanel')) +
            '</p>';

        try {
            const widgets = await fetchWidgets();
            if (gen !== mountGeneration) {
                return;
            }
            await renderGrid(containerEl, widgets);
        } catch (e) {
            if (gen !== mountGeneration) {
                return;
            }
            console.error('SyrBentoCockpit', e);
            const canceled =
                e?.code === 'ERR_CANCELED' ||
                (e?.message && /canceled|aborted/i.test(e.message));
            containerEl.innerHTML = canceled
                ? '<p class="text-muted col-12">' + escapeHtml(bdT('loadCanceled')) + '</p>'
                : '<p class="text-danger col-12">' + escapeHtml(bdT('loadError')) + '</p>';
        }
    }

    async function remount() {
        let host = document.getElementById('syrBentoCockpit');
        if (!host) {
            const slim = document.getElementById('telecomOperatorDashSlim');
            if (slim) host = ensureShell(slim);
        }
        if (host) await mount(host);
    }

    function ensureShell(slimRoot) {
        const existing = document.getElementById('syrBentoCockpit');
        if (existing) {
            return existing;
        }
        const fluid = slimRoot.querySelector('.syriatel-dash-slim') || slimRoot.querySelector('.container-fluid') || slimRoot;
        fluid.innerHTML = `
            <header class="mb-4">
                <h1 class="h4 fw-semibold text-syriatel-red mb-1" data-telecom-i18n="defaultDashboard.title">Syriatel — Executive dashboard</h1>
                <p class="text-muted small mb-0" id="syrBentoSubtitle" data-telecom-i18n="defaultDashboard.loading">Loading…</p>
            </header>
            <div id="syrBentoCockpit" class="syr-bento-grid" data-persona=""></div>`;
        applyPageChromeI18n();
        return document.getElementById('syrBentoCockpit');
    }

    document.documentElement.addEventListener('syriatel-locale-changed', function () {
        applyPageChromeI18n();
        const host = document.getElementById('syrBentoCockpit');
        if (host && mountedWidgets.length) {
            renderGrid(host, mountedWidgets);
        }
    });

    window.addEventListener('beforeunload', clearPollers);

    return { mount, remount, ensureShell, getPersona, clearPollers };
})();
