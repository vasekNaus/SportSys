# SportSys

Interní informační systém hokejového klubu pro evidenci sportovních událostí,
personalistiku trenérů, archivaci docházky a skladové hospodářství. Aktivní
aplikace používá ASP.NET Core Razor Pages, .NET 10, EF Core 10 a SQL Server.

## Projekty

| Projekt | Účel |
|---|---|
| `src/SportSys.Razor` | Aktivní webová aplikace |
| `src/SportSys.Contract` | Aplikační služby, validace a DTO |
| `src/SportSys.Database` | EF Core model a DbContext |
| `src/SportSys.Model` | Sdílené modely |
| `src/SportSys.ConsoleApp` | Dávkové importy a pomocné příkazy |
| `src/SportSys.Web` | Izolovaný scaffoldovaný prototyp; není aktivní aplikací |

## Předpoklady

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Microsoft SQL Server 2019 nebo novější
- Node.js a npm pro kompilaci SCSS
- EF Core CLI pro uživatelskou správu migrací

## Rychlý start

Connection string nepatří do verzovaného `appsettings.json`. Nastavte jej přes
user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>" --project src\SportSys.Razor
dotnet build SportSys.slnx
dotnet run --project src\SportSys.Razor
```

Migrace vytváří, kontroluje a aplikuje výhradně vývojář:

```powershell
dotnet ef migrations list --project src\SportSys.Database
dotnet ef database update --project src\SportSys.Database
```

## Dokumentace

| Dokument | Obsah |
|---|---|
| [Architektura](docs/architecture.md) | Vrstvy, závislosti, schémata a integrace |
| [Konvence](docs/conventions.md) | Projektová pravidla pro EF Core a frontend |
| [Sport](docs/modules/sport.md) | Tréninky, plány, požadavky a číselníky |
| [Personalistika](docs/modules/hr.md) | Trenéři, smlouvy, licence a docházka |
| [Sklad](docs/modules/inventory.md) | Majetek, výstroj, zápůjčky a inventury |
| [Autentizace](docs/modules/auth.md) | Entra ID, Identity a autorizace |
| [Frontend](docs/modules/frontend.md) | Razor, SCSS, tokeny a komponenty |
| [ADR](docs/decisions/README.md) | Přijatá architektonická rozhodnutí |
| [Funkce](docs/features.md) | Stručný produktový přehled aktuálních funkcí |
| [Scénáře](docs/use-cases.md) | Typické uživatelské toky |
| [Instrukce pro AI](.github/copilot-instructions.md) | Pravidla a navigace pro agenty |

Produktový kontext je v [přehledovém dokumentu](docs/overview.md). Historické
implementační plány jsou v `.github/tasks/` a výzkumné podklady v
`docs/research/`; nejsou zdrojem aktuálního stavu.

## Licence

Interní projekt - není určen k veřejnému šíření.
