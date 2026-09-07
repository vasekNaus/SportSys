
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.hr;
using SportSys.Database.Models.dboSchema;


namespace SportSys.Database.Models.sport;

[PrimaryKey("CoachId", "TrainingEntitlementId", "CoachRoleId")]
[Table(nameof(CoachTrainingEntitlement), Schema = Schemas.Sport)]
public partial class CoachTrainingEntitlement
{
    [Key]
    public int CoachId { get; set; }

    [Key]
    public int TrainingEntitlementId { get; set; }

    [Key]
    public int CoachRoleId { get; set; }

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual Coach Coach { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual CoachRole CoachRole { get; set; } = null!;

    [DeleteBehavior(DeleteBehavior.ClientSetNull)]
    public virtual TrainingEntitlement TrainingEntitlement { get; set; } = null!;
}