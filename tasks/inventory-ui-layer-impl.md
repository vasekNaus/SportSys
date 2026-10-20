# Implementační plán: UI vrstva – Inventory modul

## Přehled

Tento dokument popisuje implementaci UI vrstvy modulu Inventory pro projekt SportSys.
Zadání je v souboru `.github/tasks/inventory-ui-layer.md`, popis modulu v `docs/modules/inventory.md`.

Rozsah první verze:
- Správa výrobců (Manufacturers)
- Správa umístění (Locations)
- Evidence výpůjček (Loans)

**Datová vrstva je hotová.** Migrace `AddInventoryModule` existuje. Žádné změny DB modelu v tomto úkolu.

---

## Technické konvence

### Závislosti

- `SportSys.Razor` závisí **výhradně** na `SportSys.Contract` – nikdy přímo na `SportSys.Database`
- Všechny DB přístupy jdou přes injektované servisy z `SportSys.Contract`
- Registrace servisů **výhradně** v `AddSportSysServices()` v `src/SportSys.Contract/ServiceCollectionExtensions.cs`

### DTO konvence

- Soubory: `src/SportSys.Contract/Models/Inventory/`
- Namespace: `SportSys.Contract.Models.Inventory`
- Validační atributy na DTO, ne v PageModelu
- `[Display(Name = "...")]` pro všechna pole zobrazovaná ve formuláři (generuje label text)
- `[Required(ErrorMessage = "...")]`, `[StringLength(...)]`, `[Url]` atd.
- Vzor: `IceRinkDto.cs` v `src/SportSys.Contract/Models/`

### Servis konvence

- Soubory: `src/SportSys.Contract/Services/`
- Namespace: `SportSys.Contract.Services`
- Konstruktor: injektuje `SportSysDbContext _db`
- Metody: async s `CancellationToken ct = default`
- Projekce do DTO přes `Select(e => new Dto { ... })` v LINQ dotazu – nikdy vracet DB entity
- Vzor: `IceRinkService.cs` v `src/SportSys.Contract/Services/`

### PageModel konvence

- Metody: `async Task<IActionResult> OnGetAsync()` / `async Task<IActionResult> OnPostAsync()`
- `[BindProperty]` pro vstup z formuláře
- `[BindProperty(SupportsGet = true)]` pro filtr (GET parametry)
- `[TempData] public string? StatusMessage { get; set; }` pro potvrzení po redirectu
- Žádná business logika v PageModelu – vše přes servis
- Vzor: `Edit.cshtml.cs` v `src/SportSys.Razor/Areas/sport/Pages/IceRink/`

### Razor Page HTML konvence

- CSS třídy z existujícího stylesheets:
  - `button` – primární tlačítko (červená)
  - `button secondary` – sekundární (modrý obrys, pro Zpět/Zrušit)
  - `button tertiary` – destruktivní (červený text, float right, pro Smazat)
  - `grid` – pro tabulky (`<table class="grid">`)
  - `textbox` – explicitní třída pro selecty a vstupy, kde automatický selektor nestačí
  - `infobox` – pro StatusMessage (potvrzení po uložení)
  - `field` – obalující div pro label+input ve filtrech
- Font Awesome ikony: `fa-solid fa-plus`, `fa-pen`, `fa-trash`, `fa-floppy-disk`, `fa-xmark`, `fa-magnifying-glass`, `fa-filter-circle-xmark`, `fa-arrow-left`, `fa-check` (fa-fw vždy)
- `asp-validation-summary="ModelOnly"` na začátku formuláře
- `<span asp-validation-for="..." class="field-validation-error"></span>` za každým vstupem
- `<partial name="_ValidationScriptsPartial" />` v `@section Scripts`
- `<input type="hidden" asp-for="Input.Id" />` pro Edit formuláře
- Vzor: `Edit.cshtml` a `Index.cshtml` v `src/SportSys.Razor/Areas/sport/Pages/IceRink/`

---

## Fáze 1: DTO objekty

Vytvořit soubory v `src/SportSys.Contract/Models/Inventory/`.

### 1.1 Manufacturer

Soubor: `Manufacturer.cs`

```csharp
namespace SportSys.Contract.Models.Inventory;

public class Manufacturer
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Název je povinný.")]
    [StringLength(200, ErrorMessage = "Název nesmí přesáhnout 200 znaků.")]
    [Display(Name = "Název")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "URL nesmí přesáhnout 500 znaků.")]
    [Url(ErrorMessage = "Zadejte platnou URL adresu.")]
    [Display(Name = "Web")]
    public string? Website { get; set; }

    [Display(Name = "Aktivní")]
    public bool IsActive { get; set; } = true;
}
```

Potřebuje `using System.ComponentModel.DataAnnotations;`.

### 1.2 Location + LocationListItem + LocationSelectItem

Soubor: `Location.cs`

```csharp
namespace SportSys.Contract.Models.Inventory;

public class Location
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Název je povinný.")]
    [StringLength(200, ErrorMessage = "Název nesmí přesáhnout 200 znaků.")]
    [Display(Name = "Název")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "Popis nesmí přesáhnout 500 znaků.")]
    [Display(Name = "Popis")]
    public string? Description { get; set; }

    [Display(Name = "Nadřazené umístění")]
    public int? ParentLocationId { get; set; }

    [Display(Name = "Aktivní")]
    public bool IsActive { get; set; } = true;
}

public class LocationListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? ParentLocationName { get; set; }
    public bool IsActive { get; set; }
}

public class LocationSelectItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

### 1.3 Loan modely

Soubor: `Loans.cs`

**Datový model – skupinová logika:**
- Entita `Loan` eviduje jedno zapůjčení jedné položky
- UI seskupuje záznamy `Loan` dle `(MemberId, LoanDate)` do logické výpůjčky
- Při vytvoření výpůjčky se vytvoří N záznamů `Loan` ve stejné transakci se shodným `LoanDate = DateOnly.FromDateTime(DateTime.Today)`
- Číslo výpůjčky = min(Id) v grupě, formát: `V-{Id:D5}`
- Stav skupiny:
  - `Aktivní`: žádná položka nemá `ReturnedDate`
  - `Částečně vráceno`: část položek má `ReturnedDate`
  - `Uzavřeno`: všechny položky mají `ReturnedDate != null` nebo `IsClosed = true`

```csharp
namespace SportSys.Contract.Models.Inventory;

// Řádek v přehledu výpůjček (skupina dle MemberId + LoanDate)
public class LoanListItem
{
    public int GroupId { get; set; }          // min(Loan.Id) v grupě
    public string LoanNumber { get; set; } = "";  // "V-{GroupId:D5}"
    public string MemberName { get; set; } = "";
    public DateOnly LoanDate { get; set; }
    public int ItemCount { get; set; }
    public int ReturnedCount { get; set; }
    public string Status { get; set; } = "";  // "Aktivní" / "Částečně vráceno" / "Uzavřeno"
}

// Hlavička detailu výpůjčky (jen čtení)
public class LoanDetail
{
    public int GroupId { get; set; }
    public string LoanNumber { get; set; } = "";
    public string MemberName { get; set; } = "";
    public DateOnly LoanDate { get; set; }
    public DateOnly? ExpectedReturnDate { get; set; }
    public string Status { get; set; } = "";
    public List<LoanDetailItem> Items { get; set; } = [];
}

// Řádek tabulky položek ve výpůjčce
public class LoanDetailItem
{
    public int LoanId { get; set; }           // Id záznamu Loan
    public string InventoryNumber { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public bool IsReturned { get; set; }
    public DateOnly? ReturnedDate { get; set; }
}

// Vstup pro vytvoření výpůjčky
public class CreateLoan
{
    [Required(ErrorMessage = "Vyberte člena.")]
    [Display(Name = "Člen")]
    public int? MemberId { get; set; }

    // InventoryNumbers = seznam inventárních čísel přidaných na stránce
    public List<string> InventoryNumbers { get; set; } = [];
}

// Výsledek vyhledání položky dle inventárního čísla (pro QR skener / AJAX lookup)
public class InventoryItemLookup
{
    public bool Found { get; set; }
    public bool IsAvailable { get; set; }
    public string? ErrorMessage { get; set; }
    public int InventoryItemId { get; set; }
    public string InventoryNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string CurrentLocationName { get; set; } = "";
}

// Položka selectu pro výběr člena
public class MemberSelectItem
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
}

// Filtr na přehledu výpůjček
public class LoanFilter
{
    [Display(Name = "Člen")]
    public string? MemberName { get; set; }

    [Display(Name = "Pouze aktivní")]
    public bool ActiveOnly { get; set; }

    [Display(Name = "Datum od")]
    public DateOnly? DateFrom { get; set; }

    [Display(Name = "Datum do")]
    public DateOnly? DateTo { get; set; }
}
```

---

## Fáze 2: Aplikační servisy

### 2.1 ManufacturerService

Soubor: `src/SportSys.Contract/Services/ManufacturerService.cs`

Namespace: `SportSys.Contract.Services`
Using: `SportSys.Database.Context`, `SportSys.Database.Models.dbo`, `SportSys.Contract.Models.Inventory`, `Microsoft.EntityFrameworkCore`

Metody:

```csharp
// Vrátí seznam výrobců filtrovaný dle Name (pokud není null/empty)
// Seřazeno: Name ASC
Task<List<Manufacturer>> GetAllAsync(string? nameFilter = null, CancellationToken ct = default)

// Vrátí výrobce dle Id nebo null
Task<Manufacturer?> GetByIdAsync(int id, CancellationToken ct = default)

// Vytvoří nového výrobce
Task<Manufacturer> CreateAsync(Manufacturer dto, CancellationToken ct = default)

// Aktualizuje existujícího výrobce; hodí InvalidOperationException pokud nenalezen
Task UpdateAsync(Manufacturer dto, CancellationToken ct = default)
```

Implementace: vzor `IceRinkService.cs`. Přidávat do DB entity `Manufacturer` z `SportSys.Database.Models.dbo`.

### 2.2 LocationService

Soubor: `src/SportSys.Contract/Services/LocationService.cs`

Metody:

```csharp
// Seznam umístění pro přehledovou tabulku (s názvem rodiče)
// Seřazeno: ParentLocationName ASC NULLS FIRST, Name ASC
Task<List<LocationListItem>> GetAllAsync(string? nameFilter = null, CancellationToken ct = default)

// Vrátí umístění dle Id nebo null
Task<Location?> GetByIdAsync(int id, CancellationToken ct = default)

// Vrátí všechna aktivní umístění pro dropdown (seřazeno Name ASC)
// Vylučuje Id 'excludeId' (aby umístění nemohlo být rodičem sebe sama)
Task<List<LocationSelectItem>> GetSelectListAsync(int? excludeId = null, CancellationToken ct = default)

// Vytvoří nové umístění
Task<Location> CreateAsync(Location dto, CancellationToken ct = default)

// Aktualizuje existující umístění
Task UpdateAsync(Location dto, CancellationToken ct = default)
```

Implementace:
- `GetAllAsync` – left join na Parent location: `.Include(l => l.ParentLocation)` nebo EF projection se subquery
- `GetSelectListAsync` – `.Where(l => l.IsActive && l.Id != excludeId)` (excludeId = null means no exclusion)

### 2.3 LoanService

Soubor: `src/SportSys.Contract/Services/LoanService.cs`

Metody:

```csharp
// Vrátí seznam výpůjček (grupovaných dle MemberId + LoanDate) s aplikovaným filtrem
// Seřazeno: LoanDate DESC
Task<List<LoanListItem>> GetLoansAsync(LoanFilter filter, CancellationToken ct = default)

// Vrátí detail výpůjčky dle GroupId (= min LoanId v grupě)
// Najde všechny Loan záznamy ve skupině (stejný MemberId + LoanDate)
Task<LoanDetail?> GetLoanDetailAsync(int groupId, CancellationToken ct = default)

// Vrátí všechny aktivní členy pro select dropdown
// Seřazeno: DisplayName ASC
Task<List<MemberSelectItem>> GetActiveMembersAsync(CancellationToken ct = default)

// Vyhledá položku dle inventárního čísla a ověří dostupnost
// Vrátí InventoryItemLookup s Found=false pokud nenalezena, IsAvailable=false pokud vypůjčena/vyřazena
Task<InventoryItemLookup> LookupItemAsync(string inventoryNumber, CancellationToken ct = default)

// Vytvoří výpůjčku: N záznamů Loan ve stejné transakci
// LoanDate = DateOnly.FromDateTime(DateTime.Today)
// Pro každou položku: vytvoří Loan + InventoryTransaction typu Loan
// Nastaví ItemStatus na Borrowed (EItemStatus.Borrowed)
// Hodí InvalidOperationException pokud člen nebo položka nenalezena / položka nedostupná
// Vrátí GroupId (min z vytvořených Loan.Id)
Task<int> CreateLoanAsync(CreateLoan dto, CancellationToken ct = default)

// Potvrdí vrácení jedné položky (dle LoanId)
// Nastaví Loan.ReturnedDate = dnes
// Vytvoří InventoryTransaction typu Return
// Nastaví ItemStatus zpět na InStock (EItemStatus.InStock)
Task ReturnItemAsync(int loanId, CancellationToken ct = default)

// Potvrdí vrácení všech nevrácených položek v grupě
// Volá ReturnItemAsync pro každou nevracenou položku
Task ReturnAllAsync(int groupId, CancellationToken ct = default)
```

**LookupItemAsync – logika dostupnosti:**
- Položka neexistuje (`Found = false`): InventoryItem se zadaným číslem není v DB
- Položka nedostupná (`IsAvailable = false`) pokud:
  - `ItemStatus == EItemStatus.Borrowed` (již vypůjčena – existuje aktivní Loan bez ReturnedDate)
  - `ItemStatus == EItemStatus.Disposed` (vyřazena)
  - `ItemStatus == EItemStatus.Lost` (ztracena)
  - `IsActive == false`

**Poznámka k TPC:**
- `Loan.InventoryItemId` nemá DB FK (TPC omezení – viz `docs/modules/inventory.md`)
- Vyhledávat přes `_db.Equipment.Where(e => e.InventoryNumber == n)` UNION `_db.Assets.Where(a => a.InventoryNumber == n)`
- Nebo: vyhledávat dle `InventoryNumber` na obou DbSetech a slučovat výsledky v paměti
- Při vytváření Loan: ItemStatus aktualizovat přes `Equipment` nebo `Asset` DbSet dle toho, co bylo nalezeno

**LookupItemAsync ErrorMessage hodnoty:**
- `"Položka s inventárním číslem '{n}' nebyla nalezena."` – pokud `Found = false`
- `"Položka je již vypůjčena."` – pokud `ItemStatus == Borrowed`
- `"Položka je vyřazena nebo ztracena."` – jiné nedostupné stavy

**Skupinová identita v GetLoansAsync:**
Grupování: `.GroupBy(l => new { l.MemberId, l.LoanDate })`. V EF Core je serverové GroupBy s agregacemi podporováno. Při problémech s překladem: načíst data do paměti (`.ToListAsync()`) a grupovat v C#.

---

## Fáze 3: Registrace servisů

Soubor: `src/SportSys.Contract/ServiceCollectionExtensions.cs`

Přidat na konec bloku registrací (před `return services;`):

```csharp
services.AddScoped<ManufacturerService>();
services.AddScoped<LocationService>();
services.AddScoped<LoanService>();
```

Přidat using: `using SportSys.Contract.Services;` (pokud chybí – zkontrolovat, že namespace odpovídá).

---

## Fáze 4: Area setup

### 4.1 Adresářová struktura

Vytvořit strukturu:

```
src/SportSys.Razor/Areas/Inventory/Pages/
├── _ViewImports.cshtml
├── _ViewStart.cshtml
├── Manufacturers/
│   ├── Index.cshtml
│   ├── Index.cshtml.cs
│   ├── Edit.cshtml
│   └── Edit.cshtml.cs
├── Locations/
│   ├── Index.cshtml
│   ├── Index.cshtml.cs
│   ├── Edit.cshtml
│   └── Edit.cshtml.cs
└── Loans/
    ├── Index.cshtml
    ├── Index.cshtml.cs
    ├── Create.cshtml
    ├── Create.cshtml.cs
    ├── Edit.cshtml
    └── Edit.cshtml.cs
```

### 4.2 _ViewImports.cshtml

```cshtml
@using SportSys.Razor
@namespace SportSys.Razor.Areas.Inventory.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

### 4.3 _ViewStart.cshtml

```cshtml
@{
    Layout = "/Pages/Shared/_Layout.cshtml";
}
```

---

## Fáze 5: Manufacturers stránky

### 5.1 Index.cshtml.cs

Namespace: `SportSys.Razor.Areas.Inventory.Pages.Manufacturers`

```csharp
public class IndexModel : PageModel
{
    private readonly ManufacturerService _service;

    public IndexModel(ManufacturerService service) { _service = service; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? NameFilter { get; set; }

    public List<Manufacturer> Manufacturers { get; set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Manufacturers = await _service.GetAllAsync(NameFilter, ct);
    }
}
```

### 5.2 Index.cshtml

- `ViewData["Title"] = "Výrobci";`
- Zobrazit `StatusMessage` v `<div class="infobox">` (pokud není null)
- Filtrační formulář (method="get"):
  - Pole Název (`<input type="text" ... class="textbox">`)
  - Tlačítko Hledat (`fa-magnifying-glass`)
  - Odkaz/tlačítko Vymazat filtr (secondary, `fa-filter-circle-xmark`, odkazuje na `/Inventory/Manufacturers` bez parametrů)
- Tlačítko Nový výrobce nad tabulkou (`fa-plus`, `asp-page="Edit"`)
- Tabulka `<table class="grid">` se sloupci: **Název**, **Web**, **Aktivní**, (tlačítka)
  - Web: pokud není null, zobrazit jako `<a href="@m.Website" target="_blank">@m.Website</a>`
  - Aktivní: ikonka `fa-check` zelená nebo pomlčka
  - Tlačítko Upravit: `button secondary`, `fa-pen`, `asp-page="Edit" asp-route-id="@m.Id"`
- Prázdný stav: `<p class="text-muted">Žádní výrobci nejsou zadáni.</p>`

### 5.3 Edit.cshtml.cs

```csharp
public class EditModel : PageModel
{
    private readonly ManufacturerService _service;

    public EditModel(ManufacturerService service) { _service = service; }

    [BindProperty]
    public Manufacturer Input { get; set; } = new();

    public bool IsNew => Input.Id == 0;

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken ct)
    {
        if (id is null) return Page();

        var dto = await _service.GetByIdAsync(id.Value, ct);
        if (dto is null) return NotFound();

        Input = dto;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();

        if (Input.Id == 0)
            await _service.CreateAsync(Input, ct);
        else
            await _service.UpdateAsync(Input, ct);

        return RedirectToPage("Index");
    }
}
```

### 5.4 Edit.cshtml

- `ViewData["Title"] = Model.IsNew ? "Nový výrobce" : "Upravit výrobce";`
- `<div asp-validation-summary="ModelOnly" class="validation-summary-errors"></div>`
- Formulář s poli: Název, Web, Aktivní (checkbox)
- `<input type="hidden" asp-for="Input.Id" />`
- Footer formuláře: Uložit (`fa-floppy-disk`) + Zpět (`fa-arrow-left`, secondary, `asp-page="Index"`)
- `@section Scripts { <partial name="_ValidationScriptsPartial" /> }`

---

## Fáze 6: Locations stránky

### 6.1 Index.cshtml.cs

```csharp
public class IndexModel : PageModel
{
    private readonly LocationService _service;

    public IndexModel(LocationService service) { _service = service; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? NameFilter { get; set; }

    public List<LocationListItem> Locations { get; set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Locations = await _service.GetAllAsync(NameFilter, ct);
    }
}
```

### 6.2 Index.cshtml

- Filtr: Název + Hledat + Vymazat filtr
- Tlačítko Nové umístění
- Tabulka: **Název**, **Nadřazené umístění** (nebo `—` pokud null), **Aktivní**, (tlačítka)
- Tlačítko Upravit

### 6.3 Edit.cshtml.cs

```csharp
public class EditModel : PageModel
{
    private readonly LocationService _service;

    public EditModel(LocationService service) { _service = service; }

    [BindProperty]
    public Location Input { get; set; } = new();

    public List<LocationSelectItem> ParentLocations { get; set; } = [];

    public bool IsNew => Input.Id == 0;

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken ct)
    {
        ParentLocations = await _service.GetSelectListAsync(excludeId: id, ct);

        if (id is null) return Page();

        var dto = await _service.GetByIdAsync(id.Value, ct);
        if (dto is null) return NotFound();

        Input = dto;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ParentLocations = await _service.GetSelectListAsync(
                excludeId: Input.Id == 0 ? null : Input.Id, ct);
            return Page();
        }

        if (Input.Id == 0)
            await _service.CreateAsync(Input, ct);
        else
            await _service.UpdateAsync(Input, ct);

        return RedirectToPage("Index");
    }
}
```

### 6.4 Edit.cshtml

- Pole: Název, Nadřazené umístění (select z `ParentLocations`, první možnost `— žádné —` s value `""`), Popis, Aktivní
- Dropdown: `<select asp-for="Input.ParentLocationId" asp-items="...">`
  - Použít `SelectList` nebo ručně generovat `<option>` v Razor

---

## Fáze 7: Loans stránky

### 7.1 Index.cshtml.cs

```csharp
public class IndexModel : PageModel
{
    private readonly LoanService _service;

    public IndexModel(LoanService service) { _service = service; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public LoanFilter Filter { get; set; } = new();

    public List<LoanListItem> Loans { get; set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Loans = await _service.GetLoansAsync(Filter, ct);
    }
}
```

### 7.2 Index.cshtml

- Filtr formulář (method="get"):
  - Člen (text input)
  - Pouze aktivní (checkbox)
  - Datum od / Datum do (date inputs)
  - Hledat + Vymazat filtr
- Tlačítko Nová výpůjčka (`asp-page="Create"`)
- Tabulka: **Číslo výpůjčky**, **Člen**, **Datum vydání**, **Počet položek**, **Vráceno**, **Stav**, (Detail)
  - Datum vydání: `@item.LoanDate.ToString("d.M.yyyy")`
  - Stav: badge/span s třídou dle stavu (`status-active`, `status-partial`, `status-closed`) nebo prostý text
  - Tlačítko Detail: `button secondary`, `fa-eye`, `asp-page="Edit" asp-route-id="@item.GroupId"`

### 7.3 Create.cshtml.cs

```csharp
public class CreateModel : PageModel
{
    private readonly LoanService _service;

    public CreateModel(LoanService service) { _service = service; }

    public List<MemberSelectItem> Members { get; set; } = [];

    [BindProperty]
    public CreateLoan Input { get; set; } = new();

    // Pomocná property pro zobrazení přidaných položek (renderuje se z TempData nebo z Input.InventoryNumbers)
    public List<InventoryItemLookup> AddedItems { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        Members = await _service.GetActiveMembersAsync(ct);
        return Page();
    }

    // Handler pro přidání položky dle inventárního čísla (volaný z JS fetch nebo jako fallback POST)
    public async Task<IActionResult> OnPostLookupAsync(
        [FromQuery] string inventoryNumber, CancellationToken ct)
    {
        var result = await _service.LookupItemAsync(inventoryNumber, ct);
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid || Input.InventoryNumbers.Count == 0)
        {
            if (Input.InventoryNumbers.Count == 0)
                ModelState.AddModelError("", "Přidejte alespoň jednu položku.");

            Members = await _service.GetActiveMembersAsync(ct);
            return Page();
        }

        try
        {
            var groupId = await _service.CreateLoanAsync(Input, ct);
            return RedirectToPage("Edit", new { id = groupId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            Members = await _service.GetActiveMembersAsync(ct);
            return Page();
        }
    }
}
```

### 7.4 Create.cshtml

Tato stránka je nejdůležitější obrazovka modulu. Musí být optimalizována pro práci s QR čtečkou (chová se jako klávesnice).

**Krok 1 – Výběr člena:**
```html
<div class="field">
    <label asp-for="Input.MemberId">Člen</label>
    <select asp-for="Input.MemberId" class="textbox">
        <option value="">— vyberte člena —</option>
        @foreach (var m in Model.Members)
        {
            <option value="@m.Id">@m.DisplayName</option>
        }
    </select>
    <span asp-validation-for="Input.MemberId" class="field-validation-error"></span>
</div>
```

**Krok 2 – Přidávání položek (QR skener):**

Vstupní pole pro inventární číslo. Po stisku Enter:
1. Volá `?handler=Lookup&inventoryNumber={value}` přes `fetch`
2. Pokud `found && isAvailable`: přidá do tabulky + přidá hidden input `Input.InventoryNumbers` + vyčistí pole + nastaví focus zpět
3. Pokud chyba: zobrazí inline chybovou hlášku

Struktura HTML:
```html
<div id="scan-section">
    <div class="field">
        <label for="scan-input">Inventární číslo</label>
        <input type="text" id="scan-input" placeholder="Naskenujte nebo zadejte inventární číslo…" autocomplete="off" />
        <span id="scan-error" class="field-validation-error" style="display:none"></span>
    </div>

    <table class="grid" id="items-table">
        <thead>
            <tr>
                <th>Inventární číslo</th>
                <th>Název</th>
                <th>Kategorie</th>
                <th>Aktuální umístění</th>
                <th></th>
            </tr>
        </thead>
        <tbody id="items-body">
            <!-- JS přidává řádky -->
        </tbody>
    </table>
    <div id="items-empty" class="text-muted">Zatím nebyly přidány žádné položky.</div>
</div>
```

**Tlačítko Vytvořit výpůjčku:**
- Deaktivovat (`disabled`) pokud není vybrán člen nebo nejsou přidány položky
- Kontrolu provádět JS, nebo server-side v OnPost

**JavaScript (vanilla JS, žádné frameworky):**

Umístit do `@section Scripts { <script> ... </script> }`.

```javascript
const scanInput = document.getElementById('scan-input');
const scanError = document.getElementById('scan-error');
const itemsBody = document.getElementById('items-body');
const itemsEmpty = document.getElementById('items-empty');
const addedNumbers = new Set();

scanInput.addEventListener('keydown', async (e) => {
    if (e.key !== 'Enter') return;
    e.preventDefault();

    const number = scanInput.value.trim();
    if (!number) return;
    if (addedNumbers.has(number)) {
        showError('Tato položka je již přidána.');
        return;
    }

    scanError.style.display = 'none';

    const resp = await fetch(`?handler=Lookup&inventoryNumber=${encodeURIComponent(number)}`, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    });
    const data = await resp.json();

    if (!data.found || !data.isAvailable) {
        showError(data.errorMessage || 'Položka není dostupná.');
        return;
    }

    addedNumbers.add(number);
    addRow(data);
    addHiddenInput(number);
    scanInput.value = '';
    scanInput.focus();
    itemsEmpty.style.display = 'none';
});

function showError(msg) {
    scanError.textContent = msg;
    scanError.style.display = 'block';
    scanInput.select();
}

function addRow(item) {
    const tr = document.createElement('tr');
    tr.innerHTML = `
        <td>${item.inventoryNumber}</td>
        <td>${item.name}</td>
        <td>${item.categoryName}</td>
        <td>${item.currentLocationName}</td>
        <td class="buttons">
            <button type="button" class="button tertiary" onclick="removeItem(this, '${item.inventoryNumber}')">
                <i class="fa-solid fa-xmark fa-fw"></i> Odebrat
            </button>
        </td>`;
    itemsBody.appendChild(tr);
}

function addHiddenInput(number) {
    const input = document.createElement('input');
    input.type = 'hidden';
    input.name = 'Input.InventoryNumbers';
    input.value = number;
    input.dataset.number = number;
    document.getElementById('loan-form').appendChild(input);
}

function removeItem(btn, number) {
    btn.closest('tr').remove();
    addedNumbers.delete(number);
    document.querySelector(`input[data-number="${number}"]`)?.remove();
    if (addedNumbers.size === 0) itemsEmpty.style.display = '';
}
```

**Důležité:** formulář musí mít `id="loan-form"`, `asp-antiforgery="true"`.

### 7.5 Edit.cshtml.cs (Detail + vrácení)

```csharp
public class EditModel : PageModel
{
    private readonly LoanService _service;

    public EditModel(LoanService service) { _service = service; }

    [TempData]
    public string? StatusMessage { get; set; }

    public LoanDetail? Loan { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        Loan = await _service.GetLoanDetailAsync(id, ct);
        if (Loan is null) return NotFound();
        return Page();
    }

    // Vrácení jedné položky
    public async Task<IActionResult> OnPostReturnItemAsync(int id, int loanId, CancellationToken ct)
    {
        await _service.ReturnItemAsync(loanId, ct);
        StatusMessage = "Položka byla vrácena.";
        return RedirectToPage(new { id });
    }

    // Hromadné vrácení všech položek
    public async Task<IActionResult> OnPostReturnAllAsync(int id, CancellationToken ct)
    {
        await _service.ReturnAllAsync(id, ct);
        StatusMessage = "Všechny položky byly vráceny.";
        return RedirectToPage(new { id });
    }
}
```

### 7.6 Edit.cshtml (Detail + vrácení)

**Hlavička (jen čtení):**
- Číslo výpůjčky, Člen, Datum vydání, Stav

**Hromadné vrácení:**
```html
@if (Model.Loan.Items.Any(i => !i.IsReturned))
{
    <form method="post" asp-page-handler="ReturnAll" asp-route-id="@Model.Loan.GroupId">
        <button type="submit" class="button"
                onclick="return confirm('Opravdu vrátit všechny položky?')">
            <i class="fa-solid fa-check fa-fw"></i> Vrátit vše
        </button>
    </form>
}
```

**Tabulka položek:**
- Sloupce: Inventární číslo, Název, Kategorie, Vráceno (ikonka), Datum vrácení, Akce
- Pro nevrácenou položku: formulář s `asp-page-handler="ReturnItem"`, `asp-route-id="@Model.Loan.GroupId"`, `asp-route-loanId="@item.LoanId"`

**Zpět:** `asp-page="Index"` (secondary button)

---

## Fáze 8: Navigace

### 8.1 Úprava _Layout.cshtml

Soubor: `src/SportSys.Razor/Pages/Shared/_Layout.cshtml`

Přidat do `<ul>` v `<nav>` sekci navigační položky pro Inventory:

```html
<li>
    <a asp-area="Inventory" asp-page="/Manufacturers/Index">
        <i class="fa-solid fa-industry fa-fw"></i> Sklad
    </a>
    <ul>
        <li>
            <a asp-area="Inventory" asp-page="/Manufacturers/Index">
                <i class="fa-solid fa-industry fa-fw"></i> Výrobci
            </a>
        </li>
        <li>
            <a asp-area="Inventory" asp-page="/Locations/Index">
                <i class="fa-solid fa-location-dot fa-fw"></i> Umístění
            </a>
        </li>
        <li>
            <a asp-area="Inventory" asp-page="/Loans/Index">
                <i class="fa-solid fa-hand-holding-box fa-fw"></i> Výpůjčky
            </a>
        </li>
    </ul>
</li>
```

> **Poznámka:** Zkontroluj existující CSS třídy pro vnořenou navigaci. Pokud neexistují, přidej minimální SCSS do existujícího `_rnav.scss` nebo nového `_inventory.scss`.

---

## Fáze 9: Build a ověření

Po implementaci:

```bash
dotnet build SportSys.slnx
```

Build musí proběhnout bez chyb. Neočekávají se žádné nové warningy.

Zkontrolovat:
- Všechny stránky se načítají bez chyby (Index, Edit/Create, Detail)
- Filtrování funguje na Index stránkách
- Formuláře validují required pole
- StatusMessage se zobrazuje po uložení/smazání
- QR skener flow (Create Loan): přidání položky, odstranění, odeslání formuláře

---

## Poznámky k implementaci

### TPC a LookupItemAsync

Inventory položky jsou v TPC hierarchii – existují jako záznamy v `inventory.Equipment` nebo `inventory.Asset`. Při vyhledávání dle `InventoryNumber`:

```csharp
// V LoanService.LookupItemAsync:
var eq = await _db.Equipment.Where(e => e.InventoryNumber == inventoryNumber)
    .Select(e => new { e.Id, e.InventoryNumber, e.Name, e.ItemStatus, e.IsActive,
                       CategoryName = e.Category.Name,
                       LocationName = e.CurrentLocation != null ? e.CurrentLocation.Name : "" })
    .FirstOrDefaultAsync(ct);

if (eq != null) { /* mapovat */ }
else
{
    var asset = await _db.Assets.Where(a => a.InventoryNumber == inventoryNumber) /* ... */;
    // ...
}
```

### AJAX endpoint autorizace

`OnPostLookupAsync` je POST handler pro JSON lookup. Pokud je zapnutá globální autorizace (`FallbackPolicy`), handler bude chráněn automaticky – to je žádoucí. Nevolat bez přihlášení.

Fetch volání musí obsahovat antiforgery token:
```javascript
// Při GET requestu (handler=Lookup) není antiforgery nutný – použij GET variantu
// Změn handler: Task<IActionResult> OnGetLookupAsync(string inventoryNumber, ...)
// URL: ?handler=Lookup&inventoryNumber=...
```

→ **Použij `OnGetLookupAsync`** (GET handler) pro JSON lookup místo POST – GET nepotřebuje antiforgery token a je semanticky správné pro čtecí operaci.

### Existující CSS třídy

Ověřit existenci třídy `infobox` v `_layout.scss` nebo `_utilities.scss`. Vzor použití v `Areas/sport/Pages/IceRink/Index.cshtml`:
```html
<div class="infobox">
    <i class="fa-solid fa-circle-check fa-fw"></i> @Model.StatusMessage
</div>
```

### Namespace v Razor Pages

`_ViewImports.cshtml` v `Areas/Inventory/Pages/` musí definovat správný namespace:
```
@namespace SportSys.Razor.Areas.Inventory.Pages
```

Jednotlivé stránky budou mít namespace dle podadresáře, např.:
- `SportSys.Razor.Areas.Inventory.Pages.Manufacturers`
- `SportSys.Razor.Areas.Inventory.Pages.Locations`
- `SportSys.Razor.Areas.Inventory.Pages.Loans`

---

## Soubory ke změně / vytvoření

### Nové soubory – SportSys.Contract

```
src/SportSys.Contract/Models/inventory/Manufacturer.cs
src/SportSys.Contract/Models/inventory/Location.cs
src/SportSys.Contract/Models/inventory/Loans.cs
src/SportSys.Contract/Services/ManufacturerService.cs
src/SportSys.Contract/Services/LocationService.cs
src/SportSys.Contract/Services/LoanService.cs
```

### Upravené soubory – SportSys.Contract

```
src/SportSys.Contract/ServiceCollectionExtensions.cs
```

### Nové soubory – SportSys.Razor

```
src/SportSys.Razor/Areas/Inventory/Pages/_ViewImports.cshtml
src/SportSys.Razor/Areas/Inventory/Pages/_ViewStart.cshtml
src/SportSys.Razor/Areas/Inventory/Pages/Manufacturers/Index.cshtml
src/SportSys.Razor/Areas/Inventory/Pages/Manufacturers/Index.cshtml.cs
src/SportSys.Razor/Areas/Inventory/Pages/Manufacturers/Edit.cshtml
src/SportSys.Razor/Areas/Inventory/Pages/Manufacturers/Edit.cshtml.cs
src/SportSys.Razor/Areas/Inventory/Pages/Locations/Index.cshtml
src/SportSys.Razor/Areas/Inventory/Pages/Locations/Index.cshtml.cs
src/SportSys.Razor/Areas/Inventory/Pages/Locations/Edit.cshtml
src/SportSys.Razor/Areas/Inventory/Pages/Locations/Edit.cshtml.cs
src/SportSys.Razor/Areas/Inventory/Pages/Loans/Index.cshtml
src/SportSys.Razor/Areas/Inventory/Pages/Loans/Index.cshtml.cs
src/SportSys.Razor/Areas/Inventory/Pages/Loans/Create.cshtml
src/SportSys.Razor/Areas/Inventory/Pages/Loans/Create.cshtml.cs
src/SportSys.Razor/Areas/Inventory/Pages/Loans/Edit.cshtml
src/SportSys.Razor/Areas/Inventory/Pages/Loans/Edit.cshtml.cs
```

### Upravené soubory – SportSys.Razor

```
src/SportSys.Razor/Pages/Shared/_Layout.cshtml
```

---

## Související dokumenty

- [`docs/modules/inventory.md`](../../docs/modules/inventory.md) – popis modulu (datový model, entity, UI vrstva)
- [`.github/tasks/inventory-ui-layer.md`](inventory-ui-layer.md) – UI zadání (specifikace stránek)
- [`.github/tasks/inventory-data-layer.md`](inventory-data-layer.md) – implementační plán datové vrstvy
