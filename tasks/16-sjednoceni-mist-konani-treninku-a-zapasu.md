# Implementační plán: #16 Sjednocení míst konání tréninků a zápasů

**Issue:** [#16 — Sjednocení míst konání tréninků a zápasů](https://github.com/vasekNaus/SportSys/issues/16)

**Stav:** Připraveno k implementaci.

## Cíl

Nahradit sportovní číselník `sport.IceRink` obecným `sport.Location` a použít
jej jako povinné místo konání skutečné sportovní události. `Training` i
`Match` tak budou odkazovat na stejnou entitu lokality; existující stadiony,
zápasy i textové lokality tréninků zůstanou zachovány.

## Výchozí stav

- `Training` a `Match` již dědí z abstraktního TPC předka `SportEvent`
  (`src/SportSys.Database/Models/Sport/SportEvent.cs`), ale místo konání v
  něm není.
- `Match.IceRinkId` odkazuje na `sport.IceRink`, `Team.HomeIceRinkId` určuje
  domácí stadion a import zápasů jej používá jako místo nového zápasu.
- `Training.Location` je povinný text do 100 znaků. `TrainingScheduleService`
  z něj skládá filtr i projekci pro přehled a `TrainingService` jej ukládá
  při editaci.
- `TrainingPlan` také používá textovou `Location`, ale není sportovní
  událostí a issue nepožaduje jeho převod. Zůstane proto mimo databázovou i
  aplikační změnu #16.
- Aktuální TPC model a rozvrh přidaný pro #15 již obsahují `Match.TimeTo` a
  `MatchScheduleService`; implementace #16 na tyto necommitované změny
  naváže, nikoli je nebude vracet.
- Fyzické FK sloupce podle aktuálního model snapshotu jsou
  `sport.Match.IceRink_Id` a `sport.Team.HomeIceRink_Id`.

## Potvrzené požadavky a rozhodnutí

- `sport.IceRink` se nahradí `sport.Location`; dosavadní řádky stadionů a
  jejich ID zůstanou zachovány.
- Všechny zápasy a domácí lokality týmů se převedou na vazbu k lokaci.
- Každá existující neprázdná textová lokalita tréninku se po oříznutí
  krajních mezer stane jednou sportovní lokací, pokud již stejně pojmenovaná
  lokalita neexistuje.
- Při shodě názvu textové lokality se stávajícím stadionem se trénink připojí
  k tomuto zachovanému řádku, nevznikne duplicitní lokalita.
- Adresní údaje `Street`, `City` a `ZipCode` budou u `Location` volitelné:
  původní stadiony je uchovají, lokality vzniklé z textu je nemají k dispozici.
- Neprázdnost, maximální délka a jednoznačné mapování jsou předpokladem
  převodu. Ruční SQL skript při jejich porušení skončí chybou bez změny dat;
  hodnoty se nesmí potichu ořezávat ani nahrazovat.
- V aplikaci se terminologie změní ze „zimního stadionu“ na „lokalitu“,
  včetně administrace, týmů, importu zápasů, rozvrhu, editace a filtrů.
- Agent nevytvoří ani neupraví EF Core migraci ani model snapshot. Ruční SQL
  skripty jsou připraveny samostatně a uživatel je spouští po kontrole.

## Technický návrh

### Databázový model

`SportEvent` získá povinné `LocationId` a navigaci `Location`. Odvozené
`Training` a `Match` již nebudou deklarovat vlastní místo konání. `Location`
zůstane ve schématu `sport`, zachová pole a ID bývalého `IceRink` a převezme
navigace na zápasy, domácí lokality týmů a tréninky. Explicitní index na
`SportEvent.LocationId` nepřidávat: projekt odstranil
`ForeignKeyIndexConvention` a issue nový index nevyžaduje.

`Team.HomeLocationId` bude nadále volitelný. `CsvMatchImportService` jej použije
stejně jako dnes, ale proměnné, chybové hlášky a DTO budou pracovat s názvem
`HomeLocation`.

Ruční SQL je rozdělené pro bezpečné použití:

1. `src/DB Model/Migration/16_00_location_preflight.sql` ověří očekávané
   schéma, prázdné či příliš dlouhé texty a nejednoznačné shody se stadiony;
   vytiskne přehled převáděných hodnot.
2. `src/DB Model/Migration/16_01_location_migration.sql` ve výchozím režimu
   provede celý převod v transakci a vrátí jej zpět. Teprve po ověření se
   nastaví `@Commit = 1`: dynamicky odstraní FK na stadion, přejmenuje tabulku
   a fyzické FK sloupce, přepojí FK, vytvoří nové lokality, vyplní
   `Training.Location_Id` a odstraní až ověřený zdrojový textový sloupec.
3. `src/DB Model/Migration/16_02_location_verify.sql` po ostrém provedení
   ověří výsledné schéma, FK a úplnost vazeb tréninků.

SQL skript je nutné spouštět až s aplikací obsahující změněný model. Dokud
nebude implementace nasazená, databáze po převodu neodpovídá aktuálnímu
kódu.

### Contract a Razor

`IceRinkDto`, `IceRinkService` a Razor area `IceRink` se přejmenují na
`LocationDto`, `LocationService` a `Location`. Služba zachová dosavadní CRUD,
filtrování aktivity a výběr aktivních záznamů včetně aktuálně přiřazené
neaktivní položky. DTO budou mít povinný pouze název; adresní údaje budou
validovatelné maximální délkou, ale nepovinné.

`TrainingEditDto` nahradí text `Location` hodnotou `LocationId` s
`[UIHint("Select")]`. Kontext editace ponese ID i název lokality pro všechny
členy skupiny a `TrainingService` ověří, že vybraná lokalita existuje a je
aktivní, nebo že ji už skupina používá a je neaktivní. Stejná hodnota
`LocationId` bude součástí kontroly konzistence a optimistic-concurrency
otisku.

`TrainingScheduleItemDto` a `MatchScheduleItemDto` ponesou `LocationId` a
`LocationName`. `TrainingScheduleService.GetTrainingLocationsAsync` nahradí
dotazem aktivních lokalit s případným zahrnutím aktuálně filtrovaných
neaktivních lokalit. GET filtr stránky bude `List<int> SelectedLocationIds`,
aby se filtrovalo podle identity, nikoliv podle textu. Renderer, tooltip i
Excel export budou zobrazovat `LocationName`.

## Implementační kroky

### Fáze 1: Model a databázová příprava

1. Přejmenovat entity `IceRink` na `Location`, upravit `[Table]`, DbSet,
   konfiguraci, default constrainty a navigace ve
   `SportSys.Database.Models.sport`.
2. Přesunout `LocationId` a navigaci na `SportEvent`; odstranit
   `Training.Location` a `Match.IceRinkId`/`IceRink`.
3. Přejmenovat `Team.HomeIceRinkId` a navigaci na `HomeLocationId` /
   `HomeLocation`; zachovat nulovatelnost.
4. Upravovat pouze model a konfiguraci, nikdy `Migrations/` ani snapshot.
5. Spustit preflight a nejprve transakční dry run ručních skriptů; ostrý běh
   provede uživatel až společně s nasazením aplikační změny.

### Fáze 2: Contract služby a kontrakty

1. Přejmenovat DTO a službu číselníku na `LocationDto` a `LocationService`;
   zaregistrovat je v `AddSportSysServices()` místo `IceRinkService`.
2. Přejmenovat sportovní vlastnosti, projekce a vazby v `TeamService`,
   `CsvMatchImportService`, `MatchScheduleService`,
   `TrainingScheduleService` a `TrainingService`.
3. Před uložením tréninku načítat vybranou lokalitu a explicitně odmítnout
   neexistující nebo nově vybranou neaktivní lokalitu. Nesmí vzniknout
   neplatné FK ani tichý návrat k textu.
4. Nahradit textové filtry a DTO lokalitou identifikovanou ID; do UI a
   exportu promítat jen zobrazovaný název.

### Fáze 3: Razor administrace, formuláře a rozvrh

1. Přesunout `Areas/sport/Pages/IceRink/` na `Areas/sport/Pages/Location/`,
   změnit názvy modelů, routy, nadpisy, zprávy a položku navigace na
   „Lokality“.
2. Upravit formulář týmu na `HomeLocationId` a načítat nabídku přes
   `LocationService`.
3. Do formuláře editace tréninku dodat položky výběru lokalit; tabulka členů
   skupiny zobrazuje jejich název lokality.
4. Na `/sport/Training/Schedule` nahradit seznam textů výběrem lokality
   podle ID, normalizovat pouze platná ID a zobrazovat názvy ze společného
   číselníku.
5. Zachovat textové lokality a stávající filtr na stránce
   `/sport/Training/Plan`; tento plán není `SportEvent` a #16 jej nemění.

### Fáze 4: Dokumentace a ověření

1. Aktualizovat `docs/modules/sport.md`: tabulku číselníků, administraci,
   aktivitu, popis lokality, týmovou vazbu, služby a filtry Schedule.
2. Aktualizovat příklady starých constraintů v `docs/conventions.md`, pokud
   implementace přejmenuje `IceRinkConfiguration` na
   `LocationConfiguration`.
3. Do dokumentace nebo release postupu zapsat pořadí tří SQL skriptů,
   podmínku `@Commit = 1` a zálohu produkční databáze.

## Soubory ke změně

| Oblast | Soubory |
|---|---|
| EF entity a mapování | `src/SportSys.Database/Models/Sport/SportEvent.cs`, `Training.cs`, `Match.cs`, `Team.cs`, `IceRink.cs` → `Location.cs`, `Context/SportSysDbContext.cs`, `Configurations/sport/IceRinkConfiguration.cs` → `LocationConfiguration.cs` |
| Contract | `src/SportSys.Contract/Models/IceRinkDto.cs` → `LocationDto.cs`, `TeamDto.cs`, `TrainingEditDto.cs`, `TrainingScheduleDto.cs`, `MatchScheduleItemDto.cs`, `Services/IceRinkService.cs` → `LocationService.cs`, `TeamService.cs`, `TrainingService.cs`, `TrainingScheduleService.cs`, `MatchScheduleService.cs`, `CsvMatchImportService.cs`, `ServiceCollectionExtensions.cs` |
| Razor | `src/SportSys.Razor/Areas/sport/Pages/IceRink/` → `Location/`, `Areas/sport/Pages/Team/Edit.cshtml*`, `Areas/sport/Pages/Training/Schedule/Index.cshtml*`, `Areas/sport/Pages/Training/Schedule/Edit.cshtml*`, `src/SportSys.Razor/Pages/Shared/_Layout.cshtml`, rozvrhové/exportní modely a služby, které čtou `Location` |
| SQL | `src/DB Model/Migration/16_00_location_preflight.sql`, `16_01_location_migration.sql`, `16_02_location_verify.sql` |
| Dokumentace a testy | `docs/modules/sport.md`, případně `docs/conventions.md`, dotčené testy v `tests/SportSys.Razor.Tests/` a nové unit testy služeb |

## Testy a ověření

- Unit testy `LocationService` pro filtrování, aktivitu, výběr a volitelné
  adresní údaje.
- Unit testy `TrainingService` pro uložení `LocationId`, odmítnutí neexistující
  či nově zvolené neaktivní lokality, atomickou skupinovou editaci a konflikt
  změněné lokality.
- Unit testy `TrainingScheduleService` pro filtr podle ID a projekci názvu;
  test `MatchScheduleService` a importu pro přejmenovanou týmovou vazbu.
- Upravit testy DTO, factory a exportu rozvrhu tak, aby ověřovaly
  `LocationName`, nikoli historický text.
- Spustit `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`
  a `dotnet build SportSys.slnx`.
- Na záloze databáze spustit `16_00`, dry run `16_01` s `@Commit = 0`, potom
  ostrý běh s `@Commit = 1` a `16_02`; porovnat počet tréninků před a po
  převodu a ručně ověřit připojené záznamy.

## Manuální akceptace

1. Administrace zobrazuje dosavadní stadiony jako lokality se stejnými údaji
   a dovolí vytvořit lokalitu bez adresy.
2. Tým lze navázat na aktivní lokalitu; při editaci zůstává vybraná neaktivní
   lokalita viditelná.
3. Existující zápas ukazuje stejné místo jako před změnou a import nového
   zápasu převezme domácí lokalitu týmu.
4. Každý existující trénink ukazuje lokalitu, nové a editované tréninky ji
   vybírají ze společného číselníku a nelze odeslat libovolný text.
5. Rozvrh filtruje tréninky dle vybraného ID lokality, správně zobrazuje
   název a export obsahuje tentýž název.
6. Preflight zastaví převod s prázdnou, příliš dlouhou nebo nejednoznačnou
   hodnotou a žádná data se při chybě nezmění.

## Beze změny

- `TrainingPlan`, jeho textová lokalita, editace a filtry.
- Skladový číselník `inventory.Location`; je jinou entitou v jiném schématu.
- Logika kategorií, týmů, času, výsledků, seskupování a Excel exportu mimo
  nahrazení zdroje názvu lokality.
- EF Core migrace a model snapshot.

## Mimo rozsah

- Geokódování, ceníky, náklady, využití lokalit a další budoucí metadata.
- Slučování existujících duplicitních stadionů, které nesdílejí textovou
  lokalitu tréninku.
- Převod lokalit tréninkových plánů.

## Hotovo, když

- Aplikační model, Contract a UI používají `sport.Location` pro všechny
  skutečné tréninky a zápasy.
- Žádný produkční trénink po převodu nemá chybějící vazbu na lokalitu a
  žádný existující stadion, zápas ani domácí lokalita týmu neztratí vazbu.
- Filtr rozvrhu pracuje podle ID lokality a UI/export zobrazují její název.
- Ruční SQL preflight, transakční převod a následné ověření jsou uloženy,
  spustitelné ve správném pořadí a nevyžadují EF Core migraci.
- Neexistuje vytvořená ani upravená EF Core migrace nebo snapshot.
