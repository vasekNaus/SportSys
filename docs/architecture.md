# Architektura systému

## Účel systému

SportSys je interní informační systém hokejového klubu. Aktivní aplikace
centralizuje sportovní agendu, personalistiku trenérů, archiv docházky a
skladové hospodářství. Nenahrazuje rezervační ani účetní systém; integrace s
nimi musí být explicitně navržena a nesmí umožnit zápis do externích dat.

## Architektonické vrstvy

```text
SportSys.Razor
      |
      v
SportSys.Contract ---> SportSys.Model
      |
      v
SportSys.Database ---> SQL Server
```

| Vrstva | Projekt nebo složka | Role |
|---|---|---|
| Prezentace | `src/SportSys.Razor/` | Razor Pages, HTTP handlery, mapování vstupu a výstupu |
| Aplikace | `src/SportSys.Contract/` | Business pravidla, validace, DTO, autorizační helpery a DI |
| Data | `src/SportSys.Database/` | EF Core entity, DbContext, mapování a migrační historie |
| Sdílené modely | `src/SportSys.Model/` | Typy sdílené napříč vrstvami |
| Dávkové operace | `src/SportSys.ConsoleApp/` | Importy a jednorázové zpracování |
| Sdílené knihovny | `src/External/Apollo/` | `IdConvention`, DB inicializace a HTTP utility |

`SportSys.Razor` smí referencovat pouze `SportSys.Contract`. Contract
zapouzdřuje databázové entity a do prezentace vrací DTO nebo modely. Toto
pravidlo brání úniku EF entit do UI a udržuje business logiku testovatelnou.

### Izolovaný prototyp

`src/SportSys.Web/` je trackovaný scaffoldovaný Blazor/Identity prototyp s
vlastním `ApplicationDbContext` a migrací. Není součástí aktivního datového ani
autentizačního toku SportSys a nesmí sloužit jako implementační vzor pro
`SportSys.Razor`. Mění se pouze na explicitní zadání.

## Databázová schémata

| Schéma | Odpovědnost |
|---|---|
| `dbo` | Sdílené entity a číselníky mimo doménová schémata |
| `sport` | Tréninky, zápasy, plány, požadavky a sportovní číselníky |
| `identity` | ASP.NET Core Identity bez prefixu `AspNet` |
| `inventory` | Majetek, výstroj, pohyby, zápůjčky a inventury |
| `hr` | Trenéři, personální nastavení, licence, smlouvy a docházka |
| `plan` | Rezervováno pro read-only integraci s externím rezervačním systémem |

Aktuální `SportSysDbContext` nemá modely `plan.*`; jde o integrační hranici,
nikoli o implementovanou zápisovou část systému.

## Klíčové datové struktury

### Sportovní události

`Training` a `Match` používají TPC a sdílenou sekvenci
`sport.SportEventSeq`. ID je unikátní napříč oběma tabulkami.
`DurationMinutes` je persisted computed sloupec a nepočítá se v C#.

```text
sport.SportEventSeq
  +-- sport.Training
  +-- sport.Match
```

`TrainingGroup` sdružuje konkrétní tréninky, zatímco `TrainingPlanGroup`
sdružuje obecné plány. Shodné `GroupId` mezi těmito tabulkami nevyjadřuje
vztah.

### Personalistika

```text
identity.User
  +-- hr.Coach (TPT, sdílený PK)
        |
  +-- hr.CoachSetting
  +-- hr.CoachLicense --> hr.CoachLicenseType
  +-- hr.CoachContract --> sport.Season
  +-- hr.CoachAttendance --> identity.User (uživatel uploadu)

sport.CoachTraining
sport.CoachTrainingPlan
sport.CoachTrainingRequirement --> hr.Coach.Id
```

`Coach : User` používá TPT. `hr.Coach.Id` je PK i FK na
`identity.User.Id`; jméno, e-mail a telefon jsou uloženy pouze v základní
Identity tabulce. Trenérská tabulka obsahuje osobní číslo, rodné číslo a
fotografii. Stejné ID používají všechny HR a Sport vazby. Docházka je unikátní
pro trenéra, rok a měsíc.

TPC zůstává preferovanou strategií běžných doménových hierarchií. TPT nad
Identity je zdokumentovaná výjimka, protože trenér musí sdílet fyzický
uživatelský řádek a současně být cílem databázových FK.

### Sklad

`InventoryItem` je abstraktní TPC základ pro `Equipment` a `Asset`. Sdílená
sekvence `inventory.InventoryItemSeq` zajišťuje unikátní ID. Vazby z
`Loan`, `InventoryTransaction`, `InventoryItemPurchase`,
`ItemLocationHistory` a `InventoryCheck` na abstraktní položku nelze v SQL
vyjádřit jedním FK; integritu proto vynucuje Contract vrstva.

Podrobnosti jsou v `docs/modules/inventory.md`.

## Integrace s externími systémy

| Integrace | Stav | Hranice |
|---|---|---|
| Microsoft Entra ID | Aktivní | OIDC přihlášení, provisioning do `identity.User` |
| Lokální ASP.NET Core Identity | Aktivní fallback | Stejný user store, vlastní cookie schémata |
| SQL Server | Aktivní | Vlastní schémata SportSys |
| Rezervační systém `plan.*` | Produktový požadavek | Výhradně read-only; modely nejsou v aktuálním DbContext |
| Excel/XLSX | Aktivní | Export rozvrhu a archivace docházky; importy v ConsoleApp |

## Architektonická omezení

- Hranice `Razor -> Contract -> Database` a jediný composition root jsou
  závazné podle `docs/decisions/adr-001-vrstvy-a-composition-root.md`.
- Autentizace sdílí Entra OIDC a lokální Identity store podle
  `docs/decisions/adr-002-hybridni-identita.md`.
- TPC hierarchie a aplikační integrita abstraktních FK se řídí
  `docs/decisions/adr-003-tpc-se-sdilenymi-sekvencemi.md`.
- TPT hierarchie `User -> Coach` je výjimka popsaná v
  `docs/decisions/adr-004-coach-jako-tpt-potomek-user.md`.
- Detailní implementační zákazy pro EF Core a frontend jsou pouze v
  `.github/copilot-instructions.md` a `docs/conventions.md`.

## Klíčové soubory

| Logický celek | Cesta |
|---|---|
| Composition root aplikačních služeb | `src/SportSys.Contract/ServiceCollectionExtensions.cs` |
| HTTP pipeline aktivní aplikace | `src/SportSys.Razor/Program.cs` |
| DbContext | `src/SportSys.Database/Context/SportSysDbContext.cs` |
| Konstanty schémat | `src/SportSys.Database/Models/Schemas.cs` |
| EF Core entity | `src/SportSys.Database/Models/{schema}/` |
| EF Core konfigurace | `src/SportSys.Database/Configurations/{schema}/` |
| Razor Areas | `src/SportSys.Razor/Areas/` |
| SCSS | `src/SportSys.Razor/Styles/` |
| Testy | `tests/SportSys.Razor.Tests/` |

## Odkazovaná dokumentace

- `docs/conventions.md`
- `docs/modules/auth.md`
- `docs/modules/frontend.md`
- `docs/modules/hr.md`
- `docs/modules/inventory.md`
- `docs/modules/sport.md`
- `docs/decisions/README.md`
