
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.dboSchema;

namespace SportSys.Database.Models.sport;

[PrimaryKey("SeasonId", "Name")]
[Table(nameof(SeasonCategory), Schema = Schemas.Sport)]
public partial class SeasonCategory
{
    [Key]
    public int SeasonId { get; set; }

    [Key]
    [StringLength(10)]
    [Unicode(false)]
    public required string Name { get; set; }

    public int Order { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public required string CompetitionCode { get; set; } = string.Empty;

    [StringLength(100)]
    public required string CompetitionTeamName { get; set; } = string.Empty;

    [StringLength(4000)]
    public required string BirthYears { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();

    [ForeignKey(nameof(SeasonId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual Season Season { get; set; } = null!;

    public virtual ICollection<Training> Training { get; set; } = new List<Training>();

    public virtual ICollection<TrainingRequirement> TrainingRequirements { get; set; } = new List<TrainingRequirement>();

    public virtual ICollection<TrainingPlan> TrainingPlans { get; set; } = new List<TrainingPlan>();
}