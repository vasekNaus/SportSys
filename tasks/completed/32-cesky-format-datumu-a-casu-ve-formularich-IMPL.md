# Implementace: Český formát data a času ve formulářích

## Výsledek

Datumová a časová pole v `SportSys.Razor` používají sdílené EditorTemplates
s českým zobrazením (`d. M. yyyy`) a 24hodinovým časem (`HH:mm`). Klientský
kalendář poskytuje flatpickr 4.6.13 s českou lokalizací; serverový model binder
přijímá české varianty i původní ISO formát v GET i POST požadavcích.

## Provedené změny

- Přidán `CzechDateTimeFormat` a model binder/provider pro `DateOnly`,
  `TimeOnly` a odpovídající typy s atributem `DataType.Date` / `DataType.Time`.
- EditorTemplates `Date` a `Time` nyní vykreslují textové vstupy, formátují
  hodnoty česky a zachovávají chybně zadanou hodnotu pro opravu.
- Datumová pole v rozvrhu, plánech, požadavcích, skladových položkách,
  výpůjčkách a údajích trenérů používají sdílenou šablonu. U skladových
  položek jsou doplněny validační zprávy pro obě datumová pole.
- Flatpickr, česká lokalizace a přizpůsobené SCSS styly jsou připojeny
  v layoutu; styly se generují příkazem `npm run build:css`.
- Aktualizována dokumentace frontendových a obecných konvencí a doplněny
  testy formátování i model bindingu.

## Ověření

- `dotnet build src\SportSys.Razor\SportSys.Razor.csproj -c Release --no-restore` — úspěšný.
- `npm run build:css` a `node --check wwwroot\js\site.js` — úspěšné.
- 209 testů prošlo při vynechání `CoachAttendanceTests.cs`.
- Standardní testovací sestavení stále blokují existující chyby v
  `tests\SportSys.Razor.Tests\CoachAttendanceTests.cs`: testy odkazují na
  chybějící `CoachAttendanceImportRow.DurationMinutes` a neodpovídající
  konstruktor. Tento soubor nebyl součástí změny.
- Kontrola zdrojů `SportSys.Razor` nenašla nativní `type="date"`,
  `type="time"`, `type="datetime-local"` ani formátování data
  `ToString("yyyy-MM-dd")`.

Ruční ověření v prohlížeči, včetně anglického prostředí, mobilního zobrazení
a tmavého režimu, nebylo v tomto prostředí provedeno.
