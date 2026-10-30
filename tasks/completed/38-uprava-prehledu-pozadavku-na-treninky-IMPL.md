# Implementace: #38 Úprava přehledu požadavků na tréninky

**Issue:** [#38](https://github.com/vasekNaus/SportSys/issues/38)

**Datum:** 21. 10. 2026

**Stav:** Implementováno včetně zarovnání podle komentáře z 21. 10. 2026.
Formát hodin `0.##` i zarovnání jsou automaticky ověřené.
Manuální akceptace proti vývojové databázi a kontrola zalamování v prohlížeči
zůstávají neprovedené.

## Provedené změny

- `TrainingRequirementListItem` obsahuje `SeasonCategoryName`;
  `TrainingRequirementService.GetAllAsync` jej projektuje přímo
  ze související `SeasonCategory.Name`.
- Tréninková tabulka zobrazuje název kategorie a záhlaví
  `Sezóna | Kategorie | Typ tréninku | Fáze | Od | Do | Rozsah [h] | Trenéři | Akce`.
- `RequirementCoachGroups.Group` přijímá volitelný parametr `includeHours`.
  Výstupní kolekce má nyní název `CoachDisplayTexts`.
  Tréninková varianta po upřesnění uživatele ze dne 21. 10. 2026 formátuje
  každé přiřazení existujícím `CzechNumberFormat.FormatDecimal` s formátem
  `0.##`: nejvýše dvě desetinná místa, bez koncových nul, například
  `Novák (1,5)`, `Krs (2)` nebo `Nauš (1,25)`.
- Seskupení a pořadí rolí zůstává podle `CoachRoleOrder`, následně ID.
  Příjmení se řadí českým porovnávačem před formátováním hodin.
  Tréninková varianta nededuplikuje texty, takže neztrácí různé trenéry
  se stejným příjmením ani se stejnými hodinami.
- `_CoachGroups.cshtml` přebírá nastavení `IncludeHours`.
  `Index.cshtml` jej předává pouze tréninkům v samostatné kopii
  `ViewDataDictionary`; sdílená data stránky nemění.
- Zápasová tabulka nadále používá kód kategorie, dosavadní záhlaví
  a příjmení bez hodin včetně původní deduplikace.
- Zachovány jsou osobní číslo při prázdném jméně, znak `-` bez trenérů
  a standardní HTML kódování jmen i názvů rolí.
- Aktualizována sekce požadavků v `docs\modules\sport.md`.
- Doplněno zarovnání podle
  [komentáře issue](https://github.com/vasekNaus/SportSys/issues/38#issuecomment-6309561032):
  všechna záhlaví na střed; sezóna, fáze, data a akce na střed, rozsah
  hodin doprava a ostatní hodnoty vlevo. Použity jsou existující utility
  `c` a `r`, akční buňka má `buttons c`. Zápasová tabulka, globální grid
  ani SCSS se nemění.

## Automatické ověření

Příkazy:

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --filter "FullyQualifiedName~TrainingRequirementTests|FullyQualifiedName~CzechNumberFormatTests|FullyQualifiedName~CoachAssignmentQueryTests"
dotnet build SportSys.slnx
```

Po doplnění zarovnání a aktualizaci očekávání na formát `0.##` prošel cílený
výběr s `--no-restore`: **49 úspěšných testů, 0 selhání, 0 přeskočených**.
Build celého řešení s `--no-restore` prošel bez chyb.
Kompletní sada Razor testů následně prošla s `-c Release --no-build --no-restore`:
**298 úspěšných testů, 0 selhání, 0 přeskočených**.

Původní rozšíření `TrainingRequirementTests` pokrylo:

- český formát i při kultuře `en-US`;
- české řazení příjmení, pořadí rolí a samostatné hodiny stejného trenéra
  ve více rolích;
- různé trenéry shodného příjmení s odlišnými i shodnými hodinami;
- náhradní osobní číslo, prázdná přiřazení a původní zápasovou deduplikaci;
- vykreslení skutečného kompilovaného partialu pomocí `IRazorViewEngine`:
  hodiny v tréninkovém režimu, zápasový režim bez hodin,
  izolaci lokálního přepínače, prázdný výstup `-` a HTML kódování.

Očekávání testů nyní používají `0`, `0,5`, `1`, `1,25`, `1,5`, `2` a `999,99`,
včetně příkladů seskupování a vykreslování. Produkční formátování hodin se
při této úpravě neměnilo.

Test `RequirementViews_KeepTrainingFormattingAndAlignmentSeparateFromMatches`
vykresluje také celou kompilovanou stránku `Index.cshtml` s testovacím
PageModelem a korektním `PageContext`, popisem stránky a endpointem.
Ověřuje devět centrovaných záhlaví, přesnou matici tříd datových buněk
i nezměněné třídy všech sloupců zápasové tabulky. Databáze se nepřipojuje.

Kontrola diffu potvrdila propojení názvu kategorie od projekce přes DTO
do první tabulky a nezměněnou logiku filtrů i editovacích akcí.
`git diff --check` prošel.

Build nadále hlásí existující upozornění na závislosti
(`NU1510`, `NU1902`, `NU1903`), upozornění `CS0618` a `EF1002` v Apollo
a nullable upozornění `CS8619` v `SearchMultiSelectTagHelperTests.cs`.
Nesouvisející závislosti ani tyto soubory nebyly měněny.

## Rozsah a zbývající manuální ověření

Nevznikly nové závislosti ani změny DbContextu, databázového modelu,
migrací, snapshotu, autorizace, DI, stylů nebo prototypu `SportSys.Web`.
Formát celkového `DurationHours`, data, filtry a editace zůstávají původní.

Nebylo provedeno načtení celé stránky proti skutečné vývojové databázi
ani vizuální ověření v prohlížeči. Automatický test vykresluje skutečný
partial i celou stránku s testovacími daty, nikoli HTTP požadavek
s databázovou projekcí ani výsledné rozložení v prohlížeči.
Pro úplnou manuální akceptaci zbývají scénáře z archivovaného plánu,
zejména kontrola skutečného názvu kategorie, filtrů a zalamování na užší
obrazovce. Žádná produkční ani testovací databázová data nebyla upravena.
