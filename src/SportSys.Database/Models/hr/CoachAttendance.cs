using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.identity;

namespace SportSys.Database.Models.hr;

[Table(nameof(CoachAttendance), Schema = Schemas.Hr)]
[Index(
    nameof(CoachId),
    nameof(PeriodYear),
    nameof(PeriodMonth),
    IsUnique = true,
    Name = "IX_CoachAttendance_Coach_Period")]
[Index(nameof(UserUploadId), Name = "IX_CoachAttendance_UserUpload")]
[Index(nameof(UploadedAt), Name = "IX_CoachAttendance_UploadedAt")]
public class CoachAttendance
{
    [Key]
    public int Id { get; set; }

    public int CoachId { get; set; }

    public int PeriodYear { get; set; }

    public byte PeriodMonth { get; set; }

    [StringLength(255)]
    public required string FileName { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public required string ContentType { get; set; }

    [Column(TypeName = "varbinary(max)")]
    public required byte[] FileContent { get; set; }

    [Column(TypeName = "datetime2(0)")]
    public DateTime UploadedAt { get; set; }

    public int UserUploadId { get; set; }

    public Coach Coach { get; set; } = null!;

    public User UserUpload { get; set; } = null!;
}
