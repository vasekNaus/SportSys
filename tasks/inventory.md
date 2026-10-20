# Modul skladového hospodářství

## Účel modulu

Modul skladového hospodářství slouží pro evidenci:

- hokejové výstroje
- sportovního oblečení
- dlouhodobého majetku
- vybavení sportovišť a tělocvičen
- elektroniky
- zapůjčeného vybavení členům klubu

Hlavním cílem modulu je:

- evidence majetku a výstroje
- podpora inventur
- sledování pohybů položek
- evidence zapůjčení členům klubu
- evidence nákupů a zdrojů financování
- identifikace pomocí inventárního čísla
- identifikace pomocí QR kódu
- dohledatelnost historie celé životnosti položky

---

# Architektura

## Databázová vrstva

Databáze: MSSQL

### Schémata

#### dbo

Sdílené entity použitelné i v jiných modulech:

- Manufacturer
- Location
- User (existující evidence členů)

#### inventory

Entity skladového hospodářství.

### Namespace

```csharp
Company.Project.Inventory
```

Datové entity:

```csharp
Company.Project.Inventory.Entities
```

Konfigurace EF Core:

```csharp
Company.Project.Inventory.EntityConfigurations
```

---

# Základní koncept

Systém eviduje jednotlivé kusy majetku a výstroje.

Každá položka:

- má vlastní identitu
- má inventární číslo
- může být opatřena QR kódem
- má aktuální stav
- má historii pohybů
- může být zapůjčena členu klubu
- účastní se inventur

---

# Dědičnost

Použít strategii:

```csharp
TPC (Table Per Concrete Type)
```

Abstraktní předek:

```csharp
InventoryItem
```

Potomci:

```csharp
Equipment
Asset
```

---

# InventoryItem

Abstraktní základní entita.

## Společné vlastnosti

| Název | Popis |
|---------|---------|
| Id | PK |
| InventoryNumber | Inventární číslo |
| Name | Název |
| Description | Poznámka |
| CategoryId | Kategorie |
| ManufacturerId | Výrobce |
| AssignedLocationId | Přidělené umístění |
| CurrentLocationId | Aktuální umístění |
| ItemStatus | Stav položky |
| AcquisitionDate | Datum pořízení |
| AcquisitionPrice | Pořizovací cena |
| QRCodeValue | Hodnota QR |
| IsActive | Aktivní |
| CreatedAt | Vytvořeno |
| ModifiedAt | Upraveno |

---

# Equipment

Výstroj.

## Příklady

- dres
- helma
- rukavice
- hokejka
- brusle
- tepláková souprava

## Specifické vlastnosti

| Název | Popis |
|---------|---------|
| SizeId | Velikost |

---

# Asset

Majetek.

## Příklady

- notebook
- PC
- monitor
- tiskárna
- spinningové kolo
- posilovací stroj

## Specifické vlastnosti

| Název | Popis |
|---------|---------|
| SerialNumber | Výrobní číslo |
| WarrantyUntil | Konec záruky |
| ExternalId | Externí označení |

---

# Stav položky

## ItemStatus

```text
InStock
Assigned
Borrowed
InRepair
Lost
Disposed
```

### Význam

| Stav | Popis |
|---------|---------|
| InStock | Ve skladu |
| Assigned | Přidělena na umístění |
| Borrowed | Zapůjčena členu |
| InRepair | V servisu |
| Lost | Ztracena |
| Disposed | Vyřazena |

---

# Kategorie

Kategorie tvoří stromovou strukturu.

## Category

```sql
inventory.Category
```

| Název | Popis |
|---------|---------|
| Id | PK |
| ParentCategoryId | FK |
| Name | Název |
| Code | Kód |
| CategoryType | Equipment/Asset |
| SortOrder | Řazení |
| IsActive | Aktivní |

---

# Výchozí struktura kategorií

```text
Výstroj
├── Dresy
├── Helmy
├── Rukavice
├── Kalhoty
├── Brusle
├── Hokejky

Majetek
├── IT
│   ├── Notebooky
│   ├── PC
│   ├── Monitory
│   └── Tiskárny
├── Tělocvična
│   ├── Spinningová kola
│   └── Posilovací stroje
└── Ostatní
```

---

# Velikosti

Velikosti jsou navázány na kategorii.

## Size

```sql
inventory.Size
```

| Název | Popis |
|---------|---------|
| Id | PK |
| Name | Název |
| SortOrder | Řazení |
| IsActive | Aktivní |

---

## CategorySize

```sql
inventory.CategorySize
```

| Název | Popis |
|---------|---------|
| CategoryId | FK |
| SizeId | FK |

---

# Výrobci

## Manufacturer

```sql
dbo.Manufacturer
```

| Název | Popis |
|---------|---------|
| Id | PK |
| Name | Název |
| Website | Web |
| IsActive | Aktivní |

---

# Umístění

Každá položka obsahuje dvě umístění.

## AssignedLocation

Místo, kam položka organizačně patří.

Příklady:

- hlavní sklad
- kabina A
- posilovna
- kancelář

## CurrentLocation

Skutečné aktuální umístění.

Příklady:

- servis
- turnaj
- hlavní sklad
- autobus

---

## Location

```sql
dbo.Location
```

| Název | Popis |
|---------|---------|
| Id | PK |
| ParentLocationId | FK |
| Name | Název |
| Description | Popis |
| IsActive | Aktivní |

---

# Členové klubu

Položky lze zapůjčovat členům.

Předpokládá se vazba na existující modul členů.

```sql
dbo.User
```

---

# Zápůjčky

Každá zápůjčka musí být evidována samostatně.

## Loan

```sql
inventory.Loan
```

| Název | Popis |
|---------|---------|
| Id | PK |
| InventoryItemId | Položka |
| MemberId | Člen |
| LoanDate | Datum vydání |
| ExpectedReturnDate | Očekávané vrácení |
| ReturnedDate | Datum vrácení |
| Note | Poznámka |
| IsClosed | Ukončená zápůjčka |

---

# Pohyby skladu

Klíčová entita celého modulu.

Veškeré operace nad skladovou položkou musí vytvářet záznam pohybu.

---

## InventoryTransaction

```sql
inventory.InventoryTransaction
```

| Název | Popis |
|---------|---------|
| Id | PK |
| InventoryItemId | Položka |
| TransactionTypeId | Typ pohybu |
| TransactionDate | Datum |
| Quantity | Počet |
| UserId | Uživatel |
| Note | Poznámka |

---

## TransactionType

```sql
inventory.TransactionType
```

### Výchozí hodnoty

```text
Purchase
Loan
Return
Transfer
RepairStart
RepairEnd
InventoryCheck
Lost
Dispose
```

### Popis

| Typ | Význam |
|---------|---------|
| Purchase | Nákup |
| Loan | Zapůjčení |
| Return | Vrácení |
| Transfer | Přesun |
| RepairStart | Zahájení opravy |
| RepairEnd | Ukončení opravy |
| InventoryCheck | Inventura |
| Lost | Ztráta |
| Dispose | Vyřazení |

---

# Nákupy

Je nutné evidovat původ a financování pořízení položky.

---

## PurchaseDocument

Reprezentuje fakturu nebo jiný nákupní doklad.

```sql
inventory.PurchaseDocument
```

| Název | Popis |
|---------|---------|
| Id | PK |
| DocumentNumber | Číslo dokladu |
| SupplierName | Dodavatel |
| PurchaseDate | Datum nákupu |
| TotalAmount | Celková částka |
| Note | Poznámka |

---

## InventoryItemPurchase

Vazba položky na nákup.

```sql
inventory.InventoryItemPurchase
```

| Název | Popis |
|---------|---------|
| Id | PK |
| InventoryItemId | Položka |
| PurchaseDocumentId | Doklad |
| PurchasePrice | Cena |

---

# Historie umístění

## ItemLocationHistory

```sql
inventory.ItemLocationHistory
```

| Název | Popis |
|---------|---------|
| Id | PK |
| InventoryItemId | Položka |
| PreviousLocationId | Původní umístění |
| NewLocationId | Nové umístění |
| ChangedAt | Datum |
| ChangedByUserId | Uživatel |
| Note | Poznámka |

---

# Inventární číslo

Každá položka musí mít unikátní inventární číslo.

Příklad:

```text
INV-2026-000001
INV-2026-000002
INV-2026-000003
```

Po vytvoření je číslo neměnné.

---

# QR kódy

QR kód slouží jako primární identifikace položky při inventurách.

## Doporučený obsah

```text
INV-2026-000001
```

nebo

```text
https://app.domain.cz/inventory/item/12345
```

Do databáze se ukládá pouze hodnota QR.

Generování obrázku QR probíhá aplikačně.

---

# Inventury

Inventura se provádí minimálně jednou ročně.

---

## InventorySession

Inventurní běh.

```sql
inventory.InventorySession
```

| Název | Popis |
|---------|---------|
| Id | PK |
| Name | Název inventury |
| StartedAt | Zahájení |
| FinishedAt | Ukončení |
| IsClosed | Uzavřena |

---

## InventoryCheck

Výsledek kontroly konkrétní položky.

```sql
inventory.InventoryCheck
```

| Název | Popis |
|---------|---------|
| Id | PK |
| InventorySessionId | Inventura |
| InventoryItemId | Položka |
| CheckedAt | Čas kontroly |
| CheckedByUserId | Uživatel |
| Found | Nalezena |
| ActualLocationId | Skutečné umístění |
| Note | Poznámka |

---

# Audit

Všechny entity modulu musí podporovat auditní údaje:

```csharp
CreatedAt
CreatedBy

ModifiedAt
ModifiedBy
```

Dle existující architektury projektu.

---

# EF Core

Použít:

```csharp
builder.UseTpcMappingStrategy();
```

Hierarchie:

```csharp
InventoryItem
 ├─ EquipmentItem
 └─ AssetItem
```

---

# Budoucí rozšíření

Návrh musí být připraven na:

- přidělení výstroje konkrétnímu hráči sezónně
- vratné zálohy za vybavení
- schvalování výdeje majetku
- servisní zásahy
- plán údržby majetku
- fotodokumentaci
- elektronické podpisy při převzetí
- mobilní inventuru pomocí telefonu
- RFID identifikaci
- více skladů
- automatické generování inventurních štítků
- hromadný import z Excelu
- export inventurních sestav