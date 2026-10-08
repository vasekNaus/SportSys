# Implementační plán: #25 Evidence požadovaného počtu zápasů

**Issue:** [#25 — Evidence požadovaného počtu zápasů](https://github.com/vasekNaus/SportSys/issues/25)

**Stav:** Implementováno a ověřeno testy; čeká na EF migraci (vytváří uživatel).

## Cíl

Evidovat plánovaný počet zápasů kategorie v období (`sport.MatchRequirement`)
a trenéry, kteří se na požadavku podílejí, včetně jejich role
(`sport.CoachMatchRequirement`). Stránku `/sport/Training/Requirement`
rozšířit tak, aby pod tabulkou požadavků na tréninky zobrazovala tabulku
požadavků na zápasy. Obě tabulky ovládá jeden společný filtr.

## Výchozí stav

- `TrainingRequirement`
  ([model](../../src/SportSys.Database/Models/sport/TrainingRequirement.cs))
  má `Id`, složený FK `SeasonId + SeasonCategoryCode` na `SeasonCategory`,
  `TrainingTypeId`, `TrainingPhaseId`, `From`, `To`, `DurationHours`.
- Vazební tabulka
  [CoachTrainingRequirement](../../src/SportSys.Database/Models/sport/CoachTrainingRequirement.cs)
  má složený PK `CoachId + TrainingRequirementId + CoachRoleId`
  (`hr.Coach`, `dbo.CoachRole`).
- Pojmenování sloupců (`SeasonCategory_Season_Id`, `Coach_Id`, ...) zajišťuje
  `modelBuilder.IdConvention()` v `SportSysDbContext.OnModelCreating`;
  `HasColumnName` se nepoužívá.
- `TrainingRequirementService`
  ([služba](../../src/SportSys.Contract/Services/TrainingRequirementService.cs))
  obsahuje `GetSeasonsAsync`, `GetCategoriesAsync`, `GetTrainingTypesAsync`,
  `GetTrainingPhasesAsync`, `GetAllAsync` (registrace
  `services.AddScoped<TrainingRequirementService>()` v
  `ServiceCollectionExtensions.cs`).
- DTO
  [TrainingRequirementDto.cs](../../src/SportSys.Contract/Models/TrainingRequirementDto.cs):
  `TrainingRequirementListItem`, `TrainingRequirementCoachListItem`
  (`DisplayText` = jméno nebo osobní číslo + role).
- Stránka
  [Index.cshtml](../../src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml)
  a [Index.cshtml.cs](../../src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml.cs)
  (`/sport/Training/Requirement`, položka „Požadavky“ v `_Layout.cshtml`).
  GET filtr: `SeasonId`, `SelectedCategoryCodes`, `SelectedTrainingTypeIds`,
  `SelectedTrainingPhaseIds`; nevalidní hodnoty se normalizují
  (`NormalizeIds`).
- Testy bez databáze (xUnit, bez InMemory/SQLite):
  [TrainingRequirementTests.cs](../../tests/SportSys.Razor.Tests/TrainingRequirementTests.cs).
- SQL skripty pro ruční převod DB jsou v `src/DB Model/` (např.
  `sport.Match-ALTER-AddMatchType.sql`).

## Potvrzené požadavky a rozhodnutí

Potvrzeno v issue a zadání:

1. Nová tabulka `[sport].[MatchRequirement]`: `Id` (IDENTITY), `SeasonCategory_Season_Id`,
   `SeasonCategory_Code` (varchar(10)), `From` (date), `To` (date),
   `MatchCount` (int), vše NOT NULL, `PK_MatchRequirement`.
   Ukázkové SQL v issue je poškozené (chybí název tabulky a sloupec kódu
   kategorie); vychází se z textového popisu sloupců.
2. Nová vazební tabulka `[sport].[CoachMatchRequirement]` — trenér a jeho role
   u požadavku na zápasy (zadání uživatele, v issue není).
3. Nový EF objekt v databázovém projektu a DTO objekt v Contractu.
4. Stránka `/sport/Training/Requirement` zobrazí pod tabulkou požadavků na
   tréninky tabulku požadavků na zápasy; obě jsou napojené na jeden filtr.

Technická rozhodnutí:

- `CoachMatchRequirement` zrcadlí `CoachTrainingRequirement`: PK
  `CoachId + MatchRequirementId + CoachRoleId`, FK na `hr.Coach`,
  `MatchRequirement` a `dbo.CoachRole`, `DeleteBehavior.ClientSetNull`.
- **Jeden filtr:** sezóna a kategorie platí pro obě tabulky. Typ a fáze
  tréninku se týkají pouze tréninků; zápasy je ignorují (zápasy tyto
  dimenze nemají) a v UI se to u popisků těchto polí uvede
  („platí pro tréninky“). Prázdný výběr = všechny hodnoty (stejně jako dnes).
- Načítání zápasových požadavků do nové `MatchRequirementService`; sezóny a
  kategorie pro filtr se nadále berou z `TrainingRequirementService`
  (beze změny, bez duplikace).
- Společný základ pro trenéra v DTO: nová třída `RequirementCoachListItem`
  (z `TrainingRequirementCoachListItem` se přesune `CoachRoleId`,
  `CoachRoleName`, `DisplayText`), z níž dědí
  `TrainingRequirementCoachListItem` i `MatchRequirementCoachListItem`.
  Stávající testy a použití zůstanou platné.
- Nad rámec issue se nepřidávají další omezení (CHECK `From <= To`,
  `MatchCount >= 0`), aby bylo chování shodné s `TrainingRequirement`.
- **Změna databázového modelu je potřeba** (2 nové tabulky). Migraci
  EF Core agent nevytváří ani neaplikuje; vytváří ji uživatel. Do
  `src/DB Model/` se přidá pouze referenční SQL skript.

## Technický návrh

### Datový model

`MatchRequirement` (`sport`):

| Vlastnost | Typ | Sloupec |
|---|---|---|
| `Id` | `int` `[Key]` | `Id` |
| `SeasonId` | `int` | `SeasonCategory_Season_Id` |
| `SeasonCategoryCode` | `string` `[StringLength(10)]` `[Unicode(false)]`, `required` | `SeasonCategory_Code` |
| `From` | `DateOnly` | `From` |
| `To` | `DateOnly` | `To` |
| `MatchCount` | `int` | `MatchCount` |

Navigace: `SeasonCategory` (`[ForeignKey(nameof(SeasonId) + ", " +
nameof(SeasonCategoryCode))]`, `ClientSetNull`) a
`ICollection<CoachMatchRequirement> CoachMatchRequirements`.

Index: `[Index(nameof(SeasonId), nameof(SeasonCategoryCode), nameof(From),
nameof(To), Name = "IX_MatchRequirement_SeasonCategory_Period")]` (FK indexy
se negenerují automaticky).

`CoachMatchRequirement` (`sport`): `[PrimaryKey(nameof(CoachId),
nameof(MatchRequirementId), nameof(CoachRoleId))]`, navigace `Coach`,
`MatchRequirement`, `CoachRole`; indexy
`IX_CoachMatchRequirement_MatchRequirement` (`MatchRequirementId`) a
`IX_CoachMatchRequirement_CoachRole` (`CoachRoleId`).

Zpětné navigace: `SeasonCategory.MatchRequirements`,
`Coach.CoachMatchRequirements`, `CoachRole.CoachMatchRequirements`.

Nové třídy jsou čisté atributové modely; Fluent konfigurace není potřeba.

### Contract

- `MatchRequirementListItem`: `Id`, `SeasonId`, `SeasonName`,
  `SeasonCategoryCode`, `SeasonCategoryOrder`, `From`, `To`, `MatchCount`,
  `CoachAssignments` (`IReadOnlyList<MatchRequirementCoachListItem>`).
- `MatchRequirementService.GetAllAsync(int seasonId,
  IReadOnlyCollection<string> categoryCodes, CancellationToken ct)` —
  `AsNoTracking`, filtr sezóny a (pokud neprázdné) kategorií, řazení shodné
  s `TrainingRequirementService.GetAllAsync` bez typu a fáze (sezóna sestupně,
  `Order`, `Code`, `From`, `To`, `Id`), trenéři řazeni podle jména, osobního
  čísla, role, `CoachId`, `CoachRoleId`.

### Razor

`IndexModel` dostane `MatchRequirementService` a vlastnost
`List<MatchRequirementListItem> MatchRequirements`. V `OnGetAsync` se po
načtení `Requirements` načtou `MatchRequirements` se stejným `SeasonId` a
`SelectedCategoryCodes`. Filtr, normalizace ani URL parametry se nemění.

## Implementační kroky

### Fáze 1: Databázový projekt

1. Vytvořit `src/SportSys.Database/Models/sport/MatchRequirement.cs` a
   `CoachMatchRequirement.cs` podle skillu `new-ef-entity`
   (`[Table(nameof(X), Schema = Schemas.Sport)]`, `nameof`, `[Index]`).
2. Doplnit navigační kolekce do `SeasonCategory.cs`, `Coach.cs`
   (`hr`) a `CoachRole.cs` (`dbo`).
3. Přidat do `SportSysDbContext` `DbSet<MatchRequirement> MatchRequirements`
   a `DbSet<CoachMatchRequirement> CoachMatchRequirements` (stejným stylem
   jako okolní `DbSet`y).
4. Přidat referenční skript `src/DB Model/sport.MatchRequirement.sql`:
   `CREATE TABLE [sport].[MatchRequirement]` (sloupce výše, `PK_MatchRequirement`),
   FK `FK_MatchRequirement_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Code`
   → `[sport].[SeasonCategory] ([Season_Id], [Code])` s `ON UPDATE CASCADE`
   (jako u `TrainingRequirement`), `CREATE TABLE [sport].[CoachMatchRequirement]`
   (`Coach_Id`, `MatchRequirement_Id`, `CoachRole_Id`, PK přes všechny tři) a
   FK na `[hr].[Coach]`, `[sport].[MatchRequirement]`, `[dbo].[CoachRole]`,
   plus příslušné indexy. Názvy sloupců ověřit proti skutečnému schématu
   `sport.SeasonCategory` (v `Crebas.sql` je ještě staré `Name`, entita
   používá `Code`).
5. **Neprovádět** `dotnet ef migrations add` ani úpravu snapshotu; uživatel
   vytvoří a aplikuje migraci / spustí skript.

### Fáze 2: Contract

1. V `TrainingRequirementDto.cs` vyčlenit `RequirementCoachListItem` a nechat
   `TrainingRequirementCoachListItem` dědit z něj; přidat
   `MatchRequirementListItem` a `MatchRequirementCoachListItem` (nové soubory
   `MatchRequirementDto.cs`).
2. Vytvořit `src/SportSys.Contract/Services/MatchRequirementService.cs`.
3. Zaregistrovat `services.AddScoped<MatchRequirementService>()` v
   `ServiceCollectionExtensions.cs` vedle `TrainingRequirementService`.

### Fáze 3: Razor stránka

1. `Index.cshtml.cs`: injektovat `MatchRequirementService`, přidat
   `MatchRequirements`, načíst je v `OnGetAsync` (viz výše).
2. `Index.cshtml`:
   - `ViewData["Title"]` změnit na „Požadavky“,
   - u popisků „Typ tréninku“ a „Fáze tréninku“ doplnit informaci, že filtr
     platí jen pro tréninky,
   - obalit stávající tabulku sekcí s nadpisem „Požadavky na tréninky“,
   - pod ni přidat sekci „Požadavky na zápasy“ s tabulkou
     (`Sezóna`, `Kategorie`, `Platnost od`, `Platnost do`, `Počet zápasů`,
     `Trenéři`), formát dat `dd.MM.yyyy`, trenéři jako dnes přes `DisplayText`
     nebo `-`,
   - prázdný stav každé sekce samostatně („Žádné požadavky na zápasy
     neodpovídají filtru.“); hláška „Neexistuje žádná aktivní sezóna.“ se
     zobrazí jednou místo obou sekcí.
3. Použít existující CSS třídy (`grid`, `text-muted`); SCSS ani `site.css` se
   neupravují. Barevná schémata se nemění.

### Fáze 4: Dokumentace

- `docs/modules/sport.md`: v tabulce rout doplnit `sport.MatchRequirement`,
  v sekci „Požadavky na tréninky“ popsat druhou tabulku a sémantiku filtru
  (typ/fáze jen pro tréninky), v „Datový model“ zmínit `MatchRequirement` a
  `CoachMatchRequirement`, v „Klíčové komponenty“ přidat
  `MatchRequirementService`.
- `docs/architecture.md` a `docs/modules/hr.md`: do diagramu vazeb na
  `hr.Coach.Id` doplnit `sport.CoachMatchRequirement`.

## Soubory ke změně

Nové:

- `src/SportSys.Database/Models/sport/MatchRequirement.cs`
- `src/SportSys.Database/Models/sport/CoachMatchRequirement.cs`
- `src/SportSys.Contract/Models/MatchRequirementDto.cs`
- `src/SportSys.Contract/Services/MatchRequirementService.cs`
- `src/DB Model/sport.MatchRequirement.sql`

Upravené:

- `src/SportSys.Database/Context/SportSysDbContext.cs`
- `src/SportSys.Database/Models/sport/SeasonCategory.cs`
- `src/SportSys.Database/Models/hr/Coach.cs`
- `src/SportSys.Database/Models/dbo/CoachRole.cs`
- `src/SportSys.Contract/Models/TrainingRequirementDto.cs`
- `src/SportSys.Contract/ServiceCollectionExtensions.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml.cs`
- `tests/SportSys.Razor.Tests/TrainingRequirementTests.cs`
- `docs/modules/sport.md`, `docs/architecture.md`, `docs/modules/hr.md`

## Testy a ověření

- Rozšířit `TrainingRequirementTests.cs` (bez DB, stejný styl):
  - `MatchRequirementCoachListItem.DisplayText` — jméno + role, fallback na
    osobní číslo,
  - `TrainingRequirementCoachListItem` a `MatchRequirementCoachListItem` jsou
    `RequirementCoachListItem` (regrese refaktoru),
  - stávající test `NormalizeIds_RemovesInvalidAndDuplicateValues` zůstává
    beze změny.
- Sestavení: `dotnet build SportSys.slnx`.
- Testy: `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`.
- Pozn.: dotazy `MatchRequirementService` nejsou pokryty automatickým testem,
  protože projekt testů nemá DB provider; ověřují se manuálně níže.

## Manuální akceptace

Po vytvoření a aplikaci migrace uživatelem:

1. `/sport/Training/Requirement` zobrazí sekci „Požadavky na tréninky“ a pod ní
   „Požadavky na zápasy“.
2. Změna sezóny přenačte obě tabulky; výběr kategorií omezí obě tabulky.
3. Výběr typu / fáze tréninku omezí jen tabulku tréninků; tabulka zápasů se
   nemění.
4. „Vymazat filtr“ vrátí výchozí sezónu a prázdný výběr pro obě tabulky.
5. Požadavek na zápasy bez trenéra zobrazí `-`; s trenéry zobrazí
   `Jméno (Role)` oddělené čárkami.
6. Sezóna bez zápasových požadavků zobrazí prázdný stav jen u zápasové sekce.
7. Stávající tabulka tréninků se chová beze změny.

## Beze změny

- Chování, URL a parametry filtru, `NormalizeIds`, `TrainingRequirementService`
  (kromě změny základu DTO trenéra), navigace v `_Layout.cshtml`.
- EF migrace a `SportSysDbContextModelSnapshot.cs`.
- `SportSys.Web`, SCSS a `wwwroot/css/site.css`.

## Mimo rozsah

- Editace/zakládání požadavků na zápasy (stránka zůstává read-only).
- Porovnání požadovaného počtu zápasů se skutečně naplánovanými zápasy
  v `sport.Match`.
- Import dat do nových tabulek a CHECK omezení.
- Filtrování zápasů podle typu zápasu.

## Hotovo, když

- Obě nové entity existují v `SportSys.Database`, jsou v `SportSysDbContext`
  a odpovídají popisu sloupců z issue (včetně `CoachMatchRequirement`).
- `MatchRequirementService` a DTO v Contractu jsou zaregistrované a
  `SportSys.Razor` nemá novou referenci na `SportSys.Database`.
- Stránka zobrazuje obě tabulky pod jedním filtrem podle pravidel výše.
- `dotnet build SportSys.slnx` a testy projdou.
- Dokumentace je aktualizována a není vytvořena žádná EF migrace.
