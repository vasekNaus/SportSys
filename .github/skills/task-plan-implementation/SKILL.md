---
name: task-plan-implementation
description: >
  Implementuje schválený plán z .github/tasks end-to-end: ověří aktuálnost
  rozhodnutí, načte relevantní skills a dokumentaci, provede změny po vrstvách,
  doplní testy a zkontroluje soulad výsledku s plánem. Použij při požadavku
  "implementuj @.github/tasks/...".
user-invocable: true
---

# Implementace task plánu

## Kdy použít

- Uživatel odkáže na konkrétní soubor v `.github/tasks/` a požaduje implementaci.
- Schválený plán zasahuje více vrstev nebo obsahuje navazující fáze.
- Je nutné udržet shodu mezi plánem, kódem, testy a dokumentací.

## Předpoklady

- Task soubor existuje a obsahuje cíl, rozhodnutí, kroky a akceptaci.
- Pracovní strom může obsahovat uživatelské změny, které se nesmí vracet.
- Projektové instrukce a relevantní modulová dokumentace jsou dostupné.

## Postup

### 1. Načti celý plán

Přečti celý task soubor včetně rozhodnutí, rozsahu, testů a sekce mimo rozsah.
Nevycházej pouze z názvu nebo checklistu.

### 2. Ověř aktuálnost

Porovnej plán s aktuálním kódem, ADR a dokumentací. Novější explicitní
rozhodnutí uživatele mají přednost před taskem. Pokud si dvě části plánu
odporují, použij pořadí:

1. novější rozhodnutí uživatele nebo issue komentář,
2. explicitní technické rozhodnutí v plánu,
3. akceptační kritéria,
4. starší popisné pasáže.

Rozpor, který zásadně mění data, bezpečnost nebo veřejné chování, vyjasni před
implementací. Běžné technické rozhodnutí vyřeš podle existujících vzorů.

### 3. Načti cílený kontext

Použij routovací tabulku v `.github/copilot-instructions.md`. Aktivuj všechny
skills, které odpovídají změně, například pro novou EF entitu, lookup tabulku,
pojmenovaný default constraint nebo EditorTemplates.

Vyhledej existující obdobné řešení dříve, než vytvoříš nový helper nebo vzor.

### 4. Rozděl implementaci podle závislostí

Typické pořadí:

1. databázový model a konfigurace,
2. Contract DTO, validace a služby,
3. registrace služeb,
4. Razor Page, navigace a SCSS,
5. dokumentace,
6. cílené testy.

Pořadí změň, pokud plán nebo architektura vyžadují jinou závislost.

### 5. Implementuj úplné vertikální řešení

- Zachovej vrstvy a veřejné kontrakty projektu.
- Nevracej uživatelské změny mimo scope.
- Řeš chybové stavy explicitně a podle vzoru modulu.
- Při změně chování doplň test, pokud existuje odpovídající testovací projekt.
- Aktualizuj pouze kanonickou dokumentaci; task ponech jako historický plán.

### 6. Ověř výsledek proti plánu

Nevystač s úspěšným buildem. Projdi akceptační kritéria a ověř:

- datové invarianty a bezpečnostní hranice,
- registraci a navigaci,
- chování validace a chybových stavů,
- že Razor nezískal přímou závislost na Database,
- že nebyla vytvořena nebo upravena migrace,
- že dokumentace popisuje skutečný výsledný stav.

### 7. Proveď nezávislou kontrolu

U větší změny použij code review nebo rubber-duck kontrolu zaměřenou na
logické chyby, bezpečnost a mezní stavy. Nálezy oprav a znovu spusť nejmenší
relevantní sadu testů.

## Omezení

- Nevytvářej ani neupravuj EF Core migrace nebo model snapshot.
- Neimplementuj body označené jako mimo rozsah.
- Nepovažuj historický task za přesnější zdroj než aktuální kód a ADR.
- Neměň izolovaný prototyp nebo nesouvisející modul bez explicitního důvodu.
- Nezakrývej databázové ani validační chyby obecným úspěšným výsledkem.

## Checklist

- [ ] Celý task byl přečten.
- [ ] Rozpory a novější rozhodnutí byly vyhodnoceny.
- [ ] Relevantní dokumentace a skills byly načteny.
- [ ] Změna respektuje vrstvy projektu.
- [ ] Všechny požadované plochy jsou propojené.
- [ ] Testy pokrývají změněné chování a mezní stavy.
- [ ] Navigace, DI a dokumentace odpovídají implementaci.
- [ ] Nebyla vytvořena ani změněna EF Core migrace.
- [ ] Akceptační kritéria byla ověřena jednotlivě.
- [ ] Nezávislá kontrola nemá nevyřešené závažné nálezy.

## Příklady

```text
Implementuj @.github/tasks/12-export-treninku-do-excelu.md
```

Pokud uživatel doplní výjimku, zachovej ji jako vyšší prioritu:

```text
Implementuj @.github/tasks/12-export-treninku-do-excelu.md;
autorizaci zatím neměň a nevytvářej migraci.
```
