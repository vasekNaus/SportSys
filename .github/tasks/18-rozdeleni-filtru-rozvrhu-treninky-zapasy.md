# Implementační plán: Rozdělení filtrů rozvrhu na společné a specifické (trénink/zápas)

**Stav:** Připraveno k implementaci.

## Cíl

Na `/sport/Training/Schedule` zavést přepínač „Zobrazit tréninky“ /
„Zobrazit zápasy“ (oba výchozí zapnuté) a podle něj podmíněně zobrazit jen
relevantní specifické filtry. Sjednotit filtr lokality, aby platil pro
tréninky i zápasy, a doplnit chybějící filtr „Typ zápasu“, aby měly obě
kategorie položek srovnatelnou sadu filtrů. Navazuje na dřívější analýzu
nekonzistence filtrů a na již implementovaný `sport.MatchState`
(`.github/tasks/17-stav-zapasu-matchstate.md`).

## Výchozí stav

- `IndexModel` (`Areas/sport/Pages/Training/Schedule/Index.cshtml.cs`) načítá
  a normalizuje `TrainingTypes`, `TrainingStates`, `Locations`, `MatchStates`;
  `MatchScheduleService.GetMatchesAsync` už filtruje podle `matchStateIds`
  (dokončeno v #17), ale **ne** podle lokality ani typu zápasu.
- `TrainingScheduleService.GetTrainingLocationsAsync` dotazuje `_db
  .SportLocations` bez vazby na konkrétní typ události — jde o obecný
  číselník sportovních lokalit, nikoli o projekci závislou na tréninku.
  Stávající metodu lze beze změny použít i pro zápasy.
- `Match.LocationId` je zděděné z `SportEvent` stejně jako u `Training` —
  sjednocení filtru lokality nevyžaduje žádnou databázovou změnu.
- `Match.MatchTypeId` → `sport.MatchType` existuje v modelu od začátku, ale
  nemá Contract metodu pro číselník (`GetMatchTypesAsync`) ani UI filtr;
  `MatchScheduleItemDto.MatchTypeName` se dnes jen zobrazuje, nefiltruje.
- `Index.cshtml` má v horním společném řádku filtrů „Sezóna“, „Typ tréninku“,
  „Lokalita“, „Datum od/do“ — „Typ tréninku“ je tedy dnes chybně umístěn mezi
  společné filtry, ačkoli platí jen pro tréninky.
- Fieldsety „Stavy tréninku“ a „Stavy zápasu“ se dnes zobrazují vždy
  (podmínka je jen na `Count > 0` číselníku, ne na relevanci k vybranému
  druhu položek).
- `OnGetAsync`/`OnGetExportAsync` vždy volají `LoadTrainingsAsync` i
  `_matchService.GetMatchesAsync`, bez ohledu na to, zda uživatel o daný druh
  položek stojí.

## Potvrzená rozhodnutí

- **Přepínač**: dva nezávislé checkboxy `ShowTrainings` / `ShowMatches`
  (výchozí `true`/`true`), ne radio group. Uživatel tak může zobrazit obojí,
  jen tréninky, jen zápasy, nebo (validní, i když neužitečný stav) nic.
- **Lokalita**: sjednotit ihned — filtr „Lokalita“ se přesune do společné
  části a bude platit pro tréninky i zápasy zároveň (`LocationId` je sdílené
  pole `SportEvent`).
- **Typ zápasu**: přidat nový filtr, analogický „Typ tréninku“, navázaný na
  `sport.MatchType`. Zobrazí se jen v sekci specifické pro zápasy.
- Rozsah je čistě Contract + Razor; **žádná databázová změna** není potřeba
  (na rozdíl od #17 zde nevzniká nový sloupec ani číselník).
- Task se implementuje rovnou po tomto schváleném plánu (bez dalšího
  mezikroku).

## Technický návrh

### Contract vrstva

1. **`MatchScheduleService.GetMatchTypesAsync(CancellationToken ct = default)`**
   — nová metoda, analogická `TrainingScheduleService.GetTrainingTypesAsync`:
   `_db.MatchTypes.OrderBy(t => t.Id).Select(t => new LookupSelectItem
   { Id = t.Id, Name = t.Name })`.
2. **`MatchScheduleService.GetMatchesAsync`** — rozšířit signaturu o
   `IReadOnlyCollection<int>? locationIds = null` a
   `IReadOnlyCollection<int>? matchTypeIds = null` (za `matchStateIds`,
   všechny nové parametry s výchozí `null`/prázdný filtr). V query přidat:
   ```
   if (locationIds is { Count: > 0 })
       query = query.Where(m => locationIds.Contains(m.LocationId));
   if (matchTypeIds is { Count: > 0 })
       query = query.Where(m => matchTypeIds.Contains(m.MatchTypeId));
   ```
3. Beze změny zůstává `TrainingScheduleService.GetTrainingLocationsAsync` —
   znovupoužije se pro sdílený filtr lokality (dotazuje celý číselník
   `sport.Location`, není vázaná na `Training`).

### Razor — `IndexModel`

1. Nové vlastnosti:
   - `[BindProperty(SupportsGet = true)] public bool ShowTrainings { get; set; } = true;`
   - `[BindProperty(SupportsGet = true)] public bool ShowMatches { get; set; } = true;`
   - `public List<LookupSelectItem> MatchTypes { get; private set; } = [];`
   - `[BindProperty(SupportsGet = true)] public List<int> SelectedMatchTypeIds { get; set; } = [];`
2. `LoadAndNormalizeFiltersAsync`: načíst `MatchTypes` stejným vzorem jako
   `MatchStates`; normalizovat `SelectedMatchTypeIds` proti platným ID.
3. `OnGetAsync`/`OnGetExportAsync`:
   - `var trainings = ShowTrainings ? await LoadTrainingsAsync(filter, categories, ct) : [];`
   - `var matches = ShowMatches ? await _matchService.GetMatchesAsync(filter.SeasonId, categories, filter.DateFrom, filter.DateTo, SelectedMatchStateIds, SelectedLocationIds, SelectedMatchTypeIds, ct) : [];`
   - `HasExportableTrainings = trainings.Count > 0;` zůstává beze změny —
     export je i nadále výhradně o trénincích, takže při `ShowTrainings =
     false` je `trainings` prázdné a export automaticky zmizí (žádná další
     podmínka není potřeba).
4. `LoadTrainingsAsync` zůstává beze změny (lokalita se do
   `TrainingScheduleService.GetTrainingsAsync` už předává).

### Razor — `Index.cshtml`

1. **Společný řádek filtrů** (nahoře, vždy viditelný): Sezóna, Lokalita
   (přesunuto beze změny vzhledu, jen se změní jeho účinek), Datum od, Datum
   do. „Typ tréninku“ se z tohoto řádku **odstraní** (přesune se do sekce
   specifické pro tréninky).
2. **Nový fieldset „Zobrazit v rozvrhu“** hned pod společným řádkem:
   ```html
   <fieldset class="schedule-filter-categories">
       <legend>Zobrazit v rozvrhu</legend>
       <div class="schedule-filter-checkboxes">
           <label class="checkbox-label">
               <input asp-for="ShowTrainings" /> Tréninky
           </label>
           <label class="checkbox-label">
               <input asp-for="ShowMatches" /> Zápasy
           </label>
       </div>
   </fieldset>
   ```
   Bez `onchange="this.form.submit()"` — konzistentní s ostatními checkboxy
   ve fieldsetu Kategorie, potvrzuje se tlačítkem „Zobrazit rozvrh“.
3. **Fieldset Kategorie** zůstává společný (kategorie platí pro oba druhy
   položek). „Spojovat tréninky“ se zobrazí jen uvnitř
   `@if (Model.ShowTrainings)`, protože spojování je čistě tréninková
   funkce; „Zobrazovat prázdné řádky“ zůstává vždy viditelné.
4. **Sekce specifická pro tréninky** — nový `@if (Model.ShowTrainings)` blok
   obsahující (v tomto pořadí): multiselect „Typ tréninku“ (přesunutý sem) a
   existující fieldset „Stavy tréninku“.
5. **Sekce specifická pro zápasy** — nový `@if (Model.ShowMatches)` blok
   obsahující: nový multiselect „Typ zápasu“ (stejný vzor jako „Typ
   tréninku“, navázaný na `Model.MatchTypes` / `Model.SelectedMatchTypeIds`)
   a existující fieldset „Stavy zápasu“.
6. Beze změny zůstává renderování samotného rozvrhu (`ScheduleView`) — pokud
   jsou `ShowTrainings` i `ShowMatches` vypnuté, `trainings`/`matches` budou
   prázdné a stránka zobrazí existující hlášku „Pro zadané parametry nebyly
   nalezeny žádné tréninky ani zápasy.“ beze změny textu (drobná
   nepřesnost — hláška zůstává obecná i v tomto krajním stavu, úprava textu
   není součástí tohoto plánu).

## Implementační kroky

1. `MatchScheduleService`: `GetMatchTypesAsync` + rozšíření `GetMatchesAsync`
   o `locationIds`/`matchTypeIds` a jejich filtrování.
2. `IndexModel`: nové vlastnosti `ShowTrainings`, `ShowMatches`, `MatchTypes`,
   `SelectedMatchTypeIds`; načtení, normalizace, podmíněné volání služeb,
   předání všech filtrů do `GetMatchesAsync`.
3. `Index.cshtml`: přesun „Typ tréninku“ ze společného řádku, nový fieldset
   přepínače, podmíněné sekce pro tréninky a zápasy, nový multiselect „Typ
   zápasu“.
4. Aktualizovat `docs/modules/sport.md` (sekce „Filtry Schedule“) o nový
   přepínač, sjednocenou lokalitu a filtr typu zápasu.
5. Rozšířit `MatchScheduleServiceTests` o test filtrování podle lokality a
   podle typu zápasu (analogicky existujícím testům pro `matchStateIds`,
   pokud takové již existují — jinak přidat unit test nad in-memory/mock
   projekcí, konzistentní se stávajícím stylem testů v souboru).

## Soubory ke změně

| Oblast | Soubory |
|---|---|
| Contract | `src/SportSys.Contract/Services/MatchScheduleService.cs` |
| Razor | `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml.cs`, `Index.cshtml` |
| Dokumentace | `docs/modules/sport.md` |
| Testy | `tests/SportSys.Razor.Tests/MatchScheduleServiceTests.cs` |

## Testy a ověření

- `dotnet build SportSys.slnx`.
- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`.
- Manuální ověření na `/sport/Training/Schedule`:
  - vypnutí „Tréninky“ skryje filtr Typ/Stav tréninku i „Spojovat tréninky“
    a z rozvrhu zmizí tréninkové bloky;
  - vypnutí „Zápasy“ skryje filtr Typ/Stav zápasu a z rozvrhu zmizí zápasové
    bloky;
  - filtr „Lokalita“ nyní omezuje zobrazené zápasy stejně jako tréninky;
  - nový filtr „Typ zápasu“ omezuje zobrazené zápasy podle vybraných typů.

## Beze změny

- Export do Excelu zůstává výhradně exportem tréninků.
- Databázový model, migrace, číselníky `MatchType`/`MatchState` — beze změny
  struktury, jen nové čtecí dotazy.
- Spojování tréninků (`MergeTrainings`) — logika beze změny, jen viditelnost
  ovládacího prvku v UI.

## Mimo rozsah

- Textová úprava hlášky „nebyly nalezeny žádné tréninky ani zápasy“ pro
  stav, kdy jsou vypnuté oba přepínače.
- Perzistence uživatelské volby přepínače mezi návštěvami (např. cookie).
- Filtr Domácí/Venkovní nebo fulltext na soupeře — mimo rozsah tohoto plánu.

## Hotovo, když

- `/sport/Training/Schedule` nabízí přepínač „Tréninky“/„Zápasy“ a podle něj
  skrývá nerelevantní specifické filtry.
- Filtr „Lokalita“ omezuje zobrazené tréninky i zápasy.
- Nový filtr „Typ zápasu“ funguje analogicky k „Typ tréninku“.
- Build i testy prochází, dokumentace odpovídá výsledné implementaci.
