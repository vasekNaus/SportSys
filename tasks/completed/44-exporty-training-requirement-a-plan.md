# Implementační plán: #44 Exporty na stránkách Training/Requirement a Training/Plan

**Issue:** [#44 — Exporty na stránkách Training/Requirement a Training/Plan](https://github.com/vasekNaus/SportSys/issues/44)

**Stav:** Implementováno; export rozvrhu do PDF zůstává na žádost uživatele odložen.

## Cíl

Přidat samostatný export do XLSX pro tabulku požadavků na tréninky a tabulku pokrytí na `/sport/Training/Requirement` a pro tabulku tréninkových plánů a tabulku pokrytí na `/sport/Training/Plan`. Každý export musí odpovídat řádkům zobrazeným pro aktuální filtry, vynechat akční sloupce a použít název souboru z issue.

## Výchozí stav

- `Training/Requirement/IndexModel.OnGetAsync` načítá filtrované `Requirements` a data pro pokrytí do `CoverageRequirements` a `CoveragePlans`. Sdílenou tabulku vykresluje `TrainingRequirementCoverageViewComponent` přes `TrainingRequirementCoverageBuilder`.
- `Training/Plan/IndexModel.OnGetAsync` načítá filtrované `PlanRows` a samostatná data pro pokrytí. Tabulka je v `_TrainingPlanTable.cshtml`; grafický rozvrh vykresluje `TrainingSchedule` komponenta.
- Obě stránky používají GET filtry. Pokrytí záměrně nefiltruje podle fáze, dne ani lokality; export má odpovídat řádkům existující tabulky, nikoli měnit její logiku.
- `SportSys.Razor` již odkazuje na ClosedXML. `TrainingScheduleExcelExporter` a `Training/Schedule/IndexModel.OnGetExportAsync` poskytují vzor pro tvorbu XLSX, DI registraci a vrácení souboru. V aktivní aplikaci není PDF exporter.
- Datový model ani databázové schéma nejsou pro XLSX exporty potřeba měnit.

## Potvrzené požadavky a rozhodnutí

- Na každé ze dvou stránek budou dva samostatné exporty Excel: požadavky, resp. přehled plánů, a porovnání požadavků s plánem.
- Soubor požadavků: `TrainingRequirement.xlsx`; soubor pokrytí: `TrainingRequirementCoverage.xlsx`; soubor plánů: `TrainingPlan.xlsx`.
- V XLSX bude tučné záhlaví, autofilter, zamrznutý první řádek a šířky sloupců upravené podle obsahu.
- Datové hodnoty se zapíší jako skutečná data a čísla, nikoli jako naformátované texty. Z exportů se vynechají akční sloupce.
- Uživatel výslovně odložil export PDF („Zatím export do PDF neřeš“). PDF proto není součástí tohoto implementačního rozsahu.

## Technický návrh

- Přidat do `SportSys.Razor` exportní službu pro tabulkové XLSX přehledy, která používá existující ClosedXML a společně aplikuje formát záhlaví, autofilter, freeze prvního řádku a automatické šířky.
- Exportovat tři datové tvary: požadavky na tréninky, plánové řádky a řádky `TrainingRequirementCoverageBuilder`. Sdílený export pokrytí použít z obou stránek.
- GET handlery v PageModelech znovu načtou data stejnou cestou jako `OnGetAsync` (včetně normalizace filtrů) a předají exporteru stejné řádky, jaké vykresluje stránka. Zachovat vícenásobné hodnoty filtrů v exportním požadavku.
- Sloupce `Od` a `Do` budou Excel date values; `Rozsah [h]`, `Požadavek [h]` a `Plán [h]` budou numerické hodnoty. U přehledu plánů zachovat typované datumy a časy a numerickou délku trvání.
- Při nulovém počtu řádků exportní akci nezpřístupnit nebo vrátit uživatelsky srozumitelný prázdný stav v souladu se stávajícím vzorem exportu rozvrhu.

## Implementační kroky

### Fáze 1: Sdílený XLSX exporter

1. Přidat `src/SportSys.Razor/Services/TrainingOverviewExcelExporter.cs` se samostatnými metodami pro požadavky na tréninky, přehled plánů a pokrytí.
2. Využít ClosedXML již dostupný v `SportSys.Razor.csproj`; nastavení exportního listu sjednotit podle vzoru `TrainingScheduleExcelExporter` a registrovat službu v `Program.cs`.
3. Zapsat přesné hlavičky a zdrojové hodnoty z modelů, vynechat akční sloupce a aplikovat společné formátování XLSX.

### Fáze 2: Exporty požadavků

1. V `Areas/sport/Pages/Training/Requirement/Index.cshtml` přidat tlačítko nad tabulku požadavků a nad tabulku pokrytí.
2. V `Requirement/Index.cshtml.cs` přidat GET handlery, které používají stejnou normalizaci a filtrování jako `OnGetAsync`; pro pokrytí exportovat výsledek `TrainingRequirementCoverageBuilder`.
3. Vrátit XLSX s názvy `TrainingRequirement.xlsx` a `TrainingRequirementCoverage.xlsx`.

### Fáze 3: Exporty plánů

1. Přidat tlačítko přehledu plánů do `_TrainingPlanTable.cshtml` a tlačítko pokrytí do `Plan/Index.cshtml` před vykreslení sdílené komponenty.
2. V `Plan/Index.cshtml.cs` přidat GET handlery, které načtou stejné filtrované `PlanRows` a řádky pokrytí jako běžné zobrazení, včetně filtrování dnů.
3. Vrátit XLSX s názvy `TrainingPlan.xlsx` a `TrainingRequirementCoverage.xlsx`. Export pokrytí musí zachovat současnou sémantiku samostatné tabulky pokrytí.

### Fáze 4: Testy a ověření

1. Přidat `tests/SportSys.Razor.Tests/TrainingOverviewExcelExporterTests.cs` pro hlavičky, vynechání akcí, skutečné typy dat/hodin a společné vlastnosti listu.
2. Doplnit testy handlerů pro názvy a MIME typ souborů a pro předání aktuálních filtrů/řádků; navázat na `TrainingRequirementTests.cs` a podle potřeby přidat cílený test pro stránku Plan.
3. Ověřit všechna čtyři tlačítka a exporty ručně s kombinací jednoho i více filtrů a s prázdným výsledkem.

## Soubory ke změně

- `src/SportSys.Razor/Services/TrainingOverviewExcelExporter.cs` — nový exporter.
- `src/SportSys.Razor/Program.cs` — DI registrace exporteru.
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/_TrainingPlanTable.cshtml`
- `tests/SportSys.Razor.Tests/TrainingOverviewExcelExporterTests.cs` — nový test exporteru.
- `tests/SportSys.Razor.Tests/TrainingRequirementTests.cs` a případný nový test PageModelu Plan.

## Testy a ověření

- XLSX lze otevřít přes ClosedXML a obsahuje očekávané hlavičky, pořadí sloupců a všechny odpovídající řádky bez sloupce Akce.
- Datumové buňky jsou hodnoty data a číselné/hodinové buňky zůstávají numerické; kontrolovat hodnotu buňky, nikoli pouze její formát.
- Každý list má tučné záhlaví, autofilter, zamrznutý první řádek a upravené šířky.
- Handlery vracejí správný MIME typ, jména souborů a stejný filtrovaný obsah jako odpovídající tabulky na stránce.
- Cílené ověření: `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`; případné změny SCSS se v tomto rozsahu neočekávají.

## Manuální akceptace

- Na `/sport/Training/Requirement` jsou samostatná tlačítka Excel pro požadavky na tréninky a pokrytí. Oba exporty odpovídají aktuálním filtrům a mají správný název.
- Na `/sport/Training/Plan` jsou samostatná tlačítka Excel pro přehled plánů a pokrytí. Export plánů respektuje všechny filtry tabulky včetně dne; pokrytí odpovídá aktuálnímu zobrazení pokrytí.
- V každém exportu jsou sloupce `Akce` vynechány, datumové a číselné hodnoty zůstávají strojově použitelné a formát listu odpovídá společným pravidlům.
- Při prázdném výsledku není nabídnut soubor s nesprávným nebo zavádějícím obsahem.

## Beze změny

- Filtry, datové načítání ani význam existujících tabulek; export pouze zrcadlí jejich aktuální řádky.
- Tabulka požadavků na zápasy na stránce Requirement, problémové bloky na stránce Plan a existující export `/sport/Training/Schedule`.
- Databázové entity, Contract API, schéma SQL Serveru a migrace.

## Mimo rozsah

- Export grafického rozvrhu stránky Plan do PDF je odložen na žádost uživatele; přestože je v původním issue, nebude součástí této implementace.
- Export tabulky požadavků na zápasy, import/export jiných přehledů a změny vizuálního rozvrhu.
- Změna filtrovací logiky tabulky pokrytí nebo rozšíření jejích dimenzí.

## Hotovo, když

- Každá ze dvou cílových stránek nabízí dva samostatné XLSX exporty se správnými názvy a přesnými sloupci bez akcí.
- Exporty vracejí stejná data jako příslušné tabulky při stejných filtrech a zachovávají datové typy hodnot.
- XLSX formátování a testy uvedené výše procházejí.
- Export PDF zůstává odložen a není omylem zahrnut do implementace tohoto plánu.
