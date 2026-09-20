using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileFieldsPlusBankTwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Employees",
                newName: "PermanentAddress");

            migrationBuilder.AddColumn<string>(
                name: "FathersName",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalAddress",
                table: "Employees",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "Employees",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BankDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BSB = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxPayerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankIdentifierCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BranchLocation = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankDetails_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankDetails_EmployeeId",
                table: "BankDetails",
                column: "EmployeeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankDetails");

            migrationBuilder.DropColumn(
                name: "FathersName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LocalAddress",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "PermanentAddress",
                table: "Employees",
                newName: "Address");
        }
    }
}
