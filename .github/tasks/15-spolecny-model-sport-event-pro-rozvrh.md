# Implementační plán: #15 Zavedení společného modelu SportEvent pro rozvrh tréninků a zápasů

**Issue:** [#15 — Zavedení společného modelu SportEvent pro rozvrh tréninků a zápasů](https://github.com/vasekNaus/SportSys/issues/15)

**Stav:** Připraveno k implementaci.

## Cíl

Rozšířit `/sport/Training/Schedule` tak, aby na jedné časové ose zobrazoval
reálné tréninky i zápasy. Na Contract vrstvě zavést společného DTO předka pro
datované sportovní události, ale zachovat samostatné služby a dotazy pro
`Training` a `Match`. Razor komponenta má po změně pracovat pouze s obecným
renderovacím modelem a nesmí znát EF entity ani konkrétní DTO tréninku či
zápasu.

## Výchozí stav

- Databázové entity `Training` a `Match` již dědí z abstraktního
  `SportEvent` a používají TPC se sdílenou sekvencí
  `sport.SportEventSeq`.
- `SportEvent` aktuálně obsahuje `Id`, `SeasonId`, `SeasonCategoryName`,
  `Date`, `TimeFrom`, `Note` a navigace na sezónu a kategorii.
- `Training` navíc obsahuje `TimeTo`, computed `DurationMinutes`, volně
  zadanou textovou `Location`, typ, fázi, stav, trenéry a případné členství
  v `TrainingGroup`.
- `Match` obsahuje stadion, domácí a hostující tým, typ zápasu a volitelný
  JSON výsledek. Nemá `TimeTo`, `DurationMinutes` ani samostatný stav.
- `Match.TimeTo` a `Match.DurationMinutes` byly historicky odstraněny migrací
  `20260526073840_Match_ResultJson`; tvrzení v `docs/conventions.md`, že jsou
  stále přítomné i na `Match`, proto neodpovídá aktuálnímu modelu.
- Kategorie není identifikována samostatným `CategoryId`. Používá složený klíč
  `SeasonId + SeasonCategoryName`; společný DTO předek proto nesmí zavádět
  neexistující `CategoryId`.
- Trénink nemá `TrainingLocationId`; ukládá text `Location`. Zápas naopak
  používá `IceRinkId`. Do společného DTO předka proto nepatří konkrétní
  identifikátor ani název lokality.
- `TrainingScheduleService.GetTrainingsAsync` načítá a filtruje reálné
  tréninky a současně jim přiděluje dočasné `VisualizationGroupId`.
  Stejná služba také načítá `TrainingPlan`, který není potomkem
  `SportEvent`.
- `TrainingScheduleItemDto` nevychází ze sportovní události, ale dědí
  z `TrainingPlanScheduleItemDto`. Obě DTO implementují
  `ITrainingScheduleItem`.
- Razor modely `TrainingScheduleRow`, `TrainingScheduleBlockFactory` a
  `TrainingScheduleComponentModel` pracují přímo s
  `ITrainingScheduleItem` a při rozhodování o editaci testují konkrétní typy
  DTO.
- `TrainingScheduleBlockFactory` nejprve vytvoří bloky podle
  `VisualizationGroupId`/`GroupId`; `TrainingScheduleComponentModel` potom
  řadí hotové bloky do lanes tak, aby se nepřekrývaly.
- Export `TrainingScheduleExcelExporter` je výslovně exportem tréninků,
  používá stejné seskupení jako vizualizace a vytváří list `Tréninky`.

## Potvrzené požadavky a rozhodnutí

- Rozvrh zobrazí tréninky a zápasy společně v řádcích podle data.
- Oba typy událostí budou řazeny a rozkládány do lanes jedním algoritmem.
- Contract vrstva dostane společného DTO předka sportovní události.
- DTO tréninku i zápasu budou z tohoto předka dědit.
- Načítání zápasů bude v samostatné `MatchScheduleService`; mezi Training a
  Match službami nebude dědičnost.
- Renderovací komponenta bude přijímat pouze obecný
  `ScheduleEventModel`.
- Tréninkový blok zachová současný význam čtyř řádků: kategorie, čas,
  kombinace typu a lokality, trenéři.
- Zápasový blok zobrazí kategorii, čas, soupeře a výsledek.
- `Match.TimeTo` bude znovu uložen v databázi a `DurationMinutes` bude stejně
  jako u tréninku persisted computed sloupec.
- Existující hodnoty `TimeTo` doplní uživatel ručně v databázi. Implementace
  nesmí zavést domnělou výchozí délku zápasu ani automatický backfill.
- Agent nevytvoří ani neupraví EF Core migraci nebo model snapshot. Po změně
  modelu vytvoří a zkontroluje migraci uživatel.
- Stávající Excel export zůstane exportem pouze tréninků. Zápasy se do něj
  v rámci #15 nepřidávají.
- Filtry specifické pro trénink (`TrainingType`, `TrainingState`,
  `Location`, `MergeTrainings`) budou nadále ovlivňovat pouze tréninky.
  Společné filtry sezóny, kategorií a data se použijí pro oba typy událostí.
- Volba `MergeTrainings` nebude spojovat zápas s tréninkem ani dva zápasy do
  jednoho bloku. Zápasy se však vždy účastní společné detekce kolizí a
  rozložení do lanes.
- `TrainingPlan` zůstane podporovaný stejnou ViewComponentou, ale nebude
  vydáván za `SportEventDto`; do obecného renderovacího modelu se převede
  samostatným adaptérem.

## Technický návrh

### Databázový model

Do `Match` se vrátí povinný `TimeTo` s přesností `time(0)` a nullable
`DurationMinutes`. `MatchConfiguration` nastaví `DurationMinutes` jako
persisted computed column pomocí stejného výrazu jako u `Training`:

```text
DATEDIFF(minute, TimeFrom, TimeTo)
```

Vlastnosti zůstanou na konkrétních typech `Training` a `Match`; přesun
`TimeTo` a `DurationMinutes` do EF předka `SportEvent` není pro DTO ani
renderovací polymorfismus nutný a zbytečně by měnil fungující TPC mapování
tréninků.

Před aplikací uživatelsky vytvořené migrace musí být připraven ruční postup
pro doplnění `TimeTo` u existujících řádků. Migrace nesmí ponechat
produkčním záznamům technickou hodnotu `00:00` ani jiný domnělý default.

### Contract DTO

V `SportSys.Contract.Models` vznikne abstraktní `SportEventDto` pouze pro
skutečné datované potomky databázového `SportEvent`. Ponese ověřené společné
údaje potřebné rozvrhem:

```csharp
Id
SeasonId
SeasonCategoryName
SeasonCategoryOrder
Date
TimeFrom
TimeTo
DurationMinutes
Note
```

`TrainingScheduleItemDto` bude dědit z `SportEventDto` a ponese tréninková
pole `GroupId`, `VisualizationGroupId`, `Location`, názvy typu a fáze,
trenéry a stav. Přestane dědit z `TrainingPlanScheduleItemDto`.

`TrainingPlanScheduleItemDto` zůstane samostatným DTO. Společné technické
rozhraní pro seskupování tréninků a plánů lze zachovat nebo zúžit tak, aby
obsahovalo jen data skutečně používaná v
`TrainingScheduleService.ApplyVisualizationGrouping`; nesmí z něj vzniknout
falešná doménová dědičnost `TrainingPlan -> SportEvent`.

Nový `MatchScheduleItemDto : SportEventDto` ponese nejméně:

```csharp
IceRinkId
IceRinkName
MatchTypeName
HomeTeamName
AwayTeamName
OpponentName
IsHome
HomeGoals
AwayGoals
```

Text výsledku se nebude ukládat duplicitně do databáze. Pro renderování se
sestaví z `HomeGoals` a `AwayGoals`; pokud výsledek není známý, použije se
`-`.

### Match služba

Nová `MatchScheduleService` bude záviset přímo na `SportSysDbContext` stejně
jako stávající `TrainingScheduleService`, ale nebude z ní dědit ani ji
volat. Metoda `GetMatchesAsync` přijme společné filtry:

```text
seasonId
categoryNames
dateFrom
dateTo
CancellationToken
```

Dotaz načte zápasy včetně pořadí kategorie, stadionu, typu, domácího a
hostujícího týmu a výsledku. `OpponentName` a `IsHome` se odvodí porovnáním
`SeasonCategory.CompetitionTeamName` s domácím a hostujícím týmem, tedy
stejným pravidlem, které používá `CsvMatchImportService` při importu:

- vlastní tým je domácí -> soupeř je `AwayTeam`,
- vlastní tým je hostující -> soupeř je `HomeTeam`,
- vlastní tým neodpovídá ani jedné straně -> vyvolat explicitní chybu s ID
  zápasu; nevracet zavádějícího soupeře ani záznam tiše nepřeskakovat.

Služba se zaregistruje výhradně v
`SportSys.Contract.ServiceCollectionExtensions.AddSportSysServices()`.

### Obecný renderovací model

V `SportSys.Razor.Models.TrainingSchedule` vznikne obecný
`ScheduleEventModel`, který bude hotovým vstupem časové osy. Bude obsahovat
alespoň:

```text
EventType
SourceId
TimeFrom
TimeTo
TitleLine
DetailLine1
DetailLine2
ColorKey
Tooltip
EditPage
EditItemId
StateIcon
StateTooltip
```

`EventType` bude typovaný enum, například `Training`, `Match`,
`TrainingPlan`; nebude se používat volný řetězec. Model nebude odkazovat na
Contract DTO ani EF entity.

Samostatný mapper/factory na hranici PageModelu a prezentace vytvoří:

- jeden nebo více agregovaných `ScheduleEventModel` z tréninkových DTO při
  zachování `GroupId`, `VisualizationGroupId`, stavových ikon, tooltipů a
  editačních odkazů,
- jeden `ScheduleEventModel` pro každý zápas bez editačního odkazu,
- renderovací modely z `TrainingPlanScheduleItemDto` pro stávající stránku
  `/sport/Training/Plan`.

Současná agregace v `TrainingScheduleBlockFactory` se zachová jako
tréninkový/plánový adaptační krok, ale výsledkem budou obecné renderovací
události. `TrainingScheduleComponentModel` již nebude používat
`ITrainingScheduleItem`, `TrainingScheduleItemDto` ani
`TrainingPlanScheduleItemDto`; bude pouze řadit předané obecné události do
lanes podle `TimeFrom`/`TimeTo`.

Barva zůstane určena kategorií, aby se nezměnila existující legenda.
`EventType` lze promítnout do stabilní CSS třídy bloku pro sémantické
rozlišení a budoucí stylování, ale #15 nebude zavádět nový barevný systém.

### Datový tok stránky Schedule

`Areas/sport/Pages/Training/Schedule/Index.cshtml.cs` načte po normalizaci
filtrů nezávisle:

1. tréninky přes `TrainingScheduleService.GetTrainingsAsync`,
2. zápasy přes `MatchScheduleService.GetMatchesAsync`.

Obě kolekce převede na `ScheduleEventModel`, spojí je podle data a předá
řádkům `TrainingScheduleViewModel`. Teprve obecná komponenta provede společné
řazení a rozložení překryvů, takže zápas kolidující s tréninkem skončí v jiné
lane.

Výběr typu, stavu a textové lokality se nepřenese do Match služby. Tím se
zachová dosavadní filtrování tréninků a současně se zápasy neztratí jen
proto, že pro ně daný tréninkový atribut neexistuje.

PageModel bude sledovat počet exportovatelných tréninků odděleně od
`ScheduleView.HasItems`. Tlačítko exportu se zobrazí pouze tehdy, když
aktuální tréninkové filtry vracejí alespoň jeden trénink; samotná přítomnost
zápasu nesmí nabídnout export, který by následně skončil chybou „nebyly
nalezeny žádné tréninky“.

## Implementační kroky

### Fáze 1: Doplnění časového intervalu zápasu

1. Do `SportSys.Database.Models.sport.Match` přidat `TimeTo` s
   `[Precision(0)]` a `DurationMinutes`.
2. V `MatchConfiguration.Configure` nastavit `DurationMinutes` jako
   persisted computed column shodně s `TrainingConfiguration`.
3. Neprovádět změny existujících migrací ani snapshotu.
4. Připravit model tak, aby uživatel mohl následně vytvořit vlastní EF Core
   migraci a ručně vyřešit doplnění `TimeTo` pro existující zápasy.

### Fáze 2: Narovnání DTO hierarchie

1. Přidat abstraktní `SportEventDto` s ověřenými společnými vlastnostmi.
2. Změnit `TrainingScheduleItemDto` na potomka `SportEventDto` a zachovat
   všechny hodnoty používané filtrováním, seskupováním, exportem a editací.
3. Ponechat `TrainingPlanScheduleItemDto` mimo hierarchii sportovních
   událostí; případné sdílené rozhraní použít jen pro algoritmus slučování.
4. Přidat `MatchScheduleItemDto : SportEventDto` s údaji stadionu, týmů,
   soupeře, typu a výsledku.
5. Upravit projekci v `TrainingScheduleService.GetTrainingsAsync`, aby
   naplnila nově společné `SeasonId` a ostatní zděděné vlastnosti bez změny
   výsledků dotazu.

### Fáze 3: Samostatná služba zápasů

1. Vytvořit `MatchScheduleService` s dotazem omezeným na sezónu, kategorie a
   datum.
2. Projektovat pouze potřebná data a používat async EF Core API s předaným
   `CancellationToken`.
3. Po materializaci explicitně určit domácí/venkovní roli a soupeře podle
   `SeasonCategory.CompetitionTeamName`.
4. Ošetřit nekonzistentní data explicitní výjimkou obsahující ID zápasu.
5. Zaregistrovat službu v `AddSportSysServices()`.

### Fáze 4: Obecná renderovací pipeline

1. Přidat enum typu události a `ScheduleEventModel`.
2. Převést současnou logiku tvorby tréninkových bloků na adaptér, který
   zachová:
   - databázové i dočasné vizualizační skupiny,
   - pořadí kategorií,
   - agregaci času, typů, lokalit a trenérů,
   - jednotný a smíšený stav,
   - tooltipy,
   - editační odkaz na nejnižší ID člena.
3. Přidat mapování zápasu na blok:
   - `TitleLine` = kategorie,
   - čas = `TimeFrom–TimeTo`,
   - `DetailLine1` = soupeř,
   - `DetailLine2` = výsledek `HomeGoals:AwayGoals`, jinak `-`,
   - tooltip doplní domácí/venkovní roli, stadion, typ zápasu a poznámku.
4. Přidat adaptér pro `TrainingPlanScheduleItemDto`, aby stránka Plan
   používala stejný obecný renderovací model bez regresí.
5. Změnit `TrainingScheduleRow.Items` a navazující modely na kolekci
   `ScheduleEventModel`.
6. Zjednodušit `TrainingScheduleComponentModel`: odstranit testování
   konkrétních DTO a ponechat pouze časovou osu, legendu a společné lane
   rozložení.
7. Upravit `Default.cshtml` na obecné názvy detailních řádků a použít
   editační metadata již připravená v renderovacím modelu.

### Fáze 5: Zapojení zápasů do Schedule

1. Injektovat `MatchScheduleService` do PageModelu Schedule.
2. Načíst zápasy stejným společným filtrem sezóny, kategorií a data.
3. Převést tréninky a zápasy na obecné renderovací události a seskupit je
   společně podle `Date`.
4. Zachovat generování všech dnů intervalu, paritu řádků, víkendové
   zvýraznění a volbu `ShowEmptyRows`.
5. Zachovat `MergeTrainings` pouze pro tréninky; po převodu kombinovat
   výsledné tréninkové bloky se samostatnými zápasovými bloky.
6. Změnit titul stránky a prázdnou zprávu z „tréninků“ na „sportovní
   události“ nebo „tréninky a zápasy“.
7. Oddělit `HasExportableTrainings` od obecného `ScheduleView.HasItems` a
   ponechat exportní handler i soubor obsahově beze změny.

### Fáze 6: Dokumentace a ověření

1. Aktualizovat `docs/modules/sport.md`:
   - Schedule načítá `Training` i `Match`,
   - společný DTO a renderovací model,
   - pravidla filtrů a společného lane rozložení,
   - zápasové údaje a skutečnost, že export zůstává training-only.
2. Opravit `docs/conventions.md`, aby popis `DurationMinutes` odpovídal
   implementovanému modelu po znovuzavedení sloupce na `Match`.
3. Doplnit a upravit automatické testy uvedené níže.
4. Sestavit řešení a spustit cílený testovací projekt.

## Soubory ke změně

### Databáze

- `src/SportSys.Database/Models/sport/Match.cs`
  - `TimeTo`, `DurationMinutes`.
- `src/SportSys.Database/Configurations/sport/MatchConfiguration.cs`
  - persisted computed konfigurace `DurationMinutes`.
- Beze změny:
  `src/SportSys.Database/Migrations/` a
  `SportSysDbContextModelSnapshot.cs`; migraci vytvoří uživatel.

### Contract

- `src/SportSys.Contract/Models/SportEventDto.cs` — nový společný DTO předek.
- `src/SportSys.Contract/Models/TrainingScheduleDto.cs`
  - narovnání dědičnosti tréninku a plánu.
- `src/SportSys.Contract/Models/MatchScheduleItemDto.cs` — nový DTO zápasu.
- `src/SportSys.Contract/Services/TrainingScheduleService.cs`
  - projekce do upraveného DTO a zachování seskupování tréninků/plánů.
- `src/SportSys.Contract/Services/MatchScheduleService.cs`
  - nový nezávislý read service pro rozvrh zápasů.
- `src/SportSys.Contract/ServiceCollectionExtensions.cs`
  - DI registrace Match služby.

### Razor

- `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml.cs`
  - načtení a spojení tréninků a zápasů, exportní příznak.
- `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml`
  - obecnější titul, prázdný stav a podmínka exportu.
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs`
  - převod plánů do obecného renderovacího modelu.
- `src/SportSys.Razor/Models/TrainingSchedule/ScheduleEventModel.cs`
  - nový obecný renderovací model a typ události.
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleRow.cs`
  - obecná kolekce položek.
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleViewModel.cs`
  - výpočet rozsahu časové osy z obecných událostí.
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleBlockData.cs`
  - převod stávající agregace na adaptér obecných renderovacích událostí;
    podle výsledného rozdělení může být soubor přejmenován na přesnější
    `ScheduleEventModelFactory.cs`.
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleComponentModel.cs`
  - společný lane algoritmus bez znalosti DTO.
- `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`
  - vykreslení obecných detailních řádků.
- `src/SportSys.Razor/Styles/_schedule.scss`
  - pouze případná sémantická třída typu události; generovaný
    `wwwroot/css/site.css` ručně neupravovat.

### Dokumentace a testy

- `docs/modules/sport.md`
- `docs/conventions.md`
- `tests/SportSys.Razor.Tests/TrainingScheduleBlockFactoryTests.cs`
- `tests/SportSys.Razor.Tests/TrainingScheduleComponentModelTests.cs`
- `tests/SportSys.Razor.Tests/TrainingScheduleVisualizationGroupingTests.cs`
- `tests/SportSys.Razor.Tests/TrainingScheduleExcelExporterTests.cs`
- `tests/SportSys.Razor.Tests/MatchScheduleServiceTests.cs` — nový test
  projekce/odvození soupeře, pokud bude použit testovatelný pure helper;
  test projektu nemá databázový provider a není důvod kvůli #15 přidávat
  novou databázovou testovací závislost.
- `tests/SportSys.Razor.Tests/ScheduleEventModelFactoryTests.cs` — nové testy
  mapování tréninků, zápasů a plánů do renderovacího modelu.

## Testy a ověření

### Automatické testy

- `SportEventDto`
  - tréninkové a zápasové DTO dědí ze společného předka,
  - `TrainingPlanScheduleItemDto` z něj nedědí.
- Match interval
  - EF model obsahuje `Match.TimeTo`,
  - `Match.DurationMinutes` je computed a při zápisu se nenastavuje z C#.
- Match projekce
  - vlastní tým doma -> soupeř je hostující tým a `IsHome == true`,
  - vlastní tým venku -> soupeř je domácí tým a `IsHome == false`,
  - vlastní tým neodpovídá ani jedné straně -> explicitní chyba,
  - známý výsledek se formátuje `home:away`,
  - chybějící výsledek se vykreslí jako `-`.
- Renderovací model
  - samostatný trénink zachová čtyři stávající řádky, tooltip, stav a editaci,
  - skupina tréninků zachová agregaci a nejnižší editační ID,
  - dočasná `VisualizationGroupId` funguje stejně jako před změnou,
  - zápas nemá editační odkaz a obsahuje soupeře a výsledek,
  - TrainingPlan zachová editační stránku a tooltip platnosti.
- Společné lanes
  - nepřekrývající se trénink a zápas jsou ve stejné lane,
  - překrývající se trénink a zápas jsou v různých lanes,
  - dva překrývající se zápasy jsou v různých lanes,
  - řazení při shodném čase je deterministické podle kategorie, typu události
    a ID,
  - `MergeTrainings` spojí pouze tréninky a nepohltí zápas.
- Regrese
  - stávající testy seskupování, stavů, tooltipů, editace a časové osy projdou
    po převodu na obecný model,
  - všechny stávající testy Excel exportu projdou beze změny významu,
  - export nepřijímá ani nezapisuje zápasy.

### Příkazy

```powershell
dotnet build SportSys.slnx
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
```

Pokud se změní `_schedule.scss`, navíc:

```powershell
Set-Location src\SportSys.Razor
npm run build:css
```

Vygenerovaný `src/SportSys.Razor/wwwroot/css/site.css` zůstává ignorovaný a
necommitovaný.

## Manuální akceptace

1. Otevřít `/sport/Training/Schedule`, vybrat sezónu, kategorie a období
   obsahující tréninky i zápasy.
2. Ověřit, že oba typy jsou v jednom datovém řádku a na stejné časové ose.
3. Ověřit blok tréninku: kategorie, čas, typ/lokalita, trenéři, stavová ikona
   a editační odkaz odpovídají stavu před změnou.
4. Ověřit blok zápasu: kategorie, čas, soupeř a výsledek; bez výsledku se
   zobrazí `-`.
5. Ověřit domácí i venkovní zápas a správný výběr soupeře podle
   `CompetitionTeamName`.
6. Vytvořit kolizi tréninku a zápasu a ověřit, že se vykreslí v různých
   lanes bez překrytí bloků.
7. Zapnout `Spojovat tréninky` a ověřit, že se stávající tréninky spojí
   stejně jako dosud, ale zápas zůstane samostatný.
8. Použít filtry typu, stavu a lokality a ověřit, že nadále filtrují
   tréninky; zápasy jsou omezeny společnými filtry sezóny, kategorie a data.
9. Ověřit den obsahující pouze zápas: rozvrh není prázdný, ale tlačítko
   exportu tréninků není dostupné.
10. Ověřit export s tréninky: název souboru, list, sloupce, seskupení a obsah
    jsou stejné jako před změnou a zápasy v exportu nejsou.
11. Otevřít `/sport/Training/Plan` a ověřit renderování, slučování, tooltipy
    a editační odkazy plánů.

## Beze změny

- TPC strategie `SportEvent -> Training / Match` a sdílená sekvence.
- Databázové skupiny `TrainingGroup` a `TrainingPlanGroup`.
- Editace tréninků a tréninkových plánů.
- Význam a výchozí stav `MergeTrainings`.
- Stavové ikony tréninků.
- Barevná paleta kategorií a algoritmus rozsahu časové osy.
- Obsah a formát Excel exportu tréninků.
- Route `/sport/Training/Schedule` a `/sport/Training/Plan`.
- Služby `TrainingService` a `TrainingPlanService`.

## Mimo rozsah

- Vytvoření, úprava nebo aplikace EF Core migrace agentem.
- Automatický backfill `Match.TimeTo`; existující data doplní uživatel
  ručně.
- Editace nebo vytváření zápasů z rozvrhu.
- Samostatné filtry podle typu zápasu, stadionu, domácí/venkovní role nebo
  stavu výsledku.
- Export zápasů nebo společný export sportovních událostí.
- Slučování zápasů s tréninky pomocí `MergeTrainings`.
- Změna databázového typu JSON výsledku nebo struktury `MatchResult`.
- Přesun stránky na novou route.
- Změny izolovaného prototypu `SportSys.Web`.

## Hotovo, když

- `Match` má uložený koncový čas a databázově počítanou délku.
- Existuje společný `SportEventDto` a dědí z něj DTO reálného tréninku i
  zápasu, nikoli TrainingPlan.
- Zápasy načítá samostatná `MatchScheduleService`.
- Razor renderovací komponenta nepracuje s konkrétními Contract DTO ani EF
  entitami.
- Schedule kombinuje tréninky a zápasy podle data a společně je rozkládá do
  lanes.
- Tréninkové bloky, filtry, skupiny, stavy, editace a export nemají regresi.
- Zápasové bloky zobrazují správnou kategorii, interval, soupeře a výsledek.
- TrainingPlan nadále používá stejnou komponentu a funguje beze změny.
- Dokumentace odpovídá implementaci.
- Řešení se sestaví a cílené testy projdou.
- Migrace a ruční doplnění existujících `Match.TimeTo` zůstaly explicitně na
  uživateli.
