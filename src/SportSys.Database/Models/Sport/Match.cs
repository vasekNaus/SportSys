#nullable enable
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.sportSchema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportSys.Database.Models.sport;

[Table(nameof(Match), Schema = Schemas.Sport)]
public partial class Match : SportEvent
{
    [StringLength(10)]
    [Unicode(false)]
    public string? MatchCode { get; set; }

    public int HomeTeamId { get; set; }

    public int AwayTeamId { get; set; }

    [Precision(0)]
    public TimeOnly TimeTo { get; set; }

    public int? DurationMinutes { get; set; }

    public MatchResult? Result { get; set; }

    public int MatchTypeId { get; set; }

    public int? MatchStateId { get; set; }

    [ForeignKey(nameof(HomeTeamId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual Team HomeTeam { get; set; } = null!;

    [ForeignKey(nameof(AwayTeamId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual Team AwayTeam { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual sportSchema.MatchType MatchType { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual sportSchema.MatchState? MatchState { get; set; }
}
