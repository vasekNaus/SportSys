# Instrukce pro založení a rozvoj webové aplikace (ASP.NET Core Razor Pages + EF Core + SQL Server)

Obecný, na konkrétním projektu nezávislý návod pro AI agenta. Použij jej jako
výchozí `.github/copilot-instructions.md` (nebo `AGENTS.md`) nového repozitáře
a nahraď zástupné symboly:

| Zástupný symbol | Význam | Příklad |
|---|---|---|
| `{App}` | Prefix názvů projektů a namespace | `Acme` |
| `{module}` | Doménový modul = DB schéma = složka/Area | `inventory` |
| `{Entity}` | Název entity / agregátu | `Product` |

Pokud pravidlo konfliktuje s explicitním zadáním uživatele nebo ADR v
repozitáři, má přednost zadání/ADR.

---

## 1. Jazyk a styl práce

- Komunikuj s uživatelem v jeho jazyce (výchozí: čeština, `cs-CZ`).
- Zdrojový kód, názvy typů, členů, souborů a DB objektů piš anglicky.
- Komentáře piš jen tam, kde kód není samovysvětlující.
- Dělej malé, cílené změny; neopravuj nesouvisející problémy.
- Před implementací ověř existující vzor v kódu a podle něj pokračuj, nevymýšlej nový.
- Ptej se jen na rozhodnutí, která mění veřejné chování, bezpečnost, nevratné
  zacházení s daty nebo rozsah migrace a nelze je odvodit z kódu.
- **Nikdy** nevytvářej ani neaplikuj EF Core migrace, nemaž data a nevypínej autorizaci.
  Změny modelu připrav v kódu; migraci vytváří a aplikuje uživatel.

## 2. Technologický stack

| Oblast | Volba |
|---|---|
| Platforma | .NET (aktuální LTS), `Nullable` + `ImplicitUsings` zapnuto ve všech projektech |
| Web | ASP.NET Core **Razor Pages** (server-side rendering, žádné SPA) |
| Data | EF Core + **SQL Server** |
| Frontend | Čisté HTML + vlastní CSS (SCSS → jeden CSS soubor), minimální vanilla JS |
| Logování | Serilog (konfigurace v `appsettings.json`) |
| Testy | xUnit (nebo MSTest dle volby), testy aplikační vrstvy a UI logiky |

Bez frontendových frameworků (Bootstrap, Tailwind, React, Blazor…), pokud to
uživatel výslovně nezadá.

## 3. Struktura řešení

```text
{App}.slnx
Directory.Build.props            ← společné vlastnosti (TargetFramework, Nullable, analyzátory)
global.json                      ← připnutá verze SDK
.editorconfig
.github/
  copilot-instructions.md        ← tento dokument (upravený)
  skills/                        ← opakovatelné postupy (nová entita, číselník, formulář…)
docs/
  architecture.md                ← vrstvy, schémata, integrace
  conventions.md                 ← konvence kódu a EF Core
  decisions/                     ← ADR (adr-001-….md), nikdy nepřepisovat, jen Supersedes
  modules/                       ← popis doménových modulů
src/
  {App}.Razor/                   ← prezentace
  {App}.Contract/                ← aplikační vrstva + DTO
  {App}.Database/                ← EF Core entity, DbContext, migrace
  {App}.ConsoleApp/              ← (volitelné) importy, dávky, údržba
tests/
  {App}.Tests/
```

### Závislosti mezi projekty (jednosměrné)

```text
{App}.Razor ──► {App}.Contract ──► {App}.Database ──► SQL Server
```

| Projekt | Odpovědnost | Smí referencovat |
|---|---|---|
| `{App}.Razor` | Razor Pages, PageModely, layout, CSS/JS, HTTP handlery, mapování vstupu na DTO | pouze `Contract` |
| `{App}.Contract` | Business logika, validace, **DTO**, autorizační helpery, registrace DI | `Database` |
| `{App}.Database` | EF Core entity, `DbContext`, konfigurace, enumy, migrace | – |
| `{App}.ConsoleApp` | Dávkové operace | `Contract` |

**Tvrdá pravidla:**

- `Razor` nikdy nereferencuje `Database` (ani přímo, ani tranzitivně přes `using`
  entit). UI nikdy nevidí EF entity – pracuje pouze s DTO z `Contract`.
- Business pravidla, validace a dotazy do DB patří do `Contract` služeb, **ne**
  do PageModelů.
- **Jediný composition root** je extension metoda
  `AddXxxServices(this IServiceCollection, IConfiguration)` v `Contract`
  (`ServiceCollectionExtensions.cs`). Registruje `DbContext`, Identity i všechny
  služby. `Program.cs` v Razor projektu ji pouze zavolá.
- Connection string patří do user secrets / proměnných prostředí, nikdy do verzované konfigurace.

## 4. Databázová vrstva (`{App}.Database`)

```text
Context/{App}DbContext.cs
Models/
  Schemas.cs                    ← const string názvy schémat
  {module}/{Entity}.cs          ← entity po modulech (schématech)
  {module}/{Entity}.Seed.cs     ← partial třída s konstruktory pro seeding
Configurations/{module}/{Entity}Configuration.cs   ← jen když atributy nestačí
Enums/E{Name}.cs                ← enumy pro číselníky
Resources/E{Name}.resx(+.cs.resx) ← lokalizace názvů enumů
Migrations/                     ← generuje a aplikuje uživatel
```

### Schémata

- Každý doménový modul má vlastní DB schéma (`dbo` = sdílené, `identity`,
  `{module}` …). Názvy jsou `const string` ve třídě `Schemas` (kvůli atributům).
- Každá entita: `[Table(nameof(Entity), Schema = Schemas.Module)]`.
- Vypni konvenci pojmenování tabulky podle `DbSet` (`TableNameFromDbSetConvention`),
  aby název tabulky vždy určoval atribut.
- Vypni `ForeignKeyIndexConvention` a FK indexy deklaruj explicitně `[Index(...)]`
  na třídě entity.

### Konvence mapování

- **Data atributy mají přednost před Fluent API.** Fluent API pouze pro to, co atributy neumí:
  computed sloupce, pojmenované default constrainty, value convertory, seed (`HasData`),
  sekvence, TPC/TPT.
- Vždy `nameof(...)` místo string literálů; názvy schémat přes `Schemas.*`.
- FK pojmenovávej konvencí `{Navigace}_Id` (nebo jinou jednotnou konvencí
  nastavenou centrálně v `OnModelCreating`); `HasColumnName` nepoužívej pro běžné FK.
- `[ForeignKey]` jen pro složené FK, sdílené sloupce ve více FK a FK, které je součástí PK.
- `[InverseProperty]` jen při více vztazích mezi stejnou dvojicí entit.
- `HasDefaultValue*` vždy s pojmenovaným constraintem: `DF_{Tabulka}_{Sloupec}`.
- Řetězce vždy s `[StringLength]`/`[MaxLength]`; `[Unicode(false)]` pro ASCII; `[Precision]`/`TypeName` pro čísla a časy.
- Hodnoty počítané databází (persisted computed column) nikdy nenastavuj v C#.
- Dědičnost: výchozí **TPC** se sdílenou DB sekvencí pro unikátní ID. TPT pouze jako zdokumentovaná výjimka (ADR).
- Zachovej `Nullable` sémantiku; `null!` jen s vysvětlujícím komentářem.
- Konfigurační soubor v `Configurations/` vytvářej jen, když je co vyjádřit Fluent API.

### Číselníky (lookup tabulky)

- Struktura: `int Id` (PK, bez identity) + `required string Name`.
- Pro každý číselník existuje `enum E{Name}`: členy anglicky, PascalCase, hodnoty od 1.
  **Nikdy** neměň ani nemaž existující hodnoty (korupce FK dat).
- `Name` v DB je neutrální klíč (`enum.ToString()`), nikdy lokalizovaný text.
  Lokalizace přes RESX (`E{Name}.resx` anglicky, `E{Name}.cs.resx` česky) a `[Display(ResourceType=…)]`.
- Seed: `builder.HasData(Enum.GetValues<EName>().Select(e => new Name(e)))`.

## 5. Aplikační vrstva (`{App}.Contract`)

```text
ServiceCollectionExtensions.cs  ← composition root
Services/{Entity}Service.cs     ← business logika a přístup k datům
Models/{module}/*.cs            ← DTO a modely pro UI, filtry, výjimky
Auth/                           ← claims, policies, resolvery aktuálního uživatele
Config/                         ← typované options
```

- Služby jsou `Scoped`, vstřikují `DbContext`, každá veřejná metoda je `async` a
  přijímá `CancellationToken ct = default`.
- Čtecí dotazy projektují přímo do DTO (`.Select(e => new Dto { … })`),
  nevracej EF entity; pro čtení použij `AsNoTracking()` tam, kde se nepoužije projekce.
- Zápis: DTO → entita → `SaveChangesAsync`. Nenalezené záznamy řeš výjimkou
  (`InvalidOperationException`) nebo `null` dle vzoru modulu.
- Validace patří sem (ne do PageModelu). Neplatná data **nikdy tiše neignoruj** –
  vyvolej validační výjimku (`{Module}ValidationException`), kterou PageModel
  převede na `ModelState` chybu.
- DTO validuj data atributy (`[Required]`, `[StringLength]`, `[Display]`),
  aby fungovaly s EditorTemplates a klientskou validací.
- Pokud kolize názvů DTO a entity: alias `using DbEntity = {App}.Database.Models.{module}.Entity;`.
- Integritu, kterou DB nedokáže vyjádřit (např. FK na abstraktní TPC předka), vynucuje služba.
- Datové operace nad externími systémy označenými jako read-only nikdy nezapisují.

## 6. Prezentační vrstva (`{App}.Razor`)

```text
Program.cs
Pages/
  Index.cshtml(.cs), Error.cshtml(.cs)
  Shared/
    _Layout.cshtml
    _ValidationScriptsPartial.cshtml
    EditorTemplates/            ← šablony formulářových polí podle typu
    Components/{Name}/Default.cshtml   ← ViewComponents pro složitější UI bloky
Areas/{module}/Pages/{Entity}/
  Index.cshtml(.cs)             ← seznam + filtr
  Edit.cshtml(.cs)              ← vytvoření/úprava (jedna stránka, volitelné id)
  Detail.cshtml(.cs)            ← (volitelné)
  _Partial.cshtml               ← dílčí záložky/bloky
Areas/{module}/Pages/_ViewImports.cshtml, _ViewStart.cshtml
TagHelpers/                     ← vlastní tag helpery
Styles/                         ← SCSS zdroje
wwwroot/css/site.css            ← GENEROVANÝ výstup, ručně neupravovat
wwwroot/js/site.js              ← minimální vanilla JS
package.json                    ← sass + skripty build:css / watch:css
```

- Každý doménový modul je **Razor Area** (`Areas/{module}/Pages/…`) se shodným názvem jako DB schéma.
- `Program.cs`: Serilog, `AddRazorPages()`, `AddXxxServices(configuration)`,
  lokalizace (výchozí kultura), `UseHttpsRedirection`, `UseAuthentication`, `UseAuthorization`,
  `MapStaticAssets`, `MapRazorPages().WithStaticAssets()`. **Žádná** registrace
  DbContextu ani business služeb zde.
- Výchozí autorizace: `FallbackPolicy` vyžaduje přihlášeného uživatele;
  výjimky jsou explicitní `[AllowAnonymous]`. Politiky se definují v Contract.
- Pro Identity používej `AddIdentityCore<User>().AddSignInManager()`, ne `AddIdentity`
  (přepsalo by výchozí autentizační schéma při externím OIDC/Entra přihlášení).
  Po scaffoldingu Identity stránek odstraň duplicitní registrace schémat.

### PageModel – vzor

```csharp
public class IndexModel : PageModel
{
    private readonly ProductService _service;
    public IndexModel(ProductService service) => _service = service;

    [BindProperty(SupportsGet = true)] public ProductFilter Filter { get; set; } = new();
    [TempData] public string? StatusMessage { get; set; }
    public List<ProductListItem> Items { get; set; } = [];

    public async Task OnGetAsync(CancellationToken ct) =>
        Items = await _service.GetAllAsync(Filter, ct);
}
```

- PageModel je tenký: načti → zavolej službu → nastav view data / redirect.
- `[BindProperty] Input` je DTO z Contractu; `OnPostAsync` zkontroluje `ModelState`,
  zavolá službu, chyby validace přidá do `ModelState`, po úspěchu `RedirectToPage`
  se `StatusMessage` (Post-Redirect-Get).
- Do view se nikdy nepředávají EF entity. Select listy jsou `SelectListItem`
  naplněné z DTO (`LookupSelectItem`).
- Mazání a jiné změny stavu pouze přes POST + antiforgery token.

## 7. Frontend: čisté HTML + CSS

### Principy

- **Sémantické HTML** (`<header>`, `<nav>`, `<main>`, `<table>`, `<form>`, `<label>`, `<footer>`).
  Žádný CSS framework, žádné utility-class soupy; třídy pojmenovávej významově.
- Stránky jsou renderované serverem; JavaScript jen pro chování, které nelze
  rozumně řešit na serveru (např. multiselect, modál, potvrzení). Bez frameworků.
- Přístupnost: každý input má `<label>`, ikonová tlačítka mají `title`/`aria-label`,
  validační zprávy přes `asp-validation-*`, dostatečný kontrast (WCAG 2.1 AA).
- Layout: jeden `_Layout.cshtml` (hlavička, navigace, `<main>`, patička);
  `_ViewImports.cshtml` v každé Area přidává `@addTagHelper *, {App}.Razor`.
- Používej Tag Helpery (`asp-page`, `asp-route-*`, `asp-for`, `asp-validation-for`),
  ne ručně skládané URL.

### SCSS a build

```text
Styles/
  site.scss        ← entry point, pouze @use/@import
  _vars.scss       ← design tokeny (CSS custom properties)
  _layout.scss     ← html/body/header/nav/footer
  _forms.scss      ← formuláře, tlačítka, validace
  _grid.scss       ← tabulky
  _{component}.scss← jedna komponenta = jeden partial
```

- Pouze SCSS je zdroj pravdy. `wwwroot/css/site.css` je generovaný, nikdy ručně.
- `package.json`:
  ```json
  {
    "private": true,
    "scripts": {
      "build:css": "sass Styles/site.scss wwwroot/css/site.css --style=compressed --no-source-map",
      "watch:css": "sass --watch Styles/site.scss:wwwroot/css/site.css"
    },
    "devDependencies": { "sass": "^1.77.8" }
  }
  ```
- V `.csproj` Razor projektu připoj MSBuild target, který před buildem spustí
  `npm install` (pokud chybí `node_modules`) a `npm run build:css`.

### Design tokeny

- Barvy, fonty, mezery, radiusy a breakpointy jsou **CSS custom properties** v `_vars.scss`.
- Dvě úrovně: primitivní (`--palette-red-500`) a **sémantické** (`--color-brand-primary`,
  `--color-surface`, `--color-text`, `--color-danger`). Komponenty používají výhradně
  sémantické tokeny; **žádné přímé hex hodnoty v komponentách**.
- Připrav tokeny pro light i dark režim (`prefers-color-scheme` / `[data-theme]`).
- Hover/active varianty mají vlastní token (`--color-brand-primary-active`).

### Standardní CSS kontrakt

| Třída | Použití |
|---|---|
| `.button` | primární akce |
| `.button.secondary` | sekundární akce (zpět, zrušit) |
| `.button.tertiary` | nebezpečná akce (smazat) |
| `.icon-btn` | tlačítko pouze s ikonou (v řádcích tabulek) |
| `.grid` | datová tabulka; sloupec akcí `th.buttons` / `td.buttons` |
| `.field` | blok label + input + validační hláška |
| `footer` ve formuláři | řádek s akcemi formuláře |

### Seznam (vzor)

```cshtml
<table class="grid">
    <thead><tr><th>Název</th><th class="buttons"></th></tr></thead>
    <tbody>
    @foreach (var item in Model.Items)
    {
        <tr>
            <td>@item.Name</td>
            <td class="buttons">
                <a class="button secondary icon-btn" asp-page="Edit" asp-route-id="@item.Id" title="Upravit">
                    <i class="fa-solid fa-pen fa-fw"></i>
                </a>
            </td>
        </tr>
    }
    </tbody>
</table>
```

### Formulář (vzor)

```cshtml
<form method="post">
    <div class="field">
        <label asp-for="Input.Name"></label>
        <input asp-for="Input.Name" />
        <span asp-validation-for="Input.Name"></span>
    </div>
    <footer>
        <div asp-validation-summary="ModelOnly"></div>
        <button type="submit" class="button"><i class="fa-solid fa-floppy-disk fa-fw"></i> Uložit</button>
        <a class="button secondary" asp-page="Index"><i class="fa-solid fa-arrow-left fa-fw"></i> Zpět</a>
    </footer>
</form>
```

### Ikony

Font Awesome (CDN) nebo jiná jediná sada; každá ikona má `fa-fw`. Akce v řádcích
tabulky jsou pouze ikony s `title`. Jednotný slovník: přidat `plus`, upravit `pen`,
smazat `trash`, detail `eye`, hledat `magnifying-glass`, uložit `floppy-disk`,
zpět `arrow-left`, varování `triangle-exclamation`.

### Automatické formuláře (EditorTemplates)

Admin formuláře generuj z DTO pomocí `Html.EditorForModel()` / `<editor asp-for>`:

- `Pages/Shared/EditorTemplates/` obsahuje šablony podle typu (`String`, `Int32`,
  `Boolean`, `Date`, `Time`, `MultilineText`, `Select`, `Object`, `_Layout`).
- Metadata pole (popisek, nápověda, typ šablony, pořadí) pocházejí z data atributů
  na DTO (`[Display]`, `[DataType]`, `[UIHint]`) – žádné ruční HTML pro každé pole.
- Společný obal pole (`_Layout`) vykresluje `.field`, label, input a validační hlášku.

## 8. Autentizace a autorizace (pokud je potřeba)

- Lokální ASP.NET Core Identity s vlastním schématem `identity` (tabulky bez prefixu `AspNet`),
  volitelně externí OIDC (Entra ID) – oba používají stejný user store.
- Role a business oprávnění jako claims; politiky registrované v Contract.
- Přihlášený uživatel se v Contract získává přes resolver (`CurrentUserIdResolver`),
  ne přímo z `HttpContext` ve službách.
- Autorizační pravidla jsou default-deny (`FallbackPolicy`).

## 9. Testy

- Projekt `tests/{App}.Tests`; testy cílí na Contract služby (EF Core
  SQLite in-memory nebo testovací DB), mapování/validaci DTO a čistou UI logiku
  (factory, tag helpery, exportéry).
- Pojmenování: `{Třída}Tests`, metoda `Metoda_Scénář_Očekávání`.
- Před dokončením: `dotnet build` + cílené `dotnet test`.

## 10. Příkazy

```powershell
dotnet build {App}.slnx
dotnet test tests\{App}.Tests\{App}.Tests.csproj -c Release
dotnet run --project src\{App}.Razor
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>" --project src\{App}.Razor
dotnet ef migrations list --project src\{App}.Database --startup-project src\{App}.Razor   # pouze čtení; migrace vytváří uživatel

Set-Location src\{App}.Razor
npm run build:css
npm run watch:css
```

## 11. Postup přidání nové funkce (checklist)

1. **Databáze:** entita v `Models/{module}/` (`[Table]`, `[Index]` na FK, atributy),
   `DbSet` v `DbContext`, případně `Configurations/`. Migraci nevytvářej – upozorni uživatele.
2. **Contract:** DTO v `Models/{module}/`, služba `{Entity}Service`, registrace v `AddXxxServices`.
3. **Razor:** stránky `Areas/{module}/Pages/{Entity}/Index|Edit`, PageModely s DI služby,
   odkaz v navigaci `_Layout.cshtml`.
4. **Styly:** nové komponenty jako SCSS partial + import v `site.scss`, jen sémantické tokeny; `npm run build:css`.
5. **Testy:** služba + validace.
6. **Dokumentace:** aktualizuj `docs/modules/{module}.md`; architektonická rozhodnutí zapiš jako nové ADR.
7. **Ověření:** `dotnet build`, `dotnet test`, ruční kontrola stránky.

## 12. Zákazy a správné alternativy

| Zákaz | Alternativa |
|---|---|
| Reference `Razor → Database` | Contract služba + DTO |
| EF entita v PageModelu/view | DTO projekce |
| Registrace `DbContext`/služeb v Razor `Program.cs` | `AddXxxServices` v Contract |
| `AddIdentity<T>()` při OIDC | `AddIdentityCore<T>().AddSignInManager()` |
| Ruční editace `site.css` | úprava SCSS + `npm run build:css` |
| Hex barvy v komponentách | sémantické CSS tokeny |
| CSS/JS frameworky bez zadání | vlastní SCSS, vanilla JS |
| String literály v atributech | `nameof(...)`, `Schemas.*` |
| Tiché ignorování neplatných dat | validační výjimka → `ModelState` |
| Lokalizovaný text v DB číselníku | neutrální klíč + RESX |
| Změna/mazání hodnot enumů číselníků | pouze přidávání nových hodnot |
| Vytváření/aplikace EF migrací agentem | uživatel po kontrole modelu |
| Connection string ve verzované konfiguraci | user secrets / env proměnné |
| Mazání nebo přepis ADR | nové ADR s `Supersedes` |

## 13. Dokumentace v repozitáři

Udržuj stručné kanonické dokumenty a v `copilot-instructions.md` na ně odkazuj
tabulkou „úkol → co přečíst“:

- `docs/architecture.md` – vrstvy, projekty, DB schémata, integrace.
- `docs/conventions.md` – EF Core a kódové konvence.
- `docs/modules/{module}.md` – doménový model a pravidla modulu.
- `docs/decisions/adr-NNN-*.md` – rozhodnutí (kontext, rozhodnutí, důsledky, `Supersedes`).
- `.github/skills/*/SKILL.md` – postupy: nová entita, číselník, default constraint,
  admin formulář, plán z issue.

Zdroje pravdy v pořadí: aktuální kód → ADR → `docs/` → tento soubor → skills.
Historické plány a research dokumenty nepřepisují aktuální implementaci.
