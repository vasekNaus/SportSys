# Implementační plán: #35 Rozšíření číselníku CoachRole

**Issue:** [#35 — Rozšíření číselníku CoachRole](https://github.com/vasekNaus/SportSys/issues/35)

**Stav:** Připraveno k implementaci.

## Cíl

Rozšířit `dbo.CoachRole` o jednoznakový kód `Code` a pořadí `Order` a pořadí
rolí v aplikaci určovat hodnotou `CoachRole.Order` místo řazení podle
názvu/ID. `Code` se zatím jen zpřístupní (model, DTO) pro budoucí vizualizace
a exporty.

## Výchozí stav

- Entita `src/SportSys.Database/Models/dbo/CoachRole.cs`: `Id`, `Name`
  (`[StringLength(50)]`) + kolekce `CoachTrainingRequirementCoachRoles`,
  `CoachMatchRequirements`.
- Seed: `CoachRole.Seed.cs` (ctor `CoachRole(ECoachRole id)`, název z
  resource `ECoachRole`, cs), `Configurations/dbo/CoachRoleConfiguration.cs`
  (`HasData(Enum.GetValues<ECoachRole>().Select(e => new CoachRole(e)))`),
  enum `Enums/ECoachRole.cs` (Head=1, Assistant=2, Defenders=3,
  Goalkeepers=4, Physiotherapist=5, Conditioning=6).
- Role se dnes používají pouze v požadavcích (`CoachTrainingRequirement`,
  `CoachMatchRequirement`). Tréninkové plány a tréninky `CoachRole` nepoužívají
  (ověřeno vyhledáním v `src`) – issue je zmiňuje, ale není co řadit.
- Řazení rolí dnes:
  - `RequirementEditHelper.GetRolesAsync` – `OrderBy(role => role.Id)` (combobox),
  - `TrainingRequirementService` (~ř. 145–155) a `MatchRequirementService`
    (~ř. 60–70) – `ThenBy(assignment.CoachRole.Name)` / `ThenBy(CoachRoleId)`,
  - `src/SportSys.Razor/Models/RequirementCoachGroups.cs` –
    `OrderBy(group => group.Key)` (`CoachRoleId`).
- Vzor sloupce `Order` v modelu: `SeasonCategory.Order` (`public int Order`);
  vzor `Code`: `[StringLength] [Unicode(false)]`.
- Administrace číselníků v aplikaci neexistuje (žádná stránka v `Areas`).

## Potvrzené požadavky a rozhodnutí

Z issue:
- `Code char(1) NOT NULL`, `[Order] int NOT NULL` v `dbo.CoachRole`.
- Pořadí v comboboxech, tabulkách, rozvrzích a editorech podle `CoachRole.Order`.
- Editace rolí v požadavcích se nemění; `Code`/`Order` uživatel needituje.
- Naplnění hodnot řeší databázová migrace (vytvoří a aplikuje uživatel).

Potvrzeno uživatelem (hodnoty seedu):

| Role (ECoachRole) | Id | Code | Order |
|---|---|---|---|
| Head | 1 | H | 1 |
| Assistant | 2 | A | 2 |
| Defenders | 3 | O | 3 |
| Goalkeepers | 4 | B | 4 |
| Physiotherapist | 5 | F | 5 |
| Conditioning | 6 | K | 6 |

Technická rozhodnutí:
- `Code` v C# jako `string` s `[StringLength(1, MinimumLength = 1)]`,
  `[Column(TypeName = "char(1)")]`, `[Unicode(false)]`; unikátní index
  `[Index(nameof(Code), IsUnique = true)]` (kód má roli jednoznačně označit).
- Hodnoty `Code`/`Order` se do `HasData` dostanou přes `CoachRole.Seed.cs`
  (EF vyžaduje všechny NOT NULL hodnoty v seedu; jinak by migrace vložila
  `'\0'`/0). Mapování `ECoachRole -> (Code, Order)` je v jediné
  privátní metodě v `CoachRole.Seed.cs`.
- Sekundární řazení při shodném `Order`: `Id`.
- DTO `RequirementCoachListItem` rozšířit o `CoachRoleOrder` a `CoachRoleCode`.
- **Databázový model se mění** (2 nové sloupce + unikátní index).
  Migraci vytváří a aplikuje uživatel; agent ji nevytváří ani needituje
  snapshot. Migrace musí sloupce přidat s výchozími hodnotami pro existující
  řádky (např. přes `defaultValue` + `UpdateData`/`InsertData` ze seedu) –
  uživatel zkontroluje vygenerovaný skript.

## Technický návrh

1. **Database** – `CoachRole.cs`: přidat `Code` a `Order` (data atributy dle
   `docs/conventions.md`, bez Fluent API). `[Order]` je rezervované slovo; EF
   jej ve SQL kóduje automaticky.
2. **Seed** – `CoachRole.Seed.cs`: v ctoru `CoachRole(ECoachRole)` nastavit
   `Code` a `Order` z mapování výše; privátní bezparametrový ctor doplnit
   `Code = null!`.
3. **Contract** – řazení a DTO:
   - `RequirementEditHelper.GetRolesAsync`: `OrderBy(role => role.Order).ThenBy(role => role.Id)`.
   - `TrainingRequirementService` a `MatchRequirementService`: v řazení
     přiřazení nahradit `ThenBy(CoachRole.Name)` za `ThenBy(CoachRole.Order)`
     a doplnit `CoachRoleOrder`, `CoachRoleCode` do projekce.
   - `TrainingRequirementDto.cs`: `RequirementCoachListItem.CoachRoleOrder`,
     `CoachRoleCode`.
4. **Razor** – `RequirementCoachGroups.Group`: řadit skupiny podle
   `CoachRoleOrder`, pak `CoachRoleId`.
5. **Dokumentace** – `docs/conventions.md` (poznámka u lookup tabulek, že
   `CoachRole` má `Code` a `Order`) a `docs/modules/hr.md`/`sport.md`, kde se
   role popisují.

## Implementační kroky

### Fáze 1: Databázový model
- `CoachRole.cs`, `CoachRole.Seed.cs` (viz výše). `CoachRoleConfiguration`
  beze změny (seed jde přes ctor).
- Uživatel vytvoří migraci (`dotnet ef migrations add`) a zkontroluje ji.

### Fáze 2: Contract
- DTO + služby + `RequirementEditHelper`.

### Fáze 3: Razor
- `RequirementCoachGroups`.

### Fáze 4: Testy a dokumentace

## Soubory ke změně

- `src/SportSys.Database/Models/dbo/CoachRole.cs`
- `src/SportSys.Database/Models/dbo/CoachRole.Seed.cs`
- `src/SportSys.Contract/Models/TrainingRequirementDto.cs`
- `src/SportSys.Contract/Services/RequirementEditHelper.cs`
- `src/SportSys.Contract/Services/TrainingRequirementService.cs`
- `src/SportSys.Contract/Services/MatchRequirementService.cs`
- `src/SportSys.Razor/Models/RequirementCoachGroups.cs`
- `tests/SportSys.Razor.Tests/TrainingRequirementTests.cs`
- `docs/conventions.md`

## Testy a ověření

- Unit test `RequirementCoachGroups.Group`: skupiny řazené podle
  `CoachRoleOrder`, ne podle `CoachRoleId` (role s vyšším Id a nižším Order
  jde první).
- Test seedu: `new CoachRole(e)` pro všechny `ECoachRole` má neprázdný
  jednoznakový unikátní `Code` a unikátní `Order`.
- `dotnet build SportSys.slnx`;
  `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`.
- Pozn.: testovací projekt se dnes nepřekládá kvůli nesouvisející chybě v
  `CoachAttendanceTests.cs`; je nutné ji opravit nebo testy spouštět
  izolovaně.

## Manuální akceptace

1. Po aplikaci migrace obsahuje `dbo.CoachRole` sloupce `Code char(1) NOT NULL`
   a `Order int NOT NULL` s hodnotami dle tabulky.
2. Editace požadavku: combobox rolí je ve pořadí Order.
3. Přehled požadavků: role ve sloupci Trenéři jsou v pořadí Order.
4. Uložení a editace požadavků funguje beze změny.

## Beze změny

- Editační formuláře požadavků (role se vybírají jako dosud), validace
  přiřazení.
- Tréninky a tréninkové plány (nepoužívají `CoachRole`).
- `ECoachRole` hodnoty a Id, resource názvy rolí.
- `SportSys.Web`, `src\DB Model\*.sql` referenční skripty.

## Mimo rozsah

- Administrace číselníků (UI pro `Code`/`Order`).
- Využití `Code` ve vizualizacích, exportech a tiscích.
- Role „Vedoucí družstva“ (není v `ECoachRole`).
- Vytvoření/aplikace migrace agentem.

## Hotovo, když

- Model a seed obsahují `Code` a `Order`, projekt se přeloží.
- Všechna řazení rolí (combobox, přiřazení, seskupení v přehledu) používají
  `CoachRole.Order`.
- Testy projdou, dokumentace odpovídá stavu.
- Migraci vytvořil a aplikoval uživatel; agent žádnou nevytvořil.
