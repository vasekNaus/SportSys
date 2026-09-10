# 05 – Vlastní šablony a DataType atributy

> **Navigace:** [← Property templates](04-property-templates.md) | [Project setup →](06-project-setup.md) | [Skill](SKILL.md)

Jak vytvořit vlastní EditorTemplates pro specifické typy polí: Markdown, barevný picker, dropdown ze seznamu, slider, nebo šablonu pojmenovanou přímo po CLR typu.

---

## Vzor: vlastní DataType atribut

Nejjednodušší způsob jak vytvořit vlastní šablonu je zdědění z `DataTypeAttribute`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace MyApp.Attributes;

// Vlastní atribut → routes to EditorTemplates/Markdown.cshtml
public class MarkdownAttribute() : DataTypeAttribute("Markdown") { }
```

Použití:
```csharp
[Markdown]
public string Notes { get; set; } = string.Empty;

// Nebo přímo (bez vlastní třídy):
[DataType("Markdown")]
public string Notes { get; set; } = string.Empty;
```

**Výhoda vlastního atributu:** čistší kód v modelu, možnost přidat parametry, lépe čitelné.

---

## Vzor: atribut s parametry

Atribut může nést konfiguraci pro šablonu. Šablona ji čte přes reflexi.

### Definice atributu

```csharp
// Attributes/SliderAttribute.cs
public class SliderAttribute(int min, int max, int step = 1) : DataTypeAttribute("Slider") {
    public int Min { get; } = min;
    public int Max { get; } = max;
    public int Step { get; } = step;
    public string ExtraFieldSuffix { get; set; } = "Extra";
}
```

### Šablona čtoucí parametry přes reflexi

```cshtml
@* EditorTemplates/Slider.cshtml *@
@using MyApp.Attributes
@{
    this.Layout = string.Empty;

    // Pomocná funkce pro čtení atributu z property přes reflexi
    T? getPropertyAttribute<T>() where T : Attribute {
        var propertyName = this.ViewData.ModelExplorer.Metadata.PropertyName;
        if (propertyName == null) return null;
        var propertyInfo = this.ViewData.ModelExplorer.Container.Model
            .GetType().GetProperty(propertyName);
        return propertyInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
    }

    var sliderAttr = getPropertyAttribute<SliderAttribute>();

    var numberAttrs = new {
        type = "number",
        min = sliderAttr?.Min,
        max = sliderAttr?.Max,
        step = sliderAttr?.Step,
        oninput = "this.nextElementSibling.value = this.value",
        @class = "isextra"
    };
    var rangeAttrs = new {
        type = "range",
        min = sliderAttr?.Min,
        max = sliderAttr?.Max,
        step = sliderAttr?.Step,
        oninput = "this.previousElementSibling.value = this.value",
        @class = "hasextra"
    };
    var extraFieldSuffix = sliderAttr?.ExtraFieldSuffix ?? "Extra";
}
@* Dvě propojená pole: number + range slider *@
@Html.TextBox(extraFieldSuffix, ViewData.TemplateInfo.FormattedModelValue, numberAttrs)
@Html.TextBox("", ViewData.TemplateInfo.FormattedModelValue, rangeAttrs)
```

Použití:
```csharp
[Slider(1, 10)]
public int Priority { get; set; } = 5;

[Slider(0, 100, step: 5)]
public int Completion { get; set; }
```

**Zdroj:** [Altairis.RazorPages.EditorTemplates](https://github.com/ridercz/Altairis.RazorPages.EditorTemplates)

---

## Vzor: dropdown ze seznamu (Select)

Konvence: datový zdroj pro dropdown je sibling property s názvem `{PropertyName}List`.

### Atribut

```csharp
// Attributes/SelectAttribute.cs
public class SelectAttribute(string? listPropertyName = null) : DataTypeAttribute("Select") {
    // Volitelně přepsat konvenční jméno {PropertyName}List
    public string? ListPropertyName { get; } = listPropertyName;
}
```

### Šablona

```cshtml
@* EditorTemplates/Select.cshtml *@
@using MyApp.Attributes
@{
    this.Layout = string.Empty;

    T? getPropertyAttribute<T>() where T : Attribute {
        var propertyName = this.ViewData.ModelExplorer.Metadata.PropertyName;
        if (propertyName == null) return null;
        var propertyInfo = this.ViewData.ModelExplorer.Container.Model.GetType().GetProperty(propertyName);
        return propertyInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
    }

    var listItems = new List<SelectListItem>();
    var model = ViewData.ModelExplorer.Container.Model;

    if (model != null) {
        // Jméno list property: z atributu nebo konvence {PropertyName}List
        var listPropertyName = getPropertyAttribute<SelectAttribute>()?.ListPropertyName
            ?? $"{this.ViewData.ModelExplorer.Metadata.Name}List";

        var listPropertyInfo = model.GetType().GetProperty(listPropertyName);
        if (listPropertyInfo != null) {
            var result = listPropertyInfo.GetValue(model) as IEnumerable<SelectListItem>;
            if (result != null) listItems.AddRange(result);
        }
    }
}
@Html.DropDownList(string.Empty, listItems)
```

### Použití v InputModel

```csharp
public class InputModel {
    // Dropdown pole
    [Select]
    [Display(GroupName = "Kategorizace")]
    public int CategoryId { get; set; }

    // Datasource pro dropdown (konvence: CategoryId + List = CategoryIdList)
    [ScaffoldColumn(false)]           // ← NUTNÉ – nechceme renderovat jako pole
    public IEnumerable<SelectListItem> CategoryIdList { get; set; } = new List<SelectListItem>();

    // Nebo s vlastním názvem list property:
    [Select(listPropertyName: "AvailableCategories")]
    public int TypeId { get; set; }

    [ScaffoldColumn(false)]
    public IEnumerable<SelectListItem> AvailableCategories { get; set; } = new List<SelectListItem>();
}
```

**Zdroj:** [Altairis.RazorPages.EditorTemplates](https://github.com/ridercz/Altairis.RazorPages.EditorTemplates)

---

## Vzor: šablona pojmenovaná po CLR typu

Pro komplexní typ se šablona hledá dle jména CLR třídy. Toto umožňuje specialní layout pro určitý typ.

```cshtml
@* EditorTemplates/StreetModel.cshtml – auto-použita pro vlastnosti typu StreetModel *@
@model MyApp.Models.StreetModel
@{
    this.Layout = string.Empty;
}
@* Dvě pole inline: ulice (širší) + číslo (úzké) *@
<input asp-for="@Model.StreetName" class="hasextra" />
<input asp-for="@Model.StreetNumber" class="isextra" />
<span asp-validation-for="@Model.StreetName"></span>
<span asp-validation-for="@Model.StreetNumber"></span>
```

InputModel:
```csharp
public class AddressModel {
    public StreetModel Street { get; set; } = new();  // → StreetModel.cshtml automaticky
    public string City { get; set; } = string.Empty;
    [DataType(DataType.PostalCode)]
    public string Zip { get; set; } = string.Empty;
}
```

**Zdroj:** [Altairis.RazorPages.EditorTemplates](https://github.com/ridercz/Altairis.RazorPages.EditorTemplates)

---

## Vzor: vlastní barva (Color picker)

Využívá nativní `<input type="color">` HTML5.

### Varianta A – vlastní atribut (jako v Altairis.ValidationToolkit)

```csharp
// Předpokládáme, že [Color] z Altairis.ValidationToolkit nastavuje DataTypeName="Color"
// a existuje Color.cshtml šablona
[Required, Color]
public string BackgroundColor { get; set; } = "#ffffff";
```

```cshtml
@* EditorTemplates/Color.cshtml *@
@{
    ViewData["type"] = "color";
}
<partial name="HtmlInput.cshtml" />
```

### Varianta B – přes UIHint

```csharp
[UIHint("Color")]
public string ThemeColor { get; set; } = "#000000";
```

---

## Vzor: PSČ (PostalCode)

```cshtml
@* EditorTemplates/PostalCode.cshtml *@
@{
    this.Layout = string.Empty;
}
@Html.TextBox(string.Empty, ViewData.TemplateInfo.FormattedModelValue,
    new { @class = "text-box single-line short" })
```

CSS – `.short` třída omezí šířku:
```css
input.short { max-width: 8em; }
```

---

## Vzor: měna (Currency)

```cshtml
@* EditorTemplates/Currency.cshtml *@
@{
    this.Layout = string.Empty;
}
@Html.TextBox(string.Empty, ViewData.TemplateInfo.FormattedModelValue,
    new { type = "number", step = "any", @class = "text-box single-line short" })
```

---

## Předávání ViewData místo reflexe

Alternativa k reflexi pro šablony volanéz Razor stránky (ne z Object.cshtml):

```cshtml
@* Volání z Razor stránky s daty *@
@Html.EditorFor(m => m.Priority, additionalViewData: new {
    sliderMin = 1,
    sliderMax = 10,
    sliderStep = 1
})
```

```cshtml
@* EditorTemplates/Slider.cshtml – jednodušší verze bez reflexe *@
@{
    this.Layout = string.Empty;
    var min = ViewData["sliderMin"] ?? 0;
    var max = ViewData["sliderMax"] ?? 100;
    var step = ViewData["sliderStep"] ?? 1;
}
<input type="range" min="@min" max="@max" step="@step"
       name="@ViewData.TemplateInfo.GetFullHtmlFieldName(string.Empty)"
       value="@ViewData.TemplateInfo.FormattedModelValue" />
```

> ⚠️ Tento přístup nefunguje dobře při volání z `Object.cshtml` přes `Html.Editor(prop.PropertyName)`, protože tam nelze předat `additionalViewData`. Pro Object.cshtml použijte reflexi.

---

## Kompletní příklady

- [examples/MarkdownAttribute.cs](examples/MarkdownAttribute.cs) – vlastní DataType atribut

---

## Související soubory

- [04-property-templates.md](04-property-templates.md) – základní šablony
- [02-data-annotations.md](02-data-annotations.md) – atributy
- [01-core-concepts.md](01-core-concepts.md) – resolution order
