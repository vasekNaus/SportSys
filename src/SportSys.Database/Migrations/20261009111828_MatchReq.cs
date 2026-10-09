using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportSys.Database.Migrations
{
    /// <inheritdoc />
    public partial class MatchReq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchRequirement",
                schema: "sport",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeasonCategory_Season_Id = table.Column<int>(type: "int", nullable: false),
                    SeasonCategory_Code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    From = table.Column<DateOnly>(type: "date", nullable: false),
                    To = table.Column<DateOnly>(type: "date", nullable: false),
                    MatchCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchRequirement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchRequirement_SeasonCategory_SeasonCategory_Season_Id_SeasonCategory_Code",
                        columns: x => new { x.SeasonCategory_Season_Id, x.SeasonCategory_Code },
                        principalSchema: "sport",
                        principalTable: "SeasonCategory",
                        principalColumns: new[] { "Season_Id", "Code" });
                });

            migrationBuilder.CreateTable(
                name: "CoachMatchRequirement",
                schema: "sport",
                columns: table => new
                {
                    Coach_Id = table.Column<int>(type: "int", nullable: false),
                    MatchRequirement_Id = table.Column<int>(type: "int", nullable: false),
                    CoachRole_Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachMatchRequirement", x => new { x.Coach_Id, x.MatchRequirement_Id, x.CoachRole_Id });
                    table.ForeignKey(
                        name: "FK_CoachMatchRequirement_CoachRole_CoachRole_Id",
                        column: x => x.CoachRole_Id,
                        principalSchema: "dbo",
                        principalTable: "CoachRole",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CoachMatchRequirement_Coach_Coach_Id",
                        column: x => x.Coach_Id,
                        principalSchema: "hr",
                        principalTable: "Coach",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CoachMatchRequirement_MatchRequirement_MatchRequirement_Id",
                        column: x => x.MatchRequirement_Id,
                        principalSchema: "sport",
                        principalTable: "MatchRequirement",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachMatchRequirement_CoachRole",
                schema: "sport",
                table: "CoachMatchRequirement",
                column: "CoachRole_Id");

            migrationBuilder.CreateIndex(
                name: "IX_CoachMatchRequirement_MatchRequirement",
                schema: "sport",
                table: "CoachMatchRequirement",
                column: "MatchRequirement_Id");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRequirement_SeasonCategory_Period",
                schema: "sport",
                table: "MatchRequirement",
                columns: new[] { "SeasonCategory_Season_Id", "SeasonCategory_Code", "From", "To" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachMatchRequirement",
                schema: "sport");

            migrationBuilder.DropTable(
                name: "MatchRequirement",
                schema: "sport");
        }
    }
}
