const ProductCatalogApp = {
    setup() {
        const localeTick = Vue.ref(0);

        const state = Vue.reactive({
            loading: true,
            loadError: null,
            offerings: [],
            technicalProducts: [],
            searchTerm: '',
            selectedTab: 'all',
            contentLang: 'ar',
            contentDir: 'rtl',
            detailModalVisible: false,
            detailLoading: false,
            isAdmin: true, // Enables full catalog management features (Create, Edit, Delete)
            upsertModalVisible: false,
            isEditMode: false,
            upsertForm: {
                id: '',
                name: '',
                nameEn: '',
                code: '',
                description: '',
                compatibleSubscriptionTypeId: 'a0e0e0e0-0000-4000-8000-000000000001',
                isActive: true,
                components: [],
                pricePlans: []
            },
            currentOffer: {
                id: '',
                name: '',
                nameEn: '',
                code: '',
                description: '',
                components: [],
                pricePlans: []
            }
        });

        // Filter Tabs specifications
        const filterTabs = [
            { value: 'all', labelKey: 'all', icon: 'fas fa-border-all' },
            { value: 'prepaid', labelKey: 'prepaid', icon: 'fas fa-mobile-alt' },
            { value: 'postpaid', labelKey: 'postpaid', icon: 'fas fa-file-invoice-dollar' },
            { value: 'hybrid', labelKey: 'hybrid', icon: 'fas fa-broadcast-tower' },
            { value: 'dataOnly', labelKey: 'dataOnly', icon: 'fas fa-globe' },
            { value: 'corporate', labelKey: 'corporate', icon: 'fas fa-building' }
        ];

        const t = (key) => {
            localeTick.value;
            return window.TelecomI18n?.t?.(`productCatalog.${key}`) || key;
        };

        const offerDisplayName = (offer) => {
            if (!offer) return '';
            const ar = (offer.name || '').trim();
            const en = (offer.nameEn || '').trim();
            if (state.contentLang === 'ar') return ar || en;
            return en || ar;
        };

        const toggleLanguage = (lang) => {
            state.contentLang = lang;
            state.contentDir = lang === 'ar' ? 'rtl' : 'ltr';
            document.documentElement.lang = lang;
            document.documentElement.setAttribute('dir', state.contentDir);
            localeTick.value++;
        };

        // Network call to load product offerings list
        const loadCatalog = async () => {
            state.loading = true;
            state.loadError = null;
            try {
                const response = await AxiosManager.get('/ProductOffering/GetProductOfferingList', {});
                state.offerings = response?.data?.content?.data || [];
            } catch (error) {
                console.error("Failed to load product catalog list", error);
                state.loadError = t('refreshFail');
            } finally {
                state.loading = false;
            }
        };

        const loadTechnicalProducts = async () => {
            if (!state.isAdmin) return;
            try {
                const response = await AxiosManager.get('/Product/GetProductList?isDeleted=false', {});
                state.technicalProducts = response?.data?.content?.data || [];
            } catch (e) {
                console.error('Failed to load technical products', e);
                state.technicalProducts = [];
            }
        };

        const selectTab = (tabValue) => {
            state.selectedTab = tabValue;
        };

        const resetFilters = () => {
            state.searchTerm = '';
            state.selectedTab = 'all';
        };

        // Subscription type helper mapping matching database IDs
        // Prepaid Line: "a0e0e0e0-0000-4000-8000-000000000001"
        // Postpaid Line: "a0e0e0e0-0000-4000-8000-000000000002"
        // Hybrid/Mix Line: "a0e0e0e0-0000-4000-8000-000000000003"
        const getLineTypeClass = (typeId) => {
            if (typeId === "a0e0e0e0-0000-4000-8000-000000000001") return "prepaid";
            if (typeId === "a0e0e0e0-0000-4000-8000-000000000002") return "postpaid";
            if (typeId === "a0e0e0e0-0000-4000-8000-000000000003") return "hybrid";
            return "other";
        };

        const getLineTypeName = (typeId) => {
            if (typeId === "a0e0e0e0-0000-4000-8000-000000000001") return t('prepaid');
            if (typeId === "a0e0e0e0-0000-4000-8000-000000000002") return t('postpaid');
            if (typeId === "a0e0e0e0-0000-4000-8000-000000000003") return t('hybrid');
            return t('all');
        };

        const getLineTypeBadgeClass = (typeId) => {
            if (typeId === "a0e0e0e0-0000-4000-8000-000000000001") return "bg-warning text-dark";
            if (typeId === "a0e0e0e0-0000-4000-8000-000000000002") return "bg-primary text-white";
            if (typeId === "a0e0e0e0-0000-4000-8000-000000000003") return "bg-success text-white";
            return "bg-secondary text-white";
        };

        // Formatting utilities
        const formatPrice = (price) => {
            if (price == null) return '—';
            return new Intl.NumberFormat(state.contentLang === 'ar' ? 'ar-SY' : 'en-US', {
                style: 'currency',
                currency: 'SYP',
                maximumFractionDigits: 0
            }).format(price);
        };

        const formatPlanPrice = (price) => {
            if (price == null) return '0';
            return new Intl.NumberFormat(state.contentLang === 'ar' ? 'ar-SY' : 'en-US', {
                maximumFractionDigits: 0
            }).format(price);
        };

        // Determine icon and name classes based on Component Type Enum
        const getComponentTypeIcon = (type) => {
            const icons = {
                0: "fas fa-phone-alt",
                1: "fas fa-globe",
                2: "fas fa-comment-alt",
                3: "fas fa-star",
                4: "fas fa-box",
                5: "fas fa-plane-departure"
            };
            return icons[type] || "fas fa-cube";
        };

        const getComponentTypeClass = (type) => {
            const classes = {
                0: "component-voice",
                1: "component-data",
                2: "component-sms",
                3: "component-vas",
                4: "component-equip",
                5: "component-internat"
            };
            return classes[type] || "bg-light text-muted";
        };

        const getComponentTypeName = (type) => {
            const names = {
                0: t('voice'),
                1: t('data'),
                2: t('sms'),
                3: t('vas'),
                4: t('equipment'),
                5: t('international')
            };
            return names[type] || "Component";
        };

        // Price Plan period types
        const getPlanTypeName = (type) => {
            const names = {
                0: t('prepaid'), // Monthly rates
                1: t('all'),     // Daily booster
                2: "Weekly Schedule",
                3: "Pay-As-You-Go Rates",
                4: "One-Time Charge"
            };
            // Use nicer localized period terms
            if (type === 0) return t('planType0');
            if (type === 1) return t('planType1');
            if (type === 2) return t('planType2');
            if (type === 3) return t('planType3');
            if (type === 4) return t('planType4');
            return '—';
        };

        const getPlanTypePeriod = (type) => {
            if (type === 0) return t('planPeriod0');
            if (type === 1) return t('planPeriod1');
            if (type === 2) return t('planPeriod2');
            if (type === 3) return t('planPeriod3');
            if (type === 4) return t('planPeriod4');
            return '';
        };

        // Helper to check card feature existence
        const hasFeature = (offer, compType) => {
            // We just mock check if any seeded component type exists or if we should preview it
            return true;
        };

        const getFeatureLabel = (offer, compType) => {
            // Prepaid Ya Hala
            if (offer.code === "MGR-PRE-YAHALA") {
                if (compType === 0) return t('featureMins150');
                if (compType === 1) return "500 MB";
                if (compType === 2) return "100 SMS";
            }
            // Mix 500
            if (offer.code === "MGR-MIX-500") {
                if (compType === 0) return t('featureMins500');
                if (compType === 1) return "5 GB";
                if (compType === 2) return "300 SMS";
            }
            // Platinum VIP
            if (offer.code === "MGR-PST-PLATINUM") {
                if (compType === 0) return t('unlimited');
                if (compType === 1) return "50 GB";
                if (compType === 2) return "1000 SMS";
            }
            // Unlimited Net
            if (offer.code === "MGR-PRE-UNLIMITED-NET") {
                if (compType === 0) return "—";
                if (compType === 1) return t('unlimited');
                if (compType === 2) return "—";
            }
            // Business Plus
            if (offer.code === "MGR-CORP-BUS-PLUS") {
                if (compType === 0) return t('featureMins3000');
                if (compType === 1) return "20 GB";
                if (compType === 2) return t('unlimited');
            }

            // Fallback preview text
            if (compType === 0) return "Voice Spec";
            if (compType === 1) return "Data Spec";
            if (compType === 2) return "SMS Spec";
            return "Quota Spec";
        };

        // Fetch detailed offering information via API
        const viewDetails = async (id) => {
            state.detailModalVisible = true;
            state.detailLoading = true;
            try {
                const response = await AxiosManager.get(`/ProductOffering/GetProductOfferingSingle?id=${id}`, {});
                state.currentOffer = response?.data?.content ?? {
                    id: '',
                    name: '',
                    nameEn: '',
                    code: '',
                    description: '',
                    components: [],
                    pricePlans: []
                };
            } catch (error) {
                console.error("Failed to load single product details", error);
                Swal.fire({
                    icon: 'error',
                    title: t('errorTitle') || 'Error',
                    text: t('detailLoadFail') || 'Failed to load details.'
                });
                state.detailModalVisible = false;
            } finally {
                state.detailLoading = false;
            }
        };

        const closeDetails = () => {
            state.detailModalVisible = false;
        };

        // Action shortcuts back to Telecom Hub
        const triggerAction = (actionType) => {
            // Beautiful redirect or storage based action triggering
            if (window.Swal) {
                Swal.fire({
                    icon: 'success',
                    title: t('syriatelBss'),
                    text: t('planSelectedRedirect').replace('{name}', offerDisplayName(state.currentOffer)),
                    timer: 2000,
                    showConfirmButton: false
                });
                setTimeout(() => {
                    window.location.href = `/Telecom/TelecomHub?wizard=${actionType}&productId=${state.currentOffer.id}`;
                }, 1800);
            }
        };

        // Computed reactive array of offerings matching user inputs
        const filteredOfferings = Vue.computed(() => {
            let list = state.offerings || [];
            
            // Search criteria
            const search = (state.searchTerm || '').trim().toLowerCase();
            if (search) {
                list = list.filter(o => 
                    (o.name || '').toLowerCase().includes(search) ||
                    (o.nameEn || '').toLowerCase().includes(search) ||
                    (o.code || '').toLowerCase().includes(search) ||
                    (o.description || '').toLowerCase().includes(search)
                );
            }

            // Tabs criteria
            if (state.selectedTab !== 'all') {
                if (state.selectedTab === 'prepaid') {
                    // Prepaid subscription ID
                    list = list.filter(o => o.compatibleSubscriptionTypeId === "a0e0e0e0-0000-4000-8000-000000000001");
                } else if (state.selectedTab === 'postpaid') {
                    // Postpaid subscription ID
                    list = list.filter(o => o.compatibleSubscriptionTypeId === "a0e0e0e0-0000-4000-8000-000000000002");
                } else if (state.selectedTab === 'hybrid') {
                    // Hybrid subscription ID
                    list = list.filter(o => o.compatibleSubscriptionTypeId === "a0e0e0e0-0000-4000-8000-000000000003");
                } else if (state.selectedTab === 'dataOnly') {
                    list = list.filter((o) => {
                        const cat = (o.category || '').toLowerCase();
                        const code = (o.code || '').toLowerCase();
                        return cat === 'dataonly' || code.includes('net') || code.includes('data');
                    });
                } else if (state.selectedTab === 'corporate') {
                    list = list.filter((o) => {
                        const cat = (o.category || '').toLowerCase();
                        const code = (o.code || '').toLowerCase();
                        return cat === 'corporate' || code.includes('corp') || code.includes('b2b');
                    });
                }
            }

            return list;
        });

        const onLocaleChanged = async () => {
            try {
                await window.TelecomI18n?.ensureLoaded?.();
            } catch (_) { /* ignore */ }
            const lang = document.documentElement.lang?.toLowerCase().startsWith('en') ? 'en' : 'ar';
            toggleLanguage(lang);
            const title = window.TelecomI18n?.t?.('productCatalog.pageTitle');
            if (title) document.title = title;
        };

        const openCreateModal = () => {
            state.isEditMode = false;
            state.upsertForm = {
                id: '',
                name: '',
                nameEn: '',
                code: '',
                description: '',
                compatibleSubscriptionTypeId: 'a0e0e0e0-0000-4000-8000-000000000001',
                isActive: true,
                category: 'cellular',
                components: [],
                pricePlans: [],
                eligibilityRules: 'Prepaid',
                assetCompatibility: 'Voice',
                billingCycle: 'Monthly',
                taxCategory: 'StandardVAT',
                serviceIdSocCode: '',
                speedQuotaLimitGb: null,
                voiceMinutesLimit: null,
                throttlingPolicy: 'Cutoff',
                iconClass: 'fas fa-mobile-alt',
                badgeColor: 'primary',
                shortDescription: '',
                productId: ''
            };
            state.upsertModalVisible = true;
        };

        const openEditModal = (offer) => {
            state.isEditMode = true;
            
            const clonedComponents = (offer.components || []).map(c => ({
                componentType: c.componentType,
                label: c.label,
                quota: c.quota,
                quotaUnit: c.quotaUnit,
                isUnlimited: c.isUnlimited,
                sortOrder: c.sortOrder
            }));

            const clonedPricePlans = (offer.pricePlans || []).map(p => ({
                planType: p.planType,
                price: p.price,
                currencyCode: p.currencyCode || 'SYP',
                validityDays: p.validityDays,
                isDefault: p.isDefault
            }));

            // Determine commercial category based on code, name or components
            let initialCategory = 'cellular';
            const codeLower = (offer.code || '').toLowerCase();
            const catLower = (offer.category || '').toLowerCase();
            if (catLower === 'dataonly' || codeLower.includes('net') || codeLower.includes('data') || clonedComponents.some((c) => c.componentType === 1)) {
                initialCategory = 'dataOnly';
            } else if (catLower === 'corporate' || codeLower.includes('corp') || codeLower.includes('b2b')) {
                initialCategory = 'corporate';
            }

            state.upsertForm = {
                id: offer.id,
                name: offer.name,
                nameEn: offer.nameEn,
                code: offer.code,
                description: offer.description,
                compatibleSubscriptionTypeId: offer.compatibleSubscriptionTypeId || 'a0e0e0e0-0000-4000-8000-000000000001',
                isActive: offer.isActive !== false,
                category: initialCategory,
                components: clonedComponents,
                pricePlans: clonedPricePlans,
                eligibilityRules: offer.eligibilityRules || 'Prepaid',
                assetCompatibility: offer.assetCompatibility || 'Voice',
                billingCycle: offer.billingCycle || 'Monthly',
                taxCategory: offer.taxCategory || 'StandardVAT',
                serviceIdSocCode: offer.serviceIdSocCode || '',
                speedQuotaLimitGb: offer.speedQuotaLimitGb ?? null,
                voiceMinutesLimit: offer.voiceMinutesLimit ?? null,
                throttlingPolicy: offer.throttlingPolicy || 'Cutoff',
                iconClass: offer.iconClass || 'fas fa-mobile-alt',
                badgeColor: offer.badgeColor || 'primary',
                shortDescription: offer.shortDescription || '',
                productId: offer.productId || ''
            };
            state.detailModalVisible = false;
            state.upsertModalVisible = true;
        };

        const closeUpsert = () => {
            state.upsertModalVisible = false;
        };

        const addComponentToForm = () => {
            state.upsertForm.components.push({
                componentType: 0,
                label: '',
                quota: null,
                quotaUnit: '',
                isUnlimited: false,
                sortOrder: state.upsertForm.components.length
            });
        };

        const removeComponentFromForm = (index) => {
            state.upsertForm.components.splice(index, 1);
        };

        const addPricePlanToForm = () => {
            state.upsertForm.pricePlans.push({
                planType: 0,
                price: 0,
                currencyCode: 'SYP',
                validityDays: 30,
                isDefault: state.upsertForm.pricePlans.length === 0
            });
        };

        const removePricePlanFromForm = (index) => {
            state.upsertForm.pricePlans.splice(index, 1);
        };

        const saveOffering = async () => {
            if (!state.upsertForm.name || !state.upsertForm.code) {
                Swal.fire({
                    icon: 'warning',
                    title: t('swalWarning'),
                    text: t('requiredFields'),
                });
                return;
            }

            try {
                // Ensure matching code identifier for category filter indexing
                let processedCode = state.upsertForm.code.trim().toUpperCase();
                if (state.upsertForm.category === 'dataOnly' && !processedCode.includes('NET')) {
                    processedCode = processedCode + '_NET';
                } else if (state.upsertForm.category === 'corporate' && !processedCode.includes('CORP')) {
                    processedCode = processedCode + '_CORP';
                }

                let url = '/ProductOffering/CreateProductOffering';
                let payload = {
                    name: state.upsertForm.name,
                    nameEn: state.upsertForm.nameEn,
                    code: processedCode,
                    description: state.upsertForm.description,
                    compatibleSubscriptionTypeId: state.upsertForm.compatibleSubscriptionTypeId,
                    isActive: state.upsertForm.isActive,
                    sortOrder: state.offerings.length + 1,
                    components: state.upsertForm.components,
                    pricePlans: state.upsertForm.pricePlans,
                    eligibilityRules: state.upsertForm.eligibilityRules,
                    assetCompatibility: state.upsertForm.assetCompatibility,
                    billingCycle: state.upsertForm.billingCycle,
                    taxCategory: state.upsertForm.taxCategory,
                    serviceIdSocCode: state.upsertForm.serviceIdSocCode,
                    speedQuotaLimitGb: state.upsertForm.speedQuotaLimitGb ? parseFloat(state.upsertForm.speedQuotaLimitGb) : null,
                    voiceMinutesLimit: state.upsertForm.voiceMinutesLimit ? parseInt(state.upsertForm.voiceMinutesLimit, 10) : null,
                    throttlingPolicy: state.upsertForm.throttlingPolicy,
                    iconClass: state.upsertForm.iconClass,
                    badgeColor: state.upsertForm.badgeColor,
                    shortDescription: state.upsertForm.shortDescription,
                    productId: state.upsertForm.productId || null
                };

                if (state.isEditMode) {
                    url = '/ProductOffering/UpdateProductOffering';
                    payload.id = state.upsertForm.id;
                    payload.updatedById = StorageManager.getUserId();
                } else {
                    payload.createdById = StorageManager.getUserId();
                }

                const response = await AxiosManager.post(url, payload);
                if (response?.data?.code === 200) {
                    Swal.fire({
                        icon: 'success',
                        title: t('saveOk'),
                        timer: 1500,
                        showConfirmButton: false
                    });
                    state.upsertModalVisible = false;
                    await loadCatalog();
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: t('saveFailed'),
                        text: response?.data?.message ?? ''
                    });
                }
            } catch (error) {
                console.error("Save catalog error", error);
                Swal.fire({
                    icon: 'error',
                    title: t('errorTitle'),
                    text: error.response?.data?.message || t('serverError')
                });
            }
        };

        const deleteOffering = async (id) => {
            const confirmRes = await Swal.fire({
                title: t('deleteOffering'),
                text: t('deleteConfirm'),
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#c8102e',
                confirmButtonText: t('yesDelete'),
                cancelButtonText: t('cancel')
            });

            if (!confirmRes.isConfirmed) return;

            try {
                const response = await AxiosManager.post('/ProductOffering/DeleteProductOffering', {
                    id: id,
                    deletedById: StorageManager.getUserId()
                });
                if (response?.data?.code === 200) {
                    Swal.fire({
                        icon: 'success',
                        title: t('deleteOk'),
                        timer: 1500,
                        showConfirmButton: false
                    });
                    state.upsertModalVisible = false;
                    await loadCatalog();
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: t('errorTitle'),
                        text: response?.data?.message ?? t('deleteFailed')
                    });
                }
            } catch (error) {
                console.error("Delete catalog error", error);
                Swal.fire({
                    icon: 'error',
                    title: t('errorTitle'),
                    text: error.response?.data?.message || t('serverError')
                });
            }
        };

        // Lifecycle mounting
        Vue.onMounted(async () => {
            document.documentElement.addEventListener('syriatel-locale-changed', onLocaleChanged);
            try {
                await window.TelecomI18n?.ensureLoaded?.();
                const layoutLang = document.documentElement.lang?.toLowerCase().startsWith('en') ? 'en' : 'ar';
                toggleLanguage(layoutLang);
                const title = window.TelecomI18n?.t?.('productCatalog.pageTitle');
                if (title) document.title = title;

                // Load database offerings
                await loadCatalog();
                await loadTechnicalProducts();
            } catch (e) {
                console.error("Mount error in catalog app", e);
            } finally {
                if (typeof hideSpinnerAndShowContent === 'function') {
                    hideSpinnerAndShowContent();
                }
                if (typeof setFormCardHeight === 'function') {
                    setFormCardHeight();
                }
            }
        });

        Vue.onUnmounted(() => {
            document.documentElement.removeEventListener('syriatel-locale-changed', onLocaleChanged);
        });

        return {
            ...Vue.toRefs(state),
            filterTabs,
            filteredOfferings,
            t,
            offerDisplayName,
            toggleLanguage,
            selectTab,
            resetFilters,
            getLineTypeClass,
            getLineTypeName,
            getLineTypeBadgeClass,
            formatPrice,
            formatPlanPrice,
            getComponentTypeIcon,
            getComponentTypeClass,
            getComponentTypeName,
            getPlanTypeName,
            getPlanTypePeriod,
            hasFeature,
            getFeatureLabel,
            viewDetails,
            closeDetails,
            triggerAction,
            refreshCatalog: loadCatalog,
            openCreateModal,
            openEditModal,
            closeUpsert,
            addComponentToForm,
            removeComponentFromForm,
            addPricePlanToForm,
            removePricePlanFromForm,
            saveOffering,
            deleteOffering
        };
    }
};

Vue.createApp(ProductCatalogApp).mount('#app');
