# Implementační plán: #10 Filtr plánů podle data platnosti

**Issue:** [#10 — Filtr plánů podle data platnosti](https://github.com/vasekNaus/SportSys/issues/10)

**Stav:** Implementováno a ověřeno.

## Cíl

Doplnit na stránku `/sport/Training/Plan` nepovinný filtr data platnosti,
který omezí týdenní přehled na plány platné v konkrétní den. Filtr musí
fungovat společně se sezónou, kategoriemi, typy, lokalitami a fází tréninku,
při vymazání obnovit dosavadní výsledek a při změně automaticky znovu načíst
stránku.

## Výchozí stav

- Přehled plánů tvoří Razor Page
  `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml` a její
  `IndexModel` v `Index.cshtml.cs`.
- PageModel přijímá GET filtry pro sezónu, kategorie, typy, lokality, jednu
  fázi tréninku a zobrazování prázdných řádků.
- `TrainingScheduleService.GetTrainingPlansAsync` skládá jeden EF Core dotaz
  nad `sport.TrainingPlan` a aplikuje současné filtry před projekcí do
  `TrainingPlanScheduleItemDto`.
- `TrainingPlan` už obsahuje nenulové hranice platnosti `From` a `To`.
  Přehled je nyní vrací v DTO a tooltipu, ale dotaz podle nich nefiltruje.
- Stránka vždy skládá řádky pondělí až neděle; datum platnosti nemění strukturu
  týdne ani `DayName`, pouze množinu plánů v jednotlivých řádcích.
- Nativní datumová pole se v modulu používají jako `<input type="date">`
  s hodnotou ve formátu `yyyy-MM-dd`.
- Automatické odeslání jednotlivého filtru je už na stránkách řešeno přímo
  atributem `onchange="this.form.submit()"`; globální `site.js` obsluhuje jen
  multivýběry a není potřeba jej rozšiřovat.
- Issue nemá komentáře ani dodatečná rozhodnutí.

## Potvrzené požadavky a rozhodnutí

- Datumové pole bude umístěno bezprostředně za filtrem **Fáze tréninku**.
- Filtr bude nepovinný a bude se přenášet v query stringu GET formuláře.
- Nevyplněné datum zachová současné chování a nebude přidávat podmínku
  platnosti.
- Vyplněné datum zobrazí pouze plány, pro které platí inkluzivní podmínka:

  ```text
  TrainingPlan.From <= ValidOn && TrainingPlan.To >= ValidOn
  ```

  Plán je tedy platný také přesně v první a poslední den svého intervalu.
- Datum se kombinuje průnikem se všemi stávajícími filtry; žádný z nich se
  kvůli novému filtru nemění ani neresetuje.
- Změna data i jeho vymazání automaticky odešle stávající GET formulář.
- Použije se nativní datumový input, který umožňuje ruční zápis, výběr
  z kalendáře i vymazání hodnoty podle možností prohlížeče.
- Datum se nebude automaticky omezovat na interval vybrané sezóny. Issue
  požaduje libovolný den a samotná data plánů určují, zda vznikne výsledek.

## Technický návrh

### Razor Page a datový tok

Do `Plan.IndexModel` přidat nullable GET-bound vlastnost, například:

```csharp
[BindProperty(SupportsGet = true)]
public DateOnly? ValidOn { get; set; }
```

Hodnota se předá do `TrainingScheduleService.GetTrainingPlansAsync` spolu se
stávajícími filtry. Podmínky pro načtení přehledu zůstanou stejné: sezóna a
fáze jsou nadále povinné, datum nikoli. Prázdné datum proto nesmí způsobit
časný návrat ani blokovat vytvoření `ScheduleView`.

V `Index.cshtml` vykreslit za selectem `TrainingPhaseId` pole s popiskem
**Datum platnosti**, názvem odpovídajícím bindované vlastnosti a hodnotou
formátovanou jako `yyyy-MM-dd`. Na `change` odeslat formulář, aby se stejným
způsobem zpracovalo zadání i vymazání data. Ostatní hodnoty GET formuláře se
při odeslání zachovají automaticky.

Není potřeba přidávat samostatné tlačítko pro vymazání: nativní nullable
datumové pole lze vyprázdnit a prázdná hodnota se naváže jako `null`.
Stávající tlačítko **Zobrazit plán** zůstane zachováno pro společné použití
ostatních filtrů.

### Contract služba a databázový dotaz

Rozšířit signaturu `TrainingScheduleService.GetTrainingPlansAsync` o
`DateOnly? validOn` před `CancellationToken`. Po vytvoření základního dotazu a
vedle stávajících volitelných filtrů přidat podmínku pouze při
`validOn.HasValue`:

```csharp
query = query.Where(plan =>
    plan.From <= validOn.Value &&
    plan.To >= validOn.Value);
```

Filtrování musí proběhnout před `Select` a `ToListAsync`, aby podmínku přeložil
EF Core do SQL a neprováděla se nad materializovaným seznamem. Projekce,
načítání trenérů, řazení podle dne a času, seskupování bloků i výpočet lanes
zůstanou beze změny.

Pro cílené unit testy lze podmíněnou část dotazu vyčlenit do malé interní
statické metody služby přijímající `IQueryable<TrainingPlan>` a nullable datum.
Metoda musí pouze vrátit původní query pro `null`, nebo připojit uvedenou
inkluzivní podmínku; nesmí materializovat data. Projekt Contract už zpřístupňuje
interní členy `SportSys.Razor.Tests`.

### Dokumentace

V `docs/modules/sport.md` aktualizovat sekci **Filtry Plan**:

- doplnit nepovinné datum platnosti,
- změnit tvrzení, že Plan vždy zobrazuje záznamy bez omezení podle `From–To`,
- popsat inkluzivní filtrování pouze při vyplněném datu,
- zachovat informaci, že platnost zůstává uvedena v tooltipu.

## Implementační kroky

### Fáze 1: Contract filtrování

1. Rozšířit `TrainingScheduleService.GetTrainingPlansAsync` o nullable datum
   platnosti.
2. Přidat podmíněný, serverově vyhodnocený filtr `From <= datum <= To`.
3. Zachovat stejný dotaz a výsledek při hodnotě `null`.
4. Pokud bude podmínka vyčleněna kvůli testovatelnosti, ponechat helper
   interní a založený na `IQueryable`, bez nové veřejné aplikační abstrakce.

### Fáze 2: GET filtr v Razor Page

1. Přidat `ValidOn` do `Plan.IndexModel` jako nullable `DateOnly` s
   `SupportsGet`.
2. Předat hodnotu do `GetTrainingPlansAsync` bez změny normalizace ostatních
   filtrů.
3. Přidat za `TrainingPhaseId` nativní datumové pole **Datum platnosti**.
4. Nastavit ISO hodnotu `yyyy-MM-dd` a automatické odeslání formuláře při
   změně nebo vymazání.

### Fáze 3: Testy a dokumentace

1. Doplnit unit testy podmíněného filtru pro prázdnou hodnotu, vnitřek
   intervalu a obě hraniční data.
2. Ověřit v testech, že jsou vyloučeny plány začínající až po zadaném datu
   i plány končící před ním.
3. Aktualizovat dokumentaci sportovního modulu.

## Soubory ke změně

| Soubor | Změna |
|---|---|
| `src/SportSys.Contract/Services/TrainingScheduleService.cs` | Nullable parametr data a podmíněný inkluzivní filtr v EF Core dotazu. |
| `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs` | GET-bound datum a jeho předání Contract službě. |
| `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml` | Datumové pole za fází tréninku a automatické odeslání formuláře. |
| `tests/SportSys.Razor.Tests/TrainingPlanValidityFilterTests.cs` | Cílené testy nepovinného filtru a hranic platnosti. |
| `docs/modules/sport.md` | Dokumentace nového filtru a změna popisu chování `From–To`. |

Pokud budou testy přidány do existujícího tematicky vhodného souboru namísto
nového `TrainingPlanValidityFilterTests.cs`, produkční rozsah změn se tím
nemění.

## Testy a ověření

### Automatické testy

- `null` datum vrátí stejnou množinu plánů bez omezení podle platnosti.
- Datum uvnitř intervalu plán zachová.
- Datum shodné s `From` plán zachová.
- Datum shodné s `To` plán zachová.
- Datum před `From` plán vyloučí.
- Datum po `To` plán vyloučí.
- Kombinace více plánů vrátí pouze položky platné v zadaný den.

Spustit:

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
dotnet build SportSys.slnx
```

### Chybové a prázdné stavy

- Platné datum bez odpovídajících plánů zobrazí stávající zprávu
  „Pro zadané parametry nebyly nalezeny žádné tréninkové plány.“
- Prázdné datum neaktivuje časový filtr.
- Chybějící sezóna nebo fáze se nadále zpracuje stávajícím časným návratem.
- Datum mimo interval sezóny není validační chyba; přirozeně může vést
  k prázdnému výsledku.

## Manuální akceptace

1. Otevřít `/sport/Training/Plan`, vybrat sezónu a fázi a bez data ověřit
   stejný seznam jako před změnou.
2. Zadat datum uvnitř platnosti jednoho plánu a ověřit jeho zobrazení.
3. Zadat datum přesně rovné `From` a následně `To`; plán musí zůstat zobrazen.
4. Zadat datum mimo platnost části plánů a ověřit, že neplatné plány zmizí,
   zatímco struktura pondělí až neděle a nastavení prázdných řádků zůstanou
   zachovány.
5. Kombinovat datum s kategorií, typem, lokalitou a fází a ověřit průnik všech
   podmínek.
6. Vymazat datum a ověřit automatické obnovení standardního seznamu.
7. Ověřit ruční zápis i výběr data z kalendáře podporovaného prohlížečem.
8. Otevřít blok plánu a ověřit, že zobrazení a editace plánu ani vizualizace
   spojených plánů nebyly změněny.

## Beze změny

- Databázové entity, EF Core konfigurace, schéma a migrace.
- Význam a ukládání `TrainingPlan.From` a `TrainingPlan.To`.
- Povinnost vybrat sezónu a jednu fázi tréninku před načtením plánu.
- Filtry kategorií, typů, lokalit a nastavení prázdných řádků.
- Projekce `TrainingPlanScheduleItemDto` a zobrazení platnosti v tooltipu.
- Řazení, seskupování, spojování přes `TrainingPlanGroup`, lanes a legenda.
- Zobrazení a editace jednotlivých tréninkových plánů.
- Sdílená ViewComponent a její SCSS.

## Mimo rozsah

- Výchozí předvyplnění data dnešním dnem.
- Omezení kalendáře na hranice sezóny.
- Filtrování podle překryvu s vícedenním intervalem.
- Změna nebo validace uložených intervalů `From–To`.
- Přidání nového JavaScriptového date pickeru, knihovny, CSS nebo tlačítka
  pro vymazání.
- Změna filtrů reálného rozvrhu `/sport/Training/Schedule` nebo požadavků
  `/sport/Training/Requirement`.

## Hotovo, když

- Za filtrem **Fáze tréninku** je nepovinné pole **Datum platnosti**.
- Změna i vymazání data automaticky obnoví stránku se zachováním ostatních
  GET filtrů.
- Vyplněné datum omezuje databázový dotaz inkluzivně podle `From` a `To`.
- Nevyplněné datum vrací přesně dosavadní výsledek.
- Datum lze kombinovat se všemi stávajícími filtry stránky.
- Prázdný výsledek používá stávající UI zprávu a týdenní vizualizace se jinak
  nezmění.
- Dokumentace odpovídá výslednému chování a relevantní testy i build projdou.
