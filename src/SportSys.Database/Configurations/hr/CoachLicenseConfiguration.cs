using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportSys.Database.Models.hr;

namespace SportSys.Database.Configurations.hr;

public class CoachLicenseConfiguration : IEntityTypeConfiguration<CoachLicense>
{
    public void Configure(EntityTypeBuilder<CoachLicense> builder)
    {
        builder.ToTable(table =>
            table.HasCheckConstraint(
                "CK_CoachLicense_Validity",
                "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]"));
    }
}
