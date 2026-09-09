using Microsoft.EntityFrameworkCore;
using Apollo.Data.Core.Extensions;
using SportSys.Database.Models.dboSchema;
using SportSys.Database.Models.sportSchema;
using SportSys.Database.Models.identity;
using MatchType = SportSys.Database.Models.sportSchema.MatchType;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SportSys.Database.Models;
using SportSys.Database.Models.dbo;
using SportSys.Database.Models.hr;
using SportSys.Database.Models.sport;
using SportSys.Database.Models.inventory;

namespace SportSys.Database.Context;

public class SportSysDbContext : IdentityDbContext<User, Role, int, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
{
  public SportSysDbContext(DbContextOptions<SportSysDbContext> options)
      : base(options)
  {
  }

  public virtual DbSet<Coach> Coaches { get; set; }

  public virtual DbSet<CoachSetting> CoachSettings { get; set; }

  public virtual DbSet<CoachLicenseType> CoachLicenseTypes { get; set; }

  public virtual DbSet<CoachLicense> CoachLicenses { get; set; }

  public virtual DbSet<CoachContract> CoachContracts { get; set; }

  public virtual DbSet<CoachRole> CoachRoles { get; set; }

  public virtual DbSet<CoachTraining> CoachTrainings { get; set; }

  public virtual DbSet<CoachTrainingRequirement> CoachTrainingRequirements { get; set; }

  public virtual DbSet<CoachTrainingPlan> CoachTrainingPlans { get; set; }

  public virtual DbSet<IceRink> IceRinks { get; set; }

  public virtual DbSet<Match> Matches { get; set; }

  public virtual DbSet<MatchType> MatchTypes { get; set; }

  public virtual DbSet<Team> Teams { get; set; }

  public virtual DbSet<ParticipationType> ParticipationTypes { get; set; }

  public virtual DbSet<Season> Seasons { get; set; }

  public virtual DbSet<SeasonCategory> SeasonCategories { get; set; }

  public virtual DbSet<Training> Training { get; set; }

  public virtual DbSet<TrainingRequirement> TrainingRequirements { get; set; }

  public virtual DbSet<TrainingGroup> TrainingGroups { get; set; }

  public virtual DbSet<TrainingPhase> TrainingPhases { get; set; }

  public virtual DbSet<TrainingPlan> TrainingPlans { get; set; }

  public virtual DbSet<TrainingPlanGroup> TrainingPlanGroups { get; set; }

  public virtual DbSet<TrainingState> TrainingStates { get; set; }

  public virtual DbSet<TrainingType> TrainingTypes { get; set; }

  //public virtual DbSet<Permission> Permissions { get; set; }

  //public virtual DbSet<BusinessRole> BusinessRoles { get; set; }

  //public virtual DbSet<BusinessRolePermission> BusinessRolePermissions { get; set; }

  //public virtual DbSet<UserBusinessRole> UserBusinessRoles { get; set; }

  // dbo – sdílené entity
  public virtual DbSet<Manufacturer> Manufacturers { get; set; }

  // inventory – lookup
  public virtual DbSet<Location> Locations { get; set; }
  public virtual DbSet<Category> InventoryCategories { get; set; }

  public virtual DbSet<ItemKind> InventoryItemKinds { get; set; }

  public virtual DbSet<TransactionType> InventoryTransactionTypes { get; set; }

  // inventory – TPC hierarchie
  public virtual DbSet<Equipment> Equipment { get; set; }

  public virtual DbSet<Asset> Assets { get; set; }

  // inventory – transakční
  public virtual DbSet<Loan> Loans { get; set; }

  public virtual DbSet<InventoryTransaction> InventoryTransactions { get; set; }

  public virtual DbSet<PurchaseDocument> PurchaseDocuments { get; set; }

  public virtual DbSet<InventoryItemPurchase> InventoryItemPurchases { get; set; }

  public virtual DbSet<ItemLocationHistory> ItemLocationHistories { get; set; }

  public virtual DbSet<InventorySession> InventorySessions { get; set; }

  public virtual DbSet<InventoryCheck> InventoryChecks { get; set; }

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
    //nechceme automaticky indexy na FK
    configurationBuilder.Conventions.Remove(typeof(ForeignKeyIndexConvention));

    configurationBuilder.Conventions.Remove<TableNameFromDbSetConvention>();
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    // Identity nastaví výchozí konfigurace tabulek (AspNetUsers, AspNetRoles atd.)
    base.OnModelCreating(modelBuilder);

    //zmnena pojmenovani tabulek s daty Identity
    modelBuilder.Entity<User>().ToTable(nameof(User), Schemas.Identity);
    modelBuilder.Entity<Role>().ToTable(nameof(Role), Schemas.Identity);
    modelBuilder.Entity<UserRole>().ToTable(nameof(UserRole), Schemas.Identity);
    modelBuilder.Entity<UserClaim>().ToTable(nameof(UserClaim), Schemas.Identity);
    modelBuilder.Entity<UserLogin>().ToTable(nameof(UserLogin), Schemas.Identity);
    modelBuilder.Entity<UserToken>().ToTable(nameof(UserToken), Schemas.Identity);
    modelBuilder.Entity<RoleClaim>().ToTable(nameof(RoleClaim), Schemas.Identity);

    modelBuilder.Entity<Role>(role =>
    {
      role.HasKey(ur => ur.Id);
      role.Property(r => r.Id).ValueGeneratedNever();
    });

    // Sdílená sekvence pro Training.Id a Match.Id (TPC vzor na úrovni DB)
    modelBuilder.HasSequence<int>("SportEventSeq", Schemas.Sport)
      .StartsAt(1)
      .IncrementsBy(1);

    // Sdílená sekvence pro Equipment.Id a Asset.Id (TPC vzor na úrovni DB)
    modelBuilder.HasSequence<int>("InventoryItemSeq", Schemas.Inventory)
      .StartsAt(1)
      .IncrementsBy(1);

    // Veškerá konfigurace entit je v IEntityTypeConfiguration<T> třídách v Configurations/
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(SportSysDbContext).Assembly);

    modelBuilder.IdConvention();
    modelBuilder.InitDatetime2();
  }
}