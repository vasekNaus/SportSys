---
name: editor-template
description: "Instructs Copilot how to implement automatic admin form generation in ASP.NET Core Razor Pages using EditorTemplates, DataAnnotations, and Altairis.ConventionalMetadataProviders. Reference project: Altairis.ReP.Web (ASP.NET Core 10)."
---

# Skill: EditorTemplates pro administrační stránky

Tento skill popisuje, jak v ASP.NET Core Razor Pages automaticky generovat HTML formuláře z datových atributů na DTO/InputModel třídách pomocí mechanismu **EditorTemplates**.

**Referenční implementace:** [Altairis.ReP](https://github.com/ridercz/ReP) a [Altairis.RazorPages.EditorTemplates](https://github.com/ridercz/Altairis.RazorPages.EditorTemplates)

---

## Klíčový princip

Jeden řádek Razor kódu vygeneruje celý formulář:

```cshtml
<form method="post">
    @Html.EditorFor(m => this.Model.Input)
    <footer>
        <div asp-validation-summary="All"></div>
        <input type="submit" value="Uložit" />
    </footer>
</form>
```

Engine přečte metadata InputModel třídy (DataAnnotations atributy + `.resx` soubory) a automaticky vygeneruje správné input typy, labely, popisky a validaci.

---

## Pořadí výběru šablony

```
1. Explicitní jméno: @Html.EditorFor(m => m.Prop, "MojeTemplate")
2. [UIHint("MojeTemplate")]        ← nejvyšší priorita atributů
3. [DataType("Markdown")]          ← vlastní řetězcové jméno → Markdown.cshtml
4. DataType enum → název           ← DataType.Date → Date.cshtml
5. Jméno CLR typu                  ← "String", "Int32", "Boolean", "DateTime"
6. Pro Enum: "Enum" → "String"
7. Komplexní typ: jméno třídy → Object.cshtml  ← finální fallback
8. IEnumerable → Collection.cshtml
```

Soubor šablony se hledá v:
1. `Pages/Shared/EditorTemplates/{jméno}.cshtml`
2. `Pages/EditorTemplates/{jméno}.cshtml` (jako v ReP projektu)
3. Vestavěná C# implementace (fallback)

---

## DataAnnotations → výběr šablony

| Atribut na vlastnosti | Použitá šablona | HTML výstup |
|---|---|---|
| *(žádný)* pro `string` | `String.cshtml` | `<input type="text">` |
| `[EmailAddress]` | `EmailAddress.cshtml` | `<input type="email">` |
| `[Phone]` | `PhoneNumber.cshtml` | `<input type="tel">` |
| `[Url]` | `Url.cshtml` | `<input type="url">` |
| `[DataType(DataType.Date)]` | `Date.cshtml` | `<input type="date">` |
| `[DataType(DataType.DateTime)]` | `DateTime.cshtml` | `<input type="datetime-local">` |
| `[DataType(DataType.Time)]` | `Time.cshtml` | `<input type="time">` |
| `[DataType(DataType.Password)]` | `Password.cshtml` | `<input type="password">` + toggle |
| `[DataType(DataType.MultilineText)]` | `MultilineText.cshtml` | `<textarea>` |
| `[DataType(DataType.Html)]` | `Html.cshtml` | `<textarea class="html">` |
| `[DataType("Markdown")]` | `Markdown.cshtml` | `<textarea>` + ikona |
| `[DataType(DataType.Upload)]` | `Upload.cshtml` | `<input type="file">` |
| `[DataType(DataType.Currency)]` | `Currency.cshtml` | `<input type="number" step="any">` |
| `[DataType(DataType.PostalCode)]` | `PostalCode.cshtml` | krátký textbox |
| `[HiddenInput(DisplayValue=false)]` | `HiddenInput.cshtml` | `<input type="hidden">` |
| `[UIHint("Color")]` | `Color.cshtml` | `<input type="color">` |
| *(žádný)* pro `bool` | `Boolean.cshtml` | `<input type="checkbox">` |
| *(žádný)* pro `bool?` | `Boolean.cshtml` | tri-state `<select>` |
| *(žádný)* pro `int`/`long`/... | `Int32.cshtml` → `Number.cshtml` | `<input type="number">` |
| *(žádný)* pro `decimal` | `Decimal.cshtml` | textbox (formát 0.00) |
| `[ScaffoldColumn(false)]` | — vynecháno zcela | žádný HTML výstup |

---

## Metadata atributy (label, popisek, pořadí)

```csharp
[Display(
    Name = "E-mail",                  // text labelu
    Description = "Pracovní e-mail",  // popis pod polem
    Prompt = "vas@firma.cz",          // HTML placeholder
    Order = 2,                         // pořadí vlastnosti
    GroupName = "Kontakty"             // skupina (collapsible <details>)
)]
```

> S `Altairis.ConventionalMetadataProviders` **nepotřebujete** `[Display]` – metadata se načítají z `Display.resx` dle konvence jméno vlastnosti. Viz sekce Konfigurace.

---

## Object.cshtml – klíčová šablona

Použije se pro komplexní typy (InputModel). Iteruje vlastnosti a generuje label+editor páry. **Musí existovat jako vlastní soubor** – vestavěná verze má omezení pro zanořené typy.

```cshtml
@{
    this.Layout = "_Layout.cshtml";   // EditorTemplates/_Layout.cshtml = @RenderBody()

    foreach (var prop in ViewData.ModelMetadata.Properties.Where(p => p.ShowForEdit)) {
        if (prop.IsComplexType) {
            <fieldset>
                <legend>@prop.GetDisplayName()</legend>
                @Html.Editor(prop.PropertyName)
            </fieldset>
        } else if (prop.HideSurroundingHtml) {
            @Html.Editor(prop.PropertyName)   // [HiddenInput(DisplayValue=false)]
        } else if (prop.ModelType.Equals(typeof(bool))) {
            <p>
                @Html.Editor(prop.PropertyName)  // checkbox VLEVO od labelu
                @Html.Label(prop.PropertyName)
                @Html.ValidationMessage(prop.PropertyName)
            </p>
        } else {
            <p>
                @Html.Label(prop.PropertyName, prop.GetDisplayName() + ":")<br />
                @if (!string.IsNullOrWhiteSpace(prop.Description)) {
                    <span class="description">@prop.Description</span>
                }
                @Html.Editor(prop.PropertyName)
                @Html.ValidationMessage(prop.PropertyName)
            </p>
        }
    }
}
```

**Klíčové vlastnosti `prop` (ModelMetadata):**
- `prop.ShowForEdit` → `false` při `[ScaffoldColumn(false)]`
- `prop.HideSurroundingHtml` → `true` při `[HiddenInput(DisplayValue=false)]`
- `prop.IsComplexType` → vnořený objekt (rekurzivní volání)
- `prop.ModelType.Equals(typeof(bool))` → speciální checkbox layout
- `prop.GetDisplayName()` → label z atributů nebo `.resx`
- `prop.Description` → popis pod polem

> ⚠️ Uvnitř Object.cshtml vždy `Html.Editor("PropertyName")` – **ne** `Html.EditorFor()`!

---

## HtmlInput.cshtml – sdílená base šablona

Ostatní šablony nastavují `ViewData["type"]` a delegují sem:

```cshtml
@{
    this.Layout = "_Layout.cshtml";
    var htmlAttributes = new {
        @class = string.Join(" ", "textbox", ViewData["additionalCssClass"]),
        type = ViewData["type"],
        placeholder = ViewData.ModelMetadata.Placeholder,
    };
}
@Html.TextBox("", ViewData.TemplateInfo.FormattedModelValue, htmlAttributes)
```

Delegující šablony:
```cshtml
@* String.cshtml *@  @{ ViewData["type"] = "text"; }  <partial name="HtmlInput.cshtml" />
@* EmailAddress.cshtml *@  @{ ViewData["type"] = "email"; }  <partial name="HtmlInput.cshtml" />
@* Color.cshtml *@  @{ ViewData["type"] = "color"; }  <partial name="HtmlInput.cshtml" />
@* Int32.cshtml *@  <partial name="Number.cshtml" />   (Number nastaví type="number")
```

---

## Date / Time šablony (DateTime a TimeSpan)

```cshtml
@* Date.cshtml *@
@{
    this.Layout = "_Layout.cshtml";
    var value = ViewData.Model != null
        ? ((DateTime)ViewData.Model).ToString("yyyy-MM-dd")
        : string.Empty;
}
@Html.TextBox("", value, new { type = "date", @class = "textbox" })

@* Time.cshtml – TimeSpan nemá vestavěnou šablonu, nutno vytvořit *@
@{
    this.Layout = "_Layout.cshtml";
    var value = ViewData.Model != null
        ? ((TimeSpan)ViewData.Model).ToString(@"hh\:mm")
        : string.Empty;
}
@Html.TextBox("", value, new { type = "time", @class = "textbox" })
```

---

## Konfigurace projektu

### Program.cs

```csharp
builder.Services.AddRazorPages(options => {
    options.Conventions.AuthorizeFolder("/Admin", "IsAdministrator");
})
.AddMvcOptions(options => {
    // Automatické display names + validační zprávy z .resx souborů
    options.SetConventionalMetadataProviders<Display, Validation>();
});
```

NuGet: `Altairis.ConventionalMetadataProviders`

### Pages/_ViewImports.cshtml

```cshtml
@namespace MyApp.Pages
@using MyApp.Resources
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

### Display.resx – konvence klíčů

```xml
<data name="Email"><value>E-mail</value></data>
<data name="MaximumReservationTime"><value>Maximální čas rezervace</value></data>
<data name="MaximumReservationTime_Description"><value>v minutách, 0 = neomezeno</value></data>
<data name="Email_Placeholder"><value>vas@email.cz</value></data>
```

Klíč `Email` funguje pro **všechny vlastnosti nazvané `Email`** v celé aplikaci (konvence od nejspecifičtějšího po generické).

### Validation.resx

```xml
<data name="Required"><value>Pole {0} je povinné.</value></data>
<data name="MaxLength"><value>Pole {0} může mít maximálně {1} znaků.</value></data>
<data name="Range"><value>Pole {0} musí být v rozsahu od {1} do {2}.</value></data>
```

---

## Vzor Create admin stránky

**Create.cshtml:**
```cshtml
@page
@model MyApp.Pages.Admin.Items.CreateModel
<form method="post">
    @Html.EditorFor(m => this.Model.Input)
    <footer>
        <div asp-validation-summary="All"></div>
        <input type="submit" value="Uložit" />
        <a asp-page="Index" class="button secondary">Zrušit</a>
    </footer>
</form>
@section Scripts { <partial name="_ValidationScriptsPartial" /> }
```

**Create.cshtml.cs:**
```csharp
public class CreateModel : PageModel {
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [DataType("Markdown")]
        public string? Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime ValidFrom { get; set; } = DateTime.Today;

        public bool IsActive { get; set; } = true;
    }

    public async Task<IActionResult> OnPostAsync() {
        if (!this.ModelState.IsValid) return this.Page();
        // ... uložení ...
        return this.RedirectToPage("Index");
    }
}
```

**Vzor Edit stránky:** Stejný pattern + `[HiddenInput(DisplayValue = false)]` pro Id + handler `OnPostDeleteAsync`.

---

## Vlastní DataType atributy

```csharp
// Vlastní atribut → EditorTemplates/Markdown.cshtml
public class MarkdownAttribute() : DataTypeAttribute("Markdown") { }

// Atribut s parametry pro Slider šablonu
public class SliderAttribute(int min, int max, int step = 1) : DataTypeAttribute("Slider") {
    public int Min { get; } = min;
    public int Max { get; } = max;
    public int Step { get; } = step;
}
```

Šablona čte parametry atributu přes reflexi:
```cshtml
@{
    T? getAttr<T>() where T : Attribute {
        var pi = ViewData.ModelExplorer.Container.Model.GetType()
            .GetProperty(ViewData.ModelExplorer.Metadata.PropertyName ?? "");
        return pi?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
    }
    var sliderAttr = getAttr<SliderAttribute>();
}
```

---

## Kritická pravidla (musí být splněna vždy)

1. **Každá šablona musí nastavit layout** – `this.Layout = "_Layout.cshtml"` nebo `this.Layout = string.Empty`. Bez toho šablona zdědí layout stránky a vygeneruje `<html>` uvnitř formuláře.

2. **`EditorTemplates/_Layout.cshtml` musí existovat** – s obsahem pouze `@RenderBody()`.

3. **Uvnitř Object.cshtml**: `Html.Editor("PropertyName")`, ne `Html.EditorFor()`.

4. **Boolean vlastnosti** musí být ošetřeny před obecnou větví v Object.cshtml (checkbox vlevo od labelu).

5. **`[ScaffoldColumn(false)]` vs `[HiddenInput(DisplayValue=false)]`**: ScaffoldColumn = vynecháno z formuláře (hodnota se neposílá), HiddenInput = skryté ale odesílané pole.

6. **TimeSpan** nemá vestavěnou šablonu – nutno vytvořit `Time.cshtml` s explicitním castem.

---

## Kdy použít EditorFor vs přímé Tag Helpers

| Situace | Doporučení |
|---------|-----------|
| Standardní CRUD formulář | `@Html.EditorFor(m => this.Model.Input)` |
| Nestandardní layout (2 sloupce, inline) | Přímé `<input asp-for>` Tag Helpers |
| Radio buttony / checkbox list | Přímé Tag Helpers nebo vlastní šablona |
| AJAX dynamické formuláře | Přímé Tag Helpers |

---

## Podrobná dokumentace (v tomto skill adresáři)

| Soubor | Obsah |
|--------|-------|
| [01-core-concepts.md](01-core-concepts.md) | Princip, ViewData, resolution order |
| [02-data-annotations.md](02-data-annotations.md) | Kompletní reference atributů |
| [03-object-template.md](03-object-template.md) | Object.cshtml (2 varianty) |
| [04-property-templates.md](04-property-templates.md) | Všechny property šablony s kódem |
| [05-custom-templates.md](05-custom-templates.md) | Vlastní DataType atributy |
| [06-project-setup.md](06-project-setup.md) | Program.cs, .resx konfigurace |
| [07-admin-patterns.md](07-admin-patterns.md) | Create/Edit vzory + kompletní InputModel |
| [08-validation.md](08-validation.md) | Client-side validace, jquery.validate |
| [09-gotchas.md](09-gotchas.md) | 13 nejčastějších problémů + debug checklist |
| [examples/](examples/) | Funkční ukázky kódu připravené k použití |
| [_research/original-research.md](_research/original-research.md) | Originální výzkumná zpráva |
