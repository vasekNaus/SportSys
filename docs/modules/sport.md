# Modul Sport

## Účel

Modul Sport spravuje sportovní číselníky a zobrazuje rozpisy tréninků. Administrační
stránky jsou v Razor Area `sport` a přistupují k databázi výhradně přes služby
projektu `SportSys.Contract`.

## Rozvrhy tréninků

| Stránka | Route | Zdroj dat |
|---|---|---|
| Reálný rozvrh | `/sport/Training/Schedule` | `sport.Training` |
| Obecný týdenní plán | `/sport/Training/Plan` | `sport.TrainingPlan` |

Původní route `/sport/Schedule` není zachována.

### Společný datový kontrakt

`ITrainingScheduleItem` definuje vlastnosti potřebné pro vykreslení bloku na
časové ose. `TrainingPlanScheduleItemDto` interface implementuje a obsahuje navíc
období `From–To` a `DayName`.

`TrainingScheduleItemDto` přímo dědí z `TrainingPlanScheduleItemDto` a přidává
konkrétní `Date`. U reálného tréninku jsou zděděné hodnoty nastaveny jako plán
platný právě v den tréninku.

### Sdílená ViewComponent

Obě stránky předávají data přes `ITrainingScheduleViewModel` komponentě:

- třída: `src/SportSys.Razor/ViewComponents/TrainingScheduleViewComponent.cs`,
- view: `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`,
- prezentační modely: `src/SportSys.Razor/Models/TrainingSchedule/`.

Komponenta pouze vykresluje předaná data. Zajišťuje časové markery, dynamický
rozsah osy, rozdělení překryvů do lanes, barvy kategorií a bezpečně HTML
enkódované tooltipy. Data načítají PageModely přes `TrainingScheduleService`.

Spojené položky používají dvě nezávislé vazební tabulky. `sport.TrainingGroup`
sdružuje pouze reálné tréninky a `sport.TrainingPlanGroup` pouze tréninkové
plány. Více členských řádků se stejným `GroupId` tvoří skupinu; položka bez
členského řádku zůstává samostatná. Shodná hodnota `GroupId` v obou tabulkách
nevyjadřuje vzájemnou vazbu.

Komponenta seskupuje položky pouze uvnitř aktuálního řádku a teprve potom
rozděluje výsledné bloky do lanes. Titulek spojeného bloku obsahuje názvy všech
kategorií oddělené ` + ` a seřazené podle `SeasonCategory.Order`, názvu
kategorie a ID položky. Časový rozsah vede od nejčasnějšího začátku po
nejpozdější konec; případná časová mezera mezi členy je tedy součástí
společného bloku. Barvu určuje první kategorie a tooltip zachovává informace
o všech členech oddělené ` | `. Nepropojené kolidující bloky zůstávají
v samostatných lanes.

Legenda se sestavuje z výsledných bloků, nikoliv přímo ze seznamu vybraných
kategorií. Spojený blok se proto v legendě zobrazí pod stejným názvem jako
v rozvrhu, například `U12 + U14`, a každá kombinace je uvedena pouze jednou.

Každý blok zobrazuje čtyři řádky: kategorie, čas, unikátní typy tréninku a
unikátní osobní čísla přiřazených trenérů. Údaje spojeného bloku se agregují ze
všech jeho členů a oddělují čárkou. Pokud trénink nemá přiřazeného trenéra,
zobrazí se `Bez trenéra`. U plánů se zahrnou přiřazení z `CoachTrainingPlan`,
jejichž interval platnosti se překrývá s intervalem `TrainingPlan.From–To`.
Osobní číslo je dočasným identifikátorem do zavedení vazby `hr.Coach` na
`identity.User`.

Při materializaci více reálných tréninků z propojených plánů se pro vzniklé
tréninky vytvoří nová skupina v `TrainingGroup`. Identifikátor skupiny z
`TrainingPlanGroup` se mezi tabulkami nekopíruje.

PageModel určuje typovanou paritu každého řádku. Schedule ji odvozuje z čísla
dne v měsíci, takže zůstává stabilní i při změně začátku intervalu. Plan ji
odvozuje z pořadí pondělí až neděle, kde pondělí je liché a úterý sudé.
ViewComponent převádí paritu na CSS variantu řádku a kombinuje ji s nezávislým
víkendovým zvýrazněním.

### Filtry Schedule

- aktivní sezóna,
- jedna nebo více aktivních kategorií,
- nula, jeden nebo více typů tréninku; prázdný výběr znamená všechny typy,
- nula, jedna nebo více lokalit; prázdný výběr znamená všechny lokality,
- datum od a do.

Řádky odpovídají konkrétním datům z vybraného intervalu, včetně dnů bez tréninku.

Pokud rozvrh obsahuje alespoň jeden blok, lze aktuálně vyfiltrovaná data
exportovat do souboru `.xlsx`. Export obsahuje sloupce Kategorie, Datum, Čas od,
Čas do, Typ tréninku, Lokalita a Trenéři. Tréninky propojené přes
`sport.TrainingGroup` se exportují jako jeden řádek se stejným časovým rozsahem
a agregovanými hodnotami jako zobrazený blok. Při prázdném výsledku není
exportní akce dostupná.

### Editace tréninku

Kliknutím na blok reálného tréninku v `/sport/Training/Schedule` se v novém
panelu otevře `/sport/Training/Edit?id={id}`. Bloky obecných plánů na
`/sport/Training/Plan` editační odkaz nemají.

Formulář umožňuje měnit pouze datum, čas od, čas do, lokalitu a poznámku.
Kategorie a typ tréninku jsou pouze informativní; fáze, stav, trenéři, vazba na
plán a členství ve skupině se nemění.

U spojených tréninků stránka zobrazí tabulku všech členů. Pokud mají všichni
členové shodné editovatelné hodnoty, uloží se změny atomicky celé skupině.
Pokud se alespoň jedna hodnota liší, stránka rozdíly zobrazí a editaci zablokuje
v UI i v Contract službě. `DurationMinutes` se při editaci nenastavuje v C#;
zůstává databázovým persisted computed sloupcem.

### Filtry Plan

- aktivní sezóna,
- jedna nebo více aktivních kategorií,
- nula, jeden nebo více typů tréninku; prázdný výběr znamená všechny typy,
- jedna fáze tréninku.

Plan vždy vykreslí pondělí až neděli včetně prázdných dnů. Zobrazuje všechny
odpovídající záznamy bez omezení podle `From–To`; překrývající se záznamy a plány
s různými obdobími platnosti jsou rozděleny do samostatných lanes. Platnost je
uvedena v tooltipu.

`TrainingPlan.DayName` musí obsahovat přesnou anglickou hodnotu `Monday` až
`Sunday`. Neplatná hodnota vyvolá explicitní chybu a není tiše přeskočena.

## Spravované číselníky

| Číselník | Databázová entita | Administrační stránky |
|---|---|---|
| Zimní stadiony | `sport.IceRink` | `Areas/sport/Pages/IceRink/` |
| Týmy | `sport.Team` | `Areas/sport/Pages/Team/` |
| Sezóny | `sport.Season` | `Areas/sport/Pages/Season/` |
| Kategorie sezón | `sport.SeasonCategory` | `Areas/sport/Pages/SeasonCategory/` |

Enumové lookup tabulky `TrainingType`, `TrainingState`, `TrainingPhase`,
`ParticipationType` a `MatchType` se přes administrační UI nespravují. Jejich
identifikátory jsou svázané s C# enumy a seed konfigurací.

## Administrační vzor

Každý číselník používá dvojici stránek:

- `Index` obsahuje textový filtr, filtr stavu, tabulku a řádkové akce.
- `Edit` je jediný formulář pro vytvoření i úpravu záznamu.

Formuláře používají `@Html.EditorFor` a šablony v
`src/SportSys.Razor/Pages/Shared/EditorTemplates/`. Validační a zobrazovací
metadata jsou definována pomocí DataAnnotations na DTO v `SportSys.Contract`.

## Aktivita místo mazání

Entity `IceRink`, `Team`, `Season` a `SeasonCategory` mají příznak `IsActive`.
Fyzické mazání se v administraci nepoužívá.

- Výchozí hodnota `IsActive` je `true`.
- Každý DEFAULT constraint má název `DF_{Entity}_IsActive`.
- Index standardně zobrazuje pouze aktivní záznamy.
- Filtr umožňuje zobrazit aktivní, neaktivní nebo všechny záznamy.
- Záznam lze zneaktivnit z Indexu i z editačního formuláře.
- Neaktivní záznam lze znovu aktivovat.
- Zneaktivnění se nekaskáduje na související entity.

## Specifika entit

### IceRink

Administrace spravuje `Name`, `Street`, `City`, `ZipCode` a `IsActive`.
Geografické pole `Location` není součástí formuláře.

### Team

Administrace spravuje `Code`, `Name`, `Address`, `City`, `HomeIceRinkId` a
`IsActive`. Výběr domácího stadionu nabízí aktivní stadiony a při editaci zachová
i aktuálně přiřazený neaktivní stadion.

### Season

Administrace spravuje `Name`, `From`, `To` a `IsActive`. Platí invariant
`From <= To`.

### SeasonCategory

Entita má složený primární klíč `SeasonId + Name`. Při vytvoření jsou obě části
klíče povinné; při editaci jsou neměnné. Formulář dále spravuje `Order`,
`CompetitionCode`, `CompetitionTeamName`, `BirthYears` a `IsActive`.

## Contract služby

| Služba | Odpovědnost |
|---|---|
| `IceRinkService` | CRUD bez fyzického mazání, filtrování, seznam stadionů |
| `TeamService` | CRUD bez fyzického mazání a filtrování týmů |
| `SeasonService` | CRUD bez fyzického mazání, filtrování, seznam sezón |
| `SeasonCategoryService` | CRUD bez změny složeného klíče a filtrování kategorií |

Změna aktivity se provádí explicitní metodou `SetActiveAsync`. Služby nikdy
nevracejí databázové entity do Razor vrstvy.

## Databázové změny

Agent upravuje modely a EF Core konfigurace, ale nikdy nevytváří ani neupravuje
migrace nebo model snapshot. Vytvoření a aplikaci migrace provádí výhradně
uživatel.
