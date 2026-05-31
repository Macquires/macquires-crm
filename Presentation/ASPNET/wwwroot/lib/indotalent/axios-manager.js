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
                    // Global intercept for Business Logic
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'warning',
                            title: 'تنبيه',
                            text: errData.message || 'لا يمكن إتمام العملية بسبب قواعد العمل.',
                            confirmButtonColor: '#c8102e'
                        });
                    }
                } else if (status === 429 || status === 500) {
                    // Chaos Engineering / Mock Server Rate Limits & Exceptions
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'error',
                            title: 'خطأ في الشبكة',
                            text: 'الشبكة مشغولة حالياً أو يوجد مشكلة في المخدم الخارجي، يرجى المحاولة بعد لحظات.',
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
                    // General errors without showing StackTrace (Sanitized)
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'error',
                            title: 'حدث خطأ',
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
        const { data, headers = {}, responseType = 'json', params } = options;
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
            });
            return response;
        } catch (error) {
            throw error;
        }
    };

    return {
        request,
        get: (url, config = {}) =>
            request('get', url, {
                headers: config.headers,
                responseType: config.responseType,
                params: config.params,
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
