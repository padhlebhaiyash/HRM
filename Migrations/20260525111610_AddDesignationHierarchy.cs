using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignationHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM LeaveBalances");
            migrationBuilder.Sql("DELETE FROM LeaveRequests");
            migrationBuilder.Sql("DELETE FROM PayrollRecords");
            migrationBuilder.Sql("DELETE FROM BankDetails");
            migrationBuilder.Sql("DELETE FROM ProfileChangeRequests");
            migrationBuilder.Sql("DELETE FROM PasswordResetTokens");
            migrationBuilder.Sql("DELETE FROM UserRoles");
            migrationBuilder.Sql("DELETE FROM Users");
            migrationBuilder.Sql("DELETE FROM Employees");

            migrationBuilder.DropColumn(
                name: "Designation",
                table: "Employees");

            migrationBuilder.AddColumn<int>(
                name: "DesignationId",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Designations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Designations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Designations",
                columns: new[] { "Id", "Level", "Name" },
                values: new object[,]
                {
                    { 1, 1, "Admin" },
                    { 2, 2, "Manager" },
                    { 3, 3, "Team Lead" },
                    { 4, 4, "Executive" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DesignationId",
                table: "Employees",
                column: "DesignationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Designations_DesignationId",
                table: "Employees",
                column: "DesignationId",
                principalTable: "Designations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Designations_DesignationId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "Designations");

            migrationBuilder.DropIndex(
                name: "IX_Employees_DesignationId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DesignationId",
                table: "Employees");

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
