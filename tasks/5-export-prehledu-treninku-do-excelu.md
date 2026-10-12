# #5 Export přehledu tréninků do Excelu

## Cíl

Doplnit na stránku `/sport/Training/Schedule` export aktuálně vyfiltrovaného
rozvrhu do souboru Microsoft Excel (`.xlsx`). Export nesmí měnit stávající
načítání ani vykreslení rozvrhu a musí používat stejná data i pravidla
seskupování jako obrazovka.

## Potvrzená rozhodnutí

- Export patří na stránku `/sport/Training/Schedule`.
- Tréninky propojené pomocí `sport.TrainingGroup` se exportují jako jeden řádek,
  stejně jako jeden blok ve vizuálním rozvrhu.
- Pokud filtry nevrátí žádný trénink, exportní akce se nezobrazí nebo bude
  zakázaná; prázdný Excel se nestahuje.
- Každý nepropojený trénink tvoří samostatný řádek.

> Potvrzené slučování upravuje původní formulaci issue „každý trénink jako jeden
> řádek“. Implementace má exportovat každý zobrazený blok jako jeden řádek,
> protože export má odpovídat aktuálně zobrazeným datům.

## Výstupní formát

- Název souboru:
  `treninky-{datum-od:yyyyMMdd}-{datum-do:yyyyMMdd}.xlsx`.
- Jeden list s názvem `Tréninky`.
- Data budou vytvořena jako excelová tabulka s filtrem v záhlaví.
- První řádek bude zvýrazněné záhlaví.
- Sloupce se automaticky přizpůsobí obsahu.
- Datum a časy budou uloženy jako typované excelové hodnoty, nikoli jako text:
  - `Datum`: formát `dd.MM.yyyy`,
  - `Čas od`, `Čas do`: formát `HH:mm`.

| Sloupec | Hodnota jednoho exportního řádku |
|---|---|
| Kategorie | Kategorie bloku spojené ` + ` v pořadí `SeasonCategory.Order`, názvu a ID |
| Datum | Datum tréninku |
| Čas od | Nejčasnější začátek ve skupině |
| Čas do | Nejpozdější konec ve skupině |
| Typ tréninku | Unikátní typy seřazené a spojené `, ` |
| Lokalita | Unikátní neprázdné lokality seřazené a spojené `, ` |
| Trenéři | Unikátní zobrazovaná jména trenérů seřazená a spojená `, `; bez trenéra `-` |

Seskupování se provádí pouze v rámci stejného data. Záznamy bez `GroupId`
se neslučují. Časová mezera mezi členy skupiny zůstává zahrnuta do intervalu
od nejčasnějšího začátku po nejpozdější konec, stejně jako ve vizuálním bloku.

## Technický návrh

### Rozdělení odpovědností

- `SportSys.Contract` nadále pouze načte vyfiltrované
  `TrainingScheduleItemDto`; není potřeba nový DB dotaz ani změna databázového
  modelu.
- Sdílená logika vytvoření zobrazovaných bloků bude oddělena od výpočtu CSS
  pozice, aby ji používal vizuální rozvrh i Excel export.
- Generování Excelu bude v projektu `SportSys.Razor`, protože jde o prezentační
  výstup. Razor nebude získávat přímou závislost na `SportSys.Database`.
- Pro vytvoření `.xlsx` použít `ClosedXML` `0.105.1`. Knihovna podporuje vytvoření
  listu a tabulky, typované datum/čas, formátování, `AdjustToContents()` a zápis
  workbooku do streamu bez instalace Microsoft Excel.

### Navržené typy

1. Zavést prezentační model logického bloku, například
   `TrainingScheduleBlockData`, který bude obsahovat:
   - zdrojové položky,
   - agregovanou kategorii,
   - datum pro reálný rozvrh,
   - agregovaný začátek a konec,
   - samostatné kolekce nebo souhrny typů, lokalit a trenérů,
   - pořadí kategorie a minimální ID pro stabilní řazení.
2. Zavést jednu sdílenou továrnu/agregátor, například
   `TrainingScheduleBlockFactory`, která:
   - vytvoří samostatné bloky pro položky bez `GroupId`,
   - seskupí položky se stejným `GroupId`,
   - zachová současné pořadí kategorií a stabilní řazení,
   - vrátí bloky seřazené podle data, času, kategorie a minimálního ID.
3. Upravit `TrainingScheduleComponentModel`, aby z těchto bloků pouze dopočítal
   lanes, barvu, tooltip a procentuální pozici na časové ose.
4. Zavést `TrainingScheduleExcelExporter`, který přijme již agregované bloky
   a vrátí obsah souboru jako `byte[]` nebo stream.

Tím se zabrání tomu, aby měl export vlastní kopii pravidel pro `GroupId`,
kategorie, časy a trenéry.

## Implementační kroky

1. **Přidat Excel závislost**
   - Do `src/SportSys.Razor/SportSys.Razor.csproj` přidat balíček
     `ClosedXML` ve verzi `0.105.1`.
   - Nepoužívat staré Apollo EPPlus projekty: nejsou součástí aktuální solution,
     jsou založené na starší struktuře a přinesly by zbytečnou vazbu na externí
     submodul.

2. **Oddělit sdílené seskupování rozvrhu**
   - Přesunout logiku z privátních metod `CreateBlocks` a agregační části
     `CreateBlock` v
     `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleComponentModel.cs`
     do samostatného typu ve stejné složce.
   - Zachovat současné chování vizuálního rozvrhu:
     - skupiny pouze uvnitř jednoho řádku/data,
     - pořadí kategorií,
     - minimální a maximální čas,
     - unikátní a stabilně seřazené hodnoty,
     - chování nepropojených kolizí v lanes.
   - Agregovat typ a lokalitu odděleně. Stávající text
     `"{typ} - {lokalita}"` zůstane pouze prezentačním formátem bloku na stránce.

3. **Implementovat generátor workbooku**
   - Přidat například
     `src/SportSys.Razor/Services/TrainingScheduleExcelExporter.cs`.
   - Vytvořit workbook, list `Tréninky`, záhlaví a datové řádky.
   - Vložit rozsah jako excelovou tabulku s jednoznačným interním názvem bez
     diakritiky, například `TrainingSchedule`.
   - Nastavit české názvy sloupců, datové formáty a přiměřené šířky.
   - U dlouhých seznamů trenérů, typů a lokalit povolit zalamování textu a
     omezit automatickou šířku na rozumné maximum.
   - Workbook uložit do paměti a stream před předáním odpovědi vrátit na
     začátek.

4. **Přidat GET handler exportu**
   - V
     `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule.cshtml.cs`
     přidat `OnGetExportAsync`.
   - Sdílet normalizaci filtrů s `OnGetAsync`, aby oba handlery stejně:
     - ověřily aktivní sezónu,
     - odstranily neplatné kategorie, typy a lokality,
     - ověřily povinné datum od/do a `DateFrom <= DateTo`.
   - Nevytvářet export z hodnot odeslaných klientem bez opětovného načtení dat.
     Handler znovu zavolá `TrainingScheduleService.GetTrainingsAsync` se
     stejnými normalizovanými filtry.
   - Pokud nejsou data, nevracet prázdný workbook; vrátit uživatelsky
     srozumitelnou odpověď bez souboru.
   - Při úspěchu vrátit `File(...)` s MIME typem
     `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
     a bezpečně sestaveným názvem souboru.
   - Předat `CancellationToken` až do databázového dotazu.

5. **Doplnit exportní akci do stránky**
   - V
     `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule.cshtml`
     přidat do existujícího GET formuláře druhé submit tlačítko s handlerem
     `Export`.
   - Tlačítko ponese aktuální hodnoty všech filtrů automaticky jako query
     parametry stejného formuláře.
   - Použít text `Export do Excelu`, ikonu
     `fa-solid fa-file-excel fa-fw` a existující button styly.
   - Akci vykreslit pouze tehdy, když
     `Model.ScheduleView?.HasItems == true`.
   - Běžné tlačítko `Zobrazit rozvrh` musí nadále volat výchozí GET handler.

6. **Registrace služby**
   - Pokud bude exporter instanční služba, zaregistrovat jej v Razor projektu
     u ostatních čistě prezentačních služeb.
   - Databázové a aplikační služby ponechat výhradně v
     `AddSportSysServices()`; exporter nesmí přijímat `SportSysDbContext`.

7. **Aktualizovat dokumentaci**
   - V `docs/modules/sport.md` doplnit export rozvrhu, respektování filtrů,
     slučování `TrainingGroup` a sloupce souboru.
   - Neuvádět export jako obecnou funkci tréninkového plánu
     `/sport/Training/Plan`; issue se týká pouze reálného rozvrhu.

## Testy

Protože solution aktuálně nemá testovací projekt, přidat minimální
`tests/SportSys.Razor.Tests` a zahrnout jej do `SportSys.slnx`. Testy nemají
vyžadovat SQL Server.

### Jednotkové testy seskupování

- nepropojené tréninky vytvoří samostatné bloky,
- stejné `GroupId` ve stejný den vytvoří jeden blok,
- stejné `GroupId` v různých dnech nevytvoří společný blok,
- výsledná kategorie dodrží pořadí kategorií,
- čas je minimum `TimeFrom` a maximum `TimeTo`,
- typy, lokality a trenéři jsou unikátní, stabilně seřazené a správně spojené,
- skupina bez trenéra má hodnotu `-`,
- pořadí exportních řádků je datum, čas, kategorie a minimální ID.

### Jednotkové testy Excel exportu

- výstup lze znovu otevřít pomocí `ClosedXML`,
- workbook obsahuje právě list `Tréninky`,
- záhlaví má přesně sedm požadovaných sloupců ve správném pořadí,
- počet datových řádků odpovídá počtu zobrazených bloků,
- datum a časy jsou uloženy jako typované hodnoty se správným formátem,
- sloučená skupina má agregované kategorie, časy, typy, lokality a trenéry.

### Test handleru nebo integrační kontrola

- query parametry exportu odpovídají bindovaným filtrům stránky,
- neplatná sezóna, kategorie, typ, lokalita nebo interval nevytvoří soubor,
- prázdný výsledek nevytvoří soubor,
- odpověď s daty má správný MIME typ, příponu `.xlsx` a neprázdný obsah.

## Manuální akceptace

1. Otevřít `/sport/Training/Schedule`, nastavit sezónu, více kategorií, typů,
   lokalit a omezený interval.
2. Porovnat počet a obsah vizuálních bloků s řádky exportu.
3. Ověřit samostatný i propojený trénink, více trenérů a trénink bez trenéra.
4. Otevřít soubor v Microsoft Excelu nebo LibreOffice a ověřit tabulku,
   filtrování, české záhlaví a formáty data a času.
5. Změnit každý filtr jednotlivě a ověřit, že export odpovídá novému rozvrhu.
6. Ověřit, že při nulovém výsledku není exportní akce dostupná.

## Mimo rozsah

- Export obecného tréninkového plánu `/sport/Training/Plan`.
- Export dalších interních údajů, například fáze, poznámky, stavu nebo ID.
- Změny databázového modelu a EF Core migrace.
- Ukládání vygenerovaných souborů na server.
- Asynchronní/background generování exportu.

## Hotovo, když

- Export stáhne validní `.xlsx` pro aktuální normalizované filtry.
- Jeden řádek odpovídá jednomu vizuálnímu bloku rozvrhu.
- Vizuální rozvrh a export používají jednu sdílenou implementaci seskupování.
- Soubor obsahuje přesně požadovaných sedm sloupců.
- Prázdný výsledek nenabízí ani negeneruje soubor.
- Stávající filtrování a vykreslení rozvrhu zůstane beze změny.
- Cílené testy a `dotnet build SportSys.slnx` projdou.
