# Implementace: #39 Zobrazení role trenéra v tréninkových plánech

**Plán:** [39-zobrazeni-role-trenera-v-treninkovych-planech.md](39-zobrazeni-role-trenera-v-treninkovych-planech.md)

## Provedené změny

- `TrainingPlanScheduleItemDto` nyní nese plánová přiřazení trenérů včetně
  identity a hodnot `CoachRole.Code` a `CoachRole.Order`.
- `TrainingScheduleService` načítá roli v hromadném dotazu, zachovává
  překryvné intervaly platnosti a slučuje pouze opakované přiřazení stejné
  dvojice trenér–role. Stávající `Coaches` zůstává kompatibilní projekcí.
- Nový `TrainingPlanCoachPresentation` zajišťuje jednotný formát
  `Příjmení (Code)` a řazení podle pořadí kategorie, role a českého příjmení.
  Bloky zachovávají zdrojovou kategorii každého přiřazení, tooltip obsahuje
  jeden společný seznam a tabulkový přehled používá shodný formatter.
- Dokumentace `docs/modules/sport.md` popisuje nové chování. Databázový model,
  migrace, snapshot, skutečné tréninky a CSS se neměnily.

## Ověření

- Cílené testy: **45 prošlo**; pokrývají platnost a její krajní dny,
  deduplikaci podle identity a role, české řazení, prioritu kategorií,
  opakované trenéry ve spojených plánech, tooltip i prázdný seznam.
- `dotnet build SportSys.slnx`: úspěšný, bez chyb.
- Kompletní `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`:
  **287 prošlo, 11 selhalo**. Všech 11 selhání je v
  `TrainingRequirementTests` a souvisí s oddělenými, již staged změnami
  přehledu požadavků a formátováním hodin; těchto souborů se implementace #39
  nedotkla.
- SQL Server dotaz nebylo možné vykonat: lokální
  `ConnectionStrings:DefaultConnection` není nakonfigurovaný. Rozšíření
  dotazu je ověřeno kompilací a mapovacími jednotkovými testy, ne skutečným
  databázovým během.

Build hlásí také existující upozornění na zranitelné verze NuGet balíčků;
závislosti nebyly součástí rozsahu issue #39.
