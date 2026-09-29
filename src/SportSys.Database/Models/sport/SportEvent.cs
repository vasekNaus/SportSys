#nullable enable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SportSys.Database.Models.sport;

public abstract class SportEvent
{
    [Key]
    public int Id { get; set; }

    [Column("SeasonCategory_Season_Id")]
    public int SeasonId { get; set; }

    [Column("SeasonCategory_Name")]
    [StringLength(10)]
    [Unicode(false)]
    public required string SeasonCategoryName { get; set; }

    public DateOnly Date { get; set; }

    [Precision(0)]
    public TimeOnly TimeFrom { get; set; }

    public int LocationId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public required string Note { get; set; }

    [ForeignKey(nameof(SeasonId))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual Season Season { get; set; } = null!;

    [ForeignKey(nameof(SeasonId) + ", " + nameof(SeasonCategoryName))]
    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual SeasonCategory SeasonCategory { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual Location Location { get; set; } = null!;
}
