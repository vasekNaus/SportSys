# Implementační plán: #8 Evidence docházky trenérů

**Issue:** [#8 — Evidence docházky trenérů](https://github.com/vasekNaus/SportSys/issues/8)

**Stav:** Připraveno k implementaci.

## Cíl

Přidat do modulu HR evidenci původních měsíčních XLSX souborů s docházkou
trenérů. Přihlášený uživatel vybere trenéra a měsíc, nahraje soubor, systém
bezpečně uloží jeho původní obsah do databáze a zaznamená čas i uživatele,
který upload provedl.

Nová stránka `/hr/Attendance/Index` poskytne:

- formulář pro nahrání docházky,
- historii všech nahraných souborů,
- filtrování podle trenéra a období od–do,
- stažení původního souboru.

Obsah XLSX se v této etapě nebude interpretovat ani přenášet do tabulek
tréninků.

## Výchozí stav

- HR Area existuje v `src/SportSys.Razor/Areas/hr/Pages/`.
- Navigace v `Pages/Shared/_Layout.cshtml` obsahuje sekci `Personalistika`
  a položku `Trenéři`.
- Trenéři jsou uloženi v `hr.Coach`; aktuální model používá pro zobrazení
  `Coach.DisplayName` a `Coach.PersonalNumber`.
- `SportSysDbContext` již obsahuje HR DbSety a Identity tabulku
  `identity.User`.
- `CoachService` poskytuje seznam trenérů, ale zatím neexistuje samostatný
  jednoduchý select-list kontrakt určený pro docházku.
- Přihlášení přes Entra ID je provisionováno v
  `SportSys.Contract/Auth/EntraClaimsTransformation.cs`.
- Lokální databázové ID přihlášeného uživatele není v současnosti spolehlivě
  dostupné Razor stránkám jako jednotný claim. OIDC `NameIdentifier` nelze
  automaticky považovat za `identity.User.Id`.
- Výchozí authorization fallback vyžaduje přihlášeného uživatele. Dočasně není
  zapnutá politika `SystemAdmin` pro HR Area.
- V projektu není obecná evidence binárních dokumentů. Fotografie trenéra
  ukazuje existující vzor odděleného načítání binárního obsahu, validace typu
  souboru a privátní cache.
- Existující `sport.CoachTraining` eviduje účast trenéra na jednotlivém
  tréninku. Není úložištěm zdrojových měsíčních XLSX a tato změna jej nebude
  používat ani měnit.
- Testovací projekt `tests/SportSys.Razor.Tests` používá xUnit a referencuje
  Razor projekt.

## Potvrzené požadavky a rozhodnutí

### Požadavky z issue

- Evidovat trenéra, rok a měsíc docházky.
- Uložit původní XLSX soubor přímo do databáze.
- Uchovat původní název a typ souboru.
- Evidovat UTC datum a čas nahrání.
- Evidovat uživatele, který soubor nahrál.
- Uchovávat historii všech uploadů.
- Umožnit stažení původního souboru.
- Filtrovat historii podle trenéra a období od–do.
- Přidat stránku do modulu HR pod route `HR/Attendance`.
- Nečíst ani nevyhodnocovat obsah docházky.
- Neukládat data z XLSX do tréninkových tabulek.
- Návrh nesmí blokovat budoucí samostatné zpracování uloženého dokumentu.

Issue nemá komentáře ani pozdější upřesnění.

### Technická rozhodnutí

- Nová entita se bude jmenovat `CoachAttendance` a bude uložena v
  `hr.CoachAttendance`.
- Pro stejného trenéra a měsíc bude možné nahrát jediný soubor. Bude vytvořen
  unikátní index nad `CoachId + PeriodYear + PeriodMonth`
- Záznamy se v této etapě nebudou nahrazovat ani fyzicky mazat.
- XLSX bude validován podle velikosti, bezpečného názvu, přípony, MIME typu,
  ZIP signatury a základní struktury Open XML balíčku. Kontrola struktury není
  zpracováním docházkových dat.
- Maximální velikost jednoho souboru bude 10 MiB. Limit bude definován na
  jednom místě v Contract vrstvě a kontrolován před i po načtení streamu.
- Do databáze se uloží kanonický MIME typ
  `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.
- Binární `FileContent` nebude součástí seznamového DTO ani dotazu pro přehled.
  Načte se pouze samostatnou metodou při stažení konkrétního záznamu.
- `UploadedAt` se bude ukládat jako UTC `DateTime`.
- Pro Entra uživatele se do principalu doplní vlastní claim s lokálním
  `identity.User.Id`. Lokální Identity přihlášení použije číselný
  `ClaimTypes.NameIdentifier`. Razor nebude referencovat `SportSys.Database`.
- Stránka bude dostupná přihlášeným uživatelům přes stávající fallback policy.
  Nebude znovu zavedena role `SystemAdmin`, dokud nebude pro HR potvrzená
  cílová autorizační politika.
- Všechny očekávané validační chyby budou vráceny explicitně a zobrazeny ve
  formuláři; neplatný soubor se neuloží ani částečně.

## Technický návrh

### Datový model

Nová entita `SportSys.Database.Models.hr.CoachAttendance`:

| Vlastnost | SQL reprezentace | Pravidlo |
|---|---|---|
| `Id` | `int`, PK, identity | Identifikátor uploadu |
| `CoachId` | `int`, NOT NULL | FK na `hr.Coach.Id` |
| `PeriodYear` | `int`, NOT NULL | Rok 1–9999 |
| `PeriodMonth` | `tinyint`, NOT NULL | Měsíc 1–12 |
| `FileName` | `nvarchar(255)`, NOT NULL | Pouze bezpečný název bez cesty |
| `ContentType` | `varchar(100)`, NOT NULL | Kanonický XLSX MIME typ |
| `FileContent` | `varbinary(max)`, NOT NULL | Původní soubor |
| `UploadedAt` | `datetime2(0)`, NOT NULL | UTC čas uploadu |
| `UserUploadId` | `int`, NOT NULL | FK na `identity.User.Id` |

Omezení:

- CHECK `PeriodYear BETWEEN 1 AND 9999`,
- CHECK `PeriodMonth BETWEEN 1 AND 12`,
- FK na trenéra i uživatele s `DeleteBehavior.Restrict`,
- index `IX_CoachAttendance_Coach_Period` nad `CoachId + PeriodYear + PeriodMonth` s unikátním omezením
  `CoachId, PeriodYear, PeriodMonth`,
- index `IX_CoachAttendance_UserUpload` nad `UserUploadId`,
- index `IX_CoachAttendance_UploadedAt` nad `UploadedAt`.

Navigace:

- `Coach.Attendances`,
- `CoachAttendance.Coach`,
- `CoachAttendance.UserUpload`.

`SportSysDbContext` dostane `DbSet<CoachAttendance> CoachAttendances`.

### Identita nahrávajícího uživatele

Do `SportSys.Contract/Auth/` přidat:

- konstantu vlastního claim typu, například
  `SportSysClaimTypes.UserId`,
- resolver lokálního uživatelského ID z `ClaimsPrincipal`.

`EntraClaimsTransformation` po nalezení nebo provisioningu uživatele doplní
claim s hodnotou `user.Id`. Resolver:

1. použije vlastní SportSys user ID claim,
2. pro lokální Identity účet přijme číselný `ClaimTypes.NameIdentifier`,
3. při chybějícím nebo neplatném ID vyvolá explicitní chybu místo anonymního
   nebo nulového auditního záznamu.

PageModel předá získané `UserUploadId` do služby. Nebude používat
`UserManager<User>` ani databázovou entitu `User`.

### Contract modely

Vytvořit `src/SportSys.Contract/Models/hr/CoachAttendanceModels.cs`:

- `CoachAttendanceFilter`
  - `int? CoachId`,
  - `int? PeriodFromYear`,
  - `int? PeriodFromMonth`,
  - `int? PeriodToYear`,
  - `int? PeriodToMonth`,
  - validace úplnosti obou částí období a `from <= to`,
- `CoachAttendanceUploadDto`
  - `int CoachId`,
  - `int PeriodYear`,
  - `int PeriodMonth`,
  - validační atributy pro povinného trenéra a rozsah období,
- `CoachAttendanceListItem`
  - `Id`,
  - `CoachId`,
  - `CoachDisplayName`,
  - `CoachPersonalNumber`,
  - `PeriodYear`,
  - `PeriodMonth`,
  - `FileName`,
  - `UploadedAt`,
  - `UploadedByDisplayName`,
- `CoachAttendanceFileDto`
  - `Content`,
  - `ContentType`,
  - `FileName`.

Pro select trenérů přidat malé DTO `CoachSelectItem` s `CoachId`,
`DisplayName` a `PersonalNumber`. Nepoužívat `CoachListItem`, protože jeho
projekce načítá licence, smlouvy a informaci o fotografii, které upload
docházky nepotřebuje.

### Služba

Vytvořit
`src/SportSys.Contract/Services/CoachAttendanceService.cs` a registrovat ji v
`ServiceCollectionExtensions.AddSportSysServices()`.

Veřejné operace:

```csharp
Task<List<CoachAttendanceListItem>> GetAllAsync(
    CoachAttendanceFilter filter,
    CancellationToken ct = default);

Task<List<CoachSelectItem>> GetCoachesAsync(
    CancellationToken ct = default);

Task<int> UploadAsync(
    CoachAttendanceUploadDto dto,
    byte[] content,
    string contentType,
    string fileName,
    int userUploadId,
    CancellationToken ct = default);

Task<CoachAttendanceFileDto?> GetFileAsync(
    int id,
    CancellationToken ct = default);
```

Chování služby:

- `GetAllAsync` projektuje přímo do list DTO a nikdy nevybírá `FileContent`.
- Filtr období porovnává dvojici rok/měsíc chronologicky; neprovádí textové
  porovnání.
- Výsledky jsou řazeny sestupně podle období a následně podle `UploadedAt`.
- `GetCoachesAsync` vrací trenéry podle zobrazovaného jména a osobního čísla.
- `UploadAsync` ověří existenci trenéra i nahrávajícího uživatele.
- Název normalizuje přes `Path.GetFileName`, odmítne prázdný nebo příliš dlouhý
  název a vyžaduje příponu `.xlsx` bez ohledu na velikost písmen.
- MIME typ z klienta použije pouze jako validační vstup, ne jako jediný důkaz
  typu souboru.
- Soubor musí být validní ZIP s položkami `[Content_Types].xml` a
  `xl/workbook.xml`; šifrovaný, poškozený nebo jiný ZIP se odmítne.
- Po úspěšné validaci uloží původní bytes, bezpečný název, kanonický MIME typ,
  `DateTime.UtcNow` a `UserUploadId`.
- `GetFileAsync` načte binární obsah pouze pro jeden konkrétní záznam.
- Neočekávané databázové chyby se nebudou maskovat jako úspěch.

Validaci XLSX vyčlenit do interního helperu v Contract vrstvě, aby byla
samostatně testovatelná a pozdější parser mohl navázat na již ověřený Open XML
balíček. Není potřeba přidávat knihovnu pro Excel; kontrola základní struktury
použije `System.IO.Compression.ZipArchive`.

### Razor Page

Vytvořit:

```text
src/SportSys.Razor/Areas/hr/Pages/Attendance/
├── Index.cshtml
└── Index.cshtml.cs
```

Stránka bude mít dva oddělené formuláře:

1. POST formulář uploadu s `enctype="multipart/form-data"`:
   - trenér,
   - rok,
   - měsíc,
   - XLSX soubor,
   - handler `OnPostUploadAsync`.
2. GET formulář filtru:
   - trenér,
   - období od,
   - období do,
   - možnost vymazat filtr.

Přehled zobrazí:

- období ve formátu `MM/yyyy`,
- trenéra a osobní číslo,
- původní název souboru,
- datum a čas nahrání,
- zobrazované jméno nahrávajícího uživatele,
- ikonovou akci pro stažení.

Download handler `OnGetDownloadAsync(int id)`:

- vrátí `404`, pokud záznam neexistuje,
- vrátí `FileContentResult` s původním bezpečným názvem,
- nastaví `Cache-Control: private, no-store`,
- nepoužije veřejné cachování personálního dokumentu.

Při chybě uploadu se znovu načtou select-listy i aktuálně filtrovaný přehled a
zachovají se zadané metadata. Po úspěchu se použije PRG redirect a
`TempData["StatusMessage"]`.

### Navigace a styly

Do sekce `Personalistika` v `Pages/Shared/_Layout.cshtml` přidat položku:

```text
Personalistika
├── Trenéři
└── Docházka
```

Odkaz povede na Area `hr`, stránku `/Attendance/Index`. Ikona bude mít `fa-fw`.

Stránka primárně použije existující komponenty `grid`, `field`, `button`,
`filter-form` a `infobox`. Nové SCSS přidat do `Styles/_hr.scss` pouze pokud
standardní komponenty nestačí. `wwwroot/css/site.css` se neupravuje ručně;
vznikne existujícím npm buildem.

### Budoucí zpracování

Budoucí parser může navázat na stabilní `CoachAttendance.Id` a načítat původní
soubor přes Contract/Database vrstvu. V této změně se nepřidávají:

- stav zpracování,
- chybový protokol parseru,
- importované řádky docházky,
- vazby na `sport.Training` nebo `sport.CoachTraining`.

Tyto údaje lze doplnit později bez změny identity uloženého dokumentu.

## Implementační kroky

### Fáze 1: Databázová entita

1. Přidat `Models/hr/CoachAttendance.cs` s atributem `[Table]`, datovými typy,
   navigacemi a explicitními indexy.
2. Přidat Fluent konfiguraci CHECK constraints do
   `Configurations/hr/CoachAttendanceConfiguration.cs`.
3. Doplnit kolekční navigace do `Models/hr/Coach.cs` a
   `Models/identity/User.cs`.
4. Přidat `CoachAttendances` do `Context/SportSysDbContext.cs`.
5. Nevytvářet ani neupravovat EF Core migraci; migraci vytvoří uživatel.

### Fáze 2: Auditní identita

1. Definovat stabilní claim typu pro lokální `identity.User.Id`.
2. Doplnit claim v `EntraClaimsTransformation` po úspěšném načtení nebo
   vytvoření uživatele.
3. Přidat Contract resolver, který podporuje Entra i lokální Identity principal.
4. Chybějící lokální ID řešit explicitní chybou a zabránit uploadu bez auditní
   vazby.

### Fáze 3: DTO a validace

1. Přidat filter, upload, list, select a file DTO.
2. Implementovat validační pravidla období.
3. Implementovat testovatelnou validaci XLSX včetně limitu 10 MiB.
4. Nepřenášet binární data do list DTO.

### Fáze 4: Contract služba

1. Implementovat seznam s filtry a projekcí.
2. Implementovat seznam trenérů pro select.
3. Implementovat bezpečný upload s kontrolou trenéra a uživatele.
4. Implementovat načtení jednotlivého souboru.
5. Zaregistrovat službu výhradně v
   `SportSys.Contract/ServiceCollectionExtensions.cs`.

### Fáze 5: HR stránka

1. Vytvořit `Attendance/Index.cshtml.cs`.
2. Vytvořit oddělený multipart upload formulář a GET filtr.
3. Zobrazit historii bez načítání obsahu souborů.
4. Přidat download handler s privátními no-store hlavičkami.
5. Doplnit stavové a validační zprávy a PRG po úspěšném uploadu.

### Fáze 6: Navigace a dokumentace

1. Přidat položku `Docházka` do HR submenu v `_Layout.cshtml`.
2. Aktualizovat `docs/modules/hr.md` o evidenci zdrojových docházkových
   souborů, validaci, audit a rozsah první etapy.
3. Aktualizovat `docs/architecture.md`, pokud diagram HR modelu vypisuje
   konkrétní podřízené entity `Coach`.

### Fáze 7: Testy a ověření

1. Přidat xUnit testy validačních modelů a XLSX helperu.
2. Ověřit, že Razor projekt nepoužívá `SportSys.Database`.
3. Sestavit solution a zkompilovat SCSS existujícím build targetem.
4. Po uživatelském vytvoření migrace zkontrolovat generované FK, indexy,
   CHECK constraints a absenci unikátního omezení období.

## Soubory ke změně

### Nové

- `src/SportSys.Database/Models/hr/CoachAttendance.cs`
- `src/SportSys.Database/Configurations/hr/CoachAttendanceConfiguration.cs`
- `src/SportSys.Contract/Models/hr/CoachAttendanceModels.cs`
- `src/SportSys.Contract/Services/CoachAttendanceService.cs`
- `src/SportSys.Contract/Auth/SportSysClaimTypes.cs`
- `src/SportSys.Contract/Auth/CurrentUserIdResolver.cs`
- `src/SportSys.Razor/Areas/hr/Pages/Attendance/Index.cshtml`
- `src/SportSys.Razor/Areas/hr/Pages/Attendance/Index.cshtml.cs`
- `tests/SportSys.Razor.Tests/CoachAttendanceTests.cs`

### Existující

- `src/SportSys.Database/Models/hr/Coach.cs`
- `src/SportSys.Database/Models/identity/User.cs`
- `src/SportSys.Database/Context/SportSysDbContext.cs`
- `src/SportSys.Contract/Auth/EntraClaimsTransformation.cs`
- `src/SportSys.Contract/ServiceCollectionExtensions.cs`
- `src/SportSys.Razor/Pages/Shared/_Layout.cshtml`
- `src/SportSys.Razor/Styles/_hr.scss` pouze při potřebě nového layoutu
- `docs/modules/hr.md`
- `docs/architecture.md` pouze pokud bude potřeba doplnit diagram

## Testy a ověření

### DTO a období

- měsíc mimo 1–12 je odmítnut,
- neúplné období filtru je odmítnuto,
- období `od` pozdější než `do` je odmítnuto,
- hranice mezi prosincem a lednem se porovnává chronologicky správně.

### Soubor

- validní `.xlsx` s očekávanou Open XML strukturou je přijat,
- `.xls`, `.csv`, přejmenovaný ZIP a jiný typ souboru jsou odmítnuty,
- soubor s nesprávným MIME typem je odmítnut,
- poškozený ZIP je odmítnut,
- prázdný a nadlimitní soubor je odmítnut,
- cesta v klientském názvu souboru se odstraní,
- výstupní název nepřekročí databázový limit.

### Contract vrstva

- list dotaz neprojektuje `FileContent`,
- filtr podle trenéra funguje samostatně i s obdobím,
- více uploadů stejného trenéra za stejný měsíc zůstane zachováno,
- nelze uložit docházku pro neexistujícího trenéra,
- nelze uložit upload s neexistujícím uživatelem,
- download vrátí přesně původní bytes a bezpečný název,
- neexistující ID vrátí `null` a PageModel jej převede na `404`.

### Identita

- Entra principal dostane lokální SportSys user ID claim,
- lokální Identity principal se vyřeší z číselného `NameIdentifier`,
- neplatný principal nemůže vytvořit auditní záznam.

### Razor a architektura

- oba formuláře se navzájem neblokují validací,
- upload formulář má `multipart/form-data`,
- validační chyba zachová zvoleného trenéra a období,
- download odpověď má `private, no-store`,
- HR menu obsahuje položku `Docházka`,
- Razor Area `hr` neobsahuje `using SportSys.Database` ani referenci na
  databázové entity.

## Manuální akceptace

1. Přihlásit se přes Entra ID a otevřít `Personalistika → Docházka`.
2. Nahrát validní XLSX pro vybraného trenéra a měsíc.
3. Ověřit nový řádek se správným trenérem, obdobím, názvem, časem a uživatelem.
4. Nahrát druhý soubor pro stejného trenéra a měsíc a ověřit zachování obou
   záznamů.
5. Stáhnout oba záznamy a binárně porovnat s původními soubory.
6. Vyzkoušet filtr pouze podle trenéra, pouze podle období a jejich kombinaci.
7. Ověřit přechod filtru přes konec roku.
8. Ověřit odmítnutí `.xls`, přejmenovaného ZIP, poškozeného a nadlimitního
   souboru.
9. Ověřit, že přímý download neexistujícího ID vrátí `404`.
10. Ověřit, že odhlášený uživatel je zachycen stávající fallback policy.

## Beze změny

- `sport.CoachTraining` a hodnoty `EParticipationType`,
- rozvrh, plán a editace jednotlivých tréninků,
- importní logika v `SportSys.ConsoleApp`,
- existující fotografie, licence, smlouvy a nastavení trenéra,
- obsah a formát dodávaných XLSX souborů.

## Mimo rozsah

- parsování buněk XLSX,
- výpočet skutečné účasti nebo odměny trenéra,
- propojení řádků souboru s `sport.Training`,
- změny `sport.CoachTraining`,
- schvalování, zamykání nebo workflow zpracování,
- verzování odvozených dat,
- mazání nahraných souborů,
- export či generování docházky,
- notifikace a dávkové zpracování.

## Hotovo, když

- existuje model `hr.CoachAttendance` se všemi požadovanými vazbami,
  omezeními a explicitními indexy,
- každý upload má platného trenéra, období, UTC čas a auditního uživatele,
- validní XLSX lze bezpečně nahrát, zobrazit v historii a znovu stáhnout,
- historie uchovává více souborů pro stejného trenéra a měsíc,
- přehled lze filtrovat podle trenéra a období od–do,
- seznam nikdy nenačítá binární obsah dokumentů,
- položka `Docházka` je dostupná v HR menu,
- nejsou čtena ani zapisována docházková data do sportovních tabulek,
- Razor vrstva komunikuje pouze přes Contract,
- dokumentace a testy odpovídají výslednému chování,
- agent nevytvořil ani neupravil EF Core migraci.
