# Přehled systému SportSys

## Účel

SportSys je interní informační systém hokejového klubu. Centralizuje agendu,
kterou stávající rezervační a účetní systémy nepokrývají společně, a poskytuje
jedno místo pro sportovní evidenci, personalistiku a sklad.

## Uživatelé

- členové výboru a administrátoři,
- trenéři a pracovníci připravující sportovní podklady,
- správci majetku a výstroje.

## Aktuální rozsah

| Oblast | Aktivní schopnosti |
|---|---|
| Sport | Rozvrhy, plány, požadavky, editace a XLSX export tréninků |
| Personalistika | Trenéři, nastavení, licence, smlouvy a archiv docházky |
| Sklad | Majetek, výstroj, kategorie, umístění a zápůjčky |
| Identita | Entra ID, lokální Identity fallback a systémové role |
| Importy | Pomocné dávkové operace v `SportSys.ConsoleApp` |

## Integrační hranice

Rezervační systém sportoviště a účetnictví jsou externí systémy. SportSys je
nenahrazuje. Přístup k rezervacím `plan.*` je navržen jako read-only; aktuální
`SportSysDbContext` tyto modely neobsahuje.

## Zdroje aktuálního stavu

- [Architektura](architecture.md)
- [Konvence](conventions.md)
- [Modulová dokumentace](modules/)
- [Architektonická rozhodnutí](decisions/README.md)
- [Přehled funkcí](features.md)
- [Scénáře použití](use-cases.md)

`features.md` a `use-cases.md` jsou stručné produktové pohledy. Historické
plány v `.github/tasks/` ani rešerše v `docs/research/` nejsou specifikací
aktuální implementace.
