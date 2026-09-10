# ADR-003: TPC se sdílenými sekvencemi

- **Status:** Accepted
- **Datum:** 2026-09-09
- **Rozhodující:** správce projektu

## Kontext a problém

Sportovní události a skladové položky mají společné vlastnosti, ale jednotlivé
konkrétní typy potřebují vlastní fyzické tabulky. Jejich identifikátory přitom
musí být unikátní napříč celou hierarchií.

## Zvažované varianty

1. TPH s jednou tabulkou a discriminator sloupcem.
2. TPT se společnou tabulkou předka.
3. TPC se samostatnými tabulkami a sdílenou databázovou sekvencí.

## Rozhodnutí

Hierarchie `SportEvent -> Training / Match` a
`InventoryItem -> Equipment / Asset` používají TPC. Každá hierarchie má
sdílenou SQL Server sekvenci, která zajišťuje unikátní ID napříč konkrétními
tabulkami.

Vazby na abstraktní `InventoryItem` nemohou mít jeden databázový FK, protože
tabulka předka neexistuje. Jejich referenční integritu ověřují Contract služby.

## Důsledky

### Pozitivní

- Konkrétní tabulky neobsahují nerelevantní nullable sloupce.
- ID lze bezpečně používat jako společný identifikátor hierarchie.
- Dotazy na konkrétní typ nevyžadují join na tabulku předka.

### Negativní

- Některé FK nelze vynutit na úrovni SQL Serveru.
- Aplikační služby musí explicitně kontrolovat existenci cílové položky.
- Změna hierarchie vyžaduje koordinovanou změnu konfigurace a migrace.

## Reference

- `src/SportSys.Database/Configurations/sport/`
- `src/SportSys.Database/Configurations/inventory/`
- `docs/modules/inventory.md`
- `docs/modules/sport.md`
