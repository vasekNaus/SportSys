# 08 – Validace a client-side scripty

> **Navigace:** [← Admin vzory](07-admin-patterns.md) | [Gotchas →](09-gotchas.md) | [Skill](SKILL.md)

---

## Jak funguje validace s EditorTemplates

```
InputModel atributy → ConventionalMetadataProviders → ModelMetadata.ValidatorMetadata
    ↓
Razor tag helpers / Html.Editor() → generují data-val-* HTML atributy
    ↓
jquery.validate.unobtrusive → parsuje data-val-* → client-side validace
    ↓
POST → ModelState.IsValid (server-side validace)
```

---

## Nutné scripty

```cshtml
@* Pages/Shared/_ValidationScriptsPartial.cshtml *@
<script src="~/lib/jquery/dist/jquery.min.js"></script>
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
```

Vložení do Razor stránky přes `@section Scripts`:

```cshtml
@* Na konci každé stránky s formulářem *@
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

Nebo přímo v `_Layout.cshtml` (globálně pro všechny admin stránky):

```cshtml
@* V _Layout.cshtml před </body> *@
<script src="~/lib/jquery/dist/jquery.min.js"></script>
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
@await RenderSectionAsync("Scripts", required: false)
```

---

## Jak DataAnnotations generují client-side validaci

### Příklad – Email pole s [Required] a [EmailAddress]

InputModel:
```csharp
[Required]
[EmailAddress, MaxLength(200)]
public string Email { get; set; } = string.Empty;
```

Generovaný HTML (`<input asp-for>` nebo z EmailAddress.cshtml):
```html
<input type="email"
       data-val="true"
       data-val-required="Pole E-mail je povinné."
       data-val-email="Pole E-mail musí obsahovat platnou e-mailovou adresu."
       data-val-maxlength="Pole E-mail může mít maximálně 200 znaků."
       data-val-maxlength-max="200"
       id="Input_Email" name="Input.Email" value="" />
<span class="field-validation-valid"
      data-valmsg-for="Input.Email"
      data-valmsg-replace="true"></span>
```

`jquery.validate.unobtrusive` automaticky parsuje tyto `data-val-*` atributy a registruje pravidla.

---

## Validační atributy → data-val-* atributy

| C# atribut | Generuje data-val-* |
|---|---|
| `[Required]` | `data-val-required` |
| `[MaxLength(n)]` | `data-val-maxlength` + `data-val-maxlength-max` |
| `[MinLength(n)]` | `data-val-minlength` + `data-val-minlength-min` |
| `[StringLength(max, Min=min)]` | `data-val-length` + max/min |
| `[Range(min, max)]` | `data-val-range` + min/max |
| `[EmailAddress]` | `data-val-email` |
| `[Phone]` | `data-val-phone` |
| `[Url]` | `data-val-url` |
| `[RegularExpression(pattern)]` | `data-val-regex` + pattern |
| `[Compare("OtherProp")]` | `data-val-equalto` |
| `[CreditCard]` | `data-val-creditcard` |

---

## Validation summary ve formuláři

```cshtml
@* Zobrazí všechny chyby (model + jednotlivá pole) *@
<div asp-validation-summary="All"></div>

@* Zobrazí pouze model-level chyby (ne per-field) *@
<div asp-validation-summary="ModelOnly"></div>

@* Skryje validační summary *@
<div asp-validation-summary="None"></div>
```

Doporučená pozice – těsně před submit tlačítkem:
```cshtml
<form method="post">
    @Html.EditorFor(m => this.Model.Input)
    <footer>
        <div asp-validation-summary="All"></div>    ← zde
        <input type="submit" value="Uložit" />
    </footer>
</form>
```

---

## Server-side validace v OnPost

```csharp
public async Task<IActionResult> OnPostAsync() {
    // ModelState.IsValid zkontroluje všechny DataAnnotations atributy
    if (!this.ModelState.IsValid) {
        // Vrátí stránku s chybami
        return this.Page();
    }

    // Vlastní validace (business rules)
    if (Input.EndDate < Input.StartDate) {
        this.ModelState.AddModelError(
            "Input.EndDate",
            "Datum konce musí být po datu začátku.");
        return this.Page();
    }

    // ... zpracování ...
    return this.RedirectToPage("Index");
}
```

---

## IValidatableObject – komplexní validace na úrovni objektu

```csharp
public class InputModel : IValidatableObject {
    [DataType(DataType.Time)]
    public TimeSpan OpeningTime { get; set; }

    [DataType(DataType.Time)]
    public TimeSpan ClosingTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (ClosingTime <= OpeningTime) {
            yield return new ValidationResult(
                "Čas zavření musí být po čase otevření.",
                new[] { nameof(ClosingTime) });
        }
    }
}
```

---

## ConventionalMetadataProviders a validační zprávy

S `options.SetConventionalMetadataProviders<Display, Validation>()`:

1. Validační atribut bez vlastní zprávy (např. `[Required]`)
2. Provider hledá klíč `Required` (nebo `Email_Required`, `Model_Email_Required`) v `Validation.resx`
3. Zpráva `"Pole {0} je povinné."` je nastavena s `{0}` = display name z `Display.resx`
4. Výsledek: `"Pole E-mail je povinné."`

**Žádné hardcoded ErrorMessage v atributech!**

---

## Minimální Validation.resx

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="Required"><value>Pole {0} je povinné.</value></data>
  <data name="MaxLength"><value>Pole {0} může mít maximálně {1} znaků.</value></data>
  <data name="MinLength"><value>Pole {0} musí mít alespoň {1} znaků.</value></data>
  <data name="StringLength"><value>Pole {0} musí mít od {2} do {1} znaků.</value></data>
  <data name="Range"><value>Pole {0} musí být v rozsahu od {1} do {2}.</value></data>
  <data name="EmailAddress"><value>Pole {0} musí obsahovat platnou e-mailovou adresu.</value></data>
  <data name="Phone"><value>Pole {0} musí obsahovat platné telefonní číslo.</value></data>
  <data name="Url"><value>Pole {0} musí obsahovat platnou URL adresu.</value></data>
  <data name="Compare"><value>Pole {0} a {1} se musí shodovat.</value></data>
  <data name="RegularExpression"><value>Pole {0} musí odpovídat vzoru {1}.</value></data>
  <data name="CreditCard"><value>Pole {0} musí obsahovat platné číslo kreditní karty.</value></data>
  <data name="FileExtensions"><value>Pole {0} může obsahovat pouze soubory s příponou: {1}</value></data>
</root>
```

---

## Dynamické formuláře (AJAX)

Pokud přidáváte formulář dynamicky (AJAX), musíte re-parsovat validaci:

```javascript
// Po vložení nového formuláře do DOM
const newForm = document.getElementById('my-ajax-form');

// Odeber existující validaci a re-parsuj
$(newForm).removeData('validator').removeData('unobtrusiveValidation');
$.validator.unobtrusive.parse(newForm);
```

---

## Instalace knihoven (LibMan)

```json
// libman.json
{
  "version": "1.0",
  "defaultProvider": "cdnjs",
  "libraries": [
    {
      "library": "jquery@3.7.1",
      "destination": "wwwroot/lib/jquery"
    },
    {
      "library": "jquery-validate@1.19.5",
      "destination": "wwwroot/lib/jquery-validation"
    },
    {
      "library": "jquery-validation-unobtrusive@4.0.0",
      "destination": "wwwroot/lib/jquery-validation-unobtrusive"
    }
  ]
}
```

---

## Related soubory

- [06-project-setup.md](06-project-setup.md) – ConventionalMetadataProviders, Validation.resx
- [07-admin-patterns.md](07-admin-patterns.md) – vzory formulářů
- [09-gotchas.md](09-gotchas.md) – problémy s validací
