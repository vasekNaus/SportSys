# Implementační plán: #7 Přehled požadavků na tréninky

**Issue:** [#7 — Přehled požadavků na tréninky](https://github.com/vasekNaus/SportSys/issues/7)

**Stav:** Implementováno a ověřeno sestavením a automatickými testy.

## Cíl

Přidat do modulu Sport samostatnou read-only stránku
`/sport/Training/Requirement`, která zobrazí požadavky evidované v
`sport.TrainingRequirement`, umožní je filtrovat podle hlavních plánovacích
dimenzí a zpřístupní stránku z navigace pod názvem `Požadavky`.

Přehled musí zobrazit nejen vlastní hodnoty požadavku, ale také názvy
související sezóny, kategorie, typu a fáze tréninku a přiřazené trenéry včetně
jejich rolí. Razor vrstva získá všechna data výhradně přes novou Contract
službu.

## Výchozí stav

- Entita `TrainingRequirement` a její `DbSet<TrainingRequirement>` již
  existují v projektu `SportSys.Database`.
- Požadavek obsahuje:
  - sezónu a kategorii přes složenou vazbu `SeasonId +
    SeasonCategoryName`,
  - typ a fázi tréninku,
  - interval platnosti `From–To`,
  - požadovaný rozsah `DurationHours`.
- Vazební entita `CoachTrainingRequirement` přiřazuje k požadavku trenéra a
  jeho roli. Trenér poskytuje `DisplayName` a `PersonalNumber`, role poskytuje
  `Name`.
- V aplikaci neexistuje Contract DTO, služba ani Razor Page pro seznam
  požadavků.
- Existující stránky `Training/Schedule` a `Training/Plan` používají filtry
  sezóny, kategorií, typů a fáze a načítají pouze DTO přes Contract služby.
- Navigace modulu Sport je definována v
  `src/SportSys.Razor/Pages/Shared/_Layout.cshtml`; mezi položkami
  `Tréninkové plány` a `Zimní stadiony` nyní žádná položka není.
- Sdílené styly již obsahují vzory `filter-form`, `filter-row`, `field`,
  `filter-actions`, `button` a `grid`, takže pro běžný filtrovaný tabulkový
  přehled není potřeba nový SCSS.

## Potvrzené požadavky a rozhodnutí

### Potvrzeno v issue

- Vytvořit samostatný přehled dat z `sport.TrainingRequirement`.
- Přehled musí podporovat filtrování.
- Stránka bude dostupná na route `/sport/Training/Requirement`.
- Do menu modulu Sport přidat položku `Požadavky`.
- Položku vložit mezi `Tréninkové plány` a `Zimní stadiony`.
- Issue nemá komentáře, které by zadání dále měnily.

### Technická rozhodnutí podle existujících vzorů

- Stránka bude read-only. Issue nepožaduje vytvoření, editaci ani mazání
  požadavků.
- Filtry budou odesílány metodou GET, aby byl stav přehledu uložen v URL a
  fungovalo obnovení, záložky i tlačítko Zpět:
  - sezóna: jedna hodnota, výchozí je nejnovější aktivní sezóna,
  - kategorie: nula, jedna nebo více aktivních kategorií vybrané sezóny;
    prázdný výběr znamená všechny kategorie,
  - typ tréninku: nula, jedna nebo více hodnot; prázdný výběr znamená všechny
    typy,
  - fáze tréninku: nula, jedna nebo více hodnot; prázdný výběr znamená všechny
    fáze.
- Neplatné hodnoty z query stringu se před datovým dotazem odstraní podle
  načtených možností. Neplatná nebo chybějící sezóna se nahradí výchozí aktivní
  sezónou; pokud žádná aktivní sezóna neexistuje, stránka zobrazí prázdný stav
  bez DB dotazu na požadavky.
- Kategorie se po změně sezóny normalizují pouze na aktivní kategorie této
  sezóny. Samotný přehled však nebude skrývat historický požadavek jen proto,
  že jeho kategorie byla později zneaktivněna, pokud byl dotaz vyvolán platným
  filtrem sezóny bez výběru kategorií.
- Tabulka zobrazí sloupce:
  - Sezóna,
  - Kategorie,
  - Typ tréninku,
  - Fáze,
  - Platnost od,
  - Platnost do,
  - Rozsah v hodinách,
  - Trenéři.
- Trenéry zobrazit stabilně seřazené podle zobrazovaného jména, osobního čísla
  a role. Každé přiřazení formátovat jako `Jméno (role)`; pokud je
  `DisplayName` prázdné, použít `PersonalNumber`. Požadavek bez přiřazeného
  trenéra zobrazí `-`.
- Výsledky seřadit podle začátku sezóny sestupně, pořadí a názvu kategorie,
  `From`, `To`, názvu typu, názvu fáze a ID požadavku.
- Pro přehled vznikne samostatná `TrainingRequirementService`; nebude se
  rozšiřovat `TrainingScheduleService`, protože požadavky jsou samostatná
  doménová agenda a nepoužívají vizuální rozvrh.
- Změna nevyžaduje úpravu databázového modelu ani EF Core migraci.

## Technický návrh

### Contract model

Vytvořit `TrainingRequirementDto.cs` s prezentačními modely bez závislosti
Razor vrstvy na databázových entitách:

- `TrainingRequirementListItem`:
  - `Id`,
  - `SeasonId`,
  - `SeasonName`,
  - `SeasonCategoryName`,
  - `SeasonCategoryOrder`,
  - `TrainingTypeId`,
  - `TrainingTypeName`,
  - `TrainingPhaseId`,
  - `TrainingPhaseName`,
  - `From`,
  - `To`,
  - `DurationHours`,
  - `CoachAssignments`.
- `TrainingRequirementCoachListItem`:
  - `CoachId`,
  - `DisplayName`,
  - `PersonalNumber`,
  - `CoachRoleId`,
  - `CoachRoleName`,
  - odvozený zobrazovaný text lze vytvořit v Razor view nebo jako read-only
    vlastnost DTO bez databázové logiky.

Pro nabídky filtrů znovu použít existující `SeasonDto`,
`SeasonCategoryDto` a `LookupSelectItem`; nevytvářet jejich duplicitní varianty.

### Contract služba

Nová `TrainingRequirementService` bude obsahovat:

1. `GetSeasonsAsync` pro aktivní sezóny seřazené od nejnovější.
2. `GetCategoriesAsync(int seasonId)` pro aktivní kategorie vybrané sezóny.
3. `GetTrainingTypesAsync` a `GetTrainingPhasesAsync` pro lookup nabídky.
4. `GetAllAsync(...)` pro načtení filtrovaných požadavků.

Datový dotaz:

- začne z `_db.TrainingRequirements.AsNoTracking()`,
- vždy omezí data na vybranou sezónu,
- volitelné kolekce kategorií, typů a fází aplikuje pouze tehdy, když nejsou
  prázdné,
- projektuje názvy navigací a přiřazení trenérů přímo v SQL,
- nevrací EF entity ani nepoužívá `Include`, pokud všechny údaje lze získat
  projekcí,
- předává `CancellationToken` do všech asynchronních databázových operací.

Duplicitní lookup dotazy ze `TrainingScheduleService` se v rámci tohoto issue
nepřesouvají do nové sdílené služby. Nová služba může použít stejný projekční
vzor; větší refaktoring společných lookupů by byl mimo rozsah jednoduchého
přehledu.

### Razor Page

Vytvořit dvojici:

- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement.cshtml.cs`,
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement.cshtml`.

`RequirementModel`:

- injektuje pouze `TrainingRequirementService`,
- binduje filtry přes `[BindProperty(SupportsGet = true)]`,
- načte nabídky filtrů,
- normalizuje hodnoty z query stringu,
- po určení platné sezóny načte požadavky,
- poskytne view kolekce možností a výsledků.

View:

- nastaví titulek `Požadavky na tréninky`,
- použije existující GET filter form a multiselect vzor ze stránek
  `Training/Schedule` a `Training/Plan`,
- nabídne akce `Zobrazit` a `Vymazat filtr`,
- zobrazí sémantickou tabulku s českými názvy sloupců,
- formátuje data jako `dd.MM.yyyy` a `DurationHours` bez zbytečných koncových
  nul, ale beze změny uložené přesnosti,
- při nulovém výsledku zobrazí text
  `Žádné požadavky na tréninky neodpovídají filtru.`,
- při absenci aktivní sezóny zobrazí samostatnou srozumitelnou informaci a
  nepředstírá chybu aplikace.

### Navigace a dokumentace

V `_Layout.cshtml` vložit odkaz:

```text
asp-area="sport"
asp-page="/Training/Requirement"
text: Požadavky
```

Odkaz musí být bezprostředně za `Tréninkové plány` a před
`Zimní stadiony`. Použít ikonu z již načtené sady Font Awesome, například
`fa-list-check`.

V `docs/modules/sport.md` doplnit novou route, zdroj dat, podporované filtry,
sloupce přehledu a skutečnost, že jde o read-only stránku.

## Implementační kroky

### Fáze 1: Contract DTO

Vytvořit:

`src/SportSys.Contract/Models/TrainingRequirementDto.cs`

1. Definovat list item požadavku a přiřazení trenéra.
2. Použít typy odpovídající databázovému modelu: `DateOnly` a `decimal`.
3. Nevystavovat navigační entity ani kolekce z `SportSys.Database`.
4. Inicializovat textové a kolekční vlastnosti bezpečnými výchozími hodnotami.

### Fáze 2: Contract služba a registrace

Vytvořit:

`src/SportSys.Contract/Services/TrainingRequirementService.cs`

Upravit:

`src/SportSys.Contract/ServiceCollectionExtensions.cs`

1. Implementovat nabídky sezón, kategorií, typů a fází.
2. Implementovat `GetAllAsync` s volitelnými kolekčními filtry.
3. Projektovat přiřazení trenérů a jejich rolí do vnořených DTO.
4. Zajistit stabilní řazení výsledků i vnořených přiřazení.
5. Registrovat službu přes `AddScoped<TrainingRequirementService>()` vedle
   ostatních sportovních služeb.

### Fáze 3: PageModel a normalizace filtrů

Vytvořit:

`src/SportSys.Razor/Areas/sport/Pages/Training/Requirement.cshtml.cs`

1. Přidat GET bindované vlastnosti `SeasonId`,
   `SelectedCategoryNames`, `SelectedTrainingTypeIds` a
   `SelectedTrainingPhaseIds`.
2. Načíst aktivní sezóny a zvolit výchozí nejnovější sezónu.
3. Načíst aktivní kategorie vybrané sezóny a globální lookupy typů a fází.
4. Odstranit z filtrů hodnoty, které nejsou v odpovídajících nabídkách.
5. Zavolat `GetAllAsync` pouze s normalizovanými hodnotami.
6. Zachovat prázdný výběr kolekčního filtru jako význam `vše`, nikoli jako
   nulový výsledek.

### Fáze 4: Razor view

Vytvořit:

`src/SportSys.Razor/Areas/sport/Pages/Training/Requirement.cshtml`

1. Sestavit GET formulář podle filtrů Training Schedule/Plan.
2. U vícenásobných filtrů zobrazit v `<summary>` hodnotu `Vše`, jedinou
   vybranou položku nebo `Vybráno: N`.
3. Vykreslit tabulku požadavků s definovanými sloupci.
4. U přiřazení trenérů zajistit HTML encoding běžným Razor výstupem a oddělit
   více hodnot čárkou.
5. Doplnit prázdné stavy pro chybějící sezónu a nulový výsledek.
6. Nepřidávat akce Edit/Delete ani odkazy na dosud neexistující detail.

### Fáze 5: Navigace

Upravit:

`src/SportSys.Razor/Pages/Shared/_Layout.cshtml`

1. Přidat položku `Požadavky` na `/sport/Training/Requirement`.
2. Zachovat požadované pořadí mezi plánem a zimními stadiony.
3. Ověřit desktopovou i mobilní variantu stávajícího vnořeného menu.

### Fáze 6: Dokumentace

Upravit:

`docs/modules/sport.md`

1. Doplnit stránku do tabulky sportovních přehledů.
2. Popsat filtry, tabulkové sloupce, řazení a zobrazení trenérských rolí.
3. Uvést read-only charakter stránky a zdroj
   `sport.TrainingRequirement`.
4. Výslovně uvést, že stránka zapisuje pouze do žádné tabulky a nemění
   databázové schéma.

## Soubory ke změně

| Akce | Soubor |
|---|---|
| vytvořit | `src/SportSys.Contract/Models/TrainingRequirementDto.cs` |
| vytvořit | `src/SportSys.Contract/Services/TrainingRequirementService.cs` |
| upravit | `src/SportSys.Contract/ServiceCollectionExtensions.cs` |
| vytvořit | `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement.cshtml.cs` |
| vytvořit | `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement.cshtml` |
| upravit | `src/SportSys.Razor/Pages/Shared/_Layout.cshtml` |
| upravit | `docs/modules/sport.md` |
| vytvořit/rozšířit | cílené testy Contract služby a PageModelu |

## Testy a ověření

### Automatické testy Contract služby

Vytvořit vhodný Contract testovací projekt, pokud při implementaci stále
neexistuje, a použít stejný xUnit stack jako
`tests/SportSys.Razor.Tests`. Databázové testy musí používat izolovaný testovací
provider a nesmí vyžadovat produkční SQL Server.

Ověřit:

1. Bez kolekčních filtrů se vrátí všechny požadavky vybrané sezóny.
2. Filtr kategorií vrátí pouze vybrané kategorie.
3. Filtry typů a fází podporují jednu i více hodnot.
4. Kombinace filtrů používá průnik podmínek.
5. Výsledky jsou ve stabilním požadovaném pořadí.
6. Projekce obsahuje názvy sezóny, kategorie, typu a fáze.
7. Požadavek bez trenéra má prázdnou kolekci přiřazení.
8. Více trenérů a rolí se vrátí bez ztráty vazby a ve stabilním pořadí.
9. Každý dotaz respektuje zrušený `CancellationToken`.

### Testy PageModelu

Pokud existující testovací architektura umožní izolovat službu bez porušení
zavedeného vzoru, ověřit:

1. Chybějící `SeasonId` zvolí nejnovější aktivní sezónu.
2. Neplatná sezóna se nahradí výchozí.
3. Neplatné kategorie, typy a fáze se odstraní.
4. Prázdné kolekce znamenají všechny hodnoty.
5. Bez aktivní sezóny se nenačítají požadavky a model připraví prázdný stav.

Není-li pro PageModel vhodná izolace bez zavádění rozhraní pouze kvůli testu,
ponechat normalizaci v malé samostatně testovatelné metodě a integrační chování
ověřit manuálně. Nezavádět produkční abstrakci, která nemá jiný účel.

### Ověřovací příkazy

```powershell
dotnet build SportSys.slnx --no-restore
dotnet test --no-build --no-restore
```

SCSS build není potřeba, pokud implementace skutečně použije pouze existující
styly a žádný soubor v `Styles/` nezmění.

## Manuální akceptace

| Scénář | Očekávaný výsledek |
|---|---|
| Otevření menu Sport | `Požadavky` jsou mezi `Tréninkové plány` a `Zimní stadiony` |
| Otevření odkazu | Na `/sport/Training/Requirement` se zobrazí přehled |
| První otevření bez query stringu | Je vybrána nejnovější aktivní sezóna |
| Prázdné kolekční filtry | Zobrazí se všechny požadavky sezóny |
| Výběr více kategorií | Zobrazí se pouze požadavky vybraných kategorií |
| Výběr více typů nebo fází | Výsledky odpovídají průniku aktivních filtrů |
| Vymazání filtru | URL i formulář se vrátí do výchozího stavu |
| Požadavek s trenéry | Zobrazí se jména/osobní čísla a role |
| Požadavek bez trenéra | Ve sloupci Trenéři se zobrazí `-` |
| Neplatné hodnoty v URL | Stránka neselže a použije normalizované filtry |
| Žádný odpovídající záznam | Zobrazí se srozumitelný prázdný stav |
| Žádná aktivní sezóna | Stránka zobrazí informaci a nevyvolá chybu |
| Mobilní navigace | Nová položka je dostupná a menu zůstává ovladatelné |

## Beze změny

- Databázové entity `TrainingRequirement` a `CoachTrainingRequirement`.
- `SportSysDbContext` a existující mapování vztahů.
- Tabulky `sport.Training`, `sport.TrainingPlan` a jejich rozvrhové stránky.
- Editace tréninku a export rozvrhu.
- Správa sezón, kategorií a lookup tabulek.
- Obsah a zápisy do read-only schématu `plan.*`.
- SCSS a kompilovaný `wwwroot/css/site.css`, pokud budou existující styly
  dostačující.

## Mimo rozsah

- Vytváření, editace nebo mazání požadavků.
- Přiřazování a odebírání trenérů nebo jejich rolí.
- Export požadavků do Excelu či jiného formátu.
- Stránkování, řazení volené uživatelem a fulltextové hledání.
- Nový detail požadavku.
- Porovnávání požadavků se skutečnými tréninky nebo tréninkovými plány.
- Výpočty splnění požadovaných hodin.
- Změny databázového schématu a EF Core migrace.
- Refaktoring společných lookup metod všech sportovních služeb.

## Hotovo, když

- [x] Route `/sport/Training/Requirement` zobrazí read-only přehled z
      `sport.TrainingRequirement`.
- [x] Razor projekt neobsahuje přímou závislost na `SportSys.Database`.
- [x] Přehled zobrazuje sezónu, kategorii, typ, fázi, platnost, rozsah a
      přiřazené trenéry s rolemi.
- [x] Sezóna, kategorie, typy a fáze lze filtrovat přes GET parametry.
- [x] Neplatné filtry jsou bezpečně normalizovány.
- [x] Výsledky a přiřazení trenérů mají stabilní pořadí.
- [x] Prázdné stavy jsou uživatelsky srozumitelné.
- [x] Položka `Požadavky` je v menu na přesně požadované pozici.
- [x] Contract služba je registrována pouze v `AddSportSysServices()`.
- [x] Dokumentace modulu Sport odpovídá výslednému chování.
- [x] Cílené testy a `dotnet build SportSys.slnx` projdou.
- [x] Nebyla vytvořena ani upravena EF Core migrace.
