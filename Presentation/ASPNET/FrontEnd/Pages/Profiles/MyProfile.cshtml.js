const TELECOM_ROLES = ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice', 'TelecomCallCenter', 'TelecomManagement'];

const App = {
    setup() {
        const state = Vue.reactive({
            firstName: '',
            lastName: '',
            companyName: '',
            email: '',
            oldPassword: '',
            newPassword: '',
            confirmPassword: '',
            avatarPreview: '/noimage.png',
            avatarFile: null,
            isSavingProfile: false,
            isSavingPassword: false,
            isUploadingAvatar: false,
        });

        const services = {
            loadProfile: () => AxiosManager.get('/Security/GetMyProfileList', {}),
            saveProfile: (body) => AxiosManager.post('/Security/UpdateMyProfile', body),
            changePassword: (body) => AxiosManager.post('/Security/UpdateMyProfilePassword', body),
            uploadImage: (formData) =>
                AxiosManager.post('/FileImage/UploadImage', formData, {
                    headers: { 'Content-Type': 'multipart/form-data' },
                }),
            saveAvatar: (body) => AxiosManager.post('/Security/UpdateMyProfileAvatar', body),
        };

        const handler = {
            loadAvatarPreview: async () => {
                const avatar = StorageManager.getAvatar();
                if (!avatar) {
                    return;
                }
                try {
                    const response = await AxiosManager.get('/FileImage/GetImage?imageName=' + avatar, {
                        responseType: 'blob',
                    });
                    if (response.status === 200) {
                        const reader = new FileReader();
                        reader.onloadend = () => {
                            state.avatarPreview = reader.result;
                        };
                        reader.readAsDataURL(response.data);
                    }
                } catch {
                    /* ignore */
                }
            },
            onAvatarSelected: (e) => {
                const file = e.target.files?.[0];
                state.avatarFile = file || null;
                if (file) {
                    state.avatarPreview = URL.createObjectURL(file);
                }
            },
            uploadAvatar: async () => {
                if (!state.avatarFile) {
                    Swal.fire({ icon: 'warning', title: 'Choose an image file first.' });
                    return;
                }
                state.isUploadingAvatar = true;
                try {
                    const formData = new FormData();
                    formData.append('file', state.avatarFile);
                    const uploadRes = await services.uploadImage(formData);
                    const imageName = uploadRes?.data?.content?.imageName ?? uploadRes?.data?.content?.ImageName;
                    if (!imageName) {
                        throw new Error('Upload failed');
                    }
                    await services.saveAvatar({
                        avatar: imageName,
                    });
                    StorageManager.saveAvatar(imageName);
                    await handler.loadAvatarPreview();
                    Swal.fire({ icon: 'success', title: 'Avatar updated', timer: 1800, showConfirmButton: false });
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Upload failed',
                        text: e.response?.data?.message || e.message,
                    });
                } finally {
                    state.isUploadingAvatar = false;
                }
            },
            saveProfile: async () => {
                state.isSavingProfile = true;
                try {
                    const res = await services.saveProfile({
                        firstName: state.firstName,
                        lastName: state.lastName,
                        companyName: state.companyName || '',
                    });
                    if (res?.data?.code === 200) {
                        StorageManager.saveFirstName(state.firstName);
                        StorageManager.saveLastName(state.lastName);
                        Swal.fire({ icon: 'success', title: 'Profile saved', timer: 1800, showConfirmButton: false });
                    }
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Save failed',
                        text: e.response?.data?.message || e.message,
                    });
                } finally {
                    state.isSavingProfile = false;
                }
            },
            changePassword: async () => {
                if (!state.oldPassword || !state.newPassword) {
                    Swal.fire({ icon: 'warning', title: 'Fill all password fields.' });
                    return;
                }
                if (state.newPassword !== state.confirmPassword) {
                    Swal.fire({ icon: 'warning', title: 'Passwords do not match.' });
                    return;
                }
                state.isSavingPassword = true;
                try {
                    const res = await services.changePassword({
                        oldPassword: state.oldPassword,
                        newPassword: state.newPassword,
                        confirmNewPassword: state.confirmPassword,
                    });
                    if (res?.data?.code === 200) {
                        state.oldPassword = '';
                        state.newPassword = '';
                        state.confirmPassword = '';
                        Swal.fire({ icon: 'success', title: 'Password updated', timer: 1800, showConfirmButton: false });
                    }
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Password change failed',
                        text: e.response?.data?.message || e.message,
                    });
                } finally {
                    state.isSavingPassword = false;
                }
            },
        };

        Vue.onMounted(async () => {
            try {
                await SecurityManager.authorizePage(TELECOM_ROLES);
                await SecurityManager.validateToken();
                state.email = StorageManager.getEmail() || '';
                const userId = StorageManager.getUserId();
                if (!userId) {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Session expired',
                        text: 'Please sign in again.',
                    }).then(() => {
                        window.location.href = '/Accounts/Login';
                    });
                    return;
                }
                const res = await services.loadProfile();
                const row = res?.data?.content?.data?.[0];
                if (row) {
                    state.firstName = row.firstName ?? row.FirstName ?? '';
                    state.lastName = row.lastName ?? row.LastName ?? '';
                    state.companyName = row.companyName ?? row.CompanyName ?? '';
                }
                await handler.loadAvatarPreview();
            } catch (e) {
                console.error('MyProfile init:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        return { state, handler };
    },
};

Vue.createApp(App).mount('#app');
