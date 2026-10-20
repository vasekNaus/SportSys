# Implementační plán: #2 Zachování zobrazení překrývajících se tréninků

**Stav:** Implementováno a ověřeno.

## Kontext

Issue: [#2](https://github.com/vasekNaus/HoSys/issues/2)

Sdílená `TrainingScheduleViewComponent` dnes řadí časově kolidující položky do
samostatných lanes. Tuto funkci je nutné zachovat a současně doplnit explicitní
spojování souvisejících tréninků do jednoho vizuálního bloku.

Změna se týká obou stránek:

- `/sport/Training/Schedule` pro reálné tréninky,
- `/sport/Training/Plan` pro obecné tréninkové plány.

## Potvrzené chování

- Nespojené položky se chovají jako dnes:
  - překrývající se intervaly jsou v samostatných lanes,
  - navazující intervaly, kde `previous.TimeTo == next.TimeFrom`, mohou sdílet lane,
  - nepřekrývající se položky zůstávají ve stejném lane.
- Lane algoritmus musí podporovat libovolný počet současných kolizí, ne pouze dvě.
- Explicitně spojené položky se v rámci jednoho dne vykreslí jako jeden blok.
- Spojení podporuje libovolný počet položek stejného typu:
  - reálné tréninky lze spojovat pouze s dalšími `Training`,
  - tréninkové plány lze spojovat pouze s dalšími `TrainingPlan`,
  - `Training` a `TrainingPlan` se nikdy nestávají členy stejné skupiny.
- Viditelný titulek spojeného bloku vznikne spojením názvů kategorií znakem `+`.
- Opakované názvy se neodstraňují; každý člen skupiny přispěje jednou částí.
- Části titulku se řadí podle `SeasonCategory.Order`, následně podle názvu
  kategorie a ID položky pro deterministický výsledek.
- Časový interval spojeného bloku je sjednocený rozsah od nejčasnějšího
  `TimeFrom` po nejpozdější `TimeTo`.
- Barva spojeného bloku odpovídá první kategorii v určeném pořadí.
- Tooltip zachová informace o všech členech skupiny; jednotlivé položky budou
  oddělené jednoznačným textovým oddělovačem.
- Spojený blok vstupuje do stejného lane algoritmu jako běžný blok. Pokud
  koliduje s jiným spojeným nebo samostatným blokem, zobrazí se v samostatném lane.
- Skupina se nikdy neslučuje napříč řádky timeline. Stejné group ID v různých
  dnech nebo dnech týdne vytvoří samostatný blok v každém řádku.
- Funkčnost filtrů, parity řádků, víkendového zvýraznění, timeline a barev
  kategorií se nemění.

## Datový návrh

Použít dvě nezávislé vazební tabulky v databázovém schématu `sport`.
Samostatná hlavičková tabulka skupiny ani společná skupina pro oba typy položek
nevznikne.

```text
sport.TrainingGroup
├── GroupId uniqueidentifier
└── TrainingId int → sport.Training.Id

PK  (GroupId, TrainingId)
UQ  (TrainingId)

sport.TrainingPlanGroup
├── GroupId uniqueidentifier
└── TrainingPlanId int → sport.TrainingPlan.Id

PK  (GroupId, TrainingPlanId)
UQ  (TrainingPlanId)
```

`GroupId` je logický identifikátor skupiny generovaný aplikací. Více řádků se
stejným `GroupId` tvoří jednu skupinu. Unikátní index na ID člena zajišťuje, že
jedna položka může patřit nejvýše do jedné skupiny. Položka bez řádku ve vazební
tabulce je samostatná.

Tento model umožní:

- spojit libovolný počet reálných tréninků přes `TrainingGroup`,
- spojit libovolný počet tréninkových plánů přes `TrainingPlanGroup`,
- ponechat nespojené položky bez pomocných záznamů,
- vynutit oddělené skupiny pro `Training` a `TrainingPlan`,
- odstranit skupinu smazáním jejích vazebních řádků bez smazání tréninků nebo
  plánů.

Při smazání `Training` nebo `TrainingPlan` se smaže pouze jeho vazební řádek.
Prázdná skupina fyzicky neexistuje, protože skupinu reprezentují přímo její
členské řádky.

## Fáze 1: EF Core model vazebních tabulek

Upravit projekt `src/SportSys.Database`.

1. Vytvořit `Models/Sport/TrainingGroup.cs` jako vazební entitu:

   - `[Table(nameof(TrainingGroup), Schema = Schemas.Sport)]`,
   - složený primární klíč `GroupId + TrainingId`,
   - `Guid GroupId`,
   - `int TrainingId`,
   - povinnou navigaci `Training`,
   - unikátní index `UX_TrainingGroup_TrainingId`,
   - delete behavior `Cascade` z `Training` pouze na vazební řádek.

2. Vytvořit `Models/Sport/TrainingPlanGroup.cs` stejným vzorem:

   - `[Table(nameof(TrainingPlanGroup), Schema = Schemas.Sport)]`,
   - složený primární klíč `GroupId + TrainingPlanId`,
   - `Guid GroupId`,
   - `int TrainingPlanId`,
   - povinnou navigaci `TrainingPlan`,
   - unikátní index `UX_TrainingPlanGroup_TrainingPlanId`,
   - delete behavior `Cascade` z `TrainingPlan` pouze na vazební řádek.

3. Rozšířit `Models/Sport/Training.cs` o nullable referenční navigaci na jeho
   jediný vazební řádek. `Training` nebude obsahovat sloupec `GroupId`.

4. Stejně rozšířit `Models/Sport/TrainingPlan.cs`. Ani `TrainingPlan` nebude
   obsahovat sloupec `GroupId`.

5. Přidat `DbSet<TrainingGroup>` a `DbSet<TrainingPlanGroup>` do:

   `Context/SportSysDbContext.cs`.

6. Nepřidávat Fluent API konfiguraci, pokud nebude potřeba vyjádřit pravidlo,
   které nelze zapsat atributem.

7. Nepřidávat `HasColumnName`; pojmenování FK zajistí Apollo `IdConvention()`.

8. Nevytvářet ani neupravovat EF Core migraci. Migraci vytvoří uživatel.

## Fáze 2: Datový kontrakt pro seskupování

Upravit:

`src/SportSys.Contract/Models/TrainingScheduleDto.cs`

Rozšířit `ITrainingScheduleItem` a jeho implementace o:

```csharp
Guid? GroupId { get; }
int SeasonCategoryOrder { get; }
```

Význam:

- `GroupId == null` znamená samostatný blok,
- shodné nenulové ID označuje členy jednoho spojeného bloku,
- `SeasonCategoryOrder` určuje pořadí částí výsledného titulku a primární barvu.

`GroupId` má společný prezentační název pouze proto, aby sdílená komponenta
pracovala se stejným kontraktem. U reálných tréninků pochází z
`TrainingGroup`, u plánů z `TrainingPlanGroup`; skupiny se mezi těmito tabulkami
nesdílejí.

Do Contract DTO se nepřenáší EF navigace ani databázové entity.

## Fáze 3: Projekce dat ve službě

Upravit:

`src/SportSys.Contract/Services/TrainingScheduleService.cs`

V obou projekcích doplnit načtení ID z příslušné vazební tabulky:

```csharp
GroupId = entity.GroupMembership == null
    ? null
    : entity.GroupMembership.GroupId;
SeasonCategoryOrder = entity.SeasonCategory.Order;
```

Konkrétně:

1. `GetTrainingsAsync` přenese `GroupId` z `TrainingGroup`.
2. `GetTrainingPlansAsync` přenese `GroupId` z `TrainingPlanGroup`.
3. Stávající databázové filtry a řazení zůstanou zachovány.
4. Služba nebude vytvářet prezentační bloky ani lanes.

## Fáze 4: Normalizace položek na vizuální bloky

Upravit:

`src/SportSys.Razor/Models/TrainingSchedule/TrainingScheduleComponentModel.cs`

Před rozdělením do lanes převést položky řádku na interní
`TrainingScheduleBlock`:

1. Položku bez `GroupId` převést na samostatný blok.
2. Položky se shodným nenulovým `GroupId` seskupit do jednoho bloku.
3. Členy skupiny seřadit podle:
   - `SeasonCategoryOrder`,
   - `SeasonCategoryName`,
   - `Id`.
4. Pro každý blok vypočítat:
   - `TimeFrom` jako minimum členů,
   - `TimeTo` jako maximum členů,
   - `Title` spojením `SeasonCategoryName` pomocí ` + `,
   - `Color` z první seřazené položky,
   - `Tooltip` ze všech seřazených položek spojených oddělovačem ` | `,
   - procentuální `Left` a `Width` ze sjednoceného intervalu.
5. V bloku zachovat typovaný seznam zdrojových položek, aby žádná informace
   nebyla ztracena a view nemuselo znovu seskupovat.

Samostatná položka projde stejnou cestou jako skupina s jedním členem. Tím se
zamezí dvěma rozdílným vykreslovacím větvím.

## Fáze 5: Zachování lane algoritmu

Metodu `CreateLanes` upravit tak, aby pracovala s již vytvořenými bloky, nikoli
přímo s `ITrainingScheduleItem`.

Bloky řadit deterministicky podle:

1. `TimeFrom`,
2. `TimeTo`,
3. pořadí první kategorie,
4. nejnižšího ID člena.

Pro každý blok použít stávající podmínku:

```csharp
existing.Count == 0 || existing[^1].TimeTo <= block.TimeFrom
```

Pokud žádný lane podmínku nesplní, vytvořit nový. Tento greedy interval
partitioning zachovává minimální potřebný počet lanes pro seřazené intervaly a
funguje pro částečné i úplné překrytí libovolného počtu bloků.

Seskupování a detekce kolizí musí být dvě oddělené fáze:

```text
zdrojové položky
→ samostatné nebo spojené vizuální bloky
→ seřazení bloků
→ rozdělení do lanes
→ vykreslení
```

## Fáze 6: Úprava ViewComponent view

Upravit:

`src/SportSys.Razor/Pages/Shared/Components/TrainingSchedule/Default.cshtml`

View bude používat prezentační vlastnosti bloku:

- `block.Title`,
- `block.TimeFrom`,
- `block.TimeTo`,
- `block.Color`,
- `block.Tooltip`.

Nesmí předpokládat, že blok obsahuje právě jednu `Item`. HTML struktura
`.schedule-block` a existující SCSS zůstanou beze změny, pokud ověření neodhalí
konkrétní problém s delším titulkem.

Text titulku musí zůstat HTML-enkódovaný standardním Razor vykreslením. Tooltip
se sestaví jako obyčejný text a nesmí používat `Html.Raw`.

## Fáze 7: Datová integrita a zápis skupin

Při všech budoucích zápisech platí:

- jedna položka může mít nejvýše jeden členský řádek díky unikátnímu indexu,
- skupina může obsahovat libovolný počet položek stejného typu,
- spojení skupiny vznikne vložením nejméně dvou řádků se stejným novým
  `GroupId`,
- zrušení spojení znamená smazání příslušných vazebních řádků,
- skupiny `TrainingGroup` a `TrainingPlanGroup` jsou nezávislé; shodná hodnota
  `GroupId` v obou tabulkách nevyjadřuje vazbu mezi plánem a tréninkem,
- materializuje-li se více reálných `Training` z propojených `TrainingPlan`,
  musí zápis pro vzniklé tréninky vytvořit novou skupinu v `TrainingGroup`;
  `GroupId` z `TrainingPlanGroup` se nekopíruje napříč tabulkami,
- připojení položek z různých řádků nezpůsobí jejich sloučení napříč daty;
  komponenta je vždy seskupuje pouze uvnitř aktuálního řádku.

Současný repozitář neobsahuje administrační UI pro vytváření tréninků ani plánů.
CRUD obrazovka pro správu skupin proto není součástí tohoto issue; datový model
a čtecí cesta však musí být připravené pro její pozdější doplnění.

## Fáze 8: Dokumentace

Aktualizovat:

`docs/modules/sport.md`

Doplnit:

- význam vazebních tabulek `TrainingGroup` a `TrainingPlanGroup`,
- jejich oddělené použití pro Schedule a Plan,
- pravidla sestavení titulku a časového rozsahu,
- pořadí seskupení před lane algoritmem,
- zachování samostatných lanes pro nepropojené kolize,
- pravidlo vytvoření nové skupiny reálných tréninků při materializaci
  propojených plánů.

Po dokončení změnit stav tohoto plánu na implementováno a ověřeno.

## Fáze 9: Ověření

1. Spustit:

   ```powershell
   dotnet build SportSys.slnx --no-restore
   ```

2. Ověřit Schedule i Plan pro:

   | Scénář | Očekávaný výsledek |
   |---|---|
   | Dvě nespojené položky bez kolize | Jeden lane |
   | Dvě nespojené částečně kolidující položky | Dva lanes |
   | Dvě nespojené položky se shodným intervalem | Dva lanes |
   | Tři současně kolidující položky | Tři lanes |
   | Intervaly navazující hranou | Jeden lane |
   | Dvě spojené položky | Jeden blok s titulkem `A + B` |
   | Tři spojené položky v jiném vstupním pořadí | Jeden deterministický blok |
   | Spojená skupina kolidující se samostatnou položkou | Dva lanes |
   | Dvě spojené skupiny ve vzájemné kolizi | Dva lanes |
   | Stejné group ID ve dvou datech | Jeden blok v každém dni |
   | Shodné group ID v obou vazebních tabulkách | Žádné propojení mezi Plan a Schedule |
   | Prázdný den | Jeden prázdný track jako dosud |

3. U spojeného bloku ověřit:

   - pořadí názvů podle `SeasonCategory.Order`,
   - znak `+` mezi všemi názvy,
   - rozsah od minimálního začátku po maximální konec,
   - tooltip se všemi členy,
   - barvu první kategorie.

4. Ověřit, že se dynamická výška dne přizpůsobí počtu lanes a nedochází k
   překryvu bloků, textů, gridlines ani hranic řádku.

5. Ověřit, že změna nezasáhla:

   - filtry Schedule a Plan,
   - paritu a víkendové zvýraznění řádků,
   - dynamický rozsah timeline,
   - pořadí dnů,
   - barvy nespojených položek.

6. Ověřit, že agent nevytvořil ani nezměnil EF Core migraci nebo model snapshot.

## Mimo rozsah

- administrační UI pro ruční vytváření a rušení skupin,
- automatické odvozování skupin pouze podle shodného času,
- slučování položek napříč různými řádky timeline,
- změna filtrů Schedule nebo Plan,
- změna barevné palety kategorií,
- editace nebo vytvoření EF Core migrace,
- společné skupiny obsahující současně `Training` i `TrainingPlan`,
- skupiny zápasů nebo jiných typů `SportEvent`.
