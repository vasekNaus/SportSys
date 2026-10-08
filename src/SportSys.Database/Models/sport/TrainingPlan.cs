
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.dboSchema;
using SportSys.Database.Models.sportSchema;

namespace SportSys.Database.Models.sport;

[Table(nameof(TrainingPlan), Schema = Schemas.Sport)]
public partial class TrainingPlan
{
  [Key]
  public int Id { get; set; }

  public int SeasonId { get; set; }

  [StringLength(10)]
  [Unicode(false)]
  public required string SeasonCategoryCode { get; set; }

  public int TrainingTypeId { get; set; }

  public int TrainingPhaseId { get; set; }

  public DateOnly From { get; set; }

  public DateOnly To { get; set; }

  public int LocationId { get; set; }

  [Precision(0)]
  public TimeOnly TimeFrom { get; set; }

  [Precision(0)]
  public TimeOnly TimeTo { get; set; }

  public int? DurationMinutes { get; set; }

  [StringLength(10)]
  [Unicode(false)]
  public required string DayName { get; set; }

  [StringLength(100)]
  public required string Title { get; set; }

  public virtual ICollection<CoachTrainingPlan> CoachTrainingPlans { get; set; } = new List<CoachTrainingPlan>();

  [DeleteBehavior(DeleteBehavior.ClientSetNull)]
  public virtual Location Location { get; set; } = null!;

  [ForeignKey(nameof(SeasonId) + ", " + nameof(SeasonCategoryCode))]
  [DeleteBehavior(DeleteBehavior.ClientSetNull)]
  public virtual SeasonCategory SeasonCategory { get; set; } = null!;

  public virtual ICollection<Training> Training { get; set; } = new List<Training>();

  public virtual TrainingPlanGroup? GroupMembership { get; set; }

  [DeleteBehavior(DeleteBehavior.ClientSetNull)]
  public virtual TrainingPhase TrainingPhase { get; set; } = null!;

  [DeleteBehavior(DeleteBehavior.ClientSetNull)]
  public virtual TrainingType TrainingType { get; set; } = null!;
}