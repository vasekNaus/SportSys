# Implementační plán: #19 Víceúběrová komponenta s fulltextovým vyhledáváním (Tom Select)

**Issue:** [#19 — Víceúběrová komponenta s fulltextovým vyhledáváním](https://github.com/vasekNaus/SportSys/issues/19)

**Stav:** Připraveno k implementaci.

## Cíl

Vytvořit znovupoužitelnou UI komponentu pro víceúběr (`multiselect`) nad
standardním HTML `<select multiple>`, která:

- umí fulltextově hledat v položkách,
- zobrazuje vybrané hodnoty jako tagy/chips s tlačítkem „Odebrat“,
- je ovladatelná klávesnicí,
- zachovává standardní Razor Pages model binding (`asp-for` + `List<int>`),
- nepoužívá jQuery,
- je rozšiřitelná o AJAX načítání položek (mimo rozsah této implementace),
- vizuálně odpovídá ostatním formulářovým prvkům aplikace.

Na základě rešerše (`docs/research` — viz session artefakt citovaný v zadání
issue) je zvolenou knihovnou **Tom Select** (Apache-2.0, bez závislostí,
vestavěné fulltextové/diakriticky necitlivé hledání, plugin `remove_button`
přesně odpovídající požadavku na tag s křížkem).

## Výchozí stav

- V repozitáři neexistuje žádný vlastní TagHelper (`src/SportSys.Razor` má
  pouze `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` ve všech
  `_ViewImports.cshtml` — root i area `sport`/`hr`/`Inventory`/`Identity`).
- Existuje hotový, ale **bez fulltextového hledání**, ruční multiselect widget:
  - `src/SportSys.Razor/wwwroot/js/site.js` — IIFE sekce „Multivýběr“, ovládá
    prvky s atributem `[data-multiselect]` (checkbox list v `<details>`).
  - `src/SportSys.Razor/Styles/_schedule.scss` — třídy `.schedule-multiselect*`.
  - Použití: `Training/Schedule/Index.cshtml`, `Training/Requirement/Index.cshtml`,
    `Training/Plan/Index.cshtml` (filtry) a `Training/Plan/Edit.cshtml`
    (přiřazení trenérů — sdílené pole i fáze 2 seskupená pole po členech,
    issue #18).
- `src/SportSys.Razor/Pages/Shared/_Layout.cshtml` načítá Font Awesome 7 z
  CDN (`cdnjs.cloudflare.com`) bez SRI hashe a žádnou jinou externí JS/CSS
  knihovnu. `~/js/site.js` je vykreslen na konci `<body>` (žádný bundler,
  žádné LibMan, žádné `wwwroot/lib`).
- `src/SportSys.Razor/package.json` slouží výhradně ke kompilaci Sass
  (`sass`, skripty `build:css`/`watch:css`) — žádný JS bundler.
- `_ValidationScriptsPartial.cshtml` je prázdný komentář; skript
  `aspnet-client-validation` je v `_Layout.cshtml` zakomentovaný → klientská
  validace aktuálně neběží, aplikace dělá plné server-side postbacky (žádné
  AJAX částečné překreslování stránky).
- `src/SportSys.Razor/EditorTemplates/Select.cshtml` je existující konvence
  pro `asp-items`-řízené dropdowny (čte `ViewData["{Property}Items"]`,
  vykresluje přes `Html.DropDownList`) — netýká se víceúběru, ale ukazuje styl
  práce s `SelectListItem` v projektu.
- Barevné tokeny jsou v `src/SportSys.Razor/Styles/_vars.scss`
  (`--color-surface-1`, `--color-border-strong`, `--color-border-focus`,
  `--color-text-primary`, `--color-interactive-selected`, `--shadow-sm` atd.,
  light i dark mode). Základní vzhled `<select>`/`<input>` je v
  `src/SportSys.Razor/Styles/_forms.scss` (mixin `textbox`: `border: 1px solid
  var(--color-border-strong)`, `border-radius: 4px`,
  `background-color: var(--color-surface-1)`; focus stav: `outline: 3px solid
  var(--color-border-focus)`).
- Reálný konzument s odpovídajícím datovým modelem (z issue #18):
  `src/SportSys.Contract/Models/TrainingPlanEditDto.cs` —
  `TrainingPlanEditDto.SelectedCoachIds: List<int>`,
  `MemberCoachAssignments: List<TrainingPlanMemberCoachInputDto>` (každý s
  `TrainingPlanId` + `CoachIds: List<int>`),
  `TrainingPlanEditContextDto.AvailableCoaches: IReadOnlyList<CoachSelectItem>`.
  `CoachSelectItem : UserSelectItem` (`src/SportSys.Contract/Models/hr/CoachModels.cs:222`
  definuje `UserSelectItem { Id, DisplayName, Email }`;
  `CoachAttendanceModels.cs` přidává `PersonalNumber`).
- Testovací projekt `tests/SportSys.Razor.Tests` obsahuje pouze čisté unit
  testy modelů/služeb — žádná `WebApplicationFactory`/integrační infrastruktura
  pro vykreslování Razor stránek nebo TagHelperů.

## Potvrzené požadavky a rozhodnutí

- Knihovna: **Tom Select**, verze **2.6.2** (aktuální stabilní v době rešerše),
  soubor `tom-select.complete.min.js` (obsahuje `remove_button` plugin, není
  třeba načítat plugin zvlášť).
- Distribuce assetů: **CDN (jsDelivr), stejný vzor jako Font Awesome**
  v `_Layout.cshtml` — konkrétně `<link>` na `tom-select.css` *se nenačítá*
  (viz níže), `<script>` na `tom-select.complete.min.js` pinovaný na verzi.
  - **Alternativa zvážena a zamítnuta:** zavedení LibMan (`libman.json`) pro
    self-hosting do `wwwroot/lib/tom-select`. Zamítnuto, protože by to byl
    nový nástroj/build krok, který repozitář dnes nikde nepoužívá (žádný
    `libman.json`, žádný `wwwroot/lib`), a projekt už dnes akceptuje stejné
    riziko (závislost na externí CDN) pro Font Awesome. CDN přístup je tedy
    konzistentní s existující konvencí a nepřidává novou infrastrukturu.
  - **Dopad/riziko:** běh komponenty závisí na dostupnosti jsDelivr CDN;
    toto riziko je identické s already-accepted rizikem u Font Awesome.
- CSS: **nenačítáme výchozí `tom-select.css` téma.** Místo toho napíšeme
  vlastní SCSS partial se selektory, které Tom Select generuje
  (`.ts-wrapper`, `.ts-control`, `.ts-dropdown`, `.ts-dropdown .option`,
  `.ts-dropdown .active`, `.item` — chip, `.item .remove` — křížek,
  `.ts-wrapper.focus .ts-control`), navázaný na existující design tokeny
  (`--color-surface-1`, `--color-border-strong`, `--color-border-focus`,
  `--color-text-primary`, `--color-interactive-selected`,
  `--color-interactive-hover`). Důvod: požadavek uživatele na „čisté řešení
  s minimem JS a CSS“ a nutnost sjednoceného vzhledu se zbytkem formulářů
  (vlastní paleta, ne bootstrap/dafault motiv).
- Fulltextové hledání: **lokální (client-side), bez AJAX** — odpovídá
  explicitnímu požadavku issue, že lokální hledání má být výchozí varianta
  pro malé/střední seznamy (desítky až stovky položek), a že AJAX je jen
  bonus.
- Architektura komponenty: **vlastní TagHelper** `<search-multiselect>`
  postavený nad `IHtmlGenerator.GenerateSelect(...)` (stejná interní metoda,
  kterou uvnitř používá vestavěný `SelectTagHelper`), **ne** dědičnost z
  `SelectTagHelper`. Důvod: dědičnost z `SelectTagHelper` zdědí i jeho
  `[HtmlTargetElement]` atributy, což by riskovalo, že by nová třída
  nechtěně zasahovala do zpracování obyčejných `<select asp-for>` elementů
  v celé aplikaci. Kompozice přes `IHtmlGenerator` dává 100% shodné chování
  s nativním `<select asp-for asp-items multiple>` (generování `name`/`id`,
  `data-val-*` validační atributy, označení `selected` u aktuálně vybraných
  hodnot — včetně hodnot z `ModelState` po neúspěšné validaci) bez rizika
  kolize a bez duplikace logiky model bindingu.
- Mimo rozsah zůstává: AJAX remote search handler, objektové položky
  s bohatým víceatributovým hledáním (např. jméno+tým+osobní číslo
  samostatně), migrace všech stávajících `[data-multiselect]` použití na
  novou komponentu — vše explicitně označeno v issue jako „bonus navíc“ nebo
  „cílový směr“, ne požadavek této implementace.
- Jedna reálná aplikace komponenty jako důkaz funkčnosti: pole trenérů na
  `Training/Plan/Edit.cshtml` (sdílené pole `SelectedCoachIds` i seskupená pole
  `MemberCoachAssignments[i].CoachIds` z issue #18) — nahrazují existující
  `[data-multiselect]` checkbox widget. Toto je rozsah **fáze 3**.

## Technický návrh

### Fáze 1 — Integrace knihovny a základní styl

1. `src/SportSys.Razor/Pages/Shared/_Layout.cshtml`:
   - Přidat `<script src="https://cdn.jsdelivr.net/npm/tom-select@2.6.2/dist/js/tom-select.complete.min.js" crossorigin="anonymous" referrerpolicy="no-referrer"></script>`
     **před** `<script src="~/js/site.js" ...>`.
   - CSS `<link>` na `tom-select.css` se **nepřidává** — plně vlastní SCSS.
2. `src/SportSys.Razor/Styles/_multiselect.scss` (nový soubor):
   - Vlastní téma Tom Select postavené na `.ts-wrapper`, `.ts-control`,
     `.ts-dropdown`, `.ts-dropdown .option`, `.ts-dropdown .active`,
     `.item`, `.item .remove`, `.ts-wrapper.focus .ts-control`,
     `.ts-wrapper.disabled .ts-control`.
   - Barvy/okraje/radius vycházejí z tokenů použitých v `_forms.scss`
     mixinu `textbox` (`border-color: var(--color-border-strong)`,
     `border-radius: 4px`, `background: var(--color-surface-1)`), focus
     stav `outline/border-color: var(--color-border-focus)` (shoda s
     `select:focus` v `_forms.scss`), chip pozadí
     `var(--color-interactive-selected)`, hover položky dropdownu
     `var(--color-interactive-hover)`.
   - Validační stav: selektor `.input-validation-error + .ts-wrapper .ts-control`
     (nebo obdoba podle toho, jak ASP.NET Core vygeneruje třídu na `<select>`)
     s barvou `var(--color-error)`/`var(--color-error-text)`, shodně s
     existujícím `.field-validation-error` vzorem v `_forms.scss`.
   - Registrovat `@use 'multiselect';` v `src/SportSys.Razor/Styles/site.scss`
     (za `@use 'forms';`, logicky navazuje na formulářové prvky).
3. `src/SportSys.Razor/wwwroot/js/site.js`: nová IIFE sekce „Multivýběr
   s fulltextovým hledáním (Tom Select)“ analogická stávající sekci
   „Multivýběr“:
   ```js
   (function () {
       if (typeof TomSelect === 'undefined') return;
       document.querySelectorAll('select[data-search-multiselect]').forEach(function (select) {
           new TomSelect(select, {
               plugins: { remove_button: { title: select.dataset.removeLabel || 'Odebrat' } },
               placeholder: select.dataset.placeholder || '',
               searchField: ['text'],
               maxItems: null,
               closeAfterSelect: false,
           });
       });
   })();
   ```
   - Žádný `DOMContentLoaded` wrapper (stejně jako okolní IIFE bloky) —
     `site.js` je vykreslen na konci `<body>`, DOM je již zpracován.
   - Žádná logika reinicializace po AJAX částečném překreslení — aplikace
     dělá pouze plné server-side postbacky, Tom Select se tedy inicializuje
     přesně jednou na každé vykreslení stránky (žádný z community-popsaných
     problémů s „reinit po partial update“ se netýká tohoto projektu).
4. Ověření fáze 1: ručně otestovat na libovolném existujícím `<select
   multiple>` dočasně označeném `data-search-multiselect` (např. v lokální
   kopii/testovací stránce), že funguje hledání, chips, klávesnice a že
   `npm run build:css` projde bez chyb.

### Fáze 2 — Znovupoužitelný TagHelper

1. Nový soubor `src/SportSys.Razor/TagHelpers/SearchMultiSelectTagHelper.cs`:
   - `[HtmlTargetElement("search-multiselect")]`.
   - Vstupní atributy:
     - `[HtmlAttributeName("asp-for")] public ModelExpression For`
     - `[HtmlAttributeName("asp-items")] public IEnumerable<SelectListItem> Items`
     - `[HtmlAttributeName("placeholder")] public string? Placeholder`
     - `[HtmlAttributeName("remove-label")] public string RemoveLabel = "Odebrat"`
     - `[HtmlAttributeName("disabled")] public bool Disabled`
   - Injektované závislosti: `[ViewContext] public ViewContext ViewContext`
     a `IHtmlGenerator` (konstruktorová injekce, stejně jako to dělá
     `Microsoft.AspNetCore.Mvc.TagHelpers.SelectTagHelper` interně).
   - `ProcessAsync`/`Process`:
     1. Spočítat aktuální hodnoty pro multi-select výběr přes
        `htmlGenerator.GetCurrentValues(ViewContext, For.ModelExplorer, For.Name, allowMultiple: true)`
        — toto automaticky řeší prioritu `ModelState` (hodnoty z postbacku po
        validační chybě) před aktuální hodnotou modelu, čímž se bez
        dodatečného kódu splní akceptační kritérium „validační chyba
        nezpůsobí ztrátu výběru“.
     2. Zavolat `htmlGenerator.GenerateSelect(ViewContext, For.ModelExplorer,
        optionLabel: null, For.Name, Items, currentValues, allowMultiple:
        true, htmlAttributes: null)` → vrátí `TagBuilder` reprezentující
        `<select>` s korektně vygenerovanými `<option>` (včetně `selected`)
        a validačními `data-val-*` atributy.
     3. Nastavit `output.TagName = "select"`, zkopírovat atributy z
        `TagBuilder` (`id`, `name`, existující `class`, `data-val*`) do
        `output.Attributes`, přidat/`MergeAttribute` `multiple`.
     4. Přidat `data-search-multiselect` (marker pro JS auto-init),
        `data-placeholder` (z `Placeholder`), `data-remove-label` (z
        `RemoveLabel`), `disabled` pokud `Disabled == true`.
     5. `output.Content.SetHtmlContent(tagBuilder.InnerHtml)` — přenese
        vygenerované `<option>` prvky.
   - Žádná vlastní logika pro `selected`/binding se neimplementuje ručně —
     vše deleguje na `IHtmlGenerator`, čímž se zamezí duplikaci/rozbití
     standardního chování Razor Pages model bindingu.
2. Registrace ve všech `_ViewImports.cshtml`:
   - `src/SportSys.Razor/Pages/_ViewImports.cshtml`
   - `src/SportSys.Razor/Areas/sport/Pages/_ViewImports.cshtml`
   - `src/SportSys.Razor/Areas/hr/Pages/_ViewImports.cshtml`
   - `src/SportSys.Razor/Areas/Inventory/Pages/_ViewImports.cshtml`
   - `src/SportSys.Razor/Areas/Identity/Pages/_ViewImports.cshtml`
   - Přidat řádek `@addTagHelper *, SportSys.Razor` hned za existující
     `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`.
3. Použití (cílová syntaxe z issue):
   ```cshtml
   <search-multiselect asp-for="SelectedCoachIds" asp-items="Model.AvailableCoaches" placeholder="Vyber trenéry..." />
   ```
   kde `Model.AvailableCoaches` musí být předáno jako
   `IEnumerable<SelectListItem>` (např. `.Select(c => new SelectListItem(c.DisplayName, c.Id.ToString()))`
   v PageModelu nebo helperu — stejný vzor, jaký dnes používá `SelectList`
   v jiných formulářích).

### Fáze 3 — Ověření na reálném konzumentovi

1. `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml`:
   - Nahradit `[data-multiselect]` markup pro:
     - sdílené pole `Input.SelectedCoachIds` (ungrouped plán),
     - per-member pole `Input.MemberCoachAssignments[i].CoachIds` (grouped
       plán, issue #18),
   - novou `<search-multiselect asp-for="..." asp-items="..." placeholder="Vyber trenéry..." />`.
   - `Edit.cshtml.cs` — doplnit/upravit přípravu `AvailableCoaches` jako
     `IEnumerable<SelectListItem>` pro `asp-items`, pokud dosud není v tomto
     tvaru (aktuálně je `IReadOnlyList<CoachSelectItem>` — je potřeba
     mapovací krok, buď v PageModelu, nebo pomocnou extension metodou
     `ToSelectListItems()` u `CoachSelectItem`).
   - Ponechat beze změny veškerou validační a ukládací logiku
     (`TrainingPlanUpdateResult` switch, `ReloadPageAsync`) — mění se pouze
     vykreslování, ne datový tok.
2. Toto je jediné místo migrace v rámci této implementace — ostatní
   konzumenti `[data-multiselect]` (`Schedule/Index`, `Requirement/Index`,
   `Plan/Index` filtry) zůstávají beze změny (viz „Mimo rozsah“).

## Implementační kroky

### Fáze 1: Integrace Tom Select + základní styl

- [ ] `_Layout.cshtml`: přidat `<script>` tag na `tom-select.complete.min.js` z jsDelivr (verze 2.6.2), před `site.js`.
- [ ] Nový `Styles/_multiselect.scss` s vlastním tématem nad tokeny z `_vars.scss`.
- [ ] `Styles/site.scss`: `@use 'multiselect';`.
- [ ] `wwwroot/js/site.js`: nová IIFE sekce s auto-init na `[data-search-multiselect]`.
- [ ] `npm run build:css` v `src/SportSys.Razor`.
- [ ] Ruční ověření na dočasném testovacím `<select multiple data-search-multiselect>`.

### Fáze 2: TagHelper `<search-multiselect>`

- [ ] `TagHelpers/SearchMultiSelectTagHelper.cs` dle technického návrhu.
- [ ] Registrace `@addTagHelper *, SportSys.Razor` ve všech 5 `_ViewImports.cshtml`.
- [ ] Unit testy TagHelperu (viz „Testy a ověření“).

### Fáze 3: Nasazení na Training Plan Edit (trenéři)

- [ ] `Edit.cshtml`: nahradit obě použití `[data-multiselect]` novou komponentou.
- [ ] `Edit.cshtml.cs`: doplnit mapování `AvailableCoaches` → `IEnumerable<SelectListItem>`.
- [ ] Manuální akceptace dle issue (viz níže).

## Soubory ke změně

- `src/SportSys.Razor/Pages/Shared/_Layout.cshtml` — načtení Tom Select JS.
- `src/SportSys.Razor/Styles/_multiselect.scss` — nový soubor, vlastní téma.
- `src/SportSys.Razor/Styles/site.scss` — import nového partialu.
- `src/SportSys.Razor/wwwroot/js/site.js` — auto-init Tom Select.
- `src/SportSys.Razor/TagHelpers/SearchMultiSelectTagHelper.cs` — nový soubor.
- `src/SportSys.Razor/Pages/_ViewImports.cshtml` a 4× area `_ViewImports.cshtml` — registrace TagHelperu.
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml` — nasazení komponenty.
- `src/SportSys.Razor/Areas/sport/Pages/Training/Plan/Edit.cshtml.cs` — mapování `AvailableCoaches`.
- `docs/modules/frontend.md` — dokumentace nové komponenty (konvence, kdy použít).
- `tests/SportSys.Razor.Tests/SearchMultiSelectTagHelperTests.cs` — nový testovací soubor.

## Testy a ověření

- Nová unit testová třída `SearchMultiSelectTagHelperTests.cs` (vzor podobný
  `Microsoft.AspNetCore.Mvc.TagHelpers`' vlastním testům `SelectTagHelperTest`
  v ASP.NET Core zdrojích — instancace `DefaultHtmlGenerator` s testovacím
  `IModelMetadataProvider`/`ViewDataDictionary`, vytvoření `TagHelperContext`
  a `TagHelperOutput`, zavolání `Process`):
  - Vybrané hodnoty z `List<int>` modelu se promítnou jako `selected`
    `<option>`.
  - Po simulovaném `ModelState` s neúspěšnou validací se zachová výběr
    odeslaný uživatelem, ne původní hodnota modelu.
  - Výstup obsahuje `multiple`, `data-search-multiselect`,
    `data-placeholder`, `data-remove-label`.
  - `Disabled = true` přidá atribut `disabled`.
- `npm run build:css` musí projít bez chyb (ověří validitu nové SCSS).
- `dotnet build SportSys.slnx` a `dotnet test tests\SportSys.Razor.Tests\SportSys.Razor.Tests.csproj -c Release` musí projít.

## Manuální akceptace

Dle akceptačních kritérií issue #19, ověřit na `Training/Plan/Edit.cshtml`
(pole trenérů):

1. Lze vybrat libovolný počet trenérů ze seznamu.
2. Lze filtrovat psaním (fulltextové hledání v zobrazeném textu).
3. Ovládání funguje myší i klávesnicí (šipky, Enter, Backspace pro odebrání
   posledního tagu, Tab).
4. Každou vybranou položku lze jednotlivě odebrat (křížek na tagu).
5. Po odeslání formuláře se hodnoty správně navážou do
   `SelectedCoachIds`/`MemberCoachAssignments[i].CoachIds`.
6. Při editaci existujícího plánu je už uložený výběr předvyplněný.
7. Vyvolaná validační chyba formuláře (např. prázdný povinný název plánu)
   nezpůsobí ztrátu aktuálně vybraných trenérů po znovu-vykreslení stránky.
8. Komponenta vzhledově odpovídá ostatním formulářovým prvkům (shodné
   okraje, fokus stav, barvy v light i dark módu).
9. Žádné volání jQuery, žádná chyba v konzoli prohlížeče.

## Beze změny

- Existující `[data-multiselect]` widget (`site.js`, `_schedule.scss`) a jeho
  použití na `Training/Schedule/Index`, `Training/Requirement/Index`,
  `Training/Plan/Index` (filtry) — zůstávají beze změny, nejsou předmětem
  této implementace.
- Datový model, validace a ukládací logika Training Plan (`TrainingPlanService`,
  `TrainingPlanEditDto`, `TrainingPlanUpdateResult`) — mění se pouze
  vykreslování polí, ne tok dat.
- Žádná změna EF Core modelu ani migrace — jde čistě o UI/prezentační vrstvu.

## Mimo rozsah

- AJAX/remote vyhledávání (Razor Pages handler typu `?handler=SearchX&q=...`,
  `load` callback Tom Select) — explicitně označeno v issue jako bonus.
- Bohaté víceatributové hledání nad objektovými položkami (např. zvlášť podle
  jména, týmu, osobního čísla) — lze řešit už dnes bez změny komponenty
  sloučením do `SelectListItem.Text` na straně volajícího (např.
  `"Jan Novák (U15 • 00123)"`), ale sofistikovanější řešení (zvýrazňování
  shod, vážené skóre napříč poli) není součástí této implementace.
- Migrace ostatních konzumentů `[data-multiselect]` na novou komponentu.
- Lokalizace textů (`placeholder`, `Odebrat`) do jiných jazyků — komponenta je
  pouze připravena na budoucí lokalizaci (texty nejsou hardcoded v JS, ale
  procházejí přes `data-*` atributy nastavitelné z Razor view).

## Hotovo, když

- Tom Select je načten přes CDN a inicializuje se automaticky na
  `[data-search-multiselect]` prvcích bez jQuery.
- Existuje TagHelper `<search-multiselect asp-for asp-items placeholder />`
  registrovaný ve všech oblastech aplikace a pokrytý unit testy.
- `Training/Plan/Edit.cshtml` používá novou komponentu pro obě pole trenérů
  (sdílené i per-member) a všech 9 manuálních akceptačních kritérií prochází.
- `dotnet build`, `dotnet test` a `npm run build:css` procházejí bez chyb.
- `docs/modules/frontend.md` popisuje novou komponentu a kdy ji použít.
