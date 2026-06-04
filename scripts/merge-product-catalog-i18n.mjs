import fs from 'fs';
import path from 'path';

const root = path.resolve('c:/Users/HP/source/repos/macquires-crm');
const jsPath = path.join(root, 'Presentation/ASPNET/FrontEnd/Pages/Telecom/ProductCatalog.cshtml.js');
const js = fs.readFileSync(jsPath, 'utf8');

function extractLang(lang) {
    const re =
        lang === 'ar'
            ? /ar:\s*\{([\s\S]*?)\r?\n\s*\}\s*,\s*\r?\n\s*en:\s*\{/
            : /en:\s*\{([\s\S]*?)\r?\n\s*\}\r?\n\s*\}\s*,/;
    const m = js.match(re);
    if (!m) throw new Error(`extract ${lang}`);
    const body = `{${m[1]}}`;
    // eslint-disable-next-line no-eval
    return eval(`(${body})`);
}

const extraEn = {
    refreshFail: 'Failed to load catalog offerings.',
    errorTitle: 'Error',
    detailLoadFail: 'Failed to load details.',
    planSelectedRedirect: 'Plan "{name}" selected. Navigating to Operations Hub...',
    planType0: 'Monthly',
    planType1: 'Daily',
    planType2: 'Weekly',
    planType3: 'Pay-As-You-Go',
    planType4: 'One-Time Activation',
    planPeriod0: 'mo',
    planPeriod1: 'day',
    planPeriod2: 'wk',
    planPeriod3: 'unit',
    planPeriod4: 'once',
    swalWarning: 'Warning',
    requiredFields: 'Please fill all required fields (*).',
    saveOk: 'Saved successfully',
    saveFailed: 'Save failed',
    deleteOk: 'Deleted successfully',
    yesDelete: 'Yes, delete',
    serverError: 'Server error occurred.',
    deleteFailed: 'Delete failed.',
};
const extraAr = {
    refreshFail: 'تعذّر تحميل العروض.',
    errorTitle: 'خطأ',
    detailLoadFail: 'تعذّر تحميل التفاصيل.',
    planSelectedRedirect: 'تم تحديد باقة «{name}» وجاري الانتقال لمركز العمليات...',
    planType0: 'شهري',
    planType1: 'يومي',
    planType2: 'أسبوعي',
    planType3: 'حسب الاستهلاك',
    planType4: 'تفعيل لمرة واحدة',
    planPeriod0: 'شهر',
    planPeriod1: 'يوم',
    planPeriod2: 'أسبوع',
    planPeriod3: 'وحدة',
    planPeriod4: 'مرة',
    swalWarning: 'تنبيه',
    requiredFields: 'يرجى ملء جميع الحقول المطلوبة (*)',
    saveOk: 'تم الحفظ بنجاح',
    saveFailed: 'فشل الحفظ',
    deleteOk: 'تم الحذف بنجاح',
    yesDelete: 'نعم، احذف',
    serverError: 'حدث خطأ في الخادم.',
    deleteFailed: 'فشل الحذف.',
};

const productCatalogEn = { ...extractLang('en'), ...extraEn };
const productCatalogAr = { ...extractLang('ar'), ...extraAr };

for (const [file, block] of [
    ['telecom.en.json', productCatalogEn],
    ['telecom.ar.json', productCatalogAr],
]) {
    const p = path.join(root, 'Presentation/ASPNET/wwwroot/locales', file);
    const doc = JSON.parse(fs.readFileSync(p, 'utf8'));
    doc.telecom.productCatalog = block;
    fs.writeFileSync(p, JSON.stringify(doc, null, 2) + '\n', 'utf8');
    console.log('Updated', file);
}
