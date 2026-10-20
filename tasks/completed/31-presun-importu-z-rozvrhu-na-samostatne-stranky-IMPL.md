# Implementace: #31 Přesun importů z rozvrhu na samostatné stránky

## Provedené změny

- Přesunuty formuláře, jejich handlery a výsledkové zprávy na samostatné stránky:
  `/sport/Training/Import/Trainings` a `/sport/Training/Import/Matches`.
- Zachovány stávající validace uploadu, zpracování importních chyb, výsledné
  souhrny a limit 20 zobrazených varování.
- Ze stránky rozvrhu byly importní formuláře odstraněny a nahrazeny odkazy na
  nové stránky.
- Na obě stránky bylo doplněno tlačítko „Zpět na rozvrh“; breadcrumb byl
  následně odebrán podle novějšího komentáře k issue #31.
- Aktualizován `docs/modules/sport.md`.
- Přidán sdílený `KisImportFileValidator` a testy případu chybějícího,
  prázdného, nepodporovaného a příliš velkého souboru i platného XLSX.
- Databázový model ani migrace nebyly změněny.

## Ověření

- `dotnet build src\SportSys.Razor\SportSys.Razor.csproj -c Release --no-restore`
  — úspěch; zůstává existující upozornění NU1510 v `SportSys.Contract`.
- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --no-restore`
  — nelze dokončit kvůli kompilačním chybám v existujícím
  `CoachAttendanceTests.cs`: test odkazuje na neexistující
  `CoachAttendanceImportRow.DurationMinutes` a používá konstruktor s
  neplatným počtem argumentů.
- `KisImportFileValidatorTests` — cíleně spuštěno 6 testů, všechny prošly.
  Při cíleném běhu byla dočasně vyloučena pouze nekompilující třída
  `CoachAttendanceTests`; dočasná konfigurace byla odstraněna.
- `dotnet build SportSys.slnx -c Release --no-restore`
  — stejná blokující chyba v testovacím projektu; aplikační Razor projekt se
  v rámci buildu sestavil.
