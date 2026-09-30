using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportSys.Database.Models.sport;

namespace SportSys.Database.Configurations.sport;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.Property(e => e.ZipCode)
               .HasDefaultValue("", "DF_Location_ZipCode");

        builder.Property(e => e.IsActive)
               .HasDefaultValue(true, "DF_Location_IsActive");
    }
}
