using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportSys.Database.Migrations
{
    /// <inheritdoc />
    public partial class CoachRole_Code : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "dbo",
                table: "CoachRole",
                type: "char(1)",
                unicode: false,
                maxLength: 1,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Order",
                schema: "dbo",
                table: "CoachRole",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "CoachRole",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Code", "Order" },
                values: new object[] { "H", 1 });

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "CoachRole",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Code", "Order" },
                values: new object[] { "A", 2 });

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "CoachRole",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Code", "Order" },
                values: new object[] { "O", 3 });

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "CoachRole",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Code", "Order" },
                values: new object[] { "B", 4 });

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "CoachRole",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Code", "Order" },
                values: new object[] { "F", 5 });

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "CoachRole",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Code", "Order" },
                values: new object[] { "K", 6 });

            migrationBuilder.CreateIndex(
                name: "UX_CoachRole_Code",
                schema: "dbo",
                table: "CoachRole",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_CoachRole_Code",
                schema: "dbo",
                table: "CoachRole");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "dbo",
                table: "CoachRole");

            migrationBuilder.DropColumn(
                name: "Order",
                schema: "dbo",
                table: "CoachRole");
        }
    }
}
