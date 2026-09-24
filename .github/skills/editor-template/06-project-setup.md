# 06 – Konfigurace projektu

> **Navigace:** [← Vlastní šablony](05-custom-templates.md) | [Admin vzory →](07-admin-patterns.md) | [Skill](SKILL.md)

Nastavení projektu pro plné využití EditorTemplate systému včetně `Altairis.ConventionalMetadataProviders`.

---

## NuGet balíčky

```xml
<PackageReference Include="Altairis.ConventionalMetadataProviders" Version="1.0.5" />
<PackageReference Include="Altairis.TagHelpers" Version="2.0.1" />
<!-- Volitelně pro validaci: -->
<PackageReference Include="Altairis.ValidationToolkit" Version="..." />
```

---

## Program.cs – klíčová konfigurace

```csharp
using Altairis.ConventionalMetadataProviders;
using MyApp.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options => {
    // Autorizace admin sekce (volitelné)
    options.Conventions.AuthorizeFolder("/Admin", "IsAdministrator");
})
.AddMvcOptions(options => {
    // ← KLÍČOVÁ ŘÁDKA: conventional metadata providers
    options.SetConventionalMetadataProviders<Display, Validation>();
    // nebo jen s display:
    // options.SetConventionalMetadataProviders<Display>();
    // nebo s vlastním binding resource:
    // options.SetConventionalMetadataProviders<Display, Validation, Binding>();
});

// Lokalizace (pokud potřebujete vícejazyčnost)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options => {
    options.SetDefaultCulture("cs-CZ");
    options.AddSupportedCultures("cs-CZ", "en-US");
    options.AddSupportedUICultures("cs-CZ", "en-US");
    options.RequestCultureProviders = [new CookieRequestCultureProvider()];
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRequestLocalization();  // pokud používáte lokalizaci
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
```

**Zdroj:** [Altairis.ReP](https://github.com/ridercz/ReP) – `Altairis.ReP.Web/Program.cs`

---

## Altairis.ConventionalMetadataProviders

### Co dělá

Místo psaní `[Display(Name = nameof(Display.Email), ResourceType = typeof(Display))]` na každou vlastnost, tato knihovna **automaticky mapuje** display names, descriptions a validační zprávy z centrálního `.resx` souboru dle konvence jméno vlastnosti.

**GitHub:** [ridercz/Altairis.ConventionalMetadataProviders](https://github.com/ridercz/Altairis.ConventionalMetadataProviders)

### Konvence vyhledávání klíče v .resx

Pro vlastnost `Email` na třídě `MyApp.Pages.Admin.CreateModel`:

```
Hledá klíče od nejspecifičtějšího:
  MyApp_Pages_Admin_CreateModel_Email    ← nejspecifičtější
  Pages_Admin_CreateModel_Email
  Admin_CreateModel_Email
  CreateModel_Email
  Email                                  ← nejčastěji stačí toto
```

Díky tomu klíč `Email` v `Display.resx` pokryje **všechny vlastnosti nazvané `Email`** v celé aplikaci.

### Dostupné display metadata (z Display.resx)

| Konvence klíče | Metadata | Použití v šabloně |
|---|---|---|
| `PropertyName` | `DisplayName` | text labelu |
| `PropertyName_Description` | `Description` | popis pod polem |
| `PropertyName_Placeholder` | `Placeholder` | HTML placeholder |
| `PropertyName_Null` | `NullDisplayText` | text při null hodnotě |
| `PropertyName_DisplayFormat` | `DisplayFormatString` | formát pro zobrazení |
| `PropertyName_EditFormat` | `EditFormatString` | formát v edit módu |

### Automatické [Required] pro value types

`ConventionalValidationMetadataProvider` automaticky přidává `[Required]` na:
- `int`, `long`, `byte`, `float`, `double`, `decimal`
- `DateTime`, `TimeSpan`
- `bool` (ale výsledkem je jen povinné pole, ne nutně validační chyba pro bool)

Díky tomu **nemusíte psát** `[Required]` na tyto typy vlastností.

---

## Display.resx – struktura

Soubor: `Resources/Display.resx`

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <!-- Základní display names -->
  <data name="Name" xml:space="preserve">
    <value>Název</value>
  </data>
  <data name="Email" xml:space="preserve">
    <value>E-mail</value>
  </data>
  <data name="Password" xml:space="preserve">
    <value>Heslo</value>
  </data>

  <!-- Popis pod polem -->
  <data name="MaximumReservationTime" xml:space="preserve">
    <value>Maximální čas rezervace</value>
  </data>
  <data name="MaximumReservationTime_Description" xml:space="preserve">
    <value>v minutách, použijte 0 pro neomezeno</value>
  </data>

  <!-- Placeholder -->
  <data name="Email_Placeholder" xml:space="preserve">
    <value>vas@email.cz</value>
  </data>

  <!-- Null text -->
  <data name="Note_Null" xml:space="preserve">
    <value>(bez poznámky)</value>
  </data>
</root>
```

Příklad reálných klíčů (z projektu [Altairis.ReP](https://github.com/ridercz/ReP)):

| Klíč | Hodnota |
|------|---------|
| `BackgroundColor` | Background color |
| `ClosingTime` | Closing time |
| `DateBegin` | Begin date |
| `DateEnd` | End date |
| `Email` | E-mail |
| `ForegroundColor` | Foreground color |
| `Instructions` | Instructions |
| `Instructions_Description` | instructions for use or similar information |
| `IsAdministrator` | This user is administrator |
| `MaximumReservationTime` | Maximum reservation time |
| `MaximumReservationTime_Description` | in minutes, use 0 for unlimited |
| `Name` | Name |
| `OpeningTime` | Opening time |
| `Password` | Password |
| `PhoneNumber` | Phone number |
| `ResourceEnabled` | This resource is enabled and visible to regular users |
| `Text` | Text |
| `Title` | Title |
| `UserName` | User name |

---

## Validation.resx – struktura

Soubor: `Resources/Validation.resx`

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <!-- Klíč = jméno validačního atributu (bez "Attribute" suffixu) -->
  <data name="Required" xml:space="preserve">
    <value>Pole {0} je povinné.</value>
  </data>
  <data name="MaxLength" xml:space="preserve">
    <value>Pole {0} může mít maximálně {1} znaků.</value>
  </data>
  <data name="Range" xml:space="preserve">
    <value>Pole {0} musí být v rozsahu od {1} do {2}.</value>
  </data>
  <data name="EmailAddress" xml:space="preserve">
    <value>Pole {0} musí obsahovat platnou e-mailovou adresu.</value>
  </data>
  <data name="Phone" xml:space="preserve">
    <value>Pole {0} musí obsahovat platné telefonní číslo.</value>
  </data>

  <!-- Pro vlastní validační atributy: -->
  <data name="GreaterThan" xml:space="preserve">
    <value>Pole {0} musí být větší než {1}.</value>
  </data>
</root>
```

Konvence vyhledávání validační zprávy pro `[Required]` na `Email`:
```
CreateModel_Email_Required    ← specifická pro model+vlastnost+atribut
Email_Required                ← specifická pro vlastnost+atribut
Required                      ← globální pro atribut
```

---

## Pages/_ViewImports.cshtml

```cshtml
@namespace MyApp.Pages

@inject Microsoft.Extensions.Options.IOptions<AppSettings> OptionsAccessor   ← volitelné

@using MyApp.Resources    ← přístup k Display.xxx, Validation.xxx, UI.xxx v šablonách

@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers    ← asp-for, asp-validation-for atd.
@addTagHelper *, MyApp                                  ← vlastní tag helpery (pokud existují)
```

---

## Pages/_ViewStart.cshtml

```cshtml
@{ Layout = "_Layout"; }
```

---

## Struktura Resources složky

```
Resources/
├── Display.resx                ← display names pro vlastnosti (en-US default)
├── Display.Designer.cs         ← auto-generovaná třída (Build Action: EmbeddedResource)
├── Display.cs-CZ.resx          ← česká lokalizace (volitelné)
├── Validation.resx             ← validační zprávy
├── Validation.Designer.cs      ← auto-generovaná třída
└── UI.resx                     ← UI stringy (tituly stránek, tlačítka) – volitelné
```

> ⚠️ Soubory `.resx` musí mít `Build Action: Embedded Resource` a `Custom Tool: ResXFileCodeGenerator` pro generování `.Designer.cs`. Ověřte v Properties okně Visual Studio.

---

## Kompletní příklady

- [examples/Program-setup.cs](examples/Program-setup.cs) – kompletní Program.cs konfigurace

---

## Související soubory

- [07-admin-patterns.md](07-admin-patterns.md) – jak použít v admin stránkách
- [08-validation.md](08-validation.md) – validace a client-side scripts
- [02-data-annotations.md](02-data-annotations.md) – atributy pro pole
