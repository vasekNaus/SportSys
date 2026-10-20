# Implementační plán: #13 Úprava vizualizace stavů tréninků (revize)

**Issue:** [#13 — Vizualizace stavů tréninků v rozvrhu a exportech](https://github.com/vasekNaus/SportSys/issues/13)

**Stav:** Připraveno k implementaci.

## Cíl

Revidovat již implementovanou vizualizaci stavů tréninků (viz
`.github/tasks/13-vizualizace-stavu-treninku.md`, implementováno) podle
novějšího komentáře na issue, který ruší barevné rozlišení kategorií podle
stavu a nahrazuje jej výhradně stavovou ikonou v pravém dolním rohu bloku,
doplněnou o nový symbol „Stav neznámý“ pro rozdílné stavy spojených tréninků
a tooltip s rozpisem stavů. Export do Excelu se touto revizí nemění.

Tento plán je **doplňkem**, ne náhradou původního plánu. Popisuje pouze delta
oproti již odevzdanému stavu kódu.

## Výchozí stav (již implementováno)

- `src/SportSys.Razor/Models/TrainingSchedule/TrainingStateVisual.cs` mapuje
  `TrainingStateId` (1–7) na dvojici `CssClass` + `Icon`.
- `TrainingScheduleBlockData`/`TrainingScheduleBlockFactory`
  (`src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleBlockData.cs`)
  počítají `IsUniformState`, `UniformStateCssClass`, `UniformStateIcon`,
  `UniformStateName` a `CategorySegments` (každý segment nese
  `CategoryName` + `StateCssClass`).
- `TrainingScheduleComponentModel.CreateBlock` mapuje tato data do
  `TrainingScheduleBlock.CategorySegments`, `IsUniformState`,
  `UniformStateCssClass`, `StateIcon`.
- `Default.cshtml` vykresluje název kategorie obarvený podle
  `UniformStateCssClass`, nebo (u rozdílných stavů) každý segment zvlášť podle
  jeho vlastní CSS třídy, a ikonu stavu v `<span class="schedule-block-state-icon">`.
- `_vars.scss` obsahuje 7 tokenů `--color-training-state-*` (light/dark/media
  fallback), `_schedule.scss` obsahuje selektory `.schedule-block-cat.training-state-*`
  a `.schedule-block-state-icon`.
- `AllowEditing` na `ITrainingScheduleViewModel` je `false`, právě když je
  aktivní GET filtr **Spojovat tréninky** (`MergeTrainings`) — na stránkách
  `sport/Training/Schedule/Index` i `sport/Training/Plan/Index`
  (`allowEditing: !MergeTrainings`). Blok tehdy vzniká spojením podle
  časového překryvu (`VisualizationGroupId`), ne podle databázové vazby.
- Export (`TrainingScheduleExcelExporter`) i filtr stavů (ikona před názvem ve
  `sport/Training/Schedule/Index.cshtml`) zůstávají beze změny — komentář je
  nemění.
- `TrainingPlan` nemá vazbu na stav (`TrainingStateId` je u
  `TrainingPlanScheduleItemDto` vždy `null`), takže na stránce Plan se ikona
  dnes nezobrazuje vůbec — to je žádoucí i po revizi.

## Potvrzené požadavky a rozhodnutí (z komentáře)

1. Zrušit CSS třídy `.training-state-*` a jakékoliv barevné rozlišení názvů
   kategorií podle stavu — kategorie se vždy zobrazují jednotným stylem
   (aktuální `.schedule-block-cat` bez modifikátoru).
2. Zachovat stavovou ikonu v pravém dolním rohu bloku.
3. Přidat nový stav „Stav neznámý“ s ikonou ❓, použitý výhradně jako
   zástupný symbol pro blok s rozdílnými stavy (není to skutečná hodnota
   číselníku `TrainingState`, jde o UI sentinel).
4. Samostatný trénink: ikona odpovídá jeho stavu, název kategorie bez barvy.
5. Spojené tréninky se shodným stavem: ikona odpovídá společnému stavu, název
   kategorií (včetně „+“) bez barvy.
6. Spojené tréninky s rozdílnými stavy: zobrazí se ikona ❓, název kategorií
   bez barvy, po najetí na ikonu ❓ tooltip s rozpisem `ikona stavu + název
   kategorie` pro každý dílčí trénink, v pořadí podle `SeasonCategory.Order`.
7. Při aktivním filtru **Spojovat tréninky** (`AllowEditing == false`) se
   ikona stavu (včetně ❓) ani tooltip rozpisu nezobrazují vůbec — blok
   reprezentuje jen časový interval. Toto platí shodně na stránce rozvrhu i
   plánu.
8. Export do Excelu, filtr stavů a jeho ikony, řazení kategorií, spojování
   podle databázových vazeb, filtrování a načítání dat se nesmí změnit.

## Otevřené otázky

Žádné zásadní — chování je komentářem jednoznačně popsáno. Drobné technické
rozhodnutí (viz níže) řeším podle existujících vzorů bez nutnosti dotazu.

- **Umístění sentinelu ❓**: `TrainingStateVisual` je dnes čistě
  `Id → (CssClass, Icon)` slovník indexovaný reálnými hodnotami číselníku.
  „Stav neznámý“ nemá `TrainingStateId`, proto se přidá jako samostatná
  konstanta (např. `TrainingStateVisual.UnknownIcon`), ne jako položka
  slovníku.
- **Tooltip ikony vs. tooltip bloku**: aktuální `block.Tooltip` (atribut
  `title` na celém `<a>`/`<div>`) už obsahuje `TrainingStateName` u každé
  položky (viz `CreateTooltip`) a zůstává beze změny. Nový tooltip je
  samostatný `title` atribut jen na `<span class="schedule-block-state-icon">`,
  aktivní pouze v případě rozdílných stavů.

## Technický návrh

### CSS třídy a proměnné — odstranit

- `src/SportSys.Razor/Styles/_vars.scss`: odstranit všech 7 výskytů bloku
  `--color-training-state-*` ve všech třech sekcích (light `:root`, dark
  `[data-theme="dark"]`, `@media (prefers-color-scheme: dark)` fallback).
- `src/SportSys.Razor/Styles/_schedule.scss`: odstranit vnořené selektory
  `&.training-state-plan` … `&.training-state-zs-failure` z pravidla
  `.schedule-block-cat`. Pravidlo `.schedule-block-state-icon` zůstává beze
  změny (ikona se zachovává).

### `TrainingStateVisual.cs`

- Přidat `public const string UnknownIcon = "❓";` (sentinel pro rozdílné
  stavy, mimo slovník `ById`).
- Slovník `ById` a `Get(int?)` zůstávají beze změny (mapování reálných
  `TrainingStateId` na ikonu se dál používá pro shodný/jediný stav).

### `TrainingScheduleBlockData.cs` / `TrainingScheduleBlockFactory`

- `TrainingScheduleCategorySegment`: odstranit `StateCssClass`, přidat
  `StateIcon` (ikona konkrétní položky, pro použití v tooltipu rozpisu).
- V `CreateBlock`:
  - `isUniformState` beze změny (shoda existuje jen tehdy, když všechny
    položky mají stav a jsou shodné).
  - `hasAnyState` beze změny.
  - Nové odvozené pole na `TrainingScheduleBlockData`:
    `HasMixedState` = `hasAnyState && !isUniformState` (alespoň jeden stav a
    zároveň nejde o shodu) — použije se k volbě ikony ❓ a k povolení
    tooltipu rozpisu.
  - `UniformStateIcon`/`UniformStateCssClass`/`UniformStateName` beze změny
    (počítají se jen když `isUniformState`).
  - `CategorySegments` nově plní `StateIcon = TrainingStateVisual.Get(item.TrainingStateId)?.Icon`
    místo `StateCssClass`, pořadí segmentů zůstává podle `SeasonCategoryOrder`
    (položky `items` jsou už seřazené tímto klíčem na začátku metody).
- `IsUniformState` field na `TrainingScheduleBlockData` zůstává (řídí, zda se
  `Title` vykresluje jako jeden span, nebo — nově — vždy jako jeden span,
  protože barvení mizí; viz níže úprava view).

### `TrainingScheduleComponentModel.cs`

- `TrainingScheduleBlock`:
  - Odstranit `UniformStateCssClass` (barva se dál nepoužívá).
  - `StateIcon` nově vypočítat takto:
    - pokud `block.HasMixedState` → `TrainingStateVisual.UnknownIcon`,
    - jinak (uniform, včetně žádného stavu) → `block.UniformStateIcon`
      (může být `null`, když stav chybí, např. u Plánu).
  - Přidat `StateTooltip` (`string?`) — vyplněný jen když
    `block.HasMixedState`, obsahující řádky `"{icon} {categoryName}"` spojené
    `\n` (nebo `Environment.NewLine`) z `block.CategorySegments`, v pořadí,
    v jakém jsou v `CategorySegments` (již seřazeno podle kategorie).
  - `CategorySegments` property může zůstat pro potřeby `StateTooltip`
    (bez `StateCssClass`, s `StateIcon`).
- `CreateBlock`: nahradit řádek
  `UniformStateCssClass = block.UniformStateCssClass,`
  výpočtem `StateIcon`/`StateTooltip` popsaným výše.
- `CreateTooltip` (tooltip celého bloku) zůstává beze změny — `TrainingStateName`
  v hlavním tooltipu se zachovává, revize se týká jen ikony a barvy.

### `Default.cshtml`

- V obou variantách bloku (`<a>` i `<div>`):
  - Vždy vykreslit `<span class="schedule-block-cat">@block.Title</span>`
    (bez podmínky na `IsUniformState`, bez CSS modifikátoru, bez smyčky přes
    `CategorySegments`).
  - Ikonu vykreslit **pouze když** `Model.AllowEditing` je `true` **a**
    `!string.IsNullOrEmpty(block.StateIcon)`:
    ```cshtml
    @if (Model.AllowEditing && !string.IsNullOrEmpty(block.StateIcon))
    {
        <span class="schedule-block-state-icon"
              aria-hidden="@(block.StateTooltip is null ? "true" : "false")"
              title="@block.StateTooltip">@block.StateIcon</span>
    }
    ```
    `title` atribut se vypíše jen pro ❓ stav (jinak `null`/prázdný, Razor
    nevykreslí atribut). `aria-hidden` zůstává `true` pro čistě dekorativní
    ikonu jediného/shodného stavu; pro ❓ s tooltipem nastavit `false`, aby byl
    obsah dostupný accessibility nástrojům.
  - Odstranit dřívější `@if (block.IsUniformState) { ... } else { smyčka
    přes CategorySegments }` větvení kolem `schedule-block-cat`.

### Beze změny (ověřit, že revize nic z toho neporuší)

- `TrainingScheduleService.GetTrainingsAsync` a projekce
  `TrainingStateId`/`TrainingStateName`.
- Filtr stavů a jeho ikony na `sport/Training/Schedule/Index.cshtml`
  (`TrainingStateVisual.Get(state.Id)?.Icon`).
- `TrainingScheduleExcelExporter` a sloupec „Stav“.
- Spojování podle `sport.TrainingGroup`/`sport.TrainingPlanGroup` a logika
  `VisualizationGroupId`.
- Filtrování podle kategorií a stavů, řazení kategorií.

## Implementační kroky

### Fáze 1: Odstranění barevného rozlišení (SCSS)

1. `_vars.scss` — odstranit všech 21 řádků `--color-training-state-*` (7 × 3
   sekce).
2. `_schedule.scss` — odstranit 7 vnořených selektorů
   `.schedule-block-cat.training-state-*`.

### Fáze 2: Sentinel „Stav neznámý“

3. `TrainingStateVisual.cs` — přidat `UnknownIcon` konstantu s komentářem, že
   jde o sentinel mimo číselník `TrainingState`.

### Fáze 3: Aggregace v `TrainingScheduleBlockData`

4. `TrainingScheduleCategorySegment` — nahradit `StateCssClass` polem
   `StateIcon`.
5. `TrainingScheduleBlockData` — přidat `HasMixedState`.
6. `TrainingScheduleBlockFactory.CreateBlock` — dopočítat `HasMixedState`,
   naplnit `CategorySegments[].StateIcon` místo `StateCssClass`.

### Fáze 4: Prezentační model

7. `TrainingScheduleComponentModel.TrainingScheduleBlock` — odstranit
   `UniformStateCssClass`, přidat `StateTooltip`, upravit výpočet `StateIcon`
   podle `HasMixedState`/`UniformStateIcon`.
8. `CreateBlock` v `TrainingScheduleComponentModel` — použít nové výpočty.

### Fáze 5: View

9. `Default.cshtml` — sjednotit vykreslení `schedule-block-cat` (vždy jeden
   span, bez CSS modifikátoru a bez smyčky), podmínit zobrazení ikony
   hodnotou `Model.AllowEditing`, doplnit `title`/`aria-hidden` podle
   `StateTooltip`.

### Fáze 6: Testy

10. `tests/SportSys.Razor.Tests/TrainingScheduleBlockFactoryTests.cs` —
    upravit/doplnit case: shodný stav (ikona), rozdílný stav
    (`HasMixedState = true`, `CategorySegments[i].StateIcon` odpovídá
    jednotlivým položkám), žádný stav (Plán, `HasMixedState = false`,
    `UniformStateIcon = null`). Odstranit asserty na zaniklý `StateCssClass`.
11. `tests/SportSys.Razor.Tests/TrainingScheduleComponentModelTests.cs` —
    upravit `Create_MapsUniformStateCssClassAndIconToBlock` (přejmenovat,
    odstranit assert na `UniformStateCssClass`, ponechat/ověřit `StateIcon`).
    Upravit `Create_MixedStatesDisableUniformFlagAndIcon` na nové očekávání:
    `block.StateIcon == TrainingStateVisual.UnknownIcon` a
    `block.StateTooltip` obsahuje očekávaný rozpis (`"📅 U14"`, `"❌ U12"`
    apod., v pořadí kategorií). Doplnit test ověřující, že při
    `allowEditing: false` (simulace `MergeTrainings`) view model i tak vrací
    `StateIcon`/`StateTooltip` (potlačení ikony řeší view, ne model — nebo
    zvolit opačně, viz rozhodnutí níže) — **rozhodnutí**: potlačení ponechat
    výhradně v `Default.cshtml` přes `Model.AllowEditing`, model tedy vždy
    vrací spočtenou hodnotu; test na modelu tedy jen ověří výpočet ikony,
    ne potlačení.
12. `TrainingScheduleExcelExporterTests.cs` — beze změny (export se
    nerevidoval).

### Fáze 7: Dokumentace

13. `docs/modules/sport.md` — přepsat odstavec zavedený předchozí
    implementací (aktuálně popisuje barevné rozlišení a `TrainingStateVisual`)
    tak, aby popisoval:
    - kategorie se nikdy nebarví,
    - ikona stavu v pravém dolním rohu pro jediný/shodný stav,
    - ikona ❓ + tooltip s rozpisem `ikona + kategorie` pro rozdílné stavy,
    - potlačení ikony a tooltipu při aktivním filtru **Spojovat tréninky** —
      shodně na Schedule i Plan.
    - Ponechat větu o exportu (sloupec „Stav“) beze změny obsahu.

## Soubory ke změně

- `src/SportSys.Razor/Styles/_vars.scss`
- `src/SportSys.Razor/Styles/_schedule.scss`
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingStateVisual.cs`
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleBlockData.cs`
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleComponentModel.cs`
- `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`
- `tests/SportSys.Razor.Tests/TrainingScheduleBlockFactoryTests.cs`
- `tests/SportSys.Razor.Tests/TrainingScheduleComponentModelTests.cs`
- `docs/modules/sport.md`

## Testy a ověření

- `dotnet build SportSys.slnx` — čistý build (SCSS se přebuilduje automaticky
  jako součást `SportSys.Razor` buildu).
- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`
  — všechny testy zelené, včetně upravených/nových případů pro `HasMixedState`,
  `StateIcon`, `StateTooltip`.
- Ruční kontrola vykresleného `site.css` (`npm run build:css` v
  `src/SportSys.Razor`), že `--color-training-state-*` a
  `.training-state-*` selektory zmizely.

## Manuální akceptace

1. Rozvrh, `MergeTrainings` vypnuto, samostatný trénink → název kategorie bez
   barvy, ikona odpovídající stavu vpravo dole, bez tooltipu na ikoně.
2. Rozvrh, spojené tréninky se shodným stavem → jeden název (`A + B`), jedna
   ikona stavu, bez tooltipu na ikoně.
3. Rozvrh, spojené tréninky s rozdílnými stavy → ikona ❓, po najetí myší
   tooltip s řádky `ikona kategorie` v pořadí `SeasonCategory.Order`.
4. Rozvrh, `MergeTrainings` zapnuto → žádná ikona ani tooltip na žádném bloku,
   bez ohledu na to, zda by dílčí tréninky měly shodný nebo rozdílný stav.
5. Plán (`/sport/Training/Plan`) → beze změny, žádná ikona (plán stav nemá).
6. Export do Excelu → sloupec „Stav“ se chová stejně jako dosud
   (nezměněno touto revizí).
7. Filtr stavů na stránce rozvrhu → ikony před názvem stavu beze změny.

## Beze změny

- Datový tok `TrainingStateId`/`TrainingStateName` z `TrainingScheduleService`
  do DTO.
- Export do Excelu (`TrainingScheduleExcelExporter`) a jeho testy.
- Filtr stavů (`#12`) a zobrazení ikon ve filtru.
- Spojování tréninků/plánů podle databázových vazeb i podle časového překryvu
  (`VisualizationGroupId`).
- Řazení kategorií, editace tréninků/plánů, detekce překryvů.

## Mimo rozsah

- Nahrazení emoji ikon skutečnými ikonami aplikace (issue to výslovně
  ponechává na budoucí UI revizi).
- Jakákoliv změna číselníku `TrainingState` nebo EF Core modelu/migrace.
- Přidání stavu k `TrainingPlan`.

## Hotovo, když

- [ ] Kategorie se v bloku rozvrhu i plánu nikdy nebarví podle stavu.
- [ ] CSS třídy `.training-state-*` a proměnné `--color-training-state-*`
      jsou odstraněny.
- [ ] Samostatný trénink zobrazuje ikonu svého stavu.
- [ ] Spojené tréninky se shodným stavem zobrazují jednu ikonu tohoto stavu.
- [ ] Spojené tréninky s rozdílnými stavy zobrazují ikonu ❓ s tooltipem
      obsahujícím `ikona + kategorie` pro každou dílčí položku, v pořadí
      kategorie.
- [ ] Při aktivním filtru „Spojovat tréninky“ se na Schedule i Plan
      nezobrazuje žádná stavová ikona ani tooltip.
- [ ] Export do Excelu, filtr stavů a jejich ikony zůstávají nezměněny.
- [ ] `dotnet build` a `dotnet test tests\SportSys.Razor.Tests\...` bez chyb.
- [ ] `docs/modules/sport.md` popisuje aktuální (revidované) chování.
