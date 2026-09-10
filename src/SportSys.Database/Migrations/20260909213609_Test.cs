using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportSys.Database.Migrations
{
    /// <inheritdoc />
    public partial class Test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Coach_IdentificationNumber",
                schema: "hr",
                table: "Coach",
                column: "IdentificationNumber",
                unique: true,
                filter: "[IdentificationNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Coach_PersonalNumber",
                schema: "hr",
                table: "Coach",
                column: "PersonalNumber",
                unique: true,
                filter: "[PersonalNumber] IS NOT NULL");
        }
    }
}
