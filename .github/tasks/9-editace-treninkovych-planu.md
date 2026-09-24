# Implementační plán: #9 Editace tréninkových plánů

**Issue:** [#9 — Editace tréninkových plánů](https://github.com/vasekNaus/SportSys/issues/9)

**Stav:** Připraveno k implementaci.

## Cíl

Umožnit otevřít blok tréninkového plánu z týdenního přehledu a upravit jej
s využitím ověřených technických principů z editace reálného tréninku.
Editace musí respektovat samostatné skupiny `TrainingPlanGroup`, při změně
spojeného bloku atomicky aktualizovat všechny jeho členy a bezpečně odmítnout
nekonzistentní nebo mezitím změněná data.

## Výchozí stav

- Přehled plánů je na Razor Page
  `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml`.
- `TrainingScheduleService.GetTrainingPlansAsync` vrací
  `TrainingPlanScheduleItemDto` a společná komponenta plány vykresluje, ale
  `TrainingScheduleComponentModel` jim záměrně nepřidává editační odkaz.
- Klikatelný blok reálného tréninku vede na
  `/sport/Training/Schedule/Edit?id={id}`. Stránka používá
  `TrainingService`, `TrainingEditDto` a `TrainingEditContextDto`.
- `TrainingService` načítá buď samostatný trénink, nebo všechny členy
  `TrainingGroup`, kontroluje shodu editovatelných hodnot, vytváří SHA-256
  snapshot pro optimistickou kontrolu souběhu a ukládá skupinu v serializovatelné
  transakci.
- `TrainingPlanGroup` je samostatná vazební tabulka. Její `GroupId` nemá žádný
  vztah k `TrainingGroup.GroupId`.
- `TrainingPlan` obsahuje editovatelné hodnoty `From`, `To`, `DayName`,
  `TimeFrom`, `TimeTo` a `Location`.
- `TrainingPlan.DurationMinutes` je persisted computed sloupec a nesmí se
  nastavovat v C#.
- Issue nemá komentáře ani dodatečná rozhodnutí.

## Potvrzené požadavky a rozhodnutí

- Editace reálného tréninku slouží jako technická a UX inspirace; rozsah polí
  se řídí modelem `TrainingPlan`.
- Upravovat se musí termíny, časy, lokalita plánovaného tréninku.
- Tréninkový plán se nerozšiřuje o poznámku, přestože ji zmiňuje původní text
  issue. Toto novější rozhodnutí uživatele má přednost.
- Editace musí zahrnovat logiku spojených tréninkových plánů.
- Jeden blok plánu se otevře v samostatném panelu stejně jako blok reálného
  tréninku.
- Kategorie a typ tréninku zůstanou informativní. Stejně jako u reálného
  tréninku se v tomto formuláři nemění fáze, trenéři ani členství ve skupině.
- „Termíny“ se pro `TrainingPlan` mapují na `From`, `To` a `DayName`; všechny
  tři hodnoty jsou součástí společně kontrolovaného a ukládaného stavu.


## Technický návrh

### Datový model

Databázový model se nemění. Implementace používá existující vlastnosti
`TrainingPlan`; není potřeba EF Core migrace ani změna konfigurace entity.

### Contract vrstva

Vytvořit samostatný `TrainingPlanService` a DTO modely, aby Razor vrstva
nepřistupovala k EF entitám a aby logika skupin nebyla směšována s
`TrainingService`.

`TrainingPlanEditDto` bude obsahovat:

- skrytá pole `Id` a `OriginalVersion`,
- `From` a `To` jako datumová pole,
- `DayName` jako výběr platného dne pondělí až neděle,
- `TimeFrom` a `TimeTo`,
- `Location`.

DTO bude pomocí DataAnnotations a `IValidatableObject` vynucovat:

- `From <= To`,
- `TimeFrom < TimeTo`,
- přesnou hodnotu `DayName` odpovídající `DayOfWeek.Monday` až
  `DayOfWeek.Sunday`,
- povinnou lokalitu do 100 znaků.

Pro `DayName` připravit hodnoty pro `<select>` v kontextovém DTO nebo
samostatném select-item modelu. Do entity se nadále uloží přesná anglická
hodnota enumu, zatímco UI zobrazí český název dne.

`TrainingPlanEditContextDto` bude obdobou `TrainingEditContextDto` a ponese:

- vstupní DTO,
- seřazené členy skupiny,
- `IsGrouped`,
- `CanEdit`,
- agregované názvy kategorií.

Každý člen skupiny bude pro kontrolní tabulku obsahovat ID, pořadí a název
kategorie, typ tréninku, `From`, `To`, `DayName`, časy a lokalitu.

`TrainingPlanService.GetEditAsync`:

1. Najde cílový plán a případné `TrainingPlanGroup.GroupId`.
2. Načte cílový plán nebo všechny členy stejné skupiny.
3. Seřadí je podle pořadí kategorie, názvu a ID.
4. Označí skupinu za editovatelnou pouze tehdy, když mají všichni členové
   stejné `From`, `To`, `DayName`, `TimeFrom`, `TimeTo` a `Location`.
5. Vytvoří `OriginalVersion` z ID skupiny a všech editovatelných hodnot všech
   členů.

`TrainingPlanService.UpdateAsync`:

1. Zopakuje serverovou validaci DTO.
2. Otevře serializovatelnou transakci.
3. Znovu načte cíl, členství a všechny členy skupiny.
4. Porovná odeslaný snapshot s aktuálním stavem pomocí
   `CryptographicOperations.FixedTimeEquals`.
5. Odmítne nekonzistentní skupinu i v Contract vrstvě.
6. Nastaví všem členům stejné editovatelné hodnoty a uloží je jedním
   `SaveChangesAsync`.
7. Vrátí explicitní výsledek `Success`, `NotFound`, `GroupInconsistent`,
   `Conflict` nebo `InvalidInput`.

`DurationMinutes`, kategorie, typ, fáze, trenéři a `TrainingPlanGroup` se při
editaci nemění.

### Razor Pages

Přidat stránku:

```text
src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml
src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml.cs
```

PageModel bude kopírovat ověřený tok `Schedule/EditModel`:

- GET vrátí 404 pro neexistující plán,
- POST při validační chybě znovu načte kontext,
- úspěch nastaví `TempData` zprávu a provede PRG redirect,
- konflikt zachová aktuální data z databáze a zobrazí uživateli výzvu ke
  kontrole,
- nekonzistentní skupina zůstane needitovatelná,
- zavření panelu se pokusí zavřít okno a jako fallback přejde na
  `Plan/Index`.

`Edit.cshtml` převezme strukturu stránky reálného tréninku:

- souhrn kategorií a typů,
- upozornění, že změna zasáhne všechny spojené plány,
- tabulku členů skupiny včetně platnosti, dne, času a lokality,
- zablokování formuláře při rozdílných editovatelných hodnotách,
- `@Html.EditorFor(model => model.Input)`,
- validační souhrn, tlačítka Uložit a Zavřít a klientskou validaci.

Pro výběr dne použít existující EditorTemplates mechanismus. Pokud současné
šablony neumějí vykreslit požadovaný select z DTO metadat, přidat úzce
zaměřenou vlastní šablonu nebo vykreslit pouze toto pole explicitně; ostatní
standardní pole musí zůstat generovaná přes `EditorFor`.

### Klikatelný blok společné komponenty

`TrainingScheduleComponentModel` musí rozlišit editační cíl bloku:

- blok složený z `TrainingScheduleItemDto` vede na
  `/Training/Schedule/Edit`,
- blok složený z `TrainingPlanScheduleItemDto` vede na
  `/Training/Plan/Edit`.

Místo současného pravidla „plán nemá `EditItemId`“ přidat k bloku explicitní
editační route nebo typ cíle spolu s ID nejnižšího člena. View komponenty pak
použije dynamickou `asp-page` hodnotu. Skupinový blok nadále předává nejnižší
ID; služba podle něj dohledá celé členství.

Komponenta nesmí odvozovat cíl pouze z přítomnosti `GroupId`, protože
`TrainingGroup` a `TrainingPlanGroup` jsou nezávislé a oba typy mohou být
samostatné i seskupené.

### Dokumentace

`docs/modules/sport.md` aktualizovat tak, aby:

- uváděl editaci plánů na `/sport/Training/Plan/Edit?id={id}`,
- popsal editovatelná pole a skupinové chování,
- odstranil tvrzení, že bloky plánů nejsou editovatelné,
- zachoval oddělení `TrainingGroup` a `TrainingPlanGroup`,
- uváděl aktuální route editace reálného tréninku
  `/sport/Training/Schedule/Edit?id={id}`.

## Implementační kroky

### Fáze 1: DTO a servisní logika

1. Přidat editační DTO, kontext, člena skupiny a enum výsledku aktualizace.
2. Implementovat `TrainingPlanService.GetEditAsync` včetně projekce a
   kontroly konzistence.
3. Implementovat validaci, snapshot souběhu, serializovatelnou transakci a
   atomický `UpdateAsync`.
4. Zaregistrovat `TrainingPlanService` v `AddSportSysServices()`.

### Fáze 2: Editační Razor Page

1. Přidat `Plan/Edit.cshtml.cs` podle toku `Schedule/Edit.cshtml.cs`.
2. Přidat formulář, skupinový přehled a chybové stavy do `Plan/Edit.cshtml`.
3. Zapojit EditorTemplates a klientskou validaci.
4. Nastavit návratovou route na `Plan/Index`.

### Fáze 3: Propojení s týdenním plánem

1. Rozšířit `TrainingScheduleBlock` o explicitní editační cíl.
2. Nastavit route a nejnižší ID pro reálné tréninky i plány.
3. Upravit `Default.cshtml`, aby klikatelné bloky směrovaly na správnou
   editační stránku.

### Fáze 4: Testy a dokumentace

1. Doplnit validační testy DTO.
2. Aktualizovat testy komponenty pro samostatný i seskupený plán.
3. Pokrýt pomocnou logiku konzistence a snapshotu skupiny.
4. Aktualizovat dokumentaci sportovního modulu.

## Soubory ke změně

| Soubor | Změna |
|---|---|
| `src/SportSys.Contract/Models/TrainingPlanEditDto.cs` | Nové editační DTO, kontext, členové skupiny a výsledek aktualizace. |
| `src/SportSys.Contract/Services/TrainingPlanService.cs` | Načtení a atomická editace samostatného nebo spojeného plánu. |
| `src/SportSys.Contract/ServiceCollectionExtensions.cs` | Registrace nové služby. |
| `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml` | Nový editační formulář. |
| `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml.cs` | GET/POST tok a mapování výsledků služby. |
| `src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleComponentModel.cs` | Editační route pro blok plánu i tréninku. |
| `src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml` | Dynamický odkaz na správnou editaci. |
| `tests/SportSys.Razor.Tests/TrainingPlanEditDtoTests.cs` | Validace rozsahů, času, dne a lokality. |
| `tests/SportSys.Razor.Tests/TrainingScheduleComponentModelTests.cs` | Odkazy a ID pro samostatné i seskupené plány. |
| `docs/modules/sport.md` | Popis editace plánů a aktuálních rout. |

Podle zvoleného způsobu vykreslení `DayName` může přibýt jedna úzce zaměřená
EditorTemplate v `src/SportSys.Razor/Pages/Shared/EditorTemplates/`.

## Testy a ověření

### Automatické testy

- `TrainingPlanEditDto` odmítne `From > To`.
- DTO odmítne `TimeFrom >= TimeTo`.
- DTO odmítne neplatný nebo nesprávně zapsaný `DayName`.
- DTO přijme všech sedm platných anglických názvů dnů.
- Komponenta vytvoří pro samostatný plán odkaz na `/Training/Plan/Edit` se
  správným ID.
- Komponenta použije u spojeného plánu nejnižší ID člena.
- Reálný trénink po rozšíření komponenty nadále vede na
  `/Training/Schedule/Edit`.
- Kontrola konzistence považuje skupinu za editovatelnou pouze při shodě všech
  editovatelných hodnot.
- Snapshot se změní při změně kteréhokoli člena, skupiny nebo editovatelné
  hodnoty.

Spustit:

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release
dotnet build SportSys.slnx
```

### Chybové stavy

- Neexistující ID vrátí 404.
- Neplatný formulář se zobrazí se zachovanými validačními chybami.
- Skupina s rozdílnou platností, dnem, časem nebo lokalitou zobrazí
  členy, ale neumožní uložení.
- Změna databázových dat mezi GET a POST vrátí konflikt a nepřepíše novější
  hodnoty.
- Selhání kteréhokoli člena skupiny nesmí vést k částečnému uložení.

## Manuální akceptace

1. Otevřít samostatný blok na `/sport/Training/Plan`, změnit platnost, den,
   čas a lokalitu a ověřit jejich zobrazení po PRG redirectu.
2. Zavřít editační panel a ověřit návrat na přehled plánů, pokud prohlížeč
   nepovolí `window.close()`.
3. Otevřít spojený blok s konzistentními hodnotami, ověřit seznam všech členů
   a potvrdit, že jedna změna aktualizovala celou skupinu.
4. Připravit spojenou skupinu s rozdílnou editovatelnou hodnotou a ověřit, že
   formulář není dostupný a tabulka rozdíl zobrazuje.
5. Otevřít stejný plán ve dvou panelech, uložit změnu v prvním a ověřit, že
   druhý panel při uložení zobrazí konflikt.
6. Ověřit, že kliknutí na blok reálného tréninku stále otevírá jeho původní
   editaci.
7. Ověřit, že `DurationMinutes` po změně času přepočítala databáze.

## Beze změny

- Vrstvení `Razor -> Contract -> Database`.
- Vytváření, rušení nebo změna členství `TrainingPlanGroup`.
- Kategorie, typ a fáze tréninkového plánu.
- Přiřazení trenérů v `CoachTrainingPlan` a jejich intervaly platnosti.
- Materializace reálných tréninků z plánů.
- Výpočet `DurationMinutes` v databázi.
- Filtry a skládání lanes na stránce `Plan/Index`.

## Mimo rozsah

- Vytvoření nového tréninkového plánu.
- Mazání plánu.
- Spojování a rozpojování plánů.
- Hromadná změna trenérů, kategorií, typů nebo fází.
- Přidání poznámky k tréninkovému plánu.
- Synchronizace existujících reálných tréninků po změně jejich zdrojového
  plánu.

## Hotovo, když

- Každý blok plánu otevírá `/sport/Training/Plan/Edit?id={id}`.
- Samostatný plán lze upravit a uložit s validací všech požadovaných polí.
- Konzistentní skupina se uloží atomicky a nekonzistentní skupinu nelze
  editovat ani obejit přímým POST požadavkem.
- Souběžná změna je rozpoznána a novější data nejsou přepsána.
- Reálné tréninky si zachovají stávající editační chování.
- Dokumentace odpovídá výsledným routám a chování.
- Build a relevantní automatické testy projdou.
