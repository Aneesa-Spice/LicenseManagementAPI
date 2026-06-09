using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LicensingAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLicenseTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activations_AspNetUsers_UserId",
                table: "Activations");

            migrationBuilder.DropIndex(
                name: "IX_Activations_LicenseId_UserId_MachineName",
                table: "Activations");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Activations",
                newName: "ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Activations_UserId",
                table: "Activations",
                newName: "IX_Activations_ApplicationUserId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Activations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderMachineId",
                table: "Activations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "MachineName",
                table: "Activations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "MachineFingerprint",
                table: "Activations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Activations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserLicenseId",
                table: "Activations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UserLicenses",
                columns: table => new
                {
                    UserLicenseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LicenseId = table.Column<int>(type: "integer", nullable: false),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    UserEmail = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLicenses", x => x.UserLicenseId);
                    table.ForeignKey(
                        name: "FK_UserLicenses_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserLicenses_Licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "Licenses",
                        principalColumn: "LicenseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activations_LicenseId_UserEmail_MachineName",
                table: "Activations",
                columns: new[] { "LicenseId", "UserEmail", "MachineName" });

            migrationBuilder.CreateIndex(
                name: "IX_Activations_UserLicenseId",
                table: "Activations",
                column: "UserLicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLicenses_CompanyId",
                table: "UserLicenses",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLicenses_LicenseId",
                table: "UserLicenses",
                column: "LicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLicenses_UserEmail_LicenseId",
                table: "UserLicenses",
                columns: new[] { "UserEmail", "LicenseId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Activations_AspNetUsers_ApplicationUserId",
                table: "Activations",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Activations_UserLicenses_UserLicenseId",
                table: "Activations",
                column: "UserLicenseId",
                principalTable: "UserLicenses",
                principalColumn: "UserLicenseId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activations_AspNetUsers_ApplicationUserId",
                table: "Activations");

            migrationBuilder.DropForeignKey(
                name: "FK_Activations_UserLicenses_UserLicenseId",
                table: "Activations");

            migrationBuilder.DropTable(
                name: "UserLicenses");

            migrationBuilder.DropIndex(
                name: "IX_Activations_LicenseId_UserEmail_MachineName",
                table: "Activations");

            migrationBuilder.DropIndex(
                name: "IX_Activations_UserLicenseId",
                table: "Activations");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Activations");

            migrationBuilder.DropColumn(
                name: "UserLicenseId",
                table: "Activations");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "Activations",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Activations_ApplicationUserId",
                table: "Activations",
                newName: "IX_Activations_UserId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Activations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderMachineId",
                table: "Activations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "MachineName",
                table: "Activations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MachineFingerprint",
                table: "Activations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activations_LicenseId_UserId_MachineName",
                table: "Activations",
                columns: new[] { "LicenseId", "UserId", "MachineName" });

            migrationBuilder.AddForeignKey(
                name: "FK_Activations_AspNetUsers_UserId",
                table: "Activations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
