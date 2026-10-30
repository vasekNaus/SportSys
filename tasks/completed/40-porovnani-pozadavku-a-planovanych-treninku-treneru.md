# Implementační plán: #40 Porovnání požadavků a plánovaných tréninků trenérů

**Issue:** [#40 — Porovnání požadavků a plánovaných tréninků trenérů](https://github.com/vasekNaus/SportSys/issues/40)

**Stav:** Implementováno; podrobnosti jsou v
`40-porovnani-pozadavku-a-planovanych-treninku-treneru-IMPL.md`.

## Cíl

Na `/sport/Training/Requirement` i `/sport/Training/Plan` zobrazit stejnou
tabulku, která porovnává požadované a naplánované hodiny trenérů podle sezóny,
kategorie, typu tréninku, trenéra a role. Komponenta spotřebuje Contract DTO
předaná stránkou a nebude samostatně přistupovat k databázi.

## Výchozí stav

- `Areas/sport/Pages/Training/Requirement/Index.cshtml(.cs)` načítá
  `TrainingRequirementListItem` přes `TrainingRequirementService.GetAllAsync`.
  Trenérské přiřazení už nese `Id` trenéra, `CoachRoleId`, `CoachRoleName`
  a individuální `Hours`; `TrainingRequirementListItem` obsahuje sezónu,
  kategorii, `TrainingTypeId` a její název.
- `Areas/sport/Pages/Training/Plan/Index.cshtml(.cs)` načítá
  `TrainingPlanScheduleItemDto` přes `TrainingScheduleService.GetTrainingPlansAsync`.
  Každá položka obsahuje kategorii, název typu tréninku, `DurationMinutes`
  a `CoachAssignments` s `CoachId` a `CoachRoleId`, kódem a pořadím role.
  DTO zatím neobsahuje `TrainingTypeId`, název kategorie/sezóny ani název role.
- Stránka požadavků má vícefázový filtr; stránka plánů má jednofázový filtr.
  Stránka plánů navíc filtruje lokalitu a den týdne. Fáze, lokalita ani den
  nejsou součástí klíče porovnání popsaného v issue.
- Existující `TrainingPlan.DurationMinutes` je persisted computed hodnota
  načítaná z databáze; nesmí se počítat ani nastavovat v C#.
- `TrainingRequirementListItem.DurationHours` je celkový rozsah požadavku.
  Pro porovnání za trenéra a roli se musí používat jednotlivé hodnoty
  `CoachAssignments.Hours`, nikoli celkový rozsah požadavku.
- Aktuální výpis hodin požadavků používá české formátování v
  `Requirement/_CoachGroups.cshtml`; SCSS se překládá přes `Styles/site.scss`
  a generovaný `wwwroot/css/site.css` se neupravuje ručně.

## Potvrzené požadavky a rozhodnutí

### Potvrzené požadavky z issue

- Jedna sdílená komponenta se zobrazí na stránce požadavků i plánů.
- Komponenta nepřistupuje k databázi; pracuje s DTO předanými stránkou.
- Požadované hodiny se sčítají z `CoachTrainingRequirement.Hours`.
- Plánované hodiny se počítají jako součet `TrainingPlan.DurationMinutes`
  přiřazených danému trenérovi a roli, převedený na hodiny.
- Klíč porovnání je sezóna, kategorie, typ tréninku, trenér a role. Fáze
  v požadovaném klíči ani výstupních sloupcích není.
- Tabulka obsahuje sezónu, kategorii, typ tréninku, trenéra, roli,
  požadované hodiny, plánované hodiny a status. Výchozí řazení je sezóna,
  pořadí kategorie, typ tréninku, trenér a role.
- Status rozlišuje shodu, plán nad požadavkem a plán pod požadavkem; nesmí
  spoléhat pouze na barvu a musí mít ikonu i textovou nápovědu.
- Chybějící protistrana se počítá jako nula: samotný požadavek je
  „Nesplňuje plán“, samotný plán je „Nad plán“.
- Komponenta bude pod přehledem požadavků, respektive pod grafickým rozvrhem
  a tabulkou plánů.

### Upřesnění podle komentáře z 21. 10. 2026

[Nový komentář k #40](https://github.com/vasekNaus/SportSys/issues/40#issuecomment-6310598724)
nahrazuje původní zelený úspěšný stav při `Plán > Požadavek`:

- Text statusu bude **Nad plán**, nikoli „Splňuje plán“.
- Zelenou fajfku nahradí oranžový vykřičník (`fa-triangle-exclamation`).
- Použije se sémantický token `--color-warning-icon`, s oranžovou variantou
  pro světlý, tmavý i automatický tmavý režim. Ostatní varovné tokeny a jejich
  dosavadní použití se nemění.
- Tooltip „Plán obsahuje více hodin než požadavek.“ i výpočet hodin zůstávají
  beze změny. Pravidlo platí také pro plán bez požadavku.
- Změna sdíleného modelu a SCSS se projeví na obou stránkách.

### Technická rozhodnutí

- Zůstat u toku `Razor → Contract → Database`; ViewComponent pouze zobrazí
  připravené vstupy a čistý prezentační model provede agregaci/párování DTO.
  Žádné EF entity, `DbContext` ani databázové dotazy se do Razor komponenty
  nepřidají.
- Při načítání dat pro porovnání zachovat sezónu, kategorii, typ tréninku,
  datum platnosti a výběr trenérů z dané stránky. Protože fáze, den a lokalita
  nejsou v klíči a požadavky se sčítají přes fáze, porovnávací sady se načtou
  bez filtru fáze; filtr fáze/den/lokality nadále ovlivní pouze stávající
  přehled stránky.
- Výběr trenérů aplikovat také na vnořená přiřazení v DTO. Stávající služby
  mohou omezit rodičovské záznamy podle vybraného trenéra, ale v DTO ponechat
  i ostatní trenéry z téhož záznamu.
- Rozšířit pouze `TrainingPlanScheduleItemDto`/projekci o chybějící
  identifikační a zobrazovací údaje potřebné k párování a řádkům: minimálně
  `TrainingTypeId`, název kategorie, sezóny a role. Kategorie/role se zobrazí
  názvy z číselníků; párování vždy používá jejich stabilní ID/kód.
  Databázový model ani schéma se nemění.
- Agregovat nejprve celkové minuty a pak převést na hodiny. Před porovnáním
  zaokrouhlit plánované hodiny na dvě desetinná místa, aby porovnání
  odpovídalo přesnosti `decimal(5,2)` uvedené v issue; požadavkové hodiny už
  mají odpovídající přesnost. Testy ověří hranici shody po zaokrouhlení.
- Hodiny zobrazovat s českou desetinnou čárkou a přesností nejvýše dvě místa,
  v souladu s příklady issue (`6`, `6,50`, `7,25`). Status zobrazit ikonou
  i textovým popiskem; tooltip ponese text z issue. Barvy použijí existující
  sémantické `--color-*` tokeny pro neutrální/informační, varovný a chybový
  stav, s kontrastem pro světlý i tmavý režim.

## Technický návrh

### Datové načtení a DTO

Rozšířit projekci v `TrainingScheduleService.GetTrainingPlansAsync` o
identifikační a zobrazovací údaje potřebné pro přehled. Rozšíření
`TrainingPlanCoachDto` doplní název role; řádek plánu doplní
`TrainingTypeId`, `SeasonId`, `SeasonName` a název kategorie. Zachovat
stávající výpočty platnosti přiřazení trenérů a stávající UI plánu.

PageModely získají druhou sadu DTO určenou pro komponentu, aby se nemusely
měnit současné filtry a data hlavních přehledů:

- **Requirement:** načíst plány pro zvolenou sezónu, kategorie, typy, datum
  a trenéry. Fázi a lokalitu neomezovat; předat kompletní položky plánu.
  Znovu načíst požadavky bez fázového filtru, aby jejich hodiny odpovídaly
  souhrnu všech fází.
- **Plan:** načíst požadavky pro zvolenou sezónu, kategorie, typy, datum
  a trenéry, bez fázového filtru. Pro porovnávací plány načíst položky bez
  fáze; den a lokalitu do porovnání nezahrnovat.
- Pokud nejsou aktivní sezóna/kategorie nebo existují prázdné sady, předat
  komponentě validní prázdné seznamy; hlavní obsah stránky se tím nemění.

Při filtrování přiřazení komponenta použije ID trenéra ze seznamu vybraných
trenérů (prázdný výběr znamená všechny). Pro plánované hodiny přiřadí celý
`DurationMinutes` každému přiřazenému páru trenér–role, jak vyžaduje klíč
issue. Stejný plán s více trenéry proto přispívá každému trenérovi.

### Agregace, párování a render

V Razor vrstvě přidat prezentační model/agregátor, který:

1. Agreguje `CoachAssignments.Hours` podle `SeasonId`, kódu kategorie,
   `TrainingTypeId`, `CoachId` a `CoachRoleId`; vynechá požadavky bez
   odpovídajícího přiřazení trenér–role, protože z nich nelze vytvořit řádek
   podle požadovaného klíče.
2. Agreguje plánové `DurationMinutes` podle téhož klíče a identitního
   přiřazení z `CoachAssignments`. `null` délku nepřevádí potichu na nulu;
   ověří invariant zdrojových dat, případně vyvolá explicitní chybu podle
   zavedeného vzoru.
3. Provede full outer join agregací; chybějící stranu doplní nulou. Jména
   trenéra a popisky řádku vezme z dostupné strany a při shodě klíče použije
   hodnoty požadavku.
4. Určí status porovnáním požadovaných a zaokrouhlených plánovaných hodin.
5. Seřadí výsledky podle názvu sezóny, `SeasonCategoryOrder`, názvu typu,
   českého jména trenéra a pořadí role; stabilní ID doplní řazení při shodě.

ViewComponent v `src/SportSys.Razor/ViewComponents/` předá tento model
sdílenému `Default.cshtml` v
`src/SportSys.Razor/Pages/Shared/Components/TrainingRequirementCoverage/`.
Tabulka zobrazí všechna požadovaná záhlaví. Pro status vykreslí odpovídající
ikonu, viditelný text a `title`/přístupný popisek s vysvětlením. Prázdný
výsledek zobrazí srozumitelný stav místo prázdné tabulky.

Styly nové komponenty patří do samostatného SCSS partialu a jeho `@use` do
`Styles/site.scss`. Použij existující sémantické barevné tokeny; nikdy
neupravuj ručně `wwwroot/css/site.css`.

## Implementační kroky

### Fáze 1: Contract data pro porovnání

1. Rozšířit `TrainingPlanScheduleItemDto` a `TrainingPlanCoachDto` o
   identifikátory/popisky, které chybí pro shodu a vykreslení.
2. Doplnit projekci v `TrainingScheduleService.GetTrainingPlansAsync`.
   Zachovat současné filtrování, řazení, výpočty délky a přiřazování trenérů.
3. Ověřit, že změna DTO nezasáhne existující rozvrh, tabulku, tooltipy ani
   export, které DTO mohou používat.

### Fáze 2: DTO agregace a komponenta

1. Přidat typy prezentačního řádku/statusu a čistou agregaci/párování nad
   `TrainingRequirementListItem` a `TrainingPlanScheduleItemDto`.
2. Implementovat filtrování vybraných trenérů, agregaci požadavkových hodin,
   minut plánu, zaokrouhlení, full outer join a deterministické řazení.
3. Přidat bezdatabázovou `TrainingRequirementCoverageViewComponent` a její
   sdílené Razor view.
4. Přidat SCSS stavů a tabulky s ikonou, textem a nápovědou; zapojit partial
   do `Styles/site.scss`.

### Fáze 3: Načítání a zapojení obou stránek

1. Rozšířit `Requirement/IndexModel` o službu pro načtení plánů a samostatné
   porovnávací sady požadavků bez fázového omezení.
2. Rozšířit `Plan/IndexModel` o `TrainingRequirementService` a načtení
   porovnávacích požadavků/plánů bez fázového omezení; současné `PlanRows`
   a grafický plán ponechat beze změny.
3. Vložit stejnou ViewComponent pod obě hlavní stránky na požadovaná místa.
4. Zachovat chování stávajících filtrů a prázdných stavů jejich přehledů.

### Fáze 4: Testy a ověření

Pokrýt výpočty, párování, render komponenty a načítání DTO. Poté spustit
cílené testy projektu `SportSys.Razor.Tests`, build řešení a CSS build.

### Fáze 5: Varovný stav „Nad plán“ podle nového komentáře

1. V `TrainingRequirementCoverageRow.StatusText` a `StatusIconClass` změnit
   pouze větev `OverPlan` na „Nad plán“ a `fa-triangle-exclamation`.
2. V `_vars.scss` doplnit oranžový `--color-warning-icon` pro všechny režimy
   a v `_training-requirement-coverage.scss` jím nahradit zelenou barvu stavu.
3. V `TrainingRequirementCoverageTests` ověřit render všech tří stavů včetně
   tooltipu, přístupného popisku a nepřítomnosti původní fajfky/textu.
4. Aktualizovat dokumentaci modulu a implementační záznam; spustit cílené
   testy a kompilaci SCSS, ověřit kontrast oranžové ikony.

## Soubory ke změně

- `src/SportSys.Contract/Models/TrainingScheduleDto.cs`
- `src/SportSys.Contract/Services/TrainingScheduleService.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml`
- `src/SportSys.Razor/Models/` (nový model/agregátor porovnání)
- `src/SportSys.Razor/ViewComponents/TrainingRequirementCoverageViewComponent.cs` (nový)
- `src/SportSys.Razor/Pages/Shared/Components/TrainingRequirementCoverage/Default.cshtml` (nový)
- `src/SportSys.Razor/Styles/_training-requirement-coverage.scss` (nový)
- `src/SportSys.Razor/Styles/_vars.scss` (oranžový stavový token)
- `src/SportSys.Razor/Styles/site.scss`
- `tests/SportSys.Razor.Tests/` (cílené nové testy a případné úpravy existujících testů)

## Testy a ověření

- Agregace požadavků: součet hodin více požadavků stejného klíče, rozlišení
  sezóny/kategorie/typu/trenéra/role a nezapočtení `DurationHours` rodiče.
- Agregace plánů: součet minut pro každého trenéra a roli, převod na hodiny,
  rozlišení rolí a zaokrouhlení na dvě desetinná místa před porovnáním.
- Párování: úplná shoda, pouze požadavek, pouze plán, různé role téhož
  trenéra a plány bez přiřazení.
- Filtry trenérů: vybraný trenér se zobrazí bez ostatních přiřazení
  na stejném rodičovském záznamu; prázdný výběr zahrne všechny.
- Řazení: sezóna, `SeasonCategoryOrder`, typ, české jméno trenéra a pořadí
  role; výsledek je stabilní při změně vstupního pořadí.
- Statusy a popisky pro `<`, `=`, `>` včetně textu tooltipu; barva není jedinou
  informací. Ověřit HTML enkódování jmen/názvů.
- Integrační testy PageModelů: samostatné porovnávací sady respektují
  sezónu/kategorii/typ/datum/trenéry a ignorují fázi, den a lokalitu; hlavní
  `Requirements`, `PlanRows` a `ScheduleView` zůstávají filtrovány původně.
- Render sdílené komponenty z obou stránek se stejnými DTO a prázdným vstupem.
- CSS build: `npm run build:css` z `src\SportSys.Razor`.
- Build a testy:

  ```powershell
  dotnet build SportSys.slnx
  dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
  ```

## Manuální akceptace

1. Otevřít `/sport/Training/Requirement` a `/sport/Training/Plan` pro stejnou
   sezónu, kategorii, typ, datum a trenéry; porovnávací tabulka zobrazuje
   shodné řádky a hodnoty na obou stránkách.
2. Zkontrolovat trenéra s více rolemi: každá role má samostatný řádek a
   plánované hodiny se započítají k odpovídající roli.
3. Ověřit shodu, přeplánování a nedostatek hodin; každá varianta má správnou
   ikonu, text i tooltip, a je pochopitelná bez barev.
   Přeplánování se zobrazuje jako „Nad plán“ s oranžovým vykřičníkem,
   nikoli zelenou fajfkou; platí to i pro plán bez požadavku.
4. Ověřit požadavek bez plánu a plán bez požadavku; chybějící strana se
   zobrazuje jako `0`.
5. Změnit filtr fáze, dne a lokality; hlavní přehled se změní jako dosud,
   porovnání dál zahrnuje všechny fáze/dny/lokality pro zvolené dimenze.
6. Ověřit české formátování hodin a čitelnost statusů ve světlém i tmavém
   schématu.

## Beze změny

- Databázové entity, schéma, snapshot a EF Core migrace; agent žádnou migraci
  nevytváří ani neaplikuje.
- Výpočet `TrainingPlan.DurationMinutes` v C#; zdroj zůstává computed sloupec.
- Současné filtry a hlavní tabulky/rozvrh na obou stránkách.
- Původní plánové a požadavkové editace, importy, rozvrh skutečných tréninků
  i `SportSys.Web`.
- Generovaný `src/SportSys.Razor/wwwroot/css/site.css` (mění se pouze SCSS).

## Mimo rozsah

- Porovnávání zápasových požadavků nebo zápasů.
- Porovnávání po fázi, dni nebo lokalitě; tyto dimenze nejsou součástí klíče
  uvedeného v issue.
- Změna pravidel přiřazování trenérů, jejich rolí nebo platnosti plánů.
- Nové databázové tabulky, sloupce, migrace nebo závislosti.

## Hotovo, když

- [x] Stejná databázově nezávislá komponenta je na obou stránkách a přijímá DTO.
- [x] Požadované a plánované hodiny jsou agregované a párované dle klíče issue.
- [x] Všechny sloupce, výchozí řazení a tři statusy odpovídají zadání.
- [x] Chybějící strana agregace se zobrazuje jako nula a má správný status.
- [x] Sady pro porovnání respektují společné relevantní filtry a zahrnují
  všechny fáze/dny/lokality.
- [x] Procházejí cílené testy, build řešení a CSS build.
- [x] Nebyla změněna databáze ani vytvořena/aplikována EF Core migrace.
- [x] Doplnění podle komentáře: stav „Nad plán“ má oranžový vykřičník,
  původní zelená fajfka a text „Splňuje plán“ se již nevykreslují.
