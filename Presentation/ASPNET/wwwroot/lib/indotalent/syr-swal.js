/**

 * Global SweetAlert2 defaults — compact Syriatel styling (login-style).

 * Patches Swal.fire so existing calls inherit typography and layout without edits.

 */

(function (global) {

    if (typeof global.Swal === 'undefined' || global.Swal.__syrTelPatched) {

        return;

    }



    const nativeFire = global.Swal.fire.bind(global.Swal);



    function isRtlPreferred(opts) {

        if (opts && typeof opts.rtl === 'boolean') {

            return opts.rtl;

        }

        const dir = global.document?.documentElement?.getAttribute('dir');

        return dir === 'rtl';

    }



    function popupClassFor(opts) {

        const parts = ['syr-swal-popup'];

        const extra = opts.customClass?.popup;

        if (extra && typeof extra === 'string') {

            parts.push(extra);

        }

        const w = opts.width;

        const px = typeof w === 'number' ? w : typeof w === 'string' ? parseInt(w, 10) : 0;

        if (px > 400 || extra === 'audit-swal-popup') {

            parts.push('syr-swal-popup--wide');

        }

        return parts.join(' ');

    }



    function normalizeOptions(arg) {

        let opts = arg;

        if (typeof arg === 'string') {

            opts = { title: arg };

        }

        if (!opts || typeof opts !== 'object') {

            opts = {};

        }



        const merged = { ...opts };

        merged.buttonsStyling = merged.buttonsStyling ?? false;

        merged.heightAuto = merged.heightAuto ?? true;

        const rtlPreferred = isRtlPreferred(merged);

        delete merged.rtl;

        merged.customClass = {

            container: 'syr-swal-container',

            title: 'syr-swal-title',

            htmlContainer: 'syr-swal-html',

            confirmButton: 'btn syr-swal-confirm',

            cancelButton: 'btn syr-swal-cancel',

            denyButton: 'btn syr-swal-deny',

            actions: 'syr-swal-actions',

            icon: 'syr-swal-icon',

            ...(merged.customClass || {}),

            popup: (() => {
                const base = popupClassFor(merged);
                return rtlPreferred && !base.includes('syr-swal-popup--rtl')
                    ? `${base} syr-swal-popup--rtl`
                    : base;
            })(),

        };



        return merged;

    }



    function fireNormalized(arg1, arg2, arg3) {

        if (typeof arg1 === 'string') {

            return nativeFire(

                normalizeOptions({

                    title: arg1,

                    html: arg2,

                    icon: arg3,

                })

            );

        }

        return nativeFire(normalizeOptions(arg1));

    }



    global.Swal.fire = fireNormalized;

    global.Swal.__syrTelPatched = true;



    global.SyrTelSwal = {

        fire: (opts) => fireNormalized(opts),

        mixin: (opts) => global.Swal.mixin(opts),

        nativeFire,

    };

})(typeof window !== 'undefined' ? window : globalThis);

