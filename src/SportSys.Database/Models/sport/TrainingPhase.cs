
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.dboSchema;
using SportSys.Database.Models.sport;

namespace SportSys.Database.Models.sportSchema;

[Table(nameof(TrainingPhase), Schema = Schemas.Sport)]
public partial class TrainingPhase
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public required string Name { get; set; }

    public virtual ICollection<Training> Training { get; set; } = new List<Training>();

    public virtual ICollection<TrainingRequirement> TrainingRequirements { get; set; } = new List<TrainingRequirement>();

    public virtual ICollection<TrainingPlan> TrainingPlans { get; set; } = new List<TrainingPlan>();
}