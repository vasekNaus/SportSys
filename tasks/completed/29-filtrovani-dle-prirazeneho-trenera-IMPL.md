# Implementace: #29 Filtrování dle přiřazeného trenéra

## Provedené změny

- Na stránkách Tréninkové plány, Rozvrh tréninků a zápasů a Požadavky je
  vyhledatelný vícenásobný filtr **Trenéři** přes `SearchMultiSelectTagHelper`.
- PageModely načítají možnosti trenérů přes Contract služby a normalizují
  opakovaná i neznámá ID před dotazováním.
- Contract dotazy filtrují plány, tréninky, zápasy a oba typy požadavků podle
  libovolného vybraného trenéra (OR). Prázdný výběr filtr neuplatní.
- Plány se zahrnou pouze tehdy, když přiřazení vybraného trenéra časově
  překrývá platnost plánu.
- GET export rozvrhu používá stejný výběr trenérů jako zobrazený rozvrh.
- Dokumentace modulu SportSys popisuje nové filtry. Testy ověřují logiku OR,
  prázdný výběr, intervalovou platnost plánu a překlad všech dotazů SQL
  Serverem.

## Ověření

- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --no-restore`
  — 171 testů prošlo.
- `dotnet build SportSys.slnx -c Release --no-restore --no-incremental`
  — build řešení prošel bez chyb.
- Standardní Debug build byl blokován tím, že spuštěné Visual Studio drželo
  výstupní DLL; kompletní Release build proběhl úspěšně.

## Databázové změny

Pro filtrování zápasů byla použita již přítomná entita `CoachMatch`. Tato
implementace nevytvořila ani neupravila migraci nebo model snapshot.
