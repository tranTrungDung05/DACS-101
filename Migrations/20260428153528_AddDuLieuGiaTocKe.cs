using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACS.Migrations
{
    /// <inheritdoc />
    public partial class AddDuLieuGiaTocKe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DuLieuGiaTocKes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HanhTrinhIdHanhTrinh = table.Column<int>(type: "int", nullable: false),
                    IdThietBi = table.Column<int>(type: "int", nullable: true),
                    GiaTocDoc = table.Column<decimal>(type: "decimal(10,6)", nullable: false),
                    GiaTocNgang = table.Column<decimal>(type: "decimal(10,6)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuLieuGiaTocKes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DuLieuGiaTocKes_HanhTrinhs_HanhTrinhIdHanhTrinh",
                        column: x => x.HanhTrinhIdHanhTrinh,
                        principalTable: "HanhTrinhs",
                        principalColumn: "IdHanhTrinh",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DuLieuGiaTocKes_ThietBiGPS_IdThietBi",
                        column: x => x.IdThietBi,
                        principalTable: "ThietBiGPS",
                        principalColumn: "IdThietBi");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuLieuGiaTocKes_HanhTrinhIdHanhTrinh",
                table: "DuLieuGiaTocKes",
                column: "HanhTrinhIdHanhTrinh");

            migrationBuilder.CreateIndex(
                name: "IX_DuLieuGiaTocKes_IdThietBi",
                table: "DuLieuGiaTocKes",
                column: "IdThietBi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DuLieuGiaTocKes");
        }
    }
}
