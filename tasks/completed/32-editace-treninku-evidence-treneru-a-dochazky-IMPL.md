# Implementace: #32 Editace tréninků – evidence trenérů a docházky

## Provedené změny

- Rozšířeny Contract DTO a `TrainingService` o načítání trenérských přiřazení,
  dostupné trenéry/kategorie a lokalizované stavy docházky.
- Aktualizace přiřazení probíhá ve stávající transakci se sériovou izolací.
  Snapshot pro souběh zahrnuje členy skupiny a všechny hodnoty přiřazení.
- Server ověřuje trenéry, členství tréninku ve skupině, platné stavy, poznámku
  do 50 znaků, duplicity napříč skupinou a zachování všech existujících
  přiřazení. Změna docházky nahradí aktuální stav bez vytváření auditní historie.
- Editační stránka zobrazuje tabulku Trenér/Kategorie/Docházka/Poznámka, umí
  přidat řádek s výchozím stavem „Neúčast“ a znovu zobrazí všechny uložené
  přiřazení i po validační chybě.
- Dokumentace modulu Sport a testy DTO/servisních invariantů byly aktualizovány.
- Databázový model ani migrace se neměnily.

## Ověření

- Prošly cílené testy `TrainingEditDtoTests` a `TrainingServiceTests`: 15 testů.
- Prošla celá testovací sada kromě existujícího souboru
  `CoachAttendanceTests.cs`: 220 testů. Soubor byl při tomto běhu vynechán
  parametrem `DefaultItemExcludesInProjectFolder`, protože nekompiluje:
  používá chybějící `CoachAttendanceImportRow.DurationMinutes` a neplatný
  10-parametrový konstruktor.
- Samostatný build projektů `SportSys.Contract` a `SportSys.Razor` v Release
  konfiguraci prošel; sestavení Razor spustilo i `npm run build:css`.
- `dotnet build SportSys.slnx` je blokován stejnými nesouvisejícími chybami
  v `CoachAttendanceTests.cs`.
- `git diff --check` prošel.
