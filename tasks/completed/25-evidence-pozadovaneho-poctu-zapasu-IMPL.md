# Implementace #25: Evidence požadovaného počtu zápasů

Plán: [25-evidence-pozadovaneho-poctu-zapasu.md](./25-evidence-pozadovaneho-poctu-zapasu.md)

## Provedeno

- **Database:** nové entity `MatchRequirement` a `CoachMatchRequirement`
  (`src/SportSys.Database/Models/sport/`), `DbSet`y v `SportSysDbContext`,
  zpětné navigace v `SeasonCategory`, `Coach` a `CoachRole`. Indexy
  `IX_MatchRequirement_SeasonCategory_Period`,
  `IX_CoachMatchRequirement_MatchRequirement`,
  `IX_CoachMatchRequirement_CoachRole`.
- **Contract:** `MatchRequirementListItem`, `MatchRequirementCoachListItem`
  (`MatchRequirementDto.cs`), společný základ `RequirementCoachListItem`
  v `TrainingRequirementDto.cs`, `MatchRequirementService`
  (registrace v `ServiceCollectionExtensions.cs`).
- **Razor:** `/sport/Training/Requirement` zobrazuje pod tabulkou tréninků
  tabulku zápasů; sezóna a kategorie filtrují obě, typ a fáze jen tréninky
  (označeno u popisků filtru). Prázdné stavy jsou samostatné.
- **SQL:** referenční skript `src/DB Model/sport.MatchRequirement.sql`.
- **Dokumentace:** `docs/modules/sport.md`, `docs/modules/hr.md`,
  `docs/architecture.md`.
- **Testy:** `TrainingRequirementTests.cs` rozšířen o `DisplayText`
  zápasového trenéra a společný základ DTO.

## Ověření

- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`:
  133 testů úspěšných.
- EF model se sestaví a validuje (dočasná kontrola mapování, odstraněna).
- Debug build `SportSys.slnx` selhal jen kvůli zamčeným DLL běžící aplikací
  `SportSys.Razor`, ne kvůli chybám kompilace.

## Zbývá na uživateli

- Vytvořit a aplikovat EF migraci (nebo spustit skript
  `sport.MatchRequirement.sql`); agent migraci nevytvářel.
- Ruční akceptace stránky po vytvoření tabulek a naplnění daty.
