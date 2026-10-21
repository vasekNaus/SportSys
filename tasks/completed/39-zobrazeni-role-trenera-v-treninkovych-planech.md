# Implementační plán: #39 Zobrazení role trenéra v tréninkových plánech

**Issue:** [#39 — Zobrazení role trenéra v tréninkových plánech](https://github.com/vasekNaus/SportSys/issues/39)

**Stav:** Implementováno; ověření dotazu na SQL Serveru čeká na dostupnou konfiguraci.

## Cíl

Na stránce `/sport/Training/Plan` zobrazovat přiřazené trenéry ve formátu
`Příjmení (Role)`, například `Křepel (H), Nauš (A)`. Zkratku přebírat
z `CoachRole.Code`, nikoli odvozovat z názvu nebo ID role.

Stejná přiřazení a stejné pořadí používat v grafickém rozvrhu, jeho tooltipech
a tabulkovém přehledu na této stránce. Zachovat zalamování a současný vzhled.

## Výchozí stav

- Issue je otevřené a při přípravě plánu nemá komentáře ani štítky.
- `sport.CoachTrainingPlan` již obsahuje `CoachRoleId`, navigaci `CoachRole`
  a interval `ValidFrom`–`ValidTo`. `dbo.CoachRole` má povinný jednoznakový
  `Code` a číselný `Order`. **Změna databázového modelu není potřeba.**
- `TrainingScheduleService.GetTrainingPlansAsync` načítá
  `SeasonCategory.Order` do `TrainingPlanScheduleItemDto.SeasonCategoryOrder`.
  Přetížení `AssignCoachesAsync` pro plány ale načítá pouze jméno a platnost
  přiřazení. Filtruje překryv s platností plánu, deduplikuje podle celého jména
  a vrací `SimpleCoachDto`, které nemá identitu ani roli.
- `TrainingScheduleBlockFactory.CreateBlock` sestavuje společné `Coaches`
  a znovu je globálně řadí podle `FullName`. Pouhé seřazení ve službě proto
  nestačí; u spojeného bloku se ztratí pořadí kategorií i rolí.
- `EventModelFactory.CreateTrainingPlanBlock` používá
  `TrainingScheduleBlockData.CoachSurnameSummary`. `CreateTooltip` vypisuje
  celá jména bez rolí a tooltipy členů skupiny spojuje pomocí ` | `.
- `_ScheduleBlockContent.cshtml` vykresluje text trenérů plánu třídami
  `schedule-block-coaches schedule-block-coaches--wrap`. Stávající
  `Styles\_schedule.scss` již podporuje zalamování a výšku podle obsahu.
- `Training\Plan\Index.cshtml.cs` předává stejné DTO do grafického rozvrhu
  i `PlanRows`. `_TrainingPlanTable.cshtml` dosud vypisuje celá jména bez rolí.
  Tabulka zachovává jeden řádek na plán, bez vizuální agregace.
- Samostatná stránka `Detail` neexistuje. `/sport/Training/Plan/Edit`
  je editační formulář a již umožňuje vybírat trenéra a roli.

## Potvrzené požadavky a rozhodnutí

### Zadání issue

- Zobrazit `CoachRole.Code` za příjmením v závorce.
- Řadit vzestupně podle `SeasonCategory.Order`, potom `CoachRole.Order`
  a nakonec podle příjmení.
- Zajistit shodné informace v bloku a tooltipu včetně spojených plánů.
- Zachovat zalamování při nedostatku místa a nenarušit vzhled rozvrhu.

### Upřesnění uživatele

Uživatel upřesnil, že tréninkovým plánem je myšlena stránka s rozvrhem
`pages/training/plan`. Cílovou existující stránkou je tedy
`Areas\sport\Pages\Training\Plan\Index`. Zmínka issue o detailu neznamená
vytvoření nové stránky ani rozšíření editace. Zobrazení rolí pokryje také
současný tabulkový přehled na téže stránce.

### Technická rozhodnutí

- Zachovat vrstvy `Razor → Contract → Database`, bez nové závislosti.
- Informace o roli přidat pouze kontraktu tréninkových plánů.
  `SimpleCoachDto`, `ITrainingScheduleItem` a skutečné tréninky nerozšiřovat
  o volitelné vlastnosti, které pro ně tento úkol nevyžaduje.
- Příjmení nadále odvozovat existující metodou
  `TrainingScheduleService.ExtractLastName` z `User.DisplayName`.
- Pro příjmení použít explicitní české porovnávání
  `StringComparer.Create(CultureInfo.GetCultureInfo("cs-CZ"), ignoreCase: true)`,
  obdobně jako existující `RequirementCoachGroups.Group`.
- Role se čte z konkrétního `CoachTrainingPlan`, ne z požadavku na tréninky,
  jiné kategorie ani personálního nastavení trenéra.

## Technický návrh

### Datový tok a Contract

V `TrainingScheduleDto.cs` přidat plánově specifický DTO, například
`TrainingPlanCoachDto`, s `CoachId`, `FullName`, `LastName`,
`CoachRoleId`, `CoachRoleCode` a `CoachRoleOrder`.
Do `TrainingPlanScheduleItemDto` přidat kolekci `CoachAssignments` tohoto typu.
Názvy nových symbolů jsou návrhem; nejde o již existující API.

V přetížení `AssignCoachesAsync` pro plány rozšířit stávající hromadný dotaz
o identitu trenéra a údaje role. Zachovat samostatné načtení přiřazení:
současný kód tím obchází omezení překladu korelovaného EF dotazu.
Nevytvářet dotaz na každého trenéra či plán a nenačítat celý číselník zvlášť.

Po materializaci pro každý plán:

1. Zachovat podmínku `ValidFrom <= plan.To && ValidTo >= plan.From`,
   včetně obou krajních dnů. Datum filtru `ValidOn` nadále vybírá plány,
   nemění nově pravidla pro zahrnutí trenérů.
2. Sestavit přiřazení včetně role. Nededuplikovat podle zobrazovaného jména:
   dva různí trenéři se stejným jménem musí zůstat samostatní.
3. Opakované intervaly stejné dvojice `CoachId`–`CoachRoleId` v jednom plánu
   sloučit pro zobrazení do jedné položky. Pokud historická data obsahují
   stejného trenéra s různými rolemi v překrývajícím se období plánu,
   zachovat obě role; neměnit pravidla zápisu ani vybírat roli náhodně.
4. Seřadit podle `CoachRoleOrder`, českého příjmení a pro úplnou shodu
   doplnit stabilní technické klíče, například `CoachId`, `CoachRoleId`.
5. Stávající `Coaches` zachovat jako kompatibilní projekci pro společný
   kontrakt; nové zobrazení plánů musí čerpat z `CoachAssignments`.

Mapování a filtrování materializovaných přiřazení oddělit do malé interní
čisté metody ve stejné službě, aby šlo ověřit platnost, identitu a role
bez zavádění databázového testovacího provideru.

### Sestavení bloků, pořadí a formát

V `TrainingScheduleBlockData.cs` doplnit samostatnou kolekci přiřazení plánů.
Při agregaci zachovat vazbu na zdrojový plán a jeho `SeasonCategoryOrder`.
Plochý seznam řadit podle:

```text
SeasonCategoryOrder → CoachRoleOrder → LastName (cs-CZ)
→ stabilní klíče pro shodu všech předchozích hodnot
```

Kód kategorie ani ID plánu nesmí předběhnout roli či příjmení při shodném
`SeasonCategoryOrder`. Nesmí se použít stávající globální řazení
`Coaches` podle `FullName`. Při spojení více plánů zachovat každé zdrojové
přiřazení, i když je stejný trenér ve více kategoriích nebo s jinou rolí;
neprovádět deduplikaci napříč plány.

V prezentační vrstvě použít jeden malý sdílený formatter/seřazení pro blok,
tooltip a tabulku, například v novém
`Models\TrainingSchedule\TrainingPlanCoachPresentation.cs`.
Formátovací logiku neimplementovat třikrát v Razor šablonách.
Helper přijímá Contract DTO, nikoli databázové entity.

`EventModelFactory.CreateTrainingPlanBlock` naplní dosavadní textové pole
`CoachSurnameSummary` již z formátu `Příjmení (Code)`. Název společné
vlastnosti v tomto úkolu není nutné refaktorovat.

Tooltip plánu musí použít stejný formát a pořadí přiřazení jako blok.
U sloučeného bloku uvést jeden společný seznam `Trenéři: …` ve výsledném
pořadí, nikoli znovu jinak seřazené seznamy členů. Ostatní informace všech
členů, kategorie i platnosti zachovat v dosavadních oddělených částech.
Větvení tooltipů skutečných tréninků se nemění.

`_TrainingPlanTable.cshtml` použije tentýž formatter nad přiřazeními řádku.
Grafický rozvrh i tabulka při prázdném seznamu zobrazí `-`; tooltip nebude
obsahovat prázdnou sekci trenérů.

### UI, validace a chyby

Zachovat současný textový výpis oddělený čárkami, nikoli vynucovat jednoho
trenéra na řádek. Stávající `schedule-block-coaches--wrap` zajistí přirozené
zalamování. Odkazy, barvy, pořadí ostatních řádků a rozměry časové osy neměnit.

Používat běžné Razor HTML enkódování, nikoli `Html.Raw`; totéž platí pro
atributy tooltipů. Zkratky nikdy nehardcodovat ani nenahrazovat názvem role.
Nový uživatelský vstup nevzniká a validace editačního formuláře se nemění.
Chyby načtení databáze nezachytávat tichým návratem prázdných přiřazení.
Povinný kód role vychází z existujícího modelu; nezavádět náhradní roli.

## Implementační kroky

### Fáze 1: Zachovat roli a identitu při načítání

Rozšířit Contract DTO a hromadný dotaz přiřazení plánů. Doplnit čisté
mapování, překryv platností a identitní deduplikaci. Pokrýt tyto části
jednotkovými testy ještě před zapojením UI.

### Fáze 2: Jednotná prezentace plánů

Zavést společný formatter/seřazení, propojit jej s agregací bloků
a `EventModelFactory`. Upravit pouze plánovou větev tooltipu tak, aby pořadí
odpovídalo celému bloku. Přepojit sloupec trenérů tabulkového přehledu.
Ověřit existující partial pro editovatelnou i needitovatelnou variantu;
jeho změna není nutná, pokud nadále vykresluje připravený text.

### Fáze 3: Regrese a dokumentace

Doplnit testy nespojených i spojených plánů a porovnání bloku s tooltipem.
V `docs\modules\sport.md` aktualizovat tvrzení o samotných příjmeních plánů,
zdokumentovat kódy rolí, pravidla řazení a tabulkový přehled.
Zachovat souběžné uživatelské změny dokumentace a požadavků na tréninky.

## Soubory ke změně

| Soubor | Změna |
|---|---|
| `src\SportSys.Contract\Models\TrainingScheduleDto.cs` | Nový DTO přiřazení plánu a `TrainingPlanScheduleItemDto.CoachAssignments`. |
| `src\SportSys.Contract\Services\TrainingScheduleService.cs` | Role, identita, platnost a testovatelné mapování v plánovém `AssignCoachesAsync`. |
| `src\SportSys.Razor\Models\TrainingSchedule\TrainingPlanCoachPresentation.cs` | Nový malý helper pro české řazení a text `Příjmení (Code)`. |
| `src\SportSys.Razor\Models\TrainingSchedule\TrainingScheduleBlockData.cs` | Plánově specifická agregace se zachováním zdroje a pořadí. |
| `src\SportSys.Razor\Models\TrainingSchedule\EventModel.cs` | Zapojení do `CreateTrainingPlanBlock` a tooltipu plánů. |
| `src\SportSys.Razor\Areas\sport\Pages\Training\Plan\_TrainingPlanTable.cshtml` | Shodný výpis trenérů s rolemi. |
| `tests\SportSys.Razor.Tests\TrainingPlanCoachPresentationTests.cs` | Nové testy formatteru a řazení. |
| `tests\SportSys.Razor.Tests\TrainingPlanCoachMappingTests.cs` | Nové testy materializovaných přiřazení a jejich platnosti. |
| `tests\SportSys.Razor.Tests\TrainingScheduleBlockFactoryTests.cs` | Testy agregace přiřazení plánů. |
| `tests\SportSys.Razor.Tests\TrainingScheduleComponentModelTests.cs` | Testy výsledného plánového modelu a tooltipů. |
| `docs\modules\sport.md` | Aktualizace popisu zobrazení plánů bez přepsání souběžných změn. |

## Testy a ověření

- Přenos `CoachRole.Code` a `CoachRole.Order` z materializovaných dat,
  včetně kódu, který není některou z ukázkových hodnot issue.
- Krajní překryvy platnosti, vyloučení přiřazení zcela mimo interval,
  sloučení opakovaných intervalů stejné dvojice a zachování různých rolí.
- Kategorie má přednost před rolí: asistent kategorie s nižším `Order`
  musí být před hlavním trenérem pozdější kategorie.
- Role má přednost před příjmením i celým jménem; při stejné roli rozhoduje
  příjmení, ne křestní jméno. Ověřit české řazení včetně `H`, `Ch`, `I`.
- Při stejném pořadí různých kategorií rozhoduje dále role a příjmení,
  nikoli kód kategorie. Vstup v opačném pořadí musí dát stejný výsledek.
- Různí trenéři se stejným jménem se nesloučí; stejný trenér ve dvou
  zdrojových plánech zůstane uveden dvakrát s příslušnými rolemi.
- Samostatný plán, explicitní `GroupId` i vizuální `VisualizationGroupId`:
  shodný formát a pořadí trenérů v bloku a tooltipu.
- Plán bez trenéra: blok a tabulka `-`, bez prázdné sekce v tooltipu.
- Nezměněné modely skutečných tréninků, ikony účasti, editovatelnost,
  názvy plánů, lokality a data exportu reálného rozvrhu.

Testy přidávat do aktivních tříd: `ScheduleEventModelFactoryTests.cs`
je v testovacím projektu explicitně vyřazen z kompilace a obsahuje staré API;
pro tento úkol jej neobnovovat ani neopravovat.

Po implementaci spustit cílené testy a build:

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --filter "FullyQualifiedName~TrainingPlanCoach|FullyQualifiedName~TrainingScheduleBlockFactoryTests|FullyQualifiedName~TrainingScheduleComponentModelTests|FullyQualifiedName~TrainingScheduleVisualizationGroupingTests|FullyQualifiedName~TrainingScheduleExcelExporterTests"
dotnet build SportSys.slnx
```

Na skutečném SQL Serveru navíc ověřit překlad rozšířeného dotazu a správné
přiřazení kódu ze souvisejícího číselníku. Nezavádět nové testovací balíčky
ani neaplikovat migrace kvůli tomuto ověření.

## Manuální akceptace

1. Otevřít `/sport/Training/Plan` a vybrat sezónu s přiřazenými trenéry.
   Blok i tabulka zobrazují například `Křepel (H), Nauš (A)`.
2. U dvou spojených kategorií ověřit pořadí kategorie → role → příjmení.
   Stejný trenér přiřazený oběma členům zůstává uveden u obou přiřazení.
3. Zapnout i vypnout „Spojovat tréninky“. Formát a řazení se zachovají,
   při zapnutí zůstává editace vizuálně spojených bloků vypnutá.
4. Zobrazit tooltip. Obsahuje stejné trenéry s rolemi ve stejném pořadí
   jako blok a zachovává další informace jednotlivých plánů.
5. Zúžit okno a ověřit zalamování delšího seznamu bez překrytí sousedních
   bloků a bez změny dosavadního barevného vzhledu.
6. Ověřit plán bez trenérů, filtr trenéra a datum platnosti. Filtr trenéra
   nadále vybírá celé plány, nikoli jen vybraná jména uvnitř jejich seznamu.
7. Otevřít skutečný rozvrh `/sport/Training/Schedule`: zobrazení účasti
   skutečných tréninků a zápasů zůstává beze změny.

## Beze změny

- Databázový model, existující číselník, seed, migrace a snapshot.
- Zápis a validace přiřazení v `TrainingPlanService` a stránce `Plan\Edit`.
- Pravidla platnosti, filtry a vizuální spojování plánů.
- Skutečné tréninky, zápasy, ikony účasti a export reálného rozvrhu do XLSX.
- Stávající SCSS a generované `wwwroot\css\site.css`.

## Mimo rozsah

- Nová stránka detailu, nové filtry rolí a správa číselníku.
- Změna názvů, příjmení nebo personálních údajů trenérů.
- Sjednocování historických přiřazení v databázi a změny pravidel editace.
- Nesouvisející rozpracované změny přehledu požadavků na tréninky.

## Hotovo, když

- [ ] Na stránce plánů je každý přiřazený trenér zobrazen s hodnotou `CoachRole.Code`.
- [ ] Blok, tooltip a tabulka používají `Příjmení (Code)` a stejné řazení.
- [ ] Kategorie a role mají přednost před českým řazením podle příjmení.
- [ ] Spojené bloky neztrácejí zdrojová přiřazení ani rozdílné role trenéra.
- [ ] Zachovány jsou platnost, filtry, zalamování, vzhled a editovatelnost.
- [x] Procházejí cílené testy a build.
- [ ] SQL dotaz je ověřen na SQL Serveru; v prostředí není nakonfigurovaný `ConnectionStrings:DefaultConnection`.
- [x] Dokumentace odpovídá novému chování.
- [x] Nevznikla změna schématu, migrace, snapshotu ani samostatného detailu.
