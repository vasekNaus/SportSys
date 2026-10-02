# Implementační plán: Stav zápasu (MatchState)

**Stav:** Připraveno k implementaci.

## Cíl

Rozšířit `sport.Match` o `MatchStateId` a zavést nový číselník `sport.MatchState`
s hodnotami `1. Plán`, `2. Potvrzený`, `3. Zrušený`. Stav zápasu se má chovat
analogicky ke stávajícímu `TrainingStateId` u tréninků: promítne se do
Contract vrstvy, filtru na `/sport/Training/Schedule` a vizuálu (ikona) v
komponentě rozvrhu. Navazuje na dřívější návrh sjednocení filtrů tréninků a
zápasů na téže stránce.

## Výchozí stav

- `Match` (`src/SportSys.Database/Models/sport/Match.cs`) dědí ze společného
  TPC předka `SportEvent` a nemá žádný stavový sloupec; workflow stavu řeší
  jen nullable `Result` (JSON sloupec).
- `Training` má obdobu `TrainingStateId` (`int`, povinné) →
  `sport.TrainingState`, číselník `ETrainingState` má 7 hodnot a je
  seedovaný přes `HasData` v `TrainingStateConfiguration`.
- `TrainingScheduleService.GetTrainingsAsync` filtruje podle
  `trainingStateIds`; `MatchScheduleService.GetMatchesAsync` obdobný filtr
  nemá a nemá se ani z čeho filtrovat.
- Vizuál stavu tréninku řeší `SportSys.Razor.Models.TrainingSchedule
  .TrainingStateVisual` — statická mapa `int Id → (CssClass, Icon)`
  s komentářem, že Id musí odpovídat `ETrainingState`. `Razor` nesmí
  odkazovat na `SportSys.Database`, mapování je proto vedeno čistě přes
  číselné Id.
- `ScheduleEventModel.StateIcon` / `StateTooltip` a jejich vykreslení v
  `Pages/Shared/Components/TrainingSchedule/Default.cshtml` jsou už obecné
  (nezávislé na `EventType`) — blok zápasu může ikonu stavu zobrazit beze
  změny v komponentě, jen musí být vyplněna v `ScheduleEventModelFactory
  .CreateMatch`.
- `MatchConfiguration` má komentář: *„TPC dědičnost nepodporuje pojmenované
  DEFAULT constrainty – nelze použít dvouparametrový overload.“* Proto `Id`
  používá jednoparametrový `HasDefaultValueSql`. Stejné omezení platí i pro
  nový `MatchStateId` — skill `has-default-value` (pojmenovaný constraint
  `DF_{Tabulka}_{Sloupec}`) se zde **nepoužije** ve své standardní podobě.
- `MatchScheduleItemDto` (`src/SportSys.Contract/Models/MatchScheduleItemDto.cs`)
  ani `Index.cshtml.cs` zatím žádný stav zápasu nenačítají ani nefiltrují.

## Potvrzené požadavky a rozhodnutí

- Nový číselník `sport.MatchState` se řídí konvencí lookup tabulky
  (`int Id` PK + `required string Name`), hodnoty spravuje enum
  `EMatchState`: `Plan = 1`, `ConfirmedKis = 2`, `Cancelled = 3` — pojmenování
  členů koresponduje s `ETrainingState` (`Plan`, `ConfirmedKis`, `Cancelled`),
  aby stejné anglické klíče šlo sdílet i vizuálně/textově.

  > Uživatelem zadané popisky „Plán / Potvrzený / Zrušený“ odpovídají třem
  > vybraným hodnotám z existujícího výčtu `ETrainingState` (`Plan`,
  > `ConfirmedKis`, `Cancelled`), nikoli sadě všech 7 stavů tréninku. Číselník
  > zápasu je ale samostatná tabulka/enum, ne sdílený s tréninkem — sémantika
  > zápasu (bez „KisOnly“, „ArenaFault“ apod.) je jednodušší a nezávislá.

- `Match.MatchStateId` je **nullable** (`int?`), na rozdíl od povinného
  `Training.TrainingStateId`. Důvod: historické/importované zápasy (např.
  `CsvMatchImportService`) nemají zdroj dat pro stav a nemá se donutit
  vymýšlet umělou hodnotu jen kvůli NOT NULL sloupci.
- I přes nullabilitu sloupce se do `MatchConfiguration` přidá výchozí
  hodnota `HasDefaultValueSql("(1)")` (bez pojmenovaného overloadu, viz TPC
  omezení výše), aby **nově vytvářené** zápasy (typicky ruční založení v
  administraci) dostaly stav `Plán`, pokud volající kód hodnotu explicitně
  nenastaví. Existující řádky se řeší v migraci (viz níže), nikoli v
  aplikačním kódu.
- Backfill existujících zápasů při migraci: nastavit `MatchStateId = 1`
  (`Plán`) u všech současných řádků `sport.Match`, kde je hodnota `NULL` po
  přidání sloupce (typicky všechny, protože sloupec je nový). Toto je
  jednorázová `UPDATE` součást migrace, kterou vytváří uživatel — agent
  migraci nevytváří, jen na tento krok v poznámce migrace upozorní.
- Filtr „Stav zápasu“ na `/sport/Training/Schedule` bude **samostatný**
  multiselect vedle „Stavy tréninku“ (ne sloučený do jednoho seznamu), protože
  `MatchState` a `TrainingState` jsou odlišné číselníky s odlišnými ID
  prostory. Zobrazí se jen v sekci týkající se zápasů (viz dřívější návrh
  přepínače „Zobrazit tréninky/zápasy“ — pokud ten návrh ještě není
  implementován, filtr se prozatím zobrazí vždy, souběžně s filtrem tréninku).
- Vizuál: `MatchStateVisual` v `SportSys.Razor` bude samostatná statická mapa
  analogická `TrainingStateVisual`, s vlastními ikonami (např. 📅 Plán,
  ✅ Potvrzený, ❌ Zrušený — stejné emoji jako u odpovídajících stavů
  tréninku pro vizuální konzistenci).
- Agent nevytváří ani neupravuje EF Core migraci ani model snapshot.

## Technický návrh

### Databázový model

1. **Enum** `src/SportSys.Database/Enums/EMatchState.cs`:
   `Plan = 1`, `ConfirmedKis = 2`, `Cancelled = 3`, s `[Display]` atributy
   směřujícími na `SportSys.Database.Resources.EMatchState`.
2. **Entita** `src/SportSys.Database/Models/sportSchema/MatchState.cs`
   (auto-generated styl, `[Table(nameof(MatchState), Schema = Schemas.Sport)]`):
   `Id` (PK), `Name` (`StringLength(50)`), navigační kolekce
   `ICollection<Match> Matches`.
3. **Seed partial** `src/SportSys.Database/Models/sportSchema/MatchState.Seed.cs`:
   privátní bezparametrický konstruktor + `[SetsRequiredMembers]` konstruktor
   `MatchState(EMatchState id)`, `Name` z `Resources.EMatchState
   .ResourceManager.GetString(id.ToString(), CultureInfo.GetCultureInfo("cs"))`
   (stejný vzor jako `TrainingState.Seed.cs`).
4. **Konfigurace** `src/SportSys.Database/Configurations/sport
   /MatchStateConfiguration.cs`: `IEntityTypeConfiguration<MatchState>` s
   `HasData(Enum.GetValues<EMatchState>().Select(e => new MatchState(e)))`.
5. **RESX lokalizace** v `src/SportSys.Database/Resources/`:
   - `EMatchState.cs` — ResourceManager wrapper (stejný vzor jako
     `ETrainingState.cs`).
   - `EMatchState.resx` — anglický fallback (`Plan`, `ConfirmedKis`,
     `Cancelled` → čitelné anglické texty).
   - `EMatchState.cs.resx` — české překlady („Plán“, „Potvrzený“, „Zrušený“).
6. **Rozšíření `Match`** (`src/SportSys.Database/Models/sport/Match.cs`):
   přidat `public int? MatchStateId { get; set; }` a navigaci
   `[DeleteBehavior(DeleteBehavior.ClientSetNull)] public virtual MatchState?
   MatchState { get; set; }`.
7. **`MatchConfiguration`** (`src/SportSys.Database/Configurations/sport
   /MatchConfiguration.cs`): doplnit
   `builder.Property(e => e.MatchStateId).HasDefaultValueSql("(1)");`
   — jednoparametrový overload kvůli TPC (viz komentář u `Id` ve stejném
   souboru), **ne** dvouparametrový `HasDefaultValue(..., "DF_...")`.
8. **`SportSysDbContext`**: přidat `public virtual DbSet<MatchState>
   MatchStates { get; set; }` vedle stávajícího `DbSet<MatchType> MatchTypes`.
9. Migraci ani model snapshot agent nevytváří ani neupravuje. V poznámce k
   zadání pro uživatele zdůraznit nutnost ručního backfillu (`UPDATE
   sport.Match SET MatchStateId = 1 WHERE MatchStateId IS NULL` — nebo
   ekvivalentně přes `migrationBuilder.Sql` v generované migraci) po přidání
   sloupce, protože EF Core scaffolding pro nullable sloupec bez explicitní
   `Sql`/`Data` akce žádná stará data nedoplní.

### Contract vrstva

1. **`LookupSelectItem`** se znovupoužije beze změny pro nabídku hodnot
   číselníku (stejně jako u `TrainingTypes`/`TrainingStates`).
2. **`MatchScheduleService`**:
   - Nová metoda `GetMatchStatesAsync(CancellationToken ct = default)` →
     `List<LookupSelectItem>` analogická
     `TrainingScheduleService.GetTrainingStatesAsync`.
   - `GetMatchesAsync` rozšířit o parametr
     `IReadOnlyCollection<int> matchStateIds` a filtr
     `if (matchStateIds.Count > 0) query = query.Where(m =>
     m.MatchStateId.HasValue && matchStateIds.Contains(m.MatchStateId.Value));`
   - `MatchProjection` a `CreateDto` rozšířit o `MatchStateId` (`int?`) a
     `MatchStateName` (`string?`, z `match.MatchState!.Name`, s `LEFT JOIN`
     sémantikou přes navigaci — ošetřit `null` stav bez vyhození výjimky,
     protože sloupec je nullable).
3. **`MatchScheduleItemDto`**: přidat `public int? MatchStateId { get; set;
   }` a `public string? MatchStateName { get; set; }`.

### Razor vrstva

1. **`IndexModel`** (`Areas/sport/Pages/Training/Schedule/Index.cshtml.cs`):
   - `public List<LookupSelectItem> MatchStates { get; private set; } = [];`
   - `[BindProperty(SupportsGet = true)] public List<int>
     SelectedMatchStateIds { get; set; } = [];`
   - V `LoadAndNormalizeFiltersAsync` načíst `MatchStates` a normalizovat
     `SelectedMatchStateIds` stejným vzorem jako
     `SelectedTrainingStateIds`.
   - Předat `SelectedMatchStateIds` do volání `_matchService
     .GetMatchesAsync(...)` na obou místech (`OnGetAsync` i
     `OnGetExportAsync`).
2. **`ScheduleEventModelFactory.CreateMatch`**: vyplnit `StateIcon` a
   `StateTooltip` z nové `MatchStateVisual.Get(match.MatchStateId)`, stejným
   způsobem jako `CreateTrainingBlock` používá `TrainingStateVisual`. Do
   `tooltipParts` přidat `MatchStateName`, pokud není `null`.
3. **Nová třída** `src/SportSys.Razor/Models/TrainingSchedule
   /MatchStateVisual.cs`:
   ```
   [1] = new("match-state-plan", "📅"),
   [2] = new("match-state-confirmed", "✅"),
   [3] = new("match-state-cancelled", "❌"),
   ```
   s komentářem odkazujícím na `SportSys.Database.Enums.EMatchState`
   (`Plan=1, ConfirmedKis=2, Cancelled=3`) — Razor nesmí odkazovat na
   `SportSys.Database`, mapování jen přes Id (stejné pravidlo jako u
   `TrainingStateVisual`).
4. **`Index.cshtml`**: přidat multiselect „Stav zápasu“ podle vzoru
   stávajícího `schedule-multiselect` bloku pro „Lokalita“/„Typ tréninku“,
   navázaný na `Model.MatchStates` / `Model.SelectedMatchStateIds`. Umístit
   vedle fieldsetu „Stavy tréninku“ (buď jako druhý fieldset „Stavy zápasu“,
   nebo do stejného řádku filtrů jako multiselect — preferovaná varianta:
   samostatný `fieldset` se stejnou třídou `schedule-filter-categories`,
   viditelný vždy, dokud nebude implementován širší přepínač
   Trénink/Zápas z předchozího návrhu).

### CSS

- Doplnit `.match-state-plan`, `.match-state-confirmed`,
  `.match-state-cancelled` do stylů komponenty rozvrhu (SCSS zdroj v
  `src/SportSys.Razor/Styles/`), pokud stávající `.training-state-*` třídy
  nesou jen sémantický název bez vlastního vizuálního stylu (ikona nese
  emoji, třída slouží jen pro CSS hook/testovatelnost) — ověřit při
  implementaci, zda je barvy/styl vůbec potřeba dodávat, nebo zda emoji
  postačí stejně jako u `TrainingStateVisual`.

## Implementační kroky

### Fáze 1: Databázový model a číselník

1. Vytvořit `EMatchState`, `MatchState.cs`, `MatchState.Seed.cs`,
   `MatchStateConfiguration.cs` a RESX trojici podle skillu
   `lookup-table`.
2. Rozšířit `Match.cs` o `MatchStateId` + navigaci `MatchState`.
3. Doplnit `MatchConfiguration` o `HasDefaultValueSql("(1)")` pro
   `MatchStateId` (jednoparametrový overload kvůli TPC).
4. Zaregistrovat `DbSet<MatchState>` v `SportSysDbContext`.
5. Nevytvářet ani neupravovat migraci — připravit model pro migraci, kterou
   spustí uživatel, a v shrnutí odpovědi zdůraznit nutnost ručního backfillu
   existujících řádků na `MatchStateId = 1`.

### Fáze 2: Contract vrstva

1. `MatchScheduleService.GetMatchStatesAsync` — nová metoda pro nabídku
   číselníku.
2. `MatchScheduleService.GetMatchesAsync` — přidat parametr
   `matchStateIds` a filtr; rozšířit `MatchProjection`/`CreateDto`.
3. `MatchScheduleItemDto` — přidat `MatchStateId`, `MatchStateName`.

### Fáze 3: Razor UI

1. `IndexModel` — nové vlastnosti, načtení, normalizace, předání do služby.
2. `MatchStateVisual` — nová statická mapa ikon.
3. `ScheduleEventModelFactory.CreateMatch` — vyplnění `StateIcon`/
   `StateTooltip`/tooltip textu.
4. `Index.cshtml` — nový filtr „Stav zápasu“.
5. Podle potřeby doplnit CSS třídy stavu zápasu.

### Fáze 4: Dokumentace

1. Aktualizovat `docs/modules/sport.md` (tabulka číselníků, sekce Rozvrh —
   filtr a vizuál stavu zápasu).
2. Pokud existuje zmínka o číselnících v `docs/conventions.md`, ověřit, že
   nový vzor `MatchState` nevyžaduje žádnou výjimku k zapsání.

## Soubory ke změně

| Oblast | Soubory |
|---|---|
| Enum a RESX | `src/SportSys.Database/Enums/EMatchState.cs`, `src/SportSys.Database/Resources/EMatchState.cs`, `EMatchState.resx`, `EMatchState.cs.resx` |
| EF model a mapování | `src/SportSys.Database/Models/sportSchema/MatchState.cs`, `MatchState.Seed.cs`, `src/SportSys.Database/Models/sport/Match.cs`, `src/SportSys.Database/Configurations/sport/MatchStateConfiguration.cs`, `MatchConfiguration.cs`, `src/SportSys.Database/Context/SportSysDbContext.cs` |
| Contract | `src/SportSys.Contract/Services/MatchScheduleService.cs`, `src/SportSys.Contract/Models/MatchScheduleItemDto.cs` |
| Razor | `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml.cs`, `Index.cshtml`, `src/SportSys.Razor/Models/TrainingSchedule/MatchStateVisual.cs` (nový), `ScheduleEventModel.cs` (`CreateMatch`) |
| SCSS (volitelně) | `src/SportSys.Razor/Styles/` — třídy `match-state-*` |
| Dokumentace | `docs/modules/sport.md` |

## Testy a ověření

- Rozšířit/doplnit unit testy `MatchScheduleService` (pokud existují) o
  filtr podle `matchStateIds` a správné mapování `MatchStateName` pro
  `NULL` i vyplněný stav.
- Ověřit `dotnet build SportSys.slnx`.
- Spustit `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`.
- Manuálně ověřit na `/sport/Training/Schedule`: filtr „Stav zápasu“
  omezuje zobrazené zápasy, ikona stavu se zobrazuje v bloku zápasu a
  tooltip obsahuje název stavu.

## Beze změny

- `TrainingState`/`ETrainingState` a jejich filtr — zůstávají samostatné,
  nesjednocují se s `MatchState` do společného číselníku.
- `MatchType`/`MatchTypeId` — mimo rozsah tohoto plánu (řešeno případně
  samostatně, viz dřívější návrh filtru „Typ zápasu“).
- EF Core migrace a model snapshot — vytváří výhradně uživatel.

## Mimo rozsah

- Přepínač „Zobrazit tréninky/zápasy“ a filtr „Typ zápasu“ z dřívějšího
  návrhu sjednocení filtrů — samostatné navazující úkoly.
- Automatické odvození `MatchStateId` z existujícího `Result` (např.
  auto-nastavení „Potvrzený“ po zadání výsledku) — lze řešit later jako
  business pravidlo, není součástí tohoto plánu.

## Hotovo, když

- `sport.MatchState` existuje jako číselník se třemi hodnotami (`Plán`,
  `Potvrzený`, `Zrušený`), model a konfigurace jsou připravené pro migraci.
- `Match.MatchStateId` je nullable FK s výchozí hodnotou `1` pro nově
  vytvářené řádky (bez pojmenovaného DEFAULT constraintu kvůli TPC).
- Rozvrh na `/sport/Training/Schedule` nabízí filtr „Stav zápasu“ a
  zobrazuje ikonu/tooltip stavu u bloku zápasu.
- Neexistuje vytvořená ani upravená EF Core migrace nebo snapshot.
