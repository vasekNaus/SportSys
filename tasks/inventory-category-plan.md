# Změna návrhu kategorií a velikostí

## Účel změny

Původní návrh obsahoval:

- sloupec `CategoryType` pro rozlišení kategorií typu Equipment / Asset
- samostatnou entitu `Size`
- vazební tabulku `CategorySize`

Po revizi návrhu byly tyto části zjednodušeny.

Cílem je:

- zjednodušit datový model
- snížit množství administrativních obrazovek
- odstranit zbytečné referenční tabulky
- zachovat dostatečnou flexibilitu při definici velikostí

---

# Kategorie

## Nový koncept

Typ kategorie nebude určován sloupcem `CategoryType`.

Místo toho budou existovat dvě kořenové kategorie:

```text
Sportovní vybavení
Majetek
```

Příslušnost kategorie k typu položky je určena její pozicí ve stromu.

Příklad:

```text
Sportovní vybavení
│
├── Výstroj
│   ├── Dres
│   ├── Brusle
│   ├── Helma
│   └── Hokejka
│   └── Vesta
│   └── Holeně
│   └── Suspenzor
│   └── Kalhoty
│   └── Lokty
│   └── Nákrčník
│
├── Oblečení
│   ├── Tričko
│   ├── Mikina
│   └── Bunda
│
└── Tréninková pomůcka
    ├── Puk
    ├── Kužel
    └── Překážka

Majetek
│
├── IT
│   ├── Notebook
│   ├── PC
│   └── Monitor
    └── Tiskárna

│
├── Spotřebiče
    └── Vysoušeč
│
├── Posilovna
│   ├── Spinningové kolo
│   └── Posilovací stroj
│
└── Kancelář
    └── Nábytek
```

---

# Změna entity Category

## Původní návrh

```csharp
Category
{
    int Id;
    int? ParentCategoryId;
    string Name;
    string Code;
    CategoryType CategoryType;
    int SortOrder;
    bool IsActive;
}
```

---

## Nový návrh

```csharp
Category
{
    int Id;
    int? ParentCategoryId;
    string Name;
    string? AvailableSizesJson;
    int SortOrder;
    bool IsActive;
}
```

---

# Odstraněné vlastnosti

Následující vlastnosti budou z návrhu odstraněny:

```csharp
Code
```

a

```csharp
CategoryType
```

---

## Důvody

### Code

Interní kód není v rámci modulu využíván.

Jednoznačnou identifikaci zajišťuje:

```csharp
Id
```

a

```csharp
Name
```

---

### CategoryType

Příslušnost kategorie je odvozena ze stromové struktury.

Není potřeba ukládat:

```csharp
Equipment
Asset
```

do databáze.

Při výběru kategorií:

- Equipment používá pouze podstrom kategorie „Sportovní vybavení“
- Asset používá pouze podstrom kategorie „Majetek“

Tím se eliminuje riziko nekonzistence dat.

---

# Velikosti

## Nový koncept

Velikosti nebudou samostatnou entitou.

Nebudou existovat tabulky:

```text
Size
CategorySize
```

Nebudou existovat:

```text
administrace velikostí
vazby kategorií na velikosti
číselníky velikostí
```

---

# Definice velikostí na kategorii

Každá kategorie může definovat seznam povolených velikostí.

Tyto velikosti budou uloženy jako JSON pole.

---

## Sloupec

```csharp
AvailableSizesJson
```

Typ:

```sql
nvarchar(max)
```

---

## Příklad - dres

```json
[
  "XS",
  "S",
  "M",
  "L",
  "XL",
  "XXL"
]
```

---

## Příklad - brusle

```json
[
  "36",
  "37",
  "38",
  "39",
  "40",
  "41",
  "42",
  "43",
  "44",
  "45",
  "46"
]
```

---

## Příklad - rukavice

```json
[
  "10",
  "11",
  "12",
  "13",
  "14",
  "15"
]
```

---

## Kategorie bez velikostí

Pokud kategorie velikosti nevyužívá:

```json
[]
```

nebo

```json
null
```

---

# Změna entity Equipment

## Původní návrh

```csharp
public class Equipment : InventoryItem
{
    int? SizeId;
}
```

---

## Nový návrh

```csharp
public class Equipment : InventoryItem
{
    string? Size;
}
```

---

## Příklady uložených hodnot

```text
XS
M
XL
```

```text
41
42
43
```

```text
13
14
15
```

---

# Chování UI

## Editace kategorie

Administrace kategorie bude obsahovat pole:

```text
Povolené velikosti
```

Formou víceřádkového textového pole.

Jeden řádek = jedna velikost.

Příklad:

```text
XS
S
M
L
XL
XXL
```

Při ukládání aplikace převede hodnoty na JSON.

---

## Editace výstroje

Při založení nebo úpravě položky:

### Kategorie obsahuje definované velikosti

Zobrazit:

```text
Dropdown
```

s hodnotami z `AvailableSizesJson`.

---

### Kategorie neobsahuje definované velikosti

Zobrazit:

```text
TextBox
```

pro ruční zadání velikosti.

---

# Validace

Validace velikostí nebude řešena databázovou referenční integritou.

Databáze umožní uložit libovolnou hodnotu:

```text
M
XL
42
Custom
Test
```

Kontrola bude řešena pouze v UI.

Cílem je:

- jednoduchý datový model
- jednoduchá administrace
- minimální počet tabulek
- možnost kdykoliv doplnit novou velikost bez databázových změn

---

# Odstraněné entity

Z návrhu budou kompletně odstraněny:

```text
inventory.Size
inventory.CategorySize
```

Včetně:

```text
DTO modelů
Entity Framework konfigurací
CRUD obrazovek
servisních metod
seed dat
```

---

# Výsledný model

```text
Category
│
├── Name
├── ParentCategoryId
├── AvailableSizesJson
├── SortOrder
└── IsActive
```

```text
Equipment
│
├── všechny vlastnosti InventoryItem
└── Size
```

Princip:

Kategorie určuje pouze množinu doporučených velikostí pro UI.

Konkrétní hodnota velikosti je uložena přímo na položce výstroje jako text.