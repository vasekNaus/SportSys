using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportSys.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeasonCategory_XyDurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MatchDurationMinutes",
                schema: "sport",
                table: "SeasonCategory",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "WarmupDurationMinutes",
                schema: "sport",
                table: "SeasonCategory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SeasonCategory_MatchDurationMinutes",
                schema: "sport",
                table: "SeasonCategory",
                sql: "[MatchDurationMinutes] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SeasonCategory_WarmupDurationMinutes",
                schema: "sport",
                table: "SeasonCategory",
                sql: "[WarmupDurationMinutes] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SeasonCategory_MatchDurationMinutes",
                schema: "sport",
                table: "SeasonCategory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SeasonCategory_WarmupDurationMinutes",
                schema: "sport",
                table: "SeasonCategory");

            migrationBuilder.DropColumn(
                name: "MatchDurationMinutes",
                schema: "sport",
                table: "SeasonCategory");

            migrationBuilder.DropColumn(
                name: "WarmupDurationMinutes",
                schema: "sport",
                table: "SeasonCategory");
        }
    }
}
