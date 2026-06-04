(function () {
    'use strict';

    /**
     * UI language for grids: trust <html lang> / <body lang>, then <html dir>, then localStorage.
     * (Removed lang-vs-dir overrides that could force English while the toggle showed Arabic.)
     */
    function getUiLang() {
        var raw = '';
        try {
            raw = (document.documentElement && document.documentElement.getAttribute('lang'))
                || (document.body && document.body.getAttribute('lang'))
                || '';
        } catch (ignore) { /* empty */ }
        var l = String(raw).toLowerCase();
        if (l.indexOf('ar') === 0) return 'ar';
        if (l.indexOf('en') === 0) return 'en';

        try {
            var d = document.documentElement && document.documentElement.getAttribute('dir');
            if (d === 'rtl') return 'ar';
            if (d === 'ltr') return 'en';
        } catch (ignore2) { /* empty */ }

        try {
            var s = localStorage.getItem('syriatelUiLang');
            if (s === 'ar' || s === 'en') return s === 'en' ? 'en' : 'ar';
        } catch (ignore3) { /* empty */ }

        var nav = (typeof navigator !== 'undefined' && navigator.language) ? String(navigator.language).toLowerCase() : '';
        return nav.indexOf('en') === 0 ? 'en' : 'ar';
    }

    function isRtl() {
        return document.documentElement.getAttribute('dir') === 'rtl';
    }

    /** One language at a time: Arabic or English from `cur`, following getUiLang(). */
    function pickUiLangString(cur) {
        if (!cur || typeof cur.ar !== 'string' || typeof cur.en !== 'string') return '';
        return getUiLang() === 'ar' ? cur.ar : cur.en;
    }

    function resolveLabelsOrFormEntry(pack, key) {
        if (!pack || typeof pack !== 'object' || !key) return null;
        var k = String(key);
        var labels = pack.labels;
        var form = pack.form;
        function ok(cur) {
            return cur && typeof cur.ar === 'string' && typeof cur.en === 'string';
        }
        if (labels && ok(labels[k])) return labels[k];
        if (form && ok(form[k])) return form[k];
        return null;
    }

    /** Entity modal title (add/edit/delete) — Arabic OR English */
    function modalBilingual(entityKey, verbKey) {
        var pack = window.__UI_MODALS;
        if (!pack || typeof pack !== 'object') {
            return String(entityKey) + '.' + String(verbKey);
        }
        var cur = pack[entityKey] && pack[entityKey][verbKey];
        if (!cur || typeof cur.ar !== 'string' || typeof cur.en !== 'string') {
            return String(entityKey) + '.' + String(verbKey);
        }
        return pickUiLangString(cur);
    }

    /** Shared label from `labels` or `form` — Arabic OR English */
    function modalLabel(key) {
        var cur = resolveLabelsOrFormEntry(window.__UI_MODALS, key);
        var s = pickUiLangString(cur);
        return s || String(key);
    }

    /** Any `labels` / `form` string by key — Arabic OR English */
    function uiFormString(key) {
        var cur = resolveLabelsOrFormEntry(window.__UI_MODALS, key);
        var s = pickUiLangString(cur);
        return s || String(key);
    }

    /**
     * Placeholder by English source string (`ui-modals.json` → `phByEn`).
     * Evaluated at call time so language toggle applies on next control create / refresh.
     */
    function placeholderByEnglish(en) {
        var s = String(en || '');
        var pack = window.__UI_MODALS;
        var cur = pack && pack.phByEn && pack.phByEn[s];
        if (cur && typeof cur.ar === 'string' && typeof cur.en === 'string') {
            return pickUiLangString(cur);
        }
        return s;
    }

    window.MacquiresUiI18n = {
        mb: modalBilingual,
        lbl: modalLabel,
        t: uiFormString,
        phByEn: placeholderByEnglish,
        getUiLang: getUiLang
    };

    /** Reactive modal footer strings for v-text="$mcqFooter.*" (Vue overwrites directive-only / empty spans). */
    var mcqFooterReactive = null;
    var mcqFooterLocaleHooksDone = false;
    function fillMcqFooterReactiveLabels() {
        if (!mcqFooterReactive || !window.MacquiresUiI18n) return;
        try {
            mcqFooterReactive.close = MacquiresUiI18n.lbl('close');
            mcqFooterReactive.save = MacquiresUiI18n.lbl('save');
            mcqFooterReactive.del = MacquiresUiI18n.lbl('delete');
            mcqFooterReactive.saving = MacquiresUiI18n.lbl('saving');
            mcqFooterReactive.deleting = MacquiresUiI18n.lbl('deleting');
            mcqFooterReactive.submit = MacquiresUiI18n.lbl('submit');
            mcqFooterReactive.submitting = MacquiresUiI18n.lbl('submitting');
        } catch (ignore) { /* empty */ }
    }

    function ensureMcqFooterReactive(V) {
        if (mcqFooterReactive) return mcqFooterReactive;
        try {
            if (!V || typeof V.reactive !== 'function') return null;
            mcqFooterReactive = V.reactive({
                close: 'Close',
                save: 'Save',
                del: 'Delete',
                saving: 'Saving...',
                deleting: 'Deleting...',
                submit: 'Submit',
                submitting: 'Submitting...'
            });
            if (!mcqFooterLocaleHooksDone) {
                mcqFooterLocaleHooksDone = true;
                document.documentElement.addEventListener('syriatel-locale-changed', fillMcqFooterReactiveLabels);
                document.documentElement.addEventListener('syriatel-ui-modals-loaded', fillMcqFooterReactiveLabels);
            }
            fillMcqFooterReactiveLabels();
            return mcqFooterReactive;
        } catch (ignore2) { /* empty */ }
        return null;
    }

    function applyPlaceholderToNode(pel, ps) {
        if (!pel || !ps) return;
        try {
            var inst0 = pel.ej2_instances && pel.ej2_instances[0];
            if (inst0 && typeof inst0.setProperties === 'function') {
                inst0.setProperties({ placeholder: ps });
                return;
            }
        } catch (ignore) { /* empty */ }
        var ctrls = pel.querySelectorAll ? pel.querySelectorAll('.e-control') : [];
        for (var q = 0; q < ctrls.length; q++) {
            var inst = ctrls[q].ej2_instances && ctrls[q].ej2_instances[0];
            if (inst && typeof inst.setProperties === 'function') {
                try {
                    inst.setProperties({ placeholder: ps });
                    return;
                } catch (ignore2) { /* empty */ }
            }
        }
        var tag = pel.tagName ? pel.tagName.toUpperCase() : '';
        if (tag === 'INPUT' || tag === 'TEXTAREA') {
            try {
                pel.setAttribute('placeholder', ps);
            } catch (ignore3) { /* empty */ }
        }
    }

    function applyModalChromeI18n() {
        var pack = window.__UI_MODALS;
        if (pack && typeof pack === 'object') {
            var nodes = document.querySelectorAll('[data-mcq-i18n]');
            for (var i = 0; i < nodes.length; i++) {
                var el = nodes[i];
                var k = el.getAttribute('data-mcq-i18n');
                if (!k) continue;
                var cur = resolveLabelsOrFormEntry(pack, k);
                var s = pickUiLangString(cur);
                if (s) el.textContent = s;
            }
            var phNodes = document.querySelectorAll('[data-mcq-ph]');
            for (var j = 0; j < phNodes.length; j++) {
                var pel = phNodes[j];
                var pk = pel.getAttribute('data-mcq-ph');
                if (!pk) continue;
                var pcur = resolveLabelsOrFormEntry(pack, pk);
                var ps = pickUiLangString(pcur);
                if (ps) {
                    applyPlaceholderToNode(pel, ps);
                }
            }
            var closeLabel = pickUiLangString(resolveLabelsOrFormEntry(pack, 'close'));
            if (closeLabel) {
                var closes = document.querySelectorAll('.modal .btn-close[data-bs-dismiss="modal"]');
                for (var c = 0; c < closes.length; c++) {
                    try {
                        closes[c].setAttribute('aria-label', closeLabel);
                    } catch (ignore) { /* empty */ }
                }
            }
        }
        try {
            var Vw = window.Vue;
            if (Vw && typeof Vw.reactive === 'function') {
                ensureMcqFooterReactive(Vw);
            }
        } catch (ignoreVw) { /* empty */ }
        fillMcqFooterReactiveLabels();
    }

    /**
     * Vue often re-patches the modal right after open (e.g. state.mainTitle from MacquiresUiI18n.mb), which resets
     * static footer text to the English template. Re-run chrome i18n after Vue's DOM flush (double nextTick).
     */
    function scheduleApplyModalChromeAfterVueFlush() {
        try {
            var V = window.Vue;
            if (V && typeof V.nextTick === 'function') {
                V.nextTick(function () {
                    applyModalChromeI18n();
                    V.nextTick(function () {
                        applyModalChromeI18n();
                        try {
                            if (typeof requestAnimationFrame === 'function') {
                                requestAnimationFrame(function () {
                                    applyModalChromeI18n();
                                    requestAnimationFrame(applyModalChromeI18n);
                                });
                            }
                        } catch (ignoreRaf) { /* empty */ }
                    });
                });
                return;
            }
        } catch (ignore) { /* empty */ }
        try {
            if (typeof queueMicrotask === 'function') {
                queueMicrotask(function () {
                    applyModalChromeI18n();
                });
            }
        } catch (ignore2) { /* empty */ }
        try {
            setTimeout(applyModalChromeI18n, 0);
        } catch (ignore3) { /* empty */ }
    }

    var mcqApplyChromeDeb = null;
    function debouncedApplyModalChromeWhenModalOpen() {
        try {
            if (!document.querySelector('.modal.show')) return;
        } catch (ignore) { /* empty */ }
        if (mcqApplyChromeDeb) {
            try {
                clearTimeout(mcqApplyChromeDeb);
            } catch (ignoreC) { /* empty */ }
        }
        mcqApplyChromeDeb = setTimeout(function () {
            mcqApplyChromeDeb = null;
            try {
                if (!document.querySelector('.modal.show')) return;
            } catch (ignore2) { /* empty */ }
            applyModalChromeI18n();
        }, 32);
    }

    /** Re-sync [data-mcq-i18n] inside open modals after Vue patches DOM. */
    function patchVueCreateAppForModalChrome() {
        try {
            var V = window.Vue;
            if (!V || typeof V.createApp !== 'function' || V.__mcqModalChromeCreatePatched) return;
            Object.defineProperty(V, '__mcqModalChromeCreatePatched', { value: true, enumerable: false, configurable: false });
            var orig = V.createApp;
            V.createApp = function () {
                var app = orig.apply(this, arguments);
                try {
                    var foot = ensureMcqFooterReactive(V);
                    if (foot) {
                        app.config.globalProperties.$mcqFooter = foot;
                    }
                } catch (ignoreGp) { /* empty */ }
                app.mixin({
                    updated: function () {
                        try {
                            if (this.$root && this !== this.$root) return;
                        } catch (ignoreR) { /* empty */ }
                        debouncedApplyModalChromeWhenModalOpen();
                    }
                });
                return app;
            };
        } catch (ignore) { /* empty */ }
    }

    function walkColumns(cols, visitor) {
        if (!cols || !cols.length) return;
        for (var i = 0; i < cols.length; i++) {
            var c = cols[i];
            visitor(c);
            if (c.columns && c.columns.length) walkColumns(c.columns, visitor);
        }
    }

    function ensureOrigHeader(col) {
        if (!col || col.type === 'checkbox') return;
        if (col._syriatelOrigHeader === undefined && col.headerText !== undefined) {
            col._syriatelOrigHeader = col.headerText;
        }
    }

    function lookupByEn(byEn, byEnLc, text) {
        if (text === undefined || text === null) return '';
        var s = String(text);
        var t = s.trim();
        if (!t) return '';
        if (byEn[t]) return byEn[t];
        if (byEn[s]) return byEn[s];
        var k = t.toLowerCase();
        if (byEnLc && byEnLc[k]) return byEnLc[k];
        return '';
    }

    function lastFieldSegment(field) {
        if (!field || typeof field !== 'string') return '';
        var i = field.lastIndexOf('.');
        return i >= 0 ? field.slice(i + 1) : field;
    }

    function applyFieldHeadersToColumns(columns, lang) {
        var dict = window.__GRID_FIELD_HEADERS_AR || {};
        var byEn = window.__GRID_HEADER_TEXT_EN_TO_AR || {};
        var byEnLc = window.__GRID_HEADER_TEXT_EN_TO_AR_LC || {};
        walkColumns(columns, function (col) {
            if (!col || col.type === 'checkbox') return;
            ensureOrigHeader(col);
            var orig = col._syriatelOrigHeader;
            var field = col.field;
            if (lang === 'ar') {
                var origStr = orig !== undefined && orig !== null ? String(orig).trim() : '';
                var looksArabic = /[\u0600-\u06FF\u0750-\u077F]/.test(origStr);
                var arByField = field ? (dict[field] || dict[lastFieldSegment(field)] || '') : '';
                var arByOrig = origStr ? lookupByEn(byEn, byEnLc, origStr) : '';
                var ar = '';
                if (looksArabic) {
                    ar = origStr;
                } else if (field && /statusname$/i.test(String(field))) {
                    ar = arByField || arByOrig;
                } else {
                    ar = arByOrig || arByField;
                }
                if (ar) col.headerText = ar;
            } else if (orig !== undefined) {
                col.headerText = orig;
            }
        });
    }

    function toolbarMapLookup(map, key) {
        if (!map || key === undefined || key === null) return '';
        var k0 = String(key);
        if (map[k0]) return map[k0];
        var t = k0.trim();
        if (map[t]) return map[t];
        var lc = t.toLowerCase();
        for (var k in map) {
            if (!Object.prototype.hasOwnProperty.call(map, k)) continue;
            if (String(k).trim().toLowerCase() === lc) return map[k];
        }
        return '';
    }

    function applyToolbarStrings(toolbar, lang) {
        if (!Array.isArray(toolbar)) return;
        var map = window.__GRID_TOOLBAR_EN_TO_AR || {};
        for (var i = 0; i < toolbar.length; i++) {
            var item = toolbar[i];
            if (!item || typeof item !== 'object' || item.type === 'Separator') continue;
            if (typeof item.text === 'string') {
                if (item._syriatelOrigText === undefined) item._syriatelOrigText = item.text;
                var t0 = item._syriatelOrigText;
                if (lang === 'ar') {
                    var arT = toolbarMapLookup(map, t0);
                    item.text = arT || t0;
                } else {
                    item.text = t0;
                }
            }
            if (typeof item.tooltipText === 'string') {
                if (item._syriatelOrigTip === undefined) item._syriatelOrigTip = item.tooltipText;
                var tip0 = item._syriatelOrigTip;
                if (lang === 'ar') {
                    var arTip = toolbarMapLookup(map, tip0);
                    item.tooltipText = arTip || tip0;
                } else {
                    item.tooltipText = tip0;
                }
            }
        }
    }

    /**
     * Syncfusion Grid: first render with persistSelection:true + checkbox can throw inside refreshPersistSelection
     * (.length on undefined) during appendTo. Start with persistSelection off, then restore after first dataBound.
     */
    function scheduleRestorePersistSelection(inst) {
        if (!inst || inst.__syriatelPersistRestoreHooked) return;
        inst.__syriatelPersistRestoreHooked = true;
        function doRestore() {
            if (!inst || inst.isDestroyed === true) return;
            try {
                var ss = inst.selectionSettings || {};
                var selOn = {};
                var sk;
                for (sk in ss) {
                    if (Object.prototype.hasOwnProperty.call(ss, sk)) {
                        selOn[sk] = ss[sk];
                    }
                }
                selOn.persistSelection = true;
                if (typeof inst.setProperties === 'function') {
                    inst.setProperties({ selectionSettings: selOn });
                }
            } catch (ignore) { /* empty */ }
        }
        function onBound() {
            try {
                inst.removeEventListener('dataBound', onBound);
            } catch (ignoreRm) { /* empty */ }
            try {
                if (typeof requestAnimationFrame === 'function') {
                    requestAnimationFrame(doRestore);
                } else {
                    setTimeout(doRestore, 0);
                }
            } catch (ignoreRaf) {
                setTimeout(doRestore, 0);
            }
        }
        try {
            inst.addEventListener('dataBound', onBound);
        } catch (ignoreAdd) {
            setTimeout(doRestore, 0);
        }
    }

    /** Every Syncfusion grid: resizable columns (grid + each column). */
    function ensureGridColumnResizing(options) {
        if (!options || typeof options !== 'object') return;
        options.allowResizing = true;
        if (!options.resizeSettings || typeof options.resizeSettings !== 'object') {
            options.resizeSettings = { mode: 'Normal' };
        } else if (!options.resizeSettings.mode) {
            options.resizeSettings.mode = 'Normal';
        }
        if (Array.isArray(options.columns)) {
            for (var ci = 0; ci < options.columns.length; ci++) {
                var col = options.columns[ci];
                if (col && typeof col === 'object') {
                    col.allowResizing = true;
                }
            }
        }
    }

    function ensureGridInstanceResizing(inst) {
        if (!inst || inst.isDestroyed === true) return;
        try {
            if (typeof inst.setProperties === 'function') {
                inst.setProperties({
                    allowResizing: true,
                    resizeSettings: { mode: 'Normal' },
                });
            } else {
                inst.allowResizing = true;
            }
        } catch (ignoreRes) { /* empty */ }
        try {
            if (Array.isArray(inst.columns)) {
                for (var i = 0; i < inst.columns.length; i++) {
                    var c = inst.columns[i];
                    if (c && typeof c === 'object') {
                        c.allowResizing = true;
                    }
                }
                if (typeof inst.refreshColumns === 'function') {
                    inst.refreshColumns();
                }
            }
        } catch (ignoreCols) { /* empty */ }
    }

    function patchGridConstructorOptions(options) {
        if (!options || typeof options !== 'object') return options;
        if (options.selectionSettings && options.selectionSettings.persistSelection === true) {
            options.syriatelRestorePersistSelection = true;
            options.selectionSettings = Object.assign({}, options.selectionSettings, { persistSelection: false });
        }
        ensureGridColumnResizing(options);
        applyEj2LocaleAndCulture();
        var lang = getUiLang();
        var rtl = isRtl();
        if (options.enableRtl === undefined) options.enableRtl = rtl;
        options.locale = lang === 'ar' ? 'ar-SA' : 'en-US';
        if (Array.isArray(options.columns)) {
            applyFieldHeadersToColumns(options.columns, lang);
        }
        if (Array.isArray(options.toolbar)) {
            applyToolbarStrings(options.toolbar, lang);
        }
        return options;
    }

    /**
     * Some Syncfusion bundles expose `ej.grids.Grid` as a non-writable export (read-only namespace).
     * Fall back to prototype appendTo hook only for persistSelection deferral (no post-append refreshOneGrid —
     * that caused double setProperties / flicker; dictionaries + refreshAllGrids handle i18n).
     */
    function installGridReadOnlyFallback(Original) {
        if (!Original || !Original.prototype) return;
        if (Original.__syriatelGridI18nHooked) return;
        Original.__syriatelGridI18nHooked = true;
        var proto = Original.prototype;
        if (typeof proto.appendTo === 'function') {
            var app0 = proto.appendTo;
            if (!app0.__syriatelGridAppendPatched) {
                proto.appendTo = function (selector) {
                    var inst = this;
                    try {
                        if (!inst.__syriatelPersistMountGuarded
                            && inst.selectionSettings
                            && inst.selectionSettings.persistSelection === true) {
                            inst.__syriatelPersistMountGuarded = true;
                            /* Do NOT call setProperties before appendTo — services/modules are not ready → getService undefined. */
                            try {
                                inst.selectionSettings.persistSelection = false;
                            } catch (ignoreMut) { /* empty */ }
                            scheduleRestorePersistSelection(inst);
                        }
                    } catch (ignorePre) { /* empty */ }
                    var ret = app0.apply(this, arguments);
                    ensureGridInstanceResizing(inst);
                    return ret;
                };
                proto.appendTo.__syriatelGridAppendPatched = true;
                proto.appendTo.__syriatelResizePatched = true;
            }
        }
        patchGridAppendToForResizing(Original);
    }

    function installGridWrapper() {
        if (!window.ej || !ej.grids || !ej.grids.Grid) return false;
        var Current = ej.grids.Grid;
        if (Current.__syriatelWrapped) return true;
        if (Current.__syriatelGridI18nHooked) return true;
        var Original = Current;
        function WrappedGrid() {
            var args = Array.prototype.slice.call(arguments);
            var opts = args[0] && typeof args[0] === 'object' ? args[0] : null;
            if (opts) patchGridConstructorOptions(opts);
            var inst = null;
            try {
                inst = Reflect.construct(Original, args, new.target || Original);
            } catch (ignoreRc) {
                try {
                    switch (args.length) {
                        case 0:
                            inst = new Original();
                            break;
                        case 1:
                            inst = new Original(args[0]);
                            break;
                        default:
                            inst = new Original(args[0], args[1]);
                    }
                } catch (ignoreSecond) {
                    inst = null;
                }
            }
            if (inst && opts && opts.syriatelRestorePersistSelection === true) {
                scheduleRestorePersistSelection(inst);
            }
            return inst;
        }
        WrappedGrid.__syriatelWrapped = true;
        Object.setPrototypeOf(WrappedGrid, Original);
        WrappedGrid.prototype = Original.prototype;
        Object.getOwnPropertyNames(Original).forEach(function (key) {
            if (key === 'length' || key === 'name' || key === 'prototype') return;
            try {
                var d = Object.getOwnPropertyDescriptor(Original, key);
                if (d) Object.defineProperty(WrappedGrid, key, d);
            } catch (ignore) { /* empty */ }
        });
        var assigned = false;
        try {
            ej.grids.Grid = WrappedGrid;
            assigned = true;
        } catch (ignoreAssign) { /* empty */ }
        if (!assigned) {
            try {
                Object.defineProperty(ej.grids, 'Grid', {
                    value: WrappedGrid,
                    writable: true,
                    enumerable: true,
                    configurable: true
                });
                assigned = true;
            } catch (ignoreDef) { /* empty */ }
        }
        if (!assigned) {
            installGridReadOnlyFallback(Original);
        } else {
            patchGridAppendToForResizing(Original);
        }
        return true;
    }

    function patchGridAppendToForResizing(Original) {
        if (!Original || !Original.prototype) return;
        var proto = Original.prototype;
        if (typeof proto.appendTo !== 'function' || proto.appendTo.__syriatelResizePatched) return;
        var app0 = proto.appendTo;
        proto.appendTo = function () {
            var ret = app0.apply(this, arguments);
            ensureGridInstanceResizing(this);
            return ret;
        };
        proto.appendTo.__syriatelResizePatched = true;
    }

    function applyEj2LocaleAndCulture() {
        if (!window.ej || !ej.base) return;
        var lang = getUiLang();
        if (lang === 'ar' && window.__SYNCFUSION_AR_L10N && Object.keys(window.__SYNCFUSION_AR_L10N).length) {
            try {
                ej.base.L10n.load(window.__SYNCFUSION_AR_L10N);
            } catch (ignore) { /* empty */ }
        }
        try {
            ej.base.setCulture(lang === 'ar' ? 'ar' : 'en');
        } catch (ignore) { /* empty */ }
    }

    /** Avoid setProperties(locale/rtl) when already applied — Syncfusion re-binds rows and causes visible flicker. */
    function gridLocalePropsAlreadyApplied(inst, patch) {
        if (!inst || !patch) return false;
        try {
            var loc = inst.locale;
            var rtl = !!inst.enableRtl;
            return String(loc || '') === String(patch.locale || '') && rtl === !!patch.enableRtl;
        } catch (ignore) {
            return false;
        }
    }

    var GRID_KANBAN_I18N_WAVE = 0;
    var gridHeaderMapGeneration = 0;

    function markGridWaveIf(inst, wave) {
        if (inst && wave != null) {
            try {
                inst.__syriatelI18nWave = wave;
            } catch (ignoreMw) { /* empty */ }
        }
    }

    function refreshOneGrid(inst, wave) {
        if (!inst || !inst.columns) return;
        ensureGridInstanceResizing(inst);
        if (wave != null) {
            try {
                if (inst.__syriatelI18nWave === wave) {
                    return;
                }
            } catch (ignoreWv) { /* empty */ }
        }
        if (inst.__syriatelI18nGridBusy) return;
        inst.__syriatelI18nGridBusy = true;
        var deferredRelease = false;
        try {
            var lang = getUiLang();
            var rtl = isRtl();
            applyFieldHeadersToColumns(inst.columns, lang);
            if (Array.isArray(inst.toolbar)) {
                applyToolbarStrings(inst.toolbar, lang);
            }
            /* Never setProperties(columns): full column rebind + persistSelection causes refreshPersistSelection to throw (.length on undefined).
             * Avoid setProperties(toolbar) too — refresh toolbarModule after mutating items in place. */
            var patch = {
                enableRtl: rtl,
                locale: lang === 'ar' ? 'ar-SA' : 'en-US'
            };
            var skipLocaleProps = gridLocalePropsAlreadyApplied(inst, patch);
            var persistSel = false;
            try {
                persistSel = !!(inst.selectionSettings && inst.selectionSettings.persistSelection);
            } catch (ignorePs) { /* empty */ }

            function afterLocalePatch() {
                if (typeof inst.refreshHeader === 'function') {
                    try {
                        inst.refreshHeader();
                    } catch (ignoreRh) { /* empty */ }
                }
                if (inst.toolbarModule && typeof inst.toolbarModule.refresh === 'function') {
                    try {
                        inst.toolbarModule.refresh();
                    } catch (ignoreTb) { /* empty */ }
                }
            }

            function applyHeaderOrFullPaint() {
                var mg = gridHeaderMapGeneration;
                try {
                    if (!skipLocaleProps) {
                        afterLocalePatch();
                    } else if (inst.__syriatelHeaderMapGen !== mg) {
                        inst.__syriatelHeaderMapGen = mg;
                        if (typeof inst.refreshHeader === 'function') {
                            try {
                                inst.refreshHeader();
                            } catch (ignoreLh) { /* empty */ }
                        }
                    }
                } catch (ignorePaint) { /* empty */ }
            }

            function releaseBusy() {
                inst.__syriatelI18nGridBusy = false;
            }

            if (persistSel && typeof inst.setProperties === 'function' && !skipLocaleProps) {
                /* Syncfusion: setProperties(locale/rtl) while persistSelection is true can trigger refreshPersistSelection
                 * before internal checkbox/row state exists → .length on undefined (uncaught in rAF). */
                try {
                    var ss = inst.selectionSettings || {};
                    var selOff = {};
                    var sk;
                    for (sk in ss) {
                        if (Object.prototype.hasOwnProperty.call(ss, sk)) {
                            selOff[sk] = ss[sk];
                        }
                    }
                    selOff.persistSelection = false;
                    inst.setProperties({
                        enableRtl: patch.enableRtl,
                        locale: patch.locale,
                        selectionSettings: selOff
                    });
                } catch (ignoreSp0) { /* empty */ }
                afterLocalePatch();
                markGridWaveIf(inst, wave);
                deferredRelease = true;
                try {
                    setTimeout(function () {
                        try {
                            if (!inst || inst.isDestroyed === true) {
                                releaseBusy();
                                return;
                            }
                            if (typeof inst.setProperties === 'function' && inst.selectionSettings) {
                                var ss2 = inst.selectionSettings || {};
                                var selOn = {};
                                var sk2;
                                for (sk2 in ss2) {
                                    if (Object.prototype.hasOwnProperty.call(ss2, sk2)) {
                                        selOn[sk2] = ss2[sk2];
                                    }
                                }
                                selOn.persistSelection = true;
                                inst.setProperties({ selectionSettings: selOn });
                            }
                        } catch (ignoreSp1) { /* empty */ }
                        releaseBusy();
                    }, 0);
                } catch (ignoreSt) {
                    releaseBusy();
                }
                return;
            }

            if (persistSel && skipLocaleProps) {
                applyHeaderOrFullPaint();
                markGridWaveIf(inst, wave);
                return;
            }

            try {
                if (typeof inst.setProperties === 'function' && !skipLocaleProps) {
                    inst.setProperties(patch);
                }
            } catch (ignore) { /* empty */ }
            applyHeaderOrFullPaint();
            markGridWaveIf(inst, wave);
        } finally {
            if (!deferredRelease) {
                inst.__syriatelI18nGridBusy = false;
            }
        }
    }

    var refreshAllGridsRaf = null;
    /** Coalesce burst refreshAllGrids+Kanbans (locale toggle + observer + deferred) into one pass. */
    var gridKanbanI18nDebounceT = null;
    var GRID_KANBAN_I18N_DEBOUNCE_MS = 720;

    function debouncedRefreshAllGridsAndKanbans() {
        if (gridKanbanI18nDebounceT !== null) {
            try {
                clearTimeout(gridKanbanI18nDebounceT);
            } catch (ignoreCl) { /* empty */ }
            gridKanbanI18nDebounceT = null;
        }
        gridKanbanI18nDebounceT = setTimeout(function () {
            gridKanbanI18nDebounceT = null;
            try {
                refreshAllGrids();
            } catch (ignoreRun) { /* empty */ }
        }, GRID_KANBAN_I18N_DEBOUNCE_MS);
    }

    function refreshAllGrids() {
        if (!window.ej) return;
        if (!ej.grids || !ej.grids.Grid) {
            try {
                refreshAllKanbans();
            } catch (ignoreKbOnly) { /* empty */ }
            return;
        }
        if (refreshAllGridsRaf != null) {
            try {
                if (typeof cancelAnimationFrame === 'function') {
                    cancelAnimationFrame(refreshAllGridsRaf);
                } else {
                    clearTimeout(refreshAllGridsRaf);
                }
            } catch (ignoreCaf) { /* empty */ }
            refreshAllGridsRaf = null;
        }
        var schedule = typeof requestAnimationFrame === 'function' ? requestAnimationFrame : function (cb) {
            return setTimeout(cb, 0);
        };
        refreshAllGridsRaf = schedule(function () {
            refreshAllGridsRaf = null;
            applyEj2LocaleAndCulture();
            GRID_KANBAN_I18N_WAVE++;
            var w = GRID_KANBAN_I18N_WAVE;
            var nodes = document.querySelectorAll('.e-grid');
            for (var i = 0; i < nodes.length; i++) {
                var inst = nodes[i].ej2_instances && nodes[i].ej2_instances[0];
                if (!inst) continue;
                refreshOneGrid(inst, w);
            }
            if (window.ej && ej.kanban && ej.kanban.Kanban) {
                var kbNodes = document.querySelectorAll('.e-kanban');
                for (var j = 0; j < kbNodes.length; j++) {
                    var kb = kbNodes[j].ej2_instances && kbNodes[j].ej2_instances[0];
                    if (!kb) continue;
                    refreshOneKanban(kb, w);
                }
            }
        });
    }

    /** Previously scheduled extra grid passes; idle re-layout caused visible flicker — kept as no-op for callers. */
    function scheduleDeferredGridI18nRefresh() {
    }

    function patchKanbanConstructorOptions(options) {
        if (!options || typeof options !== 'object') return options;
        applyEj2LocaleAndCulture();
        var lang = getUiLang();
        var rtl = isRtl();
        if (options.enableRtl === undefined) options.enableRtl = rtl;
        if (options.locale === undefined) {
            options.locale = lang === 'ar' ? 'ar-SA' : 'en-US';
        }
        if (Array.isArray(options.columns)) {
            applyFieldHeadersToColumns(options.columns, lang);
        }
        return options;
    }

    function installKanbanReadOnlyFallback(Original) {
        if (!Original || !Original.prototype) return;
        if (Original.__syriatelKanbanI18nHooked) return;
        Original.__syriatelKanbanI18nHooked = true;
        /* No appendTo hook: post-append Kanban refresh duplicated refreshAllGrids and caused extra flicker. */
    }

    function installKanbanWrapper() {
        if (!window.ej || !ej.kanban || !ej.kanban.Kanban) return false;
        var CurrentK = ej.kanban.Kanban;
        if (CurrentK.__syriatelWrapped) return true;
        if (CurrentK.__syriatelKanbanI18nHooked) return true;
        var Original = CurrentK;
        function WrappedKanban() {
            var argsK = Array.prototype.slice.call(arguments);
            if (argsK[0] && typeof argsK[0] === 'object') patchKanbanConstructorOptions(argsK[0]);
            try {
                return Reflect.construct(Original, argsK, new.target || Original);
            } catch (ignoreRcK) {
                switch (argsK.length) {
                    case 0:
                        return new Original();
                    case 1:
                        return new Original(argsK[0]);
                    default:
                        return new Original(argsK[0], argsK[1]);
                }
            }
        }
        WrappedKanban.__syriatelWrapped = true;
        Object.setPrototypeOf(WrappedKanban, Original);
        WrappedKanban.prototype = Original.prototype;
        Object.getOwnPropertyNames(Original).forEach(function (key) {
            if (key === 'length' || key === 'name' || key === 'prototype') return;
            try {
                var d = Object.getOwnPropertyDescriptor(Original, key);
                if (d) Object.defineProperty(WrappedKanban, key, d);
            } catch (ignore) { /* empty */ }
        });
        var assignedK = false;
        try {
            ej.kanban.Kanban = WrappedKanban;
            assignedK = true;
        } catch (ignoreAssignK) { /* empty */ }
        if (!assignedK) {
            try {
                Object.defineProperty(ej.kanban, 'Kanban', {
                    value: WrappedKanban,
                    writable: true,
                    enumerable: true,
                    configurable: true
                });
                assignedK = true;
            } catch (ignoreDefK) { /* empty */ }
        }
        if (!assignedK) {
            installKanbanReadOnlyFallback(Original);
        }
        return true;
    }

    function refreshOneKanban(inst, wave) {
        if (!inst || !inst.columns) return;
        if (wave != null) {
            try {
                if (inst.__syriatelI18nWave === wave) {
                    return;
                }
            } catch (ignoreKw) { /* empty */ }
        }
        var lang = getUiLang();
        var rtl = isRtl();
        var patch = { enableRtl: rtl, locale: lang === 'ar' ? 'ar-SA' : 'en-US' };
        applyFieldHeadersToColumns(inst.columns, lang);
        var skip = gridLocalePropsAlreadyApplied(inst, patch);
        if (!skip) {
            try {
                inst.setProperties(patch);
            } catch (ignore) { /* empty */ }
            try {
                if (typeof inst.setProperties === 'function' && inst.columns) {
                    inst.setProperties({ columns: inst.columns });
                }
            } catch (ignore2) { /* empty */ }
        } else {
            try {
                if (typeof inst.refresh === 'function') {
                    inst.refresh();
                }
            } catch (ignore3) { /* empty */ }
        }
        if (wave != null) {
            try {
                inst.__syriatelI18nWave = wave;
            } catch (ignoreMk) { /* empty */ }
        }
    }

    function refreshAllKanbans() {
        if (!window.ej || !ej.kanban || !ej.kanban.Kanban) return;
        applyEj2LocaleAndCulture();
        GRID_KANBAN_I18N_WAVE++;
        var w = GRID_KANBAN_I18N_WAVE;
        var nodes = document.querySelectorAll('.e-kanban');
        for (var i = 0; i < nodes.length; i++) {
            var inst = nodes[i].ej2_instances && nodes[i].ej2_instances[0];
            if (!inst) continue;
            refreshOneKanban(inst, w);
        }
    }

    function resolveLocaleUrl(path) {
        try {
            var p = String(path || '').trim();
            if (!p) return p;
            /* Always resolve from site origin. Using document.baseURI breaks on nested routes
               (e.g. /ProgramManagers/ProgramManagerList → .../ProgramManagers/locales/... 404). */
            var origin = (typeof window !== 'undefined' && window.location && window.location.origin)
                ? window.location.origin
                : '';
            if (!origin) return '/' + p.replace(/^\/+/, '');
            if (p.charAt(0) !== '/') p = '/' + p;
            return new URL(p, origin).href;
        } catch (e) {
            return '/' + String(path || '').replace(/^\/+/, '');
        }
    }

    function loadJson(path) {
        return fetch(resolveLocaleUrl(path), { credentials: 'same-origin' }).then(function (r) {
            if (!r.ok) throw new Error(path);
            return r.json();
        });
    }

    function settledJson(r) {
        if (!r || r.status !== 'fulfilled' || !r.value) return null;
        return r.value;
    }

    /** Shallow-merge fetched ui-modals over server-embedded pack; never drop `labels` / `form` if fetch is partial. */
    function mergeUiModalsDict(base, fetched) {
        if (!fetched || typeof fetched !== 'object' || !Object.keys(fetched).length) {
            return base && typeof base === 'object' ? base : {};
        }
        base = base && typeof base === 'object' ? base : {};
        var out = {};
        var k;
        for (k in base) {
            if (Object.prototype.hasOwnProperty.call(base, k)) {
                out[k] = base[k];
            }
        }
        for (k in fetched) {
            if (Object.prototype.hasOwnProperty.call(fetched, k)) {
                out[k] = fetched[k];
            }
        }
        if ((!out.labels || typeof out.labels !== 'object') && base.labels && typeof base.labels === 'object') {
            out.labels = base.labels;
        }
        if ((!out.form || typeof out.form !== 'object') && base.form && typeof base.form === 'object') {
            out.form = base.form;
        }
        var bPh = base.phByEn && typeof base.phByEn === 'object' ? base.phByEn : {};
        var fPh = out.phByEn && typeof out.phByEn === 'object' ? out.phByEn : {};
        if (Object.keys(bPh).length || Object.keys(fPh).length) {
            var mergedPh = {};
            var pk;
            for (pk in bPh) {
                if (Object.prototype.hasOwnProperty.call(bPh, pk)) {
                    mergedPh[pk] = bPh[pk];
                }
            }
            for (pk in fPh) {
                if (Object.prototype.hasOwnProperty.call(fPh, pk)) {
                    mergedPh[pk] = fPh[pk];
                }
            }
            out.phByEn = mergedPh;
        }
        return out;
    }

    function loadDictionariesThenRefresh() {
        Promise.allSettled([
            loadJson('locales/grid-field-headers.json'),
            loadJson('locales/syncfusion-ar-core.json'),
            loadJson('locales/ui-modals.json')
        ]).then(function (results) {
            if (gridKanbanI18nDebounceT !== null) {
                try {
                    clearTimeout(gridKanbanI18nDebounceT);
                } catch (ignoreClr) { /* empty */ }
                gridKanbanI18nDebounceT = null;
            }
            var headers = settledJson(results[0]) || {};
            var syncfusionAr = settledJson(results[1]) || {};
            var baseModals = window.__UI_MODALS && typeof window.__UI_MODALS === 'object' ? window.__UI_MODALS : {};
            window.__UI_MODALS = mergeUiModalsDict(baseModals, settledJson(results[2]));
            window.__GRID_FIELD_HEADERS_AR = headers.ar || {};
            window.__GRID_HEADER_TEXT_EN_TO_AR = headers.headerTextEnToAr || {};
            (function buildLcMap() {
                var m = {};
                var src = window.__GRID_HEADER_TEXT_EN_TO_AR;
                if (src && typeof src === 'object') {
                    Object.keys(src).forEach(function (k) {
                        m[String(k).trim().toLowerCase()] = src[k];
                    });
                }
                window.__GRID_HEADER_TEXT_EN_TO_AR_LC = m;
            })();
            window.__GRID_TOOLBAR_EN_TO_AR = headers.toolbarEnToAr || {};
            gridHeaderMapGeneration++;
            window.__SYNCFUSION_AR_L10N = (function cloneArLocales(sf) {
                if (!sf || typeof sf !== 'object' || !sf.ar) return sf || {};
                try {
                    var out = {};
                    Object.keys(sf).forEach(function (k) {
                        out[k] = sf[k];
                    });
                    out['ar-SA'] = sf.ar;
                    out['ar-EG'] = sf.ar;
                    return out;
                } catch (e) {
                    return sf;
                }
            })(syncfusionAr);
            applyEj2LocaleAndCulture();
            refreshAllGrids();
            applyModalChromeI18n();
            scheduleApplyModalChromeAfterVueFlush();
            try {
                document.documentElement.dispatchEvent(new CustomEvent('syriatel-ui-modals-loaded'));
            } catch (ignore) { /* empty */ }
            [900].forEach(function (ms) {
                setTimeout(function () {
                    applyModalChromeI18n();
                    scheduleApplyModalChromeAfterVueFlush();
                }, ms);
            });
        });
    }

    if (window.ej && ej.grids && ej.grids.Grid) {
        installGridWrapper();
    }
    if (window.ej && ej.kanban && ej.kanban.Kanban) {
        installKanbanWrapper();
    }

    document.documentElement.addEventListener('syriatel-locale-changed', function () {
        applyEj2LocaleAndCulture();
        debouncedRefreshAllGridsAndKanbans();
        applyModalChromeI18n();
        scheduleApplyModalChromeAfterVueFlush();
    });

    function bindDocumentLangObserverForModals() {
        if (bindDocumentLangObserverForModals.__done) return;
        bindDocumentLangObserverForModals.__done = true;
        var langDirObsSkipGridsOnce = true;
        function onLangDirMaybeChanged() {
            applyModalChromeI18n();
            scheduleApplyModalChromeAfterVueFlush();
            if (langDirObsSkipGridsOnce) {
                langDirObsSkipGridsOnce = false;
                return;
            }
            debouncedRefreshAllGridsAndKanbans();
        }
        try {
            new MutationObserver(onLangDirMaybeChanged).observe(document.documentElement, { attributes: true, attributeFilter: ['lang', 'dir'] });
            onLangDirMaybeChanged();
        } catch (ignoreDoc) { /* empty */ }
    }

    function bindModalShownForI18n() {
        var b = document.body;
        if (!b) {
            document.addEventListener('DOMContentLoaded', function once() {
                document.removeEventListener('DOMContentLoaded', once);
                bindModalShownForI18n();
            });
            return;
        }
        b.addEventListener('shown.bs.modal', function (ev) {
            applyModalChromeI18n();
            [0, 140, 320].forEach(function (ms) {
                setTimeout(function () {
                    applyModalChromeI18n();
                }, ms);
            });
            try {
                if (typeof queueMicrotask === 'function') {
                    queueMicrotask(applyModalChromeI18n);
                }
            } catch (ignoreQm) { /* empty */ }
            scheduleApplyModalChromeAfterVueFlush();
            /* Vue recreates footer nodes (v-if / v-show); keep i18n in sync until the modal closes. */
            var modal = ev && ev.target && ev.target.closest ? ev.target.closest('.modal') : null;
            if (!modal) return;
            var debTimer = null;
            var sub = null;
            var onHidden = function () {
                try {
                    if (debTimer) clearTimeout(debTimer);
                } catch (ignoreCt) { /* empty */ }
                debTimer = null;
                try {
                    if (sub) sub.disconnect();
                } catch (ignoreDc) { /* empty */ }
                sub = null;
                try {
                    modal.removeEventListener('hidden.bs.modal', onHidden);
                } catch (ignoreRh) { /* empty */ }
                applyModalChromeI18n();
            };
            try {
                modal.addEventListener('hidden.bs.modal', onHidden);
            } catch (ignoreH) { /* empty */ }
            try {
                sub = new MutationObserver(function () {
                    if (debTimer) clearTimeout(debTimer);
                    debTimer = setTimeout(function () {
                        debTimer = null;
                        applyModalChromeI18n();
                        scheduleApplyModalChromeAfterVueFlush();
                    }, 40);
                });
                sub.observe(modal, { subtree: true, characterData: true, childList: true });
            } catch (ignoreMo) { /* empty */ }
        });
    }
    patchVueCreateAppForModalChrome();
    bindModalShownForI18n();
    bindDocumentLangObserverForModals();

    /* Vue mounts on DOMContentLoaded; run chrome i18n again so modal footers inside #app are present. */
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function mcqModalChromeAfterDom() {
            document.removeEventListener('DOMContentLoaded', mcqModalChromeAfterDom);
            applyModalChromeI18n();
            scheduleApplyModalChromeAfterVueFlush();
        });
    } else {
        try {
            if (typeof queueMicrotask === 'function') {
                queueMicrotask(applyModalChromeI18n);
            }
        } catch (ignoreMq0) { /* empty */ }
        setTimeout(applyModalChromeI18n, 0);
        scheduleApplyModalChromeAfterVueFlush();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', loadDictionariesThenRefresh);
    } else {
        loadDictionariesThenRefresh();
    }

    window.SyriatelGridI18n = {
        refreshAllGrids: refreshAllGrids,
        refreshAllKanbans: refreshAllKanbans,
        refreshOneKanban: refreshOneKanban,
        applyEj2LocaleAndCulture: applyEj2LocaleAndCulture,
        refreshOneGrid: refreshOneGrid,
        applyModalChromeI18n: applyModalChromeI18n,
        scheduleApplyModalChromeAfterVueFlush: scheduleApplyModalChromeAfterVueFlush,
        fillMcqFooterReactiveLabels: fillMcqFooterReactiveLabels,
        scheduleDeferredGridI18nRefresh: scheduleDeferredGridI18nRefresh
    };
})();
