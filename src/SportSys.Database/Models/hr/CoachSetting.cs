using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportSys.Database.Models.hr;

[Table(nameof(CoachSetting), Schema = Schemas.Hr)]
[Index(nameof(CoachId), nameof(ValidFrom), nameof(ValidTo), Name = "IX_CoachSetting_Coach_Validity")]
public class CoachSetting
{
    [Key]
    public int Id { get; set; }

    public int CoachId { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    [StringLength(6)]
    [Unicode(false)]
    public string? BankAccountPrefix { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public required string BankAccountNumber { get; set; }

    [StringLength(4)]
    [Unicode(false)]
    public required string BankCode { get; set; }

    [StringLength(200)]
    public required string Street { get; set; }

    [StringLength(100)]
    public required string City { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public required string ZipCode { get; set; }

    [StringLength(3)]
    [Unicode(false)]
    public required string HealthInsuranceCode { get; set; }

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Coach Coach { get; set; } = null!;
}
