# 07 – Admin vzory (Create / Edit stránky)

> **Navigace:** [← Project setup](06-project-setup.md) | [Validace →](08-validation.md) | [Skill](SKILL.md)

Vzory pro typické administrační CRUD stránky. Vycházejí z projektu [Altairis.ReP](https://github.com/ridercz/ReP).

---

## Vzor A – Create stránka (nejjednodušší)

### Razor stránka – Create.cshtml

```cshtml
@page
@model MyApp.Pages.Admin.Items.CreateModel
@{ this.ViewBag.Title = "Vytvořit položku"; }

<h1>@this.ViewBag.Title</h1>

<form method="post">
    @Html.EditorFor(m => this.Model.Input)
    <footer>
        <div asp-validation-summary="All"></div>
        <input type="submit" value="Uložit" />
        <a asp-page="Index" class="button secondary">Zrušit</a>
    </footer>
</form>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

### Code-behind – Create.cshtml.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Pages.Admin.Items;

public class CreateModel : PageModel {
    private readonly IItemRepository _repo;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        [DataType("Markdown")]
        public string? Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ValidFrom { get; set; } = DateTime.Today;

        public bool IsActive { get; set; } = true;
    }

    public CreateModel(IItemRepository repo) {
        _repo = repo;
    }

    public async Task<IActionResult> OnPostAsync() {
        if (!this.ModelState.IsValid) return this.Page();

        await _repo.CreateAsync(new Item {
            Name = Input.Name,
            Description = Input.Description,
            ValidFrom = Input.ValidFrom,
            IsActive = Input.IsActive
        });

        return this.RedirectToPage("Index");
    }
}
```

**Zdroj:** [Altairis.ReP](https://github.com/ridercz/ReP) – `Altairis.ReP.Web/Pages/Admin/Resources/`

---

## Vzor B – Edit stránka (s Delete)

### Razor stránka – Edit.cshtml

```cshtml
@page "{id:int}"
@model MyApp.Pages.Admin.Items.EditModel
@{ this.ViewBag.Title = "Upravit položku"; }

<h1>@this.ViewBag.Title</h1>

<form method="post">
    @Html.EditorFor(m => this.Model.Input)
    <footer>
        <div asp-validation-summary="All"></div>
        <input type="submit" value="Uložit" />
        <a asp-page="Index" class="button secondary">Zrušit</a>

        @* Delete tlačítko s potvrzením *@
        <input type="submit"
               asp-page-handler="Delete"
               class="button danger"
               value="Smazat"
               onclick="return confirm('Opravdu smazat tuto položku?')" />
    </footer>
</form>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

### Code-behind – Edit.cshtml.cs

```csharp
public class EditModel : PageModel {
    private readonly IItemRepository _repo;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel {
        // Hidden ID – odesílá se s formulářem, nezobrazuje
        [HiddenInput(DisplayValue = false)]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        [DataType("Markdown")]
        public string? Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ValidFrom { get; set; }

        public bool IsActive { get; set; }
    }

    public EditModel(IItemRepository repo) {
        _repo = repo;
    }

    public async Task<IActionResult> OnGetAsync(int id) {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return this.NotFound();

        // Mapování entity na InputModel
        Input = new InputModel {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            ValidFrom = item.ValidFrom,
            IsActive = item.IsActive
        };

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync() {
        if (!this.ModelState.IsValid) return this.Page();

        await _repo.UpdateAsync(new Item {
            Id = Input.Id,
            Name = Input.Name,
            Description = Input.Description,
            ValidFrom = Input.ValidFrom,
            IsActive = Input.IsActive
        });

        return this.RedirectToPage("Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync() {
        await _repo.DeleteAsync(Input.Id);
        return this.RedirectToPage("Index");
    }
}
```

**Zdroj:** [Altairis.ReP](https://github.com/ridercz/ReP) – `Altairis.ReP.Web/Pages/Admin/Resources/`

---

## Kompletní InputModel – všechny typy polí

Reference pro agenta – jak deklarovat různé typy polí v jednom InputModel:

```csharp
public class InputModel {
    // ── Textová pole ─────────────────────────────────────────────────────────
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }                  // volitelný text

    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }                         // <textarea>

    [DataType("Markdown")]
    public string? MarkdownBody { get; set; }                  // Markdown editor

    [DataType(DataType.Html)]
    public string? HtmlContent { get; set; }                   // HTML editor

    // ── Kontaktní údaje ──────────────────────────────────────────────────────
    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }                         // type="email"

    [Phone, MaxLength(20)]
    public string? PhoneNumber { get; set; }                   // type="tel"

    [Url, MaxLength(500)]
    public string? Website { get; set; }                       // type="url"

    // ── Datum a čas ──────────────────────────────────────────────────────────
    [DataType(DataType.Date)]
    public DateTime ValidFrom { get; set; } = DateTime.Today;  // type="date"

    [DataType(DataType.DateTime)]
    public DateTime StartAt { get; set; } = DateTime.Now;      // type="datetime-local"

    [DataType(DataType.Time)]
    [Range(typeof(TimeSpan), "00:00:00", "23:59:59")]
    public TimeSpan OpeningTime { get; set; } = TimeSpan.Zero; // type="time"

    // ── Čísla ────────────────────────────────────────────────────────────────
    [Range(0, 1440)]
    public int MaxMinutes { get; set; }                        // type="number"

    [Range(0.0, 9999.99)]
    public decimal Price { get; set; }                         // formátované textbox

    // ── Barvy ────────────────────────────────────────────────────────────────
    [Required]
    [UIHint("Color")]                                           // type="color"
    public string ForegroundColor { get; set; } = "#000000";

    [Required]
    [UIHint("Color")]
    public string BackgroundColor { get; set; } = "#ffffff";

    // ── Boolean ──────────────────────────────────────────────────────────────
    public bool IsActive { get; set; } = true;                 // checkbox

    public bool? OptionalFlag { get; set; }                    // tri-state dropdown

    // ── Heslo ────────────────────────────────────────────────────────────────
    [DataType(DataType.Password), MinLength(8)]
    public string? NewPassword { get; set; }

    // ── Nahrání souboru ──────────────────────────────────────────────────────
    [DataType(DataType.Upload)]
    public IFormFile? Attachment { get; set; }

    // ── Skrytá pole ──────────────────────────────────────────────────────────
    [HiddenInput(DisplayValue = false)]
    public int Id { get; set; }                                // jen hidden, nezobrazuje

    [HiddenInput]
    public string? VersionTag { get; set; }                    // hidden + zobrazí hodnotu

    // ── Vynechat z formuláře (datasource pro dropdown) ────────────────────────
    [ScaffoldColumn(false)]
    public IEnumerable<SelectListItem> CategoryList { get; } = new List<SelectListItem>();

    // ── Seskupení a pořadí ───────────────────────────────────────────────────
    [Display(GroupName = "Kontaktní údaje", Order = 100)]
    [EmailAddress, MaxLength(200)]
    public string? ContactEmail { get; set; }

    [Display(GroupName = "Kontaktní údaje", Order = 101)]
    [Phone, MaxLength(20)]
    public string? ContactPhone { get; set; }
}
```

---

## Kdy použít EditorFor vs přímé Tag Helpers

### Použij `@Html.EditorFor()` když:
- Standardní CRUD formulář s jednoduchou strukturou
- Nechcete HTML v Razor stránce
- Chcete konzistentní vzhled napříč stránkami
- InputModel má méně než ~15 polí

### Použij přímé Tag Helpers (`<input asp-for>`) když:
- Nestandartní rozložení (2 sloupce, inline prvky)
- Radio buttony nebo checkbox list (bez custom šablony)
- Potřebujete přesnou kontrolu nad HTML atributy
- AJAX formuláře s dynamickými poli
- Pole vedle sebe bez wrapperu (např. jméno + příjmení na jednom řádku)

### Příklad přímých Tag Helpers (kde EditorFor nestačí)

```cshtml
@* Admin stránka s nestandardním layoutem *@
<form method="post">
    <p>
        <label asp-for="Input.UserName"></label>:<br />
        <input asp-for="Input.UserName" />
        <span asp-validation-for="Input.UserName" class="text-danger"></span>
    </p>

    @* Checkbox list – vlastní tag helper nebo ruční HTML *@
    <p>
        <label>Oprávnění:</label><br />
        @foreach (var role in Model.AvailableRoles) {
            <label>
                <input type="checkbox" name="Input.Roles" value="@role.Value"
                       checked="@(Model.Input.Roles.Contains(role.Value) ? "checked" : null)" />
                @role.Text
            </label>
        }
    </p>
</form>
```

**Zdroj:** [Altairis.ReP](https://github.com/ridercz/ReP) – `Altairis.ReP.Web/Pages/Admin/Users/`

---

## Pattern pro Display.resx při admin stránkách

Pro každý nový InputModel přidejte do `Display.resx`:

```xml
<!-- Pouze nové vlastnosti, které nemají obecné klíče -->
<data name="ValidFrom"><value>Platí od</value></data>
<data name="ValidFrom_Description"><value>datum začátku platnosti</value></data>
<data name="IsActive"><value>Tato položka je aktivní</value></data>
```

Obecné klíče (`Name`, `Email`, `Description`) nevyžadují opakování – fungují pro všechny modely.

---

## Kompletní příklady

- [examples/AdminCreate.cshtml](examples/AdminCreate.cshtml) – Create stránka
- [examples/AdminCreate.cshtml.cs](examples/AdminCreate.cshtml.cs) – Create code-behind
- [examples/InputModel-full.cs](examples/InputModel-full.cs) – kompletní InputModel

---

## Související soubory

- [03-object-template.md](03-object-template.md) – jak Object.cshtml iteruje InputModel
- [06-project-setup.md](06-project-setup.md) – konfigurace (Display.resx, Program.cs)
- [08-validation.md](08-validation.md) – client-side validace ve formulářích
- [09-gotchas.md](09-gotchas.md) – časté problémy
