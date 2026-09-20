using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationBasedAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClockInTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClockOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClockInLatitude = table.Column<double>(type: "float", nullable: true),
                    ClockInLongitude = table.Column<double>(type: "float", nullable: true),
                    ClockOutLatitude = table.Column<double>(type: "float", nullable: true),
                    ClockOutLongitude = table.Column<double>(type: "float", nullable: true),
                    ClockInLocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClockOutLocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsLocationVerified = table.Column<bool>(type: "bit", nullable: false),
                    WorkDurationMinutes = table.Column<double>(type: "float", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OfficeLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    AllowedRadiusMeters = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsEnforced = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficeLocations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "OfficeLocations",
                columns: new[] { "Id", "Address", "AllowedRadiusMeters", "CreatedAt", "IsActive", "IsEnforced", "Latitude", "Longitude", "Name", "UpdatedAt" },
                values: new object[] { 1, "Corporate Hub, Main Office Site", 300.0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, true, 23.022500000000001, 72.571399999999997, "Main Headquarters Office", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeId",
                table: "Attendances",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "OfficeLocations");
        }
    }
}
