
#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.dboSchema;
using SportSys.Database.Models.sportSchema;


namespace SportSys.Database.Models.sport;

[Table(nameof(Training), Schema = Schemas.Sport)]
public partial class Training : SportEvent
{
  public int TrainingTypeId { get; set; }

  public int TrainingPhaseId { get; set; }

  public int TrainingStateId { get; set; }

  public int? TrainingPlanId { get; set; }

  [Precision(0)]
  public TimeOnly TimeTo { get; set; }

  public int? DurationMinutes { get; set; }

  [DeleteBehavior(DeleteBehavior.ClientSetNull)]
  public virtual TrainingPhase TrainingPhase { get; set; } = null!;

  public virtual TrainingPlan? TrainingPlan { get; set; }

  public virtual TrainingGroup? GroupMembership { get; set; }

  [DeleteBehavior(DeleteBehavior.ClientSetNull)]
  public virtual TrainingState TrainingState { get; set; } = null!;

  [DeleteBehavior(DeleteBehavior.ClientSetNull)]
  public virtual TrainingType TrainingType { get; set; } = null!;

  public virtual ICollection<CoachTraining> CoachTrainings { get; set; } = new List<CoachTraining>();
}