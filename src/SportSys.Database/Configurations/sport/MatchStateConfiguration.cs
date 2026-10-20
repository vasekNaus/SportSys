using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportSys.Database.Enums;
using SportSys.Database.Models.sportSchema;

namespace SportSys.Database.Configurations.sport;

public class MatchStateConfiguration : IEntityTypeConfiguration<MatchState>
{
    public void Configure(EntityTypeBuilder<MatchState> builder)
    {
        builder.HasData(
            Enum.GetValues<EMatchState>()
                .Select(e => new MatchState(e))
        );
    }
}
