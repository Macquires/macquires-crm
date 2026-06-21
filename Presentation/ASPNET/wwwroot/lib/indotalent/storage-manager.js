const STORAGE_KEYS = {
    ACCESS_TOKEN: 'accessToken',
    REFRESH_TOKEN: 'refreshToken',
    IS_AUTHENTICATED: 'isAuthenticated',
    FIRST_NAME: 'firstName',
    LAST_NAME: 'lastName',
    EMAIL: 'email',
    USER_ID: 'userId',
    USER_ROLES: 'userRoles',
    MENU_NAVIGATION: 'menuNavigation',
    PRIMARY_MENU_PERSONA: 'primaryMenuPersona',
    MENU_BADGES: 'menuBadges',
    AVATAR: 'avatar',
    COMPANY: 'company',
    PERMISSIONS: 'userPermissions',
    LANDING_PATH: 'landingPath',
};

const StorageManager = {
    save: (key, value) => {
        try {
            localStorage.setItem(key, JSON.stringify(value));
        } catch (error) {
            console.error('Failed to save data to localStorage', error);
        }
    },

    get: (key) => {
        try {
            const value = localStorage.getItem(key);
            return value ? JSON.parse(value) : null;
        } catch (error) {
            console.error('Failed to retrieve data from localStorage', error);
            return null;
        }
    },

    remove: (key) => {
        try {
            localStorage.removeItem(key);
        } catch (error) {
            console.error('Failed to remove data from localStorage', error);
        }
    },

    clearStorage: () => {
        try {
            StorageManager.clearAccessTokenCookie();
            localStorage.clear();
            StorageManager.clearUiSessionState();
        } catch (error) {
            console.error('Failed to clear localStorage', error);
        }
    },

    clearUiSessionState: () => {
        try {
            [
                'syrSessionSynced',
                'syrPreviewPersona',
                'syrNavExpandedModules',
                'syrSidebarScrollPosition',
                'sidebarScrollPosition',
            ].forEach((key) => sessionStorage.removeItem(key));
        } catch (_) {
            /* ignore */
        }
    },

    saveAccessToken: (token) => {
        StorageManager.save(STORAGE_KEYS.ACCESS_TOKEN, token);
        StorageManager.saveAccessTokenCookie(token);
    },
    saveAccessTokenCookie: (token) => {
        if (!token || typeof token !== 'string') return;
        const maxAge = 60 * 60 * 8;
        document.cookie = `accessToken=${encodeURIComponent(token)}; path=/; max-age=${maxAge}; SameSite=Lax`;
    },
    clearAccessTokenCookie: () => {
        document.cookie = 'accessToken=; path=/; max-age=0; SameSite=Lax';
    },
    getAccessToken: () => StorageManager.get(STORAGE_KEYS.ACCESS_TOKEN),
    removeAccessToken: () => StorageManager.remove(STORAGE_KEYS.ACCESS_TOKEN),

    saveRefreshToken: (token) => StorageManager.save(STORAGE_KEYS.REFRESH_TOKEN, token),
    getRefreshToken: () => StorageManager.get(STORAGE_KEYS.REFRESH_TOKEN),
    removeRefreshToken: () => StorageManager.remove(STORAGE_KEYS.REFRESH_TOKEN),

    saveIsAuthenticated: (status) => StorageManager.save(STORAGE_KEYS.IS_AUTHENTICATED, status),
    getIsAuthenticated: () => StorageManager.get(STORAGE_KEYS.IS_AUTHENTICATED),
    removeIsAuthenticated: () => StorageManager.remove(STORAGE_KEYS.IS_AUTHENTICATED),

    saveFirstName: (firstName) => StorageManager.save(STORAGE_KEYS.FIRST_NAME, firstName),
    getFirstName: () => StorageManager.get(STORAGE_KEYS.FIRST_NAME),
    removeFirstName: () => StorageManager.remove(STORAGE_KEYS.FIRST_NAME),

    saveLastName: (lastName) => StorageManager.save(STORAGE_KEYS.LAST_NAME, lastName),
    getLastName: () => StorageManager.get(STORAGE_KEYS.LAST_NAME),
    removeLastName: () => StorageManager.remove(STORAGE_KEYS.LAST_NAME),

    saveEmail: (email) => StorageManager.save(STORAGE_KEYS.EMAIL, email),
    getEmail: () => StorageManager.get(STORAGE_KEYS.EMAIL),
    removeEmail: () => StorageManager.remove(STORAGE_KEYS.EMAIL),

    saveUserId: (userId) => StorageManager.save(STORAGE_KEYS.USER_ID, userId),
    getUserId: () => {
        let userId = StorageManager.get(STORAGE_KEYS.USER_ID);
        if (userId) {
            return userId;
        }
        userId = StorageManager.parseUserIdFromAccessToken();
        if (userId) {
            StorageManager.saveUserId(userId);
        }
        return userId;
    },
    removeUserId: () => StorageManager.remove(STORAGE_KEYS.USER_ID),

    /**
     * Unwraps ApiSuccessResult → MediatR wrapper (LoginResult/RefreshTokenResult) → DTO.
     * API shape: { content: { data: { data: { landingPath, permissions, ... } } } }
     */
    loginPayload: (apiResponse) => {
        const envelope = apiResponse?.content ?? apiResponse?.Content ?? apiResponse;
        const outer = envelope?.data ?? envelope?.Data ?? envelope;
        const inner = outer?.data ?? outer?.Data;
        return inner ?? outer ?? {};
    },

    pickLoginField: (payload, camel, pascal) => {
        if (!payload) {
            return null;
        }
        return payload[camel] ?? payload[pascal] ?? null;
    },

    parseUserIdFromAccessToken: () => {
        const token = StorageManager.getAccessToken();
        if (!token || typeof token !== 'string') {
            return null;
        }
        try {
            const parts = token.split('.');
            if (parts.length < 2) {
                return null;
            }
            const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
            const json = JSON.parse(atob(base64));
            return (
                json.sub ??
                json.nameid ??
                json['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
                null
            );
        } catch {
            return null;
        }
    },

    saveUserRoles: (roles) => StorageManager.save(STORAGE_KEYS.USER_ROLES, roles),
    getUserRoles: () => StorageManager.get(STORAGE_KEYS.USER_ROLES),
    removeUserRoles: () => StorageManager.remove(STORAGE_KEYS.USER_ROLES),

    saveMenuNavigation: (navigations) => {
        const rows = Array.isArray(navigations) ? navigations : [];
        const normalized = rows.map((r) => {
            const nav = r.navURL || r.navUrl || r.NavURL || '';
            return Object.assign({}, r, {
                id: String(r.id ?? r.Id ?? ''),
                pid: r.pid ?? r.Pid ?? null,
                name: r.name ?? r.Name ?? '',
                navURL: nav,
                navUrl: nav,
                hasChild: !!(r.hasChild ?? r.HasChild),
            });
        });
        StorageManager.save(STORAGE_KEYS.MENU_NAVIGATION, normalized);
    },
    getMenuNavigation: () => StorageManager.get(STORAGE_KEYS.MENU_NAVIGATION),
    removeMenuNavigation: () => StorageManager.remove(STORAGE_KEYS.MENU_NAVIGATION),

    savePrimaryMenuPersona: (persona) => StorageManager.save(STORAGE_KEYS.PRIMARY_MENU_PERSONA, persona),
    getPrimaryMenuPersona: () => StorageManager.get(STORAGE_KEYS.PRIMARY_MENU_PERSONA),
    removePrimaryMenuPersona: () => StorageManager.remove(STORAGE_KEYS.PRIMARY_MENU_PERSONA),

    saveMenuBadges: (badges) => StorageManager.save(STORAGE_KEYS.MENU_BADGES, badges),
    getMenuBadges: () => StorageManager.get(STORAGE_KEYS.MENU_BADGES) || {},
    removeMenuBadges: () => StorageManager.remove(STORAGE_KEYS.MENU_BADGES),

    saveAvatar: (avatar) => StorageManager.save(STORAGE_KEYS.AVATAR, avatar),
    getAvatar: () => StorageManager.get(STORAGE_KEYS.AVATAR),
    removeAvatar: () => StorageManager.remove(STORAGE_KEYS.AVATAR),

    saveCompany: (company) => StorageManager.save(STORAGE_KEYS.COMPANY, company),
    getCompany: () => StorageManager.get(STORAGE_KEYS.COMPANY),
    removeCompany: () => StorageManager.remove(STORAGE_KEYS.COMPANY),

    PERSONA_LANDING_PATHS: {
        SysAdmin: '/Telecom/TelecomHub',
        BackOffice: '/Telecom/BackOfficeDashboard',
        Executive: '/Executive/CommandCenter?tab=scorecard',
        Retail: '/Telecom/TelecomHub',
        CallCenter: '/Telecom/UnifiedSearch',
    },

    PERMISSION_LANDING_RULES: [
        {
            any: ['admin.users.manage', 'admin.settings.manage', 'admin.roles.manage'],
            path: '/Administration/UserList',
        },
        {
            any: ['bulk.import.upload', 'bulk.import.monitor', 'telecom.asset.manage', 'admin.settings.manage'],
            path: '/Telecom/BackOfficeDashboard',
        },
        {
            any: [
                'telecom.hub.frontline',
                'telecom.hub.backoffice',
                'telecom.hub.supervisor',
                'telecom.line.activate',
                'telecom.asset.manage',
            ],
            path: '/Telecom/TelecomHub',
        },
        { any: ['customer.view'], path: '/Telecom/UnifiedSearch' },
    ],

    savePermissions: (keys) => StorageManager.save(STORAGE_KEYS.PERMISSIONS, keys || []),
    getPermissions: () => StorageManager.get(STORAGE_KEYS.PERMISSIONS) || [],

    saveLandingPath: (path) => {
        if (path) {
            StorageManager.save(STORAGE_KEYS.LANDING_PATH, path);
        }
    },
    getSavedLandingPath: () => StorageManager.get(STORAGE_KEYS.LANDING_PATH),

    hasAnyPermission: (perms, keys) => {
        const set = new Set((perms || []).map((k) => String(k).toLowerCase()));
        return (keys || []).some((k) => set.has(String(k).toLowerCase()));
    },

    getLandingPathForPermissions: (perms) => {
        const keys = perms || StorageManager.getPermissions();
        if (!keys || keys.length === 0) {
            return '/Profiles/MyProfile';
        }
        for (const rule of StorageManager.PERMISSION_LANDING_RULES) {
            if (StorageManager.hasAnyPermission(keys, rule.any)) {
                return rule.path;
            }
        }
        return '/Profiles/MyProfile';
    },

    getLandingPath: () => {
        const saved = StorageManager.getSavedLandingPath();
        if (saved) {
            return saved;
        }
        if (StorageManager.isStrictSyriatelTelecomWorkspaceUser()) {
            const persona = StorageManager.getPrimaryMenuPersona();
            if (persona && StorageManager.PERSONA_LANDING_PATHS[persona]) {
                return StorageManager.PERSONA_LANDING_PATHS[persona];
            }
        }
        return StorageManager.getLandingPathForPermissions(StorageManager.getPermissions());
    },

    /** True when the API envelope reports success (code 200) or HTTP 2xx with no envelope. */
    isApiSuccess: (response) => {
        const body = response?.data;
        const code = body?.code ?? body?.Code;
        if (code !== undefined && code !== null) {
            return Number(code) === 200;
        }
        const status = response?.status;
        return status >= 200 && status < 300;
    },

    /** Unwrap ApiSuccessResult content; supports MediatR { data: dto } wrappers. */
    apiContent: (response) => {
        const envelope = response?.data?.content ?? response?.data?.Content;
        if (!envelope) {
            return null;
        }
        const inner = envelope.data ?? envelope.Data;
        if (inner !== undefined && inner !== null && typeof inner === 'object' && !Array.isArray(inner)) {
            const nested = inner.data ?? inner.Data;
            if (nested !== undefined) {
                return nested;
            }
        }
        return inner ?? envelope;
    },

    apiList: (response) => {
        const payload = StorageManager.apiContent(response);
        if (Array.isArray(payload)) {
            return payload;
        }
        if (payload && typeof payload === 'object') {
            const nested = payload.data ?? payload.Data;
            if (Array.isArray(nested)) {
                return nested;
            }
        }
        return [];
    },

    applyOperatorSession: (session) => {
        if (!session) {
            return;
        }
        const perms = session.permissions ?? session.Permissions;
        if (Array.isArray(perms) && perms.length > 0) {
            StorageManager.savePermissions(perms);
        }
        const landing = session.landingPath ?? session.LandingPath;
        if (landing) {
            StorageManager.saveLandingPath(landing);
        }
        const menu = session.menuNavigation ?? session.MenuNavigation;
        if (Array.isArray(menu)) {
            StorageManager.saveMenuNavigation(menu);
        }
        const roles = session.roles ?? session.Roles;
        if (Array.isArray(roles) && roles.length > 0) {
            StorageManager.saveUserRoles(roles);
        }
        const persona = session.primaryMenuPersona ?? session.PrimaryMenuPersona;
        if (persona) {
            StorageManager.savePrimaryMenuPersona(persona);
        }
    },

    getLandingPathForPersona: (persona) => {
        const saved = StorageManager.getSavedLandingPath();
        if (saved) {
            return saved;
        }
        const perms = StorageManager.getPermissions();
        if (perms && perms.length > 0) {
            return StorageManager.getLandingPathForPermissions(perms);
        }
        const p = persona || StorageManager.getPrimaryMenuPersona();
        return StorageManager.PERSONA_LANDING_PATHS[p] || '/Profiles/MyProfile';
    },

    normalizeNavPath: (url) => {
        if (!url || url === '#') return '';
        const path = String(url).split('?')[0].split('#')[0].trim().toLowerCase();
        if (!path) return '';
        const withSlash = path.startsWith('/') ? path : '/' + path;
        return withSlash.replace(/\/+$/, '') || '/';
    },

    /** Hub-linked lookup pages — allowed by PersonaStrictGate but omitted from sidebar JSON. */
    SUBSCRIBER_LOOKUP_PATHS: ['/telecom/unifiedsearch', '/telecom/customer360profile'],

    isSubscriberLookupPath: (path) => {
        const p = path || StorageManager.normalizeNavPath(window.location.pathname);
        return StorageManager.SUBSCRIBER_LOOKUP_PATHS.includes(p);
    },

    isPathAllowedForCurrentMenu: () => {
        const current = StorageManager.normalizeNavPath(window.location.pathname);
        if (!current || current.startsWith('/accounts')) {
            return true;
        }
        if (StorageManager.isSubscriberLookupPath(current)) {
            return true;
        }
        if (typeof SecurityManager !== 'undefined' && typeof SecurityManager.canAccessPortalPath === 'function') {
            const portalPath = SecurityManager.normalizePortalPath(window.location.pathname);
            const keys = SecurityManager.PORTAL_PATH_PERMISSIONS[portalPath];
            if (keys && keys.length && SecurityManager.canAccessPortalPath(window.location.pathname)) {
                return true;
            }
        }
        const rows = StorageManager.getMenuNavigation() || [];
        const allowed = new Set();
        rows.forEach((r) => {
            const url = r.navURL || r.navUrl;
            if (url && url !== '#') {
                allowed.add(StorageManager.normalizeNavPath(url));
            }
        });
        if (allowed.size === 0) {
            return true;
        }
        return allowed.has(current);
    },

    saveLoginResult: (data) => {
        StorageManager.clearUiSessionState();
        const p = StorageManager.loginPayload(data);
        StorageManager.saveAccessToken(StorageManager.pickLoginField(p, 'accessToken', 'AccessToken'));
        StorageManager.saveRefreshToken(StorageManager.pickLoginField(p, 'refreshToken', 'RefreshToken'));
        StorageManager.saveFirstName(StorageManager.pickLoginField(p, 'firstName', 'FirstName'));
        StorageManager.saveLastName(StorageManager.pickLoginField(p, 'lastName', 'LastName'));
        StorageManager.saveEmail(StorageManager.pickLoginField(p, 'email', 'Email'));
        StorageManager.saveUserId(
            StorageManager.pickLoginField(p, 'userId', 'UserId') ?? StorageManager.parseUserIdFromAccessToken()
        );
        StorageManager.saveUserRoles(StorageManager.pickLoginField(p, 'roles', 'Roles'));
        StorageManager.saveMenuNavigation(StorageManager.pickLoginField(p, 'menuNavigation', 'MenuNavigation'));
        StorageManager.savePrimaryMenuPersona(
            StorageManager.pickLoginField(p, 'primaryMenuPersona', 'PrimaryMenuPersona')
        );
        StorageManager.savePermissions(
            StorageManager.pickLoginField(p, 'permissions', 'Permissions') || []
        );
        const landing =
            StorageManager.pickLoginField(p, 'landingPath', 'LandingPath') ||
            (StorageManager.isStrictSyriatelTelecomWorkspaceUser() &&
            StorageManager.getPrimaryMenuPersona() &&
            StorageManager.PERSONA_LANDING_PATHS[StorageManager.getPrimaryMenuPersona()]
                ? StorageManager.PERSONA_LANDING_PATHS[StorageManager.getPrimaryMenuPersona()]
                : StorageManager.getLandingPathForPermissions(StorageManager.getPermissions()));
        StorageManager.saveLandingPath(landing);
        StorageManager.saveAvatar(StorageManager.pickLoginField(p, 'avatar', 'Avatar'));
        StorageManager.saveIsAuthenticated(StorageManager.getUserId() != null);
    },

    /** True when the operator has any telecom workspace permission (role-name agnostic). */
    isTelecomWorkspaceUser: () => {
        const perms = StorageManager.getPermissions();
        if (!perms || perms.length === 0) {
            return false;
        }
        const prefixes = ['telecom.', 'bulk.import.', 'customer.', 'admin.'];
        return perms.some((p) => {
            const key = String(p).toLowerCase();
            return prefixes.some((prefix) => key.startsWith(prefix));
        });
    },

    isStrictSyriatelTelecomWorkspaceUser: () => StorageManager.isTelecomWorkspaceUser(),

    /** Display name for Operator Console top bar (persona for demos). */
    getSyriatelOperatorConsoleDisplayName: () => {
        const persona = StorageManager.getPrimaryMenuPersona();
        const personaLabels = {
            Executive: 'الإدارة العليا',
            CallCenter: 'مركز الاتصال',
            Retail: 'نقطة البيع',
            BackOffice: 'العمليات',
            SysAdmin: 'الإدارة التقنية',
        };
        if (persona && personaLabels[persona]) {
            return personaLabels[persona];
        }
        const roles = StorageManager.getUserRoles() || [];
        const order = [
            ['TelecomManagement', 'الإدارة العليا'],
            ['TelecomAdmin', 'الإدارة التقنية'],
            ['TelecomBackOffice', 'العمليات'],
            ['TelecomCallCenter', 'مركز الاتصال'],
            ['TelecomShowroom', 'نقطة البيع'],
        ];
        for (const [role, label] of order) {
            if (roles.includes(role)) {
                return label;
            }
        }
        return null;
    },
};
