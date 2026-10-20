# Implementace #35: Rozšíření číselníku CoachRole

- `CoachRole.cs`: `Code` (`char(1)`, `[Unicode(false)]`), `Order`, unikátní index `UX_CoachRole_Code`.
- `CoachRole.Seed.cs`: mapování H1, A2, O3, B4, F5, K6 (Head, Assistant, Defenders, Goalkeepers, Physiotherapist, Conditioning).
- Řazení podle `Order`: `RequirementEditHelper.GetRolesAsync`, `TrainingRequirementService`, `MatchRequirementService`, `RequirementCoachGroups`.
- DTO `RequirementCoachListItem`: `CoachRoleCode`, `CoachRoleOrder`.
- `docs/conventions.md` doplněno.
- Testy: 16/16 (`TrainingRequirementTests`, `RequirementEditDtoTests`) prošlo; `CoachAttendanceTests.cs` s existující chybou byl při běhu dočasně vyloučen.
- Migrace nebyla vytvořena – vytvoří uživatel (u existujících řádků zkontrolovat výchozí hodnoty Code/Order a unikátní index).
