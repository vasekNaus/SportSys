# Implementační plán: #34 Rozšíření evidence rolí trenérů

**Issue:** [#34 — Rozšíření evidence rolí trenérů](https://github.com/vasekNaus/SportSys/issues/34)

**Stav:** Implementováno; databázovou migraci vytváří uživatel.

## Cíl

Sjednotit evidenci rolí trenérů v požadavcích, tréninkových plánech a
skutečných trénincích. U každého přiřazení trenéra k plánu nebo tréninku
evidovat roli z `dbo.CoachRole`, zachovat stav docházky a zabránit
opakovanému přiřazení stejného trenéra ke stejnému plánu či tréninku.

Rozšířit vazbu `sport.CoachTrainingRequirement` o editovatelný počet hodin
konkrétního trenéra a role v požadavku.

## Výchozí stav

- Číselník `dbo.CoachRole` již existuje a obsahuje `Id`, `Name`, `Code` a
  `Order`. Role se řadí podle `Order`; jejich seed zahrnuje hodnoty
  `ECoachRole`, včetně `Head` s ID 1.
- Požadavky na tréninky již propojují trenéra a roli přes entitu
  `CoachTrainingRequirement` v
  `src/SportSys.Database/Models/sport/CoachTrainingRequirement.cs`.
  Vazba má složený klíč `CoachId + TrainingRequirementId + CoachRoleId`;
  vlastnost pro počet hodin zatím nemá.
- Editace požadavků a přiřazení trenérů se obsluhují přes
  `TrainingRequirementService`. Přehled je na
  `Areas/sport/Pages/Training/Requirement/Index.cshtml`; modulová dokumentace
  jej nyní popisuje jako read-only.
- Editace trenérů u plánů používá `TrainingPlanEditDto` a
  `TrainingPlanService`. DTO nyní přijímá `SelectedCoachIds` a pro spojené
  plány kolekci `MemberCoachAssignments` s `CoachIds`; roli nepřenáší.
- Reálný trénink používá entitu `CoachTraining` v `sport.CoachTraining`.
  Obsahuje trenéra a stav účasti `ParticipationTypeId`. Editace je v
  `Areas/sport/Pages/Training/Schedule/Edit.cshtml` a její Contract službou je
  `TrainingService`.
- Existující editace tréninku načítá a ukládá přiřazení trenérů včetně
  docházky; nový údaj role musí zachovat transakční ukládání, ochranu proti
  souběžné změně a pravidla pro skupinové tréninky.
- `SportSys.Razor` přistupuje k datům výhradně přes `SportSys.Contract`.

## Potvrzené požadavky a rozhodnutí

Z issue:

- Každé přiřazení trenéra k tréninkovému plánu i skutečnému tréninku obsahuje
  `Coach` a `CoachRole`.
- Docházka zůstává zachována; při skutečném tréninku je její stav nadále
  součástí přiřazení trenéra.
- Jeden trenér smí být v rámci jednoho plánu nebo jednoho tréninku přiřazen
  pouze jednou, bez ohledu na zvolenou roli.
- Požadavková vazba obsahuje `Hours decimal(5,2) NOT NULL`; hodnotu lze editovat
  a musí být validována i uložena.

Potvrzeno uživatelem pro historická data:

- Historická přiřazení trenérů k plánům a skutečným tréninkům dostanou roli
  `Head` (`CoachRole.Id = 1`).
- Existující vazby `CoachTrainingRequirement` dostanou `Hours = 0,00`.
  Celkové hodiny požadavku nelze bez potvrzených dat spolehlivě rozdělit mezi
  více trenérů.

Technická rozhodnutí:

- Přidat povinný FK `CoachRoleId` a navigaci `CoachRole` do obou vazebních
  entit: `sport.CoachTrainingPlan` i `sport.CoachTraining`. Obě tabulky budou
  odkazovat na `dbo.CoachRole.Id`. U `CoachTraining` zůstává
  `ParticipationTypeId` beze změny; role přidává
  identifikaci toho, v jaké roli trenér tréninku participuje.
- Nové i editované hodnoty rolí ověřovat v Contract službě proti existujícímu
  číselníku. UI nabízí role seřazené podle `CoachRole.Order`.
- Unikátnost trenéra vynucovat v Contract službách v rámci příslušného
  `TrainingPlanId` nebo `TrainingId`, nezávisle na roli. U skutečných
  tréninků zohlednit všechny členy spojené skupiny podle stávajícího chování
  `TrainingService`.
- `Hours` bude nezáporné `decimal` s přesností `decimal(5,2)` a rozsahem
  `0,00–999,99`. Nulová hodnota je platná, mimo jiné kvůli bezpečnému
  backfillu historických vazeb.
- Databázový model se mění. Agent upraví entity a potřebnou EF Core
  konfiguraci, ale nevytvoří ani neupraví migraci či model snapshot. Uživatel
  vytvoří a zkontroluje migraci včetně backfillu rolí, hodin a případných
  unikátních indexů.

## Technický návrh

### Databázový model a historická data

1. Rozšířit entitu `CoachTrainingPlan` a tabulku `sport.CoachTrainingPlan`
   o povinný `CoachRoleId` s FK na `dbo.CoachRole.Id` a navigaci `CoachRole`.
   Role tedy patří konkrétnímu přiřazení trenéra k plánu, nikoli samotnému
   `TrainingPlan`.
2. Rozšířit `CoachTraining` o povinnou navigaci/FK na `CoachRole`; zachovat
   stávající stav docházky a poznámku.
3. Rozšířit `CoachTrainingRequirement` o povinné `Hours` typu `decimal(5,2)`.
   Použít atributy odpovídající konvencím projektu pro přesnost a typ SQL.
4. Deklarovat indexy nových FK explicitně atributem `[Index]`, protože
   konvence automatického vytváření FK indexů je vypnutá. U unikátnosti
   trenéra použít unikátní index pouze tehdy, pokud jej lze zavést bez
   konfliktu se stávajícím klíčem a historickými daty; aplikační validace
   zůstává povinná.
5. Pro migraci existujících řádků nastavit `CoachRoleId = 1` a
   `Hours = 0,00`. Hodnoty jsou podloženy potvrzeným rozhodnutím výše; po
   migraci nesmí vzniknout NULL FK ani NULL hodiny.

### Contract a ukládání

- Rozšířit `TrainingPlanEditDto` o role přiřazených trenérů; u spojených plánů
  musí vstup zachovat přiřazení ke konkrétnímu členu plánu i jeho roli.
- Rozšířit `TrainingPlanService` o načítání a ukládání
  `CoachTrainingPlan.CoachRoleId` a validaci, že trenér není pro stejný plán
  uveden vícekrát.
- Rozšířit `TrainingEditDto`/kontext o `CoachRoleId` na každém řádku
  přiřazení. `TrainingService` musí načíst a uložit roli společně s trenérem,
  tréninkem a `ParticipationTypeId`; odmítnout neplatnou roli a duplicitu
  trenéra v rozsahu daném existující editací/skupinou.
- Rozšířit data pro úpravu požadavku tak, aby každý řádek
  `CoachTrainingRequirement` přenášel a ukládal `Hours`. `TrainingRequirementService`
  ověří nezápornou hodnotu, maximální hodnotu 999,99, počet desetinných míst
  a existenci editované vazby/role.
- Chyby validace vracet jako explicitní výsledky Contract služby a zobrazit
  je u příslušného formuláře. Zachovat atomické ukládání a ochranu proti
  souběžným změnám tam, kde je používá stávající služba.

### Razor UI

- Editace tréninkového plánu zobrazí roli u každého trenéra jako výběr z
  `CoachRole`; u spojených plánů zůstane výběr specifický pro člena skupiny.
- Editace skutečného tréninku přidá sloupec „Role“ k dosavadní tabulce
  trenérů. U stavu docházky se nezmění dostupné hodnoty ani způsob editace.
- V přehledu požadavků na tréninky zobrazit u každého trenéra a jeho role
  editovatelný počet hodin. Uložit hodnotu přes Contract službu, nikoli
  přímým přístupem Razor k EF entitě.
- Při validační chybě znovu načíst role a zachovat již zadané hodnoty stejně
  jako ostatní výběrová data formuláře.

## Implementační kroky

### Fáze 1: Databázové entity

1. V `CoachTrainingPlan.cs` přidat povinný `CoachRoleId`, navigaci `CoachRole`
   a explicitní FK index; tím rozšířit tabulku `sport.CoachTrainingPlan`
   o referenci na `dbo.CoachRole`.
2. Přidat FK a navigaci `CoachRole` do `CoachTraining`; zachovat vazbu na
   `ParticipationType`.
3. Přidat `Hours` do `CoachTrainingRequirement` s přesností `decimal(5,2)`.
4. Doplnit explicitní FK indexy a ověřit, že datové anotace odpovídají
   `docs/conventions.md`.
5. Nepřidávat ani neupravovat EF Core migraci nebo snapshot.

### Fáze 2: Contract modely a služby

1. Rozšířit DTO pro editaci plánu a jeho členů o role trenérů.
2. Rozšířit načtení, validaci a transakční uložení v `TrainingPlanService`;
   odmítnout opakovaného trenéra bez ohledu na roli.
3. Rozšířit DTO a `TrainingService` o roli na existujících i nových
   přiřazeních skutečného tréninku; zachovat `ParticipationTypeId`, poznámku,
   skupinové ukládání a concurrency kontrolu.
4. Rozšířit model požadavku a `TrainingRequirementService` o editaci `Hours`
   v rámci vazby trenér-role. Validovat rozsah a existenci požadavku, trenéra
   i role.

### Fáze 3: Razor formuláře

1. Upravit `Training/Plan/Edit` pro výběr role u každého trenéra, včetně
   formuláře pro členy spojeného plánu.
2. Upravit `Training/Schedule/Edit` o sloupec Role a zachovat existující
   editaci docházky.
3. Rozšířit přehled požadavků na tréninky o editaci hodin pro jednotlivé
   přiřazené trenéry a o zobrazení validačních chyb.
4. Všechny výběry rolí naplnit z Contract DTO a řadit podle `CoachRole.Order`.

### Fáze 4: Testy a dokumentace

1. Doplnit testy validace, načtení a uložení role pro plány a skutečné
   tréninky; pokrýt odmítnutí duplicitního trenéra v různých rolích.
2. Doplnit testy požadavkových hodin pro `0`, desetinné hodnoty, horní limit,
   zápornou hodnotu a hodnotu s přílišnou přesností.
3. Aktualizovat `docs/modules/sport.md` a podle potřeby `docs/conventions.md`
   o nových vztazích a invariantách.
4. Spustit relevantní testy a build podle příkazů projektu.

### Fáze 5: Migrace uživatelem

1. Uživatel vytvoří migraci až po kontrole změněného EF modelu.
2. Ověří přidání povinné reference na `dbo.CoachRole` do obou tabulek
   `sport.CoachTrainingPlan` a `sport.CoachTraining` a backfill
   `CoachRoleId = 1` u jejich historických přiřazení. U historických vazeb
   `sport.CoachTrainingRequirement` ověří `Hours = 0,00`.
3. Zkontroluje FK/indexy, NOT NULL omezení a případný unikátní index proti
   existujícím datům; migraci sám aplikuje.

## Soubory ke změně

| Akce | Soubor |
|---|---|
| upravit | `src/SportSys.Database/Models/sport/CoachTrainingPlan.cs` |
| upravit | `src/SportSys.Database/Models/sport/CoachTraining.cs` |
| upravit | `src/SportSys.Database/Models/sport/CoachTrainingRequirement.cs` |
| upravit | `src/SportSys.Contract/Models/TrainingPlanEditDto.cs` |
| upravit | `src/SportSys.Contract/Services/TrainingPlanService.cs` |
| upravit | `src/SportSys.Contract/Models/TrainingEditDto.cs` |
| upravit | `src/SportSys.Contract/Services/TrainingService.cs` |
| upravit | `src/SportSys.Contract/Services/TrainingRequirementService.cs` |
| upravit | `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml` a `.cshtml.cs` |
| upravit | `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Edit.cshtml` a `.cshtml.cs` |
| upravit | `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml` a `.cshtml.cs` |
| upravit | `tests/SportSys.Razor.Tests/TrainingRequirementTests.cs` a odpovídající testy služeb/DTO |
| upravit | `docs/modules/sport.md` |

## Testy a ověření

- EF model `CoachTrainingPlan` obsahuje povinný FK `CoachRoleId` na
  `CoachRole` a explicitní FK index pro tabulku `sport.CoachTrainingPlan`.
- Načtení a uložení role pro samostatný i spojený tréninkový plán.
- Opakovaný trenér v jednom plánu se odmítne i při různých rolích; různí
  trenéři se stejnou rolí projdou.
- Načtení a uložení role při skutečném tréninku zachová původní stav
  docházky a poznámku.
- Duplicitní trenér v jednom tréninku se odmítne i při různých rolích; u
  spojené skupiny se ověří stejný rozsah unikátnosti jako ve stávající
  `TrainingService`.
- Neplatné nebo neexistující `CoachRoleId` se odmítne serverovou validací.
- Požadavek uloží hodiny pro konkrétní vazbu trenér-role; odmítne záporné
  hodnoty, hodnoty nad 999,99 a hodnoty s přesností vyšší než dvě desetinná
  místa. Hodnoty `0`, `0,50`, `2,25` a `999,99` jsou přijaty.
- Při chybě validace se formulář znovu zobrazí se seznamem rolí a bez ztráty
  zadaných hodnot.
- `dotnet build SportSys.slnx`
- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`

## Manuální akceptace

1. V editaci plánu přiřadit trenéra s rolí, uložit a ověřit správnou roli po
   opětovném načtení; totéž ověřit pro členy spojeného plánu.
2. Pokusit se přidat téhož trenéra k jednomu plánu pod jinou rolí a ověřit
   validační chybu bez částečného zápisu.
3. V editaci tréninku změnit roli trenéra a docházku, uložit a ověřit, že se
   obě hodnoty zachovaly.
4. Pokusit se přidat trenéra k témuž tréninku pod dvěma rolemi a ověřit
   odmítnutí duplicity.
5. V požadavku změnit hodiny přiřazeného trenéra, uložit hodnotu s desetinnou
   částí a ověřit její zobrazení i po opětovném načtení.
6. Ověřit validační hlášky pro zápornou a příliš vysokou hodnotu hodin.
7. Po vytvoření migrace uživatelem ověřit historické role `Head`, historické
   hodiny `0,00` a funkčnost existující docházky.

## Beze změny

- Hodnoty a lokalizace číselníku `CoachRole`; nepřidávat nové role.
- Číselník stavů docházky `ParticipationType` a jeho současné chování.
- Přiřazení trenérů k zápasům a `CoachMatchRequirement`.
- Evidence HR souborů `CoachAttendance` a import XLSX.
- Architektura `Razor → Contract → Database`; Razor nebude odkazovat na
  `SportSys.Database`.
- `SportSys.Web`, generované `wwwroot/css/site.css` a databázové migrace.

## Mimo rozsah

- Změna významu nebo historie docházkových stavů.
- Rozdělování celkového rozsahu hodin požadavku automaticky mezi trenéry.
- Mazání požadavků, změna ostatních polí požadavku nebo přepracování celé
  administrační stránky požadavků nad rámec editace hodin.
- Nové role, změna názvů rolí nebo administrace číselníku rolí.
- Vytváření nebo aplikace EF Core migrace agentem.

## Hotovo, když

- Entita i tabulka `sport.CoachTrainingPlan` obsahují povinnou referenci
  `CoachRoleId` na `dbo.CoachRole` a služba ji načítá i ukládá.
- Tréninkové plány a skutečné tréninky ukládají roli každého trenéra z
  existujícího `CoachRole`.
- Docházka u skutečného tréninku nadále funguje a role se ukládá spolu s
  přiřazením.
- Stejný trenér nemůže být v jednom plánu ani tréninku přiřazen vícekrát,
  ani pod různými rolemi.
- `CoachTrainingRequirement` ukládá editovatelné a validované
  `Hours decimal(5,2) NOT NULL`.
- Historické role a hodiny jsou připraveny pro backfill hodnotami potvrzenými
  v tomto plánu.
- Testy a build projdou, dokumentace odpovídá výslednému chování a nebyla
  vytvořena ani upravena migrace.
