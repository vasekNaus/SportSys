# Implementační plán: #12 Filtrování rozvrhu tréninků podle stavu

**Issue:** [#12 — Filtrování rozvrhu tréninků podle stavu](https://github.com/vasekNaus/SportSys/issues/12)

**Stav:** Připraveno k implementaci.

## Cíl

Na stránce `/sport/Training/Schedule` přidat filtr „Stavy tréninku“ pod
stávající filtr kategorií. Filtr umožní vybrat nula, jeden nebo více stavů
z číselníku `sport.TrainingState` a omezí zobrazené tréninky logikou OR.
Prázdný výběr znamená, že se filtr neaplikuje.

## Výchozí stav

- Stránka `IndexModel`
  (`src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml.cs`)
  drží filtry jako `[BindProperty(SupportsGet = true)]` vlastnosti
  (`SelectedCategories`, `SelectedTrainingTypeIds`, `SelectedLocations` atd.)
  a normalizuje je v `LoadAndNormalizeFiltersAsync`.
- `TrainingScheduleService.GetTrainingsAsync`
  (`src/SportSys.Contract/Services/TrainingScheduleService.cs`) přijímá
  kolekce filtrů a podmíněně je aplikuje na `IQueryable<Training>`, např.:
  ```csharp
  if (trainingTypeIds.Count > 0)
      query = query.Where(t => trainingTypeIds.Contains(t.TrainingTypeId));
  ```
- Entita `Training` (`src/SportSys.Database/Models/sport/Training.cs`) už má
  sloupec `TrainingStateId` a navigaci `TrainingState`. Databázový model se
  **nemění**.
- Číselník `sport.TrainingState`
  (`src/SportSys.Database/Models/sportSchema/TrainingState.cs`,
  `TrainingState.Seed.cs`) má `Id` + lokalizovaný `Name` (naplněný z RESX
  `ETrainingState.resx` při seedu) — stejný tvar jako `TrainingType`, takže lze
  použít existující `LookupSelectItem`
  (`src/SportSys.Contract/Models/LookupSelectItem.cs`).
- V UI (`Index.cshtml`) je filtr kategorií realizován jako `<fieldset>` se
  `<legend>Kategorie</legend>` a seznamem `checkbox-label` prvků se jménem
  `SelectedCategories` — přesně tento vzor issue vyžaduje i pro stavy
  („Filtr bude realizován stejným způsobem jako filtr kategorií“), na rozdíl
  od `details`/`summary` multivýběru použitého pro typ tréninku a lokalitu.
- `docs/modules/sport.md`, sekce „Filtry Schedule“ (řádky 126–133), vyjmenovává
  aktuální filtry stránky.

## Potvrzené požadavky a rozhodnutí

- Filtr se týká výhradně stránky `/sport/Training/Schedule` (stránka `Plan`
  není dotčena).
- Zdroj dat: číselník `TrainingState` (`Id`, `Name`).
- UI vzor: stejný jako filtr kategorií — `<fieldset>`/`<legend>` se seznamem
  checkboxů (name `SelectedTrainingStateIds`), umístěný hned pod fieldsetem
  „Kategorie“. Nejde o `details`/`summary` multivýběr typu tréninku.
- Prázdný výběr = žádné omezení (zobrazí se všechny stavy).
- Více vybraných stavů = logika OR (`IN (...)`).
- Filtr je kombinovatelný se všemi ostatními filtry beze změny jejich chování.
- Beze změny: načítání tréninků mimo přidanou podmínku, vizualizace,
  spojování tréninků, detekce překryvů, export do Excelu (issue export
  nezmiňuje).
- Databázový model se nemění, migrace se nevytváří.

## Technický návrh

Přidat čtvrtou nezávislou podmínku filtru analogickou k `trainingTypeIds`,
provázanou přes celý řetězec `Index.cshtml.cs` → `TrainingScheduleService` →
`Index.cshtml`.

## Implementační kroky

### Fáze 1: Načtení číselníku stavů v Contract službě

Upravit `src/SportSys.Contract/Services/TrainingScheduleService.cs`:

1. Přidat metodu analogickou `GetTrainingTypesAsync`:
   ```csharp
   public async Task<List<LookupSelectItem>> GetTrainingStatesAsync(CancellationToken ct = default)
   {
       return await _db.TrainingStates
           .OrderBy(s => s.Id)
           .Select(s => new LookupSelectItem
           {
               Id = s.Id,
               Name = s.Name,
           })
           .ToListAsync(ct);
   }
   ```
   (`DbSet<TrainingState> TrainingStates` je ověřen v
   `src/SportSys.Database/Context/SportSysDbContext.cs:71`.)

2. Rozšířit signaturu `GetTrainingsAsync` o nový parametr
   `IReadOnlyCollection<int> trainingStateIds` (za `trainingTypeIds`, před
   `locations`, aby pořadí odpovídalo pořadí filtrů v UI — pořadí parametrů
   nemá funkční dopad, jde jen o čitelnost).

3. V těle metody přidat podmíněný filtr stejného tvaru jako u typu tréninku:
   ```csharp
   if (trainingStateIds.Count > 0)
       query = query.Where(t => trainingStateIds.Contains(t.TrainingStateId));
   ```

4. `GetTrainingPlansAsync` a `TrainingPlan` **nezměnit** — `TrainingPlan` nemá
   `TrainingStateId` a stránka `Plan` není součástí požadavku.

### Fáze 2: Stav filtru v PageModelu

Upravit
`src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml.cs`:

1. Přidat vlastnosti:
   ```csharp
   public List<LookupSelectItem> TrainingStates { get; private set; } = [];

   [BindProperty(SupportsGet = true)]
   public List<int> SelectedTrainingStateIds { get; set; } = [];
   ```

2. V `LoadAndNormalizeFiltersAsync`:
   - načíst `TrainingStates = await _service.GetTrainingStatesAsync(ct);`,
   - normalizovat `SelectedTrainingStateIds` stejným způsobem jako
     `SelectedTrainingTypeIds` (odstranit neplatné a duplicitní hodnoty):
     ```csharp
     var requestedTrainingStateIds = SelectedTrainingStateIds.ToHashSet();
     SelectedTrainingStateIds = TrainingStates
         .Where(s => requestedTrainingStateIds.Contains(s.Id))
         .Select(s => s.Id)
         .ToList();
     ```

3. V `LoadTrainingsAsync` předat `SelectedTrainingStateIds` do
   `_service.GetTrainingsAsync(...)` na odpovídající pozici parametru.

4. Filtr nesmí vyžadovat žádnou hodnotu k načtení rozvrhu — podmínky pro
   `return false` v `LoadAndNormalizeFiltersAsync` zůstávají beze změny.

### Fáze 3: Uživatelské rozhraní

Upravit
`src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml`:

1. Do fieldsetu s `<legend>Kategorie</legend>` (blok
   `schedule-filter-categories`) přidat bezprostředně pod
   `schedule-filter-checkboxes` s kategoriemi nový blok pro stavy, ve stejném
   fieldsetu nebo v novém fieldsetu hned za ním — zvolit variantu **nový
   `<fieldset>` s `<legend>Stavy tréninku</legend>`, umístěný ihned po
   fieldsetu Kategorie**, aby vizuálně i sémanticky odpovídal požadavku „pod
   filtrem kategorií“ a zachoval nezávislou `legend` stejně jako u kategorií:
   ```html
   @if (Model.TrainingStates.Count > 0)
   {
       <fieldset class="schedule-filter-categories">
           <legend>Stavy tréninku</legend>
           <div class="schedule-filter-checkboxes">
               @foreach (var state in Model.TrainingStates)
               {
                   <label class="checkbox-label">
                       <input type="checkbox"
                              name="SelectedTrainingStateIds"
                              value="@state.Id"
                              checked="@Model.SelectedTrainingStateIds.Contains(state.Id)" />
                       @state.Name
                   </label>
               }
           </div>
       </fieldset>
   }
   ```
2. Nepoužívat `details`/`summary` multivýběr ani JavaScriptovou komponentu
   `data-multiselect` — filtr má vzhled i ovládání checkboxů, stejně jako
   kategorie.
3. Zachovat stávající odeslání formuláře tlačítkem „Zobrazit rozvrh“; žádné
   `onchange="this.form.submit()"` u checkboxů stavů (stejně jako u kategorií).

### Fáze 4: Dokumentace

Upravit `docs/modules/sport.md`, sekci „Filtry Schedule“ (řádky 126–133) —
doplnit položku:

```markdown
- nula, jeden nebo více stavů tréninku; prázdný výběr znamená všechny stavy,
```

## Soubory ke změně

- `src/SportSys.Contract/Services/TrainingScheduleService.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml`
- `docs/modules/sport.md`

## Testy a ověření

Repozitář nemá jednotkové testy nad `TrainingScheduleService.GetTrainingsAsync`
(vyžaduje EF Core kontext); ověření proběhne buildem a manuální akceptací.

```powershell
dotnet build SportSys.slnx
dotnet run --project src\SportSys.Razor
```

## Manuální akceptace

1. Otevřít `/sport/Training/Schedule`, zvolit sezónu a rozsah dat — filtr
   „Stavy tréninku“ se zobrazí pod filtrem „Kategorie“.
2. Bez výběru stavu se zobrazí tréninky všech stavů (beze změny oproti
   současnému chování).
3. Výběr jednoho stavu zobrazí pouze odpovídající tréninky.
4. Výběr více stavů zobrazí tréninky odpovídající kterémukoliv z nich (OR),
   bez duplicit.
5. Odebrání všech zaškrtnutých stavů zruší filtrování.
6. Filtr funguje současně s filtrem kategorií, typu tréninku, lokality a
   s přepínačem „Spojovat tréninky“; výsledky, vizualizace a export zůstávají
   korektní.
7. Neplatné/duplicitní hodnoty `SelectedTrainingStateIds` v URL se po odeslání
   normalizují (analogicky k `SelectedTrainingTypeIds`).

## Beze změny

- Stránka `/sport/Training/Plan` a `TrainingScheduleService.GetTrainingPlansAsync`.
- Databázový model, konfigurace `TrainingStateConfiguration`, seed dat, EF Core
  migrace.
- `TrainingScheduleViewComponent`, prezentační modely timeline, algoritmus
  seskupování a rozdělování překryvů do lanes.
- Export do Excelu (`TrainingScheduleExcelExporter`) a jeho sloupce.
- Filtrování podle kategorií, typu tréninku, lokality a ostatní filtry
  stránky.

## Mimo rozsah

- Rozšíření exportu o sloupec stavu tréninku.
- Jakákoliv úprava číselníku `TrainingState` nebo hodnot `ETrainingState`.

## Hotovo, když

- Na stránce `Schedule` existuje fieldset „Stavy tréninku“ pod fieldsetem
  „Kategorie“, naplněný z číselníku `TrainingState`.
- Lze vybrat žádný, jeden nebo více stavů; výběr je zachován v query stringu
  po odeslání formuláře a je normalizován vůči neplatným/duplicitním hodnotám.
- Bez výběru se zobrazí všechny tréninky; s výběrem pouze tréninky odpovídající
  alespoň jednomu vybranému stavu.
- Ostatní filtry, vizualizace, spojování, detekce překryvů a export fungují
  beze změny.
- `dotnet build SportSys.slnx` proběhne bez chyb.
