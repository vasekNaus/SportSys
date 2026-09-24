
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.dboSchema;
using SportSys.Database.Models.sport;

namespace SportSys.Database.Models.dboSchema;

[Table(nameof(CoachRole), Schema = Schemas.Dbo)]
public partial class CoachRole
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public required string Name { get; set; }

    public virtual ICollection<CoachTrainingRequirement> CoachTrainingRequirementCoachRoles { get; set; } = new List<CoachTrainingRequirement>();
}