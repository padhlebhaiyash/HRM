using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveTypesAndProfileRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LeaveType",
                table: "LeaveRequests",
                newName: "LeaveTypeId");

            migrationBuilder.RenameColumn(
                name: "LeaveType",
                table: "LeaveBalances",
                newName: "LeaveTypeId");

            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LocalAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PermanentAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhotoPath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsPending = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileChangeRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "LeaveTypes",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "Casual", true, "Casual Leave" },
                    { 2, "Sick", true, "Sick Leave" },
                    { 3, "Earned", true, "Earned Leave" },
                    { 4, "Unpaid", true, "Unpaid Leave" },
                    { 5, "WFH", true, "Work From Home" }
                });

            // Migrate existing data by shifting enum values from 0-4 to 1-5
            migrationBuilder.Sql("UPDATE LeaveBalances SET LeaveTypeId = LeaveTypeId + 1;");
            migrationBuilder.Sql("UPDATE LeaveRequests SET LeaveTypeId = LeaveTypeId + 1;");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_LeaveTypeId",
                table: "LeaveBalances",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileChangeRequests_EmployeeId",
                table: "ProfileChangeRequests",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveBalances_LeaveTypes_LeaveTypeId",
                table: "LeaveBalances",
                column: "LeaveTypeId",
                principalTable: "LeaveTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId",
                principalTable: "LeaveTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveBalances_LeaveTypes_LeaveTypeId",
                table: "LeaveBalances");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "LeaveTypes");

            migrationBuilder.DropTable(
                name: "ProfileChangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveBalances_LeaveTypeId",
                table: "LeaveBalances");

            migrationBuilder.Sql("UPDATE LeaveBalances SET LeaveTypeId = LeaveTypeId - 1;");
            migrationBuilder.Sql("UPDATE LeaveRequests SET LeaveTypeId = LeaveTypeId - 1;");

            migrationBuilder.RenameColumn(
                name: "LeaveTypeId",
                table: "LeaveRequests",
                newName: "LeaveType");

            migrationBuilder.RenameColumn(
                name: "LeaveTypeId",
                table: "LeaveBalances",
                newName: "LeaveType");
        }
    }
}
