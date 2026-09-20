using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddParentDesignation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentDesignationId",
                table: "Designations",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Designations",
                keyColumn: "Id",
                keyValue: 1,
                column: "ParentDesignationId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Designations",
                keyColumn: "Id",
                keyValue: 2,
                column: "ParentDesignationId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Designations",
                keyColumn: "Id",
                keyValue: 3,
                column: "ParentDesignationId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Designations",
                keyColumn: "Id",
                keyValue: 4,
                column: "ParentDesignationId",
                value: 3);

            migrationBuilder.CreateIndex(
                name: "IX_Designations_ParentDesignationId",
                table: "Designations",
                column: "ParentDesignationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Designations_Designations_ParentDesignationId",
                table: "Designations",
                column: "ParentDesignationId",
                principalTable: "Designations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Designations_Designations_ParentDesignationId",
                table: "Designations");

            migrationBuilder.DropIndex(
                name: "IX_Designations_ParentDesignationId",
                table: "Designations");

            migrationBuilder.DropColumn(
                name: "ParentDesignationId",
                table: "Designations");
        }
    }
}
