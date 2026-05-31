/**
 * Merges placeholder English→{ar,en} map into wwwroot/locales/ui-modals.json as top-level "phByEn".
 * Run: node tools/merge-phByEn-into-ui-modals.mjs
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(__dirname, '..');
const uiPath = path.join(root, 'Presentation', 'ASPNET', 'wwwroot', 'locales', 'ui-modals.json');

/** All unique placeholder strings from FrontEnd Pages *.cshtml.js (scan 2026-05) */
const AR = {
  Search: 'بحث',
  'Authority Score': 'درجة الصلاحية',
  'Budget Score': 'درجة الميزانية',
  'Enter Amount': 'أدخل المبلغ',
  'Enter City': 'أدخل المدينة',
  'Enter Closed Amount': 'أدخل المبلغ المغلق',
  'Enter Company Name': 'أدخل اسم الشركة',
  'Enter Country': 'أدخل الدولة',
  'Enter Currency': 'أدخل العملة',
  'Enter Email': 'أدخل البريد الإلكتروني',
  'Enter Email Address': 'أدخل عنوان البريد الإلكتروني',
  'Enter Employee Number': 'أدخل رقم الموظف',
  'Enter Facebook': 'أدخل فيسبوك',
  'Enter Fax Number': 'أدخل رقم الفاكس',
  'Enter First Name': 'أدخل الاسم الأول',
  'Enter Full Name': 'أدخل الاسم الكامل',
  'Enter Instagram': 'أدخل إنستغرام',
  'Enter Job Title': 'أدخل المسمى الوظيفي',
  'Enter Last Name': 'أدخل اسم العائلة',
  'Enter LinkedIn': 'أدخل لينكد إن',
  'Enter Location': 'أدخل الموقع',
  'Enter Mobile Number': 'أدخل رقم الجوال',
  'Enter Name': 'أدخل الاسم',
  'Enter Payment Amount': 'أدخل مبلغ الدفع',
  'Enter Percentage': 'أدخل النسبة المئوية',
  'Enter Phone Number': 'أدخل رقم الهاتف',
  'Enter State': 'أدخل المحافظة',
  'Enter Street': 'أدخل الشارع',
  'Enter Subject': 'أدخل الموضوع',
  'Enter Summary': 'أدخل الملخص',
  'Enter Target Amount': 'أدخل المبلغ المستهدف',
  'Enter TikTok': 'أدخل تيك توك',
  'Enter Title': 'أدخل العنوان',
  'Enter Twitter': 'أدخل تويتر',
  'Enter Twitter/X': 'أدخل تويتر / X',
  'Enter Unit Price': 'أدخل سعر الوحدة',
  'Enter Website': 'أدخل الموقع الإلكتروني',
  'Enter WhatsApp': 'أدخل واتساب',
  'Enter Zip Code': 'أدخل الرمز البريدي',
  'Need Score': 'درجة الحاجة',
  'Priority': 'الأولوية',
  'Resource': 'المورد',
  'Select Bill': 'اختر الفاتورة',
  'Select Bill Status': 'اختر حالة الفاتورة',
  'Select Booking Resource': 'اختر مورد الحجز',
  'Select Campaign': 'اختر الحملة',
  'Select CreditNote Status': 'اختر حالة إشعار الدائن',
  'Select Date': 'اختر التاريخ',
  'Select DebitNote Status': 'اختر حالة إشعار المدين',
  'Select Delivery Order': 'اختر أمر التسليم',
  'Select End Time': 'اختر وقت الانتهاء',
  'Select Invoice': 'اختر الفاتورة',
  'Select Invoice Status': 'اختر حالة الفاتورة',
  'Select Payment Disburse Status': 'اختر حالة الصرف',
  'Select Payment Method': 'اختر طريقة الدفع',
  'Select Payment Receive Status': 'اختر حالة التحصيل',
  'Select Priority': 'اختر الأولوية',
  'Select Product': 'اختر المنتج',
  'Select Purchase Order': 'اختر أمر الشراء',
  'Select Purchase Return': 'اختر مرتجع الشراء',
  'Select Resource': 'اختر المورد',
  'Select Sales Order': 'اختر أمر البيع',
  'Select Sales Return': 'اختر مرتجع البيع',
  'Select Start Time': 'اختر وقت البدء',
  'Select Status': 'اختر الحالة',
  'Select Transfer Out': 'اختر إخراج المخزون',
  'Select Warehouse': 'اختر المستودع',
  'Select Warehouse From': 'اختر المستودع المصدر',
  'Select Warehouse To': 'اختر المستودع الوجهة',
  'Select a Booking Group': 'اختر مجموعة الحجز',
  'Select a Campaign': 'اختر الحملة',
  'Select a Closing Status': 'اختر حالة الإغلاق',
  'Select a Customer': 'اختر المشترك',
  'Select a Customer Category': 'اختر فئة المشترك',
  'Select a Customer Group': 'اختر مجموعة المشتركين',
  'Select a Lead': 'اختر العميل المحتمل',
  'Select a Pipeline Stage': 'اختر مرحلة المسار',
  'Select a Product': 'اختر المنتج',
  'Select a Product Group': 'اختر مجموعة المنتجات',
  'Select a Sales Team': 'اختر فريق المبيعات',
  'Select a Tax': 'اختر الضريبة',
  'Select a Todo': 'اختر المهمة',
  'Select a Unit Measure': 'اختر وحدة القياس',
  'Select a Vendor': 'اختر المورد',
  'Select a Vendor Category': 'اختر فئة المورد',
  'Select a Vendor Group': 'اختر مجموعة الموردين',
  'Select a Warehouse': 'اختر المستودع',
  'Select an Activity Type': 'اختر نوع النشاط',
  'Select an Order Status': 'اختر حالة الطلب',
  'Status': 'الحالة',
  'Summary': 'الملخص',
  'Timeline Score': 'درجة الجدول الزمني',
  '[auto]': '[تلقائي]'
};

const phByEn = {};
for (const [en, ar] of Object.entries(AR)) {
  phByEn[en] = { ar, en };
}

const raw = fs.readFileSync(uiPath, 'utf8');
const data = JSON.parse(raw);
data.phByEn = { ...(data.phByEn || {}), ...phByEn };
fs.writeFileSync(uiPath, JSON.stringify(data, null, 2) + '\n', 'utf8');
console.log('merged phByEn keys:', Object.keys(phByEn).length);
