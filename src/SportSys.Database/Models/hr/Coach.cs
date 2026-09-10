using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.identity;
using SportSys.Database.Models.sport;

namespace SportSys.Database.Models.hr;

[Table(nameof(Coach), Schema = Schemas.Hr)]
//[Index(nameof(PersonalNumber), IsUnique = true, Name = "UX_Coach_PersonalNumber")]
//[Index(nameof(IdentificationNumber), IsUnique = true, Name = "UX_Coach_IdentificationNumber")]
public class Coach : User
{
  [StringLength(20)]
  [Unicode(false)]
  public required string PersonalNumber { get; set; }

  [StringLength(10)]
  [Unicode(false)]
  public required string IdentificationNumber { get; set; }

  public byte[]? Photo { get; set; }

  [StringLength(100)]
  [Unicode(false)]
  public string? PhotoContentType { get; set; }

  [StringLength(255)]
  public string? PhotoFileName { get; set; }

  public ICollection<CoachSetting> Settings { get; set; } = [];

  public ICollection<CoachLicense> Licenses { get; set; } = [];

  public ICollection<CoachContract> Contracts { get; set; } = [];

  public ICollection<CoachAttendance> Attendances { get; set; } = [];

  public ICollection<CoachTrainingRequirement> CoachTrainingRequirementCoaches { get; set; } = [];

  public ICollection<CoachTrainingPlan> CoachTrainingPlans { get; set; } = [];

  public ICollection<CoachTraining> CoachTrainings { get; set; } = [];
}
