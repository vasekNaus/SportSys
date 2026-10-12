# UI Zadání – Modul skladového hospodářství

## Dokument
Určeno pro AI agenta implementujícího uživatelské rozhraní modulu skladového hospodářství.

Projekt:

```text
SportSys.Razor
```

Technologie:

```text
ASP.NET Core Razor Pages
čisté HTML, CSS
```

Umístění:

```text
Areas/Inventory
```

Cílem je vytvořit jednoduché, přehledné administrativní rozhraní bez složitých JavaScript frameworků.

---

# Obecné požadavky

## URL struktura

Všechny stránky budou umístěny pod:

```text
/Inventory
```

Příklad:

```text
/Inventory/Manufacturers
/Inventory/Locations
/Inventory/Loans
```

---

# Vzhled

Používat standardní vzhled aplikace.

Požadavky:

- stránkování dle existujících konvencí projektu
- responzivní rozložení
- zachovat jednotný vzhled s ostatními moduly

---

# Navigace

V menu modulu Inventory vytvořit sekce:

```text
Sklad

├─ Výrobci
├─ Umístění
├─ Výpůjčky
```

Další položky modulu budou doplněny později.

---

# Číselníkové administrace

Pro všechny jednoduché číselníky použít jednotný vzor.

Každý číselník obsahuje:

```text
Index
Edit
Create
```

Edit i Create mohou sdílet stejnou Razor Page.

---

# Administrace výrobců

## Umístění

```text
Areas/Inventory/Pages/Manufacturers
```

---

## Seznam výrobců

### Stránka

```text
/Inventory/Manufacturers
```

### Funkce

- zobrazení výrobců
- filtrování dle názvu
- řazení podle názvu
- tlačítko Nový výrobce
- tlačítko Editace

### Sloupce

```text
Název
Web
Aktivní
```

### Filtr

Pole:

```text
Název
```

Tlačítka:

```text
Hledat
Vymazat filtr
```

---

## Editace výrobce

### Stránka

```text
/Inventory/Manufacturers/Edit/{id}
```

### Pole

```text
Název
Web
Aktivní
```

### Tlačítka

```text
Uložit
Zpět
```

---

## Nový výrobce

### Stránka

```text
/Inventory/Manufacturers/Create
```

Stejný formulář jako Editace.

---

# Administrace umístění

## Umístění

```text
Areas/Inventory/Pages/Locations
```

---

## Seznam umístění

### Stránka

```text
/Inventory/Locations
```

### Funkce

- seznam umístění
- hierarchické zobrazení
- filtr dle názvu
- vytvoření nového umístění
- editace existujícího umístění

### Sloupce

```text
Název
Nadřazené umístění
Aktivní
```

---

## Editace umístění

### Stránka

```text
/Inventory/Locations/Edit/{id}
```

### Pole

```text
Název
Nadřazené umístění
Popis
Aktivní
```

Nadřazené umístění:

```text
Dropdown
```

---

## Nové umístění

### Stránka

```text
/Inventory/Locations/Create
```

Stejný formulář jako editace.

---

# Výpůjčky

Výpůjčka představuje vydání jedné nebo více skladových položek členu klubu.

---

# Přehled výpůjček

## Umístění

```text
Areas/Inventory/Pages/Loans
```

---

## Stránka

```text
/Inventory/Loans
```

---

## Funkce

- přehled výpůjček
- filtrování
- otevření detailu
- vytvoření nové výpůjčky

---

## Filtr

Pole:

```text
Člen
Aktivní výpůjčka
Datum od
Datum do
```

---

## Sloupce

```text
Číslo výpůjčky
Člen
Datum vydání
Počet položek
Vráceno
Stav
```

---

## Stav výpůjčky

Možné stavy:

```text
Aktivní
Částečně vráceno
Uzavřeno
```

---

# Detail výpůjčky

## Stránka

```text
/Inventory/Loans/Edit/{id}
```

Slouží současně jako:

```text
Detail
Vrácení položek
Uzavření výpůjčky
```

---

## Základní údaje

Pouze pro čtení:

```text
Číslo výpůjčky
Člen
Datum vydání
Datum vrácení
```

---

## Seznam vypůjčených položek

Tabulka:

```text
Inventární číslo
Název
Kategorie
Vráceno
Datum vrácení
Akce
```

---

## Vrácení položky

Pokud položka není vrácena:

zobrazit tlačítko

```text
Potvrdit vrácení
```

Po stisknutí:

```text
nastavit datum vrácení
označit položku jako vrácenou
vytvořit InventoryTransaction typu Return
```

---

## Hromadné vrácení

Nad tabulkou zobrazit tlačítko:

```text
Vrátit vše
```

Po potvrzení:

```text
vrátit všechny nevrácené položky
```

---

# Nová výpůjčka

## Stránka

```text
/Inventory/Loans/Create
```

Toto je nejdůležitější obrazovka modulu.

---

# Krok 1 - výběr člena

Pole:

```text
Člen
```

Implementace:

```text
Autocomplete
```

Vyhledávání:

```text
Jméno
Příjmení
Členské číslo
```

Po výběru člena se aktivuje sekce položek.

---

# Krok 2 - přidávání položek

Uživatel zadává inventární čísla.

Primární scénář:

```text
načtení QR kódu čtečkou
```

Čtečka se chová jako klávesnice a zapisuje inventární číslo do vstupního pole.

---

## Pole pro přidání položky

```text
Inventární číslo
```

Po potvrzení:

```text
Enter
```

Systém:

1. vyhledá položku
2. ověří existenci
3. ověří dostupnost
4. přidá položku do seznamu

---

## Kontroly

Nesmí být možné přidat položku pokud:

```text
je již vypůjčená
je vyřazená
neexistuje
```

Zobrazit validační hlášku.

---

## Seznam položek výpůjčky

Tabulka:

```text
Inventární číslo
Název
Kategorie
Aktuální umístění
Akce
```

Akce:

```text
Odebrat
```

---

## Požadavky na UX

Po přidání položky:

```text
vyčistit vstupní pole
nastavit kurzor zpět do vstupního pole
umožnit okamžité načtení další položky
```

Optimalizováno pro práci s QR čtečkou.

---

# Uložení výpůjčky

Po stisku:

```text
Vytvořit výpůjčku
```

Systém:

1. vytvoří Loan
2. vytvoří LoanItems
3. vytvoří InventoryTransaction typu Loan
4. nastaví stav položek na Borrowed
5. uloží člena, datum a položky

---

# Doporučená struktura Razor Pages

```text
Areas
└─ Inventory
   └─ Pages

      ├─ Manufacturers
      │  ├─ Index.cshtml
      │  ├─ Index.cshtml.cs
      │  ├─ Edit.cshtml
      │  └─ Edit.cshtml.cs

      ├─ Locations
      │  ├─ Index.cshtml
      │  ├─ Index.cshtml.cs
      │  ├─ Edit.cshtml
      │  └─ Edit.cshtml.cs

      └─ Loans
         ├─ Index.cshtml
         ├─ Index.cshtml.cs
         ├─ Create.cshtml
         ├─ Create.cshtml.cs
         ├─ Edit.cshtml
         └─ Edit.cshtml.cs
```

---

# PageModel konvence

Používat:

```csharp
async Task<IActionResult> OnGetAsync()
async Task<IActionResult> OnPostAsync()
```

Veškerý přístup k databázi realizovat přes aplikační služby.

PageModel nesmí obsahovat business logiku.

---

# Business logika

Veškeré operace:

```text
vytvoření výpůjčky
vrácení položky
uzavření výpůjčky
změna stavu položky
vytvoření transakce
```

musí být implementovány ve službách aplikační vrstvy.

Razor Pages jsou pouze prezentační vrstva.

---

# Nedělat

Není součástí první verze:

- inventury
- generování QR kódů
- grafy
- exporty
- tiskové sestavy
- drag & drop
- SPA frameworky

Tyto části budou implementovány v dalších iteracích.