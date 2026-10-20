# Implementace: #30 Vizualizace docházky trenérů v přehledu tréninků a zápasů

## Provedené změny

- Contract DTO nově předávají identifikátor trenéra, jméno, příjmení a ID i
  název stavu účasti.
- `TrainingScheduleService` a `MatchScheduleService` načítají účasti jedním
  dávkovým dotazem pro vrácené události; původní filtry, řazení a projekce
  zůstaly zachovány.
- Prezentační mapování zobrazuje všech pět stavů odlišnými ikonami. Tooltip
  ikony i tooltip události obsahují textový stav a celé jméno trenéra.
- Seskupené tréninky zachovávají účasti po zdrojových kategoriích, včetně
  opakovaného trenéra s různými stavy.
- Zápasy zobrazují trenéry na samostatném řádku bez nahrazení soupeře,
  výsledku ani stavu zápasu. Tréninkové plány zůstaly beze změny.
- SCSS umožňuje zalomení seznamu; aktualizována dokumentace `docs/modules/sport.md`.

## Ověření

- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`
  — všech 179 testů prošlo.
- `dotnet build SportSys.slnx -c Release` — build řešení prošel bez chyb.
- `npm run build:css` — generování CSS prošlo.
- Prohlížečová manuální kontrola úzkých bloků nebyla provedena.

## Databázové změny

Tato implementace neupravila entity, migrace ani model snapshot. Již existující
necommitnuté změny `CoachMatch` a jeho migrace byly zachovány beze změny.
