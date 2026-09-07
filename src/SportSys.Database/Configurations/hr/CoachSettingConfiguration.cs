using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportSys.Database.Models.hr;

namespace SportSys.Database.Configurations.hr;

public class CoachSettingConfiguration : IEntityTypeConfiguration<CoachSetting>
{
    public void Configure(EntityTypeBuilder<CoachSetting> builder)
    {
        builder.ToTable(table =>
            table.HasCheckConstraint(
                "CK_CoachSetting_Validity",
                "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]"));
    }
}
