#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.dboSchema;
using SportSys.Database.Models.hr;

namespace SportSys.Database.Models.sport;

[PrimaryKey(nameof(CoachId), nameof(MatchRequirementId), nameof(CoachRoleId))]
[Table(nameof(CoachMatchRequirement), Schema = Schemas.Sport)]
[Index(nameof(MatchRequirementId), Name = "IX_CoachMatchRequirement_MatchRequirement")]
[Index(nameof(CoachRoleId), Name = "IX_CoachMatchRequirement_CoachRole")]
public partial class CoachMatchRequirement
{
    [Key]
    public int CoachId { get; set; }

    [Key]
    public int MatchRequirementId { get; set; }

    [Key]
    public int CoachRoleId { get; set; }

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual Coach Coach { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual CoachRole CoachRole { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual MatchRequirement MatchRequirement { get; set; } = null!;
}
