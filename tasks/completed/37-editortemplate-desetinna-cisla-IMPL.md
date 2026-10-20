# Implementace: #37 EditorTemplate - desetinná čísla

**Plán:** [37-editortemplate-desetinna-cisla.md](37-editortemplate-desetinna-cisla.md)

## Provedené změny

- Přidán `CzechNumberFormat` pro české formátování a parsování decimal hodnot.
  Přijímá desetinnou čárku i tečku, toleruje běžné mezery, NBSP a úzké NBSP
  a odmítá neplatné či nejednoznačné zápisy.
- Přidán `CzechDecimalModelBinder` a provider; binder je zaregistrován
  v `Program.cs` a zachovává původní hodnotu při chybě.
- Přidán `Decimal.cshtml`: textový vstup s `inputmode="decimal"`, čárkovým
  formátem a možností dodat atributy specifické pro formulář.
- Decimal pole `Hours`, `DurationHours`, `AcquisitionPrice` a `RewardAmount`
  převedena na EditorTemplate.
- `TrainingRequirementEditDto` nově odmítá `DurationHours` s více než dvěma
  desetinnými místy; existující validace přesnosti `Hours` zůstala zachována.
- Doplněny testy parsování, formátování, binderu, provideru a přesnosti
  `DurationHours`.
- Aktualizována frontendová dokumentace, obecné konvence a dokumentace skillu
  EditorTemplates.

## Ověření

- `dotnet build SportSys.slnx --no-restore` — úspěšně, 0 chyb.
- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --no-restore`
  — úspěšně, 268/268 testů.
- `git diff --check` — bez chyb.
- Zkontrolováno, že zbylé `type="number"` vstupy jsou celočíselná pole.
- Neprovedl jsem ruční test ve spuštěné aplikaci. Build hlásí existující
  NuGet advisory warnings a preview .NET SDK; žádné závislosti se neměnily.
- V rámci této implementace nebyly vytvořeny ani upraveny EF Core migrace
  nebo snapshot.

