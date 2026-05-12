const TELECOM_ROLE_NAMES = new Set(['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice', 'TelecomCallCenter', 'TelecomManagement']);

const App = {
    setup() {
        const hubPermissions = Vue.computed(() => {
            const roles = StorageManager.getUserRoles() || [];
            return {
                canCreateOps: roles.some((x) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(x)),
                canConfirmCbs: roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice'].includes(x)),
                canSeeBillingLog: roles.some((x) => ['TelecomAdmin', 'TelecomBackOffice', 'TelecomManagement'].includes(x)),
                showRetailShortcuts: roles.some((x) => !TELECOM_ROLE_NAMES.has(x)),
            };
        });

        const state = Vue.reactive({
            kpis: { arpuDemo: 0, churnPercentDemo: 0, branchHeat: [] },
            operations: [],
            billingLogs: [],
            searchResults: [],
            searchTerm: '',
            searchBusy: false,
            hasSearched: false,
            loading: true,
            loadError: null,
            wizard: {
                visible: false,
                kind: '',
                step: 1,
                primarySearchTerm: '',
                primaryResults: [],
                primaryBusy: false,
                primarySubscriberProfileId: '',
                primaryMsisdnAssetId: '',
                primaryLabel: '',
                primaryRowKey: '',
                secondarySearchTerm: '',
                secondaryResults: [],
                secondaryBusy: false,
                secondarySubscriberProfileId: '',
                secondaryLabel: '',
                notes: '',
                createdOperationId: '',
                createdOperationNumber: '',
                documentMarkedUploaded: false,
                submitBusy: false,
                uploadBusy: false,
                targetOfferName: '',
            },
        });

        const kindToApiEnum = () => {
            const k = state.wizard.kind;
            if (k === 'activate') return 0;
            if (k === 'migrate') return 1;
            if (k === 'takeover') return 2;
            if (k === 'simswap') return 3;
            if (k === 'addpackage' || k === 'support') return 4;
            return 0;
        };

        const wizardTitle = Vue.computed(() => {
            const m = {
                activate: 'تفعيل خط جديد (NewActivation)',
                migrate: 'تحويل MGR (Migration)',
                takeover: 'نقل ملكية TKO (TakeOver)',
                support: 'دعم / تعديل خدمة (ServiceModification)',
                simswap: 'تبديل شريحة SIM Swap',
                addpackage: 'إضافة باقة / خدمة (ServiceModification)',
            };
            return m[state.wizard.kind] ?? '';
        });

        const needsSecondaryParty = Vue.computed(() => state.wizard.kind === 'takeover' || state.wizard.kind === 'migrate');

        const secondaryRequired = Vue.computed(() => state.wizard.kind === 'takeover');

        const canWizardGoToStep3 = Vue.computed(
            () => !!state.wizard.createdOperationId && state.wizard.documentMarkedUploaded
        );

        const rowKey = (r) => `${r.resultType}:${r.id}`;

        const resetWizardState = () => {
            state.wizard.step = 1;
            state.wizard.primarySearchTerm = '';
            state.wizard.primaryResults = [];
            state.wizard.primaryBusy = false;
            state.wizard.primarySubscriberProfileId = '';
            state.wizard.primaryMsisdnAssetId = '';
            state.wizard.primaryLabel = '';
            state.wizard.primaryRowKey = '';
            state.wizard.secondarySearchTerm = '';
            state.wizard.secondaryResults = [];
            state.wizard.secondaryBusy = false;
            state.wizard.secondarySubscriberProfileId = '';
            state.wizard.secondaryLabel = '';
            state.wizard.notes = '';
            state.wizard.createdOperationId = '';
            state.wizard.createdOperationNumber = '';
            state.wizard.documentMarkedUploaded = false;
            state.wizard.submitBusy = false;
            state.wizard.uploadBusy = false;
            state.wizard.targetOfferName = '';
        };

        const formatMoney = (n) => {
            if (n == null || isNaN(n)) return '—';
            return new Intl.NumberFormat('ar-SY', { maximumFractionDigits: 0 }).format(n);
        };

        const formatDate = (d) => {
            if (!d) return '';
            const dt = new Date(d);
            return isNaN(dt.getTime()) ? '' : dt.toLocaleString('ar-SY');
        };

        const statusBadge = (status) => {
            const s = Number(status);
            if (s === 3) return 'bg-success';
            if (s === 2) return 'bg-warning text-dark';
            if (s === 4) return 'bg-danger';
            return 'bg-secondary';
        };

        const loadKpis = async () => {
            try {
                const res = await AxiosManager.get('/Telecom/GetTelecomDashboardKpis', {});
                const c = res?.data?.content;
                if (!c) return false;
                state.kpis.arpuDemo = c.arpuDemo ?? 0;
                state.kpis.churnPercentDemo = c.churnPercentDemo ?? 0;
                state.kpis.branchHeat = c.branchHeat ?? [];
                return true;
            } catch {
                state.kpis.branchHeat = [];
                return false;
            }
        };

        const loadOperations = async () => {
            try {
                const res = await AxiosManager.get('/Telecom/GetTelecomOperationList', {});
                state.operations = res?.data?.content?.data ?? [];
                return true;
            } catch {
                state.operations = [];
                return false;
            }
        };

        const loadBillingLogs = async () => {
            try {
                const res = await AxiosManager.get('/Telecom/GetBillingIntegrationLogList', {});
                state.billingLogs = res?.data?.content?.data ?? [];
                return true;
            } catch {
                state.billingLogs = [];
                return false;
            }
        };

        const refreshAll = async () => {
            state.loading = true;
            state.loadError = null;
            try {
                const p = hubPermissions.value;
                const tasks = [loadKpis(), loadOperations()];
                if (p.canSeeBillingLog) {
                    tasks.push(loadBillingLogs());
                } else {
                    state.billingLogs = [];
                }
                const results = await Promise.all(tasks);
                const okK = results[0];
                const okO = results[1];
                const okB = p.canSeeBillingLog ? results[2] : true;
                if (!okK || !okO || !okB) {
                    state.loadError = 'تعذر تحميل بعض البيانات. تحقق من الاتصال أو الجلسة ثم أعد المحاولة.';
                }
            } finally {
                state.loading = false;
            }
        };

        const runSearch = async () => {
            const term = (state.searchTerm || '').trim();
            if (term.length < 2) {
                state.searchResults = [];
                state.hasSearched = false;
                return;
            }
            state.searchBusy = true;
            state.hasSearched = true;
            try {
                const q = '/Telecom/GetTelecomUniversalSearch?term=' + encodeURIComponent(term);
                const res = await AxiosManager.get(q, {});
                state.searchResults = res?.data?.content?.data ?? [];
            } catch {
                state.searchResults = [];
                if (window.Swal) {
                    Swal.fire({ icon: 'error', title: 'بحث', text: 'تعذر تنفيذ البحث.', timer: 2000, showConfirmButton: false });
                }
            } finally {
                state.searchBusy = false;
            }
        };

        const wizardSearch = async (term, which) => {
            const t = (term || '').trim();
            if (t.length < 2) {
                if (which === 'primary') state.wizard.primaryResults = [];
                else state.wizard.secondaryResults = [];
                return;
            }
            if (which === 'primary') state.wizard.primaryBusy = true;
            else state.wizard.secondaryBusy = true;
            try {
                const q = '/Telecom/GetTelecomUniversalSearch?term=' + encodeURIComponent(t);
                const res = await AxiosManager.get(q, {});
                const rows = res?.data?.content?.data ?? [];
                if (which === 'primary') state.wizard.primaryResults = rows;
                else state.wizard.secondaryResults = rows;
            } catch {
                if (which === 'primary') state.wizard.primaryResults = [];
                else state.wizard.secondaryResults = [];
                if (window.Swal) {
                    Swal.fire({ icon: 'error', title: 'بحث', text: 'تعذر تنفيذ البحث.', timer: 1800, showConfirmButton: false });
                }
            } finally {
                if (which === 'primary') state.wizard.primaryBusy = false;
                else state.wizard.secondaryBusy = false;
            }
        };

        const runWizardPrimarySearch = () => wizardSearch(state.wizard.primarySearchTerm, 'primary');
        const runWizardSecondarySearch = () => wizardSearch(state.wizard.secondarySearchTerm, 'secondary');

        const profileIdFromRow = (r) => {
            if (!r) return '';
            if (r.subscriberProfileId) return r.subscriberProfileId;
            if (r.resultType === 'SubscriberProfile' && r.id) return r.id;
            return '';
        };

        const selectWizardPrimary = (r) => {
            const pid = profileIdFromRow(r);
            if (!pid) {
                if (window.Swal) {
                    Swal.fire({
                        icon: 'warning',
                        title: 'لا يوجد ملف مشترك',
                        text: 'هذا السجل غير مرتبط بـ SubscriberProfile. اختر مشتركاً من نوع SubscriberProfile أو اربط الخط بالملف.',
                    });
                }
                return;
            }
            state.wizard.primarySubscriberProfileId = pid;
            state.wizard.primaryMsisdnAssetId = r.resultType === 'Msisdn' ? r.id : '';
            state.wizard.primaryLabel = `${r.title || '—'} (${r.resultType})`;
            state.wizard.primaryRowKey = rowKey(r);
        };

        const clearWizardPrimary = () => {
            state.wizard.primarySubscriberProfileId = '';
            state.wizard.primaryMsisdnAssetId = '';
            state.wizard.primaryLabel = '';
            state.wizard.primaryRowKey = '';
        };

        const selectWizardSecondary = (r) => {
            const pid = profileIdFromRow(r);
            if (!pid) {
                if (window.Swal) {
                    Swal.fire({
                        icon: 'warning',
                        title: 'لا يوجد ملف مشترك',
                        text: 'اختر سجلاً مرتبطاً بملف مشترك.',
                    });
                }
                return;
            }
            if (pid === state.wizard.primarySubscriberProfileId) {
                if (window.Swal) {
                    Swal.fire({ icon: 'info', title: 'نفس المشترك', text: 'اختر طرفاً ثانياً مختلفاً.' });
                }
                return;
            }
            state.wizard.secondarySubscriberProfileId = pid;
            state.wizard.secondaryLabel = `${r.title || '—'} (${r.resultType})`;
        };

        const clearWizardSecondary = () => {
            state.wizard.secondarySubscriberProfileId = '';
            state.wizard.secondaryLabel = '';
        };

        const openWizard = (kind) => {
            resetWizardState();
            state.wizard.visible = true;
            state.wizard.kind = kind;
        };

        const closeWizard = () => {
            state.wizard.visible = false;
            resetWizardState();
        };

        const finishWizard = async () => {
            state.wizard.visible = false;
            resetWizardState();
            await refreshAll();
        };

        const wizardStepNext = () => {
            if (state.wizard.step === 1) {
                if (!state.wizard.primarySubscriberProfileId) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: 'بيانات ناقصة', text: 'اختر المشترك الأساسي من نتائج البحث.' });
                    return;
                }
                if (secondaryRequired.value && !state.wizard.secondarySubscriberProfileId) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'warning', title: 'بيانات ناقصة', text: 'نقل الملكية يتطلب اختيار طرف ثانٍ (مشترك آخر).' });
                    }
                    return;
                }
                state.wizard.step = 2;
                return;
            }
            if (state.wizard.step === 2) {
                if (!canWizardGoToStep3.value) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'info',
                            title: 'أكمل الخطوات',
                            text: 'أنشئ المسودة ثم سجّل رفع الوثيقة قبل الانتقال للخلاصة.',
                        });
                    }
                    return;
                }
                state.wizard.step = 3;
            }
        };

        const wizardStepPrev = () => {
            if (state.wizard.step > 1) state.wizard.step -= 1;
        };

        const submitCreateOperation = async () => {
            state.wizard.submitBusy = true;
            try {
                const body = {
                    kind: kindToApiEnum(),
                    subscriberProfileId: state.wizard.primarySubscriberProfileId,
                    secondarySubscriberProfileId: state.wizard.secondarySubscriberProfileId || null,
                    msisdnAssetId: state.wizard.primaryMsisdnAssetId || null,
                    productId: null,
                    notes: (state.wizard.notes || '').trim() || null,
                    targetOfferName:
                        state.wizard.kind === 'migrate' ? (state.wizard.targetOfferName || '').trim() || null : null,
                    createdById: StorageManager.getUserId(),
                };
                const res = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                const ok = res?.data?.code === 200;
                const entity = res?.data?.content?.data;
                if (ok && entity?.id) {
                    state.wizard.createdOperationId = entity.id;
                    state.wizard.createdOperationNumber = entity.number || '';
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: 'تم إنشاء المسودة',
                            text: state.wizard.createdOperationNumber,
                            timer: 1600,
                            showConfirmButton: false,
                        });
                    }
                } else if (window.Swal) {
                    Swal.fire({ icon: 'error', title: 'رفض الخادم', text: res?.data?.message || 'تحقق من الحقول والتحقق من الطرف الثاني لنقل الملكية.' });
                }
            } catch (e) {
                const msg = e.response?.data?.message || e.response?.data?.errors?.[0] || e.message || 'خطأ غير متوقع';
                if (window.Swal) Swal.fire({ icon: 'error', title: 'فشل الإنشاء', text: String(msg) });
            } finally {
                state.wizard.submitBusy = false;
            }
        };

        const submitMarkDocumentUploaded = async () => {
            if (!state.wizard.createdOperationId) return;
            state.wizard.uploadBusy = true;
            try {
                const res = await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', {
                    id: state.wizard.createdOperationId,
                    updatedById: StorageManager.getUserId(),
                });
                if (res?.data?.code === 200) {
                    state.wizard.documentMarkedUploaded = true;
                    if (window.Swal) {
                        Swal.fire({ icon: 'success', title: 'تم تسجيل الوثيقة', timer: 1200, showConfirmButton: false });
                    }
                } else if (window.Swal) {
                    Swal.fire({ icon: 'error', title: 'فشل', text: res?.data?.message ?? '' });
                }
            } catch (e) {
                if (window.Swal) {
                    Swal.fire({ icon: 'error', title: 'خطأ', text: e.response?.data?.message ?? '' });
                }
            } finally {
                state.wizard.uploadBusy = false;
            }
        };

        const confirmFirstReadyDraft = async () => {
            if (!hubPermissions.value.canConfirmCbs) {
                if (window.Swal) {
                    Swal.fire({
                        icon: 'info',
                        title: 'غير مصرّح',
                        text: 'تأكيد CBS متاح لمكتب الخلفية أو المشرف فقط.',
                    });
                }
                return;
            }
            const ready = state.operations.find(
                (o) => Number(o.status) === 0 && Number(o.documentStatus) >= 1
            );
            if (!ready?.id) {
                if (window.Swal) {
                    Swal.fire({ icon: 'info', title: 'لا توجد مسودة جاهزة', text: 'يجب أن تكون الحالة Draft والوثائق على الأقل Uploaded.' });
                }
                return;
            }
            try {
                const res = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                    id: ready.id,
                    updatedById: StorageManager.getUserId(),
                });
                if (res?.data?.code === 200) {
                    if (window.Swal) Swal.fire({ icon: 'success', title: 'تم التأكيد', timer: 1400, showConfirmButton: false });
                    await refreshAll();
                } else if (window.Swal) {
                    Swal.fire({ icon: 'error', title: 'فشل', text: res?.data?.message ?? '' });
                }
            } catch (e) {
                if (window.Swal) Swal.fire({ icon: 'error', title: 'خطأ', text: e.response?.data?.message ?? 'تأكد من الجلسة.' });
            }
        };

        Vue.onMounted(async () => {
            try {
                await refreshAll();
            } finally {
                if (typeof hideSpinnerAndShowContent === 'function') {
                    hideSpinnerAndShowContent();
                }
                if (typeof setFormCardHeight === 'function') {
                    setFormCardHeight();
                }
            }
        });

        return {
            ...Vue.toRefs(state),
            hubPermissions,
            wizardTitle,
            needsSecondaryParty,
            secondaryRequired,
            canWizardGoToStep3,
            rowKey,
            formatMoney,
            formatDate,
            statusBadge,
            refreshAll,
            runSearch,
            openWizard,
            closeWizard,
            wizardStepNext,
            wizardStepPrev,
            runWizardPrimarySearch,
            runWizardSecondarySearch,
            selectWizardPrimary,
            clearWizardPrimary,
            selectWizardSecondary,
            clearWizardSecondary,
            submitCreateOperation,
            submitMarkDocumentUploaded,
            finishWizard,
            confirmFirstReadyDraft,
        };
    },
};

Vue.createApp(App).mount('#app');
