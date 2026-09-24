---
name: github-issue-implementation-plan
description: >
  Vytvoří podrobný implementační plán pro SportSys z odkazu na GitHub issue
  a uloží jej do .github/tasks/. Použij tento skill, když uživatel pošle URL
  GitHub issue a požádá o plán, specifikaci, technický návrh nebo přípravu
  implementace, i když zadání obsahuje jen odkaz a krátkou instrukci.
user-invocable: true
---

# Implementační plán z GitHub issue

## Kdy použít

- Uživatel zadá odkaz na GitHub issue a chce implementační plán, specifikaci
  nebo technický návrh.
- Uživatel pošle pouze odkaz na issue a stručný pokyn jako `připrav plán`.
- Je potřeba převést produktové požadavky z issue do konkrétních změn
  v repozitáři SportSys.

Skill vytváří pouze plán. Zdrojový kód podle plánu neimplementuje, pokud o to
uživatel výslovně nepožádá samostatně.

## Předpoklady

- Vstup obsahuje URL GitHub issue.
- GitHub CLI `gh` je přihlášené a má přístup k issue.
- Issue se vztahuje k aktuálnímu repozitáři. Pokud URL míří do jiného
  repozitáře a vazba na aktuální kód není zřejmá, vyžádej si upřesnění.
- Výstupní adresář je vždy `.github/tasks/`.

## Postup

### 1. Načti celé issue

Použij GitHub CLI a načti minimálně číslo, název, popis, komentáře, štítky,
stav a kanonickou URL:

```powershell
gh issue view "<issue-url>" --json number,title,body,comments,labels,state,url
```

- Komentáře považuj za součást zadání. Novější explicitní rozhodnutí mají
  přednost před starším textem issue.
- Neodvozuj požadavky jen z názvu issue.
- Pokud `gh` selže kvůli přihlášení nebo oprávnění, oznam konkrétní překážku;
  nevytvářej plán z neúplných údajů.

### 2. Prozkoumej aktuální stav repozitáře

Podle issue vyhledej a přečti:

- dotčené stránky, služby, modely, konfigurace a testy,
- související dokumentaci z routovací tabulky v
  `.github/copilot-instructions.md`,
- existující obdobnou implementaci nebo sdílený helper,
- relevantní konvence a projektové skills.

Ověř skutečné názvy souborů, typů, metod, rout a projektů. Plán nesmí stavět na
neověřených cestách ani vymyšlených API.

### 3. Vyhodnoť nejasnosti

Rozliš:

- **potvrzené požadavky** — přímo uvedené v issue nebo komentářích,
- **ověřený současný stav** — zjištěný v repozitáři,
- **technická rozhodnutí** — vyplývající z architektury a konvencí,
- **otevřené otázky** — varianty, které významně mění chování, data nebo rozsah.

Na běžná implementační rozhodnutí se neptej; zvol řešení odpovídající
existujícím vzorům. Dotaz uživateli polož pouze tehdy, když různé odpovědi
zásadně mění datový model, veřejné chování, bezpečnost nebo rozsah práce.
Potvrzené odpovědi následně zapracuj do plánu.

### 4. Navrhni úplné řešení

Plán musí:

- řešit kořen požadavku, ne pouze viditelný symptom,
- respektovat vrstvy `Razor → Contract → Database`,
- uvádět přesné soubory a existující symboly, které se mají změnit,
- rozdělit práci do navazujících implementačních fází,
- popsat změny datového toku, validace, chybových stavů a UI chování,
- uvést testy a manuální akceptační scénáře,
- vymezit části beze změny a mimo rozsah,
- výslovně uvést, zda je potřeba změna databázového modelu,
- nikdy neplánovat vytvoření nebo aplikaci EF Core migrace agentem.

Pokud issue vyžaduje novou knihovnu, schéma nebo významné architektonické
rozhodnutí, uveď důvod, alternativy a dopady. Nevymýšlej závislost, pokud lze
použít stávající řešení.

### 5. Ulož plán

Použij cestu:

```text
.github/tasks/{issue-number}-{slug-nazvu}.md
```

Pravidla názvu:

- číslo issue je bez `#`,
- slug je malými písmeny, bez diakritiky,
- slova odděluj spojovníkem,
- odstraň ostatní speciální znaky,
- název má být stručný, ale jednoznačný.

Příklad:

```text
.github/tasks/12-export-treninku-do-excelu.md
```

Pokud soubor pro stejné issue již existuje, nejprve jej přečti a aktualizuj.
Nevytvářej druhý konkurenční plán a nepřepisuj potvrzená rozhodnutí bez opory
v novějším obsahu issue.

### 6. Použij jednotnou strukturu dokumentu

```markdown
# Implementační plán: #{číslo} {název issue}

**Issue:** [#{číslo} — {název}]({url})

**Stav:** Připraveno k implementaci.

## Cíl

## Výchozí stav

## Potvrzené požadavky a rozhodnutí

## Technický návrh

## Implementační kroky

### Fáze 1: ...

## Soubory ke změně

## Testy a ověření

## Manuální akceptace

## Beze změny

## Mimo rozsah

## Hotovo, když
```

Sekce bez relevantního obsahu vynech. U rozsáhlé změny rozděl implementační
kroky podle vrstev nebo závislostí, ne podle náhodného pořadí souborů.

### 7. Zkontroluj plán

Před dokončením ověř:

- soulad s issue včetně komentářů,
- soulad s aktuálním kódem a dokumentací,
- správnost všech uvedených cest,
- pokrytí všech dotčených vrstev a integračních bodů,
- konkrétní a ověřitelné dokončovací podmínky,
- absenci implementačních změn mimo výsledný Markdown plán.

## Omezení

- Nevytvářej ani neupravuj zdrojový kód, konfiguraci aplikace, závislosti nebo
  migrace.
- Neukládej tasky do kořenové složky `tasks/`; vždy použij `.github/tasks/`.
- Nekopíruj issue beze změny. Plán musí doplnit technickou analýzu repozitáře.
- Neoznačuj plán jako implementovaný, dokud implementace skutečně neexistuje
  a nebyla ověřena.
- Nevydávej předpoklad za potvrzený požadavek.
- Nezahrnuj budoucí rozšíření, která issue nevyžaduje.

## Checklist

- [ ] Issue včetně komentářů načteno přes `gh`
- [ ] Relevantní kód, testy a dokumentace prozkoumány
- [ ] Potvrzené požadavky odděleny od technických rozhodnutí
- [ ] Nejasnosti s významným dopadem vyřešeny
- [ ] Uvedeny přesné soubory a symboly
- [ ] Popsány implementační fáze a jejich návaznosti
- [ ] Popsány automatické testy a manuální akceptace
- [ ] Vymezeny části beze změny a mimo rozsah
- [ ] Plán uložen do `.github/tasks/{issue-number}-{slug}.md`
- [ ] Nebyl změněn zdrojový kód ani vytvořena migrace

## Příklady aktivace

```text
https://github.com/vasekNaus/HoSys/issues/12 připrav plán
```

```text
Udělej implementační specifikaci k
https://github.com/vasekNaus/HoSys/issues/12
```

```text
Naplánuj toto issue: https://github.com/vasekNaus/HoSys/issues/12
```

## Reference

- [Microsoft Learn: Skills overview for agents](https://learn.microsoft.com/en-us/microsoft-copilot-studio/agents-experience/skills-overview)
- [Microsoft Learn: Create a skill for an agent](https://learn.microsoft.com/en-us/microsoft-copilot-studio/agents-experience/skills-create)
- `.github/copilot-instructions.md`
- `.github/tasks/`
