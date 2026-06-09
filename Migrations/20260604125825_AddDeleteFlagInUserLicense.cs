using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LicensingAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDeleteFlagInUserLicense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserLicenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserLicenses");
        }
    }
}
