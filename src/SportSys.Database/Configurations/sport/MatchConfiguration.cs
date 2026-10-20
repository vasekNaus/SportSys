using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportSys.Database.Models.sport;

namespace SportSys.Database.Configurations.sport;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        // TPC dědičnost nepodporuje pojmenované DEFAULT constrainty – nelze použít dvouparametrový overload.
        builder.Property(e => e.Id)
               .HasDefaultValueSql("(NEXT VALUE FOR [sport].[SportEventSeq])");

        builder.Property(e => e.DurationMinutes)
               .HasComputedColumnSql("(datediff(minute,[TimeFrom],[TimeTo]))", stored: true);

        // Nullable sloupec, ale nově vytvářené zápasy mají dostat stav "Plán" (Id 1),
        // pokud volající kód hodnotu explicitně nenastaví. TPC dědičnost nepodporuje
        // pojmenované DEFAULT constrainty (viz komentář u Id výše) – jednoparametrový overload.
        builder.Property(e => e.MatchStateId)
               .HasDefaultValueSql("(1)");

        // Výsledek zápasu jako JSON sloupec přes value converter.
        // OwnsOne...ToJson není kompatibilní s TPC dědičností (EF Core omezení).
        builder.Property(e => e.Result)
               .HasColumnName("Result")
               .HasColumnType("json")
               .HasConversion(
                   v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                   v => v == null ? null : JsonSerializer.Deserialize<MatchResult>(v, (JsonSerializerOptions?)null));
    }
}
