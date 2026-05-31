/**
 * One-off generator: locales/ui-modals.json from entity modal strings.
 * Run: node wwwroot/lib/indotalent/build-ui-modals.mjs (from Presentation/ASPNET)
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

/** [entityKey, englishAddSuffix, arabicNounPhrase] — Add/Edit/Delete built from English pattern in List.cshtml.js */
const rows = [
  ['purchaseRequisition', 'Purchase Requisition', 'طلب شراء'],
  ['purchaseOrder', 'Purchase Order', 'أمر شراء'],
  ['customer', 'Customer', 'مشترك'],
  ['customerContact', 'Customer Contact', 'جهة اتصال مشترك'],
  ['debitNote', 'Debit Note', 'إشعار مدين'],
  ['creditNote', 'Credit Note', 'إشعار دائن'],
  ['product', 'Product', 'منتج'],
  ['budget', 'Budget', 'ميزانية'],
  ['expense', 'Expense', 'مصروف'],
  ['leadContact', 'Lead Contact', 'جهة اتصال عميل محتمل'],
  ['salesRepresentative', 'Sales Representative', 'مندوب مبيعات'],
  ['stockCount', 'Stock Count', 'جرد مخزون'],
  ['todo', 'Todo', 'مهمة'],
  ['deliveryOrder', 'Delivery Order', 'أمر تسليم'],
  ['scrapping', 'Scrapping', 'إتلاف مخزون'],
  ['purchaseReturn', 'Purchase Return', 'مرتجع مشتريات'],
  ['transferOut', 'Transfer Out', 'صرف تحويل'],
  ['transferIn', 'Transfer In', 'استلام تحويل'],
  ['user', 'User', 'مستخدم'],
  ['vendorContact', 'Vendor Contact', 'جهة اتصال مورد'],
  ['warehouse', 'Warehouse', 'مستودع'],
  ['lead', 'Lead', 'عميل محتمل'],
  ['customerCategory', 'Customer Category', 'تصنيف مشترك'],
  ['customerGroup', 'Customer Group', 'مجموعة مشتركين'],
  ['positiveAdjustment', 'Positive Adjustment', 'تسوية موجبة'],
  ['vendorGroup', 'Vendor Group', 'مجموعة موردين'],
  ['salesReturn', 'Sales Return', 'مرتجع مبيعات'],
  ['programManager', 'Program Manager', 'مدير برنامج'],
  ['tax', 'Tax', 'ضريبة'],
  ['paymentReceive', 'Payment Receive', 'قبض دفعة'],
  ['productGroup', 'Product Group', 'مجموعة منتجات'],
  ['salesQuotation', 'Sales Quotation', 'عرض سعر مبيعات'],
  ['vendorCategory', 'Vendor Category', 'تصنيف مورد'],
  ['campaign', 'Campaign', 'حملة'],
  ['bookingGroup', 'Booking Group', 'مجموعة حجوزات'],
  ['goodsReceive', 'Goods Receive', 'استلام بضاعة'],
  ['vendor', 'Vendor', 'مورد'],
  ['paymentMethod', 'Payment Method', 'طريقة دفع'],
  ['programResource', 'ProgramResource', 'مورد برنامج'],
  ['leadActivity', 'Lead Activity', 'نشاط عميل محتمل'],
  ['negativeAdjustment', 'Negative Adjustment', 'تسوية سالبة'],
  ['paymentDisburse', 'Payment Disburse', 'صرف دفعة'],
  ['salesOrder', 'Sales Order', 'أمر بيع'],
  ['salesTeam', 'Sales Team', 'فريق مبيعات'],
  ['invoice', 'Invoice', 'فاتورة'],
  ['bill', 'Bill', 'فاتورة مورد'],
  ['todoItem', 'Todo Item', 'عنصر مهمة'],
  ['bookingResource', 'Booking Resource', 'مورد حجز'],
  ['unitMeasure', 'Unit Measure', 'وحدة قياس'],
  ['booking', 'Booking', 'حجز'],
];

const out = {};
for (const [key, en, ar] of rows) {
  out[key] = {
    add: { ar: `إضافة ${ar}`, en: `Add ${en}` },
    edit: { ar: `تعديل ${ar}`, en: `Edit ${en}` },
    delete: { ar: `حذف ${ar}؟`, en: `Delete ${en}?` },
  };
}

const outPath = path.join(__dirname, '..', '..', 'locales', 'ui-modals.json');
fs.writeFileSync(outPath, JSON.stringify(out, null, 2), 'utf8');
console.log('Wrote', outPath);
