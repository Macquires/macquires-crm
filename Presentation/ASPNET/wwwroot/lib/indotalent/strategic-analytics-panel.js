/**
 * Executive strategic MIS panel — scoped by user/role via GET /Telecom/GetStrategicMetrics.
 * Mount on a container that includes the markup from DefaultDashboard strategic embed.
 */
const StrategicAnalyticsPanel = (function () {
    const MIS_PERMISSION = 'telecom.reports.mis';

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
        const embed =
            document.getElementById('strategicAnalyticsEmbed') ||
            document.getElementById('strategic-analytics');
        if (embed) {
            embed.classList.remove('d-none');
        }
    }

    function scrollToPanel() {
        const embed =
            document.getElementById('strategicAnalyticsEmbed') ||
            document.getElementById('strategic-analytics');
        if (embed && window.location.hash === '#strategic-analytics') {
            embed.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    function mount(rootEl) {
        if (!rootEl || typeof Vue === 'undefined') {
            return null;
        }

        const state = Vue.reactive({
            filters: { regionId: null, branchId: null },
            metrics: null,
            loading: false,
        });

        let revenueChart = null;
        let segmentChart = null;
        let leaderboardGrid = null;

        const pick = (o, ...keys) => {
            if (!o) return undefined;
            for (const k of keys) {
                if (o[k] !== undefined && o[k] !== null) return o[k];
            }
            return undefined;
        };

        const normalizeMetrics = (c) => {
            if (!c) return null;
            return {
                revenueIndex: pick(c, 'revenueIndex', 'RevenueIndex') ?? 0,
                arpu: pick(c, 'arpu', 'Arpu') ?? 0,
                revenueChangePercent: pick(c, 'revenueChangePercent', 'RevenueChangePercent') ?? 0,
                ticketsHandled: pick(c, 'ticketsHandled', 'TicketsHandled') ?? 0,
                slaCompliancePercent: pick(c, 'slaCompliancePercent', 'SlaCompliancePercent') ?? 0,
                topBranchName: pick(c, 'topBranchName', 'TopBranchName'),
                topBranchRank: pick(c, 'topBranchRank', 'TopBranchRank') ?? 0,
                accessLevel: pick(c, 'accessLevel', 'AccessLevel') ?? '',
                scopeLabelAr: pick(c, 'scopeLabelAr', 'ScopeLabelAr') ?? '',
                canUseFilters: pick(c, 'canUseFilters', 'CanUseFilters') === true,
                effectiveRegionId: pick(c, 'effectiveRegionId', 'EffectiveRegionId'),
                effectiveBranchId: pick(c, 'effectiveBranchId', 'EffectiveBranchId'),
                regions: (pick(c, 'regions', 'Regions') ?? []).map((r) => ({
                    id: pick(r, 'id', 'Id'),
                    nameAr: pick(r, 'nameAr', 'NameAr'),
                })),
                branches: (pick(c, 'branches', 'Branches') ?? []).map((b) => ({
                    id: pick(b, 'id', 'Id'),
                    nameAr: pick(b, 'nameAr', 'NameAr'),
                    regionId: pick(b, 'regionId', 'RegionId'),
                })),
                revenueTrend: (pick(c, 'revenueTrend', 'RevenueTrend') ?? []).map((p) => ({
                    label: pick(p, 'label', 'Label'),
                    value: pick(p, 'value', 'Value') ?? 0,
                })),
                subscriberSegments: (pick(c, 'subscriberSegments', 'SubscriberSegments') ?? []).map((s) => ({
                    segment: pick(s, 'segment', 'Segment'),
                    count: pick(s, 'count', 'Count') ?? 0,
                    percent: pick(s, 'percent', 'Percent') ?? 0,
                })),
                branchLeaderboard: (pick(c, 'branchLeaderboard', 'BranchLeaderboard') ?? []).map((row) => ({
                    branchId: pick(row, 'branchId', 'BranchId'),
                    branchName: pick(row, 'branchName', 'BranchName'),
                    activeSubscriptions: pick(row, 'activeSubscriptions', 'ActiveSubscriptions') ?? 0,
                    revenueContribution: pick(row, 'revenueContribution', 'RevenueContribution') ?? 0,
                    avgResolutionHours: pick(row, 'avgResolutionHours', 'AvgResolutionHours') ?? 0,
                    managerRating: pick(row, 'managerRating', 'ManagerRating') ?? 0,
                })),
            };
        };

        const branchOptions = Vue.computed(() => {
            const m = state.metrics;
            if (!m?.branches?.length) return [];
            if (!state.filters.regionId) return m.branches;
            return m.branches.filter((b) => b.regionId === state.filters.regionId);
        });

        const accessLevelLabel = Vue.computed(() => {
            const level = state.metrics?.accessLevel || '';
            const map = {
                GeneralManager: 'المدير العام',
                RegionalDirector: 'مدير إقليم',
                BranchManager: 'مدير فرع',
            };
            return map[level] || level || '—';
        });

        const formatMoney = (n) => {
            if (n == null || isNaN(n)) return '—';
            return new Intl.NumberFormat('ar-SY', { maximumFractionDigits: 0 }).format(n);
        };

        const formatPercent = (n) => {
            if (n == null || isNaN(n)) return '—';
            return `${Number(n).toFixed(1)}%`;
        };

        const deltaClass = (n) => {
            if (n == null || isNaN(n)) return '';
            return Number(n) >= 0 ? 'strategic-delta-up' : 'strategic-delta-down';
        };

        const waitForSyncfusion = () =>
            new Promise((resolve) => {
                const tick = () => {
                    if (typeof ej !== 'undefined' && ej.grids && ej.charts) resolve();
                    else setTimeout(tick, 50);
                };
                tick();
            });

        const refreshRevenueChart = (metrics) => {
            const host = rootEl.querySelector('#revenueTrendChart');
            if (!host || typeof ej === 'undefined') return;

            const trend = metrics?.revenueTrend ?? [];
            const series = [
                {
                    type: 'Area',
                    dataSource: trend,
                    xName: 'label',
                    yName: 'value',
                    name: 'الإيراد',
                    fill: 'rgba(200, 16, 46, 0.25)',
                    border: { color: '#c8102e', width: 2 },
                },
            ];

            if (revenueChart) {
                revenueChart.series = series;
                revenueChart.refresh();
                return;
            }

            revenueChart = new ej.charts.Chart(
                {
                    primaryXAxis: {
                        valueType: 'Category',
                        labelStyle: { color: '#64748b' },
                        majorGridLines: { width: 0 },
                    },
                    primaryYAxis: {
                        labelStyle: { color: '#64748b' },
                        majorGridLines: { color: '#e2e8f0' },
                        lineStyle: { width: 0 },
                    },
                    chartArea: { border: { width: 0 } },
                    series,
                    tooltip: { enable: true },
                    legendSettings: { visible: false },
                    background: 'transparent',
                    palettes: ['#c8102e'],
                },
                host
            );
        };

        const refreshSegmentChart = (metrics) => {
            const host = rootEl.querySelector('#subscriberSegmentChart');
            if (!host || typeof ej === 'undefined') return;

            const segments = (metrics?.subscriberSegments ?? []).map((s) => ({
                x: s.segment,
                y: s.count,
                text: `${s.percent}%`,
            }));

            if (segmentChart) {
                segmentChart.series[0].dataSource = segments;
                segmentChart.refresh();
                return;
            }

            segmentChart = new ej.charts.AccumulationChart(
                {
                    enableSmartLabels: true,
                    background: 'transparent',
                    series: [
                        {
                            type: 'Pie',
                            dataSource: segments,
                            xName: 'x',
                            yName: 'y',
                            innerRadius: '55%',
                            dataLabel: {
                                visible: true,
                                name: 'text',
                                position: 'Outside',
                                font: { color: '#1e293b', size: '11px' },
                            },
                        },
                    ],
                    legendSettings: {
                        visible: true,
                        position: 'Bottom',
                        textStyle: { color: '#64748b' },
                    },
                    palettes: ['#c8102e', '#a30d25', '#64748b', '#94a3b8'],
                },
                host
            );
        };

        const refreshLeaderboardGrid = (rows) => {
            const host = rootEl.querySelector('#ExecutiveLeaderboardGrid');
            if (!host || typeof ej === 'undefined') return;

            const data = rows ?? [];
            const columns = [
                { field: 'branchName', headerText: 'الفرع', width: 180 },
                { field: 'activeSubscriptions', headerText: 'اشتراكات نشطة', width: 120, textAlign: 'Right' },
                {
                    field: 'revenueContribution',
                    headerText: 'مساهمة الإيراد',
                    width: 130,
                    textAlign: 'Right',
                    format: 'N0',
                },
                {
                    field: 'avgResolutionHours',
                    headerText: 'سرعة حل التذاكر (س)',
                    width: 140,
                    textAlign: 'Right',
                    format: 'N1',
                },
                {
                    field: 'managerRating',
                    headerText: 'تقييم المدير',
                    width: 110,
                    textAlign: 'Right',
                    format: 'N1',
                },
            ];

            if (leaderboardGrid) {
                leaderboardGrid.dataSource = data;
                leaderboardGrid.dataBind();
                return;
            }

            leaderboardGrid = new ej.grids.Grid(
                {
                    dataSource: data,
                    columns,
                    allowPaging: data.length > 8,
                    pageSettings: { pageSize: 8 },
                    gridLines: 'Horizontal',
                    height: Math.min(360, 48 + data.length * 42),
                    rowHeight: 42,
                },
                host
            );
        };

        const syncFiltersFromMetrics = (m) => {
            if (!m) return;
            state.filters.regionId = m.effectiveRegionId ?? null;
            state.filters.branchId = m.effectiveBranchId ?? null;
        };

        const loadExecutiveMetrics = async () => {
            state.loading = true;
            try {
                const params = {};
                if (state.metrics?.canUseFilters) {
                    if (state.filters.regionId) params.regionId = state.filters.regionId;
                    if (state.filters.branchId) params.branchId = state.filters.branchId;
                }

                const res = await AxiosManager.get('/Telecom/GetStrategicMetrics', { params });
                const m = normalizeMetrics(res?.data?.content);
                state.metrics = m;
                if (m?.canUseFilters) {
                    syncFiltersFromMetrics(m);
                }

                await waitForSyncfusion();
                refreshRevenueChart(m);
                refreshSegmentChart(m);
                refreshLeaderboardGrid(m?.branchLeaderboard);
            } catch (err) {
                console.error('GetStrategicMetrics failed', err);
                state.metrics = state.metrics ?? {
                    scopeLabelAr: 'تعذّر التحميل',
                    canUseFilters: false,
                    branchLeaderboard: [],
                };
            } finally {
                state.loading = false;
            }
        };

        const onRegionChange = () => {
            state.filters.branchId = null;
            loadExecutiveMetrics();
        };

        const app = Vue.createApp({
            setup() {
                Vue.onMounted(() => {
                    loadExecutiveMetrics().then(scrollToPanel);
                });

                return {
                    ...Vue.toRefs(state),
                    branchOptions,
                    accessLevelLabel,
                    formatMoney,
                    formatPercent,
                    deltaClass,
                    loadExecutiveMetrics,
                    onRegionChange,
                };
            },
        });

        app.mount(rootEl);
        return app;
    }

    async function initFromDashboard() {
        if (!hasPermission()) {
            return null;
        }
        revealEmbed();
        const root = document.getElementById('strategicAnalyticsApp');
        if (!root) {
            return null;
        }
        try {
            await SecurityManager.validateToken();
        } catch {
            /* redirect handled by SecurityManager */
        }
        return mount(root);
    }

    async function initStandalone() {
        if (!hasPermission()) {
            return null;
        }
        const root = document.getElementById('strategicAnalyticsApp');
        if (!root) {
            return null;
        }
        return mount(root);
    }

    return {
        hasPermission,
        mount,
        initFromDashboard,
        initStandalone,
        revealEmbed,
        scrollToPanel,
    };
})();
