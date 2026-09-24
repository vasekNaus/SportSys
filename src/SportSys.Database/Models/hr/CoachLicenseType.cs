using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportSys.Database.Models.hr;

[Table(nameof(CoachLicenseType), Schema = Schemas.Hr)]
[Index(nameof(Code), IsUnique = true, Name = "UX_CoachLicenseType_Code")]
public class CoachLicenseType
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public required string Code { get; set; }

    [StringLength(100)]
    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<CoachLicense> Licenses { get; set; } = [];
}
