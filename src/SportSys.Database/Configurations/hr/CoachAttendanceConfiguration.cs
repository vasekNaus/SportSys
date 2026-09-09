using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportSys.Database.Models.hr;

namespace SportSys.Database.Configurations.hr;

public class CoachAttendanceConfiguration : IEntityTypeConfiguration<CoachAttendance>
{
    public void Configure(EntityTypeBuilder<CoachAttendance> builder)
    {
        builder.HasOne(e => e.Coach)
            .WithMany(e => e.Attendances)
            .HasForeignKey(e => e.CoachId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.UserUpload)
            .WithMany(e => e.UploadedCoachAttendances)
            .HasForeignKey(e => e.UserUploadId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_CoachAttendance_PeriodYear",
                "[PeriodYear] BETWEEN 1 AND 9999");
            table.HasCheckConstraint(
                "CK_CoachAttendance_PeriodMonth",
                "[PeriodMonth] BETWEEN 1 AND 12");
        });
    }
}
