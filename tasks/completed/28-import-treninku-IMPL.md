# Implementace: #28 Import tréninků

## Výsledek

- Přidán upload XLSX KIS na `/sport/Training/Schedule` s výběrem aktivní
  sezóny a fáze pro nově vytvářené tréninky.
- `KisTrainingImportService` čte list `Export` přes ClosedXML, validuje
  přesné hlavičky, datum, časy a interval a vrací strukturovaná varování
  s čísly řádků.
- `Led` se mapuje na `Ice` a `Suchá příprava` na `Dry`; neznámé typy,
  nejednoznačné názvy a neplatná data se přeskočí bez zakládání číselníků.
- Import páruje datum, kategorii, lokalitu a typ. Jediná přesná shoda nastaví
  `ConfirmedKis`, jediný kandidát s jiným intervalem přepíše oba časy a
  nastaví `TimeChanged`; bez kandidáta vznikne `KisOnly` bez vazby na plán.
- Rozhodnutí o existenci kandidáta probíhá v serializovatelné transakci, takže
  souběžné importy nevloží stejný trénink dvakrát.

## Ověření

```powershell
dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --no-restore
dotnet build SportSys.slnx --no-restore -c Release
```

Oba příkazy prošly. Testovací projekt obsahuje 165 úspěšných testů. Build
nadále hlásí předexistující upozornění `NU1510` pro
`Microsoft.Extensions.Options`.

## Bez změny

Databázový model, EF Core migrace, konzolový import zápasů, export rozvrhu a
ostatní filtry rozvrhu zůstaly beze změny.
