using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACS.Migrations
{
    /// <inheritdoc />
    public partial class TestClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdPhuongTien",
                table: "HanhTrinhs");

            migrationBuilder.DropColumn(
                name: "IdHanhTrinh",
                table: "DuLieuGPS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdPhuongTien",
                table: "HanhTrinhs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IdHanhTrinh",
                table: "DuLieuGPS",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
