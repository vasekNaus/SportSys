using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportSys.Database.Migrations
{
    /// <inheritdoc />
    public partial class CoachRole_UseInPlanAndTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Hours",
                schema: "sport",
                table: "CoachTrainingRequirement",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CoachRole_Id",
                schema: "sport",
                table: "CoachTrainingPlan",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CoachRole_Id",
                schema: "sport",
                table: "CoachTraining",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CoachTrainingPlan_CoachRoleId",
                schema: "sport",
                table: "CoachTrainingPlan",
                column: "CoachRole_Id");

            migrationBuilder.CreateIndex(
                name: "IX_CoachTraining_CoachRoleId",
                schema: "sport",
                table: "CoachTraining",
                column: "CoachRole_Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CoachTraining_CoachRole_CoachRole_Id",
                schema: "sport",
                table: "CoachTraining",
                column: "CoachRole_Id",
                principalSchema: "dbo",
                principalTable: "CoachRole",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CoachTrainingPlan_CoachRole_CoachRole_Id",
                schema: "sport",
                table: "CoachTrainingPlan",
                column: "CoachRole_Id",
                principalSchema: "dbo",
                principalTable: "CoachRole",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoachTraining_CoachRole_CoachRole_Id",
                schema: "sport",
                table: "CoachTraining");

            migrationBuilder.DropForeignKey(
                name: "FK_CoachTrainingPlan_CoachRole_CoachRole_Id",
                schema: "sport",
                table: "CoachTrainingPlan");

            migrationBuilder.DropIndex(
                name: "IX_CoachTrainingPlan_CoachRoleId",
                schema: "sport",
                table: "CoachTrainingPlan");

            migrationBuilder.DropIndex(
                name: "IX_CoachTraining_CoachRoleId",
                schema: "sport",
                table: "CoachTraining");

            migrationBuilder.DropColumn(
                name: "Hours",
                schema: "sport",
                table: "CoachTrainingRequirement");

            migrationBuilder.DropColumn(
                name: "CoachRole_Id",
                schema: "sport",
                table: "CoachTrainingPlan");

            migrationBuilder.DropColumn(
                name: "CoachRole_Id",
                schema: "sport",
                table: "CoachTraining");
        }
    }
}
