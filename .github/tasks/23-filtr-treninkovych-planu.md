# Implementační plán: #23 Filtr tréninkových plánů

**Issue:** [#23 — Filtr tréninkových plánů](https://github.com/vasekNaus/SportSys/issues/23)

**Stav:** Implementováno. `GetTrainingPlansAsync` přijímá `int?
trainingPhaseId` a filtruje podmíněně, `OnGetAsync` nevyžaduje vyplněnou
fázi, popisek selectu je „— všechny fáze —“ a `docs/modules/sport.md` je
aktualizováno. `dotnet build` projektu `SportSys.Contract` a `dotnet test`
(127/127) prošly bez chyb.

## Cíl

Pole „Fáze tréninku“ ve filtru stránky Tréninkové plány
(`/sport/Training/Plan`) bude nepovinné. Lze filtrovat (zobrazit výsledky) i
bez jeho vyplnění — prázdná volba bude znamenat „všechny fáze“, stejně jako
to dnes funguje u filtrů Typ tréninku a Lokalita.

## Výchozí stav

- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs:41-42`
  — vlastnost `TrainingPhaseId` je už dnes `int?` (nullable), ale chování
  metody `OnGetAsync` ji **fakticky vynucuje jako povinnou**:
  ```csharp
  if (!SeasonId.HasValue || !TrainingPhaseId.HasValue)
  {
      return;
  }
  ```
  Pokud uživatel nevybere fázi, `ScheduleView` zůstane `null` a stránka
  nevykreslí žádné řádky ani prázdné řádky — ani při vyplněné sezóně a
  dalších filtrech.
- O pár řádků níže (`Index.cshtml.cs:105-113`) se do
  `_service.GetTrainingPlansAsync(...)` předává `TrainingPhaseId.Value` —
  tedy nenullable `int`, což vyžaduje, aby `TrainingPhaseId` mělo hodnotu.
- `src/SportSys.Contract/Services/TrainingScheduleService.cs:175-191` —
  `GetTrainingPlansAsync` přijímá `int trainingPhaseId` (non-nullable) a
  filtruje rovnou v hlavní `Where` podmínce:
  ```csharp
  var query = _db.TrainingPlans
      .Where(p => p.SeasonId == seasonId
          && categoryNames.Contains(p.SeasonCategoryName)
          && p.TrainingPhaseId == trainingPhaseId);
  ```
  Typ tréninku a lokalita jsou oproti tomu řešeny jako volitelné filtry
  (`trainingTypeIds.Count > 0` / `locationIds.Count > 0`) přidané
  podmíněně přes `query = query.Where(...)` až za touto hlavní podmínkou.
- `src/SportSys.Database/Models/sport/TrainingPlan.cs:26` — `TrainingPhaseId`
  je v databázi **povinný (non-nullable) FK** na každém řádku
  `TrainingPlan`. To se neumění — nejde o nullable sloupec, který by bylo
  třeba měnit v modelu; jde jen o to, že filtr podle této hodnoty nemá být
  vynucený.
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml:111-119`
  — `<select id="TrainingPhaseId">` s výchozí prázdnou možností
  „— vyberte fázi —“, bez `required`/validačního atributu — nepovinnost je
  tedy na UI úrovni už dnes v pořádku, problém je čistě v `OnGetAsync`
  a signatuře `GetTrainingPlansAsync`.
- `docs/modules/sport.md:312-325` (sekce „Filtry Plan“) popisuje současné
  (nesprávné) chování explicitně: „jedna fáze tréninku“ — dokumentace bude
  potřeba aktualizovat společně s kódem.
- Jediné volání `GetTrainingPlansAsync` v produkčním kódu je právě z této
  stránky (ověřeno — žádný jiný volající nezávisí na nenullable parametru).

## Potvrzené požadavky a rozhodnutí

Z textu issue: „Fáze tréninku ve filtru tréninkových plánů bude nepovinná.
Lze filtrovat bez jejího zadání.“ Žádné komentáře k issue nejsou.

Technická rozhodnutí (odvozená z existujícího vzoru stejné stránky):

- `TrainingPhaseId` zůstává `int?` v `IndexModel` (beze změny) — mění se jen
  podmínka, za které se filtr vůbec vyhodnotí.
- `GetTrainingPlansAsync` změní parametr `int trainingPhaseId` na
  `int? trainingPhaseId` a filtrování přesune za hlavní `Where` jako
  podmíněné `query = query.Where(p => p.TrainingPhaseId ==
  trainingPhaseId.Value)` jen když `trainingPhaseId.HasValue` — stejný
  vzor, jaký už metoda používá pro `trainingTypeIds`/`locationIds`.
- `TrainingPlan.TrainingPhaseId` v databázi **zůstává non-nullable** —
  nejde o změnu datového modelu, jen o to, že filtr podle fáze je
  volitelný; bez vyplnění se zobrazí plány všech fází.
- Požadavek na vyplněnou **sezónu** (`SeasonId`) zůstává nezměněn — issue se
  týká výhradně pole „Fáze tréninku“, sezóna není předmětem této změny.
- Popisek prázdné volby v `<select id="TrainingPhaseId">` se změní z
  „— vyberte fázi —“ na „— všechny fáze —“, aby odpovídal sémantice
  nepovinného filtru (stejná konvence jako prázdné popisky „Všechny typy“/
  „Všechny lokality“ u multiselect polí na téže stránce).

Žádná z těchto změn nemění datový model, bezpečnost ani rozsah migrace —
nejde se tedy ptát uživatele, plán používá existující vzor beze změny
veřejného chování mimo samotné vyžadování pole.

## Technický návrh

Změna je omezena na `SportSys.Contract` (signatura a dotaz služby) a
`SportSys.Razor` (PageModel + popisek v markupu). Žádný zásah do
`SportSys.Database` ani migrace.

### 1. `TrainingScheduleService.GetTrainingPlansAsync`

```csharp
public async Task<List<TrainingPlanScheduleItemDto>> GetTrainingPlansAsync(
    int seasonId,
    IReadOnlyCollection<string> categoryNames,
    IReadOnlyCollection<int> trainingTypeIds,
    IReadOnlyCollection<int> locationIds,
    int? trainingPhaseId,
    DateOnly? validOn,
    bool mergeOverlapping,
    CancellationToken ct = default)
{
    var query = _db.TrainingPlans
        .Where(p => p.SeasonId == seasonId
            && categoryNames.Contains(p.SeasonCategoryName));

    if (trainingPhaseId.HasValue)
        query = query.Where(p => p.TrainingPhaseId == trainingPhaseId.Value);

    if (trainingTypeIds.Count > 0)
        query = query.Where(p => trainingTypeIds.Contains(p.TrainingTypeId));
    ...
```

### 2. `IndexModel.OnGetAsync`

- Změnit guard na `if (!SeasonId.HasValue) { return; }` (odebrat
  `|| !TrainingPhaseId.HasValue`).
- Změnit volání služby z `TrainingPhaseId.Value` na `TrainingPhaseId`
  (předání nullable hodnoty beze změny ostatní logiky).

### 3. `Index.cshtml`

- Změnit text prázdné možnosti `<option value="">— vyberte fázi —</option>`
  na `<option value="">— všechny fáze —</option>`.

### 4. Dokumentace

- `docs/modules/sport.md`, sekce „Filtry Plan“: nahradit „jedna fáze
  tréninku“ za „nula nebo jedna fáze tréninku; prázdný výběr znamená
  všechny fáze“ — stejná formulace jako u typu tréninku a lokality o řádek
  výše/níže v témže výčtu.

## Implementační kroky

### Fáze 1: Contract — volitelný filtr podle fáze

1. V `TrainingScheduleService.GetTrainingPlansAsync` změnit parametr na
   `int? trainingPhaseId` a přesunout podmínku fáze do podmíněného
   `.Where(...)` za hlavní dotaz (viz Technický návrh).

### Fáze 2: Razor — odstranění vynucení pole

2. V `Index.cshtml.cs` upravit guard `OnGetAsync` a volání služby.
3. V `Index.cshtml` upravit popisek prázdné volby selectu.

### Fáze 3: Dokumentace

4. Aktualizovat `docs/modules/sport.md` (sekce „Filtry Plan“).

### Fáze 4: Ověření

5. `dotnet build SportSys.slnx`.
6. `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`
   (ověří, že beze změny prochází existující `TrainingPlanValidityFilterTests`
   a ostatní testy závislé na `TrainingScheduleService`).
7. Manuální ověření podle akceptačních scénářů níže.

## Soubory ke změně

- `src/SportSys.Contract/Services/TrainingScheduleService.cs` —
  `GetTrainingPlansAsync` (signatura + podmíněný filtr).
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs` —
  guard v `OnGetAsync` + volání služby.
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml` —
  popisek prázdné volby selectu „Fáze tréninku“.
- `docs/modules/sport.md` — sekce „Filtry Plan“.

## Testy a ověření

Existující `tests/SportSys.Razor.Tests/TrainingPlanValidityFilterTests.cs`
cílí na `ApplyValidityFilter` (filtr podle data platnosti) a touto změnou
není dotčen — stačí ověřit, že po úpravě signatury `GetTrainingPlansAsync`
projekt stále sestaví a testy projdou beze změny. Podmíněný filtr podle fáze
je stejně triviální jako již netestované podmíněné filtry
`trainingTypeIds`/`locationIds` ve stejné metodě — nový jednotkový test se
proto nezavádí, aby se zachovala konzistence s existujícím pokrytím této
metody. Ověření je primárně manuální (viz níže).

## Manuální akceptace

1. Na `/sport/Training/Plan` vybrat sezónu, nevyplnit „Fáze tréninku“ →
   stránka zobrazí tréninkové plány napříč všemi fázemi (dříve se
   nezobrazilo nic).
2. Vybrat konkrétní fázi → zobrazí se pouze plány dané fáze (beze změny
   oproti současnému chování).
3. Prázdná volba v selectu „Fáze tréninku“ má text „— všechny fáze —“.
4. Kombinace s dalšími filtry (typ tréninku, lokalita, den, kategorie, datum
   platnosti) funguje shodně s vyplněnou i nevyplněnou fází.
5. Odkaz „Vymazat filtry“ i nadále vrátí stránku bez parametrů do výchozího
   stavu.

## Beze změny

- Vynucení vyplnění sezóny (`SeasonId`) před zobrazením výsledků.
- Datový model `TrainingPlan.TrainingPhaseId` (zůstává non-nullable v DB).
- Chování filtrů typu tréninku, lokality, dne, kategorie a data platnosti.
- Export a vizualizace rozvrhu (`Training/Schedule`) — issue se týká pouze
  stránky Plan.

## Mimo rozsah

- Jakákoli změna číselníku `TrainingPhase` nebo jeho hodnot.
- Úprava stránky `Training/Schedule/Index`, která fázi tréninku ve filtru
  nepoužívá.

## Hotovo, když

- Filtr na `/sport/Training/Plan` vrátí výsledky i bez vyplnění pole „Fáze
  tréninku“ (při vyplněné sezóně), zobrazí plány všech fází.
- Vyplněná fáze filtruje stejně jako dnes.
- `dotnet build` a cílené testy projdou bez chyb.
- `docs/modules/sport.md` popisuje fázi tréninku jako nepovinný filtr.
