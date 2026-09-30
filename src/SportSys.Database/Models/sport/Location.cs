
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SportSys.Database.Models.dboSchema;

namespace SportSys.Database.Models.sport;

[Table(nameof(Location), Schema = Schemas.Sport)]
public partial class Location
{
  [Key]
  public int Id { get; set; }

  [StringLength(100)]
  public required string Name { get; set; }

  [StringLength(200)]
  public string? Street { get; set; }

  [StringLength(100)]
  public string? City { get; set; }

  [StringLength(100)]
  public string? ZipCode { get; set; }

  public bool IsActive { get; set; } = true;

  [Column("Location")]
  public Geometry? GeographicLocation { get; set; }

  public virtual ICollection<Match> Matches { get; set; } = new List<Match>();

  public virtual ICollection<Team> HomeTeams { get; set; } = new List<Team>();

  public virtual ICollection<Training> Trainings { get; set; } = new List<Training>();
}