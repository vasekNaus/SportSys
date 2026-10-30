# Implementační plán: #42 Vizualizace duplicitně přiřazených trenérů v tréninkových plánech

**Issue:** [#42 — Vizualizace duplicitně přiřazených trenérů v tréninkových plánech](https://github.com/vasekNaus/SportSys/issues/42)

**Stav:** Implementováno podle novějšího komentáře k issue; podrobnosti jsou v
`42-vizualizace-duplicitne-prirazenych-treneru-v-treninkovych-planech-IMPL.md`.

## Cíl

V grafickém rozvrhu tréninkových plánů označit chybovou ikonou trenéra, jehož
identita se ve výsledném bloku vyskytuje vícekrát. Kontrola musí fungovat pro
samostatné i spojené plány a nesmí měnit jejich současné pořadí ani význam rolí.

## Výchozí stav

- Issue je otevřené a má [komentář od `vnaus`](https://github.com/vasekNaus/SportSys/issues/42#issuecomment-6422123436):
  tučné označení není dostatečně viditelné, má se použít ikona značící chybu.
  Tento novější pokyn nahrazuje původní požadavek issue na tučný řez písma.
- [Další komentář od `vnaus`](https://github.com/vasekNaus/SportSys/issues/42#issuecomment-6450654482)
  upřesňuje, že ikona `fa-circle-exclamation` je špatně vidět a má se použít
  ikona ❌ jako u neúčasti v docházce. Tento nejnovější pokyn nahrazuje
  předchozí volbu ikony.
- `/sport/Training/Plan` sestavuje rozvrh v
  `Areas/sport/Pages/Training/Plan/Index.cshtml.cs` přes
  `EventModelFactory.CreateTrainingPlans`.
- `TrainingScheduleBlockFactory.CreateBlocks` již vytváří výsledné bloky
  podle `VisualizationGroupId ?? GroupId`. Pro plány ukládá všechna zdrojová
  přiřazení do `TrainingScheduleBlockData.PlanCoachAssignments`; při spojení
  se přiřazení trenéra napříč plány záměrně nededuplikují.
- `TrainingPlanCoachPresentation.Order` zachovává řazení podle
  `SeasonCategoryOrder`, `CoachRoleOrder` a českého porovnání příjmení.
  `TrainingPlanCoachDto` poskytuje stabilní `CoachId` i `CoachRoleId`.
- `EventModelFactory.CreateTrainingPlanBlock` převádí přiřazení na text
  `Příjmení (Role)` v `TrainingPlanEventModel.CoachSurnameSummary`.
  Sdílený partial `_ScheduleBlockContent.cshtml` nyní vykresluje tento text
  jako prostý řetězec pro plány i skutečné tréninky.
- Tabulka `_TrainingPlanTable.cshtml` zobrazuje samostatné řádky plánů,
  nikoli výsledné spojené bloky. Databázový model ani načítání přiřazení
  nejsou pro požadovanou vizualizaci potřeba měnit.

## Potvrzené požadavky a rozhodnutí

### Potvrzené požadavky z issue

- Duplicita se vyhodnocuje podle identity trenéra v rámci výsledného bloku.
- Kontrola platí i pro blok složený z více tréninkových plánů.
- Novější komentáře k issue nahrazují tučné písmo viditelnou ikonou ❌
  shodnou s neúčastí v docházce. Ostatní trenéři zůstávají bez ikony.
- Více duplicitních trenérů se zvýrazní nezávisle.
- Zvýraznění nemění současné řazení: `SeasonCategory.Order`,
  `CoachRole.Order` a příjmení.
- Tooltip není součástí tohoto požadavku.

### Technická rozhodnutí

- Počet výskytů počítat podle `CoachId`, nikoli podle jména nebo role.
  Jedna osoba se dvěma přiřazeními rolí je tedy duplicitní; dvě osoby se
  shodným zobrazovaným jménem se za duplicitu nepovažují.
- Vyhodnocení provést nad `PlanCoachAssignments` již sestaveného bloku.
  Tím stejná logika pokryje běžné plány, explicitně spojené skupiny
  i vizuální sloučení bez dalšího dotazu nebo opakování pravidel seskupení.
- Zachovat současný seznam přiřazení včetně opakovaných položek. Změní se
  pouze jejich vizuální označení; nevznikne nové pravidlo pro slučování
  nebo odstraňování trenérů a pořadí zůstane beze změny.
- Výsledek předat do view jako typovaný prezentační model s příznakem
  duplicity; nepřidávat HTML do modelu ani nepoužívat `Html.Raw`.
- V `_ScheduleBlockContent.cshtml` zobrazit za duplicitním přiřazením
  ikonu ❌ ze sdílené konstanty `CoachParticipationVisual.AbsenceIcon`
  se stejným markupem `schedule-block-coach-icon` jako v docházce
  a s přístupným popisem. Text trenéra zůstane normálním řezem písma.
- Skutečné tréninky a jejich seznamy účastí zůstanou beze změny.

## Technický návrh

Rozšířit prezentační helper
`TrainingPlanCoachPresentation` o metodu, která z již seřazených
`TrainingSchedulePlanCoachAssignment` vytvoří položky pro vykreslení.
Každá položka ponese existující DTO trenéra a příznak, zda se její
`CoachId` ve stejném výsledném bloku vyskytuje více než jednou. Metoda
zachová vstupní pořadí; `Format` může zůstat pro stávající textové použití
v tooltipu.

`TrainingPlanEventModel` dostane kolekci těchto položek. V
`CreateTrainingPlanBlock` se naplní ze stejného `block.PlanCoachAssignments`,
které už určují textový seznam. Plan-specific větev partialu vykreslí za
každou duplicitní položku ikonu ❌ (`CoachParticipationVisual.AbsenceIcon`)
v `span.schedule-block-coach-icon` s rolí `img`, `title` a přístupným
popisem; jméno zůstane obyčejným textem. Odsazení ikony zajistí
`.schedule-block-coach-duplicate` ve zdrojovém `_schedule.scss`.
Neduplicitní plány si zachovají stávající výpis a řazení.

## Implementační kroky

### Fáze 1: Výpočet duplicit v prezentační vrstvě

1. V `TrainingPlanCoachPresentation.cs` doplnit typovanou prezentační
   položku nebo odpovídající návratový model a metodu pro označení duplicit.
2. Počítat výskyty podle `CoachId` nad všemi přiřazeními v bloku; neřadit
   nově ani nededuplikovat výstup.
3. Doplnit příslušnou kolekci do `TrainingPlanEventModel` a naplnit ji
   v `EventModelFactory.CreateTrainingPlanBlock`.

### Fáze 2: Vykreslení pouze v blocích plánů

1. Upravit `Pages/Shared/Components/TrainingSchedule/_ScheduleBlockContent.cshtml`,
   aby pro `TrainingPlanEventModel` vykreslil přístupnou ikonu ❌ za každou
   duplicitní položkou, bez tučného písma.
2. Zachovat stávající prostý výpis pro `TrainingEventModel` a účastnické
   ikony skutečných tréninků.
3. Zachovat `CoachSurnameSummary` pro současný text tooltipu; tooltipy
   neupravovat. Do `Styles/_schedule.scss` doplnit pouze odsazení ikony.

### Fáze 3: Testy a dokumentace

1. Doplnit testy helperu a výsledného `TrainingPlanEventModel` pro jednotlivý
   plán i seskupené plány.
2. Ověřit vykreslení ikony, přístupného popisu a normálního řezu jména
   v editovatelné i needitovatelné variantě sdílené komponenty.
3. Aktualizovat část o rozvrhu tréninkových plánů v `docs/modules/sport.md`
   a zkompilovat SCSS přes `npm run build:css`.

## Soubory ke změně

- `src/SportSys.Razor/Models/TrainingSchedule/TrainingPlanCoachPresentation.cs`
- `src/SportSys.Razor/Models/TrainingSchedule/CoachParticipationVisual.cs`
- `src/SportSys.Razor/Models/TrainingSchedule/EventModel.cs`
- `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/_ScheduleBlockContent.cshtml`
- `src/SportSys.Razor/Styles/_schedule.scss`
- `tests/SportSys.Razor.Tests/TrainingPlanCoachPresentationTests.cs`
- `tests/SportSys.Razor.Tests/TrainingScheduleComponentModelTests.cs`
- `tests/SportSys.Razor.Tests/TrainingScheduleBlockFactoryTests.cs`
- `docs/modules/sport.md`

## Testy a ověření

- Prezentační helper: jedna položka trenéra zůstane bez ikony; opakovaný
  `CoachId` je označen jako duplicitní; různí `CoachId` se stejným jménem
  duplicitní nejsou.
- Více duplicitních trenérů je označeno samostatně; rozdílné role stejného
  trenéra se počítají jako více výskytů.
- Pořadí podle kategorie, role a českého příjmení zůstane stejné před
  i po označení duplicit.
- Výsledný model obsahuje duplicity pro běžný plán, skupinu podle `GroupId`
  i vizuální seskupení podle `VisualizationGroupId`.
- Plán bez trenérů nadále zobrazí `-`; tooltip používá nezměněný textový
  seznam; modely a účastnické zobrazení skutečných tréninků zůstávají
  nezměněné.
- Renderovací test ověřuje ikonu ❌, absenci `fa-circle-exclamation`, její přístupný
  popis, normální řez textu i zachování pořadí v obou variantách bloku.
- Spustit cílené testy:

  ```powershell
  dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --filter "FullyQualifiedName~TrainingPlanCoachPresentationTests|FullyQualifiedName~TrainingScheduleComponentModelTests|FullyQualifiedName~TrainingScheduleBlockFactoryTests"
  ```

  a zkompilovat styl:

  ```powershell
  Set-Location src\SportSys.Razor
  npm run build:css
  ```

## Manuální akceptace

1. Na `/sport/Training/Plan` zobrazit plán s jedním trenérem a ověřit formát
   `Příjmení (Role)` bez ikony ❌.
2. Ověřit trenéra přiřazeného vícekrát v jednom výsledném bloku; u každého
   jeho opakovaného přiřazení je za jménem viditelná ikona ❌, jméno není tučné
   a ostatní položky ikonu nemají.
3. Zapnout spojování plánů a ověřit duplicitu stejného trenéra napříč členy
   bloku; opakované položky zůstávají v původním pořadí a jsou označené ikonou.
4. Ověřit blok s více duplicitními trenéry i blok bez duplicit.
5. Porovnat řazení před a po zvýraznění a ověřit, že text tooltipu,
   tabulkový přehled a skutečné tréninky nejsou touto změnou ovlivněny.

## Beze změny

- Contract DTO, databázové entity, databázové schéma, EF Core snapshot
  a migrace.
- Načítání, validace a ukládání přiřazení trenérů.
- Spojování bloků, stávající řazení a pravidla platnosti plánů.
- Tooltipy, tabulkové řádky plánů a zobrazení skutečných tréninků.
- Ostatní SCSS a barevné tokeny; žádné nové schéma ani token.
- Generované `wwwroot/css/site.css` se upravuje pouze kompilací SCSS.
- `SportSys.Web`.

## Mimo rozsah

- Odstranění nebo sloučení duplicitních přiřazení.
- Nová validace při editaci plánů nebo oprava uložených dat.
- Tooltip, další barevná varování či jiné změny vizuálního designu.
- Změny databázového modelu, migrace nebo nová knihovna.

## Hotovo, když

- [x] Duplicity trenérů se vyhodnocují podle `CoachId` v celém výsledném bloku.
- [x] Běžné, explicitně spojené i vizuálně sloučené plány správně zvýrazní
  duplicitní trenéry.
- [x] Duplicitní přiřazení mají ikonu ❌ s přístupným popisem; jméno
  zůstává normálním řezem písma a neduplicitní položky ikonu nemají.
- [x] Pořadí a současný obsah seznamu trenérů se nezmění.
- [x] Skutečné tréninky, tooltipy a databázový model zůstávají nedotčené.
- [x] Procházejí cílené testy, vykreslení obou variant a CSS build.
