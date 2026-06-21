/**
 * Branch geo heatmap — GET /Telecom/GetBranchGeoHeatmap
 */
const BranchGeoHeatmapPanel = (function () {
    const MIS_PERMISSION = 'telecom.reports.mis';

    function t(key) {
        return (typeof TelecomI18n !== 'undefined' && TelecomI18n.t
            ? TelecomI18n.t('defaultDashboard.geoHeatmap.' + key)
            : null) || key;
    }

    function hasPermission() {
        const perms = StorageManager.getPermissions?.() || [];
        return StorageManager.hasAnyPermission?.(perms, [MIS_PERMISSION]);
    }

    function pick(o, ...keys) {
        for (const k of keys) {
            if (o?.[k] !== undefined && o?.[k] !== null) return o[k];
        }
        return undefined;
    }

    function project(lat, lng) {
        const minLat = 32.3;
        const maxLat = 37.4;
        const minLng = 35.7;
        const maxLng = 42.4;
        const x = ((lng - minLng) / (maxLng - minLng)) * 100;
        const y = (1 - (lat - minLat) / (maxLat - minLat)) * 100;
        return { x: Math.max(2, Math.min(98, x)), y: Math.max(2, Math.min(98, y)) };
    }

    function mount(rootEl) {
        const state = Vue.reactive({ data: null, loading: false });

        const load = async () => {
            state.loading = true;
            try {
                const params = typeof ExecutiveScopeBar !== 'undefined'
                    ? ExecutiveScopeBar.getQueryParams()
                    : {};
                const res = await AxiosManager.get('/Telecom/GetBranchGeoHeatmap', { params });
                const c = res?.data?.content;
                state.data = {
                    scopeLabelAr: pick(c, 'scopeLabelAr', 'ScopeLabelAr') ?? '',
                    points: (pick(c, 'points', 'Points') ?? []).map((p) => ({
                        branchName: pick(p, 'branchName', 'BranchName'),
                        revenue: pick(p, 'revenue', 'Revenue') ?? 0,
                        heatScore: pick(p, 'heatScore', 'HeatScore') ?? 0,
                        lat: pick(p, 'latitude', 'Latitude'),
                        lng: pick(p, 'longitude', 'Longitude'),
                    })),
                };
            } catch (e) {
                console.error('BranchGeoHeatmap failed', e);
                state.data = { scopeLabelAr: '—', points: [] };
            } finally {
                state.loading = false;
            }
        };

        const app = Vue.createApp({
            setup() {
                const numberLocale = typeof TelecomI18n !== 'undefined' && TelecomI18n.getLang?.() === 'en' ? 'en-US' : 'ar-SY';
                const formatMoney = (v) => Number(v || 0).toLocaleString(numberLocale) + ' ' + t('currency');
                const dotStyle = (p) => {
                    const pos = project(Number(p.lat), Number(p.lng));
                    const size = 12 + Math.round((p.heatScore || 0) / 8);
                    const alpha = 0.35 + (p.heatScore || 0) / 200;
                    return {
                        left: pos.x + '%',
                        top: pos.y + '%',
                        width: size + 'px',
                        height: size + 'px',
                        transform: 'translate(-50%,-50%)',
                        background: `rgba(200,16,46,${alpha})`,
                    };
                };
                Vue.onMounted(load);
        document.addEventListener(
            typeof ExecutiveScopeBar !== 'undefined' ? ExecutiveScopeBar.EVENT : 'executive-scope-changed',
            load
        );
        return { ...Vue.toRefs(state), t, load, dotStyle, formatMoney };
            },
            template: `
            <div>
            <header class="strategic-exec-hero mb-3">
                <p class="strategic-exec-kicker mb-1">{{ t('kicker') }}</p>
                <h2 class="h5 fw-bold text-syriatel-red">{{ t('title') }}</h2>
                <p class="small text-muted">{{ t('scopePrefix') }} <strong>{{ data?.scopeLabelAr || '—' }}</strong></p>
            </header>
            <div class="geo-heatmap-canvas position-relative border rounded mb-3" style="height:320px; background:linear-gradient(180deg,#e8f4fc,#f8fafc);">
                <div v-if="loading" class="position-absolute top-50 start-50 translate-middle text-muted">{{ t('loading') }}</div>
                <div v-for="(p, i) in data?.points || []" :key="i"
                     class="position-absolute rounded-circle border border-white shadow-sm"
                     :style="dotStyle(p)"
                     :title="p.branchName"></div>
            </div>
            <ul class="list-group list-group-flush strategic-panel">
                <li v-for="(p, i) in data?.points || []" :key="'r'+i" class="list-group-item d-flex justify-content-between py-2 bg-transparent">
                    <span>{{ p.branchName }}</span>
                    <span><strong>{{ formatMoney(p.revenue) }}</strong> <span class="badge bg-danger ms-1">{{ p.heatScore }}%</span></span>
                </li>
            </ul>
            </div>`,
        });
        app.mount(rootEl);
        return app;
    }

    async function initFromDashboard() {
        if (!hasPermission()) return null;
        const section = document.getElementById('branch-geo-heatmap');
        const root = document.getElementById('branchGeoHeatmapApp');
        if (!section || !root) return null;
        section.classList.remove('d-none');
        if (window.location.hash === '#branch-geo-heatmap') {
            section.scrollIntoView({ behavior: 'smooth' });
        }
        await SecurityManager.validateToken();
        return mount(root);
    }

    return { initFromDashboard, hasPermission, mount };
})();
