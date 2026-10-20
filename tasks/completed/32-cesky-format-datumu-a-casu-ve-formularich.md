# Implementační plán: Český formát data a času ve formulářích

**Issue:** — (zadání uživatele, bez GitHub issue)

**Stav:** Implementováno.

## Cíl

Všechna vstupní pole pro datum a čas v `SportSys.Razor` musí zobrazovat
a přijímat český formát (`d. M. yyyy`, 24hodinový čas `HH:mm`) nezávisle na
jazyce prohlížeče a operačního systému uživatele.

## Výchozí stav

- Server běží v kultuře `cs-CZ` (`src/SportSys.Razor/Program.cs:11-14`,
  `UseRequestLocalization` na ř. 43–46). Kořenový `<html lang="cs-CZ">` je
  v `Pages/Shared/_Layout.cshtml:2`.
- **Příčina chyby:** formuláře používají nativní `<input type="date">`
  a `<input type="time">`. Jejich zobrazovaný formát určuje jazyk
  prohlížeče/OS (v EN prohlížeči `MM/DD/YYYY` a `AM/PM`), nikoli serverová
  kultura. Atribut `lang="cs-CZ"` v EditorTemplates Chrome/Edge ignorují.
- Hodnota nativních vstupů je vždy ISO (`yyyy-MM-dd`, `HH:mm`), proto ji
  aplikace nyní renderuje přes `ToString("yyyy-MM-dd")`.
- **Model binding:** hodnoty z formuláře (POST) se parsují s `CurrentCulture`,
  ale hodnoty z query stringu (GET filtry) s `InvariantCulture`. Po přechodu
  na textový vstup v CZ formátu by GET filtry bez úpravy přestaly fungovat.

### Inventura dotčených vstupů

| Soubor | Pole | Typ | Způsob renderování |
|---|---|---|---|
| `Pages/Shared/EditorTemplates/Date.cshtml` | všechny `[DataType(DataType.Date)]` | date | EditorTemplate |
| `Pages/Shared/EditorTemplates/Time.cshtml` | všechny `[DataType(DataType.Time)]` | time | EditorTemplate |
| `Areas/sport/Pages/Training/Schedule/Index.cshtml:126,135` | `DateFrom`, `DateTo` | date | ruční `<input>`, GET |
| `Areas/sport/Pages/Training/Plan/Index.cshtml:125` | `ValidOn` | date | ruční `<input>`, GET |
| `Areas/sport/Pages/Training/Requirement/Index.cshtml:105` | `ValidOn` | date | ruční `<input>`, GET |
| `Areas/Inventory/Pages/Loans/Index.cshtml:23,28` | `Filter.DateFrom`, `Filter.DateTo` | date | `asp-for` + ruční `value`, GET |
| `Areas/Inventory/Pages/Items/Edit.cshtml:74,129` | `AcquisitionDate`, `WarrantyUntil` | date | `asp-for`, POST |
| `Areas/sport/Pages/Training/Requirement/EditMatch.cshtml:28,33` | `Input.From`, `Input.To` | date | `asp-for`, POST |
| `Areas/sport/Pages/Training/Requirement/EditTraining.cshtml:30,35` | `Input.From`, `Input.To` | date | `asp-for`, POST |
| `Areas/hr/Pages/Coach/_Licenses.cshtml:47,52` | `LicenseInput.ValidFrom/To` | date | `asp-for`, POST |
| `Areas/hr/Pages/Coach/_Settings.cshtml:49,54` | `SettingInput.ValidFrom/To` | date | `asp-for`, POST |

DTO používající EditorTemplates (přes `@Html.EditorFor`):
`SeasonDto` (From/To), `TrainingEditDto` (Date, TimeFrom, TimeTo),
`TrainingPlanEditDto` (From, To, TimeFrom, TimeTo) v `src/SportSys.Contract/Models/`.
`CoachModels.cs` a `RequirementEditModels.cs` mají `[DataType(DataType.Date)]`,
ale renderují se ručně přes `asp-for` (viz tabulka).

## Potvrzené požadavky a rozhodnutí

- Všechna datumová pole v CZ formátu – potvrzeno uživatelem.
- Výběr data přes **textové pole + kalendář flatpickr** s českou lokalizací –
  potvrzeno uživatelem.
- **Časová pole jsou součástí rozsahu** (24h formát `HH:mm`) – potvrzeno
  uživatelem; odpovídá i dřívějšímu rozhodnutí o českém 24h formátu napříč
  systémem.
- Zobrazovaný formát data: `d. M. yyyy` (např. `5. 10. 2026`) – shodný
  s existujícím zobrazením v `Models/TrainingSchedule/EventModel.cs:226`.
- Parser přijímá varianty s mezerami i bez a s úvodními nulami
  (`5.10.2026`, `05.10.2026`, `5. 10. 2026`) a kvůli zpětné kompatibilitě
  záložek/odkazů také ISO `yyyy-MM-dd`.

## Technický návrh

### 1. Serverové parsování nezávislé na value provideru

Nový `IModelBinderProvider` + `IModelBinder` v `src/SportSys.Razor/ModelBinding/`:

- `CzechDateTimeModelBinderProvider` – vrací binder pro `DateOnly`, `TimeOnly`,
  `DateTime`, `TimeSpan` a jejich nullable varianty.
- `CzechDateTimeModelBinder` – parsuje přes `TryParseExact` s
  `CultureInfo.GetCultureInfo("cs-CZ")` a pevnou sadou formátů:
  - datum: `d.M.yyyy`, `d. M. yyyy`, `yyyy-MM-dd`,
  - čas: `H:mm`, `HH:mm`, `HH:mm:ss`.
  Ignoruje kulturu value provideru, takže GET i POST fungují stejně.
  Prázdná hodnota → `null` u nullable typů; u non-nullable ponechá chování
  `[Required]`. Neplatná hodnota → `ModelState.TryAddModelError` s českou
  hláškou (`Zadejte datum ve formátu D. M. RRRR.` / `Zadejte čas ve formátu HH:MM.`)
  a `ModelBindingResult.Failed()` (žádné tiché ignorování).
- Registrace v `Program.cs`:
  `AddRazorPages().AddMvcOptions(o => o.ModelBinderProviders.Insert(0, new CzechDateTimeModelBinderProvider()))`.
  Jde o prezentační infrastrukturu, nikoli business službu, takže patří do
  Razor projektu (nekoliduje s pravidlem o composition rootu v Contract).

### 2. Centrální renderování vstupů přes EditorTemplates

Jediným místem, kde vzniká HTML datumového/časového vstupu, jsou šablony
`Pages/Shared/EditorTemplates/Date.cshtml` (datum) a `Time.cshtml` (čas).
Šablonu volí typ pole – datum vždy `"Date"`, čas vždy `"Time"`:

- **Časová pole** (`TimeFrom`, `TimeTo` v `TrainingEditDto`
  a `TrainingPlanEditDto`) už šablonu `Time` používají přes
  `[DataType(DataType.Time)]` a `@Html.EditorFor(m => m.Input)`; nemění se
  jejich markup, jen samotná šablona `Time.cshtml`.
- **Datumová pole přes DTO** (`SeasonDto`, `TrainingEditDto.Date`,
  `TrainingPlanEditDto.From/To`) analogicky používají šablonu `Date`.
- **Ručně psaná pole** (16 vstupů v 9 souborech, viz fáze 2) jsou výhradně
  datumová a převedou se na `@Html.EditorFor(..., "Date", ...)`. Žádné ručně
  psané časové pole v aplikaci není.

- Šablony vykreslí `@Html.TextBox(string.Empty, value, new { ... })` s:
  - `type="text"`, `autocomplete="off"`, `inputmode="numeric"`,
  - hodnotou z `CzechDateTimeFormat.Format(...)` (`d. M. yyyy` / `HH:mm`),
  - `data-date-input` resp. `data-time-input`,
  - `placeholder` (`d. m. rrrr` / `hh:mm`),
  - bez atributu `lang`.
- CSS třída: výchozí `short`, přepsatelná přes `ViewData["InputCssClass"]`
  (stejný vzor jako `EditorTemplates/HtmlInput.cshtml`), např.
  `@Html.EditorFor(m => m.Input.From, "Date", new { InputCssClass = "textbox" })`.
- Explicitní název šablony (`"Date"`/`"Time"`) zajistí správnou šablonu
  i u vlastností PageModelu bez `[DataType]` (filtry `DateFrom`, `ValidOn`…).
- `EditorFor` generuje stejné `id`/`name` jako `asp-for`
  (např. `LicenseInput.ValidFrom`, `DateFrom`), takže binding a
  `<label asp-for>` zůstanou beze změny.
- Formátovací logiku vyčlenit do statického helperu
  `src/SportSys.Razor/Infrastructure/CzechDateTimeFormat.cs`
  (konstanty formátů, `Format(DateOnly?)`, `Format(DateTime?)`,
  `Format(TimeOnly?)`, `Format(TimeSpan?)`, `TryParseDate`, `TryParseTime`),
  sdíleného binderem i EditorTemplates – jediný zdroj pravdy pro formáty.

### 3. Klientský kalendář flatpickr

- Do `_Layout.cshtml` přidat z jsDelivr (stejný vzor jako Tom Select,
  pevně zafixovaná verze, např. `flatpickr@4.6.13`):
  `flatpickr.min.css`, `flatpickr.min.js`, `l10n/cs.js`.
- V `wwwroot/js/site.js` nový blok `// ── Datum a čas (flatpickr) ──`:
  - `[data-date-input]` → `flatpickr(el, { locale: 'cs', dateFormat: 'j. n. Y', allowInput: true, disableMobile: true })`,
  - `[data-time-input]` → `{ enableTime: true, noCalendar: true, time_24hr: true, dateFormat: 'H:i', allowInput: true, disableMobile: true }`,
  - `parseDate` callback přijímající stejné varianty jako server
    (s/bez mezer, s/bez nul), aby ruční zápis neztrácel hodnotu.
  - Guard `if (typeof flatpickr === 'undefined') return;` – bez JS zůstane
    funkční textové pole se serverovou validací.
- `disableMobile: true` je nutné, jinak flatpickr na mobilu vrátí nativní
  `type="date"` a problém se vrátí.

### 4. Styly

- `src/SportSys.Razor/Styles/_schedule.scss:38` – selektor
  `input[type="date"]` nahradit `input[data-date-input]` (případně doplnit
  `input[data-time-input]`).
- Nový partial `Styles/_flatpickr.scss` (import v `site.scss`) s přebarvením
  kalendáře přes CSS tokeny (`--color-surface-1`, `--color-border-strong`,
  `--color-text-primary`, akcentní barva) včetně dark mode – podle skillu
  `.github/skills/barevna-schemata/SKILL.md`.
- `wwwroot/css/site.css` neupravovat ručně, pouze `npm run build:css`.

## Implementační kroky

### Fáze 1: Sdílený formát a model binding

1. Vytvořit `Infrastructure/CzechDateTimeFormat.cs`.
2. Vytvořit `ModelBinding/CzechDateTimeModelBinder.cs`
   a `ModelBinding/CzechDateTimeModelBinderProvider.cs`.
3. Zaregistrovat provider v `Program.cs`.
4. Unit testy (viz níže). Po této fázi musí stávající ISO vstupy fungovat
   beze změny (fallback `yyyy-MM-dd`).

### Fáze 2: Renderování vstupů

1. Upravit `EditorTemplates/Date.cshtml` a `Time.cshtml`: `type="text"`,
   hodnota přes `CzechDateTimeFormat`, `data-date-input`/`data-time-input`,
   `placeholder`, třída z `ViewData["InputCssClass"]` (výchozí `short`),
   odstranit `lang`.
2. Všech 16 ručně psaných vstupů (všechny jsou datumové) nahradit
   `@Html.EditorFor(..., "Date", ...)` (zachovat stávající CSS třídu přes
   `InputCssClass`). Časová pole se v této fázi nepřevádějí – už jdou přes
   šablonu `Time` (krok 1):
   - `Training/Schedule/Index.cshtml` (`DateFrom`, `DateTo`, třída `textbox`),
   - `Training/Plan/Index.cshtml` (`ValidOn`),
   - `Training/Requirement/Index.cshtml` (`ValidOn`),
   - `Inventory/Pages/Loans/Index.cshtml` (`Filter.DateFrom`, `Filter.DateTo`;
     odpadne ruční `value="...ToString("yyyy-MM-dd")"`),
   - `Inventory/Pages/Items/Edit.cshtml` (`AcquisitionDate`, `WarrantyUntil`),
   - `Training/Requirement/EditMatch.cshtml` a `EditTraining.cshtml`
     (`Input.From`, `Input.To`, třída `textbox`),
   - `hr/Pages/Coach/_Licenses.cshtml` a `_Settings.cshtml`
     (`ValidFrom`, `ValidTo`).
3. Ověřit, že `<label asp-for>` a `<span asp-validation-for>` u převedených
   polí odkazují na stejné `id` jako vygenerovaný vstup.
4. Grep kontrola: v `src/SportSys.Razor` nesmí zůstat `type="date"`,
   `type="time"` ani `ToString("yyyy-MM-dd")` ve vstupech (výjimkou je
   `_schedule.scss`, který se mění ve fázi 3).

### Fáze 3: Kalendář a styly

1. Přidat flatpickr do `_Layout.cshtml`.
2. Inicializace v `site.js`.
3. SCSS (`_schedule.scss`, nový `_flatpickr.scss`), `npm run build:css`.

### Fáze 4: Dokumentace

- `docs/modules/frontend.md` – sekce „Datum a čas“: formáty, povinné
  použití `@Html.EditorFor(..., "Date"|"Time")`, `InputCssClass`, flatpickr,
  zákaz nativních `type="date|time"`.
- `docs/conventions.md` – krátká poznámka, že `[DataType(Date|Time)]` se
  renderuje v CZ formátu a binduje přes `CzechDateTimeModelBinder`.

## Soubory ke změně

Nové:
- `src/SportSys.Razor/Infrastructure/CzechDateTimeFormat.cs`
- `src/SportSys.Razor/ModelBinding/CzechDateTimeModelBinder.cs`
- `src/SportSys.Razor/ModelBinding/CzechDateTimeModelBinderProvider.cs`
- `src/SportSys.Razor/Styles/_flatpickr.scss`
- `tests/SportSys.Razor.Tests/CzechDateTimeModelBinderTests.cs`
- `tests/SportSys.Razor.Tests/CzechDateTimeFormatTests.cs`

Upravené:
- `src/SportSys.Razor/Program.cs`
- `src/SportSys.Razor/Pages/Shared/_Layout.cshtml`
- `src/SportSys.Razor/Pages/Shared/EditorTemplates/Date.cshtml`
- `src/SportSys.Razor/Pages/Shared/EditorTemplates/Time.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml`
- `src/SportSys.Razor/Areas/Inventory/Pages/Loans/Index.cshtml`
- `src/SportSys.Razor/Areas/Inventory/Pages/Items/Edit.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/EditMatch.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/EditTraining.cshtml`
- `src/SportSys.Razor/Areas/hr/Pages/Coach/_Licenses.cshtml`
- `src/SportSys.Razor/Areas/hr/Pages/Coach/_Settings.cshtml`
- `src/SportSys.Razor/wwwroot/js/site.js`
- `src/SportSys.Razor/Styles/_schedule.scss`, `Styles/site.scss`
- `src/SportSys.Razor/wwwroot/css/site.css` (pouze vygenerováno)
- `docs/modules/frontend.md`, `docs/conventions.md`

## Testy a ověření

`CzechDateTimeModelBinderTests` (xUnit, vzor `SearchMultiSelectTagHelperTests`):
- `5. 10. 2026`, `5.10.2026`, `05.10.2026`, `2026-10-05` → `DateOnly(2026,10,5)`,
- `7:05`, `07:05`, `19:30` → `TimeOnly`,
- prázdná hodnota u `DateOnly?` → `null` bez chyby,
- `10/05/2026`, `31. 2. 2026`, `25:00`, `abc` → chyba v `ModelState`
  s českou hláškou,
- hodnota z query stringu (value provider s `InvariantCulture`) se parsuje
  stejně jako z formuláře.

`CzechDateTimeFormatTests`:
- `DateOnly(2026,10,5)` → `5. 10. 2026`, `DateTime` stejně (bez času),
- `TimeOnly(7,5)` → `07:05`, `TimeSpan` stejně,
- `null` → prázdný řetězec,
- `TryParseDate`/`TryParseTime` – round-trip s `Format`.

Renderování EditorTemplates se ověřuje manuální akceptací (projekt nemá
infrastrukturu pro renderování Razor šablon v unit testech).

Příkazy:

```powershell
dotnet build SportSys.slnx
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
Set-Location src\SportSys.Razor; npm run build:css
```

## Manuální akceptace

Prohlížeč přepnutý do angličtiny (en-US):

1. Rozvrh (`/sport/Training/Schedule`) – filtr Od/Do zobrazuje `d. M. yyyy`,
   kalendář je česky (Po–Ne, české měsíce), filtrování funguje a hodnoty
   zůstanou po odeslání ve stejném formátu.
2. Starý odkaz s `?DateFrom=2026-10-01` stále funguje.
3. Editace tréninku / tréninkového plánu – datum i časy (`HH:mm`, 24h, bez
   AM/PM), uložení a opětovné zobrazení.
4. Sezóna, požadavky (EditMatch/EditTraining), trenér (licence, nastavení),
   položky skladu, filtr výpůjček – zobrazení, uložení, filtrování.
5. Ruční zápis `5.10.2026` bez kalendáře se uloží jako 5. 10. 2026.
6. Neplatné datum (`31. 2. 2026`) zobrazí českou validační hlášku.
7. Dark mode – kalendář čitelný, barvy podle tokenů.
8. Mobilní zobrazení – kalendář flatpickr, ne nativní EN picker.

## Beze změny

- Databázový model, entity, migrace – **žádná změna DB není potřeba**.
- Contract DTO a jejich `[DataType]` atributy.
- Serverová kultura a `UseRequestLocalization` v `Program.cs`.
- Formátování dat mimo vstupní pole (tabulky, rozvrh, Excel export).
- Výběr měsíce/roku v `hr/Pages/Attendance/Index.cshtml` (není datumový vstup).
- `SportSys.Web` a scaffoldované Identity stránky.

## Mimo rozsah

- Sjednocení formátu zobrazovaných (needitovatelných) dat v přehledech.
- Výběr rozsahu dat v jednom poli (flatpickr `mode: 'range'`).
- Přechod z CDN na lokální bundling JS knihoven.

## Hotovo, když

- V `SportSys.Razor` není žádný renderovaný `type="date"`/`type="time"`.
- Všechna datumová pole zobrazují `d. M. yyyy` a časová `HH:mm` v EN
  i CZ prohlížeči, včetně mobilu.
- GET filtry i POST formuláře přijímají CZ formát a ISO fallback; neplatné
  hodnoty vrací českou validační chybu.
- Nové unit testy procházejí, `dotnet build` a `npm run build:css` bez chyb.
- Dokumentace `docs/modules/frontend.md` a `docs/conventions.md` aktualizována.
