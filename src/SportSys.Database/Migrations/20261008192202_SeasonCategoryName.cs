using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportSys.Database.Migrations
{
  /// <inheritdoc />
  public partial class SeasonCategoryName : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<string>(
          name: "Name",
          schema: "sport",
          table: "SeasonCategory",
          type: "varchar(20)",
          unicode: false,
          maxLength: 20,
          nullable: false,
          defaultValue: "")
          .Annotation("Relational:DefaultConstraintName", "DF_SeasonCategory_Name");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "Name",
          schema: "sport",
          table: "SeasonCategory");

    }
  }
}
