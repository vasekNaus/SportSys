# Implementační plán: #32 Editace tréninků – evidence trenérů a docházky

**Issue:** [#32 — Editace tréninků – evidence trenérů a docházky](https://github.com/vasekNaus/SportSys/issues/32)

**Stav:** Implementováno; ověření viz doprovodný soubor `32-editace-treninku-evidence-treneru-a-dochazky-IMPL.md`.

## Cíl

Rozšířit existující editaci reálného tréninku o tabulku přiřazených trenérů,
která umožní přidávat trenéry a měnit jejich docházku a poznámku. Přiřazení
trenéra k tréninku nelze z formuláře odstranit. U spojeného tréninku se
přiřazení váže na konkrétní kategorii (člena skupiny).

## Výchozí stav

- Editační stránka existuje v
  `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Edit.cshtml`
  a `Edit.cshtml.cs`. Používá `TrainingService`, který již načítá všechny
  členy spojeného tréninku a ukládá společná pole transakčně se sériovou
  izolací a kontrolou souběžné změny.
- `TrainingEditDto` a `TrainingEditContextDto` jsou v
  `src/SportSys.Contract/Models/TrainingEditDto.cs`. `TrainingService` je
  registrován v `AddSportSysServices()`.
- Editace trenérů v plánu je ve
  `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml`;
  `TrainingPlanService` již poskytuje dostupné trenéry, načítá přiřazení po
  členech skupiny a řeší atomické uložení.
- Přiřazení trenéra k reálnému tréninku je entita
  `SportSys.Database.Models.sport.CoachTraining` v tabulce `sport.CoachTraining`.
  Nese `CoachId`, `TrainingId`, `ParticipationTypeId` a `Note` s limitem
  50 znaků. Kategorie je určena samotným tréninkem.
- Hodnoty docházky již existují v `EParticipationType` a jejich české popisky
  v `src/SportSys.Database/Resources/EParticipationType.cs.resx`.
- `CoachTraining` má složený klíč `CoachId + TrainingId + ParticipationTypeId`.
  Databázový klíč tedy sám nevynucuje jediný stav na trenéra a logický trénink;
  aplikační služba musí ověřit unikátnost trenéra i platnost stavu.
- `CoachAttendance` v HR eviduje nahrané soubory XLSX; není to model
  jednotlivých přiřazení ani docházky na trénink.
- Související existující plány jsou `tasks/6-editace-treninku.md`,
  `tasks/8-evidence-dochazky-treneru.md` a
  `tasks/18-administrace-treneru-u-treninkoveho-planu.md`. Aktuální implementace
  a issue #32 jsou pro tento plán rozhodující.

## Potvrzené požadavky a rozhodnutí

- Editace tréninku zobrazí tabulku se sloupci Trenér, Kategorie, Docházka
  a Poznámka.
- U existujícího přiřazení je trenér i kategorie pouze informativní;
  kategorii nelze změnit. Tím se řídí potvrzená odpověď uživatele na rozpor
  mezi popisem sloupce v issue a jeho akceptačním kritériem.
- Docházka je editovatelný combobox s hodnotami `Attended`, `NoResponse`,
  `Absent`, `JointTraining` a `AbsentOtherTraining`, zobrazovanými přes
  existující české lokalizace.
- Poznámka je editovatelná a má nejvýše 50 znaků.
- Lze přidat nové přiřazení s výběrem trenéra a kategorie; jeho výchozí stav
  je `Absent` („Neúčast“).
- Kategorie nového přiřazení se vybírá pouze z kategorií tréninku nebo členů
  jeho spojené skupiny.
- Jeden trenér smí být přiřazen nejvýše jednou v rámci jednoho logického
  tréninku, včetně jeho spojených kategorií.
- Nové přiřazení vyžaduje trenéra, kategorii a platný stav docházky.
- Existující přiřazení nelze odstranit. Při uložení se proto zachová každé
  načtené přiřazení; odeslaná data nesmí umožnit jeho tiché smazání.
- „Zachování historie“ zde znamená zachovat samotné přiřazení trenéra; historie
  předchozích hodnot docházky/audit jednotlivých změn se nepožaduje. Změna
  stavu uchovává aktuální hodnotu podle existujícího modelu.
- Tabulka a ovládací prvky mají navázat na vizuální vzor administračního
  seznamu trenérů v editaci tréninkového plánu.

## Technický návrh

### Contract a data

Rozšířit `TrainingEditDto` o kolekci vstupních řádků pro trenéry. Řádek ponese
identifikátory vybraného trenéra a konkrétního tréninku, `ParticipationTypeId`
a poznámku. Identifikátor tréninku bude u existujících řádků pouze skrytým
technickým údajem; server jej ověří proti aktuálním členům tréninku. Pro nová
přiřazení zůstane výběr trenéra a kategorie prázdný a stav se předvyplní na
`EParticipationType.Absent`.

Rozšířit `TrainingEditContextDto` / `TrainingEditMemberDto` nebo doplnit
samostatné DTO tak, aby kontext obsahoval aktuální přiřazení se jménem trenéra,
kódem kategorie a popiskem docházky, seznam trenérů pro výběr a seznam
dostupných kategorií. Data načítat projekcí v Contract vrstvě; Razor nesmí
pracovat s EF entitami.

### Načtení a uložení

Rozšířit `TrainingService.GetEditAsync` a `UpdateAsync`:

1. Načíst přiřazení přes `CoachTraining` pro jeden trénink nebo všechny členy
   jeho `TrainingGroup`; zobrazit je v přehledu podle pořadí kategorie,
   následně trenéra.
2. Zahrnout ID členů skupiny a úplný stav přiřazení (trenér, trénink, typ
   účasti a poznámka) do `OriginalVersion`, aby změna přiřazení mezi GET a POST
   vedla ke konfliktu stejně jako změna ostatních polí.
3. Při POST v transakci znovu načíst aktuální členství i přiřazení a ověřit
   snapshot, že každý existující záznam je stále obsažen a že příchozí ID
   tréninků patří do aktuálního cíle/skupiny.
4. Ověřit dostupnost trenéra, povolené ID `EParticipationType`, povinný výběr
   trenéra i kategorie a délku poznámky. Duplicitní trenér se odmítne napříč
   všemi členy skupiny; chyba bude pojmenovávat trenéra podle issue.
5. Nová přiřazení přidávat do `CoachTrainings`. U existujícího přiřazení měnit
   stav a poznámku bez možnosti odstranit samotné přiřazení. Při změně stavu
   respektovat složený klíč a transakční zápis; předchozí stav se nearchivuje,
   protože auditní historie není požadována.
6. Uložit celý formulář atomicky a vracet samostatné výsledky pro duplicitu,
   neplatný vstup a souběžný konflikt. Razor doplní konkrétní validační chybu
   a znovu načte výběrové seznamy při redisplayi.

Unikátnost přes různé členy spojené skupiny závisí na členství ve skupině
a nelze ji vyjádřit prostým unikátním indexem nad `CoachTraining`. Zajistí ji
validace v Contract službě v transakčním toku; souběžné požadavky nesmí obejít
ověření. Existující `CoachAttendanceService` při importu předpokládá nejvýše
jeden stav na trenéra a trénink a při změně stavu nahrazuje řádek; editace má
zachovat stejný význam jednoho aktuálního stavu.

### Razor UI

V `Schedule/Edit.cshtml` přidat tabulku trenérů do existujícího formuláře.
Řádky existujících přiřazení budou mít informativní jméno a kategorii,
combobox docházky a textové pole poznámky; nebude v nich akce odstranění.
Tlačítko „Přidat trenéra“ vloží nový řádek s výběrem trenéra, kategorií
a výchozím stavem „Neúčast“. Pro přidávání řádků použít klientský template
a správné indexy model bindingu; neplatné hodnoty a duplicitní výběry zároveň
ověří server.

V `Schedule/Edit.cshtml.cs` načítat výběrová data při GET i při návratu po
neplatném POST a přeložit nové výsledky `TrainingUpdateResult` do českých
validačních chyb. Ostatní editaci tréninku, včetně skupinového ukládání a
Post/Redirect/Get, zachovat.

## Implementační kroky

### Fáze 1: Contract modely a validace

1. Rozšířit `TrainingEditDto`, `TrainingEditContextDto` a DTO řádku o vstup
   přiřazení a datové položky pro UI.
2. Přidat validaci povinných polí, délky poznámky a povolených hodnot
   docházky; zavést explicitní výsledky pro duplicitní přiřazení a konflikt.
3. Použít existující enum a RESX pro stav docházky; nepřidávat nový číselník
   ani lokalizační sadu.

### Fáze 2: Contract služba

1. Doplnit načítání trenérů a přiřazení k tréninku/skupině do
   `TrainingService.GetEditAsync`.
2. Rozšířit verzi snapshotu o ID a hodnoty všech přiřazení.
3. Implementovat transakční synchronizaci přiřazení bez mazání existujících
   trenérů, včetně ochrany před duplikátem napříč spojenými kategoriemi.
4. Zachovat existující zpracování lokality, konzistence skupiny a souběžných
   změn.

### Fáze 3: Razor formulář

1. Rozšířit PageModel o select-list trenérů a kategorií a doplnit jejich
   opětovné načtení po validační chybě.
2. Vykreslit tabulku se čtyřmi sloupci a existující řádky bez možnosti
   odstranění.
3. Přidat klientské vložení nového prázdného řádku s výchozí docházkou
   „Neúčast“ a aktualizací indexů polí pro model binding.
4. Zobrazit validační chyby pro chybějící výběr, duplicitu, neplatný stav
   a konflikt.

### Fáze 4: Testy a dokumentace

1. Doplnit testy DTO, normalizace a validace požadavku služby.
2. Aktualizovat `docs/modules/sport.md` o správu trenérů a docházky přímo
   v editaci reálného tréninku.
3. Ověřit build solution a testovací projekt podle příkazů projektu.

## Soubory ke změně

| Akce | Soubor |
|---|---|
| upravit | `src/SportSys.Contract/Models/TrainingEditDto.cs` |
| upravit | `src/SportSys.Contract/Services/TrainingService.cs` |
| upravit | `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Edit.cshtml.cs` |
| upravit | `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Edit.cshtml` |
| upravit | `tests/SportSys.Razor.Tests/TrainingEditDtoTests.cs` |
| vytvořit | `tests/SportSys.Razor.Tests/TrainingServiceTests.cs` nebo rozšířit vhodný existující testovací soubor |
| upravit | `docs/modules/sport.md` |
| případně upravit | existující SCSS pro admin tabulky, pouze pokud současné třídy nestačí; `wwwroot/css/site.css` neupravovat ručně |

## Testy a ověření

- DTO odmítne chybějícího trenéra nebo kategorii, poznámku delší než 50 znaků
  a nedeklarovanou hodnotu docházky.
- Výchozí stav nového řádku je `Absent`; existující lokalizované názvy všech
  pěti stavů se zobrazí v comboboxu.
- Načtení samostatného i spojeného tréninku vrátí přiřazení se správným jménem
  trenéra a kategorií.
- Nové platné přiřazení se uloží; duplicitní trenér v jedné kategorii i v jiné
  kategorii spojené skupiny se odmítne.
- Nelze uložit záznam bez trenéra nebo kategorie; neexistující trenér,
  cizí/neexistující ID tréninku a neplatný stav se odmítnou.
- Změna docházky a poznámky aktualizuje stav existujícího přiřazení, zatímco
  chybějící existující řádek v POST nesmaže přiřazení.
- Při souběžné změně přiřazení nebo členství skupiny se vrátí konflikt a
  nedojde k částečnému zápisu.
- Razor při validační chybě znovu vykreslí tabulku se seznamy trenérů
  a kategorií a zobrazí českou chybovou zprávu.

Ověřit:

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
dotnet build SportSys.slnx
```

## Manuální akceptace

1. Otevřít editaci samostatného tréninku a ověřit tabulku Trenér, Kategorie,
   Docházka, Poznámka.
2. Změnit stav a poznámku existujícího trenéra, uložit a ověřit hodnoty po
   opětovném načtení.
3. Přidat trenéra, vybrat jej a ověřit předvybranou „Neúčast“ a možnost zvolit
   kategorii pouze z kategorií daného tréninku.
4. U spojeného tréninku ověřit, že nový řádek může být přiřazen ke konkrétní
   kategorii a stejný trenér nelze přidat podruhé v jiné kategorii.
5. Ověřit chybu při duplicitě, chybějícím trenérovi či kategorii a příliš
   dlouhé poznámce; stránka musí zachovat výběrové seznamy.
6. Ověřit, že u existujícího přiřazení není možné změnit trenéra/kategorii ani
   jej odstranit a že editace ostatních polí tréninku zůstala funkční.

## Beze změny

- Existující model `CoachTraining`, `ParticipationType` a hodnoty
  `EParticipationType`.
- Model pro HR uploady docházky `CoachAttendance`.
- Obecná editace data, času, lokality a poznámky tréninku a její skupinová
  transakce.
- Přiřazování trenérů u tréninkových plánů mimo potřebnou vizuální inspiraci.
- Vrstvení `Razor → Contract → Database`; Razor nebude odkazovat přímo
  na `SportSys.Database`.

## Mimo rozsah

- Odebrání přiřazení trenéra, změna trenéra nebo přesun existujícího přiřazení
  do jiné kategorie.
- Historie změn stavů/auditní záznamy jednotlivých změn docházky.
- Úprava importu XLSX, HR reportů nebo výpočtu odměn.
- Nový lookup číselník, nová tabulka, změna EF Core modelu nebo databázového
  schématu.
- Vytváření ani aplikace EF Core migrace.
- Změny plánování tréninků, rozvrhu nebo jiných administračních stránek.

## Hotovo, když

- [x] Editace tréninku zobrazuje tabulku trenérů se čtyřmi požadovanými sloupci.
- [x] Docházku lze změnit comboboxem a poznámku upravit do 50 znaků.
- [x] Lze přidat trenéra s kategorií z aktuálního tréninku/skupiny a výchozím
      stavem „Neúčast“.
- [x] Existující přiřazení nelze změnit na jiného trenéra či kategorii ani
      odstranit.
- [x] Server odmítne neúplná a duplicitní přiřazení včetně duplicit napříč
      spojenými kategoriemi.
- [x] Změny tréninku i přiřazení se ukládají atomicky a konflikty souběhu
      nezpůsobí částečný zápis.
- [x] Automatické testy a build relevantních projektů projdou a dokumentace modulu Sport odpovídá
      implementovanému chování.
- [x] Nebyla vytvořena ani upravena EF Core migrace.
