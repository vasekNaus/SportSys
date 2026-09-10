# Modul Personalistika

## Účel

Modul `hr` spravuje trenéry, jejich časově platná personální nastavení,
licence, smlouvy a měsíční archiv docházky. Razor Area `hr` používá výhradně
Contract služby a DTO.

## Odpovědnosti

- Seznam a editace trenérů.
- Evidence bankovního spojení, adresy a zdravotní pojišťovny v čase.
- Evidence trenérských licencí a smluv pro sezóny.
- Bezpečné uložení fotografie trenéra.
- Upload, filtrování a stažení zdrojových XLSX souborů docházky.
- Audit uživatele, který docházku nahrál.

## Datový model

```text
identity.User
  +-- hr.Coach (TPT, PK = FK)
        +-- hr.CoachSetting
        +-- hr.CoachLicense --> hr.CoachLicenseType
        +-- hr.CoachContract --> sport.Season
        +-- hr.CoachAttendance --> identity.User

sport.CoachTraining
sport.CoachTrainingPlan
sport.CoachTrainingRequirement --> hr.Coach.Id
```

`Coach : User` používá TPT. `Coach.Id` je zděděné `User.Id` a v databázi je
současně PK/FK `hr.Coach -> identity.User`. Jméno, e-mail, telefon a
přihlašovací údaje jsou uloženy pouze v `identity.User`; osobní číslo, rodné
číslo a fotografie jsou v `hr.Coach`.

Při založení trenéra administrátor vybírá existujícího uživatele. Contract
služba v transakci aktualizuje společná profilová pole a vloží pouze derived
řádek `hr.Coach`. Stejného uživatele nelze povýšit podruhé.

### Personální nastavení

`CoachSetting` používá interval včetně obou krajních dat. Otevřený interval má
`ValidTo = NULL`. Intervaly jednoho trenéra se nesmějí překrývat; nový
chronologicky navazující záznam uzavře předchozí otevřený interval.

### Licence

`CoachLicense` spojuje trenéra s `CoachLicenseType`. Intervaly stejného typu
licence u jednoho trenéra se nesmějí překrývat. Typy licencí se zavádějí
idempotentním skriptem `src/DB Model/hr.CoachLicenseType.Data.sql`.

### Smlouvy

`CoachContract` patří trenérovi a sezoně. Typ smlouvy je DPP nebo OSVČ,
odměna je nezáporná a historie se zachovává přes `IsActive`.

### Docházka

`CoachAttendance` uchovává jeden původní XLSX soubor pro kombinaci trenéra,
roku a měsíce. Obsahuje UTC čas uploadu a povinný odkaz na
`identity.User`, který upload provedl.

## Tok zpracování

### Editace trenéra

1. Při založení PageModel vybere existujícího uživatele přes
   `SelectedUserId`.
2. `CoachService` vytvoří TPT profil se stejným ID.
3. Při editaci PageModel načte zděděné DTO přes `CoachService`.
4. Každá záložka odešle samostatný POST handler.
5. Contract služba normalizuje a validuje data.
6. Uloží změnu a vrátí doménovou validační chybu při konfliktu.
7. Fotografie se načítá samostatným autorizovaným handlerem.

### Upload docházky

1. PageModel získá lokální user ID přes `CurrentUserIdResolver`.
2. Contract validátor zkontroluje název, příponu, velikost, MIME typ a
   strukturu Open XML balíčku.
3. Služba ověří trenéra a unikátní období.
4. Soubor uloží jako binární obsah s auditem.
5. Seznamové dotazy binární obsah neprojektují; načítá se až při stažení.

## Klíčové komponenty

| Komponenta | Cesta |
|---|---|
| Coach služba | `src/SportSys.Contract/Services/CoachService.cs` |
| Attendance služba | `src/SportSys.Contract/Services/CoachAttendanceService.cs` |
| XLSX validátor | `src/SportSys.Contract/Services/CoachAttendanceXlsxValidator.cs` |
| Coach stránky | `src/SportSys.Razor/Areas/hr/Pages/Coach/` |
| Attendance stránka | `src/SportSys.Razor/Areas/hr/Pages/Attendance/` |
| HR entity | `src/SportSys.Database/Models/hr/` |
| HR styly | `src/SportSys.Razor/Styles/_hr.scss` |

## Rozhraní

| Route | Funkce |
|---|---|
| `/hr/Coach/Index` | Hledání a filtrování trenérů |
| `/hr/Coach/Edit` | Základní údaje, fotografie, smlouvy, nastavení a licence |
| `/hr/Attendance/Index` | Upload, historie, filtry a stažení docházky |

## Integrační vazby

- `sport.*` odkazuje na stabilní `Coach.Id`.
- `CoachContract` používá `sport.Season`.
- `CoachAttendance.UserUploadId` používá lokální `identity.User.Id`.
- Entra OID se nikdy neukládá do auditního FK.
- Entra provisioning vytváří `User`; trenérský TPT řádek vzniká pouze přes
  personální administraci.

## Závislosti

Razor Area závisí jen na `SportSys.Contract`. EF Core entity ani DbContext
nesmí být použity v PageModelech.

## Omezení a pravidla

- HR Area aktuálně nemá konvenci `SystemAdmin`; v `Program.cs` je dočasně
  zakomentovaná. Fallback policy stále vyžaduje přihlášení.
- Toto dočasné uvolnění autorizace se nesmí bez explicitního zadání obnovit
  ani vydávat za cílový bezpečnostní stav.
- Fotografie může být JPEG, PNG nebo WebP a má limit 5 MiB.
- Docházka může být pouze `.xlsx` do 10 MiB.
- Stažení docházky používá privátní `no-store` cache.
- Archivace docházky nečte buňky a nezapisuje do `sport.Training`.
- Agent nevytváří EF Core migraci.
- TPC je výchozí strategie projektu; TPT `User -> Coach` je výjimka podle
  ADR-004.

## Příklady

Pro trenéra `42` a období `2026-09` lze uložit nejvýše jeden záznam
`CoachAttendance`. Další upload stejné kombinace musí skončit validační
chybou, nikoli přepsáním původního souboru.

## Odkazovaná dokumentace

- `docs/architecture.md`
- `docs/conventions.md`
- `docs/modules/auth.md`
- `docs/decisions/adr-004-coach-jako-tpt-potomek-user.md`
- `.github/tasks/hr-coach-administration.md` - historický implementační plán
- `.github/tasks/8-evidence-dochazky-treneru.md` - historický implementační plán
