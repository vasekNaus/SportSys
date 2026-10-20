# Implementační plán: Datová vrstva modulu Inventory

## Kontext

Tento dokument popisuje kroky pro implementaci databázové vrstvy modulu skladového hospodářství do projektu `SportSys.Database`. Implementace zahrnuje EF Core modely, enumerace, konfiguraci a migraci. Aplikační servisy (Contract) a UI (Razor) nejsou součástí tohoto plánu.

**Referenční dokumentace:** [docs/modules/inventory.md](../../docs/modules/inventory.md)

---

## Přehled rozsahu

| Projekt | Dotek |
|---|---|
| `SportSys.Database` | Nové modely, konfigurace, enumerace, RESX, DbContext |
| `SportSys.Database` (migrace) | Nová migrace `AddInventoryModule` |

Žádný jiný projekt se v tomto plánu nemění.

---

## Fáze 1 – Infrastruktura

### 1.1 Přidat `Schemas.Inventory`

**Soubor:** `src/SportSys.Database/Models/Schemas.cs`

Přidat konstantu:

```csharp
public const string Inventory = "inventory";
```

---

## Fáze 2 – Sdílené entity (schéma `dbo`)

Entity `Manufacturer` a `Location` jsou v `dbo` schématu a mohou být sdíleny s budoucími moduly.

### 2.1 `dbo.Manufacturer`

**Soubor:** `src/SportSys.Database/Models/dbo/Manufacturer.cs`  
**Namespace:** `SportSys.Database.Models.dbo`

```csharp
[Table(nameof(Manufacturer), Schema = Schemas.Dbo)]
public partial class Manufacturer
{
    [Key]
    public int Id { get; set; }

    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Website { get; set; }

    public bool IsActive { get; set; }
}
```

> Konfigurační soubor pro `Manufacturer` není potřeba – vše lze vyjádřit atributy.

### 2.2 `dbo.Location`

**Soubor:** `src/SportSys.Database/Models/dbo/Location.cs`  
**Namespace:** `SportSys.Database.Models.dbo`

```csharp
[Table(nameof(Location), Schema = Schemas.Dbo)]
public partial class Location
{
    [Key]
    public int Id { get; set; }

    public int? ParentLocationId { get; set; }

    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }

    [ForeignKey(nameof(ParentLocationId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual Location? ParentLocation { get; set; }

    [InverseProperty(nameof(ParentLocation))]
    public virtual ICollection<Location> ChildLocations { get; set; } = new List<Location>();
}
```

> Self-referenční hierarchie vyžaduje `[InverseProperty]` pro disambiguaci – viz pravidlo v copilot-instructions.md.  
> Konfigurační soubor není potřeba.

---

## Fáze 3 – Enumerace a lokalizace

Inventory enumerace se umísťují přímo do `src/SportSys.Database/Models/inventory/` s namespace `SportSys.Database.Models.inventory` — jsou součástí modulu, nikoli sdíleného `Enums/` folderu. Pro každý enum se vytváří trojice RESX souborů v `src/SportSys.Database/Resources/`.

### 3.1 `EItemStatus`

**Soubor enum:** `src/SportSys.Database/Models/inventory/EItemStatus.cs`

```csharp
namespace SportSys.Database.Models.inventory;

public enum EItemStatus
{
    [Display(Name = "InStock", ResourceType = typeof(SportSys.Database.Resources.EItemStatus))]
    InStock = 1,

    [Display(Name = "Assigned", ResourceType = typeof(SportSys.Database.Resources.EItemStatus))]
    Assigned = 2,

    [Display(Name = "Borrowed", ResourceType = typeof(SportSys.Database.Resources.EItemStatus))]
    Borrowed = 3,

    [Display(Name = "InRepair", ResourceType = typeof(SportSys.Database.Resources.EItemStatus))]
    InRepair = 4,

    [Display(Name = "Lost", ResourceType = typeof(SportSys.Database.Resources.EItemStatus))]
    Lost = 5,

    [Display(Name = "Disposed", ResourceType = typeof(SportSys.Database.Resources.EItemStatus))]
    Disposed = 6
}
```

**Resources:**
- `src/SportSys.Database/Resources/EItemStatus.cs` – ResourceManager wrapper (ruční)
- `src/SportSys.Database/Resources/EItemStatus.resx` – anglické hodnoty (fallback)
- `src/SportSys.Database/Resources/EItemStatus.cs.resx` – české překlady

Vzor hodnot pro RESX:

| Klíč | EN | CS |
|---|---|---|
| `InStock` | In Stock | Ve skladu |
| `Assigned` | Assigned | Přidělena |
| `Borrowed` | Borrowed | Zapůjčena |
| `InRepair` | In Repair | V servisu |
| `Lost` | Lost | Ztracena |
| `Disposed` | Disposed | Vyřazena |

> `EItemStatus` se ukládá jako `int` sloupec přímo na `Equipment` a `Asset` (není to samostatná lookup tabulka – stejný vzor jako hypotetický stav, nikoli `TrainingState`). **Nesekat seed data pro tento enum** – hodnoty jsou v kódu, ne v DB tabulce.

### 3.2 `ETransactionType`

**Soubor enum:** `src/SportSys.Database/Models/inventory/ETransactionType.cs`

```csharp
namespace SportSys.Database.Models.inventory;

public enum ETransactionType
{
    [Display(Name = "Purchase", ResourceType = typeof(SportSys.Database.Resources.ETransactionType))]
    Purchase = 1,

    [Display(Name = "Loan", ResourceType = typeof(SportSys.Database.Resources.ETransactionType))]
    Loan = 2,

    [Display(Name = "Return", ResourceType = typeof(SportSys.Database.Resources.ETransactionType))]
    Return = 3,

    [Display(Name = "Transfer", ResourceType = typeof(SportSys.Database.Resources.ETransactionType))]
    Transfer = 4,

    [Display(Name = "RepairStart", ResourceType = typeof(SportSys.Database.Resources.ETransactionType))]
    RepairStart = 5,

    [Display(Name = "RepairEnd", ResourceType = typeof(SportSys.Database.Resources.ETransactionType))]
    RepairEnd = 6,

    [Display(Name = "InventoryCheck", ResourceType = typeof(SportSys.Database.Resources.ETransactionType))]
    InventoryCheck = 7,

    [Display(Name = "Lost", ResourceType = typeof(SportSys.Database.Resources.ETransactionType))]
    Lost = 8,

    [Display(Name = "Dispose", ResourceType = typeof(SportSys.Database.Resources.ETransactionType))]
    Dispose = 9
}
```

**Resources** (3 soubory, vzor jako výše):

| Klíč | EN | CS |
|---|---|---|
| `Purchase` | Purchase | Nákup |
| `Loan` | Loan | Zapůjčení |
| `Return` | Return | Vrácení |
| `Transfer` | Transfer | Přesun |
| `RepairStart` | Repair Start | Zahájení opravy |
| `RepairEnd` | Repair End | Ukončení opravy |
| `InventoryCheck` | Inventory Check | Inventura |
| `Lost` | Lost | Ztráta |
| `Dispose` | Dispose | Vyřazení |

> `ETransactionType` seeduje tabulku `inventory.TransactionType` – viz fáze 4.

### 3.3 `ECategoryType`

**Soubor enum:** `src/SportSys.Database/Models/inventory/ECategoryType.cs`

```csharp
namespace SportSys.Database.Models.inventory;

public enum ECategoryType
{
    [Display(Name = "Equipment", ResourceType = typeof(SportSys.Database.Resources.ECategoryType))]
    Equipment = 1,

    [Display(Name = "Asset", ResourceType = typeof(SportSys.Database.Resources.ECategoryType))]
    Asset = 2
}
```

**Resources** (3 soubory):

| Klíč | EN | CS |
|---|---|---|
| `Equipment` | Equipment | Výstroj |
| `Asset` | Asset | Majetek |

> `ECategoryType` se ukládá jako `int` sloupec `CategoryType` v tabulce `inventory.Category`. Není to samostatná lookup tabulka.

---

## Fáze 4 – Lookup tabulky (schéma `inventory`)

### 4.1 `inventory.Size`

**Soubor:** `src/SportSys.Database/Models/sport/Size.cs`  
**Soubor:** `src/SportSys.Database/Models/inventory/Size.cs`  
**Namespace:** `SportSys.Database.Models.inventorySchema`

```csharp
[Table(nameof(Size), Schema = Schemas.Inventory)]
public partial class Size
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    public required string Name { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<CategorySize> CategorySizes { get; set; } = new List<CategorySize>();
}
```

> Velikosti jsou uživatelsky konfigurovatelné; **nesekat z enumu**. Konfigurační soubor není potřeba.

### 4.2 `inventory.Category`

**Soubor:** `src/SportSys.Database/Models/inventory/Category.cs`  
**Namespace:** `SportSys.Database.Models.inventorySchema`

```csharp
[Table(nameof(Category), Schema = Schemas.Inventory)]
[Index(nameof(Code), IsUnique = true)]
public partial class Category
{
    [Key]
    public int Id { get; set; }

    public int? ParentCategoryId { get; set; }

    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public required string Code { get; set; }

    // Hodnota ECategoryType uložena jako int
    public int CategoryType { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    [ForeignKey(nameof(ParentCategoryId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual Category? ParentCategory { get; set; }

    [InverseProperty(nameof(ParentCategory))]
    public virtual ICollection<Category> ChildCategories { get; set; } = new List<Category>();

    public virtual ICollection<CategorySize> CategorySizes { get; set; } = new List<CategorySize>();
}
```

> Seed data pro výchozí strukturu kategorií přidáme v samostatném `Category.Seed.cs` nebo konfiguraci. Seed je volitelný – lze zadat i ručně přes UI po spuštění.

### 4.3 `inventory.CategorySize`

**Soubor:** `src/SportSys.Database/Models/inventory/CategorySize.cs`  
**Namespace:** `SportSys.Database.Models.inventorySchema`

```csharp
[Table(nameof(CategorySize), Schema = Schemas.Inventory)]
[PrimaryKey(nameof(CategoryId), nameof(SizeId))]
public partial class CategorySize
{
    public int CategoryId { get; set; }

    public int SizeId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public virtual Category Category { get; set; } = null!;

    [ForeignKey(nameof(SizeId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public virtual Size Size { get; set; } = null!;
}
```

### 4.4 `inventory.TransactionType`

**Soubor:** `src/SportSys.Database/Models/inventory/TransactionType.cs`  
**Namespace:** `SportSys.Database.Models.inventorySchema`

```csharp
[Table(nameof(TransactionType), Schema = Schemas.Inventory)]
public partial class TransactionType
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public required string Name { get; set; }
}
```

**Seed partial:** `src/SportSys.Database/Models/inventory/TransactionType.Seed.cs`

```csharp
using SportSys.Database.Models.inventory; // ETransactionType je v namespace inventory, ne Enums

namespace SportSys.Database.Models.inventorySchema;

public partial class TransactionType
{
    private TransactionType() { Name = null!; }

    [SetsRequiredMembers]
    public TransactionType(ETransactionType id)
    {
        Id   = (int)id;
        Name = Resources.ETransactionType.ResourceManager
                   .GetString(id.ToString(), CultureInfo.GetCultureInfo("cs"))
               ?? id.ToString();
    }
}
```

**Konfigurační soubor:** `src/SportSys.Database/Configurations/inventory/TransactionTypeConfiguration.cs`

```csharp
using SportSys.Database.Models.inventory; // ETransactionType
using SportSys.Database.Models.inventorySchema;

public class TransactionTypeConfiguration : IEntityTypeConfiguration<TransactionType>
{
    public void Configure(EntityTypeBuilder<TransactionType> builder)
    {
        builder.HasData(
            Enum.GetValues<ETransactionType>()
                .Select(e => new TransactionType(e))
        );
    }
}
```

---

## Fáze 5 – TPC hierarchie InventoryItem

### 5.1 Abstraktní základ `InventoryItem`

**Soubor:** `src/SportSys.Database/Models/inventory/InventoryItem.cs`  
**Namespace:** `SportSys.Database.Models.inventory`

```csharp
#nullable enable

namespace SportSys.Database.Models.inventory;

public abstract class InventoryItem
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public required string InventoryNumber { get; set; }

    [StringLength(200)]
    public required string Name { get; set; }

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public int? ManufacturerId { get; set; }

    public int? AssignedLocationId { get; set; }

    public int? CurrentLocationId { get; set; }

    // Hodnota EItemStatus uložena jako int
    public int ItemStatus { get; set; }

    public DateOnly? AcquisitionDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? AcquisitionPrice { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? QRCodeValue { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public int? ModifiedByUserId { get; set; }

    // Navigační vlastnosti
    [ForeignKey(nameof(CategoryId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual inventorySchema.Category Category { get; set; } = null!;

    [ForeignKey(nameof(ManufacturerId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual dbo.Manufacturer? Manufacturer { get; set; }

    [ForeignKey(nameof(AssignedLocationId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    [InverseProperty(nameof(dbo.Location.AssignedInventoryItems))]
    public virtual dbo.Location? AssignedLocation { get; set; }

    [ForeignKey(nameof(CurrentLocationId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    [InverseProperty(nameof(dbo.Location.CurrentInventoryItems))]
    public virtual dbo.Location? CurrentLocation { get; set; }
}
```

> Protože `InventoryItem` odkazuje na `Location` dvěma různými FK (AssignedLocation / CurrentLocation), je nutný `[InverseProperty]`. Odpovídající kolekce `AssignedInventoryItems` a `CurrentInventoryItems` je třeba přidat do třídy `Location`.

### 5.2 Konkrétní typ `Equipment`

**Soubor:** `src/SportSys.Database/Models/inventory/Equipment.cs`  
**Namespace:** `SportSys.Database.Models.inventory`

```csharp
[Table(nameof(Equipment), Schema = Schemas.Inventory)]
public partial class Equipment : InventoryItem
{
    public int? SizeId { get; set; }

    [ForeignKey(nameof(SizeId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual inventorySchema.Size? Size { get; set; }
}
```

### 5.3 Konkrétní typ `Asset`

**Soubor:** `src/SportSys.Database/Models/inventory/Asset.cs`  
**Namespace:** `SportSys.Database.Models.inventory`

```csharp
[Table(nameof(Asset), Schema = Schemas.Inventory)]
public partial class Asset : InventoryItem
{
    [StringLength(100)]
    public string? SerialNumber { get; set; }

    public DateOnly? WarrantyUntil { get; set; }

    [StringLength(100)]
    public string? ExternalId { get; set; }
}
```

### 5.4 Konfigurační soubor TPC + sekvence

**Soubor:** `src/SportSys.Database/Configurations/inventory/InventoryItemConfiguration.cs`

```csharp
public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        // TPC – InventoryItem nemá vlastní tabulku; UseTpcMappingStrategy nelze vyjádřit atributem.
        builder.UseTpcMappingStrategy();
    }
}
```

**Soubor:** `src/SportSys.Database/Configurations/inventory/EquipmentConfiguration.cs`

```csharp
public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        // TPC dědičnost nepodporuje pojmenované DEFAULT constrainty.
        builder.Property(e => e.Id)
               .HasDefaultValueSql("(NEXT VALUE FOR [inventory].[InventoryItemSeq])");
    }
}
```

**Soubor:** `src/SportSys.Database/Configurations/inventory/AssetConfiguration.cs`

```csharp
public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.Property(e => e.Id)
               .HasDefaultValueSql("(NEXT VALUE FOR [inventory].[InventoryItemSeq])");
    }
}
```

### 5.5 Aktualizace `dbo.Location` – navigační vlastnosti pro InverseProperty

Do existující třídy `Location` (nebo jejího partialu) přidat dvě kolekce:

```csharp
[InverseProperty(nameof(InventoryItem.AssignedLocation))]
public virtual ICollection<InventoryItem> AssignedInventoryItems { get; set; } = new List<InventoryItem>();

[InverseProperty(nameof(InventoryItem.CurrentLocation))]
public virtual ICollection<InventoryItem> CurrentInventoryItems { get; set; } = new List<InventoryItem>();
```

> Pokud je `Location.cs` generovaný soubor, použij partial třídu `Location.Inventory.cs`.

---

## Fáze 6 – Transakční a auditní entity

> **Poznámka k TPC FK:** Entity `Loan`, `InventoryTransaction`, `InventoryItemPurchase`, `ItemLocationHistory` a `InventoryCheck` mají `InventoryItemId` odkazující na abstraktní typ bez fyzické tabulky. **DB-level FK constraint nelze vynutit** (stejné omezení TPC jako u SportEvent). Integrita se zajišťuje aplikačně. V modelech se navigace na `InventoryItem` nedefinuje, nebo se označí jako `[NotMapped]` pokud je potřeba.

### 6.1 `inventory.Loan`

**Soubor:** `src/SportSys.Database/Models/inventory/Loan.cs`  
**Namespace:** `SportSys.Database.Models.inventory`

```csharp
[Table(nameof(Loan), Schema = Schemas.Inventory)]
public partial class Loan
{
    [Key]
    public int Id { get; set; }

    // Odkazuje na Equipment nebo Asset – DB FK constraint nelze vynutit (TPC omezení)
    public int InventoryItemId { get; set; }

    public int MemberId { get; set; }

    public DateOnly LoanDate { get; set; }

    public DateOnly? ExpectedReturnDate { get; set; }

    public DateOnly? ReturnedDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public bool IsClosed { get; set; }

    [ForeignKey(nameof(MemberId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual identity.User Member { get; set; } = null!;
}
```

### 6.2 `inventory.InventoryTransaction`

**Soubor:** `src/SportSys.Database/Models/inventory/InventoryTransaction.cs`  
**Namespace:** `SportSys.Database.Models.inventory`

```csharp
[Table(nameof(InventoryTransaction), Schema = Schemas.Inventory)]
[Index(nameof(InventoryItemId), nameof(TransactionDate), Name = "IX_InventoryTransaction_ItemDate")]
public partial class InventoryTransaction
{
    [Key]
    public int Id { get; set; }

    // TPC omezení – DB FK constraint nelze vynutit
    public int InventoryItemId { get; set; }

    public int TransactionTypeId { get; set; }

    public DateTime TransactionDate { get; set; }

    public int Quantity { get; set; }

    public int? UserId { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [ForeignKey(nameof(TransactionTypeId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual inventorySchema.TransactionType TransactionType { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual identity.User? User { get; set; }
}
```

### 6.3 `inventory.PurchaseDocument`

**Soubor:** `src/SportSys.Database/Models/inventory/PurchaseDocument.cs`  
**Namespace:** `SportSys.Database.Models.inventory`

```csharp
[Table(nameof(PurchaseDocument), Schema = Schemas.Inventory)]
public partial class PurchaseDocument
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public required string DocumentNumber { get; set; }

    [StringLength(200)]
    public required string SupplierName { get; set; }

    public DateOnly PurchaseDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public virtual ICollection<InventoryItemPurchase> InventoryItemPurchases { get; set; } = new List<InventoryItemPurchase>();
}
```

### 6.4 `inventory.InventoryItemPurchase`

**Soubor:** `src/SportSys.Database/Models/inventory/InventoryItemPurchase.cs`  
**Namespace:** `SportSys.Database.Models.inventory`

```csharp
[Table(nameof(InventoryItemPurchase), Schema = Schemas.Inventory)]
public partial class InventoryItemPurchase
{
    [Key]
    public int Id { get; set; }

    // TPC omezení – DB FK constraint nelze vynutit
    public int InventoryItemId { get; set; }

    public int PurchaseDocumentId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PurchasePrice { get; set; }

    [ForeignKey(nameof(PurchaseDocumentId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual PurchaseDocument PurchaseDocument { get; set; } = null!;
}
```

### 6.5 `inventory.ItemLocationHistory`

**Soubor:** `src/SportSys.Database/Models/inventory/ItemLocationHistory.cs`  
**Namespace:** `SportSys.Database.Models.inventory`

```csharp
[Table(nameof(ItemLocationHistory), Schema = Schemas.Inventory)]
[Index(nameof(InventoryItemId), nameof(ChangedAt), Name = "IX_ItemLocationHistory_ItemDate")]
public partial class ItemLocationHistory
{
    [Key]
    public int Id { get; set; }

    // TPC omezení – DB FK constraint nelze vynutit
    public int InventoryItemId { get; set; }

    public int? PreviousLocationId { get; set; }

    public int NewLocationId { get; set; }

    public DateTime ChangedAt { get; set; }

    public int? ChangedByUserId { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [ForeignKey(nameof(PreviousLocationId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual dbo.Location? PreviousLocation { get; set; }

    [ForeignKey(nameof(NewLocationId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual dbo.Location NewLocation { get; set; } = null!;

    [ForeignKey(nameof(ChangedByUserId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual identity.User? ChangedByUser { get; set; }
}
```

> `ItemLocationHistory` odkazuje na `Location` dvěma FK → ověřit, zda EF Core odvozuje oba vztahy správně. Pokud ne, přidat `[InverseProperty]` do `Location`.

### 6.6 `inventory.InventorySession`

**Soubor:** `src/SportSys.Database/Models/inventory/InventorySession.cs`  
**Namespace:** `SportSys.Database.Models.inventory`

```csharp
[Table(nameof(InventorySession), Schema = Schemas.Inventory)]
public partial class InventorySession
{
    [Key]
    public int Id { get; set; }

    [StringLength(200)]
    public required string Name { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public bool IsClosed { get; set; }

    public virtual ICollection<InventoryCheck> InventoryChecks { get; set; } = new List<InventoryCheck>();
}
```

### 6.7 `inventory.InventoryCheck`

**Soubor:** `src/SportSys.Database/Models/inventory/InventoryCheck.cs`  
**Namespace:** `SportSys.Database.Models.inventory`

```csharp
[Table(nameof(InventoryCheck), Schema = Schemas.Inventory)]
[Index(nameof(InventorySessionId), nameof(InventoryItemId), IsUnique = true, Name = "UX_InventoryCheck_SessionItem")]
public partial class InventoryCheck
{
    [Key]
    public int Id { get; set; }

    public int InventorySessionId { get; set; }

    // TPC omezení – DB FK constraint nelze vynutit
    public int InventoryItemId { get; set; }

    public DateTime CheckedAt { get; set; }

    public int? CheckedByUserId { get; set; }

    public bool Found { get; set; }

    public int? ActualLocationId { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [ForeignKey(nameof(InventorySessionId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public virtual InventorySession InventorySession { get; set; } = null!;

    [ForeignKey(nameof(ActualLocationId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual dbo.Location? ActualLocation { get; set; }

    [ForeignKey(nameof(CheckedByUserId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual identity.User? CheckedByUser { get; set; }
}
```

---

## Fáze 7 – Aktualizace `SportSysDbContext`

### 7.1 Přidat DbSet pro všechny nové entity

```csharp
// dbo
public DbSet<Manufacturer> Manufacturers { get; set; }
public DbSet<Location> Locations { get; set; }

// inventory – lookup
public DbSet<inventorySchema.Category> InventoryCategories { get; set; }
public DbSet<inventorySchema.Size> InventorySizes { get; set; }
public DbSet<inventorySchema.CategorySize> InventoryCategorySizes { get; set; }
public DbSet<inventorySchema.TransactionType> InventoryTransactionTypes { get; set; }

// inventory – TPC hierarchy
public DbSet<Equipment> Equipment { get; set; }
public DbSet<Asset> Assets { get; set; }

// inventory – transakční
public DbSet<Loan> Loans { get; set; }
public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
public DbSet<PurchaseDocument> PurchaseDocuments { get; set; }
public DbSet<InventoryItemPurchase> InventoryItemPurchases { get; set; }
public DbSet<ItemLocationHistory> ItemLocationHistories { get; set; }
public DbSet<InventorySession> InventorySessions { get; set; }
public DbSet<InventoryCheck> InventoryChecks { get; set; }
```

### 7.2 Přidat do `OnModelCreating`

```csharp
// Sekvence pro sdílené ID Equipment a Asset
modelBuilder.HasSequence<int>("InventoryItemSeq", Schemas.Inventory).StartsAt(1).IncrementsBy(1);

// Aplikovat konfigurační soubory
modelBuilder.ApplyConfiguration(new InventoryItemConfiguration());
modelBuilder.ApplyConfiguration(new EquipmentConfiguration());
modelBuilder.ApplyConfiguration(new AssetConfiguration());
modelBuilder.ApplyConfiguration(new TransactionTypeConfiguration());
```

---

## Fáze 8 – EF Core migrace

Po implementaci všech předchozích fází spustit:

```bash
dotnet ef migrations add AddInventoryModule --project src/SportSys.Database
```

Zkontrolovat vygenerovanou migraci:

- [ ] Vytváří se schéma `inventory` (pokud EF Core negeneruje `CREATE SCHEMA`, přidat ručně do migrace)
- [ ] Sekvence `inventory.InventoryItemSeq` je přítomna
- [ ] Tabulky `Equipment` a `Asset` mají `DEFAULT (NEXT VALUE FOR [inventory].[InventoryItemSeq])` na sloupci `Id`
- [ ] Seed data pro `inventory.TransactionType` jsou vygenerována
- [ ] FK constraints na `Loan.InventoryItemId`, `InventoryTransaction.InventoryItemId` atd. **nejsou** generovány (TPC omezení – EF Core je negeneruje automaticky)
- [ ] Složené indexy jsou přítomny (`UX_InventoryCheck_SessionItem`, `IX_InventoryTransaction_ItemDate`, atd.)

Aplikovat migraci:

```bash
dotnet ef database update --project src/SportSys.Database
```

---

## Fáze 9 – Ověřovací kontrolní seznam

### Modely

- [ ] Každý model má `[Table(nameof(X), Schema = Schemas.Inventory)]`
- [ ] `InventoryItem` je `abstract` a nemá `[Table]` atribut
- [ ] `Equipment` a `Asset` mají `[Table]` atribut
- [ ] Nullable reference types (`?`) jsou správně anotovány
- [ ] `[StringLength]`, `[Unicode]`, `[Precision]`, `[Column(TypeName)]` jsou na místě

### Enumerace

- [ ] `EItemStatus`, `ETransactionType`, `ECategoryType` jsou v `Models/inventory/` s namespace `SportSys.Database.Models.inventory`
- [ ] Každý enum člen má `[Display(..., ResourceType = typeof(Resources.E{Enum}))]` s fully-qualified typem
- [ ] Resources namespace není importován v enum souborech (kolize názvů – Resources třída a enum mají stejné jméno)
- [ ] Trojice RESX souborů existuje pro každý enum

### Konfigurace

- [ ] `InventoryItemConfiguration` volá `UseTpcMappingStrategy()`
- [ ] `EquipmentConfiguration` a `AssetConfiguration` volají `HasDefaultValueSql` pro `Id`
- [ ] `TransactionTypeConfiguration` má `HasData`
- [ ] Všechny konfigurace jsou registrovány v `OnModelCreating`

### DbContext

- [ ] `HasSequence<int>("InventoryItemSeq", Schemas.Inventory)` je v `OnModelCreating`
- [ ] Všechny `DbSet<T>` jsou přidány

### Build

```bash
dotnet build SportSys.slnx
```

Projekt musí sestavit bez chyb a varování před vytvořením migrace.

---

## Poznámky pro implementátora

1. **Pořadí implementace:** Dodržuj pořadí fází. Fáze 5 (TPC) závisí na fázi 2 (Location, Manufacturer) a fázi 4 (Category). Fáze 6 závisí na fázi 5.

2. **TPC FK constraint:** Entity `Loan`, `InventoryTransaction`, `InventoryItemPurchase`, `ItemLocationHistory`, `InventoryCheck` mají `InventoryItemId` bez DB FK constraintu. Toto je vědomé rozhodnutí plynoucí z omezení TPC strategie. Integrita musí být vynucena v Contract servisech.

3. **InverseProperty pro Location:** Třída `Location` bude mít kolekce pro `AssignedInventoryItems` a `CurrentInventoryItems`. Pokud je soubor generovaný, vytvoř `Location.Inventory.cs` partial.

4. **Schéma v migraci:** EF Core nevytváří automaticky SQL schéma `CREATE SCHEMA [inventory]`. Přidej řádek ručně do `Up()` metody migrace, pokud chybí.

5. **Seed data pro Category:** Výchozí stromová struktura kategorií je volitelná pro tuto fázi. Lze doplnit v follow-up migraci nebo přes administrátorské UI.
