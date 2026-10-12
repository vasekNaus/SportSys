# Implementační plán: #27 Import zápasů

**Issue:** [#27 — Import zápasů](https://github.com/vasekNaus/SportSys/issues/27)

**Stav:** Implementováno.

## Cíl

Spolehlivě importovat zápasy z exportu KIS `games.xlsx` do `sport.Match`.
Import musí při opakovaném spuštění rozpoznat zápasy již založené předchozím
KIS importem, aniž by vytvářel duplicitní nebo nepravděpodobně spárované
záznamy.

## Výchozí stav

- `Match` a zděděný `SportEvent` mají povinné datum, začátek, sezonu,
  kategorii, lokaci, oba týmy, typ a poznámku. Konec, výsledek, stav a kód
  zápasu mohou být nevyplněné nebo odvozené. `DurationMinutes` je persisted
  computed sloupec.
- `SeasonCategory` zatím neobsahuje délku zápasu ani rozbruslení. Její
  administrační formulář je generovaný z `SeasonCategoryEditDto` pomocí
  `@Html.EditorFor(m => m.Input)`.
- Dodaný `src/SportSys.ConsoleApp/games.xlsx` má list `Export`, dva řádky
  hlaviček a 203 datových řádků. Čtyři datové řádky nemají `Místo`, jeden
  nemá `Typ`. Tyto neúplné řádky nelze bezpečně uložit.

## Analýza atributů KIS

| Sloupec KIS | Cíl v modelu | Stav a pravidlo importu |
|---|---|---|
| `Datum` | `SportEvent.Date` | Přímý převod excelového data na `DateOnly`; je k dispozici ve všech datových řádcích. |
| `Čas` | `SportEvent.TimeFrom` | Přímý převod na `TimeOnly`; je k dispozici ve všech datových řádcích. |
| `Soupiska` | `SportEvent.SeasonCategoryCode` | Neexistuje zdrojový kód kategorie; hodnota se musí jednoznačně vyhledat proti `SeasonCategory.Name` v sezoně předané importu. |
| `Domácí` | `Match.HomeTeamId` | Jméno se musí jednoznačně vyhledat v `Team.Name`. |
| `Hosté` | `Match.AwayTeamId` | Jméno se musí jednoznačně vyhledat v `Team.Name`. |
| `Místo` | `SportEvent.LocationId` | Jméno se musí jednoznačně vyhledat v `Location.Name`; čtyři prázdné hodnoty se přeskočí s varováním. |
| `Typ` | `Match.MatchTypeId` | Explicitní mapování `Ligový zápas → League`, `Příprava → Friendly`, `Turnaj → Tournament`; jeden prázdný řádek se přeskočí s varováním. |

KIS neobsahuje žádný atribut, který by neměl věcný cíl v současném modelu.
Následující atributy modelu však z KIS nelze získat:

| Atribut modelu | Dopad |
|---|---|
| `SeasonId` | Neexistuje ve zdroji; bude povinným explicitním parametrem importu. |
| `MatchCode` | KIS nemá externí identifikátor; pro nově vložené zápasy zůstane `null`. |
| `TimeTo` | KIS uvádí pouze začátek; import jej odvodí jako `TimeFrom + SeasonCategory.MatchDurationMinutes`. |
| `Result` | Ve zdroji není výsledek; zůstane `null`. |
| `MatchStateId` | Ve zdroji není stav; použije se databázový výchozí stav `Plán` (`1`). |
| `Note` | Ve zdroji není poznámka; uloží se prázdný řetězec, který splňuje nenulový kontrakt modelu. |
| `DurationMinutes` | Neimportuje se, protože jde o computed sloupec. |

Trojice `LocationId + Date + TimeFrom`, kterou issue uvádí pro hledání
shody, není v KIS jedinečná: ve vzorku nastává 24 kolizí. Naproti tomu
kombinace `datum + čas + soupiska + domácí + hosté + místo` je mezi 203
datovými řádky jedinečná. Import proto nesmí trojici používat jako jediný
deduplikační klíč.

## Potvrzené požadavky a rozhodnutí

- Importuje se výhradně export KIS `games.xlsx`; svazový CSV import a
  pozůstatky jeho implementace se odstraní.
- KIS neposkytuje spolehlivý externí identifikátor. Import nejprve najde
  kandidáty podle kanonické lokace, data a času začátku a následně je
  jednoznačně ověří kategorií a oběma týmy.
- Neúplné, neznámé nebo nejednoznačné hodnoty nesmí vést k automatickému
  založení číselníku ani k odhadnutému spárování. Řádek se přeskočí a výsledek
  importu uvede důvod.
- `sport.SeasonCategory` se rozšíří o nenulové celočíselné sloupce
  `MatchDurationMinutes` a `WarmupDurationMinutes`. První vyjadřuje délku
  utkání od `Match.TimeFrom` do `Match.TimeTo`; druhý vyjadřuje délku
  rozbruslení před začátkem utkání.
- `Match.TimeFrom` zachová význam začátku utkání. Rozbruslení se proto
  nepřičítá k `TimeTo` a nemění začátek importovaného zápasu.
- Administrátor nastaví délku zápasu jako kladný počet minut a rozbruslení
  jako nezáporný počet minut pro každou sezonní kategorii.
- Změna databázového modelu a následná EF Core migrace jsou nutné. Agent
  změní model a konfiguraci; migraci vytvoří a aplikuje uživatel.

## Technický návrh

### Konfigurace délky zápasu podle kategorie

1. Rozšířit entitu `SeasonCategory` o `int MatchDurationMinutes` a
   `int WarmupDurationMinutes`. Minuty jsou správným doménovým typem pro
   interval, na rozdíl od `TimeOnly`, který vyjadřuje okamžik v rámci dne.
2. Doplnit obě hodnoty do `SeasonCategoryEditDto`,
   `SeasonCategoryListItem` a projekcí v `SeasonCategoryService`. Data
   annotations zajistí číselné pole generované EditorTemplates a serverovou i
   klientskou validaci: délka zápasu musí být větší než nula, délka
   rozbruslení nesmí být záporná.
3. `SeasonCategoryService.CreateAsync` a `UpdateAsync` hodnoty uloží a
   načtení pro editaci je vrátí. Index kategorií zobrazí obě délky v minutách,
   aby byla aktivní konfigurace ověřitelná bez otevření detailu.
4. Nová databázová migrace musí nejprve doplnit hodnoty existujících kategorií
   a až poté vynutit `NOT NULL`; nesmí zavést tichý produkční výchozí interval.
   Konkrétní délky historických kategorií stanoví uživatel při přípravě
   migrace. Agent migraci ani snapshot nevytváří.
5. `WarmupDurationMinutes` se v této změně pouze eviduje a administruje.
   Zobrazení samostatného bloku rozbruslení, rezervace ledu od začátku
   rozbruslení nebo posun `TimeFrom` nejsou součástí importu; konec
   importovaného utkání určuje výhradně `MatchDurationMinutes`.

### Importní služba a datový tok

1. Přidat do Contract vrstvu samostatnou službu `KisMatchImportService` a
   vstupní řádkový model odpovídající sedmi hlavičkám KIS. Služba ověří
   existenci souboru, list a úplnost očekávaných hlaviček, přeskočí opakovaný
   řádek hlaviček a bude zpracovávat excelové datum i čas ve formátech, které
   poskytuje `ClosedXML`.
2. Přijímat cestu k souboru a `seasonId` explicitně. Před zpracováním načíst
   pro danou sezonu kategorie, dále týmy, lokace, existující zápasy a pevný
   číselník typů do normalizovaných lookupů. `Soupiska` se bude porovnávat s
   `SeasonCategory.Name`, nikoliv s technickým kódem.
3. Pro každý plně rozpoznaný řádek vyhledat existující zápas nejprve podle
   `LocationId`, data a začátku; kandidáta přijmout pouze při shodě
   `SeasonCategoryCode`, `HomeTeamId` a `AwayTeamId`. Jedna shoda znamená
   „již existuje“ bez zápisu, nula shod znamená nový zápas a více shod nebo
   konflikt kandidátů znamená varování a přeskočení.
4. Nový zápas vytvořit přes sdílenou sekvenci `sport.SportEventSeq` se
   zjištěnými FK, prázdnou poznámkou,
   `TimeTo = TimeFrom.AddMinutes(MatchDurationMinutes)`, `MatchCode = null`
   a bez výsledku. Nevyplněním `MatchStateId` se uplatní současný databázový
   default `Plán`.
5. Výsledek importu rozšířit o počet již existujících zápasů a o strukturovaná
   varování s číslem řádku a příčinou. Zachovat současný přehled vložených a
   přeskočených řádků, aby konzolový běh zobrazil úplný audit.

### Načtení XLSX a spuštění

- Použít `ClosedXML` pro čtení XLSX a přidat jeho referenci do
  `SportSys.Contract`, protože parser a validační pravidla patří k aplikační
  službě. Referenci použít v konzistentní verzi `0.105.1`, kterou již používá
  `SportSys.Razor`. Odstranit nyní nadbytečnou referenci
  `ExcelDataReader.DataSet` z ConsoleApp; po odstranění historického
  `ImportRun.cs` ji konzolový projekt nepoužívá.
- Zaregistrovat novou službu v
  `SportSys.Contract/ServiceCollectionExtensions.cs` a v hostu
  `SportSys.ConsoleApp/Program.cs` ji zpřístupnit jako explicitně volaný
  konzolový import se souborem a ID sezony. Neaktivovat import automaticky při
  každém spuštění ConsoleApp.

## Implementační kroky

### Fáze 1: Délky zápasů v sezonní kategorii

1. Rozšířit `src/SportSys.Database/Models/sport/SeasonCategory.cs` o
   `MatchDurationMinutes` a `WarmupDurationMinutes` a doplnit odpovídající
   omezení do
   `src/SportSys.Database/Configurations/sport/SeasonCategoryConfiguration.cs`.
2. Doplnit hodnoty a validaci do
   `src/SportSys.Contract/Models/SeasonCategoryDto.cs` a kompletní datový tok
   vytvoření, čtení a úpravy do
   `src/SportSys.Contract/Services/SeasonCategoryService.cs`.
3. Zobrazit konfiguraci v
   `src/SportSys.Razor/Areas/sport/Pages/SeasonCategory/Index.cshtml`.
   Formulář `Edit.cshtml` se nemění: jeho existující `EditorFor` automaticky
   vytvoří dvě číselná pole podle rozšířeného DTO.
4. Připravit model pro migraci a předat uživateli požadavek na bezpečné
   doplnění hodnot u existujících kategorií. Nevytvářet ani neupravovat EF
   Core migraci nebo model snapshot.

### Fáze 2: Kontrakt importu KIS

1. V `src/SportSys.Contract/Models/` vytvořit výsledek importu zápasů pro KIS
   s počtem vložených, již existujících a přeskočených záznamů a se
   strukturovanými varováními.
2. Přidat `KisMatchImportService` do
   `src/SportSys.Contract/Services/`. Oddělit parsování řádku, mapování typů,
   normalizované lookupy a rozhodnutí vložit/již existuje/přeskočit, aby šly
   kritické scénáře testovat bez souborového systému a databáze.
3. Zajistit, že služba nikdy nevytváří `Team`, `Location`,
   `SeasonCategory` ani `MatchType`; chybějící reference musí být explicitní
   validační varování.

### Fáze 3: Integrace a závislosti

1. Upravit `src/SportSys.Contract/SportSys.Contract.csproj` pro čtení XLSX
   přes `ClosedXML` ve verzi `0.105.1`. Referenci `ExcelDataReader.DataSet` v
   `src/SportSys.ConsoleApp/SportSys.ConsoleApp.csproj` odstranit, protože po
   odstranění historického `ImportRun.cs` ji konzolový projekt nepoužívá.
2. Zaregistrovat `KisMatchImportService` v
   `src/SportSys.Contract/ServiceCollectionExtensions.cs` a doplnit
   odpovídající registraci/příkaz v `src/SportSys.ConsoleApp/Program.cs`.
3. Odstranit `CsvMatchImportService`, `CsvMatchImportResult`, jejich
   registrace, související unit test a zakomentovaný konzolový spouštěcí blok.
   Odstranit také vyloučený historický `ImportRun.cs` a položku
   `<Compile Remove="MatchImportRun.cs" />`, pokud cílový soubor již v
   projektu neexistuje.
4. Neměnit `Match`, `SportEvent`, jejich konfigurace, migrace ani model
   snapshot.

### Fáze 4: Testy a dokumentace

1. Doplnit unit testy v `tests/SportSys.Razor.Tests/` pro konfiguraci
   kategorie, parser a pravidla importu: validace obou minutových hodnot,
   platný řádek, druhá vložená hlavička, chybějící místo, chybějící typ,
   neznámá soupisku/tým/lokaci, mapování všech tří typů a konec odvozený jen
   z délky utkání.
2. Otestovat rozhodnutí o shodě: jediný plný kandidát je již existující
   zápas, stejná lokace/datum/čas s jinou kategorií nebo týmy se nesmí
   ztotožnit a nejednoznačné kandidáty musí skončit varováním.
3. Aktualizovat `docs/modules/sport.md` o zdroj KIS, mapování kategorie přes
   `SeasonCategory.Name`, chování pro chybějící vstupy a pravidlo
   vícesložkového párování.

## Soubory ke změně

| Soubor | Změna |
|---|---|
| `src/SportSys.Contract/Models/KisMatchImportResult.cs` | Výsledek KIS importu včetně počtu již existujících záznamů a varování. |
| `src/SportSys.Database/Models/sport/SeasonCategory.cs` | Dva nenulové sloupce konfigurace délky v minutách. |
| `src/SportSys.Database/Configurations/sport/SeasonCategoryConfiguration.cs` | Konfigurace databázových sloupců pro délky kategorie. |
| `src/SportSys.Contract/Models/SeasonCategoryDto.cs` | Editační a listovací DTO, názvy polí a validační pravidla délky. |
| `src/SportSys.Contract/Services/SeasonCategoryService.cs` | Čtení a zápis obou konfigurací kategorie. |
| `src/SportSys.Contract/Services/KisMatchImportService.cs` | Nová aplikační služba pro validaci, mapování, párování a vložení KIS XLSX. |
| `src/SportSys.Contract/SportSys.Contract.csproj` | Reference `ClosedXML` ve verzi používané Razor projektem. |
| `src/SportSys.Contract/ServiceCollectionExtensions.cs` | Registrace KIS importní služby. |
| `src/SportSys.ConsoleApp/SportSys.ConsoleApp.csproj` | Odstranění redundantní reference Excel readeru a zastaralé položky pro vyloučený soubor. |
| `src/SportSys.ConsoleApp/Program.cs` | Explicitní spuštění importu KIS s cestou k souboru a sezonou; odstranění zakomentovaného CSV importu. |
| `src/SportSys.Razor/Areas/sport/Pages/SeasonCategory/Index.cshtml` | Zobrazení délky utkání a rozbruslení v administraci kategorií. |
| `tests/SportSys.Razor.Tests/KisMatchImportModelTests.cs` | Testy parsování, mapování, validace a bezpečného párování. |
| `docs/modules/sport.md` | Provozní pravidla importu KIS. |
| `src/SportSys.Contract/Services/CsvMatchImportService.cs` | Odstranění nepoužívaného svazového CSV importu. |
| `src/SportSys.Contract/Models/CsvMatchImportResult.cs` | Odstranění výsledku nepoužívaného CSV importu. |
| `tests/SportSys.Razor.Tests/CsvMatchImportModelTests.cs` | Odstranění testu nepoužívaného CSV importu. |
| `src/SportSys.ConsoleApp/ImportRun.cs` | Odstranění nepoužívaného historického importního prototypu. |

## Testy a ověření

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
dotnet build SportSys.slnx
```

Testy musí zejména prokázat, že čtyři řádky bez místa a řádek bez typu z
dodávaného souboru nevedou k vložení zápasu, druhý řádek hlaviček se nepočítá
jako chyba dat a záznamy se stejnou lokací, datem a časem, ale jinými týmy
nejsou omylem ztotožněny. Dále musí ověřit, že kladná délka utkání prodlouží
`TimeTo`, zatímco délka rozbruslení konec utkání ani `TimeFrom` nemění.

## Manuální akceptace

1. Spustit import nad `games.xlsx` s existujícím ID sezony, jejíž
   `SeasonCategory.Name`, `Team.Name` a `Location.Name` pokrývají zdrojová
   data.
2. Ověřit souhrn vložených, již existujících a přeskočených řádků; přeskočené
   řádky musí uvádět číslo řádku a konkrétní chybějící či nerozpoznanou
   hodnotu.
3. Spustit import podruhé a ověřit, že nevloží duplicity.
4. Ověřit zápasy souběžné ve stejném místě a čase, například dvě utkání
   kategorií na stadionu v Klatovech; oba musí zůstat samostatnými záznamy.
5. Ověřit na rozvrhu, že nové zápasy mají správnou kategorii, soupeře,
   lokaci, typ a konec odvozený z délky utkání kategorie.
6. Upravovat kategorii sezóny v administraci a ověřit, že obě délky přijmou
   pouze platné celé minuty, po uložení se zobrazí v seznamu a ovlivní další
   import KIS.

## Beze změny

- Databázové entity a konfigurace mimo `SeasonCategory`, EF Core migrace a
  model snapshot.
- Razor UI mimo administraci sezonních kategorií, rozvrh zápasů a
  administrace týmů a lokací.

## Mimo rozsah

- Import výsledků, koncových časů, poznámek nebo stavů, které KIS neposkytuje.
- Automatické vytváření a geokódování chybějících týmů či lokací.
- Trvalé ukládání původu řádku nebo cizího identifikátoru; KIS ho neposkytuje.
- Vizualizace rozbruslení jako samostatného časového bloku či započtení do
  rezervace ledu.

## Hotovo, když

- KIS XLSX se importuje přes Contract službu s explicitní sezonou.
- Všech sedm sloupců má ověřené mapování a všechny chybějící atributy modelu
  mají zdokumentované bezpečné výchozí chování.
- Každá sezonní kategorie má administrativně spravovanou kladnou délku
  utkání a nezápornou délku rozbruslení v minutách; délka utkání určuje konec
  nově importovaného zápasu.
- Import nevytváří duplicitní ani nejednoznačně spárované zápasy.
- Chybějící či neznámé referenční hodnoty jsou dohledatelné ve varováních,
  nikoliv tiše domýšlené.
- Cílené testy a build projektu projdou.
