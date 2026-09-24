# 02 – DataAnnotations – kompletní reference atributů

> **Navigace:** [← Core concepts](01-core-concepts.md) | [Object.cshtml →](03-object-template.md) | [Skill](SKILL.md)

---

## Atributy ovlivňující výběr šablony

### [UIHint] – přímá volba šablony (nejvyšší priorita)

```csharp
[UIHint("ColorPicker")]          // → EditorTemplates/ColorPicker.cshtml
public string ThemeColor { get; set; }
```

- Má **nejvyšší prioritu** ze všech atributů (přebíjí `[DataType]`)
- **Namespace:** `System.ComponentModel.DataAnnotations`

### [DataType] – sémantický typ (druhá priorita)

```csharp
// Přes enum hodnotu
[DataType(DataType.Password)]
[DataType(DataType.Date)]
[DataType(DataType.MultilineText)]

// Přes vlastní řetězec → EditorTemplates/{řetězec}.cshtml
[DataType("Markdown")]
[DataType("ColorPicker")]
```

### Validační atributy → typ inputu (třetí priorita)

```csharp
[EmailAddress]    // → EmailAddress.cshtml (type="email")
[Phone]           // → PhoneNumber.cshtml  (type="tel")
[Url]             // → Url.cshtml          (type="url")
```

---

## DataType enum – kompletní hodnoty

| Enum hodnota | Šablona | HTML výstup |
|---|---|---|
| `DataType.Custom` | vlastní řetězec | — |
| `DataType.DateTime` | `DateTime.cshtml` | `<input type="datetime-local">` |
| `DataType.Date` | `Date.cshtml` | `<input type="date">` |
| `DataType.Time` | `Time.cshtml` | `<input type="time">` |
| `DataType.Duration` | `Duration.cshtml` (pokud existuje) | text |
| `DataType.PhoneNumber` | `PhoneNumber.cshtml` | `<input type="tel">` |
| `DataType.Currency` | `Currency.cshtml` | `<input type="number" step="any">` |
| `DataType.Text` | `Text.cshtml` → `String.cshtml` | `<input type="text">` |
| `DataType.Html` | `Html.cshtml` | `<textarea class="html">` |
| `DataType.MultilineText` | `MultilineText.cshtml` | `<textarea>` |
| `DataType.EmailAddress` | `EmailAddress.cshtml` | `<input type="email">` |
| `DataType.Password` | `Password.cshtml` | `<input type="password">` |
| `DataType.Url` | `Url.cshtml` | `<input type="url">` |
| `DataType.ImageUrl` | `ImageUrl.cshtml` (pokud existuje) | text |
| `DataType.CreditCard` | `CreditCard.cshtml` (pokud existuje) | text |
| `DataType.PostalCode` | `PostalCode.cshtml` | `<input class="short">` |
| `DataType.Upload` | `Upload.cshtml` | `<input type="file">` |

> **Zdroj:** `learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.datatype`

---

## CLR typ → šablona (automatická volba)

| C# typ | Hledaná šablona | Výsledek |
|--------|----------------|----------|
| `string` | `String.cshtml` | text input |
| `bool` | `Boolean.cshtml` | checkbox |
| `bool?` | `Boolean.cshtml` | tri-state dropdown |
| `int`, `Int32` | `Int32.cshtml` | number input |
| `long`, `Int64` | `Int64.cshtml` | number input |
| `decimal` | `Decimal.cshtml` | text (formát 0.00) |
| `float` | `Single.cshtml` | number input |
| `DateTime` | `DateTime.cshtml` | datetime-local |
| `TimeSpan` | `TimeSpan.cshtml` ⚠️ je třeba vytvořit | text |
| `byte[]` | `HiddenInput.cshtml` (base64) | hidden |
| Enum typy | `Enum.cshtml` → `String.cshtml` | select |
| Komplexní typ | `{TypeName}.cshtml` → `Object.cshtml` | iterace |
| `IEnumerable<T>` | `Collection.cshtml` | iterace |

> ⚠️ `TimeSpan` nemá vestavěnou šablonu. V projektu [Altairis.ReP](https://github.com/ridercz/ReP) je `Time.cshtml` který castuje `TimeSpan` na `hh:mm`. Viz [04-property-templates.md](04-property-templates.md).

---

## Atributy pro metadata (label, popisek, pořadí)

### [Display]

```csharp
[Display(
    Name = "E-mail",                   // text labelu
    Description = "Pracovní e-mail",   // popis pod polem
    Prompt = "vas@firma.cz",           // HTML placeholder
    Order = 2,                          // pořadí vlastnosti v Object.cshtml
    GroupName = "Kontakty",             // seskupení (collapsible group)
    AutoGenerateField = false           // = [ScaffoldColumn(false)]
)]
public string Email { get; set; }
```

> S `Altairis.ConventionalMetadataProviders` **nepotřebujete** `[Display]` – metadata se načítají z `Display.resx` dle konvence jméno vlastnosti. Viz [06-project-setup.md](06-project-setup.md).

### [ScaffoldColumn(false)]

```csharp
[ScaffoldColumn(false)]
public List<SelectListItem> CategoryOptions { get; }  // datasource pro dropdown – vynecháno z formuláře
```

- Nastaví `ShowForEdit = false`
- Vlastnost **kompletně vynechána** z iterace v `Object.cshtml`
- Rozdíl od `[HiddenInput]`: hidden input hodnotu pošle s formulářem, ScaffoldColumn ne

---

## Atributy pro formátování hodnot

### [DisplayFormat]

```csharp
[DisplayFormat(
    DataFormatString = "{0:d}",         // formát pro zobrazení
    ApplyFormatInEditMode = true,       // aplikovat i v edit módu
    NullDisplayText = "—",             // text při null hodnotě
    ConvertEmptyStringToNull = true,    // "" → null
    HtmlEncode = false                  // neescapovat HTML (pro HTML obsah)
)]
public DateTime ReleaseDate { get; set; }
```

---

## [HiddenInput] – skryté pole

```csharp
[HiddenInput]                          // <input type="hidden"> + zobrazí hodnotu jako text
[HiddenInput(DisplayValue = false)]    // pouze <input type="hidden">, nic nezobrazí
```

- `DisplayValue = false` nastaví `HideSurroundingHtml = true`
- Hodnota **se odesílá** s formulářem (rozdíl od `[ScaffoldColumn(false)]`)
- **Namespace:** `Microsoft.AspNetCore.Mvc` (ne DataAnnotations!)

---

## Validační atributy

```csharp
[Required]
[Required(AllowEmptyStrings = false)]

[MaxLength(100)]
[MinLength(3)]
[StringLength(100, MinimumLength = 3)]

[Range(0, 1440)]
[Range(typeof(TimeSpan), "00:00:00", "23:59:59")]

[EmailAddress]
[Phone]
[Url]
[CreditCard]
[RegularExpression(@"^\d{5}$")]
[Compare("ConfirmPassword")]
[FileExtensions(Extensions = "jpg,png,gif")]
[AllowedValues("A", "B", "C")]    // .NET 8+
[DeniedValues("X", "Y")]          // .NET 8+
```

### Automatické [Required] pro non-nullable typy

S `<Nullable>enable</Nullable>`:
- `string` (bez `?`) → implicitní `[Required]`
- `int`, `DateTime`, `bool` → implicitní `[Required]` (value types)

S `Altairis.ConventionalMetadataProviders` se automaticky přidává `[Required]` i pro `int`, `DateTime`, `TimeSpan` – bez explicitního atributu.

---

## Rychlé mapování pole → atribut

| Chci pole | Použij atribut |
|-----------|---------------|
| Textový input | (žádný – default pro string) |
| Víceřádkový text | `[DataType(DataType.MultilineText)]` |
| Email | `[EmailAddress]` |
| Telefon | `[Phone]` |
| URL | `[Url]` |
| Datum | `[DataType(DataType.Date)]` |
| Datum a čas | `[DataType(DataType.DateTime)]` nebo žádný |
| Čas (TimeSpan) | `[DataType(DataType.Time)]` |
| Heslo | `[DataType(DataType.Password)]` |
| Barva | `[UIHint("Color")]` nebo vlastní `[Color]` |
| Markdown | `[DataType("Markdown")]` nebo vlastní `[Markdown]` |
| HTML editor | `[DataType(DataType.Html)]` |
| PSČ | `[DataType(DataType.PostalCode)]` |
| Nahrání souboru | `[DataType(DataType.Upload)]` |
| Skryté pole (posílá se) | `[HiddenInput(DisplayValue = false)]` |
| Vynechat ze scaffoldu | `[ScaffoldColumn(false)]` |
| Checkbox | `bool` (default) |
| Tri-state dropdown | `bool?` (default) |
| Dropdown ze seznamu | vlastní `[Select]` + `Select.cshtml` |
| Měna | `[DataType(DataType.Currency)]` |

---

## Kombinace atributů – reálné příklady (z [Altairis.ReP](https://github.com/ridercz/ReP))

```csharp
// Povinný text s max délkou
[Required, MaxLength(100)]
public string Name { get; set; } = string.Empty;

// Volitelný email
[EmailAddress, MaxLength(200)]
public string? Email { get; set; }

// Datum s date pickerem
[DataType(DataType.Date)]
public DateTime StartDate { get; set; } = DateTime.Today;

// Barva (custom atribut z Altairis.ValidationToolkit)
[Required, Color]
public string BackgroundColor { get; set; } = "#ffffff";

// Markdown editor
[DataType("Markdown")]
public string? Instructions { get; set; }

// Seskupení
[Display(GroupName = "Vizuální nastavení", Order = 10)]
[DataType("Markdown")]
public string? Notes { get; set; }

// Skrytý ID pro edit formulář
[HiddenInput(DisplayValue = false)]
public int Id { get; set; }

// Datový zdroj pro dropdown – vynechat z formuláře
[ScaffoldColumn(false)]
public IEnumerable<SelectListItem> CategoryList { get; } = new List<SelectListItem>();
```

---

## Související soubory

- [01-core-concepts.md](01-core-concepts.md) – jak engine vybírá šablonu
- [04-property-templates.md](04-property-templates.md) – implementace šablon
- [05-custom-templates.md](05-custom-templates.md) – vlastní DataType atributy
- [06-project-setup.md](06-project-setup.md) – ConventionalMetadataProviders (Display.resx)
- [examples/InputModel-full.cs](examples/InputModel-full.cs) – kompletní příklad InputModel
