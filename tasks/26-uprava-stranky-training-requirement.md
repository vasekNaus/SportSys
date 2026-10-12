# Implementační plán: #26 Úprava stránky Training/Requirement

**Issue:** [#26 — Úprava stránky Training/Requirement](https://github.com/vasekNaus/SportSys/issues/26)

**Stav:** Připraveno k implementaci.

## Cíl

Upravit `Training/Requirement` tak, aby filtr a ovládání odpovídaly stránce
rozvrhu tréninků a zápasů, aby šlo samostatně zobrazit tréninky a zápasy,
a doplnit editaci požadavku včetně tabulky trenérů a rolí s validací.

## Výchozí stav

- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml(.cs)`:
	jeden plochý filtr (Sezóna, Typ tréninku, Fáze tréninku, Kategorie),
	`IndexModel` používá `TrainingRequirementService` a `MatchRequirementService`.
	Filtr nemá Datum ani přepínač Tréninky/Zápasy; tabulka nemá sloupec Akce.
- Vzor filtru: `Training/Schedule/Index.cshtml` (checkboxy `ShowTrainings`/`ShowMatches`,
	panely `schedule-filter-panel--common`, `data-multiselect`).
- Vzor Datum: `Training/Plan/Index.cshtml.cs` (`ValidOn`) a
	`TrainingScheduleService.ApplyValidityFilter` (From <= datum <= To).
- Vzor editace: `Training/Plan/Edit.cshtml(.cs)`, `TrainingPlanEditDto`,
	`TrainingPlanService.GetEditAsync/UpdateAsync` (+ `TrainingPlanUpdateResult`,
	concurrency `OriginalVersion`).
- Entity: `TrainingRequirement` + `CoachTrainingRequirement`,
	`MatchRequirement` + `CoachMatchRequirement`
	(PK = CoachId + RequirementId + CoachRoleId, tj. jeden trenér může být
	v DB ve více rolích; unikátnost trenéra musí hlídat validace).
- Číselník rolí: `CoachRole` (`ECoachRole`).
- Služby `TrainingRequirementService` a `MatchRequirementService` mají jen čtení.

## Potvrzené požadavky a rozhodnutí

- Filtr rozdělit na Společné (Sezóna, Fáze, Datum), Tréninky, Zápasy.
- Checkboxy Tréninky / Zápasy se zapínají nezávisle; určují sekce filtru i data.
- Datum = den, ke kterému jsou požadavky platné.
- Sekce Zápasy je v první verzi jen strukturální (bez specifických filtrů).
- Každý řádek přehledu má sloupec Akce s ikonou editace.
- Editace zahrnuje všechny editovatelné atributy modelu: `From`, `To`
  a `DurationHours` (tréninkový požadavek), resp. `From`, `To` a `MatchCount`
  (zápasový požadavek). Sezóna, kategorie, typ a fáze tréninku zůstávají
  jen ke čtení (identifikují požadavek).
- Editace vychází z Training/Plan, včetně trenérů;
	`Trenér | Role | Akce`, trenér searchable select, role select z číselníku,
	tlačítko „Přidat trenéra“ (prázdný řádek), ikona smazání řádku.
- Validace: Trenér i Role povinné; trenér max. jednou v požadavku (i v různých
	rolích). Hláška: „Trenér {jméno} je již k tomuto požadavku přiřazen. Jeden
	trenér může být v rámci požadavku přiřazen pouze jednou.“

## Technický návrh

Technická rozhodnutí (běžná, bez dotazu):

- **Fáze** je u zápasů neexistující (`MatchRequirement` nemá fázi); filtr Fáze
	ve společné sekci se proto použije jen na tréninkové požadavky (označit
	„jen tréninky“ jako dnes). Při zapnutých jen zápasech se Fáze ignoruje.
- **Typ tréninku** se přesune do sekce Tréninky; Fáze zůstává ve společné sekci
	dle issue.
- **Kategorie** zůstává pod společnými filtry (beze změny chování).
- **Datum** je nepovinné (`ValidOn`, `DateOnly?`); bez hodnoty se zobrazí vše.
- Editace: dvě stránky `Training/Requirement/EditTraining` a
	`Training/Requirement/EditMatch` (nebo jedna stránka s parametrem `kind`);
	doporučeno dvě stránky + sdílený partial `_CoachAssignmentsTable.cshtml`.
- Tabulka trenérů: model binding indexovaným seznamem
	`Input.CoachAssignments[i].CoachId/CoachRoleId`; přidání řádku klientským JS
	(šablona řádku) nebo handlerem `OnPostAddCoach` (server-side přidá prázdný
	řádek bez ztráty ostatních hodnot) — zvolit server-side handler pro shodu
	s Razor Pages bez nového JS; mazání handlerem `OnPostRemoveCoach(index)`.
- Persist: nahradit sadu `CoachTrainingRequirements` / `CoachMatchRequirements`
	požadavku novou sadou v jedné transakci (`SaveChanges`).
- Editují se atributy požadavku (`From`, `To`, `DurationHours` / `MatchCount`)
	i trenéři v jednom formuláři a jedné transakci. Atributy mají data atributy
	(`[DataType(DataType.Date)]`, `[Display]`, `[Range]`) a `IValidatableObject`
	kontroluje `From <= To`; `DurationHours` > 0 a max. 999,99 (`decimal(5, 2)`),
	`MatchCount` >= 0 (ověřit proti existujícím datům/seedu).
- Změna databázového modelu: **není potřeba**. Migrace se nevytváří.

## Implementační kroky

### Fáze 1: Contract (služby a DTO)

1. `TrainingRequirementService.GetAllAsync` a `MatchRequirementService.GetAllAsync`:
	 přidat parametr `DateOnly? validOn` (`From <= validOn && To >= validOn`).
2. Nová DTO v `src/SportSys.Contract/Models/`: `TrainingRequirementEditDto`
	 (From, To, DurationHours) a `MatchRequirementEditDto` (From, To, MatchCount),
	 obě se sdíleným seznamem trenérů:
	 (Id, hlavička jen pro zobrazení, `List<RequirementCoachAssignmentInput>`
	 s `CoachId?` a `CoachRoleId?`), implementace `IValidatableObject`
	 (povinnost obou hodnot, duplicitní trenér, `From <= To`, rozsah
	 `DurationHours`/`MatchCount`).
	 skládat ve službě/PageModelu, který zná `DisplayName`.
3. Do služeb přidat `GetEditAsync(int id)` a `UpdateAsync(dto)` vracející
	 result enum (`Success`, `NotFound`, `InvalidInput`, `DuplicateCoach`)
	 obdobně jako `TrainingPlanUpdateResult`. Seznam trenérů (`CoachSelectItem`)
	 a rolí (`CoachRole`, `LookupSelectItem`) načíst ve službě. Duplicitu
	 a prázdné hodnoty kontrolovat i na serveru.

### Fáze 2: Razor – přehled

4. `Index.cshtml.cs`: přidat `ShowTrainings` (výchozí true), `ShowMatches`
	 (výchozí false nebo true — viz Schedule `IndexModel`, převzít stejné výchozí
	 chování), `ValidOn`; předat `validOn` službám; nenačítat data vypnuté skupiny.
5. `Index.cshtml`: přestavět filtr podle `Schedule/Index.cshtml`:
	 fieldset „Zobrazit“ s checkboxy, panel „Společné filtry“ (Sezóna, Fáze, Datum,
	 Kategorie), panel „Tréninky“ (Typ tréninku), panel „Zápasy“ (prázdný
	 placeholder s textem). Panely zobrazovat podle checkboxů.
6. `Index.cshtml`: ke každé tabulce (tréninky, zápasy) přidat poslední sloupec
	 „Akce“ s odkazem na editaci (ikona `fa-pen`, jako jinde v aplikaci, např.
	 `Training/Plan/_TrainingPlanTable.cshtml`).
7. Styly: použít existující třídy `schedule-filter-*` z `_schedule.scss`;
	 `site.css` neupravovat. Případné nové třídy jen v SCSS + `npm run build:css`.

### Fáze 3: Razor – editace

8. Vytvořit `EditTraining.cshtml(.cs)` a `EditMatch.cshtml(.cs)` podle
	 `Training/Plan/Edit` (TempData `StatusMessage`, NotFound, reload po chybě).
9. Do formulářů přidat pole Platnost od, Platnost do a Rozsah v hodinách
   (`EditTraining`), resp. Počet zápasů (`EditMatch`); hlavičku (sezóna,
   kategorie, typ, fáze) zobrazit read-only.
10. Vytvořit partial tabulky trenérů:
	 `<search-multiselect>`/searchable select, pokud podporuje jednovýběr
	 (ověřit; jinak `<select>` s `asp-items`), select role, tlačítko smazání,
	 tlačítko „Přidat trenéra“.
11. Handlery `OnPostAddCoach`
		mapování výsledků na `ModelState` s hláškou z issue.

## Soubory ke změně

- `src/SportSys.Contract/Services/TrainingRequirementService.cs`
- `src/SportSys.Contract/Services/MatchRequirementService.cs`
- `src/SportSys.Contract/Models/` (nové DTO a result enum)
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml`, `.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/` (nové Edit stránky a partial)
- `tests/SportSys.Razor.Tests/TrainingRequirementTests.cs` + nové testy

## Testy a ověření

- Služby: filtr `validOn` (hranice From/To, null), čtení bez zápasů/tréninků.
- Update: změna From/To/DurationHours/MatchCount, `From > To`, mimo rozsah,
  uložení více trenérů,
	prázdný trenér/role, neexistující requirement, neexistující role/trenér.
- DTO validace: povinné hodnoty, duplicity.
- PageModel: `ShowTrainings/ShowMatches` kombinace, normalizace parametrů.
- `dotnet build SportSys.slnx`, `dotnet test tests\SportSys.Razor.Tests`.

## Manuální akceptace

- Všechny kombinace checkboxů mění sekce filtrů i tabulky.
- Datum 15. 10. 2026 zobrazí jen platné požadavky.
- Editace: přidat/odebrat řádek, uložit, duplicita zobrazí hlášku z issue,
	nekompletní řádek nelze uložit.

## Beze změny

- Databázový model, migrace, `SportSys.Web`, `site.css`, stránky Training/Plan
	a Training/Schedule.

## Mimo rozsah

- Specifické filtry zápasů.
- Vytváření/mazání samotných požadavků a změna sezóny, kategorie, typu a fáze.

## Otevřené body

- Výchozí hodnota `ShowMatches` — převzít ze Schedule.

## Hotovo, když

- Splněna všechna akceptační kritéria issue #26.
- Build a testy projdou, žádná migrace nebyla vytvořena.
