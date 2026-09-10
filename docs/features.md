# Přehled aktuálních funkcí

Tento dokument je produktový rozcestník. Technické invarianty a přesné cesty
jsou v modulové dokumentaci.

| Modul | Aktuální funkce | Podrobnosti |
|---|---|---|
| Sport | Rozvrh reálných tréninků, týdenní plán, požadavky, editace, XLSX export a správa číselníků | [sport.md](modules/sport.md) |
| Personalistika | Trenéři, fotografie, časová nastavení, licence, smlouvy a měsíční archiv docházky | [hr.md](modules/hr.md) |
| Sklad | Evidence výstroje a majetku, kategorie, výrobci, umístění, zápůjčky a auditní pohyby | [inventory.md](modules/inventory.md) |
| Identita | Entra ID, lokální Identity fallback, provisioning a systémové role | [auth.md](modules/auth.md) |
| Frontend | Razor Pages, vlastní SCSS, design tokeny, EditorTemplates a Font Awesome | [frontend.md](modules/frontend.md) |
| Dávkové operace | Importní a pomocné příkazy v `SportSys.ConsoleApp` | [architecture.md](architecture.md) |

Funkce, které nejsou implementované v kódu ani přijaté v ADR, patří do GitHub
issues, nikoli do tohoto přehledu.
