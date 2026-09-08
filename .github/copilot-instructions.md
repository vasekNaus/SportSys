# Copilot Instructions – SportSys

## Agentic communication

- Default to code and direct edits over prose.
- No progress narration ("Now I will...", "Let me..."). Just do the action.
- No recaps or summaries of completed work unless requested.
- When prose is needed, use short bullets, not paragraphs.

**Jazykové nastavení:** komunikace česky (cs-cz) · zdrojový kód anglicky (en-us) · komentáře česky

SportSys je informační systém pro hokejový klub (.NET 10) — nadstavba nad rezervačním systémem sportoviště. Zajišťuje kontrolu fakturace ledového času, evidenci tréninků a zápasů a automatizaci plateb.

## Build & spuštění

```bash
dotnet build SportSys.slnx
dotnet run --project src/SportSys.Razor
dotnet run --project src/SportSys.ConsoleApp

dotnet ef migrations list --project src/SportSys.Database
```

```bash
cd src/SportSys.Razor
npm run build:css    # SCSS → wwwroot/css/site.css
npm run watch:css
```

Connection stringy nejsou commitovány — nastavit přes user secrets:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=SportSys;..." --project src/SportSys.Razor
```

## Architektura projektu

Solution: formát `.slnx`. Závislost: **Razor → Contract → Database → SQL Server**

| Projekt | Typ | Role |
|---|---|---|
| `SportSys.Database` | Class Library | EF Core modely, DbContext, migrace |
| `SportSys.Model` | Class Library | Doménové objekty a DTO sdílené napříč vrstvami |
| `SportSys.Contract` | Class Library | Aplikační servisy; závisí na Database, vrací Model objekty |
| `SportSys.Razor` | ASP.NET Core Web App | Razor Pages; závisí **výhradně** na Contract |
| `SportSys.ConsoleApp` | Console App | Import dat z Excelu do SQL Serveru |
| `src/Apollo/` | Git submodul | Sdílená knihovna (IdConvention, InitDatetime2, HttpService…) |

## Klíčová pravidla

**Architektura:**
- ❌ `SportSys.Razor` nesmí referencovat `SportSys.Database` — vše jde přes Contract servisy; porušení narušuje izolaci vrstev a znemožňuje testování.
- ❌ `DbContext`, Identity ani auth nesmí být registrovány v Razor projektu — jediné místo je `AddSportSysServices()` v `SportSys.Contract/ServiceCollectionExtensions.cs`.

**EF Core:**
- ❌ Nikdy nevytvářet EF Core migrace — agent upraví modely a konfigurace, ale vytvoření a aplikaci migrace provádí výhradně uživatel.
- ❌ Nikdy nepřidávat `HasColumnName` pro běžné FK — Apollo `IdConvention()` je pojmenuje automaticky; ruční přepis způsobí konflikty migrace.
- ❌ Každý model musí mít `[Table(nameof(X), Schema = Schemas.Y)]` — `TableNameFromDbSetConvention` je odstraněna, bez atributu EF Core tabulku nenajde.
- ❌ Indexy na FK nevznikají automaticky — `ForeignKeyIndexConvention` je odstraněna; přidávej indexy výhradně explicitně přes `[Index]`.
- ❌ `DurationMinutes` nepočítat v C# — jde o persisted computed column v DB.
- ❌ Nikdy zapisovat do tabulek schématu `plan.*` — read-only přístup k externímu rezervačnímu systému.
- Data atributy mají přednost před Fluent API. Viz `docs/conventions.md`.

**Autentizace:**
- ❌ `AddIdentity<T>()` nesmí být použito — nahrazuje OIDC jako výchozí schéma a přeruší Entra ID login; vždy `AddIdentityCore<User>()` + `.AddSignInManager()`.
- ❌ Po scaffoldingu Identity stránek okamžitě smazat 3 vložené řádky z `Program.cs` — jinak app spadne s `"Scheme already exists"`. Viz `.github/skills/identity-scaffold-cleanup/SKILL.md`.

**Frontend:**
- ❌ `wwwroot/css/site.css` nikdy neupravovat ručně — je kompilovaný výstup z `Styles/*.scss`.

## Kontext pro konkrétní úkoly

| Úkol zahrnuje... | Přečti |
|---|---|
| EF Core modely, konvence, migrace | `docs/conventions.md` |
| Architektura vrstev, DB schémata | `docs/architecture.md` |
| Modul Inventory | `docs/inventory.md` |
| Modul Sport, sportovní číselníky | `docs/modules/sport.md` |
| Autentizace, autorizace, Identity | `docs/modules/auth.md` |
| Frontend, SCSS, ikony | `docs/modules/frontend.md`, `.github/skills/barevna-schemata/hc-klatovy/` |
| Přidání lookup tabulky | `.github/skills/lookup-table/SKILL.md` |
| Přidání EF Core entity | `.github/skills/new-ef-entity/SKILL.md` |
| Scaffolding Identity stránek | `.github/skills/identity-scaffold-cleanup/SKILL.md` |
| Optimalizace MD dokumentace | `.github/skills/optimalizace-instrukci/SKILL.md` |
| Implementační plán z GitHub issue | `.github/skills/github-issue-implementation-plan/SKILL.md` |