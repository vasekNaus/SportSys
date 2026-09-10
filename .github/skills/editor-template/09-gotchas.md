# 09 – Gotchas – časté problémy a řešení

> **Navigace:** [← Validace](08-validation.md) | [Skill](SKILL.md)

---

## 1. Šablona zdědila layout stránky

**Příznak:** Šablona generuje `<html><body>...</body></html>` místo HTML fragmentu. Formulář obsahuje vnořené HTML stránky.

**Příčina:** Šablona zdědila layout přes `_ViewStart.cshtml` (`Layout = "_Layout"`).

**Řešení:** Na začátek každé šablony přidat:

```cshtml
@* Varianta A – použij minimální template layout *@
@{ this.Layout = "_Layout.cshtml"; }   // _Layout.cshtml obsahuje pouze @RenderBody()

@* Varianta B – žádný layout *@
@{ this.Layout = string.Empty; }
```

A vytvořit `EditorTemplates/_Layout.cshtml`:
```cshtml
@RenderBody()
```

---

## 2. Šablona se nenachází – fallback na vestavěnou

**Příznak:** Vlastní `.cshtml` šablona se nepoužívá, framework použije vestavěnou implementaci.

**Příčiny a řešení:**

| Příčina | Řešení |
|---------|--------|
| Špatné jméno souboru | `[EmailAddress]` → `EmailAddress.cshtml` (ne `Email.cshtml`) |
| Špatná složka | Ověřit přesnou cestu: `Pages/Shared/EditorTemplates/` nebo `Pages/EditorTemplates/` |
| Case-sensitive (Linux/macOS) | `EditorTemplates` musí mít přesně tato velká/malá písmena |
| Atribut chybí na vlastnosti | Ověřit, že `[DataType("Markdown")]` je na správné vlastnosti |
| Špatný DataType string | `[DataType("Markdownx")]` ≠ `Markdown.cshtml` |

**Debug tip:** Přidejte dočasně text do šablony:
```cshtml
@* MojeTemplate.cshtml *@
TEMPLATE_DEBUG: MojeTemplate
```
Pokud se text neobjeví, šablona se nenašla.

---

## 3. ScaffoldColumn(false) vs HiddenInput – záměna

| Atribut | ShowForEdit | Renderuje HTML | Hodnota v POST |
|---------|-------------|----------------|----------------|
| `[ScaffoldColumn(false)]` | `false` | ❌ Nic | ❌ Nepošle |
| `[HiddenInput(DisplayValue=false)]` | `true` | ✅ `<input type="hidden">` | ✅ Pošle |
| `[HiddenInput]` | `true` | ✅ hidden + text | ✅ Pošle |

**Typická chyba:** Použití `[ScaffoldColumn(false)]` pro Id v Edit formuláři → Id se nepošle → `Input.Id = 0` v OnPost.

**Správně pro Id v Edit:**
```csharp
[HiddenInput(DisplayValue = false)]  // ← správně: pošle hidden input
public int Id { get; set; }

// [ScaffoldColumn(false)]           // ← ŠPATNĚ: Id se nepošle s formulářem
```

---

## 4. Html.Editor vs Html.EditorFor uvnitř Object.cshtml

**Příznak:** Kompilátor nebo runtime chyba v `Object.cshtml`.

**Správně uvnitř Object.cshtml:**
```cshtml
@Html.Editor(prop.PropertyName)          ← string jméno vlastnosti
```

**Špatně (nefunguje):**
```cshtml
@Html.EditorFor(m => m.PropertyName)     ← lambda nad ViewData, ne nad stránkovým modelem
```

---

## 5. TimeSpan nemá vestavěnou šablonu

**Příznak:** `TimeSpan` vlastnost se renderuje jako textový input s hodnotou `00:00:00` místo `type="time"` inputu.

**Řešení:** Vytvořit `Time.cshtml` která castuje model:

```cshtml
@{
    this.Layout = "_Layout.cshtml";
    var value = string.Empty;
    if (ViewData.Model != null) {
        var tsVal = (TimeSpan)ViewData.Model;
        value = tsVal.ToString(@"hh\:mm");
    }
}
@Html.TextBox("", value, new { type = "time", @class = "textbox" })
```

A označit vlastnost:
```csharp
[DataType(DataType.Time)]
public TimeSpan OpeningTime { get; set; }
```

---

## 6. Collection template a indexované fieldy

**Příznak:** Při kolekci v InputModel se hodnoty správně nezobrazují nebo se při POST nenamapují zpět.

**Příčina:** `HtmlFieldPrefix` musí být správně nastaven pro každý prvek kolekce.

**Řešení v Collection.cshtml:**
```cshtml
@{
    var originalPrefix = ViewData.TemplateInfo.HtmlFieldPrefix;
    if (Model is System.Collections.IEnumerable items) {
        int index = 0;
        foreach (var item in items) {
            ViewData.TemplateInfo.HtmlFieldPrefix = $"{originalPrefix}[{index}]";
            @Html.EditorFor(_ => item)
            index++;
        }
        ViewData.TemplateInfo.HtmlFieldPrefix = originalPrefix;  // ← obnovit!
    }
}
```

---

## 7. Zanořené objekty za depth 1 se renderují jako text

**Příznak:** Vnořený komplexní objekt (např. `AddressModel`) se zobrazuje jako plain text místo skupiny polí.

**Příčina:** Vestavěná `Object` šablona za depth 1 volá `GetSimpleDisplayText()`.

**Řešení:** Vlastní `Object.cshtml` která kontroluje `TemplateDepth`:

```cshtml
@if (ViewData.TemplateInfo.TemplateDepth == 1) {
    RenderProperties(ViewData.ModelMetadata.Properties);
} else {
    // Zanořený objekt – wrap do div a renderuj normálně
    <div class="editor-complex-field">
        @{ RenderProperties(ViewData.ModelMetadata.Properties); }
    </div>
}
```

---

## 8. Kolize klíčů ve ViewData additionalViewData

**Příznak:** Vlastní data předaná do šablony se ztratí nebo přepíší jiná data.

**Příčina:** Název vlastnosti v `additionalViewData` koliduje s vestavěným ViewData klíčem.

```cshtml
@* ŠPATNĚ – "type" přepíše interní nastavení šablony *@
@Html.EditorFor(m => m.Field, additionalViewData: new { type = "email" })

@* SPRÁVNĚ – použij unikátní prefix *@
@Html.EditorFor(m => m.Field, additionalViewData: new { editorType = "email" })
```

Nebezpečné názvy (vyhněte se jim): `type`, `class`, `id`, `name`, `value`, `Model`, `ViewData`.

---

## 9. Validace nefunguje po AJAX update

**Příznak:** Po dynamickém přidání formuláře přes AJAX jquery.validate nepracuje.

**Řešení:**
```javascript
function reInitValidation(formElement) {
    $(formElement)
        .removeData('validator')
        .removeData('unobtrusiveValidation');
    $.validator.unobtrusive.parse(formElement);
}

// Po AJAX:
fetch('/admin/create-partial')
    .then(r => r.text())
    .then(html => {
        document.getElementById('form-container').innerHTML = html;
        reInitValidation(document.getElementById('my-form'));
    });
```

---

## 10. Password pole se znovu nenaplní

**Příznak:** Po neúspěšném POST se password pole zobrazí prázdné (intentional chování).

**Příčina:** ASP.NET Core záměrně nevyplňuje password pole z modelu pro bezpečnost.

**Řešení (pokud je potřeba):** Nastavit AppContext switch:
```csharp
AppContext.SetSwitch("Microsoft.AspNetCore.Mvc.UsePasswordValue", true);
```

Nebo renderovat password pole mimo EditorFor a naplnit ho ručně.

---

## 11. Display.resx klíče nefungují pro vnořené modely

**Příznak:** Vlastnost `Street` na `AddressModel` pokazuje klíč `AddressModel_Street`, ale v Display.resx je jen `Street`.

**Příčina:** `ConventionalMetadataProviders` prohledává od nejspecifičtějšího ke generickému. Klíč `Street` FUNGUJE – je to platný fallback. Pokud přesto nefunguje:

1. Ověřit, že `Display.resx` je správně kompilovaný (Build Action: EmbeddedResource)
2. Ověřit, že `Display.Designer.cs` je aktuální (pravý klik na .resx → Run Custom Tool)
3. Ověřit registraci v Program.cs: `options.SetConventionalMetadataProviders<Display, Validation>()`

---

## 12. Boolean vlastnost – checkbox bez labelu nebo špatné pořadí

**Příznak:** Boolean pole má label nad checkboxem místo vedle něj, nebo naopak.

**Příčina:** `Object.cshtml` musí mít speciální handling pro `bool`:

```cshtml
@* SPRÁVNĚ: checkbox VLEVO od labelu *@
} else if (prop.ModelType.Equals(typeof(bool))) {
    <p>
        @Html.Editor(prop.PropertyName)   ← checkbox
        @Html.Label(prop.PropertyName)    ← label vpravo
        @Html.ValidationMessage(prop.PropertyName)
    </p>
}
```

```cshtml
@* ŠPATNĚ: label nad checkboxem (jako by to byl textový input) *@
} else {  ← Boolean skončí tady, label je nad inputem
    <p>
        @Html.Label(prop.PropertyName)<br />
        @Html.Editor(prop.PropertyName)
    </p>
}
```

**Řešení:** Zajistit, aby `Object.cshtml` testoval `prop.ModelType.Equals(typeof(bool))` PŘED obecnou větví.

---

## 13. EditorFor na vlastnost mimo InputModel

**Příznak:** `@Html.EditorFor(m => this.Model.SomeProp)` nefunguje správně nebo způsobuje chyby.

**Příčina:** `Html.EditorFor()` je určeno pro `BindProperty` InputModel nebo přímé property modelu, ne pro computed properties nebo properties bez `[BindProperty]`.

**Řešení:**
- Ověřit `[BindProperty]` na InputModel
- Pro display (ne edit) použít `@Html.DisplayFor()` nebo přímý Razor výstup

---

## Rychlý debug checklist

Pokud EditorTemplates nefungují správně:

```
□ Soubor šablony má správný název? (přesně dle DataType nebo CLR type)
□ Šablona je ve správné složce? (EditorTemplates/ s přesným case)
□ Šablona nastavuje Layout? (this.Layout = "_Layout.cshtml" nebo string.Empty)
□ Object.cshtml existuje a je správně napsaná? (ShowForEdit, HideSurroundingHtml)
□ Program.cs má SetConventionalMetadataProviders? (pokud používáte .resx)
□ Display.resx a Validation.resx jsou EmbeddedResource? (Build Action)
□ _ViewImports.cshtml importuje správné namespace a tag helpers?
□ Pro boolean: Object.cshtml testuje typeof(bool) před obecnou větví?
□ Pro HiddenInput v Edit: [HiddenInput(DisplayValue=false)] (ne [ScaffoldColumn(false)])?
```
