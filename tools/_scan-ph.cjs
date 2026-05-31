'use strict';
const fs = require('fs');
const path = require('path');
const root = path.join(__dirname, '..', 'Presentation', 'ASPNET', 'FrontEnd', 'Pages');
const re = /placeholder:\s*['"]([^'"]+)['"]/g;
const s = new Set();
function walk(d) {
  for (const n of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, n.name);
    if (n.isDirectory()) walk(p);
    else if (n.name.endsWith('.cshtml.js')) {
      const c = fs.readFileSync(p, 'utf8');
      let m;
      while ((m = re.exec(c)) !== null) s.add(m[1]);
    }
  }
}
walk(root);
console.log([...s].sort().join('\n'));
console.error('count', s.size);
