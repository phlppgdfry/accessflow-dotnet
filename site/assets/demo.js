import { inspect, EN } from './rules.js';

const PRESETS = {
  'Municipal page with issues': { consent: true, html: `<html>
<head><title></title></head>
<body>
  <h2>Informatie over studietoelagen</h2>
  <img src="studenten.jpg">
  <p>Je kan een toelage aanvragen tot 31 oktober.</p>
  <h4>Voorwaarden</h4>
  <a href="/voorwaarden">lees meer</a>
</body>
</html>` },
  'Clean page': { consent: false, html: `<html lang="nl">
<head><title>Praktische openingsuren | Gemeente</title></head>
<body>
  <main>
    <h1>Praktische openingsuren</h1>
    <h2>Loket burgerzaken</h2>
    <img src="loket.jpg" alt="Het loket burgerzaken aan de hoofdingang">
    <a href="/afspraak">Maak een afspraak bij burgerzaken</a>
  </main>
</body>
</html>` },
  'Start from scratch': { consent: false, html: '<html lang="en">\n<head><title>My page</title></head>\n<body>\n  <main>\n    <h1>Hello</h1>\n    <a href="#">click here</a>\n  </main>\n</body>\n</html>' },
};

const $ = id => document.getElementById(id);
const esc = s => s.replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
let state = 'Needs review', findings = [];

// log() takes trusted HTML from this file; anything read from the page goes through esc().
function log(text) {
  const li = document.createElement('li');
  li.innerHTML = `<span class="dim mono">${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}</span> ${text}`;
  $('af-audit').prepend(li);
}
function setStatus(s) {
  state = s; const el = $('af-status'); el.textContent = s;
  el.className = 'pill ' + (s === 'Approved' ? 'ok' : s === 'Changes requested' ? 'bad' : 'warn');
  $('af-approve').hidden = $('af-changes').hidden = s !== 'Needs review';
}
function run(fromEdit) {
  try { findings = inspect($('af-html').value); } catch (e) { $('af-summary').textContent = e.message; $('af-findings').innerHTML = ''; return; }
  $('af-summary').textContent = findings.length ? `${findings.length} checks need context from a reviewer` : 'No automated checks need attention. A human review remains useful.';
  $('af-findings').innerHTML = findings.map(f => `<div class="af-f ${f.severity.toLowerCase()}">
    <div class="af-ft"><b>${f.code} · ${esc(f.title)}</b><span class="pill ${f.severity === 'High' ? 'bad' : 'warn'}">${f.severity}</span></div>
    <div class="small dim">${EN[f.code]}</div>
    <div class="af-rec">${esc(f.recommendation)}</div>
    <code class="af-ev" title="What the rule matched">${esc(f.evidence.length > 90 ? f.evidence.slice(0, 90) + '…' : f.evidence)}</code></div>`).join('');
  $('af-suggestion').hidden = true;
  if (fromEdit && state !== 'Needs review') { setStatus('Needs review'); log('Content changed: back to <b>Needs review</b>'); }
  $('af-suggest').disabled = !$('af-consent').checked;
  $('af-suggest').title = $('af-consent').checked ? '' : 'This item has no AI consent.';
}
function suggest() {
  if (!$('af-consent').checked) return;
  const top = findings.find(f => f.severity === 'High') || findings[0];
  const box = $('af-suggestion'); box.hidden = false;
  box.innerHTML = top
    ? `<b>Safe suggestion</b> Begin met "${esc(top.title.toLowerCase())}": ${esc(top.recommendation)}<div class="small dim" style="margin-top:6px">Geen inhoud werd doorgestuurd naar een extern model; een mens beslist altijd over publicatie.</div>`
    : `<b>Safe suggestion</b> Geen automatische bevindingen. Laat een redacteur de leesbaarheid nalezen.`;
  log('Suggestion shown to the reviewer (status unchanged)');
}
$('af-presets').innerHTML = Object.keys(PRESETS).map(n => `<button type="button" role="tab">${n}</button>`).join('');
function load(n) {
  $('af-html').value = PRESETS[n].html; $('af-consent').checked = PRESETS[n].consent;
  document.querySelectorAll('#af-presets button').forEach(b => b.setAttribute('aria-selected', b.textContent === n));
  $('af-audit').innerHTML = ''; setStatus('Needs review'); run(); log(`Submitted “${esc(n)}”, AI consent: <b>${PRESETS[n].consent ? 'yes' : 'no'}</b>`);
}
$('af-presets').addEventListener('click', e => e.target.tagName === 'BUTTON' && load(e.target.textContent));
$('af-html').addEventListener('input', () => run(true));
$('af-consent').addEventListener('change', () => { run(); log(`AI consent changed to <b>${$('af-consent').checked ? 'yes' : 'no'}</b>`); });
$('af-suggest').addEventListener('click', suggest);
$('af-approve').addEventListener('click', () => { setStatus('Approved'); log(`<b>Approved</b> by a reviewer with ${findings.length} open finding(s) acknowledged`); });
$('af-changes').addEventListener('click', () => { setStatus('Changes requested'); log('<b>Changes requested</b>, sent back to the editor'); });
load('Municipal page with issues');
