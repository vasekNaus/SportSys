# Implementační plán: #8 Evidence docházky trenérů — import XLSX

**Issue:** [#8 — Evidence docházky trenérů](https://github.com/vasekNaus/SportSys/issues/8)

**Stav:** Implementováno.

## Cíl

Rozšířit existující archiv měsíčních XLSX docházek o skutečný import
jednotlivých řádků. Každý řádek se musí jednoznačně spárovat s existujícím
tréninkem nebo zápasem a zapsat účast vybraného trenéra:

- trénink do `sport.CoachTraining`,
- zápas do nové tabulky `sport.CoachMatch`.

Import je atomický. Původní soubor a záznam `hr.CoachAttendance` se uloží
pouze tehdy, když je celý soubor syntakticky i významově platný a všechny
řádky lze jednoznačně importovat. Při chybě se nesmí uložit archiv, nová
účast ani změna existující účasti.

## Výchozí stav

### Implementováno

- `/hr/Attendance/Index` vybírá trenéra, měsíc, rok a XLSX soubor.
- `CoachAttendanceXlsxValidator` ověřuje velikost, příponu, MIME typ a základní
  Open XML strukturu.
- `CoachAttendanceService.UploadAsync` v transakci ověří trenéra, nahrávajícího
  uživatele a unikátní kombinaci trenér–rok–měsíc.
- `hr.CoachAttendance` ukládá původní soubor, období a audit nahrávajícího
  uživatele.
- `sport.CoachTraining` eviduje účast trenéra na konkrétním tréninku pomocí
  `ParticipationTypeId` a poznámky.
- `EParticipationType` již obsahuje stavy:
  - `Attended` — Účast,
  - `NoResponse` — Bez reakce,
  - `Absent` — Neúčast,
  - `JointTraining` — Spojený trénink,
  - `AbsentOtherTraining` — Neúčast - jiný trénink.
- Contract projekt již používá `ClosedXML` 0.105.1 v importech KIS.

### Chybí

- čtení datových buněk XLSX docházky,
- úplná validace hlavičky a řádků,
- párování řádků na existující sportovní události,
- zápis do `sport.CoachTraining`,
- datový model skutečné účasti trenéra na zápase,
- atomická vazba mezi importem řádků a archivací původního souboru,
- uživatelsky srozumitelný výsledek importu.

Současná dokumentace v `docs/modules/hr.md` výslovně uvádí, že archivace buňky
nečte; po implementaci již tato informace nebude platit.

## Potvrzené požadavky a rozhodnutí

### Z issue a komentáře

- Zdroj je XLSX export docházky.
- Soubor se otevře a obsah se skutečně importuje.
- Pokud import nebude úspěšný, záznam docházky se neuloží.
- Původní soubor zůstane po úspěchu uložený a dostupný ke stažení.

### Potvrzené uživatelem

- Řádky typu `zápas` se uloží do nové tabulky `sport.CoachMatch`.
- `CoachMatch` bude datovým významem a strukturou odpovídat
  `sport.CoachTraining`.

### Technická rozhodnutí

1. Import zpracuje celý soubor metodou all-or-nothing; nebude podporovat
   částečný úspěch ani „přeskočené řádky“.
2. Parser použije existující ClosedXML; nepřidá se další XLSX knihovna.
3. Vstupní sezona, trenér a období budou explicitně zadané ve formuláři.
   Sezona se nebude odvozovat z data, protože intervaly sezon nemusí být
   databázově vzájemně disjunktní.
4. Každý datový řádek musí ležet ve vybraném měsíci, roce a intervalu vybrané
   sezony.
5. Import pouze vloží nebo aktualizuje účasti reprezentované řádky souboru.
   Účasti na událostech, které v souboru nejsou, se nebudou mazat.
6. Zdrojový soubor nebude zakládáním sportovních událostí. Chybějící nebo
   nejednoznačná událost je chyba celého importu.
7. `Počet hodin` je kontrolní hodnota pro trénink, nikoli samostatně ukládaný
   údaj. Zdroj pravdy zůstává `Training.TimeFrom`/`TimeTo`.
8. Agent upraví model a konfiguraci, ale nevytvoří ani neupraví EF Core
   migraci nebo snapshot.
9. Již archivované období nelze znovu importovat ani nahradit. Aktualizace
   existující účasti se týká pouze vazeb vzniklých před importem jiným
   aplikačním tokem; import není mechanismus opravy již uloženého měsíčního
   archivu.

## Analýza vzorového souboru

Příloha z komentáře:
`ZÁŘÍ 2026_export docházky.xlsx`.

Ověřená struktura:

- list: `Export`,
- rozsah: `A1:I72`,
- 69 datových řádků,
- období: 2.–30. září 2026,
- 63 řádků `trénink`,
- 6 řádků `zápas`,
- všechny ukázkové řádky mají stav `účast`,
- řádky 71–72 jsou souhrn (`69/69`, `62:45`, `100 %`) a nejsou datovými
  řádky.

Přesná hlavička:

| Sloupec | Název |
|---|---|
| A | Datum |
| B | Čas od |
| C | Čas do |
| D | Název |
| E | Typ |
| F | Soupiska |
| G | Účast / neúčast |
| H | Poznámka |
| I | Počet hodin |

U tréninků je `Název` prázdný a jsou vyplněny časy od/do a počet hodin.
U zápasů obsahuje `Název` text ve tvaru `Domácí - Hosté`; `Čas do` a
`Počet hodin` jsou prázdné.

## Datový model

### Nová entita `CoachMatch`

Přidat `src/SportSys.Database/Models/sport/CoachMatch.cs`:

```text
sport.CoachMatch
  CoachId             PK, FK -> hr.Coach
  MatchId             PK, FK -> sport.Match
  ParticipationTypeId PK, FK -> sport.ParticipationType
  Note                varchar(50), NOT NULL, default ''
```

Použít stejný význam složeného PK jako u `CoachTraining`:

```csharp
[PrimaryKey(nameof(CoachId), nameof(MatchId), nameof(ParticipationTypeId))]
```

Přidat explicitní FK indexy, protože `ForeignKeyIndexConvention` je vypnutá:

- `IX_CoachMatch_Match` nad `MatchId`,
- `IX_CoachMatch_ParticipationType` nad `ParticipationTypeId`.

Do `Configurations/sport/CoachMatchConfiguration.cs` přidat pojmenovaný
DEFAULT constraint:

```text
DF_CoachMatch_Note
```

`SportSysDbContext` používá `ApplyConfigurationsFromAssembly`, takže nová
`IEntityTypeConfiguration<CoachMatch>` se registruje automaticky. Metadata
test musí přesto ověřit, že default constraint je součástí modelu.

### Navigace a DbContext

Rozšířit:

- `Coach.CoachMatches`,
- `Match.CoachMatches`,
- `ParticipationType.CoachMatches`,
- `SportSysDbContext.CoachMatches`.

`CoachAttendance` zůstane archivem jednoho původního souboru za kombinaci
trenér–měsíc. Nová přímá FK z `CoachTraining` nebo `CoachMatch` na archiv se
nepřidá: tyto vazby představují aktuální stav účasti a mohou existovat i z
jiného aplikačního toku.

### EF Core migrace

Změna databázového modelu vyžaduje migraci pro `sport.CoachMatch`. Agent ji
nevytvoří ani neupraví. Uživatel ji po implementaci vygeneruje a zkontroluje:

- schéma `sport`,
- složený PK,
- tři FK,
- explicitní indexy,
- pojmenovaný default constraint,
- delete behavior odpovídající `CoachTraining`.

## Formát a validace XLSX

### Parser

Nahradit `CoachAttendanceXlsxValidator` jedním parserem
`CoachAttendanceXlsxParser` v Contract vrstvě. Původní samostatný validátor se
odstraní; bezpečnostní kontroly se přesunou do parseru, aby nevznikly dva
zdroje pravdy a ZIP balíček se neotevíral dvakrát. Parser:

1. zachová současné kontroly velikosti, přípony, bezpečného názvu a MIME typu,
2. otevře obsah přes `XLWorkbook` ze streamu,
3. vyžádá právě jeden list s názvem `Export` case-insensitive; ostatní listy
   jsou povolené,
4. ověří všech devět hlaviček v přesném pořadí,
5. načte datové řádky do interního typovaného modelu,
6. odliší koncové souhrnné řádky od datových řádků,
7. vrátí bezpečný název souboru, kanonický MIME typ a kolekci řádků.

Veřejné konstanty `MaxFileSizeBytes`, `CanonicalContentType` a
`GenericBinaryContentType` přesunout z odstraňovaného validátoru do parseru a
upravit PageModel i testy, aby odstranění třídy nerozbilo build.

### Rozpoznání datových a souhrnných řádků

- Úplně prázdný řádek se ignoruje.
- Po prvním datovém řádku lze ignorovat pouze koncové souhrnné řádky, které
  nemají datum, čas, typ ani soupisku a obsahují jen souhrn v G/I.
- Datový řádek po začátku souhrnu je chyba.
- Částečně vyplněný řádek se nesmí tiše přeskočit; vrátí chybu s číslem
  řádku.
- Soubor musí obsahovat alespoň jeden datový řádek.

### Typovaný řádek

Interní model má nést:

- číslo zdrojového řádku,
- `DateOnly Date`,
- `TimeOnly TimeFrom`,
- volitelný `TimeOnly TimeTo`,
- volitelný `string Name`,
- druh události `Training`/`Match`,
- `string RosterName`,
- text stavu účasti určený k následnému mapování na databázový
  `ParticipationType`,
- normalizovanou poznámku,
- volitelnou dobu z `Počet hodin`.

### Společná pravidla

- Datum přijmout z nativní Excel date buňky i z českého textu.
- Časy přijmout z nativní Excel time buňky i z textu `H:mm`/`HH:mm`.
- Všechny texty před validací oříznout.
- Porovnávání známých českých hodnot je case-insensitive, ale ne fuzzy.
- Poznámka může mít nejvýše 50 znaků, stejně jako cílové entity.
- `Soupiska` je povinná.
- Neznámý typ události nebo stav účasti je chyba celého importu.

### Mapování stavu účasti

Neduplikovat lokalizované texty v parseru. Služba načte existující
`ParticipationType.Id/Name` a normalizovaný text z XLSX musí odpovídat právě
jednomu `ParticipationType.Name` case-insensitive. Tím zůstane zdrojem pravdy
existující seed z `EParticipationType` a RESX:

| XLSX | Enum |
|---|---|
| Účast | `Attended` |
| Bez reakce | `NoResponse` |
| Neúčast | `Absent` |
| Spojený trénink | `JointTraining` |
| Neúčast - jiný trénink | `AbsentOtherTraining` |

Varianty, které nejsou v číselníku, se nesmí tiše převést na `NoResponse`.
Normalizovat pouze okolní whitespace a opakované mezery; význam textu ani
diakritiku neopravovat fuzzy pravidly.

### Pravidla pro trénink

Povinné:

- datum,
- čas od,
- čas do,
- typ `trénink`,
- soupiska,
- stav účasti,
- počet hodin.

Musí platit:

- `TimeFrom < TimeTo`,
- `Počet hodin` odpovídá rozdílu časů přesně na celé minuty,
- `Název` může být prázdný.

`Počet hodin` přijmout jako nativní Excel time/duration hodnotu nebo jako
text `[h]:mm`/`h:mm`. Obecná desetinná hodnota typu `1,5` se nepřijímá,
protože její jednotka není ze souboru jednoznačná.

### Pravidla pro zápas

Povinné:

- datum,
- čas od,
- název ve tvaru `Domácí - Hosté`,
- typ `zápas`,
- soupiska,
- stav účasti.

`Čas do` a `Počet hodin` mohou být prázdné. Pokud jsou vyplněné, musí být
syntakticky platné; párování používá čas začátku a existující zápas v DB.

Název předem nerozdělovat. Kandidátní zápasy se nejprve omezí podle sezony,
kategorie, data a času; následně se porovná normalizovaný text
`HomeTeam.Name + " - " + AwayTeam.Name` s celým sloupcem `Název`. Tím názvy
týmů obsahující spojovník nezpůsobí chybné rozdělení.

## Párování na sportovní události

### Určení sezony a kategorie

Formulář nově vyžaduje `SeasonId`, stejně jako existující KIS importy. Pro
každé datum:

1. ověřit, že datum leží v intervalu vybrané sezony,
2. ve vybrané sezoně najít právě jednu `SeasonCategory`, jejíž `Name` nebo
   `Code` se case-insensitive přesně rovná `Soupiska`.

Kategorie se nefiltruje přes aktuální `IsActive`, protože docházka se může
importovat zpětně po deaktivaci historické kategorie. Chybějící nebo
nejednoznačná kategorie je chyba řádku a tím celého importu. Nepárovat
kategorii globálně. Pokud jedna hodnota odpovídá `Name` jedné kategorie a
`Code` jiné kategorie ve stejné sezoně, jde o nejednoznačnou chybu.

### Trénink

Kandidát musí mít shodné:

- `Date`,
- `SeasonId`,
- `SeasonCategoryCode`,
- `TimeFrom`,
- `TimeTo`.

Export neobsahuje místo ani konkrétní typ tréninku. Proto:

- právě jeden kandidát znamená úspěšné spárování,
- žádný kandidát znamená chybu „trénink nebyl nalezen“,
- více kandidátů znamená chybu „trénink je nejednoznačný“.

Import nesmí vybrat první kandidát ani založit nový trénink.
Časová tolerance se nepoužije: bez místa a typu tréninku by mohla přiřadit
účast k jiné události. Pokud se docházkový export liší od sportovního
kalendáře, musí uživatel nejprve synchronizovat kalendář existujícím KIS
importem.

Z kandidátů vyloučit stavy `Cancelled`, `DateChanged` a `ArenaFault`.
`Plan`, `ConfirmedKis`, `KisOnly` a `TimeChanged` jsou platné, pokud přesně
odpovídají časům exportu.

### Zápas

Kandidát musí mít shodné:

- `Date`,
- `SeasonId`,
- `SeasonCategoryCode`,
- názvy `HomeTeam.Name` a `AwayTeam.Name` podle sloupce `Název`.

Názvy týmů porovnat case-insensitive po oříznutí. `Čas od` v docházkovém
exportu může být čas nástupu/srazu a nemusí se rovnat oficiálnímu
`Match.TimeFrom`; proto není součástí primárního klíče párování.

- Právě jeden kandidát podle data, kategorie a týmů se přijme bez ohledu na
  rozdíl času.
- Pokud existuje více takových kandidátů, použije se přesná shoda
  `Match.TimeFrom` jako rozlišovač.
- Pokud ani čas neurčí právě jednoho kandidáta, řádek je nejednoznačný.
- Počet kandidátů různý od jedné po aplikaci pravidel je chyba celého
  importu.

Zápas ve stavu `Cancelled` není kandidát. `MatchStateId = NULL` a ostatní
nezrušené stavy jsou platné.

### Duplicitní řádky

Jeden soubor nesmí obsahovat více datových řádků, které se pro stejného
trenéra spárují na stejný `TrainingId` nebo `MatchId`, bez ohledu na stav
účasti. Duplicita je chyba s uvedením obou zdrojových řádků.

## Zápis účasti

### Existující stav

Složený PK `CoachTraining` a navrženého `CoachMatch` obsahuje
`ParticipationTypeId`, takže databáze technicky dovoluje více stavů jednoho
trenéra pro stejnou událost. Import musí aplikačně vynutit právě jeden stav:

1. načíst všechny vazby pro trenéra a párované události,
2. pokud pro jednu událost existuje více vazeb, ukončit celý import jako
   nekonzistentní data,
3. pokud vazba neexistuje, vložit novou,
4. pokud existuje se stejným stavem, aktualizovat poznámku,
5. pokud existuje s jiným stavem, odstranit původní vazbu a vložit novou,
   protože součást primárního klíče nelze běžně změnit.

Řádky, které ve vstupním souboru nejsou, nesmějí způsobit odstranění
existujících vazeb.

### Atomický postup služby

`CoachAttendanceService.UploadAsync` změnit na orchestrace:

1. validovat vstupní DTO včetně `SeasonId` a auditního uživatele,
2. parsovat celý soubor mimo DB transakci,
3. zahájit `Serializable` transakci,
4. ověřit existenci trenéra, uživatele a neexistenci archivu období,
5. přednačíst sezony, kategorie, události a existující účasti,
6. vyřešit a validovat všechny řádky bez změny EF entit,
7. sestavit interní importní plán operací,
8. teprve po úplném úspěchu aplikovat všechny insert/update/remove operace,
9. přidat `CoachAttendance` s původním souborem,
10. provést jeden `SaveChangesAsync`,
11. commitnout transakci.

Při parser chybě, nejednoznačnosti, validační chybě, konfliktu unikátního
indexu nebo DB chybě:

- transakci necommitnout,
- záznam `CoachAttendance` neuložit,
- neuložit žádnou účast,
- vrátit uživateli agregovaný seznam chyb s čísly XLSX řádků, pokud jsou
  známá.

Parser a resolver mají sesbírat všechny zjistitelné chyby řádků v jednom
průchodu, nejvýše prvních 50, a teprve potom import odmítnout. Chyby
znemožňující další čtení celého souboru, například poškozený XLSX nebo
neplatná hlavička, se vracejí okamžitě.

Pokud služba po zachycené chybě pokračuje ve stejném scoped DbContextu,
vyčistit neuložené změny z change trackeru před opětovným čtením stránky.

### Souběh

- Zachovat unikátní index `IX_CoachAttendance_Coach_Period`.
- Duplicitní import stejného trenéra a období převést na současnou doménovou
  validační chybu.
- Nové `CoachMatch` a stávající `CoachTraining` chrání složené PK.
- `Serializable` transakce zajistí, že paralelní importy neuvidí částečný
  stav.
- SQL Server deadlock nebo serializační konflikt (`1205`, případně `3960`)
  převést na doménovou chybu s výzvou k opakování importu; neukazovat HTTP
  500.

## Contract modely a výsledky

Přidat interní parser/import modely do samostatného souboru, například:

```text
src/SportSys.Contract/Models/hr/CoachAttendanceImportModels.cs
```

Veřejný výsledek služby má obsahovat minimálně:

- ID uloženého `CoachAttendance`,
- vybranou sezonu,
- počet importovaných tréninků,
- počet importovaných zápasů,
- počet nových vazeb,
- počet aktualizovaných vazeb,
- počet beze změny.

Neobsahuje kolekci varování, protože žádný neplatný řádek se nepřeskakuje.

`CoachAttendanceUploadDto` rozšířit o povinné `SeasonId`. Přidat načtení sezon
pro select list; použít existující význam sezon a nezavádět nový lookup model,
pokud lze znovu použít `LookupSelectItem`.

## Razor UI

V `Areas/hr/Pages/Attendance/Index.cshtml.cs`:

- přidat povinný výběr sezony a zachovat výběr trenéra, měsíce, roku a
  souboru,
- změnit volání na nový atomický import,
- zachytit očekávané parser/import validační chyby,
- po chybě zachovat zadaného trenéra a období,
- zobrazit agregované řádkové chyby jako validační seznam, maximálně 50
  položek,
- po úspěchu zobrazit souhrn importovaných tréninků a zápasů.

V `Index.cshtml`:

- změnit text tlačítka z „Nahrát“ na „Importovat“,
- upravit popis tak, aby bylo zřejmé, že soubor nebude jen archivován,
- uvést, že import proběhne pouze při úspěšném spárování všech řádků,
- upozornit, že export neobsahuje identitu trenéra a správnost vybraného
  trenéra proto nelze ze souboru automaticky ověřit,
- uvést omezení poznámky na 50 znaků; delší poznámka je validační chyba a
  nesmí se tiše ořezat,
- ponechat historii a stažení původního souboru.

Nevytvářet preview ani potvrzovací mezikrok; issue požaduje přímý import.

## Implementační kroky

### Fáze 1: Datový model zápasové účasti

1. Přidat entitu `CoachMatch`.
2. Přidat Fluent API konfiguraci výchozí poznámky.
3. Přidat navigace na `Coach`, `Match` a `ParticipationType`.
4. Přidat `DbSet<CoachMatch>`.
5. Doplnit metadata testy mapování, PK, FK, indexů a default constraintu.

### Fáze 2: Parser XLSX

1. Zavést parser postavený na ClosedXML.
2. Zachovat bezpečnostní limity současného validátoru.
3. Ověřit list, přesnou hlavičku a klasifikaci datových/souhrnných řádků.
4. Implementovat české datumy, časy a délku; text účasti ponechat pro
   databázové mapování v resolveru.
5. Vrátit typované řádky s číslem zdrojového řádku.

### Fáze 3: Resolver událostí

1. Ověřit vybranou sezonu a načíst její kategorie bez filtru `IsActive`.
2. Implementovat přesné párování tréninků.
3. Implementovat přesné párování zápasů a názvů týmů.
4. Odhalit chybějící, nejednoznačné a duplicitní řádky.
5. Sestavit importní plán bez změny entit.
6. Sesbírat až 50 řádkových chyb v jednom výsledku.

### Fáze 4: Atomický zápis

1. Načíst existující `CoachTraining` a `CoachMatch`.
2. Validovat invariant jednoho stavu na trenéra a událost.
3. Připravit insert/update/remove operace.
4. Přidat archivní `CoachAttendance`.
5. Uložit vše jedním `SaveChangesAsync` v `Serializable` transakci.
6. Zachovat konkrétní zpracování souběžného duplicitního období.

### Fáze 5: UI a dokumentace

1. Upravit texty a výsledek importu na stránce Attendance.
2. Aktualizovat `docs/modules/hr.md`.
3. Aktualizovat `docs/modules/sport.md` o `CoachMatch`.
4. Odstranit tvrzení, že docházka nečte buňky nebo nezapisuje sportovní data.

## Soubory ke změně

### Nové

- `src/SportSys.Database/Models/sport/CoachMatch.cs`
- `src/SportSys.Database/Configurations/sport/CoachMatchConfiguration.cs`
- `src/SportSys.Contract/Models/hr/CoachAttendanceImportModels.cs`
- `src/SportSys.Contract/Services/CoachAttendanceXlsxParser.cs`
- testovací soubor parseru nebo rozšíření stávajícího testu podle výsledného
  rozdělení tříd

### Existující

- `src/SportSys.Database/Models/hr/Coach.cs`
- `src/SportSys.Database/Models/sport/Match.cs`
- `src/SportSys.Database/Models/sport/ParticipationType.cs`
- `src/SportSys.Database/Context/SportSysDbContext.cs`
- `src/SportSys.Contract/Models/hr/CoachAttendanceModels.cs`
- `src/SportSys.Contract/Services/CoachAttendanceService.cs`
- `src/SportSys.Contract/Services/CoachAttendanceXlsxValidator.cs`
- `src/SportSys.Razor/Areas/hr/Pages/Attendance/Index.cshtml.cs`
- `src/SportSys.Razor/Areas/hr/Pages/Attendance/Index.cshtml`
- `tests/SportSys.Razor.Tests/CoachAttendanceTests.cs`
- `docs/modules/hr.md`
- `docs/modules/sport.md`

`CoachAttendanceXlsxValidator.cs` se odstraní po přesunu všech jeho
bezpečnostních kontrol do parseru.

### Uživatelsky vlastněná migrace

- nový soubor v `src/SportSys.Database/Migrations/`,
- aktualizace `SportSysDbContextModelSnapshot.cs`.

Agent tyto soubory nevytváří ani neupravuje.

## Testy a ověření

### Parser

Programově generovat malé ClosedXML sešity; vzorový binární soubor z GitHub
komentáře není nutné commitovat.

Ověřit:

- přesnou hlavičku a list `Export`,
- český nativní Excel datum a textové datum,
- nativní Excel čas a text `H:mm`,
- nativní Excel duration a text `[h]:mm`,
- správný tréninkový řádek,
- správný zápasový řádek bez času do,
- všech pět stavů `EParticipationType`,
- ignorování koncového souhrnu,
- odmítnutí datového řádku za souhrnem,
- chybnou nebo přeházenou hlavičku,
- prázdný soubor,
- neznámý typ nebo stav,
- datum mimo vybraný měsíc,
- rozdíl `Počet hodin` proti času od/do,
- příliš dlouhou poznámku,
- chybějící povinnou buňku,
- další listy vedle jediného listu `Export`,
- chybějící nebo duplicitní list `Export`.

### Resolver

Ověřit čistými jednotkovými testy:

- jednoznačný trénink,
- chybějící a nejednoznačný trénink,
- jednoznačný zápas včetně domácího/hostujícího týmu,
- chybějící a nejednoznačný zápas,
- správné rozlišení dvou souběžných kategorií,
- duplicitní řádky stejné události,
- kategorie stejného názvu v různých sezonách,
- vyloučení zrušeného nebo přesunutého tréninku,
- vyloučení zrušeného zápasu,
- názvy týmů obsahující spojovník,
- soupisku párovanou přes `SeasonCategory.Name` i `SeasonCategory.Code`,
- zápas s časem srazu odlišným od `Match.TimeFrom`,
- více zápasů stejných týmů v jednom dni rozlišených časem,
- mapování všech seedovaných `ParticipationType.Name`.

### Zápis

Ověřit:

- vytvoření nové účasti na tréninku,
- vytvoření nové účasti na zápase,
- stejný stav aktualizuje pouze poznámku,
- změna stavu odstraní starou a vloží novou vazbu,
- více existujících stavů stejné události import zablokuje,
- řádky chybějící v souboru se nemažou,
- duplicitní archiv období se odmítne,
- chyba libovolného řádku nezanechá `CoachAttendance`, `CoachTraining` ani
  `CoachMatch`,
- DB chyba při `SaveChangesAsync` rollbackne celý import.

Větve aktualizace poznámky a změny stavu testovat proti předem existujícím
vazbám bez `CoachAttendance` archivu. Druhý import stejného trenéra a období
musí být odmítnut před aplikací těchto změn.

V repozitáři nyní není SQL Server integrační testovací harness. Oddělit proto
čistou tvorbu importního plánu od zápisu do EF a plně ji pokrýt jednotkovými
testy bez provideru. InMemory provider není důkazem transakční atomiky ani SQL
constraintů; rollback a souběh povinně ověřit manuálně na testovací SQL Server
databázi.

### Regrese

Spustit:

```powershell
dotnet build SportSys.slnx -c Release
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
```

## Manuální akceptace

1. Na testovací databázi existují sportovní události odpovídající vzorovému
   září 2026.
2. Uživatel vybere odpovídající sezonu, trenéra, září 2026 a vzorový soubor.
3. Import uspěje, uloží 63 tréninkových a 6 zápasových účastí a jeden archiv.
4. Původní soubor lze beze změny stáhnout z historie.
5. Import souboru s jedním neexistujícím tréninkem zobrazí číslo řádku a
   neuloží žádný archiv ani účast.
6. Import souboru s nejednoznačným tréninkem nebo zápasem nevybere kandidáta
   automaticky.
7. Import s datem mimo zvolený měsíc skončí bez zápisu.
8. Paralelní nebo opakovaný import stejného trenéra a období vytvoří nejvýše
   jeden archiv.
9. Existující účast na události, která není ve vstupním souboru, zůstane
   zachována.

## Beze změny

- TPT vztah `User -> Coach`.
- Formát a uchování původního souboru v `CoachAttendance`.
- Filtrování historie a download souboru.
- Audit pomocí lokálního `identity.User.Id`.
- Limit souboru 10 MiB a povolený formát `.xlsx`.
- Dočasně uvolněná autorizace HR Area.
- Importy KIS tréninků a zápasů.

## Mimo rozsah

- Zakládání chybějících tréninků, zápasů, týmů, sezon nebo kategorií.
- Fuzzy párování názvů soupisek a týmů.
- Částečný import platných řádků.
- Preview a ruční mapování nejednoznačných řádků.
- Nahrazení nebo smazání již uloženého měsíčního archivu.
- Mazání účastí, které nejsou uvedené ve vstupním souboru.
- Výpočet odměn nebo mzdových podkladů.
- Vytvoření nebo aplikace EF Core migrace agentem.

## Hotovo, když

- `sport.CoachMatch` odpovídá významem `sport.CoachTraining`.
- Parser bezpečně načte ověřený formát vzorového XLSX.
- Každý řádek se jednoznačně spáruje s existujícím tréninkem nebo zápasem.
- Import podporuje všech pět existujících stavů účasti.
- Jeden trenér má po importu nejvýše jeden stav pro každou událost.
- Celý import včetně archivu je atomický.
- Chyba obsahuje číslo řádku a nezanechá žádná částečná data.
- Více nezávislých chyb řádků se zobrazí společně, nejvýše 50.
- UI po úspěchu ukáže počty tréninků a zápasů.
- Původní soubor zůstane dostupný ke stažení.
- Automatické testy a manuální test se vzorovým souborem projdou.
- Dokumentace odpovídá skutečnému importu.
- Agent nevytvořil ani neupravil migraci.
