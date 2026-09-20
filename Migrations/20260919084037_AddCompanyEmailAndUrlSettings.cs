using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyEmailAndUrlSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppBaseUrl",
                table: "CompanySettings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromEmail",
                table: "CompanySettings",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromName",
                table: "CompanySettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppBaseUrl",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "FromEmail",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "FromName",
                table: "CompanySettings");
        }
    }
}
