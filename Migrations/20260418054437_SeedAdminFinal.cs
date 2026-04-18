using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACS.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaiKhoans_ChucVus_ChucVuIdChucVu",
                table: "TaiKhoans");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoans_ChucVuIdChucVu",
                table: "TaiKhoans");

            migrationBuilder.DropColumn(
                name: "ChucVuIdChucVu",
                table: "TaiKhoans");

            migrationBuilder.InsertData(
                table: "TaiKhoans",
                columns: new[] { "IdTaiKhoan", "Email", "HoTen", "IdChucVu", "MatKhau", "TenDangNhap" },
                values: new object[] { 1, "admin@gps.com", "Super Admin", 1, "123", "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_IdChucVu",
                table: "TaiKhoans",
                column: "IdChucVu");

            migrationBuilder.AddForeignKey(
                name: "FK_TaiKhoans_ChucVus_IdChucVu",
                table: "TaiKhoans",
                column: "IdChucVu",
                principalTable: "ChucVus",
                principalColumn: "IdChucVu",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaiKhoans_ChucVus_IdChucVu",
                table: "TaiKhoans");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoans_IdChucVu",
                table: "TaiKhoans");

            migrationBuilder.DeleteData(
                table: "TaiKhoans",
                keyColumn: "IdTaiKhoan",
                keyValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ChucVuIdChucVu",
                table: "TaiKhoans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_ChucVuIdChucVu",
                table: "TaiKhoans",
                column: "ChucVuIdChucVu");

            migrationBuilder.AddForeignKey(
                name: "FK_TaiKhoans_ChucVus_ChucVuIdChucVu",
                table: "TaiKhoans",
                column: "ChucVuIdChucVu",
                principalTable: "ChucVus",
                principalColumn: "IdChucVu",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
