# Implementace: #8 Evidence docházky trenérů

## Výsledek

Původní archivace XLSX byla rozšířena na atomický import účasti trenéra na
existujících trénincích a zápasech. Původní soubor se uloží pouze tehdy, když
parser i párování všech řádků uspějí.

## Provedené změny

- Přidána entita `sport.CoachMatch` se složeným klíčem, explicitními FK indexy
  a pojmenovaným default constraintem `DF_CoachMatch_Note`.
- Přidány navigace na trenéra, zápas a stav účasti a nový `DbSet`.
- `CoachAttendanceXlsxValidator` byl nahrazen `CoachAttendanceXlsxParser`.
- Parser ověřuje soubor, list `Export`, přesnou hlavičku, česká data a časy,
  délku tréninku, povinná pole, délku poznámky a koncové souhrny.
- Přidán čistý `CoachAttendanceImportPlanner` pro párování kategorií, stavů,
  tréninků a zápasů a pro detekci duplicitních řádků.
- `CoachAttendanceService.UploadAsync` zapisuje účasti i archiv jedním
  `SaveChangesAsync` v serializovatelné transakci.
- Existující účast se vloží, aktualizuje nebo nahradí změněným stavem; události
  neuvedené ve vstupu se nemažou.
- UI nově vyžaduje sezonu, zobrazuje agregované chyby a po úspěchu souhrn
  importovaných a změněných vazeb.
- Aktualizována dokumentace modulů HR a Sport.

## Ověření

- Vzorový soubor `ZARI.2026_export.dochazky.xlsx` byl parserem načten jako
  69 datových řádků: 63 tréninků a 6 zápasů.
- `dotnet build SportSys.slnx -c Release --no-restore` prošel.
- `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release --no-restore`
  prošel: 165 testů.
- Nezávislá code review kontrola odhalila a následně byly opraveny chyby
  zpracování poškozeného Open XML a delete behavior vazby `CoachMatch.Match`.

## Navazující krok

Agent nevytvořil ani neupravil EF Core migraci nebo model snapshot. Uživatel
musí vytvořit migraci pro `sport.CoachMatch` a zkontrolovat složený PK, tři
FK, indexy, kaskádové mazání vazby na `Match` a pojmenovaný default constraint.
