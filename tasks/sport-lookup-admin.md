# Implementační plán: Administrace číselníků Sport

**Stav:** Implementováno v kódu; vytvoření databázové migrace zůstává na uživateli.

## Cíl

Rozšířit `Areas/sport` o konzistentní administraci provozních číselníků:

- `IceRink`
- `Team`
- `Season`
- `SeasonCategory`

Enumové lookup tabulky (`TrainingType`, `TrainingState`, `TrainingPhase`,
`ParticipationType`, `MatchType`) nejsou součástí této etapy.

## Rozhodnutí

- Každý číselník má vlastní `Index` a jediný formulář `Edit` pro přidání i editaci.
- Stránky sdílejí EditorTemplates, vizuální komponenty a jednotný CRUD vzor.
- Razor projekt komunikuje s databází výhradně přes Contract služby.
- Fyzické mazání se nepoužívá. Záznam se zneaktivní pomocí `IsActive`.
- Neaktivní záznam lze znovu aktivovat.
- Výchozí Index zobrazuje aktivní záznamy a nabízí filtr Aktivní / Neaktivní / Vše.
- Zneaktivnění je dostupné z formuláře i ikonou v seznamu.
- Speciální administrační authorization policy se v této etapě nezavádí.

## Fáze 1: Datový model

1. Přidat `IsActive` do `IceRink`, `Team`, `Season` a `SeasonCategory`.
2. Nastavit výchozí hodnotu `true` pomocí pojmenovaného DEFAULT constraintu
   `DF_{Entity}_IsActive`.
3. Neupravovat ručně model snapshot; aktualizuje jej až uživatelem vytvořená migrace.
4. **Nevytvářet ani neupravovat EF Core migrace.** Vytvoření a aplikaci migrace
   provádí výhradně uživatel.

## Fáze 2: Sdílený administrační vzor

1. Připravit společné EditorTemplates pro standardní textová, číselná, datumová,
   výběrová a boolean pole.
2. Vytvořit společné rozložení formuláře s validačním souhrnem a akcemi Uložit,
   Zpět a Zneaktivnit.
3. Zachovat samostatné URL a PageModel pro každý číselník.
4. Používat DataAnnotations na Contract DTO; databázové entity do Razor vrstvy
   nevystavovat.

## Fáze 3: Referenční implementace IceRink

1. Rozšířit `IceRinkDto` o `IsActive`.
2. Upravit `IceRinkService`:
   - filtrování podle textu a stavu,
   - vytvoření a editace,
   - explicitní změna aktivity místo fyzického DELETE.
3. Upravit `Index`:
   - textový filtr podle názvu a města,
   - stavový filtr s výchozí hodnotou Aktivní,
   - indikace stavu,
   - akce Upravit, Zneaktivnit a Aktivovat.
4. Upravit společný `Edit` formulář pro create/edit.
5. Pole `Location` v této etapě nespravovat.

## Fáze 4: Team

Formulář obsahuje `Code`, `Name`, `Address`, `City`, `HomeIceRink` a `IsActive`.
Výběr domácího stadionu načítá aktivní stadiony a zachová aktuálně přiřazený
neaktivní stadion při editaci existujícího týmu.

## Fáze 5: Season

Formulář obsahuje `Name`, `From`, `To` a `IsActive`. Validace vyžaduje
`From <= To`.

## Fáze 6: SeasonCategory

Formulář obsahuje `Season`, `Name`, `Order`, `CompetitionCode`,
`CompetitionTeamName`, `BirthYears` a `IsActive`.

Při editaci jsou části složeného primárního klíče `SeasonId + Name` neměnné.
Při vytvoření nové položky jsou obě hodnoty povinné.

## Fáze 7: Navigace

Přidat sekci Sport do hlavní navigace a odkazy na všechny čtyři administrační
seznamy. Zachovat existující cestu k rozpisu.

## Fáze 8: Aktualizace dokumentace

1. Aktualizovat dokumentaci modulu Sport o spravované číselníky, soft-delete
   chování a význam `IsActive`.
2. Popsat strukturu Index/Edit stránek a odpovědnosti Contract služeb.
3. Doplnit pravidlo, že agent nikdy nevytváří ani neupravuje EF Core migrace.
4. Aktualizovat dokumentaci při každém rozšíření společného administračního vzoru.

