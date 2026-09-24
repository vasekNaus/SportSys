using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportSys.Database.Models.hr;

[Table(nameof(CoachLicense), Schema = Schemas.Hr)]
[Index(nameof(CoachId), nameof(ValidFrom), nameof(ValidTo), Name = "IX_CoachLicense_Coach_Validity")]
[Index(nameof(CoachLicenseTypeId), Name = "IX_CoachLicense_CoachLicenseType")]
public class CoachLicense
{
    [Key]
    public int Id { get; set; }

    public int CoachId { get; set; }

    public int CoachLicenseTypeId { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Coach Coach { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public CoachLicenseType CoachLicenseType { get; set; } = null!;
}
