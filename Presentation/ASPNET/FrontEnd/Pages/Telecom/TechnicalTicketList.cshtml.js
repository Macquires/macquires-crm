const ttT = (key, fallback) => {
    try {
        const raw = String(key);
        const paths = raw.includes('.') && !raw.startsWith('technicalTickets.')
            ? [raw, `technicalTickets.${raw}`]
            : [`technicalTickets.${raw}`, raw];
        for (const k of paths) {
            const hit = window.TelecomI18n?.t?.(k);
            if (hit) return hit;
        }
        return fallback;
    } catch {
        return fallback;
    }
};

const ticketEnumLabel = (group, code) => window.TelecomI18n?.t?.(`ticketEnums.${group}.${code}`) || '—';

const formatDt = (utc) => {
    if (!utc) return '—';
    try {
        const lang = (document.documentElement.lang || 'en').toLowerCase();
        const loc = lang.startsWith('en') ? 'en-US' : 'ar-SY';
        return new Date(utc).toLocaleString(loc, { dateStyle: 'short', timeStyle: 'short' });
    } catch {
        return String(utc);
    }
};

const parseTicketList = (res) => {
    const fromHelper = StorageManager.apiList(res);
    if (fromHelper.length > 0) return fromHelper;
    const content = res?.data?.content ?? res?.data?.Content;
    const list = content?.data ?? content?.Data;
    return Array.isArray(list) ? list : [];
};

const plainRows = (rows) => (Array.isArray(rows) ? rows.map((r) => ({ ...r })) : []);

const waitForSyncfusion = async (maxMs = 8000) => {
    const step = 100;
    let waited = 0;
    while (typeof ej === 'undefined' || !ej.grids) {
        if (waited >= maxMs) return false;
        await new Promise((r) => setTimeout(r, step));
        waited += step;
    }
    return true;
};

const App = {
    setup() {
        const state = Vue.reactive({
            rows: [],
            loading: true,
            pageSize: 10,
            filters: { status: '', msisdn: '' },
            aiSim: {
                msisdn: '0933123456',
                transcript: 'أخي شحنت كاش والنت واقف عندي بعد الشحن',
                busy: false,
            },
            selected: null,
        });

        const gridRef = Vue.ref(null);
        const grid = { obj: null };
        let statusDrawer = null;

        const computeTtGridHeight = () => {
            const shell = document.querySelector('.tt-grid-panel .tt-grid-shell');
            if (!shell) return 480;
            const top = shell.getBoundingClientRect().top;
            const footer = document.querySelector('.adminfooter');
            const footerH = footer?.getBoundingClientRect().height ?? 44;
            const pagerReserve = 52;
            const gap = 36;
            const available = window.innerHeight - top - footerH - pagerReserve - gap;
            return Math.max(340, Math.min(available, 640));
        };

        const getTicketColumns = () => [
            { field: 'id', isPrimaryKey: true, visible: false },
            { field: 'createdAtUtc', visible: false, type: 'date', format: 'yyyy-MM-ddTHH:mm:ss' },
            {
                field: 'ticketNumber',
                headerText: ttT('grid.ticketNumber', 'Ticket #'),
                width: 130,
                minWidth: 110,
                allowResizing: true,
                clipMode: 'EllipsisWithTooltip',
            },
            {
                field: 'customerDisplayName',
                headerText: ttT('grid.subscriber', 'Subscriber'),
                width: 140,
                minWidth: 110,
                allowResizing: true,
                clipMode: 'EllipsisWithTooltip',
            },
            {
                field: 'msisdn',
                headerText: 'MSISDN',
                width: 118,
                minWidth: 100,
                allowResizing: true,
                clipMode: 'EllipsisWithTooltip',
            },
            {
                field: 'categoryLabelHtml',
                headerText: ttT('grid.operationType', 'Type'),
                width: 130,
                minWidth: 100,
                allowFiltering: false,
                allowResizing: true,
            },
            {
                field: 'statusLabel',
                headerText: ttT('grid.status', 'Status'),
                width: 120,
                minWidth: 100,
                allowFiltering: false,
                allowResizing: true,
            },
            {
                field: 'priorityLabel',
                headerText: ttT('grid.priority', 'Priority'),
                width: 105,
                minWidth: 90,
                allowFiltering: false,
                allowResizing: true,
            },
            {
                field: 'createdByChannel',
                headerText: ttT('grid.source', 'Source'),
                width: 150,
                minWidth: 110,
                allowFiltering: false,
                allowResizing: true,
            },
            {
                field: 'createdDisplay',
                headerText: ttT('grid.createdAt', 'Created'),
                width: 145,
                minWidth: 120,
                allowResizing: true,
            },
            {
                field: 'notes',
                headerText: ttT('grid.description', 'Description'),
                width: 280,
                minWidth: 180,
                allowResizing: true,
                allowTextWrap: true,
                clipMode: 'Clip',
            },
            {
                field: 'resolutionNotes',
                headerText: ttT('grid.resolutionNotes', 'Resolution'),
                width: 220,
                minWidth: 160,
                allowResizing: true,
                allowTextWrap: true,
                clipMode: 'Clip',
            },
            {
                field: 'resolvedDisplay',
                headerText: ttT('grid.resolvedAt', 'Closed'),
                width: 145,
                minWidth: 120,
                allowResizing: true,
            },
        ];

        const syncGridPageSize = () => {
            if (!grid.obj) return;
            grid.obj.pageSettings.pageSize = state.pageSize;
            grid.obj.pageSettings.currentPage = 1;
            if (typeof grid.obj.goToPage === 'function') grid.obj.goToPage(1);
        };

        const resolveCategory = (r) => {
            const cat = r.ticketCategory ?? r.TicketCategory;
            if (cat === undefined || cat === null || cat === '') return 'Complaint';
            const n = Number(cat);
            const map = { 0: 'Complaint', 1: 'SimSwap', 2: 'PackageMigration', 3: 'OwnershipTransfer', 4: 'LineActivation', 5: 'VasActivation' };
            return !Number.isNaN(n) && map[n] ? map[n] : String(cat);
        };

        const mapRow = (r) => {
            const ticketCategory = resolveCategory(r);
            return {
                id: r.id ?? r.Id,
                ticketNumber: r.ticketNumber ?? r.TicketNumber,
                msisdn: r.msisdn ?? r.Msisdn,
                customerDisplayName: r.customerDisplayName ?? r.CustomerDisplayName ?? '—',
                notes: r.notes ?? r.Notes,
                resolutionNotes: r.resolutionNotes ?? r.ResolutionNotes,
                priority: r.priority ?? r.Priority,
                status: r.status ?? r.Status,
                issueType: r.issueType ?? r.IssueType,
                ticketCategory,
                createdByChannel: r.createdByChannel ?? r.CreatedByChannel ?? 'CallCenter_Agent',
                createdAtUtc: r.createdAtUtc ?? r.CreatedAtUtc ?? null,
                createdDisplay: formatDt(r.createdAtUtc ?? r.CreatedAtUtc),
                resolvedDisplay: formatDt(r.resolvedAtUtc ?? r.ResolvedAtUtc),
                issueLabel: ticketEnumLabel('issue', Number(r.issueType ?? r.IssueType)),
                categoryLabelHtml:
                    window.TelecomUiBadges?.ticketCategory(ticketCategory) || ticketCategory,
                priorityLabel: ticketEnumLabel('priority', Number(r.priority ?? r.Priority)),
                statusLabel: ticketEnumLabel('status', Number(r.status ?? r.Status)),
            };
        };

        const buildQuery = () => {
            const q = { includeResolved: true };
            if (state.filters.status !== '' && state.filters.status != null) {
                q.status = Number(state.filters.status);
            }
            if (state.filters.msisdn?.trim()) {
                q.msisdn = state.filters.msisdn.trim();
            }
            return q;
        };

        const load = async () => {
            state.loading = true;
            try {
                const res = await AxiosManager.get('/TelecomBackOffice/GetTechnicalTickets', {
                    params: buildQuery(),
                });
                state.rows = parseTicketList(res)
                    .map(mapRow)
                    .sort((a, b) => {
                        const ta = a.createdAtUtc ? new Date(a.createdAtUtc).getTime() : 0;
                        const tb = b.createdAtUtc ? new Date(b.createdAtUtc).getTime() : 0;
                        return tb - ta;
                    });
            } catch (e) {
                state.rows = [];
                const msg =
                    e?.response?.data?.message ||
                    e?.response?.data?.error?.message ||
                    e?.message ||
                    ttT('swal.loadFailed', 'Load failed');
                if (typeof Swal !== 'undefined') {
                    Swal.fire({ icon: 'error', title: ttT('swal.loadError', 'Error'), text: msg });
                } else {
                    console.error('TechnicalTicketList load failed', e);
                }
            } finally {
                state.loading = false;
            }
        };

        const bindGrid = () => {
            if (!grid.obj) return;
            const rows = plainRows(state.rows);
            grid.obj.dataSource = rows;
            applyGridHeight();
            if (typeof grid.obj.dataBind === 'function') grid.obj.dataBind();
            else grid.obj.refresh();
        };

        const openStatusDrawer = (row) => {
            state.selected = row;
            document.getElementById('ttDrawerTitle').textContent = row.ticketNumber || '—';
            document.getElementById('ttDrawerCustomer').textContent = row.customerDisplayName || '—';
            document.getElementById('ttDrawerMsisdn').textContent = row.msisdn || '—';
            document.getElementById('ttDrawerNotes').textContent = row.notes || '—';
            const sel = document.getElementById('ttTicketStatus');
            if (sel) sel.value = String(row.status ?? 0);
            const priSel = document.getElementById('ttTicketPriority');
            if (priSel) priSel.value = String(row.priority ?? 1);
            const notes = document.getElementById('ttOperatorNotes');
            if (notes) notes.value = row.resolutionNotes || '';
            if (!statusDrawer) {
                statusDrawer = new bootstrap.Offcanvas(document.getElementById('ticketStatusDrawer'));
            }
            statusDrawer.show();
        };

        const paintTicketCells = (args) => {
            if (!args?.cell || !args?.data) return;
            const row = args.data;
            if (args.column.field === 'statusLabel') {
                args.cell.innerHTML =
                    window.TelecomUiBadges?.ticketStatus(row.status, row.statusLabel) || row.statusLabel || '—';
            } else if (args.column.field === 'priorityLabel') {
                args.cell.innerHTML =
                    window.TelecomUiBadges?.ticketPriority(row.priority, row.priorityLabel) || row.priorityLabel || '—';
            } else if (args.column.field === 'createdByChannel') {
                args.cell.innerHTML =
                    window.TelecomUiBadges?.ticketChannel(row.createdByChannel) || row.createdByChannel || '—';
            } else if (args.column.field === 'categoryLabelHtml') {
                args.cell.innerHTML = row.categoryLabelHtml || '—';
            }
        };

        const applyGridHeight = () => {
            if (!grid.obj) return;
            const h = computeTtGridHeight();
            grid.obj.height = h;
            if (typeof grid.obj.refresh === 'function') grid.obj.refresh();
        };

        const createGrid = () => {
            if (!gridRef.value || grid.obj) return;
            const gridHeight = computeTtGridHeight();
            grid.obj = new ej.grids.Grid({
                id: 'TechnicalTicketListGrid',
                height: gridHeight,
                width: '100%',
                dataSource: plainRows(state.rows),
                allowPaging: true,
                allowSorting: true,
                allowFiltering: true,
                allowResizing: true,
                allowTextWrap: true,
                gridLines: 'Horizontal',
                resizeSettings: { mode: 'Normal' },
                filterSettings: { type: 'Menu' },
                pageSettings: {
                    currentPage: 1,
                    pageSize: state.pageSize,
                    pageSizes: [10, 25, 50, 100],
                },
                sortSettings: {
                    columns: [{ field: 'createdAtUtc', direction: 'Descending' }],
                },
                recordDoubleClick: (args) => openStatusDrawer(mapRow(args.rowData)),
                queryCellInfo: paintTicketCells,
                actionComplete: (args) => {
                    if (args.requestType === 'paging' && grid.obj?.pageSettings?.pageSize) {
                        const size = Number(grid.obj.pageSettings.pageSize);
                        if (size > 0 && size !== state.pageSize) state.pageSize = size;
                    }
                },
                columns: getTicketColumns(),
            });
            grid.obj.appendTo(gridRef.value);
            if (window.MacquiresUiI18n?.getUiLang) {
                try {
                    grid.obj.locale = window.MacquiresUiI18n.getUiLang() === 'ar' ? 'ar-SA' : 'en-US';
                } catch (_) { /* ignore */ }
            }
        };

        const handler = {
            search: async () => {
                await load();
                bindGrid();
            },
            changePageSize: () => {
                if (!grid.obj) return;
                syncGridPageSize();
                applyGridHeight();
                if (typeof grid.obj.refresh === 'function') grid.obj.refresh();
                else if (typeof grid.obj.dataBind === 'function') grid.obj.dataBind();
            },
            simulateAiCall: async () => {
                const msisdn = (state.aiSim.msisdn || '').trim();
                const transcript = (state.aiSim.transcript || '').trim();
                if (!msisdn || transcript.length < 5) {
                    Swal.fire({ icon: 'warning', title: ttT('swal.aiInputRequired', '') });
                    return;
                }
                state.aiSim.busy = true;
                try {
                    const res = await AxiosManager.post('/TelecomBackOffice/SimulateVoiceAiIncomingCall', {
                        msisdn,
                        rawVoiceTranscript: transcript,
                    });
                    const body = res?.data?.content ?? res?.data?.Content;
                    await load();
                    bindGrid();
                    Swal.fire({
                        icon: 'success',
                        title: ttT('swal.aiCreated', ''),
                        html: `<p class="small mb-0">${body?.ticketNumber ?? ''}<br/>${body?.summaryAr ?? ''}</p>`,
                    });
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: e?.response?.data?.message || ttT('swal.aiCreateFailed', ''),
                    });
                } finally {
                    state.aiSim.busy = false;
                }
            },
            saveStatus: async () => {
                if (!state.selected) return;
                const newStatus = Number(document.getElementById('ttTicketStatus')?.value ?? 0);
                const newPriority = Number(document.getElementById('ttTicketPriority')?.value ?? 1);
                const notes = document.getElementById('ttOperatorNotes')?.value?.trim() || '';
                if (newStatus === 2 && notes.length < 5) {
                    Swal.fire({ icon: 'warning', title: ttT('swal.resolutionRequired', '') });
                    return;
                }
                try {
                    await AxiosManager.post('/TelecomBackOffice/UpdateTechnicalTicketStatus', {
                        ticketId: state.selected.id,
                        newStatus,
                        newPriority,
                        operatorNotesAr: notes,
                    });
                    statusDrawer?.hide();
                    await load();
                    bindGrid();
                    Swal.fire({ icon: 'success', title: ttT('swal.saved', ''), timer: 2000, showConfirmButton: false });
                } catch (e) {
                    Swal.fire({ icon: 'error', title: e?.response?.data?.message || ttT('swal.updateFailed', '') });
                }
            },
        };

        const localeTick = Vue.ref(0);
        const ui = Vue.computed(() => {
            localeTick.value;
            return {
                pageTitle: ttT('pageTitle', 'Tickets'),
                subtitle: ttT('subtitle', ''),
                openFromSubscriber: ttT('openFromSubscriber', ''),
                aiSimTitle: ttT('aiSimTitle', ''),
                aiSimHint: ttT('aiSimHint', ''),
                callTranscript: ttT('callTranscript', ''),
                createAiTicket: ttT('createAiTicket', ''),
                filterStatus: ttT('filterStatus', ''),
                filterAll: ttT('filterAll', ''),
                searchMsisdn: ttT('searchMsisdn', ''),
                search: ttT('search', ''),
                gridHint: ttT('gridHint', ''),
                pageSizeLabel: ttT('pageSizeLabel', 'Rows per page'),
                drawer: {
                    subscriber: ttT('drawer.subscriber', ''),
                    msisdn: ttT('drawer.msisdn', 'MSISDN'),
                    status: ttT('drawer.status', ''),
                    resolutionNotes: ttT('drawer.resolutionNotes', ''),
                    save: ttT('drawer.save', ''),
                },
            };
        });

        const refreshGridHeaders = () => {
            if (!grid.obj || typeof grid.obj.getColumnByField !== 'function') return;
            grid.obj.columns = getTicketColumns();
            if (typeof grid.obj.refreshHeader === 'function') grid.obj.refreshHeader();
            else if (typeof grid.obj.refreshColumns === 'function') grid.obj.refreshColumns();
        };

        const refreshPageI18n = async () => {
            try {
                await window.TelecomI18n?.ensureLoaded?.();
                localeTick.value++;
                const title = ttT('pageTitle', 'Tickets');
                if (title) document.title = title;
                window.TelecomI18n?.refresh?.();
                refreshGridHeaders();
            } catch (_) { /* ignore */ }
        };

        Vue.onMounted(async () => {
            document.documentElement.addEventListener('syriatel-locale-changed', refreshPageI18n);
            try {
                if (typeof PortalNavigation !== 'undefined' && PortalNavigation.syncOperatorSession) {
                    await PortalNavigation.syncOperatorSession(true);
                }
            } catch (e) {
                console.warn('TechnicalTicketList: session sync failed', e);
            }
            await SecurityManager.authorizePage([
                'TelecomCallCenter',
                'TelecomBackOffice',
                'TelecomAdmin',
                'TelecomManagement',
            ]);
            await load();
            await refreshPageI18n();
            const sfReady = await waitForSyncfusion();
            if (!sfReady) {
                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        icon: 'error',
                        title: ttT('swal.gridLibMissing', ''),
                        text: ttT('swal.gridLibHint', ''),
                    });
                }
                if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
                return;
            }
            await Vue.nextTick();
            createGrid();
            bindGrid();
            requestAnimationFrame(() => {
                applyGridHeight();
                if (typeof grid.obj?.dataBind === 'function') grid.obj.dataBind();
            });
            const onResize = () => applyGridHeight();
            window.addEventListener('resize', onResize);
            Vue.onUnmounted(() => window.removeEventListener('resize', onResize));
            if (typeof hideSpinnerAndShowContent === 'function') hideSpinnerAndShowContent();
        });

        return { gridRef, state, handler, ui, ttT, ticketEnumLabel, loading: Vue.computed(() => state.loading) };
    },
};
Vue.createApp(App).mount('#app');
