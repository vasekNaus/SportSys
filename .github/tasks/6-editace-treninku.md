# Implementační plán: #6 Editace tréninku

**Issue:** [#6 — Editace tréninku](https://github.com/vasekNaus/HoSys/issues/6)

**Stav:** Implementováno a ověřeno sestavením a automatickými testy.

## Cíl

Doplnit stránku `/sport/Training/Edit`, která umožní upravit datum, začátek,
konec, lokalitu a poznámku existujícího tréninku. Editace bude dostupná
kliknutím na blok v reálném rozvrhu `/sport/Training/Schedule` a bude se
otevírat v novém okně nebo panelu prohlížeče.

Samostatný trénink se uloží přímo. U propojených tréninků se změny provedou
atomicky nad všemi členy skupiny, ale pouze pokud mají všichni členové před
editací shodné hodnoty všech editovatelných vlastností. Pokud se hodnoty liší,
stránka zobrazí tabulku všech členů a editaci ani uložení nepovolí.

## Výchozí stav

- Reálný rozvrh existuje na `/sport/Training/Schedule`; jeho data načítá
  `TrainingScheduleService.GetTrainingsAsync`.
- Bloky vytváří `TrainingScheduleBlockFactory`. Položky se stejným nenulovým
  `GroupId` jsou v rámci jednoho dne spojeny do jednoho bloku.
- `TrainingScheduleViewComponent` nyní vykresluje blok jako neinteraktivní
  `<div>` bez odkazu na detail nebo editaci.
- `TrainingScheduleItemDto` obsahuje ID každého člena a `GroupId`, ale
  prezentační blok nemá samostatný editovací odkaz.
- Databázová entita `sport.Training` dědí `Date`, `TimeFrom` a `Note` ze
  `SportEvent` a přímo obsahuje `TimeTo` a `Location`.
- Poznámka má maximálně 50 znaků a lokalita maximálně 100 znaků.
- Členství spojeného tréninku reprezentuje `sport.TrainingGroup`; jedna položka
  může patřit nejvýše do jedné skupiny.
- Administrační formuláře používají Contract DTO s DataAnnotations,
  `@Html.EditorFor` a šablony v
  `src/SportSys.Razor/Pages/Shared/EditorTemplates/`.
- V současnosti neexistuje Contract metoda ani Razor stránka pro načtení a
  uložení editace tréninku.

## Potvrzené požadavky a rozhodnutí

- Upravovat lze pouze:
  - datum,
  - čas od,
  - čas do,
  - lokalitu,
  - poznámku.
- Kategorie a typ tréninku se zobrazí pouze textově.
- Nelze měnit kategorii, typ, fázi, stav, tréninkový plán, trenéry ani členství
  ve skupině.
- U spojeného tréninku se změny aplikují na všechny členy stejné skupiny.
- Uložení celé skupiny musí být atomické.
- Formulář spojeného tréninku zobrazí informaci, že změny platí pro všechny
  členy, a seznam dotčených kategorií.
- Pokud členové skupiny nemají shodné hodnoty všech pěti editovatelných polí:
  - načtou se informace o všech členech,
  - zobrazí se v tabulce,
  - zobrazí se chyba vysvětlující, že skupinu nelze editovat,
  - editační pole a akce `Uložit` nebudou dostupné,
  - stejná podmínka se znovu ověří na serveru při POST.
- Tabulka členů se zobrazí pro každý spojený trénink, nejen při konfliktu.
- Blok reálného rozvrhu otevře editaci v novém okně/panelu přes
  `target="_blank"` a `rel="noopener"`.
- Tréninkový plán `/sport/Training/Plan` není editací reálného tréninku dotčen a
  jeho bloky nesmí odkazovat na stránku editace.
- Po úspěšném uložení zůstane editační stránka otevřená a znovu načte uložená
  data se stavovou zprávou. Původní rozvrh získá změny po obnovení stránky.
- Změna nevyžaduje úpravu databázového modelu ani EF Core migraci.

## Technický návrh

### Contract model

Vytvořit samostatné modely pro editační operaci, aby Razor vrstva nepracovala
s databázovými entitami:

- `TrainingEditDto`:
  - `Id`,
  - `Date`,
  - `TimeFrom`,
  - `TimeTo`,
  - `Location`,
  - `Note`.
- `TrainingEditContextDto`:
  - `Input`,
  - seznam `Members`,
  - `IsGrouped`,
  - `CanEdit`,
  - seznam kategorií,
  - případně důvod, proč editace není povolena.
- `TrainingEditMemberDto` pro tabulku:
  - ID,
  - kategorie,
  - typ tréninku,
  - datum,
  - čas od,
  - čas do,
  - lokalita,
  - poznámka.

`TrainingEditDto` ponese DataAnnotations pro české labely, povinnost, délkové
limity a výběr editor templates. `Date`, `TimeFrom` a `TimeTo` použijí
`DataType.Date` a `DataType.Time`; pro `TimeOnly` se doplní chybějící
`Time.cshtml`. `Note` použije `DataType.MultilineText`.

### Načtení editačního kontextu

Nový `TrainingService.GetEditAsync(int id)`:

1. Najde požadovaný trénink.
2. Pokud nemá `GroupMembership`, načte pouze jej.
3. Pokud má skupinu, načte všechny `TrainingGroup` se stejným `GroupId` a jejich
   tréninky včetně kategorií a typů.
4. Členy seřadí podle `SeasonCategory.Order`, názvu kategorie a ID.
5. `CanEdit` nastaví na `true` pouze tehdy, když všichni členové mají shodné:
   `Date`, `TimeFrom`, `TimeTo`, `Location` a `Note`.
6. Pro shodnou skupinu naplní formulář společnými hodnotami. Pro konfliktní
   skupinu vrátí tabulková data, ale neposkytne editovatelný formulář.

Porovnání má být přesné podle uložených hodnot. Lokalita a poznámka se
normalizují pouze stejným způsobem jako při uložení; rozdílný text nesmí být
tiše sjednocen.

### Atomické uložení

`TrainingService.UpdateAsync(TrainingEditDto dto)`:

1. Zahájí explicitní databázovou transakci se sériovou izolací, aby se mezi
   kontrolou skupiny a uložením nemohlo změnit její členství nebo hodnoty.
2. Znovu načte cílový trénink a aktuální členy skupiny.
3. Znovu ověří, že skupina existuje ve stejné podobě a všichni členové mají
   shodné editovatelné hodnoty.
4. Ověří serverové invarianty:
   - `TimeFrom < TimeTo`,
   - neprázdná lokalita do 100 znaků,
   - poznámka do 50 znaků.
5. Aktualizuje všech pět vlastností u cílového tréninku nebo všech členů
   skupiny.
6. Provede jeden `SaveChangesAsync` a commit.
7. Při neexistujícím záznamu nebo změně skupiny vrátí explicitní doménový
   výsledek/chybu; nesmí provést částečný zápis.

`DurationMinutes` se nenastavuje v C# — zůstává databázovým persisted computed
sloupcem.

### Razor stránka

`Training/Edit.cshtml.cs`:

- GET přijme povinné `id`; neexistující trénink vrátí `NotFound`.
- POST binduje pouze `TrainingEditDto Input`.
- Před uložením ověří ModelState a následně Contract službu.
- Při konfliktní skupině přidá chybu do ModelState a znovu načte tabulku členů.
- Po úspěchu přesměruje na stejnou stránku s ID a TempData zprávou, aby fungoval
  Post/Redirect/Get a opakované odeslání formuláře nevytvořilo druhý zápis.

`Training/Edit.cshtml`:

- vždy zobrazí kategorii a typ textově,
- u spojené skupiny zobrazí informační upozornění a tabulku všech členů,
- u konfliktní skupiny zobrazí chybové upozornění a pouze akci `Zavřít`,
- u samostatného nebo konzistentního spojeného tréninku vykreslí
  `@Html.EditorFor(m => m.Input)`,
- nabídne `Uložit` a `Zavřít`; zavření použije klientské `window.close()` s
  bezpečným odkazem zpět na Schedule jako fallback, pokud stránka nebyla
  otevřena skriptem.

### Odkaz z rozvrhu

Interaktivita se přidá do sdíleného prezentačního modelu bez závislosti na
konkrétní Razor stránce:

- `TrainingScheduleBlock` dostane nullable `EditItemId`.
- `TrainingScheduleComponentModel.CreateBlock` nastaví ID pouze pro blok, jehož
  položky jsou `TrainingScheduleItemDto`.
- U skupiny se použije stabilní `MinimumItemId`; služba následně podle něj načte
  celou skupinu.
- ViewComponent vykreslí reálný blok jako `<a>` na
  `/sport/Training/Edit?id={EditItemId}` s `target="_blank"` a `rel="noopener"`.
- Blok tréninkového plánu zůstane neinteraktivní `<div>`.
- Zachovají se stejné CSS třídy, tooltip, barva, rozměry a obsah, aby nedošlo ke
  změně seskupování a lane algoritmu.

## Implementační kroky

### Fáze 1: Contract DTO a validace

Vytvořit:

`src/SportSys.Contract/Models/TrainingEditDto.cs`

1. Definovat `TrainingEditDto`, `TrainingEditContextDto` a
   `TrainingEditMemberDto`.
2. Skrýt `Id` jako hidden input.
3. Nastavit české názvy polí a délkové validace podle databázového modelu.
4. Přidat cross-field validaci `TimeFrom < TimeTo`, například přes
   `IValidatableObject`, aby stejná podmínka platila na serveru nezávisle na UI.
5. Nezahrnout žádnou editovatelnou vlastnost pro kategorii, typ, skupinu ani
   ostatní vazby.

### Fáze 2: Editor template pro `TimeOnly`

Vytvořit:

`src/SportSys.Razor/Pages/Shared/EditorTemplates/Time.cshtml`

1. Použít editor-template layout.
2. Naformátovat `TimeOnly` jako `HH:mm`.
3. Vykreslit `<input type="time">` se stávající třídou formulářových polí.
4. Zachovat unobtrusive validační atributy generované MVC helperem.

### Fáze 3: Contract služba

Vytvořit:

`src/SportSys.Contract/Services/TrainingService.cs`

1. Implementovat `GetEditAsync`.
2. Implementovat společný dotaz pro cílový trénink a všechny členy jeho
   `TrainingGroup`.
3. Vyhodnotit konzistenci pěti editovatelných polí.
4. Implementovat `UpdateAsync` s explicitní transakcí a opakovanou kontrolou
   členství i hodnot.
5. Aktualizovat jen povolené sloupce.
6. Vrátit rozlišitelné výsledky pro nenalezený trénink, nekonzistentní skupinu a
   úspěšné uložení; Razor nesmí parsovat text výjimky.

Upravit:

`src/SportSys.Contract/ServiceCollectionExtensions.cs`

- registrovat `TrainingService` přes `AddScoped`.

### Fáze 4: Razor Page editace

Vytvořit:

- `src/SportSys.Razor/Areas/sport/Pages/Training/Edit.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Edit.cshtml`

1. Implementovat GET, POST, `NotFound`, Post/Redirect/Get a stavovou zprávu.
2. Při neplatném ModelState zachovat znovu načtené read-only informace o
   kategoriích, typech a členech skupiny.
3. Vykreslit přehled členů jako sémantickou tabulku se sloupci:
   Kategorie, Typ, Datum, Čas od, Čas do, Lokalita a Poznámka.
4. U konzistentní skupiny zobrazit informaci, že uložení aktualizuje všechny
   uvedené kategorie.
5. U nekonzistentní skupiny skrýt editační formulář a tlačítko `Uložit`.
6. Doplnit klientskou validaci přes `_ValidationScriptsPartial`.

### Fáze 5: Editační odkaz v blocích Schedule

Upravit:

- `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleComponentModel.cs`
- `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`

1. Přenést do reálného bloku stabilní ID člena.
2. Vykreslit reálné bloky jako odkazy na stránku editace.
3. Otevřít odkaz pomocí `target="_blank"` a zabezpečit `rel="noopener"`.
4. Zachovat neinteraktivní vykreslení bloků z `/sport/Training/Plan`.
5. Zajistit viditelný focus stav a nezměnit absolutní pozicování bloků.

### Fáze 6: Styly

Upravit:

- `src/SportSys.Razor/Styles/_schedule.scss`
- případně `src/SportSys.Razor/Styles/_forms.scss` nebo `Styles/_details.scss`,
  pokud pro upozornění a tabulku neexistuje vhodný sdílený styl.

1. Odstranit výchozí podtržení odkazu a zachovat textovou barvu bloku.
2. Doplnit `:focus-visible` pro blok editovatelného tréninku.
3. Použít pouze stávající sémantické barevné tokeny.
4. Zajistit čitelnost tabulky členů na úzkém displeji.
5. SCSS zkompilovat existujícím `npm run build:css`; kompilovaný
   `wwwroot/css/site.css` neupravovat ručně.

### Fáze 7: Dokumentace

Upravit:

`docs/modules/sport.md`

Doplnit:

- route a účel stránky `Training/Edit`,
- seznam editovatelných a needitovatelných údajů,
- skupinové atomické uložení,
- blokaci editace nekonzistentní skupiny,
- otevření editace z bloků Schedule,
- skutečnost, že Plan zůstává needitovatelný.

## Soubory ke změně

| Akce | Soubor |
|---|---|
| vytvořit | `src/SportSys.Contract/Models/TrainingEditDto.cs` |
| vytvořit | `src/SportSys.Contract/Services/TrainingService.cs` |
| upravit | `src/SportSys.Contract/ServiceCollectionExtensions.cs` |
| vytvořit | `src/SportSys.Razor/Areas/sport/Pages/Training/Edit.cshtml.cs` |
| vytvořit | `src/SportSys.Razor/Areas/sport/Pages/Training/Edit.cshtml` |
| vytvořit | `src/SportSys.Razor/Pages/Shared/EditorTemplates/Time.cshtml` |
| upravit | `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleComponentModel.cs` |
| upravit | `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml` |
| upravit | `src/SportSys.Razor/Styles/_schedule.scss` |
| případně upravit | sdílený SCSS soubor pro tabulku a upozornění |
| upravit | `docs/modules/sport.md` |
| vytvořit/rozšířit | cílené testy Contract služby a prezentačního modelu |

## Testy a ověření

### Automatické testy Contract služby

Přidat testovací projekt pro `SportSys.Contract`, pokud pro něj v solution
neexistuje vhodný projekt, a použít již zavedený testovací framework. Testy
nesmí vyžadovat produkční SQL Server.

Ověřit:

1. `GetEditAsync` vrátí `null` pro neexistující ID.
2. Samostatný trénink je editovatelný a obsahuje správné read-only údaje.
3. Konzistentní skupina vrátí všechny členy ve stabilním pořadí a `CanEdit`.
4. Skupina s rozdílným datem není editovatelná.
5. Totéž pro rozdílný čas od, čas do, lokalitu a poznámku.
6. Update samostatného tréninku mění pouze pět povolených polí.
7. Update skupiny změní všech pět polí všem členům.
8. Kategorie, typ, fáze, stav, trenéři, plán a `GroupId` zůstanou beze změny.
9. Selhání při aktualizaci jednoho člena nezanechá částečně uloženou skupinu.
10. Změna členství nebo hodnot skupiny mezi GET a POST způsobí odmítnutí
    uložení.
11. `TimeFrom >= TimeTo` je odmítnuto.
12. Délkové limity lokality a poznámky jsou vynuceny.

### Testy Razor prezentačního modelu

Rozšířit:

`tests/SportSys.Razor.Tests/TrainingScheduleBlockFactoryTests.cs`

nebo vytvořit samostatné testy `TrainingScheduleComponentModel`:

1. Reálný samostatný blok obsahuje editovací ID.
2. Reálný spojený blok používá stabilní nejnižší ID.
3. Blok `TrainingPlanScheduleItemDto` editovací ID nemá.
4. Přidání odkazu nemění počet bloků, titulky, časové rozsahy ani lanes.

### Ověřovací příkazy

```powershell
Set-Location src\SportSys.Razor
npm run build:css
```

```powershell
dotnet build SportSys.slnx --no-restore
dotnet test --no-build --no-restore
```

## Manuální akceptace

| Scénář | Očekávaný výsledek |
|---|---|
| Kliknutí na samostatný blok Schedule | V novém panelu se otevře editace správného tréninku |
| Kliknutí na spojený blok Schedule | Otevře se jedna editace celé skupiny |
| Kliknutí na blok Plan | Blok není odkazem na editaci reálného tréninku |
| Editace samostatného tréninku | Změní se pouze vybraný záznam |
| Editace konzistentní skupiny | Tabulka ukáže členy a uložení změní všechny |
| Skupina s rozdílnými hodnotami | Tabulka zobrazí rozdíly, formulář i uložení jsou blokované |
| Neplatný časový interval | Formulář zobrazí validační chybu a nic neuloží |
| Neexistující ID | Stránka vrátí HTTP 404 |
| Uložení a obnovení Schedule | Blok má nové datum, čas, lokalitu a tooltip s poznámkou |
| Přesun tréninku na datum s kolizí | Lane algoritmus jej správně umístí bez grafického překryvu |
| Editace spojené skupiny | Skupina zůstane jedním blokem s názvy kategorií ve stejném pořadí |
| Zavření editačního panelu | Původní rozvrh zůstane otevřený |

## Beze změny

- Databázové tabulky a EF Core mapování `Training`, `TrainingGroup` a
  `TrainingPlanGroup`.
- Členství tréninků ve skupinách.
- Kategorie, typ, fáze, stav, vazba na plán a trenéři.
- Algoritmus agregace bloků a rozdělování časových kolizí do lanes.
- Export do Excelu; při příštím načtení použije aktualizovaná data automaticky.
- Stránka `/sport/Training/Plan` a editace `TrainingPlan`.
- Zápis do read-only schématu `plan.*`.

## Mimo rozsah

- Vytváření nebo mazání tréninků.
- Editace kategorie, typu, fáze, stavu, trenérů nebo skupin.
- Sjednocování nekonzistentních skupin prostřednictvím tohoto formuláře.
- Automatické obnovení původního rozvrhu bez reloadu.
- Samostatný detail tréninku.
- Editace tréninkového plánu.
- EF Core migrace nebo změna databázového schématu.
- Přidávání editačních odkazů do dosud neexistujících kalendářových pohledů.

## Hotovo, když

- [ ] Existující trénink lze otevřít z bloku Schedule v novém panelu.
- [ ] Formulář upravuje výhradně datum, čas od, čas do, lokalitu a poznámku.
- [ ] Kategorie a typ jsou viditelné, ale needitovatelné.
- [ ] Konzistentní spojená skupina se ukládá atomicky jako jeden celek.
- [ ] Všichni členové spojené skupiny jsou zobrazeni v tabulce.
- [ ] Nekonzistentní skupinu nelze editovat ani serverovým POST požadavkem.
- [ ] Po uložení zůstává skupina jedním blokem a kolize se stále vykreslují
      bez překryvu.
- [ ] Bloky Plan neodkazují na editaci Training.
- [ ] Jsou pokryty pozitivní, validační, konfliktní a regresní testy.
- [ ] Dokumentace modulu Sport odpovídá implementovanému chování.
- [ ] Nebyla vytvořena ani upravena EF Core migrace.
