const ISSUE = { 0: 'شبكة', 1: 'فوترة', 2: 'حظر شريحة', 3: 'تفعيل' };
const PRIORITY = { 0: 'منخفض', 1: 'متوسط', 2: 'عالي', 3: 'حرج' };
const STATUS = { 0: 'مفتوحة', 1: 'قيد المعالجة', 2: 'تم الحل', 3: 'مصعّدة' };

const formatDt = (utc) => {
    if (!utc) return '—';
    try {
        return new Date(utc).toLocaleString('ar-SY', { dateStyle: 'short', timeStyle: 'short' });
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
                issueLabel: ISSUE[Number(r.issueType ?? r.IssueType)] ?? '—',
                categoryLabelHtml:
                    window.TelecomUiBadges?.ticketCategory(ticketCategory) || ticketCategory,
                priorityLabel: PRIORITY[Number(r.priority ?? r.Priority)] ?? '—',
                statusLabel: STATUS[Number(r.status ?? r.Status)] ?? '—',
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
                    'تعذر تحميل التذاكر';
                if (typeof Swal !== 'undefined') {
                    Swal.fire({ icon: 'error', title: 'خطأ في التحميل', text: msg });
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
            const h =
                typeof computeTelecomGridHeight === 'function'
                    ? computeTelecomGridHeight('.tt-grid-panel')
                    : 420;
            grid.obj.height = h;
        };

        const createGrid = () => {
            if (!gridRef.value || grid.obj) return;
            const gridHeight =
                typeof computeTelecomGridHeight === 'function'
                    ? computeTelecomGridHeight('.tt-grid-panel')
                    : 420;
            grid.obj = new ej.grids.Grid({
                id: 'TechnicalTicketListGrid',
                height: gridHeight,
                width: '100%',
                dataSource: plainRows(state.rows),
                allowPaging: true,
                allowSorting: true,
                allowFiltering: true,
                filterSettings: { type: 'Menu' },
                pageSettings: { pageSize: 15, pageSizes: [15, 25, 50, 100] },
                sortSettings: {
                    columns: [{ field: 'createdAtUtc', direction: 'Descending' }],
                },
                recordDoubleClick: (args) => openStatusDrawer(mapRow(args.rowData)),
                queryCellInfo: paintTicketCells,
                columns: [
                    { field: 'id', isPrimaryKey: true, visible: false },
                    { field: 'createdAtUtc', visible: false, type: 'date', format: 'yyyy-MM-ddTHH:mm:ss' },
                    { field: 'ticketNumber', headerText: 'رقم التذكرة', width: 120 },
                    { field: 'customerDisplayName', headerText: 'المشترك', width: 130 },
                    { field: 'msisdn', headerText: 'MSISDN', width: 110 },
                    { field: 'categoryLabelHtml', headerText: 'نوع العملية', width: 130, allowFiltering: false },
                    { field: 'statusLabel', headerText: 'الحالة', width: 120, allowFiltering: false },
                    { field: 'priorityLabel', headerText: 'الأولوية', width: 100, allowFiltering: false },
                    { field: 'createdByChannel', headerText: 'المصدر', width: 150, allowFiltering: false },
                    { field: 'createdDisplay', headerText: 'تاريخ الإنشاء', width: 140 },
                    { field: 'notes', headerText: 'الوصف', width: 220, minWidth: 120 },
                    { field: 'resolutionNotes', headerText: 'ملاحظات الحل', width: 180 },
                    { field: 'resolvedDisplay', headerText: 'تاريخ الإغلاق', width: 140 },
                ],
            });
            grid.obj.appendTo(gridRef.value);
        };

        const handler = {
            search: async () => {
                await load();
                bindGrid();
            },
            simulateAiCall: async () => {
                const msisdn = (state.aiSim.msisdn || '').trim();
                const transcript = (state.aiSim.transcript || '').trim();
                if (!msisdn || transcript.length < 5) {
                    Swal.fire({ icon: 'warning', title: 'أدخل الرقم ونص المكالمة (5 أحرف+)' });
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
                        title: 'تذكرة AI في الأرشيف والطابور الحي',
                        html: `<p class="small mb-0">${body?.ticketNumber ?? ''}<br/>${body?.summaryAr ?? ''}</p>`,
                    });
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: e?.response?.data?.message || 'تعذر إنشاء التذكرة',
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
                    Swal.fire({ icon: 'warning', title: 'أدخل ملاحظات الحل عند الإغلاق' });
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
                    Swal.fire({ icon: 'success', title: 'تم حفظ الحالة', timer: 2000, showConfirmButton: false });
                } catch (e) {
                    Swal.fire({ icon: 'error', title: e?.response?.data?.message || 'فشل التحديث' });
                }
            },
        };

        Vue.onMounted(async () => {
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
            const sfReady = await waitForSyncfusion();
            if (!sfReady) {
                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        icon: 'error',
                        title: 'مكتبة الجداول غير محمّلة',
                        text: 'أعد تحميل الصفحة (Syncfusion).',
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

        return { gridRef, state, handler, loading: Vue.computed(() => state.loading) };
    },
};
Vue.createApp(App).mount('#app');
