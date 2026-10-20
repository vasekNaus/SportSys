# Implementační plán: Personalistika trenérů

**Stav:** Připraveno k implementaci. EF Core migraci vytvoří a aplikuje výhradně uživatel.

## Cíl

Přidat databázové schéma `hr` pro personální agendu trenérů a administrační Area
`hr` se stránkami `Coach/Index` a `Coach/Edit`.

Řešení zahrnuje:

- propojení trenéra s uživatelem ASP.NET Core Identity,
- personální údaje a fotografii,
- časově platné personální nastavení,
- časově platné trenérské licence,
- smlouvy trenérů pro jednotlivé sezony,
- administrační formulář rozdělený do záložek.

## Výchozí stav a zásadní rozhodnutí

- Uživatelská tabulka se v aktuálním projektu jmenuje `identity.User`, nikoliv
  `dbo.User`.
- V projektu již existuje `dbo.Coach`. Odkazují na ni tabulky
  `sport.CoachTraining`, `sport.CoachTrainingRequirement` a
  `sport.CoachTrainingPlan`.
- Stávající `Coach.Id` se zachová, aby nebylo nutné přečíslovat existující
  sportovní záznamy.
- Databázově bude `Coach` propojen s `identity.User` povinnou vazbou 1:1 přes
  samostatný unikátní `UserId`.
- EF entity budou používat kompozici. Požadovaná dědičnost bude realizována
  v aplikačním DTO modelu jako `CoachDetailDto : UserDto`.
- Nedoporučuje se mapovat EF entitu `Coach : User` pomocí TPT. Komplikovalo by
  to ASP.NET Core Identity, automatický Entra provisioning a převod existujících
  trenérů na uživatele.
- Fyzické mazání personálních a smluvních záznamů se nebude používat tam, kde
  je nutné zachovat historii.

## Fáze 1: Schéma `hr`

1. Do `src/SportSys.Database/Models/Schemas.cs` přidat:

   ```csharp
   public const string Hr = "hr";
   ```

2. Nové entity ukládat do:

   ```text
   src/SportSys.Database/Models/hr/
   ```

3. Potřebné Fluent API konfigurace ukládat do:

   ```text
   src/SportSys.Database/Configurations/hr/
   ```

4. Každá entita musí mít atribut
   `[Table(nameof(Entity), Schema = Schemas.Hr)]`.
5. Všechny FK a časové dotazy musí mít explicitní indexy, protože automatická
   konvence FK indexů je v projektu vypnutá.
6. Nevytvářet ani neupravovat EF Core migraci. Po dokončení modelu ji vytvoří
   uživatel.

## Fáze 2: `hr.Coach`

Přesunout stávající `dbo.Coach` do schématu `hr` a rozšířit jej.

| Sloupec | SQL typ | Null | Poznámka |
|---|---|---:|---|
| `Id` | `int` | ne | Stávající PK, zachovat hodnoty |
| `User_Id` | `int` | ne | FK na `identity.User.Id` |
| `PersonalNumber` | `varchar(20)` | ne | Osobní číslo |
| `BirthNumber` | `varchar(10)` | ne | Rodné číslo bez lomítka |
| `Photo` | `varbinary(max)` | ano | Binární obsah fotografie |
| `PhotoContentType` | `varchar(100)` | ano | Např. `image/jpeg` |
| `PhotoFileName` | `nvarchar(255)` | ano | Původní název souboru |

### Omezení a indexy

- unikátní index `UX_Coach_User` na `User_Id`,
- unikátní index `UX_Coach_PersonalNumber`,
- unikátní index `UX_Coach_BirthNumber`,
- FK na uživatele s `DeleteBehavior.Restrict`,
- stávající FK ze `sport.*` nadále odkazují na `Coach.Id`.

### Validace

- `BirthNumber` před uložením normalizovat odstraněním `/` a mezer,
- povolit pouze 9 nebo 10 číslic,
- rodné číslo nezobrazovat v přehledu a nezapisovat do logů,
- `PersonalNumber` oříznout a vyžadovat neprázdnou hodnotu,
- fotografie povolit pouze jako JPEG, PNG nebo WebP,
- nastavit maximální velikost fotografie, doporučeně 5 MB,
- nekontrolovat typ fotografie pouze podle přípony; ověřit MIME typ i signaturu
  souboru.

### Stávající jmenné sloupce

Současná entita obsahuje `FirstName`, `LastName` a computed `FullName`.
Po zavedení vazby na uživatele budou základní identifikační údaje čteny
z `identity.User`.

Migrační postup:

1. Dočasně ponechat původní jmenné sloupce.
2. Propojit každého trenéra s konkrétním uživatelem.
3. Pokud `User.DisplayName` chybí, naplnit jej z `FirstName + LastName`.
4. Ověřit, že všichni trenéři mají uživatele.
5. Teprve poté odstranit `FirstName`, `LastName`, `FullName` a původní
   `CoachConfiguration` s computed sloupcem.

## Fáze 3: `hr.CoachSetting`

Entita uchovává časově platné bankovní, adresní a pojistné údaje.

| Sloupec | SQL typ | Null | Poznámka |
|---|---|---:|---|
| `Id` | `int` | ne | PK |
| `Coach_Id` | `int` | ne | FK na `hr.Coach.Id` |
| `ValidFrom` | `date` | ne | Začátek platnosti |
| `ValidTo` | `date` | ano | Konec platnosti včetně |
| `BankAccountPrefix` | `varchar(6)` | ano | Nepovinná předčíslí |
| `BankAccountNumber` | `varchar(10)` | ne | Číslo účtu |
| `BankCode` | `varchar(4)` | ne | Kód banky |
| `Street` | `nvarchar(200)` | ne | Podle adresního vzoru `IceRink` |
| `City` | `nvarchar(100)` | ne | Podle adresního vzoru `IceRink` |
| `ZipCode` | `varchar(10)` | ne | PSČ |
| `HealthInsuranceCode` | `varchar(3)` | ne | Kód zdravotní pojišťovny |

### Omezení a pravidla

- index `IX_CoachSetting_Coach_Validity` nad
  `Coach_Id, ValidFrom, ValidTo`,
- CHECK constraint `ValidTo IS NULL OR ValidTo >= ValidFrom`,
- servis nepovolí časový překryv dvou nastavení stejného trenéra,
- změna údajů vytvoří nový časový záznam a uzavře předchozí interval,
- otevřený interval má `ValidTo = NULL`,
- validaci překryvů provádět v transakci.

## Fáze 4: Číselník trenérských licencí

### `hr.CoachLicenseType`

| Sloupec | SQL typ | Null | Poznámka |
|---|---|---:|---|
| `Id` | `int` | ne | PK bez identity |
| `Code` | `varchar(30)` | ne | Stabilní unikátní kód |
| `Name` | `nvarchar(100)` | ne | Zobrazovaný název |
| `IsActive` | `bit` | ne | Možnost vyřazení bez smazání |

Pevné hodnoty:

| ID | Code | Name |
|---:|---|---|
| 1 | `A` | Licence A |
| 2 | `B` | Licence B |
| 3 | `B_GOALKEEPER` | Licence B - brankář |
| 4 | `C_PLUS_YOUTH` | Licence C+ mládež |
| 5 | `C_PLAYER` | Licence C hráč |

### Jednorázový SQL skript

Vytvořit:

```text
src/DB Model/hr.CoachLicenseType.Data.sql
```

Skript:

- nebude použit jako EF Core seed ani přes `HasData`,
- poběží v explicitní transakci,
- bude idempotentní,
- vloží pevná ID pouze tehdy, pokud neexistují,
- při kolizi ID s jiným kódem skončí chybou,
- nebude přepisovat existující používané ID.

## Fáze 5: `hr.CoachLicense`

Evidence časové platnosti licence trenéra.

| Sloupec | SQL typ | Null | Poznámka |
|---|---|---:|---|
| `Id` | `int` | ne | PK |
| `Coach_Id` | `int` | ne | FK na `hr.Coach.Id` |
| `CoachLicenseType_Id` | `int` | ne | FK na číselník |
| `ValidFrom` | `date` | ne | Začátek platnosti |
| `ValidTo` | `date` | ano | Konec platnosti včetně |

### Omezení a pravidla

- index `IX_CoachLicense_Coach_Validity`,
- index `IX_CoachLicense_CoachLicenseType`,
- CHECK constraint `ValidTo IS NULL OR ValidTo >= ValidFrom`,
- jeden trenér může mít současně více různých typů licencí,
- servis nepovolí překrytí stejného typu licence u stejného trenéra,
- historické licence se fyzicky nemažou, pokud již byly platné.

## Fáze 6: `hr.CoachContract`

Smlouva je navázána na sezonu. Jeden trenér může mít v jedné sezoně několik
současně aktivních smluv.

| Sloupec | SQL typ | Null | Poznámka |
|---|---|---:|---|
| `Id` | `int` | ne | PK |
| `Coach_Id` | `int` | ne | FK na `hr.Coach.Id` |
| `Season_Id` | `int` | ne | FK na `sport.Season.Id` |
| `ContractType` | `tinyint` | ne | `1 = DPP`, `2 = OSVČ` |
| `RewardAmount` | `decimal(18,2)` | ne | Odměna v CZK |
| `IsActive` | `bit` | ne | Historie bez fyzického mazání |

### Aplikační enum

Vytvořit `ECoachContractType`:

```csharp
public enum ECoachContractType : byte
{
    Dpp = 1,
    SelfEmployed = 2,
}
```

Zobrazení hodnot lokalizovat jako `DPP` a `OSVČ`.

### Omezení a indexy

- index `IX_CoachContract_Coach_Season` nad `Coach_Id, Season_Id`,
- index na `Season_Id`,
- CHECK constraint povolující pouze typy 1 a 2,
- CHECK constraint `RewardAmount >= 0`,
- pojmenovaný DEFAULT constraint `DF_CoachContract_IsActive`,
- nevytvářet unikátní index nad trenérem a sezonou.

## Fáze 7: DbContext a navigace

Do `SportSysDbContext` přidat `DbSet` pro:

- `CoachSetting`,
- `CoachLicenseType`,
- `CoachLicense`,
- `CoachContract`.

Aktualizovat navigace:

- `User.Coach`,
- `Coach.User`,
- `Coach.Settings`,
- `Coach.Licenses`,
- `Coach.Contracts`,
- `CoachContract.Season`,
- `Season.CoachContracts`.

Pro jednoduché vztahy dodržet konvenci FK bez ručního `HasColumnName`.
Všechny potřebné indexy deklarovat explicitně.

## Fáze 8: Contract DTO

Vytvořit modely v:

```text
src/SportSys.Contract/Models/hr/
```

### Základní typy

- `UserDto`
  - `UserId`
  - `UserName`
  - `DisplayName`
  - `Email`
  - `PhoneNumber`
- `CoachDetailDto : UserDto`
  - `CoachId`
  - `PersonalNumber`
  - `BirthNumber`
  - `HasPhoto`
  - `PhotoFileName`
- `CoachListItem`
  - neobsahuje rodné číslo ani binární fotografii,
- `CoachSettingDto`,
- `CoachLicenseDto`,
- `CoachContractDto`,
- `CoachLicenseTypeSelectItem`,
- `UserSelectItem`.

Validační atributy patří na Contract DTO, nikoliv na Razor PageModel.
Databázové entity nesmí být vystaveny do Razor projektu.

## Fáze 9: `CoachService`

Vytvořit:

```text
src/SportSys.Contract/Services/CoachService.cs
```

A registrovat jej výhradně v
`SportSys.Contract/ServiceCollectionExtensions.cs`.

### Operace služby

- `GetAllAsync`:
  - hledání podle jména, e-mailu a osobního čísla,
  - volitelný filtr sezony,
  - volitelný filtr aktivní smlouvy,
  - projekce přímo do `CoachListItem`,
- `GetByIdAsync`,
- `GetAvailableUsersAsync`,
- `CreateAsync`,
- `UpdateBasicAsync`,
- `GetPhotoAsync`,
- `SetPhotoAsync`,
- `DeletePhotoAsync`,
- `CreateContractAsync`,
- `UpdateContractAsync`,
- `SetContractActiveAsync`,
- `CreateSettingAsync`,
- `UpdateSettingAsync`,
- `CreateLicenseAsync`,
- `UpdateLicenseAsync`,
- načtení seznamu sezon a typů licencí.

### Business pravidla

- Uživatel může být propojen nejvýše s jedním trenérem.
- Vytvoření trenéra pouze propojí existujícího uživatele; nebude vytvářet
  Identity heslo ani obcházet Entra/Identity provisioning.
- Aktualizace `User` a `Coach` proběhne v jedné transakci.
- Každý zápis podřízeného záznamu musí ověřit, že patří k zadanému `CoachId`.
- Intervaly nastavení a stejného typu licence se nesmí překrývat.
- Souběžné zápisy časových intervalů chránit transakcí.
- Chybové stavy neskrývat; vracet explicitní validační nebo doménovou chybu.

## Fáze 10: Area `hr`

Vytvořit strukturu:

```text
src/SportSys.Razor/Areas/hr/Pages/
├── _ViewImports.cshtml
├── _ViewStart.cshtml
└── Coach/
    ├── Index.cshtml
    ├── Index.cshtml.cs
    ├── Edit.cshtml
    ├── Edit.cshtml.cs
    ├── _Basic.cshtml
    ├── _Contracts.cshtml
    ├── _Settings.cshtml
    └── _Licenses.cshtml
```

Stránky zabezpečit politikou `SystemAdmin`. Autorizaci nastavit konzistentně
pro celou složku `Coach`, ne pouze pro jednotlivé POST handlery.

## Fáze 11: Stránka `Coach/Index`

Přehled zobrazí:

- fotografii nebo výchozí avatar,
- zobrazované jméno,
- e-mail,
- osobní číslo,
- aktuálně platné licence,
- aktivní smlouvy pro zvolenou nebo aktuální sezonu,
- akci Upravit.

Filtry:

- fulltext jméno, e-mail nebo osobní číslo,
- sezona,
- pouze trenéři s aktivní smlouvou.

Rodné číslo a binární fotografie se do seznamového dotazu neprojektují.

## Fáze 12: Stránka `Coach/Edit`

Použít existující komponentu `.tabs` a přístupný tabový skript podle
`Areas/Inventory/Pages/Items/Edit.cshtml`.

### Záložka Základní údaje

- při vytvoření výběr uživatele, který ještě není trenérem,
- `DisplayName`,
- e-mail,
- telefon,
- osobní číslo,
- rodné číslo,
- náhled fotografie,
- upload, nahrazení a odstranění fotografie.

### Záložka Smlouvy

- seznam smluv se sezonou, typem, odměnou a aktivním stavem,
- přidání nové smlouvy,
- editace existující smlouvy,
- aktivace a deaktivace,
- žádné omezení na jednu aktivní smlouvu trenéra v sezoně.

### Záložka Nastavení

- chronologický seznam intervalů,
- bankovní účet,
- adresa,
- zdravotní pojišťovna,
- přidání a editace intervalu,
- zvýraznění aktuálně platného záznamu.

### Záložka Licence

- chronologický seznam licencí,
- typ licence,
- `ValidFrom`,
- `ValidTo`,
- přidání a editace intervalu,
- zvýraznění aktuálně platných licencí.

### Formuláře a handlery

Každá záložka bude mít samostatný formulář a samostatný handler:

- `OnPostSaveBasicAsync`,
- `OnPostSavePhotoAsync`,
- `OnPostDeletePhotoAsync`,
- `OnPostSaveContractAsync`,
- `OnPostSetContractActiveAsync`,
- `OnPostSaveSettingAsync`,
- `OnPostSaveLicenseAsync`.

Samostatné formuláře zabrání tomu, aby validace skrytých záložek blokovala
uložení právě editované části. Při validační chybě se znovu otevře záložka,
ze které byl formulář odeslán.

U nového trenéra jsou záložky Smlouvy, Nastavení a Licence dostupné až po
uložení základních údajů.

## Fáze 13: Fotografie

- Formulář musí mít `enctype="multipart/form-data"`.
- Binární data se nesmí bindovat do běžného DTO při každém načtení stránky.
- Fotografii načítat samostatným GET handlerem, například
  `OnGetPhotoAsync(int id)`.
- Handler vrátí `FileContentResult` s uloženým MIME typem.
- Pokud fotografie neexistuje, UI zobrazí statický výchozí avatar.
- Odpověď může používat privátní cache hlavičky, ale nesmí být veřejně
  cachována kvůli personálním údajům.

## Fáze 14: Navigace a frontend

1. Do `Pages/Shared/_Layout.cshtml` přidat sekci:

   ```text
   Personalistika
   └── Trenéři
   ```

2. Odkaz směrovat na Area `hr`, stránku `/Coach/Index`.
3. Použít stávající SCSS komponenty `grid`, `button`, `field` a `tabs`.
4. Neupravovat ručně `wwwroot/css/site.css`.
5. Pokud budou potřeba nové styly fotografie nebo personálních přehledů,
   přidat nový SCSS partial a importovat jej ze `Styles/site.scss`.
6. Všechny ikony musí obsahovat `fa-fw`.

## Fáze 15: Migrace existujících trenérů

Protože současní trenéři nemusí mít Identity účet, je nutný řízený datový
přechod.

1. Přidat `User_Id` nejprve jako nullable.
2. Vygenerovat seznam všech stávajících trenérů a vhodných uživatelů.
3. Automaticky propojit pouze jednoznačné shody.
4. Nejednoznačné nebo chybějící vazby předat k ručnímu přiřazení.
5. Nevytvářet umělé Identity účty bez e-mailu nebo bez dohodnutého
   přihlašovacího mechanismu.
6. Doplnit `PersonalNumber` a `BirthNumber`.
7. Ověřit, že každý trenér má právě jednoho uživatele.
8. Nastavit `User_Id`, `PersonalNumber` a `BirthNumber` jako NOT NULL.
9. Přesunout `dbo.Coach` do schématu `hr` při zachování `Id`.
10. Znovu vytvořit FK z tabulek `sport.*` na `hr.Coach`.
11. Po ověření dat odstranit původní jmenné sloupce.

Tento postup může být rozdělen do dvou uživatelských migrací, pokud nebude
možné bezpečně doplnit všechna personální data v jednom nasazení.

## Fáze 16: Dokumentace

1. Aktualizovat `docs/architecture.md`:
   - přidat schéma `hr`,
   - přesunout `Coach` z `dbo` do `hr`,
   - popsat vazbu na `identity.User`.
2. Vytvořit `docs/modules/hr.md` s popisem:
   - trenérů,
   - personálního nastavení,
   - licencí,
   - smluv,
   - časové platnosti,
   - přístupových oprávnění.
3. Aktualizovat případné diagramy a odkazy, které stále uvádějí `dbo.Coach`.

## Fáze 17: Testy a ověření

### Datový model

- `Coach.UserId` je povinný a unikátní.
- Stávající `CoachId` ve sportovních tabulkách zůstávají platná.
- Nelze uložit duplicitní osobní nebo rodné číslo.
- `ValidTo` nemůže být před `ValidFrom`.
- FK indexy jsou vytvořeny explicitně.
- Smlouva přijme pouze DPP nebo OSVČ a nezápornou odměnu.

### Contract vrstva

- uživatele nelze propojit s více trenéry,
- aktualizace základních údajů je transakční,
- nastavení jednoho trenéra se časově nepřekrývají,
- stejný typ licence jednoho trenéra se časově nepřekrývá,
- různé typy licencí se mohou překrývat,
- jeden trenér může mít několik aktivních smluv pro jednu sezonu,
- deaktivace smlouvy zachová historický záznam,
- cizí podřízený záznam nelze upravit přes jiné `CoachId`.

### Razor Pages

- uživatel bez politiky `SystemAdmin` nemá do Area `hr` přístup,
- Index filtruje a otevírá správného trenéra,
- každá záložka se ukládá nezávisle,
- validační chyba znovu otevře správnou záložku,
- fotografie odmítne nadlimitní nebo nepodporovaný soubor,
- Razor projekt nepoužívá `SportSys.Database` přímo.

## Akceptační kritéria

- Existuje schéma `hr` a všechny navržené EF entity.
- Stávající trenérské vazby ve sportovním modulu jsou zachovány.
- Trenér je povinně propojen právě s jedním `identity.User`.
- Aplikační detail trenéra dědí ze společného uživatelského DTO.
- Licence jsou naplněny samostatným jednorázovým SQL skriptem, nikoliv seedem.
- Časové intervaly nastavení a stejných licencí se nepřekrývají.
- Je možné evidovat několik aktivních smluv trenéra pro jednu sezonu.
- `Coach/Index` a záložkový `Coach/Edit` fungují výhradně přes Contract servis.
- Personální Area je dostupná pouze administrátorům.
- Nebyla vytvořena ani upravena EF Core migrace.
