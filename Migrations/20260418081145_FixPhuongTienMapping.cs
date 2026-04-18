using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACS.Migrations
{
    /// <inheritdoc />
    public partial class FixPhuongTienMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhuongTiens_ThietBiGPS_ThietBiGPSIdThietBi",
                table: "PhuongTiens");

            migrationBuilder.DropIndex(
                name: "IX_PhuongTiens_ThietBiGPSIdThietBi",
                table: "PhuongTiens");

            migrationBuilder.DropColumn(
                name: "ThietBiGPSIdThietBi",
                table: "PhuongTiens");

            migrationBuilder.CreateIndex(
                name: "IX_PhuongTiens_IdThietBi",
                table: "PhuongTiens",
                column: "IdThietBi");

            migrationBuilder.AddForeignKey(
                name: "FK_PhuongTiens_ThietBiGPS_IdThietBi",
                table: "PhuongTiens",
                column: "IdThietBi",
                principalTable: "ThietBiGPS",
                principalColumn: "IdThietBi",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhuongTiens_ThietBiGPS_IdThietBi",
                table: "PhuongTiens");

            migrationBuilder.DropIndex(
                name: "IX_PhuongTiens_IdThietBi",
                table: "PhuongTiens");

            migrationBuilder.AddColumn<int>(
                name: "ThietBiGPSIdThietBi",
                table: "PhuongTiens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PhuongTiens_ThietBiGPSIdThietBi",
                table: "PhuongTiens",
                column: "ThietBiGPSIdThietBi");

            migrationBuilder.AddForeignKey(
                name: "FK_PhuongTiens_ThietBiGPS_ThietBiGPSIdThietBi",
                table: "PhuongTiens",
                column: "ThietBiGPSIdThietBi",
                principalTable: "ThietBiGPS",
                principalColumn: "IdThietBi",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
