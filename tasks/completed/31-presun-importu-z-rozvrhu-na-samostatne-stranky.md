# Implementační plán: #31 Přesun importů z rozvrhu na samostatné stránky

**Issue:** [#31 — Přesun importů z rozvrhu na samostatné stránky](https://github.com/vasekNaus/SportSys/issues/31)

**Stav:** Implementováno.

## Cíl

Odebrat oba importní formuláře ze stránky rozvrhu a zpřístupnit je na dvou
samostatných stránkách. Zachovat současné zpracování souborů, validaci,
výsledné souhrny a varování; na rozvrhu ponechat pouze navigační tlačítka.

## Výchozí stav

- Aktivní stránka rozvrhu je `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml`
  s modelem `IndexModel` v `Index.cshtml.cs`.
- Model rozvrhu aktuálně obsluhuje oba importy: přijímá `KisTrainingImportDto`
  a `KisMatchImportDto`, volá `KisTrainingImportService` a
  `KisMatchImportService` a uchovává výsledek i varování přes `TempData`.
- Tréninkový import vyžaduje aktivní sezónu, fázi pro nově vytvořené tréninky
  a XLSX do 10 MiB. Import zápasů vyžaduje sezónu a export `games.xlsx` do
  10 MiB. Oba importy zobrazují nejvýše 20 varování.
- Seznam aktivních sezon a fází lze načíst přes
  `TrainingScheduleService.GetSeasonsAsync()` a
  `TrainingScheduleService.GetTrainingPhasesAsync()`.
- Sdílený layout `src/SportSys.Razor/Pages/Shared/_Layout.cshtml` nemá
  breadcrumb komponentu. Novější komentář k issue výslovně rozhodl breadcrumb
  nepřidávat.

## Potvrzené požadavky a rozhodnutí

- Rozvrh nebude obsahovat importní formuláře, pouze tlačítka vedoucí na
  odpovídající importní stránky.
- Každá importní stránka zachová svůj formulář, výběr souboru, validační a
  informační hlášky, spuštění importu a zobrazení výsledku.
- Obě stránky nabídnou tlačítko návratu na rozvrh. Novější komentář k issue
  ruší požadavek na breadcrumb navigaci.
- Jde o přesun existujícího UI a jeho obsluhy. Chování importů a jejich pravidla
  se nemění.

## Technický návrh

- Umístit nové Razor Pages do oblasti `sport`:
  - `/sport/Training/Import/Trainings` pro import tréninků,
  - `/sport/Training/Import/Matches` pro import zápasů.
- Přesunout importní stav a handlery z `Schedule.IndexModel` do odpovídajících
  modelů stránek. Tréninková stránka načte sezony a fáze; stránka zápasů načte
  sezony. Obě budou volat stávající služby z `SportSys.Contract` a nebudou
  přistupovat přímo k databázi.
- Zachovat kontrolu přípony, neprázdného souboru a limitu 10 MiB, současné
  mapování chyb na validační hlášky i výsledné počty. Po úspěšném importu
  použít PRG přesměrování zpět na tutéž importní stránku; zpráva a nejvýše
  20 varování se zobrazí po přesměrování.
- Při chybě validace nebo zpracování znovu načíst nabídky a vrátit stejnou
  importní stránku s chybami formuláře.
- Breadcrumb se nezobrazuje; navigaci zpět zajišťuje tlačítko na každé stránce.
- Databázový model se nemění; migrace není potřeba.

## Implementační kroky

### Fáze 1: Přesun importu tréninků

1. Vytvořit `Training/Import/Trainings/Index.cshtml` a
   `Index.cshtml.cs` s formulářem stávajícího importu, nabídkou aktivních sezon
   a fází, obsluhou `KisTrainingImportService.ImportAsync` a zachováním
   výsledného souhrnu a varování.
2. Doplnit tlačítko „Zpět na rozvrh“.
3. Ověřit zachování validačních chyb pro chybějící, prázdný, příliš velký
   nebo nepodporovaný soubor a chyb importní služby.

### Fáze 2: Přesun importu zápasů

1. Vytvořit `Training/Import/Matches/Index.cshtml` a
   `Index.cshtml.cs` s formulářem importu zápasů, nabídkou aktivních sezon
   a obsluhou `KisMatchImportService.ImportAsync`.
2. Zachovat specifické zpracování neplatného XLSX, validační hlášky, výsledek
   importu a nejvýše 20 varování.
3. Doplnit tlačítko „Zpět na rozvrh“.

### Fáze 3: Očištění rozvrhu a dokumentace

1. Ze `Schedule/Index.cshtml` odstranit oba formuláře a jejich výsledkové
   bloky; na jejich místo přidat dvě tlačítka odkazující na nové stránky.
2. Ze `Schedule/Index.cshtml.cs` odstranit pouze stav, závislosti, TempData
   a handlery používané výhradně importy. Zachovat filtrování, vykreslení
   rozvrhu, export a ostatní chování.
3. Aktualizovat `docs/modules/sport.md`: tabulku relevantních stránek a popisy
   importů včetně nových rout a ponechaných validačních/výsledkových pravidel.

### Fáze 4: Testy a ověření

1. Doplnit cílené testy pro validaci a výsledek obou samostatných importních
   stránek podle možností testovacího vzoru projektu; existující testy
   importních služeb/modelů ponechat.
2. Spustit `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`
   a `dotnet build SportSys.slnx`.
3. Ručně ověřit navigaci mezi rozvrhem a oběma stránkami, chování při chybě
   i zobrazení výsledku po úspěšném importu.

## Soubory ke změně

- `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Import/Trainings/Index.cshtml`
  *(nový)*
- `src/SportSys.Razor/Areas/sport/Pages/Training/Import/Trainings/Index.cshtml.cs`
  *(nový)*
- `src/SportSys.Razor/Areas/sport/Pages/Training/Import/Matches/Index.cshtml`
  *(nový)*
- `src/SportSys.Razor/Areas/sport/Pages/Training/Import/Matches/Index.cshtml.cs`
  *(nový)*
- `docs/modules/sport.md`
- `tests/SportSys.Razor.Tests/` — cílené testy importních stránek, pokud
  odpovídající testovací vzor umožní ověřit jejich handlery bez neúměrné
  integrační infrastruktury.

## Testy a ověření

- Automatické testy ověří, že obě stránky zachovají požadovaná validační
  pravidla, zavolají odpovídající importní službu a po úspěchu zpřístupní
  souhrn i omezený seznam varování.
- Stávající testy `KisTrainingImportModelTests` a
  `KisMatchImportModelTests` ověřují doménové chování importů a musí zůstat
  zelené.
- Build ověří Razor kompilaci nových stránek, jejich rout a odkazů.

## Manuální akceptace

- Rozvrh již nezobrazuje importní formuláře; tlačítka Import tréninků a Import
  zápasů otevřou správnou stránku.
- Každá stránka má tlačítko „Zpět na rozvrh“ a nezobrazuje breadcrumb.
- Neplatné nebo chybějící soubory a neplatné vstupy zobrazí chybu na příslušné
  importní stránce bez ztráty voleb formuláře.
- Úspěšný import zobrazí na své stránce odpovídající počty a nejvýše 20
  varování s číslem řádku.

## Beze změny

- Importní algoritmy a kontrakty `KisTrainingImportService`,
  `KisMatchImportService`, `KisTrainingImportDto` a `KisMatchImportDto`.
- Filtry, vykreslení a export rozvrhu.
- Databázový model, konfigurace, závislosti a migrace.
- Ostatní stránky a hlavní navigace aplikace.

## Mimo rozsah

- Úprava mapování dat KIS, nová importní funkcionalita nebo změna chování při
  opakovaném importu.
- Breadcrumb navigace na importních stránkách ani globální breadcrumb
  komponenta.

## Hotovo, když

- Na rozvrhu zůstala pouze dvě tlačítka pro přechod na importní stránky.
- Oba importy fungují na samostatných routách, zachovávají validaci, výsledky
  i varování a mají tlačítko návratu na rozvrh bez breadcrumb navigace.
- Dokumentace popisuje nové stránky a build i testy projdou.
- Nebyla změněna databáze ani vytvořena migrace.
