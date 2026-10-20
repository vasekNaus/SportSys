using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportSys.Database.Models.sport;

namespace SportSys.Database.Configurations.sport;

public class CoachMatchConfiguration : IEntityTypeConfiguration<CoachMatch>
{
    public void Configure(EntityTypeBuilder<CoachMatch> builder)
    {
        builder.Property(e => e.Note)
            .HasDefaultValue(string.Empty, "DF_CoachMatch_Note");
    }
}
