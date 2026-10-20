# Implementace #33: Přehled požadavků – agregace trenérů

- `RequirementCoachListItem.LastName` (Contract) – příjmení z `DisplayName`, fallback `PersonalNumber`.
- `src/SportSys.Razor/Models/RequirementCoachGroups.cs` – seskupení podle role (`CoachRoleId`), příjmení řazena `cs-CZ`, bez duplicit.
- Partial `Areas/sport/Pages/Training/Requirement/_CoachGroups.cshtml` použitý v obou tabulkách `Index.cshtml`.
- `Styles/_coach-groups.scss` (grid, popisek role `18ch`), `@use` v `site.scss`.
- Testy v `TrainingRequirementTests.cs` (8/8 prošlo).
- Bez změny DB, bez migrace.

Poznámka: celý testovací projekt se nepřekládá kvůli již existující chybě v `CoachAttendanceTests.cs` (`CoachAttendanceImportRow`), nesouvisející s touto změnou; testy byly spuštěny s dočasným vyloučením souboru.
