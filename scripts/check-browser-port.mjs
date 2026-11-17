// Checks the browser port of the inspection rules (site/assets/rules.js) against the shared fixtures
// that the C# tests also use. Run: node scripts/check-browser-port.mjs
import { readFileSync } from 'node:fs';
import { inspect } from '../site/assets/rules.js';

const { cases } = JSON.parse(readFileSync(new URL('../tests/parity/inspection-cases.json', import.meta.url), 'utf8'));
let failed = 0;
for (const c of cases) {
  const actual = inspect(c.html).map(f => f.code);
  const ok = JSON.stringify(actual) === JSON.stringify(c.expected);
  if (!ok) failed++;
  console.log(`${ok ? 'ok  ' : 'FAIL'} ${c.name}${ok ? '' : `: expected ${c.expected.join(',') || 'none'}, got ${actual.join(',') || 'none'}`}`);
}
if (failed) { console.error(`${failed} case(s) differ from the C# rules`); process.exit(1); }
console.log(`browser port matches all ${cases.length} shared cases`);
