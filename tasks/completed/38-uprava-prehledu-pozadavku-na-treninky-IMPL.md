# Implementace: #38 Úprava přehledu požadavků na tréninky

**Issue:** [#38](https://github.com/vasekNaus/SportSys/issues/38)

**Datum:** 21. 10. 2026

**Stav:** Implementováno. Původní automatické ověření proběhlo před následnou
změnou formátu hodin na `0.##`; tato změna nebyla znovu otestována.
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

## Automatické ověření

Příkazy:

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --filter "FullyQualifiedName~TrainingRequirementTests|FullyQualifiedName~CzechNumberFormatTests|FullyQualifiedName~CoachAssignmentQueryTests"
dotnet build SportSys.slnx
```

Build celého řešení prošel bez chyb. Po doplnění testu vykreslování byl
stejný cílený výběr zopakován s `--no-restore`: **49 úspěšných testů,
0 selhání, 0 přeskočených**. Tento výsledek pochází z doby před změnou
formátu hodin na `0.##`, nikoli z ověření nynějšího formátu.

Původní rozšíření `TrainingRequirementTests` pokrylo:

- český formát i při kultuře `en-US`;
- české řazení příjmení, pořadí rolí a samostatné hodiny stejného trenéra
  ve více rolích;
- různé trenéry shodného příjmení s odlišnými i shodnými hodinami;
- náhradní osobní číslo, prázdná přiřazení a původní zápasovou deduplikaci;
- vykreslení skutečného kompilovaného partialu pomocí `IRazorViewEngine`:
  hodiny v tréninkovém režimu, zápasový režim bez hodin,
  izolaci lokálního přepínače, prázdný výstup `-` a HTML kódování.

Po změně formátu na `0.##` testy stále obsahují očekávání s pevnými
desetinnými místy. Je potřeba je upravit na `0`, `0,5`, `1`, `1,25`, `1,5`,
`2` a `999,99`, včetně příkladů v testech seskupování a vykreslování,
a cílené testy znovu spustit. Tato aktualizace dokumentace testy ani kód
nemění.

Kontrola diffu potvrdila propojení názvu kategorie od projekce přes DTO
do první tabulky a nezměněnou logiku filtrů i editovacích akcí.
`git diff --check` prošel.

Build nadále hlásí existující upozornění na závislosti
(`NU1510`, `NU1902`, `NU1903`) a nullable upozornění `CS8619` v
`SearchMultiSelectTagHelperTests.cs`. Nesouvisející závislosti ani test
nebyly v rámci tohoto issue měněny.

## Rozsah a zbývající manuální ověření

Nevznikly nové závislosti ani změny DbContextu, databázového modelu,
migrací, snapshotu, autorizace, DI, stylů nebo prototypu `SportSys.Web`.
Formát celkového `DurationHours`, data, filtry a editace zůstávají původní.

Nebylo provedeno načtení celé stránky proti skutečné vývojové databázi
ani vizuální ověření v prohlížeči. Automatický test vykresluje skutečný
partial s testovacími daty, nikoli celý HTTP požadavek s databázovou projekcí.
Pro úplnou manuální akceptaci zbývají scénáře z archivovaného plánu,
zejména kontrola skutečného názvu kategorie, filtrů a zalamování na užší
obrazovce. Žádná produkční ani testovací databázová data nebyla upravena.
