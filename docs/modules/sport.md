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
| Reálný rozvrh | `/sport/Training/Schedule` | `sport.Training`, `sport.Match` |
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

### Společné datové kontrakty

`SportEventDto` je Contract předek skutečných datovaných sportovních událostí.
`TrainingScheduleItemDto` a `MatchScheduleItemDto` z něj dědí a sdílejí datum,
časový interval, sezonu, kategorii a poznámku. Tréninkový plán není sportovní
událostí a používá samostatný `TrainingPlanScheduleItemDto`.

`ITrainingScheduleItem` zůstává technickým kontraktem pro seskupování tréninků
a tréninkových plánů. Zápasy se do tréninkových skupin nikdy nezapojují.

Zápasy načítá samostatná `MatchScheduleService`. Soupeře a domácí/venkovní roli
určuje podle `SeasonCategory.CompetitionTeamName`; nekonzistentní zápas bez
jednoznačně určeného vlastního týmu vyvolá explicitní chybu.

### Sdílená ViewComponent

Obě stránky předávají data přes `ITrainingScheduleViewModel` komponentě:

- třída: `src/SportSys.Razor/ViewComponents/TrainingScheduleViewComponent.cs`,
- view: `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`,
- prezentační modely: `src/SportSys.Razor/Models/TrainingSchedule/`.

Komponenta pouze vykresluje obecné `ScheduleEventModel`. Zajišťuje časové
markery, dynamický rozsah osy, společné rozdělení tréninků a zápasů do lanes,
barvy kategorií a bezpečně HTML enkódované tooltipy. DTO se na renderovací
model převádějí před vstupem do ViewComponenty.

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

Blok tréninku a tréninkového plánu zobrazuje pět pevných řádků v tomto
pořadí: kategorie, čas od–do (tučně, nejvýraznější informace v bloku), název
lokality a příjmení přiřazených trenérů (zalomená na nejvýše dva řádky —
modifikátor `schedule-block-coaches--wrap`, aby se vešli i trenéři u
spojených tréninků více kategorií). U tréninkového plánu s neprázdnou
textovou vlastností `sport.TrainingPlan.Title` zobrazuje řádek lokality
text ve tvaru `Title - Lokalita`; pokud je `Title` prázdný, zobrazí se jen
název lokality beze změny (`TrainingPlanEventModel.PlanTitleSummary`,
`EventModelFactory.CreateTrainingPlanBlock`). U spojeného bloku více plánů
se neprázdné hodnoty `Title` deduplikují a spojují čárkou. Reálný trénink
(`Training`) sloupec `Title` nemá a jeho řádek lokality se tímto nemění.
Blok zápasu používá stejné pořadí
vykreslovaných pozic (kategorie, čas, třetí a čtvrtý řádek), ale obsah
třetího a čtvrtého řádku je zápasu vlastní: třetí řádek zobrazuje výsledek
a čtvrtý soupeře (`ScheduleEventModelFactory.CreateMatch`), ne lokalitu a
trenéry; jde jen o sjednocení vizuálního pořadí napříč typy bloků, ne o
sdílený význam dat. Typ
tréninku (led / suchá příprava) se v bloku nevypisuje textem — rozlišuje ho
vizuální vykreslení pozadí: led má plnou barvu podle kategorie beze změny,
suchá příprava stejnou barvu doplněnou o jemné diagonální šrafování
(modifikátor `schedule-block--dry`, CSS `repeating-linear-gradient`, bez
obrázků a bez JS). Příjmení trenéra se odvozuje jako poslední mezerou
oddělené slovo z `User.DisplayName` — výpočet provádí jednou
`TrainingScheduleService` (Contract) a předává přes DTO `SimpleCoachDto`
(`FullName` + `LastName`), Razor vrstva už žádný řetězec neparsuje. Údaje
spojeného bloku se agregují ze všech jeho členů a oddělují čárkou. Pokud
trénink nemá přiřazeného trenéra, zobrazí se `-`. U plánů se zahrnou
přiřazení z `CoachTrainingPlan`, jejichž interval platnosti se překrývá s
intervalem `TrainingPlan.From–To`. Pokud spojený blok obsahuje položky s
různým typem tréninku (led i suchá příprava), šrafování se nezobrazí a blok
se vykreslí jako plná barva — stejný bezpečný fallback jako u smíšeného
stavu tréninku (`HasMixedState`). Celá jména trenérů i text typu tréninku
zůstávají dostupné v tooltipu bloku a v Excel exportu rozvrhu
(`TrainingScheduleBlockData.CoachSummary`/`TrainingTypeSummary`), export se
touto úpravou nemění.

Zápasový blok používá stejnou časovou osu, ale zobrazuje kategorii, čas,
soupeře a výsledek. Neznámý výsledek se zobrazuje jako `-`. Zápas nemá
editační odkaz, ale zobrazuje stavovou ikonu podle `Match.MatchStateId`
(viz níže). Překryv zápasu s tréninkem nebo jiným zápasem vytvoří další lane.

Při materializaci více reálných tréninků z propojených plánů se pro vzniklé
tréninky vytvoří nová skupina v `TrainingGroup`. Identifikátor skupiny z
`TrainingPlanGroup` se mezi tabulkami nekopíruje.

Reálný trénink navíc nese stav (`sport.TrainingState`). Kategorie se nikdy
nebarví podle stavu; vizualizace je omezena na ikonu v pravém dolním rohu
bloku. Mapování `TrainingStateId → ikona` je v
`src/SportSys.Razor/Models/TrainingSchedule/TrainingStateVisual.cs`. Pokud
blok obsahuje jediný trénink nebo spojené tréninky se shodným stavem, zobrazí
se ikona tohoto stavu bez tooltipu. Pokud mají spojené tréninky rozdílné
stavy, zobrazí se místo toho sentinel „Stav neznámý“ (`TrainingStateVisual.UnknownIcon`,
❓); po najetí myší na tuto ikonu se zobrazí tooltip s rozpisem `ikona + název
kategorie` pro každý dílčí trénink, seřazený podle `SeasonCategory.Order`.
Plán (`TrainingPlan`) stav nemá, takže stránka `/sport/Training/Plan` ikonu
nikdy nezobrazuje.

Zápas nese vlastní, samostatný stav (`sport.MatchState`, sloupec
`Match.MatchStateId`), nezávislý na číselníku stavů tréninku. Hodnoty jsou
`1. Plán`, `2. Potvrzený` a `3. Zrušený`. Sloupec je nullable — historické a
importované zápasy bez zdroje dat mohou mít stav nevyplněný a blok pak ikonu
nezobrazí. Nově založený zápas dostává výchozí hodnotu `1` (Plán) přímo z
databáze (`DEFAULT` na sloupci), pokud volající kód hodnotu nenastaví
explicitně. Mapování `MatchStateId → ikona` je v
`src/SportSys.Razor/Models/TrainingSchedule/MatchStateVisual.cs`; na rozdíl od
tréninku zápas nikdy nevytváří spojené bloky, takže odpadá sentinel pro
smíšený stav.

Při aktivním GET filtru **Spojovat tréninky** (`MergeTrainings`, tedy
`AllowEditing == false`) se stavová ikona ani tooltip nezobrazují na žádné
stránce (Schedule i Plan), protože takový blok reprezentuje jen časově
spojený interval, ne skutečný společně evidovaný trénink.

PageModel určuje typovanou paritu každého řádku. Schedule ji odvozuje z čísla
dne v měsíci, takže zůstává stabilní i při změně začátku intervalu. Plan ji
odvozuje z pořadí pondělí až neděle, kde pondělí je liché a úterý sudé.
ViewComponent převádí paritu na CSS variantu řádku a kombinuje ji s nezávislým
víkendovým zvýrazněním.

### Filtry Schedule

Formulář je vizuálně seskupen do panelů: přepínač „Zobrazit v rozvrhu“ nahoře,
pod ním panel „Společné filtry“ a dále (podle stavu přepínače) panely
„Tréninky“ a „Zápasy“ vedle sebe s jemným barevným akcentem (bez vlivu na
filtrovací logiku). Pod panely je akční lišta s odkazem „Vymazat filtry“
(GET navigace na stránku bez parametrů, obnoví výchozí hodnoty všech filtrů)
a tlačítkem „Použít filtry“.

- přepínač „Zobrazit v rozvrhu“ — dva nezávislé checkboxy „Tréninky“ /
  „Zápasy“ (výchozí oba zapnuté); podle nich se zobrazují jen relevantní
  specifické filtry a načítají jen relevantní data,
- aktivní sezóna,
- jedna nebo více aktivních kategorií (společné pro tréninky i zápasy),
- nula, jedna nebo více lokalit; prázdný výběr znamená všechny lokality —
  filtr je společný a omezuje tréninky i zápasy zároveň,
- datum od a do,
- nula, jeden nebo více dnů v týdnu (checkbox skupina Pondělí–Neděle); prázdný
  výběr znamená všechny dny. Filtr omezuje přímo načtená data (tréninky i
  zápasy) stejně jako ostatní společné filtry — při výběru jen vybraných dnů
  se načítají a zobrazují (i exportují) výhradně bloky spadající na tyto dny,
  ostatní dny se v intervalu Datum od–do zobrazují jako řádky bez položek
  (podle volby „Zobrazovat prázdné řádky“ stejně jako ostatní dny bez
  tréninku/zápasu),
- při zapnutém „Tréninky“: nula, jeden nebo více typů tréninku (prázdný výběr
  = všechny typy), nula, jeden nebo více stavů tréninku (prázdný výběr =
  všechny stavy) a volitelné spojování časově překrývajících se nebo
  navazujících tréninků,
- při zapnutém „Zápasy“: nula, jeden nebo více typů zápasu (prázdný výběr =
  všechny typy) a nula, jeden nebo více stavů zápasu (prázdný výběr = všechny
  stavy).

Řádky odpovídají konkrétním datům z vybraného intervalu, včetně dnů bez tréninku.
Sezóna, kategorie, datum, den v týdnu a lokalita omezují tréninky i zápasy. Typ
tréninku, stav tréninku a volba spojování se vztahují pouze na tréninky; zápasy
se nikdy neslučují s tréninky ani mezi sebou. Typ zápasu a stav zápasu se
vztahují pouze na zápasy a používají odlišné číselníky
(`sport.MatchType`, `sport.MatchState`) než tréninkové filtry; zápas s
nevyplněným `MatchStateId` se do vybraných stavů nepočítá.

Pokud jsou vypnuté oba přepínače „Tréninky“ i „Zápasy“, rozvrh nenačítá ani
nezobrazuje žádné položky (zůstávají jen prázdné, resp. skryté řádky podle
volby „Zobrazovat prázdné řádky“).

Pokud rozvrh obsahuje alespoň jeden blok, lze aktuálně vyfiltrovaná data
exportovat do souboru `.xlsx`. Export obsahuje sloupce Kategorie, Datum, Čas od,
Čas do, Typ tréninku, Lokalita, Trenéři a Stav. Tréninky propojené přes
`sport.TrainingGroup` se exportují jako jeden řádek se stejným časovým rozsahem
a agregovanými hodnotami jako zobrazený blok. Sloupec Stav obsahuje název stavu
pouze u samostatného tréninku nebo u spojených tréninků se shodným stavem;
pokud mají spojené tréninky rozdílné stavy, zůstává buňka prázdná. Při
prázdném výsledku není exportní akce dostupná.

Export respektuje filtr **Spojovat tréninky**. Při jeho zapnutí používá stejné
dočasné intervalové skupiny jako vizualizace, takže jeden zobrazený blok
odpovídá jednomu řádku exportu. Při vypnutém filtru zůstává seskupování omezené
na explicitní `sport.TrainingGroup`.

Export zůstává výhradně exportem tréninků. Zápasy se do XLSX nezapisují a den
obsahující pouze zápasy exportní akci nenabízí.

### Časový interval zápasu

`Match` ukládá `TimeFrom` a `TimeTo`; `DurationMinutes` je persisted computed
sloupec. Import svazového CSV obsahuje pouze začátek, proto novému zápasu
nastaví `TimeTo = TimeFrom`. Takový zápas má délku `0` a koncový čas lze
následně ručně upřesnit. Vizualizace nulový interval vykreslí minimální
čitelnou šířkou a dvě bodové události ve stejném čase rozdělí do různých lanes.

### Editace tréninku a tréninkového plánu

Kliknutím na blok reálného tréninku v `/sport/Training/Schedule` se v novém
panelu otevře `/sport/Training/Schedule/Edit?id={id}`. Kliknutím na blok
obecného plánu v `/sport/Training/Plan` se obdobně otevře
`/sport/Training/Plan/Edit?id={id}`.

Formulář umožňuje měnit pouze datum, čas od, čas do, lokalitu a poznámku.
Lokalita je povinný výběr ze společného číselníku; nově zvolená neaktivní
lokalita nebo neexistující ID jsou odmítnuty i Contract službou. Aktuálně
přiřazená neaktivní lokalita zůstává při editaci dostupná.
Kategorie a typ tréninku jsou pouze informativní; fáze, stav, trenéři, vazba na
plán a členství ve skupině se nemění.

U spojených tréninků stránka zobrazí tabulku všech členů. Pokud mají všichni
členové shodné editovatelné hodnoty, uloží se změny atomicky celé skupině.
Pokud se alespoň jedna hodnota liší, stránka rozdíly zobrazí a editaci zablokuje
v UI i v Contract službě. `DurationMinutes` se při editaci nenastavuje v C#;
zůstává databázovým persisted computed sloupcem.

Editace tréninkového plánu používá stejný technický princip, ale mění pouze
platnost od a do, den týdne, čas od a do, lokalitu a přiřazené trenéry.
Kategorie a typ jsou informativní; fáze a členství v `TrainingPlanGroup` se
nemění. U spojených plánů se kontroluje shoda všech editovatelných hodnot a
konzistentní skupina se ukládá atomicky. Hodnota `DayName` zůstává přesným
anglickým názvem dne `Monday` až `Sunday`.

Pole **Trenéři** je multivýběr založený na stejné komponentě
(`data-multiselect`) jako filtry stránky Plan; umožňuje vybrat libovolný
počet trenérů včetně žádného. U nespojeného plánu je pole jedno a váže se na
`TrainingPlan.Id` editovaného plánu. U spojeného plánu (skupina s více
tréninky, např. `U12 + U14`) stránka místo jednoho sdíleného pole zobrazuje
tabulku Kategorie + Trenér s vlastním multivýběrem pro každý člen skupiny —
protože editace celé skupiny má jediný vstupní bod (`EditItemId =
block.MinimumItemId`), jinak by trenéři ostatních členů nebyli editovatelní.

> **Dočasný stav (HACK):** Validace, že jeden trenér smí být v rámci skupiny
> přiřazen nejvýše k jednomu tréninku, je v
> `TrainingPlanService.UpdateAsync` dočasně vypnutá (zakomentovaná, označená
> komentářem `HACK`), takže stejný trenér může být nyní přiřazen k více
> spojeným plánům současně. `TrainingPlanUpdateResult.DuplicateCoachAssignment`
> i pomocná metoda `HasDuplicateCoachAcrossPlans` v kódu zůstávají
> zachované pro případné obnovení validace. Jde o přechodné řešení do
> finálního rozhodnutí; tento odstavec je potřeba opravit zpět (nebo kód
> trvale odstranit), jakmile padne finální verdikt.

Při uložení se vybraní trenéři (za každý `TrainingPlan.Id` ve skupině
samostatně) sesynchronizují s `sport.CoachTrainingPlan`: nově vybraní se
přidají, odebraní se smažou a u ponechaných se interval platnosti
(`ValidFrom`/`ValidTo`) nastaví na aktuální `From`/`To` plánu, pokud se liší.
Neplatná nebo neexistující ID trenérů i cizí/neexistující ID tréninku
z requestu se tiše ignorují. Verze pro optimistickou konkurenci
(`TrainingPlan.OriginalVersion`) zahrnuje přiřazené trenéry všech členů
skupiny, takže souběžná změna trenéra u jiného člena skupiny je detekována
jako konflikt.

### Filtry Plan

Celý filtr je vizuálně obalen jedním panelem „Tréninky“ (ikona
`fa-person-skating`, levý barevný akcent `--color-brand-primary`), shodně se
stejnojmenným panelem na stránce Schedule — stránka Plan řeší jen tréninky,
takže není potřeba dělit filtry na společné/specifické jako na Schedule.
Vnořené Kategorie používají stejnou třídu `schedule-filter-categories--nested`
jako na Schedule. Tlačítko „Zobrazit plán“ zůstává beze změny; pod panelem je
akční lišta s odkazem „Vymazat filtry“ (GET navigace na stránku bez
parametrů, obnoví výchozí hodnoty všech filtrů, stejné provedení jako na
stránce Schedule).

- aktivní sezóna,
- jedna nebo více aktivních kategorií,
- nula, jeden nebo více typů tréninku; prázdný výběr znamená všechny typy,
- nula, jedna nebo více lokalit; prázdný výběr znamená všechny lokality,
- nula nebo jedna fáze tréninku; prázdný výběr znamená všechny fáze,
- nepovinné datum platnosti; zobrazí plány, pro které platí
  `From <= datum <= To`,
- nula, jeden nebo více dnů v týdnu (checkbox skupina za polem „Datum
  platnosti“); prázdný výběr znamená všechny dny. Na rozdíl od ostatních
  filtrů zde vybrané dny přímo určují, které řádky (Pondělí–Neděle) se vůbec
  vykreslí — pro nevybrané dny se nezobrazí ani prázdný řádek, bez ohledu na
  volbu „Zobrazovat prázdné řádky“,
- volitelné spojování časově překrývajících se nebo navazujících plánů.

Plan bez filtru „Den“ vykreslí pondělí až neděli včetně prázdných dnů. Při
nevyplněném datu zobrazuje všechny odpovídající záznamy bez omezení podle
`From–To`. Při vyplněném datu se hranice platnosti vyhodnocují inkluzivně.
Překrývající se záznamy a plány s různými obdobími platnosti jsou rozděleny
do samostatných lanes, pokud není zapnuté spojování tréninků. Při zapnutém
spojování se plány ve stejném dni týdne seskupují pouze podle času; jejich
období platnosti není další podmínkou spojení. Platnost je uvedena v
tooltipu.

`TrainingPlan.DayName` musí obsahovat přesnou anglickou hodnotu `Monday` až
`Sunday`. Neplatná hodnota vyvolá explicitní chybu a není tiše přeskočena.

## Spravované číselníky

| Číselník | Databázová entita | Administrační stránky |
|---|---|---|
| Lokality | `sport.Location` | `Areas/sport/Pages/Location/` |
| Týmy | `sport.Team` | `Areas/sport/Pages/Team/` |
| Sezóny | `sport.Season` | `Areas/sport/Pages/Season/` |
| Kategorie sezón | `sport.SeasonCategory` | `Areas/sport/Pages/SeasonCategory/` |

Enumové lookup tabulky `TrainingType`, `TrainingState`, `TrainingPhase`,
`ParticipationType`, `MatchType` a `MatchState` se přes administrační UI
nespravují. Jejich identifikátory jsou svázané s C# enumy a seed konfigurací.

## Administrační vzor

Každý číselník používá dvojici stránek:

- `Index` obsahuje textový filtr, filtr stavu, tabulku a řádkové akce.
- `Edit` je jediný formulář pro vytvoření i úpravu záznamu.

Formuláře používají `@Html.EditorFor` a šablony v
`src/SportSys.Razor/Pages/Shared/EditorTemplates/`. Validační a zobrazovací
metadata jsou definována pomocí DataAnnotations na DTO v `SportSys.Contract`.

## Aktivita místo mazání

Entity `Location`, `Team`, `Season` a `SeasonCategory` mají příznak `IsActive`.
Fyzické mazání se v administraci nepoužívá.

- Výchozí hodnota `IsActive` je `true`.
- Každý DEFAULT constraint má název `DF_{Entity}_IsActive`.
- Index standardně zobrazuje pouze aktivní záznamy.
- Filtr umožňuje zobrazit aktivní, neaktivní nebo všechny záznamy.
- Záznam lze zneaktivnit z Indexu i z editačního formuláře.
- Neaktivní záznam lze znovu aktivovat.
- Zneaktivnění se nekaskáduje na související entity.

## Specifika entit

### Location

Administrace spravuje `Name`, volitelně `Street`, `City`, `ZipCode` a
`IsActive`. Geografické pole `GeographicLocation` není součástí formuláře.
Dosavadní stadiony zůstávají lokacemi včetně adresních údajů; lokality
vzniklé převodem textových tréninků adresu nemají.

### Team

Administrace spravuje `Code`, `Name`, `Address`, `City`, `HomeLocationId` a
`IsActive`. Výběr domácí lokality nabízí aktivní lokality a při editaci zachová
i aktuálně přiřazenou neaktivní lokalitu.

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
| `SportLocationService` | CRUD bez fyzického mazání, filtrování a seznam sportovních lokalit |
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

## Ruční převod databáze pro #16

EF Core migrace se pro sjednocení lokalit nevytváří. Na záloze produkční
databáze se spustí v tomto pořadí:

1. `src/DB Model/Migration/16_00_location_preflight.sql`;
2. `src/DB Model/Migration/16_01_location_migration.sql` nejprve s
   `@Commit = 0`, potom po kontrole s `@Commit = 1`;
3. `src/DB Model/Migration/16_02_location_verify.sql`.

Preflight záměrně zastaví převod při prázdných, příliš dlouhých nebo
nejednoznačně přiřaditelných textových lokalitách tréninku. Skripty přejmenují
stadiony na lokality, vytvoří chybějící lokality z textů tréninků a zachovají
vazby zápasů i domácích lokalit týmů.

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
