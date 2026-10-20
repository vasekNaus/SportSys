using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.hr;
using SportSys.Database.Models.sportSchema;

namespace SportSys.Database.Models.sport;

[PrimaryKey(nameof(CoachId), nameof(MatchId), nameof(ParticipationTypeId))]
[Table(nameof(CoachMatch), Schema = Schemas.Sport)]
[Index(nameof(MatchId), Name = "IX_CoachMatch_Match")]
[Index(nameof(ParticipationTypeId), Name = "IX_CoachMatch_ParticipationType")]
public class CoachMatch
{
    [Key]
    public int CoachId { get; set; }

    [Key]
    public int MatchId { get; set; }

    [Key]
    public int ParticipationTypeId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public required string Note { get; set; }

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    [ForeignKey(nameof(CoachId))]
    public Coach Coach { get; set; } = null!;

    [ForeignKey(nameof(MatchId))]
    public Match Match { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    [ForeignKey(nameof(ParticipationTypeId))]
    public ParticipationType ParticipationType { get; set; } = null!;
}
