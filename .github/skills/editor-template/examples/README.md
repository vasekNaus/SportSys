# Examples – přehled ukázek

> **Navigace:** [← Skill](../SKILL.md)

Funkční ukázky kódu připravené k přímému použití nebo jako základ pro vlastní implementaci.

---

## Soubory

| Soubor | Popis | Návaznost |
|--------|-------|-----------|
| [InputModel-full.cs](InputModel-full.cs) | Kompletní InputModel se všemi typy polí | [02-data-annotations.md](../02-data-annotations.md) |
| [Object-simple.cshtml](Object-simple.cshtml) | Object.cshtml – jednoduchá verze (plochá) | [03-object-template.md](../03-object-template.md) |
| [Object-grouped.cshtml](Object-grouped.cshtml) | Object.cshtml – s groupováním a `<details>` | [03-object-template.md](../03-object-template.md) |
| [HtmlInput.cshtml](HtmlInput.cshtml) | Sdílená base šablona pro `<input>` | [04-property-templates.md](../04-property-templates.md) |
| [AdminCreate.cshtml](AdminCreate.cshtml) | Create admin stránka (Razor view) | [07-admin-patterns.md](../07-admin-patterns.md) |
| [AdminCreate.cshtml.cs](AdminCreate.cshtml.cs) | Create admin stránka (code-behind) | [07-admin-patterns.md](../07-admin-patterns.md) |
| [MarkdownAttribute.cs](MarkdownAttribute.cs) | Vlastní DataType atribut pro Markdown | [05-custom-templates.md](../05-custom-templates.md) |
| [Program-setup.cs](Program-setup.cs) | Program.cs konfigurace | [06-project-setup.md](../06-project-setup.md) |

---

## Jak použít

1. Zkopírujte soubor do svého projektu
2. Upravte namespace (`MyApp` → váš namespace)
3. Přizpůsobte názvy tříd a vlastností
4. Odstraňte nepotřebné části

---

## Checklist pro nový projekt

```
□ Vytvořit Pages/Shared/EditorTemplates/ (nebo Pages/EditorTemplates/)
□ Zkopírovat Object-simple.cshtml nebo Object-grouped.cshtml → Object.cshtml
□ Zkopírovat HtmlInput.cshtml
□ Vytvořit _Layout.cshtml v EditorTemplates/ (obsah: @RenderBody())
□ Přidat property templates: String, Boolean, Date, DateTime, Time,
  Decimal, EmailAddress, PhoneNumber, Password, MultilineText, HiddenInput
□ Přidat Program-setup.cs konfiguraci do Program.cs
□ Vytvořit Resources/Display.resx a Validation.resx
□ Zkopírovat AdminCreate.cshtml a AdminCreate.cshtml.cs jako základ
□ Přidat _ValidationScriptsPartial.cshtml
```
