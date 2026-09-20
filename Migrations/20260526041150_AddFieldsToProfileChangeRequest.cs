using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldsToProfileChangeRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "ProfileChangeRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FathersName",
                table: "ProfileChangeRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "ProfileChangeRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "ProfileChangeRequests",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "ProfileChangeRequests");

            migrationBuilder.DropColumn(
                name: "FathersName",
                table: "ProfileChangeRequests");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "ProfileChangeRequests");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "ProfileChangeRequests");
        }
    }
}
