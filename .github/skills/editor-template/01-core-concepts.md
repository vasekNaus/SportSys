# 01 – Jak EditorTemplates fungují

> **Navigace:** [← Skill](SKILL.md) | [Atributy →](02-data-annotations.md) | [Object.cshtml →](03-object-template.md)

---

## Princip

EditorTemplates jsou Razor `.cshtml` soubory v speciální složce `EditorTemplates/`, které **přepisují** způsob, jakým `Html.EditorFor()` renderuje konkrétní typy nebo vlastnosti. Místo psaní HTML formuláře ručně deklarujete metadata přímo na DTO třídě a engine vygeneruje formulář automaticky.

```
DTO třída s atributy  →  @Html.EditorFor()  →  engine vybere šablonu  →  HTML
```

---

## Tok zpracování

```
@Html.EditorFor(m => this.Model.Input)          ← volání v Razor stránce
    │
    ▼
Object.cshtml                                    ← šablona pro komplexní typ
    │  foreach (var prop in ModelMetadata.Properties.Where(p => p.ShowForEdit))
    │
    ├── Html.Editor("Name")          → String.cshtml → HtmlInput.cshtml
    │                                               → <input type="text">
    ├── Html.Editor("Email")         → EmailAddress.cshtml → HtmlInput.cshtml
    │                                               → <input type="email">
    ├── Html.Editor("BirthDate")     → Date.cshtml
    │                                               → <input type="date">
    ├── Html.Editor("IsActive")      → Boolean.cshtml
    │                                               → <input type="checkbox">
    ├── Html.Editor("Notes")         → MultilineText.cshtml
    │                                               → <textarea>
    └── Html.Editor("Instructions")  → Markdown.cshtml     ← vlastní šablona
                                                    → <textarea class="markdown">
```

---

## Pořadí výběru šablony (Template Resolution Order)

Engine hledá šablonu v tomto přesném pořadí – **první nalezená vyhrává**:

```
1.  Explicitní jméno v @Html.EditorFor(m => m.Prop, "MojeTemplate")
2.  [UIHint("MojeTemplate")]           ← nejvyšší priorita u atributů
3.  [DataType("Markdown")]             ← vlastní řetězcové jméno
4.  DataType enum → název              ← DataType.Date → "Date"
5.  Jméno CLR typu                     ← "String", "Int32", "Boolean", "DateTime"
6.  Pro Enum:                          "Enum" → "String"
7.  Komplexní typ:                     jde nahoru hierarchií (BaseClass.Name...)
8.  IEnumerable → "Collection"
9.  "Object"                           ← finální fallback pro komplexní typy
```

> **Zdroj:** `TemplateRenderer.cs:GetViewNames()` v ASP.NET Core source

Pro každý kandidát engine hledá fyzický soubor:
1. Relativně k aktuální stránce: `EditorTemplates/{jméno}.cshtml`
2. `Pages/Shared/EditorTemplates/{jméno}.cshtml`
3. `Views/Shared/EditorTemplates/{jméno}.cshtml`
4. Vestavěná C# implementace (`DefaultEditorTemplates.cs`) – fallback

---

## Umístění složky EditorTemplates

| App typ | Doporučené umístění |
|---------|---------------------|
| **Razor Pages** | `Pages/Shared/EditorTemplates/` |
| **MVC (sdílené)** | `Views/Shared/EditorTemplates/` |
| **MVC (per-controller)** | `Views/{ControllerName}/EditorTemplates/` |

> ⚠️ Projekt [Altairis.ReP](https://github.com/ridercz/ReP) používá `Pages/EditorTemplates/` (ne `Shared/`). Funguje, protože engine hledá i relativně od executing page. Pro nové projekty doporučuji `Pages/Shared/EditorTemplates/` pro explicitnost.

---

## Vestavěné šablony (bez .cshtml souboru)

Tyto šablony jsou implementovány v C# v `DefaultEditorTemplates.cs` a fungují i bez fyzického souboru:

```
String, Boolean, Decimal, Int32, DateTime
MultilineText, Password, EmailAddress, PhoneNumber, Url
HiddenInput, Object, Collection
Byte, SByte, Int16, Int64, UInt16, UInt32, UInt64, Single
Date, DateTime, Time (přes DataType)
IFormFile, IEnumerable<IFormFile>
```

Vlastní `.cshtml` soubor **přepíše** vestavěnou implementaci.

---

## ViewData dostupné v každé šabloně

```cshtml
@* Hodnota vlastnosti *@
ViewData.Model                              → aktuální hodnota (object)
ViewData.TemplateInfo.FormattedModelValue   → formátovaná hodnota (respektuje [DisplayFormat])

@* Metadata vlastnosti *@
ViewData.ModelMetadata.DisplayName          → z [Display(Name=...)] nebo z .resx konvence
ViewData.ModelMetadata.Description          → z [Display(Description=...)] nebo z .resx
ViewData.ModelMetadata.Placeholder          → z [Display(Prompt=...)] nebo z .resx
ViewData.ModelMetadata.IsRequired           → true když [Required]
ViewData.ModelMetadata.IsNullableValueType  → true pro bool?, int?,...
ViewData.ModelMetadata.HideSurroundingHtml  → true pro [HiddenInput(DisplayValue=false)]
ViewData.ModelMetadata.DataTypeName         → z [DataType(...)]
ViewData.ModelMetadata.PropertyName         → jméno vlastnosti jako string
ViewData.ModelMetadata.ModelType            → CLR typ System.Type
ViewData.ModelMetadata.IsComplexType        → true pro složité typy
ViewData.ModelMetadata.ShowForEdit          → false při [ScaffoldColumn(false)]

@* Kontext formuláře *@
ViewData.TemplateInfo.HtmlFieldPrefix       → prefix pro HTML name atribut
ViewData.TemplateInfo.GetFullHtmlFieldName(string.Empty) → plné jméno pole
ViewData.TemplateInfo.TemplateDepth         → hloubka zanořování (1 = top-level)

@* Pomocné metody *@
Html.GenerateIdFromName(fieldName)          → generuje id z name atributu
```

---

## Předávání vlastních dat do šablony

### Metoda 1: `additionalViewData` parametr

```cshtml
@Html.EditorFor(model => model.Notes, additionalViewData: new {
    additionalCssClass = "wide-textarea",
    rows = 10
})
```

V šabloně:
```cshtml
@{
    var cssClass = ViewData["additionalCssClass"] as string ?? string.Empty;
    var rows = (int?)ViewData["rows"] ?? 5;
}
```

### Metoda 2: Nastavení ViewData před delegací (template chaining)

```cshtml
@* Color.cshtml – nastaví type a deleguje na HtmlInput.cshtml *@
@{
    ViewData["type"] = "color";
}
<partial name="HtmlInput.cshtml" />
```

---

## Html.EditorFor vs Html.Editor

| Metoda | Kde použít | Poznámka |
|--------|-----------|----------|
| `Html.EditorFor(m => m.Input)` | Razor stránka | Expression lambda, strong-typed |
| `Html.Editor("PropertyName")` | Uvnitř Object.cshtml | String, volání per-vlastnost |
| `Html.EditorFor(m => m.Prop, "Template")` | Razor stránka | Explicitní šablona |
| `Html.EditorFor(m => m.Prop, additionalViewData: new {...})` | Razor stránka | Předání dat do šablony |

> ⚠️ Uvnitř `Object.cshtml` se vždy používá `Html.Editor("PropertyName")`, ne `Html.EditorFor()`.

---

## Minimální příklad fungujícího systému

**InputModel:**
```csharp
public class InputModel {
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime ValidFrom { get; set; } = DateTime.Today;
}
```

**Razor stránka:**
```cshtml
<form method="post">
    @Html.EditorFor(m => this.Model.Input)
    <input type="submit" value="Uložit" />
</form>
```

**Object.cshtml** (minimální verze):
```cshtml
@{
    this.Layout = string.Empty;
    foreach (var prop in ViewData.ModelMetadata.Properties.Where(p => p.ShowForEdit)) {
        <div>
            @Html.Label(prop.PropertyName)<br />
            @Html.Editor(prop.PropertyName)
            @Html.ValidationMessage(prop.PropertyName)
        </div>
    }
}
```

Výsledek: dvě pole s labely a validací, bez jediného řádku HTML v Razor stránce.

---

## Související soubory

- [03-object-template.md](03-object-template.md) – kompletní implementace Object.cshtml
- [04-property-templates.md](04-property-templates.md) – šablony pro jednotlivé typy
- [02-data-annotations.md](02-data-annotations.md) – jaké atributy triggery jakou šablonu
