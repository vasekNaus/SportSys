# Tiskový vzor a zdroje

## Omezený příklad

Ukázka patří do SCSS partiálu zařazeného přes `@use 'print';` na konci
`site.scss`. Pokrývá jen aplikační layout a základ `table.grid`.
Další komponenty, tokeny témat, šířky a konkrétní filtry vyžadují vlastní
průchod podle [postupu skillu](../SKILL.md).

`.no-print` je navržené označení prvků v HTML. Má-li skrýt celý akční
sloupec, musí být na odpovídajícím `th` i všech `td`; ukázka jeho existenci
v šablonách nepředpokládá.

```scss
@media print {
  @page {
    size: A4 portrait;
    margin: 15mm 12mm;
  }

  html,
  body {
    color: #000;
    background: #fff;
  }

  body {
    width: auto;
    max-width: none;
    margin: 0;
    padding: 0;
  }

  body > main {
    padding: 0;
  }

  body > header,
  body > nav,
  body > footer,
  .no-print {
    display: none !important;
  }

  table.grid {
    width: 100%;
    color: #000;
    background: #fff;
    box-shadow: none;
  }

  table.grid thead th {
    color: #000;
    background: transparent;
    border-bottom: 1pt solid #000;
    white-space: normal;
  }

  table.grid td {
    color: #000;
    overflow-wrap: anywhere;
  }

  table.grid tbody tr,
  table.grid tbody tr:hover {
    background: transparent !important;
  }

  table.grid thead {
    display: table-header-group;
  }

  table.grid tr {
    break-inside: avoid;
  }

  h1, h2, h3 {
    break-after: avoid;
  }
}
```

`!important` u pozadí řádku reaguje na důležité hover deklarace existující
komponenty. Pořadí a specificitu ostatních pravidel je stále třeba ověřit.
Ukázka neupravuje rozvrh ani nezaručuje opakování záhlaví v každém enginu.

## Primární zdroje

- [MDN: Printing](https://developer.mozilla.org/en-US/docs/Web/CSS/Guides/Media_queries/Printing)
  — tiskový stylesheet, média a události.
- [MDN: @page](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/At-rules/@page)
  — stránkový box, rozměry a okraje.
- [MDN: print-color-adjust](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Properties/print-color-adjust)
  — přednost uživatelských voleb a úpravy barev prohlížečem.
- [MDN: break-before](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Properties/break-before)
  — zlomy a mapování legacy vlastností.
- [CSS Fragmentation: Possible Break Points](https://drafts.csswg.org/css-break-3/#possible-breaks)
  — monolitický obsah a overflow; pracovní specifikace.
- [MDN: orphans](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Properties/orphans)
  — řádky textu a kompatibilita.
- [W3C CSS 2.1: Table model](https://www.w3.org/TR/CSS2/tables.html#tables)
  — role záhlaví a patiček tabulek, možnost jejich opakování.
- [Chrome Developers: Print margins](https://developer.chrome.com/blog/print-margins)
  — margin boxes od Chrome 131 a vztah k hlavičkám prohlížeče;
  údaj není zárukou podpory jiných motorů.
- [WHATWG HTML: Printing](https://html.spec.whatwg.org/multipage/timers-and-user-prompts.html#printing)
  — tisk aktuálního dokumentu a tiskové události.
- [MDN: FontFaceSet.ready](https://developer.mozilla.org/en-US/docs/Web/API/FontFaceSet/ready)
  — připravenost fontů.
- [MDN: afterprint](https://developer.mozilla.org/en-US/docs/Web/API/Window/afterprint_event)
  — událost není potvrzení fyzického tisku.
- [Chrome DevTools: Emulate CSS media](https://developer.chrome.com/docs/devtools/rendering/emulate-css)
  — ladění mediálního typu.
- [Playwright: page.pdf()](https://playwright.dev/docs/api/class-page#page-pdf)
  — volitelné PDF ověření, `preferCSSPageSize` a `printBackground`.
- [GitHub: Agent skills](https://docs.github.com/en/copilot/concepts/customizations/agent-skills)
  — formát a použití agent skills.
