# Implementační plán: #30 Vizualizace docházky trenérů v přehledu tréninků a zápasů

**Issue:** [#30 — Vizualizace docházky trenérů v přehledu tréninků a zápasů](https://github.com/vasekNaus/SportSys/issues/30)

**Stav:** Připraveno k implementaci.

## Cíl

Zobrazit u každého trenéra přiřazeného ke skutečnému tréninku nebo zápasu
ikonku jeho stavu účasti. Ikona bude přímo za příjmením a její tooltip i tooltip
události sdělí textový stav. Zobrazení zachová stávající informace v bloku
události a zůstane použitelné i při úzkém bloku nebo dlouhém seznamu trenérů.

## Výchozí stav

- Skutečné tréninky načítá `TrainingScheduleService`; trenéry získává z
  `sport.CoachTraining`, ale nyní projekce nese pouze jméno a příjmení.
- `MatchScheduleService` vrací `MatchScheduleItemDto` bez seznamu trenérů.
  Blok zápasu ve sdílené komponentě zobrazuje výsledek a soupeře.
- Razor převádí DTO na typově rozlišené modely `TrainingEventModel`,
  `MatchEventModel` a `TrainingPlanEventModel` v
  `Models/TrainingSchedule/EventModel.cs`. `_ScheduleBlockContent.cshtml`
  vybírá zobrazení podle skutečného typu události.
- `sport.ParticipationType` a `EParticipationType` již obsahují všech pět
  stavů. Jejich stabilní ID jsou `Attended=1`, `NoResponse=2`, `Absent=3`,
  `JointTraining=4` a `AbsentOtherTraining=5`.
- V aktuálním pracovním stromu je již model `sport.CoachMatch`, jeho
  konfigurace, DbSet a migrace. Tento úkol je pouze vizualizační a nemá
  rozšiřovat databázové schéma.

## Potvrzené požadavky a rozhodnutí

- Issue vyžaduje zobrazení u tréninků i zápasů, ikonu za příjmením, vlastní
  ikonu a tooltip pro každý z pěti stavů a kompletní přehled trenérů se stavy
  v tooltipu události.
- Navržené ikony z issue: `✅` Účast, `❓` Bez reakce, `❌` Neúčast,
  `🔗` Spojený trénink a `↔` Neúčast – jiný trénink.
- Text stavu pro tooltip se převezme z `ParticipationType.Name`; ikona se
  namapuje podle stabilního ID v prezentační vrstvě, která nesmí odkazovat na
  `SportSys.Database`.
- Stav Bez reakce se zobrazí, pokud má trenér pro událost záznam se stavem
  `NoResponse`. Chybějící vazba na trenéra se nebude automaticky domýšlet jako
  Bez reakce.
- Plán tréninků (`TrainingPlan`) není skutečná událost a do vizualizace
  docházky nepatří.
- Pokud seskupený blok zastupuje více skutečných tréninků, musí zachovat stav
  u každé dílčí účasti. Stejný trenér přiřazený k více dílčím tréninkům se
  nesmí sloučit do zavádějícího jediného stavu; rozlišení jednotlivých
  tréninků/kategorií zůstane dostupné v tooltipu.

## Technický návrh

Rozšířit Contract DTO o data účasti trenéra (ID, celé jméno, příjmení, ID a
název stavu), samostatně pro skutečné tréninky a zápasy. Obě schedule služby
mají načíst přiřazení dávkově pro načtené ID událostí, nikoli dotazem pro
každého trenéra nebo každou událost zvlášť. Zachovat stávající filtrování,
řazení trenérů a chování rozvrhu.

Razor převede tato data do modelu události a pomocí mapování ID na ikonu je
vykreslí inline vedle příjmení. U zápasu přidá seznam trenérů bez nahrazení
výsledku ani soupeře. Současný `title` tooltip bloku se rozšíří o celé jméno a
text stavu každého trenéra. Ikony budou mít textovou alternativu přes
`title`/přístupný popisek; barva nebude jediným nositelem informace.

## Implementační kroky

### Fáze 1: Contract data a načítání účastí

1. V `SportSys.Contract/Models/` zavést DTO účasti trenéra a přidat kolekci
   do `TrainingScheduleItemDto` a `MatchScheduleItemDto`. `TrainingPlanScheduleItemDto`
   ponechat beze změny.
2. V `TrainingScheduleService.GetTrainingsAsync` a `AssignCoachesAsync` načíst
   z `CoachTraining` ID a název `ParticipationType` spolu s údaji trenéra.
   Zachovat dávkové načítání a dosavadní příjmení odvozené v Contract vrstvě.
3. V `MatchScheduleService.GetMatchesAsync` načíst z `CoachMatch` účasti pro
   vrácené zápasy a přiřadit je k DTO. Zachovat existující filtr trenérů,
   projekci soupeře a výslovnou chybu pro nejednoznačně určený vlastní tým.

### Fáze 2: Prezentace stavů a tooltipy událostí

1. Přidat v `SportSys.Razor/Models/TrainingSchedule/` mapování pěti
   `ParticipationTypeId` na ikonu a použít název stavu z DTO jako uživatelský
   popisek.
2. Rozšířit `TrainingEventModel` a `MatchEventModel` v `EventModel.cs` o data
   potřebná pro vykreslení trenérů se stavy. `TrainingPlanEventModel` ani
   obecný model tréninkových plánů nerozšiřovat o docházku.
3. Aktualizovat `EventModelFactory` tak, aby převáděl data trenérů pro
   tréninky i zápasy a sestavil plný tooltip každé události. U seskupených
   tréninků zachovat detail každého zdrojového tréninku.
4. V `TrainingScheduleBlockData.cs` a souvisejícím převodu seskupených
   tréninků zachovat oddělené stavy účasti jednotlivých položek i při
   opakování trenéra.

### Fáze 3: Vykreslení a layout

1. V `_ScheduleBlockContent.cshtml` zobrazit u tréninku i zápasu příjmení a
   bezprostředně za ním ikonu se stavovým tooltipem. U zápasu zachovat
   stávající výsledek a soupeře; trenéry zobrazit v samostatném řádku.
2. Upravit `Styles/_schedule.scss` tak, aby se jméno s ikonou při nedostatku
   místa zalamovalo nebo bezpečně ořízlo bez rozbití rozvrhu. Zachovat
   viditelnost stavu v tooltipu bloku. Generované `wwwroot/css/site.css`
   neupravovat ručně.
3. Aktualizovat část o zobrazení rozvrhu v `docs/modules/sport.md`; zachovat a
   integrovat již existující změny dokumentace.

### Fáze 4: Ověření

1. Doplnit testy Contract načítání účastí a mapování DTO pro tréninky i zápasy.
2. Doplnit testy vizuálního mapování všech pěti stavů, tooltipů a
   vykreslovacího modelu pro trénink, zápas a seskupený trénink.
3. Ověřit, že plán tréninků zůstává beze změny a že zápas nadále zobrazuje
   soupeře, výsledek i stav zápasu.

## Soubory ke změně

- `src/SportSys.Contract/Models/TrainingScheduleDto.cs`
- `src/SportSys.Contract/Models/MatchScheduleItemDto.cs`
- nový Contract model v `src/SportSys.Contract/Models/` pro účast trenéra
- `src/SportSys.Contract/Services/TrainingScheduleService.cs`
- `src/SportSys.Contract/Services/MatchScheduleService.cs`
- `src/SportSys.Razor/Models/TrainingSchedule/EventModel.cs`
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleBlockData.cs`
- nový vizuální mapper stavů v `src/SportSys.Razor/Models/TrainingSchedule/`
- `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/_ScheduleBlockContent.cshtml`
- `src/SportSys.Razor/Styles/_schedule.scss`
- `docs/modules/sport.md`
- související testy v `tests/SportSys.Razor.Tests/`, zejména
  `MatchScheduleServiceTests.cs`, `ScheduleEventModelFactoryTests.cs`,
  `TrainingScheduleBlockFactoryTests.cs` a
  `TrainingScheduleComponentModelTests.cs`

## Testy a ověření

- Contract testy ověří, že účasti trenérů jsou vráceny pro příslušné ID
  událostí a že každý DTO obdrží správného trenéra i stav bez N+1 načítání.
- Prezentační testy ověří jednoznačné mapování pěti ID na pět různých ikon a
  textů tooltipu včetně `NoResponse`.
- Testy seskupeného tréninku ověří, že dvě účasti téhož trenéra s různými
  stavy zůstanou rozlišitelné a jejich detail je v tooltipu.
- Testy zápasu ověří seznam trenérů i zachování existujícího soupeře, výsledku
  a stavu zápasu.
- Spustit cílené testy `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`
  a build řešení `dotnet build SportSys.slnx`.

## Manuální akceptace

- U tréninku a zápasu s trenéry ověřit příjmení následované správnou ikonou
  pro všech pět stavů; stav musí být čitelný po najetí i bez rozlišení barvou.
- Ověřit, že tooltip celé události obsahuje jméno každého trenéra a český
  název jeho stavu.
- Ověřit seskupený trénink s více kategoriemi, včetně stejného trenéra s
  různým stavem v jednotlivých položkách.
- Ověřit úzký blok a dlouhý seznam trenérů: layout zůstane funkční a stavové
  informace jsou dostupné i při oříznutém textu.
- Ověřit, že zápas stále zobrazuje výsledek a soupeře a že plán tréninku nemá
  novou indikaci docházky.

## Beze změny

- Význam a seedování stavů v `EParticipationType` a `sport.ParticipationType`.
- Evidence a import účastí, archiv XLSX i stránky modulu HR.
- Tréninkové plány a jejich přiřazení trenérů.
- Filtry rozvrhu, seskupování událostí, export do Excelu a stavové ikony
  tréninků či zápasů.

## Mimo rozsah

- Změna databázového modelu, vytváření nebo aplikace EF Core migrace.
- Změna způsobu potvrzování nebo importu docházky.
- Zobrazení účasti trenérů mimo rozvrh skutečných tréninků a zápasů.

## Hotovo, když

- Každý trenér přiřazený ke skutečnému tréninku i zápasu má vedle příjmení
  svou stavovou ikonu a srozumitelný tooltip.
- Všech pět podporovaných stavů má odlišnou ikonu a textový popisek.
- Tooltip události obsahuje úplný seznam trenérů a jejich stavy.
- Seskupené tréninky neztratí informaci o odlišných stavech jednotlivých
  účastí a zobrazení nerozbije layout při nedostatku prostoru.
- Stávající informace bloku zůstanou zachovány, plán tréninku se nezmění a
  databázový model ani migrace nebudou součástí této implementace.
