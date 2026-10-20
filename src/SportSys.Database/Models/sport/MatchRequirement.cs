#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportSys.Database.Models.sport;

[Table(nameof(MatchRequirement), Schema = Schemas.Sport)]
[Index(nameof(SeasonId), nameof(SeasonCategoryCode), nameof(From), nameof(To), Name = "IX_MatchRequirement_SeasonCategory_Period")]
public partial class MatchRequirement
{
    [Key]
    public int Id { get; set; }

    public int SeasonId { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public required string SeasonCategoryCode { get; set; }

    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    public int MatchCount { get; set; }

    public virtual ICollection<CoachMatchRequirement> CoachMatchRequirements { get; set; } = new List<CoachMatchRequirement>();

    [ForeignKey(nameof(SeasonId) + ", " + nameof(SeasonCategoryCode))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual SeasonCategory SeasonCategory { get; set; } = null!;
}
