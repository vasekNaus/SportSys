using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.identity;
using SportSys.Database.Models.sport;

namespace SportSys.Database.Models.hr;

[Table(nameof(Coach), Schema = Schemas.Hr)]
//[Index(nameof(UserId), IsUnique = true, Name = "UX_Coach_User")]
//[Index(nameof(PersonalNumber), IsUnique = true, Name = "UX_Coach_PersonalNumber")]
//[Index(nameof(BirthNumber), IsUnique = true, Name = "UX_Coach_BirthNumber")]
public class Coach
{
    [Key]
    public int Id { get; set; }

    //public int UserId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public required string PersonalNumber { get; set; }

    //[StringLength(10)]
    //[Unicode(false)]
    //public required string BirthNumber { get; set; }

    public byte[]? Photo { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? PhotoContentType { get; set; }

    [StringLength(255)]
    public string? PhotoFileName { get; set; }

    //[DeleteBehavior(DeleteBehavior.Restrict)]
    //public User User { get; set; } = null!;

    public ICollection<CoachSetting> Settings { get; set; } = [];

    public ICollection<CoachLicense> Licenses { get; set; } = [];

    public ICollection<CoachContract> Contracts { get; set; } = [];

    public ICollection<CoachTrainingEntitlement> CoachTrainingEntitlementCoaches { get; set; } = [];

    public ICollection<CoachTrainingPlan> CoachTrainingPlans { get; set; } = [];

    public ICollection<CoachTraining> CoachTrainings { get; set; } = [];
}
