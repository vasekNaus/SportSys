# Implementační plán: #38 Úprava přehledu požadavků na tréninky

**Issue:** [#38 — Úprava přehledu požadavků na tréninky](https://github.com/vasekNaus/SportSys/issues/38)

**Stav:** Implementováno včetně zarovnání podle komentáře z 21. 10. 2026.
Testy formátu `0.##` a zarovnání prošly. Manuální akceptace
proti vývojové databázi neprovedena. Podrobnosti jsou v
`38-uprava-prehledu-pozadavku-na-treninky-IMPL.md`.

## Cíl

Zpřehlednit první tabulku na stránce `/sport/Training/Requirement`:
zkrátit záhlaví, zobrazit název kategorie místo kódu a u každého přiřazení
trenéra uvést jeho plánované hodiny. Zachovat filtrování, řazení a přehled
požadavků na zápasy.

## Výchozí stav

Ověřeno proti aktuálnímu kódu a issue včetně komentářů dne 21. 10. 2026.
Původní návrh vznikl bez doplňujících komentářů. Nový komentář
z 21. 10. 2026 doplňuje zarovnání první tabulky:
[požadavek na zarovnání](https://github.com/vasekNaus/SportSys/issues/38#issuecomment-6309561032).

- `Index.cshtml` v `src\SportSys.Razor\Areas\sport\Pages\Training\Requirement\`
  vykresluje dvě tabulky: tréninkové a zápasové požadavky.
  První má již devět sloupců ve správném pořadí, ale používá dlouhá záhlaví
  a `requirement.SeasonCategoryCode`.
- `TrainingRequirementListItem` v
  `src\SportSys.Contract\Models\TrainingRequirementDto.cs`
  neobsahuje název kategorie. Entita `SeasonCategory` již má vlastnost `Name`.
- `TrainingRequirementService.GetAllAsync` projektuje přiřazení trenérů
  včetně `Hours = assignment.Hours` a údajů o roli včetně jejího `Order`.
  Hodiny tedy není potřeba nově načítat ani dopočítávat.
- `CoachTrainingRequirement.Hours` již má konfiguraci `decimal(5,2)`.
  Každá dvojice trenér–role v rámci požadavku má vlastní počet hodin.
- Obě tabulky používají stejný partial `_CoachGroups.cshtml` a helper
  `RequirementCoachGroups.Group`. Helper seskupuje podle role, řadí role
  podle `CoachRoleOrder` a následně ID; uvnitř role řadí příjmení českým
  porovnávačem bez rozlišení velikosti písmen.
- `RequirementCoachGroup.LastNames` nyní obsahuje pouze příjmení.
  Helper nad nimi používá `Distinct()`, čímž může sloučit dva různé trenéry
  se stejným příjmením. Pro tréninkovou variantu je nutné zachovat jednotlivá
  přiřazení i jejich hodiny, nikoli odstraňovat duplicity podle textu.
- `RequirementCoachListItem.LastName` již řeší náhradní zobrazení osobního
  čísla při prázdném jméně. `DisplayText` je jiný formát obsahující roli;
  není vhodné měnit jeho význam.
- Existují testy seskupování v `TrainingRequirementTests.cs` a český
  formátovací helper `CzechNumberFormat.FormatDecimal`.

Podkladem návrhu jsou také `docs\architecture.md`,
`docs\decisions\adr-001-vrstvy-a-composition-root.md`,
`docs\modules\sport.md` a `docs\modules\frontend.md`.

## Potvrzené požadavky a rozhodnutí

Požadavky z issue s následným upřesněním uživatele ze dne 21. 10. 2026.
Formát `0.##` má přednost před původním požadavkem issue na pevná
desetinná místa:

1. Záhlaví tréninkové tabulky bude přesně:
   `Sezóna | Kategorie | Typ tréninku | Fáze | Od | Do | Rozsah [h] | Trenéři | Akce`.
2. Kategorie bude zobrazovat `SeasonCategory.Name`, nikoli `Code`.
3. Trenér bude zobrazen jako `Příjmení (hodiny)`, například
   `Krs (2)` nebo `Nauš (1,5)`.
4. Hodiny pocházejí z konkrétního `CoachTrainingRequirement.Hours`.
   Zobrazují se formátem `0.##`, tedy nejvýše dvě desetinná místa s českou
   desetinnou čárkou a bez koncových nul. Nula a celé hodiny nemají
   desetinnou část.
5. Zůstane seskupení podle rolí, jejich pořadí podle `CoachRole.Order`
   a stávající řazení trenérů uvnitř role.
6. Požadavek bez trenérů bude nadále zobrazovat `-`.
7. Všech devět záhlaví tréninkové tabulky bude zarovnáno na střed.
   Hodnoty sezóny, fáze, `Od`, `Do` a akcí budou na střed, `Rozsah [h]`
   doprava; kategorie, typ tréninku a trenéři zůstanou vlevo.

Technická rozhodnutí návrhu:

- Změna platí pouze pro tabulku tréninkových požadavků. Zápasová tabulka
  nemá dostat hodiny, nový text záhlaví ani název kategorie.
- Název kategorie doplnit do výstupního DTO; kód ponechat pro identifikaci,
  filtrování a stávající řazení. Neměnit popisky kategorií ve filtrech.
- Formátovat až v prezentační vrstvě existujícím českým helperem.
- Neagregovat hodiny, nevypočítávat je z rozsahu požadavku a nepřidávat
  kontrolu jejich součtu. Issue mění pouze zobrazení.
- **Databázový model se nemění.** Nevytvářet ani neupravovat migrace,
  model snapshot nebo data. Nejsou potřeba nové závislosti ani ADR.

Nebyly zjištěny otevřené otázky, které by blokovaly přípravu implementace.

## Technický návrh

### Contract: přenos názvu kategorie

V `TrainingRequirementListItem` přidat řetězcovou vlastnost
`SeasonCategoryName` se stejnou inicializací jako ostatní názvy.
V projekci `TrainingRequirementService.GetAllAsync` ji naplnit přímo
z `requirement.SeasonCategory.Name`.

Zachovat dosavadní filtry, pořadí řádků, `AsNoTracking()`, předání
`CancellationToken` i projekci všech přiřazení. Nepřidávat samostatné dotazy,
`Include` ani přístup do Database z Razor projektu.

Při prázdném názvu kategorie zobrazit skutečnou prázdnou hodnotu;
nezavádět nepožadovaný fallback na kód ani opravování dat při čtení.

### Razor: sdílené skupiny trenérů

Rozšířit existující helper, nikoli vytvářet druhou kopii seskupování:

- Přidat do `RequirementCoachGroups.Group` volitelný přepínač
  `includeHours = false`.
- Výstupní kolekci `RequirementCoachGroup.LastNames` přejmenovat na
  `CoachDisplayTexts`, protože v tréninkové variantě již nebude obsahovat
  pouze příjmení. Aktualizovat partial a stávající testy.
- V obou režimech zachovat seskupení a pořadí rolí.
- V režimu s hodinami nejprve řadit přiřazení podle `LastName`
  stávajícím českým porovnávačem a až potom vytvářet text
  `LastName (CzechNumberFormat.FormatDecimal(Hours, "0.##"))`.
  Neřadit podle výsledného textu obsahujícího číslo.
- V tomto režimu nepoužívat `Distinct()` nad příjmením ani výsledným
  popiskem: dva různí trenéři se shodným příjmením a třeba i stejnými
  hodinami musí zůstat dvěma položkami. Stejný trenér v různých rolích
  musí být zobrazen v každé příslušné skupině se správnými hodinami.
- Výchozí režim bez hodin zachová dosavadní výstup včetně deduplikace
  příjmení pro zápasy; tento task nemění jejich chování.

V `_CoachGroups.cshtml` načíst přepínač
`ViewData["IncludeHours"] is true` a předat ho helperu.
Pouze u tréninkového volání partialu v `Index.cshtml` předat lokální kopii
`ViewDataDictionary` s `IncludeHours = true` přes atribut `view-data`.
Neměnit sdílené `ViewData` stránky, aby se nastavení nepřeneslo
do následující zápasové tabulky. Zápasové volání ponechat bez přepínače.

Zachovat stávající HTML tříd `coach-groups`, `coach-group`,
`coach-group-role` a `coach-group-names`, oddělení jmen čárkou a přirozené
zalamování. Příklady v issue nevyžadují vynucený řádek pro každého trenéra.
Výstup ponechat standardně HTML kódovaný; nepoužívat `Html.Raw`.
Pro prázdné skupiny zachovat `-`.

### Razor: tabulka a chybové stavy

V první tabulce `Index.cshtml` změnit tři záhlaví a kategorii vykreslovat
z `SeasonCategoryName`. Pořadí a počet sloupců ponechat.

Neměnit formát `DurationHours` (`0.##`). Hodiny přiřazených trenérů používají
po upřesnění uživatele stejný formát bez koncových nul.
Zachovat datum `dd.MM.yyyy`, editovací odkazy i prázdné stavy obou tabulek.

Jde o read-only změnu bez nového vstupu, handleru nebo validace.
Zachovat existující validaci GET filtrů a průchod databázových chyb;
nepřidávat catch ani náhradní úspěšný výsledek při selhání načtení.

### Doplnění podle komentáře: zarovnání

V první tabulce použít existující utility z `Styles\_utilities.scss`:
`class="c"` pro všechna `th` a buňky sezóny, fáze a dat;
`class="r"` pro rozsah hodin. Akční buňce ponechat `buttons` a přidat `c`,
která díky existujícímu `!important` překryje obecné pravé zarovnání akcí.
Ostatní buňky nepřepisovat; obecné `table.grid` již používá levé zarovnání.
Zápasová tabulka ani globální pravidla gridu se nemění.
Nové SCSS, ruční úpravy CSS, datové změny ani závislosti nejsou potřeba.

## Implementační kroky

### Fáze 1: Doplnění datového kontraktu

1. Přidat `TrainingRequirementListItem.SeasonCategoryName`.
2. Doplnit projekci v `TrainingRequirementService.GetAllAsync`.
3. Ponechat databázové entity, přiřazené hodiny i filtrování beze změny.

### Fáze 2: Zobrazení a ochrana sdíleného chování

1. Rozšířit `RequirementCoachGroups.Group` o režim s hodinami,
   aktualizovat název výstupní kolekce a zachovat výchozí zápasový režim.
2. Zapojit přepínač v partialu a lokálně jej nastavit pouze pro tréninky.
3. Upravit první tabulku: záhlaví a zobrazení názvu kategorie z fáze 1.

### Fáze 3: Regrese a dokumentace

1. Rozšířit testy podle následující sekce.
2. Aktualizovat popis tréninkové tabulky v části
   „Požadavky na tréninky a zápasy“ v `docs\modules\sport.md`:
   název kategorie, zkrácená záhlaví a plánované hodiny u trenérů.
3. Provést cílené testy, build a manuální akceptaci.

### Fáze 4: Doplnění zarovnání z komentáře

1. Doplnit utility `c` a `r` pouze první tabulce podle matice výše.
2. Ověřit třídy na skutečně vykresleném Razor výstupu obou tabulek.
3. Aktualizovat očekávání testů již schváleného formátu `0.##`, včetně
   vykreslení partialu, a znovu spustit cílené testy.
4. Doplnit dokumentaci modulu a existující implementační záznam.

## Soubory ke změně

| Soubor | Změna |
|---|---|
| `src\SportSys.Contract\Models\TrainingRequirementDto.cs` | Vlastnost `TrainingRequirementListItem.SeasonCategoryName`. |
| `src\SportSys.Contract\Services\TrainingRequirementService.cs` | Projekce názvu kategorie v `GetAllAsync`. |
| `src\SportSys.Razor\Models\RequirementCoachGroups.cs` | Přepínač hodin, správné řazení a výstupní popisky bez ztráty tréninkových přiřazení. |
| `src\SportSys.Razor\Areas\sport\Pages\Training\Requirement\_CoachGroups.cshtml` | Předání přepínače helperu a vykreslení popisků. |
| `src\SportSys.Razor\Areas\sport\Pages\Training\Requirement\Index.cshtml` | Záhlaví, název kategorie a zapnutí hodin pouze pro tréninky. |
| `tests\SportSys.Razor.Tests\TrainingRequirementTests.cs` | Pokrytí obou režimů seskupování a regresí. |
| `docs\modules\sport.md` | Aktualizace popisu tréninkového přehledu. |

`Index.cshtml.cs`, `CzechNumberFormat.cs`, `MatchRequirementService`,
zápasové DTO a databázové entity není potřeba měnit.

## Testy a ověření

V existujícím `TrainingRequirementTests.cs`:

1. Aktualizovat aserce po přejmenování `LastNames`; dosavadní testy
   výchozího režimu musí dál ověřovat stejná příjmení a pořadí.
2. Ověřit tréninkové popisky pro hodnoty `0`, `0.5`, `1`, `1.25`, `1.5`,
   `2` a `999.99`: očekávat `0`, `0,5`, `1`, `1,25`, `1,5`, `2`
   a `999,99`, například `Novák (0)` a `Novák (1,5)`.
3. Ověřit český formát nezávisle na aktuální kultuře vlákna, například
   při `en-US`; případnou změnu kultury v testu vždy obnovit.
4. Ověřit řazení podle příjmení a `CoachRoleOrder`, včetně role s vyšším
   ID, ale nižším `Order`; různé hodiny nesmějí měnit pořadí.
5. Ověřit dva trenéry stejného příjmení s odlišnými i shodnými hodinami:
   v režimu s hodinami musí zůstat oba.
6. Ověřit stejného trenéra ve dvou rolích s různými hodinami.
7. Ověřit fallback na osobní číslo při prázdném jméně a prázdnou kolekci.
8. Ověřit, že výchozí režim pro zápasy nadále zobrazuje jen příjmení
   bez závorek a hodin a zachovává dosavadní deduplikaci.
9. Na vykreslené stránce ověřit všech devět záhlaví `c` a buňky podle
   matice `c, left, left, c, c, c, r, left, buttons c`.
   Druhá tabulka musí zachovat původní třídy bez doplněného zarovnání.

Použít stávající xUnit projekt bez přidávání testovacího provideru
nebo jiné závislosti. Formátovací helper je již samostatně testovaný;
jeho implementaci není potřeba měnit.

Při implementaci spustit:

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --filter "FullyQualifiedName~TrainingRequirementTests|FullyQualifiedName~CzechNumberFormatTests|FullyQualifiedName~CoachAssignmentQueryTests"
dotnet build SportSys.slnx
```

Projekci skutečného `SeasonCategory.Name` a výsledné HTML ověřit
manuálně proti existující vývojové databázi podle následujících scénářů.
Samotný test inicializace DTO by správnost databázové projekce neprokázal.

## Manuální akceptace

1. Otevřít `/sport/Training/Requirement` se zobrazenými oběma tabulkami.
   První má přesně požadovaných devět záhlaví; druhá se nezměnila.
2. Vybrat kategorii, jejíž název se liší od kódu. V tréninkové tabulce
   je název, zatímco filtr kategorií a zápasová tabulka nadále používají kód.
3. U požadavku s více rolemi a trenéry porovnat každý zobrazený počet hodin
   s hodnotou konkrétního přiřazení. Hodnoty používají formát `0.##` bez
   koncových nul, například `0`, `0,5`, `1,25` a `2`; desetinná část je
   oddělena čárkou, pouze pokud je potřeba.
4. Ověřit pořadí rolí podle `CoachRole.Order` a české řazení příjmení.
   U stejného trenéra ve více rolích zůstávají hodiny oddělené.
5. Ověřit požadavek bez trenérů (`-`), trenéra bez zobrazovaného jména
   a dvě různá přiřazení se shodným příjmením.
6. Ověřit společné filtry sezóny, kategorií, trenérů a data,
   tréninkové filtry typu a fáze i přepínače zobrazení tabulek.
   Filtrování trenérem nemá skrýt ostatní přiřazení vyhovujícího požadavku.
7. Ověřit prázdný výsledek a editovací odkazy. Na užší obrazovce
   zkontrolovat zalamování jmen bez úpravy stylů nebo struktury tabulky.
8. Ověřit všechna záhlaví na střed, sezónu, fázi, data a akce na střed,
   rozsah doprava a kategorii, typ tréninku a trenéry vlevo.
   Porovnat se zápasovou tabulkou, jejíž zarovnání se nezměnilo.

Pro scénáře použít existující nebo standardně připravená testovací data;
implementace nesmí automaticky upravovat produkční záznamy.

## Beze změny

- Route, PageModel, GET parametry, logika filtrů a pořadí požadavků.
- Zápasová tabulka, její kategorie, záhlaví a zobrazení trenérů bez hodin.
- Editace požadavků, vstupní validace a pravidla přiřazování trenérů.
- `RequirementCoachListItem.DisplayText`, výpočet `LastName`
  a náhradní identifikace osobním číslem.
- Význam a formát celkového `DurationHours`, formát dat, akce a prázdné stavy.
- Databázový model, DI, autorizace, SCSS a generované CSS.

## Mimo rozsah

- Přejmenování kategorií v ostatních přehledech nebo ve filtrech.
- Součty, přepočty a nová omezení plánovaných hodin.
- Nové formuláře, exporty, knihovny nebo redesign stránky.
- Úpravy dat, databázových migrací a `SportSys.Web`.

## Hotovo, když

- [x] Tréninková tabulka má přesná záhlaví a zobrazuje `SeasonCategory.Name`.
- [x] Každé tréninkové přiřazení ukazuje vlastní `Hours` formátem `0.##`, například `Příjmení (1,5)`.
- [x] Role i trenéři mají zachované požadované řazení.
- [x] Různá tréninková přiřazení nezanikají kvůli stejnému příjmení.
- [x] Prázdná přiřazení zobrazují `-` a zápasový přehled zůstává beze změny.
- [x] Logika filtrů, akcí a prázdných stavů je ponechána beze změny.
- [x] Původní cílené testy a build prošly před změnou formátu hodin.
- [x] Očekávání testů odpovídají formátu `0.##` a cílené testy po této změně projdou.
- [x] Tréninková tabulka má zarovnání podle nového komentáře; zápasová se nezměnila.
- [ ] Manuální scénáře proti vývojové databázi jsou ověřeny.
- [x] Dokumentace modulu odpovídá výslednému zobrazení.
- [x] Nezměnil se databázový model ani nevznikla migrace.

Teprve po dokončené implementaci přesunout tento plán do `tasks\completed\`
a přidat vedle něj soubor
`38-uprava-prehledu-pozadavku-na-treninky-IMPL.md`
s informacemi o skutečně provedené implementaci podle
`.github\instructions\tasks.instructions.md`.
