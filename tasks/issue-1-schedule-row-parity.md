# Implementační plán: #1 Zvýraznění sudých a lichých dnů v rozvrhu

## Kontext

Issue: [#1](https://github.com/vasekNaus/HoSys/issues/1)

Sdílená `TrainingScheduleViewComponent` aktuálně rozlišuje pouze běžný pracovní
den a víkend. Cílem je doplnit jemné střídání pozadí sousedních řádků a zachovat
stávající víkendový akcent.

Změna se týká obou stránek:

- `/sport/Training/Schedule`,
- `/sport/Training/Plan`.

## Potvrzené chování

- Schedule určuje paritu podle pořadového čísla dne v měsíci:
  `date.Day % 2`.
- Plan nemá konkrétní datum, proto určuje paritu podle pořadí řádku v týdnu:
  pondělí je liché, úterý sudé, …, neděle lichá.
- Víkendové zvýraznění zůstane zachováno.
- Víkendový a sudý/lichý styl se kombinují; žádný z významů nepřebíjí druhý.
- Rozdíl bude jemný, ale patrný v light i dark režimu.
- Funkčnost filtrů, timeline, lanes a tréninkových bloků se nemění.

## Fáze 1: Typovaná vizuální varianta řádku

Rozšířit modely v:

`src/SportSys.Razor/Models/TrainingSchedule/`

1. Vytvořit enum `TrainingScheduleRowParity`:

   ```csharp
   public enum TrainingScheduleRowParity
   {
       Odd,
       Even,
   }
   ```

2. Doplnit povinnou vlastnost `Parity` do `TrainingScheduleRow`.
3. Přenést `Parity` do `TrainingScheduleComponentRow` v
   `TrainingScheduleComponentModel`.
4. Nepředávat CSS class jako libovolný string z PageModelu. Význam řádku musí
   zůstat typovaný a ViewComponent jej převede na konkrétní třídu.

Tím zůstane rozhodnutí o paritě v PageModelu, zatímco znalost HTML a CSS tříd
zůstane ve ViewComponent.

## Fáze 2: Parita reálného Schedule

Upravit:

`src/SportSys.Razor/Areas/sport/Pages/Training/Schedule.cshtml.cs`

Při vytváření každého řádku nastavit:

```csharp
Parity = date.Day % 2 == 0
    ? TrainingScheduleRowParity.Even
    : TrainingScheduleRowParity.Odd;
```

Parita se vždy odvozuje z čísla kalendářního dne, nikoli z indexu řádku ve
vybraném intervalu. Výsledek proto zůstane stabilní při změně `DateFrom`.

Příklady:

| Datum | Varianta |
|---|---|
| 1. 9. | Odd |
| 2. 9. | Even |
| 30. 9. | Even |
| 1. 10. | Odd |

## Fáze 3: Parita týdenního Plan

Upravit:

`src/SportSys.Razor/Areas/sport/Pages/Training/Plan.cshtml.cs`

Při projekci pole `WeekDays` použít index a nastavit paritu podle pořadí řádku:

```csharp
Parity = index % 2 == 0
    ? TrainingScheduleRowParity.Odd
    : TrainingScheduleRowParity.Even;
```

Výsledné pořadí:

| Den | Pořadí | Varianta |
|---|---:|---|
| Pondělí | 1 | Odd |
| Úterý | 2 | Even |
| Středa | 3 | Odd |
| Čtvrtek | 4 | Even |
| Pátek | 5 | Odd |
| Sobota | 6 | Even + Weekend |
| Neděle | 7 | Odd + Weekend |

## Fáze 4: CSS třídy ve ViewComponent

Upravit:

`src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`

Každý řádek dostane:

- vždy `schedule-row`,
- právě jednu třídu:
  - `schedule-row--odd`,
  - `schedule-row--even`,
- u soboty a neděle navíc `schedule-row--weekend`.

Výsledné příklady:

```html
<div class="schedule-row schedule-row--odd">
<div class="schedule-row schedule-row--even">
<div class="schedule-row schedule-row--even schedule-row--weekend">
```

Sestavení tříd provést z enumu explicitním `switch`, aby neznámá hodnota
nevedla k tichému vykreslení bez parity.

## Fáze 5: Barevné tokeny

Upravit:

`src/SportSys.Razor/Styles/_vars.scss`

Přidat sémantické tokeny:

```scss
--color-schedule-row-odd;
--color-schedule-row-even;
--color-schedule-row-weekend-overlay;
```

Tokeny definovat pro:

1. výchozí light režim,
2. explicitní dark režim,
3. automatický dark režim přes `prefers-color-scheme`.

Pravidla:

- komponenta nesmí používat primitivní klubové tokeny ani nové přímé hex barvy,
- odd/even pozadí musí být neutrální a jemné,
- víkendový token bude průhledný overlay klubové sekundární barvy,
- výsledné pozadí nesmí snížit kontrast textu pod WCAG 2.1 AA,
- bloky tréninků a jejich barvy se nemění.

Konkrétní hodnoty zvolit podle existujících surface a brand tokenů. Preferovat
průhledné hodnoty, které fungují v obou režimech bez změny barvy textu.

## Fáze 6: Kombinace stylů

Upravit:

`src/SportSys.Razor/Styles/_schedule.scss`

1. Přidat odd/even varianty:

   ```scss
   &--odd {
     background-color: var(--color-schedule-row-odd);
   }

   &--even {
     background-color: var(--color-schedule-row-even);
   }
   ```

2. Víkend nesmí přepsat základní odd/even pozadí. Použít kombinovatelný způsob,
   například více background layers:

   ```scss
   &--odd.schedule-row--weekend {
     background:
       linear-gradient(
         var(--color-schedule-row-weekend-overlay),
         var(--color-schedule-row-weekend-overlay)
       ),
       var(--color-schedule-row-odd);
   }
   ```

   Stejný vzor použít pro sudý víkendový řádek.

3. Zachovat čitelnost svislých gridlines, oddělovačů lanes a levého sloupce.
4. Neaplikovat paritu na záhlaví `.schedule-header`.

## Fáze 7: Dokumentace

Aktualizovat:

`docs/modules/sport.md`

Doplnit:

- pravidlo parity Schedule podle čísla dne v měsíci,
- pravidlo parity Plan podle pořadí dne v týdnu,
- kombinaci parity s víkendovým zvýrazněním,
- odpovědnost PageModelu za určení parity,
- odpovědnost ViewComponent za převod na CSS variantu.

Po dokončení změnit stav tohoto plánu na implementováno a ověřeno.

## Fáze 8: Ověření

1. Spustit:

   ```powershell
   dotnet build SportSys.slnx --no-restore
   ```

2. Ověřit kompilaci SCSS přes existující build target.
3. Ověřit Schedule na intervalu obsahujícím:
   - lichý a sudý den,
   - přechod mezi měsíci,
   - pracovní dny,
   - sobotu a neděli.
4. Ověřit Plan:
   - sedm řádků Po–Ne,
   - střídání Odd/Even,
   - kombinované styly soboty a neděle,
   - prázdné i obsazené řádky.
5. Ověřit light a dark režim:
   - rozlišitelnost sousedních řádků,
   - zachování víkendového akcentu,
   - čitelnost textu, gridlines a bloků.
6. Ověřit, že změna nezasáhla:
   - filtrování,
   - řazení dat,
   - výpočet timeline,
   - rozdělení bloků do lanes,
   - DTO ani Contract služby.
7. Ověřit, že nebyla vytvořena ani změněna EF Core migrace nebo model snapshot.

## Mimo rozsah

- změny databázového schématu,
- EF Core migrace,
- uživatelské nastavení barev,
- legenda vysvětlující liché a sudé řádky,
- změna barev tréninkových bloků,
- změna víkendové logiky,
- JavaScriptové přepínání zvýraznění.
