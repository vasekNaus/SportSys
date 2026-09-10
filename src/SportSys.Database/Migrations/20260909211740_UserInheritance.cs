using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportSys.Database.Migrations
{
  /// <inheritdoc />
  public partial class UserInheritance : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "DisplayName",
          schema: "hr",
          table: "Coach");

      migrationBuilder.AddColumn<int>(
         name: "User_Id",
         schema: "hr",
         table: "Coach",
         type: "int",
         nullable: false,
         defaultValue: 0);

      migrationBuilder.Sql(@"
          UPDATE hr.Coach
          SET User_Id = Id");

      migrationBuilder.DropColumn(
          name: "Id",
          schema: "hr",
          table: "Coach");

      migrationBuilder.RenameColumn(
    name: "User_Id",
    schema: "hr",
    table: "Coach",
    newName: "Id");

      migrationBuilder.AddColumn<string>(
          name: "IdentificationNumber",
          schema: "hr",
          table: "Coach",
          type: "varchar(10)",
          unicode: false,
          maxLength: 10,
          nullable: false,
          defaultValue: "");

      migrationBuilder.CreateTable(
          name: "CoachAttendance",
          schema: "hr",
          columns: table => new
          {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("SqlServer:Identity", "1, 1"),
            Coach_Id = table.Column<int>(type: "int", nullable: false),
            PeriodYear = table.Column<int>(type: "int", nullable: false),
            PeriodMonth = table.Column<byte>(type: "tinyint", nullable: false),
            FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            ContentType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
            FileContent = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
            UploadedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
            UserUpload_Id = table.Column<int>(type: "int", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_CoachAttendance", x => x.Id);
            table.CheckConstraint("CK_CoachAttendance_PeriodMonth", "[PeriodMonth] BETWEEN 1 AND 12");
            table.CheckConstraint("CK_CoachAttendance_PeriodYear", "[PeriodYear] BETWEEN 1 AND 9999");
            //table.ForeignKey(
            //          name: "FK_CoachAttendance_Coach_Coach_Id",
            //          column: x => x.Coach_Id,
            //          principalSchema: "hr",
            //          principalTable: "Coach",
            //          principalColumn: "Id",
            //          onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_CoachAttendance_User_UserUpload_Id",
                      column: x => x.UserUpload_Id,
                      principalSchema: "identity",
                      principalTable: "User",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      //migrationBuilder.CreateIndex(
      //    name: "UX_Coach_IdentificationNumber",
      //    schema: "hr",
      //    table: "Coach",
      //    column: "IdentificationNumber",
      //    unique: true,
      //    filter: "[IdentificationNumber] IS NOT NULL");

      //migrationBuilder.CreateIndex(
      //    name: "UX_Coach_PersonalNumber",
      //    schema: "hr",
      //    table: "Coach",
      //    column: "PersonalNumber",
      //    unique: true,
      //    filter: "[PersonalNumber] IS NOT NULL");

      migrationBuilder.CreateIndex(
          name: "IX_CoachAttendance_Coach_Period",
          schema: "hr",
          table: "CoachAttendance",
          columns: new[] { "Coach_Id", "PeriodYear", "PeriodMonth" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_CoachAttendance_UploadedAt",
          schema: "hr",
          table: "CoachAttendance",
          column: "UploadedAt");

      migrationBuilder.CreateIndex(
          name: "IX_CoachAttendance_UserUpload",
          schema: "hr",
          table: "CoachAttendance",
          column: "UserUpload_Id");

      migrationBuilder.AddForeignKey(
          name: "FK_Coach_User_Id",
          schema: "hr",
          table: "Coach",
          column: "Id",
          principalSchema: "identity",
          principalTable: "User",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropForeignKey(
          name: "FK_Coach_User_Id",
          schema: "hr",
          table: "Coach");

      migrationBuilder.DropTable(
          name: "CoachAttendance",
          schema: "hr");

      migrationBuilder.DropTable(
          name: "CoachTrainingRequirement",
          schema: "sport");

      migrationBuilder.DropTable(
          name: "TrainingRequirement",
          schema: "sport");

      migrationBuilder.DropIndex(
          name: "UX_Coach_IdentificationNumber",
          schema: "hr",
          table: "Coach");

      migrationBuilder.DropIndex(
          name: "UX_Coach_PersonalNumber",
          schema: "hr",
          table: "Coach");

      migrationBuilder.DropColumn(
          name: "IdentificationNumber",
          schema: "hr",
          table: "Coach");

      migrationBuilder.AlterColumn<int>(
          name: "Id",
          schema: "hr",
          table: "Coach",
          type: "int",
          nullable: false,
          oldClrType: typeof(int),
          oldType: "int")
          .Annotation("SqlServer:Identity", "1, 1");

      migrationBuilder.AddColumn<string>(
          name: "DisplayName",
          schema: "hr",
          table: "Coach",
          type: "varchar(100)",
          unicode: false,
          maxLength: 100,
          nullable: false,
          defaultValue: "");

    }
  }
}
