const TEL_SUB_DEFAULT_PREPAID_ID = 'a0e0e0e0-0000-4000-8000-000000000001';

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
            createMainData: async (name, customerGroupId, customerCategoryId, description, street, city, stateVal, zipCode, country, phoneNumber, faxNumber, emailAddress, website, whatsApp, linkedIn, facebook, instagram, twitterX, tikTok, createdById, subscriberType, nationalId, dateOfBirth, commercialRegistration, taxNumber, authorizedSignatory) => {
                try {
                    const response = await AxiosManager.post('/Customer/CreateCustomer', {
                        name, customerGroupId, customerCategoryId, description, street, city, state: stateVal, zipCode, country, phoneNumber, faxNumber, emailAddress, website, whatsApp, linkedIn, facebook, instagram, twitterX, tikTok, createdById, subscriberType, nationalId, dateOfBirth, commercialRegistration, taxNumber, authorizedSignatory
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateMainData: async (id, name, customerGroupId, customerCategoryId, description, street, city, stateVal, zipCode, country, phoneNumber, faxNumber, emailAddress, website, whatsApp, linkedIn, facebook, instagram, twitterX, tikTok, updatedById, subscriberType, nationalId, dateOfBirth, commercialRegistration, taxNumber, authorizedSignatory) => {
                try {
                    const response = await AxiosManager.post('/Customer/UpdateCustomer', {
                        id, name, customerGroupId, customerCategoryId, description, street, city, state: stateVal, zipCode, country, phoneNumber, faxNumber, emailAddress, website, whatsApp, linkedIn, facebook, instagram, twitterX, tikTok, updatedById, subscriberType, nationalId, dateOfBirth, commercialRegistration, taxNumber, authorizedSignatory
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteMainData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/Customer/DeleteCustomer', {
                        id, deletedById
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
            createSecondaryData: async (name, jobTitle, phoneNumber, emailAddress, description, customerId, createdById) => {
                try {
                    const response = await AxiosManager.post('/CustomerContact/CreateCustomerContact', {
                        name, jobTitle, phoneNumber, emailAddress, description, customerId, createdById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            updateSecondaryData: async (id, name, jobTitle, phoneNumber, emailAddress, description, customerId, updatedById) => {
                try {
                    const response = await AxiosManager.post('/CustomerContact/UpdateCustomerContact', {
                        id, name, jobTitle, phoneNumber, emailAddress, description, customerId, updatedById
                    });
                    return response;
                } catch (error) {
                    throw error;
                }
            },
            deleteSecondaryData: async (id, deletedById) => {
                try {
                    const response = await AxiosManager.post('/CustomerContact/DeleteCustomerContact', {
                        id, deletedById
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
                          const badge = subscriberTypeBadgeFields(item);
                          return {
                              ...item,
                              subscriberType: item.subscriberType || 'Individual',
                              subscriptionTypeName: item.subscriptionTypeName || '—',
                              subscriptionTypeDisplayColor: item.subscriptionTypeDisplayColor || '#333',
                              telecomMsisdnsSummary: item.telecomMsisdnsSummary || '—',
                              primaryMsisdn: item.primaryMsisdn || '—',
                              createdAtUtc,
                              subscriberTypeBadgeClass: badge.subscriberTypeBadgeClass,
                              subscriberTypeLabel: badge.subscriberTypeLabel,
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
                                state.errors.nationalId = 'الرقم الوطني مطلوب للأفراد ويجب أن يتكون من 10 خانات.';
                                isValid = false;
                            }
                        } else if (state.subscriberType === 1) {
                            if (!state.commercialRegistration || state.commercialRegistration.trim().length < 4) {
                                state.errors.commercialRegistration = 'رقم السجل التجاري مطلوب للشركات (صيغة صالحة).';
                                isValid = false;
                            }
                        }
                    }

                    if (!isValid) return;

                    const commitWasUpdate = state.id !== '' && !state.deleteMode;

                    const nidCommit = nationalIdForSave();
                    const response = state.id === ''
                        ? await services.createMainData(state.name, state.customerGroupId, state.customerCategoryId, state.description, state.street, state.city, state.state, state.zipCode, state.country, state.phoneNumber, state.faxNumber, state.emailAddress, state.website, state.whatsApp, state.linkedIn, state.facebook, state.instagram, state.twitterX, state.tikTok, StorageManager.getUserId(), state.subscriberType, nidCommit, state.dateOfBirth ? new Date(state.dateOfBirth).toISOString() : null, state.commercialRegistration, state.taxNumber, state.authorizedSignatory)
                        : state.deleteMode
                            ? await services.deleteMainData(state.id, StorageManager.getUserId())
                            : await services.updateMainData(state.id, state.name, state.customerGroupId, state.customerCategoryId, state.description, state.street, state.city, state.state, state.zipCode, state.country, state.phoneNumber, state.faxNumber, state.emailAddress, state.website, state.whatsApp, state.linkedIn, state.facebook, state.instagram, state.twitterX, state.tikTok, StorageManager.getUserId(), state.subscriberType, nidCommit, state.dateOfBirth ? new Date(state.dateOfBirth).toISOString() : null, state.commercialRegistration, state.taxNumber, state.authorizedSignatory);

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
                                    updatedById: StorageManager.getUserId(),
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
                                            title: 'تم حفظ بيانات المشترك',
                                            text:
                                                telecomRes?.data?.message ||
                                                'تعذّر تحديث الخط الأساسي (MSISDN / نوع الخط).',
                                            confirmButtonText: 'حسناً',
                                        });
                                    } else {
                                        state.telecomSubscriptionIdInitial = state.telecomSubscriptionId;
                                        state.telecomSubscriptionTypeIdInitial = state.telecomSubscriptionTypeId;
                                    }
                                } catch (telecomErr) {
                                    await Swal.fire({
                                        icon: 'warning',
                                        title: 'تم حفظ بيانات المشترك',
                                        text:
                                            telecomErr.response?.data?.message ||
                                            telecomErr.message ||
                                            'تعذّر تحديث الخط الأساسي.',
                                        confirmButtonText: 'حسناً',
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
                                    createdById: StorageManager.getUserId(),
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
                                        title: 'تم حفظ بيانات المشترك',
                                        text:
                                            regRes?.data?.message ||
                                            'تعذّر تسجيل ملف المشترك أو الخط الجديد.',
                                        confirmButtonText: 'حسناً',
                                    });
                                } else {
                                    state.telecomMsisdnInitial = state.telecomMsisdn || '';
                                    state.telecomSubscriptionTypeIdInitial = state.telecomSubscriptionTypeId;
                                }
                            } catch (regErr) {
                                await Swal.fire({
                                    icon: 'warning',
                                    title: 'تم حفظ بيانات المشترك',
                                    text:
                                        regErr.response?.data?.message ||
                                        regErr.message ||
                                        'تعذّر تسجيل ملف المشترك أو الخط الجديد.',
                                    confirmButtonText: 'حسناً',
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
                                title: 'Delete Successful',
                                text: 'Form will be closed...',
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
                            title: state.deleteMode ? 'Delete Failed' : 'Save Failed',
                            text: response.data.message ?? 'Please check your data.',
                            confirmButtonText: 'Try Again'
                        });
                    }

                } catch (error) {
                    Swal.fire({
                        icon: 'error',
                        title: 'An Error Occurred',
                        text: error.response?.data?.message ?? 'Please try again.',
                        confirmButtonText: 'OK'
                    });
                } finally {
                    state.isSubmitting = false;
                }
            },
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
                        title: 'تعذّر التحميل',
                        text: 'لم يُعثر على بيانات العميل.',
                        confirmButtonText: 'حسناً',
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
                    title: 'خطأ',
                    text: e.response?.data?.message ?? e.message ?? 'فشل تحميل العميل.',
                    confirmButtonText: 'حسناً',
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
                        title: 'بحث',
                        text: 'أدخل رقم هوية (حرفين على الأقل) أو رقم جوال (حرفين على الأقل).',
                        confirmButtonText: 'حسناً',
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
                            title: 'بحث',
                            text: res?.data?.message || 'تعذّر تنفيذ البحث.',
                            confirmButtonText: 'حسناً',
                        });
                        return;
                    }
                    const list = res?.data?.content?.data;
                    state.customerSearchResults = Array.isArray(list) ? list : [];
                } catch (e) {
                    state.customerSearchResults = [];
                    await Swal.fire({
                        icon: 'error',
                        title: 'خطأ',
                        text: e.response?.data?.message ?? e.message ?? 'تعذّر تنفيذ البحث.',
                        confirmButtonText: 'حسناً',
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
            },
            proceedNewCustomer: () => {
                state.customerOnboardingStep = 1;
                state.addFlowBootstrapTelecom = true;
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
                let subHeader = 'نوع الخط';
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

        const gridLocaleIsEnglish = () => {
            const lang = (document.documentElement.lang || '').toLowerCase();
            return lang.startsWith('en');
        };

        /** Pre-compute badge fields for Syncfusion string template (avoids fragile function templates across ej2 builds). */
        const subscriberTypeBadgeFields = (item) => {
            const raw = item?.subscriberType;
            const s = String(raw ?? '').trim().toLowerCase();
            const isCorporate = s === 'corporate' || raw === 1 || raw === '1';
            const en = gridLocaleIsEnglish();
            if (isCorporate) {
                return {
                    subscriberTypeBadgeClass: 'badge bg-warning text-dark px-2 py-1 fs-6',
                    subscriberTypeLabel: en ? '🏢 Corporate' : '🏢 شركة',
                };
            }
            return {
                subscriberTypeBadgeClass: 'badge bg-primary text-white px-2 py-1 fs-6',
                subscriberTypeLabel: en ? '👤 Individual' : '👤 فرد',
            };
        };

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
                            headerText: gridLocaleIsEnglish() ? 'Type' : 'النوع',
                            width: 140,
                            minWidth: 100,
                            template: '#subscriberTypeBadgeTemplate',
                        },
                        ...(gridAccess.showFullColumns
                            ? [{ field: 'telecomSubscriptionLineCount', headerText: '#خطوط', width: 72, minWidth: 64 }]
                            : []),
                        {
                            field: 'primaryMsisdn',
                            headerText: gridAccess.showFullColumns ? 'أرقام الخطوط' : 'MSISDN',
                            width: gridAccess.showFullColumns ? 260 : 140,
                            minWidth: gridAccess.showFullColumns ? 160 : 120,
                            template: '#msisdnsSummaryTemplate',
                        },
                        {
                            field: 'subscriptionTypeName',
                            headerText: 'نوع الخط',
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
                                args.data.name, args.data.jobTitle, args.data.phoneNumber, args.data.emailAddress, args.data.description, state.id, StorageManager.getUserId()
                            );
                            await methods.populateSecondaryData(state.id);
                            secondaryGrid.refresh();
                            Swal.fire({
                                icon: 'success',
                                title: 'Save Successful',
                                timer: 2000,
                                showConfirmButton: false
                            });
                        }
                        if (args.requestType === 'save' && args.action === 'edit') {
                            const response = await services.updateSecondaryData(
                                args.data.id, args.data.name, args.data.jobTitle, args.data.phoneNumber, args.data.emailAddress, args.data.description, state.id, StorageManager.getUserId()
                            );
                            await methods.populateSecondaryData(state.id);
                            secondaryGrid.refresh();
                            Swal.fire({
                                icon: 'success',
                                title: 'Update Successful',
                                timer: 2000,
                                showConfirmButton: false
                            });
                        }
                        if (args.requestType === 'delete') {
                            const response = await services.deleteSecondaryData(
                                args.data[0].id, StorageManager.getUserId()
                            );
                            await methods.populateSecondaryData(state.id);
                            secondaryGrid.refresh();
                            Swal.fire({
                                icon: 'success',
                                title: 'Delete Successful',
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

        Vue.onMounted(async () => {
            document.documentElement.addEventListener('syriatel-locale-changed', refreshManageContactModalTitle);
            document.documentElement.addEventListener('syriatel-ui-modals-loaded', refreshManageContactModalTitle);
            refreshManageContactModalTitle();
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
                        state.mainTitle = typeof MacquiresUiI18n !== 'undefined' ? MacquiresUiI18n.mb('customer','add') : 'إضافة مشترك';
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
                vasPanelError[m] = e?.response?.data?.error?.message ?? e?.response?.data?.message ?? 'تعذّر تحميل VAS';
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
                const res = await AxiosManager.post('/Vas/ToggleSubscriberVasService', {
                    msisdn,
                    serviceCode: code,
                    action: wantActive ? 0 : 1,
                    actorUserId: StorageManager.getUserId(),
                });
                if (res?.data?.code === 200) {
                    await loadVasPanelForMsisdn(msisdn);
                    if (wantActive) {
                        Swal.fire({
                            icon: 'success',
                            title: 'تم التفعيل',
                            text: res?.data?.content?.operationNumber
                                ? 'عملية: ' + res.data.content.operationNumber
                                : '',
                            timer: 2000,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    if (event?.target) event.target.checked = !wantActive;
                    Swal.fire({ icon: 'error', title: 'فشل', text: res?.data?.message || '' });
                }
            } catch (e) {
                if (event?.target) event.target.checked = !wantActive;
                const errName = e?.response?.data?.error?.name;
                const msg = e?.response?.data?.error?.message ?? e?.response?.data?.message ?? e?.message ?? '';
                Swal.fire({
                    icon: errName === 'BusinessRuleViolationException' ? 'warning' : 'error',
                    title: errName === 'BusinessRuleViolationException' ? 'تعذّر التفعيل' : 'خطأ',
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
            simReplacementReason: '',
            simLostOrStolen: false,
            simIdentityFile: null,
            cnTargetMsisdnAssetId: '',
            cnNumberChangeReason: '',
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
            rcnReconnectReason: '',
            rcnClearanceType: 'Customer',
            rcnPaymentReference: '',
            rcnFraudClearanceConfirmed: false,
            rcnRequiresBackOffice: false,
            rcnEligibility: null,
            rcnEligibilityBusy: false,
            rfdRefundType: 'Deposit',
            rfdRefundMethod: 'CreditNote',
            rfdRefundAmount: '',
            rfdRefundReason: '',
            rfdRequiresBackOffice: false,
            rfdIdentityFile: null,
            bdrCollectionAction: 'PaymentRecorded',
            bdrDunningStage: 'Reminder1',
            bdrCollectedAmount: '',
            bdrWriteOffAmount: '',
            bdrPaymentReference: '',
            bdrRequiresBackOffice: false,
            bdrEligibility: null,
            bdrEligibilityBusy: false,
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
            || gridAccess.canSimSwapLine
            || gridAccess.canChangeNumberLine
            || gridAccess.canTerminateLine
            || gridAccess.canTakeOverLine
            || gridAccess.canSuspensionLine
            || gridAccess.canReconnectLine
            || gridAccess.canRefundLine
            || gridAccess.canCollectionLine
            || gridAccess.canDeviceSaleLine;

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
                    title: errName === 'BusinessRuleViolationException' ? 'تعذّر التنفيذ' : 'خطأ',
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
            lineActionModal.rcnReconnectReason = '';
            lineActionModal.rcnClearanceType = 'Customer';
            lineActionModal.rcnPaymentReference = '';
            lineActionModal.rcnFraudClearanceConfirmed = false;
            lineActionModal.rcnRequiresBackOffice = false;
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
            lineActionModal.bdrRequiresBackOffice = false;
            lineActionModal.bdrEligibility = null;
        };

        const onSusTypeChangeList = () => {
            const ty = (lineActionModal.susSuspensionType || '').trim();
            lineActionModal.susRequiresBackOffice = ty === 'Fraud' || ty === 'Regulatory';
        };

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
                ty === 'Fraud' || ty === 'Regulatory' || ty === 'Collections';
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
            const id = (lineActionModal.cnTargetMsisdnAssetId || '').trim();
            const row = (lineActionModal.cnPoolNumbers || []).find((r) => String(r.id ?? r.Id) === id);
            const cat = row?.category ?? row?.Category;
            lineActionModal.cnRequiresBackOffice = isPremiumMsisdnCategory(cat);
            if (!lineActionModal.cnRequiresBackOffice) {
                lineActionModal.cnPremiumFeeAmount = '';
            }
        };

        const primarySubscriberProfileId = () => {
            const subs = state.customer360?.activeSubscriptions || [];
            const p = subs.find((s) => s.isPrimaryLine) || subs[0];
            return (p?.subscriberProfileId || '').trim();
        };

        const runTelecomPipeline = async ({ processingKey, kindLabel, msisdn, buildBody }) => {
            const key = processingKey || '__line__';
            if (lineProcessing[key]) return false;
            lineProcessing[key] = true;
            const uid = StorageManager.getUserId();
            try {
                const notes = `Customer360|${kindLabel}|${msisdn || '—'}`;
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', {
                    ...buildBody(),
                    notes,
                    createdById: uid,
                });
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || 'فشل إنشاء العملية'), {
                        response: createRes,
                    });
                }
                const opId = createRes?.data?.content?.data?.id;
                if (!opId) throw new Error('لم يُرجع معرّف العملية');
                await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', { id: opId, updatedById: uid });
                const confirmRes = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                    id: opId,
                    updatedById: uid,
                });
                if (confirmRes?.data?.code !== 200) {
                    throw Object.assign(new Error(confirmRes?.data?.message || 'فشل التأكيد'), {
                        response: confirmRes,
                    });
                }
                if (window.Swal) {
                    Swal.fire({ icon: 'success', title: 'تم التنفيذ', timer: 1800, showConfirmButton: false });
                }
                await loadCustomer360(state.id);
                return true;
            } catch (e) {
                showLineActionError(e);
                return false;
            } finally {
                lineProcessing[key] = false;
            }
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

        const loadNewLineModalData = async () => {
            try {
                const [poolRes, offRes] = await Promise.all([
                    AxiosManager.get('/Telecom/GetMsisdnAssetPoolList?status=Available', {}),
                    AxiosManager.get('/ProductOffering/GetProductOfferingList', {}),
                ]);
                lineActionModal.poolNumbers = parseMsisdnPoolRows(poolRes).filter(isAvailableMsisdnPoolRow);
                const offContent = offRes?.data?.content ?? offRes?.data?.Content;
                const allOfferings = offContent?.data ?? offContent?.Data ?? [];
                lineActionModal.offerings = (Array.isArray(allOfferings) ? allOfferings : []).filter(
                    (o) => o.isActive !== false
                );
            } catch (e) {
                lineActionModal.poolNumbers = [];
                lineActionModal.offerings = [];
                console.warn('loadNewLineModalData failed', e);
            }
        };

        const openNewLineModal = async () => {
            if (!gridAccess.canActivateLine || !state.id) return;
            resetLineActionModal();
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

        const openSimSwapModal = (sub) => {
            if (!gridAccess.canSimSwapLine || !sub) return;
            resetLineActionModal();
            lineActionModal.sub = sub;
            showBsModal('C360SimSwapModal');
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

        const onRefundTypeChangeList = () => {
            lineActionModal.rfdRequiresBackOffice = lineActionModal.rfdRefundType === 'SyriatelCash';
        };

        const onRefundIdentityFileChange = (ev) => {
            lineActionModal.rfdIdentityFile = ev?.target?.files?.[0] || null;
        };

        const searchTakeoverTarget = async () => {
            const nat = (lineActionModal.takeoverSearchNationalId || '').trim();
            const ph = (lineActionModal.takeoverSearchPhone || '').trim();
            if (nat.length < 2 && ph.length < 2) {
                if (window.Swal) Swal.fire({ icon: 'info', title: 'أدخل رقم وطني أو جوال (حرفين على الأقل)' });
                return;
            }
            lineActionModal.busy = true;
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
                lineActionModal.busy = false;
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
                    Swal.fire({ icon: 'warning', title: 'لا يوجد ملف مشترك للعميل المختار' });
                }
            } catch (e) {
                showLineActionError(e);
            } finally {
                lineActionModal.busy = false;
            }
        };

        const submitNewLineActivation = async () => {
            const profileId = primarySubscriberProfileId();
            const assetId = (lineActionModal.msisdnAssetId || '').trim();
            const offeringId = (lineActionModal.productOfferingId || '').trim();
            const iccid = (lineActionModal.simIccid || '').trim();
            if (!profileId || !assetId || !offeringId || iccid.length < 19) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'أكمل الرقم والباقة وICCID (19 رقم)' });
                return;
            }
            lineActionModal.busy = true;
            try {
                await AxiosManager.post('/Telecom/ReserveMsisdnForCustomer', {
                    msisdnAssetId: assetId,
                    customerId: state.id,
                    reservedByUserId: StorageManager.getUserId(),
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
                    simIccid: iccid,
                }),
            });
            lineActionModal.busy = false;
            if (ok) hideBsModal('C360NewLineModal');
        };

        const submitMigrate = async () => {
            const sub = lineActionModal.sub;
            const offeringId = (lineActionModal.productOfferingId || '').trim();
            if (!sub || !offeringId) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'اختر الباقة الجديدة' });
                return;
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
                }),
            });
            lineActionModal.busy = false;
            if (ok) hideBsModal('C360MigrateModal');
        };

        const submitSimSwap = async () => {
            const sub = lineActionModal.sub;
            const iccid = (lineActionModal.simIccid || '').trim();
            const reason = (lineActionModal.simReplacementReason || '').trim();
            if (!sub || !reason) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'اختر سبب التبديل' });
                return;
            }
            if (iccid.length < 19) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'أدخل ICCID الجديد (19 رقم)' });
                return;
            }
            if (lineActionModal.simLostOrStolen && !lineActionModal.simIdentityFile) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'ارفع إقرار / هوية المشترك' });
                return;
            }
            if (lineActionModal.simLostOrStolen) {
                const key = sub.id || '__line__';
                if (lineProcessing[key]) return;
                lineProcessing[key] = true;
                lineActionModal.busy = true;
                const uid = StorageManager.getUserId();
                try {
                    const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', {
                        kind: 3,
                        subscriberProfileId: sub.subscriberProfileId,
                        msisdnAssetId: sub.msisdnAssetId || null,
                        simIccid: iccid,
                        replacementReason: reason,
                        isLostOrStolenReport: true,
                        notes: `Customer360|SimSwap|${sub.msisdn || '—'}`,
                        createdById: uid,
                    });
                    if (createRes?.data?.code !== 200) {
                        throw Object.assign(new Error(createRes?.data?.message || 'فشل إنشاء الطلب'), {
                            response: createRes,
                        });
                    }
                    const opId = createRes?.data?.content?.data?.id;
                    if (!opId) throw new Error('لم يُرجع معرّف العملية');
                    const form = new FormData();
                    form.append('id', opId);
                    form.append('updatedById', uid || '');
                    form.append('file', lineActionModal.simIdentityFile);
                    await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: 'تم الإرسال للباك أوفيس',
                            text: 'الطلب بانتظار اعتماد المشرف (SIM-).',
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
                }),
            });
            lineActionModal.busy = false;
            if (ok) hideBsModal('C360SimSwapModal');
        };

        const submitTermination = async () => {
            const sub = lineActionModal.sub;
            const reason = (lineActionModal.trmTerminationReason || '').trim();
            const type = (lineActionModal.trmTerminationType || '').trim();
            if (!sub || !reason || !type) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'أكمل نوع وسبب الإنهاء' });
                return;
            }
            if (type === 'Voluntary' && !(lineActionModal.trmRetentionOfferOutcome || '').trim()) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'نتيجة عرض الاحتفاظ مطلوبة' });
                return;
            }
            if (lineActionModal.trmRequiresBackOffice && !lineActionModal.trmIdentityFile) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'ارفع الوثيقة / القرار الإداري' });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            const uid = StorageManager.getUserId();
            try {
                const body = {
                    kind: 7,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    terminationType: type,
                    terminationReason: reason,
                    retentionOfferOutcome:
                        type === 'Voluntary' ? (lineActionModal.trmRetentionOfferOutcome || '').trim() : null,
                    notes: `CustomerList|Termination|${sub.msisdn || '—'}`,
                    createdById: uid,
                };
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || 'فشل إنشاء الطلب'), {
                        response: createRes,
                    });
                }
                const opId = createRes?.data?.content?.data?.id;
                const entity = createRes?.data?.content?.data;
                if (!opId) throw new Error('لم يُرجع معرّف العملية');
                lineActionModal.trmRequiresBackOffice =
                    String(entity?.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                    || lineActionModal.trmRequiresBackOffice;
                if (lineActionModal.trmRequiresBackOffice) {
                    const form = new FormData();
                    form.append('id', opId);
                    form.append('updatedById', uid || '');
                    form.append('file', lineActionModal.trmIdentityFile);
                    await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: 'تم الإرسال للباك أوفيس',
                            text: 'الطلب بانتظار اعتماد الإنهاء (TRM-).',
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', { id: opId, updatedById: uid });
                    const confirmRes = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                        id: opId,
                        updatedById: uid,
                    });
                    if (confirmRes?.data?.code !== 200) {
                        throw Object.assign(new Error(confirmRes?.data?.message || 'فشل التأكيد'), {
                            response: confirmRes,
                        });
                    }
                    if (window.Swal) {
                        Swal.fire({ icon: 'success', title: 'تم إنهاء الخط', timer: 1800, showConfirmButton: false });
                    }
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
            const reason = (lineActionModal.susSuspensionReason || '').trim();
            if (!sub || !reason) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'سبب الحظر مطلوب' });
                return;
            }
            if (lineActionModal.susAutoReconnectEnabled && !(lineActionModal.susEndDateLocal || '').trim()) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'تاريخ انتهاء الحظر مطلوب' });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            const uid = StorageManager.getUserId();
            try {
                const body = {
                    kind: 8,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    suspensionType: (lineActionModal.susSuspensionType || '').trim(),
                    suspensionReason: reason,
                    barringLevel: (lineActionModal.susBarringLevel || 'Full').trim(),
                    autoReconnectEnabled: !!lineActionModal.susAutoReconnectEnabled,
                    suspensionEndDateUtc:
                        lineActionModal.susAutoReconnectEnabled && lineActionModal.susEndDateLocal
                            ? new Date(lineActionModal.susEndDateLocal).toISOString()
                            : null,
                    notes: `CustomerList|Suspension|${sub.msisdn || '—'}`,
                    createdById: uid,
                };
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || 'فشل'), { response: createRes });
                }
                const opId = createRes?.data?.content?.data?.id;
                const entity = createRes?.data?.content?.data;
                if (!opId) throw new Error('لم يُرجع معرّف العملية');
                lineActionModal.susRequiresBackOffice =
                    String(entity?.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                    || lineActionModal.susRequiresBackOffice;
                if (lineActionModal.susRequiresBackOffice) {
                    await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', { id: opId, updatedById: uid });
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: 'تم الإرسال للباك أوفيس',
                            text: 'الطلب بانتظار اعتماد الحظر (SUS-).',
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', { id: opId, updatedById: uid });
                    const confirmRes = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                        id: opId,
                        updatedById: uid,
                    });
                    if (confirmRes?.data?.code !== 200) {
                        throw Object.assign(new Error(confirmRes?.data?.message || 'فشل التأكيد'), {
                            response: confirmRes,
                        });
                    }
                    if (window.Swal) {
                        Swal.fire({ icon: 'success', title: 'تم حظر الخط', timer: 1800, showConfirmButton: false });
                    }
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
            const reason = (lineActionModal.rcnReconnectReason || '').trim();
            if (!sub || !reason) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'سبب إعادة التفعيل مطلوب' });
                return;
            }
            if (lineActionModal.rcnClearanceType === 'Payment' && !(lineActionModal.rcnPaymentReference || '').trim()) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'مرجع الدفع مطلوب' });
                return;
            }
            await loadListReconnectEligibility();
            if (
                lineActionModal.rcnEligibility
                && !lineActionModal.rcnEligibility.allowed
                && !lineActionModal.rcnEligibility.Allowed
            ) {
                const msg =
                    lineActionModal.rcnEligibility.messageAr || lineActionModal.rcnEligibility.MessageAr || '';
                if (window.Swal) Swal.fire({ icon: 'error', title: 'غير مسموح', text: msg });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            const uid = StorageManager.getUserId();
            try {
                const body = {
                    kind: 9,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    reconnectReason: reason,
                    clearanceType: (lineActionModal.rcnClearanceType || '').trim(),
                    fraudClearanceConfirmed: !!lineActionModal.rcnFraudClearanceConfirmed,
                    paymentReference: (lineActionModal.rcnPaymentReference || '').trim() || null,
                    notes: `CustomerList|Reconnect|${sub.msisdn || '—'}`,
                    createdById: uid,
                };
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || 'فشل'), { response: createRes });
                }
                const opId = createRes?.data?.content?.data?.id;
                const entity = createRes?.data?.content?.data;
                if (!opId) throw new Error('لم يُرجع معرّف العملية');
                lineActionModal.rcnRequiresBackOffice =
                    String(entity?.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                    || lineActionModal.rcnRequiresBackOffice;
                if (lineActionModal.rcnRequiresBackOffice) {
                    await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', { id: opId, updatedById: uid });
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: 'تم الإرسال للباك أوفيس',
                            text: 'الطلب بانتظار اعتماد RCN.',
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', { id: opId, updatedById: uid });
                    const confirmRes = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                        id: opId,
                        updatedById: uid,
                    });
                    if (confirmRes?.data?.code !== 200) {
                        throw Object.assign(new Error(confirmRes?.data?.message || 'فشل التأكيد'), {
                            response: confirmRes,
                        });
                    }
                    if (window.Swal) {
                        Swal.fire({ icon: 'success', title: 'تم إعادة التفعيل', timer: 1800, showConfirmButton: false });
                    }
                }
                await loadCustomer360(state.id);
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
            const reason = (lineActionModal.rfdRefundReason || '').trim();
            const amt = Number(lineActionModal.rfdRefundAmount);
            if (!sub || !reason || !amt || amt <= 0) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'أكمل بيانات الاسترداد' });
                return;
            }
            if (lineActionModal.rfdRequiresBackOffice && !lineActionModal.rfdIdentityFile) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'ارفع وثيقة الاعتماد' });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            const uid = StorageManager.getUserId();
            try {
                const body = {
                    kind: 11,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    refundType: (lineActionModal.rfdRefundType || '').trim(),
                    refundMethod: (lineActionModal.rfdRefundMethod || '').trim(),
                    refundReason: reason,
                    refundAmount: amt,
                    notes: `CustomerList|Refund|${sub.msisdn || '—'}`,
                    createdById: uid,
                };
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || 'فشل'), { response: createRes });
                }
                const opId = createRes?.data?.content?.data?.id;
                const entity = createRes?.data?.content?.data;
                if (!opId) throw new Error('لم يُرجع معرّف العملية');
                lineActionModal.rfdRequiresBackOffice =
                    String(entity?.approvalLevelRequired || '').toLowerCase() === 'backoffice'
                    || !!entity?.requiresDualApproval
                    || lineActionModal.rfdRequiresBackOffice;
                if (lineActionModal.rfdRequiresBackOffice) {
                    const form = new FormData();
                    form.append('id', opId);
                    form.append('updatedById', uid || '');
                    form.append('file', lineActionModal.rfdIdentityFile);
                    await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: 'تم الإرسال للباك أوفيس',
                            text: 'الطلب بانتظار اعتماد الاسترداد (RFD-).',
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', { id: opId, updatedById: uid });
                    const confirmRes = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                        id: opId,
                        updatedById: uid,
                    });
                    if (confirmRes?.data?.code !== 200) {
                        throw Object.assign(new Error(confirmRes?.data?.message || 'فشل التأكيد'), {
                            response: confirmRes,
                        });
                    }
                    if (window.Swal) {
                        Swal.fire({ icon: 'success', title: 'تم تنفيذ الاسترداد', timer: 1800, showConfirmButton: false });
                    }
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
                    if (window.Swal) Swal.fire({ icon: 'warning', title: 'مرجع الدفع مطلوب' });
                    return;
                }
                if (!(Number(lineActionModal.bdrCollectedAmount) > 0)) {
                    if (window.Swal) Swal.fire({ icon: 'warning', title: 'المبلغ المحصّل مطلوب' });
                    return;
                }
            }
            await loadListBadDebtEligibility();
            if (
                lineActionModal.bdrEligibility
                && !lineActionModal.bdrEligibility.allowed
                && !lineActionModal.bdrEligibility.Allowed
            ) {
                const msg =
                    lineActionModal.bdrEligibility.messageAr || lineActionModal.bdrEligibility.MessageAr || '';
                if (window.Swal) Swal.fire({ icon: 'error', title: 'غير مسموح', text: msg });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            const uid = StorageManager.getUserId();
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
                    notes: `CustomerList|BadDebt|${sub.msisdn || '—'}`,
                    createdById: uid,
                };
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || 'فشل'), { response: createRes });
                }
                const opId = createRes?.data?.content?.data?.id;
                if (!opId) throw new Error('لم يُرجع معرّف العملية');
                lineActionModal.bdrRequiresBackOffice =
                    String(createRes?.data?.content?.data?.approvalLevelRequired || '').toLowerCase() ===
                        'backoffice'
                    || lineActionModal.bdrRequiresBackOffice;
                if (lineActionModal.bdrRequiresBackOffice) {
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: 'تم الإرسال للباك أوفيس',
                            text: 'الطلب بانتظار اعتماد التحصيل (BDR-).',
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', { id: opId, updatedById: uid });
                    const confirmRes = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                        id: opId,
                        updatedById: uid,
                    });
                    if (confirmRes?.data?.code !== 200) {
                        throw Object.assign(new Error(confirmRes?.data?.message || 'فشل التأكيد'), {
                            response: confirmRes,
                        });
                    }
                    if (window.Swal) {
                        Swal.fire({ icon: 'success', title: 'تم تسجيل التحصيل', timer: 1800, showConfirmButton: false });
                    }
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
            const targetId = (lineActionModal.cnTargetMsisdnAssetId || '').trim();
            const reason = (lineActionModal.cnNumberChangeReason || '').trim();
            if (!sub || !targetId || !reason) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'اختر الرقم الجديد وسبب التغيير' });
                return;
            }
            if (lineActionModal.cnRequiresBackOffice && !lineActionModal.cnPaymentFile) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'ارفع إيصال الدفع أو موافقة المشرف' });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            const uid = StorageManager.getUserId();
            try {
                await AxiosManager.post('/Telecom/ReserveMsisdnForCustomer', {
                    msisdnAssetId: targetId,
                    customerId: state.id,
                    reservedByUserId: uid,
                });
                const body = {
                    kind: 5,
                    subscriberProfileId: sub.subscriberProfileId,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    targetMsisdnAssetId: targetId,
                    numberChangeReason: reason,
                    notes: `Customer360|ChangeNumber|${sub.msisdn || '—'}`,
                    createdById: uid,
                };
                if (lineActionModal.cnPremiumFeeAmount) {
                    body.premiumFeeAmount = Number(lineActionModal.cnPremiumFeeAmount);
                }
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', body);
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || 'فشل إنشاء الطلب'), {
                        response: createRes,
                    });
                }
                const opId = createRes?.data?.content?.data?.id;
                if (!opId) throw new Error('لم يُرجع معرّف العملية');
                if (lineActionModal.cnRequiresBackOffice) {
                    const form = new FormData();
                    form.append('id', opId);
                    form.append('updatedById', uid || '');
                    form.append('file', lineActionModal.cnPaymentFile);
                    await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                        headers: { 'Content-Type': 'multipart/form-data' },
                    });
                    if (window.Swal) {
                        Swal.fire({
                            icon: 'success',
                            title: 'تم الإرسال للباك أوفيس',
                            text: 'الطلب بانتظار اعتماد تغيير الرقم (CNR-).',
                            timer: 2800,
                            showConfirmButton: false,
                        });
                    }
                } else {
                    await AxiosManager.post('/Telecom/UploadTelecomOperationDocument', { id: opId, updatedById: uid });
                    const confirmRes = await AxiosManager.post('/Telecom/ConfirmTelecomOperation', {
                        id: opId,
                        updatedById: uid,
                    });
                    if (confirmRes?.data?.code !== 200) {
                        throw Object.assign(new Error(confirmRes?.data?.message || 'فشل التأكيد'), {
                            response: confirmRes,
                        });
                    }
                    if (window.Swal) {
                        Swal.fire({ icon: 'success', title: 'تم تغيير الرقم', timer: 1800, showConfirmButton: false });
                    }
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
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'اختر المالك الجديد' });
                return;
            }
            if (!(lineActionModal.takeoverTransferReason || '').trim()) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'سبب نقل الملكية مطلوب' });
                return;
            }
            if (!lineActionModal.takeoverIdentityFile) {
                if (window.Swal) Swal.fire({ icon: 'warning', title: 'ارفع هوية المالك الجديد' });
                return;
            }
            const key = sub.id || '__line__';
            if (lineProcessing[key]) return;
            lineProcessing[key] = true;
            lineActionModal.busy = true;
            const uid = StorageManager.getUserId();
            try {
                const createRes = await AxiosManager.post('/Telecom/CreateTelecomOperation', {
                    kind: 2,
                    subscriberProfileId: sub.subscriberProfileId,
                    secondarySubscriberProfileId: secondary,
                    msisdnAssetId: sub.msisdnAssetId || null,
                    transferReason: (lineActionModal.takeoverTransferReason || '').trim(),
                    depositTransferPolicy: Number(lineActionModal.takeoverDepositPolicy) || 1,
                    notes: `Customer360|TakeOver|${sub.msisdn || '—'}`,
                    createdById: uid,
                });
                if (createRes?.data?.code !== 200) {
                    throw Object.assign(new Error(createRes?.data?.message || 'فشل إنشاء الطلب'), { response: createRes });
                }
                const opId = createRes?.data?.content?.data?.id;
                if (!opId) throw new Error('لم يُرجع معرّف العملية');
                const form = new FormData();
                form.append('id', opId);
                form.append('updatedById', uid || '');
                form.append('file', lineActionModal.takeoverIdentityFile);
                await AxiosManager.post('/Telecom/UploadTelecomOperationIdentityDocument', form, {
                    headers: { 'Content-Type': 'multipart/form-data' },
                });
                if (window.Swal) {
                    Swal.fire({
                        icon: 'success',
                        title: 'تم الإرسال للباك أوفيس',
                        text: 'الطلب قيد التدقيق القانوني. بعد الاعتماد يُنفَّذ النقل على CBS وHLR تلقائياً.',
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
            const s = Number(status);
            const failed = s === 4;
            return [
                { label: 'مسودة', done: s !== 4, active: s === 0, failed },
                { label: 'وثائق', done: s >= 5 || s === 6 || s === 3 || s === 1, active: s === 5, failed },
                { label: 'تجهيز', done: s === 3 || s === 1, active: s === 6, failed },
                { label: 'منجز', done: s === 3, active: false, failed },
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
                state.customer360Error = e?.response?.data?.message || e?.message || 'تعذّر تحميل Customer 360';
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
            const map = { 0: 'دقائق', 1: 'إنترنت', 2: 'رسائل', Voice: 'دقائق', Data: 'إنترنت', Sms: 'رسائل' };
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
            for (let i = 0; i < maxAttempts; i++) {
                const res = await AxiosManager.get(
                    '/Telecom/GetPaymentTransactionDetail?id=' + encodeURIComponent(paymentId),
                    {}
                );
                const detail = res?.data?.content ?? res?.data?.Content;
                const status = detail?.status ?? detail?.Status;
                if (status === 2 || status === 'Completed') return detail;
                if (status === 3 || status === 'Failed') {
                    throw new Error(detail?.failureReason || detail?.FailureReason || 'فشلت معاملة الدفع');
                }
                await new Promise((r) => setTimeout(r, 500));
            }
            return null;
        };

        const executeListPaymentFlow = async (customerId, subscriptionId, busyKey) => {
            const { value: method } = await Swal.fire({
                title: 'طريقة الشحن',
                input: 'radio',
                inputOptions: { wallet: 'محفظة / نقد', voucher: 'قسيمة' },
                inputValue: 'wallet',
                showCancelButton: true,
                confirmButtonText: 'متابعة',
            });
            if (!method) return;

            let createBody;
            let gatewayRef;
            if (method === 'voucher') {
                const { value: voucherCode } = await Swal.fire({
                    title: 'رمز القسيمة',
                    input: 'text',
                    showCancelButton: true,
                    inputValidator: (v) => (!v || !String(v).trim() ? 'أدخل الرمز' : undefined),
                });
                if (!voucherCode) return;
                const valRes = await AxiosManager.post('/Telecom/ValidateVoucher', {
                    voucherCode: String(voucherCode).trim(),
                });
                const val = valRes?.data?.content ?? valRes?.data?.Content;
                if (!(val?.valid ?? val?.Valid)) {
                    Swal.fire({ icon: 'error', title: val?.messageAr || val?.MessageAr || 'قسيمة غير صالحة' });
                    return;
                }
                createBody = {
                    type: 1,
                    customerId,
                    subscriptionId,
                    amount: val?.faceValue ?? val?.FaceValue ?? 0,
                    paymentChannel: 2,
                    voucherCode: String(voucherCode).trim(),
                    createdById: StorageManager.getUserId(),
                };
                gatewayRef = `VCHR-${String(voucherCode).trim()}`;
            } else {
                const { value: amountStr } = await Swal.fire({
                    title: 'مبلغ الشحن',
                    input: 'number',
                    showCancelButton: true,
                    confirmButtonText: 'التالي',
                    inputValidator: (v) => {
                        const n = parseFloat(v);
                        if (!v || Number.isNaN(n) || n <= 0) return 'مبلغ غير صالح';
                    },
                });
                if (!amountStr) return;
                const { value: gw } = await Swal.fire({
                    title: 'مرجع الدفع',
                    input: 'text',
                    showCancelButton: true,
                    confirmButtonText: 'تأكيد',
                    inputValidator: (v) => (!v || !String(v).trim() ? 'مرجع مطلوب' : undefined),
                });
                if (!gw) return;
                createBody = {
                    type: 0,
                    customerId,
                    subscriptionId,
                    amount: parseFloat(amountStr),
                    paymentChannel: 1,
                    createdById: StorageManager.getUserId(),
                };
                gatewayRef = String(gw).trim();
            }

            state.rechargeBusy = busyKey;
            try {
                const createRes = await AxiosManager.post('/Telecom/CreatePaymentTransaction', createBody);
                const draft = createRes?.data?.content ?? createRes?.data?.Content;
                const paymentId = draft?.paymentId ?? draft?.PaymentId;
                const confirmRes = await AxiosManager.post('/Telecom/ConfirmPaymentTransaction', {
                    paymentId,
                    gatewayReference: gatewayRef,
                    confirmedById: StorageManager.getUserId(),
                });
                const confirm = confirmRes?.data?.content ?? confirmRes?.data?.Content;
                if (!(confirm?.success ?? confirm?.Success)) {
                    throw new Error(confirm?.messageAr || confirm?.MessageAr || 'فشل التأكيد');
                }
                await pollPaymentDetailList(paymentId);
                await loadCustomer360(state.id);
                Swal.fire({ icon: 'success', title: 'تم الشحن', text: confirm?.messageAr || confirm?.MessageAr });
            } catch (e) {
                Swal.fire({ icon: 'error', title: e?.message || 'تعذّر الشحن' });
            } finally {
                state.rechargeBusy = '';
            }
        };

        const openRechargeLineModal = async (sub) => {
            if (!state.id || !sub?.id) return;
            const wallet = lineWalletForSub(sub.id);
            const msisdn = sub.msisdn || wallet?.msisdn || '';
            if (!msisdn) {
                Swal.fire({ icon: 'warning', title: 'لا يوجد رقم خط للشحن' });
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
                    Swal.fire({ icon: 'info', title: 'غير متاح', text: 'لا يوجد رقم وطني لهذا السجل.' });
                }
            } catch (e) {
                if (window.Swal) {
                    Swal.fire({ icon: 'error', title: 'صلاحية', text: e?.response?.data?.message || 'ليس لديك صلاحية ViewDecryptedPII' });
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
                    actorUserId: StorageManager.getUserId(),
                });
                const body = res?.data?.content;
                if (res?.data?.code === 200 && body?.success) {
                    await queryCustomerHlrLiveStatus();
                    alert(body.message || 'تمت المزامنة');
                } else {
                    alert(body?.message || res?.data?.message || 'فشل المزامنة');
                }
            } catch (e) {
                alert(e?.response?.data?.message || e?.message || 'HLR');
            } finally {
                state.hlrResyncBusy = false;
            }
        };

        return {
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
            uniqueSubscriberProfileIds,
            queryCustomerHlrLiveStatus,
            resyncCustomerFromHlr,
            loadCustomer360,
            revealNationalId,
            pipelineStepsForOp,
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
            isLineProcessing,
            hasAnyLineAction,
            openNewLineModal,
            openMigrateModal,
            openSimSwapModal,
            openChangeNumberModal,
            openTerminationModal,
            openSuspensionModal,
            openReconnectModal,
            openRefundModal,
            openBadDebtModal,
            openTakeOverModal,
            submitSuspension,
            submitReconnect,
            submitRefund,
            submitBadDebt,
            onSusTypeChangeList,
            loadListReconnectEligibility,
            loadListBadDebtEligibility,
            onRefundTypeChangeList,
            onRefundIdentityFileChange,
            submitNewLineActivation,
            submitMigrate,
            submitSimSwap,
            submitChangeNumber,
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
            openSupportTicketModal: async () => {
                const subs = state.customer360?.activeSubscriptions || [];
                const primary = subs.find((s) => s.isPrimaryLine) || subs[0];
                const msisdn = primary?.msisdn || '';
                if (!msisdn) {
                    Swal.fire({ icon: 'warning', title: 'لا يوجد رقم MSISDN للمشترك' });
                    return;
                }
                const { value: form } = await Swal.fire({
                    title: 'فتح تذكرة دعم فني',
                    html:
                        `<p class="small text-muted" dir="ltr">${msisdn}</p>` +
                        '<select id="swal-issue" class="form-select mb-2"><option value="0">شبكة</option><option value="1">فوترة</option><option value="2">حظر شريحة</option><option value="3">تفعيل</option></select>' +
                        '<textarea id="swal-notes" class="form-control" rows="3" placeholder="وصف المشكلة"></textarea>',
                    focusConfirm: false,
                    showCancelButton: true,
                    confirmButtonText: 'إنشاء',
                    preConfirm: () => ({
                        issueType: Number(document.getElementById('swal-issue')?.value ?? 0),
                        notes: document.getElementById('swal-notes')?.value?.trim() || '',
                    }),
                });
                if (!form) return;
                try {
                    await AxiosManager.post('/TelecomBackOffice/CreateTechnicalTicket', {
                        msisdn,
                        issueType: form.issueType,
                        priority: 1,
                        notes: form.notes,
                        customerId: state.id,
                        subscriberProfileId: primary?.subscriberProfileId,
                    });
                    Swal.fire({ icon: 'success', title: 'تم إنشاء التذكرة', timer: 2000, showConfirmButton: false });
                } catch (e) {
                    Swal.fire({ icon: 'error', title: e?.response?.data?.message || 'فشل إنشاء التذكرة' });
                }
            },
        };
    }
};

Vue.createApp(App).mount('#app');