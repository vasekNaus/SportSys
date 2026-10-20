#nullable enable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.sport;

namespace SportSys.Database.Models.sportSchema;

[Table(nameof(MatchState), Schema = Schemas.Sport)]
public partial class MatchState
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public required string Name { get; set; }

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
}
