
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SportSys.Database.Models.dboSchema;
using SportSys.Database.Models.sport;

namespace SportSys.Database.Models.sportSchema;

[Table(nameof(ParticipationType), Schema = Schemas.Sport)]
public partial class ParticipationType
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public required string Name { get; set; }

    public virtual ICollection<CoachTraining> CoachTrainings { get; set; } = new List<CoachTraining>();

    public virtual ICollection<CoachMatch> CoachMatches { get; set; } = new List<CoachMatch>();
}