# 04 – Property Templates – implementace šablon

> **Navigace:** [← Object.cshtml](03-object-template.md) | [Vlastní šablony →](05-custom-templates.md) | [Skill](SKILL.md)

Šablony pro jednotlivé typy vlastností. Vycházejí ze vzoru projektu [Altairis.ReP](https://github.com/ridercz/ReP), kompletní implementaci najdete také v [Altairis.RazorPages.EditorTemplates](https://github.com/ridercz/Altairis.RazorPages.EditorTemplates).

---

## _Layout.cshtml – minimální layout šablon

**Povinný soubor.** Umístit do `EditorTemplates/_Layout.cshtml`.

```cshtml
@RenderBody()
```

Každá šablona musí nastavit `this.Layout = "_Layout.cshtml"` nebo `this.Layout = string.Empty`, jinak zdědí layout stránky.

---

## HtmlInput.cshtml – sdílená base šablona

Centrální šablona pro všechny `<input>` elementy. Ostatní šablony nastavují `ViewData["type"]` a delegují sem.

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

**Přijímá z ViewData:**
- `ViewData["type"]` – HTML type atribut (text, email, tel, url, color, number, file)
- `ViewData["additionalCssClass"]` – volitelná CSS třída navíc

---

## Jednoduché šablony delegující na HtmlInput

```cshtml
@* String.cshtml *@
@{ ViewData["type"] = "text"; }
<partial name="HtmlInput.cshtml" />

@* Text.cshtml *@
<partial name="String.cshtml" />

@* EmailAddress.cshtml *@
@{ ViewData["type"] = "email"; }
<partial name="HtmlInput.cshtml" />

@* PhoneNumber.cshtml *@
@{ ViewData["type"] = "tel"; }
<partial name="HtmlInput.cshtml" />

@* Url.cshtml *@
@{ ViewData["type"] = "url"; }
<partial name="HtmlInput.cshtml" />

@* Color.cshtml *@
@{ ViewData["type"] = "color"; }
<partial name="HtmlInput.cshtml" />

@* Upload.cshtml – type="file" *@
@{ ViewData["type"] = "file"; }
<partial name="HtmlInput.cshtml" />

@* Number.cshtml *@
@{ ViewData["type"] = "number"; }
<partial name="HtmlInput.cshtml" />

@* Int32.cshtml, Int64.cshtml, Byte.cshtml, SByte.cshtml, Single.cshtml, UInt32.cshtml, UInt64.cshtml *@
<partial name="Number.cshtml" />
```

---

## Date.cshtml – datum

Triggery: `[DataType(DataType.Date)]` na `DateTime` vlastnosti.

```cshtml
@{
    this.Layout = "_Layout.cshtml";

    var value = string.Empty;
    if (ViewData.Model != null) {
        var dtVal = (DateTime)ViewData.Model;
        if (dtVal > DateTime.MinValue) {
            value = dtVal.ToString("yyyy-MM-dd");
        }
    }

    var htmlAttributes = new {
        type = "date",
        @class = "textbox",
        placeholder = ViewData.ModelMetadata.Placeholder
    };
}
@Html.TextBox("", value, htmlAttributes)
```

---

## DateTime.cshtml – datum a čas

Trigger: `DateTime` bez explicitního `[DataType(DataType.Date)]`.

```cshtml
@{
    this.Layout = "_Layout.cshtml";

    var value = string.Empty;
    if (ViewData.Model != null) {
        var dtVal = (DateTime)ViewData.Model;
        if (dtVal > DateTime.MinValue) {
            value = dtVal.ToString("yyyy-MM-ddTHH:mm:ss");
        }
    }

    var htmlAttributes = new {
        type = "datetime-local",
        @class = "textbox"
    };
}
@Html.TextBox("", value, htmlAttributes)
```

---

## Time.cshtml – čas (TimeSpan)

Trigger: `[DataType(DataType.Time)]`. Nutné pro `TimeSpan`, který nemá vestavěnou šablonu.

```cshtml
@{
    this.Layout = "_Layout.cshtml";

    var value = string.Empty;
    if (ViewData.Model != null) {
        var tsVal = (TimeSpan)ViewData.Model;
        value = tsVal.ToString(@"hh\:mm");
    }

    var htmlAttributes = new {
        type = "time",
        @class = "textbox"
    };
}
@Html.TextBox("", value, htmlAttributes)
```

---

## Boolean.cshtml – zaškrtávací políčko nebo tri-state

Trigger: `bool` nebo `bool?` (automaticky dle CLR typu).

```cshtml
@{
    this.Layout = "_Layout.cshtml";

    bool? value = null;
    if (ViewData.Model != null) {
        value = Convert.ToBoolean(ViewData.Model, System.Globalization.CultureInfo.InvariantCulture);
    }

    if (ViewData.ModelMetadata.IsNullableValueType) {
        // bool? → tri-state dropdown
        var triStateValues = new List<SelectListItem> {
            new SelectListItem { Text = "Nenastaveno", Value = string.Empty, Selected = !value.HasValue },
            new SelectListItem { Text = "Ano",         Value = "true",        Selected = value.HasValue && value.Value },
            new SelectListItem { Text = "Ne",          Value = "false",       Selected = value.HasValue && !value.Value },
        };
        @Html.DropDownList("", triStateValues)
    } else {
        // bool → checkbox
        @Html.CheckBox("", value ?? false)
    }
}
```

> ⚠️ V `Object.cshtml` musí být speciální handling pro `bool` – checkbox se renderuje **vlevo od labelu** (checkbox → label), ne nad ním.

---

## Decimal.cshtml – desetinné číslo

Trigger: `decimal` CLR typ.

```cshtml
@{
    this.Layout = "_Layout.cshtml";

    object formattedValue;
    if (ViewData.TemplateInfo.FormattedModelValue == Model) {
        // Aplikuj formát pouze pokud není vlastní DisplayFormat
        formattedValue = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            "{0:0.00}",
            Model);
    } else {
        formattedValue = ViewData.TemplateInfo.FormattedModelValue;
    }
}
@Html.TextBox("", formattedValue, new { @class = "textbox" })
```

---

## MultilineText.cshtml – víceřádkový text

Trigger: `[DataType(DataType.MultilineText)]`.

```cshtml
@{
    this.Layout = "_Layout.cshtml";
}
@Html.TextArea("", ViewData.TemplateInfo.FormattedModelValue.ToString())
```

---

## Markdown.cshtml – Markdown editor

Trigger: `[DataType("Markdown")]` nebo vlastní `[Markdown]` atribut.

```cshtml
@{
    this.Layout = "_Layout.cshtml";
}
<span class="control-icons"><i class="fa-brands fa-markdown" title="Markdown"></i></span>
@Html.TextArea("", ViewData.TemplateInfo.FormattedModelValue.ToString())
```

> Alternativa bez Font Awesome – CSS background s inline SVG (viz [09-gotchas.md § CSS styling](09-gotchas.md)):

```cshtml
@{
    this.Layout = "_Layout.cshtml";
}
@Html.TextArea("", ViewData.TemplateInfo.FormattedModelValue.ToString(), new { @class = "markdown" })
```

S CSS:
```css
textarea.markdown {
    background-image: url("data:image/svg+xml,...");  /* Markdown M↓ ikona */
    background-position: right top;
    background-repeat: no-repeat;
    background-size: 25px;
    font-family: monospace;
}
```

---

## Html.cshtml – HTML editor

Trigger: `[DataType(DataType.Html)]`.

```cshtml
@{
    this.Layout = "_Layout.cshtml";
}
@Html.TextArea("", ViewData.TemplateInfo.FormattedModelValue.ToString(), 0, 0, new { @class = "html" })
```

---

## Password.cshtml – heslo se show/hide

Trigger: `[DataType(DataType.Password)]`.

```cshtml
@{
    this.Layout = "_Layout.cshtml";

    var passwordId = Html.GenerateIdFromName(
        ViewData.TemplateInfo.GetFullHtmlFieldName(string.Empty));
    var checkboxId = "Hide_" + passwordId;
    var jsCode = $"document.getElementById('{passwordId}').type = this.checked ? 'password' : 'text';";
}
@Html.Password("", ViewData.TemplateInfo.FormattedModelValue, new { style = "margin-bottom: 1em" })
<br />
<input id="@checkboxId" type="checkbox" onclick="@jsCode" checked="checked" />
<label for="@checkboxId">Skrýt heslo při psaní</label>
```

---

## HiddenInput.cshtml – skryté pole

Trigger: `[HiddenInput]` nebo `[HiddenInput(DisplayValue=false)]`.

```cshtml
@{
    this.Layout = "_Layout.cshtml";

    // byte[] → base64 pro hidden input
    object? modelValue;
    if (ViewData.Model is byte[] byteArray) {
        modelValue = Convert.ToBase64String(byteArray);
    } else {
        modelValue = ViewData.TemplateInfo.FormattedModelValue;
    }
}
@* Zobrazení hodnoty (pouze když DisplayValue=true, tj. HideSurroundingHtml=false) *@
@if (!Html.ViewContext.ViewData.ModelMetadata.HideSurroundingHtml) {
    <text>@ViewData.TemplateInfo.FormattedModelValue</text>
}
@Html.Hidden("", modelValue)
```

---

## Collection.cshtml – kolekce

Trigger: `IEnumerable<T>`.

```cshtml
@{
    this.Layout = "_Layout.cshtml";

    var originalPrefix = ViewData.TemplateInfo.HtmlFieldPrefix;
    if (Model is System.Collections.IEnumerable items) {
        int index = 0;
        foreach (var item in items) {
            ViewData.TemplateInfo.HtmlFieldPrefix = $"{originalPrefix}[{index}]";
            @Html.EditorFor(_ => item)
            index++;
        }
        ViewData.TemplateInfo.HtmlFieldPrefix = originalPrefix;
    }
}
```

---

## Kompletní inventář šablon

| Soubor | Trigger |
|--------|---------|
| `_Layout.cshtml` | — (minimální layout) |
| `HtmlInput.cshtml` | Sdílená base pro všechny `<input>` |
| `Object.cshtml` | Komplexní typ (InputModel) |
| `Collection.cshtml` | `IEnumerable<T>` |
| `String.cshtml` | `string` |
| `Text.cshtml` | `[DataType(DataType.Text)]` |
| `EmailAddress.cshtml` | `[EmailAddress]` / `[DataType(DataType.EmailAddress)]` |
| `PhoneNumber.cshtml` | `[Phone]` / `[DataType(DataType.PhoneNumber)]` |
| `Url.cshtml` | `[Url]` / `[DataType(DataType.Url)]` |
| `Color.cshtml` | `[Color]` (vlastní atribut) |
| `Upload.cshtml` | `[DataType(DataType.Upload)]` |
| `Number.cshtml` | Číselné typy (přes Int32 atd.) |
| `Int32.cshtml`, `Int64.cshtml`, `Byte.cshtml`... | CLR integer typy |
| `Decimal.cshtml` | `decimal` |
| `Boolean.cshtml` | `bool` / `bool?` |
| `Date.cshtml` | `[DataType(DataType.Date)]` |
| `DateTime.cshtml` | `DateTime` bez DataType |
| `Time.cshtml` | `[DataType(DataType.Time)]` / `TimeSpan` |
| `MultilineText.cshtml` | `[DataType(DataType.MultilineText)]` |
| `Html.cshtml` | `[DataType(DataType.Html)]` |
| `Markdown.cshtml` | `[DataType("Markdown")]` |
| `Password.cshtml` | `[DataType(DataType.Password)]` |
| `HiddenInput.cshtml` | `[HiddenInput]` |

---

## Kompletní příklady

- [examples/HtmlInput.cshtml](examples/HtmlInput.cshtml) – sdílená base šablona

---

## Související soubory

- [03-object-template.md](03-object-template.md) – Object.cshtml
- [05-custom-templates.md](05-custom-templates.md) – vlastní šablony (Markdown, Slider, Select)
- [09-gotchas.md](09-gotchas.md) – TimeSpan, layout inheritance
