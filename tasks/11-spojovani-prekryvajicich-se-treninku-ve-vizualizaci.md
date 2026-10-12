# Implementační plán: #11 Spojování překrývajících se tréninků ve vizualizaci

**Issue:** [#11 — Spojování překrývajících se tréninků ve vizualizaci](https://github.com/vasekNaus/SportSys/issues/11)

**Stav:** Implementováno a ověřeno.

## Cíl

Doplnit na stránky `/sport/Training/Schedule` a `/sport/Training/Plan`
volitelný GET filtr **Spojovat tréninky**. Po zapnutí se mají pouze pro účely
vizualizace spojit všechny položky ve stejném řádku, jejichž časové intervaly
se překrývají nebo se přesně dotýkají. Spojení musí být tranzitivní, musí
zachovat existující explicitní skupiny a nesmí zapisovat do databáze ani měnit
editaci. Export rozvrhu musí respektovat stejnou hodnotu checkboxu a použít
stejné skupiny jako aktuální vizualizace.

## Výchozí stav

- Reálný rozvrh tvoří Razor Page
  `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml` a
  `Index.cshtml.cs`.
- Týdenní plán tvoří Razor Page
  `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml` a
  `Index.cshtml.cs`.
- Obě stránky načítají DTO přes
  `src/SportSys.Contract/Services/TrainingScheduleService.cs` a předávají
  položky po jednotlivých datech nebo dnech týdne sdílenému
  `TrainingScheduleViewComponent`.
- `ITrainingScheduleItem.GroupId` nyní reprezentuje pouze skutečné členství v
  `sport.TrainingGroup` nebo `sport.TrainingPlanGroup`.
- `TrainingScheduleBlockFactory.CreateBlocks` v
  `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleBlockData.cs`
  slučuje pouze položky se stejným nenulovým `GroupId`. Ostatní položky
  ponechává samostatně bez ohledu na jejich čas.
- `TrainingScheduleComponentModel` následně rozděluje vzniklé bloky do lanes.
  Nespojené kolidující bloky se proto zobrazí nad sebou; přesně navazující
  bloky mohou sdílet stejný lane.
- Agregace spojeného bloku už podporuje všechny údaje požadované issue:
  kategorie, typy, lokality, trenéry, tooltip, minimální začátek, maximální
  konec a deterministické řazení kategorií.
- Kliknutí na blok otevírá editaci člena s nejnižším ID. Samotná editační
  služba vždy znovu načte skutečné členství v databázové skupině.
- Export Schedule používá stejnou továrnu bloků a dnes slučuje pouze skutečné
  databázové skupiny.
- Issue nemá komentáře ani dodatečná rozhodnutí.

## Potvrzené požadavky a rozhodnutí

- Na obou stránkách bude checkbox **Spojovat tréninky**.
- Checkbox bude GET-bound a jeho výchozí hodnota bude `false`.
- Při vypnutém checkboxu zůstane současné vykreslení beze změny.
- Při zapnutém checkboxu se spojí položky ve stejném řádku, pokud:
  - se jejich intervaly překrývají,
  - nebo `TimeTo` jedné položky přesně odpovídá `TimeFrom` druhé položky.
- Spojení bude tranzitivní. Pokud A souvisí s B a B souvisí s C, vznikne jeden
  blok A + B + C, i když A a C přímo nekolidují.
- Výsledný blok použije minimum `TimeFrom` a maximum `TimeTo` všech členů.
- Kategorie budou seřazeny podle `SeasonCategoryOrder`, názvu a ID a spojeny
  pomocí ` + `. Stávající agregace typů, lokalit, trenérů a tooltipů se
  použije pro všechny členy výsledné skupiny.
- Spojování proběhne až nad materializovanými DTO v aplikační vrstvě
  `SportSys.Contract`; nevznikne SQL dotaz pro sjednocování intervalů.
- Pro Schedule je hranicí skupiny konkrétní `Date`. Pro Plan je hranicí řádek
  `DayOfWeek`. Plány v témže dni týdne se posuzují podle času; jejich interval
  platnosti `From–To` není další podmínkou spojení, protože issue definuje
  spojení pouze časem a případný filtr `ValidOn` už předem omezuje načtená data.
- Existující explicitní skupiny zůstanou skupinami i tehdy, když mezi jejich
  členy existuje časová mezera. Při zapnutém filtru se mohou propojit s dalšími
  položkami pouze tehdy, když je splněna časová podmínka alespoň mezi některými
  skutečnými členy; samotná obálka explicitní skupiny nesmí vytvořit falešné
  spojení přes prázdnou mezeru.
- Checkbox ovlivní vizualizaci i Excel export Schedule. Při vypnuté hodnotě
  export sloučí pouze členy skutečných `TrainingGroup`; při zapnuté hodnotě
  použije stejné tranzitivní časové spojování jako zobrazený rozvrh.
- Při zapnutém checkboxu nebudou bloky ve vizualizaci editovatelné. Po jeho
  vypnutí se zachová současná editace jednotlivých záznamů a explicitních
  databázových skupin.
- Dynamicky spojený blok použije současný odkaz na editaci člena s nejnižším
  ID. Editační služby z tohoto ID znovu odvodí pouze skutečnou databázovou
  skupinu, takže virtuální skupina se nikdy neuloží ani hromadně needituje.

## Technický návrh

### Oddělení databázové a vizualizační skupiny

Nepřepisovat význam existujícího `GroupId`. V
`src/SportSys.Contract/Models/TrainingScheduleDto.cs` rozšířit
`ITrainingScheduleItem` a obě DTO o samostatnou nullable vlastnost, například:

```csharp
Guid? VisualizationGroupId { get; }
```

- `GroupId` zůstane přesným obrazem členství v databázi.
- `VisualizationGroupId` určí seskupení pouze pro aktuální vykreslení.
- Při vypnutém filtru bude `VisualizationGroupId` odpovídat `GroupId`.
- Při zapnutém filtru dostanou všechny položky jedné vypočtené komponenty
  stejné dočasné ID. Samostatná položka bez skutečné skupiny může zůstat s
  hodnotou `null`.

Toto oddělení zabrání tomu, aby prezentační volba změnila význam dat používaný
editací nebo budoucí aplikační logikou. Export může
`VisualizationGroupId` použít pro sestavení aktuálně požadovaného výstupu,
aniž by virtuální skupinu zaměnil za databázovou vazbu.

### Aplikační algoritmus spojování

Do `TrainingScheduleService` přidat malý interní, čistý helper pro přiřazení
`VisualizationGroupId` nad již načtenými DTO. Obě veřejné načítací metody
rozšířit o boolean parametr, například `mergeOverlapping`, s výchozím nebo
explicitně předávaným stavem `false`.

Zpracování jednoho data nebo dne týdne:

1. Vytvořit uzly pro všechny položky řádku.
2. Propojit všechny položky se stejným nenulovým databázovým `GroupId`.
3. Propojit dvojice, pro které platí inkluzivní kolize:

   ```text
   left.TimeFrom <= right.TimeTo
   && right.TimeFrom <= left.TimeTo
   ```

   Inkluzivní porovnání pokryje překryv i přesné navázání hranou.
4. Najít souvislé komponenty grafu, například pomocí union-find nebo
   deterministického průchodu komponentami.
5. Každé komponentě s více členy přiřadit společné
   `VisualizationGroupId`; členy a komponenty zpracovat ve stabilním pořadí
   podle času, pořadí kategorie a ID.

Grafové pojetí je nutné kvůli kombinaci tranzitivního časového spojení a
explicitních skupin s případnými mezerami. Prosté slučování obálek seřazených
intervalů by mohlo nesprávně připojit trénink ležící pouze v mezeře mezi členy
explicitní skupiny.

Helper se spustí:

- pro `TrainingScheduleItemDto` odděleně po `Date`,
- pro `TrainingPlanScheduleItemDto` odděleně po validovaném `DayOfWeek`.

Neplatný `TrainingPlan.DayName` musí nadále vyvolat stávající explicitní chybu.

### Vykreslení bloků

`TrainingScheduleBlockFactory` upravit tak, aby pro ViewComponent i export
seskupovala podle `VisualizationGroupId`. Při vypnutém checkboxu tato hodnota
odpovídá databázovému `GroupId`, takže se zachová současné chování. Při
zapnutém checkboxu oba výstupy použijí stejnou dočasnou skupinu vypočtenou
Contract službou.

Továrna bude nadále jediným místem pro agregaci kategorií, typů, lokalit,
trenérů, časů a tooltipových podkladů. Nesmí vzniknout druhá kopie agregačních
pravidel v Contract službě ani exportéru.

`TrainingScheduleComponentModel.CreateLanes` použije vizualizační seskupení.
Po vytvoření bloků zůstane stávající lane algoritmus beze změny: případné
bloky, které po seskupení stále kolidují, se vykreslí v samostatných lanes.

### Filtry a HTTP tok

Do obou PageModelů přidat:

```csharp
[BindProperty(SupportsGet = true)]
public bool MergeTrainings { get; set; }
```

Hodnota se předá příslušné metodě `TrainingScheduleService`. V obou Razor views
přidat checkbox do existujícího `.schedule-filter-options` vedle volby
**Zobrazovat prázdné řádky**. Změna checkboxu odešle stávající GET formulář,
aby se vizualizace ihned přegenerovala a ostatní filtry zůstaly zachovány.

`Schedule.OnGetExportAsync` předá do `TrainingScheduleService` stejnou hodnotu
`MergeTrainings` jako běžné zobrazení. Exportér následně vytvoří řádky podle
`VisualizationGroupId`, takže jeden exportní řádek odpovídá jednomu bloku,
který by pro stejné filtry vznikl ve vizualizaci.

## Implementační kroky

### Fáze 1: Rozšíření Contract DTO

1. Přidat `VisualizationGroupId` do `ITrainingScheduleItem`,
   `TrainingScheduleItemDto` a `TrainingPlanScheduleItemDto`.
2. Zachovat existující `GroupId` jako neměnný obraz databázové vazby.
3. Nepřidávat databázovou entitu, sloupec, konfiguraci ani migraci.

### Fáze 2: Aplikační spojování intervalů

1. Rozšířit `TrainingScheduleService.GetTrainingsAsync` a
   `GetTrainingPlansAsync` o volbu dynamického spojování.
2. Po `ToListAsync` inicializovat vizualizační skupiny z databázových
   `GroupId`.
3. Při zapnuté volbě rozdělit položky podle `Date`, respektive `DayOfWeek`, a
   vypočítat souvislé komponenty podle skutečných skupin a inkluzivní časové
   kolize.
4. Implementovat algoritmus jako interní čistý helper testovatelný bez
   databáze. Helper nesmí měnit `GroupId`, pořadí vstupních dat ani ostatní
   hodnoty DTO.
5. Zachovat stávající databázové filtry, projekce a řazení; intervalové
   spojování nesmí být součástí `IQueryable`.

### Fáze 3: Použití vizualizačních skupin ve sdílené komponentě

1. Upravit `TrainingScheduleBlockFactory.CreateBlocks`, aby seskupovala podle
   `VisualizationGroupId`.
2. V `TrainingScheduleComponentModel.CreateLanes` nadále používat tuto
   sdílenou továrnu.
3. Zachovat současnou implementaci `CreateBlock`, která agreguje všechny
   popisky a řadí kategorie.
4. Zachovat stávající výpočet barvy, tooltipu, odkazu na editaci, pozice,
   šířky, legendy a lanes.
5. Nevytvářet novou vykreslovací větev v
   `Pages/Shared/Components/TrainingSchedule/Default.cshtml`; view už pracuje s
   agregovaným blokem.

### Fáze 4: GET filtry v Schedule a Plan

1. Přidat `MergeTrainings` do obou `IndexModel` jako GET-bound boolean s
   výchozí hodnotou `false`.
2. Předat hodnotu do Contract služby při běžném `OnGetAsync`.
3. Přidat checkbox **Spojovat tréninky** do obou formulářů vedle volby
   prázdných řádků.
4. Na změnu checkboxu odeslat existující GET formulář bez nového JavaScriptu.
5. Zachovat normalizaci sezóny, kategorií, typů, lokalit, data a fáze.

### Fáze 5: Regrese exportu a editace

1. V `Schedule.OnGetExportAsync` předat do služby aktuální hodnotu query
   parametru `MergeTrainings`.
2. V `TrainingScheduleExcelExporter` používat stejnou tvorbu bloků podle
   `VisualizationGroupId` jako ViewComponent.
3. Zachovat jeden exportní řádek pro každý výsledný vizualizační blok v rámci
   konkrétního data.
4. Zachovat současné určení `EditItemId` jako nejnižší ID člena vizuálního
   bloku.
5. Neměnit `TrainingService`, `TrainingPlanService` ani jejich pravidla pro
   načtení a atomickou editaci skutečných skupin.

### Fáze 6: Dokumentace

Aktualizovat `docs/modules/sport.md`:

- přidat oba nové filtry a jejich výchozí vypnutý stav,
- popsat inkluzivní překryv, navazování a tranzitivní spojení,
- odlišit dočasnou vizualizační skupinu od `TrainingGroup` a
  `TrainingPlanGroup`,
- uvést hranici data nebo dne týdne,
- popsat, že export Schedule respektuje checkbox a používá stejné výsledné
  bloky jako vizualizace,
- potvrdit, že editace nadále používá pouze skutečné databázové vazby.

## Soubory ke změně

| Soubor | Změna |
|---|---|
| `src/SportSys.Contract/Models/TrainingScheduleDto.cs` | Samostatné ID dočasné vizualizační skupiny. |
| `src/SportSys.Contract/Services/TrainingScheduleService.cs` | Boolean volba a post-processing materializovaných DTO po datech nebo dnech týdne. |
| `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml.cs` | GET filtr a jeho shodné předání pro zobrazení i export. |
| `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml` | Checkbox **Spojovat tréninky**. |
| `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs` | GET filtr a předání do služby. |
| `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml` | Checkbox **Spojovat tréninky**. |
| `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleBlockData.cs` | Tvorba bloků podle výsledné vizualizační skupiny. |
| `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleComponentModel.cs` | Použití vizualizačního seskupení před lane algoritmem. |
| `src/SportSys.Razor/Services/TrainingScheduleExcelExporter.cs` | Použití stejných výsledných bloků jako ve vizualizaci. |
| `tests/SportSys.Razor.Tests/TrainingScheduleVisualizationGroupingTests.cs` | Nové testy intervalového a tranzitivního spojování. |
| `tests/SportSys.Razor.Tests/TrainingScheduleBlockFactoryTests.cs` | Testy volby skupinovacího klíče a agregace dynamické skupiny. |
| `tests/SportSys.Razor.Tests/TrainingScheduleComponentModelTests.cs` | Regrese bloků, lanes a editačního cíle. |
| `tests/SportSys.Razor.Tests/TrainingScheduleExcelExporterTests.cs` | Testy exportu při vypnutém i zapnutém dynamickém spojování. |
| `docs/modules/sport.md` | Dokumentace filtru a oddělení obou druhů skupin. |

`Default.cshtml`, SCSS a `site.css` se nemají měnit, pokud implementace
checkboxu použije existující třídy a vykreslení bloku nevyžádá prokazatelnou
úpravu.

## Testy a ověření

### Automatické testy aplikačního algoritmu

- vypnutý filtr ponechá dva překrývající se nepropojené tréninky v různých
  vizualizačních skupinách,
- částečně překrývající se intervaly vytvoří jednu skupinu,
- intervaly se shodným začátkem nebo koncem vytvoří jednu skupinu,
- intervaly navazující přesně hranou vytvoří jednu skupinu,
- intervaly s kladnou mezerou zůstanou oddělené,
- řetězec A–B–C vytvoří jednu komponentu, i když A a C přímo nekolidují,
- pořadí vstupu nezmění členství ani výsledné řazení,
- čtyři a více propojených intervalů nemají zvláštní limit,
- stejné časy v různých datech Schedule se nespojí,
- stejné časy v různých dnech týdne Plan se nespojí,
- explicitní skupina zůstane pohromadě i s mezerou mezi členy,
- položka ležící pouze v mezeře explicitní skupiny se bez přímého dotyku s
  členem nepřipojí,
- položka překrývající člena explicitní skupiny se tranzitivně připojí k celé
  skupině,
- `GroupId` zůstane po výpočtu beze změny.

### Automatické testy prezentace a regresí

- `TrainingScheduleBlockFactory` při vizualizačním režimu agreguje kategorie,
  typy, lokality a trenéry celé dynamické skupiny,
- výsledný blok používá minimum začátku a maximum konce,
- kategorie jsou v pořadí `SeasonCategoryOrder`, názvu a ID,
- zapnuté spojení sníží počet bloků a lane zůstane konzistentní,
- nesouvisející blok, který stále koliduje s výsledným blokem, se vykreslí v
  samostatném lane,
- dynamický blok zachová nejnižší ID jako odkaz na editaci,
- při vypnutém checkboxu zůstane export explicitní `TrainingGroup` beze změny,
- při zapnutém checkboxu Excel sloučí překrývající se a navazující tréninky do
  stejných řádků jako vizualizace,
- tranzitivní skupina A–B–C vytvoří v Excelu právě jeden řádek,
- dva intervaly s kladnou mezerou zůstanou v Excelu ve dvou řádcích,
- agregované hodnoty a pořadí členů jsou v zobrazení a exportu shodné.

Spustit:

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
dotnet build SportSys.slnx
```

## Manuální akceptace

1. Na `/sport/Training/Schedule` ponechat checkbox vypnutý a ověřit, že dva
   nepropojené překrývající se tréninky zůstávají v samostatných lanes.
2. Checkbox zapnout a ověřit, že stejné tréninky vytvoří jeden blok s
   agregovanými kategoriemi, časy, typy a trenéry.
3. Ověřit dvojici přesně navazující hranou a řetězec alespoň tří intervalů.
4. Ověřit dva intervaly s minutovou mezerou; nesmí se spojit.
5. Ověřit explicitní databázovou skupinu, samostatný překrývající se trénink a
   skupinu s časovou mezerou mezi členy.
6. Stejné scénáře ověřit na `/sport/Training/Plan` v jednom dni týdne a
   potvrdit, že se nic nespojuje napříč řádky pondělí až neděle.
7. Změnit checkbox a ověřit automatické obnovení stránky se zachováním všech
   ostatních filtrů.
8. Kliknout na dynamicky spojený blok a ověřit, že se otevře existující editace
   jednoho skutečného záznamu; databázová skupina ani další virtuálně spojení
   členové se nezmění.
9. Exportovat Schedule s vypnutým checkboxem a ověřit současné seskupování
   pouze podle explicitních `TrainingGroup`.
10. Zapnout checkbox, export zopakovat a ověřit, že překrývající se,
    navazující a tranzitivně propojené tréninky tvoří stejné bloky a řádky jako
    ve vizualizaci.

## Beze změny

- Databázové entity, EF Core konfigurace, schéma, migrace a model snapshot.
- Tabulky `sport.TrainingGroup` a `sport.TrainingPlanGroup` a jejich význam.
- SQL filtry pro sezónu, kategorie, typy, lokality, datum, fázi a platnost.
- Editační DTO, stránky, služby, concurrency kontrola a atomická editace
  skutečných skupin.
- Výpočet `DurationMinutes`.
- Agregované texty, tooltipy, barvy, legenda, časová osa, parita řádků a
  víkendové zvýraznění.
- Výstupní formát Excel exportu; mění se pouze určení výsledných řádků podle
  hodnoty checkboxu.
- SCSS a generovaný `wwwroot/css/site.css`.

## Mimo rozsah

- Ukládání automaticky vzniklých skupin do databáze.
- Vytváření, rušení nebo změna `TrainingGroup` a `TrainingPlanGroup`.
- Nový způsob hromadné editace virtuálně spojených položek.
- Spojování napříč různými daty nebo dny týdne.
- Spojování podle lokality, typu, fáze, trenéra nebo platnosti plánu.
- Nastavitelná tolerance mezery; spojí se pouze přesný dotyk nebo překryv.
- Export tréninkového plánu; dynamické spojování se týká existujícího exportu
  reálného Schedule.
- Nová JavaScriptová knihovna, CSS komponenta nebo změna barevného schématu.

## Hotovo, když

- Obě stránky obsahují výchozí vypnutý GET filtr **Spojovat tréninky**.
- Vypnutý filtr zachová současné bloky a lanes.
- Zapnutý filtr spojí všechny překrývající se a přesně navazující intervaly
  tranzitivně a pouze uvnitř jednoho řádku.
- Výsledný blok správně agreguje čas, kategorie, typy, lokality, trenéry,
  tooltip a legendu.
- Excel export při stejné hodnotě checkboxu obsahuje stejné bloky jako
  vizualizace Schedule.
- Skutečné `GroupId` zůstane oddělené a beze změny.
- Dynamické spojování proběhne až nad materializovanými DTO v Contract vrstvě.
- Editace ani export nezačnou považovat virtuální skupinu za databázovou
  vazbu; export ji používá pouze jako klíč pro agregaci výstupních řádků.
- Nevznikne změna databázového modelu ani EF Core migrace.
- Dokumentace odpovídá výslednému chování a cílené testy i build projdou.
