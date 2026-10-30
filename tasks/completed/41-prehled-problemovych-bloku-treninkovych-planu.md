# Implementační plán: #41 Přehled problémových bloků tréninkových plánů

**Issue:** [#41 — Přehled problémových bloků tréninkových plánů](https://github.com/vasekNaus/SportSys/issues/41)

**Stav:** Implementováno; podrobnosti jsou v
`41-prehled-problemovych-bloku-treninkovych-planu-IMPL.md`.

## Cíl

Na stránce `/sport/Training/Plan` zobrazit pod rozvrhem a tabulkovými přehledy
pouze konfliktní dvojice plánovaných tréninků. Detekovat překryv trenéra jako
chybu a překryv kategorie jako chybu na ledě, případně upozornění mimo led.
Přehled musí odpovídat právě zobrazenému rozvrhu a jeho filtrům.

## Výchozí stav

- `Areas/sport/Pages/Training/Plan/Index.cshtml.cs` načítá plány přes
  `TrainingScheduleService.GetTrainingPlansAsync`. Filtry sezóny, kategorie,
  typu, lokality, fáze, platnosti a trenéra se aplikují při načítání; vybrané
  dny se aplikují následně přes `WeekDayNames.FilterByDay`. Výsledkem po
  všech těchto krocích jsou `PlanRows`.
- `TrainingPlanScheduleItemDto` již obsahuje den v týdnu, časy, interval
  platnosti, kategorii, typ, `GroupId`, `VisualizationGroupId` a přiřazení
  trenérů s jejich ID a rolí. Databázová změna pro konfliktní kontroly není
  potřeba.
- Stránka `/sport/Training/Plan` vykresluje nejprve rozvrh, poté tabulku
  plánů a `TrainingRequirementCoverage`. Nový přehled patří za ně.
- `MergeTrainings` vytváří pouze vizualizační seskupení; `GroupId` označuje
  explicitně propojené plány. Tyto vazby mají pro kontrolu konfliktů odlišný
  význam.
- DTO nyní poskytuje `IsDryTraining`, ale ne jednoznačný příznak pro led.
  `ETrainingType` rozlišuje `Dry` a `Ice`; klasifikaci typu je nutné předat
  přes Contract DTO, nikoli odkazovat z Razor vrstvy přímo na Database.

## Potvrzené požadavky a rozhodnutí

### Potvrzené požadavky z issue

- Přehled je pod rozvrhem a přehledem plánů a obsahuje pouze nalezené
  konflikty.
- Kontrolují se konflikty trenéra a kategorie.
- Stejný trenér v překrývajících se trénincích je `Error`.
- Souběh stejné kategorie na ledě je `Error`; souběh stejné kategorie mimo
  led je `Warning`.
- Záznam obsahuje závažnost, typ problému, kategorii, případného trenéra,
  typ tréninku, časový interval, oba konfliktní tréninky a popis.
- Výsledky respektují právě aktivní filtry stránky. Řazení je podle
  závažnosti (`Error` před `Warning`), dne, začátku a kategorie.
- Komentáře k issue nejsou; nebyla nalezena další potvrzená upřesnění.

### Technická rozhodnutí

- Výpočet bude čistou prezentační logikou nad `PlanRows` po všech filtrech
  včetně dne. Nebude dělat další databázové dotazy. Pokud je vybrán filtr
  trenéra, položky a jejich přiřazení zůstanou přesně takové, jaké se
  zobrazují v rozvrhu; přiřazení ostatních trenérů uvnitř nalezeného plánu
  se dodatečně neodfiltrují.
- Konflikt vyžaduje shodný den v týdnu, překryv intervalů platnosti
  `From–To` a překryv časů. Intervaly budou posuzovány jako
  `[začátek, konec)`, takže konec jednoho tréninku přesně na začátku druhého
  konflikt nevytvoří. Čas konfliktu je průnik obou časových intervalů.
- Záznamy se stejným neprázdným databázovým `GroupId` představují jednu
  společnou plánovanou aktivitu a nebudou vykázány jako dva konfliktní
  tréninky. Samotný `VisualizationGroupId` ani `MergeTrainings` nesmí
  konflikty skrýt: vizuální sloučení může spojovat i nesouvisející plány.
- Výstup bude po dvojicích. Pokud se ve stejném čase překrývají tři nezávislé
  plány, zobrazí se každá konfliktní dvojice. Konfliktní interval, den,
  kategorie a stabilní ID zajistí deterministické řazení i při shodě.
- Závažnost konfliktu kategorie se určí podle dvojice: obsahuje-li některý
  plán typ `Ice`, jde o `Error`; dvojice plánů mimo led je `Warning`. Toto
  pravidlo pokrývá i kombinaci ledu a neledové aktivity.
- Typ led/suchá příprava se bude klasifikovat identifikátorem enumu na
  straně Contract a předávat DTO příznakem `IsIceTraining`. Razor nebude
  porovnávat lokalizovaný název ani přidávat referenci na Database.

## Technický návrh

1. Rozšířit `TrainingPlanScheduleItemDto` o `IsIceTraining` a nastavit jej
   v projekci `TrainingScheduleService.GetTrainingPlansAsync` podle
   `ETrainingType.Ice`. Stávající `TrainingTypeId`, `TrainingTypeName` ani
   `IsDryTraining` se nemění.
2. Přidat do `SportSys.Razor.Models.TrainingSchedule` typ výsledného
   problémového řádku a čistý builder, například `TrainingPlanProblemBuilder`.
   Builder vytvoří z `PlanRows` dvojice a pro každou použije shodu trenéra
   (ID trenéra) nebo kategorie (kód kategorie), průnik platnosti a času,
   den a pravidla závažnosti.
3. Typy řádků ponesou údaje pro tabulku: severity, druh problému, den,
   kategorii/kategorie, trenéra jen u jeho konfliktu, typ/y tréninku,
   průnik času, popisy obou plánů a detail. Název plánu použije `Title`;
   při jeho absenci srozumitelnou kombinaci kategorie, typu a lokality.
4. Přidat `TrainingPlanProblemsViewComponent` a sdílené Razor view. Komponenta
   obdrží připravené řádky, nevstupuje do Contract služeb ani databáze.
   Tabulka zobrazí jen problémy, výrazně odliší `Error`/`Warning` a předá
   význam také textem, ne pouze barvou. Při nulovém počtu problémů zobrazí
   informaci, že nebyly nalezeny žádné problémové bloky.
5. `IndexModel` vytvoří problémové řádky z `PlanRows`; view stránky vloží
   komponentu za existující přehled plánů i `TrainingRequirementCoverage`.
   Vzhled komponenty doplní samostatný SCSS partial zapojený v `Styles/site.scss`.

## Implementační kroky

### Fáze 1: Typ tréninku v Contract DTO

1. Doplnit `IsIceTraining` do `TrainingPlanScheduleItemDto`.
2. Nastavit příznak v projekci `TrainingScheduleService.GetTrainingPlansAsync`
   přes `ETrainingType.Ice`.
3. Ověřit zachování projekce, přiřazení trenérů a existujícího rozvrhu.

### Fáze 2: Detekce problémových dvojic

1. Přidat prezentační model problémového řádku a čistý builder do
   `Models/TrainingSchedule/`.
2. Implementovat časový a datumový průnik, filtr stejného dne, ignorování
   dvojice uvnitř explicitní skupiny a kontroly trenéra/kategorie.
3. Implementovat závažnost, text popisu a deterministické řazení podle
   `Error`, `Warning`, dne, času od, kategorie a stabilních ID.

### Fáze 3: Zobrazení na stránce plánů

1. Přidat výsledek builderu do `Areas/sport/Pages/Training/Plan/Index.cshtml.cs`
   z `PlanRows` po filtrování dnů.
2. Přidat `TrainingPlanProblemsViewComponent` a její view do sdílených
   komponent.
3. Vložit sekci pod rozvrh a tabulkové přehledy podle pořadí z issue.
4. Doplnit SCSS komponenty a zapojit jej do `Styles/site.scss`; generované
   `wwwroot/css/site.css` neupravovat ručně.

### Fáze 4: Testy a dokumentace

1. Přidat cílené testy builderu, vazby komponenty na stránku a vykreslení
   prázdného i konfliktního výsledku.
2. Aktualizovat `docs/modules/sport.md` o pravidla přehledu a význam
   `GroupId` oproti vizualizačnímu sloučení.
3. Spustit cílené testy `SportSys.Razor.Tests`, build řešení a CSS build.

## Soubory ke změně

- `src/SportSys.Contract/Models/TrainingScheduleDto.cs`
- `src/SportSys.Contract/Services/TrainingScheduleService.cs`
- `src/SportSys.Razor/Models/TrainingSchedule/` (nový model a builder)
- `src/SportSys.Razor/ViewComponents/TrainingPlanProblemsViewComponent.cs` (nový)
- `src/SportSys.Razor/Pages/Shared/Components/TrainingPlanProblems/Default.cshtml` (nový)
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml`
- `src/SportSys.Razor/Styles/_training-plan-problems.scss` (nový)
- `src/SportSys.Razor/Styles/site.scss`
- `tests/SportSys.Razor.Tests/` (nové cílené testy)
- `docs/modules/sport.md`

## Testy a ověření

- Builder: shodný trenér a různý trenér; rozlišení ID trenéra bez ohledu na
  shodná jména; shodná a různá kategorie.
- Čas: částečný průnik, úplné překrytí, shodný začátek/konec a dotýkající se
  intervaly bez konfliktu; vykreslený čas je průnik, ne celé intervaly.
- Plánovací platnost a den: překrývající se a oddělené rozsahy `From–To`,
  shodný a různý den.
- Skupiny: dvojice uvnitř téhož `GroupId` se nehlásí; různé `GroupId` i
  položky pouze spojené přes `VisualizationGroupId` se vyhodnotí samostatně.
- Závažnost: konflikt trenéra je vždy `Error`; konflikt kategorie s typem
  `Ice` je `Error`, dvojice mimo led je `Warning`, včetně smíšeného páru.
- Filtry stránky: testem `IndexModel` ověřit, že výsledky vycházejí z
  `PlanRows` po aplikaci kategorie, typu, lokality, fáze, data, trenéra
  i vybraného dne; přepínač `MergeTrainings` nemění výsledek kontrol.
- Výsledek: žádné konflikty znamenají prázdný seznam; tři současné nezávislé
  plány vytvoří všechny konfliktní dvojice; řazení je stabilní.
- Render: správné sloupce, text závažnosti a popis problému, bezpečné
  vykreslení názvů plánů a prázdný stav komponenty.
- Ověřit:

  ```powershell
  dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
  dotnet build SportSys.slnx
  ```

  CSS zkompilovat přes `npm run build:css` v `src\SportSys.Razor`.

## Manuální akceptace

1. Na `/sport/Training/Plan` zobrazit souběžné plány se stejným trenérem
   a ověřit `Error`, čas průniku a oba identifikovatelné bloky.
2. Ověřit souběh stejné kategorie na dvou ledových plánech (`Error`) a na
   neledových plánech (`Warning`); pro smíšený pár ověřit `Error`.
3. Ověřit dvojici plánů se stejným explicitním `GroupId` a dvojici pouze
   vizuálně sloučenou přes `MergeTrainings`; první se nehlásí jako konflikt,
   druhá se vyhodnotí.
4. Změnit každý filtr včetně dne a ověřit, že problémový přehled odpovídá
   právě viditelným plánům. Změna `MergeTrainings` nesmí konflikty potlačit.
5. Zvolit datum, kdy se rozsahy platnosti plánů nepřekrývají, a ověřit, že
   se konflikt nevykáže. Při nulovém výsledku ověřit vysvětlující prázdný stav.
6. Ověřit pořadí `Error` před `Warning`, dále podle dne, času a kategorie.

## Beze změny

- Databázové entity, schéma, snapshot a EF Core migrace; migrace se nevytváří
  ani neaplikuje.
- Pravidla načítání plánů a existující GET filtry; detekce používá výsledek
  stránky po jejich aplikaci.
- Vizualizace, seskupování ani editace tréninkových plánů.
- Rozvrh skutečných tréninků a zápasů, stránka požadavků a `SportSys.Web`.
- `TrainingPlan.DurationMinutes`; výpočet konfliktů používá `TimeFrom` a
  `TimeTo`.
- Generované CSS; mění se pouze zdrojové SCSS.

## Mimo rozsah

- Automatická oprava konfliktů nebo změna přiřazení trenérů.
- Kontroly skutečných tréninků, zápasů, dostupnosti trenérů mimo plánované
  bloky nebo kapacity lokalit.
- Nové databázové tabulky/sloupce, EF Core migrace nebo nové knihovny.

## Hotovo, když

- [x] Sekce problémových bloků je na stránce `/sport/Training/Plan` pod
  rozvrhem a přehledem plánů.
- [x] Zobrazují se pouze problémové dvojice; nulový výsledek má srozumitelný
  prázdný stav.
- [x] Detekují se překryvy trenéra a kategorie s požadovanou závažností.
- [x] Časový průnik, den a interval platnosti jsou vyhodnoceny správně.
- [x] Všechny aktuální filtry respektují rozvrh a vizualizační sloučení
  nezakrývá konflikty.
- [x] Výsledky obsahují detaily obou plánů a jsou řazeny podle issue.
- [x] Procházejí cílené testy, build řešení a CSS build.
- [x] Databázový model ani EF Core migrace nebyly změněny.
