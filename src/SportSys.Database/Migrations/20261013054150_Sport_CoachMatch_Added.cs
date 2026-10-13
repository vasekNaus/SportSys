using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportSys.Database.Migrations
{
    /// <inheritdoc />
    public partial class Sport_CoachMatch_Added : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoachMatch",
                schema: "sport",
                columns: table => new
                {
                    Coach_Id = table.Column<int>(type: "int", nullable: false),
                    Match_Id = table.Column<int>(type: "int", nullable: false),
                    ParticipationType_Id = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "")
                        .Annotation("Relational:DefaultConstraintName", "DF_CoachMatch_Note")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachMatch", x => new { x.Coach_Id, x.Match_Id, x.ParticipationType_Id });
                    table.ForeignKey(
                        name: "FK_CoachMatch_Coach_Coach_Id",
                        column: x => x.Coach_Id,
                        principalSchema: "hr",
                        principalTable: "Coach",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CoachMatch_Match_Match_Id",
                        column: x => x.Match_Id,
                        principalSchema: "sport",
                        principalTable: "Match",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CoachMatch_ParticipationType_ParticipationType_Id",
                        column: x => x.ParticipationType_Id,
                        principalSchema: "sport",
                        principalTable: "ParticipationType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachMatch_Match",
                schema: "sport",
                table: "CoachMatch",
                column: "Match_Id");

            migrationBuilder.CreateIndex(
                name: "IX_CoachMatch_ParticipationType",
                schema: "sport",
                table: "CoachMatch",
                column: "ParticipationType_Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachMatch",
                schema: "sport");
        }
    }
}
