const TEL_SUB_DEFAULT_PREPAID_ID = 'a0e0e0e0-0000-4000-8000-000000000001';

const telecomT = (key, fallback) => {
    try {
        if (window.TelecomI18n?.resolve) {
            return window.TelecomI18n.resolve(key, fallback, ['customerList']);
        }
        const raw = String(key);
        const paths = raw.includes('.') && !raw.startsWith('customerList.')
            ? [raw, `customerList.${raw}`, `customer360Profile.${raw}`]
            : [`customerList.${raw}`, raw, `customer360Profile.${raw}`];
        for (const k of paths) {
            const hit = window.TelecomI18n?.t?.(k);
            if (hit) return hit;
        }
        return fallback;
    } catch {
        return fallback;
    }
};

const uiModalT = (key, fallback) => {
    try {
        if (typeof MacquiresUiI18n !== 'undefined' && MacquiresUiI18n.t) {
            const hit = MacquiresUiI18n.t(key);
            if (hit && hit !== key) return hit;
        }
    } catch { /* ignore */ }
    return fallback;
};

const showTelecomConfirmResult = (confirmRes, successTitle) => {
    if (window.TelecomUiBadges?.showConfirmToast) {
        return window.TelecomUiBadges.showConfirmToast(confirmRes, {
            successTitle: successTitle || telecomT('swal.executed', 'تم التنفيذ'),
        });
    }
    if (window.Swal) {
        Swal.fire({
            icon: 'success',
            title: successTitle || telecomT('swal.executed', 'تم التنفيذ'),
            timer: 1800,
            showConfirmButton: false,
        });
    }
    return { scheduled: false };
};

const operationStatusBadge = (status) =>
    window.TelecomUiBadges?.operationStatusBootstrapClass?.(status) || 'bg-secondary';

const formatOpEffectiveDate = (op) => {
    const utc =
        window.TelecomUiBadges?.pickScheduledEffectiveDate?.(op) ??
        op?.scheduledEffectiveDateUtc ??
        op?.ScheduledEffectiveDateUtc;
    if (!utc) return '';
    return window.TelecomUiBadges?.formatScheduledEffectiveDate?.(utc) || String(utc);
};

const App = {
    setup() {
        const state = Vue.reactive({
            mainData: [],
            deleteMode: false,
            customerGroupListLookupData: [],
            customerCategoryListLookupData: [],
            telecomLineTypes: [],
            secondaryData: [],
            mainTitle: null,
            manageContactTitle: '',
            id: '',
            name: '',
            number: '',
            customerGroupId: null,
            customerCategoryId: null,
            description: '',
            street: '',
            city: '',
            state: '',
            zipCode: '',
            country: '',
            phoneNumber: '',
            faxNumber: '',
            emailAddress: '',
            website: '',
            whatsApp: '',
            linkedIn: '',
            facebook: '',
            instagram: '',
            twitterX: '',
            tikTok: '',
            errors: {
                name: '',
                customerGroupId: '',
                customerCategoryId: '',
                street: '',
                city: '',
                state: '',
                zipCode: '',
                country: '',
                phoneNumber: '',
                emailAddress: '',
                nationalId: '',
                commercialRegistration: '',
            },
            isSubmitting: false,
            telecomMsisdn: '',
            telecomMsisdnInitial: '',
            telecomMsisdnPool: [],
            telecomMsisdnAssetId: '',
            telecomMsisdnPoolBusy: false,
            telecomSubscriptionId: '',
            telecomSubscriptionIdInitial: '',
            telecomSubscriptionTypeId: TEL_SUB_DEFAULT_PREPAID_ID,
            telecomSubscriptionTypeIdInitial: TEL_SUB_DEFAULT_PREPAID_ID,
            telecomSubscriptionRows: [],
            customerOnboardingActive: false,
            customerOnboardingStep: 0,
            addFlowBootstrapTelecom: false,
            customerSearchNationalId: '',
            customerSearchPhone: '',
            customerSearchResults: [],
            customerSearchBusy: false,
            customerSearchAttempted: false,
            isEmbedded: false,
            activateAfterSave: false,
            hlrSubscriberProfileId: '',
            hlrLiveData: null,
            hlrLiveBusy: false,
            hlrResyncBusy: false,
            customer360: null,
            customer360Busy: false,
            customer360Error: null,
            lineWallets: {},
            lineWalletsBusy: {},
            rechargeBusy: '',
            nationalIdMasked: '',
            nationalIdRevealed: false,
            nationalIdPersisted: '',
            subscriberType: 0,
            nationalId: '',
            dateOfBirth: '',
            commercialRegistration: '',
            taxNumber: '',
            authorizedSignatory: '',
        });

        const mainGridRef = Vue.ref(null);
        const mainModalRef = Vue.ref(null);
        const manageContactModalRef = Vue.ref(null);
        const secondaryGridRef = Vue.ref(null);
        const nameRef = Vue.ref(null);
        const numberRef = Vue.ref(null);
        const streetRef = Vue.ref(null);
        const cityRef = Vue.ref(null);
        const stateRef = Vue.ref(null);
        const zipCodeRef = Vue.ref(null);
        const countryRef = Vue.ref(null);
        const phoneNumberRef = Vue.ref(null);
        const faxNumberRef = Vue.ref(null);
        const emailAddressRef = Vue.ref(null);
        const websiteRef = Vue.ref(null);
        const whatsAppRef = Vue.ref(null);
        const linkedInRef = Vue.ref(null);
        const facebookRef = Vue.ref(null);
        const instagramRef = Vue.ref(null);
        const twitterXRef = Vue.ref(null);
        const tikTokRef = Vue.ref(null);
        const customerGroupIdRef = Vue.ref(null);
        const customerCategoryIdRef = Vue.ref(null);

        const services = {
            getMainData: async () => {
                try {
                    const pageQs = new URLSearchParams(window.location.search);
                    const listQs = new URLSearchParams();
                    listQs.set('isDeleted', 'false');
                    const cid = pageQs.get('customerId');
                    if (cid) {
                        listQs.set('customerId', cid);
                    }
                    const response = await AxiosManager.get('/Customer/GetCustomerList?' + listQs.toString(), {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getTelecomLineTypes: async () => {
                try {
                    const response = await AxiosManager.get(
                        '/TelecomSubscriptionType/GetTelecomSubscriptionTypeList?isDeleted=false&activeOnly=true',
                        {}
                    );
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createMainData: async (name, customerGroupId, customerCategoryId, description, street, city, stateVal, zipCode, country, phoneNumber, faxNumber, emailAddress, website, whatsApp, linkedIn, facebook, instagram, twitterX, tikTok, subscriberType, nationalId, dateOfBirth, commercialRegistration, taxNumber, authorizedSignatory) => {
                try {
                    const response = await AxiosManager.post('/Customer/CreateCustomer', {
                        name, customerGroupId, customerCategoryId, description, street, city, state: stateVal, zipCode, country, phoneNumber, faxNumber, emailAddress, website, whatsApp, linkedIn, facebook, instagram, twitterX, tikTok, subscriberType, nationalId, dateOfBirth, commercialRegistration, taxNumber, authorizedSignatory
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, name, customerGroupId, customerCategoryId, description, street, city, stateVal, zipCode, country, phoneNumber, faxNumber, emailAddress, website, whatsApp, linkedIn, facebook, instagram, twitterX, tikTok, subscriberType, nationalId, dateOfBirth, commercialRegistration, taxNumber, authorizedSignatory) => {
                try {
                    const response = await AxiosManager.post('/Customer/UpdateCustomer', {
                        id, name, customerGroupId, customerCategoryId, description, street, city, state: stateVal, zipCode, country, phoneNumber, faxNumber, emailAddress, website, whatsApp, linkedIn, facebook, instagram, twitterX, tikTok, subscriberType, nationalId, dateOfBirth, commercialRegistration, taxNumber, authorizedSignatory
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id) => {
                try {
                    const response = await AxiosManager.post('/Customer/DeleteCustomer', {
                        id
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getCustomerGroupListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/CustomerGroup/GetCustomerGroupList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getCustomerCategoryListLookupData: async () => {
                try {
                    const response = await AxiosManager.get('/CustomerCategory/GetCustomerCategoryList', {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            getSecondaryData: async (customerId) => {
                try {
                    const response = await AxiosManager.get('/CustomerContact/GetCustomerContactByCustomerIdList?customerId=' + customerId, {});
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            createSecondaryData: async (name, jobTitle, phoneNumber, emailAddress, description, customerId) => {
                try {
                    const response = await AxiosManager.post('/CustomerContact/CreateCustomerContact', {
                        name, jobTitle, phoneNumber, emailAddress, description, customerId
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateSecondaryData: async (id, name, jobTitle, phoneNumber, emailAddress, description, customerId) => {
                try {
                    const response = await AxiosManager.post('/CustomerContact/UpdateCustomerContact', {
                        id, name, jobTitle, phoneNumber, emailAddress, description, customerId
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteSecondaryData: async (id) => {
                try {
                    const response = await AxiosManager.post('/CustomerContact/DeleteCustomerContact', {
                        id
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
        };

        const methods = {
            populateCustomerGroupListLookupData: async () => {
                const response = await services.getCustomerGroupListLookupData();
                state.customerGroupListLookupData = response?.data?.content?.data;
            },
            populateCustomerCategoryListLookupData: async () => {
                const response = await services.getCustomerCategoryListLookupData();
                state.customerCategoryListLookupData = response?.data?.content?.data;
            },
            populateTelecomLineTypes: async () => {
                try {
                    const response = await services.getTelecomLineTypes();
                    const rows = response?.data?.content?.data || [];
                    state.telecomLineTypes = rows;
                } catch {
                    state.telecomLineTypes = [];
                }
            },
            populateMainData: async () => {
                const response = await services.getMainData();
                const rows = response?.data?.content?.data;
                state.mainData = Array.isArray(rows)
                    ? rows.map((item) => {
                          const createdRaw = item.createdAtUtc;
                          const createdAtUtc =
                              createdRaw == null || createdRaw === ''
                                  ? null
                                  : (() => {
                                        const d = new Date(createdRaw);
                                        return Number.isNaN(d.getTime()) ? null : d;
                                    })();
                          return {
                              ...item,
                              subscriberType: item.subscriberType || 'Individual',
                              subscriptionTypeName: item.subscriptionTypeName || '—',
                              subscriptionTypeDisplayColor: item.subscriptionTypeDisplayColor || '#333',
                              telecomMsisdnsSummary: item.telecomMsisdnsSummary || '—',
                              primaryMsisdn: item.primaryMsisdn || '—',
                              createdAtUtc,
                          };
                      })
                    : [];
            },
            populateSecondaryData: async (customerId) => {
                const response = await services.getSecondaryData(customerId);
                state.secondaryData = response?.data?.content?.data.map(item => ({
                    ...item,
                    createdAtUtc: new Date(item.createdAtUtc)
                }));
            },
        };

        const customerGroupListLookup = {
            obj: null,
            create: () => {
                if (state.customerGroupListLookupData && Array.isArray(state.customerGroupListLookupData)) {
                    customerGroupListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.customerGroupListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: MacquiresUiI18n.phByEn('Select a Customer Group'),
                        change: (e) => {
                            state.customerGroupId = e.value;
                        }
                    });
                    customerGroupListLookup.obj.appendTo(customerGroupIdRef.value);
                } else {
                    console.error('Customer Group list lookup data is not available or invalid.');
                }
            },
            refresh: () => {
                if (customerGroupListLookup.obj) {
                    customerGroupListLookup.obj.value = state.customerGroupId;
                }
            },
        };

        const customerCategoryListLookup = {
            obj: null,
            create: () => {
                if (state.customerCategoryListLookupData && Array.isArray(state.customerCategoryListLookupData)) {
                    customerCategoryListLookup.obj = new ej.dropdowns.DropDownList({
                        dataSource: state.customerCategoryListLookupData,
                        fields: { value: 'id', text: 'name' },
                        placeholder: MacquiresUiI18n.phByEn('Select a Customer Category'),
                        change: (e) => {
                            state.customerCategoryId = e.value;
                        }
                    });
                    customerCategoryListLookup.obj.appendTo(customerCategoryIdRef.value);
                } else {
                    console.error('Customer Category list lookup data is not available or invalid.');
                }
            },
            refresh: () => {
                if (customerCategoryListLookup.obj) {
                    customerCategoryListLookup.obj.value = state.customerCategoryId;
                }
            },
        };

        const nameText = {
            obj: null,
            create: () => {
                nameText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter Name'),
                });
                nameText.obj.appendTo(nameRef.value);
            },
            refresh: () => {
                if (nameText.obj) {
                    nameText.obj.value = state.name;
                }
            }
        };

        const numberText = {
            obj: null,
            create: () => {
                numberText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('[auto]'),
                    readonly: true
                });
                numberText.obj.appendTo(numberRef.value);
            },
            refresh: () => {
                if (numberText.obj) {
                    numberText.obj.value = state.number;
                }
            }
        };

        const streetText = {
            obj: null,
            create: () => {
                streetText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter Street'),
                });
                streetText.obj.appendTo(streetRef.value);
            },
            refresh: () => {
                if (streetText.obj) {
                    streetText.obj.value = state.street;
                }
            }
        };

        const cityText = {
            obj: null,
            create: () => {
                cityText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter City'),
                });
                cityText.obj.appendTo(cityRef.value);
            },
            refresh: () => {
                if (cityText.obj) {
                    cityText.obj.value = state.city;
                }
            }
        };

        const stateText = {
            obj: null,
            create: () => {
                stateText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter State'),
                });
                stateText.obj.appendTo(stateRef.value);
            },
            refresh: () => {
                if (stateText.obj) {
                    stateText.obj.value = state.state;
                }
            }
        };

        const zipCodeText = {
            obj: null,
            create: () => {
                zipCodeText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter Zip Code'),
                });
                zipCodeText.obj.appendTo(zipCodeRef.value);
            },
            refresh: () => {
                if (zipCodeText.obj) {
                    zipCodeText.obj.value = state.zipCode;
                }
            }
        };

        const countryText = {
            obj: null,
            create: () => {
                countryText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter Country'),
                });
                countryText.obj.appendTo(countryRef.value);
            },
            refresh: () => {
                if (countryText.obj) {
                    countryText.obj.value = state.country;
                }
            }
        };

        const phoneNumberText = {
            obj: null,
            create: () => {
                phoneNumberText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter Phone Number'),
                });
                phoneNumberText.obj.appendTo(phoneNumberRef.value);
            },
            refresh: () => {
                if (phoneNumberText.obj) {
                    phoneNumberText.obj.value = state.phoneNumber;
                }
            }
        };

        const faxNumberText = {
            obj: null,
            create: () => {
                faxNumberText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter Fax Number'),
                });
                faxNumberText.obj.appendTo(faxNumberRef.value);
            },
            refresh: () => {
                if (faxNumberText.obj) {
                    faxNumberText.obj.value = state.faxNumber;
                }
            }
        };

        const emailAddressText = {
            obj: null,
            create: () => {
                emailAddressText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter Email Address'),
                });
                emailAddressText.obj.appendTo(emailAddressRef.value);
            },
            refresh: () => {
                if (emailAddressText.obj) {
                    emailAddressText.obj.value = state.emailAddress;
                }
            }
        };

        const websiteText = {
            obj: null,
            create: () => {
                websiteText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter Website'),
                });
                websiteText.obj.appendTo(websiteRef.value);
            },
            refresh: () => {
                if (websiteText.obj) {
                    websiteText.obj.value = state.website;
                }
            }
        };

        const whatsAppText = {
            obj: null,
            create: () => {
                whatsAppText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter WhatsApp'),
                });
                whatsAppText.obj.appendTo(whatsAppRef.value);
            },
            refresh: () => {
                if (whatsAppText.obj) {
                    whatsAppText.obj.value = state.whatsApp;
                }
            }
        };

        const linkedInText = {
            obj: null,
            create: () => {
                linkedInText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter LinkedIn'),
                });
                linkedInText.obj.appendTo(linkedInRef.value);
            },
            refresh: () => {
                if (linkedInText.obj) {
                    linkedInText.obj.value = state.linkedIn;
                }
            }
        };

        const facebookText = {
            obj: null,
            create: () => {
                facebookText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter Facebook'),
                });
                facebookText.obj.appendTo(facebookRef.value);
            },
            refresh: () => {
                if (facebookText.obj) {
                    facebookText.obj.value = state.facebook;
                }
            }
        };

        const instagramText = {
            obj: null,
            create: () => {
                instagramText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter Instagram'),
                });
                instagramText.obj.appendTo(instagramRef.value);
            },
            refresh: () => {
                if (instagramText.obj) {
                    instagramText.obj.value = state.instagram;
                }
            }
        };

        const twitterXText = {
            obj: null,
            create: () => {
                twitterXText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter Twitter/X'),
                });
                twitterXText.obj.appendTo(twitterXRef.value);
            },
            refresh: () => {
                if (twitterXText.obj) {
                    twitterXText.obj.value = state.twitterX;
                }
            }
        };

        const tikTokText = {
            obj: null,
            create: () => {
                tikTokText.obj = new ej.inputs.TextBox({
                    placeholder: MacquiresUiI18n.phByEn('Enter TikTok'),
                });
                tikTokText.obj.appendTo(tikTokRef.value);
            },
            refresh: () => {
                if (tikTokText.obj) {
                    tikTokText.obj.value = state.tikTok;
                }
            }
        };

        const refreshAllCustomerFormWidgets = () => {
            nameText.refresh();
            numberText.refresh();
            streetText.refresh();
            cityText.refresh();
            stateText.refresh();
            zipCodeText.refresh();
            countryText.refresh();
            phoneNumberText.refresh();
            faxNumberText.refresh();
            emailAddressText.refresh();
            websiteText.refresh();
            whatsAppText.refresh();
            linkedInText.refresh();
            facebookText.refresh();
            instagramText.refresh();
            twitterXText.refresh();
            tikTokText.refresh();
            customerGroupListLookup.refresh();
            customerCategoryListLookup.refresh();
        };

        Vue.watch(
            () => state.name,
            (newVal, oldVal) => {
                state.errors.name = '';
                nameText.refresh();
            }
        );

        Vue.watch(
            () => state.number,
            (newVal, oldVal) => {
                numberText.refresh();
            }
        );

        Vue.watch(
            () => state.customerGroupId,
            (newVal, oldVal) => {
                state.errors.customerGroupId = '';
                customerGroupListLookup.refresh();
            }
        );

        Vue.watch(
            () => state.customerCategoryId,
            (newVal, oldVal) => {
                state.errors.customerCategoryId = '';
                customerCategoryListLookup.refresh();
            }
        );

        Vue.watch(
            () => state.street,
            (newVal, oldVal) => {
                state.errors.street = '';
                streetText.refresh();
            }
        );

        Vue.watch(
            () => state.city,
            (newVal, oldVal) => {
                state.errors.city = '';
                cityText.refresh();
            }
        );

        Vue.watch(
            () => state.state,
            (newVal, oldVal) => {
                state.errors.state = '';
                stateText.refresh();
            }
        );

        Vue.watch(
            () => state.zipCode,
            (newVal, oldVal) => {
                state.errors.zipCode = '';
                zipCodeText.refresh();
            }
        );

        Vue.watch(
            () => state.country,
            (newVal, oldVal) => {
                state.errors.country = '';
                countryText.refresh();
            }
        );

        Vue.watch(
            () => state.phoneNumber,
            (newVal, oldVal) => {
                state.errors.phoneNumber = '';
                phoneNumberText.refresh();
            }
        );

        Vue.watch(
            () => state.emailAddress,
            (newVal, oldVal) => {
                state.errors.emailAddress = '';
                emailAddressText.refresh();
            }
        );

        Vue.watch(
            () => state.nationalId,
            (newVal, oldVal) => {
                state.errors.nationalId = '';
            }
        );

        Vue.watch(
            () => state.commercialRegistration,
            (newVal, oldVal) => {
                state.errors.commercialRegistration = '';
            }
        );

        const handler = {
            handleSubmitSaveOnly: async function () {
                state.activateAfterSave = false;
                await handler.handleSubmit();
            },
            handleSubmitSaveAndActivate: async function () {
                state.activateAfterSave = true;
                await handler.handleSubmit();
            },
            handleSubmit: async function () {
                try {
                    state.isSubmitting = true;
                    await new Promise(resolve => setTimeout(resolve, 200));

                    let isValid = true;

                    if (!state.name) {
                        state.errors.name = 'Name is required.';
                        isValid = false;
                    }
                    if (!state.customerGroupId) {
                        state.errors.customerGroupId = 'Customer Group is required.';
                        isValid = false;
                    }
                    if (!state.customerCategoryId) {
                        state.errors.customerCategoryId = 'Customer Category is required.';
                        isValid = false;
                    }
                    if (!state.street) {
                        state.errors.street = 'Street is required.';
                        isValid = false;
                    }
                    if (!state.city) {
                        state.errors.city = 'City is required.';
                        isValid = false;
                    }
                    if (!state.state) {
                        state.errors.state = 'State is required.';
                        isValid = false;
                    }
                    if (!state.zipCode) {
                        state.errors.zipCode = 'Zip Code is required.';
                        isValid = false;
                    }
                    if (!state.country) {
                        state.errors.country = 'Country is required.';
                        isValid = false;
                    }
                    if (!state.phoneNumber) {
                        state.errors.phoneNumber = 'Phone Number is required.';
                        isValid = false;
                    }
                    if (!state.emailAddress) {
                        state.errors.emailAddress = 'Email Address is required.';
                        isValid = false;
                    }

                    const nationalIdForSave = () => {
                        if (state.subscriberType !== 0) return (state.nationalId || '').trim();
                        if (state.id && !state.nationalIdRevealed) return (state.nationalIdPersisted || '').trim();
                        return (state.nationalId || '').trim();
                    };

                    if (!state.deleteMode) {
                        if (state.subscriberType === 0) {
                            const nidVal = nationalIdForSave();
                            if (!nidVal || nidVal.length !== 10) {
                                state.errors.nationalId = telecomT('swal.nationalIdRequired', '');
                                isValid = false;
                            }
                        } else if (state.subscriberType === 1) {
                            if (!state.commercialRegistration || state.commercialRegistration.trim().length < 4) {
                                state.errors.commercialRegistration = telecomT('swal.commercialRegRequired', '');
                                isValid = false;
                            }
                        }
                    }

                    if (!isValid) return;

                    const commitWasUpdate = state.id !== '' && !state.deleteMode;

                    const nidCommit = nationalIdForSave();
                    const response = state.id === ''
                        ? await services.createMainData(state.name, state.customerGroupId, state.customerCategoryId, state.description, state.street, state.city, state.state, state.zipCode, state.country, state.phoneNumber, state.faxNumber, state.emailAddress, state.website, state.whatsApp, state.linkedIn, state.facebook, state.instagram, state.twitterX, state.tikTok, state.subscriberType, nidCommit, state.dateOfBirth ? new Date(state.dateOfBirth).toISOString() : null, state.commercialRegistration, state.taxNumber, state.authorizedSignatory)
                        : state.deleteMode
                            ? await services.deleteMainData(state.id)
                            : await services.updateMainData(state.id, state.name, state.customerGroupId, state.customerCategoryId, state.description, state.street, state.city, state.state, state.zipCode, state.country, state.phoneNumber, state.faxNumber, state.emailAddress, state.website, state.whatsApp, state.linkedIn, state.facebook, state.instagram, state.twitterX, state.tikTok, state.subscriberType, nidCommit, state.dateOfBirth ? new Date(state.dateOfBirth).toISOString() : null, state.commercialRegistration, state.taxNumber, state.authorizedSignatory);

                    if (response.data.code === 200) {
                        const savedCustomerId =
                            response?.data?.content?.data?.id ?? state.id ?? '';

                        if (
                            commitWasUpdate &&
                            gridAccess.showFullColumns &&
                            gridAccess.canMutate &&
                            !state.addFlowBootstrapTelecom
                        ) {
                            const subChanged =
                                state.telecomSubscriptionId !== state.telecomSubscriptionIdInitial;
                            const typeChanged =
                                state.telecomSubscriptionTypeId !== state.telecomSubscriptionTypeIdInitial;
                            if (subChanged || typeChanged) {
                                const body = {
                                    customerId: state.id,
                                };
                                if (state.telecomSubscriptionId) {
                                    body.subscriptionId = state.telecomSubscriptionId;
                                }
                                if (typeChanged) {
                                    body.primarySubscriptionTypeId = state.telecomSubscriptionTypeId;
                                }
                                try {
                                    const telecomRes = await AxiosManager.post(
                                        '/Telecom/UpdateCustomerPrimaryTelecomLine',
                                        body
                                    );
                                    if (telecomRes?.data?.code !== 200) {
                                        await Swal.fire({
                                            icon: 'warning',
                                            title: telecomT('swal.savedTitle', 'Saved'),
                                            text:
                                                telecomRes?.data?.message ||
                                                telecomT('swal.lineUpdateFailed'),
                                            confirmButtonText: telecomT('swal.ok', 'OK'),
                                        });
                                    } else {
                                        state.telecomSubscriptionIdInitial = state.telecomSubscriptionId;
                                        state.telecomSubscriptionTypeIdInitial = state.telecomSubscriptionTypeId;
                                    }
                                } catch (telecomErr) {
                                    await Swal.fire({
                                        icon: 'warning',
                                        title: telecomT('swal.savedTitle', 'Saved'),
                                        text:
                                            telecomErr.response?.data?.message ||
                                            telecomErr.message ||
                                            telecomT('swal.lineUpdateFailedShort'),
                                        confirmButtonText: telecomT('swal.ok', 'OK'),
                                    });
                                }
                            }
                        }

                        const msisdnTrim = (state.telecomMsisdn || '').trim();
                        const shouldRegisterBootstrap =
                            gridAccess.showFullColumns &&
                            gridAccess.canMutate &&
                            state.addFlowBootstrapTelecom &&
                            msisdnTrim &&
                            savedCustomerId;

                        if (shouldRegisterBootstrap) {
                            try {
                                const nat = (state.customerSearchNationalId || '').trim();
                                const regBody = {
                                    customerId: savedCustomerId,
                                    primaryMsisdn: msisdnTrim,
                                    primarySubscriptionTypeId: state.telecomSubscriptionTypeId,
                                };
                                if (nat) {
                                    regBody.nationalId = nat;
                                }
                                const regRes = await AxiosManager.post(
                                    '/Telecom/RegisterSubscriberProfileForCustomer',
                                    regBody
                                );
                                if (regRes?.data?.code !== 200) {
                                    await Swal.fire({
                                        icon: 'warning',
                                        title: telecomT('swal.savedTitle', 'Saved'),
                                        text:
                                            regRes?.data?.message ||
                                            telecomT('swal.bootstrapFailed'),
                                        confirmButtonText: telecomT('swal.ok', 'OK'),
                                    });
                                } else {
                                    state.telecomMsisdnInitial = state.telecomMsisdn || '';
                                    state.telecomSubscriptionTypeIdInitial = state.telecomSubscriptionTypeId;
                                }
                            } catch (regErr) {
                                await Swal.fire({
                                    icon: 'warning',
                                    title: telecomT('swal.savedTitle', 'Saved'),
                                    text:
                                        regErr.response?.data?.message ||
                                        regErr.message ||
                                        telecomT('swal.bootstrapFailed'),
                                    confirmButtonText: telecomT('swal.ok', 'OK'),
                                });
                            }
                        }
                        state.addFlowBootstrapTelecom = false;

                        await methods.populateMainData();
                        mainGrid.refresh();

                        if (!state.deleteMode) {
                            state.mainTitle = MacquiresUiI18n.mb('customer','edit');
                            state.id = response?.data?.content?.data.id ?? '';
                            state.number = response?.data?.content?.data.number ?? '';
                            state.name = response?.data?.content?.data.name ?? '';
                            state.customerGroupId = response?.data?.content?.data.customerGroupId ?? null;
                            state.customerCategoryId = response?.data?.content?.data.customerCategoryId ?? null;
                            state.description = response?.data?.content?.data.description ?? '';
                            state.street = response?.data?.content?.data.street ?? '';
                            state.city = response?.data?.content?.data.city ?? '';
                            state.state = response?.data?.content?.data.state ?? '';
                            state.zipCode = response?.data?.content?.data.zipCode ?? '';
                            state.country = response?.data?.content?.data.country ?? '';
                            state.phoneNumber = response?.data?.content?.data.phoneNumber ?? '';
                            state.faxNumber = response?.data?.content?.data.faxNumber ?? '';
                            state.emailAddress = response?.data?.content?.data.emailAddress ?? '';
                            state.website = response?.data?.content?.data.website ?? '';
                            state.whatsApp = response?.data?.content?.data.whatsApp ?? '';
                            state.linkedIn = response?.data?.content?.data.linkedIn ?? '';
                            state.facebook = response?.data?.content?.data.facebook ?? '';
                            state.instagram = response?.data?.content?.data.instagram ?? '';
                            state.twitterX = response?.data?.content?.data.twitterX ?? '';
                            state.tikTok = response?.data?.content?.data.tikTok ?? '';

                            Swal.fire({
                                icon: 'success',
                                title: state.deleteMode ? 'Delete Successful' : 'Save Successful',
                                text: 'Form will be closed...',
                                timer: 2000,
                                showConfirmButton: false
                            });
                            if (state.isEmbedded) {
                                setTimeout(() => {
                                    window.parent.postMessage({
                                        action: state.activateAfterSave ? 'syriatel-customer-saved-activate' : 'syriatel-customer-saved',
                                        customerId: savedCustomerId
                                    }, '*');
                                }, 1500);
                            } else {
                                setTimeout(() => {
                                    mainModal.obj.hide();
                                }, 2000);
                            }

                        } else {
                            Swal.fire({
                                icon: 'success',
                                title: uiModalT('swal_deleteSuccessful', 'Delete Successful'),
                                text: uiModalT('swal_formWillClose', 'Form will be closed...'),
                                timer: 2000,
                                showConfirmButton: false
                            });
                            setTimeout(() => {
                                mainModal.obj.hide();
                                resetFormState();
                            }, 2000);
                        }

                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: state.deleteMode
                                ? uiModalT('swal_deleteFailed', 'Delete Failed')
                                : uiModalT('swal_saveFailed', 'Save Failed'),
                            text: response.data.message ?? uiModalT('swal_checkData', 'Please check your data.'),
                            confirmButtonText: uiModalT('swal_tryAgain', 'Try Again')
                        });
                    }

                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: uiModalT('swal_errorOccurred', 'An Error Occurred'),
                        text: error.response?.data?.message ?? uiModalT('swal_tryAgainLater', 'Please try again.'),
                        confirmButtonText: uiModalT('swal_ok', 'OK')
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
        };

        const parseMsisdnPoolRows = (res) => {
            if (typeof StorageManager?.apiList === 'function') {
                const list = StorageManager.apiList(res);
                if (Array.isArray(list) && list.length > 0) return list;
            }
            const content = res?.data?.content ?? res?.data?.Content;
            const rows = content?.data ?? content?.Data;
            return Array.isArray(rows) ? rows : [];
        };

        const isAvailableMsisdnPoolRow = (row) => {
            if (!row) return false;
            const name = String(row.poolStatusName ?? row.PoolStatusName ?? '').toLowerCase();
            if (name === 'available') return true;
            const st = row.poolStatus ?? row.PoolStatus;
            return st === 0 || st === '0' || st === 'Available';
        };

        const loadBootstrapMsisdnPool = async () => {
            state.telecomMsisdnPoolBusy = true;
            state.telecomMsisdnAssetId = '';
            state.telecomMsisdn = '';
            try {
                const res = await AxiosManager.get('/Telecom/GetMsisdnAssetPoolList?status=Available', {});
                state.telecomMsisdnPool = parseMsisdnPoolRows(res).filter(isAvailableMsisdnPoolRow);
            } catch {
                state.telecomMsisdnPool = [];
            } finally {
                state.telecomMsisdnPoolBusy = false;
            }
        };

        const onBootstrapMsisdnPicked = () => {
            const id = (state.telecomMsisdnAssetId || '').trim();
            const row = (state.telecomMsisdnPool || []).find((a) => String(a.id ?? a.Id) === id);
            state.telecomMsisdn = row?.msisdn ?? row?.Msisdn ?? '';
        };

        const resetFormState = () => {
            state.id = '';
            state.number = '';
            state.name = '';
            state.customerGroupId = null;
            state.customerCategoryId = null;
            state.subscriberType = 0;
            state.nationalId = '';
            state.dateOfBirth = '';
            state.commercialRegistration = '';
            state.taxNumber = '';
            state.authorizedSignatory = '';
            state.description = '';
            state.street = '';
            state.city = '';
            state.state = '';
            state.zipCode = '';
            state.country = '';
            state.phoneNumber = '';
            state.faxNumber = '';
            state.emailAddress = '';
            state.website = '';
            state.whatsApp = '';
            state.linkedIn = '';
            state.facebook = '';
            state.instagram = '';
            state.twitterX = '';
            state.tikTok = '';
            state.telecomMsisdn = '';
            state.telecomMsisdnInitial = '';
            state.telecomMsisdnPool = [];
            state.telecomMsisdnAssetId = '';
            state.telecomMsisdnPoolBusy = false;
            state.telecomSubscriptionId = '';
            state.telecomSubscriptionIdInitial = '';
            const defLineTypeId =
                (state.telecomLineTypes || []).find((x) => x.isDefault)?.id || TEL_SUB_DEFAULT_PREPAID_ID;
            state.telecomSubscriptionTypeId = defLineTypeId;
            state.telecomSubscriptionTypeIdInitial = defLineTypeId;
            state.telecomSubscriptionRows = [];
            state.errors = {
                name: '',
                customerGroupId: '',
                customerCategoryId: '',
                street: '',
                city: '',
                state: '',
                zipCode: '',
                country: '',
                phoneNumber: '',
                emailAddress: '',
            };
            state.customerOnboardingActive = false;
            state.customerOnboardingStep = 0;
            state.addFlowBootstrapTelecom = false;
            state.customerSearchNationalId = '';
            state.customerSearchPhone = '';
            state.customerSearchResults = [];
            state.customerSearchBusy = false;
            state.customerSearchAttempted = false;
        };

        const syncTelecomLineTypeFromSelectedSubscription = () => {
            const row = (state.telecomSubscriptionRows || []).find(
                (s) => s.telecomSubscriptionId === state.telecomSubscriptionId
            );
            if (row && (row.subscriptionTypeCode || row.subscriptionTypeName)) {
                const lt = (state.telecomLineTypes || []).find(
                    (x) => x.code === row.subscriptionTypeCode || x.nameAr === row.subscriptionTypeName
                );
                if (lt && lt.id) {
                    state.telecomSubscriptionTypeId = lt.id;
                }
            }
        };

        const applyCustomerRowToState = async (customerId) => {
            if (!customerId) return;
            try {
                const r = await AxiosManager.get(
                    '/Customer/GetCustomerList?isDeleted=false&customerId=' + encodeURIComponent(customerId)
                );
                const row = r?.data?.content?.data?.[0];
                if (!row) {
                    await Swal.fire({
                        icon: 'warning',
                        title: telecomT('swal.loadFailed'),
                        text: telecomT('swal.customerNotFound'),
                        confirmButtonText: telecomT('swal.ok', 'OK'),
                    });
                    return;
                }
                state.id = row.id ?? '';
                state.number = row.number ?? '';
                state.name = row.name ?? '';
                state.customerGroupId = row.customerGroupId ?? null;
                state.customerCategoryId = row.customerCategoryId ?? null;
                state.subscriberType = row.subscriberType === 'Corporate' ? 1 : 0;
                state.nationalId = '';
                state.nationalIdPersisted = row.nationalId ?? '';
                state.nationalIdRevealed = false;
                state.nationalIdMasked = '';
                state.dateOfBirth = row.dateOfBirth ? row.dateOfBirth.substring(0, 10) : '';
                state.commercialRegistration = row.commercialRegistration ?? '';
                state.taxNumber = row.taxNumber ?? '';
                state.authorizedSignatory = row.authorizedSignatory ?? '';
                state.description = row.description ?? '';
                state.street = row.street ?? '';
                state.city = row.city ?? '';
                state.state = row.state ?? '';
                state.zipCode = row.zipCode ?? '';
                state.country = row.country ?? '';
                state.phoneNumber = row.phoneNumber ?? '';
                state.faxNumber = row.faxNumber ?? '';
                state.emailAddress = row.emailAddress ?? '';
                state.website = row.website ?? '';
                state.whatsApp = row.whatsApp ?? '';
                state.linkedIn = row.linkedIn ?? '';
                state.facebook = row.facebook ?? '';
                state.instagram = row.instagram ?? '';
                state.twitterX = row.twitterX ?? '';
                state.tikTok = row.tikTok ?? '';
                const defId =
                    (state.telecomLineTypes || []).find((x) => x.isDefault)?.id || TEL_SUB_DEFAULT_PREPAID_ID;
                state.telecomSubscriptionTypeId = row.subscriptionTypeId || defId;
                state.telecomSubscriptionRows = Array.isArray(row.telecomLines) ? row.telecomLines : [];
                const profileIds = [...new Set(state.telecomSubscriptionRows.map((s) => (s.subscriberProfileId || '').trim()).filter(Boolean))];
                state.hlrSubscriberProfileId = profileIds[0] || '';
                state.hlrLiveData = null;
                state.telecomMsisdn = '';
                state.telecomMsisdnInitial = '';
                if (state.telecomSubscriptionRows.length > 0) {
                    const prim = state.telecomSubscriptionRows.find(s => s.isPrimaryLine) || state.telecomSubscriptionRows[0];
                    state.telecomSubscriptionId = prim.telecomSubscriptionId;
                    state.telecomSubscriptionIdInitial = prim.telecomSubscriptionId;
                    syncTelecomLineTypeFromSelectedSubscription();
                } else {
                    state.telecomSubscriptionId = '';
                    state.telecomSubscriptionIdInitial = '';
                }
                state.telecomSubscriptionTypeIdInitial = state.telecomSubscriptionTypeId;
                await loadCustomer360(state.id);
                await Vue.nextTick();
                refreshAllCustomerFormWidgets();
            } catch (e) {
                await Swal.fire({
                    icon: 'error',
                    title: telecomT('swal.error'),
                    text: e.response?.data?.message ?? e.message ?? telecomT('swal.customerLoadFailed'),
                    confirmButtonText: telecomT('swal.ok', 'OK'),
                });
            }
        };

        const customerOnboarding = {
            runPreCheckSearch: async () => {
                const nat = (state.customerSearchNationalId || '').trim();
                const ph = (state.customerSearchPhone || '').trim();
                if (nat.length < 2 && ph.length < 2) {
                    await Swal.fire({
                        icon: 'info',
                        title: telecomT('swal.searchTitle'),
                        text: telecomT('swal.searchHint'),
                        confirmButtonText: telecomT('swal.ok', 'OK'),
                    });
                    return;
                }
                state.customerSearchBusy = true;
                state.customerSearchAttempted = true;
                try {
                    const qs = new URLSearchParams();
                    if (nat) qs.set('nationalId', nat);
                    if (ph) qs.set('phone', ph);
                    const res = await AxiosManager.get('/Customer/FindCustomerCandidates?' + qs.toString(), {});
                    if (res?.data?.code !== 200) {
                        state.customerSearchResults = [];
                        await Swal.fire({
                            icon: 'warning',
                            title: telecomT('swal.searchTitle'),
                            text: res?.data?.message || telecomT('swal.searchFailed'),
                            confirmButtonText: telecomT('swal.ok', 'OK'),
                        });
                        return;
                    }
                    const list = res?.data?.content?.data;
                    state.customerSearchResults = Array.isArray(list) ? list : [];
                } catch (e) {
                    state.customerSearchResults = [];
                    await Swal.fire({
                        icon: 'error',
                        title: telecomT('swal.error'),
                        text: e.response?.data?.message ?? e.message ?? telecomT('swal.searchFailed'),
                        confirmButtonText: telecomT('swal.ok', 'OK'),
                    });
                } finally {
                    state.customerSearchBusy = false;
                }
            },
            selectCandidate: async (c) => {
                if (!c?.id) return;
                state.customerOnboardingStep = 1;
                state.addFlowBootstrapTelecom = true;
                await applyCustomerRowToState(c.id);
                const defLine =
                    (state.telecomLineTypes || []).find((x) => x.isDefault)?.id || TEL_SUB_DEFAULT_PREPAID_ID;
                state.telecomSubscriptionTypeId = defLine;
                state.telecomSubscriptionTypeIdInitial = defLine;
                await loadBootstrapMsisdnPool();
            },
            proceedNewCustomer: async () => {
                state.customerOnboardingStep = 1;
                state.addFlowBootstrapTelecom = true;
                await loadBootstrapMsisdnPool();
            },
            goBackToSearch: () => {
                if (!state.customerOnboardingActive) return;
                const nat = state.customerSearchNationalId;
                const ph = state.customerSearchPhone;
                resetFormState();
                state.customerOnboardingActive = true;
                state.customerOnboardingStep = 0;
                state.customerSearchNationalId = nat;
                state.customerSearchPhone = ph;
                state.customerSearchResults = [];
                state.customerSearchAttempted = false;
                state.customerSearchBusy = false;
                state.addFlowBootstrapTelecom = false;
                Vue.nextTick(() => refreshAllCustomerFormWidgets());
            },
        };

        const resolveTelecomCustomerGridAccess = () => {
            const roles = StorageManager.getUserRoles() || [];
            const telecomAll = new Set(['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice', 'TelecomCallCenter', 'TelecomManagement']);
            const isStrictTelecom = roles.length > 0 && roles.every((r) => telecomAll.has(r));
            const canMutate =
                !isStrictTelecom ||
                roles.some((r) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(r));
            const showFullColumns =
                !isStrictTelecom || roles.some((r) => ['TelecomAdmin', 'TelecomBackOffice'].includes(r));
            const canViewDecryptedPii = roles.some((r) =>
                ['TelecomAdmin', 'TelecomBackOffice', 'TelecomManagement'].includes(r));
            const canQueryHlr = roles.some((r) =>
                ['TelecomAdmin', 'TelecomBackOffice', 'TelecomCallCenter', 'TelecomManagement'].includes(r));
            const canCreateTicket = roles.some((r) => ['TelecomAdmin', 'TelecomCallCenter'].includes(r));
            const perms = StorageManager.getUserPermissions?.() || [];
            const canToggleVas =
                StorageManager.hasAnyPermission?.(perms, ['telecom.vas.toggle']) ||
                roles.some((r) =>
                    ['TelecomAdmin', 'TelecomBackOffice', 'TelecomShowroom', 'TelecomCallCenter'].includes(r));
            const canActivateLine =
                StorageManager.hasAnyPermission?.(perms, ['telecom.line.activate']) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(r));
            const canMigrateLine =
                StorageManager.hasAnyPermission?.(perms, ['telecom.line.migrate']) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomBackOffice'].includes(r));
            const canChangeGsmLine =
                StorageManager.hasAnyPermission?.(perms, ['telecom.line.change_gsm']) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomBackOffice', 'TelecomShowroom'].includes(r));
            const canSimSwapLine =
                StorageManager.hasAnyPermission?.(perms, [
                    'telecom.line.simswap_request',
                    'telecom.line.simswap',
                ]) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(r));
            const canChangeNumberLine =
                StorageManager.hasAnyPermission?.(perms, ['telecom.line.change_number_request']) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(r));
            const canTerminateLine =
                StorageManager.hasAnyPermission?.(perms, ['telecom.line.termination_request']) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(r));
            const canSuspensionLine =
                StorageManager.hasAnyPermission?.(perms, [
                    'telecom.line.suspension_request',
                    'telecom.line.suspension',
                ]) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(r));
            const canReconnectLine =
                StorageManager.hasAnyPermission?.(perms, [
                    'telecom.line.reconnect_request',
                    'telecom.line.reconnect',
                ]) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(r));
            const canRefundLine =
                StorageManager.hasAnyPermission?.(perms, [
                    'telecom.line.refund_request',
                    'telecom.line.refund',
                ]) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(r));
            const canCollectionLine =
                StorageManager.hasAnyPermission?.(perms, [
                    'telecom.line.collection_request',
                    'telecom.line.collection',
                ]) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomBackOffice'].includes(r));
            const canDeviceSaleLine =
                StorageManager.hasAnyPermission?.(perms, [
                    'telecom.device.sell_request',
                    'telecom.device.sell',
                ]) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(r));
            const canTakeOverLine =
                StorageManager.hasAnyPermission?.(perms, ['telecom.line.activate']) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(r));
            const canRechargeLine =
                StorageManager.hasAnyPermission?.(perms, ['telecom.line.recharge']) ||
                roles.some((r) => ['TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice'].includes(r));
            return {
                isStrictTelecom,
                canMutate,
                showFullColumns,
                canViewDecryptedPii,
                canQueryHlr,
                canCreateTicket,
                canToggleVas,
                canActivateLine,
                canMigrateLine,
                canChangeGsmLine,
                canSimSwapLine,
                canChangeNumberLine,
                canTerminateLine,
                canSuspensionLine,
                canReconnectLine,
                canRefundLine,
                canCollectionLine,
                canDeviceSaleLine,
                canTakeOverLine,
                canRechargeLine,
            };
        };

        const gridAccess = resolveTelecomCustomerGridAccess();

        const telSubDisplayColorFromLookup = (nameAr) => {
            if (nameAr == null || nameAr === '') return null;
            const lt = (state.telecomLineTypes || []).find((x) => (x.nameAr || x.NameAr) === nameAr);
            if (!lt) return null;
            return lt.displayColor || lt.DisplayColor || null;
        };

        const buildSubscriptionTypeCheckboxFilterDataSource = () => {
            const seen = new Map();
            for (const row of state.mainData || []) {
                const name = row.subscriptionTypeName;
                const mapKey = name === undefined || name === null ? '__null__' : String(name);
                if (seen.has(mapKey)) continue;
                const color =
                    row.subscriptionTypeDisplayColor ||
                    row.SubscriptionTypeDisplayColor ||
                    telSubDisplayColorFromLookup(name);
                seen.set(mapKey, {
                    subscriptionTypeName: name === undefined ? null : name,
                    subscriptionTypeDisplayColor: color || null,
                });
            }
            return Array.from(seen.values()).sort((a, b) =>
                String(a.subscriptionTypeName ?? '').localeCompare(String(b.subscriptionTypeName ?? ''), 'ar', {
                    sensitivity: 'base',
                    numeric: true,
                })
            );
        };

        /** Map display name (Arabic) → color; Syncfusion checkbox filter often omits extra fields on items, so we paint `.e-list-text` after open. */
        const buildSubscriptionTypeNameToColorMap = () => {
            const map = new Map();
            for (const row of state.mainData || []) {
                const name = row.subscriptionTypeName;
                const key = name == null || name === '' ? '' : String(name).trim();
                const color =
                    row.subscriptionTypeDisplayColor ||
                    row.SubscriptionTypeDisplayColor ||
                    telSubDisplayColorFromLookup(name);
                if (color && !map.has(key)) map.set(key, color);
            }
            for (const lt of state.telecomLineTypes || []) {
                const ar = (lt.nameAr ?? lt.NameAr) == null ? '' : String(lt.nameAr ?? lt.NameAr).trim();
                const col = lt.displayColor || lt.DisplayColor;
                if (ar && col && !map.has(ar)) map.set(ar, col);
            }
            return map;
        };

        const paintSubscriptionTypeFilterChecklistColors = () => {
            const dlg = document.querySelector('.e-checkboxfilter.e-filter-popup');
            if (!dlg) return;
            const nameToColor = buildSubscriptionTypeNameToColorMap();
            dlg.querySelectorAll('.e-list-text').forEach((el) => {
                const txt = (el.textContent || '').trim();
                if (!txt) return;
                if (/^select all$/i.test(txt)) return;
                const color = nameToColor.get(txt);
                if (color) el.style.color = color;
            });
        };

        const scheduleSubscriptionTypeFilterPaint = () => {
            paintSubscriptionTypeFilterChecklistColors();
            window.requestAnimationFrame(() => paintSubscriptionTypeFilterChecklistColors());
            window.setTimeout(() => paintSubscriptionTypeFilterChecklistColors(), 0);
            window.setTimeout(() => paintSubscriptionTypeFilterChecklistColors(), 80);
        };

        /** Color group caption text when grouped by `subscriptionTypeName` (avoids fragile `captionTemplate` / globals). */
        const paintSubscriptionTypeGroupCaptionCells = () => {
            try {
                const grid = mainGrid.obj;
                if (!grid || !mainGridRef.value) return;
                let subHeader = telecomT('primaryLine.lineType');
                if (typeof grid.getColumnByField === 'function') {
                    const col = grid.getColumnByField('subscriptionTypeName');
                    if (col && col.headerText) subHeader = String(col.headerText).trim();
                }
                const map = buildSubscriptionTypeNameToColorMap();
                const esc = (s) =>
                    String(s ?? '')
                        .replace(/&/g, '&amp;')
                        .replace(/</g, '&lt;')
                        .replace(/>/g, '&gt;')
                        .replace(/"/g, '&quot;');

                mainGridRef.value.querySelectorAll('.e-groupcaption, td.e-groupcaption').forEach((cell) => {
                    if (cell.closest('.e-detailrow')) return;
                    if (cell.querySelector('span[data-mcq-sub-group-caption="1"]')) return;
                    const full = (cell.textContent || '').replace(/\s+/g, ' ').trim();
                    const idxColon = full.indexOf(':');
                    if (idxColon < 0) return;
                    const header = full.slice(0, idxColon).trim();
                    if (header !== subHeader) return;
                    const after = full.slice(idxColon + 1).trim();
                    const m = after.match(/^(.+?)\s*[—\-]\s*(.+)$/);
                    if (!m) return;
                    const keyStr = m[1].trim();
                    const tail = m[2].trim();
                    if (!keyStr) return;
                    const color = map.get(keyStr) || telSubDisplayColorFromLookup(keyStr);
                    if (!color) return;
                    cell.innerHTML =
                        esc(header) +
                        ': <span data-mcq-sub-group-caption="1" style="color:' +
                        esc(color) +
                        '">' +
                        esc(keyStr) +
                        '</span> — ' +
                        esc(tail);
                });
            } catch (e) {
                console.warn('paintSubscriptionTypeGroupCaptionCells', e);
            }
        };

        const selectCustomerRowById = (customerId) => {
            if (!mainGrid.obj || !customerId) return;
            const grid = mainGrid.obj;

            const sortedIndex = () => {
                const sorted = [...state.mainData].sort((a, b) => {
                    const da = new Date(a.createdAtUtc || 0).getTime();
                    const db = new Date(b.createdAtUtc || 0).getTime();
                    return db - da;
                });
                return sorted.findIndex((row) => row.id === customerId);
            };

            let idx =
                typeof grid.getRowIndexByPrimaryKey === 'function'
                    ? grid.getRowIndexByPrimaryKey(customerId)
                    : -1;
            if (typeof idx !== 'number' || idx < 0) {
                idx = sortedIndex();
            }
            if (idx < 0) return;

            const pageSize = grid.pageSettings?.pageSize || 50;
            const targetPage = Math.floor(idx / pageSize) + 1;
            grid.pageSettings.currentPage = targetPage;
            grid.refresh();

            window.setTimeout(() => {
                try {
                    const view = typeof grid.getCurrentViewRecords === 'function' ? grid.getCurrentViewRecords() : [];
                    const pageIdx = Array.isArray(view) ? view.findIndex((r) => r.id === customerId) : -1;
                    if (pageIdx >= 0) {
                        grid.selectRow(pageIdx);
                    } else {
                        const rowInPage = idx - (targetPage - 1) * pageSize;
                        grid.selectRow(rowInPage);
                    }
                    if (mainGridRef.value) {
                        mainGridRef.value.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                    }
                } catch (e) {
                    console.warn('selectCustomerRowById', e);
                }
            }, 450);
        };

        const subscriberTypeCellHtml = (row) => {
            if (!row || typeof row !== 'object') return '—';
            const html = window.TelecomUiBadges?.subscriberKind?.(row.subscriberType);
            if (typeof html === 'string' && html.length > 0) return html;
            return escapeHtml(String(row.subscriberType || '—'));
        };

        const escapeHtml = (s) =>
            String(s ?? '')
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;');

        const mainGrid = {
            obj: null,
            create: async (dataSource) => {
                mainGrid.obj = new ej.grids.Grid({
                    height: getDashminGridHeight(),
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowGrouping: gridAccess.showFullColumns,
                    groupSettings: {
                        columns: gridAccess.showFullColumns ? ['customerCategoryName'] : [],
                    },
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    filterSettings: { type: 'CheckBox' },
                    queryCellInfo: (args) => {
                        if (args.column?.field === 'subscriberType' && args.cell && args.data) {
                            args.cell.innerHTML = subscriberTypeCellHtml(args.data);
                        }
                    },
                    actionBegin: (args) => {
                        if (args.requestType !== 'filterbeforeopen') return;
                        const colField =
                            args.columnName || args.currentFilteringColumn || args.column?.field || args.filterModel?.field;
                        if (colField !== 'subscriptionTypeName') return;
                        const opts = args.filterModel?.options;
                        if (!opts) return;
                        opts.dataSource = buildSubscriptionTypeCheckboxFilterDataSource();
                        if (Array.isArray(opts.filteredColumns)) {
                            opts.filteredColumns = opts.filteredColumns.filter(
                                (col) => col && col.field === 'subscriptionTypeName'
                            );
                        }
                    },
                    actionComplete: (args) => {
                        const rt = args.requestType;
                        if (rt === 'filterafteropen') {
                            scheduleSubscriptionTypeFilterPaint();
                            window.setTimeout(scheduleSubscriptionTypeFilterPaint, 150);
                            window.setTimeout(scheduleSubscriptionTypeFilterPaint, 400);
                        }
                        if (rt === 'filterchoicerequest') {
                            paintSubscriptionTypeFilterChecklistColors();
                        }
                        if (rt === 'grouping' || rt === 'ungrouping' || rt === 'reorderGrouping') {
                            window.setTimeout(() => paintSubscriptionTypeGroupCaptionCells(), 0);
                            window.setTimeout(() => paintSubscriptionTypeGroupCaptionCells(), 120);
                        }
                    },
                    sortSettings: { columns: [{ field: 'createdAtUtc', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'number', headerText: 'Number', width: 150, minWidth: 150 },
                        { field: 'name', headerText: 'Name', width: 200, minWidth: 200 },
                        {
                            field: 'subscriberType',
                            headerText: telecomT('grid.type', 'Type'),
                            width: 148,
                            minWidth: 120,
                            textAlign: 'Center',
                            clipMode: 'Clip',
                            allowTextWrap: false,
                            disableHtmlEncode: false,
                        },
                        ...(gridAccess.showFullColumns
                            ? [{ field: 'telecomSubscriptionLineCount', headerText: telecomT('grid.lineCount', '# lines'), width: 72, minWidth: 64 }]
                            : []),
                        {
                            field: 'primaryMsisdn',
                            headerText: gridAccess.showFullColumns ? telecomT('grid.msisdns', 'MSISDNs') : 'MSISDN',
                            width: gridAccess.showFullColumns ? 260 : 140,
                            minWidth: gridAccess.showFullColumns ? 160 : 120,
                            template: '#msisdnsSummaryTemplate',
                        },
                        {
                            field: 'subscriptionTypeName',
                            headerText: telecomT('grid.lineType', 'Line type'),
                            width: 130,
                            minWidth: 100,
                            filter: { type: 'CheckBox', itemTemplate: '#mcqCustomerSubTypeFilterItem' },
                            template: '#subscriptionTypeNameTemplate',
                        },
                        { field: 'phoneNumber', headerText: 'Phone', width: 200, minWidth: 200 },
                        ...(gridAccess.showFullColumns
                            ? [
                                { field: 'customerGroupName', headerText: 'Group', width: 200, minWidth: 200 },
                                { field: 'customerCategoryName', headerText: 'Category', width: 200, minWidth: 200 },
                                { field: 'street', headerText: 'Street', width: 200, minWidth: 200 },
                                { field: 'emailAddress', headerText: 'Email', width: 200, minWidth: 200 },
                            ]
                            : []),
                        { field: 'createdAtUtc', headerText: 'Created At UTC', width: 150, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: gridAccess.canMutate
                        ? [
                            'ExcelExport', 'Search',
                            { type: 'Separator' },
                            { text: 'Add', tooltipText: 'Add', prefixIcon: 'e-add', id: 'AddCustom' },
                            { text: 'Edit', tooltipText: 'Edit', prefixIcon: 'e-edit', id: 'EditCustom' },
                            { text: 'Delete', tooltipText: 'Delete', prefixIcon: 'e-delete', id: 'DeleteCustom' },
                            { type: 'Separator' },
                            { text: 'Manage Contact', tooltipText: 'Manage Contact', id: 'ManageContactCustom' },
                        ]
                        : ['ExcelExport', 'Search'],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        if (gridAccess.canMutate && mainGrid.obj.toolbarModule) {
                            const ids = ['EditCustom', 'DeleteCustom', 'ManageContactCustom'];
                            mainGrid.obj.toolbarModule.enableItems(ids, false);
                        }
                        const fit = ['name', 'primaryMsisdn', 'subscriptionTypeName', 'phoneNumber', 'createdAtUtc'];
                        if (gridAccess.showFullColumns) {
                            fit.splice(1, 0, 'telecomSubscriptionLineCount');
                            fit.push('customerGroupName', 'customerCategoryName', 'street', 'emailAddress');
                        }
                        mainGrid.obj.autoFitColumns(fit);
                        paintSubscriptionTypeGroupCaptionCells();
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (!gridAccess.canMutate || !mainGrid.obj.toolbarModule) return;
                        const ids = ['EditCustom', 'DeleteCustom', 'ManageContactCustom'];
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(ids, true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(ids, false);
                        }
                    },
                    rowDeselected: () => {
                        if (!gridAccess.canMutate || !mainGrid.obj.toolbarModule) return;
                        const ids = ['EditCustom', 'DeleteCustom', 'ManageContactCustom'];
                        if (mainGrid.obj.getSelectedRecords().length == 1) {
                            mainGrid.obj.toolbarModule.enableItems(ids, true);
                        } else {
                            mainGrid.obj.toolbarModule.enableItems(ids, false);
                        }
                    },
                    rowSelecting: () => {
                        if (mainGrid.obj.getSelectedRecords().length) {
                            mainGrid.obj.clearSelection();
                        }
                    },
                    toolbarClick: async (args) => {
                        if (args.item.id === 'MainGrid_excelexport') {
                            mainGrid.obj.excelExport();
                        }

                        if (args.item.id === 'AddCustom') {
                            state.deleteMode = false;
                            state.mainTitle = MacquiresUiI18n.mb('customer','add');
                            resetFormState();
                            state.customerOnboardingActive = true;
                            state.customerOnboardingStep = 0;
                            state.addFlowBootstrapTelecom = false;
                            mainModal.obj.show();
                        }

                        if (args.item.id === 'EditCustom') {
                            state.deleteMode = false;
                            state.customerOnboardingActive = false;
                            state.customerOnboardingStep = 0;
                            state.addFlowBootstrapTelecom = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = MacquiresUiI18n.mb('customer','edit');
                                state.id = selectedRecord.id ?? '';
                                state.number = selectedRecord.number ?? '';
                                state.name = selectedRecord.name ?? '';
                                state.customerGroupId = selectedRecord.customerGroupId ?? null;
                                state.customerCategoryId = selectedRecord.customerCategoryId ?? null;
                                state.subscriberType = selectedRecord.subscriberType === 'Corporate' ? 1 : 0;
                                state.nationalId = '';
                                state.nationalIdPersisted = selectedRecord.nationalId ?? '';
                                state.nationalIdRevealed = false;
                                state.nationalIdMasked = '';
                                state.dateOfBirth = selectedRecord.dateOfBirth ? selectedRecord.dateOfBirth.substring(0, 10) : '';
                                state.commercialRegistration = selectedRecord.commercialRegistration ?? '';
                                state.taxNumber = selectedRecord.taxNumber ?? '';
                                state.authorizedSignatory = selectedRecord.authorizedSignatory ?? '';
                                state.description = selectedRecord.description ?? '';
                                state.street = selectedRecord.street ?? '';
                                state.city = selectedRecord.city ?? '';
                                state.state = selectedRecord.state ?? '';
                                state.zipCode = selectedRecord.zipCode ?? '';
                                state.country = selectedRecord.country ?? '';
                                state.phoneNumber = selectedRecord.phoneNumber ?? '';
                                state.faxNumber = selectedRecord.faxNumber ?? '';
                                state.emailAddress = selectedRecord.emailAddress ?? '';
                                state.website = selectedRecord.website ?? '';
                                state.whatsApp = selectedRecord.whatsApp ?? '';
                                state.linkedIn = selectedRecord.linkedIn ?? '';
                                state.facebook = selectedRecord.facebook ?? '';
                                state.instagram = selectedRecord.instagram ?? '';
                                state.twitterX = selectedRecord.twitterX ?? '';
                                state.tikTok = selectedRecord.tikTok ?? '';
                                state.telecomMsisdn = selectedRecord.primaryMsisdn ?? '';
                                state.telecomMsisdnInitial = state.telecomMsisdn;
                                const defId =
                                    (state.telecomLineTypes || []).find((x) => x.isDefault)?.id ||
                                    TEL_SUB_DEFAULT_PREPAID_ID;
                                state.telecomSubscriptionTypeId =
                                    selectedRecord.subscriptionTypeId || defId;
                                state.telecomSubscriptionTypeIdInitial = state.telecomSubscriptionTypeId;
                                state.telecomSubscriptionRows = Array.isArray(selectedRecord.telecomLines)
                                    ? selectedRecord.telecomLines
                                    : [];
                                if (
                                    gridAccess.showFullColumns &&
                                    state.id &&
                                    (!state.telecomSubscriptionRows || state.telecomSubscriptionRows.length === 0)
                                ) {
                                    try {
                                        const r = await AxiosManager.get(
                                            '/Customer/GetCustomerList?isDeleted=false&customerId=' +
                                                encodeURIComponent(state.id)
                                        );
                                        const row0 = r?.data?.content?.data?.[0];
                                        state.telecomSubscriptionRows = Array.isArray(row0?.telecomLines)
                                            ? row0.telecomLines
                                            : [];
                                    } catch {
                                        state.telecomSubscriptionRows = [];
                                    }
                                }
                                const hlrIds = [...new Set(state.telecomSubscriptionRows.map((s) => (s.subscriberProfileId || '').trim()).filter(Boolean))];
                                state.hlrSubscriberProfileId = hlrIds[0] || '';
                                state.hlrLiveData = null;
                                if (state.telecomSubscriptionRows.length > 0) {
                                    const prim = state.telecomSubscriptionRows.find(s => s.isPrimaryLine) || state.telecomSubscriptionRows[0];
                                    state.telecomSubscriptionId = prim.telecomSubscriptionId;
                                    state.telecomSubscriptionIdInitial = prim.telecomSubscriptionId;
                                    syncTelecomLineTypeFromSelectedSubscription();
                                } else {
                                    state.telecomSubscriptionId = '';
                                    state.telecomSubscriptionIdInitial = '';
                                }
                                state.telecomSubscriptionTypeIdInitial = state.telecomSubscriptionTypeId;
                                await loadCustomer360(state.id);
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'DeleteCustom') {
                            state.deleteMode = true;
                            state.customerOnboardingActive = false;
                            state.customerOnboardingStep = 0;
                            state.addFlowBootstrapTelecom = false;
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.mainTitle = MacquiresUiI18n.mb('customer','delete');
                                state.id = selectedRecord.id ?? '';
                                mainModal.obj.show();
                            }
                        }

                        if (args.item.id === 'ManageContactCustom') {
                            if (mainGrid.obj.getSelectedRecords().length) {
                                const selectedRecord = mainGrid.obj.getSelectedRecords()[0];
                                state.id = selectedRecord.id ?? '';
                                state.manageContactTitle = (typeof MacquiresUiI18n !== 'undefined' && MacquiresUiI18n.mb)
                                    ? MacquiresUiI18n.mb('manageContactModal', 'title')
                                    : 'Manage Contact';
                                await methods.populateSecondaryData(state.id);
                                secondaryGrid.refresh();
                                manageContactModal.obj.show();
                            }
                        }
                    }
                });
                mainGrid.obj.appendTo(mainGridRef.value);
            },
            refresh: () => {
                mainGrid.obj.setProperties({ dataSource: state.mainData });
            }
        };

        const mainModal = {
            obj: null,
            create: () => {
                mainModal.obj = new bootstrap.Modal(mainModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const manageContactModal = {
            obj: null,
            create: () => {
                manageContactModal.obj = new bootstrap.Modal(manageContactModalRef.value, {
                    backdrop: 'static',
                    keyboard: false
                });
            }
        };

        const secondaryGrid = {
            obj: null,
            create: async (dataSource) => {
                secondaryGrid.obj = new ej.grids.Grid({
                    height: getDashminGridHeight(),
                    dataSource: dataSource,
                    allowFiltering: true,
                    allowSorting: true,
                    allowSelection: true,
                    allowGrouping: false,
                    allowTextWrap: true,
                    allowResizing: true,
                    allowPaging: true,
                    allowExcelExport: true,
                    editSettings: {
                        allowEditing: gridAccess.canMutate,
                        allowAdding: gridAccess.canMutate,
                        allowDeleting: gridAccess.canMutate,
                        showDeleteConfirmDialog: true,
                        mode: 'Normal',
                        allowEditOnDblClick: gridAccess.canMutate,
                    },
                    filterSettings: { type: 'CheckBox' },
                    sortSettings: { columns: [{ field: 'createdAtUtc', direction: 'Descending' }] },
                    pageSettings: { currentPage: 1, pageSize: 50, pageSizes: ["10", "20", "50", "100", "200", "All"] },
                    selectionSettings: { persistSelection: true, type: 'Single' },
                    autoFit: true,
                    showColumnMenu: true,
                    gridLines: 'Horizontal',
                    columns: [
                        { type: 'checkbox', width: 60 },
                        {
                            field: 'id', isPrimaryKey: true, headerText: 'Id', visible: false
                        },
                        { field: 'name', headerText: 'Name', width: 200, minWidth: 200, validationRules: { required: true } },
                        { field: 'jobTitle', headerText: 'Job Title', width: 200, minWidth: 200, validationRules: { required: true } },
                        { field: 'phoneNumber', headerText: 'Phone', width: 200, minWidth: 200, validationRules: { required: true } },
                        { field: 'emailAddress', headerText: 'Email', width: 200, minWidth: 200, validationRules: { required: true } },
                        { field: 'description', headerText: 'Description', width: 400, minWidth: 400 },
                        { field: 'createdAtUtc', headerText: 'Created At UTC', width: 150, format: 'yyyy-MM-dd HH:mm' }
                    ],
                    toolbar: gridAccess.canMutate
                        ? ['ExcelExport', 'Add', 'Edit', 'Delete', 'Update', 'Cancel', 'Search']
                        : ['ExcelExport', 'Search'],
                    beforeDataBound: () => { },
                    dataBound: function () {
                        if (gridAccess.canMutate && secondaryGrid.obj.toolbarModule) {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit', 'Delete'], false);
                        }
                        secondaryGrid.obj.autoFitColumns(['name', 'jobTitle', 'phoneNumber', 'emailAddress', 'description', 'createdAtUtc']);
                    },
                    excelExportComplete: () => { },
                    rowSelected: () => {
                        if (!gridAccess.canMutate || !secondaryGrid.obj.toolbarModule) return;
                        if (secondaryGrid.obj.getSelectedRecords().length == 1) {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit', 'Delete'], true);
                        } else {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit', 'Delete'], false);
                        }
                    },
                    rowDeselected: () => {
                        if (!gridAccess.canMutate || !secondaryGrid.obj.toolbarModule) return;
                        if (secondaryGrid.obj.getSelectedRecords().length == 1) {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit', 'Delete'], true);
                        } else {
                            secondaryGrid.obj.toolbarModule.enableItems(['Edit', 'Delete'], false);
                        }
                    },
                    rowSelecting: () => {
                        if (secondaryGrid.obj.getSelectedRecords().length) {
                            secondaryGrid.obj.clearSelection();
                        }
                    },
                    actionComplete: async (args) => {
                        if (args.requestType === 'save' && args.action === 'add') {
                            console.log(state);
                            const response = await services.createSecondaryData(
                                args.data.name, args.data.jobTitle, args.data.phoneNumber, args.data.emailAddress, args.data.description, state.id
                            );
                            await methods.populateSecondaryData(state.id);
                            secondaryGrid.refresh();
                            Swal.fire({
                                icon: 'success',
                                title: uiModalT('swal_saveSuccessful', 'Save Successful'),
                                timer: 2000,
                                showConfirmButton: false
                            });
                        }
                        if (args.requestType === 'save' && args.action === 'edit') {
                            const response = await services.updateSecondaryData(
                                args.data.id, args.data.name, args.data.jobTitle, args.data.phoneNumber, args.data.emailAddress, args.data.description, state.id
                            );
                            await methods.populateSecondaryData(state.id);
                            secondaryGrid.refresh();
                            Swal.fire({
                                icon: 'success',
                                title: uiModalT('swal_updateSuccessful', 'Update Successful'),
                                timer: 2000,
                                showConfirmButton: false
                            });
                        }
                        if (args.requestType === 'delete') {
                            const response = await services.deleteSecondaryData(
                                args.data[0].id
                            );
                            await methods.populateSecondaryData(state.id);
                            secondaryGrid.refresh();
                            Swal.fire({
                                icon: 'success',
                                title: uiModalT('swal_deleteSuccessful', 'Delete Successful'),
                                timer: 2000,
                                showConfirmButton: false
                            });
                        }
                    }
                });
                secondaryGrid.obj.appendTo(secondaryGridRef.value);
            },
            refresh: () => {
                secondaryGrid.obj.setProperties({ dataSource: state.secondaryData });
            }
        };

        const refreshManageContactModalTitle = () => {
            if (typeof MacquiresUiI18n !== 'undefined' && MacquiresUiI18n.mb) {
                state.manageContactTitle = MacquiresUiI18n.mb('manageContactModal', 'title');
            }
        };

        const refreshGridTelecomHeaders = () => {
            const g = mainGrid.obj;
            if (!g || typeof g.getColumnByField !== 'function') return;
            const setH = (field, key, fb) => {
                const c = g.getColumnByField(field);
                if (c) c.headerText = telecomT(key, fb);
            };
            setH('subscriberType', 'grid.type', 'Type');
            setH('telecomSubscriptionLineCount', 'grid.lineCount', '# lines');
            setH('primaryMsisdn', 'grid.msisdns', 'MSISDNs');
            setH('subscriptionTypeName', 'grid.lineType', 'Line type');
            if (typeof g.refreshHeader === 'function') g.refreshHeader();
        };

        const refreshTelecomCustomerListI18n = async () => {
            try {
                await window.TelecomI18n?.ensureLoaded?.();
                window.TelecomI18n?.refresh?.();
                const title = telecomT('pageTitle', 'Customers');
                if (title) document.title = title;
                refreshGridTelecomHeaders();
                if (mainGrid.obj && typeof mainGrid.obj.refresh === 'function') {
                    mainGrid.obj.refresh();
                }
            } catch (_) { /* ignore */ }
        };

        Vue.onMounted(async () => {
            document.documentElement.addEventListener('syriatel-locale-changed', refreshManageContactModalTitle);
            document.documentElement.addEventListener('syriatel-ui-modals-loaded', refreshManageContactModalTitle);
            document.documentElement.addEventListener('syriatel-locale-changed', refreshTelecomCustomerListI18n);
            refreshManageContactModalTitle();
            await refreshTelecomCustomerListI18n();
            try {
                await SecurityManager.authorizePage(['Customers', 'TelecomAdmin', 'TelecomShowroom', 'TelecomBackOffice', 'TelecomCallCenter', 'TelecomManagement']);
                await SecurityManager.validateToken();

                await Promise.all([
                    methods.populateTelecomLineTypes(),
                    methods.populateMainData(),
                    methods.populateCustomerGroupListLookupData(),
                    methods.populateCustomerCategoryListLookupData(),
                ]);
                await mainGrid.create(state.mainData);
                customerGroupListLookup.create();
                customerCategoryListLookup.create();
                nameText.create();
                numberText.create();
                streetText.create();
                cityText.create();
                stateText.create();
                zipCodeText.create();
                countryText.create();
                phoneNumberText.create();
                faxNumberText.create();
                emailAddressText.create();
                websiteText.create();
                whatsAppText.create();
                linkedInText.create();
                facebookText.create();
                instagramText.create();
                twitterXText.create();
                tikTokText.create();
                mainModal.create();
                manageContactModal.create();
                secondaryGrid.create([]);
                const pageQs = new URLSearchParams(window.location.search);
                const deepCustomerId = pageQs.get('customerId');
                if (pageQs.get('embed') === '1') {
                    state.isEmbedded = true;
                }
                if (deepCustomerId) {
                    window.setTimeout(() => selectCustomerRowById(deepCustomerId), 350);
                }
                if (pageQs.get('action') === 'new') {
                    window.setTimeout(() => {
                        state.deleteMode = false;
                        state.mainTitle = typeof MacquiresUiI18n !== 'undefined' ? MacquiresUiI18n.mb('customer','add') : uiModalT('customer.add', 'Add Customer');
                        resetFormState();
                        state.customerOnboardingActive = true;
                        state.customerOnboardingStep = 0;
                        state.addFlowBootstrapTelecom = false;
                        if (mainModal.obj) {
                            mainModal.obj.show();
                        }
                    }, 400);
                }
            } catch (e) {
                console.error('page init error:', e);
            } finally {
                hideSpinnerAndShowContent();
            }
        });

        const onCustomerTelecomSubscriptionChange = () => {
            const row = (state.telecomSubscriptionRows || []).find(
                (s) => s.telecomSubscriptionId === state.telecomSubscriptionId
            );
            if (row && (row.subscriptionTypeCode || row.subscriptionTypeName)) {
                const lt = (state.telecomLineTypes || []).find(
                    (x) => x.code === row.subscriptionTypeCode || x.nameAr === row.subscriptionTypeName
                );
                if (lt && lt.id) {
                    state.telecomSubscriptionTypeId = lt.id;
                }
            }
        };

        const vasPanels = Vue.reactive({});
        const vasPanelLoading = Vue.reactive({});
        const vasPanelError = Vue.reactive({});
        const vasToggling = Vue.reactive({});

        const vasToggleKey = (msisdn, serviceCode) => `${(msisdn || '').trim()}:${(serviceCode || '').trim()}`;

        const isVasToggling = (msisdn, serviceCode) => !!vasToggling[vasToggleKey(msisdn, serviceCode)];

        const loadVasPanelForMsisdn = async (msisdn) => {
            const m = (msisdn || '').trim();
            if (!m || !gridAccess.canToggleVas) return;
            vasPanelLoading[m] = true;
            vasPanelError[m] = '';
            try {
                const res = await AxiosManager.get(
                    '/Vas/GetSubscriberVasPanel?msisdn=' + encodeURIComponent(m),
                    {}
                );
                vasPanels[m] = res?.data?.content ?? { services: [] };
            } catch (e) {
                vasPanels[m] = { services: [] };
                vasPanelError[m] = e?.response?.data?.error?.message ?? e?.response?.data?.message ?? telecomT('swal.vasLoadFailed');
            } finally {
                vasPanelLoading[m] = false;
            }
        };

        const toggleVasService = async (sub, svc, event) => {
            const msisdn = (sub?.msisdn || '').trim();
            const code = (svc?.serviceCode || '').trim();
            if (!msisdn || !code) return;
            const wantActive = !!event?.target?.checked;
            const key = vasToggleKey(msisdn, code);
            if (vasToggling[key]) {
                if (event?.target) event.target.checked = !wantActive;
                return;
            }
            vasToggling[key] = true;
            try {
                const result =
                    typeof TelecomVasToggle !== 'undefined'
                        ? await TelecomVasToggle.toggle(AxiosManager, {
                              msisdn,
                              serviceCode: code,
                              activate: wantActive,
                          })
                        : null;
                if (result?.ok) {
                    await loadVasPanelForMsisdn(msisdn);
                    if (wantActive) {
                        Swal.fire({
                            icon: 'success',
                            title: telecomT('swal.activated'),
                            text: result.operationNumber
                                ? telecomT('swal.activatedOpPrefix') + ' ' + result.operationNumber
                                : '',
                            timer: 2000,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    if (event?.target) event.target.checked = !wantActive;
                    Swal.fire({ icon: 'error', title: telecomT('swal.operationFailed'), text: '' });
                }
            } catch (e) {
                if (event?.target) event.target.checked = !wantActive;
                const isBrv =
                    typeof TelecomVasToggle !== 'undefined'
                        ? TelecomVasToggle.isBusinessRuleViolation(e)
                        : e?.response?.data?.error?.name === 'BusinessRuleViolationException';
                const msg =
                    typeof TelecomVasToggle !== 'undefined'
                        ? TelecomVasToggle.pickError(e)
                        : e?.response?.data?.error?.message ?? e?.response?.data?.message ?? e?.message ?? '';
                Swal.fire({
                    icon: isBrv ? 'warning' : 'error',
                    title: isBrv ? telecomT('swal.activationFailed') : telecomT('swal.error'),
                    text: msg,
                });
            } finally {
                vasToggling[key] = false;
            }
        };

        const lineProcessing = Vue.reactive({});
        const lineActionModal = Vue.reactive({
            sub: null,
            busy: false,
            msisdnAssetId: '',
            productOfferingId: '',
            simIccid: '',
            offerings: [],
            poolNumbers: [],
            activationChannel: 0,
            dealerCode: '',
            paymentReference: '',
            paymentAmount: '',
            paymentChannel: 0,
            paymentCashierLocked: false,
            cashierFetchBusy: false,
            activateKycDocumentReferenceId: '',
            activateIdentityFile: null,
            activateKycUploadBusy: false,
            activateKycUploadError: '',
            migrationProducts: [],
            currentProductName: '',
            takeoverSearchNationalId: '',
            takeoverSearchPhone: '',
            takeoverResults: [],
            takeoverTargetCustomerId: '',
            takeoverTargetProfileId: '',
            takeoverTransferReason: '',
            takeoverDepositPolicy: 1,
            takeoverIdentityFile: null,
            takeoverSearchBusy: false,
            migrateOfferDetail: null,
            migrateOfferDetailBusy: false,
            activateOfferDetail: null,
            activateOfferDetailBusy: false,
            mgrProrationPreview: null,
            mgrProrationBusy: false,
            notes: '',
            simReplacementReason: '',
            simLostOrStolen: false,
            simIdentityFile: null,
            cnTargetMsisdnAssetId: '',
            cnNumberChangeReason: '',
            cnChangeMode: 'Internal',
            cnPortInMsisdn: '',
            cnDonorOperatorCode: '',
            cnPortInReference: '',
            cnPremiumFeeAmount: '',
            cnPoolNumbers: [],
            cnPoolBusy: false,
            cnRequiresBackOffice: false,
            cnPaymentFile: null,
            trmTerminationType: 'Voluntary',
            trmTerminationReason: '',
            trmRetentionOfferOutcome: 'Declined',
            trmRequiresBackOffice: false,
            trmIdentityFile: null,
            susSuspensionType: 'CustomerRequest',
            susSuspensionReason: '',
            susBarringLevel: 'Full',
            susAutoReconnectEnabled: false,
            susEndDateLocal: '',
            susRequiresBackOffice: false,
            bssPaymentReference: '',
            bssSecurityTicketId: '',
            bssDocumentNumber: '',
            bssRegulatoryFile: null,
            bssKycDocumentReferenceId: '',
            bssIdentityFile: null,
            bssOriginalTransactionRef: '',
            bssPayoutDestination: '',
            rcnReconnectReason: '',
            rcnClearanceType: 'Customer',
            rcnPaymentReference: '',
            rcnSecurityTicketId: '',
            rcnDocumentNumber: '',
            rcnRegulatoryFile: null,
            rcnKycDocumentReferenceId: '',
            rcnFraudClearanceConfirmed: false,
            rcnRequiresBackOffice: false,
            rcnIdentityFile: null,
            rcnEligibility: null,
            rcnEligibilityBusy: false,
            rfdRefundType: 'Deposit',
            rfdRefundMethod: 'CreditNote',
            rfdRefundAmount: '',
            rfdRefundReason: '',
            rfdRequiresBackOffice: false,
            rfdIdentityFile: null,
            rfdDepositSnapshot: null,
            rfdWalletSnapshot: null,
            bdrCollectionAction: 'PaymentRecorded',
            bdrDunningStage: 'Reminder1',
            bdrCollectedAmount: '',
            bdrWriteOffAmount: '',
            bdrPaymentReference: '',
            bdrAgencyReference: '',
            bdrPaymentPlanMonths: '',
            bdrSupervisorConfirmed: false,
            bdrRequiresBackOffice: false,
            bdrEligibility: null,
            bdrEligibilityBusy: false,
            cgtTargets: [],
            cgtTargetsBusy: false,
            cgtCurrentTypeLabel: '',
            cgtSourceTypeCode: '',
            cgtSourceTypeId: '',
            cgtTargetTypeId: '',
            cgtMigrationReason: '',
            cgtMigrationPath: 'Standard',
            cgtOffers: [],
            cgtOffersBusy: false,
            cgtProductOfferingId: '',
            devInventoryId: '',
            devSaleType: 'Cash',
            devInstallmentPlanId: '',
            devDevices: [],
            devPlans: [],
            devDownPayment: '',
            devPaymentReference: '',
            devPaymentChannel: 0,
            devFinancingPreview: '',
            devRequiresFinance: false,
            devCreatedOperationId: '',
            supportIssueType: 0,
            supportNotes: '',
            supportBusy: false,
            vasCatalog: [],
            vasCatalogBusy: false,
            selectedVasCode: '',
            vasAction: 'Activate',
        });

        const isLineProcessing = (key) => !!lineProcessing[key || ''];

        const isLineSuspended = (sub) => {
            const st = String(sub?.profileOperationalStatus || sub?.ProfileOperationalStatus || '').toLowerCase();
            return st === 'suspended' || st === 'suspendedinbound' || st === 'suspendedoutbound';
        };

        const goToC360Wizard = (sub, wizardKind) => {
            const cid = state.id || state.customer360?.core?.id || state.customer360?.core?.Id;
            if (!cid || !sub?.subscriberProfileId) return;
            const qs = new URLSearchParams({
                customerId: cid,
                wizard: wizardKind,
                lineKey: `${sub.subscriberProfileId}|${sub.msisdn || ''}|${sub.msisdnAssetId || ''}`,
            });
            window.location.href = '/Telecom/Customer360Profile?' + qs.toString();
        };

        const hasAnyLineAction = () =>
            gridAccess.canMigrateLine
            || gridAccess.canChangeGsmLine
            || gridAccess.canSimSwapLine
            || gridAccess.canChangeNumberLine
            || gridAccess.canTerminateLine
            || gridAccess.canTakeOverLine
            || gridAccess.canSuspensionLine
            || gridAccess.canReconnectLine
            || gridAccess.canRefundLine
            || gridAccess.canCollectionLine
            || gridAccess.canDeviceSaleLine
            || gridAccess.canToggleVas;

        const isLineTerminated = (sub) =>
            String(sub?.profileOperationalStatus || sub?.ProfileOperationalStatus || '')
                .toLowerCase() === 'terminated';

        const isPremiumMsisdnCategory = (cat) => [1, 2, 3, 'Silver', 'Gold', 'Platinum'].includes(cat);

        const showBsModal = (id) => {
            const el = document.getElementById(id);
            if (el && window.bootstrap?.Modal) {
                window.bootstrap.Modal.getOrCreateInstance(el).show();
            }
        };

        const hideBsModal = (id) => {
            const el = document.getElementById(id);
            const inst = el && window.bootstrap?.Modal?.getInstance(el);
            if (inst) inst.hide();
        };

        const showLineActionError = (e) => {
            const errName = e?.response?.data?.error?.name;
            const msg = e?.response?.data?.error?.message ?? e?.response?.data?.message ?? e?.message ?? '';
            if (window.Swal) {
                Swal.fire({
                    icon: errName === 'BusinessRuleViolationException' ? 'warning' : 'error',
                    title: errName === 'BusinessRuleViolationException' ? telecomT('swal.executionFailed') : telecomT('swal.error'),
                    text: String(msg),
                });
            }
        };

        const resetLineActionModal = () => {
            lineActionModal.sub = null;
            lineActionModal.busy = false;
            lineActionModal.msisdnAssetId = '';
            lineActionModal.productOfferingId = '';
            lineActionModal.simIccid = '';
            lineActionModal.activationChannel = 0;
            lineActionModal.dealerCode = '';
            if (typeof ActivationChannelUi !== 'undefined') {
                ActivationChannelUi.applyDefaults(lineActionModal);
            }
            lineActionModal.paymentReference = '';
            lineActionModal.paymentAmount = '';
            lineActionModal.paymentChannel = 0;
            lineActionModal.paymentCashierLocked = false;
            lineActionModal.cashierFetchBusy = false;
            lineActionModal.migrationProducts = [];
            lineActionModal.currentProductName = '';
            lineActionModal.takeoverSearchNationalId = '';
            lineActionModal.takeoverSearchPhone = '';
            lineActionModal.takeoverResults = [];
            lineActionModal.takeoverTargetCustomerId = '';
            lineActionModal.takeoverTargetProfileId = '';
            lineActionModal.takeoverTransferReason = '';
            lineActionModal.takeoverDepositPolicy = 1;
            lineActionModal.takeoverIdentityFile = null;
            lineActionModal.simReplacementReason = '';
            lineActionModal.simLostOrStolen = false;
            lineActionModal.simIdentityFile = null;
            lineActionModal.cnTargetMsisdnAssetId = '';
            lineActionModal.cnNumberChangeReason = '';
            lineActionModal.cnChangeMode = 'Internal';
            lineActionModal.cnPortInMsisdn = '';
            lineActionModal.cnDonorOperatorCode = '';
            lineActionModal.cnPortInReference = '';
            lineActionModal.cnPremiumFeeAmount = '';
            lineActionModal.cnPoolNumbers = [];
            lineActionModal.cnPoolBusy = false;
            lineActionModal.cnRequiresBackOffice = false;
            lineActionModal.cnPaymentFile = null;
            lineActionModal.trmTerminationType = 'Voluntary';
            lineActionModal.trmTerminationReason = '';
            lineActionModal.trmRetentionOfferOutcome = 'Declined';
            lineActionModal.trmRequiresBackOffice = false;
            lineActionModal.trmIdentityFile = null;
            lineActionModal.susSuspensionType = 'CustomerRequest';
            lineActionModal.susSuspensionReason = '';
            lineActionModal.susBarringLevel = 'Full';
            lineActionModal.susAutoReconnectEnabled = false;
            lineActionModal.susEndDateLocal = '';
            lineActionModal.susRequiresBackOffice = false;
            lineActionModal.bssPaymentReference = '';
            lineActionModal.bssSecurityTicketId = '';
            lineActionModal.bssDocumentNumber = '';
            lineActionModal.bssRegulatoryFile = null;
            lineActionModal.bssKycDocumentReferenceId = '';
            lineActionModal.bssOriginalTransactionRef = '';
            lineActionModal.bssPayoutDestination = '';
            lineActionModal.rcnReconnectReason = '';
            lineActionModal.rcnClearanceType = 'Customer';
            lineActionModal.rcnPaymentReference = '';
            lineActionModal.rcnSecurityTicketId = '';
            lineActionModal.rcnDocumentNumber = '';
            lineActionModal.rcnRegulatoryFile = null;
            lineActionModal.rcnKycDocumentReferenceId = '';
            lineActionModal.rcnFraudClearanceConfirmed = false;
            lineActionModal.rcnRequiresBackOffice = false;
            lineActionModal.rcnIdentityFile = null;
            lineActionModal.rcnEligibility = null;
            lineActionModal.rfdRefundType = 'Deposit';
            lineActionModal.rfdRefundMethod = 'CreditNote';
            lineActionModal.rfdRefundAmount = '';
            lineActionModal.rfdRefundReason = '';
            lineActionModal.rfdRequiresBackOffice = false;
            lineActionModal.rfdIdentityFile = null;
            lineActionModal.bdrCollectionAction = 'PaymentRecorded';
            lineActionModal.bdrDunningStage = 'Reminder1';
            lineActionModal.bdrCollectedAmount = '';
            lineActionModal.bdrWriteOffAmount = '';
            lineActionModal.bdrPaymentReference = '';
            lineActionModal.bdrAgencyReference = '';
            lineActionModal.bdrPaymentPlanMonths = '';
            lineActionModal.bdrRequiresBackOffice = false;
            lineActionModal.bdrEligibility = null;
            lineActionModal.bdrEligibilityBusy = false;
            lineActionModal.cgtTargets = [];
            lineActionModal.cgtTargetsBusy = false;
            lineActionModal.cgtCurrentTypeLabel = '';
            lineActionModal.cgtSourceTypeCode = '';
            lineActionModal.cgtSourceTypeId = '';
            lineActionModal.cgtTargetTypeId = '';
            lineActionModal.cgtMigrationReason = '';
            lineActionModal.cgtMigrationPath = 'Standard';
            lineActionModal.cgtOffers = [];
            lineActionModal.cgtOffersBusy = false;
            lineActionModal.cgtProductOfferingId = '';
            lineActionModal.bssIdentityFile = null;
            lineActionModal.activateKycDocumentReferenceId = '';
            lineActionModal.activateIdentityFile = null;
            lineActionModal.activateKycUploadBusy = false;
            lineActionModal.activateKycUploadError = '';
            lineActionModal.activationLineTypeId = '';
            lineActionModal.activateSecondarySearchNationalId = '';
            lineActionModal.activateSecondarySearchPhone = '';
            lineActionModal.activateSecondaryResults = [];
            lineActionModal.activateSecondaryProfileId = '';
            lineActionModal.activateSecondaryLabel = '';
            lineActionModal.activateSecondaryBusy = false;
            lineActionModal.bssSupervisorConfirmed = false;
            lineActionModal.bssEffectiveMode = 'immediate';
            lineActionModal.bssEffectiveDateLocal = '';
            lineActionModal.mgrProrationPreview = null;
            lineActionModal.mgrProrationBusy = false;
            lineActionModal.migrateOfferDetail = null;
            lineActionModal.migrateOfferDetailBusy = false;
            lineActionModal.activateOfferDetail = null;
            lineActionModal.activateOfferDetailBusy = false;
            lineActionModal.supportIssueType = 0;
            lineActionModal.supportNotes = '';
            lineActionModal.supportBusy = false;
            lineActionModal.vasCatalog = [];
            lineActionModal.vasCatalogBusy = false;
            lineActionModal.selectedVasCode = '';
            lineActionModal.vasAction = 'Activate';
            lineActionModal.takeoverSearchBusy = false;
            lineActionModal.notes = '';
            lineActionModal.devInventoryId = '';
            lineActionModal.devSaleType = 'Cash';
            lineActionModal.devInstallmentPlanId = '';
            lineActionModal.devDevices = [];
            lineActionModal.devPlans = [];
            lineActionModal.devDownPayment = '';
            lineActionModal.devPaymentReference = '';
            lineActionModal.devPaymentChannel = 0;
            lineActionModal.devFinancingPreview = '';
            lineActionModal.devRequiresFinance = false;
            lineActionModal.devCreatedOperationId = '';
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.effectiveDate.reset(lineActionModal);
            }
        };

        const isCorporateCustomer = Vue.computed(() => state.subscriberType === 1);

        const listActivationLineTypeOptions = Vue.computed(() =>
            (state.telecomLineTypes || [])
                .filter((x) => x.isActive !== false && x.IsActive !== false)
                .map((x) => ({
                    ...x,
                    id: String(x.id ?? x.Id ?? '').trim(),
                    sortOrder: x.sortOrder ?? x.SortOrder ?? 0,
                }))
                .filter((x) => x.id)
                .sort((a, b) => a.sortOrder - b.sortOrder)
        );

        const listLineTypeDisplayName = (row) => {
            if (!row) return '—';
            return row.nameAr || row.NameAr || row.nameEn || row.NameEn || row.code || row.Code || '—';
        };

        const listFilteredActivatePool = Vue.computed(() => {
            const lineTypeId = (lineActionModal.activationLineTypeId || '').trim();
            if (!lineTypeId) return [];
            return (lineActionModal.poolNumbers || []).filter((row) => {
                const compat = row?.compatibleSubscriptionTypeId ?? row?.CompatibleSubscriptionTypeId ?? '';
                return !compat || compat === lineTypeId;
            });
        });

        const listBssEffectiveToday = () =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.effectiveDate.todayLocal()
                : new Date().toISOString().slice(0, 10);

        const listIncompleteSwal = (key, fallback = '') => {
            if (typeof TelecomBssWizardClearance !== 'undefined' && window.Swal) {
                TelecomBssWizardClearance.ui.incompleteSwal(Swal, telecomT, key, fallback);
                return;
            }
            if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(key, fallback || key) });
        };

        const barringLevelOptions = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.BARRING_LEVELS
                : [
                    { value: 'Full', key: 'suspension.barringFull' },
                    { value: 'InboundOnly', key: 'suspension.barringInbound' },
                    { value: 'OutboundOnly', key: 'suspension.barringOutbound' },
                    { value: 'DataOnly', key: 'suspension.barringDataOnly' },
                ]
        );

        const listSuspensionStartDateLocal = () => {
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                return TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal).slice(0, 10);
            }
            return listBssEffectiveToday();
        };

        const listSuspensionMaxEndDateLocal = () =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.suspensionEndDate.maxEndDateLocal(listSuspensionStartDateLocal())
                : (() => {
                    const d = new Date();
                    d.setDate(d.getDate() + 90);
                    return d.toISOString().slice(0, 10);
                })();

        const onListSusAutoReconnectChange = () => {
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.suspensionEndDate.onAutoReconnectToggled(
                    lineActionModal,
                    listSuspensionStartDateLocal
                );
            }
        };

        const listMgrProrationLabels = Vue.computed(() => {
            if (typeof TelecomBssWizardClearance === 'undefined') return null;
            return TelecomBssWizardClearance.migration.previewLabels(
                lineActionModal.mgrProrationPreview,
                telecomT
            );
        });
        const listMgrProrationPriceDifferenceLabel = Vue.computed(
            () => listMgrProrationLabels.value?.priceDifference ?? '—'
        );
        const listMgrProrationAmountLabel = Vue.computed(
            () => listMgrProrationLabels.value?.proratedAmount ?? '—'
        );
        const listMgrProrationWalletLabel = Vue.computed(
            () => listMgrProrationLabels.value?.walletBalance ?? '—'
        );
        const listMgrProrationDaysLabel = Vue.computed(
            () => listMgrProrationLabels.value?.daysRemaining ?? ''
        );
        const listMgrProrationSufficient = Vue.computed(
            () => listMgrProrationLabels.value?.sufficient !== false
        );

        const listContentLocale = () =>
            (document.documentElement.lang || 'en').toLowerCase().startsWith('en') ? 'en' : 'ar';

        const listMigrateOfferName = Vue.computed(() => {
            const d = lineActionModal.migrateOfferDetail;
            if (!d || typeof TelecomOfferDetail === 'undefined') return '';
            return TelecomOfferDetail.displayName(d, listContentLocale());
        });
        const listMigrateOfferSummary = Vue.computed(() => {
            const d = lineActionModal.migrateOfferDetail;
            if (!d || typeof TelecomOfferDetail === 'undefined') return '';
            return TelecomOfferDetail.summaryText(d, listContentLocale());
        });
        const listMigrateOfferVoice = Vue.computed(() => {
            const d = lineActionModal.migrateOfferDetail;
            if (!d || typeof TelecomOfferDetail === 'undefined') return '';
            return TelecomOfferDetail.formatQuotaLine(TelecomOfferDetail.componentByType(d, 0), listContentLocale());
        });
        const listMigrateOfferData = Vue.computed(() => {
            const d = lineActionModal.migrateOfferDetail;
            if (!d || typeof TelecomOfferDetail === 'undefined') return '';
            return TelecomOfferDetail.formatQuotaLine(TelecomOfferDetail.componentByType(d, 1), listContentLocale());
        });
        const listMigrateOfferSms = Vue.computed(() => {
            const d = lineActionModal.migrateOfferDetail;
            if (!d || typeof TelecomOfferDetail === 'undefined') return '';
            return TelecomOfferDetail.formatQuotaLine(TelecomOfferDetail.componentByType(d, 2), listContentLocale());
        });

        const listActivateOfferName = Vue.computed(() => {
            const d = lineActionModal.activateOfferDetail;
            if (!d || typeof TelecomOfferDetail === 'undefined') return '';
            return TelecomOfferDetail.displayName(d, listContentLocale());
        });
        const listActivateOfferSummary = Vue.computed(() => {
            const d = lineActionModal.activateOfferDetail;
            if (!d || typeof TelecomOfferDetail === 'undefined') return '';
            return TelecomOfferDetail.summaryText(d, listContentLocale());
        });
        const listActivateOfferVoice = Vue.computed(() => {
            const d = lineActionModal.activateOfferDetail;
            if (!d || typeof TelecomOfferDetail === 'undefined') return '';
            return TelecomOfferDetail.formatQuotaLine(TelecomOfferDetail.componentByType(d, 0), listContentLocale());
        });
        const listActivateOfferData = Vue.computed(() => {
            const d = lineActionModal.activateOfferDetail;
            if (!d || typeof TelecomOfferDetail === 'undefined') return '';
            return TelecomOfferDetail.formatQuotaLine(TelecomOfferDetail.componentByType(d, 1), listContentLocale());
        });
        const listActivateOfferSms = Vue.computed(() => {
            const d = lineActionModal.activateOfferDetail;
            if (!d || typeof TelecomOfferDetail === 'undefined') return '';
            return TelecomOfferDetail.formatQuotaLine(TelecomOfferDetail.componentByType(d, 2), listContentLocale());
        });

        const loadListActivateOfferings = async () => {
            const profileId = primarySubscriberProfileId();
            const lineTypeId = (lineActionModal.activationLineTypeId || '').trim();
            if (!profileId || !lineTypeId) {
                lineActionModal.offerings = [];
                return;
            }
            try {
                const url =
                    '/Product/GetMigrationEligibleProducts?subscriberProfileId=' +
                    encodeURIComponent(profileId) +
                    '&targetSubscriptionTypeId=' +
                    encodeURIComponent(lineTypeId);
                const res = await AxiosManager.get(url, {});
                const c = res?.data?.content ?? res?.data?.Content ?? {};
                lineActionModal.offerings = Array.isArray(c?.data) ? c.data : c?.Data ?? [];
            } catch {
                lineActionModal.offerings = [];
            }
        };

        const onListActivationLineTypeChange = async () => {
            lineActionModal.msisdnAssetId = '';
            lineActionModal.simIccid = '';
            lineActionModal.productOfferingId = '';
            lineActionModal.activateOfferDetail = null;
            await loadListActivateOfferings();
        };

        const onListActivateOfferingChanged = async () => {
            const offeringId = (lineActionModal.productOfferingId || '').trim();
            lineActionModal.activateOfferDetail = null;
            if (offeringId && typeof TelecomOfferDetail !== 'undefined') {
                lineActionModal.activateOfferDetailBusy = true;
                try {
                    lineActionModal.activateOfferDetail = await TelecomOfferDetail.loadById(offeringId);
                } catch {
                    lineActionModal.activateOfferDetail = null;
                } finally {
                    lineActionModal.activateOfferDetailBusy = false;
                }
            }
        };

        const loadListMigrationProration = async () => {
            const sub = lineActionModal.sub;
            const offeringId = (lineActionModal.productOfferingId || '').trim();
            if (!sub || !offeringId || typeof TelecomBssWizardClearance === 'undefined') {
                lineActionModal.mgrProrationPreview = null;
                return;
            }
            lineActionModal.mgrProrationBusy = true;
            try {
                lineActionModal.mgrProrationPreview = await TelecomBssWizardClearance.migration.loadPreview(
                    sub.subscriberProfileId,
                    sub.msisdnAssetId,
                    offeringId
                );
            } catch {
                lineActionModal.mgrProrationPreview = null;
            } finally {
                lineActionModal.mgrProrationBusy = false;
            }
        };

        const onListMigrateOfferingChanged = async () => {
            const offeringId = (lineActionModal.productOfferingId || '').trim();
            lineActionModal.migrateOfferDetail = null;
            if (offeringId && typeof TelecomOfferDetail !== 'undefined') {
                lineActionModal.migrateOfferDetailBusy = true;
                try {
                    lineActionModal.migrateOfferDetail = await TelecomOfferDetail.loadById(offeringId);
                } catch {
                    lineActionModal.migrateOfferDetail = null;
                } finally {
                    lineActionModal.migrateOfferDetailBusy = false;
                }
            }
            await loadListMigrationProration();
        };

        const searchListActivateSecondary = async () => {
            const nat = (lineActionModal.activateSecondarySearchNationalId || '').trim();
            const ph = (lineActionModal.activateSecondarySearchPhone || '').trim();
            if (nat.length < 2 && ph.length < 2) {
                if (window.Swal) Swal.fire({ icon: 'info', title: telecomT('swal.searchMinChars') });
                return;
            }
            lineActionModal.activateSecondaryBusy = true;
            try {
                const qs = new URLSearchParams();
                if (nat) qs.set('nationalId', nat);
                if (ph) qs.set('phone', ph);
                const res = await AxiosManager.get('/Customer/FindCustomerCandidates?' + qs.toString(), {});
                lineActionModal.activateSecondaryResults = res?.data?.content?.data ?? [];
                lineActionModal.activateSecondaryProfileId = '';
                lineActionModal.activateSecondaryLabel = '';
            } catch (e) {
                lineActionModal.activateSecondaryResults = [];
                showLineActionError(e);
            } finally {
                lineActionModal.activateSecondaryBusy = false;
            }
        };

        const selectListActivateSecondary = async (c) => {
            if (!c?.id) return;
            lineActionModal.activateSecondaryBusy = true;
            try {
                const res = await AxiosManager.get('/Customer/GetCustomer360?customerId=' + encodeURIComponent(c.id), {});
                const subs = res?.data?.content?.activeSubscriptions || [];
                const pick = subs.find((s) => s.isPrimaryLine) || subs[0];
                const pid = (pick?.subscriberProfileId || '').trim();
                lineActionModal.activateSecondaryProfileId = pid;
                lineActionModal.activateSecondaryLabel = pid ? (c.name || '—') : '';
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineActionModal.activateSecondaryBusy = false;
            }
        };

        const listSusShowsSimple = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.suspension.showsSimple(lineActionModal.susSuspensionType)
                : ['CustomerRequest', 'Operational'].includes(lineActionModal.susSuspensionType));

        const isCorporateListCustomer = () => {
            const core = state.customer360?.core;
            return typeof TelecomWizardConfirm !== 'undefined'
                ? TelecomWizardConfirm.isCorporateCustomerKind(core?.customerKind ?? core?.CustomerKind)
                : String(core?.customerKind ?? core?.CustomerKind ?? '').toLowerCase() === 'corporate';
        };

        const onSusTypeChangeList = () => {
            const ty = (lineActionModal.susSuspensionType || '').trim();
            lineActionModal.susRequiresBackOffice =
                ty === 'Fraud' || ty === 'Regulatory' || isCorporateListCustomer();
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.suspension.reset(lineActionModal, ty);
            }
        };

        const onListBssRegulatoryFileChange = (ev) => {
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.onRegulatoryFileChange(lineActionModal, ev);
            }
        };

        const onListSusIdentityFileChange = (ev) => {
            lineActionModal.bssIdentityFile = ev?.target?.files?.[0] || null;
        };

        const listSusShowsPayment = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.suspension.showsPayment(lineActionModal.susSuspensionType)
                : lineActionModal.susSuspensionType === 'Billing');
        const listSusShowsFraud = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.suspension.showsFraud(lineActionModal.susSuspensionType)
                : lineActionModal.susSuspensionType === 'Fraud');
        const listSusShowsRegulatory = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.suspension.showsRegulatory(lineActionModal.susSuspensionType)
                : lineActionModal.susSuspensionType === 'Regulatory');
        const listSusRequiresStep2Identity = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.suspension.requiresIdentityUpload(lineActionModal.susSuspensionType)
                : ['Fraud', 'Regulatory'].includes(lineActionModal.susSuspensionType || ''));

        const listTrmShowsPayment = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.termination.showsPayment(lineActionModal.trmTerminationType)
                : lineActionModal.trmTerminationType === 'Collections');
        const listTrmShowsFraud = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.termination.showsFraud(lineActionModal.trmTerminationType)
                : lineActionModal.trmTerminationType === 'Fraud');
        const listTrmShowsRegulatory = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.termination.showsRegulatory(lineActionModal.trmTerminationType)
                : lineActionModal.trmTerminationType === 'Regulatory');
        const listTrmRequiresLegacyIdentity = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.termination.requiresLegacyIdentity(lineActionModal.trmTerminationType)
                && !listTrmShowsRegulatory.value
                : lineActionModal.trmRequiresBackOffice);

        const listRfdShowsOriginalTxRef = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.refund.showsOriginalTxRef(lineActionModal.rfdRefundType)
                : ['Deposit', 'Overpayment'].includes(lineActionModal.rfdRefundType));
        const listRfdShowsPayoutDestination = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.refund.showsPayoutDestination(
                    lineActionModal.rfdRefundType,
                    lineActionModal.rfdRefundMethod
                )
                : lineActionModal.rfdRefundMethod === 'BankTransfer'
                    || lineActionModal.rfdRefundType === 'SyriatelCash');
        const listRfdPayoutLabelKey = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.refund.payoutLabel(
                    lineActionModal.rfdRefundType,
                    lineActionModal.rfdRefundMethod
                )
                : 'bss.payoutDestination');

        const listLineOutstandingBalance = () => {
            const sub = lineActionModal.sub;
            if (!sub?.id) return null;
            const w = state.lineWallets[sub.id];
            const raw = w?.outstandingBalance ?? w?.OutstandingBalance;
            if (raw === undefined || raw === null || raw === '') return null;
            const n = Number(raw);
            return Number.isFinite(n) ? n : null;
        };

        const bssTkoOptionsList = () => ({
            outstandingBalance: listLineOutstandingBalance(),
        });

        const listSimShowsLostStolenFields = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.simSwap.showsLostStolenFields(lineActionModal.simLostOrStolen)
                : !!lineActionModal.simLostOrStolen);
        const listCnShowsPremiumPayment = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.changeNumber.showsPremiumPayment(lineActionModal)
                : !!lineActionModal.cnRequiresBackOffice);
        const listCnShowsInternalPool = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.changeNumber.showsInternalPool(lineActionModal)
                : (lineActionModal.cnChangeMode || 'Internal') !== 'PortIn');
        const listCnDonorOperators = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.changeNumber.donorOperators
                : ['MTN', 'AFRICELL', 'OTHER']);
        const listTkoShowsObligationSettlement = Vue.computed(() => {
            const bal = listLineOutstandingBalance();
            return bal != null && bal < 0;
        });

        const bssCgtOptionsList = () => ({
            outstandingBalance: listLineOutstandingBalance() ?? 0,
        });

        const listCgtShowsFinancial = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.changeGsm.showsFinancial(lineActionModal, bssCgtOptionsList())
                : false);
        const listCgtShowsRegulatory = Vue.computed(() =>
            typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.changeGsm.showsRegulatory(lineActionModal)
                : lineActionModal.cgtMigrationPath === 'Regulatory');

        const onListCgtPathChange = () => {
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.changeGsm.reset(lineActionModal, lineActionModal.cgtMigrationPath);
            }
        };

        const onListCgtIdentityFileChange = (ev) => {
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.onIdentityFileChange(lineActionModal, ev);
            }
        };

        const loadListChangeGsmTargets = async () => {
            const sub = lineActionModal.sub;
            if (!sub?.subscriberProfileId) return;
            lineActionModal.cgtTargetsBusy = true;
            try {
                let url =
                    '/Product/GetChangeGsmEligibleTargets?subscriberProfileId=' +
                    encodeURIComponent(sub.subscriberProfileId);
                if (sub.msisdnAssetId) {
                    url += '&msisdnAssetId=' + encodeURIComponent(sub.msisdnAssetId);
                }
                const res = await AxiosManager.get(url, {});
                const content = res?.data?.content ?? {};
                lineActionModal.cgtTargets = (content.data || []).map((t) => ({
                    id: t.id,
                    code: t.code,
                    nameAr: t.nameAr,
                    nameEn: t.nameEn,
                }));
                lineActionModal.cgtCurrentTypeLabel =
                    content.currentSubscriptionTypeLabel || content.CurrentSubscriptionTypeLabel || '';
                lineActionModal.cgtSourceTypeId =
                    content.currentSubscriptionTypeId || content.CurrentSubscriptionTypeId || '';
                lineActionModal.cgtSourceTypeCode =
                    content.currentSubscriptionTypeCode || content.CurrentSubscriptionTypeCode || '';
            } catch {
                lineActionModal.cgtTargets = [];
            } finally {
                lineActionModal.cgtTargetsBusy = false;
            }
        };

        const onListCgtTargetTypeChanged = async () => {
            const sub = lineActionModal.sub;
            const targetId = (lineActionModal.cgtTargetTypeId || '').trim();
            lineActionModal.cgtProductOfferingId = '';
            if (!sub?.subscriberProfileId || !targetId) {
                lineActionModal.cgtOffers = [];
                return;
            }
            lineActionModal.cgtOffersBusy = true;
            try {
                let url =
                    '/Product/GetChangeGsmEligibleProducts?subscriberProfileId=' +
                    encodeURIComponent(sub.subscriberProfileId) +
                    '&targetSubscriptionTypeId=' +
                    encodeURIComponent(targetId);
                if (sub.msisdnAssetId) {
                    url += '&msisdnAssetId=' + encodeURIComponent(sub.msisdnAssetId);
                }
                const res = await AxiosManager.get(url, {});
                const content = res?.data?.content ?? {};
                lineActionModal.cgtOffers = (content.data || []).map((o) => ({
                    id: o.id,
                    name: o.name,
                    serviceCode: o.serviceCode,
                }));
            } catch {
                lineActionModal.cgtOffers = [];
            } finally {
                lineActionModal.cgtOffersBusy = false;
            }
        };

        const onBdrActionChangeList = () => {
            loadListBadDebtEligibility();
        };

        const onSimLostOrStolenChangeList = () => {
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.simSwap.reset(lineActionModal, lineActionModal.simLostOrStolen);
            }
        };

        const onRefundMethodChangeList = () => {
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.refund.reset(
                    lineActionModal,
                    lineActionModal.rfdRefundType,
                    lineActionModal.rfdRefundMethod
                );
            }
        };

        const onListRcnClearanceChange = () => {
            if (typeof TelecomReconnectClearance !== 'undefined') {
                TelecomReconnectClearance.resetConditionalFields(lineActionModal, lineActionModal.rcnClearanceType);
            }
            loadListReconnectEligibility();
        };

        const onListRcnRegulatoryFileChange = (ev) => {
            const file = ev?.target?.files?.[0] || null;
            lineActionModal.rcnRegulatoryFile = file;
            lineActionModal.rcnKycDocumentReferenceId = '';
        };

        const listRcnBdrApproved = Vue.computed(() => {
            const sub = lineActionModal.sub;
            if (!sub || typeof TelecomReconnectClearance === 'undefined') return false;
            const ops =
                state.customer360?.recentOperations ??
                state.customer360?.RecentOperations ??
                [];
            return TelecomReconnectClearance.isBdrApprovedForReconnect(
                TelecomReconnectClearance.resolveBdrStatus(ops, sub)
            );
        });
        const listRcnShowsPaymentRef = Vue.computed(() => {
            const rcn = typeof TelecomReconnectClearance !== 'undefined' ? TelecomReconnectClearance : null;
            return rcn
                ? rcn.showsPaymentReference(lineActionModal.rcnClearanceType, { bdrApproved: listRcnBdrApproved.value })
                : lineActionModal.rcnClearanceType === 'Payment' || listRcnBdrApproved.value;
        });
        const listRcnShowsFraudFields = Vue.computed(() => {
            const rcn = typeof TelecomReconnectClearance !== 'undefined' ? TelecomReconnectClearance : null;
            return rcn ? rcn.showsFraudFields(lineActionModal.rcnClearanceType) : lineActionModal.rcnClearanceType === 'Fraud';
        });
        const listRcnShowsRegulatoryFields = Vue.computed(() => {
            const rcn = typeof TelecomReconnectClearance !== 'undefined' ? TelecomReconnectClearance : null;
            return rcn ? rcn.showsRegulatoryFields(lineActionModal.rcnClearanceType) : lineActionModal.rcnClearanceType === 'Regulatory';
        });

        const loadListReconnectEligibility = async () => {
            const sub = lineActionModal.sub;
            if (!sub?.subscriberProfileId || !sub?.msisdnAssetId) return;
            lineActionModal.rcnEligibilityBusy = true;
            try {
                const qs = new URLSearchParams({
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId,
                    reconnectReason: (lineActionModal.rcnReconnectReason || 'CustomerRequest').trim(),
                    clearanceType: (lineActionModal.rcnClearanceType || 'Customer').trim(),
                    fraudClearanceConfirmed: String(!!lineActionModal.rcnFraudClearanceConfirmed),
                });
                const pay = (lineActionModal.rcnPaymentReference || '').trim();
                if (pay) qs.set('paymentReference', pay);
                const res = await AxiosManager.get('/Telecom/GetReconnectEligibility?' + qs.toString(), {});
                const d = res?.data?.content?.data ?? res?.data?.content?.Data ?? null;
                lineActionModal.rcnEligibility = d;
                if (d) {
                    lineActionModal.rcnRequiresBackOffice =
                        !!d.requiresBackOfficeApproval || !!d.RequiresBackOfficeApproval;
                }
            } catch {
                lineActionModal.rcnEligibility = null;
            } finally {
                lineActionModal.rcnEligibilityBusy = false;
            }
        };

        const loadListBadDebtEligibility = async () => {
            const sub = lineActionModal.sub;
            if (!sub?.subscriberProfileId || !sub?.msisdnAssetId) return;
            lineActionModal.bdrEligibilityBusy = true;
            try {
                const qs = new URLSearchParams({
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId,
                    collectionAction: (lineActionModal.bdrCollectionAction || 'PaymentRecorded').trim(),
                    dunningStage: (lineActionModal.bdrDunningStage || 'Reminder1').trim(),
                    collectionApprovalConfirmed: lineActionModal.bdrSupervisorConfirmed ? 'true' : 'false',
                });
                const pay = (lineActionModal.bdrPaymentReference || '').trim();
                if (pay) qs.set('paymentReference', pay);
                const col = Number(lineActionModal.bdrCollectedAmount);
                if (col > 0) qs.set('collectedAmount', String(col));
                const wo = Number(lineActionModal.bdrWriteOffAmount);
                if (wo > 0) qs.set('writeOffAmount', String(wo));
                const res = await AxiosManager.get('/Telecom/GetBadDebtEligibility?' + qs.toString(), {});
                const d = res?.data?.content?.data ?? res?.data?.content?.Data ?? null;
                lineActionModal.bdrEligibility = d;
                if (d) {
                    lineActionModal.bdrRequiresBackOffice =
                        !!d.requiresBackOfficeApproval || !!d.RequiresBackOfficeApproval;
                }
            } catch {
                lineActionModal.bdrEligibility = null;
            } finally {
                lineActionModal.bdrEligibilityBusy = false;
            }
        };

        const onTerminationTypeChangeList = () => {
            const ty = (lineActionModal.trmTerminationType || '').trim();
            lineActionModal.trmRequiresBackOffice =
                ty === 'Fraud' || ty === 'Regulatory' || ty === 'Collections' || isCorporateListCustomer();
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.termination.reset(lineActionModal, ty);
            }
        };

        const onTerminationIdentityFileChange = (ev) => {
            lineActionModal.trmIdentityFile = ev?.target?.files?.[0] || null;
        };

        const onTakeoverIdentityFileChange = (ev) => {
            lineActionModal.takeoverIdentityFile = ev?.target?.files?.[0] || null;
        };

        const onSimSwapIdentityFileChange = (ev) => {
            lineActionModal.simIdentityFile = ev?.target?.files?.[0] || null;
        };

        const onChangeNumberPaymentFileChange = (ev) => {
            lineActionModal.cnPaymentFile = ev?.target?.files?.[0] || null;
        };

        const loadChangeNumberPoolForModal = async (excludeAssetId) => {
            lineActionModal.cnPoolBusy = true;
            try {
                const res = await AxiosManager.get('/Telecom/GetMsisdnAssetPoolList?status=Available', {});
                const rows = parseMsisdnPoolRows(res).filter(isAvailableMsisdnPoolRow);
                const ex = (excludeAssetId || '').trim();
                lineActionModal.cnPoolNumbers = rows.filter((r) => {
                    const id = r.id ?? r.Id;
                    return !ex || String(id) !== ex;
                });
            } catch {
                lineActionModal.cnPoolNumbers = [];
            } finally {
                lineActionModal.cnPoolBusy = false;
            }
        };

        const onChangeNumberTargetPickedList = () => {
            lineActionModal.cnRequiresBackOffice =
                typeof TelecomWizardConfirm !== 'undefined'
                    ? TelecomWizardConfirm.resolveChangeNumberRequiresBackOffice(lineActionModal)
                    : isPremiumMsisdnCategory(
                        (lineActionModal.cnPoolNumbers || []).find(
                            (r) => String(r.id ?? r.Id) === (lineActionModal.cnTargetMsisdnAssetId || '').trim()
                        )?.category
                    );
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.changeNumber.reset(lineActionModal, lineActionModal.cnRequiresBackOffice);
            } else if (!lineActionModal.cnRequiresBackOffice) {
                lineActionModal.cnPremiumFeeAmount = '';
            }
        };

        const primarySubscriberProfileId = () => {
            const subs = state.customer360?.activeSubscriptions || [];
            const p = subs.find((s) => s.isPrimaryLine) || subs[0];
            return (p?.subscriberProfileId || '').trim();
        };

        const newLineOfferingDeposit = () => {
            const d = lineActionModal.activateOfferDetail;
            if (d && typeof TelecomOfferDetail !== 'undefined') {
                const fromDetail = TelecomOfferDetail.defaultMonthlyPrice(d);
                if (fromDetail != null) return fromDetail;
            }
            const o = (lineActionModal.offerings || []).find((x) => x.id === lineActionModal.productOfferingId);
            return Number(
                o?.unitPrice ?? o?.UnitPrice ?? o?.defaultPrice ?? o?.DefaultPrice ?? 0
            ) || 0;
        };

        const confirmListOperationIfAllowed = async (opId, requiresBackOffice = false) => {
            if (!opId || requiresBackOffice) {
                return null;
            }
            if (typeof TelecomWizardConfirm !== 'undefined') {
                return TelecomWizardConfirm.confirmOperationIfAllowed(opId, { requiresBackOffice: false });
            }
            return AxiosManager.post('/Telecom/ConfirmTelecomOperation', { id: opId });
        };

        const onListRcnIdentityFileChange = (ev) => {
            lineActionModal.rcnIdentityFile = ev?.target?.files?.[0] || null;
        };

        const finalizeListOpWithConfirm = async (opId, successMessage, identityFile) => {
            const file = identityFile || null;
            if (!file) {
                if (window.Swal) {
                    Swal.fire({
                        icon: 'warning',
                        title: telecomT('suspension.kycRequired', 'Upload document'),
                        text: telecomT('suspension.kycDocumentHint', ''),
                    });
                }
                return false;
            }
            if (typeof TelecomWizardConfirm !== 'undefined') {
                await TelecomWizardConfirm.uploadOperationIdentityDocument(opId, file);
            } else {
                const form = new FormData();
                form.append('id', opId);
                form.append('file', file);
                await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                    headers: { 'Content-Type': 'multipart/form-data' },
                });
            }
            const confirmRes = await confirmListOperationIfAllowed(opId, false);
            if (!confirmRes) {
                notifyListConfirmSkipped();
                return false;
            }
            if (confirmRes?.data?.code !== 200) {
                throw Object.assign(new Error(confirmRes?.data?.message || telecomT('swal.confirmFailed')), {
                    response: confirmRes,
                });
            }
            showTelecomConfirmResult(confirmRes, successMessage);
            return true;
        };

        const notifyListBackOfficeQueued = (hintKey = 'swal.pendingRcnHint') => {
            if (!window.Swal) {
                return;
            }
            Swal.fire({
                icon: 'success',
                title: telecomT('swal.sentToBackOffice'),
                text: telecomT(hintKey),
                timer: 2800,
                showConfirmButton: false,
            });
        };

        const notifyListConfirmSkipped = () => {
            if (!window.Swal) {
                return;
            }
            Swal.fire({
                icon: 'info',
                title: telecomT('swal.sentToBackOffice'),
                text: telecomT('wizardUi.awaitBo', 'Awaiting back office'),
                timer: 2800,
                showConfirmButton: false,
            });
        };

        const runTelecomPipeline = async ({ processingKey, kindLabel, msisdn, buildBody, beforeConfirm, requiresBackOffice = false }) => {
            const key = processingKey || '__line__';
            if (lineProcessing[key]) return false;
            lineProcessing[key] = true;
            try {
                const notes = `Customer360|${kindLabel}|${msisdn || '—'}`;
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', {
                    ...buildBody(),
                    notes,
                });
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || telecomT('swal.createOpFailed')), {
                        response: createRes,
                    });
                }
                const opId = createRes?.data?.content?.data?.id;
                if (!opId) throw new Error(telecomT('swal.noOpId'));
                await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', { id: opId });
                if (beforeConfirm) {
                    const paymentOk = await beforeConfirm(opId);
                    if (!paymentOk) return false;
                }
                if (requiresBackOffice) {
                    notifyListBackOfficeQueued();
                    await loadCustomer360(state.id);
                    return true;
                }
                const confirmRes = await confirmListOperationIfAllowed(opId, false);
                if (!confirmRes) {
                    notifyListConfirmSkipped();
                    await loadCustomer360(state.id);
                    return true;
                }
                if (confirmRes?.data?.code !== 200) {
                    throw Object.assign(new Error(confirmRes?.data?.message || telecomT('swal.confirmFailed')), {
                        response: confirmRes,
                    });
                }
                showTelecomConfirmResult(confirmRes);
                await loadCustomer360(state.id);
                return true;
            } catch (e) {
                showLineActionError(e);
                return false;
            } finally {
                lineProcessing[key] = false;
            }
        };

        const loadNewLineModalData = async () => {
            try {
                const poolRes = await AxiosManager.get('/Telecom/GetMsisdnAssetPoolList?status=Available', {});
                lineActionModal.poolNumbers = parseMsisdnPoolRows(poolRes).filter(isAvailableMsisdnPoolRow);
                lineActionModal.offerings = [];
                const def =
                    listActivationLineTypeOptions.value.find((x) => x.isDefault || x.IsDefault) ||
                    listActivationLineTypeOptions.value[0];
                if (def?.id) {
                    lineActionModal.activationLineTypeId = def.id;
                    await loadListActivateOfferings();
                }
            } catch (e) {
                lineActionModal.poolNumbers = [];
                lineActionModal.offerings = [];
                console.warn('loadNewLineModalData failed', e);
            }
        };

        const onNewLineMsisdnChanged = () => {
            const id = (lineActionModal.msisdnAssetId || '').trim();
            const row = (lineActionModal.poolNumbers || []).find((a) => a.id === id);
            lineActionModal.simIccid = String(
                row?.iccid ?? row?.Iccid ?? row?.pairedIccid ?? row?.PairedIccid ?? ''
            ).trim();
            lineActionModal.activateKycDocumentReferenceId = '';
            lineActionModal.activateKycUploadError = '';
        };

        const uploadListActivateKyc = async (file, msisdn) => {
            if (typeof TelecomBssWizardClearance !== 'undefined' && TelecomBssWizardClearance.uploadKyc) {
                return TelecomBssWizardClearance.uploadKyc(msisdn, file);
            }
            if (typeof TelecomReconnectClearance !== 'undefined' && TelecomReconnectClearance.uploadRegulatoryAttachment) {
                return TelecomReconnectClearance.uploadRegulatoryAttachment(msisdn, file);
            }
            throw new Error(telecomT('wizard.kycUpload.failed', 'KYC upload failed'));
        };

        const onActivateKycFileChange = async (ev) => {
            const file = ev?.target?.files?.[0] || null;
            lineActionModal.activateIdentityFile = file;
            lineActionModal.activateKycDocumentReferenceId = '';
            lineActionModal.activateKycUploadError = '';
            if (!file) return;
            const assetId = (lineActionModal.msisdnAssetId || '').trim();
            const row = (lineActionModal.poolNumbers || []).find((a) => a.id === assetId);
            const msisdn = (row?.msisdn || row?.Msisdn || '').trim();
            if (!msisdn) {
                lineActionModal.activateKycUploadError = telecomT('wizard.kycUpload.msisdnRequired', 'Select MSISDN first');
                return;
            }
            lineActionModal.activateKycUploadBusy = true;
            try {
                lineActionModal.activateKycDocumentReferenceId = await uploadListActivateKyc(file, msisdn);
            } catch (e) {
                lineActionModal.activateKycUploadError =
                    telecomT(e?.message || 'wizard.kycUpload.failed', e?.message || 'Upload failed');
            } finally {
                lineActionModal.activateKycUploadBusy = false;
            }
        };

        const ensureListActivateKyc = async () => {
            if ((lineActionModal.activateKycDocumentReferenceId || '').trim()) return true;
            const file = lineActionModal.activateIdentityFile;
            if (!file) {
                if (window.Swal) {
                    Swal.fire({
                        icon: 'warning',
                        title: telecomT('wizard.kycUpload.title', 'KYC required'),
                        text: telecomT('suspension.kycRequired', 'Upload document'),
                    });
                }
                return false;
            }
            const assetId = (lineActionModal.msisdnAssetId || '').trim();
            const row = (lineActionModal.poolNumbers || []).find((a) => a.id === assetId);
            const msisdn = (row?.msisdn || row?.Msisdn || '').trim();
            if (!msisdn) {
                if (window.Swal) {
                    Swal.fire({ icon: 'warning', title: telecomT('swal.newLineIncomplete') });
                }
                return false;
            }
            lineActionModal.activateKycUploadBusy = true;
            try {
                lineActionModal.activateKycDocumentReferenceId = await uploadListActivateKyc(file, msisdn);
                return true;
            } catch (e) {
                lineActionModal.activateKycUploadError =
                    telecomT(e?.message || 'wizard.kycUpload.failed', e?.message || 'Upload failed');
                if (window.Swal) {
                    Swal.fire({ icon: 'warning', text: lineActionModal.activateKycUploadError });
                }
                return false;
            } finally {
                lineActionModal.activateKycUploadBusy = false;
            }
        };

        const openNewLineModal = async () => {
            if (!gridAccess.canActivateLine || !state.id) return;
            resetLineActionModal();
            if (typeof ActivationChannelUi !== 'undefined') {
                await ActivationChannelUi.ensureLoaded();
            }
            await loadNewLineModalData();
            showBsModal('C360NewLineModal');
        };

        const openMigrateModal = async (sub) => {
            if (!gridAccess.canMigrateLine || !sub) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            try {
                let url =
                    '/Product/GetMigrationEligibleProducts?subscriberProfileId=' +
                    encodeURIComponent(sub.subscriberProfileId || '');
                if (sub.msisdnAssetId) {
                    url += '&msisdnAssetId=' + encodeURIComponent(sub.msisdnAssetId);
                }
                const res = await AxiosManager.get(url, {});
                const c = res?.data?.content;
                lineActionModal.migrationProducts = Array.isArray(c?.data) ? c.data : [];
                lineActionModal.currentProductName = c?.currentProductName || c?.CurrentProductName || sub.productName || '';
            } catch {
                lineActionModal.migrationProducts = [];
            }
            showBsModal('C360MigrateModal');
        };

        const openChangeGsmModal = async (sub) => {
            if (!gridAccess.canChangeGsmLine || !sub) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            await loadListChangeGsmTargets();
            showBsModal('C360ChangeGsmModal');
        };

        const openSimSwapModal = (sub) => {
            if (!gridAccess.canSimSwapLine || !sub) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            showBsModal('C360SimSwapModal');
        };

        const loadListVasCatalog = async () => {
            const sub = lineActionModal.sub;
            if (!sub?.subscriberProfileId) {
                lineActionModal.vasCatalog = [];
                return;
            }
            lineActionModal.vasCatalogBusy = true;
            try {
                let url =
                    '/Product/GetEligibleVasOfferings?subscriberProfileId=' +
                    encodeURIComponent(sub.subscriberProfileId);
                if (sub.msisdnAssetId) {
                    url += '&msisdnAssetId=' + encodeURIComponent(sub.msisdnAssetId);
                }
                const res = await AxiosManager.get(url, {});
                const content = res?.data?.content ?? res?.data?.Content ?? {};
                const list = content.data || content.Data || [];
                lineActionModal.vasCatalog = (Array.isArray(list) ? list : []).map((v) => ({
                    serviceCode: v.serviceCode || v.ServiceCode,
                    nameAr: v.nameAr || v.NameAr || v.catalogComponentLabel || v.CatalogComponentLabel || '',
                    nameEn: v.nameEn || v.NameEn || '',
                }));
            } catch {
                lineActionModal.vasCatalog = [];
            } finally {
                lineActionModal.vasCatalogBusy = false;
            }
        };

        const openVasModal = async (sub) => {
            if (!gridAccess.canToggleVas || !sub) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            lineActionModal.vasAction = 'Activate';
            await loadListVasCatalog();
            showBsModal('C360VasModal');
        };

        const submitVasFromList = async () => {
            const sub = lineActionModal.sub;
            const msisdn = (sub?.msisdn || '').trim();
            const code = (lineActionModal.selectedVasCode || '').trim();
            if (!msisdn || !code) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('wizard.vasPlaceholder', 'Pick VAS') });
                return;
            }
            if (lineActionModal.busy) return;
            lineActionModal.busy = true;
            try {
                const result =
                    typeof TelecomVasToggle !== 'undefined'
                        ? await TelecomVasToggle.toggle(AxiosManager, {
                              msisdn,
                              serviceCode: code,
                              activate: (lineActionModal.vasAction || 'Activate') !== 'Deactivate',
                          })
                        : null;
                if (!result?.ok) {
                    throw new Error(telecomT('swal.genericFailed', 'Failed'));
                }
                if (window.Swal) {
                    Swal.fire({
                        icon: 'success',
                        title: telecomT('swal.activated', 'Done'),
                        text: result.operationNumber
                            ? `${telecomT('swal.activatedOpPrefix', 'Op')} ${result.operationNumber}`
                            : '',
                        timer: 2200,
                        showConfirmButton: false,
                    });
                }
                await loadVasPanelForMsisdn(msisdn);
                hideBsModal('C360VasModal');
                await loadCustomer360(state.id);
            } catch (e) {
                const isBrv =
                    typeof TelecomVasToggle !== 'undefined'
                        ? TelecomVasToggle.isBusinessRuleViolation(e)
                        : e?.response?.data?.error?.name === 'BusinessRuleViolationException';
                const msg =
                    typeof TelecomVasToggle !== 'undefined'
                        ? TelecomVasToggle.pickError(e)
                        : e?.response?.data?.error?.message ?? e?.response?.data?.message ?? e?.message ?? '';
                if (window.Swal) {
                    Swal.fire({
                        icon: isBrv ? 'warning' : 'error',
                        title: isBrv ? telecomT('swal.activationFailed', 'VAL-11') : telecomT('swal.error', 'Error'),
                        text: msg,
                    });
                }
            } finally {
                lineActionModal.busy = false;
            }
        };

        const onCnChangeModeChangeList = () => {
            const portIn = (lineActionModal.cnChangeMode || 'Internal') === 'PortIn';
            lineActionModal.cnRequiresBackOffice = portIn;
            lineActionModal.cnTargetMsisdnAssetId = '';
            lineActionModal.cnPremiumFeeAmount = '';
            if (portIn) {
                lineActionModal.cnNumberChangeReason = lineActionModal.cnNumberChangeReason || 'PortIn';
            }
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.changeNumber.reset(lineActionModal, lineActionModal.cnRequiresBackOffice);
            }
            if (!portIn && lineActionModal.sub) {
                loadChangeNumberPoolForModal(lineActionModal.sub.msisdnAssetId);
            }
        };

        const openChangeNumberModal = async (sub) => {
            if (!gridAccess.canChangeNumberLine || !sub) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            await loadChangeNumberPoolForModal(sub.msisdnAssetId);
            showBsModal('C360ChangeNumberModal');
        };

        const openTerminationModal = (sub) => {
            if (!gridAccess.canTerminateLine || !sub || isLineTerminated(sub)) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            onTerminationTypeChangeList();
            showBsModal('C360TerminationModal');
        };

        const openTakeOverModal = (sub) => {
            if (!gridAccess.canTakeOverLine || !sub) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            showBsModal('C360TakeOverModal');
        };

        const openSuspensionModal = (sub) => {
            if (!gridAccess.canSuspensionLine || !sub || isLineTerminated(sub) || isLineSuspended(sub)) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            onSusTypeChangeList();
            showBsModal('C360SuspensionModal');
        };

        const openReconnectModal = async (sub) => {
            if (!gridAccess.canReconnectLine || !sub || !isLineSuspended(sub)) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            await loadListReconnectEligibility();
            showBsModal('C360ReconnectModal');
        };

        const openRefundModal = (sub) => {
            if (!gridAccess.canRefundLine || !sub || isLineTerminated(sub)) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            lineActionModal.rfdRequiresBackOffice = lineActionModal.rfdRefundType === 'SyriatelCash';
            showBsModal('C360RefundModal');
        };

        const openBadDebtModal = async (sub) => {
            if (!gridAccess.canCollectionLine || !sub || isLineTerminated(sub)) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            await loadListBadDebtEligibility();
            showBsModal('C360BadDebtModal');
        };

        const loadListDeviceCatalog = async () => {
            try {
                const [devRes, planRes] = await Promise.all([
                    AxiosManager.get('/Telecom/GetDeviceInventoryList?status=Available', {}),
                    AxiosManager.get('/Telecom/GetInstallmentPlanList', {}),
                ]);
                lineActionModal.devDevices = devRes?.data?.content?.data ?? devRes?.data?.content?.Data ?? [];
                lineActionModal.devPlans = planRes?.data?.content?.data ?? planRes?.data?.content?.Data ?? [];
            } catch {
                lineActionModal.devDevices = [];
                lineActionModal.devPlans = [];
            }
        };

        const onListDevicePicked = () => {
            const row = (lineActionModal.devDevices || []).find(
                (d) => String(d.id ?? d.Id) === String(lineActionModal.devInventoryId)
            );
            if (row) {
                lineActionModal.devDownPayment = String(row.listPrice ?? row.ListPrice ?? '');
            }
        };

        const openDeviceSaleModal = async (sub) => {
            if (!gridAccess.canDeviceSaleLine || !sub) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            await loadListDeviceCatalog();
            showBsModal('C360DeviceSaleModal');
        };

        const submitDeviceSale = async () => {
            const sub = lineActionModal.sub;
            if (!sub) return;
            if (!(lineActionModal.devInventoryId || '').trim()) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('deviceSale.deviceImei', 'Pick device') });
                return;
            }
            if (lineActionModal.devSaleType === 'Installment' && !(lineActionModal.devInstallmentPlanId || '').trim()) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('deviceSale.installmentPlan', 'Pick plan') });
                return;
            }
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const effErr = TelecomBssWizardClearance.effectiveDate.validate(lineActionModal);
                if (effErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(effErr.key, effErr.key) });
                    return;
                }
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            try {
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', {
                    kind: 10,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    deviceInventoryId: lineActionModal.devInventoryId,
                    deviceSaleType: lineActionModal.devSaleType === 'Installment' ? 1 : 0,
                    deviceInstallmentPlanId:
                        lineActionModal.devSaleType === 'Installment'
                            ? lineActionModal.devInstallmentPlanId
                            : null,
                    deviceSaleEffectiveDateUtc:
                        typeof TelecomBssWizardClearance !== 'undefined'
                            ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal)
                            : new Date().toISOString(),
                    notes: `CustomerList|DeviceSale|${sub.msisdn || '—'}`,
                });
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || telecomT('swal.createOpFailed')), {
                        response: createRes,
                    });
                }
                const entity = createRes?.data?.content?.data;
                const opId = entity?.id;
                if (!opId) throw new Error(telecomT('swal.noOpId'));
                lineActionModal.devCreatedOperationId = opId;
                lineActionModal.devRequiresFinance =
                    !!entity?.deviceApprovalLevelRequired || !!entity?.approvalLevelRequired;
                lineActionModal.devFinancingPreview = entity?.deviceFinancingNoteAr || entity?.DeviceFinancingNoteAr || '';
                if (lineActionModal.devRequiresFinance) {
                    await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', { id: opId });
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: telecomT('swal.sentToBackOffice'),
                            text: telecomT('deviceSale.financeApproval', ''),
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                    await loadCustomer360(state.id);
                    hideBsModal('C360DeviceSaleModal');
                    return;
                }
                if (lineActionModal.devSaleType === 'Installment') {
                    const amount = Number(lineActionModal.devDownPayment);
                    const ref = (lineActionModal.devPaymentReference || '').trim();
                    if (!amount || !ref) {
                        if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.paymentAmountRequired', 'Payment required') });
                        return;
                    }
                    const payRes = await AxiosManager.post('/Telecom/RecordDeviceDownPayment', {
                        operationId: opId,
                        amountPaid: amount,
                        paymentChannel: Number(lineActionModal.devPaymentChannel) || 0,
                        paymentReference: ref,
                    });
                    if (payRes?.data?.code !== 200) {
                        throw Object.assign(new Error(payRes?.data?.message || telecomT('swal.genericFailed')), {
                            response: payRes,
                        });
                    }
                }
                await finalizeListOpWithConfirm(opId, null, lineActionModal.bssIdentityFile);
                await loadCustomer360(state.id);
                hideBsModal('C360DeviceSaleModal');
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineProcessing[key] = false;
                lineActionModal.busy = false;
            }
        };

        const onRefundTypeChangeList = () => {
            const ty = (lineActionModal.rfdRefundType || '').trim();
            const method = (lineActionModal.rfdRefundMethod || '').trim();
            const amt = Number(lineActionModal.rfdRefundAmount) || 0;
            lineActionModal.rfdRequiresBackOffice =
                ty === 'SyriatelCash' || amt > 500000 || (method === 'Cash' && amt > 500000);
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                TelecomBssWizardClearance.refund.reset(lineActionModal, ty, method);
            }
        };

        const onRefundIdentityFileChange = (ev) => {
            lineActionModal.rfdIdentityFile = ev?.target?.files?.[0] || null;
        };

        const searchTakeoverTarget = async () => {
            const nat = (lineActionModal.takeoverSearchNationalId || '').trim();
            const ph = (lineActionModal.takeoverSearchPhone || '').trim();
            if (nat.length < 2 && ph.length < 2) {
                if (window.Swal) Swal.fire({ icon: 'info', title: telecomT('swal.searchOwnerHint') });
                return;
            }
            lineActionModal.takeoverSearchBusy = true;
            try {
                const qs = new URLSearchParams();
                if (nat) qs.set('nationalId', nat);
                if (ph) qs.set('phone', ph);
                const res = await AxiosManager.get('/Customer/FindCustomerCandidates?' + qs.toString(), {});
                lineActionModal.takeoverResults = res?.data?.content?.data ?? [];
                lineActionModal.takeoverTargetCustomerId = '';
                lineActionModal.takeoverTargetProfileId = '';
            } catch (e) {
                lineActionModal.takeoverResults = [];
                showLineActionError(e);
            } finally {
                lineActionModal.takeoverSearchBusy = false;
            }
        };

        const selectTakeoverTarget = async (c) => {
            if (!c?.id) return;
            lineActionModal.takeoverTargetCustomerId = c.id;
            lineActionModal.takeoverTargetProfileId = '';
            lineActionModal.busy = true;
            try {
                const res = await AxiosManager.get('/Customer/GetCustomer360?customerId=' + encodeURIComponent(c.id), {});
                const subs = res?.data?.content?.activeSubscriptions || [];
                const pick = subs.find((s) => s.isPrimaryLine) || subs[0];
                lineActionModal.takeoverTargetProfileId = (pick?.subscriberProfileId || '').trim();
                if (!lineActionModal.takeoverTargetProfileId && window.Swal) {
                    Swal.fire({ icon: 'warning', title: telecomT('swal.noSubscriberProfile') });
                }
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineActionModal.busy = false;
            }
        };

        const fetchNewLinePaymentFromCashier = async () => {
            const ref = (lineActionModal.paymentReference || '').trim();
            const deposit = newLineOfferingDeposit();
            if (!ref) {
                if (window.Swal) {
                    Swal.fire({ icon: 'warning', title: telecomT('swal.paymentRefRequired', 'Payment ref required') });
                }
                return;
            }
            lineActionModal.cashierFetchBusy = true;
            try {
                const res = await AxiosManager.post('/Telecom/FetchCashierPayment', {
                    paymentReference: ref,
                    expectedAmount: deposit > 0 ? deposit : null,
                });
                const data = res?.data?.content?.data ?? res?.data?.content?.Data;
                if (res?.data?.code === 200 && data) {
                    const amt = data.amountPaid ?? data.AmountPaid;
                    if (amt != null) lineActionModal.paymentAmount = String(amt);
                    const ch = data.paymentChannel ?? data.PaymentChannel;
                    if (ch != null) lineActionModal.paymentChannel = Number(ch);
                    lineActionModal.paymentReference = data.paymentReference ?? data.PaymentReference ?? ref;
                    lineActionModal.paymentCashierLocked = true;
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: telecomT('wizardUi.cashierFetched', 'Loaded'),
                            timer: 1200,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    throw Object.assign(
                        new Error(res?.data?.message || telecomT('swal.genericFailed', 'Failed')),
                        { response: res }
                    );
                }
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineActionModal.cashierFetchBusy = false;
            }
        };

        const submitNewLineActivation = async () => {
            const profileId = primarySubscriberProfileId();
            const lineTypeId = (lineActionModal.activationLineTypeId || '').trim();
            const assetId = (lineActionModal.msisdnAssetId || '').trim();
            const offeringId = (lineActionModal.productOfferingId || '').trim();
            const iccid = (lineActionModal.simIccid || '').trim();
            if (!profileId || !lineTypeId || !assetId || !offeringId || iccid.length < 19) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.newLineIncomplete') });
                return;
            }
            if (isCorporateCustomer.value && !(lineActionModal.activateSecondaryProfileId || '').trim()) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('wizard.corporateSecondPartyRequired') });
                return;
            }
            if (!(await ensureListActivateKyc())) return;
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const effErr = TelecomBssWizardClearance.effectiveDate.validate(lineActionModal);
                if (effErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(effErr.key, effErr.key) });
                    return;
                }
            }
            lineActionModal.busy = true;
            try {
                await AxiosManager.post('/Telecom/ReserveMsisdnForCustomer', {
                    msisdnAssetId: assetId,
                    customerId: state.id,
                });
            } catch (e) {
                lineActionModal.busy = false;
                showLineActionError(e);
                return;
            }
            const ok = await runTelecomPipeline({
                processingKey: '__new__',
                kindLabel: 'NewActivation',
                msisdn: lineActionModal.poolNumbers.find((a) => a.id === assetId)?.msisdn || assetId,
                buildBody: () => ({
                    kind: 0,
                    subscriberProfileId: profileId,
                    msisdnAssetId: assetId,
                    productOfferingId: offeringId,
                    targetSubscriptionTypeId: lineTypeId,
                    secondarySubscriberProfileId:
                        (lineActionModal.activateSecondaryProfileId || '').trim() || null,
                    simIccid: iccid,
                    kycDocumentReferenceId: (lineActionModal.activateKycDocumentReferenceId || '').trim() || null,
                    activationChannel: Number(lineActionModal.activationChannel) || 0,
                    dealerCode: (lineActionModal.dealerCode || '').trim() || null,
                    activationEffectiveDateUtc:
                        typeof TelecomBssWizardClearance !== 'undefined'
                            ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal)
                            : new Date().toISOString(),
                }),
                beforeConfirm: async (opId) => {
                    const deposit = newLineOfferingDeposit();
                    if (deposit <= 0) return true;
                    if (!lineActionModal.paymentCashierLocked) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: telecomT('wizardUi.fetchFromCashier', 'Fetch from cashier'),
                            });
                        }
                        return false;
                    }
                    const ref = (lineActionModal.paymentReference || '').trim();
                    const amt = Number(lineActionModal.paymentAmount);
                    if (!ref || !(amt > 0)) {
                        if (window.Swal) {
                            Swal.fire({
                                icon: 'warning',
                                title: telecomT('swal.paymentAmountRequired', 'Payment required'),
                            });
                        }
                        return false;
                    }
                    try {
                        const payRes = await AxiosManager.post('/Telecom/RecordSellingLinePayment', {
                            operationId: opId,
                            paymentReference: ref,
                            amountPaid: amt,
                            paymentChannel: Number(lineActionModal.paymentChannel) || 0,
                        });
                        if (payRes?.data?.code !== 200) {
                            throw Object.assign(
                                new Error(payRes?.data?.message || telecomT('swal.genericFailed', 'Failed')),
                                { response: payRes }
                            );
                        }
                        return true;
                    } catch (e) {
                        showLineActionError(e);
                        return false;
                    }
                },
            });
            lineActionModal.busy = false;
            if (ok) hideBsModal('C360NewLineModal');
        };

        const submitMigrate = async () => {
            const sub = lineActionModal.sub;
            const offeringId = (lineActionModal.productOfferingId || '').trim();
            if (!sub || !offeringId) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.pickPackage') });
                return;
            }
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const prErr = TelecomBssWizardClearance.migration.validatePreview(
                    lineActionModal.mgrProrationPreview
                );
                if (prErr) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'warning',
                            title: telecomT('wizard.migrationOffers.prorationInsufficient', 'Insufficient balance'),
                            text: telecomT(prErr.key, ''),
                        });
                    }
                    return;
                }
                const effErr = TelecomBssWizardClearance.effectiveDate.validate(lineActionModal);
                if (effErr) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'warning', title: telecomT(effErr.key, effErr.key) });
                    }
                    return;
                }
            }
            lineActionModal.busy = true;
            const ok = await runTelecomPipeline({
                processingKey: sub.id,
                kindLabel: 'Migration',
                msisdn: sub.msisdn,
                buildBody: () => ({
                    kind: 1,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    productOfferingId: offeringId,
                    migrationEffectiveDateUtc:
                        typeof TelecomBssWizardClearance !== 'undefined'
                            ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal)
                            : new Date().toISOString(),
                }),
            });
            lineActionModal.busy = false;
            if (ok) hideBsModal('C360MigrateModal');
        };

        const submitChangeGsm = async () => {
            const sub = lineActionModal.sub;
            if (!sub) return;
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const cgtErr = TelecomBssWizardClearance.changeGsm.validate(
                    lineActionModal,
                    bssCgtOptionsList()
                );
                if (cgtErr) {
                    if (window.Swal) {
                        Swal.fire({ icon: 'warning', title: telecomT(cgtErr.key, cgtErr.key) });
                    }
                    return;
                }
                const effErr = TelecomBssWizardClearance.effectiveDate.validate(lineActionModal);
                if (effErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(effErr.key, effErr.key) });
                    return;
                }
            } else if (!(lineActionModal.cgtTargetTypeId || '').trim() || !(lineActionModal.cgtMigrationReason || '').trim()) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.incompleteTitle', 'Incomplete') });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            try {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    await TelecomBssWizardClearance.changeGsm.ensureUploads(
                        lineActionModal,
                        sub.msisdn || '',
                        bssCgtOptionsList()
                    );
                }
                const cgtApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.changeGsm.buildApi(lineActionModal, bssCgtOptionsList())
                    : {};
                const body = {
                    kind: 6,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    targetSubscriptionTypeId: lineActionModal.cgtTargetTypeId,
                    gsmMigrationReason: (lineActionModal.cgtMigrationReason || '').trim(),
                    changeGsmProductOfferingId: (lineActionModal.cgtProductOfferingId || '').trim() || null,
                    paymentReference: cgtApi.paymentReference ?? null,
                    collectionNote: cgtApi.collectionNote ?? null,
                    kycDocumentReferenceId: cgtApi.kycDocumentReferenceId ?? null,
                    gsmEffectiveDateUtc:
                        typeof TelecomBssWizardClearance !== 'undefined'
                            ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal)
                            : new Date().toISOString(),
                    notes: `CustomerList|ChangeGsm|${sub.msisdn || '—'}`,
                };
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || telecomT('swal.failShort')), { response: createRes });
                }
                const opId = createRes?.data?.content?.data?.id;
                if (!opId) throw new Error(telecomT('swal.noOpId'));
                await finalizeListOpWithConfirm(opId, null, lineActionModal.bssIdentityFile);
                await loadCustomer360(state.id);
                hideBsModal('C360ChangeGsmModal');
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineProcessing[key] = false;
                lineActionModal.busy = false;
            }
        };

        const submitSimSwap = async () => {
            const sub = lineActionModal.sub;
            const iccid = (lineActionModal.simIccid || '').trim();
            const reason = (lineActionModal.simReplacementReason || '').trim();
            if (!sub || !reason) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.pickSimReason') });
                return;
            }
            if (iccid.length < 19) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.enterNewIccid') });
                return;
            }
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const simErr = TelecomBssWizardClearance.simSwap.validate(lineActionModal);
                if (simErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(simErr.key, simErr.key) });
                    return;
                }
                const effErr = TelecomBssWizardClearance.effectiveDate.validate(lineActionModal);
                if (effErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(effErr.key, effErr.key) });
                    return;
                }
            } else if (lineActionModal.simLostOrStolen && !lineActionModal.simIdentityFile) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.uploadIdentity') });
                return;
            }
            const simApi = typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.simSwap.buildApi(lineActionModal)
                : {};
            const simEffectiveUtc = typeof TelecomBssWizardClearance !== 'undefined'
                ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal)
                : new Date().toISOString();
            if (lineActionModal.simLostOrStolen) {
                const key = sub.id || '__line__';
                if (lineProcessing[key]) return;
                lineProcessing[key] = true;
                lineActionModal.busy = true;
                try {
                    const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', {
                        kind: 3,
                        subscriberProfileId: sub.subscriberProfileId,
                        msisdnAssetId: sub.msisdnAssetId || null,
                        simIccid: iccid,
                        replacementReason: reason,
                        isLostOrStolenReport: true,
                        agencyReference: simApi.agencyReference ?? null,
                        simSwapEffectiveDateUtc: simEffectiveUtc,
                        notes: `CustomerList|SimSwap|${sub.msisdn || '—'}`,
                    });
                    if (createRes?.data?.code !== 200) {
                        throw Object.assign(new Error(createRes?.data?.message || telecomT('swal.createRequestFailed')), {
                            response: createRes,
                        });
                    }
                    const opId = createRes?.data?.content?.data?.id;
                    if (!opId) throw new Error(telecomT('swal.noOpId'));
                    const form = new FormData();
                    form.append('id', opId);
                    form.append('file', lineActionModal.simIdentityFile);
                    await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: telecomT('swal.sentToBackOffice'),
                            text: telecomT('swal.sentToBackOfficeSimHint'),
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                    await loadCustomer360(state.id);
                    hideBsModal('C360SimSwapModal');
                } catch (e) {
                    showLineActionError(e);
                } finally {
                    lineProcessing[key] = false;
                    lineActionModal.busy = false;
                }
                return;
            }
            lineActionModal.busy = true;
            const ok = await runTelecomPipeline({
                processingKey: sub.id,
                kindLabel: 'SimSwap',
                msisdn: sub.msisdn,
                buildBody: () => ({
                    kind: 3,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    simIccid: iccid,
                    replacementReason: reason,
                    isLostOrStolenReport: false,
                    simSwapEffectiveDateUtc: simEffectiveUtc,
                }),
            });
            lineActionModal.busy = false;
            if (ok) hideBsModal('C360SimSwapModal');
        };

        const submitTermination = async () => {
            const sub = lineActionModal.sub;
            if (!sub) return;
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const trmErr = TelecomBssWizardClearance.termination.validate(lineActionModal);
                if (trmErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(trmErr.key, trmErr.key) });
                    return;
                }
                const effErr = TelecomBssWizardClearance.effectiveDate.validate(lineActionModal);
                if (effErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(effErr.key, effErr.key) });
                    return;
                }
            } else {
                const reason = (lineActionModal.trmTerminationReason || '').trim();
                const type = (lineActionModal.trmTerminationType || '').trim();
                if (!reason || !type) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.terminationIncomplete') });
                    return;
                }
                if (type === 'Voluntary' && !(lineActionModal.trmRetentionOfferOutcome || '').trim()) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.retentionRequired') });
                    return;
                }
            }
            if (listTrmRequiresLegacyIdentity.value && !lineActionModal.trmIdentityFile) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.uploadAdminDoc') });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            try {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    await TelecomBssWizardClearance.termination.ensureUploads(lineActionModal, sub.msisdn || '');
                }
                const trmApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.termination.buildApi(lineActionModal)
                    : {};
                const type = (lineActionModal.trmTerminationType || '').trim();
                const body = {
                    kind: 7,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    terminationType: type,
                    terminationReason: (lineActionModal.trmTerminationReason || '').trim(),
                    retentionOfferOutcome:
                        type === 'Voluntary' ? (lineActionModal.trmRetentionOfferOutcome || '').trim() : null,
                    paymentReference: trmApi.paymentReference ?? null,
                    agencyReference: trmApi.agencyReference ?? null,
                    collectionNote: trmApi.collectionNote ?? null,
                    kycDocumentReferenceId: trmApi.kycDocumentReferenceId ?? null,
                    terminationEffectiveDateUtc:
                        typeof TelecomBssWizardClearance !== 'undefined'
                            ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal)
                            : new Date().toISOString(),
                    notes: (lineActionModal.notes || '').trim()
                        ? lineActionModal.notes.trim()
                        : `CustomerList|Termination|${sub.msisdn || '—'}`,
                };
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || telecomT('swal.createRequestFailed')), {
                        response: createRes,
                    });
                }
                const opId = createRes?.data?.content?.data?.id;
                const entity = createRes?.data?.content?.data;
                if (!opId) throw new Error(telecomT('swal.noOpId'));
                lineActionModal.trmRequiresBackOffice =
                    String(entity?.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                    || lineActionModal.trmRequiresBackOffice;
                if (lineActionModal.trmRequiresBackOffice) {
                    const form = new FormData();
                    form.append('id', opId);
                    form.append('file', lineActionModal.trmIdentityFile);
                    await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: telecomT('swal.sentToBackOffice'),
                            text: telecomT('swal.pendingTrmHint'),
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    await finalizeListOpWithConfirm(opId, telecomT('swal.lineTerminated'), lineActionModal.trmIdentityFile);
                }
                await loadCustomer360(state.id);
                hideBsModal('C360TerminationModal');
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineProcessing[key] = false;
                lineActionModal.busy = false;
            }
        };

        const submitSuspension = async () => {
            const sub = lineActionModal.sub;
            if (!sub) return;
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const susErr = TelecomBssWizardClearance.suspension.validate(lineActionModal);
                if (susErr) {
                    listIncompleteSwal(susErr.key);
                    return;
                }
                const effErr = TelecomBssWizardClearance.effectiveDate.validate(lineActionModal);
                if (effErr) {
                    listIncompleteSwal(effErr.key);
                    return;
                }
                const endErr = TelecomBssWizardClearance.suspensionEndDate.validateEndDate(
                    lineActionModal,
                    listSuspensionStartDateLocal
                );
                if (endErr) {
                    listIncompleteSwal(endErr.key);
                    return;
                }
            } else if (!(lineActionModal.susSuspensionReason || '').trim()) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.suspensionReasonRequired') });
                return;
            }
            if (lineActionModal.susAutoReconnectEnabled && !(lineActionModal.susEndDateLocal || '').trim()) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.suspensionEndRequired') });
                return;
            }
            if (listSusRequiresStep2Identity.value && !lineActionModal.bssIdentityFile) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('suspension.kycRequired', 'Upload KYC document') });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            try {
                if (typeof TelecomBssWizardClearance !== 'undefined') {
                    await TelecomBssWizardClearance.suspension.ensureUploads(lineActionModal, sub.msisdn || '');
                }
                const susApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.suspension.buildApi(lineActionModal)
                    : {};
                const body = {
                    kind: 8,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    suspensionType: (lineActionModal.susSuspensionType || '').trim(),
                    suspensionReason: (lineActionModal.susSuspensionReason || '').trim(),
                    barringLevel: (lineActionModal.susBarringLevel || 'Full').trim(),
                    autoReconnectEnabled: !!lineActionModal.susAutoReconnectEnabled,
                    suspensionStartDateUtc:
                        typeof TelecomBssWizardClearance !== 'undefined'
                            ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal)
                            : new Date().toISOString(),
                    suspensionEndDateUtc:
                        lineActionModal.susAutoReconnectEnabled && lineActionModal.susEndDateLocal
                            ? new Date(lineActionModal.susEndDateLocal + 'T23:59:59').toISOString()
                            : null,
                    paymentReference: susApi.paymentReference ?? null,
                    agencyReference: susApi.agencyReference ?? null,
                    collectionNote: susApi.collectionNote ?? null,
                    kycDocumentReferenceId: susApi.kycDocumentReferenceId ?? null,
                    fraudClearanceConfirmed: susApi.fraudClearanceConfirmed ?? false,
                    notes: (lineActionModal.notes || '').trim()
                        ? lineActionModal.notes.trim()
                        : `CustomerList|Suspension|${sub.msisdn || '—'}`,
                };
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || telecomT('swal.failShort')), { response: createRes });
                }
                const opId = createRes?.data?.content?.data?.id;
                const entity = createRes?.data?.content?.data;
                if (!opId) throw new Error(telecomT('swal.noOpId'));
                lineActionModal.susRequiresBackOffice =
                    String(entity?.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                    || lineActionModal.susRequiresBackOffice;
                if (lineActionModal.susRequiresBackOffice) {
                    const form = new FormData();
                    form.append('id', opId);
                    if (listSusRequiresStep2Identity.value && lineActionModal.bssIdentityFile) {
                        form.append('file', lineActionModal.bssIdentityFile);
                        await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                            headers: { 'Content-Type': 'multipart/form-data' },
                        });
                    } else {
                        await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', { id: opId });
                    }
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: telecomT('swal.sentToBackOffice'),
                            text: telecomT('swal.pendingSusHint'),
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    await finalizeListOpWithConfirm(opId, telecomT('swal.lineSuspended'), lineActionModal.bssIdentityFile);
                }
                await loadCustomer360(state.id);
                hideBsModal('C360SuspensionModal');
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineProcessing[key] = false;
                lineActionModal.busy = false;
            }
        };

        const submitReconnect = async () => {
            const sub = lineActionModal.sub;
            if (!sub) return;
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const effErr = TelecomBssWizardClearance.effectiveDate.validate(lineActionModal);
                if (effErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(effErr.key, effErr.key) });
                    return;
                }
            }
            if (typeof TelecomReconnectClearance !== 'undefined') {
                const rcnErr = TelecomReconnectClearance.validate(lineActionModal, {
                    bdrApproved: listRcnBdrApproved.value,
                });
                if (rcnErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(rcnErr.key, rcnErr.key) });
                    return;
                }
            } else {
                const reason = (lineActionModal.rcnReconnectReason || '').trim();
                if (!reason) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.reconnectReasonRequired') });
                    return;
                }
                if (lineActionModal.rcnClearanceType === 'Payment' && !(lineActionModal.rcnPaymentReference || '').trim()) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.paymentRefRequired') });
                    return;
                }
            }
            await loadListReconnectEligibility();
            if (
                lineActionModal.rcnEligibility
                && !lineActionModal.rcnEligibility.allowed
                && !lineActionModal.rcnEligibility.Allowed
            ) {
                const msg =
                    lineActionModal.rcnEligibility.messageAr || lineActionModal.rcnEligibility.MessageAr || '';
                if (window.Swal) Swal.fire({ icon: 'error', title: telecomT('swal.notAllowed'), text: msg });
                return;
            }
            if (!lineActionModal.rcnIdentityFile) {
                if (window.Swal) {
                    Swal.fire({
                        icon: 'warning',
                        title: telecomT('suspension.kycRequired', 'Upload document'),
                        text: telecomT('suspension.kycDocumentHint', ''),
                    });
                }
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            try {
                if (typeof TelecomReconnectClearance !== 'undefined') {
                    await TelecomReconnectClearance.ensureRegulatoryAttachmentUploaded(lineActionModal, sub.msisdn || '');
                }
                const rcnFields = typeof TelecomReconnectClearance !== 'undefined'
                    ? TelecomReconnectClearance.buildApiFields(lineActionModal, { bdrApproved: listRcnBdrApproved.value })
                    : {
                        paymentReference: (lineActionModal.rcnPaymentReference || '').trim() || null,
                        agencyReference: null,
                        collectionNote: null,
                        kycDocumentReferenceId: null,
                        fraudClearanceConfirmed: !!lineActionModal.rcnFraudClearanceConfirmed,
                    };
                const reason = (lineActionModal.rcnReconnectReason || '').trim();
                const body = {
                    kind: 9,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    reconnectReason: reason,
                    clearanceType: (lineActionModal.rcnClearanceType || '').trim(),
                    fraudClearanceConfirmed: rcnFields.fraudClearanceConfirmed,
                    paymentReference: rcnFields.paymentReference,
                    agencyReference: rcnFields.agencyReference,
                    collectionNote: rcnFields.collectionNote,
                    kycDocumentReferenceId: rcnFields.kycDocumentReferenceId,
                    notes: `CustomerList|Reconnect|${sub.msisdn || '—'}`,
                    reconnectEffectiveDateUtc:
                        typeof TelecomBssWizardClearance !== 'undefined'
                            ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal)
                            : new Date().toISOString(),
                };
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || telecomT('swal.failShort')), { response: createRes });
                }
                const opId = createRes?.data?.content?.data?.id;
                const entity = createRes?.data?.content?.data;
                if (!opId) throw new Error(telecomT('swal.noOpId'));
                lineActionModal.rcnRequiresBackOffice =
                    String(entity?.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                    || lineActionModal.rcnRequiresBackOffice;
                if (lineActionModal.rcnRequiresBackOffice) {
                    if (typeof TelecomWizardConfirm !== 'undefined') {
                        await TelecomWizardConfirm.uploadOperationIdentityDocument(
                            opId,
                            lineActionModal.rcnIdentityFile
                        );
                    } else {
                        const form = new FormData();
                        form.append('id', opId);
                        form.append('file', lineActionModal.rcnIdentityFile);
                        await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                            headers: { 'Content-Type': 'multipart/form-data' },
                        });
                    }
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: telecomT('swal.sentToBackOffice'),
                            text: telecomT('swal.pendingRcnHint'),
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    if (typeof TelecomWizardConfirm !== 'undefined') {
                        await TelecomWizardConfirm.uploadOperationIdentityDocument(
                            opId,
                            lineActionModal.rcnIdentityFile
                        );
                    } else {
                        const form = new FormData();
                        form.append('id', opId);
                        form.append('file', lineActionModal.rcnIdentityFile);
                        await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                            headers: { 'Content-Type': 'multipart/form-data' },
                        });
                    }
                    const confirmRes = await confirmListOperationIfAllowed(opId, false);
                    if (!confirmRes) {
                        notifyListConfirmSkipped();
                    } else if (confirmRes?.data?.code !== 200) {
                        throw Object.assign(new Error(confirmRes?.data?.message || telecomT('swal.confirmFailed')), {
                            response: confirmRes,
                        });
                    } else {
                        const confirmContent = confirmRes?.data?.content ?? confirmRes?.data?.Content ?? {};
                        const scheduled =
                            window.TelecomUiBadges?.isScheduledOperationStatus?.(
                                window.TelecomUiBadges?.operationStatusFromConfirm?.(confirmContent)
                            ) ?? false;
                        if (
                            !scheduled
                            && (confirmContent.hlrCompletesAsynchronously ?? confirmContent.HlrCompletesAsynchronously)
                        ) {
                            const terminal = new Set([3, 4, 'Completed', 'Failed']);
                            for (let i = 0; i < 15; i++) {
                                await new Promise((r) => setTimeout(r, 2000));
                                try {
                                    const detailRes = await AxiosManager.get(
                                        '/Telecom/GetTelecomOperationDetail?id=' + encodeURIComponent(opId),
                                        {}
                                    );
                                    const detail = detailRes?.data?.content?.data ?? detailRes?.data?.content?.Data;
                                    const st = detail?.status ?? detail?.Status;
                                    if (terminal.has(st)) break;
                                } catch {
                                    /* retry */
                                }
                            }
                        }
                        showTelecomConfirmResult(confirmRes, telecomT('swal.reconnected'));
                    }
                }
                await loadCustomer360(state.id);
                await checkHlrForSubscription(sub);
                hideBsModal('C360ReconnectModal');
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineProcessing[key] = false;
                lineActionModal.busy = false;
            }
        };

        const submitRefund = async () => {
            const sub = lineActionModal.sub;
            if (!sub) return;
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const rfdErr = TelecomBssWizardClearance.refund.validate(lineActionModal);
                if (rfdErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(rfdErr.key, rfdErr.key) });
                    return;
                }
                const effErr = TelecomBssWizardClearance.effectiveDate.validate(lineActionModal);
                if (effErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(effErr.key, effErr.key) });
                    return;
                }
            } else {
                const reason = (lineActionModal.rfdRefundReason || '').trim();
                const amt = Number(lineActionModal.rfdRefundAmount);
                if (!reason || !amt || amt <= 0) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.refundIncomplete') });
                    return;
                }
            }
            const rfdTy = (lineActionModal.rfdRefundType || '').trim();
            const rfdMethod = (lineActionModal.rfdRefundMethod || '').trim();
            const rfdAmt = Number(lineActionModal.rfdRefundAmount) || 0;
            lineActionModal.rfdRequiresBackOffice =
                rfdTy === 'SyriatelCash' || rfdAmt > 500000 || (rfdMethod === 'Cash' && rfdAmt > 500000);
            if (lineActionModal.rfdRequiresBackOffice && !lineActionModal.rfdIdentityFile) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.uploadApprovalDoc') });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            try {
                const rfdApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.refund.buildApi(lineActionModal)
                    : {};
                const body = {
                    kind: 11,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    refundType: (lineActionModal.rfdRefundType || '').trim(),
                    refundMethod: (lineActionModal.rfdRefundMethod || '').trim(),
                    refundReason: (lineActionModal.rfdRefundReason || '').trim(),
                    refundAmount: Number(lineActionModal.rfdRefundAmount),
                    refundCbsReference: rfdApi.refundCbsReference ?? null,
                    refundGatewayReference: rfdApi.refundGatewayReference ?? null,
                    refundEffectiveDateUtc:
                        typeof TelecomBssWizardClearance !== 'undefined'
                            ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal)
                            : new Date().toISOString(),
                    notes: `CustomerList|Refund|${sub.msisdn || '—'}`,
                };
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || telecomT('swal.failShort')), { response: createRes });
                }
                const opId = createRes?.data?.content?.data?.id;
                const entity = createRes?.data?.content?.data;
                if (!opId) throw new Error(telecomT('swal.noOpId'));
                lineActionModal.rfdRequiresBackOffice =
                    String(entity?.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                    || !!entity?.requiresDualApproval
                    || lineActionModal.rfdRequiresBackOffice;
                lineActionModal.rfdDepositSnapshot = entity?.depositBalanceSnapshot ?? entity?.DepositBalanceSnapshot ?? null;
                lineActionModal.rfdWalletSnapshot = entity?.walletBalanceSnapshot ?? entity?.WalletBalanceSnapshot ?? null;
                if (lineActionModal.rfdRequiresBackOffice) {
                    const form = new FormData();
                    form.append('id', opId);
                    form.append('file', lineActionModal.rfdIdentityFile);
                    await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: telecomT('swal.sentToBackOffice'),
                            text: telecomT('swal.pendingRfdHint'),
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    await finalizeListOpWithConfirm(opId, telecomT('swal.refundDone'), lineActionModal.rfdIdentityFile);
                }
                await loadCustomer360(state.id);
                hideBsModal('C360RefundModal');
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineProcessing[key] = false;
                lineActionModal.busy = false;
            }
        };

        const submitBadDebt = async () => {
            const sub = lineActionModal.sub;
            if (!sub) return;
            if (lineActionModal.bdrCollectionAction === 'PaymentRecorded') {
                if (!(lineActionModal.bdrPaymentReference || '').trim()) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.paymentRefRequired') });
                    return;
                }
                if (!(Number(lineActionModal.bdrCollectedAmount) > 0)) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.collectionAmountRequired') });
                    return;
                }
            }
            if (
                (lineActionModal.bdrCollectionAction === 'WriteOffPartial'
                    || lineActionModal.bdrCollectionAction === 'WriteOffFull')
                && !(Number(lineActionModal.bdrWriteOffAmount) > 0)
            ) {
                if (window.Swal) {
                    Swal.fire({ icon: 'warning', title: telecomT('badDebt.writeOffAmount', 'Write-off required') });
                }
                return;
            }
            await loadListBadDebtEligibility();
            if (
                lineActionModal.bdrEligibility
                && !lineActionModal.bdrEligibility.allowed
                && !lineActionModal.bdrEligibility.Allowed
            ) {
                const msg =
                    lineActionModal.bdrEligibility.messageAr || lineActionModal.bdrEligibility.MessageAr || '';
                if (window.Swal) Swal.fire({ icon: 'error', title: telecomT('swal.notAllowed'), text: msg });
                return;
            }
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const effErr = TelecomBssWizardClearance.effectiveDate.validate(lineActionModal);
                if (effErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(effErr.key, effErr.key) });
                    return;
                }
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            try {
                const body = {
                    kind: 12,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    collectionAction: (lineActionModal.bdrCollectionAction || '').trim(),
                    dunningStage: (lineActionModal.bdrDunningStage || '').trim(),
                    collectedAmount: lineActionModal.bdrCollectedAmount
                        ? Number(lineActionModal.bdrCollectedAmount)
                        : null,
                    writeOffAmount: lineActionModal.bdrWriteOffAmount
                        ? Number(lineActionModal.bdrWriteOffAmount)
                        : null,
                    paymentReference: (lineActionModal.bdrPaymentReference || '').trim() || null,
                    agencyReference: (lineActionModal.bdrAgencyReference || '').trim() || null,
                    paymentPlanMonths: lineActionModal.bdrPaymentPlanMonths
                        ? Number(lineActionModal.bdrPaymentPlanMonths)
                        : null,
                    collectionApprovalConfirmed: !!lineActionModal.bdrSupervisorConfirmed,
                    badDebtEffectiveDateUtc:
                        typeof TelecomBssWizardClearance !== 'undefined'
                            ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal)
                            : new Date().toISOString(),
                    notes: (lineActionModal.notes || '').trim()
                        ? lineActionModal.notes.trim()
                        : `CustomerList|BadDebt|${sub.msisdn || '—'}`,
                };
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || telecomT('swal.failShort')), { response: createRes });
                }
                const opId = createRes?.data?.content?.data?.id;
                if (!opId) throw new Error(telecomT('swal.noOpId'));
                lineActionModal.bdrRequiresBackOffice =
                    String(createRes?.data?.content?.data?.approvalLevelRequired || '').toLowerCase() ===
                        'backoffice'
                    || lineActionModal.bdrRequiresBackOffice;
                if (lineActionModal.bdrRequiresBackOffice) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: telecomT('swal.sentToBackOffice'),
                            text: telecomT('swal.pendingBdrHint'),
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    await finalizeListOpWithConfirm(opId, telecomT('swal.collectionDone'), lineActionModal.bssIdentityFile);
                }
                await loadCustomer360(state.id);
                hideBsModal('C360BadDebtModal');
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineProcessing[key] = false;
                lineActionModal.busy = false;
            }
        };

        const submitChangeNumber = async () => {
            const sub = lineActionModal.sub;
            const targetId = listCnShowsInternalPool.value
                ? (lineActionModal.cnTargetMsisdnAssetId || '').trim()
                : (lineActionModal.cnPortInMsisdn || '').trim();
            const reason = (lineActionModal.cnNumberChangeReason || '').trim();
            if (!sub || !targetId || !reason) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.changeNumberIncomplete') });
                return;
            }
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const cnErr = TelecomBssWizardClearance.changeNumber.validate(lineActionModal);
                if (cnErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(cnErr.key, cnErr.key) });
                    return;
                }
                const effErr = TelecomBssWizardClearance.effectiveDate.validate(lineActionModal);
                if (effErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(effErr.key, effErr.key) });
                    return;
                }
            }
            if (lineActionModal.cnRequiresBackOffice && !lineActionModal.cnPaymentFile) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.uploadPaymentReceipt') });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            try {
                if (listCnShowsInternalPool.value) {
                    await AxiosManager.post('/Telecom/ReserveMsisdnForCustomer', {
                        msisdnAssetId: targetId,
                        customerId: state.id,
                    });
                }
                const cnApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.changeNumber.buildApi(lineActionModal)
                    : {};
                const body = {
                    kind: 5,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    numberChangeMode: lineActionModal.cnChangeMode || 'Internal',
                    numberChangeReason: reason,
                    notes: `Customer360|ChangeNumber|${sub.msisdn || '—'}`,
                };
                if (listCnShowsInternalPool.value) {
                    body.targetMsisdnAssetId = targetId;
                } else {
                    body.portInMsisdn = targetId;
                    body.donorOperatorCode = (lineActionModal.cnDonorOperatorCode || '').trim() || null;
                    body.agencyReference = cnApi.agencyReference ?? null;
                }
                if (lineActionModal.cnPremiumFeeAmount) {
                    body.premiumFeeAmount = Number(lineActionModal.cnPremiumFeeAmount);
                }
                body.paymentReference = cnApi.paymentReference ?? null;
                body.numberChangeEffectiveDateUtc =
                    typeof TelecomBssWizardClearance !== 'undefined'
                        ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal)
                        : new Date().toISOString();
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || telecomT('swal.createRequestFailed')), {
                        response: createRes,
                    });
                }
                const opId = createRes?.data?.content?.data?.id;
                if (!opId) throw new Error(telecomT('swal.noOpId'));
                if (lineActionModal.cnRequiresBackOffice) {
                    const form = new FormData();
                    form.append('id', opId);
                    form.append('file', lineActionModal.cnPaymentFile);
                    await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: telecomT('swal.sentToBackOffice'),
                            text: telecomT('swal.pendingCnrHint'),
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    await finalizeListOpWithConfirm(
                        opId,
                        telecomT('swal.changeNumberDone'),
                        lineActionModal.cnPaymentFile || lineActionModal.bssIdentityFile
                    );
                }
                await loadCustomer360(state.id);
                hideBsModal('C360ChangeNumberModal');
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineProcessing[key] = false;
                lineActionModal.busy = false;
            }
        };

        const submitTakeOver = async () => {
            const sub = lineActionModal.sub;
            const secondary = (lineActionModal.takeoverTargetProfileId || '').trim();
            if (!sub || !secondary) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.takeoverOwnerRequired') });
                return;
            }
            if (typeof TelecomBssWizardClearance !== 'undefined') {
                const tkoErr = TelecomBssWizardClearance.takeOver.validate(lineActionModal, bssTkoOptionsList());
                if (tkoErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(tkoErr.key, tkoErr.key) });
                    return;
                }
                const effErr = TelecomBssWizardClearance.effectiveDate.validate(lineActionModal);
                if (effErr) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT(effErr.key, effErr.key) });
                    return;
                }
            } else {
                if (!(lineActionModal.takeoverTransferReason || '').trim()) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.takeoverReasonRequired') });
                    return;
                }
            }
            if (!lineActionModal.takeoverIdentityFile) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: telecomT('swal.takeoverUploadIdentity') });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            try {
                const tkoApi = typeof TelecomBssWizardClearance !== 'undefined'
                    ? TelecomBssWizardClearance.takeOver.buildApi(lineActionModal, bssTkoOptionsList())
                    : {};
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', {
                    kind: 2,
                    subscriberProfileId: sub.subscriberProfileId,
                    secondarySubscriberProfileId: secondary,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    transferReason: (lineActionModal.takeoverTransferReason || '').trim(),
                    depositTransferPolicy: Number(lineActionModal.takeoverDepositPolicy) || 1,
                    paymentReference: tkoApi.paymentReference ?? null,
                    takeOverObligationStatus: tkoApi.takeOverObligationStatus ?? null,
                    takeOverEffectiveDateUtc:
                        typeof TelecomBssWizardClearance !== 'undefined'
                            ? TelecomBssWizardClearance.effectiveDate.resolveUtcIso(lineActionModal)
                            : new Date().toISOString(),
                    notes: `Customer360|TakeOver|${sub.msisdn || '—'}`,
                });
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || telecomT('swal.createRequestFailed')), { response: createRes });
                }
                const opId = createRes?.data?.content?.data?.id;
                if (!opId) throw new Error(telecomT('swal.noOpId'));
                const form = new FormData();
                form.append('id', opId);
                form.append('file', lineActionModal.takeoverIdentityFile);
                await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                    headers: { 'Content-Type': 'multipart/form-data' },
                });
                if (window.Swal) {
                    Swal.fire({
                        icon: 'success',
                        title: telecomT('swal.sentToBackOffice'),
                        text: telecomT('swal.takeoverSentHint'),
                        timer: 2800,
                        showConfirmButton: false,
                    });
                }
                await loadCustomer360(state.id);
                hideBsModal('C360TakeOverModal');
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineProcessing[key] = false;
                lineActionModal.busy = false;
            }
        };

        const pipelineStepsForOp = (status) => {
            if (window.TelecomUiBadges?.pipelineStepsForOperationStatus) {
                return window.TelecomUiBadges.pipelineStepsForOperationStatus(status, (k, fb) =>
                    telecomT(k, fb)
                );
            }
            const s = Number(status);
            const failed = s === 4;
            return [
                { label: telecomT('ops.pipeline.draft', 'Draft'), done: s !== 4, active: s === 0, failed },
                {
                    label: telecomT('ops.pipeline.docs', 'Documents'),
                    done: s >= 5 || s === 6 || s === 3 || s === 1 || s === 11,
                    active: s === 5,
                    failed,
                },
                {
                    label: telecomT('ops.pipeline.scheduled', 'Scheduled'),
                    done: s === 3 || s === 1 || s === 6,
                    active: s === 11,
                    failed,
                },
                { label: telecomT('ops.pipeline.provisioning', 'Provisioning'), done: s === 3 || s === 1, active: s === 6, failed },
                { label: telecomT('ops.pipeline.done', 'Done'), done: s === 3, active: false, failed },
            ];
        };

        const loadCustomer360 = async (customerId) => {
            const cid = (customerId || '').trim();
            if (!cid) {
                state.customer360 = null;
                return;
            }
            state.customer360Busy = true;
            state.customer360Error = null;
            state.lineWallets = {};
            try {
                const res = await AxiosManager.get('/Customer/GetCustomer360?customerId=' + encodeURIComponent(cid), {});
                state.customer360 = res?.data?.content ?? null;
                if (state.customer360?.nationalIdMasked) {
                    state.nationalIdMasked = state.customer360.nationalIdMasked;
                }
                const subs = state.customer360?.activeSubscriptions || [];
                await loadCustomer360LineWallets(cid, subs);
                for (const sub of subs) {
                    if (sub.msisdn && gridAccess.canToggleVas) loadVasPanelForMsisdn(sub.msisdn);
                }
            } catch (e) {
                state.customer360 = null;
                state.customer360Error = e?.response?.data?.message || e?.message || telecomT('swal.loadFailed', 'Load failed');
            } finally {
                state.customer360Busy = false;
            }
        };

        const formatWalletAmount = (val) => {
            if (val == null || val === '') return '—';
            const n = Number(val);
            if (Number.isNaN(n)) return String(val);
            return n.toLocaleString('ar-SY', { maximumFractionDigits: 2 });
        };

        const walletBucketLabel = (b) => {
            const label = b?.label ?? b?.Label;
            if (label) return label;
            const t = b?.componentType ?? b?.ComponentType;
            const map = {
                0: telecomT('c360.walletVoice', 'Voice'),
                1: telecomT('c360.walletData', 'Data'),
                2: telecomT('c360.walletSms', 'SMS'),
                Voice: telecomT('c360.walletVoice', 'Voice'),
                Data: telecomT('c360.walletData', 'Data'),
                Sms: telecomT('c360.walletSms', 'SMS'),
            };
            return map[t] ?? String(t ?? '—');
        };

        const lineWalletForSub = (subscriptionId) => state.lineWallets[subscriptionId] ?? null;

        const loadCustomer360LineWallets = async (customerId, subs) => {
            const list = subs || [];
            list.forEach((s) => {
                state.lineWalletsBusy[s.id] = true;
            });
            try {
                const res = await AxiosManager.get(
                    '/Customer/GetCustomer360LineWallets?customerId=' + encodeURIComponent(customerId),
                    {}
                );
                const map =
                    res?.data?.content?.walletsBySubscriptionId ??
                    res?.data?.content?.WalletsBySubscriptionId ??
                    {};
                state.lineWallets = { ...map };
            } catch {
                state.lineWallets = {};
            } finally {
                list.forEach((s) => {
                    state.lineWalletsBusy[s.id] = false;
                });
            }
        };

        const pollPaymentDetailList = async (paymentId, maxAttempts = 12) => {
            if (typeof TelecomRechargeFlow !== 'undefined') {
                return TelecomRechargeFlow.pollPaymentDetail(AxiosManager, paymentId, maxAttempts);
            }
            for (let i = 0; i < maxAttempts; i++) {
                const res = await AxiosManager.get(
                    '/Telecom/GetPaymentTransactionDetail?id=' + encodeURIComponent(paymentId),
                    {}
                );
                const detail = res?.data?.content ?? res?.data?.Content;
                const status = detail?.status ?? detail?.Status;
                if (status === 2 || status === 'Completed') return detail;
                if (status === 3 || status === 'Failed') {
                    throw new Error(detail?.failureReason || detail?.FailureReason || telecomT('swal.paymentTxnFailed'));
                }
                await new Promise((r) => setTimeout(r, 500));
            }
            return null;
        };

        const listRechargeFlowLabels = () => ({
            methodTitle: telecomT('swal.rechargeMethod'),
            methodHint: telecomT('swal.rechargeMethodHint', 'Wallet/cash: amount + receipt. Voucher: prepaid card code.'),
            wallet: telecomT('swal.rechargeWallet'),
            voucher: telecomT('swal.rechargeVoucher'),
            continueBtn: telecomT('swal.continueBtn'),
            cancelBtn: telecomT('common.cancel', 'Cancel'),
            amountTitle: telecomT('swal.rechargeAmount'),
            amountPlaceholder: '15000',
            amountHint: telecomT('swal.rechargeAmountHint', 'Cash amount in SYP received from the customer.'),
            amountFooter: telecomT('swal.rechargeAmountFooter', 'Tip: typical demo amounts are 15,000 or 30,000 SYP.'),
            amountEmpty: telecomT('swal.rechargeAmountEmpty', 'Please enter the cash amount.'),
            amountInvalid: telecomT('swal.rechargeAmountInvalid', 'Use numbers only (greater than zero).'),
            invalidAmount: telecomT('swal.invalidAmount'),
            amountTooHigh: telecomT('swal.rechargeAmountTooHigh', 'Maximum recharge amount is {max} SYP.'),
            amountMax: String(TelecomRechargeFlow?.DEFAULT_AMOUNT_MAX || 1000000),
            paymentRefTitle: telecomT('swal.paymentRefTitle'),
            refHint: telecomT('swal.rechargeRefHint', 'Receipt or cashier reference — required for audit.'),
            refEmpty: telecomT('swal.rechargeRefEmpty', 'Payment reference is required.'),
            confirmBtn: telecomT('swal.confirm'),
            refRequired: telecomT('swal.refRequired'),
            voucherCodeTitle: telecomT('swal.voucherCodeTitle'),
            voucherHint: telecomT('swal.rechargeVoucherHint', 'Enter the prepaid voucher code.'),
            validateBtn: telecomT('swal.validate', 'Validate'),
            voucherRequired: telecomT('swal.enterVoucherCode'),
            voucherInvalid: telecomT('swal.invalidVoucher'),
            draftMissing: telecomT('swal.noOpId'),
            confirmFailed: telecomT('swal.confirmFailed'),
            missingContext: telecomT('swal.noMsisdnForRecharge'),
        });

        const executeListPaymentFlow = async (customerId, subscriptionId, busyKey) => {
            if (typeof TelecomRechargeFlow === 'undefined') {
                Swal.fire({ icon: 'error', title: telecomT('swal.rechargeFailed') });
                return;
            }
            state.rechargeBusy = busyKey;
            try {
                const confirm = await TelecomRechargeFlow.runFlow(AxiosManager, Swal, {
                    customerId,
                    subscriptionId,
                    labels: listRechargeFlowLabels(),
                });
                if (!confirm) return;
                await loadCustomer360(state.id);
                Swal.fire({
                    icon: 'success',
                    title: telecomT('swal.rechargeOk'),
                    text: confirm?.messageAr || confirm?.MessageAr,
                });
            } catch (e) {
                Swal.fire({ icon: 'error', title: e?.message || telecomT('swal.rechargeFailed') });
            } finally {
                state.rechargeBusy = '';
            }
        };

        const openRechargeLineModal = async (sub) => {
            if (!state.id || !sub?.id) return;
            const wallet = lineWalletForSub(sub.id);
            const msisdn = sub.msisdn || wallet?.msisdn || '';
            if (!msisdn) {
                Swal.fire({ icon: 'warning', title: telecomT('swal.noMsisdnForRecharge') });
                return;
            }
            await executeListPaymentFlow(state.id, sub.id, sub.id);
        };

        const revealNationalId = async () => {
            if (!gridAccess.canViewDecryptedPii || !state.id) return;
            try {
                const res = await AxiosManager.get('/Customer/RevealNationalId?customerId=' + encodeURIComponent(state.id), {});
                const c = res?.data?.content;
                if (c?.allowed && c?.nationalId) {
                    state.nationalId = c.nationalId;
                    state.nationalIdRevealed = true;
                } else if (window.Swal) {
                    Swal.fire({
                        icon: 'info',
                        title: telecomT('swal.unavailable'),
                        text: telecomT('swal.noNationalId'),
                    });
                }
            } catch (e) {
                if (window.Swal) {
                    Swal.fire({
                        icon: 'error',
                        title: telecomT('swal.permissionError'),
                        text: e?.response?.data?.message || 'ViewDecryptedPII',
                    });
                }
            }
        };

        const checkHlrForSubscription = async (sub) => {
            const pid = (sub?.subscriberProfileId || '').trim();
            if (!pid) return;
            state.hlrSubscriberProfileId = pid;
            await queryCustomerHlrLiveStatus();
        };

        const resyncHlrForSubscription = async (sub) => {
            const pid = (sub?.subscriberProfileId || '').trim();
            if (!pid) return;
            state.hlrSubscriberProfileId = pid;
            await resyncCustomerFromHlr();
            await loadCustomer360(state.id);
        };

        const uniqueSubscriberProfileIds = () => {
            const rows = state.telecomSubscriptionRows || [];
            const ids = [...new Set(rows.map((r) => (r.subscriberProfileId || '').trim()).filter(Boolean))];
            return ids;
        };

        const queryCustomerHlrLiveStatus = async () => {
            const pid = (state.hlrSubscriberProfileId || '').trim();
            if (!pid) return;
            state.hlrLiveBusy = true;
            state.hlrLiveData = null;
            try {
                const res = await AxiosManager.get(
                    '/Telecom/QueryHlrLiveStatus?subscriberProfileId=' + encodeURIComponent(pid),
                    {}
                );
                state.hlrLiveData = res?.data?.content ?? null;
            } catch (e) {
                state.hlrLiveData = { success: false, message: e?.response?.data?.message || e?.message || 'HLR' };
            } finally {
                state.hlrLiveBusy = false;
            }
        };

        const resyncCustomerFromHlr = async () => {
            const pid = (state.hlrSubscriberProfileId || '').trim();
            if (!pid) return;
            state.hlrResyncBusy = true;
            try {
                const res = await AxiosManager.post('/Telecom/ResyncSubscriberFromHlr', {
                    subscriberProfileId: pid,
                });
                const body = res?.data?.content;
                if (res?.data?.code === 200 && body?.success) {
                    await queryCustomerHlrLiveStatus();
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: telecomT('swal.syncOk'),
                            text: body.message || '',
                            timer: 1800,
                            showConfirmButton: false,
                        });
                    }
                } else if (window.Swal) {
                    Swal.fire({
                        icon: 'error',
                        title: telecomT('swal.syncFailed'),
                        text: body?.message || res?.data?.message || '',
                    });
                }
            } catch (e) {
                if (window.Swal) {
                    Swal.fire({
                        icon: 'error',
                        title: telecomT('swal.syncFailed'),
                        text: e?.response?.data?.message || e?.message || 'HLR',
                    });
                }
            } finally {
                state.hlrResyncBusy = false;
            }
        };

        return {
            telecomT,
            mainGridRef,
            mainModalRef,
            manageContactModalRef,
            secondaryGridRef,
            nameRef,
            numberRef,
            streetRef,
            cityRef,
            stateRef,
            zipCodeRef,
            countryRef,
            phoneNumberRef,
            faxNumberRef,
            emailAddressRef,
            websiteRef,
            whatsAppRef,
            linkedInRef,
            facebookRef,
            instagramRef,
            twitterXRef,
            tikTokRef,
            customerGroupIdRef,
            customerCategoryIdRef,
            gridAccess,
            state,
            handler,
            customerOnboarding,
            onCustomerTelecomSubscriptionChange,
            onBootstrapMsisdnPicked,
            uniqueSubscriberProfileIds,
            queryCustomerHlrLiveStatus,
            resyncCustomerFromHlr,
            loadCustomer360,
            revealNationalId,
            pipelineStepsForOp,
            operationStatusBadge,
            formatOpEffectiveDate,
            lineWalletForSub,
            formatWalletAmount,
            walletBucketLabel,
            openRechargeLineModal,
            checkHlrForSubscription,
            resyncHlrForSubscription,
            vasPanels,
            vasPanelLoading,
            vasPanelError,
            isVasToggling,
            toggleVasService,
            lineActionModal,
            activationChannelUiMode: Vue.computed(() =>
                typeof ActivationChannelUi !== 'undefined' ? ActivationChannelUi.resolveMode() : 'showroom'),
            activationChannelLabels: Vue.computed(() => {
                const loc = window.TelecomI18n?.getLang?.() || 'ar';
                if (typeof ActivationChannelUi === 'undefined') {
                    return { showroom: 'POS', dealer: 'Dealer' };
                }
                return {
                    showroom: ActivationChannelUi.label(0, loc),
                    dealer: ActivationChannelUi.label(1, loc),
                };
            }),
            activationChannelLockedHint: Vue.computed(() => {
                const loc = window.TelecomI18n?.getLang?.() || 'ar';
                if (typeof ActivationChannelUi === 'undefined') {
                    return telecomT('wizardUi.channelShowroomLockedHint', '');
                }
                return ActivationChannelUi.lockedHint(loc);
            }),
            newLineOfferingDeposit,
            fetchNewLinePaymentFromCashier,
            isLineProcessing,
            hasAnyLineAction,
            openNewLineModal,
            onNewLineMsisdnChanged,
            onActivateKycFileChange,
            openMigrateModal,
            openChangeGsmModal,
            openSimSwapModal,
            openVasModal,
            submitVasFromList,
            openChangeNumberModal,
            openTerminationModal,
            openSuspensionModal,
            openReconnectModal,
            openRefundModal,
            openBadDebtModal,
            openTakeOverModal,
            submitSuspension,
            submitReconnect,
            onListRcnClearanceChange,
            onListRcnRegulatoryFileChange,
            onListRcnIdentityFileChange,
            listRcnShowsPaymentRef,
            listRcnBdrApproved,
            listRcnShowsFraudFields,
            listRcnShowsRegulatoryFields,
            submitRefund,
            submitBadDebt,
            onBdrActionChangeList,
            onSusTypeChangeList,
            onListBssRegulatoryFileChange,
            listSusShowsPayment,
            listSusShowsFraud,
            listSusShowsRegulatory,
            listSusRequiresStep2Identity,
            onListSusIdentityFileChange,
            listTrmShowsPayment,
            listTrmShowsFraud,
            listTrmShowsRegulatory,
            listTrmRequiresLegacyIdentity,
            listRfdShowsOriginalTxRef,
            listRfdShowsPayoutDestination,
            listRfdPayoutLabelKey,
            onRefundMethodChangeList,
            listSimShowsLostStolenFields,
            listCnShowsPremiumPayment,
            listCnShowsInternalPool,
            listCnDonorOperators,
            onCnChangeModeChangeList,
            listTkoShowsObligationSettlement,
            listCgtShowsFinancial,
            listCgtShowsRegulatory,
            onListCgtPathChange,
            onListCgtIdentityFileChange,
            onListCgtTargetTypeChanged,
            onSimLostOrStolenChangeList,
            loadListReconnectEligibility,
            loadListBadDebtEligibility,
            onRefundTypeChangeList,
            onRefundIdentityFileChange,
            submitNewLineActivation,
            isCorporateCustomer,
            listActivationLineTypeOptions,
            listLineTypeDisplayName,
            listFilteredActivatePool,
            onListActivationLineTypeChange,
            searchListActivateSecondary,
            selectListActivateSecondary,
            onListMigrateOfferingChanged,
            onListActivateOfferingChanged,
            listActivateOfferName,
            listActivateOfferSummary,
            listActivateOfferVoice,
            listActivateOfferData,
            listActivateOfferSms,
            listBssEffectiveToday,
            barringLevelOptions,
            listIncompleteSwal,
            listSusShowsSimple,
            onListSusAutoReconnectChange,
            listSuspensionStartDateLocal,
            listSuspensionMaxEndDateLocal,
            listMigrateOfferName,
            listMigrateOfferSummary,
            listMigrateOfferVoice,
            listMigrateOfferData,
            listMigrateOfferSms,
            listMgrProrationPriceDifferenceLabel,
            listMgrProrationAmountLabel,
            listMgrProrationWalletLabel,
            listMgrProrationDaysLabel,
            listMgrProrationSufficient,
            submitMigrate,
            submitChangeGsm,
            submitSimSwap,
            submitChangeNumber,
            openDeviceSaleModal,
            submitDeviceSale,
            onListDevicePicked,
            submitTermination,
            submitTakeOver,
            onTakeoverIdentityFileChange,
            onSimSwapIdentityFileChange,
            onChangeNumberPaymentFileChange,
            onChangeNumberTargetPickedList,
            onTerminationTypeChangeList,
            onTerminationIdentityFileChange,
            isLineTerminated,
            isLineSuspended,
            goToC360Wizard,
            searchTakeoverTarget,
            selectTakeoverTarget,
            openSupportTicketModal: () => {
                const subs = state.customer360?.activeSubscriptions || [];
                const primary = subs.find((s) => s.isPrimaryLine) || subs[0];
                if (!(primary?.msisdn || '').trim()) {
                    Swal.fire({ icon: 'warning', title: telecomT('supportTicket.noMsisdn', 'No MSISDN') });
                    return;
                }
                lineActionModal.sub = primary;
                lineActionModal.supportIssueType = 0;
                lineActionModal.supportNotes = '';
                lineActionModal.supportBusy = false;
                showBsModal('C360SupportTicketModal');
            },
            submitSupportTicket: async () => {
                const sub = lineActionModal.sub;
                const msisdn = (sub?.msisdn || '').trim();
                if (!msisdn) {
                    Swal.fire({ icon: 'warning', title: telecomT('supportTicket.noMsisdn', 'No MSISDN') });
                    return;
                }
                if (lineActionModal.supportBusy) return;
                lineActionModal.supportBusy = true;
                try {
                    const res = await AxiosManager.post('/TelecomBackOffice/CreateTechnicalTicket', {
                        msisdn,
                        issueType: Number(lineActionModal.supportIssueType) || 0,
                        priority: 1,
                        notes: (lineActionModal.supportNotes || '').trim(),
                        customerId: state.id,
                        subscriberProfileId: sub?.subscriberProfileId ?? sub?.SubscriberProfileId ?? null,
                    });
                    if (res?.data?.code !== 200) {
                        throw Object.assign(
                            new Error(res?.data?.message || telecomT('supportTicket.createFail', 'Create failed')),
                            { response: res }
                        );
                    }
                    const ticket = res?.data?.content?.data ?? res?.data?.content?.Data;
                    const ticketNo = ticket?.ticketNumber ?? ticket?.TicketNumber ?? '';
                    Swal.fire({
                        icon: 'success',
                        title: telecomT('supportTicket.createdOk', 'Ticket created'),
                        html: ticketNo ? `<p dir="ltr">${ticketNo}</p>` : undefined,
                        timer: 2200,
                        showConfirmButton: false,
                    });
                    hideBsModal('C360SupportTicketModal');
                    await loadCustomer360(state.id);
                } catch (e) {
                    Swal.fire({
                        icon: 'error',
                        title: e?.response?.data?.message || telecomT('supportTicket.createFail', 'Create failed'),
                    });
                } finally {
                    lineActionModal.supportBusy = false;
                }
            },
        };
    }
};

Vue.createApp(App).mount('#app');