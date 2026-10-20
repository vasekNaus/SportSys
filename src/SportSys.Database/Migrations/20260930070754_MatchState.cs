using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SportSys.Database.Migrations
{
    /// <inheritdoc />
    public partial class MatchState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MatchState_Id",
                schema: "sport",
                table: "Match",
                type: "int",
                nullable: true,
                defaultValueSql: "(1)");

            migrationBuilder.CreateTable(
                name: "MatchState",
                schema: "sport",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchState", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "sport",
                table: "MatchState",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Plán" },
                    { 2, "Potvrzený" },
                    { 3, "Zrušený" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Match_MatchState_MatchState_Id",
                schema: "sport",
                table: "Match",
                column: "MatchState_Id",
                principalSchema: "sport",
                principalTable: "MatchState",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Match_MatchState_MatchState_Id",
                schema: "sport",
                table: "Match");

            migrationBuilder.DropTable(
                name: "MatchState",
                schema: "sport");

            migrationBuilder.DropColumn(
                name: "MatchState_Id",
                schema: "sport",
                table: "Match");
        }
    }
}
