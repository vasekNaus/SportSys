using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace SportSys.Database.Migrations
{
  /// <inheritdoc />
  public partial class AddSportLocation : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<TimeOnly>(
          name: "TimeTo",
          schema: "sport",
          table: "Match",
          type: "time(0)",
          precision: 0,
          nullable: false,
          defaultValue: new TimeOnly(0, 0, 0));

      migrationBuilder.AddColumn<int>(
          name: "DurationMinutes",
          schema: "sport",
          table: "Match",
          type: "int",
          nullable: true,
          computedColumnSql: "(datediff(minute,[TimeFrom],[TimeTo]))",
          stored: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
  }
}
