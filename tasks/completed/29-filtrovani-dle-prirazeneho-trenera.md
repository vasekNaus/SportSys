# Implementační plán: #29 Filtrování dle přiřazeného trenéra

**Issue:** [#29 — Filtrování dle přiřazeného trenéra](https://github.com/vasekNaus/SportSys/issues/29)

**Stav:** Implementováno a ověřeno.

## Cíl

Přidat na stránky Tréninkové plány, Rozvrh tréninků a zápasů a Požadavky
vyhledatelný vícenásobný filtr trenérů. Záznam vyhoví, pokud je přiřazen
alespoň jednomu z vybraných trenérů; prázdný výběr filtr neuplatní. Filtr musí
omezit data již v Contract/EF Core dotazu a zachovat ostatní filtry i všechny
způsoby vykreslení.

## Výchozí stav

- `Training/Plan/IndexModel` načítá grafický rozvrh i tabulkové řádky ze stejného
  výsledku `TrainingScheduleService.GetTrainingPlansAsync`.
- `Training/Schedule/IndexModel` načítá tréninky přes
  `TrainingScheduleService.GetTrainingsAsync` a zápasy přes
  `MatchScheduleService.GetMatchesAsync`. Export tréninků znovu používá stejné
  načítací metody.
- `Training/Requirement/IndexModel` načítá obě tabulky samostatně přes
  `TrainingRequirementService.GetAllAsync` a `MatchRequirementService.GetAllAsync`.
  Oba výstupní modely již obsahují přiřazení trenérů.
- `TrainingScheduleService` načítá trenérská přiřazení k tréninkům a plánům
  samostatnými dotazy. U plánů se zobrazuje přiřazení, jehož interval platnosti
  se překrývá s intervalem plánu.
- `SearchMultiSelectTagHelper` generuje vícenásobný vyhledávatelný select
  s Razor Pages bindingem. Aktuální filtry rozvrhů používají vlastní
  vícenásobné checkboxy bez vyhledávání.
- V aktuálním pracovním stromu existují necommitnuté změny modelu
  `SportSys.Database.Models.sport.CoachMatch`, navigace `Match.CoachMatches`,
  `SportSysDbContext.CoachMatches` a související migrace. Před implementací
  ověřit, že tato rozpracovaná vazba je součástí cílového základu.

## Potvrzené požadavky a rozhodnutí

- Filtr má popisek **Trenéři**, podporuje vyhledávání, výběr i odebrání více
  trenérů a používá `SearchMultiSelectTagHelper`.
- Bez výběru se filtr ignoruje. U více trenérů se používá OR: záznam vyhoví,
  pokud obsahuje alespoň jednoho vybraného trenéra.
- Na rozvrhových stránkách, které oddělují společné filtry od filtrů tréninků
  a zápasů, bude filtr ve společné sekci a bude se vztahovat na oba typy
  událostí.
- Filtr ovlivní oba výstupy plánů, grafický rozvrh událostí a obě tabulky
  požadavků. Nesmí záviset na klientském vykreslení ani na přepnutí zobrazení
  tréninků/zápasů.
- Ostatní filtry se zachovají a v rámci každého dotazu se s filtrem trenérů
  kombinují jako další podmínky.

## Technický návrh

- Přidat GET kolekci ID vybraných trenérů do příslušných Razor Page modelů.
  Seznam možností načíst přes Contract službu a existující trenérský výběr
  `RequirementEditHelper.GetCoachesAsync`; Razor nesmí přistupovat přímo
  k `SportSysDbContext`. Neznámá ID normalizovat podle seznamu dostupných trenérů,
  stejně jako ostatní vícenásobné filtry.
- Ve všech dotčených Contract službách přidat volitelnou kolekci ID trenérů
  a aplikovat podmínku v EF dotazu před materializací. Prázdná kolekce
  podmínku nepřidá; jinak použít `Any` nad vazbou a `Contains` nad výběrem ID.
- U `TrainingPlan` zahrnout pouze přiřazení trenéra, jehož platnost se překrývá
  s intervalem plánu — stejná sémantika, kterou nyní používá
  `TrainingScheduleService.AssignCoachesAsync`.
- Rozvrhové plány a požadavky zobrazují trenéry v seznamech; nový filtr se
  použije nad odpovídajícími vazbami (`CoachTrainingPlan`,
  `CoachTrainingRequirement`, `CoachMatchRequirement`). U skutečných tréninků
  se filtruje přes `CoachTraining`, u zápasů přes `CoachMatch`.
- Volby vykreslit pomocí `<search-multiselect>` v samostatném poli **Trenéři**.
  Na `/sport/Training/Plan` patří do standardních filtrů; na stránkách rozvrhu
  a požadavků do sekce **Společné filtry**.
- Předat výběr také do handleru exportu rozvrhu, aby exportované tréninky
  odpovídaly stejnému filtru jako zobrazený rozvrh.

### Databázový model

Pro plány, tréninky a požadavky již existují trenérské vazební entity. V tomto
pracovním stromu je navíc přítomna rozpracovaná vazba `CoachMatch`; pokud bude
součástí integračního základu, issue #29 nevyžaduje další změnu modelu ani
migraci. Pokud tato vazba v cílovém základu chybí, implementaci filtrování
zápasů na ní nelze dokončit v rámci tohoto plánu bez vyřešení chybějícího
datového zdroje. Agent nevytváří ani neupravuje EF migrace či snapshot.

## Implementační kroky

### Fáze 1: Zpřístupnění trenérů a UI filtru

1. Doplnit načtení dostupných trenérů přes Contract vrstvu pro stránky plánů,
   rozvrhu a požadavků.
2. Přidat GET binding vybraných ID a jejich normalizaci.
3. Vložit `SearchMultiSelectTagHelper` s popiskem **Trenéři** na všechny tři
   stránky. Na rozvrhu i požadavcích umístit ovládací prvek do společných
   filtrů.

### Fáze 2: Filtrování v Contract/EF Core dotazech

1. Rozšířit `TrainingScheduleService.GetTrainingPlansAsync` a
   `GetTrainingsAsync`; omezit entity přes odpovídající trenérskou vazbu před
   `ToListAsync`.
2. Rozšířit `MatchScheduleService.GetMatchesAsync` a aplikovat filtr přes
   `Match.CoachMatches` před projekcí/materializací.
3. Rozšířit `TrainingRequirementService.GetAllAsync` a
   `MatchRequirementService.GetAllAsync` tak, aby filtrovaly přímo entity
   požadavků podle přiřazených ID trenérů.
4. Předat normalizovaný výběr ze všech tří Page modelů do uvedených metod.
   U rozvrhu jej použít jak v `OnGetAsync`, tak v `OnGetExportAsync`.

### Fáze 3: Testy a ověření

1. Doplnit testy služeb pro prázdný výběr, jednoho trenéra, více trenérů
   s logikou OR a záznam bez přiřazeného trenéra.
2. Ověřit časové překrytí přiřazení u tréninkových plánů a filtrování zápasů
   přes `CoachMatch`.
3. Ověřit, že ostatní parametry filtrů nadále omezují výsledky a že seznamové
   i grafické zobrazení plánů/rozvrhu používají shodnou sadu dat.
4. Spustit cílené testy v `tests/SportSys.Razor.Tests` a následně build řešení.

## Soubory ke změně

- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Index.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Schedule/Index.cshtml.cs`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml`
- `src/SportSys.Razor/Areas/sport/Pages/Training/Requirement/Index.cshtml.cs`
- `src/SportSys.Contract/Services/TrainingScheduleService.cs`
- `src/SportSys.Contract/Services/MatchScheduleService.cs`
- `src/SportSys.Contract/Services/TrainingRequirementService.cs`
- `src/SportSys.Contract/Services/MatchRequirementService.cs`
- `tests/SportSys.Razor.Tests/TrainingPlanServiceTests.cs`
- `tests/SportSys.Razor.Tests/TrainingScheduleServiceTests.cs` (nový, pokud
  testy nelze přirozeně doplnit do existujících testů služby)
- `tests/SportSys.Razor.Tests/MatchScheduleServiceTests.cs`
- `tests/SportSys.Razor.Tests/TrainingRequirementServiceTests.cs` (nový)
- `tests/SportSys.Razor.Tests/MatchRequirementServiceTests.cs` (nový)

## Testy a ověření

- Službové testy ověřují, že prázdný seznam trenérů nemění dosavadní výsledky,
  jeden trenér vybere jen své záznamy a více trenérů vrací sjednocení (OR),
  nikoli průnik.
- Pokrýt přiřazení trenéra ke každému typu záznamu: `CoachTraining`,
  časově platné `CoachTrainingPlan`, `CoachMatch`,
  `CoachTrainingRequirement` a `CoachMatchRequirement`.
- Ověřit, že přiřazení plánu mimo jeho interval platnosti samo o sobě plán
  neshodí do výsledků, a že ostatní filtry (sezóna, kategorie, datum, typ,
  lokalita, fáze/stav) dál platí.
- Ověřit obě cesty načtení rozvrhu včetně exportního handleru a oba výstupy
  stránky plánů.
- Cílené ověření: `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release`;
  build: `dotnet build SportSys.slnx`.

## Manuální akceptace

1. Na stránce Tréninkové plány vyhledat trenéra, vybrat jej a ověřit, že
   grafický rozvrh i tabulka obsahují pouze plány s odpovídajícím přiřazením.
2. Vybrat dva trenéry; zobrazí se plán přiřazený kterémukoli z nich, včetně
   plánu přiřazeného oběma. Odebrat výběr a ověřit obnovení výsledků ostatních
   filtrů.
3. Na stránce Rozvrh vybrat trenéra a ověřit tréninky i zápasy; změnit
   zobrazení pouze na tréninky nebo pouze na zápasy a ověřit, že filtr zůstane
   společný. Export při stejném výběru obsahuje pouze odpovídající tréninky.
4. Na stránce Požadavky ověřit, že vybraný trenér omezuje tabulky požadavků na
   tréninky i zápasy, zatímco prázdný výběr ponechá ostatní filtry beze změny.

## Beze změny

- Přiřazování trenérů, úprava plánů/požadavků a struktura tabulek výsledků.
- Chování sloučení tréninků, stavů, typů, kategorií, lokalit, datumů a exportu
  kromě uplatnění vybraného trenérského filtru.
- Datový model a migrační historie, pokud je existující `CoachMatch` součástí
  cílového základu.

## Mimo rozsah

- Nové způsoby přiřazování trenérů k událostem nebo požadavkům.
- Filtrování podle rolí, jmen částečnou shodou mimo hledání v ovládacím prvku,
  nebo logika AND mezi více vybranými trenéry.
- Vytváření nebo aplikace EF Core migrací.

## Hotovo, když

- Všechny tři stránky nabízejí vyhledatelný vícenásobný filtr **Trenéři**
  v určené společné/standardní části.
- Filtrování probíhá v databázových dotazech pro plány, tréninky, zápasy
  i oba typy požadavků; výběr více trenérů používá OR a prázdný výběr filtr
  ignoruje.
- Rozvrhový export i všechny grafické/tabulkové výstupy respektují stejný
  výběr a stávající filtry zůstávají funkční.
- Cílené testy a build řešení projdou; není přidána ani upravena migrace.
