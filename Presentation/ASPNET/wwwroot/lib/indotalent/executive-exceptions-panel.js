/**
 * Executive exception feed — GET /Telecom/GetExecutiveExceptions + weekly digest + SignalR alerts.
 */
const ExecutiveExceptionsPanel = (function () {
    const MIS_PERMISSION = 'telecom.reports.mis';
    let alertConnection = null;

    function t(key) {
        const hit = typeof TelecomI18n !== 'undefined' && TelecomI18n.t
            ? TelecomI18n.t('defaultDashboard.exceptions.' + key)
            : null;
        return hit || key;
    }

    function hasPermission() {
        const perms = typeof StorageManager !== 'undefined' && StorageManager.getPermissions
            ? StorageManager.getPermissions() || []
            : [];
        return (
            typeof StorageManager !== 'undefined' &&
            typeof StorageManager.hasAnyPermission === 'function' &&
            StorageManager.hasAnyPermission(perms, [MIS_PERMISSION])
        );
    }

    function revealEmbed() {
        const embed = document.getElementById('executive-exceptions');
        if (embed) embed.classList.remove('d-none');
    }

    function pick(o, ...keys) {
        if (!o) return undefined;
        for (const k of keys) {
            if (o[k] !== undefined && o[k] !== null) return o[k];
        }
        return undefined;
    }

    function connectAlerts() {
        if (alertConnection || typeof signalR === 'undefined' || !hasPermission()) return;
        alertConnection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/executive-alerts', {
                accessTokenFactory: () => StorageManager.getToken?.() || '',
            })
            .withAutomaticReconnect()
            .build();
        alertConnection.on('executiveAlert', (payload) => {
            const critical = payload?.criticalCount ?? payload?.CriticalCount ?? 0;
            if (critical > 0 && typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'warning',
                    title: t('critical'),
                    text: `${critical} ${t('critical')} — ${payload?.scopeLabelAr || ''}`,
                    toast: true,
                    position: 'top-end',
                    timer: 8000,
                    showConfirmButton: false,
                });
            }
            if (typeof PortalNavigation !== 'undefined' && PortalNavigation.refreshBadges) {
                PortalNavigation.refreshBadges();
            }
        });
        alertConnection
            .start()
            .then(() => alertConnection.invoke('JoinExecutiveMis'))
            .catch((e) => console.warn('Executive alerts hub failed', e));
    }

    function mount(rootEl) {
        if (!rootEl || typeof Vue === 'undefined') return null;

        const state = Vue.reactive({ data: null, digest: null, loading: false });

        const normalize = (c) => {
            if (!c) return null;
            return {
                scopeLabelAr: pick(c, 'scopeLabelAr', 'ScopeLabelAr') ?? '',
                totalCount: pick(c, 'totalCount', 'TotalCount') ?? 0,
                criticalCount: pick(c, 'criticalCount', 'CriticalCount') ?? 0,
                warningCount: pick(c, 'warningCount', 'WarningCount') ?? 0,
                items: (pick(c, 'items', 'Items') ?? []).map((i) => ({
                    code: pick(i, 'code', 'Code'),
                    severity: pick(i, 'severity', 'Severity'),
                    titleAr: pick(i, 'titleAr', 'TitleAr'),
                    detailAr: pick(i, 'detailAr', 'DetailAr'),
                    branchId: pick(i, 'branchId', 'BranchId'),
                    branchName: pick(i, 'branchName', 'BranchName'),
                    actionUrl: pick(i, 'actionUrl', 'ActionUrl'),
                    occurredAtUtc: pick(i, 'occurredAtUtc', 'OccurredAtUtc'),
                })),
            };
        };

        const severityClass = (s) => {
            const v = String(s || '').toLowerCase();
            if (v === 'critical' || v === '2') return 'bg-danger';
            if (v === 'warning' || v === '1') return 'bg-warning text-dark';
            return 'bg-secondary';
        };

        const severityLabel = (s) => {
            const v = String(s || '').toLowerCase();
            if (v === 'critical' || v === '2') return t('critical');
            if (v === 'warning' || v === '1') return t('warning');
            return t('info');
        };

        const normalizeDigest = (c) => {
            if (!c) return null;
            return {
                summaryAr: pick(c, 'summaryAr', 'SummaryAr') ?? '',
                aiInsightsAr: pick(c, 'aiInsightsAr', 'AiInsightsAr') ?? [],
                aiInsightsAr: pick(c, 'aiInsightsAr', 'AiInsightsAr') ?? [],
                highlights: (pick(c, 'highlights', 'Highlights') ?? []).map((h) => ({
                    labelAr: pick(h, 'labelAr', 'LabelAr'),
                    valueAr: pick(h, 'valueAr', 'ValueAr'),
                    trendAr: pick(h, 'trendAr', 'TrendAr'),
                })),
            };
        };

        const exportCsv = () => {
            const rows = state.data?.items || [];
            if (!rows.length) return;
            const header = ['Severity', 'Title', 'Detail', 'Branch'];
            const lines = [header.join(',')].concat(
                rows.map((r) =>
                    [severityLabel(r.severity), r.titleAr, r.detailAr, r.branchName || '']
                        .map((c) => `"${String(c || '').replace(/"/g, '""')}"`)
                        .join(',')
                )
            );
            const blob = new Blob(['\uFEFF' + lines.join('\n')], { type: 'text/csv;charset=utf-8;' });
            const a = document.createElement('a');
            a.href = URL.createObjectURL(blob);
            a.download = `executive-exceptions-${new Date().toISOString().slice(0, 10)}.csv`;
            a.click();
        };

        const exportPdf = () => {
            const rows = state.data?.items || [];
            if (!rows.length || typeof jspdf === 'undefined') {
                exportCsv();
                return;
            }
            const { jsPDF } = jspdf;
            const doc = new jsPDF({ orientation: 'p', unit: 'mm', format: 'a4' });
            doc.setFont('helvetica');
            doc.setFontSize(14);
            doc.text('Executive Exceptions', 14, 16);
            doc.setFontSize(10);
            let y = 26;
            rows.forEach((r) => {
                const line = `${severityLabel(r.severity)}: ${r.titleAr}`;
                doc.text(line.slice(0, 90), 14, y);
                y += 6;
                doc.text((r.detailAr || '').slice(0, 100), 14, y);
                y += 10;
                if (y > 270) {
                    doc.addPage();
                    y = 20;
                }
            });
            doc.save(`executive-exceptions-${new Date().toISOString().slice(0, 10)}.pdf`);
        };

        const load = async () => {
            state.loading = true;
            try {
                const params = typeof ExecutiveScopeBar !== 'undefined'
                    ? ExecutiveScopeBar.getQueryParams()
                    : {};
                const [exRes, digRes] = await Promise.all([
                    AxiosManager.get('/Telecom/GetExecutiveExceptions', { params }),
                    AxiosManager.get('/Telecom/GetExecutiveWeeklyDigest', { params }),
                ]);
                state.data = normalize(exRes?.data?.content);
                state.digest = normalizeDigest(digRes?.data?.content);
            } catch (e) {
                console.error('Executive exceptions/digest failed', e);
                state.data = { scopeLabelAr: '—', items: [] };
                state.digest = null;
            } finally {
                state.loading = false;
            }
        };

        const app = Vue.createApp({
            setup() {
                Vue.onMounted(() => {
                    load();
                    connectAlerts();
                });
                return {
                    ...Vue.toRefs(state),
                    t,
                    load,
                    exportCsv,
                    exportPdf,
                    severityClass,
                    severityLabel,
                };
            },
        });
        app.mount(rootEl);
        return app;
    }

    function scrollToPanel() {
        const embed = document.getElementById('executive-exceptions');
        if (embed && window.location.hash === '#executive-exceptions') {
            embed.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    async function initFromDashboard() {
        if (!hasPermission()) return null;
        revealEmbed();
        const root = document.getElementById('executiveExceptionsApp');
        if (!root) return null;
        try {
            await SecurityManager.validateToken();
        } catch { /* handled */ }
        const app = mount(root);
        scrollToPanel();
        return app;
    }

    async function initStandalone() {
        if (!hasPermission()) {
            const gate = document.getElementById('executiveExceptionsGate');
            if (gate) gate.classList.remove('d-none');
            return null;
        }
        const root = document.getElementById('executiveExceptionsApp');
        if (!root) return null;
        await SecurityManager.validateToken();
        return mount(root);
    }

    return { hasPermission, mount, initFromDashboard, initStandalone, revealEmbed, scrollToPanel };
})();
