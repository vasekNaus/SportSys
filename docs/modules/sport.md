# Modul Sport

## Účel

Modul Sport spravuje sportovní číselníky a zobrazuje rozpisy tréninků. Administrační
stránky jsou v Razor Area `sport` a přistupují k databázi výhradně přes služby
projektu `SportSys.Contract`.

## Odpovědnosti

- Zobrazení reálného rozvrhu a obecného týdenního plánu.
- Read-only přehled požadavků na tréninky.
- Export filtrovaného rozvrhu do XLSX.
- Omezená editace reálných tréninků.
- Správa stadionů, týmů, sezon a kategorií sezon.

## Datový model

`Training` a `Match` jsou TPC potomci `SportEvent` se sdílenou sekvencí.
`TrainingPlan` popisuje obecný týdenní plán a `TrainingRequirement` požadovaný
rozsah. Vazební tabulky trenérů odkazují na stabilní `hr.Coach.Id`.

`TrainingGroup` a `TrainingPlanGroup` jsou nezávislé; jejich ID se mezi
reálnými tréninky a plány nekopíruje.

## Rozvrhy tréninků

| Stránka | Route | Zdroj dat |
|---|---|---|
| Reálný rozvrh | `/sport/Training/Schedule` | `sport.Training` |
| Obecný týdenní plán | `/sport/Training/Plan` | `sport.TrainingPlan` |
| Požadavky na tréninky | `/sport/Training/Requirement` | `sport.TrainingRequirement` |

Původní route `/sport/Schedule` není zachována.

## Požadavky na tréninky

Stránka `/sport/Training/Requirement` je read-only přehled požadavků pro
plánování sezóny. Zobrazuje sezónu, kategorii, typ a fázi tréninku, interval
platnosti, požadovaný rozsah v hodinách a přiřazené trenéry včetně jejich rolí.
Trenér bez zobrazovaného jména je identifikován osobním číslem; požadavek bez
trenéra zobrazuje `-`.

Přehled používá GET filtry:

- aktivní sezóna; výchozí je nejnovější aktivní sezóna,
- nula, jedna nebo více aktivních kategorií vybrané sezóny,
- nula, jeden nebo více typů tréninku,
- nula, jedna nebo více fází tréninku.

Prázdný výběr kategorií, typů nebo fází znamená všechny hodnoty. Neplatné
hodnoty z URL se před načtením dat odstraní. Stránka data pouze čte a nemění
databázové schéma ani obsah tabulek.

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

Obě stránky mají výchozí vypnutý GET filtr **Spojovat tréninky**. Po jeho
zapnutí `TrainingScheduleService` nad již načtenými DTO spojí položky ve
stejném datu nebo dni týdne, pokud se jejich časové intervaly překrývají nebo
se přesně dotýkají. Spojení je tranzitivní, takže řetězec navazujících
intervalů vytvoří jeden blok. Tato vizualizační skupina je oddělená od
databázového `GroupId` a nikdy se neukládá.

Při zapnutém filtru nejsou bloky editovatelné, protože jeden vizualizační blok
může obsahovat několik navzájem nesouvisejících databázových záznamů. Po
vypnutí filtru se obnoví běžné editační odkazy jednotlivých položek a
explicitních databázových skupin.

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
zobrazí se `-`. U plánů se zahrnou přiřazení z `CoachTrainingPlan`,
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
- datum od a do,
- volitelné spojování časově překrývajících se nebo navazujících tréninků.

Řádky odpovídají konkrétním datům z vybraného intervalu, včetně dnů bez tréninku.

Pokud rozvrh obsahuje alespoň jeden blok, lze aktuálně vyfiltrovaná data
exportovat do souboru `.xlsx`. Export obsahuje sloupce Kategorie, Datum, Čas od,
Čas do, Typ tréninku, Lokalita a Trenéři. Tréninky propojené přes
`sport.TrainingGroup` se exportují jako jeden řádek se stejným časovým rozsahem
a agregovanými hodnotami jako zobrazený blok. Při prázdném výsledku není
exportní akce dostupná.

Export respektuje filtr **Spojovat tréninky**. Při jeho zapnutí používá stejné
dočasné intervalové skupiny jako vizualizace, takže jeden zobrazený blok
odpovídá jednomu řádku exportu. Při vypnutém filtru zůstává seskupování omezené
na explicitní `sport.TrainingGroup`.

### Editace tréninku a tréninkového plánu

Kliknutím na blok reálného tréninku v `/sport/Training/Schedule` se v novém
panelu otevře `/sport/Training/Schedule/Edit?id={id}`. Kliknutím na blok
obecného plánu v `/sport/Training/Plan` se obdobně otevře
`/sport/Training/Plan/Edit?id={id}`.

Formulář umožňuje měnit pouze datum, čas od, čas do, lokalitu a poznámku.
Kategorie a typ tréninku jsou pouze informativní; fáze, stav, trenéři, vazba na
plán a členství ve skupině se nemění.

U spojených tréninků stránka zobrazí tabulku všech členů. Pokud mají všichni
členové shodné editovatelné hodnoty, uloží se změny atomicky celé skupině.
Pokud se alespoň jedna hodnota liší, stránka rozdíly zobrazí a editaci zablokuje
v UI i v Contract službě. `DurationMinutes` se při editaci nenastavuje v C#;
zůstává databázovým persisted computed sloupcem.

Editace tréninkového plánu používá stejný technický princip, ale mění pouze
platnost od a do, den týdne, čas od a do a lokalitu. Kategorie a typ jsou
informativní; fáze, trenéři a členství v `TrainingPlanGroup` se nemění.
U spojených plánů se kontroluje shoda všech editovatelných hodnot a
konzistentní skupina se ukládá atomicky. Hodnota `DayName` zůstává přesným
anglickým názvem dne `Monday` až `Sunday`.

### Filtry Plan

- aktivní sezóna,
- jedna nebo více aktivních kategorií,
- nula, jeden nebo více typů tréninku; prázdný výběr znamená všechny typy,
- nula, jedna nebo více lokalit; prázdný výběr znamená všechny lokality,
- jedna fáze tréninku,
- nepovinné datum platnosti; zobrazí plány, pro které platí
  `From <= datum <= To`,
- volitelné spojování časově překrývajících se nebo navazujících plánů.

Plan vždy vykreslí pondělí až neděli včetně prázdných dnů. Při nevyplněném
datu zobrazuje všechny odpovídající záznamy bez omezení podle `From–To`.
Při vyplněném datu se hranice platnosti vyhodnocují inkluzivně. Překrývající
se záznamy a plány s různými obdobími platnosti jsou rozděleny do samostatných
lanes, pokud není zapnuté spojování tréninků. Při zapnutém spojování se plány
ve stejném dni týdne seskupují pouze podle času; jejich období platnosti není
další podmínkou spojení. Platnost je uvedena v tooltipu.

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

## Tok zpracování

1. PageModel normalizuje GET filtry.
2. Contract služba načte projekci bez předání EF entit do UI.
3. Prezentační model seskupí propojené položky a vypočítá lanes.
4. Sdílená ViewComponent vykreslí časovou osu.
5. Editace tréninků, editace tréninkových plánů a export znovu načtou data
   v Contract vrstvě a ověří invarianty.

## Klíčové komponenty

| Komponenta | Cesta |
|---|---|
| Schedule služba | `src/SportSys.Contract/Services/TrainingScheduleService.cs` |
| Editace tréninku | `src/SportSys.Contract/Services/TrainingService.cs` |
| Editace tréninkového plánu | `src/SportSys.Contract/Services/TrainingPlanService.cs` |
| Požadavky | `src/SportSys.Contract/Services/TrainingRequirementService.cs` |
| ViewComponent | `src/SportSys.Razor/ViewComponents/TrainingScheduleViewComponent.cs` |
| Prezentační model | `src/SportSys.Razor/Models/TrainingSchedule/` |
| Razor Area | `src/SportSys.Razor/Areas/sport/Pages/` |

## Rozhraní

Veřejné routes jsou uvedeny v tabulce „Rozvrhy tréninků“. Administrační
číselníky používají dvojici `Index` a `Edit`; editace tréninku je dostupná na
`/sport/Training/Schedule/Edit?id={id}` a editace tréninkového plánu na
`/sport/Training/Plan/Edit?id={id}`.

## Integrační vazby

- Vazby na trenéry používají `hr.Coach.Id`.
- XLSX export vytváří Razor služba nad DTO z Contract vrstvy.

## Závislosti

- Sportovní entity používají SQL Server computed columns a sdílené sekvence.
- Administrační formuláře používají sdílené Razor EditorTemplates.

## Omezení a pravidla

Agent upravuje modely a EF Core konfigurace, ale nikdy nevytváří ani neupravuje
migrace nebo model snapshot. Vytvoření a aplikaci migrace provádí výhradně
uživatel.

Neplatné hodnoty `TrainingPlan.DayName` nesmí být tiše ignorovány.
`DurationMinutes` se nikdy nenastavuje v C#.

## Příklady

Příkladem agregace je blok `U12 + U14`, který spojí kategorie, typy a trenéry
v rámci jedné skupiny, ale zachová bezpečně enkódovaný tooltip.

## Odkazovaná dokumentace

- `docs/architecture.md`
- `docs/conventions.md`
- `docs/modules/hr.md`
- `.github/skills/editor-template/SKILL.md`
