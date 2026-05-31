/**
 * Replaces state.mainTitle = '...' with MacquiresUiI18n.mb(...) in *List.cshtml.js
 * Run from Presentation/ASPNET: node wwwroot/lib/indotalent/apply-modal-i18n.mjs
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const pagesRoot = path.join(__dirname, '..', '..', '..', 'FrontEnd', 'Pages');

function escapeRe(s) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

/** [entityKey, englishNoun for Add/Edit/Delete — must match file strings after verb] */
const rows = [
  ['purchaseRequisition', 'Purchase Requisition'],
  ['purchaseOrder', 'Purchase Order'],
  ['customer', 'Customer'],
  ['customerContact', 'Customer Contact'],
  ['debitNote', 'Debit Note'],
  ['creditNote', 'Credit Note'],
  ['product', 'Product'],
  ['budget', 'Budget'],
  ['expense', 'Expense'],
  ['leadContact', 'Lead Contact'],
  ['salesRepresentative', 'Sales Representative'],
  ['stockCount', 'Stock Count'],
  ['todo', 'Todo'],
  ['deliveryOrder', 'Delivery Order'],
  ['scrapping', 'Scrapping'],
  ['purchaseReturn', 'Purchase Return'],
  ['transferOut', 'Transfer Out'],
  ['transferIn', 'Transfer In'],
  ['user', 'User'],
  ['vendorContact', 'Vendor Contact'],
  ['warehouse', 'Warehouse'],
  ['lead', 'Lead'],
  ['customerCategory', 'Customer Category'],
  ['customerGroup', 'Customer Group'],
  ['positiveAdjustment', 'Positive Adjustment'],
  ['vendorGroup', 'Vendor Group'],
  ['salesReturn', 'Sales Return'],
  ['programManager', 'Program Manager'],
  ['tax', 'Tax'],
  ['paymentReceive', 'Payment Receive'],
  ['productGroup', 'Product Group'],
  ['salesQuotation', 'Sales Quotation'],
  ['vendorCategory', 'Vendor Category'],
  ['campaign', 'Campaign'],
  ['bookingGroup', 'Booking Group'],
  ['goodsReceive', 'Goods Receive'],
  ['vendor', 'Vendor'],
  ['paymentMethod', 'Payment Method'],
  ['programResource', 'ProgramResource'],
  ['leadActivity', 'Lead Activity'],
  ['negativeAdjustment', 'Negative Adjustment'],
  ['paymentDisburse', 'Payment Disburse'],
  ['salesOrder', 'Sales Order'],
  ['salesTeam', 'Sales Team'],
  ['invoice', 'Invoice'],
  ['bill', 'Bill'],
  ['todoItem', 'Todo Item'],
  ['bookingResource', 'Booking Resource'],
  ['unitMeasure', 'Unit Measure'],
  ['booking', 'Booking'],
];

/** Irregular English strings → [entityKey, verb] */
const specials = [
  ["Add CreditNote", 'creditNote', 'add'],
  ["Edit CreditNote", 'creditNote', 'edit'],
  ["Edit DebitNote", 'debitNote', 'edit'],
];

function walkDir(dir, acc = []) {
  for (const name of fs.readdirSync(dir)) {
    const p = path.join(dir, name);
    const st = fs.statSync(p);
    if (st.isDirectory()) walkDir(p, acc);
    else if (name.endsWith('List.cshtml.js')) acc.push(p);
  }
  return acc;
}

function patchContent(content) {
  let c = content;
  for (const [fragment, entity, verb] of specials) {
    const re = new RegExp(`(^\\s*)state\\.mainTitle = '${escapeRe(fragment)}';`, 'gm');
    c = c.replace(re, `$1state.mainTitle = MacquiresUiI18n.mb('${entity}','${verb}');`);
  }
  for (const [key, en] of rows) {
    for (const verb of ['add', 'edit', 'delete']) {
      let label;
      if (verb === 'add') label = `Add ${en}`;
      else if (verb === 'edit') label = `Edit ${en}`;
      else label = `Delete ${en}?`;
      const re = new RegExp(`(^\\s*)state\\.mainTitle = '${escapeRe(label)}';`, 'gm');
      c = c.replace(re, `$1state.mainTitle = MacquiresUiI18n.mb('${key}','${verb}');`);
    }
  }
  return c;
}

const files = walkDir(pagesRoot);
let changed = 0;
for (const f of files) {
  const before = fs.readFileSync(f, 'utf8');
  const after = patchContent(before);
  if (after !== before) {
    fs.writeFileSync(f, after, 'utf8');
    changed++;
    console.log('patched', path.relative(pagesRoot, f));
  }
}
console.log('done, files changed:', changed, 'total list files:', files.length);
