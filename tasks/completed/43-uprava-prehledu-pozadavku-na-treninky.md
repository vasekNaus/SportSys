# Implementační plán: #43 Úprava přehledu požadavků na tréninky

**Issue:** [#43 — Úprava přehledu požadavků na tréninky](https://github.com/vasekNaus/SportSys/issues/43)

**Stav:** Implementováno. Automatické testy a build prošly; manuální
akceptace proti databázi a v prohlížeči nebyla provedena.

**Pozdější upřesnění (26. 10. 2026):** Hodiny trenérů používají formát
`0.##`; desetinná část se zobrazí jen tehdy, když je potřeba, bez koncových
nul. Toto rozhodnutí nahrazuje požadavek issue na vždy dvě desetinná místa.

## Cíl

Rozdělit trenéry v tabulce požadavků na tréninky do samostatných sloupců
podle čtyř hlavních rolí a ponechat sloupec `Trenéři` pro všechny ostatní role.
U každého trenéra zachovat zobrazení jeho evidovaných hodin a stávající
možnost editace požadavku.

## Výchozí stav

Ověřeno proti otevřenému issue bez komentářů a aktuálnímu pracovnímu stromu.

- Stránka `/sport/Training/Requirement` je v
  `src\SportSys.Razor\Areas\sport\Pages\Training\Requirement\Index.cshtml`.
  Tréninková tabulka nyní zobrazuje devět sloupců a trenéry vykresluje v jednom
  sloupci přes partial `_CoachGroups.cshtml`.
- Partial předává přiřazení do `RequirementCoachGroups.Group`. Skupiny řadí
  podle `CoachRole.Order`, následně ID role; trenéry uvnitř skupiny podle
  příjmení českým porovnáváním. Režim `includeHours: true` již zobrazuje
  `Příjmení (hodiny)` s českým formátem `0.##`.
- `TrainingRequirementListItem.CoachAssignments` nese typ
  `TrainingRequirementCoachListItem`, jehož základ
  `RequirementCoachListItem` obsahuje `CoachRoleCode`, `CoachRoleOrder`,
  `LastName` a `Hours`.
- `TrainingRequirementService.GetAllAsync` již projektuje
  `CoachRole.Code`, `CoachRole.Order` a `CoachTrainingRequirement.Hours`.
  Pro samotné rozdělení tabulky není potřeba měnit Contract ani databázový
  dotaz.
- Seed rolí v `src\SportSys.Database\Models\dbo\CoachRole.Seed.cs` mapuje
  kódy `H` = hlavní, `A` = asistent, `B` = brankáři a `O` = obránci.
  `CoachRole.Code` je unikátní a současné DTO již nese jeho hodnotu; pro
  rozhodování se proto použijí tyto stabilní kódy, nikoli lokalizované názvy.
- Zápasová tabulka používá stejný partial bez hodin a má zůstat beze změny.
  Editovací odkaz tréninkového požadavku je součástí stávající tabulky.
- `docs\modules\sport.md` popisuje současný jeden sloupec trenérů a bude
  potřeba popis aktualizovat.

V pracovním stromu jsou již změněné stránky, testy a dokumentace k přehledu.
Plán vychází z jejich aktuální podoby; tyto existující změny se při případné
implementaci nesmí přepsat ani vracet.

## Potvrzené požadavky a rozhodnutí

Požadavky přímo z issue:

1. Tabulka má mít sloupce přesně v pořadí:
   `Sezóna | Kategorie | Typ tréninku | Fáze | Od | Do | Rozsah [h] |
   Hlavní | Asistent | Brankáři | Obránci | Trenéři | Akce`.
2. Sloupce `Hlavní`, `Asistent`, `Brankáři` a `Obránci` obsahují pouze
   trenéry s příslušnou rolí.
3. `Trenéři` zůstává zachován pro všechny role mimo tyto čtyři.
4. U každého trenéra se zobrazí jeho konkrétní
   `CoachTrainingRequirement.Hours` ve formátu `Příjmení (hodiny)` se dvěma
   desetinnými místy. Tento požadavek později nahrazuje výše uvedené
   upřesnění formátu `0.##`.
5. Trenéři v každém sloupci se řadí podle `CoachRole.Order` a poté příjmení.
6. Dosavadní editace požadavku zůstane funkční.

Technická rozhodnutí:

- Klasifikovat podle kódů rolí `H`, `A`, `B`, `O`, které již služba předává
  v `CoachRoleCode`. Nepoužívat zobrazované názvy rolí, jejichž text může být
  lokalizovaný.
- Pořadí čtyř vyhrazených sloupců je pevně dáno issue (`H`, `A`, `B`, `O`),
  nikoli pořadím `CoachRole.Order`; to se použije k řazení položek uvnitř
  sloupců a pro zbývající role.
- Pro formát hodin použít existující české formátování `0.##`
  v `RequirementCoachGroups`; zobrazit desetinnou část pouze tehdy, když je
  hodnota desetinná, bez koncových nul. Toto pozdější rozhodnutí uživatele
  nahrazuje původní požadavek issue na vždy dvě desetinná místa.
- V samostatných sloupcích zobrazit seznam jmen s hodinami bez opakování
  názvu role, který již uvádí záhlaví. Ve sloupci `Trenéři` zachovat
  rozlišení ostatních rolí stávajícím seskupením.
- Databázový model se nemění; nevytvářet ani neupravovat EF Core migraci
  nebo snapshot. Nejsou potřeba nové závislosti ani změna Contract služby.

Otevřené otázky, které by blokovaly přípravu implementace, nebyly zjištěny.

## Technický návrh

### Rozdělení přiřazení a vykreslení

Rozšířit prezentaci `RequirementCoachGroups` tak, aby umožnila vybrat
přiřazení podle `CoachRoleCode` a vytvořit seřazené zobrazované texty
včetně hodin. Čtyři vyhrazené buňky použijí kódy `H`, `A`, `B` a `O`.
Zbytková buňka použije všechny přiřazené role, jejichž kód není v této
čtveřici, a zachová skupiny s názvem role.

Každá buňka bez odpovídajících přiřazení zobrazí `-`. Zpracovat přiřazení
samostatně; neslučovat různé trenéry jen proto, že mají stejné příjmení nebo
stejný počet hodin. Všechny texty nadále vykreslovat standardně HTML
kódované.

### Struktura tabulky a chování stránky

V `Index.cshtml` rozdělit nynější buňku `Trenéři` na pět buněk podle nového
pořadí hlaviček. Výběr čtyř známých rolí a výběr zbývajících rolí musí být
navzájem úplné a nepřekrývat se, aby se přiřazení neztratila ani nezobrazila
dvakrát. Zachovat dosavadní třídy a zarovnání buněk, kde to nová struktura
umožňuje; nepřidávat globální styly ani neměnit zápasovou tabulku.

Editovací odkaz zůstane v posledním sloupci `Akce` a nadále povede na
`./EditTraining` se stejným ID. Filtry, prázdné stavy, sloupec rozsahu,
kategorie a ostatní data řádku se nemění.

## Implementační kroky

### Fáze 1: Prezentace trenérů podle kódů rolí

1. Rozšířit `RequirementCoachGroups` o způsob získání přiřazení pro jeden
   kód role a pro komplementární množinu ostatních rolí.
2. Zachovat řazení podle `CoachRole.Order` a českého porovnání příjmení
   a zobrazovat hodiny z konkrétního přiřazení přes existující formátování.
3. Nezměnit stávající chování helperu v zápasové tabulce, zejména režim bez
   hodin a jeho deduplikaci příjmení.

### Fáze 2: Tréninková tabulka

1. Změnit hlavičku a buňky v `Index.cshtml` na přesných 13 sloupců dle issue.
2. Vykreslit čtyři známé role do samostatných buněk a ostatní role do
   zachovaného sloupce `Trenéři`.
3. Zachovat hodiny, prázdné hodnoty `-`, akční odkaz i všechny existující
   filtry a ostatní tabulky.

### Fáze 3: Testy a dokumentace

1. Doplnit testy rozdělení podle kódů rolí, včetně rolí mimo vyhrazenou
   čtveřici, řazení, hodin a prázdných buněk.
2. Rozšířit test vykreslení stránky o přesná záhlaví a obsah všech pěti
   sloupců trenérů; ověřit také editovací odkaz a nezměněnou zápasovou tabulku.
3. Aktualizovat popis stránky v `docs\modules\sport.md`.
4. Spustit cílené testy Razor a build řešení.

## Soubory ke změně

| Soubor | Změna |
|---|---|
| `src\SportSys.Razor\Models\RequirementCoachGroups.cs` | Výběr a řazení přiřazení podle `CoachRoleCode`, texty s hodinami a výběr zbývajících rolí. |
| `src\SportSys.Razor\Areas\sport\Pages\Training\Requirement\Index.cshtml` | Nová hlavička a vykreslení pěti sloupců trenérů. |
| `tests\SportSys.Razor.Tests\TrainingRequirementTests.cs` | Testy rozdělení, řazení a renderu obou tabulek. |
| `docs\modules\sport.md` | Aktualizace dokumentovaného rozložení tréninkového přehledu. |

`TrainingRequirementService`, DTO, `Index.cshtml.cs`, entity, `CoachRole`
seed, editovací stránky a zápasové DTO není potřeba měnit; potřebné kódy,
pořadí i hodiny jsou již součástí datového toku.

## Testy a ověření

V `TrainingRequirementTests.cs` pokrýt:

- každou vyhrazenou roli: `H` pouze v `Hlavní`, `A` pouze v `Asistent`,
  `B` pouze v `Brankáři` a `O` pouze v `Obránci`;
- přiřazení s jiným i neznámým kódem pouze ve sloupci `Trenéři`;
- žádné duplicitní ani chybějící přiřazení při kombinaci vyhrazených
  a ostatních rolí;
- hodiny konkrétního přiřazení, český desetinný formát a pořadí podle role
  a příjmení; dva trenéři stejného příjmení zůstanou samostatní;
- `-` pro každou prázdnou buňku;
- vykreslenou hlavičku se 13 sloupci ve správném pořadí, obsah každé buňky,
  zachování odkazu na editaci a původní vykreslení zápasové tabulky bez hodin.

Použít stávající xUnit testovací projekt bez nové závislosti. Cílené ověření:

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --filter "FullyQualifiedName~TrainingRequirementTests"
dotnet build SportSys.slnx
```

## Manuální akceptace

1. Na `/sport/Training/Requirement` ověřit přesných 13 záhlaví a jejich pořadí.
2. Na řádku s trenéry všech čtyř vyhrazených rolí ověřit správné sloupce
   a shodu hodin s editací konkrétního přiřazení.
3. Ověřit, že role mimo `H`, `A`, `B`, `O` zůstávají ve sloupci `Trenéři`
   a jsou tam rozlišeny svými názvy rolí.
4. Ověřit příjmení, formát `0.##` bez koncových nul, čárku jako český
   desetinný oddělovač, prázdné buňky `-` a čitelnost při více trenérech.
5. Otevřít editaci z každého řádku a potvrdit, že odkaz a upravovaný
   požadavek zůstaly správné.
6. Zapnout zobrazení zápasů a potvrdit, že zápasová tabulka zůstala
   nezměněná.

## Beze změny

- Datové načítání, filtry, řazení řádků a stránkový model.
- Editační handlery a formuláře požadavků.
- Zápasová tabulka a její současné zobrazení trenérů.
- Databázové entity, schéma, migrace, snapshot a seeding rolí.
- Globální CSS a layout aplikace.

## Mimo rozsah

- Přidávání, přejmenování nebo změna pořadí rolí v databázi.
- Úprava výpočtu, validace nebo ukládání hodin.
- Nové filtry, exporty, responzivní redesign nebo změna editace požadavku.
- Změny `SportSys.Web` a dalších přehledů trenérů.

## Hotovo, když

- [x] Tréninková tabulka má přesných 13 sloupců podle issue.
- [x] Čtyři vyhrazené role se zobrazují výhradně ve svých sloupcích.
- [x] Všechny ostatní role zůstávají ve sloupci `Trenéři`.
- [x] Každý trenér zobrazuje vlastní `Hours`, správně naformátované a seřazené.
- [x] Prázdné buňky zobrazují `-` a žádné přiřazení se neztratí ani neopakuje.
- [x] Editovací tlačítko a zápasová tabulka zachovávají stávající vykreslení.
- [x] Cílené testy i build řešení prošly.
- [x] Dokumentace odpovídá novému rozložení.
- [x] Databázový model ani migrace nebyly změněny.
