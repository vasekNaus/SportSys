
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.dboSchema;


namespace SportSys.Database.Models.sport;

[Table(nameof(Team), Schema = Schemas.Sport)]
public partial class Team
{
    [Key]
    public int Id { get; set; }

    [StringLength(5)]
    public required string Code { get; set; }

    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(200)]
    public required string Address { get; set; }

    [StringLength(100)]
    public required string City { get; set; }

    public int? HomeLocationId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Location? HomeLocation { get; set; }

    [InverseProperty(nameof(Match.HomeTeam))]
    public virtual ICollection<Match> HomeMatches { get; set; } = new List<Match>();

    [InverseProperty(nameof(Match.AwayTeam))]
    public virtual ICollection<Match> AwayMatches { get; set; } = new List<Match>();
}
