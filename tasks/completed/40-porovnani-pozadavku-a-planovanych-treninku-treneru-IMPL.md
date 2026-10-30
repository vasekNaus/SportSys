# Implementační záznam: #40 Porovnání požadavků a plánovaných tréninků trenérů

**Datum:** 21. 10. 2026

**Stav:** Implementováno.

## Výsledek

Na `/sport/Training/Requirement` a `/sport/Training/Plan` je zapojena stejná
ViewComponent pro porovnání hodin trenérů. Požadované hodiny se sčítají
z přiřazení trenér–role, plánované hodiny z délky každého plánu přiřazeného
tomuto trenérovi a roli. Porovnání používá sezónu, kód kategorie, ID typu,
ID trenéra a ID role; chybějící strana se doplní nulou.

Komponenta dostává Contract DTO a ID vybraných trenérů, provádí pouze
prezentační agregaci a nevytváří databázové dotazy. Porovnávací načtení
respektuje aktivní sezónu, kategorie, typ, datum a výběr trenérů; záměrně
ignoruje fázi, den a lokalitu podle klíče issue. Původní data hlavních
tabulek a rozvrhu zůstávají filtrována dosavadním způsobem.

Statusy zobrazují ikonu, text a přístupný tooltip; stavy používají existující
sémantické barevné tokeny. Hodiny se vykreslují s českou desetinnou čárkou
a dvěma místy u desetinných hodnot. Databázový model, snapshot ani migrace
nebyly změněny.

## Implementované změny

- Rozšířeny `TrainingPlanScheduleItemDto` a `TrainingPlanCoachDto` o
  identifikátory/popisky potřebné pro stabilní párování a tabulku.
- Rozšířena projekce `TrainingScheduleService.GetTrainingPlansAsync`;
  zachovává stávající platnost a identitní přiřazení trenérů.
- Přidán `TrainingRequirementCoverageBuilder` s agregací hodin/minut,
  zaokrouhlením plánovaných hodin, full outer joinem, statusy a českým
  řazením.
- Přidána bezdatabázová `TrainingRequirementCoverageViewComponent`, sdílené
  Razor view a SCSS.
- Oba PageModely načítají samostatné porovnávací sady, aby filtrování fáze,
  dne či lokality neměnilo hlavní přehled.
- Aktualizována kanonická dokumentace modulu Sport.

## Změněné soubory

- `src/SportSys.Contract/Models/TrainingScheduleDto.cs`
- `src/SportSys.Contract/Services/TrainingScheduleService.cs`
- `src/SportSys.Razor/Models/TrainingRequirementCoverage.cs`
- `src/SportSys.Razor/ViewComponents/TrainingRequirementCoverageViewComponent.cs`
- `src/SportSys.Razor/Pages/Shared/Components/TrainingRequirementCoverage/Default.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs`
- `src/SportSys.Razor/Styles/_training-requirement-coverage.scss`
- `src/SportSys.Razor/Styles/site.scss`
- `tests/SportSys.Razor.Tests/TrainingRequirementCoverageTests.cs`
- `tests/SportSys.Razor.Tests/TrainingPlanCoachMappingTests.cs`
- `tests/SportSys.Razor.Tests/TrainingRequirementTests.cs`
- `docs/modules/sport.md`

## Ověření původní implementace

- Kompletní `SportSys.Razor.Tests`: 310 úspěšných testů, 0 selhání.
- `dotnet build SportSys.slnx --no-restore`: úspěšný, 0 chyb.
- `npm run build:css`: úspěšný.
- `git diff --check`: bez chyb.
- Build nadále vypisuje upozornění SDK Preview, existující upozornění
  zranitelností závislostí a nullable upozornění mimo změněné soubory.

Manuální kontrola v prohlížeči a překlad rozšířené projekce proti živému
SQL Serveru nebyly provedeny. Automatické testy ověřují agregaci, statusy,
formátování, HTML enkódování, vykreslení komponenty a mapování role.

## Doplnění podle komentáře z 21. 10. 2026

[Komentář k #40](https://github.com/vasekNaus/SportSys/issues/40#issuecomment-6310598724)
mění prezentaci stavu `Plán > Požadavek`. Sdílený model nyní vrací text
„Nad plán“ a ikonu `fa-triangle-exclamation` místo textu „Splňuje plán“
a zelené fajfky. Ikona používá nový oranžový sémantický token
`--color-warning-icon` s variantami pro světlý, tmavý i automatický tmavý
režim; stávající varovné tokeny zůstávají nedotčené.

Změna platí pro obě stránky i pro plán bez požadavku. Výpočet hodin,
párování, ostatní statusy a tooltip se nemění; přístupný popisek přebírá
nový název stavu.
Regresní test renderu pokrývá všechny tři varianty včetně absence původní
fajfky a textu. Aktualizován je i implementační plán a dokumentace modulu.

Doplnění prošlo cíleným výběrem `TrainingRequirementCoverageTests`:
14 úspěšných testů, 0 selhání. Příkaz současně sestavil Razor projekt
a přeložil SCSS. Kontrola generovaného CSS potvrdila použití nového tokenu
ve všech režimech; minimální ověřený kontrast oranžové ikony vůči povrchům
tabulky včetně hoveru je 4,66:1 ve světlém a 6,18:1 v tmavém režimu,
tedy nad požadovanými 3:1 pro ikony. Vizuální kontrola v prohlížeči nebyla
provedena.
