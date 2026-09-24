using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportSys.Database.Models;
using SportSys.Database.Models.identity;

namespace SportSys.Database.Configurations.identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> builder)
  {
    // TPT je zde výjimkou z preferovaného TPC: Coach musí sdílet fyzický Identity řádek.
    builder.UseTptMappingStrategy();
    builder.ToTable(nameof(User), Schemas.Identity);
  }
}
