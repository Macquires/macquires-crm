const LANG_KEY = 'syriatelUiLang';
const CULTURE_COOKIE = '.AspNetCore.Culture';

const STRINGS = {
    en: {
        langGroup: 'Interface language',
        langAr: 'العربية',
        langEn: 'English',
        brandLine: 'Syriatel CRM',
        signInH1: 'Sign in',
        emailLabel: 'Email',
        emailPlaceholder: 'name@syriatel.sy',
        passwordLabel: 'Password',
        passwordPlaceholder: '••••••••',
        forgotLink: 'Forgot password?',
        submitSigning: 'Signing in…',
        submit: 'Sign in',
        requestAccess: 'Request access',
        home: 'Home',
        linkSep: '·',
        errEmailRequired: 'Email is required.',
        errEmailInvalid: 'Please enter a valid email address.',
        errPasswordRequired: 'Password is required.',
        errPasswordLen: 'Password must be at least 6 characters.',
        signedInTitle: 'Signed in',
        signedInText: 'Redirecting to your workspace…',
        signInFailedTitle: 'Sign-in failed',
        signInFailedText: 'Check your email and password.',
        tryAgain: 'Try again',
        errorTitle: 'An error occurred',
        errorText: 'Please try again.',
        ok: 'OK',
    },
    ar: {
        langGroup: 'لغة الواجهة',
        langAr: 'العربية',
        langEn: 'English',
        brandLine: 'سيريتل — CRM',
        signInH1: 'تسجيل الدخول',
        emailLabel: 'البريد الإلكتروني',
        emailPlaceholder: 'name@syriatel.sy',
        passwordLabel: 'كلمة المرور',
        passwordPlaceholder: '••••••••',
        forgotLink: 'نسيت كلمة المرور؟',
        submitSigning: 'جاري تسجيل الدخول…',
        submit: 'تسجيل الدخول',
        requestAccess: 'طلب صلاحية',
        home: 'الرئيسية',
        linkSep: '·',
        errEmailRequired: 'البريد الإلكتروني مطلوب.',
        errEmailInvalid: 'يرجى إدخال بريد إلكتروني صالح.',
        errPasswordRequired: 'كلمة المرور مطلوبة.',
        errPasswordLen: 'يجب أن تكون كلمة المرور 6 أحرف على الأقل.',
        signedInTitle: 'تم تسجيل الدخول',
        signedInText: 'جاري التوجيه إلى مساحة العمل…',
        signInFailedTitle: 'فشل تسجيل الدخول',
        signInFailedText: 'تحقق من البريد وكلمة المرور.',
        tryAgain: 'حاول مجدداً',
        errorTitle: 'حدث خطأ',
        errorText: 'يرجى المحاولة مرة أخرى.',
        ok: 'حسناً',
    },
};

function readCultureFromCookie() {
    const prefix = CULTURE_COOKIE + '=';
    const segment = document.cookie.split('; ').find(function (x) {
        return x.startsWith(prefix);
    });
    if (!segment) return null;
    let raw = segment.slice(prefix.length);
    try {
        raw = decodeURIComponent(raw);
    } catch (_) {
        /* ignore */
    }
    const parts = raw.split('|');
    for (let i = 0; i < parts.length; i++) {
        const p = parts[i];
        if (p.indexOf('c=') === 0) {
            const c = p.slice(2).toLowerCase();
            if (c.indexOf('en') === 0) return 'en';
            return 'ar';
        }
    }
    return null;
}

function resolveLangFromHints() {
    const fromCookie = readCultureFromCookie();
    if (fromCookie) return fromCookie;
    try {
        const saved = localStorage.getItem(LANG_KEY);
        if (saved === 'en' || saved === 'ar') return saved;
    } catch (_) {
        /* ignore */
    }
    return 'en';
}

async function persistCulture(lang) {
    const url = '/culture/set?lang=' + encodeURIComponent(lang);
    const res = await fetch(url, { credentials: 'same-origin', method: 'GET' });
    if (!res.ok) throw new Error('culture');
}

function applyDocumentLocale(lang, options) {
    options = options || {};
    const isAr = lang === 'ar';
    try {
        localStorage.setItem(LANG_KEY, isAr ? 'ar' : 'en');
    } catch (_) {
        /* ignore */
    }
    document.documentElement.lang = isAr ? 'ar' : 'en';
    document.documentElement.dir = isAr ? 'rtl' : 'ltr';
    if (document.body) {
        document.body.lang = isAr ? 'ar' : 'en';
        document.body.dir = isAr ? 'rtl' : 'ltr';
    }
    if (!options.suppressLocaleEvent) {
        document.documentElement.dispatchEvent(
            new CustomEvent('syriatel-locale-changed', { detail: { lang: isAr ? 'ar' : 'en' } })
        );
    }
}

/** Sync document root with cookie before Vue mounts so first paint matches Arabic RTL. */
function initialUiLang() {
    return resolveLangFromHints() === 'en' ? 'en' : 'ar';
}

applyDocumentLocale(initialUiLang(), { suppressLocaleEvent: true });

function t(uiLang, key) {
    const pack = STRINGS[uiLang] || STRINGS.en;
    return pack[key] ?? STRINGS.en[key] ?? key;
}

const App = {
    setup() {
        const uiLang = Vue.ref(initialUiLang());

        const state = Vue.reactive({
            email: '',
            password: '',
            isSubmitting: false,
            errors: {
                email: '',
                password: '',
            },
        });

        const validateForm = () => {
            state.errors.email = '';
            state.errors.password = '';
            let isValid = true;
            const L = (k) => t(uiLang.value, k);

            if (!state.email) {
                state.errors.email = L('errEmailRequired');
                isValid = false;
            } else if (!/\S+@\S+\.\S+/.test(state.email)) {
                state.errors.email = L('errEmailInvalid');
                isValid = false;
            }

            if (!state.password) {
                state.errors.password = L('errPasswordRequired');
                isValid = false;
            } else if (state.password.length < 6) {
                state.errors.password = L('errPasswordLen');
                isValid = false;
            }

            return isValid;
        };

        const swalOpts = () => ({ rtl: uiLang.value === 'ar' });

        const handleSubmit = async () => {
            const L = (k) => t(uiLang.value, k);
            try {
                state.isSubmitting = true;
                await new Promise((resolve) => setTimeout(resolve, 300));

                if (!validateForm()) {
                    return;
                }

                const response = await AxiosManager.post('/Security/Login', {
                    email: state.email,
                    password: state.password,
                });

                if (StorageManager.isApiSuccess(response)) {
                    try {
                        sessionStorage.removeItem('syrSessionSynced');
                    } catch {
                        /* ignore */
                    }
                    StorageManager.saveLoginResult(response.data);
                    const landingUrl =
                        (typeof StorageManager.getLandingPath === 'function'
                            ? StorageManager.getLandingPath()
                            : null) || '/Profiles/MyProfile';

                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            ...swalOpts(),
                            icon: 'success',
                            title: L('signedInTitle'),
                            text: L('signedInText'),
                            timer: 1200,
                            showConfirmButton: false,
                        });
                    }

                    setTimeout(() => {
                        window.location.replace(landingUrl);
                    }, 400);
                } else {
                    const envelope = response?.data ?? {};
                    Swal.fire({
                        ...swalOpts(),
                        icon: 'error',
                        title: L('signInFailedTitle'),
                        text: envelope.message || envelope.Message || L('signInFailedText'),
                        confirmButtonText: L('tryAgain'),
                    });
                }
            } catch (error) {
                Swal.fire({
                    ...swalOpts(),
                    icon: 'error',
                    title: L('errorTitle'),
                    text: error.response?.data?.message || L('errorText'),
                    confirmButtonText: L('ok'),
                });
            } finally {
                state.isSubmitting = false;
            }
        };

        async function setLang(lang) {
            const next = lang === 'en' ? 'en' : 'ar';
            applyDocumentLocale(next, { suppressLocaleEvent: true });
            try {
                await persistCulture(next);
            } catch (_) {
                window.location.reload();
                return;
            }
            window.location.reload();
        }

        function langBtnClass(code) {
            const on = uiLang.value === code;
            return on ? 'btn btn-secondary btn-sm' : 'btn btn-outline-secondary btn-sm';
        }

        Vue.onMounted(() => {
            (async () => {
                let lang = resolveLangFromHints();
                const hadCookie = !!readCultureFromCookie();
                if (!hadCookie) {
                    try {
                        await persistCulture(lang);
                    } catch (_) {
                        /* server optional */
                    }
                }
                uiLang.value = lang === 'en' ? 'en' : 'ar';
                applyDocumentLocale(uiLang.value, { suppressLocaleEvent: true });
            })().catch(() => {
                const lang = resolveLangFromHints();
                uiLang.value = lang === 'en' ? 'en' : 'ar';
                applyDocumentLocale(uiLang.value, { suppressLocaleEvent: true });
            });
        });

        return {
            state,
            uiLang,
            t: (key) => t(uiLang.value, key),
            setLang,
            langBtnClass,
            handleSubmit,
        };
    },
};

if (typeof Vue === 'undefined') {
    console.error('Vue is not loaded; login form cannot mount.');
    document.getElementById('app')?.removeAttribute('v-cloak');
} else {
    Vue.createApp(App).mount('#app');
}
