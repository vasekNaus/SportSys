using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportSys.Database.Models.hr;

namespace SportSys.Database.Configurations.hr;

public class CoachContractConfiguration : IEntityTypeConfiguration<CoachContract>
{
    public void Configure(EntityTypeBuilder<CoachContract> builder)
    {
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true, "DF_CoachContract_IsActive");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_CoachContract_ContractType",
                "[ContractType] IN (1, 2)");
            table.HasCheckConstraint(
                "CK_CoachContract_RewardAmount",
                "[RewardAmount] >= 0");
        });
    }
}
