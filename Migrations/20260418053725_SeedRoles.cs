using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DACS.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ChucVus",
                columns: new[] { "IdChucVu", "MoTa", "TenChucVu" },
                values: new object[,]
                {
                    { 1, "Quản trị viên toàn quyền hệ thống", "Admin" },
                    { 2, "Quản lý phương tiện", "Manager" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ChucVus",
                keyColumn: "IdChucVu",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ChucVus",
                keyColumn: "IdChucVu",
                keyValue: 2);
        }
    }
}
