---
name: new-ef-entity
description: >
  Přidá novou EF Core entitu do databáze — model, datové atributy,
  konfiguraci Fluent API (pokud je potřeba), bez vytvoření databázové migrace.
  Použij tento skill když přidáváš novou tabulku nebo rozšiřuješ
  existující databázové schéma.
user-invocable: true
---

# Přidání nové EF Core entity

## Kdy použít

- Přidáváš novou databázovou tabulku
- Přidáváš entitu do existující TPC hierarchie
- Přidáváš novou relaci mezi existujícími entitami

## Předpoklady

- Znáš DB schéma (viz `Models/Schemas.cs`: `dbo`, `sport`, `identity`, `inventory`)
- Víš, zda entita potřebuje Fluent API (computed columns, TPC, sekvence, seed data)
- Znáš vztahy s existujícími entitami

## Postup

### 1. Vytvořit model

Vytvoř `src/{App}.Database/Models/{schema}/{Entity}.cs`.

**Klíčová pravidla:**
- `[Table(nameof(Entity), Schema = Schemas.X)]` — **povinné**, bez atributu EF Core tabulku nenajde
- `nameof` všude místo string literálů
- Nullable vlastnosti označit `?`
- FK pojmenovat `{Navigace}Id` reálný a správný název pro sloupec v databázi zajistí extension metoda `IdConvention()`
- k navigačním vlastnostem nedávat klíčové slovo virtual

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using {App}.Database.Models;

namespace {App}.Database.Models.sport;

[Table(nameof(IceRink), Schema = Schemas.Sport)]
[Index(nameof(Name), IsUnique = true, Name = "UX_IceRink_Name")]
public class IceRink
{
    [Key]
    public int Id { get; set; }

    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? City { get; set; }

    // FK — použít atribut [ForeignKey] pouze pro složené FK nebo sdílené sloupce
    public int? SeasonId { get; set; }

    [ForeignKey(nameof(Season_Id))]     // ← jen pokud konvence nestačí
    public Season? Season { get; set; }

    // Navigační kolekce
    public ICollection<Training> Trainings { get; set; } = [];
}
```

### 2. Přidat `DbSet<T>` do DbContext

V `src/{App}.Database/{App}DbContext.cs` přidat:

```csharp
public DbSet<IceRink> IceRinks => Set<IceRink>();
```

### 3. Vytvořit konfiguraci Fluent API (jen pokud potřeba)

Konfigurace **NENÍ potřeba** pro entity bez:
- computed columns
- pojmenovaných default constraints
- TPC hierarchie
- value convertorů
- seed dat (mimo lookup tabulky)

Pokud potřeba, vytvoř `src/{App}.Database/Configurations/{schema}/{Entity}Configuration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {App}.Database.Models.sport;

namespace {App}.Database.Configurations.sport;

public class TrainingConfiguration : IEntityTypeConfiguration<Training>
{
    public void Configure(EntityTypeBuilder<Training> builder)
    {
        // Computed column — NELZE přes atribut
        builder.Property(e => e.DurationMinutes)
               .HasComputedColumnSql("DATEDIFF(minute, TimeFrom, TimeTo)", stored: true);

        // Default constraint s pojmenováním — NELZE přes atribut
        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()", "DF_Training_CreatedAt");
    }
}
```

> Konfigurace se registruje automaticky přes `ApplyConfigurationsFromAssembly()` — není třeba nic přidávat do DbContext.


## Omezení
- ❌ Nikdy nepřidávej ani neupravuj migraci — vytvoření migrace provádí výhradně uživatel
- ❌ Neprováděj build solution
- ❌ Nepřidávat `HasColumnName` pro běžné FK sloupce — Apollo `IdConvention()` je pojmenuje automaticky
- ❌ Nepřidávat automatické indexy na FK — `ForeignKeyIndexConvention` je odstraněna, přidávej indexy explicitně
- ❌ Každý model musí mít `[Table]` atribut — bez něj EF Core tabulku nenajde (`TableNameFromDbSetConvention` je odstraněna)
- ❌ Nikdy nepoužívat string literály v atributech — vždy `nameof` a `Schemas.X`

## Checklist

- [ ] Model vytvořen v `Models/{schema}/`
- [ ] `[Table(nameof(X), Schema = Schemas.Y)]` přítomno
- [ ] `nameof` použito ve všech atributech
- [ ] Nullable vlastnosti označeny `?`
- [ ] FK pojmenovány dle konvence `{Navigace}_Id`
- [ ] `DbSet<T>` přidáno do DbContext
- [ ] Konfigurace Fluent API vytvořena pouze pokud je potřeba


## Reference

- `docs/conventions.md` — kompletní EF Core konvence
- `docs/architecture.md` — architektura vrstev, schémata
- `src/{App}.Database/Models/Schemas.cs` — konstanty schémat
- `src/{App}.Database/Models/sport/Training.cs` — vzorový model
