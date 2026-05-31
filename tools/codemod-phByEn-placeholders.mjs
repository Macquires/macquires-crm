/**
 * Replaces literal Syncfusion placeholders with MacquiresUiI18n.phByEn('…').
 * Run from repo root: node tools/codemod-phByEn-placeholders.mjs
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const pagesRoot = path.join(__dirname, '..', 'Presentation', 'ASPNET', 'FrontEnd', 'Pages');

function walkJs(dir, out = []) {
    for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
        const p = path.join(dir, ent.name);
        if (ent.isDirectory()) walkJs(p, out);
        else if (ent.name.endsWith('.cshtml.js')) out.push(p);
    }
    return out;
}

let files = walkJs(pagesRoot);
let changed = 0;

for (const file of files) {
    let s = fs.readFileSync(file, 'utf8');
    const orig = s;

    s = s.replace(/placeholder:\s*'([^']*)'/g, (full, inner) => {
        return `placeholder: MacquiresUiI18n.phByEn('${inner.replace(/\\/g, '\\\\').replace(/'/g, "\\'")}')`;
    });

    s = s.replace(/filterBarPlaceholder:\s*'Search'/g, "filterBarPlaceholder: MacquiresUiI18n.phByEn('Search')");

    // BookingScheduler inline: ", placeholder: 'Status'" may already be handled by generic replace
    if (s !== orig) {
        fs.writeFileSync(file, s, 'utf8');
        changed++;
    }
}

console.log('files updated:', changed, '/', files.length);
