
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.dboSchema;
using SportSys.Database.Models.hr;
using SportSys.Database.Models.sportSchema;

namespace SportSys.Database.Models.sport;

[PrimaryKey("CoachId", "TrainingId", "ParticipationTypeId")]
[Table(nameof(CoachTraining), Schema = Schemas.Sport)]
[Index(nameof(CoachRoleId), Name = "IX_CoachTraining_CoachRoleId")]
public partial class CoachTraining
{
    [Key]
    public int CoachId { get; set; }

    [Key]
    public int TrainingId { get; set; }

    [Key]
    public int ParticipationTypeId { get; set; }

    public int CoachRoleId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public required string Note { get; set; }

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual Coach Coach { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual ParticipationType ParticipationType { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual CoachRole CoachRole { get; set; } = null!;

    public virtual Training Training { get; set; } = null!;
}