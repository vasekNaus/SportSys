# Implementační plán: #24 Tréninkové plány – tabulkový přehled

**Issue:** [#24 — Tréninkové plány – tabulkový přehled](https://github.com/vasekNaus/SportSys/issues/24)

**Stav:** Připraveno k implementaci.

## Cíl

Na stránku **Tréninkové plány**
(`src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml`) doplnit pod
stávající grafický týdenní rozvrh tabulku s detailním přehledem jednotlivých
záznamů `sport.TrainingPlan`, bez jakékoli agregace — jeden záznam
`TrainingPlan` = jeden řádek tabulky, i při zapnuté volbě **Spojovat
tréninky**.

## Výchozí stav

- Stránka `IndexModel.OnGetAsync` (`Index.cshtml.cs`) již načítá přesně ta
  data, která tabulka potřebuje, přes
  `TrainingScheduleService.GetTrainingPlansAsync(seasonId, categories,
  trainingTypeIds, locationIds, trainingPhaseId, validOn, mergeOverlapping,
  ct)`, který vrací `List<TrainingPlanScheduleItemDto>`
  (`src/SportSys.Contract/Models/TrainingScheduleDto.cs:44-80`).
- Výsledek je následně filtrován podle dne:
  `plans = WeekDayNames.FilterByDay(plans, plan => plan.DayOfWeek,
  SelectedDaysOfWeek);` (`Index.cshtml.cs`). Tento krok **již** zohledňuje
  všechny filtry požadované v issue (Sezóna, Typ tréninku, Lokalita, Fáze
  tréninku, Datum platnosti, Den, Kategorie) — žádná úprava filtrační logiky
  není potřeba.
- `mergeOverlapping` (parametr `MergeTrainings` z UI) ovlivňuje pouze
  `TrainingScheduleService.ApplyVisualizationGrouping`, které nastavuje
  `VisualizationGroupId` na položkách **beze změny jejich počtu** — `plans`
  zůstává list jednotlivých `TrainingPlan` záznamů bez agregace. To znamená,
  že proměnná `plans` v `IndexModel.OnGetAsync` je přesně ten zdroj dat, který
  tabulka potřebuje — není nutné volat službu znovu ani duplikovat dotaz.
- Agregace do méně řádků nastává až později, uvnitř
  `EventModelFactory.CreateTrainingPlans(...)`, které slouží výhradně pro
  vykreslení grafických bloků (`byDay` / `TrainingScheduleRow.Items`) — tabulka
  s tímto krokem nesmí pracovat.
- `TrainingPlanScheduleItemDto` (`TrainingScheduleDto.cs:44-80`) obsahuje vše
  požadované kromě názvu sezóny:
  `SeasonCategoryName`, `TrainingPhaseName`, `From`, `To`, `Title`, `DayName`
  (+ odvozené `DayOfWeek`), `TimeFrom`, `TimeTo`,
  `Coaches : IReadOnlyList<SimpleCoachDto>` (`FullName`, `LastName`).
- Stránka podporuje výběr **jediné** sezóny najednou (`SeasonId : int?`), takže
  sloupec „Sezóna“ bude pro všechny řádky tabulky konstantní — její název lze
  dohledat z již načteného `Seasons : List<SeasonDto>` podle `SeasonId`, bez
  úpravy service vrstvy.
- `sport.TrainingPlan.To` je v aktuálním modelu
  (`src/SportSys.Database/Models/sport/TrainingPlan.cs:30`) nepovinné pole typu
  `DateOnly` (non-nullable) — sloupec **Do** tedy bude v praxi vždy vyplněný;
  pravidlo issue „pokud `Do` není vyplněno, bude prázdné“ nemá v současném
  schématu reálný případ výskytu a nevyžaduje žádnou speciální větev kódu.
- Existující konvence pro zobrazení trenérů v kompaktní podobě (grafický
  rozvrh) používá jen příjmení (`SimpleCoachDto.LastName`, viz
  `_ScheduleBlockContent.cshtml:training.CoachSurnameSummary`). V tabulce je
  ale dost místa, proto zobrazíme celé jméno — podle upřesnění uživatele se
  použije přímo existující `SimpleCoachDto.FullName` (uložené jako
  `User.DisplayName`, tj. „Jméno Příjmení“) beze změny formátu; žádná nová
  vlastnost na DTO se nepřidává, požadovaný formát „Příjmení Jméno“ si
  zajistí uživatel sám mimo tento plán.
- Editace řádku: `Edit.cshtml.cs` v `Areas/sport/Pages/Training/Plan/` má
  `OnGetAsync(int id, ...)` — standardní `asp-page="./Edit"
  asp-route-id="@row.Id"`. Protože tabulka vždy zobrazuje konkrétní
  `TrainingPlan.Id` (nikdy sloučenou vizuální skupinu), editační odkaz lze
  vždy směřovat na `row.Id` bez ohledu na stav „Spojovat tréninky“ — na
  rozdíl od grafických bloků (`EventModelFactory`, `allowEditing:
  !MergeTrainings`), kde `MergeTrainings` může spojit více plánů do jednoho
  bloku a edit tam proto musí být potlačen.
- Konvence pro editační ikonu v existujících tabulkách (`grid`):
  `<a class="button secondary icon-btn" asp-page="..." asp-route-id="..."
  title="Upravit"><i class="fa-solid fa-pen fa-fw"></i></a>`, viz
  `Areas/sport/Pages/Season/Index.cshtml`.
- Datum ve formátu `dd.MM.yyyy` jako explicitní (kulturně nezávislý) formátovací
  řetězec je již zavedenou konvencí v
  `TrainingScheduleExcelExporter.cs:34` (`DateFormat.Format = "dd.MM.yyyy"`).
- Existující tabulkové přehledy v aplikaci používají `<table class="grid">`
  (např. `src/SportSys.Razor/Areas/sport/Pages/Season/Index.cshtml`).
- Legenda i grafický rozvrh se vykreslují společně uvnitř jedné komponenty
  `@await Component.InvokeAsync("TrainingSchedule", new { model =
  Model.ScheduleView })` (`TrainingScheduleViewComponent.cs` →
  `Pages/Shared/Components/TrainingSchedule/Default.cshtml`, který obsahuje
  jak `schedule-legend`, tak `schedule-timeline`). Nová tabulka proto patří
  bezprostředně **za** toto volání komponenty v `Index.cshtml` — tím je
  zachováno požadované pořadí „filtry → legenda → grafický rozvrh →
  tabulka“.

## Potvrzené požadavky a rozhodnutí

- Jeden řádek tabulky = jeden záznam `sport.TrainingPlan`; žádná agregace podle
  kategorie, dne, času ani názvu; více trenérů v jedné buňce, bez násobení
  řádků (potvrzeno v issue).
- Sloupce a jejich pořadí: Sezóna, Kategorie, Fáze, Od, Do, Název, Den v
  týdnu, Čas od, Čas do, Trenéři (potvrzeno v issue), plus nesloupcová
  editační ikona jako poslední sloupec tabulky (doplněno uživatelem nad rámec
  původního issue).
- Formáty: datum `dd.MM.yyyy`, čas `HH:mm`, trenéři oddělení čárkou `, `
  (potvrzeno v issue). Formát času `HH:mm` je zde explicitně požadován issue a
  bude použit přesně takto, i když grafický blok rozvrhu (jiné místo UI)
  používá kompaktnější `H:mm` — jde o samostatný, nezávislý zobrazovací kontext.
- Trenéři se v tabulce zobrazí celým jménem pomocí existujícího
  `SimpleCoachDto.FullName` (bez úpravy pořadí jméno/příjmení — to si
  zajistí uživatel mimo rozsah tohoto plánu; nepřidává se žádná nová
  vlastnost DTO).
- Poslední sloupec tabulky obsahuje editační ikonu (tužka), která otevře
  editaci konkrétního záznamu `TrainingPlan` (`Edit.cshtml` ve stejné
  složce) — doplněno uživatelem.
- Tabulka respektuje stejné filtry jako grafický rozvrh a volba „Spojovat
  tréninky“ nesmí měnit počet řádků tabulky (potvrzeno v issue) — zajištěno
  tím, že tabulka čerpá ze stejného, dosud needitovaného (před
  `EventModelFactory`) seznamu `plans`.
- Řazení řádků tabulky není v issue specifikováno. Zvoleno řazení shodné s
  pořadím, v jakém `plans` přicházejí ze služby — podle dne v týdnu, poté
  podle času začátku, poté podle platnosti od (`TrainingScheduleService.
  GetTrainingPlansAsync` již řadí `OrderBy(DayOfWeek).ThenBy(TimeFrom).
  ThenBy(From)`) — konzistentní s pořadím bloků v grafickém rozvrhu nad
  tabulkou.
- Tabulka se zobrazí jen tehdy, je-li vybrána sezóna a existují-li nějaké
  položky `plans` (stejná podmínka jako pro vykreslení `ScheduleView`); při
  prázdném výsledku zůstává stávající hláška „Pro zadané parametry nebyly
  nalezeny žádné tréninkové plány.“ dostatečná a tabulka se nevykresluje.

## Technický návrh

1. `IndexModel` (`Index.cshtml.cs`) rozšíří svůj stav o:
   - `public IReadOnlyList<TrainingPlanScheduleItemDto> PlanRows { get;
     private set; } = [];` — nastaví se na filtrovaný `plans` list (po
     `WeekDayNames.FilterByDay`), tedy **před** voláním
     `EventModelFactory.CreateTrainingPlans`.
   - `public string? SelectedSeasonName { get; private set; }` — dopočítá se
     z `Seasons.FirstOrDefault(s => s.Id == SeasonId)?.Name` ve chvíli, kdy je
     `SeasonId` známé a platné.
2. Nový partial `_TrainingPlanTable.cshtml` ve stejné složce
   (`Areas/sport/Pages/Training/Plan/`), s modelem `IndexModel` (stejně jako
   zbytek stránky), vykreslí `<table class="grid">` se sloupci v přesném
   pořadí: Sezóna, Kategorie, Fáze, Od, Do, Název, Den v týdnu, Čas od, Čas
   do, Trenéři, a nepojmenovaným posledním sloupcem s editační ikonou
   (stejná konvence jako `Season/Index.cshtml`: `<th></th>` v hlavičce, bez
   popisku).
   - Sezóna: `@Model.SelectedSeasonName`
   - Kategorie: `@row.SeasonCategoryName`
   - Fáze: `@row.TrainingPhaseName`
   - Od / Do: `@row.From.ToString("dd.MM.yyyy")` /
     `@row.To.ToString("dd.MM.yyyy")`
   - Název: `@row.Title`
   - Den v týdnu: `@WeekDayNames.GetFullName(row.DayOfWeek)`
   - Čas od / Čas do: `@row.TimeFrom.ToString("HH:mm")` /
     `@row.TimeTo.ToString("HH:mm")`
   - Trenéři: `@string.Join(", ", row.Coaches.Select(c => c.FullName))` —
     použije se existující `SimpleCoachDto.FullName` beze změny formátu
     (žádné nové DTO ani vlastnost).
   - Editace: buňka `class="buttons"` s odkazem
     `<a class="button secondary icon-btn" asp-page="./Edit"
     asp-route-id="@row.Id" title="Upravit"><i class="fa-solid fa-pen
     fa-fw"></i></a>` — stejná konvence jako v `Season/Index.cshtml`;
     `Edit.cshtml.cs` má `OnGetAsync(int id, ...)`, takže `row.Id`
     (`TrainingPlanScheduleItemDto.Id`) je přímo platný parametr.
3. `Index.cshtml` vloží partial bezprostředně za blok
   `@await Component.InvokeAsync("TrainingSchedule", ...)`, podmíněně jen
   pokud `Model.ScheduleView is not null && Model.PlanRows.Count > 0`.
4. Žádná změna `TrainingScheduleService`, DTO modelů, databázového schématu
   ani migrace není potřeba — všechna potřebná data už existují a jsou
   natažena jediným stávajícím dotazem.

## Implementační kroky

### Fáze 1: PageModel

- V `Index.cshtml.cs` přidat vlastnosti `PlanRows` a `SelectedSeasonName`,
  nastavit je ve správném pořadí (viz Technický návrh bod 1) tak, aby
  `PlanRows` odpovídal přesně tomu `plans`, který se již používá pro
  `EventModelFactory.CreateTrainingPlans`, ale **před** touto transformací.

### Fáze 2: `SimpleCoachDto.SurnameFirstDisplayName`

- Přidat počítanou vlastnost dle návrhu výše do
  `src/SportSys.Contract/Models/TrainingScheduleDto.cs`.

### Fáze 2: Zobrazení tabulky

- Vytvořit `_TrainingPlanTable.cshtml` dle návrhu výše, včetně editačního
  sloupce.
- V `Index.cshtml` vložit partial za grafický rozvrh s podmínkou na
  `Model.PlanRows.Count > 0`.

### Fáze 3: Ověření

- Ruční ověření scénářů z issue (viz Manuální akceptace) a případný
  jednotkový test na úrovni PageModelu / partial modelu.

## Soubory ke změně

- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs` —
  přidat `PlanRows`, `SelectedSeasonName`.
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml` — vložit
  nový partial pod komponentu rozvrhu.
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/_TrainingPlanTable.cshtml`
  (nový soubor) — tabulka s detailním přehledem a editační ikonou.

## Testy a ověření

- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c
  Release` po implementaci.
- Doporučený nový test (např. v `tests/SportSys.Razor.Tests/`), který ověří,
  že `PlanRows` obsahuje stejný počet položek jako výsledek
  `GetTrainingPlansAsync` + `FilterByDay`, a to i při `MergeTrainings =
  true` (tj. že zapnutí spojování nemění počet řádků `PlanRows`, i když mění
  `VisualizationGroupId` použité jen pro grafiku).

## Manuální akceptace

1. Na stránce Tréninkové plány vybrat sezónu — pod grafickým rozvrhem se
   zobrazí tabulka se správnými sloupci v požadovaném pořadí.
2. Dva časově i kategoricky odlišné plány ve stejný den a čas zůstanou jako
   dva samostatné řádky i při zapnuté volbě „Spojovat tréninky“.
3. Tréninkový plán s více přiřazenými trenéry se zobrazí jako jeden řádek,
   trenéři oddělení čárkou v jedné buňce, celým jménem (`FullName`).
4. Změna libovolného filtru (Sezóna, Typ tréninku, Lokalita, Fáze, Datum
   platnosti, Den, Kategorie) se promítne shodně do grafického rozvrhu i do
   tabulky.
5. Datum je ve formátu `dd.MM.yyyy`, čas ve formátu `HH:mm`.
6. Kliknutí na editační ikonu v posledním sloupci řádku otevře editaci
   správného záznamu `TrainingPlan` (`/Training/Plan/Edit?id=...`).

## Beze změny

- `TrainingScheduleService`, `TrainingPlanScheduleItemDto` a filtrovací logika
  (`GetTrainingPlansAsync`, `ApplyValidityFilter`, `ApplyVisualizationGrouping`).
- `SimpleCoachDto` beze změny — použije se stávající `FullName` bez úprav
  formátu nebo pořadí jméno/příjmení.
- Grafický týdenní rozvrh a jeho komponenta.
- Databázové schéma `sport.TrainingPlan` — není potřeba žádná migrace.

## Mimo rozsah

- Stránkování nebo řazení/filtrace přímo v hlavičce tabulky (sort by column).
- Export tabulkového přehledu do Excelu.
- Úprava obdobné stránky „Tréninky“ (`Training/Schedule/Index`).
- Mazání nebo jiné hromadné akce nad řádky tabulky — pouze editace.

## Hotovo, když

- Pod grafickým rozvrhem na stránce Tréninkové plány se zobrazuje tabulka se
  sloupci Sezóna, Kategorie, Fáze, Od, Do, Název, Den v týdnu, Čas od, Čas do,
  Trenéři ve stanoveném pořadí, a posledním sloupcem s editační ikonou.
- Trenéři jsou zobrazeni celým jménem pomocí existujícího `FullName`.
- Editační ikona u každého řádku vede na editaci odpovídajícího záznamu
  `TrainingPlan` podle jeho `Id`.
- Počet řádků tabulky odpovídá počtu záznamů `sport.TrainingPlan` splňujících
  aktuální filtry, beze změny při zapnutí/vypnutí „Spojovat tréninky“.
- Více trenérů u jednoho záznamu nezpůsobuje více řádků.
- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c
  Release` prochází.
