# Implementační plán: Multivýběr typu tréninku

**Issue:** [#3 — Změna filtru „Typ tréninku“ na multivýběr](https://github.com/vasekNaus/HoSys/issues/3)

**Stav:** Implementováno a ověřeno sestavením řešení.

## Cíl

Na stránce `/sport/Training/Plan` změnit filtr `Typ tréninku` z výběru jedné
hodnoty na multivýběr. Uživatel bude moci zobrazit jeden, více nebo všechny typy
tréninků a výběr zůstane zachován v query stringu.

Stránka `/sport/Training/Schedule` není součástí změny.

## Potvrzená rozhodnutí

- Změna se týká výhradně stránky `/sport/Training/Plan`.
- Prázdný výběr znamená všechny typy tréninků.
- Výsledky se aktualizují po stisknutí stávajícího tlačítka `Zobrazit plán`.
- Řešení nepřidá novou frontendovou knihovnu.
- Multivýběr bude založený na nativních checkBoxech a bude použitelný i bez
  JavaScriptu.
- JavaScript bude zajišťovat pouze souhrn výběru, rychlé vymazání a chování
  rozbalovací nabídky.
- Vizualizace plánu a rozdělování překrývajících se bloků do lanes se nemění.
- Změna nevyžaduje úpravu databázového modelu ani EF Core migraci.

## Fáze 1: Stav filtru v PageModelu

Upravit:

`src/SportSys.Razor/Areas/sport/Pages/Training/Plan.cshtml.cs`

1. Nahradit vlastnost:

   ```csharp
   int? TrainingTypeId
   ```

   kolekcí:

   ```csharp
   List<int> SelectedTrainingTypeIds
   ```

2. Zachovat `[BindProperty(SupportsGet = true)]`. Hodnoty se budou přenášet jako
   opakované GET parametry:

   ```text
   SelectedTrainingTypeIds=1&SelectedTrainingTypeIds=2
   ```

3. Po načtení číselníku typů:
   - odstranit neexistující identifikátory,
   - odstranit duplicity,
   - zachovat prázdnou kolekci jako volbu všech typů.

4. Odstranit podmínku, která nyní vyžaduje `TrainingTypeId`. Pro načtení plánu
   zůstanou povinné:
   - platná sezóna,
   - alespoň jedna kategorie,
   - platná fáze tréninku.

5. Předat kolekci `SelectedTrainingTypeIds` metodě
   `TrainingScheduleService.GetTrainingPlansAsync`.

## Fáze 2: Filtrování v Contract službě

Upravit:

`src/SportSys.Contract/Services/TrainingScheduleService.cs`

1. Změnit parametr metody `GetTrainingPlansAsync`:

   ```csharp
   int trainingTypeId
   ```

   na:

   ```csharp
   IReadOnlyCollection<int> trainingTypeIds
   ```

2. Nejprve sestavit základní `IQueryable` omezené podle:
   - sezóny,
   - vybraných kategorií,
   - fáze tréninku.

3. Podmínku podle typu přidat pouze pro neprázdnou kolekci:

   ```csharp
   if (trainingTypeIds.Count > 0)
   {
       query = query.Where(
           plan => trainingTypeIds.Contains(plan.TrainingTypeId));
   }
   ```

4. `Contains` se přeloží do SQL podmínky `IN (...)`. Výsledkem bude sjednocený
   seznam plánů odpovídajících alespoň jednomu vybranému typu bez vzniku
   duplicit.

5. Projekci, řazení a načítání skupin plánů ponechat beze změny.

## Fáze 3: Uživatelské rozhraní multivýběru

Upravit:

`src/SportSys.Razor/Areas/sport/Pages/Training/Plan.cshtml`

1. Nahradit jednoduchý element `select` rozbalovacím multivýběrem.
2. Každý typ vykreslit jako nativní checkbox se jménem
   `SelectedTrainingTypeIds`.
3. Checkbox označit jako vybraný, pokud je jeho ID obsaženo v kolekci
   `Model.SelectedTrainingTypeIds`.
4. Souhrn ovládacího prvku zobrazí:
   - `Všechny typy`, pokud není vybrán žádný typ,
   - název typu, pokud je vybrán právě jeden,
   - `Vybráno: N`, pokud je vybráno více typů.
5. Doplnit tlačítko `Vymazat výběr`, které odškrtne všechny položky. Nesmí
   odeslat formulář samo o sobě.
6. Zachovat stávající odeslání celého filtru tlačítkem `Zobrazit plán`.
7. Doplnit potřebné přístupnostní atributy a zachovat ovládání klávesnicí.

## Fáze 4: Klientské chování

Upravit:

`src/SportSys.Razor/wwwroot/js/site.js`

1. Inicializovat komponenty označené například atributem `data-multiselect`.
2. Po změně checkboxu aktualizovat text souhrnu.
3. Tlačítkem pro vymazání odškrtnout všechny checkboxy a nastavit souhrn na
   `Všechny typy`.
4. Rozbalenou nabídku zavřít při kliknutí mimo komponentu.
5. Neprovádět automatické odeslání formuláře.
6. Serverové odeslání checkboxů musí fungovat také při vypnutém JavaScriptu.

## Fáze 5: Styly

Upravit:

`src/SportSys.Razor/Styles/_schedule.scss`

1. Doplnit styly ovládacího prvku, rozbalovací nabídky, seznamu checkBoxů,
   souhrnu a akce pro vymazání.
2. Použít pouze existující sémantické barevné tokeny `--color-*`.
3. Zajistit:
   - viditelný stav `:focus-visible`,
   - dostatečně velké klikací plochy,
   - kontrast odpovídající WCAG 2.1 AA,
   - použitelné rozložení na úzkých obrazovkách,
   - správný vzhled ve světlém i tmavém režimu.
4. Neupravovat ručně kompilovaný soubor
   `src/SportSys.Razor/wwwroot/css/site.css`.

## Fáze 6: Dokumentace

V `docs/modules/sport.md` změnit popis filtrů stránky Plan:

- filtr typu tréninku umožňuje nula, jeden nebo více typů,
- prázdný výběr znamená všechny typy,
- filtr fáze tréninku zůstává výběrem jedné hodnoty.

## Beze změny

Následující části se nebudou upravovat:

- `TrainingScheduleViewComponent`,
- prezentační modely timeline,
- algoritmus seskupování plánů,
- algoritmus rozdělování kolizí do lanes,
- stránka `/sport/Training/Schedule`,
- databázové entity, konfigurace a migrace.

## Ověření

1. Jeden vybraný typ vrátí stejné výsledky jako původní filtr.
2. Více vybraných typů vrátí plány odpovídající kterémukoliv z nich.
3. Jeden plán se ve výsledku nezobrazí vícekrát.
4. Prázdný výběr zobrazí všechny typy.
5. Jednotlivé typy lze přidávat i odebírat.
6. Akce pro vymazání odstraní celý výběr.
7. Výběr zůstane zachován po odeslání a obnovení stránky.
8. Neplatné a duplicitní identifikátory v URL budou normalizovány.
9. Ostatní filtry stránky Plan fungují beze změny.
10. Překrývající se plány různých typů zůstanou rozdělené do samostatných
    lanes a nebudou se graficky překrývat.
11. Multivýběr je použitelný klávesnicí a ve světlém i tmavém režimu.
12. V `src/SportSys.Razor` spustit:

    ```powershell
    npm run build:css
    ```

13. V kořeni repozitáře spustit:

    ```powershell
    dotnet build SportSys.slnx
    ```
