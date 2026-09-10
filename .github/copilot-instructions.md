# Copilot Instructions - SportSys

## Jazyk komunikace

- Komunikuj česky (`cs-CZ`).
- Zdrojový kód, názvy typů a členů piš anglicky.
- Komentáře v kódu piš česky a jen tam, kde kód není samovysvětlující.

## Přehled projektu

SportSys je interní informační systém hokejového klubu postavený na .NET 10,
ASP.NET Core Razor Pages a EF Core se SQL Serverem. Aktivní webová aplikace je
`SportSys.Razor`; spravuje sportovní agendu, personalistiku trenérů, docházku
a sklad. `SportSys.Web` je izolovaný scaffoldovaný prototyp a není součástí
produkční architektury.

## Architektura na jednu obrazovku

`SportSys.Razor -> SportSys.Contract -> SportSys.Database -> SQL Server`

| Projekt | Odpovědnost |
|---|---|
| `SportSys.Razor` | Razor Pages, prezentace, HTTP handlery |
| `SportSys.Contract` | Aplikační služby, validace, DTO a registrace DI |
| `SportSys.Database` | EF Core entity, `SportSysDbContext`, konfigurace |
| `SportSys.Model` | Sdílené doménové a prezentační modely |
| `SportSys.ConsoleApp` | Dávkové importy a pomocné CLI operace |
| `src/External/Apollo/` | Sdílené databázové a HTTP utility |

Podrobnosti a výjimky jsou v `docs/architecture.md` a ADR v
`docs/decisions/`.

## Klíčová pravidla

### Architektura

- **NIKDY** nepřidávej referenci z `SportSys.Razor` na `SportSys.Database`;
  přístup přes Contract zachovává izolaci vrstev a testovatelnost.
- **NIKDY** neregistruj DbContext, Identity ani business služby v Razor
  projektu; jediný composition root je `AddSportSysServices()` v
  `src/SportSys.Contract/ServiceCollectionExtensions.cs`.
- `SportSys.Web` neměň bez explicitního zadání; má vlastní scaffoldovanou
  Identity a nesmí být použit jako vzor pro aktivní aplikaci.

### EF Core

- **NIKDY** nevytvářej ani neupravuj EF Core migrace nebo model snapshot;
  agent mění model a konfiguraci, migraci vytváří a aplikuje uživatel.
- Každá entita musí mít `[Table(nameof(X), Schema = Schemas.Y)]`, protože
  `TableNameFromDbSetConvention` je odstraněna.
- FK indexy deklaruj explicitně pomocí `[Index]`, protože
  `ForeignKeyIndexConvention` je odstraněna.
- Pro běžné FK nepoužívej `HasColumnName`; pojmenování zajišťuje Apollo
  `IdConvention()`.
- Upřednostni data atributy. Fluent API použij jen pro konfiguraci, kterou
  atributy neumí. Podrobnosti jsou v `docs/conventions.md`.
- Pro dědičnost preferuj TPC. TPT `User -> Coach` je explicitní výjimka,
  protože trenér sdílí fyzický Identity řádek; viz ADR-004.
- `DurationMinutes` nenastavuj v C#; je to persisted computed sloupec.
- Do externích tabulek `plan.*` se nesmí zapisovat; jejich modely zatím nejsou
  v aktuálním `SportSysDbContext`.

### Autentizace a frontend

- Používej `AddIdentityCore<User>()` a `.AddSignInManager()`, nikoli
  `AddIdentity<T>()`; jinak se přepíše výchozí OIDC schéma pro Entra ID.
- Po Identity scaffoldingu proveď cleanup podle
  `.github/skills/identity-scaffold-cleanup/SKILL.md`.
- `src/SportSys.Razor/wwwroot/css/site.css` neupravuj ručně; mění se pouze
  SCSS v `src/SportSys.Razor/Styles/`.

## Build, testy a spuštění

```powershell
dotnet build SportSys.slnx
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
dotnet run --project src\SportSys.Razor
dotnet run --project src\SportSys.ConsoleApp
dotnet ef migrations list --project src\SportSys.Database
```

```powershell
Set-Location src\SportSys.Razor
npm run build:css
npm run watch:css
```

Connection string nastavuj přes user secrets, nikdy do verzované konfigurace:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>" --project src\SportSys.Razor
```

## Kontext pro konkrétní úkoly

| Úkol zahrnuje | Přečti |
|---|---|
| Vrstvy, projekty, DB schémata | `docs/architecture.md`, `docs/decisions/` |
| EF Core modely a konvence | `docs/conventions.md` |
| Sport a tréninky | `docs/modules/sport.md` |
| Personalistika a docházka | `docs/modules/hr.md` |
| Sklad | `docs/modules/inventory.md` |
| Identity a autorizace | `docs/modules/auth.md` |
| SCSS, komponenty, ikony | `docs/modules/frontend.md`, `.github/skills/barevna-schemata/SKILL.md` |
| Nová EF entita | `.github/skills/new-ef-entity/SKILL.md` |
| Nový číselník | `.github/skills/lookup-table/SKILL.md` |
| Pojmenovaný DEFAULT constraint | `.github/skills/has-default-value/SKILL.md` |
| Admin formulář | `.github/skills/editor-template/SKILL.md` |
| Plán z GitHub issue | `.github/skills/github-issue-implementation-plan/SKILL.md` |
| Implementace task plánu | `.github/skills/task-plan-implementation/SKILL.md` |

## Zákazy a alternativy

| Zákaz | Správná alternativa |
|---|---|
| Přímý přístup Razor -> Database | Contract služba a DTO |
| Ruční editace generovaného CSS | Úprava SCSS a `npm run build:css` |
| Tiché ignorování neplatných dat | Validační chyba podle vzoru modulu |
| Domýšlení budoucích funkcí z research/task dokumentu | Ověření v kódu a kanonických `docs/` |
| Mazání nebo přepis existujícího ADR | Nové ADR s `Supersedes` |

## Kdy se zastavit a zeptat

Zeptej se pouze tehdy, když rozhodnutí mění veřejné chování, bezpečnost,
nevratné zacházení s daty nebo rozsah migrace a nelze je odvodit z issue,
nových komentářů, ADR ani existujícího kódu. Zastav se také při konfliktu s
neočekávanou uživatelskou změnou ve stejném souboru. Bez dotazu nikdy
neaplikuj migraci, nemaž produkční data a neobnovuj dočasně vypnutou
autorizaci.

## Zdroje pravdy

1. Aktuální kód, schéma a konfigurační soubory.
2. `docs/decisions/` pro přijatá architektonická rozhodnutí.
3. `docs/architecture.md`, `docs/conventions.md` a `docs/modules/`.
4. `.github/copilot-instructions.md` pro globální omezení.
5. `.github/skills/` pro pracovní postupy.

Soubory v `.github/tasks/` a `docs/research/` jsou historické plány a podklady;
nepřepisují aktuální implementaci ani ADR.
