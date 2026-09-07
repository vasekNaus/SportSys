
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.hr;

namespace SportSys.Database.Models.sport;

[PrimaryKey("CoachId", "TrainingPlanId", "ValidFrom", "ValidTo")]
[Table(nameof(CoachTrainingPlan), Schema = Schemas.Sport)]
[Index("CoachId", "ValidFrom", "ValidTo", Name = "IX_CoachTraining_Coach_Date")]
[Index("TrainingPlanId", "ValidFrom", "ValidTo", Name = "IX_CoachTraining_Training_Date")]
public partial class CoachTrainingPlan
{
    [Key]
    public int CoachId { get; set; }

    [Key]
    public int TrainingPlanId { get; set; }

    [Key]
    public DateOnly ValidFrom { get; set; }

    [Key]
    public DateOnly ValidTo { get; set; }

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual Coach Coach { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual TrainingPlan TrainingPlan { get; set; } = null!;
}