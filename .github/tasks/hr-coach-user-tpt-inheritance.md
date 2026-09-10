# Implementační plán: TPT dědičnost User → Coach

**Stav:** Implementováno v aplikačním kódu. Zbývá uživatelsky vytvořená,
ručně dopracovaná a nacvičená EF Core migrace.

## Cíl

Změnit personální model tak, aby trenér byl specializovaným uživatelem:

```text
identity.User
      │  TPT, PK = FK
      ▼
hr.Coach
```

`Coach` bude v C# dědit z `SportSys.Database.Models.identity.User` a jeho
tabulka `hr.Coach` bude obsahovat pouze trenérská pole. Stejný princip se
promítne do Contract DTO: DTO reprezentující trenéra budou dědit ze
společných uživatelských DTO a nebudou mít druhý nezávislý identifikátor.

## Upřesnění názvu tabulky

Aktuální uživatelská tabulka projektu je `identity.User`, nikoli `dbo.User`.
Plán proto cílí na TPT mezi:

- základní tabulkou `identity.User`,
- odvozenou tabulkou `hr.Coach`.

Přesun Identity tabulky do schématu `dbo` není součástí změny. Pokud označení
`dbo.User` znamenalo požadavek na změnu schématu Identity, musí být řešeno
samostatně, protože by zasáhlo všechny Identity FK, konfiguraci a migrace.

## Výchozí stav

### Databázový model

- `User : IdentityUser<int>` je mapován do `identity.User`.
- `Coach` je samostatná entita s vlastním generovaným `Id`.
- Aktivní model nemá vazbu `Coach.UserId`; dřívější kompoziční vazba je
  zakomentovaná.
- `Coach.DisplayName` duplikuje `User.DisplayName`.
- `BirthNumber` a unikátní indexy `PersonalNumber`/`BirthNumber` jsou
  zakomentované.
- Na `Coach.Id` odkazují:
  - `hr.CoachSetting.CoachId`,
  - `hr.CoachLicense.CoachId`,
  - `hr.CoachContract.CoachId`,
  - `hr.CoachAttendance.CoachId`,
  - `sport.CoachTraining.CoachId`,
  - `sport.CoachTrainingPlan.CoachId`,
  - `sport.CoachTrainingRequirement.CoachId`.

### Contract a UI

- `CoachDetailDto` již formálně dědí z `UserDto`, ale obsahuje současně
  `UserId` i `CoachId`.
- `CoachListItem`, `CoachSelectItem` a
  `TrainingRequirementCoachListItem` opakují uživatelská a trenérská pole bez
  společné DTO hierarchie.
- Vytvoření trenéra vybírá existujícího uživatele, ale současný model vazbu
  neukládá.
- `CoachService` má části práce s `User` a vyhledávání zakomentované.

### Historické rozhodnutí

`.github/tasks/hr-coach-administration.md` dříve doporučoval kompozici a TPT
výslovně odmítal. Tento plán uvedené rozhodnutí nahrazuje. Původní task zůstane
beze změny jako historický dokument.

### Vztah k výchozí strategii TPC

Projekt nadále preferuje TPC podle
`docs/decisions/adr-003-tpc-se-sdilenymi-sekvencemi.md`. TPT v hierarchii
`User → Coach` je vědomá lokální výjimka, nikoli změna obecné konvence.

TPT je zde výhodnější, protože:

- každý trenér musí mít právě jeden fyzický řádek v `identity.User`,
- trenér musí sdílet stejné Identity ID, přihlašovací údaje a profilová pole,
- stávající HR a Sport FK mohou odkazovat na fyzickou tabulku `hr.Coach`,
- TPC by duplikovalo Identity sloupce do `hr.Coach`, nebo by vyžadovalo
  oddělenou kompoziční vazbu a přestalo by vyjadřovat požadovanou dědičnost,
- Identity store potřebuje jednu základní tabulku uživatelů, což je s TPC
  hierarchií neslučitelné.

ADR-003 zůstává platné pro hierarchie, jejichž konkrétní typy jsou samostatné
agregáty a nepotřebují fyzický řádek společného předka.

## Potvrzená technická rozhodnutí

1. `User` zůstane neabstraktní základní třídou, protože ne každý uživatel je
   trenér.
2. `Coach : User` bude mapován pomocí TPT jako explicitní výjimka z výchozí
   projektové preference TPC.
3. `Coach.Id` nebude deklarován znovu; zděděné `User.Id` je současně:
   - PK `identity.User`,
   - PK a FK `hr.Coach`,
   - cílový klíč všech stávajících trenérských vazeb.
4. `DisplayName`, `UserName`, `Email` a `PhoneNumber` budou existovat pouze v
   `identity.User`.
5. `PersonalNumber`, `BirthNumber` a fotografie zůstanou v `hr.Coach`.
6. `PersonalNumber` a normalizované `BirthNumber` budou unikátní.
7. Výběr existujícího uživatele při založení trenéra zůstane zachován.
8. Protože EF Core neumí změnit již uloženou instanci základního CLR typu na
   odvozený typ, povýšení existujícího uživatele vloží pouze řádek do
   `hr.Coach` explicitním parametrizovaným SQL příkazem v transakci.
9. DTO, která sama reprezentují uživatele nebo trenéra, budou používat
   dědičnost. DTO reprezentující docházku, smlouvu nebo jiný vztah zůstanou
   kompoziční a ponechají významové pole `CoachId`.
10. Agent nevytvoří ani neupraví EF Core migraci.

## Cílový databázový model

### `identity.User`

Základní tabulka zůstane beze změny a bude obsahovat Identity a společná
profilová pole:

- `Id`,
- `UserName`, `NormalizedUserName`,
- `Email`, `NormalizedEmail`,
- `PhoneNumber`,
- `EntraOid`, `EntraTenantId`,
- `DisplayName`,
- `IsLocalAccount`,
- `LastLoginUtc`,
- ostatní sloupce ASP.NET Core Identity.

### `hr.Coach`

Odvozená TPT tabulka bude obsahovat:

| Sloupec | SQL typ | Null | Poznámka |
|---|---|---:|---|
| `Id` | `int` | ne | PK a FK → `identity.User.Id`, bez identity |
| `PersonalNumber` | `varchar(20)` | ne | Unikátní osobní číslo |
| `BirthNumber` | `varchar(10)` | ne | Normalizované rodné číslo |
| `Photo` | `varbinary(max)` | ano | Binární fotografie |
| `PhotoContentType` | `varchar(100)` | ano | Povolený MIME typ |
| `PhotoFileName` | `nvarchar(255)` | ano | Bezpečný původní název |

`DisplayName` bude z `hr.Coach` odstraněno, protože je zděděno z
`identity.User`.

### Omezení

- PK `PK_Coach` nad `Id`.
- Inheritance FK `FK_Coach_User_Id` z `hr.Coach.Id` na `identity.User.Id`.
- Unikátní index `UX_Coach_PersonalNumber`.
- Unikátní index `UX_Coach_BirthNumber`.
- Stávající FK z HR a Sport tabulek budou nadále odkazovat na
  `hr.Coach.Id`.
- Odstranění `identity.User` s trenérskými závislostmi musí zůstat blokované
  existujícími `Restrict` vazbami; aplikace nebude fyzické mazání trenérů
  zavádět.

## EF Core návrh

### Model `Coach`

Upravit `src/SportSys.Database/Models/hr/Coach.cs`:

```csharp
[Table(nameof(Coach), Schema = Schemas.Hr)]
[Index(nameof(PersonalNumber), IsUnique = true, Name = "UX_Coach_PersonalNumber")]
[Index(nameof(BirthNumber), IsUnique = true, Name = "UX_Coach_BirthNumber")]
public class Coach : User
{
    [StringLength(20)]
    [Unicode(false)]
    public required string PersonalNumber { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public required string BirthNumber { get; set; }

    // Photo a navigační kolekce zůstávají.
}
```

Odstranit:

- vlastní deklaraci `Coach.Id`,
- `Coach.DisplayName`,
- zakomentované `UserId`,
- zakomentovanou navigaci `Coach.User`,
- zakomentované indexy nahrazené aktivními atributy.

### Konfigurace strategie

Vytvořit
`src/SportSys.Database/Configurations/identity/UserConfiguration.cs`:

- implementovat `IEntityTypeConfiguration<User>`,
- nastavit `UseTptMappingStrategy()` na kořenové entitě `User`,
- mapovat základ na `identity.User`,
- ponechat mapování derived typu na `hr.Coach` atributem `[Table]`, případně
  jej potvrdit explicitním `ToTable`, pokud to bude vyžadovat validace modelu.

Ze `SportSysDbContext.OnModelCreating` odstranit duplicitní
`modelBuilder.Entity<User>().ToTable(...)`; ostatní Identity tabulky zůstanou
mapované na schéma `identity`.

`DbSet<Coach>` zůstane zachován.

### Dopad na Identity

- `UserManager<User>` a `SignInManager<User>` zůstanou beze změny.
- Entra provisioning nadále vytváří základní `User`, nikoli automaticky
  `Coach`.
- Dotaz na `_db.Users` může materializovat instanci `Coach`, pokud existuje
  odpovídající derived řádek; pro Identity jde stále o platný `User`.
- Ověřit SQL generované pro běžné Identity dotazy, protože TPT přidává při
  polymorfním dotazu na základní typ join na `hr.Coach`.

## Povýšení existujícího uživatele na trenéra

Aktuální UI vybírá již existujícího Identity uživatele. Běžné
`_db.Coaches.Add(new Coach { Id = userId })` není vhodné: EF by odvozenou
entitu považoval za novou a pokusil by se vložit také základní řádek
`identity.User`.

`CoachService.CreateAsync` proto bude:

1. pracovat v `Serializable` transakci,
2. načítat vybraný základní `User`,
3. ověřovat, že `_db.Coaches` neobsahuje stejné `Id`,
4. normalizovat a ověřovat osobní a rodné číslo,
5. aktualizovat společná pole uživatele přes `UserManager<User>` nebo
   stávající normalizer,
6. uložit změny základního uživatele,
7. parametrizovaným `ExecuteSqlInterpolatedAsync` vložit pouze derived řádek
   do `[hr].[Coach]`,
8. převést porušení unikátních constraintů na konkrétní
   `CoachValidationException`,
9. commitnout transakci a vrátit sdílené `User.Id`.

SQL operaci zapouzdřit v jedné privátní metodě s komentářem, proč nelze použít
standardní `DbSet.Add`. Nepoužívat obecný catch ani interpolovaný řetězec bez
parametrizace.

Po vytvoření musí další dotaz proběhnout v novém nebo vyčištěném change
trackeru, aby se původně načtený `User` nevrátil místo `Coach`.

## DTO hierarchie

### Detail

V `src/SportSys.Contract/Models/hr/CoachModels.cs`:

```text
UserDto
  └── CoachDetailDto
```

- `UserDto.UserId` přejmenovat na společné `Id`.
- `CoachDetailDto` ponechat jako potomka `UserDto`.
- odstranit `CoachDetailDto.CoachId`,
- zděděné `Id` používat jako user i coach identifikátor,
- `PersonalNumber`, `BirthNumber`, fotografie a podřízené kolekce zůstanou v
  `CoachDetailDto`.

### Seznamové a výběrové DTO

Přidat lehké základní projekce, aby seznamy nepřenášely telefon a další
nepotřebná detailní pole:

```text
UserListItem
  └── CoachListItem

UserSelectItem
  └── CoachSelectItem
        └── TrainingRequirementCoachListItem
```

- `UserListItem`: `Id`, `DisplayName`, `Email`.
- `CoachListItem`: `PersonalNumber`, `HasPhoto`, licence a smlouvy.
- `UserSelectItem`: `Id`, `DisplayName`, `Email`.
- `CoachSelectItem`: `PersonalNumber`.
- `TrainingRequirementCoachListItem`: role trenéra a `DisplayText`.

Pokud by dědění `TrainingRequirementCoachListItem` přes namespace modulu
zhoršovalo závislosti, může dědit přímo z nového společného
`CoachSummaryDto`. Nevytvářet dědičnost pro:

- `CoachAttendanceListItem`,
- `CoachAttendanceUploadDto`,
- `CoachContractDto`,
- `CoachSettingDto`,
- `CoachLicenseDto`.

Tyto typy nejsou trenérem samotným, ale záznamem nebo vztahem; jejich
`CoachId` proto zůstává.

## Contract služby

### `CoachService`

Upravit:

- seznamové projekce na zděděné `DisplayName`, `Email` a společné `Id`,
- znovu aktivovat vyhledávání a řazení podle uživatelských polí,
- `GetByIdAsync` projektovat jediný `Id`,
- `GetAvailableUsersAsync` vyloučit uživatele, jejichž `Id` již existuje v
  `_db.Coaches`; při editaci lze zahrnout aktuální ID,
- `CreateAsync` změnit na bezpečné povýšení existujícího uživatele,
- `UpdateBasicAsync` načíst `Coach` a aktualizovat v jedné entitě zděděná i
  trenérská pole,
- obnovit ukládání a unikátní kontrolu `BirthNumber`,
- odstranit zakomentované větve původní kompozice.

### Další projekce

Upravit podle nové DTO hierarchie:

- `CoachAttendanceService.GetCoachesAsync`,
- `CoachAttendanceService.GetAllAsync`,
- `TrainingRequirementService`,
- případné rozvrhové projekce zobrazující jméno trenéra.

Dotazy mají nadále projektovat pouze potřebná pole; nezavádět `Include` jen
kvůli TPT.

## Razor Pages

### Vytvoření trenéra

V `Areas/hr/Pages/Coach/Edit.cshtml.cs` oddělit:

- `BasicInput.Id` — ID již existujícího trenéra při editaci,
- samostatnou bindovanou hodnotu `SelectedUserId` — uživatel vybraný při
  vytváření.

Tím zůstane `IsNew` založené na `BasicInput.Id == 0` správné i po neplatném
POSTu, kdy již uživatel provedl výběr.

`CreateAsync` bude volán s `SelectedUserId` a `BasicInput`; po úspěchu
přesměruje na sdílené ID.

### Editace

- skryté `BasicInput.CoachId` nahradit `BasicInput.Id`,
- při editaci zobrazit zděděné `UserName`,
- propojeného uživatele nelze změnit, protože změna ID by znamenala změnu
  identity celé entity,
- odkazy na detail, fotografii a záložky používat společné `Id`,
- podřízené formuláře dále používají `CoachId`, protože ukládají FK.

Aktualizovat:

- `Areas/hr/Pages/Coach/Edit.cshtml.cs`,
- `Areas/hr/Pages/Coach/_Basic.cshtml`,
- `Areas/hr/Pages/Coach/Index.cshtml`,
- `_Contracts.cshtml`, `_Settings.cshtml`, `_Licenses.cshtml` pouze tam, kde
  čtou ID z hlavního DTO,
- `Areas/hr/Pages/Attendance/Index.cshtml.cs` pro nové `CoachSelectItem.Id`.

## Datová migrace

TPT mění význam primárního klíče `hr.Coach.Id`. Současná čísla trenérů nelze
automaticky považovat za `identity.User.Id`.

### Povinná vstupní data

Před vytvořením uživatelské migrace musí existovat jednoznačná mapa:

```text
LegacyCoachId -> UserId
```

Musí platit:

- každý existující trenér má právě jednoho uživatele,
- jeden uživatel je přiřazen nejvýše jednomu trenérovi,
- každý `UserId` existuje v `identity.User`,
- nové cílové hodnoty nevytvoří duplicity v kompozitních PK sportovních
  vazeb,
- `PersonalNumber` a `BirthNumber` jsou úplné a unikátní.

Mapování nesmí být odvozováno jen z podobného jména. Jednoznačné shody lze
navrhnout podle e-mailu nebo jiného potvrzeného identifikátoru, ale výsledek
musí uživatel zkontrolovat.

### Doporučený postup uživatelské migrace

1. Zazálohovat databázi a nacvičit migraci na kopii produkčních dat.
2. Vytvořit dočasnou mapovací tabulku s unikátním `LegacyCoachId` i `UserId`.
3. Naplnit mapu a ukončit migraci chybou, pokud není úplná nebo jednoznačná.
4. Odstranit FK všech tabulek odkazujících na `hr.Coach`.
5. Vytvořit novou dočasnou TPT tabulku trenérů s `Id` bez identity.
6. Přenést trenérská data přes mapu:
   - `Id = UserId`,
   - `DisplayName` doplnit do `identity.User`, pokud tam chybí,
   - `PersonalNumber`, `BirthNumber` a fotografie vložit do derived tabulky.
7. Přemapovat `CoachId` ve všech HR a Sport tabulkách z `LegacyCoachId` na
   `UserId`.
8. Ověřit počty řádků a absenci osiřelých FK.
9. Nahradit původní `hr.Coach` novou tabulkou.
10. Vytvořit PK/FK TPT, unikátní indexy a obnovit všechny závislé FK a indexy.
11. Odstranit dočasnou mapovací tabulku až po závěrečných kontrolách.

Pokud již v cílové databázi platí `Coach.Id == User.Id` pro všechny trenéry,
lze postup zjednodušit, ale tato podmínka se musí ověřit SQL dotazem; nesmí se
předpokládat.

### Rollback

`Down` migrace musí obnovit samostatné trenérské ID a opačně přemapovat
závislosti. Proto je nutné mapu po dobu rollback okna zachovat nebo mít její
ověřenou zálohu. Automaticky vygenerovaný `Down` nebude pro tuto datovou změnu
dostatečný.

## Testy

### EF Core metadata

Přidat `tests/SportSys.Razor.Tests/CoachTptMappingTests.cs`:

- `Coach` má base type `User`,
- `User` je mapován do `identity.User`,
- `Coach` je mapován do `hr.Coach`,
- `Coach.Id` je zděděný klíč,
- model používá TPT,
- `Coach` nemá samostatnou vlastnost `UserId`,
- FK podřízených entit směřují na `Coach`,
- `UserUploadId` docházky stále směřuje na základní `User`.

Test sestaví SQL Server model bez připojení k databázi; nepřidávat SQLite nebo
InMemory provider, protože neověří SQL Server TPT mapování.

### DTO a validace

- `CoachDetailDto` dědí z `UserDto` a má jediný `Id`.
- `CoachListItem` a `CoachSelectItem` dědí z odpovídajícího uživatelského DTO.
- normalizace a validace rodného čísla zůstává funkční,
- podřízená DTO stále používají `CoachId`.

### Contract služba

Na testovací SQL Server databázi ověřit:

- povýšení existujícího `User` vloží pouze řádek `hr.Coach`,
- druhé povýšení stejného uživatele skončí validační chybou,
- duplicitní osobní nebo rodné číslo skončí konkrétní validační chybou,
- editace trenéra aktualizuje správnou základní i derived tabulku,
- `_db.Users` vrátí trenéra jako `Coach`,
- `_db.Coaches` vrátí pouze uživatele s derived řádkem,
- Entra provisioning běžného uživatele nevytvoří `hr.Coach`.

### Migrační rehearsal

Na kopii reálných dat ověřit:

- počet trenérů před a po migraci je stejný,
- všechny závislé tabulky mají stejné počty řádků,
- všechny `CoachId` existují v `hr.Coach`,
- všechny `hr.Coach.Id` existují v `identity.User`,
- žádný uživatel nemá více trenérských řádků,
- po rollbacku jsou původní ID a FK obnovené.

## Dokumentace a ADR

Vytvořit nové přijaté ADR:

```text
docs/decisions/adr-004-coach-jako-tpt-potomek-user.md
```

ADR popíše:

- proč TPT nahrazuje původně plánovanou kompozici,
- proč je TPT v této konkrétní Identity hierarchii vhodnější než preferované
  TPC,
- že ADR-003 není nahrazeno a TPC zůstává výchozí strategií ostatních
  hierarchií,
- sdílený primární klíč,
- dopad na Identity dotazy,
- nutnost explicitního povýšení existujícího uživatele,
- migrační rizika a výkonové náklady TPT.

Aktualizovat:

- `docs/decisions/README.md`,
- `docs/architecture.md`,
- `docs/modules/auth.md`,
- `docs/modules/hr.md`,
- `docs/conventions.md` o pravidlo „TPC jako výchozí strategie, TPT nad
  Identity jako zdokumentovaná výjimka“.

Historický `.github/tasks/hr-coach-administration.md` neupravovat.

## Implementační fáze

### Fáze 1: Databázové entity a metadata

1. Změnit `Coach` na potomka `User`.
2. Obnovit `BirthNumber` a unikátní indexy.
3. Odstranit duplicitní ID, jméno a kompoziční zbytky.
4. Přidat explicitní TPT konfiguraci kořenové entity.
5. Upravit `SportSysDbContext`.
6. Přidat metadata testy.

### Fáze 2: DTO hierarchie

1. Sjednotit detailní ID na `UserDto.Id`.
2. Přidat lehké base DTO pro seznam a výběr.
3. Odvodit trenérské DTO.
4. Zachovat `CoachId` pouze u vztahových DTO.
5. Upravit DTO testy.

### Fáze 3: Contract služby

1. Obnovit projekce zděděných uživatelských polí.
2. Implementovat bezpečné povýšení existujícího uživatele.
3. Obnovit uložení a unikátnost rodného čísla.
4. Upravit seznam dostupných uživatelů.
5. Upravit attendance a training requirement projekce.

### Fáze 4: Razor UI

1. Oddělit `SelectedUserId` od identity nového `CoachDetailDto`.
2. Převést hlavní formulář a routy na společné `Id`.
3. Zachovat `CoachId` podřízených formulářů.
4. Ověřit menu, fotografie, záložky a validační reload.

### Fáze 5: Dokumentace

1. Přidat ADR-004.
2. Aktualizovat architekturu, Auth, HR a konvence.
3. Odstranit dokumentaci dočasného kompozičního modelu.

### Fáze 6: Ověření

1. Spustit metadata a DTO testy.
2. Spustit celou existující testovací sadu v Release.
3. Provést code review se zaměřením na Identity a SQL promotion.
4. Předat uživateli přesný checklist pro vytvoření a rehearsal migrace.

## Soubory ke změně

### Nové

- `src/SportSys.Database/Configurations/identity/UserConfiguration.cs`
- `tests/SportSys.Razor.Tests/CoachTptMappingTests.cs`
- `docs/decisions/adr-004-coach-jako-tpt-potomek-user.md`

### Existující

- `src/SportSys.Database/Models/hr/Coach.cs`
- `src/SportSys.Database/Context/SportSysDbContext.cs`
- `src/SportSys.Contract/Models/hr/CoachModels.cs`
- `src/SportSys.Contract/Models/hr/CoachAttendanceModels.cs`
- `src/SportSys.Contract/Models/TrainingRequirementDto.cs`
- `src/SportSys.Contract/Services/CoachService.cs`
- `src/SportSys.Contract/Services/CoachAttendanceService.cs`
- `src/SportSys.Contract/Services/TrainingRequirementService.cs`
- `src/SportSys.Razor/Areas/hr/Pages/Coach/Edit.cshtml.cs`
- `src/SportSys.Razor/Areas/hr/Pages/Coach/_Basic.cshtml`
- `src/SportSys.Razor/Areas/hr/Pages/Coach/Index.cshtml`
- `src/SportSys.Razor/Areas/hr/Pages/Coach/_Contracts.cshtml`
- `src/SportSys.Razor/Areas/hr/Pages/Coach/_Settings.cshtml`
- `src/SportSys.Razor/Areas/hr/Pages/Coach/_Licenses.cshtml`
- `src/SportSys.Razor/Areas/hr/Pages/Attendance/Index.cshtml.cs`
- `tests/SportSys.Razor.Tests/TrainingRequirementTests.cs`
- `tests/SportSys.Razor.Tests/CoachAttendanceTests.cs`
- `docs/decisions/README.md`
- `docs/architecture.md`
- `docs/modules/auth.md`
- `docs/modules/hr.md`
- `docs/conventions.md`

### Uživatelsky vlastněná migrace

- nový soubor v `src/SportSys.Database/Migrations/`,
- aktualizace `SportSysDbContextModelSnapshot.cs`.

Tyto soubory agent nevytváří ani neupravuje.

## Manuální akceptace

1. Běžný Identity uživatel není trenér a neobjeví se v `_db.Coaches`.
2. Administrátor vybere existujícího uživatele a vytvoří mu trenérský profil.
3. Stejné ID se používá v `identity.User`, `hr.Coach` i ve sportovních FK.
4. Již povýšeného uživatele nelze vybrat ani povýšit podruhé.
5. Editace jména, e-mailu a telefonu mění pouze `identity.User`.
6. Editace osobního čísla, rodného čísla a fotografie mění pouze `hr.Coach`.
7. Rozvrhy, požadavky, smlouvy, licence, nastavení a docházka zobrazují stejné
   trenéry jako před změnou.
8. Entra login a lokální Identity login zůstávají funkční.
9. Nahrávající uživatel docházky může být trenér i běžný uživatel.
10. Migrace na kopii dat zachová všechny trenérské vazby.

## Mimo rozsah

- Přesun `identity.User` do schématu `dbo`.
- Automatické vytvoření trenéra při každém Entra přihlášení.
- Fyzické mazání trenérů nebo uživatelů.
- Změna dočasně vypnuté autorizace HR Area.
- Generování nebo aplikace EF Core migrace agentem.

## Hotovo, když

- `Coach : User` je explicitně mapován jako TPT.
- Dokumentace potvrzuje, že TPC zůstává výchozí projektovou strategií a tato
  hierarchie je zdůvodněnou výjimkou.
- `hr.Coach.Id` je PK/FK na `identity.User.Id` a není identity sloupec.
- Společná uživatelská pole nejsou duplikována v `hr.Coach`.
- Contract DTO mají odpovídající dědičnost a jeden identifikátor entity.
- Vytvoření trenéra bezpečně povýší existujícího uživatele.
- Všechny HR a Sport vazby používají nové sdílené ID.
- Metadata, DTO a existující funkční testy procházejí.
- Dokumentace a ADR odpovídají TPT modelu.
- Migrační mapování bylo nacvičeno na kopii dat.
- Agent nevytvořil ani neupravil migraci.
