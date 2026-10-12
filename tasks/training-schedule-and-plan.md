# Implementační plán: Sdílená vizualizace rozvrhu tréninků

**Stav:** Implementováno a ověřeno sestavením řešení.

## Cíl

Přesunout existující rozvrh reálných tréninků do sekce `Training`, vytvořit
samostatnou stránku pro obecné plány z `sport.TrainingPlan` a extrahovat společnou
timeline do znovupoužitelné ASP.NET Core ViewComponent.

Výsledné stránky:

- `/sport/Training/Schedule` — reálné tréninky s konkrétním datem,
- `/sport/Training/Plan` — obecný týdenní plán z `sport.TrainingPlan`.

Původní route `/sport/Schedule` se odstraní bez redirectu.

## Potvrzená rozhodnutí

- Schedule zachová interval `Od–Do`.
- Plan vždy zobrazí celý týden pondělí–neděle, včetně prázdných dnů.
- Plan filtruje podle sezóny, kategorií, jednoho TrainingType a jedné TrainingPhase.
- TrainingType a TrainingPhase budou samostatné single-selecty.
- Po zvolení sezóny se žádná kategorie, typ ani fáze nevybere automaticky.
- Plan zobrazí všechny odpovídající záznamy bez filtrování `From`/`To`.
- Plány s různými obdobími platnosti se zobrazí v samostatných lanes stejně jako
  jiné překrývající se bloky.
- Období `From–To` se zobrazí v tooltipu bloku plánu.
- Ve filtrech budou pouze aktivní sezóny a aktivní kategorie.
- Schedule si zachová současné filtry bez TrainingType a TrainingPhase.
- Sdílená vizualizace bude implementována jako ASP.NET Core ViewComponent.
- `TrainingScheduleItemDto` přímo zdědí DTO pro TrainingPlan a přidá konkrétní datum.
- Datový přenos mezi PageModely a ViewComponent bude definován interface.

## Fáze 1: Přesun stránky Schedule

1. Vytvořit složku:
   `src/SportSys.Razor/Areas/sport/Pages/Training/`.
2. Přesunout:
   - `Schedule/Index.cshtml` → `Training/Schedule.cshtml`,
   - `Schedule/Index.cshtml.cs` → `Training/Schedule.cshtml.cs`.
3. Přejmenovat PageModel:
   - `IndexModel` → `ScheduleModel`.
4. Změnit namespace na:
   `SportSys.Razor.Areas.sport.Pages.Training`.
5. Upravit všechny odkazy na statické helpery původního `IndexModel`.
6. Odstranit původní složku `Areas/sport/Pages/Schedule`.
7. Aktualizovat navigaci z `/Schedule/Index` na `/Training/Schedule`.
8. Nevytvářet kompatibilní redirect pro původní route.

## Fáze 2: Společný Contract model

Upravit `src/SportSys.Contract/Models/TrainingScheduleDto.cs`.

### 2.1 Interface položky

Vytvořit `ITrainingScheduleItem` se společnými vlastnostmi potřebnými pro
vykreslení bloku:

- `Id`,
- `TimeFrom`,
- `TimeTo`,
- `DurationMinutes`,
- `SeasonCategoryName`,
- `Location`,
- `TrainingTypeName`,
- `TrainingPhaseName`,
- `Note`.

Interface nesmí obsahovat logiku rozložení ani závislost na Razor projektu.

### 2.2 DTO plánu

Vytvořit `TrainingPlanScheduleItemDto : ITrainingScheduleItem` s vlastnostmi:

- společné vlastnosti interface,
- `From`,
- `To`,
- `DayName`.

`DayName` v databázi obsahuje anglické hodnoty `Monday` až `Sunday`. Při načtení
se převede na `DayOfWeek`; neznámá hodnota nesmí být tiše ignorována.

### 2.3 DTO reálného tréninku

Změnit `TrainingScheduleItemDto` tak, aby dědil:

```csharp
TrainingScheduleItemDto : TrainingPlanScheduleItemDto
```

a přidal:

```csharp
DateOnly Date
```

Při projekci reálného tréninku nastavit zděděné hodnoty konzistentně:

- `From = Date`,
- `To = Date`,
- `DayName = Date.DayOfWeek.ToString()`.

Tím bude konkrétní trénink reprezentován jako plán platný právě jeden den.

## Fáze 3: Prezentační interface pro ViewComponent

V Razor projektu vytvořit modely například v:

`src/SportSys.Razor/Models/TrainingSchedule/`

### 3.1 `ITrainingScheduleViewModel`

Interface předá komponentě kompletní připravenou timeline:

- seznam řádků,
- mapu barev kategorií,
- začátek a konec časové osy,
- informaci, zda jsou dostupná data.

### 3.2 `TrainingScheduleRow`

Jeden řádek obsahuje:

- hlavní popisek (`Po`, `Út`, datum apod.),
- vedlejší popisek,
- příznak zvýraznění víkendu,
- seznam `ITrainingScheduleItem`.

### 3.3 `TrainingScheduleViewModel`

Konkrétní implementace interface. PageModely vytvoří pouze seznam řádků a
konfiguraci; ViewComponent zajistí výpočet lanes, markerů, pozic a šířek bloků.

## Fáze 4: ViewComponent

Vytvořit:

- `src/SportSys.Razor/ViewComponents/TrainingScheduleViewComponent.cs`,
- `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`.

Komponenta:

1. přijme `ITrainingScheduleViewModel`,
2. určí dynamický rozsah časové osy podle předaných položek,
3. vytvoří časové markery,
4. rozdělí překrývající se položky do lanes,
5. vypočítá procentuální pozici a šířku bloků,
6. vykreslí legendu kategorií,
7. vykreslí všechny řádky včetně prázdných,
8. sestaví tooltip bezpečně jako textový HTML atribut.

Komponenta nebude načítat databázi ani číst query parametry.

Pomocné výpočty `GetLanes`, `GetLeft`, `GetWidth`, `TimelineMarkers` a práce s
časovým rozsahem se odstraní z PageModelu Schedule a přesunou do komponenty nebo
jejího interního view modelu.

## Fáze 5: Úprava TrainingScheduleService

Rozšířit `TrainingScheduleService` bez přístupu Razor vrstvy k DbContextu.

### 5.1 Aktivní filtrační hodnoty

- `GetSeasonsAsync` vrací pouze aktivní sezóny.
- `GetCategoriesAsync` vrací pouze aktivní kategorie vybrané sezóny.
- Doplnit seznam TrainingType.
- Doplnit seznam TrainingPhase.

TrainingType a TrainingPhase jsou enum-backed lookupy a nefiltrují se přes
`IsActive`.

### 5.2 Reálné tréninky

Stávající `GetTrainingsAsync` zachovat a rozšířit projekci o:

- `TrainingPhaseName`,
- zděděné `From`, `To` a `DayName`.

### 5.3 Obecné plány

Doplnit `GetTrainingPlansAsync` s parametry:

- `seasonId`,
- vybrané názvy kategorií,
- `trainingTypeId`,
- `trainingPhaseId`.

Dotaz:

- filtruje podle všech zvolených hodnot,
- nefiltruje podle `From` a `To`,
- řadí podle dne v týdnu, času a období platnosti,
- projektuje přímo do `TrainingPlanScheduleItemDto`,
- nepoužívá `DurationMinutes` počítané v C#.

## Fáze 6: Refaktor stránky Schedule

`Training/Schedule.cshtml.cs` zachová:

- filtr sezóny,
- vícenásobný výběr kategorií,
- `DateFrom`,
- `DateTo`,
- načtení reálných tréninků.

PageModel:

1. připraví řádek pro každý den intervalu,
2. připojí tréninky podle `Date`,
3. vytvoří mapu barev kategorií,
4. naplní `ITrainingScheduleViewModel`.

`Training/Schedule.cshtml` bude obsahovat filtr, prázdné stavy a volání:

```cshtml
@await Component.InvokeAsync("TrainingSchedule", Model.ScheduleView)
```

## Fáze 7: Stránka Plan

Vytvořit:

- `Training/Plan.cshtml`,
- `Training/Plan.cshtml.cs`.

### Filtry

- povinná sezóna,
- vícenásobný výběr kategorií,
- povinný jeden TrainingType,
- povinná jedna TrainingPhase.

Po změně sezóny se načtou aktivní kategorie. Žádná hodnota se nevybere
automaticky. Timeline se zobrazí až po platném výběru všech filtrů a alespoň jedné
kategorie.

### Řádky timeline

PageModel vždy vytvoří sedm řádků v pořadí:

1. Pondělí,
2. Úterý,
3. Středa,
4. Čtvrtek,
5. Pátek,
6. Sobota,
7. Neděle.

Plány se přiřadí podle validovaného `DayName`. Sobota a neděle použijí víkendové
zvýraznění. Prázdné dny zůstanou viditelné.

### Tooltip plánu

Tooltip bude obsahovat:

- kategorii,
- typ tréninku,
- fázi,
- místo,
- období platnosti `From–To`,
- poznámku, pokud není prázdná.

## Fáze 8: Sdílení barev a textů

1. Přesunout mapování barev kategorií mimo PageModel Schedule do společného
   builderu nebo helperu Razor vrstvy.
2. Zachovat stabilní barvu kategorie v rámci jednoho zobrazení.
3. Nepřidávat nové přímé hex barvy do komponentových SCSS souborů; případné nové
   barvy definovat jako design tokeny.
4. Lokalizaci názvů dnů řešit v prezentační vrstvě, nikoli uložením českého textu
   do DTO nebo databáze.

## Fáze 9: Navigace

V sekci Sport změnit položku Rozpis na `/Training/Schedule` a přidat položku
Tréninkové plány odkazující na `/Training/Plan`.

## Fáze 10: Aktualizace dokumentace

Aktualizovat `docs/modules/sport.md`:

- nové routes,
- rozdíl mezi reálným tréninkem a obecným plánem,
- DTO dědičnost,
- contract interface položky,
- prezentační interface a ViewComponent,
- filtry obou stránek,
- pravidla `DayName` a období platnosti.

Aktualizovat implementační plán po případných změnách přijatého návrhu.

## Fáze 11: Ověření

1. Sestavit `SportSys.slnx`.
2. Ověřit, že `/sport/Training/Schedule` zachovalo původní funkcionalitu.
3. Ověřit, že `/sport/Schedule` již není routováno.
4. Ověřit Schedule s:
   - více dny,
   - prázdným dnem,
   - překrývajícími se tréninky,
   - víkendem.
5. Ověřit Plan s:
   - sedmi řádky,
   - prázdnými dny,
   - více obdobími platnosti ve stejném čase,
   - překrývajícími se plány,
   - různými kategoriemi,
   - neplatnou hodnotou `DayName`.
6. Ověřit filtry aktivních sezón a kategorií.
7. Ověřit HTML encoding tooltipů.
8. Ověřit, že nebyla vytvořena ani změněna EF Core migrace nebo model snapshot.

## Mimo rozsah

- editace Training a TrainingPlan,
- změny databázového schématu,
- EF Core migrace,
- redirect původní route,
- sjednocení filtrů Schedule a Plan,
- interaktivní JavaScriptová timeline.
