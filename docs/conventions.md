# Konvence kódu — SportSys

## EF Core migrace

❌ **Agent nikdy nevytváří EF Core migrace.** Při změně databázového modelu upraví entity,
`DbContext` a Fluent API konfigurace, ale příkaz `dotnet ef migrations add` nespouští a
nevytváří ani neupravuje soubory v `src/SportSys.Database/Migrations/`.

Vytvoření a aplikaci migrace provádí výhradně uživatel po kontrole změn modelu.

## EF Core: Data atributy vs. Fluent API

**Data atributy mají přednost.** Fluent API se používá výhradně pro věci, které atributy neumožňují.

| Konfigurace | Atribut |
|---|---|
| Tabulka + schéma | `[Table(nameof(Training), Schema = Schemas.Sport)]` |
| Primární klíč | `[Key]` / `[PrimaryKey(nameof(A), nameof(B))]` |
| FK + navigace | `[ForeignKey(nameof(PropertyId))]` na navigaci |
| Chování při mazání | `[DeleteBehavior(DeleteBehavior.Cascade)]` na navigaci |
| Délka řetězce | `[StringLength(50)]` |
| Unicode / VARCHAR | `[Unicode(false)]` |
| Přesnost | `[Precision(0)]` |
| Název sloupce | `[Column("sloupec")]` |
| Typ sloupce | `[Column(TypeName = "decimal(5,2)")]` |
| Unikátní index | `[Index(nameof(Prop), IsUnique = true)]` (na třídě) |
| Složený index | `[Index(nameof(A), nameof(B), Name = "IX_...")]` (na třídě) |
| Inverzní navigace | `[InverseProperty(nameof(Other.Nav))]` |

**Výhradně Fluent API** (atribut neexistuje):
- `HasComputedColumnSql(...)` — persisted computed sloupce
- `HasDefaultValue(...)` / `HasDefaultValueSql(...)` s pojmenovaným constraintem
- `UseTpcMappingStrategy()` — TPC dědičnost
- `UseTptMappingStrategy()` — TPT dědičnost
- `HasConversion(...)` — value convertory
- `HasData(...)` — seed data
- `HasSequence(...)` — databázové sekvence

Konfigurační soubor v `Configurations/` vzniká **jen tehdy**, když je co vyjádřit pomocí Fluent API.

---

## Schémata — třída `Schemas`

Názvy DB schémat jsou `const string` v `Models/Schemas.cs`. Nikdy string literál.

```csharp
[Table(nameof(Training), Schema = Schemas.Sport)]
[Table(nameof(Coach),    Schema = Schemas.Hr)]
```

> **Proč `const`, ne `static readonly`?** Atributy vyžadují compile-time konstanty.

---

## `nameof` místo string literálů

Všude, kde atribut přijímá název C# symbolu, používat `nameof`.

```csharp
// ❌ Špatně
[Table("Training", Schema = "sport")]
[ForeignKey("Season_Id")]

// ✅ Správně
[Table(nameof(Training), Schema = Schemas.Sport)]
[ForeignKey(nameof(Season_Id))]
```

**Složený FK** — `nameof` vrací compile-time konstantu, konkatenace je platná:

```csharp
[ForeignKey(nameof(Season_Id) + ", " + nameof(SeasonCategory_Name))]
```

---

## `[ForeignKey]` — jen když konvence nestačí

EF Core odvozuje FK z konvence `{NázevNavigace}_Id`. Atribut přidávat jen pro:

| Situace | Nutný? |
|---|---|
| FK splňuje konvenci, jediný vztah | ❌ Ne |
| Složený FK | ✅ Ano |
| Sdílený sloupec ve dvou FK téže entity | ✅ Ano |
| FK sloupec je zároveň součástí PK | ✅ Ano |

---

## `[InverseProperty]` — jen při více vztazích

Nutné pouze při více vztazích mezi stejnou dvojicí entit.

---

## `HasDefaultValue` — vždy pojmenovat constraint

```csharp
builder.Property(e => e.ZipCode).HasDefaultValue("", "DF_IceRink_ZipCode");
// vzor: DF_{TabulkaBezSchématu}_{Sloupec}
```

EF Core bez explicitního názvu generuje náhodný hash (např. `DF__IceRink__ZipCode__3A4CA8FD`) — obtížně referencovatelný v migracích.

---

## Lookup (číselníkové) tabulky

Lookup tabulky: `TrainingType`, `TrainingState`, `TrainingPhase`, `ParticipationType`, `MatchType`, `CoachRole`; z modulu Inventory: `TransactionType`. Vzor: `int Id` (PK) + `required string Name`.

### Enum pro každou lookup tabulku

Umístění: `src/SportSys.Database/Enums/E{Název}.cs`

Pravidla:
- Pojmenování: `E{Název}` (prefixový vzor) — `ETrainingType`, `EMatchType`
- Členy anglicky (neutrální klíč), PascalCase, int hodnoty od 1, nikdy nula
- ❌ Nikdy měnit existující int hodnoty — způsobí korupci FK dat
- ❌ Nikdy mazat hodnotu pokud na ní mohou existovat FK záznamy

Každý enum člen:
```csharp
[Display(Name = "Ice", ResourceType = typeof(SportSys.Database.Resources.ETrainingType))]
Ice = 2
```

> Wrapper třída `SportSys.Database.Resources.ETrainingType` a enum mají stejný název ale různý namespace — neimportovat Resources namespace v enum souboru, použít fully-qualified type.

### Konstruktory pro seeding

Patří do `{Entity}.Seed.cs` (partial třída) — auto-generated modely se neupravují:

```csharp
public partial class TrainingType
{
    private TrainingType() { Name = null!; }

    [SetsRequiredMembers]
    public TrainingType(ETrainingType id) { Id = (int)id; Name = id.ToString(); }
}
```

### Seeding

```csharp
builder.HasData(Enum.GetValues<ETrainingType>().Select(e => new TrainingType(e)));
```

### Lokalizace

`Name` v DB je neutrální klíč (`enum.ToString()`), nikdy lokalizovaný text.

Každý enum má trojici RESX v `src/SportSys.Database/Resources/`:
- `E{Enum}.cs` — ruční ResourceManager wrapper
- `E{Enum}.resx` — anglický fallback
- `E{Enum}.cs.resx` — české překlady

### `MatchType` koliduje se `System.IO.MatchType`

V konfiguračním souboru použij alias:
```csharp
using MatchType = SportSys.Database.Models.sportSchema.MatchType;
```

---

## TPC dědičnost

TPC je výchozí preferovaná strategie dědičnosti v projektu.

Vzor sdílené sekvence (`SportEvent → Training / Match`, `InventoryItem → Equipment / Asset`):
- Fyzická tabulka abstraktního předka neexistuje
- Sdílená sekvence zajišťuje unikátní ID napříč konkrétními tabulkami
- `UseTpcMappingStrategy()` v Fluent API
- Apollo `IdConvention()` pojmenuje FK sloupce zděděné z bázové třídy automaticky — ❌ nepřidávat `HasColumnName` pro zděděné FK

**Výjimka:** FK sdílené ve dvou navigacích TPC hierarchie (např. `SeasonId`, `SeasonCategoryName` v `SportEventConfiguration`) — zde `HasColumnName` explicitně nastavit.

### TPT výjimka pro Identity

`User → Coach` používá TPT podle
`docs/decisions/adr-004-coach-jako-tpt-potomek-user.md`, protože trenér musí
sdílet fyzický řádek a PK s `identity.User`.

- `User` zůstává neabstraktní základní typ.
- `hr.Coach.Id` je PK/FK na `identity.User.Id`.
- Společná profilová pole patří pouze do `identity.User`.
- TPT konfigurace je v `Configurations/identity/UserConfiguration.cs`.
- Tuto výjimku nerozšiřovat na další hierarchie bez samostatného ADR.

---

## Computed columns

`DurationMinutes` je persisted computed column (`DATEDIFF(minute, TimeFrom, TimeTo)`) v tabulkách `Training`, `Match` i `TrainingPlan`. ❌ Nikdy počítat v C# kódu.

---

## Nullable a ImplicitUsings

Všechny projekty: `<Nullable>enable</Nullable>` + `<ImplicitUsings>enable</ImplicitUsings>`. Vyhýbat se `null!` bez vysvětlujícího komentáře.

---

## Frontend

Frontendové konvence mají jediný zdroj pravdy v
`docs/modules/frontend.md`. Zejména platí, že
`src/SportSys.Razor/wwwroot/css/site.css` je generovaný výstup a ručně se
neupravuje.

---

## Reference

- `docs/modules/auth.md` — autentizace a autorizace
- `docs/modules/frontend.md` — SCSS struktura, barevné schéma
- `docs/modules/inventory.md` — TPC a aplikační integrita skladu
- `.github/skills/lookup-table/SKILL.md` — přidání nové lookup tabulky
- `.github/skills/has-default-value/SKILL.md` — pojmenované DB default constraints
- `.github/skills/new-ef-entity/SKILL.md` — přidání nové EF Core entity
