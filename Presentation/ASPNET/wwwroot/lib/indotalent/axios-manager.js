const AxiosManager = (() => {
    const axiosInstance = axios.create({
        baseURL: '/api',
        headers: {
            'accept': 'application/json',
            'Content-Type': 'application/json',
        }
    });

    let isRefreshing = false;
    let retryQueue = [];

    function isUiEn() {
        const lang = (document.documentElement?.lang || document.body?.lang || 'en').toLowerCase();
        return lang.startsWith('en');
    }

    function httpMsg(key, ar, en) {
        const hit = typeof TelecomI18n !== 'undefined' && TelecomI18n.t ? TelecomI18n.t(key) : null;
        if (hit) return hit;
        return isUiEn() ? en : ar;
    }

    axiosInstance.interceptors.request.use(
        (config) => {
            const token = StorageManager.getAccessToken(); 
            if (token) {
                config.headers['Authorization'] = `Bearer ${token}`;
            }
            try {
                const preview = sessionStorage.getItem('syrPreviewPersona');
                if (preview) {
                    config.headers['X-Syr-Preview-Persona'] = preview;
                }
            } catch (_) { /* ignore */ }
            return config;
        },
        (error) => {
            return Promise.reject(error);
        }
    );

    axiosInstance.interceptors.response.use(
        (response) => response,
        async (error) => {
            const originalRequest = error.config;
            if (error.response && error.response.status === 498) {
                if (!isRefreshing) {
                    isRefreshing = true;

                    try {
                        const refreshToken = StorageManager.getRefreshToken();
                        const response = await axiosInstance.post('/Security/RefreshToken', { refreshToken });

                        if (StorageManager.isApiSuccess(response)) {
                            StorageManager.saveLoginResult(response?.data);
                            isRefreshing = false;
                            retryQueue.forEach((cb) => cb());
                            retryQueue = [];
                            return axiosInstance(originalRequest);
                        } else {
                            throw new Error('Refresh token failed');
                        }
                    } catch (refreshError) {
                        retryQueue.forEach((cb) => cb());
                        retryQueue = [];
                        isRefreshing = false;
                        throw refreshError;
                    }
                }

                return new Promise((resolve, reject) => {
                    retryQueue.push(() => {
                        resolve(axiosInstance(originalRequest));
                    });
                });
            }

            if (error.response) {
                const status = error.response.status;
                const errData = error.response.data;

                if (status === 400 && errData?.error?.name === 'BusinessRuleViolationException') {
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'warning',
                            title: httpMsg('common.httpErrors.warningTitle', 'تنبيه', 'Notice'),
                            text: errData.message || httpMsg(
                                'common.httpErrors.businessRuleText',
                                'لا يمكن إتمام العملية بسبب قواعد العمل.',
                                'This action cannot be completed due to business rules.'
                            ),
                            confirmButtonColor: '#c8102e'
                        });
                    }
                } else if (status === 429 || status === 500) {
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'error',
                            title: httpMsg('common.httpErrors.networkTitle', 'خطأ في الشبكة', 'Network error'),
                            text: httpMsg(
                                'common.httpErrors.networkText',
                                'الشبكة مشغولة حالياً أو يوجد مشكلة في المخدم الخارجي، يرجى المحاولة بعد لحظات.',
                                'The network or external service is busy. Please try again shortly.'
                            ),
                            confirmButtonColor: '#c8102e'
                        });
                    }
                } else if (
                    status === 403
                    && errData?.message
                    && String(errData.message).includes('Persona')
                    && (typeof StorageManager?.getLandingPath === 'function' ||
                        typeof StorageManager?.getLandingPathForPersona === 'function')
                ) {
                    const landing =
                        typeof StorageManager.getLandingPath === 'function'
                            ? StorageManager.getLandingPath()
                            : StorageManager.getLandingPathForPersona(StorageManager.getPrimaryMenuPersona());
                    if (landing && !error.config?.__personaLandingRedirect) {
                        error.config.__personaLandingRedirect = true;
                        window.location.replace(landing);
                    }
                } else if (errData && errData.message) {
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'error',
                            title: httpMsg('common.httpErrors.errorTitle', 'حدث خطأ', 'Error'),
                            text: errData.message,
                            confirmButtonColor: '#c8102e'
                        });
                    }
                }
            }

            return Promise.reject(error);
        }
    );

    const request = async (method, url, options = {}) => {
        const { data, headers = {}, responseType = 'json', params, signal, timeout } = options;
        try {
            const response = await axiosInstance({
                method,
                url,
                ...(data !== undefined && method.toLowerCase() !== 'get' ? { data } : {}),
                ...(params !== undefined ? { params } : {}),
                headers: {
                    ...headers,
                },
                responseType,
                ...(signal !== undefined ? { signal } : {}),
                ...(timeout !== undefined ? { timeout } : {}),
            });
            return response;
        } catch (error) {
            throw error;
        }
    };

    return {
        getBaseUrl: () => axiosInstance.defaults.baseURL || '/api',
        request,
        get: (url, config = {}) =>
            request('get', url, {
                headers: config.headers,
                responseType: config.responseType,
                params: config.params,
                signal: config.signal,
                timeout: config.timeout,
            }),
        post: (url, data, config = {}) =>
            request('post', url, {
                data,
                headers: config.headers,
                responseType: config.responseType,
                params: config.params,
            }),
        put: (url, data, config = {}) =>
            request('put', url, {
                data,
                headers: config.headers,
                responseType: config.responseType,
                params: config.params,
            }),
        delete: (url, config = {}) =>
            request('delete', url, {
                headers: config.headers,
                responseType: config.responseType,
                params: config.params,
            }),
    };
})();
