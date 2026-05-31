const LOGIN_URL = '/Accounts/Login';

const App = {
    setup() {
        const state = Vue.reactive({
            isSubmitting: false,
        });

        const resolveUserId = () =>
            StorageManager.getUserId() || StorageManager.parseUserIdFromAccessToken();

        const logout = async () => {
            const userId = resolveUserId();
            const body = userId ? { userId, UserId: userId } : {};
            return AxiosManager.post('/Security/Logout', body);
        };

        const handleSubmit = async () => {

            try {
                state.isSubmitting = true;
                await new Promise(resolve => setTimeout(resolve, 300));

                const response = await logout();
                if (response?.data?.code === 200) {
                    StorageManager.clearStorage();
                    Swal.fire({
                        icon: 'success',
                        title: 'Logout Successful',
                        text: 'You are being redirected...',
                        timer: 2000,
                        showConfirmButton: false
                    });
                    setTimeout(() => {
                        window.location.href = LOGIN_URL;
                    }, 2000);
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Logout Failed',
                        text: response.data.message ?? 'Logout Failed.',
                        confirmButtonText: 'Try Again'
                    });
                }

            } catch (error) {
                // Still clear local session if server logout failed (expired token, etc.)
                StorageManager.clearStorage();
                Swal.fire({
                    icon: 'warning',
                    title: 'تم تسجيل الخروج محلياً',
                    text: error.response?.data?.message || 'تم مسح الجلسة من المتصفح.',
                    timer: 2000,
                    showConfirmButton: false,
                });
                setTimeout(() => {
                    window.location.href = LOGIN_URL;
                }, 2000);
            } finally {
                state.isSubmitting = false;
            }
        };

        return {
            state,
            handleSubmit
        };
    }
};

Vue.createApp(App).mount('#app');

