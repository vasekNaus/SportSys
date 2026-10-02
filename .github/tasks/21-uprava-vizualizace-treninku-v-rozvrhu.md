# Implementační plán: #21 Úprava vizualizace tréninků a tréninkových plánů v rozvrhu

**Issue:** [#21 — Úprava vizualizace tréninků a tréninkových plánů v rozvrhu](https://github.com/vasekNaus/SportSys/issues/21)

**Stav:** Připraveno k implementaci.

## Cíl

Sjednotit obsah a vizuální hierarchii bloku tréninku i tréninkového plánu
v komponentě rozvrhu na pevnou strukturu čtyř řádků:

1. kategorie (beze změny),
2. čas od–do — nově **tučně**, nejvýraznější informace v bloku,
3. trenéři — pouze **příjmení**, více trenérů odděleno čárkou,
4. lokalita — pouze název, bez typu tréninku jako textu.

Typ tréninku (led / suchá příprava) se nově nerozlišuje textem, ale vizuálně
způsobem vykreslení pozadí bloku: led = plná barva (beze změny), suchá
příprava = stejná barva s jemným diagonálním šrafováním přes CSS
(`repeating-linear-gradient`), bez externích obrázků a beze změny stávajícího
významu barev kategorií.

Issue nemá žádné doplňující komentáře (`gh issue view 21` vrátil prázdné pole
`comments`), zadání je tedy kompletně popsáno v těle issue.

## Výchozí stav

- Blok v rozvrhu i plánu vykresluje `ScheduleEventModel` (společný
  prezentační model pro Training/TrainingPlan/Match) ve
  `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`
  (řádky 60–115). Pro trénink/plán aktuálně platí:
  - `TitleLine` = kategorie (`schedule-block-cat`) — beze změny potřeba.
  - `DetailLine1` = `TrainingTypeLocationSummary` (např.
    `Led - Zimní stadion Klatovy`) — kombinuje typ **a** lokalitu do jednoho
    textu (`schedule-block-type`).
  - `DetailLine2` = `CoachSummary` — **celá jména** trenérů oddělená čárkou,
    nebo `-` (`schedule-block-coaches`).
  - Čas (`schedule-block-time`) se vykresluje zvlášť, sdílí netučný styl se
    `schedule-block-type`/`schedule-block-coaches`
    (`src/SportSys.Razor/Styles/_schedule.scss:538-547`).
- `ScheduleEventModel` i `TrainingScheduleBlockData`
  (`src/SportSys.Razor/Models/TrainingSchedule/`) neobsahují žádnou informaci
  o typu tréninku jinou než textový název (`TrainingTypeName`/
  `TrainingTypeNames`); nelze podle ní bezpečně rozlišit led/suchá příprava
  bez porovnávání lokalizovaného textu.
- Trenéři se v Contract vrstvě plní jako `c.Coach.DisplayName`
  (`src/SportSys.Contract/Services/TrainingScheduleService.cs`, metody
  `AssignCoachFullNamesAsync`). `User.DisplayName`
  (`src/SportSys.Database/Models/identity/User.cs`) je **jediný** textový
  sloupec se jménem — databáze nemá samostatné sloupce příjmení/jméno.
  `DisplayName` se plní buď z Entra ID klaimu `name`
  (`src/SportSys.Contract/Auth/EntraClaimsTransformation.cs:95-105`, typicky
  „Jméno Příjmení“), nebo ručně v administraci trenérů
  (`CoachService.cs`) jako libovolný text.
- Typ tréninku (`sport.TrainingType`) je číselník se dvěma hodnotami
  `ETrainingType.Dry = 1` („Suchá příprava“) a `ETrainingType.Ice = 2`
  („Led“) — `src/SportSys.Database/Enums/ETrainingType.cs`,
  `src/SportSys.Database/Resources/ETrainingType.cs.resx`. `Training` i
  `TrainingPlan` mají FK sloupec `TrainingTypeId`
  (`src/SportSys.Database/Models/sport/Training.cs:15`,
  `TrainingPlan.cs:25`) — sloupec již existuje, žádná migrace není potřeba.
- `ITrainingScheduleItem`/`TrainingScheduleItemDto`/
  `TrainingPlanScheduleItemDto`
  (`src/SportSys.Contract/Models/TrainingScheduleDto.cs`) nesou pouze
  `TrainingTypeName` (string), ne `TrainingTypeId` ani boolean příznak typu.
- Barva bloku (`block.Color`) se určuje podle kategorie
  (`CategoryColors` slovník, `--color-chart-1..8`,
  `TrainingScheduleViewModel.cs:10-20`) a předává se jako inline
  `style="background-color: ..."` — tento mechanismus zůstává beze změny.
- `TrainingScheduleBlockFactory`
  (`src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleBlockData.cs`)
  už řeší obdobný „uniformní vs. smíšený“ vzor pro stav tréninku
  (`IsUniformState`/`HasMixedState`) — stejný vzor lze použít pro typ
  tréninku u spojených bloků.
- `TrainingScheduleExcelExporter`
  (`src/SportSys.Razor/Services/TrainingScheduleExcelExporter.cs`) čte
  `TrainingScheduleBlockData` přímo (ne `ScheduleEventModel`) a používá
  `block.CoachSummary`/`block.TrainingTypeSummary`/`block.LocationSummary` —
  tyto vlastnosti **zůstávají beze změny** (export dál ukazuje celá jména a
  typ tréninku samostatně), issue se týká jen vizualizace v komponentě
  rozvrhu.

## Potvrzené požadavky a rozhodnutí

1. Pevná struktura 4 řádků (kategorie, čas, trenéři, lokalita) platí pro
   **tréninky i tréninkové plány** stejně; zápas (`Match`) není v rozsahu
   issue a jeho blok (kategorie, soupeř, výsledek) zůstává beze změny.
2. Řádek kategorie zůstává beze změny (žádná úprava `TitleLine`/
   `block.Title`/seskupovací logiky `+`).
3. Čas zůstává ve stávajícím formátu `H:mm–H:mm`, mění se pouze řez (tučně,
   `font-weight: 700`) a vizuální důraz (bez `opacity: .9`, které dnes sdílí
   s ostatními detail řádky).
4. Řádek trenérů zobrazuje pouze příjmení. Protože `User.DisplayName` je
   jediný volný textový sloupec beze struktury jméno/příjmení, příjmení se
   i tak musí odvodit jako **poslední mezerou oddělené slovo** z
   `DisplayName` (shoduje se s formátem „Jméno Příjmení“ plněným z Entra ID
   i s běžnou českou konvencí). Jednoslovný `DisplayName` (např. jen
   příjmení) se zobrazí beze změny. Tento výpočet se ale neprovádí opakovaně
   v Razor vrstvě (v `TrainingScheduleBlockFactory`) — provede se **jednou**
   v `SportSys.Contract` při načtení trenérů a výsledek se přenese přes nové
   jednoduché DTO `SimpleCoachDto` (`FullName` + `LastName`). Razor vrstva tak
   už žádný string z `DisplayName` neparsuje, jen čte hotovou vlastnost
   `LastName`.
5. Víc trenérů se i nadále odděluje čárkou; bez trenéra se zobrazí `-`
   (stávající chování, beze změny).
6. Řádek lokality zobrazuje jen název lokality (`LocationSummary`), typ
   tréninku se jako text v bloku **nevypisuje** — nahrazuje ho vizuální
   rozlišení pozadí. `TrainingTypeLocationSummary` se v bloku rozvrhu přestává
   používat (zůstává v `TrainingScheduleBlockData` pro Excel export, který
   typ i lokalitu vypisuje do samostatných sloupců).
7. Led = beze změny, plná barva pozadí (`background-color` inline styl).
   Suchá příprava = stejná barva + CSS `repeating-linear-gradient` přes
   `background-image` (nekoliduje s inline `background-color`, protože jde o
   odlišné dílčí vlastnosti zkratky `background`), bez obrázků, bez JS.
8. Typ tréninku se do Razor vrstvy nepropaguje jako syrové `TrainingTypeId`
   ani jako typ z `SportSys.Database` (architektonické pravidlo zakazuje
   referenci Razor → Database). Místo toho `TrainingScheduleService`
   (Contract) spočítá a vrátí **boolean příznak** `IsDryTraining`
   (`TrainingTypeId == (int)ETrainingType.Dry`), DTO i `ScheduleEventModel`
   pak nesou jen `bool`.
9. Spojený blok (vizualizační nebo databázová skupina) může teoreticky
   obsahovat položky s různými `TrainingTypeId`. Stejně jako u stavu tréninku
   se zavádí „uniformní vs. smíšený typ“: pokud mají všechny položky bloku
   shodný `IsDryTraining`, použije se jejich hodnota; při smíšeném typu se
   šrafování **nezobrazí** (blok se vykreslí jako led/plná barva) — jde o
   okrajový případ, pro který issue nepředepisuje chování a plné barvě
   odpovídá bezpečný, nerušivý fallback konzistentní se vzorem
   `HasMixedState`.
10. Žádná změna EF Core migrace ani databázového schématu — `TrainingTypeId`
    sloupec již existuje na obou entitách.

## Technický návrh

### Datový tok

```
Training / TrainingPlan (TrainingTypeId)
  → TrainingScheduleService (Contract): IsDryTraining = TrainingTypeId == (int)ETrainingType.Dry
    → TrainingScheduleItemDto / TrainingPlanScheduleItemDto (bool IsDryTraining)
      → TrainingScheduleBlockFactory (Razor): uniformní/smíšený IsDryTraining napříč položkami bloku
        → ScheduleEventModel (bool IsDryTraining)
          → Default.cshtml: `schedule-block--dry` modifikátor třídy

CoachTraining(s)/CoachTrainingPlan(s) (Coach.DisplayName)
  → TrainingScheduleService (Contract): AssignCoachesAsync — z DisplayName
    jednou vypočítá LastName a sestaví SimpleCoachDto { FullName, LastName }
    → TrainingScheduleItemDto / TrainingPlanScheduleItemDto
      (IReadOnlyList<SimpleCoachDto> Coaches)
      → TrainingScheduleBlockFactory (Razor): jen agreguje/řadí/distinctuje
        hotové SimpleCoachDto položky, žádné parsování řetězce
        → TrainingScheduleBlockData.CoachSummary (FullName, tooltip/export)
          a CoachSurnameSummary (LastName, viditelný text bloku)
```

### 1. `SportSys.Contract` — `ITrainingScheduleItem`, DTO a nové `SimpleCoachDto`

`src/SportSys.Contract/Models/TrainingScheduleDto.cs`:

- Přidat do `ITrainingScheduleItem`: `bool IsDryTraining { get; }`.
- Přidat `IsDryTraining` na `TrainingScheduleItemDto` a
  `TrainingPlanScheduleItemDto` (implementace interface property).
- Přidat nové jednoduché DTO:
  ```csharp
  public sealed class SimpleCoachDto
  {
      public required string FullName { get; init; }
      public required string LastName { get; init; }
  }
  ```
- Změnit typ `ITrainingScheduleItem.CoachFullNames` z
  `IReadOnlyList<string>` na `IReadOnlyList<SimpleCoachDto> Coaches`
  (přejmenování odráží, že položka už nenese jen holá jména, ale objekt
  s `FullName`/`LastName`). Upravit `TrainingScheduleItemDto` i
  `TrainingPlanScheduleItemDto` odpovídajícím způsobem.

`src/SportSys.Contract/Services/TrainingScheduleService.cs`:

- V projekci `GetTrainingsAsync`/obdobné metodě pro tréninky doplnit
  `IsDryTraining = t.TrainingTypeId == (int)SportSys.Database.Enums.ETrainingType.Dry`.
- V projekci `GetTrainingPlansAsync` (řádky ~188–210) doplnit stejný výraz nad
  `p.TrainingTypeId`.
- Výraz `(int)ETrainingType.Dry` je konstanta vyhodnocená překladačem, EF Core
  jej přeloží jako běžné porovnání `int` sloupce s literálem — žádný problém
  s překladem LINQ dotazu.
- Přejmenovat obě přetížení `AssignCoachFullNamesAsync` na
  `AssignCoachesAsync` a upravit, aby místo holého `DisplayName` stavěly
  `SimpleCoachDto`:
  ```csharp
  var assignments = await _db.CoachTrainings
      .Where(c => trainingIds.Contains(c.TrainingId))
      .Select(c => new
      {
          c.TrainingId,
          FullName = c.Coach.DisplayName ?? string.Empty,
      })
      .ToListAsync(ct);

  var assignmentsByTrainingId = assignments
      .ToLookup(a => a.TrainingId, a => a.FullName);

  foreach (var training in trainings)
  {
      training.Coaches = assignmentsByTrainingId[training.Id]
          .Distinct()
          .OrderBy(x => x)
          .Select(fullName => new SimpleCoachDto
          {
              FullName = fullName,
              LastName = ExtractLastName(fullName),
          })
          .ToList();
  }
  ```
  (obdobně pro tréninkové plány a `CoachTrainingPlans`).
- Doplnit privátní pomocnou metodu `ExtractLastName(string fullName)` do
  `TrainingScheduleService` — poslední mezerou oddělené slovo z `fullName`
  (`Trim()` → `LastIndexOf(' ')` → `Substring`), nebo celé jméno beze změny,
  pokud mezeru neobsahuje, nebo `string.Empty` pro prázdný vstup. Tím se
  výpočet příjmení z `DisplayName` provádí **jen jednou na úrovni Contract**
  a Razor vrstva už žádný string neparsuje.

### 2. `SportSys.Razor` — prezentační modely (jen agregace hotových `SimpleCoachDto`)

`src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleBlockData.cs`:

- `TrainingScheduleBlockFactory.CreateBlock` doplnit analogicky ke stavu
  tréninku:
  ```csharp
  var dryFlags = items.Select(item => item.IsDryTraining).Distinct().ToList();
  var isUniformTrainingType = dryFlags.Count == 1;
  var isDryTraining = isUniformTrainingType && dryFlags[0];
  ```
  a uložit na nový property `TrainingScheduleBlockData.IsDryTraining` (bool,
  `false` i při smíšeném typu — viz rozhodnutí 9).
- Nahradit dosavadní `CoachNames` (`IReadOnlyList<string>`) vlastností
  `Coaches` (`IReadOnlyList<SimpleCoachDto>`), sestavenou pouhou agregací bez
  jakéhokoliv parsování řetězce:
  ```csharp
  Coaches = items
      .SelectMany(item => item.Coaches)
      .DistinctBy(coach => coach.FullName, StringComparer.Ordinal)
      .OrderBy(coach => coach.FullName, StringComparer.Ordinal)
      .ToList(),
  ```
- `CoachSummary` (celá jména, pro tooltip a Excel export) i nová
  `CoachSurnameSummary` (jen příjmení, pro viditelný text bloku) se odvodí
  přímo z hotových vlastností `SimpleCoachDto`, bez volání jakékoliv
  parsovací metody v Razor vrstvě:
  ```csharp
  public string CoachSummary => Coaches.Count == 0
      ? "-"
      : string.Join(", ", Coaches.Select(c => c.FullName));
  public string CoachSurnameSummary => Coaches.Count == 0
      ? "-"
      : string.Join(", ", Coaches.Select(c => c.LastName));
  ```

`src/SportSys.Razor/Models/TrainingSchedule/ScheduleEventModel.cs`:

- Přidat `public bool IsDryTraining { get; init; }` (výchozí `false`,
  nastavuje se jen pro Training/TrainingPlan bloky; `CreateMatch` jej
  nenastavuje, zůstává `false`).
- V `CreateTrainingBlock` a `CreateTrainingPlanBlock`:
  - `IsDryTraining = block.IsDryTraining,`
  - `DetailLine1 = block.CoachSurnameSummary,` (řádek 3 — trenéři)
  - `DetailLine2 = block.LocationSummary,` (řádek 4 — lokalita, dřív
    `TrainingTypeLocationSummary`)
- `CreateTooltip` upravit na `item.Coaches.Select(c => c.FullName)` místo
  `item.CoachFullNames` (stejný výsledný text, jen přes nový typ).
- Tooltip zůstává jinak beze změny — dál zobrazuje typ tréninku textem i
  celá jména trenérů, jde jen o hover detail, ne o viditelný text bloku.

### 3. Razor view

`src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`
(řádky 60–115, obě varianty `<a>`/`<div>`):

- Třídu kontejneru rozšířit o podmíněný modifikátor:
  ```cshtml
  class="schedule-block @(block.IsDryTraining ? "schedule-block--dry" : null)"
  ```
  (a obdobně `schedule-block schedule-block--editable @(...)` u editovatelné
  varianty).
- Přeuspořádat/přejmenovat `span` prvky, aby odpovídaly pořadí kategorie →
  čas → trenéři → lokalita:
  ```cshtml
  <span class="schedule-block-cat">@block.Title</span>
  <span class="schedule-block-time">@block.TimeFrom.ToString("H:mm")–@block.TimeTo.ToString("H:mm")</span>
  <span class="schedule-block-coaches">@block.DetailLine1</span>
  <span class="schedule-block-location">@block.DetailLine2</span>
  ```
  (`schedule-block-type` třída zaniká, nahrazuje ji `schedule-block-location`).
- Stavová ikona (`schedule-block-state-icon`) zůstává beze změny.

### 4. SCSS

`src/SportSys.Razor/Styles/_schedule.scss` (okolí řádků 444–548):

- `.schedule-block-time` vyjmout ze sdíleného selektoru s `-type`/`-coaches`
  a dát mu vlastní, výraznější styl:
  ```scss
  .schedule-block-time {
    font-weight:   700;
    font-size:     .75rem;
    line-height:   1.2;
    white-space:   nowrap;
    overflow:      hidden;
    text-overflow: ellipsis;
  }

  .schedule-block-coaches,
  .schedule-block-location {
    font-size:    .6875rem;
    line-height:  1.2;
    opacity:      .9;
    white-space:  nowrap;
    overflow:     hidden;
    text-overflow: ellipsis;
  }
  ```
- Nový modifikátor pro suchou přípravu (jemné diagonální šrafování přes
  `background-image`, které se vrství nad inline `background-color`):
  ```scss
  .schedule-block--dry {
    background-image: repeating-linear-gradient(
      45deg,
      rgba(255, 255, 255, .22) 0,
      rgba(255, 255, 255, .22) 3px,
      transparent 3px,
      transparent 9px
    );
  }
  ```
  Hodnoty (šířka pruhu, krytí) ověřit vizuálně na všech barvách palety
  `--color-chart-1..8` a v dark i light módu podle `barevna-schemata` skillu
  — cílem je čitelný text a nerušivý, ale rozpoznatelný vzor. Použít
  poloprůhlednou bílou (ne novou paletovou barvu), aby šrafování fungovalo
  nezávisle na konkrétní barvě kategorie a v obou režimech vzhledu.

### 5. Dokumentace

`docs/modules/sport.md`, odstavec začínající „Každý blok zobrazuje čtyři
řádky…“ (aktuálně řádky ~113–120) — přepsat podle nové struktury (kategorie,
čas tučně, příjmení trenérů, lokalita) a doplnit popis vizuálního rozlišení
led/suchá příprava (plná barva vs. šrafování, `schedule-block--dry`,
fallback na plnou barvu při smíšeném typu spojeného bloku).

## Implementační kroky

### Fáze 1: Contract — datový příznak typu tréninku a `SimpleCoachDto`

1. Doplnit `IsDryTraining` do `ITrainingScheduleItem`,
   `TrainingScheduleItemDto`, `TrainingPlanScheduleItemDto`.
2. Doplnit výpočet `IsDryTraining` do obou projekcí v
   `TrainingScheduleService` (tréninky i plány).
3. Přidat `SimpleCoachDto` (`FullName`, `LastName`) do
   `TrainingScheduleDto.cs`, přejmenovat `ITrainingScheduleItem.CoachFullNames`
   na `Coaches` typu `IReadOnlyList<SimpleCoachDto>` a upravit obě DTO.
4. Přejmenovat obě přetížení `AssignCoachFullNamesAsync` na
   `AssignCoachesAsync`, doplnit privátní `ExtractLastName(string fullName)` a
   sestavovat `SimpleCoachDto` po materializaci `DisplayName` (viz Technický
   návrh, sekce 1).
5. Spustit `dotnet build SportSys.slnx -c Release` — ověřit 0 chyb (upraví se
   i implementace interface v testovacích pomocných metodách, pokud existují
   mimo DTO — ověřeno, že interface implementují jen tyto dvě DTO třídy).

### Fáze 2: Razor — prezentační modely

6. Doplnit `TrainingScheduleBlockData.IsDryTraining` a nahradit `CoachNames`
   vlastností `Coaches` (`IReadOnlyList<SimpleCoachDto>`), z níž se odvodí
   `CoachSummary` (celá jména) a `CoachSurnameSummary` (příjmení) — žádné
   parsování řetězce v `TrainingScheduleBlockFactory`.
7. Doplnit `ScheduleEventModel.IsDryTraining`, přemapovat `DetailLine1`/
   `DetailLine2` v `CreateTrainingBlock` a `CreateTrainingPlanBlock`, upravit
   `CreateTooltip` na `item.Coaches.Select(c => c.FullName)`.

### Fáze 3: View a SCSS

8. Upravit `Default.cshtml` — podmíněná třída `schedule-block--dry`,
   přejmenování/přeuspořádání `span` prvků (coaches před location, zrušení
   `schedule-block-type`).
9. Upravit `_schedule.scss` — tučný čas, nové/sloučené třídy
   `schedule-block-coaches`/`schedule-block-location`, nový modifikátor
   `schedule-block--dry` se šrafováním.
10. Spustit `npm run build:css` (ve `src/SportSys.Razor`) — ověřit, že SCSS
    zkompiluje bez chyb.

### Fáze 4: Testy a dokumentace

11. Doplnit/upravit jednotkové testy (viz sekce Testy a ověření níže).
12. Spustit `dotnet build SportSys.slnx -c Release` a
    `dotnet test tests/SportSys.Razor.Tests -c Release`.
13. Aktualizovat `docs/modules/sport.md` podle návrhu výše.
14. Manuálně ověřit v prohlížeči (viz Manuální akceptace), včetně dark módu.

## Soubory ke změně

- `src/SportSys.Contract/Models/TrainingScheduleDto.cs` — `IsDryTraining` na
  interface a obou DTO, nové `SimpleCoachDto`, přejmenování `CoachFullNames`
  na `Coaches` (`IReadOnlyList<SimpleCoachDto>`).
- `src/SportSys.Contract/Services/TrainingScheduleService.cs` — výpočet
  `IsDryTraining` v projekcích tréninků i plánů; přejmenování
  `AssignCoachFullNamesAsync` na `AssignCoachesAsync` a nová pomocná metoda
  `ExtractLastName`.
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleBlockData.cs` —
  `IsDryTraining`, nahrazení `CoachNames` vlastností `Coaches`
  (`IReadOnlyList<SimpleCoachDto>`), odvozené `CoachSummary` a
  `CoachSurnameSummary` (bez parsování řetězce).
- `src/SportSys.Razor/Models/TrainingSchedule/ScheduleEventModel.cs` —
  `IsDryTraining`, přemapování `DetailLine1`/`DetailLine2`, úprava
  `CreateTooltip` na `item.Coaches.Select(c => c.FullName)`.
- `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`
  — modifikátor třídy, pořadí a názvy `span` prvků.
- `src/SportSys.Razor/Styles/_schedule.scss` — tučný čas, sloučené třídy,
  `schedule-block--dry` šrafování.
- `docs/modules/sport.md` — popis nové struktury bloku a vizuálního
  rozlišení typu tréninku.
- `tests/SportSys.Razor.Tests/TrainingScheduleBlockFactoryTests.cs` — nové
  testy pro `CoachSurnameSummary`/`CoachSummary` (ze `SimpleCoachDto`) a
  `IsDryTraining` (uniformní/smíšený); testovací pomocná metoda
  `CreateTraining(...)` změní parametr `coaches` z `IReadOnlyList<string>?`
  na `IReadOnlyList<SimpleCoachDto>?`.
- `tests/SportSys.Razor.Tests/ScheduleEventModelFactoryTests.cs` — ověření
  nového mapování `DetailLine1`/`DetailLine2`/`IsDryTraining` pro Training i
  TrainingPlan blok; potvrzení, že Match blok zůstává beze změny.

## Testy a ověření

- `dotnet build SportSys.slnx -c Release` — musí proběhnout bez chyb.
- `npm run build:css` (ve `src/SportSys.Razor`) — musí proběhnout bez chyb.
- `dotnet test tests/SportSys.Razor.Tests -c Release` — všechny stávající
  testy musí procházet; přidat nové:
  - `TrainingScheduleBlockFactoryTests`: jednoslovné jméno trenéra zůstává
    beze změny; dvouslovné jméno (`"Jan Novák"`) → `CoachSurnameSummary`
    vrátí jen `"Novák"`; více trenérů oddělené čárkou (jen příjmení); bez
    trenéra → `"-"` (beze změny, ověřit i pro `CoachSurnameSummary`).
  - `TrainingScheduleBlockFactoryTests`: trénink s `IsDryTraining = true` →
    `block.IsDryTraining == true`; spojený blok se shodným
    `IsDryTraining` u všech položek → zachová hodnotu; spojený blok se
    smíšenými hodnotami → `block.IsDryTraining == false` (fallback).
  - `ScheduleEventModelFactoryTests`: `CreateTrainings`/`CreateTrainingPlans`
    nastaví `DetailLine1` na `CoachSurnameSummary` a `DetailLine2` na
    `LocationSummary`; `IsDryTraining` se propaguje z bloku; `CreateMatch`
    nemění `IsDryTraining` (zůstává `false`).
- `TrainingScheduleExcelExporterTests` — beze změny (export nepoužívá
  `ScheduleEventModel`, ale přímo `TrainingScheduleBlockData.CoachSummary`/
  `TrainingTypeSummary`/`LocationSummary`, které se nemění); spustit pro
  jistotu v rámci celé sady, neočekává se potřeba úprav.
- Poznámka k pokrytí `ExtractLastName`: repozitář má jediný testovací
  projekt `tests/SportSys.Razor.Tests` (žádný `SportSys.Contract.Tests`
  neexistuje), proto se chování `AssignCoachesAsync`/`ExtractLastName`
  ověří manuálně (viz Manuální akceptace, bod 7) a nepřímo přes
  `TrainingScheduleBlockFactoryTests`, které konstruují `SimpleCoachDto` s
  konkrétními dvojicemi `FullName`/`LastName` odpovídajícími heuristice.

## Manuální akceptace

1. Otevřít `/sport/Training/Schedule` a `/sport/Training/Plan` — každý blok
   tréninku/plánu zobrazuje čtyři řádky v pořadí: kategorie, čas (tučně),
   příjmení trenérů (oddělená čárkou nebo `-`), lokalita.
2. Trénink na ledě má plné barevné pozadí (beze změny oproti dnešku).
3. Trénink typu suchá příprava má stejnou barvu jako odpovídající kategorie,
   ale s jemným diagonálním šrafováním; text zůstává dobře čitelný.
4. Ověřit vizuální rozlišení led/suchá příprava v dark i light módu
   (`data-theme="dark"`).
5. Spojené bloky (filtr „Spojovat tréninky“) se shodným typem tréninku mezi
   členy zachovávají správné šrafování/plnou barvu; blok se smíšenými typy
   se vykreslí jako plná barva (bez šrafování).
6. Dlouhý seznam trenérů nebo dlouhý název lokality se ořízne (`ellipsis`),
   nerozbije layout bloku ani rozvrhu.
7. Trenér s jednoslovným `DisplayName` (např. jen příjmení) se zobrazí beze
   změny; trenér bez přiřazení zobrazuje `-`.
8. Tooltip (najetí myší na blok) nadále zobrazuje celé jméno trenéra i
   textový název typu tréninku a lokality (beze změny).
9. Export rozvrhu do XLSX nadále obsahuje sloupec „Typ tréninku“ s textovým
   názvem a sloupec „Trenéři“ s celými jmény (beze změny).
10. Zápasový blok (`/sport/Training/Schedule` s aktivním filtrem „Zápasy“)
    zůstává vizuálně beze změny (kategorie, čas, soupeř, výsledek).

## Beze změny

- Barevná konvence podle kategorie (`CategoryColors`, `--color-chart-1..8`)
  a způsob předání barvy (`inline style background-color`).
- Řádek kategorie (`TitleLine`/`schedule-block-cat`) a logika seskupování
  spojených bloků (`+` mezi kategoriemi).
- Stavová ikona tréninku (`TrainingStateVisual`) a stavová ikona zápasu
  (`MatchStateVisual`) — pozice, chování i tooltip zůstávají stejné.
- Zápasový blok (`ScheduleEventModelFactory.CreateMatch`) — struktura,
  obsah i vzhled.
- `Coaches` (`SimpleCoachDto.FullName`) a odvozený `CoachSummary` (celá
  jména) nadále používané v tooltipu a v Excel exportu — mění se jen
  vnitřní typ (`SimpleCoachDto` místo `string`), ne výsledný text.
- `TrainingScheduleBlockData.TrainingTypeSummary`/`LocationSummary`/
  `TrainingTypeLocationSummary` jako datové vlastnosti (zůstávají kvůli
  Excel exportu a tooltipu) — mění se jen to, co se z nich vykresluje
  v `Default.cshtml`.
- `TrainingScheduleExcelExporter` a jeho výstupní sloupce.
- Databázové schéma, EF Core model a migrace — `TrainingTypeId` sloupec již
  existuje na `Training` i `TrainingPlan`.
- Filtry stránek Schedule/Plan (#20) — nesouvisí s tímto issue.

## Mimo rozsah

- Přidávání textového označení „led“/„suchá příprava“ do bloku — issue
  explicitně vyžaduje čistě vizuální rozlišení bez textu.
- Rozšíření datového modelu `hr.Coach`/`identity.User` o strukturované
  příjmení/jméno — mimo rozsah issue; řeší se jen prezentační extrakcí
  z `DisplayName`.
- Úprava zápasového bloku, exportu do Excelu nebo tooltipu.
- Jakákoliv nová EF Core migrace.
- Konfigurovatelnost vzhledu šrafování (např. uživatelské nastavení hustoty
  vzoru) — issue žádá jen jedno pevné řešení.

## Hotovo, když

- [x] `ITrainingScheduleItem` a obě DTO nesou `IsDryTraining`, počítané v
      `TrainingScheduleService` z `TrainingTypeId`.
- [x] Blok tréninku i tréninkového plánu zobrazuje přesně čtyři řádky v
      pořadí kategorie → čas (tučně) → příjmení trenérů → lokalita.
- [x] Trénink na ledě má plnou barvu pozadí; suchá příprava má stejnou barvu
      s diagonálním šrafováním (`schedule-block--dry`, čisté CSS řešení).
- [x] Spojený blok se smíšeným typem tréninku se vykreslí jako plná barva
      (dokumentovaný fallback).
- [x] Příjmení trenéra se odvozuje z `DisplayName`; víc trenérů odděleno
      čárkou; bez trenéra se zobrazí `-`.
- [x] Zápasový blok, tooltip, Excel export a barevná konvence kategorií
      zůstávají beze změny.
- [x] `dotnet build SportSys.slnx -c Release` a `npm run build:css` proběhnou
      bez chyb.
- [x] Nové i stávající testy v `tests/SportSys.Razor.Tests` procházejí.
- [x] `docs/modules/sport.md` popisuje novou strukturu bloku a vizuální
      rozlišení typu tréninku.
- [x] Žádná EF Core migrace nebyla vytvořena ani upravena.
