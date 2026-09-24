# Implementační plán: #13 Vizualizace stavů tréninků v rozvrhu a exportech

**Issue:** [#13 — Vizualizace stavů tréninků v rozvrhu a exportech](https://github.com/vasekNaus/SportSys/issues/13)

**Stav:** Připraveno k implementaci.

## Cíl

Rozšířit vizualizaci bloků na stránce `/sport/Training/Schedule` o barevné
zobrazení stavu tréninku (`TrainingState`) a ikonu stavu, včetně korektního
chování u spojených tréninků (stejný vs. rozdílný stav). Doplnit export do
Excelu o sloupec „Stav“. Filtr stavů (`#12`) doplnit o ikonu před názvem
stavu.

## Výchozí stav

- `ITrainingScheduleItem`
  (`src/SportSys.Contract/Models/TrainingScheduleDto.cs`) neobsahuje žádnou
  informaci o stavu tréninku. Implementují ho `TrainingPlanScheduleItemDto`
  a `TrainingScheduleItemDto` (dědí z prvního, přidává `Date`).
- Entita `Training` má `TrainingStateId` (NOT NULL) a navigaci `TrainingState`
  s lokalizovaným `Name` (např. „Potvrzený KIS“ — viz
  `src/SportSys.Database/Models/sportSchema/TrainingState.Seed.cs`, který
  neukládá neutrální klíč, ale rovnou český text z RESX). Entita `TrainingPlan`
  **nemá** žádný sloupec stavu — stránka `/sport/Training/Plan` proto stav
  nemá a nebude mít.
- `TrainingScheduleService.GetTrainingsAsync`
  (`src/SportSys.Contract/Services/TrainingScheduleService.cs`) už filtruje
  podle `TrainingStateId` (issue `#12`), ale neprojektuje jej do DTO.
- Vizualizace bloků prochází přes:
  1. `TrainingScheduleBlockFactory.CreateBlocks`
     (`src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleBlockData.cs`)
     — seskupí položky do `TrainingScheduleBlockData` (spojené tréninky podle
     `VisualizationGroupId`/`GroupId`), `Title` = `"Kategorie1 + Kategorie2"`.
  2. `TrainingScheduleComponentModel.CreateBlock`
     (`src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleComponentModel.cs`)
     — převede `TrainingScheduleBlockData` na `TrainingScheduleBlock` pro
     view, mj. `Color` = barva kategorie (pozadí bloku).
  3. `Default.cshtml`
     (`src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`)
     — vykreslí `<span class="schedule-block-cat">@block.Title</span>` jako
     jediný text bez rozlišení kategorií.
  4. Stejný `TrainingScheduleBlockFactory` používá i
     `TrainingScheduleExcelExporter`
     (`src/SportSys.Razor/Services/TrainingScheduleExcelExporter.cs`) pro
     export — 7 sloupců, žádný sloupec Stav.
- Filtr „Stavy tréninku“ (`Index.cshtml`, stránka Schedule) vykresluje
  `@state.Name` bez ikony (implementováno v `#12`).
- CSS proměnné jsou tříúrovňové sémantické tokeny v
  `src/SportSys.Razor/Styles/_vars.scss` (`--color-success*`,
  `--color-warning*`, `--color-error*`, `--color-info*`,
  `--color-chart-1..8`), definované zvlášť pro light/dark mód. Skill
  `barevna-schemata` vyžaduje nové barvy zavádět jako sémantické tokeny, ne
  napevno v komponentě.
- Styl bloku (`.schedule-block-cat` v `_schedule.scss`) má bílý text s
  `text-shadow` na barevném pozadí kategorie (`.schedule-block`), takže nová
  barva stavu se aplikuje na text uvnitř barevného pozadí bloku, ne na pozadí
  samotné.

## Potvrzené požadavky a rozhodnutí

- Barva se aplikuje na text názvu kategorie, ne na pozadí bloku (pozadí
  zůstává barvou kategorie beze změny).
- Samostatný trénink: barva + ikona stavu (ikona vpravo dole v bloku).
- Spojené tréninky se stejným stavem: celý text (včetně „+“) jednou barvou +
  ikona.
- Spojené tréninky s rozdílnými stavy: každá kategorie vlastní barvou, „+“
  standardní barvou textu, ikona se nezobrazuje.
- Řazení kategorií v textu zůstává podle pořadí kategorie (beze změny
  stávající logiky v `TrainingScheduleBlockFactory`).
- Řešení podporuje libovolný počet spojených tréninků.
- Export: nový sloupec „Stav“; u jednoznačného stavu textová hodnota, u
  rozdílných stavů prázdná buňka.
- Filtr stavů zobrazuje ikonu před názvem stavu.
- Stránka `/sport/Training/Plan` se nemění — `TrainingPlan` nemá sloupec
  stavu, `TrainingPlanScheduleItemDto` proto ponese stav jako `null` a
  vizualizace zůstane beze změny (žádná barva, žádná ikona), což zachovává
  regresní požadavek na beze změny editace/vizualizace plánů.
- Mapování `TrainingStateId → CSS třída/ikona` je vedeno podle číselných `Id`
  (1–7), která jsou dle konvence číselníků (`docs/conventions.md`) neměnná od
  vytvoření. Mapování žije výhradně v `SportSys.Razor`, protože Razor nesmí
  odkazovat na `SportSys.Database` (a tedy ani na enum `ETrainingState`);
  komentář v kódu mapování odkáže na `ETrainingState` jen textově pro
  dohledatelnost.
- Ikony: použijí se emoji znaky přesně podle tabulky v issue (📅 ✅ 📝 🕒 ❌
  🔄 ⚠️), issue výslovně připouští pozdější nahrazení jiným grafickým
  provedením — není součástí rozsahu této změny.
- Konkrétní odstíny barev pro 7 stavů: issue nechává „definování v rámci UI
  návrhu“. Zavedou se nové sémantické tokeny
  `--color-training-state-{plan|confirmed|kis-only|time-change|cancelled|
  date-change|zs-failure}` v `_vars.scss` (light i dark mód), odvozené od
  existujících odstínů (`--color-info`, `--color-success`, `--color-warning`,
  `--color-error` a doplňkově `--color-chart-*` pro stavy bez jasné sémantické
  kategorie), aby všech 7 stavů bylo vzájemně odlišitelných. Přesné hex
  hodnoty lze doladit v code review bez dopadu na strukturu řešení.

## Technický návrh

Datový tok: `Training.TrainingStateId/TrainingState.Name` → DTO
(`ITrainingScheduleItem`) → `TrainingScheduleBlockData` (agregace stavu přes
skupinu) → `TrainingScheduleBlock` (CSS třída, ikona, segmenty) →
`Default.cshtml` (vykreslení) a `TrainingScheduleExcelExporter` (sloupec
Stav). Mapování Id → (CSS třída, ikona) je centralizované v novém statickém
helperu v Razor vrstvě, aby ho sdílel vizualizační i exportní kód i filtr.

## Implementační kroky

### Fáze 1: Rozšíření datového kontraktu o stav

Upravit `src/SportSys.Contract/Models/TrainingScheduleDto.cs`:

1. Do `ITrainingScheduleItem` přidat:
   ```csharp
   int? TrainingStateId { get; }
   string? TrainingStateName { get; }
   ```
2. Do `TrainingPlanScheduleItemDto` přidat vlastnosti (výchozí `null`):
   ```csharp
   public int? TrainingStateId { get; set; }
   public string? TrainingStateName { get; set; }
   ```
   `TrainingScheduleItemDto` je nezdědí explicitně — zůstává implementace ze
   základní třídy, hodnoty se nastaví jen při projekci reálných tréninků.

Upravit `src/SportSys.Contract/Services/TrainingScheduleService.cs`:

3. V projekci uvnitř `GetTrainingsAsync` doplnit:
   ```csharp
   TrainingStateId = t.TrainingStateId,
   TrainingStateName = t.TrainingState.Name,
   ```
4. `GetTrainingPlansAsync` a jeho projekce `TrainingPlanScheduleItemDto`
   **nezměnit** — nové vlastnosti zůstanou `null`.

### Fáze 2: Mapování stavu na CSS třídu a ikonu (Razor)

Vytvořit `src/SportSys.Razor/Models/TrainingSchedule/TrainingStateVisual.cs`:

```csharp
namespace SportSys.Razor.Models.TrainingSchedule;

// Id odpovídá stabilním hodnotám číselníku TrainingState
// (SportSys.Database.Enums.ETrainingState: Plan=1, ConfirmedKis=2, KisOnly=3,
// TimeChanged=4, Cancelled=5, DateChanged=6, ArenaFault=7). Razor nesmí
// odkazovat na SportSys.Database, mapování je proto vedeno přes Id.
public static class TrainingStateVisual
{
    private static readonly IReadOnlyDictionary<int, TrainingStateVisualInfo> ById =
        new Dictionary<int, TrainingStateVisualInfo>
        {
            [1] = new("training-state-plan", "📅"),
            [2] = new("training-state-confirmed", "✅"),
            [3] = new("training-state-kis-only", "📝"),
            [4] = new("training-state-time-change", "🕒"),
            [5] = new("training-state-cancelled", "❌"),
            [6] = new("training-state-date-change", "🔄"),
            [7] = new("training-state-zs-failure", "⚠️"),
        };

    public static TrainingStateVisualInfo? Get(int? trainingStateId)
        => trainingStateId.HasValue && ById.TryGetValue(trainingStateId.Value, out var info)
            ? info
            : null;
}

public readonly record struct TrainingStateVisualInfo(string CssClass, string Icon);
```

### Fáze 3: Agregace stavu při seskupování bloků

Upravit
`src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleBlockData.cs`:

1. Přidat model segmentu kategorie:
   ```csharp
   public sealed class TrainingScheduleCategorySegment
   {
       public required string CategoryName { get; init; }
       public string? StateCssClass { get; init; }
   }
   ```
2. Do `TrainingScheduleBlockData` přidat:
   ```csharp
   public required IReadOnlyList<TrainingScheduleCategorySegment> CategorySegments { get; init; }
   public bool IsUniformState { get; init; }
   public string? UniformStateCssClass { get; init; }
   public string? UniformStateIcon { get; init; }
   public string? UniformStateName { get; init; }
   ```
3. V `TrainingScheduleBlockFactory.CreateBlock` po sestavení `items` odvodit
   stav:
   ```csharp
   var stateIds = items.Select(item => item.TrainingStateId).ToList();
   var hasAnyState = stateIds.Any(id => id.HasValue);
   var isUniformState = hasAnyState
       && stateIds.All(id => id.HasValue)
       && stateIds.Distinct().Count() == 1;
   ```
   - `hasAnyState == false` (např. blok tréninkového plánu): `IsUniformState =
     true`, `UniformStateCssClass = null`, `UniformStateIcon = null`,
     `UniformStateName = null` — zachová dnešní vzhled beze změny.
   - `isUniformState == true`: `UniformStateCssClass`/`UniformStateIcon` z
     `TrainingStateVisual.Get(stateIds[0])`, `UniformStateName =
     items[0].TrainingStateName`.
   - Jinak (rozdílné stavy): `IsUniformState = false`, `UniformStateCssClass
     = null`, `UniformStateIcon = null`, `UniformStateName = null`.
4. `CategorySegments` naplnit vždy (i pro uniformní blok, pro použití mimo
   vizualizaci není potřeba, ale zjednodušuje testování):
   ```csharp
   CategorySegments = items
       .Select(item => new TrainingScheduleCategorySegment
       {
           CategoryName = item.SeasonCategoryName,
           StateCssClass = TrainingStateVisual.Get(item.TrainingStateId)?.CssClass,
       })
       .ToList(),
   ```
5. Zachovat stávající `Title` (spojený text kategorií) beze změny — používá
   ho legenda (`TrainingScheduleComponentModel.LegendItems`) a tooltip.

### Fáze 4: Promítnutí do prezentačního modelu bloku

Upravit
`src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleComponentModel.cs`:

1. Do `TrainingScheduleBlock` přidat:
   ```csharp
   public required IReadOnlyList<TrainingScheduleCategorySegment> CategorySegments { get; init; }
   public bool IsUniformState { get; init; }
   public string? UniformStateCssClass { get; init; }
   public string? StateIcon { get; init; }
   ```
2. V `CreateBlock` namapovat z `TrainingScheduleBlockData`:
   ```csharp
   CategorySegments = block.CategorySegments,
   IsUniformState = block.IsUniformState,
   UniformStateCssClass = block.UniformStateCssClass,
   StateIcon = block.UniformStateIcon,
   ```
3. Tooltip (`CreateTooltip`) doplnit o stav, pokud je znám:
   ```csharp
   if (item.TrainingStateName is not null)
       parts.Insert(1, item.TrainingStateName);
   ```
   (za název kategorie, před typ tréninku — přesné umístění lze doladit v
   review, funkčně jde jen o čitelnost tooltipu).

### Fáze 5: Vykreslení v ViewComponent

Upravit
`src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`:

Nahradit:
```html
<span class="schedule-block-cat">@block.Title</span>
```
za:
```html
@if (block.IsUniformState)
{
    <span class="schedule-block-cat @block.UniformStateCssClass">@block.Title</span>
}
else
{
    <span class="schedule-block-cat">
        @for (var i = 0; i < block.CategorySegments.Count; i++)
        {
            if (i > 0)
            {
                <span>&nbsp;+&nbsp;</span>
            }
            <span class="@block.CategorySegments[i].StateCssClass">@block.CategorySegments[i].CategoryName</span>
        }
    </span>
}
```
a doplnit ikonu (pouze mimo `<a>`/`<div>` obsah, uvnitř bloku, poté co je
`position: relative`/`absolute` kontext bloku k dispozici — `.schedule-block`
už má `position: absolute`, což stačí jako kontext pro potomka):
```html
@if (!string.IsNullOrEmpty(block.StateIcon))
{
    <span class="schedule-block-state-icon" aria-hidden="true">@block.StateIcon</span>
}
```
Vložit shodně do obou variant bloku (`<a>` i `<div>`).

### Fáze 6: Styly

Upravit `src/SportSys.Razor/Styles/_vars.scss`:

1. Do bloku `:root` (light mód) i `[data-theme="dark"]` (a media-query dark
   fallback) doplnit sedm nových sémantických tokenů, odvozených z existující
   palety tak, aby byly vzájemně odlišitelné:
   ```scss
   --color-training-state-plan:         var(--color-info-text);
   --color-training-state-confirmed:    var(--color-success-text);
   --color-training-state-kis-only:     var(--color-chart-6);
   --color-training-state-time-change:  var(--color-warning-text);
   --color-training-state-cancelled:    var(--color-error-text);
   --color-training-state-date-change:  var(--color-chart-5);
   --color-training-state-zs-failure:   var(--color-accent-gold-text);
   ```
   (přesné páry v light/dark módu doladit vizuální kontrolou proti typickým
   barvám pozadí bloku `--color-chart-1..8` — jde o kosmetický detail bez
   dopadu na strukturu řešení.)

Upravit `src/SportSys.Razor/Styles/_schedule.scss`:

2. Za `.schedule-block-cat` doplnit modifikátory tříd:
   ```scss
   .schedule-block-cat {
     &.training-state-plan        { color: var(--color-training-state-plan); }
     &.training-state-confirmed   { color: var(--color-training-state-confirmed); }
     &.training-state-kis-only    { color: var(--color-training-state-kis-only); }
     &.training-state-time-change { color: var(--color-training-state-time-change); }
     &.training-state-cancelled   { color: var(--color-training-state-cancelled); }
     &.training-state-date-change { color: var(--color-training-state-date-change); }
     &.training-state-zs-failure  { color: var(--color-training-state-zs-failure); }
   }
   ```
3. Doplnit styl ikony:
   ```scss
   .schedule-block-state-icon {
     position:   absolute;
     right:      .2rem;
     bottom:     .1rem;
     font-size:  .625rem;
     line-height: 1;
     text-shadow: none;
   }
   ```
4. Po úpravě spustit `npm run build:css` v `src/SportSys.Razor` (viz Testy a
   ověření).

### Fáze 7: Ikona ve filtru stavů

Upravit
`src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml`:

V bloku filtru „Stavy tréninku“ (zavedeném v `#12`) doplnit ikonu před název:
```html
<label class="checkbox-label">
    <input type="checkbox"
           name="SelectedTrainingStateIds"
           value="@state.Id"
           checked="@Model.SelectedTrainingStateIds.Contains(state.Id)" />
    @(SportSys.Razor.Models.TrainingSchedule.TrainingStateVisual.Get(state.Id)?.Icon) @state.Name
</label>
```
(alternativně přidat `@using SportSys.Razor.Models.TrainingSchedule` na
začátek souboru a použít `TrainingStateVisual.Get(...)` bez plně
kvalifikovaného názvu).

### Fáze 8: Export do Excelu

Upravit `src/SportSys.Razor/Services/TrainingScheduleExcelExporter.cs`:

1. `WriteHeader`: přidat osmý sloupec:
   ```csharp
   worksheet.Cell(1, 8).Value = "Stav";
   ```
2. `WriteRow`: přidat
   ```csharp
   worksheet.Cell(rowNumber, 8).Value = block.UniformStateName ?? string.Empty;
   ```
3. Upravit rozsah tabulky a formátování ze 7 na 8 sloupců:
   ```csharp
   var tableRange = worksheet.Range(1, 1, rowNumber - 1, 8);
   ...
   worksheet.Columns(5, 8).Style.Alignment.WrapText = true;
   ```

### Fáze 9: Dokumentace

Upravit `docs/modules/sport.md`:

1. V sekci „Sdílená ViewComponent“ nebo „Filtry Schedule“ (řádky cca 65–133)
   doplnit odstavec popisující:
   - barevné a ikonové zobrazení stavu tréninku v bloku (samostatný trénink,
     spojené tréninky se stejným/rozdílným stavem),
   - že `/sport/Training/Plan` stav nezobrazuje (plán stav nemá).
2. V popisu exportu (řádky cca 136–143) doplnit informaci o novém sloupci
   „Stav“ a pravidlu prázdné hodnoty u rozdílných stavů.

## Soubory ke změně

- `src/SportSys.Contract/Models/TrainingScheduleDto.cs`
- `src/SportSys.Contract/Services/TrainingScheduleService.cs`
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingStateVisual.cs` (nový)
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleBlockData.cs`
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleComponentModel.cs`
- `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml`
- `src/SportSys.Razor/Services/TrainingScheduleExcelExporter.cs`
- `src/SportSys.Razor/Styles/_vars.scss`
- `src/SportSys.Razor/Styles/_schedule.scss`
- `docs/modules/sport.md`
- `tests/SportSys.Razor.Tests/TrainingScheduleBlockFactoryTests.cs`
- `tests/SportSys.Razor.Tests/TrainingScheduleComponentModelTests.cs`
- `tests/SportSys.Razor.Tests/TrainingScheduleExcelExporterTests.cs`

## Testy a ověření

Doplnit jednotkové testy (xUnit, existující konvence testovacích helperů
`CreateTraining`):

1. `TrainingScheduleBlockFactoryTests`:
   - samostatný trénink se stavem → `IsUniformState == true`,
     `UniformStateCssClass`/`UniformStateName` odpovídají stavu.
   - dva spojené tréninky se stejným stavem → `IsUniformState == true`.
   - dva spojené tréninky s rozdílným stavem → `IsUniformState == false`,
     `CategorySegments` mají odlišné `StateCssClass` podle pořadí kategorií.
   - blok bez stavu (simulace plánu, `TrainingStateId == null` u všech
     položek) → `IsUniformState == true`, `UniformStateCssClass == null`.
2. `TrainingScheduleComponentModelTests`: ověřit, že `StateIcon` a
   `UniformStateCssClass`/`CategorySegments` se správně přenesou do
   `TrainingScheduleBlock`.
3. `TrainingScheduleExcelExporterTests`:
   - jeden trénink se stavem → sloupec 8 obsahuje název stavu.
   - spojené tréninky se stejným stavem → sloupec 8 obsahuje název stavu.
   - spojené tréninky s rozdílným stavem → sloupec 8 je prázdný.
   - hlavička obsahuje `"Stav"` jako osmý sloupec.

Spustit:

```powershell
dotnet build SportSys.slnx
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
```

```powershell
Set-Location src\SportSys.Razor
npm run build:css
```

## Manuální akceptace

1. Na stránce `/sport/Training/Schedule` má samostatný trénink barevný název
   kategorie odpovídající jeho stavu a ikonu vpravo dole v bloku.
2. Spojené tréninky se stejným stavem: celý text (kategorie i „+“) jednou
   barvou, ikona zobrazena.
3. Spojené tréninky s rozdílným stavem: každá kategorie vlastní barvou, „+“
   standardní barvou, ikona chybí.
4. Filtr „Stavy tréninku“ zobrazuje ikonu před názvem každého stavu.
5. Export do Excelu obsahuje sloupec „Stav“; u jednoznačného stavu je
   vyplněný text, u rozdílných stavů je buňka prázdná.
6. Stránka `/sport/Training/Plan` vypadá beze změny (žádná barva/ikona
   stavu).
7. Filtrování podle kategorií a stavů, spojování tréninků, detekce překryvů a
   editace tréninků/plánů fungují beze změny.
8. Vizuální kontrast textu stavu je čitelný na typických barvách pozadí bloku
   (`--color-chart-1..8`) v light i dark módu.

## Beze změny

- Databázový model, `TrainingStateConfiguration`, seed dat, EF Core migrace.
- Logika spojování tréninků/plánů (`ApplyVisualizationGrouping`,
  `TrainingScheduleBlockFactory` seskupovací pravidla) a detekce překryvů.
- Filtrování podle kategorií, typu tréninku, lokality a stavu (`#12`).
- Načítání dat z databáze mimo doplnění dvou nových sloupců do projekce.
- Editace tréninku a tréninkového plánu.
- Stránka `/sport/Training/Plan` — vizuálně i funkčně beze změny.

## Mimo rozsah

- Nahrazení emoji ikon jednotným ikonovým fontem (FontAwesome) používaným
  jinde v aplikaci — issue to výslovně připouští jako budoucí úpravu.
- Finální doladění přesných barevných odstínů nad rámec zajištění vzájemné
  odlišitelnosti sedmi stavů — ponecháno na code review / UI kontrole.
- Jakákoliv úprava číselníku `TrainingState` nebo `ETrainingState`.

## Hotovo, když

- Blok rozvrhu zobrazuje barvu a ikonu stavu podle pravidel pro samostatný i
  spojený trénink (stejný/rozdílný stav).
- Filtr stavů zobrazuje ikonu před názvem.
- Export do Excelu obsahuje sloupec „Stav“ s korektním chováním u
  jednoznačného i rozdílného stavu spojených tréninků.
- Stránka `/sport/Training/Plan` je vizuálně i funkčně beze změny.
- Nové a upravené jednotkové testy prochází, `dotnet build SportSys.slnx`
  proběhne bez chyb.
