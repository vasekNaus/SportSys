# Implementační plán: #20 Návrh úpravy filtrů rozvrhu

**Issue:** [#20 — Návrh úpravy filtrů rozvrhu](https://github.com/vasekNaus/SportSys/issues/20)

**Stav:** Fáze 1 (stránka Schedule) implementována a ověřena. Fáze 2 (stránka
Plan — doplňující komentář k issue) připravena k implementaci.

## Cíl

Vizuálně reorganizovat filtrační formulář na stránce reálného rozvrhu
(`/sport/Training/Schedule`) do tří přehledných skupin bez změny filtrovací
logiky:

1. **Zobrazit v rozvrhu** — přepínač Tréninky / Zápasy, nahoře.
2. **Společné filtry** — Sezóna, Kategorie, Datum od, Datum do, Lokalita,
   napříč celou šířkou formuláře.
3. **Tréninky** a **Zápasy** — dva panely vedle sebe (na úzkých obrazovkách
   pod sebou), zobrazené jen podle stavu odpovídajícího přepínače z bodu 1,
   s jemným barevným akcentem odlišujícím typ (bez plného podbarvení pozadí).

Akce **Vymazat filtry** / **Použít filtry** zůstávají pod všemi panely.

Jde výhradně o reorganizaci šablony a stylů — žádná filtrovací vlastnost,
DTO, Contract služba ani databázové schéma se nemění.

**Doplňující komentář k issue** (vasekNaus, 2.10.2026) rozšiřuje zadání i na
stránku tréninkových plánů:

> Aplikuj stejnou úpravu na stránku s plánem tréninků. Zde je filtr pouze
> přes „trénink“, tzn. bude tam vše v jednom filtru. Přidej tam ikonku,
> barevný pružek a uspořádání komponent stejně jako na stránce s rozvrhem.

Na stránce `/sport/Training/Plan` (`Training/Plan/Index.cshtml`) neexistuje
žádné dělení na „společné“ a „specifické“ filtry — veškeré filtry se týkají
výhradně tréninků. Cílem fáze 2 je proto **jediný panel** se stejnou vizuální
úpravou, jaká na stránce Schedule dostal panel „Tréninky“ (ikona, levý
barevný pružek, titulek panelu), obsahující všechny stávající filtry této
stránky beze změny jejich chování.

## Výchozí stav

- Stránka `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml`
  má jeden plochý `<form method="get" class="schedule-filter">` s těmito
  bloky v tomto pořadí:
  1. `.schedule-filter-row` — Sezóna (`<select>`), Lokalita
     (`.schedule-multiselect` — starší checkbox/`<details>` widget, `[data-multiselect]`
     v `site.js`, **ne** nová Tom Select komponenta z #19), Datum od, Datum do.
  2. `fieldset.schedule-filter-categories` s legendou „Zobrazit v rozvrhu“ —
     `ShowTrainings` / `ShowMatches` checkboxy.
  3. `fieldset.schedule-filter-categories` s legendou „Kategorie“ (jen pokud
     `SeasonCategories.Count > 0`) — obsahuje i `.schedule-filter-options` s
     `ShowEmptyRows` („Zobrazovat prázdné řádky“) a podmíněně (`ShowTrainings`)
     `MergeTrainings` („Spojovat tréninky“, auto-submit `onchange`).
  4. Podmíněně (`ShowTrainings`): `.schedule-filter-row` s „Typ tréninku“
     (`.schedule-multiselect`) + `fieldset` „Stavy tréninku“ (jen pokud
     `TrainingStates.Count > 0`).
  5. Podmíněně (`ShowMatches`): `.schedule-filter-row` s „Typ zápasu“
     (`.schedule-multiselect`) + `fieldset` „Stavy zápasu“ (jen pokud
     `MatchStates.Count > 0`).
  6. `.schedule-filter-actions` (jen pokud `SeasonId.HasValue &&
     SeasonCategories.Count > 0`) — `<button type="submit">Zobrazit rozvrh</button>`
     a podmíněně (`HasExportableTrainings`) `<button type="submit" name="handler"
     value="Export">Export do Excelu</button>`.
  - Žádné tlačítko/odkaz pro vymazání filtrů aktuálně neexistuje — ani na této,
    ani na žádné jiné stránce v repozitáři (ověřeno, viz
    `.schedule-multiselect-clear`, které vrací jen jednotlivý multiselect, ne
    celý formulář).
- `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml.cs`
  (`IndexModel`) definuje `[BindProperty(SupportsGet = true)]` vlastnosti
  `SeasonId`, `SelectedCategories`, `DateFrom`, `DateTo`,
  `SelectedLocationIds`, `ShowTrainings` (default `true`), `ShowMatches`
  (default `true`), `ShowEmptyRows` (default `true`), `MergeTrainings`,
  `SelectedTrainingTypeIds`, `SelectedTrainingStateIds`,
  `SelectedMatchTypeIds`, `SelectedMatchStateIds`. Všechny zůstávají beze
  změny — plán se týká pouze `.cshtml` šablony a SCSS.
- Vizuální styl filtru je v `src/SportSys.Razor/Styles/_schedule.scss`
  (třídy `.schedule-filter*`, `.schedule-multiselect*`, `.checkbox-label`).
  Žádný existující panel/karta vzor pro „skupinu filtrů s barevným akcentem“
  v této třídě zatím neexistuje; nejbližší obdobný vzor je
  `.training-edit-summary > div` (karta s rámečkem, `border-radius: 4px`,
  `background-color: var(--color-surface-1)`) na stránce editace tréninku.
- Barvy jsou vrstvené dle `.github/skills/barevna-schemata/` — sémantické
  tokeny v `_vars.scss` zahrnují mj. `--color-brand-primary` (červená),
  `--color-brand-secondary` (námořní modrá), `--color-border-default`,
  `--color-border-subtle`. V aplikaci neexistuje žádné zavedené barevné
  rozlišení „trénink vs. zápas“ — bloky v samotném rozvrhu (`ScheduleEventModelFactory`)
  se barví podle `SeasonCategoryName` (`ColorKey`), ne podle typu události.
  Nový barevný akcent pro panely tedy musí vycházet z existujících
  sémantických tokenů (pravidlo skillu: nikdy nevytvářet nové schéma bez
  výzkumu) — viz rozhodnutí níže.
- Breakpoint pro přechod na mobilní/úzké rozložení je `$StopMin: 900px`
  (`_vars.scss`), používaný napříč `_layout.scss`, `_forms.scss`, `_hr.scss`
  atd. jako `@media (max-width: $StopMin)`.
- Žádný test v `tests/SportSys.Razor.Tests` nepokrývá markup `Index.cshtml`
  (testy pokrývají `TrainingScheduleService`, `MatchScheduleService`,
  `ScheduleEventModelFactory`, `TrainingScheduleBlockFactory`, apod. — čistě
  PageModel/Contract vrstvu). Reorganizace šablony tedy nevyžaduje úpravu
  žádného existujícího testu.
- **Fáze 1 je implementována** — `Index.cshtml` stránky Schedule obsahuje
  panely `.schedule-filter-panel--common/--training/--match`,
  `.schedule-filter-specific` a akci „Vymazat filtry“ přesně dle této
  specifikace; `_schedule.scss` obsahuje všechny potřebné třídy včetně
  `.schedule-filter-categories--nested`. Fáze 2 (níže) tyto již existující
  třídy znovu použije beze změny.

### Výchozí stav stránky Plan (doplnění pro fázi 2)

- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml` má jeden
  plochý `<form method="get" class="schedule-filter">`:
  1. `.schedule-filter-row` — Sezóna (`<select>`, auto-submit), Typ tréninku
     (`.schedule-multiselect`), Lokalita (`.schedule-multiselect`, zde
     `List<string>` přes `SelectedLocations`/`Locations`, ne `int` ID jako na
     Schedule), Fáze tréninku (`<select>`, bez auto-submitu), Datum platnosti
     (`<input type="date">`, auto-submit).
  2. `fieldset.schedule-filter-categories` s legendou „Kategorie“ (jen pokud
     `SeasonCategories.Count > 0`) — obsahuje `.schedule-filter-options` s
     `ShowEmptyRows` („Zobrazovat prázdné řádky“) a `MergeTrainings`
     („Spojovat tréninky“, auto-submit), bez podmínky na jiný přepínač (na
     této stránce je vždy jen trénink).
  3. `.schedule-filter-actions` (jen pokud `SeasonId.HasValue &&
     SeasonCategories.Count > 0`) — jediné tlačítko
     `<button type="submit">Zobrazit plán</button>` (ikona
     `fa-solid fa-calendar-week`). Žádný export, žádné „Vymazat filtry“.
  - Na stránce **neexistuje** přepínač obdoby „Zobrazit v rozvrhu“ ani dělení
    na „společné“/„specifické“ filtry — veškeré filtry patří vždy pod
    trénink, protože stránka nezobrazuje zápasy.
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs`
  (`IndexModel`) definuje `[BindProperty(SupportsGet = true)]` vlastnosti
  `SeasonId`, `SelectedCategories`, `SelectedTrainingTypeIds`,
  `SelectedLocations`, `TrainingPhaseId`, `ValidOn`, `ShowEmptyRows` (default
  `true`), `MergeTrainings`. Všechny zůstávají beze změny — fáze 2 se týká
  pouze `.cshtml` šablony, žádná SCSS třída se nepřidává (znovupoužijí se
  třídy zavedené ve fázi 1).

## Potvrzené požadavky a rozhodnutí

Issue #20 definuje strukturu takto (citace ze zadání):

```
Zobrazit v rozvrhu: Tréninky, Zápasy
Společné filtry: Sezóna, Kategorie, Datum od, Datum do, Lokalita
Tréninky: Typ tréninku, Stav tréninku, Spojovat tréninky
Zápasy: Typ zápasu, Stav zápasu
Akce: Vymazat filtry, Použít filtry
```

Doplňující technická rozhodnutí (odvozená z existujícího kódu a konvencí,
nejde o změnu chování vyžadující potvrzení uživatelem):

1. **„Zobrazovat prázdné řádky“ (`ShowEmptyRows`) není v hierarchii issue
   zmíněno.** Protože se týká zobrazení řádků bez ohledu na trénink/zápas
   (obdobně jako rozsah data), zařazuje se do panelu **Společné filtry** jako
   poslední volba pod výběrem kategorií.
2. **„Spojovat tréninky“ (`MergeTrainings`)** se přesouvá z fieldsetu
   „Kategorie“ do panelu **Tréninky** (přesně dle hierarchie issue), beze
   změny chování (`onchange="this.form.submit()"` zůstává).
3. **Barevný akcent panelů** — bez zavádění nového schématu/tokenů: panel
   „Tréninky“ použije `--color-brand-primary` (levý okraj + ikona nadpisu),
   panel „Zápasy“ použije `--color-brand-secondary`, panel „Společné filtry“
   zůstává neutrální (`--color-border-default`, žádný barevný akcent). Akcent
   je jen tenký levý/horní okraj + barva ikony v nadpisu panelu — bez
   plného podbarvení pozadí, přesně dle požadavku issue.
4. **Ikony nadpisů panelů** (Font Awesome 6/7 Free, již používané v
   aplikaci): Tréninky `fa-solid fa-person-skating`, Zápasy `fa-solid
   fa-hockey-puck`, Společné filtry bez ikony (nebo `fa-solid fa-filter`,
   volitelné). Čistě kosmetická volba, snadno změnitelná při review.
5. **Rozložení panelů Tréninky/Zápasy vedle sebe:** flexbox s `flex-wrap`,
   `flex: 1 1 320px` na každém panelu — pokud je viditelný jen jeden panel
   (`ShowTrainings`/`ShowMatches` vypnuté), přirozeně vyplní celou šířku
   (žádná speciální "solo" varianta/CSS třída není potřeba). Zalomení pod
   sebe pod `$StopMin` (900px) řeší `flex-wrap: wrap` automaticky bez
   explicitního media query (stejný vzor jako stávající `.schedule-filter-row`).
6. **„Vymazat filtry“** je nový prvek — v repozitáři neexistuje obdobný vzor
   na žádné jiné stránce. Implementuje se jako obyčejný odkaz (ne submit)
   `<a class="button secondary" asp-page="./Index">Vymazat filtry</a>` bez
   querystringu — běžná GET navigace na stránku bez parametrů vrátí všechny
   `[BindProperty(SupportsGet = true)]` na výchozí hodnoty z C# (viz Výchozí
   stav). Odkaz je zobrazen vždy (není podmíněn vybranou sezónou), na rozdíl
   od tlačítek Použít/Export, u kterých zůstává stávající podmínka
   `SeasonId.HasValue && SeasonCategories.Count > 0` (beze změny — nejde o
   součást zadání issue).
7. **„Použít filtry“** nahrazuje popisek stávajícího `<button type="submit">
   Zobrazit rozvrh</button>` (stejné chování, jen text a ikona — `fa-solid
   fa-filter` místo `fa-calendar-days`, aby odpovídalo roli „aplikovat
   filtr“ spíš než „zobrazit rozvrh“). Tlačítko „Export do Excelu“ zůstává
   beze změny vedle „Použít filtry“ (sekundární), issue jej explicitně
   nezmiňuje, ale musí zůstat dostupné v rámci stejné akční lišty.
8. Nested fieldsety „Kategorie“, „Stavy tréninku“, „Stavy zápasu“ zůstávají
   jako `<fieldset>`/`<legend>` (zachování sémantiky pro screen readery), ale
   uvnitř nového panelu dostanou modifikátor bez vlastního rámečku/paddingu
   (`.schedule-filter-categories--nested`), aby nevznikl efekt „rámeček v
   rámečku“ (stejný typ problému, jaký byl opraven u Tom Select input
   borderu v #19).

### Fáze 2 — doplňující komentář (stránka Plan)

9. **Jediný panel místo tří.** Protože stránka Plan zobrazuje výhradně
   tréninky, celý filtrační obsah (Sezóna, Typ tréninku, Lokalita, Fáze
   tréninku, Datum platnosti, Kategorie, Zobrazovat prázdné řádky, Spojovat
   tréninky) se obalí do **jednoho** `section.schedule-filter-panel
   .schedule-filter-panel--training` — žádné rozdělení na „společné“ vs.
   „specifické“, protože zde takové dělení nedává smysl (vše je „trénink“).
   Beze „Zobrazit v rozvrhu“ přepínače — ten na stránce Plan nikdy
   neexistoval a komentář jeho přidání nežádá.
10. **Titulek panelu a ikona** — stejné jako panel „Tréninky“ na stránce
    Schedule, pro maximální vizuální konzistenci napříč oběma stránkami,
    přesně dle znění komentáře („uspořádání komponent stejně jako na stránce
    s rozvrhem“): titulek „Tréninky“, ikona `fa-solid fa-person-skating`.
11. **Barevný pružek** — stejný sémantický token jako na Schedule:
    `--color-brand-primary` jako levý okraj panelu (modifikátor
    `.schedule-filter-panel--training`, beze změny/rozšíření SCSS, třída již
    existuje z fáze 1).
12. **Žádný nový „solo“ layout** — jelikož existuje jen jeden panel, obaluje
    se přímo jako samostatný `section.schedule-filter-panel`, bez
    `.schedule-filter-specific` flex wrapperu (ten dává smysl jen když se mají
    zarovnat dva panely vedle sebe; zde by jen zbytečně omezil šířku přes
    `flex-basis`).
13. **Akce („Vymazat filtry“ / „Použít filtry“) nejsou součástí komentáře** —
    komentář žádá pouze vizuální sjednocení panelu (ikona, pružek,
    uspořádání), nikoliv změnu akčních tlačítek. Tlačítko „Zobrazit plán“
    zůstává beze změny textu i chování; „Vymazat filtry“ se na stránku Plan
    **nepřidává** v rámci tohoto plánu (viz „Mimo rozsah“ — lze řešit jako
    samostatný požadavek, pokud bude vznesen).
14. **Nested „Kategorie“ fieldset** uvnitř panelu dostane stejný modifikátor
    `.schedule-filter-categories--nested` jako na stránce Schedule, aby
    nevznikl „rámeček v rámečku“.

## Technický návrh

### Nová struktura `Index.cshtml` (pořadí shora dolů)

1. `fieldset.schedule-filter-categories` „Zobrazit v rozvrhu“ — **beze změny
   obsahu**, jen přesunuto na úplný začátek formuláře (před Společné filtry).
2. `section.schedule-filter-panel.schedule-filter-panel--common`
   - `h2.schedule-filter-panel-title` „Společné filtry“
   - `.schedule-filter-row` — Sezóna, Lokalita, Datum od, Datum do (beze
     změny obsahu bloků, jen přesunuto dovnitř panelu)
   - Kategorie: pokud `SeasonCategories.Count > 0`, `fieldset
     .schedule-filter-categories.schedule-filter-categories--nested`
     „Kategorie“ + `ShowEmptyRows` checkbox pod ním; jinak zachovaná hláška
     „Pro zvolenou sezónu nejsou definovány žádné aktivní kategorie.“
3. Pokud `ShowTrainings || ShowMatches`:
   `div.schedule-filter-specific` (flex kontejner)
   - Pokud `ShowTrainings`: `section.schedule-filter-panel
     .schedule-filter-panel--training`
     - `h2.schedule-filter-panel-title` s ikonou „Tréninky“
     - `.schedule-filter-row` — Typ tréninku (`.schedule-multiselect`, beze
       změny)
     - Pokud `TrainingStates.Count > 0`: `fieldset
       .schedule-filter-categories.schedule-filter-categories--nested`
       „Stavy tréninku“ (beze změny obsahu)
     - `MergeTrainings` checkbox (přesunuto sem z Kategorie, beze změny
       chování)
   - Pokud `ShowMatches`: `section.schedule-filter-panel
     .schedule-filter-panel--match`
     - `h2.schedule-filter-panel-title` s ikonou „Zápasy“
     - `.schedule-filter-row` — Typ zápasu (beze změny)
     - Pokud `MatchStates.Count > 0`: `fieldset
       .schedule-filter-categories.schedule-filter-categories--nested`
       „Stavy zápasu“ (beze změny obsahu)
4. `.schedule-filter-actions` — rozšířeno o `Vymazat filtry` (vlevo,
   vždy viditelné); `Použít filtry` + `Export do Excelu` (vpravo, zachovaná
   podmínka `SeasonId.HasValue && SeasonCategories.Count > 0` pro tuto
   dvojici).

Všechny `name`/`asp-for`/`checked`/`value` atributy jednotlivých vstupů a
veškerá podmíněná viditelnost (`@if`) zůstávají **přesně** zachované — mění
se pouze obalové elementy a jejich pořadí/vnoření, nikdy vlastní `<input>`/
`<select>` prvky ani jejich `name` atributy (nutné pro zachování model
bindingu).

### Nová struktura `Training/Plan/Index.cshtml` (fáze 2)

```
<form method="get" class="schedule-filter">
  <section class="schedule-filter-panel schedule-filter-panel--training">
    <h2 class="schedule-filter-panel-title">
      <i class="fa-solid fa-person-skating fa-fw"></i> Tréninky
    </h2>

    <div class="schedule-filter-row">
      ... Sezóna, Typ tréninku, Lokalita, Fáze tréninku, Datum platnosti
          (beze změny obsahu, jen přesunuto dovnitř panelu) ...
    </div>

    @if (Model.SeasonCategories.Count > 0)
    {
      <fieldset class="schedule-filter-categories schedule-filter-categories--nested">
        <legend>Kategorie</legend>
        ... beze změny obsahu ...
      </fieldset>
    }
    else if (Model.SeasonId.HasValue)
    {
      <p class="text-muted">Pro zvolenou sezónu nejsou definovány žádné aktivní kategorie.</p>
    }
  </section>

  @if (Model.SeasonId.HasValue && Model.SeasonCategories.Count > 0)
  {
    <div class="schedule-filter-actions">
      <button type="submit" class="button">
        <i class="fa-solid fa-calendar-week fa-fw"></i> Zobrazit plán
      </button>
    </div>
  }
</form>
```

Beze změny zůstávají: pořadí a obsah polí uvnitř `.schedule-filter-row`,
obsah `fieldset` Kategorie (checkboxy + `ShowEmptyRows` + `MergeTrainings`),
podmínky `@if`, text a chování tlačítka „Zobrazit plán“. Mění se pouze
obalové elementy — přidává se jeden `<section class="schedule-filter-panel
schedule-filter-panel--training">` okolo celého obsahu formuláře nad akční
lištou, a `fieldset` Kategorie dostává modifikátor `--nested`. Žádná nová
SCSS třída není potřeba — `.schedule-filter-panel`,
`.schedule-filter-panel--training`, `.schedule-filter-panel-title` a
`.schedule-filter-categories--nested` již existují z fáze 1.

### Nové/upravené SCSS třídy (`_schedule.scss`, fáze 1)

```scss
.schedule-filter-panel {
  border:        1px solid var(--color-border-default);
  border-left:   4px solid var(--color-border-default);
  border-radius: 4px;
  padding:       1rem;
  margin-bottom: 1rem;
  background-color: var(--color-surface-1);
}

.schedule-filter-panel--training {
  border-left-color: var(--color-brand-primary);

  .schedule-filter-panel-title i {
    color: var(--color-brand-primary);
  }
}

.schedule-filter-panel--match {
  border-left-color: var(--color-brand-secondary);

  .schedule-filter-panel-title i {
    color: var(--color-brand-secondary);
  }
}

.schedule-filter-panel-title {
  margin:      0 0 .75rem;
  font-size:   1rem;
  font-weight: 700;
  color:       var(--color-text-primary);

  i { margin-right: .5rem; }
}

.schedule-filter-specific {
  display:   flex;
  flex-wrap: wrap;
  gap:       1rem;

  > .schedule-filter-panel {
    flex:      1 1 320px;
    min-width: 280px;
    margin-bottom: 0;
  }
}

.schedule-filter-categories--nested {
  border:        none;
  padding:       0;
  margin-bottom: 0;
}

.schedule-filter-actions {
  display:         flex;
  flex-wrap:       wrap;
  justify-content: space-between;
  gap:             .75rem;
}

.schedule-filter-actions-primary {
  display: flex;
  gap:     .75rem;
}
```

(Přesné hodnoty mezer/paddingu lze doladit v review proti vzoru
`.training-edit-summary > div`; princip — rámeček + levý barevný pruh +
žádné plné podbarvení — je závazný.)

### Dark mode

Žádná nová barva se nedefinuje nad rámec stávajících `--color-brand-primary`
a `--color-brand-secondary`, které už mají definované dark-mode varianty v
`_vars.scss` — akcent tedy funguje v obou režimech bez dalších úprav.

## Implementační kroky

### Fáze 1 — Schedule (implementováno)

1. ~~Upravit `Index.cshtml`: přesunout fieldset „Zobrazit v rozvrhu“ na
   začátek formuláře; obalit Sezóna/Lokalita/Datum/Kategorie/ShowEmptyRows do
   panelu „Společné filtry“; obalit podmíněné bloky Tréninky/Zápasy do
   `.schedule-filter-specific` se dvěma panely; přesunout `MergeTrainings`
   do panelu Tréninky; přidat nested modifikátor fieldsetům uvnitř panelů;
   přidat odkaz „Vymazat filtry“ a přejmenovat/upravit ikonu tlačítka
   „Použít filtry“.~~ (hotovo)
2. ~~Doplnit SCSS třídy dle návrhu výše do `_schedule.scss`.~~ (hotovo)
3. ~~Spustit `npm run build:css` ve `src/SportSys.Razor` a ověřit bezchybnou
   kompilaci.~~ (hotovo)
4. ~~Spustit `dotnet build SportSys.slnx -c Release` a ověřit 0 chyb.~~ (hotovo)
5. ~~Manuálně ověřit v prohlížeči (light i dark mode, širokou i úzkou
   obrazovku pod/nad 900px) dle akceptačních kritérií níže.~~ (hotovo)
6. ~~Aktualizovat sekci „### Filtry Schedule“ v `docs/modules/sport.md`.~~ (hotovo)

### Fáze 2 — Plan (doplňující komentář, k implementaci)

1. Upravit `Training/Plan/Index.cshtml`: obalit celý obsah formuláře (řádek
   Sezóna/Typ tréninku/Lokalita/Fáze tréninku/Datum platnosti + fieldset
   Kategorie) do jednoho `section.schedule-filter-panel
   .schedule-filter-panel--training` s titulkem „Tréninky“ a ikonou
   `fa-solid fa-person-skating`; přidat fieldsetu Kategorie modifikátor
   `.schedule-filter-categories--nested`. Akční lišta (`Zobrazit plán`)
   zůstává mimo panel, beze změny.
2. Žádná nová SCSS třída — ověřit vizuálně, že existující třídy
   `.schedule-filter-panel--training` fungují správně i na samostatném
   panelu bez `.schedule-filter-specific` wrapperu (měly by, protože panel
   nemá vlastní `flex`/`flex-basis` pravidla, jen rámeček a padding).
3. Spustit `npm run build:css` ve `src/SportSys.Razor` (ověření, že žádná
   SCSS změna neproběhla/nerozbila kompilaci).
4. Spustit `dotnet build SportSys.slnx -c Release` a ověřit 0 chyb.
5. Manuálně ověřit v prohlížeči (light i dark mode) dle akceptačních
   kritérií níže.
6. Aktualizovat sekci „### Filtry Plan“ v `docs/modules/sport.md` — doplnit
   větu o vizuálním sjednocení s panelem „Tréninky“ ze stránky Schedule;
   zachovat beze změny popis filtrovací logiky.

## Soubory ke změně

- `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml` —
  reorganizace markupu do panelů, přidání akce „Vymazat filtry“. (fáze 1,
  hotovo)
- `src/SportSys.Razor/Styles/_schedule.scss` — nové třídy `.schedule-filter-panel*`,
  `.schedule-filter-specific`, `.schedule-filter-categories--nested`,
  rozšíření `.schedule-filter-actions`. (fáze 1, hotovo)
- `docs/modules/sport.md` — doplnění popisu vizuálního seskupení filtrů v
  sekci „### Filtry Schedule“. (fáze 1, hotovo)
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml` —
  obalení celého filtru do jednoho panelu `schedule-filter-panel--training`
  s titulkem a ikonou. (fáze 2, k implementaci)
- `docs/modules/sport.md` — doplnění popisu panelu v sekci „### Filtry Plan“.
  (fáze 2, k implementaci)

## Testy a ověření

- `dotnet build SportSys.slnx -c Release` — musí proběhnout bez chyb.
- `npm run build:css` (ve `src/SportSys.Razor`) — musí proběhnout bez chyb.
- Žádné jednotkové testy se nemění ani nepřidávají — reorganizace nemění
  žádnou vlastnost `IndexModel`, DTO ani Contract službu, takže stávající
  testy (`MatchScheduleServiceTests`, `ScheduleEventModelFactoryTests`,
  `TrainingScheduleBlockFactoryTests`, `TrainingScheduleVisualizationGroupingTests`,
  `TrainingScheduleComponentModelTests`, `TrainingScheduleExcelExporterTests`,
  `TrainingPlanValidityFilterTests`) zůstávají beze změny a musí procházet.

## Manuální akceptace

1. Otevřít `/sport/Training/Schedule` — nahoře je přepínač „Zobrazit v
   rozvrhu“ (Tréninky/Zápasy), pod ním panel „Společné filtry“ (Sezóna,
   Kategorie, Datum od/do, Lokalita, Zobrazovat prázdné řádky).
2. Při zapnutých obou přepínačích se panely „Tréninky“ a „Zápasy“ zobrazují
   vedle sebe na široké obrazovce (≥ 900 px) a pod sebou na úzké (< 900 px).
3. Panel „Tréninky“ obsahuje Typ tréninku, Stavy tréninku, „Spojovat
   tréninky“; panel „Zápasy“ obsahuje Typ zápasu, Stavy zápasu.
4. Při vypnutí jednoho z přepínačů zmizí odpovídající panel a druhý
   (zůstávající) panel vyplní celou dostupnou šířku.
5. Panely „Tréninky“/„Zápasy“ mají jemný barevný akcent (levý okraj + barva
   ikony v nadpisu) — bez plného podbarvení pozadí; panel „Společné filtry“
   zůstává neutrální.
6. Dole je akční lišta: vlevo „Vymazat filtry“ (vždy viditelné), vpravo
   „Použít filtry“ a případně „Export do Excelu“ (za stávajících podmínek).
7. Kliknutí na „Vymazat filtry“ naviguje na stránku bez querystringu a
   resetuje všechny filtry na výchozí hodnoty (`ShowTrainings`/`ShowMatches`/
   `ShowEmptyRows` zapnuté, žádná vybraná sezóna/kategorie/typy/stavy).
8. Veškerá stávající filtrovací funkčnost (auto-submit Sezóny, Spojovat
   tréninky, multiselect widgety, export do Excelu, zobrazení rozvrhu)
   funguje beze změny oproti současnému stavu.
9. Ověřit v dark módu (`data-theme="dark"`) — akcentové barvy zůstávají
   čitelné a konzistentní se zbytkem aplikace.
10. Otevřít `/sport/Training/Plan` — celý filtr je obalen jedním panelem s
    titulkem „Tréninky“, ikonou `fa-person-skating` a levým červeným
    pružkem (`--color-brand-primary`), vizuálně shodným s panelem
    „Tréninky“ na stránce Schedule.
11. Panel na stránce Plan obsahuje beze změny pořadí a chování: Sezóna, Typ
    tréninku, Lokalita, Fáze tréninku, Datum platnosti, Kategorie,
    Zobrazovat prázdné řádky, Spojovat tréninky.
12. Tlačítko „Zobrazit plán“ zůstává beze změny textu, ikony i umístění
    (mimo panel, v `.schedule-filter-actions`); na stránce Plan nepřibylo
    tlačítko/odkaz „Vymazat filtry“.
13. Ověřit stránku Plan v dark módu — barevný pružek zůstává čitelný a
    konzistentní se stránkou Schedule.

## Beze změny

- `Index.cshtml.cs` (`IndexModel`) na obou stránkách (Schedule i Plan) —
  žádná vlastnost, handler ani validace se nemění.
- `TrainingScheduleService`, `MatchScheduleService`,
  `TrainingScheduleExcelExporter`, `ScheduleEventModelFactory` a veškerá
  Contract/Database vrstva.
- Starší `.schedule-multiselect` widget (checkbox/`<details>`) pro Lokalitu,
  Typ tréninku, Typ zápasu — issue #20 řeší jen rozložení, ne výměnu
  komponenty za Tom Select (#19); ta zůstává mimo rozsah na obou stránkách.
- `Training/Requirement/Index.cshtml` — ani issue, ani doplňující komentář
  tuto stránku nezmiňují.
- Text, ikona a chování tlačítka „Zobrazit plán“ na stránce Plan — komentář
  žádá jen vizuální sjednocení panelu, ne změnu akcí.
- Žádná EF Core migrace ani model snapshot.

## Mimo rozsah

- Záměna staršího `.schedule-multiselect` widgetu za novou Tom Select
  komponentu (`<search-multiselect>`) — samostatný úkol, nebyl součástí
  zadání issue #20 ani doplňujícího komentáře.
- Přidání nových filtrovacích kritérií nebo změna výchozích hodnot filtrů na
  kterékoli ze stránek.
- Perzistentní ukládání naposledy použitých filtrů (např. do cookie/session).
- Úprava `Training/Requirement/Index.cshtml` — doplňující komentář zmiňuje
  výhradně stránku s plánem tréninků, ne požadavky na tréninky.
- Přidání akce „Vymazat filtry“ nebo přejmenování tlačítka „Zobrazit plán“
  na stránce Plan — doplňující komentář tuto změnu nežádá; lze řešit jako
  samostatný následný požadavek.

## Hotovo, když

### Fáze 1 — Schedule

- [x] `Index.cshtml` (Schedule) je reorganizováno do tří vizuálních skupin
      dle návrhu.
- [x] Panel „Tréninky“ a panel „Zápasy“ mají jemný barevný akcent
      (`--color-brand-primary` / `--color-brand-secondary`) bez plného
      podbarvení pozadí.
- [x] Panely jsou vedle sebe na široké obrazovce a pod sebou na úzké
      (breakpoint `$StopMin` = 900 px); osamocený panel vyplní celou šířku.
- [x] Existuje funkční akce „Vymazat filtry“, která resetuje všechny filtry
      na výchozí hodnoty.
- [x] Tlačítko „Použít filtry“ nahrazuje „Zobrazit rozvrh“ se stejným
      chováním.
- [x] `dotnet build SportSys.slnx -c Release` a `npm run build:css` proběhnou
      bez chyb.
- [x] Stávající testy v `tests/SportSys.Razor.Tests` procházejí beze změny.
- [x] `docs/modules/sport.md` popisuje nové vizuální seskupení filtrů.
- [x] Žádná EF Core migrace nebyla vytvořena ani upravena.

### Fáze 2 — Plan

- [x] `Training/Plan/Index.cshtml` obaluje celý filtr jedním panelem
      `schedule-filter-panel--training` s titulkem „Tréninky“ a ikonou
      `fa-person-skating`.
- [x] Panel má levý barevný pružek `--color-brand-primary`, vizuálně shodný
      s panelem „Tréninky“ na stránce Schedule.
- [x] Veškerý obsah filtru (Sezóna, Typ tréninku, Lokalita, Fáze tréninku,
      Datum platnosti, Kategorie, Zobrazovat prázdné řádky, Spojovat
      tréninky) zůstává funkčně beze změny uvnitř panelu.
- [x] Tlačítko „Zobrazit plán“ zůstává beze změny textu/ikony/chování; na
      stránku nepřibyla akce „Vymazat filtry“.
- [x] `dotnet build SportSys.slnx -c Release` a `npm run build:css` proběhnou
      bez chyb.
- [x] Stávající testy v `tests/SportSys.Razor.Tests` procházejí beze změny.
- [x] `docs/modules/sport.md` popisuje vizuální sjednocení v sekci
      „### Filtry Plan“.
- [x] Žádná EF Core migrace nebyla vytvořena ani upravena.
