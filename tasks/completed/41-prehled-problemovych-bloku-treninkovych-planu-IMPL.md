# Implementační záznam: #41 Přehled problémových bloků tréninkových plánů

**Stav:** Implementováno.

## Výsledek

Na stránce `/sport/Training/Plan` je pod rozvrhem a stávajícími přehledy
zobrazen přehled problémových bloků. Výpočet pracuje s filtrovanými
`PlanRows`, nepřistupuje do databáze a vypisuje konfliktní dvojice s průnikem
časů. Plány se stejným `GroupId` se navzájem nekontrolují jako samostatné
aktivity; čistě vizuální seskupení přes `VisualizationGroupId` výsledek
neovlivňuje.

Překryv trenéra se určuje podle ID a má závažnost `Error`. Překryv stejné
kategorie je `Error`, pokud alespoň jeden plán obsahuje ledový trénink,
jinak `Warning`. Kontrola vyžaduje stejný den v týdnu, společný platný
výskyt v průniku rozsahů `From–To` a časový překryv podle polootevřených
intervalů `[TimeFrom, TimeTo)`. Typ ledu se přes `IsIceTraining` předává
v Contract DTO, takže Razor projekt nemá závislost na Database.

## Implementované změny

- Rozšířen `TrainingPlanScheduleItemDto` o `IsIceTraining`; projekce
  `TrainingScheduleService.GetTrainingPlansAsync` jej nastavuje podle
  `ETrainingType.Ice`.
- Přidán `TrainingPlanProblemBuilder` s detekcí trenérských a kategoriálních
  konfliktů, časovým a datovým průnikem, deduplikací rolí, deterministickým
  řazením a textovými popisy.
- Přidána `TrainingPlanProblemsViewComponent`, sdílené Razor view a SCSS
  s přístupnými textovými štítky závažnosti a sémantickými barevnými tokeny.
- Problémový přehled je vykreslen z `PlanRows` až po aplikaci filtrů včetně
  dne; změna `MergeTrainings` konflikty nepotlačuje.
- Dokumentace modulu Sport a plán v `tasks/completed/` odpovídají výsledné
  implementaci.

## Změněné soubory

- `src/SportSys.Contract/Models/TrainingScheduleDto.cs`
- `src/SportSys.Contract/Services/TrainingScheduleService.cs`
- `src/SportSys.Razor/Models/TrainingSchedule/TrainingPlanProblem.cs`
- `src/SportSys.Razor/ViewComponents/TrainingPlanProblemsViewComponent.cs`
- `src/SportSys.Razor/Pages/Shared/Components/TrainingPlanProblems/Default.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml`
- `src/SportSys.Razor/Styles/_training-plan-problems.scss`
- `src/SportSys.Razor/Styles/site.scss`
- `tests/SportSys.Razor.Tests/TrainingPlanProblemBuilderTests.cs`
- `docs/modules/sport.md`
- `tasks/completed/41-prehled-problemovych-bloku-treninkovych-planu.md`

## Ověření

- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --no-restore` — 324 testů úspěšných.
- `dotnet build SportSys.slnx --no-restore` — úspěšný.
- `npm run build:css` v `src\SportSys.Razor` — úspěšný.
- `git diff --check` — bez chyb.
- Nezávislá kontrola implementace nenašla významné problémy.
- Build vypisuje existující upozornění pro preview SDK a závislosti.
  Ruční ověření v prohlížeči ani napojení na živou databázi nebylo provedeno.

Databázový model, snapshot ani EF Core migrace nebyly změněny.
