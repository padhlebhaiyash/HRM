using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddRejectionRemark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionRemark",
                table: "LeaveRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionRemark",
                table: "LeaveRequests");
        }
    }
}
