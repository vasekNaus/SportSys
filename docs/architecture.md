# Architektura systému

## Vrstvová architektura

```
┌─────────────────────────────────────────────────────┐
│  SportSys.Razor  (Razor Pages)                      │
│  – UI, závisí VÝHRADNĚ na SportSys.Contract         │
└──────────────────────┬──────────────────────────────┘
                       │ závisí na
┌──────────────────────▼──────────────────────────────┐
│  SportSys.Contract                                  │
│  – aplikační servisy, business logika               │
│  – mapování DB entit → SportSys.Model               │
└──────────┬───────────────────────────┬──────────────┘
           │ závisí na                 │ vrací
┌──────────▼──────────┐   ┌───────────▼──────────────┐
│  SportSys.Database  │   │  SportSys.Model           │
│  – EF Core modely   │   │  – doménové objekty / DTO │
│  – SportSysDbContext│   │  – sdílené napříč vrstvami│
│  – migrace          │   └──────────────────────────┘
└──────────┬──────────┘
           │ MSSQL
┌──────────▼──────────────────────────────────────────┐
│  SQL Server                                         │
│  – databáze SportSys                                │
│  – databáze externího rezervačního systému          │
└─────────────────────────────────────────────────────┘
```

> ❌ `SportSys.Razor` nesmí referencovat `SportSys.Database` — vše přes servisy v Contract.

## Projekty

| Projekt | Typ | Role |
|---|---|---|
| `SportSys.Database` | Class Library | EF Core modely, `SportSysDbContext`, migrace |
| `SportSys.Model` | Class Library | Doménové objekty a DTO (vrácené servisy) |
| `SportSys.Contract` | Class Library | Aplikační servisy; závisí na Database, vrací Model |
| `SportSys.Razor` | ASP.NET Core Web App | Razor Pages; závisí výhradně na Contract |
| `SportSys.ConsoleApp` | Console App | Import dat z Excelu do DB |
| `src/Apollo/` | Git submodul | Sdílená knihovna (IdConvention, InitDatetime2, HttpService) |

## DB schémata

| Schéma | Obsah |
|---|---|
| `dbo` | Zbývající sdílené entity mimo doménová schémata |
| `sport` | Tréninky, zápasy, SportEvent sekvence, lookup tabulky sport modulu |
| `identity` | ASP.NET Core Identity (User, Role, UserRole…) bez AspNet prefixu |
| `inventory` | Skladové hospodářství (Equipment, Asset, Loan, InventorySession…) |
| `hr` | Trenéři, personální nastavení, licence a smlouvy |
| `plan` | **Read-only** — modely externího rezervačního systému (Block, Task) |

## Datový model — personalistika trenérů

`hr.Coach` zachovává původní číselný primární klíč používaný sportovními
tabulkami, ale identitu osoby přebírá z povinné vazby 1:1 na
`identity.User`. Uživatelský účet proto může být propojen nejvýše s jedním
trenérem.

```
identity.User
      │ 1:0..1
      ▼
hr.Coach
  ├── hr.CoachSetting       (časově platné personální a platební údaje)
  ├── hr.CoachLicense       (časově platná licence + hr.CoachLicenseType)
  ├── hr.CoachContract      (smlouva pro sport.Season)
  └── hr.CoachAttendance    (měsíční zdrojový XLSX dokument)

sport.CoachTraining
sport.CoachTrainingRequirement ──► hr.Coach.Id
sport.CoachTrainingPlan
```

Rodné číslo a fotografie jsou personální údaje. Nejsou součástí seznamových
projekcí a fotografie se načítá samostatným autorizovaným endpointem.

## Datový model — SportEvent (TPC)

`Training` a `Match` jsou konkrétní tabulky sdílející sekvenci `sport.SportEventSeq` — ID jsou unikátní napříč oběma entitami.

```
sport.SportEventSeq (SEQUENCE)
       ├── sport.Training  (Season_Id, IceRink_Id, TrainingType_Id, …, DurationMinutes*)
       └── sport.Match     (Season_Id, IceRink_Id, Opponent_Id, …, DurationMinutes*)

VIEW sport.SportEvent → UNION ALL Training + Match (sloupec EventType)

* DurationMinutes = persisted computed column DATEDIFF(minute, TimeFrom, TimeTo)
  → nikdy nepočítat v C# kódu
```

## Datový model — Inventory (TPC)

```
inventory.InventoryItemSeq (SEQUENCE)
       ├── inventory.Equipment  (výstroj: Size, …)
       └── inventory.Asset      (majetek: SerialNumber, WarrantyUntil, …)

Sdílené entity v dbo: Manufacturer, Location (AssignedLocation + CurrentLocation)
TPC omezení: Loan, InventoryTransaction, InventoryItemPurchase, ItemLocationHistory,
             InventoryCheck nemají DB-level FK constraint na InventoryItemId
             → integrita vynucována v Contract servisech
```

Podrobnosti: `docs/inventory.md`

## Integrační modely — ext. rezervační systém

Modely v `SportSys.Database/Models/Emr/`, namespace `Emr`, schéma `plan`:

| Model | Tabulka | Popis |
|---|---|---|
| `Block` | `plan.Block` | Blok rezervovaného ledového času |
| `Task` | `plan.Task` | Konkrétní rezervace v rámci bloku |

> ❌ Do tabulek `plan.*` se nikdy nezapisuje.

## Klíčové soubory

| Logický celek | Cesta |
|---|---|
| Registrace servisů | `src/SportSys.Contract/ServiceCollectionExtensions.cs` |
| DbContext | `src/SportSys.Database/Context/SportSysDbContext.cs` |
| DB schémata (konstanty) | `src/SportSys.Database/Models/Schemas.cs` |
| EF Core modely | `src/SportSys.Database/Models/{dbo\|sport\|identity\|inventory}/` |
| EF Core konfigurace | `src/SportSys.Database/Configurations/{schema}/` |
| Migrace | `src/SportSys.Database/Migrations/` |
| Razor Pages | `src/SportSys.Razor/Pages/` |
| SCSS styly | `src/SportSys.Razor/Styles/` |

## Architektonická omezení

| Pravidlo | Důvod |
|---|---|
| Razor → Contract (nikoli Database) | Izolace vrstev, testovatelnost |
| Registrace jen v `AddSportSysServices()` | Zabrání duplikacím a kolizím schémat |
| `AddIdentityCore` (ne `AddIdentity`) | `AddIdentity` přebije OIDC schéma → Entra ID login selže |
| Žádné automatické FK indexy | `ForeignKeyIndexConvention` odstraněna — indexy přidávat explicitně |
| `[Table]` na každém modelu | `TableNameFromDbSetConvention` odstraněna — bez atributu EF tabulku nenajde |

## Reference

- `docs/conventions.md` — EF Core konvence, SCSS, ikony
- `docs/modules/auth.md` — autentizace a autorizace
- `docs/modules/frontend.md` — SCSS struktura, barevné schéma
- `docs/inventory.md` — modul skladového hospodářství
- `docs/modules/hr.md` — personalistika trenérů