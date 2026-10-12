# Implementační plán: #22 Design záhlaví

**Issue:** [#22 — Design záhlaví](https://github.com/vasekNaus/SportSys/issues/22)

**Stav:** Implementováno. Záhlaví používá `--color-on-brand-primary`
(stabilní bílá v obou režimech) místo `--color-text-inverse`, jméno uživatele
má vlastní třídu `header-user`, tlačítko Odhlásit/Přihlásit je sjednocené
s ikonovými tlačítky vč. `:focus-visible`, zápatí používá
`--color-text-secondary`. `site.css` přegenerováno, `dotnet build` prošel bez
chyb.

## Cíl

Opravit nečitelné prvky v pravé části červeného záhlaví (informace
o přihlášeném uživateli, tlačítko Odhlásit) a sjednotit jejich vizuální styl
(rámeček, hover, focus) se stávajícími ikonovými tlačítky vpravo. Součástí je
i oprava souvisejícího problému s nedostatečně čitelným textem v zápatí
stránky — v obou případech při zachování korektního vzhledu jak ve světlém,
tak v tmavém režimu.

## Výchozí stav

Záhlaví (`body > header`) má trvale červené pozadí
(`var(--color-brand-primary)`), a to **v obou barevných režimech** — barva
„brand-primary" je v `_vars.scss` definovaná stejně pro `light` i `dark`
(`#d8232a`). Problém je, že jednotlivé prvky uvnitř záhlaví používají barvy,
které jsou navržené pro běžné (světlé/povrchové) pozadí, ne pro trvale červené
pozadí headeru:

- `src/SportSys.Razor/Pages/Shared/_LoginPartial.cshtml:4` — jméno
  přihlášeného uživatele má třídu `text-muted small`
  (`.text-muted { color: var(--color-text-muted); }`,
  `src/SportSys.Razor/Styles/_utilities.scss:19`). `--color-text-muted` je
  šedo-modrá barva určená pro čitelnost na světlém/tmavém povrchu, ne na
  červené.
- `src/SportSys.Razor/Pages/Shared/_LoginPartial.cshtml:6-9` — tlačítko
  Odhlásit má třídu `button secondary`. Modifikátor `.secondary`
  (`src/SportSys.Razor/Styles/_forms.scss:33-38`) nastavuje
  `border-color`/`color: var(--color-brand-secondary)` — tmavě námořní
  modrou, určenou jako sekundární brand barva pro světlé povrchy, ne pro
  červené pozadí.
- Dvě ikonová tlačítka `.btn-theme-toggle` a `.btn-layout-toggle`
  (`src/SportSys.Razor/Styles/_layout.scss:106-123`) už mají vzhled vhodný
  pro červené pozadí (bílá barva, poloprůhledný bílý rámeček, jemný hover),
  ale žádný z headerových prvků (včetně těchto dvou tlačítek) nemá
  definovaný `:focus-visible` stav — klávesový focus tak na červeném pozadí
  není vidět vůbec (dědí se obecné pravidlo z `_forms.scss:29-32`, které
  cílí jen na `.button`, ne na `button` obecně).

**Zjištěný problém v tmavém režimu (nad rámec popisu v issue):** `.site-title`
i oba toggle tlačítka používají `color: var(--color-text-inverse)`
(`src/SportSys.Razor/Styles/_layout.scss:89-97, 107-123`). Token
`--color-text-inverse` je ale definovaný jako *inverze běžné textové barvy
stránky*, ne jako „bílá na brandové červené": ve světlém režimu je `#ffffff`,
ale v **tmavém režimu je přepsaný na `#0a0e1a`** (téměř černá — logické, když
běžný text je v dark módu světlý). Protože červené pozadí headeru se mezi
režimy nemění, v dark módu tak název „SportSys" i obě ikonová tlačítka
vykreslují téměř černý text/ikony na červeném pozadí.

Naměřené kontrastní poměry (WCAG 2.1, požadavek AA pro běžný text ≥ 4.5:1):

| Prvek | Barvy | Kontrast | Stav |
|---|---|---|---|
| Jméno uživatele (`text-muted` na červené) | `#6c757d` / `#d8232a` | 1.07:1 | Nevyhovuje |
| Odhlásit (`brand-secondary` na červené, light) | `#292d78` / `#d8232a` | 2.41:1 | Nevyhovuje |
| `.site-title` / ikonová tlačítka v **dark módu** (`text-inverse` na červené) | `#0a0e1a` / `#d8232a` | 3.85:1 | Nevyhovuje |
| `.site-title` / ikonová tlačítka v **light módu** (bílá na červené, referenční) | `#ffffff` / `#d8232a` | 5.01:1 | Vyhovuje |
| Zápatí, text (`text-muted` na `bg-base`, light) | `#6c757d` / `#f1f3f5` | 4.22:1 | Těsně nevyhovuje |
| Zápatí, text (`text-muted` na `bg-base`, dark) | `#6b7a96` / `#0a0e1a` | 4.45:1 | Těsně nevyhovuje |

Zápatí (`body > footer`, `src/SportSys.Razor/Styles/_layout.scss:257-271`)
používá stejný token `--color-text-muted` jako text pro `&copy; … Ochrana
soukromí`. V obou režimech je kontrast těsně pod hranicí 4.5:1 požadovanou pro
běžný text velikosti `.875rem` — to odpovídá popsanému problému s nečitelným
zápatím.

## Potvrzené požadavky a rozhodnutí

Z těla issue (jediný zdroj zadání, bez komentářů):

1. Informace o uživateli: text i ikona → bílá.
2. Odhlásit: bílý text, bílý/poloprůhledný rámeček, jemné bílé poloprůhledné
   pozadí při hoveru.
3. Sjednotit rámeček, hover a focus tlačítka Odhlásit se dvěma ikonovými
   tlačítky vpravo (`.btn-theme-toggle`, `.btn-layout-toggle`).
4. Samotné bílé ikony (theme/layout toggle) jsou v pořádku — neměnit jejich
   vzhled, jen styl rámečku/hoveru/focusu sjednotit s Odhlásit.

Technická rozhodnutí (odvozená z architektury barevných tokenů, beze změny
významu existujících proměnných mimo header/footer):

- Pro vše, co trvale sedí na `var(--color-brand-primary)` (header), použít
  token **`--color-on-brand-primary`** místo `--color-text-inverse`.
  `--color-on-brand-primary` je definovaný jako stabilní bílá v light i dark
  módu (`_vars.scss`, oba bloky `:root`/`[data-theme]`), protože vyjadřuje
  „barva textu NA brandové červené", ne „inverze vůči běžnému textu stránky".
  Tím se oprava zároveň postará o zjištěný dark-mode problém se `.site-title`
  a toggle tlačítky.
- Pro jméno uživatele zavést novou třídu `header-user` místo přetěžování
  globální `.text-muted` — `.text-muted` zůstává beze změny pro použití na
  běžných (světlých/tmavých povrchových) plochách mimo header.
- Pro „Odhlásit" nepřidávat novou obecnou variantu tlačítka do `_forms.scss`
  (nebylo by to obecně použitelné jinde), ale scopovat přepis barev pod
  `body > header .header-actions .button.secondary` — zachová se struktura
  markupu (`class="button secondary"`) i zarovnání s obecným stylem tlačítek.
- Zápatí: nahradit `--color-text-muted` tokenem **`--color-text-secondary`**
  (kontrast 7.35:1 light / 9.19:1 dark — bezpečně nad AA), beze změny
  struktury nebo barvy odkazu při hoveru (`--color-brand-primary` zůstává).

Žádná z otevřených otázek nemění veřejné chování, datový model ani rozsah
migrace — jde výhradně o barvy v CSS. Není tedy potřeba se ptát uživatele;
plán používá existující barevné tokeny beze změny jejich definic.

## Technický návrh

Změny jsou omezené na `SportSys.Razor` (view + SCSS), beze změny
Contract/Database vrstvy. Dotčené vrstvy: pouze prezentace.

### 1. `_LoginPartial.cshtml`

Přejmenovat třídu u `<span>` se jménem uživatele z `text-muted small` na
`header-user small`, aby neslo header-specifický sémantický název místo
reuse globální „muted" třídy. Markup tlačítka Odhlásit (`class="button
secondary"`) zůstává beze změny — barvy se přepíšou scopovaným CSS pravidlem.

### 2. `_layout.scss` — záhlaví

V bloku `body > header`:

- `.site-title { color: var(--color-text-inverse); }` →
  `color: var(--color-on-brand-primary);`
- `.btn-theme-toggle, .btn-layout-toggle { color: var(--color-text-inverse);
  }` → `color: var(--color-on-brand-primary);`
- Doplnit do `.header-actions`:
  - `.header-user` — `color: var(--color-on-brand-primary)`, jemné ztlumení
    přes `opacity: .85` (zachová vizuální hierarchii „méně důležitá
    informace" bez použití barvy nečitelné na červené), ikona dědí barvu.
  - `.button.secondary` (scope `body > header .header-actions`) — přepsat na
    `background: none; border: 1px solid rgba(255,255,255,.30); color:
    var(--color-on-brand-primary); box-shadow: none;`, `:hover` →
    `background: rgba(255,255,255,.12); border-color: rgba(255,255,255,.60);`
    — stejné hodnoty jako u `.btn-theme-toggle`/`.btn-layout-toggle`, čímž se
    splní požadavek na sjednocení rámečku a hoveru.
- Sjednotit `:focus-visible` pro všechny tři interaktivní prvky v
  `.header-actions` (`.btn-theme-toggle`, `.btn-layout-toggle`, `.button`
  uvnitř `.header-actions`): `outline: 3px solid
  var(--color-on-brand-primary); outline-offset: 2px;` — obecné pravidlo z
  `_forms.scss` používá `var(--color-border-focus)` (červená), což by na
  červeném pozadí bylo neviditelné.

### 3. `_layout.scss` — zápatí

V bloku `body > footer`: nahradit `color: var(--color-text-muted);` i
vnořené `a { color: var(--color-text-muted); }` tokenem
`var(--color-text-secondary)`. `&:hover { color: var(--color-brand-primary);
}` zůstává beze změny.

### 4. Build CSS

Po úpravě SCSS je nutné přegenerovat `wwwroot/css/site.css`:

```powershell
Set-Location src\SportSys.Razor
npm run build:css
```

## Implementační kroky

### Fáze 1: Záhlaví — pravá část

1. V `_LoginPartial.cshtml` přejmenovat třídu jména uživatele na
   `header-user small`.
2. V `_layout.scss` upravit `.site-title` a `.btn-theme-toggle`/
   `.btn-layout-toggle` na `var(--color-on-brand-primary)`.
3. Přidat pravidla pro `.header-user` a scopovaný přepis `.button.secondary`
   uvnitř `.header-actions`.
4. Přidat sjednocené `:focus-visible` pro všechny tři prvky.

### Fáze 2: Zápatí

5. V `_layout.scss` nahradit `--color-text-muted` → `--color-text-secondary`
   v `body > footer` (text i odkaz).

### Fáze 3: Build a ověření

6. Spustit `npm run build:css` v `src/SportSys.Razor`.
7. Vizuálně ověřit header i footer v light i dark módu (přepínač
   `#themeToggle`), včetně stavu hover a klávesového focusu (Tab) na všech
   třech tlačítkách v headeru.

## Soubory ke změně

- `src/SportSys.Razor/Pages/Shared/_LoginPartial.cshtml` — přejmenování třídy
  jména uživatele.
- `src/SportSys.Razor/Styles/_layout.scss` — barvy `body > header` (site
  title, toggle tlačítka, nová pravidla `.header-user` a `.button.secondary`,
  sjednocený `:focus-visible`) a `body > footer` (text-secondary místo
  text-muted).
- `src/SportSys.Razor/wwwroot/css/site.css` — vygenerováno z SCSS, needituje
  se ručně (`npm run build:css`).

## Testy a ověření

Projekt nemá automatizované testy pro CSS/vzhled — ověření je manuální
(viz níže). Lze doplnit kontrolu kontrastu přepočtem WCAG poměru pro finální
barvy (viz tabulka ve Výchozím stavu) jako součást code review.

## Manuální akceptace

1. **Light mód:** jméno uživatele a ikona jsou bíle čitelné na červeném
   pozadí; tlačítko Odhlásit má bílý text a poloprůhledný bílý rámeček
   vizuálně shodný s ikonovými tlačítky; hover na Odhlásit zesvětlí pozadí
   stejně jako u ikonových tlačítek.
2. **Dark mód:** přepnutím `#themeToggle` ověřit, že název „SportSys" i obě
   ikonová tlačítka zůstávají bíle čitelná na červeném pozadí (dříve se
   propadala do téměř černé).
3. **Klávesový focus:** po projetí Tab přes header (jméno uživatele se
   nefokusuje, je to jen text) jsou na ikonových tlačítkách i na Odhlásit
   vidět shodné bílé obrysy focusu v obou režimech.
4. **Zápatí:** text copyrightu a odkaz „Ochrana soukromí" jsou zřetelně
   čitelné v light i dark módu; odkaz při hoveru zčervená stejně jako dříve.

## Beze změny

- Vzhled a chování dvou ikonových tlačítek samotných (zůstávají bílá ikona,
  jen se sjednotí rámeček/hover/focus napříč třemi prvky).
- `--color-text-inverse`, `--color-text-muted` a `.text-muted` jako obecné
  tokeny/třídy pro použití mimo header/footer.
- Markup `_LoginPartial.cshtml` mimo přejmenování jedné CSS třídy.
- Contract a Database vrstva — čistě prezentační změna.

## Mimo rozsah

- Případná navigace (`body > nav`) a ostatní části layoutu neřeší issue #22
  a nejsou touto změnou dotčené.
- Globální přejmenování nebo redefinice tokenu `--color-text-inverse` v
  `_vars.scss` — token zůstává zachován pro svůj současný účel (inverze vůči
  běžnému textu stránky), header pouze přestává tento token nesprávně
  používat.

## Hotovo, když

- Header v obou režimech (light/dark) zobrazuje jméno uživatele, ikonu a
  tlačítko Odhlásit bíle čitelně na červeném pozadí s kontrastem ≥ 4.5:1.
- Tlačítko Odhlásit má vizuálně shodný rámeček, hover a focus se dvěma
  ikonovými tlačítky vpravo.
- Zápatí má kontrast textu ≥ 4.5:1 v obou režimech.
- `wwwroot/css/site.css` je přegenerováno (`npm run build:css`) a
  odpovídá upravenému SCSS.
