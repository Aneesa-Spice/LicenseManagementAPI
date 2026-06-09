using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LicensingAPI.Migrations
{
    /// <inheritdoc />
    public partial class LicenseToUserlicense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderLicenseId",
                table: "UserLicenses",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderLicenseId",
                table: "UserLicenses");
        }
    }
}
