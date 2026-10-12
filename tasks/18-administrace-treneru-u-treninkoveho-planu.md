# Implementační plán: #18 Administrace trenérů u tréninkového plánu

**Issue:** [#18 — Administrace trenérů u tréninkového plánu](https://github.com/vasekNaus/SportSys/issues/18)

**Stav:** Fáze 1 (základní administrace trenérů, viz sekce níže) i Fáze 2
(multivýběr trenérů pro jednotlivé tréninky ve spojené skupině, popsaná v
sekci
["Doplňující požadavek: Úprava trenérů u plánu se spojenými tréninky"](#doplňující-požadavek-úprava-trenérů-u-plánu-se-spojenými-tréninky-fáze-2))
jsou implementovány a otestovány (112/112 testů).

## Cíl

Na stránce editace tréninkového plánu
(`/sport/Training/Plan/Edit?id={id}`) umožnit spravovat trenéry přiřazené
k danému tréninkovému plánu pomocí pole **Trenéři** typu multivýběr. Výběr se
při uložení synchronizuje s vazební tabulkou `sport.CoachTrainingPlan`, aniž
by vznikaly duplicitní nebo osiřelé vazby.

## Výchozí stav

- Entita `sport.CoachTrainingPlan`
  (`src/SportSys.Database/Models/sport/CoachTrainingPlan.cs`) má složený
  primární klíč `(CoachId, TrainingPlanId, ValidFrom, ValidTo)` a vazby na
  `Coach` a `TrainingPlan`.
- `TrainingPlan.CoachTrainingPlans`
  (`src/SportSys.Database/Models/sport/TrainingPlan.cs:46`) je navigační
  kolekce vazeb pro konkrétní `TrainingPlan.Id`.
- `ValidFrom`/`ValidTo` u vazby vyjadřují interval platnosti přiřazení
  a používají se při zobrazení rozvrhu — `TrainingScheduleService.
  AssignCoachFullNamesAsync` (`src/SportSys.Contract/Services/
  TrainingScheduleService.cs:259-290`) zahrnuje jen vazby, jejichž interval se
  překrývá s `TrainingPlan.From–To` (zdokumentováno v
  `docs/modules/sport.md:116-118`).
- Editaci tréninkového plánu řeší `TrainingPlanService.GetEditAsync` /
  `UpdateAsync` (`src/SportSys.Contract/Services/TrainingPlanService.cs`),
  DTO `TrainingPlanEditDto` / `TrainingPlanEditContextDto`
  (`src/SportSys.Contract/Models/TrainingPlanEditDto.cs`) a Razor Page
  `EditModel` / `Edit.cshtml`
  (`src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml[.cs]`).
- `UpdateAsync` pracuje transakčně (`Serializable`), ověřuje optimistickou
  verzi `OriginalVersion` (hash přes `CreateVersion`) a u spojených plánů
  (`TrainingPlanGroup`) vyžaduje shodné editovatelné hodnoty
  (`HaveConsistentEditableValues`). Editace se váže na konkrétní
  `TrainingPlan.Id`, i když je plán součástí skupiny.
- Aktuálně žádné Contract DTO ani Razor stránka trenéry tréninkového plánu
  nespravuje — jediné čtení je přes `TrainingScheduleService` pro zobrazení
  jmen v rozvrhu.
- Obdobná M:N vazba `sport.CoachTrainingRequirement` (bez datového intervalu)
  se zobrazuje v `TrainingRequirementService.GetAllAsync`
  (`src/SportSys.Contract/Services/TrainingRequirementService.cs:94-150`),
  ale pouze pro čtení — needituje se formulářem.
- Výběr trenérů pro formulář má přímý precedens:
  `CoachAttendanceService.GetCoachesAsync`
  (`src/SportSys.Contract/Services/CoachAttendanceService.cs:75-89`) vrací
  `List<CoachSelectItem>` řazený podle `DisplayName ?? UserName ?? Email`,
  dále `PersonalNumber`. `CoachSelectItem`
  (`src/SportSys.Contract/Models/hr/CoachAttendanceModels.cs:101`) dědí z
  `UserSelectItem` (`Id`, `DisplayName`, `Email`) a přidává `PersonalNumber`.
- V projektu existuje hotová UI/JS komponenta multivýběru na bázi nativních
  checkboxů v `<details>`:
  - markup vzor v `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/
    Index.cshtml:40-70` (atributy `data-multiselect`,
    `data-multiselect-summary`, `data-multiselect-option-label`,
    `data-multiselect-clear`, `data-empty-label`),
  - JS chování v `src/SportSys.Razor/wwwroot/js/site.js:70-122` (aktualizace
    souhrnu, tlačítko vymazání, zavření při kliknutí mimo). Komponenta
    nezávisí na GET/POST ani na konkrétním `name` checkboxů, takže je přímo
    použitelná i ve formuláři odesílaném přes `POST`.
  - styly `schedule-multiselect*` v `src/SportSys.Razor/Styles/_schedule.scss`.
- `sport.CoachTrainingPlan` nemá v EF Core modelu `[DeleteBehavior]` kaskádu
  pro mazání jednotlivých vazeb přes `DbSet`; vazby lze odstranit standardním
  `_db.CoachTrainingPlans.RemoveRange(...)`.

## Potvrzené požadavky a rozhodnutí

- Pole **Trenéři** je multivýběr na editaci tréninkového plánu, zobrazí
  aktuálně přiřazené trenéry, umožní přidání i odebrání a prázdný výběr je
  platný stav (žádný trenér).
- Uložení synchronizuje vazby `sport.CoachTrainingPlan` pro konkrétní
  `TrainingPlan.Id` (přidá nové, zachová trvající, odstraní odebrané, bez
  duplicit).

## Technická rozhodnutí (odvozená z existujících konvencí)

- **Rozsah vazby:** Trenéři se spravují pro konkrétní editovaný
  `TrainingPlan.Id`, ne pro celou skupinu `TrainingPlanGroup`. Issue ani
  existující dokumentace nepožadují sdílení trenérů napříč spojenými plány a
  vazba `CoachTrainingPlan` je navázána na jednotlivý `TrainingPlanId`.
- **`ValidFrom`/`ValidTo` nové vazby:** Při přidání nové vazby se nastaví na
  aktuální `Input.From`/`Input.To` ukládaného plánu, protože to odpovídá
  stávající sémantice "interval platnosti přiřazení" použité při čtení v
  `AssignCoachFullNamesAsync` a zajišťuje, že nově přiřazený trenér bude ihned
  vidět v rozvrhu po celou dobu platnosti plánu.
- **Beze změny `ValidFrom`/`ValidTo` existující vazby:** Pokud je trenér
  ponechán ve výběru a nemění se `From`/`To` plánu, existující řádek se
  nemodifikuje (zabrání to zbytečným UPDATE a zachová případnou odlišnou
  historickou platnost, pokud by byla zadána mimo editaci).
  Pokud se `From`/`To` plánu při uložení změní, zůstávající vazby se
  aktualizují na nový interval, aby test "trenér je stále přiřazen" zůstal
  konzistentní s částí "sync beze ztráty existujících vazeb, které zůstaly
  vybrané".
- **Odebrání vazby:** Odebraný trenér se z `sport.CoachTrainingPlan` smaže
  (`DELETE`, ne logické zneplatnění) — issue ani dokumentace nevyžadují
  historii zrušených přiřazení a tabulka nemá příznak `IsActive`.
- **Zdroj seznamu trenérů:** Nová metoda v `TrainingPlanService` vrátí
  všechny trenéry (`_db.Coaches`) bez filtru aktivity, protože `hr.Coach`
  (resp. `identity.User`) nemá vlastní příznak aktivity porovnatelný s
  `CoachContract.IsActive`; filtrování podle aktivního úvazku není součástí
  zadání a přidalo by nejasnou obchodní logiku nad rámec issue.
- **UI komponenta:** Použije se existující `data-multiselect` vzor (checkboxy
  v `<details>`), ne nová knihovna — konzistentní s rozhodnutím v
  `.github/tasks/issue-3-training-type-multiselect.md` ("Řešení nepřidá novou
  frontendovou knihovnu").
- **Validace:** Odeslané hodnoty `SelectedCoachIds` se před zpracováním
  omezí na skutečně existující `Coach.Id` z načteného seznamu (stejný vzor
  jako `RequirementIndexModel.NormalizeIds` v
  `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/IndexModel.cs`),
  aby neplatné nebo cizí ID z requestu nezpůsobily chybu FK.
- Databázový model se nemění, migrace se nevytváří — `sport.CoachTrainingPlan`
  již existuje v očekávané podobě.

## Technický návrh

### Datový tok

1. `EditModel.OnGetAsync` načte `TrainingPlanEditContextDto` (rozšířený o
   seznam dostupných trenérů a aktuálně přiřazená ID) a zobrazí formulář.
2. Uživatel zaškrtne/odškrtne trenéry v poli **Trenéři** a odešle formulář.
3. `EditModel.OnPostAsync` předá `TrainingPlanEditDto` (rozšířené o
   `SelectedCoachIds`) do `TrainingPlanService.UpdateAsync`.
4. `UpdateAsync` v rámci existující transakce:
   - uloží standardní pole plánu (beze změny stávající logiky),
   - normalizuje `SelectedCoachIds` (platná, unikátní ID),
   - načte existující `CoachTrainingPlan` řádky pro `TrainingPlanId`,
   - spočítá množinové rozdíly (přidat / ponechat / odebrat),
   - provede `Add`/`Remove`/`Update ValidFrom/ValidTo` nad `_db.CoachTrainingPlans`,
   - uloží v rámci stejného `SaveChangesAsync` jako ostatní změny plánu.
5. Po úspěšném uložení se stránka znovu načte (`RedirectToPage`) se
   zobrazením aktuálně přiřazených trenérů.

### Rozšíření DTO

`src/SportSys.Contract/Models/TrainingPlanEditDto.cs`:

- `TrainingPlanEditDto`: nová vlastnost
  `public List<int> SelectedCoachIds { get; set; } = [];` s
  `[Display(Name = "Trenéři")]`. Validace v `IValidatableObject.Validate`
  se nerozšiřuje (prázdný seznam je platný stav), kontrola platnosti ID
  proběhne v service vrstvě.
- `TrainingPlanEditContextDto`: nová vlastnost
  `public required IReadOnlyList<CoachSelectItem> AvailableCoaches { get; init; }`
  pro vykreslení multivýběru (znovupoužije existující
  `SportSys.Contract.Models.hr.CoachSelectItem`).

### Rozšíření service

`src/SportSys.Contract/Services/TrainingPlanService.cs`:

- `GetEditAsync`:
  - doplnit načtení `AvailableCoaches` (stejný dotaz jako
    `CoachAttendanceService.GetCoachesAsync`, případně extrahovat sdílenou
    metodu/pomocnou projekci, pokud to zjednoduší údržbu — ponechat
    jednoduché duplicitní dotazy, pokud by sdílení vyžadovalo nový veřejný
    kontrakt mimo rozsah issue),
  - doplnit načtení aktuálně přiřazených trenérů pro `id` (dotaz nad
    `_db.CoachTrainingPlans.Where(c => c.TrainingPlanId == id)`) a naplnit
    `Input.SelectedCoachIds` jejich `CoachId`.
- `UpdateAsync` / `UpdateCoreAsync`:
  - po úspěšné validaci a před commitem transakce:
    - normalizovat `dto.SelectedCoachIds` proti `AvailableCoaches`
      (filtrovat neplatná/duplicitní ID; pokud normalizace nic nezmění,
      krok je no-op),
    - načíst `existingAssignments = await _db.CoachTrainingPlans
        .Where(c => c.TrainingPlanId == dto.Id).ToListAsync(ct)`,
    - `toRemove = existingAssignments.Where(a => !selectedIds.Contains(a.CoachId))`
      → `_db.CoachTrainingPlans.RemoveRange(toRemove)`,
    - `toAdd = selectedIds.Except(existingAssignments.Select(a => a.CoachId))`
      → vytvořit nové `CoachTrainingPlan { CoachId = id, TrainingPlanId = dto.Id,
        ValidFrom = dto.From, ValidTo = dto.To }` a přidat přes
      `_db.CoachTrainingPlans.AddRange(...)`,
    - `toUpdate = existingAssignments.Where(a => selectedIds.Contains(a.CoachId))`
      → pokud se `dto.From`/`dto.To` liší od `a.ValidFrom`/`a.ValidTo`, musí
      se řádek nahradit (skládaný klíč obsahuje `ValidFrom`/`ValidTo`, nelze
      je přímo editovat) — odebrat starý řádek a přidat nový se stejným
      `CoachId` a novým intervalem,
  - validace neplatného ID (`IsValid`) se nerozšiřuje o kontrolu trenérů —
    neplatná ID se tiše odfiltrují (konzistentní s očekáváním "multivýběr
    pracuje jen s nabízenými hodnotami").

### Razor Page

`src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml.cs`:

- Beze změny signatur handlerů; `Context.AvailableCoaches` a
  `Input.SelectedCoachIds` budou dostupné přes stávající `Context`/`Input`.

`src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml`:

- Uvnitř `<form method="post">`, mimo `Html.EditorFor(model => model.Input, …)`
  (pole trenérů nebude řešeno přes EditorTemplate, protože potřebuje přístup
  k `Context.AvailableCoaches`, nikoli jen k `Input`):
  - doplnit blok s `<span>` labelem "Trenéři" a `<details class="schedule-multiselect"
    data-multiselect data-empty-label="Žádný trenér">` podle vzoru v
    `Training/Plan/Index.cshtml`,
  - pro každého `coach in Model.Context.AvailableCoaches` vykreslit
    `<input type="checkbox" name="Input.SelectedCoachIds" value="@coach.Id"
     checked="@Model.Input.SelectedCoachIds.Contains(coach.Id)" />` se
    `<span data-multiselect-option-label>@coach.DisplayName</span>`,
  - souhrn (`data-multiselect-summary`) inicializovat na serveru stejně jako
    v `Index.cshtml` (0 → "Žádný trenér", 1 → jméno, více → "Vybráno: N"),
  - doplnit tlačítko `data-multiselect-clear` shodné s existujícím vzorem.
- Pole se renderuje jen uvnitř větve `Model.Context.CanEdit` (shodně se
  zbytkem formuláře); u needitovatelné (nekonzistentní) skupiny se
  trenéři nezobrazují jako needitovatelní, protože vazba je per-plán,
  nikoli per-skupina — pole zůstává dostupné i když `CanEdit` je `false`
  pro ostatní pole, **pokud** to nevyžaduje úpravu `TrainingPlan` řádků mimo
  transakci. Rozhodnutí: ponechat pole trenérů viditelné jen když
  `Model.Context.CanEdit` je `true`, aby formulář zůstal jednotný a aby se
  předešlo částečnému odeslání (uložení trenérů bez možnosti uložit zbytek
  plánu kvůli nekonzistentní skupině).

## Implementační kroky

### Fáze 1: Rozšíření DTO

1. Upravit `TrainingPlanEditDto.cs`:
   - přidat `SelectedCoachIds` do `TrainingPlanEditDto`,
   - přidat `AvailableCoaches` do `TrainingPlanEditContextDto`.

### Fáze 2: Rozšíření `TrainingPlanService`

1. V `GetEditAsync` doplnit načtení dostupných trenérů a aktuálně
   přiřazených ID, naplnit nová pole DTO.
2. V `UpdateCoreAsync` doplnit synchronizaci `CoachTrainingPlan` podle
   technického návrhu (normalizace, diff, add/remove/replace).
3. Ověřit, že existující testy optimistické konkurence a konzistence
   skupiny (`OriginalVersion`/`HaveConsistentEditableValues`) zůstávají
   nedotčené — synchronizace trenérů neovlivňuje verzovací hash
   (`CreateVersion` zůstává založen jen na polích plánu).

### Fáze 3: Razor Page `Edit`

1. Doplnit multivýběr trenérů do `Edit.cshtml` podle vzoru
   `data-multiselect` z `Training/Plan/Index.cshtml`.
2. Ověřit, že `EditModel` nepotřebuje žádné změny mimo to, co už poskytuje
   `Context`/`Input` (handler metody zůstávají beze změny).

### Fáze 4: Styly

1. Zkontrolovat, že `schedule-multiselect*` třídy z `_schedule.scss`
   fungují i mimo `<form method="get">` filtr (vizuálně nezávislé na
   metodě formuláře). Doplnit styl jen pokud se v kontextu editačního
   formuláře (`training-edit-*`) objeví vizuální nesrovnalost (např.
   šířka pole v kontextu `dl`/`table` layoutu editace).

### Fáze 5: Dokumentace

1. V `docs/modules/sport.md` doplnit k popisu editace tréninkového plánu
   (sekce o `Training/Plan/Edit`) větu o možnosti spravovat přiřazené
   trenéry přes multivýběr a o tom, že vazba se ukládá do
   `sport.CoachTrainingPlan` s intervalem platnosti rovným aktuální
   platnosti plánu.

## Soubory ke změně

- `src/SportSys.Contract/Models/TrainingPlanEditDto.cs`
- `src/SportSys.Contract/Services/TrainingPlanService.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml`
- `docs/modules/sport.md`
- `tests/SportSys.Razor.Tests/TrainingPlanServiceTests.cs` (nové testy)
- `tests/SportSys.Razor.Tests/TrainingPlanEditDtoTests.cs` (případné nové
  testy validace, pokud se validace DTO rozšiřuje)

## Testy a ověření

Doplnit do `tests/SportSys.Razor.Tests/TrainingPlanServiceTests.cs`
(případně nová testovací třída se zapojeným `SportSysDbContext` nad
in-memory/SQLite providerem, pokud stávající testy v tomto souboru pracují
jen nad čistými DTO bez DB — ověřit při implementaci a zvolit konzistentní
přístup s okolními testy služby):

1. `UpdateAsync` přidá nové vazby `CoachTrainingPlan` pro nově vybrané
   trenéry.
2. `UpdateAsync` ponechá vazby pro trenéry, kteří zůstali vybráni.
3. `UpdateAsync` odstraní vazby pro trenéry odebrané z výběru.
4. `UpdateAsync` nevytvoří duplicitní vazbu, pokud je trenér odeslán
   vícekrát v `SelectedCoachIds`.
5. `UpdateAsync` ignoruje neexistující/cizí ID trenéra v `SelectedCoachIds`.
6. Prázdný `SelectedCoachIds` odstraní všechny existující vazby plánu.
7. `GetEditAsync` vrátí `Input.SelectedCoachIds` odpovídající aktuálně
   uloženým vazbám a `Context.AvailableCoaches` seřazené podle
   `DisplayName`/`PersonalNumber`.

Po implementaci spustit:

```powershell
dotnet build SportSys.slnx
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
```

## Manuální akceptace

1. Otevřít editaci existujícího tréninkového plánu bez přiřazených trenérů
   — pole **Trenéři** je prázdné (`Žádný trenér`).
2. Vybrat jednoho trenéra, uložit — po znovunačtení je trenér předvybrán.
3. Přidat druhého trenéra k již přiřazenému — uložit — oba jsou předvybráni.
4. Odebrat jednoho ze dvou přiřazených trenérů — uložit — zůstává jen
   druhý.
5. Odebrat všechny trenéry — uložit — pole je prázdné a v DB nezůstává
   žádná vazba pro daný `TrainingPlanId`.
6. Zkontrolovat v DB, že `sport.CoachTrainingPlan` neobsahuje duplicitní
   řádky `(CoachId, TrainingPlanId)` po opakovaném uložení stejného výběru.
7. Ověřit, že přiřazení trenéři se nadále zobrazují v rozvrhu
   (`/sport/Training/Schedule`) podle stávající logiky
   `AssignCoachFullNamesAsync`.
8. Ověřit chování multivýběru klávesnicí a v tmavém i světlém režimu.

## Beze změny

- Databázový model `sport.CoachTrainingPlan`, `hr.Coach` a jejich
  konfigurace — žádná migrace.
- Logika zobrazení trenérů v rozvrhu
  (`TrainingScheduleService.AssignCoachFullNamesAsync`).
- Správa trenérů na `sport.CoachTrainingRequirement` (mimo rozsah issue).
- Optimistická konkurence a validace ostatních polí `TrainingPlanEditDto`.
- Chování spojených tréninkových plánů (`TrainingPlanGroup`,
  `HaveConsistentEditableValues`) mimo rozšíření o trenéry.

## Mimo rozsah

- Sdílení přiřazení trenérů napříč spojenými plány ve skupině.
- Filtrování nabízených trenérů podle aktivního úvazku/licence.
- Historie zrušených přiřazení (logické mazání).
- Úprava `/sport/Training/Requirement` nebo `/sport/Training/Schedule`.

## Hotovo, když

- Na stránce editace tréninkového plánu je dostupné pole **Trenéři** jako
  multivýběr s předvýběrem aktuálně přiřazených trenérů.
- Uložení plánu přidá nově vybrané, zachová trvající a odstraní odebrané
  vazby v `sport.CoachTrainingPlan` bez duplicit.
- Prázdný výběr je platný a výsledkem je odstranění všech vazeb plánu.
- `dotnet build SportSys.slnx` a cílené testy v
  `tests\SportSys.Razor.Tests` proběhnou bez chyby.

---

## Doplňující požadavek: Úprava trenérů u plánu se spojenými tréninky (Fáze 2)

**Zdroj:** komentář k issue #18, "Úprava trenérů u plánu se spojenými
tréninky" (2026-10-02), vzniklý z testování Fáze 1.

### Proč je nutná změna

Fáze 1 přiřazuje trenéry vždy jen k **jednomu konkrétnímu** `TrainingPlan.Id`
— tomu, který je uveden v URL editace (`?id={id}`). U spojeného plánu
(`TrainingPlanGroup`) ale rozvrh otevírá editaci vždy přes **jediné**
`EditItemId = block.MinimumItemId`
(`src/SportSys.Razor/Models/TrainingSchedule/ScheduleEventModel.cs:116,138` —
`CreateTrainingPlanBlock`/`CreateTrainingBlock` nastavují `EditItemId` na
minimální ID z celé skupiny). Ostatní členové skupiny tak **nemají žádný
vlastní vstupní bod** do editace a jejich trenéři zůstávají s Fází 1 needi­
tovatelní přes UI. Komentář požaduje, aby šlo trenéra nastavit samostatně pro
každý spojený trénink v rámci jednoho otevřeného formuláře.

### Citace požadavku (zkráceno, viz issue pro plné znění)

- Tabulka spojených tréninků v záhlaví formuláře se zredukuje na sloupce
  **Kategorie** a **Trenér** (odstraní se Typ, Platnost od/do, Den, Čas od/do,
  Lokalita — jsou mezi spojenými tréninky shodné).
  (`src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml:58-80`,
  aktuální tabulka.)
- Sloupec **Trenér** obsahuje pro každý trénink **samostatné výběrové pole**
  ("Ve sloupci Trenér bude pro každý trénink samostatné výběrové pole
  umožňující zvolit trenéra daného tréninku.") — N spojených tréninků → N
  polí.
- Validace na aplikační vrstvě (ne jen UI): stejný trenér nesmí být přiřazen
  k více tréninkům v rámci jednoho spojeného plánu; při porušení se neuloží
  nic a zobrazí se validační chyba.
- Uložení zachovává vazbu trénink → vybraný trenér pro každý trénink
  samostatně (ne společně pro celý plán).
- Pokud plán **není** spojený (jediný trénink), zůstává editace "tak, jak je
  potřeba" → Fáze 1 (multivýběr `Input.SelectedCoachIds` pro `dto.Id`)
  zůstává **beze změny** pro nespojené plány.

### Technické rozhodnutí: kardinalita pole "Trenér" na trénink

**Upřesněno uživatelem (2026-10-02):** jeden tréninkový plán může mít
přiřazeno více trenérů zároveň — pole trenéra musí být **multivýběr
všude**, u spojeného i nespojeného plánu. Toto upřesnění nahrazuje původní
odvození z jednotného čísla v textu komentáře ("výběrové pole umožňující
zvolit trenéra") — multivýběr zůstává zachován i pro řádkové pole v tabulce
spojených tréninků, jen se mění jeho **rozsah** (per trénink místo sdíleně
pro celou skupinu):

- **U spojeného plánu** (`Context.IsGrouped == true`) má každý řádek v
  tabulce (jeden na spojený trénink) vlastní **multivýběrové** pole
  (stejná komponenta `data-multiselect`/checkboxy jako ve Fázi 1), umožňující
  přiřadit 0 až N trenérů danému konkrétnímu tréninku.
- **U nespojeného plánu** (`Context.IsGrouped == false`) zůstává multivýběr
  z Fáze 1 beze změny (`Input.SelectedCoachIds`, sdílený pro jediný
  `dto.Id`) — komentář to výslovně potvrzuje ("zůstane editace tak, jak je
  potřeba").
- Validace duplicity ("stejný trenér nesmí být přiřazen k více tréninkům v
  rámci jednoho spojeného plánu") se tím nemění — platí na úrovni *trenéra
  jako hodnoty*: libovolný konkrétní `CoachId` smí být vybrán nejvýš v
  **jednom** řádkovém multivýběru napříč celou skupinou, byť v tom řádku
  může být vybráno víc různých trenérů současně s jinými.
- Žádné "sjednocování" existujících vazeb není potřeba — multivýběr beze
  ztráty přenáší libovolný počet existujících trenérů daného tréninku do
  předvýběru (stejná logika předvýběru jako ve Fázi 1 pro
  `Input.SelectedCoachIds`, jen opakovaná per řádek).

### Dotčené soubory a návrh změn

#### `src/SportSys.Contract/Models/TrainingPlanEditDto.cs`

- `TrainingPlanEditMemberDto`: přidat
  `public IReadOnlyList<int> CoachIds { get; init; } = [];`
  — aktuálně přiřazení trenéři pro tento konkrétní člen skupiny (pro
  předvýběr v řádkovém multivýběru).
- Nový typ `TrainingPlanMemberCoachInputDto` (list item pro model binding):
  ```csharp
  public class TrainingPlanMemberCoachInputDto
  {
      [HiddenInput(DisplayValue = false)]
      public int TrainingPlanId { get; set; }
      public List<int> CoachIds { get; set; } = [];
  }
  ```
- `TrainingPlanEditDto`: přidat
  `public List<TrainingPlanMemberCoachInputDto> MemberCoachAssignments { get; set; } = [];`
  — jedna položka na spojený trénink. Používá se jen když je plán spojený;
  pro nespojený plán zůstává `SelectedCoachIds` jediným zdrojem pravdy.
  Model binding využívá standardní indexovanou syntaxi ASP.NET Core pro
  `List<T>` (`name="Input.MemberCoachAssignments[i].TrainingPlanId"` jako
  skryté pole + opakované
  `name="Input.MemberCoachAssignments[i].CoachIds"` checkboxy pro výběr) —
  shodný mechanismus jako checkboxy `Input.SelectedCoachIds` ve Fázi 1, jen
  vnořený do položky seznamu. Není potřeba vlastní model binder.
- `TrainingPlanUpdateResult`: přidat hodnotu `DuplicateCoachAssignment`.

#### `src/SportSys.Contract/Services/TrainingPlanService.cs`

- `GetEditAsync`:
  - načíst **všechny** vazby `CoachTrainingPlan` pro všechny `members`
    (ne jen pro `id`): `_db.CoachTrainingPlans.Where(a =>
    memberIds.Contains(a.TrainingPlanId))`,
  - pro nespojený plán (`target.GroupId` je `null`) zachovat beze změny
    plnění `Input.SelectedCoachIds` (jen z vazeb pro `id`),
  - pro spojený plán navíc naplnit `member.CoachIds` pro každého člena
    (seskupit načtené vazby podle `TrainingPlanId`, vzít distinct `CoachId`)
    a `Input.MemberCoachAssignments` odpovídajícím seznamem (jedna položka
    na člena, v pořadí `Members`),
  - `CreateVersion` zavolat s kompletními daty o trenérech napříč **všemi**
    členy skupiny (viz níže — nejen `id`), jinak by se concurrency hash
    nezměnil při konfliktní změně trenéra jiného člena skupiny, který byl
    upraven zároveň v jiném otevřeném formuláři.
- `UpdateCoreAsync`:
  - po ověření `HaveConsistentEditableValues` a před zápisem standardních
    polí plánu:
    - pokud `groupId.HasValue` (spojený plán): pro každý `plan` v `plans`
      načíst požadovaný seznam `CoachIds` z `dto.MemberCoachAssignments`
      podle `TrainingPlanId` (vstup se páruje **jen na ID skutečně
      načtených členů ze serveru** přes `plans`, nikoliv přímo podle
      položek z requestu — brání to vložení vazby pro cizí/neexistující
      `TrainingPlanId`); každý seznam normalizovat přes
      `NormalizeCoachIds` (existující metoda z Fáze 1 — odfiltruje
      neplatná/cizí `CoachId` a duplicity v rámci jednoho řádku),
    - spočítat, zda se libovolný `CoachId` vyskytuje v normalizovaných
      seznamech **více než jednoho** člena; pokud ano, vrátit
      `TrainingPlanUpdateResult.DuplicateCoachAssignment` **před** jakýmkoli
      zápisem (transakce se nekomitne),
    - pro každého člena provést synchronizaci jeho `CoachTrainingPlan` vazeb
      vůči jeho normalizovanému seznamu `CoachIds` — znovupoužít stávající
      `ComputeCoachAssignmentDiff`/replace-on-interval-change logiku z Fáze 1
      (`SynchronizeCoachAssignmentsAsync`), zobecněnou tak, aby přijímala
      `TrainingPlanId`, interval (`From`/`To` toho konkrétního člena, ne
      `dto.From/To` celého formuláře, protože po uložení budou stejné díky
      synchronizaci standardních polí) a množinu vybraných `CoachIds` místo
      pevně `dto.Id` a `dto.SelectedCoachIds`,
    - pokud `!groupId.HasValue` (nespojený plán): zachovat beze změny
      stávající volání `SynchronizeCoachAssignmentsAsync(dto, ...)` nad
      `dto.SelectedCoachIds` z Fáze 1.
  - `CreateVersion`: rozšířit `TrainingPlanMemberVersion` o
    `IReadOnlyList<int> CoachIds` (trenéři přiřazení právě **tomuto**
    členovi, ne plochý seznam pro celý snapshot) a odstranit samostatný
    `TrainingPlanVersionSnapshot.CoachIds`. Hash tak pokryje trenéry všech
    členů skupiny nezávisle, což uzavírá i zbývající concurrency mezeru pro
    souběžnou editaci různých členů stejné skupiny. Upravit oba volající
    místa (`GetEditAsync`, `UpdateCoreAsync`) tak, aby sestavovala
    `coachIdsByMember` ze všech načtených vazeb, ne jen pro `dto.Id`.

#### `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml`

- Tabulka spojených tréninků (`Model.Context.IsGrouped`): odstranit sloupce
  Typ, Platnost od, Platnost do, Den, Čas od, Čas do, Lokalita; ponechat jen
  Kategorie. Přidat sloupec **Trenér** se samostatnou instancí stávající
  komponenty `data-multiselect` pro každý `member` (shodný markup jako
  stávající pole "Trenéři" ve Fázi 1, jen uvnitř buňky tabulky a opakovaný
  per řádek):
  ```cshtml
  @for (var i = 0; i < Model.Context.Members.Count; i++)
  {
      var member = Model.Context.Members[i];
      var assignment = Model.Input.MemberCoachAssignments.Count > i
          ? Model.Input.MemberCoachAssignments[i]
          : null;
      var selectedIds = assignment?.CoachIds ?? member.CoachIds;
      <tr>
          <td>@member.SeasonCategoryName</td>
          <td>
              <input type="hidden"
                     name="Input.MemberCoachAssignments[@i].TrainingPlanId"
                     value="@member.Id" />
              <details class="schedule-multiselect" data-multiselect data-empty-label="Žádný trenér">
                  …checkboxy name="Input.MemberCoachAssignments[@i].CoachIds"…
              </details>
          </td>
      </tr>
  }
  ```
  Předvýběr přednostně vychází z právě odeslaného
  `Model.Input.MemberCoachAssignments[i].CoachIds` (pokud položka na indexu
  `i` existuje — po neúspěšné validaci), jinak z `member.CoachIds`
  (čerstvě načtený stav), aby po chybě validace zůstal vidět uživatelův
  rozpracovaný výběr. Tabulka se renderuje jen uvnitř `Model.Context.CanEdit`
  (shodně s Fází 1).
- Stávající sdílené pole "Trenéři" (`data-multiselect` s
  `Input.SelectedCoachIds`) se zobrazuje **jen když `!Model.Context.IsGrouped`**
  (přidat podmínku `@if (!Model.Context.IsGrouped) { … }` kolem bloku
  `<div class="field">…Trenéři…</div>`). U spojeného plánu se toto pole
  úplně skryje — jeho funkci přebírají řádková pole v tabulce.

#### `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml.cs`

- `OnPostAsync`: doplnit `case TrainingPlanUpdateResult.DuplicateCoachAssignment:`
  analogicky k `GroupInconsistent` — `ModelState.AddModelError(string.Empty,
  "Stejný trenér je vybrán u více spojených tréninků. Každý trenér může být
  přiřazen jen k jednomu tréninku ve skupině.")` a
  `return await ReloadPageAsync(Input.Id, ct);` (bez `useCurrentInput: true`,
  aby zůstal viditelný odeslaný/chybný výběr uživatele).

#### `docs/modules/sport.md`

- Doplnit, že u spojených tréninkových plánů se trenér nastavuje samostatně
  pro každý spojený trénink (řádkové pole v tabulce), zatímco u nespojeného
  plánu zůstává multivýběr z Fáze 1; doplnit zmínku o validaci zamezující
  přiřazení stejného trenéra vícekrát v rámci jedné skupiny.

### Testy a ověření

Doplnit do `tests/SportSys.Razor.Tests/TrainingPlanServiceTests.cs`:

1. `UpdateAsync` u spojeného plánu uloží různé sady trenérů pro různé členy
   skupiny (včetně více trenérů u jednoho tréninku zároveň) a každý člen má
   po uložení přesně svou vlastní sadu vazeb `CoachTrainingPlan`.
2. `UpdateAsync` u spojeného plánu vrátí `DuplicateCoachAssignment` a
   neprovede žádný zápis, pokud se stejný `CoachId` objeví v seznamech
   `CoachIds` dvou různých členů skupiny.
3. `UpdateAsync` u spojeného plánu akceptuje stejný formulář, pokud je
   duplicitní výběr opraven (regresní test k bodu 2).
4. `UpdateAsync` u spojeného plánu odstraní všechny vazby člena, pokud je
   jeho seznam `CoachIds` nastaven zpět na prázdný.
5. `UpdateAsync` u nespojeného plánu (jediný člen) zachová beze změny
   chování Fáze 1 (multivýběr, viz existující testy).
6. `GetEditAsync` u spojeného plánu vrátí pro každého člena `CoachIds`
   odpovídající aktuálně uloženým vazbám (prázdný seznam, pokud žádná
   není).
7. `CreateVersion`/optimistická konkurence: změna trenéra u **jiného** člena
   skupiny, než je `dto.Id`, mezi načtením a uložením formuláře, způsobí
   `Conflict` (ne tiché přepsání) — reprodukuje a ověřuje uzavření
   concurrency mezery popsané výše.

Po implementaci spustit:

```powershell
dotnet build SportSys.slnx
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
```

### Manuální akceptace (podle akceptačních kritérií z komentáře)

1. Otevřít editaci spojeného plánu (3 kategorie) — tabulka v záhlaví má jen
   sloupce Kategorie a Trenér, 3 řádky, 3 výběrová pole.
2. U existujícího plánu s již přiřazenými trenéry jsou pole předvybrána podle
   aktuálních vazeb.
3. Změna trenéra u jedné kategorie neovlivní zobrazený výběr u ostatních.
4. Zvolit stejného trenéra u dvou kategorií (v multivýběru obou řádků) a
   uložit — zobrazí se validační chyba, nic se neuloží (ověřit i v DB).
5. Opravit duplicitu (zvolit odlišné sady trenérů) a uložit — uloží se
   úspěšně, každý trénink má svou sadu trenérů v `sport.CoachTrainingPlan`.
6. Nastavit u jedné kategorie prázdný výběr a uložit — všechny vazby pro ten
   trénink zmizí, ostatní zůstanou.
7. Otevřít editaci nespojeného plánu — chování multivýběru z Fáze 1 je beze
   změny.
8. Ověřit zobrazení trenérů v rozvrhu (`/sport/Training/Schedule`) po
   uložení — odpovídá nově uloženým vazbám per trénink; ověřit, že jeden
   trénink zobrazuje všechny své přiřazené trenéry, pokud je jich víc.

### Beze změny (Fáze 2)

- Perzistence per-`TrainingPlan.Id` (`sport.CoachTrainingPlan`) — potvrzuje
  se, nemění.
- Chování editace nespojeného plánu (multivýběr `SelectedCoachIds`).
- `HaveConsistentEditableValues` a kontrola konzistence ostatních polí
  skupiny.
- Databázový model — žádná migrace.

### Mimo rozsah (Fáze 2)

- Přidání samostatného vstupního bodu do editace pro každého člena skupiny
  zvlášť (řeší se tím, že všichni členové jsou editovatelní z jednoho
  formuláře, ne vícero URL).
- Filtrování nabízených trenérů v řádkovém poli podle toho, kdo je již
  vybraný u jiného řádku (UI-level prevence duplicity) — validace zůstává
  jen na aplikační vrstvě při uložení, podle zadání v komentáři.
- Změna `ScheduleEventModelFactory`/`EditItemId` logiky pro otevírání editace.

### Hotovo, když (Fáze 2)

- Všech 8 akceptačních kritérií z komentáře issue je splněno a ověřeno
  manuálně i testy výše.
- `dotnet build SportSys.slnx` a cílené testy proběhnou bez chyby.
