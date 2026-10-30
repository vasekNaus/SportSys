# Implementace: #43 Úprava přehledu požadavků na tréninky

**Issue:** [#43](https://github.com/vasekNaus/SportSys/issues/43)

**Datum:** 26. 10. 2026

**Stav:** Implementováno. Manuální akceptace proti databázi a v prohlížeči
nebyla provedena.

Po implementaci bylo na žádost uživatele zobrazení hodin změněno z pevných
dvou desetinných míst na formát `0.##`.

## Provedené změny

- Tréninková tabulka má 13 sloupců podle issue, včetně samostatných sloupců
  `Hlavní`, `Asistent`, `Brankáři` a `Obránci`.
- Přiřazení se zařazují podle `CoachRole.Code`: `H`, `A`, `B`, `O`; ostatní
  kódy zůstávají ve sloupci `Trenéři` a jsou nadále seskupené podle názvu role.
- Každé přiřazení zachovává vlastní `CoachTrainingRequirement.Hours`,
  příjmení se řadí podle českého porovnávání a hodiny používají formát `0.##`:
  desetinná část se zobrazí pouze při potřebě, bez koncových nul, například
  `Novák (1,5)` nebo `Krs (2)`.
- Prázdné specializované sloupce zobrazují `-`. Zachováno je samostatné
  vykreslení různých trenérů se shodným příjmením.
- Zachovány jsou existující filtry, zarovnání tabulky, editační tlačítko
  a zápasová tabulka.
- Dokumentace modulu sportu popisuje nové sloupce a formát hodin.
- Databázové entity, Contract projekce, migrace, model snapshot, SCSS
  a generované CSS se neměnily.

## Automatické ověření

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --filter "FullyQualifiedName~TrainingRequirementTests" --no-restore
dotnet build SportSys.slnx --no-restore
```

Cílené testy: **25 úspěšných, 0 neúspěšných**. Build celého řešení:
**úspěšný, 0 chyb**. Build vypisuje existující upozornění na zranitelné
balíčky, preview SDK a nullable upozornění `CS8619` v
`SearchMultiSelectTagHelperTests.cs`.

Testy pokrývají rozdělení rolí včetně nevyhrazených kódů, formát `0.##`,
řazení, prázdné výsledky, obsah a počet sloupců obou vykreslených tabulek
a zachování akčního tlačítka.

## Zbývající manuální ověření

Proti skutečné databázi nebyly ověřeny konkrétní uložené role a hodiny;
výsledné rozložení a čitelnost nebyly kontrolovány v prohlížeči.
