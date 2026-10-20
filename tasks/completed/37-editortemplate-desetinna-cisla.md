# Implementační plán: #37 EditorTemplate - desetinná čísla

**Issue:** [#37 — EditorTemplate - desetinná čísla](https://github.com/vasekNaus/SportSys/issues/37)

**Stav:** Implementováno a ověřeno automatickými testy.

## Cíl

Umožnit zadání a uložení desetinných čísel v českém formátu (`1,5`) ve všech
formulářích SportSys. Řešení tvoří sdílená EditorTemplate `Decimal.cshtml` a
model binder nezávislý na kultuře value provideru. Hlavní dotčené pole je
`Hours` u trenérů v požadavku na tréninky (`Requirement/EditTraining`).

## Výchozí stav

- Kultura aplikace je pevně `cs-CZ` (`src/SportSys.Razor/Program.cs`,
  `UseRequestLocalization`). POST value provider proto parsuje `decimal`
  v cs-CZ a očekává desetinnou čárku. GET query provider používá
  `InvariantCulture`.
- Všechna desetinná pole jsou nativní `<input type="number">`:
  - `Areas/sport/Pages/Training/Requirement/_CoachAssignmentsTable.cshtml`
    má ručně psaný `Input.CoachAssignments[i].Hours`. Hodnotu formátuje
    `InvariantCulture` (`1.50`).
  - `Areas/sport/Pages/Training/Requirement/EditTraining.cshtml` má
    `Input.DurationHours` přes `asp-for` s `type="number"`.
  - `Areas/Inventory/Pages/Items/Edit.cshtml` má `Input.AcquisitionPrice`.
  - `Areas/hr/Pages/Coach/_Contracts.cshtml` má `ContractInput.RewardAmount`.
- Příčina chyby:
  - Prohlížeč u `type="number"` vždy odesílá desetinnou tečku (`1.5`).
    Výchozí `DecimalModelBinder` ji v cs-CZ odmítne, nebo ji chybně vyloží
    jako oddělovač tisíců.
  - Zadanou čárku prohlížeč u `type="number"` ignoruje nebo nepovažuje za
    platnou hodnotu. Navíc `asp-for` u `type="number"` vyrenderuje hodnotu
    v cs-CZ (`1,50`), kterou prohlížeč nezobrazí.
- `Pages/Shared/EditorTemplates/` obsahuje `Date`, `Time`, `Int32`,
  `HtmlInput`, `String`, `Object`, `Select`, `Boolean`, `MultilineText` a
  `_Layout`. Šablona `Decimal.cshtml` zatím neexistuje, takže `decimal`
  v `Object.cshtml` padá na `String.cshtml`.
- Vzor pro řešení: `Date.cshtml`/`Time.cshtml`, `CzechDateTimeFormat`
  (`Infrastructure/`), `CzechDateTimeModelBinder` +
  `CzechDateTimeModelBinderProvider` (`ModelBinding/`, registrace v
  `Program.cs`) a testy `tests/SportSys.Razor.Tests/CzechDateTimeModelBinderTests.cs`.
- Klientská validace (`aspnet-client-validation`) je v `_Layout.cshtml`
  zakomentovaná. Validace probíhá jen na serveru a přes nativní HTML atributy.

## Potvrzené požadavky a rozhodnutí

- **Z issue:** pole generovaná EditorTemplate musí desetinné číslo zobrazit
  v platném CZ formátu. Hodnota zadaná s čárkou se musí propsat a uložit.
- **Ze zadání uživatele:** řešením je vlastní EditorTemplate pro desetinná
  čísla. Musí se opravit i editace `Hours`.
- **Technická rozhodnutí (z konvencí projektu a vzoru Date/Time):**
  - Pole bude textové (`type="text"`, `inputmode="decimal"`), nikoli
    `type="number"`. Formát pak určuje aplikace, ne prohlížeč či OS.
  - Zobrazení bude v cs-CZ s desetinnou čárkou, bez oddělovače tisíců,
    například `1,5` nebo `12500,00`.
  - Server přijme desetinnou čárku i tečku. Tečku vkládají klávesnice
    a kopírované hodnoty. Mezery (běžná, NBSP, úzká NBSP) se tolerují jako
    oddělovače tisíců. Hodnotu s oběma oddělovači nebo s více desetinnými
    oddělovači (`1.234,5`, `1,2,3`) server odmítne českou chybou, protože je
    nejednoznačná.
  - Parsování nezávisí na kultuře value provideru, stejně jako u
    `CzechDateTimeModelBinder`.
  - Kvůli konzistenci se převedou i ostatní ruční `type="number"` decimal
    vstupy, protože mají stejnou chybu.

## Technický návrh

### Formátování a parsování (`Infrastructure/CzechNumberFormat.cs`)

Nový statický helper po vzoru `CzechDateTimeFormat`:

- `string FormatDecimal(object? value, string? format = null)`
  - podporuje `decimal`, `double` i `float`,
  - pro `null` vrací prázdný řetězec,
  - bez formátu použije `ToString(CultureInfo cs-CZ)`. `decimal` si tak
    zachová škálu z DB, například `1,50`,
  - s formátem použije `string.Format(cs-CZ, format, value)`.
- `bool TryParseDecimal(string? value, out decimal result)`
  1. Ořízne mezery, odstraní mezery uvnitř (` `, `\u00A0`, `\u202F`).
  2. Pokud obsahuje `,` i `.` zároveň, nebo některý z nich víc než jednou,
     vrátí `false`.
  3. Nahradí `,` za `.` a parsuje přes `decimal.TryParse` s
     `NumberStyles.AllowLeadingSign | AllowDecimalPoint` a
     `CultureInfo.InvariantCulture`.

### Model binder (`ModelBinding/CzechDecimalModelBinder.cs` + provider)

- `CzechDecimalModelBinder : IModelBinder` má stejnou strukturu jako
  `CzechDateTimeModelBinder`:
  - prázdná hodnota u `decimal?` vrátí `null`,
  - prázdná hodnota u `decimal` přidá chybu „Pole je povinné.“,
  - neplatná hodnota přidá chybu „Zadejte číslo, např. 1,5.“,
  - vždy volá `ModelState.SetModelValue`, aby se při chybě znovu zobrazila
    zadaná hodnota.
- `CzechDecimalModelBinderProvider` vrací binder pro `decimal` a
  `decimal?`.
- Registrace v `Program.cs` přes
  `options.ModelBinderProviders.Insert(0, new CzechDecimalModelBinderProvider())`
  vedle `CzechDateTimeModelBinderProvider`.
- `double`/`float` se ve formulářích nepoužívají, proto zůstávají mimo
  rozsah binderu.

### EditorTemplate (`Pages/Shared/EditorTemplates/Decimal.cshtml`)

- `Layout = "/Pages/Shared/EditorTemplates/_Layout.cshtml"`.
- Výběr podle CLR typu (`Decimal`) funguje automaticky pro `decimal` i
  `decimal?` v `Object.cshtml`. Pro jednorázová pole se použije explicitně
  `@Html.EditorFor(..., "Decimal")`.
- Hodnota: `CzechNumberFormat.FormatDecimal(ViewData.Model,
  ViewData.ModelMetadata.EditFormatString)`. Platí stejné pravidlo jako
  v `Time.cshtml`: ModelState záznam bez chyb se odstraní, aby se použila
  naformátovaná hodnota. Záznam s chybou zůstane, aby uživatel viděl svůj
  vstup.
- Atributy:
  - `type="text"`, `inputmode="decimal"`, `autocomplete="off"`,
  - `class` = `ViewData["InputCssClass"]` nebo výchozí `short`,
  - `placeholder` = `ModelMetadata.Placeholder` nebo `0,00`,
  - `data-decimal-input` jako háček pro případné budoucí JS,
  - volitelné `ViewData["InputAttributes"]` (`IDictionary<string, object>`)
    sloučené do atributů, například `aria-label` a `required` pro tabulku.
- Validační `data-val-*` atributy generuje `Html.TextBox` automaticky.
  Klientská validace je vypnutá, proto se nic dalšího neřeší.

### Použití v `_CoachAssignmentsTable.cshtml`

Ruční `<input type="number">` nahradit voláním:

```cshtml
@Html.EditorFor(
    _ => assignment.Hours,
    "Decimal",
    $"Input.CoachAssignments[{i}].Hours",
    new { InputCssClass = "textbox", InputAttributes = new Dictionary<string, object>
          { ["aria-label"] = "Hodiny", ["required"] = "required" } })
```

Přetížení s `htmlFieldName` zachová název pole očekávaný model bindingem
`EditTrainingModel`. Metadata (`Display`, `Range`) se převezmou z
`RequirementCoachAssignmentInput.Hours`. Stávající `<span data-valmsg-for>`
zůstane. Při implementaci ověřit:

- vygenerovaný atribut `name`,
- znovuzobrazení neplatné hodnoty po POST s chybou.

Pokud by `htmlFieldName` kolidovalo s prefixem partialu, je fallback
ruční `<input type="text" inputmode="decimal">` s hodnotou z
`CzechNumberFormat.FormatDecimal`. Šablona se pak nepoužije, ale formát
i binder zůstanou stejné.

### Ostatní decimal vstupy

Nahradit `type="number"` voláním `@Html.EditorFor(m => ..., "Decimal",
new { InputCssClass = "..." })`:

| Stránka | Pole | Třída |
|---|---|---|
| `Requirement/EditTraining.cshtml` | `Input.DurationHours` | `textbox` |
| `Inventory/Pages/Items/Edit.cshtml` | `Input.AcquisitionPrice` | `short` |
| `hr/Pages/Coach/_Contracts.cshtml` | `ContractInput.RewardAmount` | `short` |

Atributy `min`/`max`/`step` zanikají. Rozsah dál hlídají `[Range]` v DTO
a server.

### Validace přesnosti

Textový vstup už nehlídá `step="0.01"`. Hodnotu s více desetinnými místy by
pak EF při ukládání do `decimal(5,2)` tiše zaokrouhlil.

- `RequirementCoachAssignmentInput.Hours` už kontroluje
  `decimal.Round(x, 2)` v `TrainingRequirementEditDto.Validate`.
- Doplnit stejnou kontrolu pro `TrainingRequirementEditDto.DurationHours`
  (`src/SportSys.Contract/Models/RequirementEditModels.cs`), s chybou
  „Rozsah v hodinách může mít nejvýše dvě desetinná místa.“.
- U `AcquisitionPrice` a `RewardAmount` přidat stejnou kontrolu jen
  tehdy, pokud jejich DTO už implementuje `IValidatableObject`. Jinak je
  ponechat mimo rozsah a zapsat do „Mimo rozsah“.

## Implementační kroky

### Fáze 1: Infrastruktura (Razor)

1. Vytvořit `src/SportSys.Razor/Infrastructure/CzechNumberFormat.cs`.
2. Vytvořit `src/SportSys.Razor/ModelBinding/CzechDecimalModelBinder.cs` a
   `CzechDecimalModelBinderProvider.cs`.
3. Zaregistrovat provider v `src/SportSys.Razor/Program.cs`.

### Fáze 2: EditorTemplate

4. Vytvořit `src/SportSys.Razor/Pages/Shared/EditorTemplates/Decimal.cshtml`.

### Fáze 3: Formuláře

5. Upravit `_CoachAssignmentsTable.cshtml` (pole `Hours`).
6. Upravit `EditTraining.cshtml` (`DurationHours`).
7. Upravit `Inventory/Pages/Items/Edit.cshtml` (`AcquisitionPrice`).
8. Upravit `hr/Pages/Coach/_Contracts.cshtml` (`RewardAmount`).

### Fáze 4: Contract validace

9. Doplnit kontrolu dvou desetinných míst pro `DurationHours` v
   `TrainingRequirementEditDto.Validate`.

### Fáze 5: Dokumentace

10. `docs/modules/frontend.md`: nová sekce „Desetinná čísla ve formulářích“
    vedle sekce „Datum a čas ve formulářích“. Obsah:
    - šablona `Decimal`,
    - formát s čárkou,
    - binder přijímá čárku i tečku,
    - zákaz `type="number"` pro decimal.
11. `docs/conventions.md` (oddíl kolem ř. 216): doplnit pravidlo pro decimal
    pole.
12. `.github/skills/editor-template/SKILL.md`: upravit řádek tabulky
    `decimal → Decimal.cshtml` na „textbox `inputmode=decimal`, formát
    cs-CZ (čárka)“.

### Fáze 6: Testy

13. Doplnit testy (viz níže) a spustit build a testy.

## Soubory ke změně

| Soubor | Změna |
|---|---|
| `src/SportSys.Razor/Infrastructure/CzechNumberFormat.cs` | nový |
| `src/SportSys.Razor/ModelBinding/CzechDecimalModelBinder.cs` | nový |
| `src/SportSys.Razor/ModelBinding/CzechDecimalModelBinderProvider.cs` | nový |
| `src/SportSys.Razor/Program.cs` | registrace provideru |
| `src/SportSys.Razor/Pages/Shared/EditorTemplates/Decimal.cshtml` | nový |
| `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/_CoachAssignmentsTable.cshtml` | `Hours` přes šablonu |
| `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/EditTraining.cshtml` | `DurationHours` přes šablonu |
| `src/SportSys.Razor/Areas/Inventory/Pages/Items/Edit.cshtml` | `AcquisitionPrice` přes šablonu |
| `src/SportSys.Razor/Areas/hr/Pages/Coach/_Contracts.cshtml` | `RewardAmount` přes šablonu |
| `src/SportSys.Contract/Models/RequirementEditModels.cs` | přesnost `DurationHours` |
| `tests/SportSys.Razor.Tests/CzechNumberFormatTests.cs` | nový |
| `tests/SportSys.Razor.Tests/CzechDecimalModelBinderTests.cs` | nový |
| `docs/modules/frontend.md`, `docs/conventions.md`, `.github/skills/editor-template/SKILL.md` | dokumentace |

**Databázový model se nemění, migrace není potřeba.**

## Testy a ověření

- `CzechNumberFormatTests`:
  - formát `1.5m` → `1,5`, `1.50m` → `1,50`, `12500m` → `12500`,
  - `null` → prázdný řetězec,
  - formát `{0:0.00}` → `1,50`,
  - parse:
    - `1,5`, `1.5`, ` 1,50 ` a `-2,25` uspějí,
    - `1 234,5` (běžná mezera i NBSP) uspěje,
    - `abc`, `1,2,3`, `1.234,5` a `1,` + `.` selžou.
- `CzechDecimalModelBinderTests` (vzor `CzechDateTimeModelBinderTests`,
  value provider v `InvariantCulture` i `cs-CZ`):
  - čárka i tečka se nabindují na `decimal` i `decimal?`,
  - prázdné `decimal?` → `null`,
  - prázdné `decimal` → chyba „povinné“,
  - neplatná hodnota → česká chyba a `AttemptedValue` je zachovaná.
- Test provideru: vrací binder pro `decimal`/`decimal?`, pro `int`/`string`
  vrací `null`.
- Test `TrainingRequirementEditDto.Validate`: `DurationHours = 1.234m`
  vrátí chybu přesnosti.
- Příkazy:

```powershell
dotnet build SportSys.slnx
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
```

## Manuální akceptace

1. Na `Requirement/EditTraining` zadat u trenéra `Hodiny` = `1,5` a uložit.
   Hodnota se uloží a po načtení se zobrazí `1,50`.
2. Totéž s `1.5`. Uloží se stejně.
3. Zadat `abc`. Formulář se vrátí s chybou „Zadejte číslo…“ a v poli zůstane
   `abc`.
4. Zadat `1,234`. Zobrazí se chyba přesnosti.
5. `Rozsah v hodinách` = `2,25`. Uloží se a zobrazí se `2,25`.
6. Smlouva trenéra s odměnou `12500,50` a pořizovací cena položky skladu
   `999,90` se uloží a zobrazí s čárkou.
7. Na mobilu se u desetinných polí zobrazí numerická klávesnice
   (`inputmode="decimal"`).

## Beze změny

- `Int32.cshtml` a celočíselná pole (`MatchCount`, `SortOrder`,
  `PeriodYear`...) zůstávají `type="number"`, protože čárku nepotřebují.
- `CzechDateTimeModelBinder`, `Date.cshtml`, `Time.cshtml`.
- Databázové entity, konfigurace a migrace.
- Zobrazení decimal hodnot mimo formuláře (tabulky, Excel export).
- `SportSys.Web` (prototyp).

## Mimo rozsah

- Zapnutí klientské validace `aspnet-client-validation`. Až se zapne, bude
  potřeba doplnit klientský parser čárky pro `range`.
- JS maska nebo automatické formátování během psaní.
- Binder pro `double`/`float`.
- Kontrola počtu desetinných míst u `AcquisitionPrice` a `RewardAmount`,
  pokud by vyžadovala nové `IValidatableObject` (viz Technický návrh).

## Hotovo, když

- [x] `Hours` na `Requirement/EditTraining` lze uložit s desetinnou čárkou
      i tečkou a hodnota se zobrazí s čárkou.
- [x] Žádný `decimal` vstup v `SportSys.Razor` nepoužívá `type="number"`.
- [x] `decimal` vlastnosti v `Object.cshtml` formulářích se renderují přes
      `Decimal.cshtml`.
- [x] Neplatný vstup vrací českou chybu a zachová zadanou hodnotu.
- [x] Hodnota s více než dvěma desetinnými místy u `Hours`/`DurationHours`
      se tiše nezaokrouhlí, ale vrátí chybu.
- [x] Nové testy prochází, build je bez chyb.
- [x] Dokumentace (`frontend.md`, `conventions.md`, skill `editor-template`)
      popisuje nový stav.
- [x] V rámci této implementace nevznikla ani nebyla změněna EF Core migrace.
