// Browser port of src/AccessFlow.Application/ContentInspectionService.cs: the same six patterns,
// rule codes, severities and Dutch texts. tests/parity/inspection-cases.json is checked against both
// implementations in CI (dotnet test + scripts/check-browser-port.mjs), so they cannot drift apart.
const RX = {
  lang: /<html\b[^>]*\slang\s*=\s*(?:"[^"\s]+"|'[^'\s]+'|[^\s"'>]+)/i,
  title: /<title[^>]*>\s*[^<\s][^<]*<\/title>/is,
  main: /<main(?:\s|>)/i,
  imgNoAlt: /<img(?![^>]*\salt\s*=)[^>]*>/i,
  vague: /<a[^>]*>\s*(klik hier|lees meer|click here|read more)\s*<\/a>/i,
  heading: /<h([1-6])(?:\s|>)/gi,
};
export const EN = {
  'AF-001': 'Document language missing', 'AF-002': 'Page title missing', 'AF-003': 'Main content not marked up',
  'AF-004': 'Image without text alternative', 'AF-005': 'Vague link text', 'AF-006': 'Heading structure needs a check',
};

export function inspect(html) {
  if (!html || !html.trim()) throw new Error('HTML is required (ArgumentException in the .NET service).');
  const f = [];
  const add = (code, severity, title, detail, recommendation, evidence) => f.push({ code, severity, title, detail, recommendation, evidence });
  if (!RX.lang.test(html)) add('AF-001', 'High', 'Documenttaal ontbreekt', 'De HTML bevat geen lang-attribuut op het html-element.', 'Declareer de primaire taal, bijvoorbeeld <html lang="nl">.', (html.match(/<html[^>]*>/i) || ['no <html> element'])[0]);
  if (!RX.title.test(html)) add('AF-002', 'Medium', 'Paginatitel ontbreekt', 'Er is geen niet-lege title-tag gevonden.', 'Voeg een unieke, beschrijvende paginatitel toe.', (html.match(/<title[^>]*>[^<]*<\/title>/i) || ['no <title> element'])[0]);
  if (!RX.main.test(html)) add('AF-003', 'Medium', 'Hoofdinhoud is niet gemarkeerd', 'Er is geen main-landmark gevonden.', 'Plaats de primaire inhoud in een <main>-element.', 'no <main> element');
  const img = html.match(RX.imgNoAlt);
  if (img) add('AF-004', 'High', 'Afbeelding zonder alternatief', 'Minstens één afbeelding heeft geen alt-attribuut.', 'Gebruik betekenisvolle alt-tekst of een lege alt voor decoratieve beelden.', img[0]);
  const vague = html.match(RX.vague);
  if (vague) add('AF-005', 'Medium', 'Onduidelijke linktekst', 'Een link gebruikt generieke tekst zoals ‘klik hier’ of ‘lees meer’.', 'Maak de linktekst zelfstandig begrijpelijk, ook buiten de omringende context.', vague[0]);
  const levels = [...html.matchAll(RX.heading)].map(m => +m[1]);
  const skipped = levels.some((l, i) => i && l > levels[i - 1] + 1);
  if (!levels.length || levels[0] !== 1 || skipped) add('AF-006', 'Medium', 'Headingstructuur verdient controle', 'De headinghiërarchie start niet met h1 of slaat een niveau over.', 'Gebruik één duidelijke h1 en verhoog headingniveaus stapsgewijs.', levels.length ? 'heading order: ' + levels.map(l => 'h' + l).join(' → ') : 'no headings');
  return f;
}

