# Scénáře použití

## UC-01: Kontrola rozvrhu tréninků

**Aktér:** člen výboru nebo trenér

1. Uživatel otevře `/sport/Training/Schedule`.
2. Vybere sezonu, kategorie, typy, lokality a období.
3. Systém zobrazí jednotlivé i propojené bloky v časové ose.
4. Uživatel otevře editaci konkrétního tréninku nebo exportuje výsledek do
   XLSX.

**Výsledek:** uživatel má filtrovaný přehled a může upravit povolené údaje.

## UC-02: Kontrola požadavků na tréninky

**Aktér:** člen výboru

1. Uživatel otevře `/sport/Training/Requirement`.
2. Zvolí sezonu a volitelné kategorie, typy a fáze.
3. Systém zobrazí požadovaný hodinový rozsah a přiřazené trenéry.

**Výsledek:** uživatel získá read-only podklad pro plánování sezony.

## UC-03: Správa trenéra

**Aktér:** přihlášený interní uživatel

1. Uživatel otevře `/hr/Coach/Index` a vybere trenéra.
2. Na samostatných záložkách upraví základní údaje, fotografii, nastavení,
   licence nebo smlouvy.
3. Každá záložka se validuje a ukládá nezávisle.

**Výsledek:** personální historie zůstává oddělená podle typu údajů.

## UC-04: Archivace měsíční docházky

**Aktér:** přihlášený interní uživatel

1. Uživatel otevře `/hr/Attendance/Index`.
2. Vybere trenéra, rok, měsíc a zdrojový `.xlsx` soubor.
3. Systém ověří soubor, unikátnost období a auditní identitu.
4. Uloží původní soubor bez interpretace buněk.
5. Uživatel může historii filtrovat a soubor znovu stáhnout.

**Výsledek:** existuje auditovaný zdrojový dokument pro daného trenéra a
měsíc.

## UC-05: Evidence zápůjčky

**Aktér:** správce skladu

1. Uživatel vybere položku skladu a člena klubu.
2. Zadá datum vydání a očekávané vrácení.
3. Contract služba ověří stav položky a uloží zápůjčku i auditní pohyb.
4. Při vrácení zápůjčku uzavře a aktualizuje stav položky.

**Výsledek:** aktuální držitel i historie pohybu jsou dohledatelné.
