# Implementace: #27 Import zápasů

**Zdrojový plán:** `tasks/27-import-zapasu.md`

**Stav:** Implementováno, čeká na vytvoření a aplikaci EF Core migrace uživatelem.

## Provedené změny

- Přidán KIS XLSX import přes `KisMatchImportService` a `ClosedXML`.
- Import čte list `Export`, ověřuje sedm očekávaných sloupců, ignoruje
  opakovanou hlavičku a vypisuje varování s číslem neplatného řádku.
- Zápasy páruje podle lokace, data, začátku, kategorie a obou týmů; neznámé
  nebo nejednoznačné reference nezakládá.
- Konec importovaného zápasu určuje `MatchDurationMinutes` kategorie.
- `SeasonCategory` obsahuje administrativně spravované délky utkání a
  rozbruslení v minutách včetně validačních omezení.
- Administrace kategorií nově spravuje také `Name`, který KIS používá pro
  přiřazení hodnoty `Soupiska`.
- Odstraněny CSV import, jeho výsledek, test a historický konzolový prototyp.
- Konzolové spuštění: `dotnet run --project src\SportSys.ConsoleApp -- import-kis <cesta-k-xlsx> <id-sezony>`.

## Neprovedené úmyslně

- EF Core migrace ani model snapshot nebyly vytvořeny nebo upraveny. Uživatel
  musí vytvořit migraci, která bezpečně doplní platné hodnoty pro existující
  `SeasonCategory` před zavedením nenulových sloupců a check constraintů.

## Ověření

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --no-restore
dotnet build SportSys.slnx --no-restore -c Release
```

Oba příkazy prošly; testovací projekt provedl 149 testů.
