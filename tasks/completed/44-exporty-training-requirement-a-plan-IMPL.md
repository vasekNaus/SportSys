# Implementace: #44 Exporty na stránkách Training/Requirement a Training/Plan

Implementovány samostatné XLSX exporty požadavků na tréninky, přehledu
tréninkových plánů a tabulky pokrytí na obou stránkách. Exporty používají
aktuálně normalizované filtry stránky, shodné zdrojové řádky jako zobrazené
tabulky, správné názvy souborů a vynechávají akční sloupce.

`TrainingOverviewExcelExporter` používá existující ClosedXML a zapisuje
datumové a číselné hodnoty jako typované buňky. Listy mají tučné záhlaví,
autofilter, zamrznutý první řádek a upravené šířky. Exportní akce se
nezobrazují pro prázdný výsledek; přímé volání handleru zobrazí chybovou
zprávu. PDF export rozvrhu zůstává odložen na žádost uživatele.

Aktualizována modulová dokumentace `docs/modules/sport.md`. Databázový model,
Contract API a migrace nebyly změněny.

## Ověření

- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --no-restore`
- Výsledek: 332 testů úspěšných.
- Build při testech hlásil existující NuGet bezpečnostní advisories pro
  `System.Security.Cryptography.Pkcs` a `System.Security.Cryptography.Xml`,
  preview SDK .NET a nullable warning v `SearchMultiSelectTagHelperTests.cs`;
  tyto nesouvisející problémy nebyly upravovány.
