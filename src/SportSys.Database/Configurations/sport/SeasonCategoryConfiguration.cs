using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportSys.Database.Models.sport;

namespace SportSys.Database.Configurations.sport;

public class SeasonCategoryConfiguration : IEntityTypeConfiguration<SeasonCategory>
{
    public void Configure(EntityTypeBuilder<SeasonCategory> builder)
    {
        builder.Property(e => e.Name)
               .HasDefaultValue(string.Empty, "DF_SeasonCategory_Name");
        builder.Property(e => e.CompetitionCode)
               .HasDefaultValue(string.Empty, "DF_SeasonCategory_CompetitionCode");

        builder.Property(e => e.CompetitionTeamName)
               .HasDefaultValue(string.Empty, "DF_SeasonCategory_CompetitionTeamName");

        builder.Property(e => e.BirthYears)
               .HasDefaultValue("[]", "DF_SeasonCategory_BirthYears");

        builder.Property(e => e.IsActive)
               .HasDefaultValue(true, "DF_SeasonCategory_IsActive");
    }
}
