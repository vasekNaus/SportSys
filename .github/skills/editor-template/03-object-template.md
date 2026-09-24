# 03 – Object.cshtml – klíčová šablona

> **Navigace:** [← Atributy](02-data-annotations.md) | [Property templates →](04-property-templates.md) | [Skill](SKILL.md)

Object.cshtml je **nejdůležitější šablona** celého systému. Použije se automaticky pro komplexní typy (InputModel). Iteruje všechny vlastnosti modelu a pro každou vygeneruje label + editor + validaci.

---

## Verze A – Jednoduchá

Vhodná pro většinu admin stránek. Plochá struktura, booleans mají checkbox vlevo od labelu.

```cshtml
@{
    this.Layout = "_Layout.cshtml";   // minimální layout: @RenderBody()

    foreach (var prop in ViewData.ModelMetadata.Properties.Where(metadata => metadata.ShowForEdit)) {

        if (prop.IsComplexType) {
            // Vnořený objekt → fieldset s rekurzivním voláním
            <fieldset>
                <legend>@prop.GetDisplayName()</legend>
                @if (!string.IsNullOrWhiteSpace(prop.Description)) {
                    <p class="description">@prop.Description</p>
                }
                @Html.Editor(prop.PropertyName)
            </fieldset>

        } else if (prop.HideSurroundingHtml) {
            // Hidden input ([HiddenInput(DisplayValue=false)]) → bez wrapperu
            @Html.Editor(prop.PropertyName)

        } else if (prop.ModelType.Equals(typeof(bool))) {
            // Boolean → checkbox vlevo od labelu
            <p>
                @Html.Editor(prop.PropertyName)
                @Html.Label(prop.PropertyName)
                @if (!string.IsNullOrWhiteSpace(prop.Description)) {
                    <span class="description">@prop.Description</span>
                }
                @Html.ValidationMessage(prop.PropertyName)
            </p>

        } else {
            // Standardní pole → label nad inputem
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

**Zdroj:** [Altairis.ReP](https://github.com/ridercz/ReP) – `Altairis.ReP.Web/Pages/EditorTemplates/Object.cshtml`

---

## Verze B – S groupováním

Pokročilá verze s podporou `[Display(GroupName="...")]` a collapsible skupin přes `<details>`. Doporučuji pro formuláře s mnoha poli.

```cshtml
@using Microsoft.AspNetCore.Mvc.ModelBinding
@using System.ComponentModel.DataAnnotations
@{
    this.Layout = string.Empty;

    // Různé chování pro top-level vs zanořený objekt
    if (ViewData.TemplateInfo.TemplateDepth == 1) {
        RenderGroupedProperties(ViewData.ModelMetadata.Properties);
    } else {
        <div class="editor-complex-field">
            @{ RenderGroupedProperties(ViewData.ModelMetadata.Properties); }
        </div>
    }

    void RenderGroupedProperties(ModelPropertyCollection properties) {
        // Seskup vlastnosti dle [Display(GroupName="...")]
        var groupedProperties =
            from mp in properties
            where mp.ShowForEdit
            orderby mp.Order
            let p = mp.ContainerType?.GetProperty(mp.PropertyName ?? string.Empty)
            let a = p?.GetCustomAttributes(true).OfType<DisplayAttribute>().FirstOrDefault()
            group p by a?.GroupName into g
            select new { Name = g.Key, PropertyNames = g.Select(x => x?.Name) };

        foreach (var group in groupedProperties) {
            var propsInGroup = properties.Where(p => group.PropertyNames.Contains(p.PropertyName));

            if (string.IsNullOrEmpty(group.Name)) {
                // Bez skupiny → renderuj přímo
                RenderProperties(propsInGroup);
            } else {
                // Se skupinou → collapsible <details>
                <details>
                    <summary>@group.Name</summary>
                    @{ RenderProperties(propsInGroup); }
                </details>
            }
        }
    }

    void RenderProperties(IEnumerable<ModelMetadata> properties) {
        foreach (var prop in properties.Where(p => p.ShowForEdit).OrderBy(p => p.Order)) {

            if (prop.IsComplexType) {
                <div class="editor-label">@Html.Label(prop.PropertyName)</div>
                @Html.Editor(prop.PropertyName)

            } else if (prop.HideSurroundingHtml) {
                @Html.Editor(prop.PropertyName)

            } else if (prop.ModelType.Equals(typeof(bool))) {
                <div class="editor-field-checkbox">
                    @Html.Editor(prop.PropertyName) @Html.Label(prop.PropertyName)
                    @Html.ValidationMessage(prop.PropertyName)
                </div>

            } else {
                <div class="editor-label">@Html.Label(prop.PropertyName)</div>
                <div class="editor-field">
                    @Html.Editor(prop.PropertyName)
                    @Html.ValidationMessage(prop.PropertyName)
                </div>
            }
        }
    }
}
```

**Zdroj:** [Altairis.RazorPages.EditorTemplates](https://github.com/ridercz/Altairis.RazorPages.EditorTemplates)

---

## Klíčové vlastnosti ModelMetadata v Object.cshtml

| Vlastnost | Typ | Popis |
|-----------|-----|-------|
| `prop.ShowForEdit` | `bool` | `false` při `[ScaffoldColumn(false)]` nebo `[Display(AutoGenerateField=false)]` |
| `prop.HideSurroundingHtml` | `bool` | `true` při `[HiddenInput(DisplayValue=false)]` |
| `prop.IsComplexType` | `bool` | `true` pro třídy (ne primitiva, ne string) |
| `prop.IsNullableValueType` | `bool` | `true` pro `bool?`, `int?`, ... |
| `prop.ModelType` | `Type` | CLR typ vlastnosti |
| `prop.PropertyName` | `string?` | Jméno vlastnosti (pro `Html.Editor()`) |
| `prop.GetDisplayName()` | `string` | Label text (z atributů nebo z .resx) |
| `prop.Description` | `string?` | Popisný text (z `[Display(Description=...)]` nebo z .resx) |
| `prop.Order` | `int` | Pořadí dle `[Display(Order=...)]` |
| `prop.DataTypeName` | `string?` | Jméno DataType (pro `[DataType("Markdown")]` → `"Markdown"`) |

---

## TemplateDepth – ochrana proti rekurzi

`ViewData.TemplateInfo.TemplateDepth` udává hloubku zanořování:
- `1` = top-level (volání z Razor stránky)
- `2+` = zanořený objekt (rekurzivní volání z Object.cshtml)

Vestavěná Object šablona za hloubkou 1 renderuje pouze text (`GetSimpleDisplayText()`). Vlastní Object.cshtml může toto přepsat a přidat `div.editor-complex-field` wrapper místo ořezání.

```cshtml
@* Bezpečné chování při zanořování: *@
if (ViewData.TemplateInfo.TemplateDepth > 3) {
    // Zastavit rekurzi
    @Html.DisplayFor(m => m)
    return;
}
```

---

## _Layout.cshtml pro EditorTemplates

Šablony nastavují `this.Layout = "_Layout.cshtml"` nebo `this.Layout = string.Empty`. Tento soubor musí existovat ve složce `EditorTemplates/`:

```cshtml
@* EditorTemplates/_Layout.cshtml – minimální layout *@
@RenderBody()
```

Důvod: bez explicitního nastavení by šablona zdědila `_ViewStart.cshtml` a renderovala by plnou HTML stránku.

> ⚠️ **Kritické:** Každá šablona musí mít buď `this.Layout = "_Layout.cshtml"` nebo `this.Layout = string.Empty`. Jinak dojde k layout inheritance a šablona vygeneruje `<html>` uvnitř formuláře.

---

## Kdy použít verzi A vs B

| Situace | Doporučení |
|---------|-----------|
| Standardní admin formulář (< 10 polí) | Verze A (jednoduchá) |
| Formulář s mnoha poli (> 10) | Verze B (grouping) |
| Chcete collapsible skupiny | Verze B s `[Display(GroupName=...)]` |
| Chcete vnořené objekty s vizuálním oddělením | Verze B (přidá border) |
| Jen základní funkčnost | Verze A |

---

## Kompletní příklady

- [examples/Object-simple.cshtml](examples/Object-simple.cshtml) – verze A
- [examples/Object-grouped.cshtml](examples/Object-grouped.cshtml) – verze B

---

## Související soubory

- [04-property-templates.md](04-property-templates.md) – šablony pro jednotlivé typy
- [01-core-concepts.md](01-core-concepts.md) – jak engine vybírá Object.cshtml
- [09-gotchas.md](09-gotchas.md) – layout inheritance, TemplateDepth problémy
