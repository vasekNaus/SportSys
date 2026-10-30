---
name: css-pro-tisk
description: >
  Navrhne, upraví a ověří CSS pro tisk HTML stránek SportSys pomocí
  @media print a @page. Použij při požadavku na tiskové styly, tiskovou
  verzi Razor stránky, tisk tabulky či rozvrhu nebo uložení HTML do PDF
  přes tiskový dialog prohlížeče.
user-invocable: true
---

# CSS pro tisk

## Kdy použít

- Přidání nebo oprava tiskových stylů stránky či komponenty.
- Volba mezi tiskem stávajícího HTML a samostatnou tiskovou projekcí.
- Řešení oříznutých tabulek, nečitelných barev nebo chybných zlomů stran.

## Předpoklady

- Přečti [globální instrukce](../../copilot-instructions.md) a
  [frontendové konvence](../../../docs/modules/frontend.md).
- Pro práci s barvami načti skill `barevna-schemata`.
- Prozkoumej cílovou Razor šablonu, její styly, layout a zdroj dat.
- Zjisti požadovaný rozsah dat, formát papíru a cílové prohlížeče.
  Chybí-li zadání, pro běžný výpis navrhni A4 na výšku; změnu obsahu
  nebo rozsahu dat bez opory v zadání nejprve vyjasni.

## Postup

### 1. Ověř současné HTML a data

- Vyhledej existující tisková pravidla a ovládání tisku; rozšiř je místo
  vytváření souběžného řešení.
- Ověř, zda jsou požadovaná data skutečně načtená v dokumentu. CSS
  nedoplní serverové stránky dat, virtualizované řádky ani nenačtené taby.
- Rozliš datový obsah, ovládání, filtry, sbalené sekce a akční sloupce.
  Skrytý filtr nahraď textovým shrnutím období a vybraných podmínek.
- Zkontroluj šířky, výšky, `nowrap`, overflow, pozicování a breakpointy.
  Dotaz bez mediálního typu, např. `@media (max-width: ...)`, může platit
  také při tisku.

### 2. Zvol nejmenší vhodnou změnu

| Podoba obsahu | Výchozí postup |
|---|---|
| Sémantická tabulka nebo detail, požadovaná data v DOM | Tiskové styly stávající stránky |
| Časová osa, překryvy, složité měřítko nebo jiný požadovaný obsah | Zvaž samostatnou tiskovou projekci |

Grid ani flex nejsou samy o sobě důvodem HTML přepisovat. Rozhodni podle
konkrétní geometrie a skutečného tiskového náhledu. Samostatná projekce
nesmí obcházet stávající filtry, validaci ani oprávnění; data načítej přes
Contract, ne přímo z Database.

### 3. Zařaď styly do existujícího buildu

- Měň SCSS v `src\SportSys.Razor\Styles\`, nikoli generovaný
  `wwwroot\css\site.css`. Neměň izolovaný `SportSys.Web`.
- Sdílený tiskový partiál, například `_print.scss`, zařaď přes `@use`
  na konec `site.scss`. Specifická pravidla drž u příslušné komponenty
  podle stávající struktury; neduplikuj je.
- Tisková pravidla uzavři do `@media print`, aby nezměnila obrazovku.
  Pozdější pořadí nepřebije vyšší specificitu, inline styly ani
  `!important`; ověř skutečnou kaskádu.
- Kolidující obrazovkové breakpointy cíleně omez na `screen` nebo
  přepiš v tiskovém režimu. Neupravuj všechny breakpointy mechanicky.

### 4. Definuj papír, layout a kontrast

- Použij `@page` se zvolenou `size` a okraji v `mm`. Orientaci určuj
  například `size: A4 landscape`, ne omezeně podporovaným
  `page-orientation`.
- Cíleně odstraň obrazovkové omezení šířky, odsazení a dekorace.
  Neresetuj globálně overflow, výšky ani pozicování: mohou tvořit
  nezbytnou geometrii komponenty.
- Zajisti čitelný text i při vypnutých pozadích a v tmavém tématu.
  `color-scheme: light` nepřepíše vlastní dark-mode tokeny ani explicitní
  barvy potomků.
- `print-color-adjust: exact` je požadavek, ne garance zachování barev.
  Význam stavu či legendy musí zůstat dostupný i textem nebo symbolem.
- Skrývej jen určené ovládací prvky. Nové `.no-print` apod. musí mít
  odpovídající označení v HTML; nejde o automaticky existující konvenci.
- Akční sloupec skrývej konzistentně v `th` i `td`. Nemá-li záhlaví
  společný selektor, doplň jej nebo dočasně skryj jen obsah akčních buněk.

### 5. Navrhni stránkování podle obsahu

- Zachovej sémantické `table`, `thead`, `tbody` a případné `tfoot`.
  Povol zalamování dlouhých názvů a hlaviček podle typu sloupce.
- Používej moderní `break-before`, `break-after`, `break-inside`.
  Legacy `page-break-*` nejsou ve všech hodnotách záměna 1:1.
- `break-inside: avoid` používej na rozumně malé celky, ne na celý
  dlouhý výpis. Blok vyšší než stránka nelze touto deklarací zachránit.
- Nadpis lze svázat s následujícím obsahem přes `break-after: avoid`.
  `orphans` a `widows` řeší řádky textu, nikoli počet řádků tabulky;
  jejich podporu nepovažuj za jednotnou.
- Opakování `thead` a `tfoot` na dalších stranách ověř skutečným
  výstupem. `display: table-header-group` není univerzální záruka.

### 6. Přidej jen nezbytné rozšíření

- Pro statické HTML preferuj nativní tisk nebo `window.print()`.
  Na dynamická data, fonty a obrázky čekej explicitně před tiskem;
  `beforeprint` automaticky nečeká na asynchronní handler.
- `afterprint` není důkaz úspěšného tisku; používej jej nanejvýš pro
  úklid dočasného UI, ne pro potvrzení dokončení dokumentu.
- Margin boxes (`@top-center`, `@bottom-right`) a stránkové čítače
  zaváděj až po ověření aktuální podpory cílových prohlížečů. Samotná
  podpora `@page` ani úspěšné parsování CSS nestačí.
- Vlastní margin boxes nenahrazují automatické hlavičky prohlížeče.
  Jejich vypnutí řeší tiskový dialog; malé okraje mohou zobrazení
  ovlivnit, ale nejsou přenositelným API pro spolehlivé vypnutí.

### 7. Sestav a ověř skutečný tisk

- Po změně SCSS spusť `npm run build:css` v `src\SportSys.Razor`.
  Zkontroluj také zachování obrazovkového vzhledu.
- DevTools emulaci `print` použij pro ladění selektorů, nikoli jako
  důkaz správné paginace. Ověř skutečný tiskový náhled nebo výsledné PDF.
- Pokryj jednu i více stran, dlouhé texty, vysoký řádek, mnoho sloupců,
  light/dark téma a pozadí zapnutá/vypnutá v cílových prohlížečích.
- Pokud už je dostupná automatizace PDF přes Playwright/Chromium,
  při řízení formátu z CSS použij `preferCSSPageSize: true`.
  Výstup jednoho enginu nedokládá správnost ostatních prohlížečů.
- Chybí-li možnost vizuálního ověření, výslovně uveď tento limit;
  úspěšná kompilace CSS sama nepotvrzuje správný tisk.

## Omezení

- Nepřidávej tiskový framework, PDF službu ani novou závislost bez zadání.
- Neskrývej plošně všechny formuláře, vstupy nebo odkazy: mohou obsahovat
  data. Automaticky nepřipojuj ke všem odkazům `attr(href)`.
- Tiskové skrytí není ochrana dat. Neměň autorizaci ani zpřístupněný obsah
  pod záminkou tiskové úpravy.
- Neslibuj identický fyzický výstup na každé tiskárně a v každém prohlížeči.

## Checklist

- [ ] Rozsah sestavy, filtry a datový zdroj odpovídají zadání.
- [ ] Zvolený layout nevyžaduje plošné destruktivní CSS resety.
- [ ] Tisk je čitelný bez barevného pozadí i při aktivním dark mode.
- [ ] Sloupce záhlaví a těla tabulky zůstaly konzistentní.
- [ ] Zlomy stran a pokračování tabulky ověřeny skutečným náhledem/PDF.
- [ ] SCSS sestaveno a obrazovkový režim zachován, nebo uvedeny překážky.

## Příklady

Před psaním pravidel načti
[omezený tiskový vzor a primární zdroje](reference/print-css.md).
Vzor není univerzální reset ani hotové řešení všech komponent.
