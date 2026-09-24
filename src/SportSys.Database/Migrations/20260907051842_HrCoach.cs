using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportSys.Database.Migrations
{
    /// <inheritdoc />
    public partial class HrCoach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                schema: "dbo",
                table: "Coach");

            migrationBuilder.DropColumn(
                name: "FirstName",
                schema: "dbo",
                table: "Coach");

            migrationBuilder.DropColumn(
                name: "LastName",
                schema: "dbo",
                table: "Coach");

            migrationBuilder.EnsureSchema(
                name: "hr");

            migrationBuilder.RenameTable(
                name: "Coach",
                schema: "dbo",
                newName: "Coach",
                newSchema: "hr");

            migrationBuilder.AddColumn<string>(
                name: "PersonalNumber",
                schema: "hr",
                table: "Coach",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "Photo",
                schema: "hr",
                table: "Coach",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoContentType",
                schema: "hr",
                table: "Coach",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoFileName",
                schema: "hr",
                table: "Coach",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CoachContract",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Coach_Id = table.Column<int>(type: "int", nullable: false),
                    Season_Id = table.Column<int>(type: "int", nullable: false),
                    ContractType = table.Column<byte>(type: "tinyint", nullable: false),
                    RewardAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_CoachContract_IsActive")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachContract", x => x.Id);
                    table.CheckConstraint("CK_CoachContract_ContractType", "[ContractType] IN (1, 2)");
                    table.CheckConstraint("CK_CoachContract_RewardAmount", "[RewardAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_CoachContract_Coach_Coach_Id",
                        column: x => x.Coach_Id,
                        principalSchema: "hr",
                        principalTable: "Coach",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachContract_Season_Season_Id",
                        column: x => x.Season_Id,
                        principalSchema: "sport",
                        principalTable: "Season",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoachLicenseType",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachLicenseType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CoachSetting",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Coach_Id = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    BankAccountPrefix = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: true),
                    BankAccountNumber = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    BankCode = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ZipCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    HealthInsuranceCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachSetting", x => x.Id);
                    table.CheckConstraint("CK_CoachSetting_Validity", "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]");
                    table.ForeignKey(
                        name: "FK_CoachSetting_Coach_Coach_Id",
                        column: x => x.Coach_Id,
                        principalSchema: "hr",
                        principalTable: "Coach",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoachLicense",
                schema: "hr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Coach_Id = table.Column<int>(type: "int", nullable: false),
                    CoachLicenseType_Id = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachLicense", x => x.Id);
                    table.CheckConstraint("CK_CoachLicense_Validity", "[ValidTo] IS NULL OR [ValidTo] >= [ValidFrom]");
                    table.ForeignKey(
                        name: "FK_CoachLicense_CoachLicenseType_CoachLicenseType_Id",
                        column: x => x.CoachLicenseType_Id,
                        principalSchema: "hr",
                        principalTable: "CoachLicenseType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachLicense_Coach_Coach_Id",
                        column: x => x.Coach_Id,
                        principalSchema: "hr",
                        principalTable: "Coach",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachContract_Coach_Season",
                schema: "hr",
                table: "CoachContract",
                columns: new[] { "Coach_Id", "Season_Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachContract_Season",
                schema: "hr",
                table: "CoachContract",
                column: "Season_Id");

            migrationBuilder.CreateIndex(
                name: "IX_CoachLicense_Coach_Validity",
                schema: "hr",
                table: "CoachLicense",
                columns: new[] { "Coach_Id", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachLicense_CoachLicenseType",
                schema: "hr",
                table: "CoachLicense",
                column: "CoachLicenseType_Id");

            migrationBuilder.CreateIndex(
                name: "UX_CoachLicenseType_Code",
                schema: "hr",
                table: "CoachLicenseType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachSetting_Coach_Validity",
                schema: "hr",
                table: "CoachSetting",
                columns: new[] { "Coach_Id", "ValidFrom", "ValidTo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachContract",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CoachLicense",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CoachSetting",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "CoachLicenseType",
                schema: "hr");

            migrationBuilder.DropColumn(
                name: "PersonalNumber",
                schema: "hr",
                table: "Coach");

            migrationBuilder.DropColumn(
                name: "Photo",
                schema: "hr",
                table: "Coach");

            migrationBuilder.DropColumn(
                name: "PhotoContentType",
                schema: "hr",
                table: "Coach");

            migrationBuilder.DropColumn(
                name: "PhotoFileName",
                schema: "hr",
                table: "Coach");

            migrationBuilder.RenameTable(
                name: "Coach",
                schema: "hr",
                newName: "Coach",
                newSchema: "dbo");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                schema: "dbo",
                table: "Coach",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                schema: "dbo",
                table: "Coach",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                schema: "dbo",
                table: "Coach",
                type: "nvarchar(101)",
                maxLength: 101,
                nullable: false,
                computedColumnSql: "(([FirstName]+N' ')+[LastName])",
                stored: true);
        }
    }
}
