using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACS.Migrations
{
    /// <inheritdoc />
    public partial class AddBehaviorFieldsToHanhTrinh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiemAnToan",
                table: "HanhTrinhs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhanLoaiHanhVi",
                table: "HanhTrinhs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiemAnToan",
                table: "HanhTrinhs");

            migrationBuilder.DropColumn(
                name: "PhanLoaiHanhVi",
                table: "HanhTrinhs");
        }
    }
}
