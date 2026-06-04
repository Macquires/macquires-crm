import fs from 'fs';

const p = new URL('../Presentation/ASPNET/FrontEnd/Pages/Telecom/BackOfficeDashboard.cshtml.js', import.meta.url);
let s = fs.readFileSync(p, 'utf8');
const before = (s.match(/t\([^)]+,\s*['"`]/g) || []).length;
s = s.replace(/t\(\s*(['"])([^'"]+)\1\s*,\s*(['"])[^'"]*\3\s*\)/g, 't($1$2$1)');
s = s.replace(/t\(\s*(['"])([^'"]+)\1\s*,\s*`[^`]*`\s*\)/g, 't($1$2$1)');
fs.writeFileSync(p, s);
const after = (s.match(/t\([^)]+,\s*['"`]/g) || []).length;
console.log('before', before, 'after', after);
