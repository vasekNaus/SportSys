# Implementační plán: #28 Import tréninků

**Issue:** [#28 — Import tréninků](https://github.com/vasekNaus/SportSys/issues/28)

**Stav:** Připraveno k implementaci.

## Cíl

Přidat na `/sport/Training/Schedule` bezpečný import XLSX exportu KIS, který
potvrdí automaticky vytvořené tréninky z plánů, označí a aktualizuje změněné
časy a založí chybějící tréninky se stavem `Jen KIS`.

## Výchozí stav

- `sport.Training` ukládá sezonu, kategorii, datum, interval, lokaci, typ,
  fázi a stav. `TrainingPhaseId` a `TrainingStateId` jsou povinné;
  `TrainingPlanId` je nullable a `DurationMinutes` je persisted computed
  sloupec.
- Číselník `ETrainingState` již obsahuje potřebné hodnoty `ConfirmedKis` (2),
  `KisOnly` (3) a `TimeChanged` (4). Změna modelu ani migrace není potřeba.
- Na stránce rozvrhu existují filtry, načítání sezon, kategorií, typů, stavů
  a lokací přes `TrainingScheduleService`, ale zatím ne upload ani importní
  handler.
- `TrainingService` umí upravit jeden existující trénink či jeho skupinu,
  ale nemá službu pro dávkové porovnání a import KIS.
- Projekt již používá `ClosedXML` v `SportSys.Contract` pro KIS XLSX import
  zápasů a `SportSys.Razor` obsahuje obdobný upload pattern na stránce
  docházky trenérů.

## Analýza souboru KIS

`src/SportSys.ConsoleApp/trainings.xlsx` obsahuje list `Export` s osmi
sloupci a 222 datovými řádky; druhý řádek listu opakuje hlavičky.

| Sloupec KIS | Cíl v modelu | Pravidlo |
|---|---|---|
| `Datum` | `Training.Date` | Povinný `DateOnly`. |
| `Čas od` | `Training.TimeFrom` | Povinný `TimeOnly`. |
| `Čas do` | `Training.TimeTo` | Povinný `TimeOnly`, musí být po začátku. |
| `Soupiska` | `Training.SeasonCategoryCode` | Jednoznačné porovnání s `SeasonCategory.Name` ve vybrané sezoně. |
| `Název` | — | Ve vzorku je prázdný; neúčastní se párování ani zápisu, protože `Training` samostatný název nemá. |
| `Místo` | `Training.LocationId` | Jednoznačné porovnání s `Location.Name`; uživatel potvrdil mapování přímo ze zdroje, nikoliv společnou lokalitu ve formuláři. |
| `Typ` | `Training.TrainingTypeId` | Explicitní mapování `Led → Ice` a `Suchá příprava → Dry`; hodnotu bez mapování (ve vzorku `Schůzka s rodiči`) import přeskočí s varováním. |
| `Poznámka trenéra` | — | Ve vzorku je prázdná; neúčastní se párování ani zápisu. |

Párovací základ `datum + soupiska + místo + typ` je ve vzorku jednoznačný.
Bez lokality však vzorek obsahuje 81 kolizí, proto lokalita musí být součástí
shody. Přesná shoda navíc vyžaduje shodný začátek i konec.

## Potvrzené požadavky a rozhodnutí

- Importní formulář obsahuje soubor XLSX, sezonu a fázi tréninku pro nové
  záznamy. Lokalita se vybírá z KIS sloupce `Místo`.
- Při plné shodě data, kategorie, lokality, typu a obou časů import nastaví
  pouze `TrainingStateId = ConfirmedKis`.
- Při jediné shodě data, kategorie, lokality a typu, ale odlišném intervalu,
  import přepíše `TimeFrom` i `TimeTo` hodnotami z KIS a nastaví
  `TrainingStateId = TimeChanged`.
- Bez kandidáta vznikne nový nepropojený `Training` se stavem `KisOnly`,
  typem a lokalitou z KIS, sezonou a zvolenou fází z formuláře. `TrainingPlanId`
  zůstane `null`.
- Více kandidátů na plnou či časově odlišnou shodu, chybějící hodnoty,
  neznámá nebo nejednoznačná kategorie, lokalita nebo typ znamenají přeskočení
  řádku s konkrétním varováním; import nevytváří číselníky.
- Opakovaný import je idempotentní: po prvním přepsání času je stejný řádek
  úplnou shodou a nastaví stav `ConfirmedKis`.

## Technický návrh

### Contract služba

1. Přidat `KisTrainingImportService` do `SportSys.Contract`. Veřejná metoda
   přijme čitelný stream XLSX, ID sezony, ID fáze pro nové tréninky a
   cancellation token, aby Razor vrstva nepředávala `IFormFile` do Contract.
2. S `ClosedXML` ověřit list `Export` a přesných osm hlaviček, přeskočit
   opakovanou hlavičku a převést řádky do interního KIS modelu. Neplatné
   datumy, časy a intervaly nesmí ukončit celý import.
3. Před načtením řádků připravit normalizované lookupy aktivních kategorií
   vybrané sezony, lokací, číselníku typů a existujících tréninků v sezoně.
   Při více shodách názvu vrátit nejednoznačnost, nikoli první nalezený záznam.
4. Pro platný řádek vyhledat kandidáty nejprve podle data, kategorie, lokality
   a typu. Pokud interval přesně odpovídá právě jednomu kandidátovi, potvrdit
   jej; pokud bez shody času existuje právě jeden kandidát, aktualizovat jeho
   interval a stav; jinak vložit `KisOnly` nebo varovat při nejednoznačnosti.
5. Nové ID získat z `sport.SportEventSeq`, nastavit prázdnou `Note` a nechat
   `DurationMinutes` dopočítat databází. Výsledek vrátí počty potvrzených,
   změněných, vložených a přeskočených řádků plus varování s číslem řádku.

### Upload ve stránce rozvrhu

1. Do `IndexModel` v
   `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml.cs`
   injektovat `KisTrainingImportService`, přidat bindovaný importní DTO
   (sezona a fáze), `IFormFile`, seznam položek fází a `TempData` zprávu.
2. V `OnPostImportAsync` validovat vybranou aktivní sezonu, existující fázi,
   přítomnost a velikost XLSX; odstranit z `ModelState` nesouvisející GET
   filtry podle vzoru docházky. Soubor zpracovat v paměti přes stream,
   zobrazit validační chyby na stejné stránce a po úspěchu provést redirect
   s přehledem importu v `TempData`.
3. Do `Index.cshtml` přidat samostatnou část **Import tréninků z KIS** s
   multipart formulářem, výběrem sezony, výběrem fáze, vstupem `.xlsx` a
   odesílacím tlačítkem. Nevazovat import na aktivní GET filtry rozvrhu.

## Implementační kroky

### Fáze 1: Datový kontrakt a importní služba

1. V `src/SportSys.Contract/Models/` vytvořit importní vstup
   `KisTrainingImportDto`, výsledek importu a strukturované varování řádku.
2. V `src/SportSys.Contract/Services/KisTrainingImportService.cs`
   implementovat čtení XLSX, parsování, mapování KIS typů, validaci a
   transakční zápis/aktualizaci tréninků.
3. Zaregistrovat službu v
   `src/SportSys.Contract/ServiceCollectionExtensions.cs`.

### Fáze 2: Razor upload

1. Rozšířit `Schedule/Index.cshtml.cs` o importní handler, seznam fází a
   opětovné načtení dat stránky při validační chybě.
2. Rozšířit `Schedule/Index.cshtml` o upload formulář, validační souhrn a
   status po dokončení.
3. Zachovat současné filtrování, export, editaci i vizualizaci rozvrhu beze
   změny.

### Fáze 3: Testy a dokumentace

1. Doplnit `KisTrainingImportModelTests` pro očekávané hlavičky, opakovanou
   hlavičku, datum, oba časy, neplatný interval, mapování `Led` a `Suchá
   příprava` a odmítnutí neznámého typu.
2. Přidat testy rozhodovacího algoritmu pro plnou shodu, změnu času, žádný
   kandidát a více kandidátů. Ověřit, že změna času přepíše oba časy a
   opakovaný import skončí jako potvrzení.
3. Aktualizovat `docs/modules/sport.md` o import tréninků KIS, obsah upload
   formuláře, mapování lokality a pravidla pro tři výsledné stavy.

## Soubory ke změně

| Soubor | Změna |
|---|---|
| `src/SportSys.Contract/Models/KisTrainingImportDto.cs` | Parametry importu, výsledek a varování řádků. |
| `src/SportSys.Contract/Services/KisTrainingImportService.cs` | Nová služba pro parsování, párování a zápis tréninků z KIS XLSX. |
| `src/SportSys.Contract/ServiceCollectionExtensions.cs` | Registrace importní služby. |
| `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml.cs` | Importní bind properties, lookup fází a POST handler. |
| `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml` | Multipart upload formulář a výsledek importu. |
| `tests/SportSys.Razor.Tests/KisTrainingImportModelTests.cs` | Testy parseru, mapování, validace a párovacích stavů. |
| `docs/modules/sport.md` | Provozní pravidla importu tréninků. |

## Testy a ověření

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
dotnet build SportSys.slnx
```

Automatické testy musí ověřit, že import neztotožní tréninky stejné kategorie
ve stejném dni bez shodné lokality, nepřepíše čas při více kandidátech a
nevytvoří záznam pro neznámý KIS typ.

## Manuální akceptace

1. Na `/sport/Training/Schedule` nahrát `trainings.xlsx`, vybrat existující
   sezonu a fázi pro nové záznamy.
2. Ověřit, že přesná shoda dostane stav `Potvrzený KIS` bez změny ostatních
   hodnot.
3. Ověřit, že jediný kandidát s jiným časem získá stav `Změna času` a interval
   z KIS.
4. Ověřit, že neexistující trénink vznikne se stavem `Jen KIS`, vybranou fází
   a bez vazby na plán.
5. Ověřit varování pro vložený řádek hlaviček, `Schůzka s rodiči` a každou
   neznámou nebo nejednoznačnou referenci.
6. Nahrát stejný soubor podruhé a ověřit, že nevzniknou duplicity a upravené
   řádky se již potvrdí.

## Beze změny

- Databázový model, konfigurace, seedování stavů a EF Core migrace.
- KIS import zápasů, konzolová aplikace a CSV/importní historie.
- GET filtry, export XLSX, editace a vizualizace rozvrhu mimo přidanou
  importní část.

## Mimo rozsah

- Automatické vytvoření nového typu pro `Schůzka s rodiči` nebo jinou novou
  hodnotu KIS.
- Mapování `Název` a `Poznámka trenéra` na nový databázový atribut.
- Přiřazení nového KIS tréninku k `TrainingPlan`, skupině nebo trenérům.
- Historie jednotlivých importních souborů a audit změn.

## Hotovo, když

- Rozvrh obsahuje bezpečný upload KIS XLSX se sezonou a fází.
- Import jednoznačně rozlišuje potvrzení, změnu času a nový KIS trénink.
- Neznámá či nejednoznačná data nezpůsobí automatické vytvoření číselníku ani
  chybnou aktualizaci existujícího tréninku.
- Opakované nahrání stejného souboru nevytváří duplicity.
- Testy a build projdou bez nové databázové migrace.
