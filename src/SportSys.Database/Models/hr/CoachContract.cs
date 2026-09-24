using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.sport;

namespace SportSys.Database.Models.hr;

[Table(nameof(CoachContract), Schema = Schemas.Hr)]
[Index(nameof(CoachId), nameof(SeasonId), Name = "IX_CoachContract_Coach_Season")]
[Index(nameof(SeasonId), Name = "IX_CoachContract_Season")]
public class CoachContract
{
    [Key]
    public int Id { get; set; }

    public int CoachId { get; set; }

    public int SeasonId { get; set; }

    public byte ContractType { get; set; }

    [Precision(18, 2)]
    public decimal RewardAmount { get; set; }

    public bool IsActive { get; set; } = true;

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Coach Coach { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Season Season { get; set; } = null!;
}
