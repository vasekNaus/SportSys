
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.dboSchema;
using SportSys.Database.Models.sportSchema;

namespace SportSys.Database.Models.sport;

[Table(nameof(TrainingRequirement), Schema = Schemas.Sport)]
public partial class TrainingRequirement
{
    [Key]
    public int Id { get; set; }

    public int SeasonId { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public required string SeasonCategoryName { get; set; }

    public int TrainingTypeId { get; set; }

    public int TrainingPhaseId { get; set; }

    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal DurationHours { get; set; }

    public virtual ICollection<CoachTrainingRequirement> CoachTrainingRequirements { get; set; } = new List<CoachTrainingRequirement>();

    [ForeignKey(nameof(SeasonId) + ", " + nameof(SeasonCategoryName))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual SeasonCategory SeasonCategory { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual TrainingPhase TrainingPhase { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual TrainingType TrainingType { get; set; } = null!;
}