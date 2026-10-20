# Implementační plán: #33 Přehled požadavků

**Issue:** [#33 — Přehled požadavků](https://github.com/vasekNaus/SportSys/issues/33)

**Stav:** Připraveno k implementaci.

## Cíl

Ve sloupci **Trenéři** na stránce přehledu požadavků agregovat trenéry podle
role (typu). Každá role je na samostatném řádku: tučný název role s dvojtečkou
a čárkou oddělený seznam příjmení trenérů. Názvy rolí mají fixní šířku, takže
seznamy jmen jsou zarovnané pod sebou.

```text
Hlavní:      Nauš, Barčák
Asistent:    Klaus
Kondiční:    Vlasák
```

## Výchozí stav

- Stránka: `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml`
  (+ `Index.cshtml.cs`). Obsahuje dvě tabulky `table.grid` – požadavky na
  tréninky a na zápasy – každá se sloupcem „Trenéři“.
- Obě buňky dnes renderují plochý seznam
  `string.Join(", ", requirement.CoachAssignments.Select(a => a.DisplayText))`,
  kde `DisplayText` je `"Jméno (Role)"` (`RequirementCoachListItem`
  v `src/SportSys.Contract/Models/TrainingRequirementDto.cs`).
- `TrainingRequirementCoachListItem` a `MatchRequirementCoachListItem`
  (`MatchRequirementDto.cs`) dědí z `RequirementCoachListItem` ->
  `CoachSelectItem` (`DisplayName`, `PersonalNumber`, `CoachRoleId`,
  `CoachRoleName`).
- Služby `TrainingRequirementService` (~ř. 150–170) a `MatchRequirementService`
  (~ř. 70–85) plní přiřazení seřazená podle jména, osobního čísla a role.
- `User` (`src/SportSys.Database/Models/identity/User.cs`) má jen
  `DisplayName` – samostatné příjmení neexistuje. Existující vzor pro
  příjmení: `MatchScheduleService.ExtractLastName` (poslední slovo
  `DisplayName`).
- Role: `ECoachRole` (Head=1, Assistant=2, Defenders=3, Goalkeepers=4,
  Physiotherapist=5, Conditioning=6), tabulka `dbo.CoachRole`, `Name` z
  resource `ECoachRole` (cs).
- Existující test `tests/SportSys.Razor.Tests/TrainingRequirementTests.cs`
  ověřuje `DisplayText`.
- `table.grid` styly: `src/SportSys.Razor/Styles/_grid.scss`; `site.css` se
  negeneruje ručně.

## Potvrzené požadavky a rozhodnutí

Z issue:
- Agregace podle typu (role) trenéra, typ tučně, za ním čárkou oddělený seznam.
- Každý typ na novém řádku.
- Fixní šířka textu typu -> první jména pod sebou.

Technická rozhodnutí:
- Seskupení je čistě prezentační; provede se v Razoru/Contractu nad stávajícími
  daty, **bez změny databázového modelu** a bez migrace.
- Zobrazuje se **příjmení** (dle příkladu v issue), odvozené z `DisplayName`
  stejným pravidlem jako `ExtractLastName`; fallback při prázdném jménu je
  `PersonalNumber`.
- Popisek role je `CoachRoleName` (dnešní text z `dbo.CoachRole`); příklad
  v issue („Hlavní“, „Asistent“, „Kondiční“) je ilustrativní. Zkracování názvů
  rolí není součástí tohoto issue.
- Pořadí rolí podle `CoachRoleId` (Hlavní, Asistent, …); trenéři uvnitř role
  podle příjmení (`StringComparer` s `cs-CZ`).
- Stejné chování pro tabulku tréninků i zápasů (sdílený partial).
- Fixní šířka popisku řešena CSS (`min-width` v `ch` / grid dvou sloupců),
  ne mezerami v textu.

## Technický návrh

1. **Contract** – do `RequirementCoachListItem` přidat vlastnost
   `LastName` (odvozenou z `DisplayName`/`PersonalNumber`; `DisplayText` beze
   změny, používá se jinde).
2. **Razor model** – nový typ `RequirementCoachGroup`
   (`RoleId`, `RoleName`, `LastNames`) a statická pomocná metoda
   `Group(IEnumerable<RequirementCoachListItem>)` (např. v
   `src/SportSys.Razor/Models/RequirementCoachGroups.cs`), která seskupí
   podle `CoachRoleId`, seřadí role i příjmení a odstraní duplicity jmen
   v rámci role.
3. **Partial** `Areas/sport/Pages/Training/Requirement/_CoachGroups.cshtml`
   s `@model IReadOnlyList<RequirementCoachListItem>`; při prázdném seznamu
   vypíše `-`. Struktura:

   ```html
   <div class="coach-groups">
     <div class="coach-group">
       <strong class="coach-group-role">Hlavní:</strong>
       <span class="coach-group-names">Nauš, Barčák</span>
     </div>
   </div>
   ```
4. **Index.cshtml** – obě buňky „Trenéři“ nahradit `<partial>`.
5. **SCSS** – nový `src/SportSys.Razor/Styles/_coach-groups.scss`
   (`@use` v `site.scss`), `.coach-group { display: grid;
   grid-template-columns: 12ch 1fr; gap: .5rem; }`, `.coach-group-role
   { white-space: nowrap; }` (šířka upravitelná proměnnou dle nejdelšího
   názvu role, ověřit vizuálně), `.coach-group-names { overflow-wrap: anywhere; }`;
   barvy jen z design tokenů (viz `barevna-schemata`). Poté
   `npm run build:css` (generovaný `site.css` se necommituje ručně, pokud to
   projekt řeší buildem – ověřit stávající praxi).

## Implementační kroky

### Fáze 1: Contract a model
- Přidat `LastName` do `RequirementCoachListItem`.
- Přidat `RequirementCoachGroup` + seskupovací logiku.

### Fáze 2: UI
- Partial `_CoachGroups.cshtml`, úprava `Index.cshtml` (tréninky i zápasy).
- SCSS a build CSS.

### Fáze 3: Testy
- Viz níže.

## Soubory ke změně

- `src/SportSys.Contract/Models/TrainingRequirementDto.cs`
- `src/SportSys.Razor/Models/RequirementCoachGroups.cs` (nový)
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/_CoachGroups.cshtml` (nový)
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml`
- `src/SportSys.Razor/Styles/_coach-groups.scss` (nový), `Styles/site.scss`
- `tests/SportSys.Razor.Tests/TrainingRequirementTests.cs`

## Testy a ověření

- Unit testy seskupení: více rolí -> správné pořadí rolí; více trenérů v roli
  seřazeno podle příjmení; trenér s více rolemi je v každé; prázdný seznam;
  prázdný `DisplayName` -> `PersonalNumber`; jednoslovné jméno.
- `dotnet build SportSys.slnx`
- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`
- `npm run build:css` v `src\SportSys.Razor`.

## Manuální akceptace

1. Přehled požadavků, požadavek se třemi rolemi -> tři řádky, role tučně,
   příjmení pod sebou zarovnaná.
2. Požadavek bez trenérů -> `-`.
3. Tabulka zápasů se chová shodně.
4. Dlouhý seznam jmen se zalomí bez rozbití zarovnání popisků.
5. Světlý i tmavý režim čitelný.

## Beze změny

- Databázový model, migrace, `TrainingRequirementService` /
  `MatchRequirementService` (data se seskupují v prezentaci).
- Editační stránky požadavků a `DisplayText`.
- `SportSys.Web`.

## Mimo rozsah

- Zkracování/přejmenování rolí v `dbo.CoachRole`.
- Samostatné pole příjmení v entitě `User`.
- Filtry stránky.

## Hotovo, když

- Sloupec Trenéři v obou tabulkách zobrazuje role tučně, každou na řádku,
  s příjmeními oddělenými čárkou a zarovnanými pod sebou.
- Testy a build projdou, SCSS je přeložen.
- Nebyla vytvořena žádná migrace.
