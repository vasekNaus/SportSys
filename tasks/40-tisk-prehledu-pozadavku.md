# Implementační plán: #40 Porovnání požadavků a plánovaných tréninků trenérů — tisk stránky Požadavky

**Issue:** [#40 — Porovnání požadavků a plánovaných tréninků trenérů](https://github.com/vasekNaus/SportSys/issues/40)

**Komentář:** [Tisk stránky (26. 10. 2026)](https://github.com/vasekNaus/SportSys/issues/40#issuecomment-6408246855)

**Stav:** Připraveno k implementaci.

## Cíl

Umožnit tisk stránky `/sport/Training/Requirement` tak, aby výstup obsahoval
pouze tabulky, byl orientován na šířku a každá ze tří tabulek začínala na
vlastní stránce:

1. Požadavky na tréninky,
2. Požadavky na zápasy,
3. Porovnání požadavků a plánovaných tréninků trenérů.

## Výchozí stav

- Původní rozsah #40 je implementován; viz
  `tasks/completed/40-porovnani-pozadavku-a-planovanych-treninku-treneru.md`
  a `...-IMPL.md`.
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml`
  vykresluje filtr `form.schedule-filter`, nadpisy `<h2>` s tabulkami
  `table.grid` (tréninky při `ShowTrainings`, zápasy při `ShowMatches`) a
  na konci `TrainingRequirementCoverage` ViewComponent (jen při
  `ShowTrainings`). Sekce nejsou obaleny žádným kontejnerem.
- Obě tabulky požadavků mají poslední sloupec „Akce“ s tlačítkem úprav
  (`td.buttons`).
- `Pages/Shared/Components/TrainingRequirementCoverage/Default.cshtml` má
  kořen `section.training-requirement-coverage` a obal
  `.training-requirement-coverage-table-wrapper` (`overflow-x: auto`);
  tabulka má `min-width: 760px`
  (`Styles/_training-requirement-coverage.scss`).
- `_Layout.cshtml` obsahuje `body > header`, `body > nav`, `main > h1`
  a `body > footer`.
- V SCSS ani v `wwwroot/js/site.js` neexistují žádná tisková pravidla
  (`@media print`, `@page`, `window.print`).
- Tmavé tokeny v `Styles/_vars.scss` platí pro `[data-theme="dark"], .dark`
  a pro `@media (prefers-color-scheme: dark)` bez omezení na médium — tmavý
  režim by se tedy propsal i do tisku.
- `tests/SportSys.Razor.Tests/TrainingRequirementTests.cs` renderuje
  `Requirement/Index.cshtml` a přesně porovnává atributy `<th>`/`<td>` obou
  tabulek (např. `<td class="buttons c">`, `<th>`).

## Potvrzené požadavky a rozhodnutí

Potvrzeno komentářem:

- Stránku Požadavky lze vytisknout; důležité jsou tabulky.
- Tisk je na šířku.
- Stránka 1: Požadavky na tréninky; stránka 2: Požadavky na zápasy;
  stránka 3: Porovnání požadavků a plánovaných tréninků trenérů.

Technická rozhodnutí (odvozená z kódu a konvencí):

- Řešení je čistě prezentační: CSS `@media print` v SCSS + tlačítko
  „Tisk“ volající `window.print()`. Žádná nová stránka, PDF knihovna ani
  změna Contract/Database.
- Tisk respektuje aktuální filtr. Tabulka skrytá filtrem (`ShowTrainings`,
  `ShowMatches`) se netiskne a následující se posune; prázdná sekce
  vytiskne existující text „Žádné požadavky … neodpovídají filtru.“
- „Na jedné stránce“ se implementuje jako *každá tabulka začíná na nové
  stránce*. Delší tabulka pokračuje na další stránce s opakovanou hlavičkou
  (`thead` jako `table-header-group`) a řádky se nedělí (`break-inside:
  avoid`). Zmenšování obsahu „na jednu stranu“ CSS spolehlivě neumí a není
  plánováno.
- V tisku se skrývají header, navigace, `h1`, filtr, patička, sloupec
  „Akce“ a tlačítka.
- Orientace na šířku přes pojmenovanou stránku CSS
  (`@page requirement-print { size: A4 landscape; }` +
  `page: requirement-print`), aby se globálně neměnila orientace tisku
  ostatních stránek. Chromium/Edge i aktuální Firefox to podporují; v
  nepodporujícím prohlížeči lze orientaci zvolit v tiskovém dialogu.
- Tisk vždy používá světlé barvy: tmavé tokeny v `_vars.scss` se omezí
  na `@media screen`.
- Barevné stavy v porovnání (ikony) a hlavička tabulky se tisknou barevně
  (`print-color-adjust: exact`); stav je i nadále nesen ikonou a textem,
  takže je čitelný i černobíle.

## Technický návrh

### Markup (`Requirement/Index.cshtml`)

- Každou ze tří sekcí (nadpis + tabulka/hláška) obalit
  `<section class="print-page">`. Porovnání se obalí v místě volání
  `Component.InvokeAsync("TrainingRequirementCoverage", …)`, ne uvnitř
  sdíleného view, aby se zalomení netýkalo stránky Plan.
- Kořenovému kontejneru výsledků (vše za formulářem) přidat třídu
  `requirement-print`, na kterou se váže pojmenovaná `@page`.
- Sloupec „Akce“ (`th` i `td`) v obou tabulkách označit utilitní třídou
  `print-hidden`.
- Do `div.schedule-filter-actions` přidat tlačítko:
  `<button type="button" class="button secondary" data-print-page>`
  s ikonou `fa-print` a textem „Tisk“. Je uvnitř filtru, takže se v tisku
  skryje spolu s ním.

### JavaScript (`wwwroot/js/site.js`)

- Nová IIFE dle stávajícího vzoru: delegovaný `click` na
  `[data-print-page]` → `window.print()`. Bez inline JS v markupu.

### SCSS

- Nový parciál `Styles/_print.scss`, importovaný v `Styles/site.scss` jako
  poslední (`@use 'print';`), obsahuje vše uvnitř `@media print`:
  - obecně: skrytí `body > header`, `body > nav`, `body > footer`,
    `.print-hidden`; reset `body` (`max-width: none`, `margin`/`padding: 0`,
    bílé pozadí, menší písmo cca 9–10 pt); `table.grid` bez `box-shadow`,
    `thead { display: table-header-group }`, `tr { break-inside: avoid }`,
    `h2 { break-after: avoid }`;
  - stránka Požadavky: `@page requirement-print { size: A4 landscape;
    margin: 10mm; }`, `.requirement-print { page: requirement-print; }`,
    skrytí filtru (formulář dostane v `Index.cshtml` třídu
    `print-hidden`) a nadpisu stránky přes
    `main:has(.requirement-print) > h1` (bez úpravy `_Layout.cshtml`),
    `.print-page + .print-page`/`.print-page ~ .print-page
    { break-before: page; }`, první `h2` bez horního okraje;
  - porovnání: `.training-requirement-coverage-table-wrapper
    { overflow: visible; }`, `.training-requirement-coverage-table
    { min-width: 0; }`, `margin-block: 0` sekce.
- `Styles/_vars.scss`: tmavé bloky (`[data-theme="dark"], .dark` a
  `@media (prefers-color-scheme: dark)`) zabalit do `@media screen` /
  `@media screen and (prefers-color-scheme: dark)`. Hodnoty tokenů se
  nemění.
- `wwwroot/css/site.css` se needituje ručně; generuje se `npm run build:css`.

## Implementační kroky

### Fáze 1: Markup a tlačítko

1. Upravit `Requirement/Index.cshtml` (sekce `print-page`, kontejner
   `requirement-print`, `print-hidden` na sloupci Akce a formuláři,
   tlačítko Tisk).
2. Přidat obsluhu `[data-print-page]` do `wwwroot/js/site.js`.

### Fáze 2: Tiskové styly

1. Vytvořit `Styles/_print.scss` a zaregistrovat jej v `Styles/site.scss`.
2. Omezit tmavé tokeny v `Styles/_vars.scss` na `screen`.
3. Spustit `npm run build:css` v `src\SportSys.Razor`.

### Fáze 3: Testy a dokumentace

1. Aktualizovat render test v `TrainingRequirementTests.cs` (viz níže).
2. Doplnit dokumentaci (`docs/modules/sport.md`, `docs/modules/frontend.md`).

## Soubory ke změně

| Soubor | Změna |
|---|---|
| `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml` | Sekce `print-page`, třídy pro tisk, tlačítko Tisk |
| `src/SportSys.Razor/wwwroot/js/site.js` | Handler `[data-print-page]` |
| `src/SportSys.Razor/Styles/_print.scss` | Nový parciál s `@media print` a `@page` |
| `src/SportSys.Razor/Styles/site.scss` | `@use 'print';` |
| `src/SportSys.Razor/Styles/_vars.scss` | Tmavé tokeny jen pro `screen` |
| `src/SportSys.Razor/wwwroot/css/site.css` | Pouze přegenerovat přes `npm run build:css` |
| `tests/SportSys.Razor.Tests/TrainingRequirementTests.cs` | Úprava očekávaných atributů a nové aserce |
| `docs/modules/sport.md` | Sekce „Požadavky na tréninky a zápasy“: popis tisku |
| `docs/modules/frontend.md` | Konvence `print-hidden`, `print-page`, `_print.scss`, tisk ve světlém režimu |

## Testy a ověření

Automaticky (`TrainingRequirementTests`, existující render
`Requirement/Index.cshtml`):

- Aktualizovat očekávané atributy sloupce Akce na
  `<th class="c print-hidden">` / `<td class="buttons c print-hidden">`
  a `<th class="print-hidden">` / `<td class="buttons print-hidden">`.
- Ověřit, že výstup obsahuje v pořadí sekce `print-page` s nadpisy
  „Požadavky na tréninky“, „Požadavky na zápasy“ a „Porovnání požadavků a
  plánovaných tréninků trenérů“.
- Ověřit přítomnost tlačítka s `data-print-page`.
- Při `ShowMatches = false` se sekce zápasů nevykreslí (žádná prázdná
  `print-page`).

Příkazy:

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --filter "FullyQualifiedName~TrainingRequirement"
Set-Location src\SportSys.Razor; npm run build:css
dotnet build SportSys.slnx
```

Po buildu CSS zkontrolovat, že `site.css` obsahuje `@media print`,
`@page requirement-print` a tmavé tokeny pouze v `@media screen`.

## Manuální akceptace

1. Na `/sport/Training/Requirement` se zobrazí tlačítko „Tisk“; kliknutí
   otevře tiskový dialog.
2. Náhled je na šířku; neobsahuje header, menu, nadpis stránky, filtr,
   patičku ani sloupec Akce.
3. Strana 1 = Požadavky na tréninky, strana 2 = Požadavky na zápasy,
   strana 3 = Porovnání.
4. Dlouhá tabulka pokračuje na další stránce s opakovanou hlavičkou, řádky
   nejsou rozdělené.
5. Při odškrtnutých Zápasech se tisknou jen tréninky a porovnání (2 strany).
6. V tmavém režimu je tisk světlý a čitelný; ikony stavů porovnání mají
   barvu a text.
7. Porovnání se netiskne oříznuté (bez horizontálního scrollu).
8. Tisk ostatních stránek (např. `/sport/Training/Plan`) zůstává na výšku
   a funkční; obrazovkové zobrazení Požadavků i Plánu je beze změny.

## Beze změny

- Contract služby, DTO, PageModel `Requirement/Index.cshtml.cs`, databázový
  model — **žádná změna DB modelu ani migrace**.
- Výpočet a vzhled komponenty `TrainingRequirementCoverage` na obrazovce.
- Hodnoty barevných tokenů.

## Mimo rozsah

- Tisk stránky `/sport/Training/Plan` (rozvrh, tabulka plánů).
- Export do PDF/Excelu, tisková hlavička s parametry filtru, automatické
  zmenšení tabulky na jednu stránku.

## Hotovo, když

- [ ] Stránka Požadavky má tlačítko „Tisk“ bez inline JS.
- [ ] Tisk je na šířku a obsahuje pouze tři tabulkové sekce, každou od nové stránky.
- [ ] Ovládací prvky, navigace a sloupec Akce se netisknou.
- [ ] Tisk je vždy ve světlém režimu.
- [ ] Ostatní stránky a obrazovkové zobrazení nejsou ovlivněny.
- [ ] Testy `TrainingRequirementTests` procházejí, `npm run build:css` a build řešení jsou úspěšné.
- [ ] Dokumentace `sport.md` a `frontend.md` je aktualizována.
