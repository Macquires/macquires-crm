const SecurityManager = {
    TELECOM_ROLES: [
        'TelecomAdmin',
        'TelecomManagement',
        'TelecomBackOffice',
        'TelecomShowroom',
        'TelecomCallCenter',
    ],

    /** Mirrors NavigationPermissionRules (server) for client-side page guards. */
    PORTAL_PATH_PERMISSIONS: {
        '/dashboards/defaultdashboard': [
            'customer.view',
            'telecom.reports.mis',
            'admin.users.manage',
            'admin.settings.manage',
            'admin.roles.manage',
        ],
        '/dashboards/dashboardwidgetlist': ['admin.settings.manage'],
        '/telecom/backofficedashboard': [
            'bulk.import.upload',
            'bulk.import.monitor',
            'telecom.asset.manage',
        ],
        '/telecom/telecomhub': [
            'customer.view',
            'telecom.line.activate',
            'telecom.line.simswap',
            'telecom.line.migrate',
        ],
        '/telecom/unifiedsearch': [
            'customer.view',
            'telecom.line.activate',
            'telecom.line.simswap',
            'telecom.line.migrate',
        ],
        '/telecom/customer360profile': [
            'customer.view',
            'telecom.line.activate',
            'telecom.line.simswap',
            'telecom.line.migrate',
        ],
        '/telecom/integrationmonitor': ['admin.integration.monitor'],
        '/telecom/bulkimportmonitor': ['bulk.import.monitor', 'bulk.import.upload'],
        '/telecom/msisdninventory': ['telecom.asset.manage'],
        '/telecom/technicalticketlist': [
            'customer.view',
            'bulk.import.upload',
            'telecom.asset.manage',
        ],
        '/telecom/backofficeauditlist': ['bulk.import.upload', 'telecom.asset.manage'],
        '/customers/customerlist': ['customer.view'],
        '/customergroups/customergrouplist': ['telecom.asset.manage'],
        '/customercategories/customercategorylist': ['telecom.asset.manage'],
        '/customercontacts/customercontactlist': ['customer.view'],
        '/telecom/productcatalog': ['telecom.line.activate'],
        '/products/productlist': ['telecom.asset.manage'],
        '/telecom/vascataloglist': ['telecom.vas.manage'],
        '/telecomsubscriptiontypes/telecomsubscriptiontypelist': ['telecom.line.activate'],
        '/telecom/telecommisreports': ['telecom.reports.mis'],
        '/administration/userlist': ['admin.users.manage'],
        '/administration/rolelist': ['admin.roles.manage'],
        '/administration/globalsettings': ['admin.settings.manage'],
        '/administration/auditloglist': ['admin.audit.view'],
        '/companies/mycompany': ['admin.settings.manage'],
        '/numbersequences/numbersequencelist': ['admin.settings.manage'],
        '/profiles/myprofile': [],
    },

    normalizePortalPath(pathname) {
        const p = String(pathname || '')
            .split('?')[0]
            .split('#')[0]
            .trim()
            .replace(/\/+$/, '')
            .toLowerCase();
        return p || '/';
    },

    canAccessPortalPath(pathname) {
        const path = SecurityManager.normalizePortalPath(pathname);
        const search = typeof window !== 'undefined' ? window.location.search || '' : '';
        if (path === '/profiles/myprofile') {
            return true;
        }
        const keys = SecurityManager.PORTAL_PATH_PERMISSIONS[path];
        if (!keys) {
            return true;
        }
        if (keys.length === 0) {
            return true;
        }
        const perms = StorageManager.getPermissions?.() || [];
        return StorageManager.hasAnyPermission(perms, keys);
    },

    denyPageAccess() {
        Swal.fire({
            icon: 'error',
            title: 'Unauthorized',
            text: 'You are being redirected...',
            timer: 2000,
            showConfirmButton: false,
        });
        setTimeout(() => {
            const landing =
                typeof StorageManager.getLandingPath === 'function'
                    ? StorageManager.getLandingPath()
                    : null;
            if (StorageManager.getAccessToken?.() && landing) {
                window.location.replace(landing);
            } else {
                window.location.href = '/Accounts/Login';
            }
        }, 2000);
    },

    authorizePage: async (requiredRoles) => {
        const userRoles = StorageManager.getUserRoles() || [];
        const roles = requiredRoles || [];

        if (roles.some((role) => userRoles.includes(role))) {
            return true;
        }

        if (
            typeof StorageManager.isStrictSyriatelTelecomWorkspaceUser === 'function' &&
            StorageManager.isStrictSyriatelTelecomWorkspaceUser()
        ) {
            if (
                typeof StorageManager.isPathAllowedForCurrentMenu === 'function' &&
                StorageManager.isPathAllowedForCurrentMenu()
            ) {
                return true;
            }
            if (SecurityManager.canAccessPortalPath(window.location.pathname)) {
                return true;
            }
            SecurityManager.denyPageAccess();
            return false;
        }

        if (SecurityManager.TELECOM_ROLES.some((r) => userRoles.includes(r))) {
            if (SecurityManager.canAccessPortalPath(window.location.pathname)) {
                return true;
            }
        }

        const pathKeys =
            SecurityManager.PORTAL_PATH_PERMISSIONS[
                SecurityManager.normalizePortalPath(window.location.pathname)
            ];
        const perms = StorageManager.getPermissions?.() || [];
        if (pathKeys && pathKeys.length && StorageManager.hasAnyPermission(perms, pathKeys)) {
            return true;
        }

        SecurityManager.denyPageAccess();
        return false;
    },

    /** Synchronous RBAC check (roles OR any listed permission). Does not redirect. */
    canAccessTelecom: ({ roles = [], permissions = [] } = {}) => {
        const userRoles = StorageManager.getUserRoles() || [];
        const userPerms = StorageManager.getPermissions?.() || [];
        const roleOk = roles.length === 0 || roles.some((r) => userRoles.includes(r));
        const permOk =
            permissions.length === 0 ||
            (typeof StorageManager.hasAnyPermission === 'function' &&
                StorageManager.hasAnyPermission(userPerms, permissions));
        return roleOk || permOk;
    },

    /** Authorize by Identity role and/or RBAC permission keys (permission-based access). */
    authorizeTelecomAccess: async ({ roles = [], permissions = [] } = {}) => {
        if (SecurityManager.canAccessTelecom({ roles, permissions })) {
            return true;
        }
        return false;
    },

    validateToken: async () => {
        try {
            const response = await AxiosManager.post('/Security/ValidateToken', {});

            if (response?.data?.code === 200) {
                return true;
            }
            Swal.fire({
                icon: 'error',
                title: 'Token not valid',
                text: 'You are being redirected...',
                timer: 2000,
                showConfirmButton: false,
            });
            setTimeout(() => {
                window.location.href = '/Accounts/Login';
            }, 2000);
            return false;
        } catch (error) {
            const msg = error?.response?.data?.message || '';
            const isPersonaBlock = error?.response?.status === 403 && String(msg).includes('Persona');
            const landing =
                isPersonaBlock && typeof StorageManager?.getLandingPath === 'function'
                    ? StorageManager.getLandingPath()
                    : isPersonaBlock && typeof StorageManager?.getLandingPathForPersona === 'function'
                      ? StorageManager.getLandingPathForPersona(StorageManager.getPrimaryMenuPersona())
                      : null;
            if (landing) {
                window.location.replace(landing);
                return false;
            }
            Swal.fire({
                icon: 'error',
                title: msg || 'Error validating token',
                text: 'You are being redirected...',
                timer: 2000,
                showConfirmButton: false,
            });
            setTimeout(() => {
                window.location.href = '/Accounts/Login';
            }, 2000);
            return false;
        }
    },
};
