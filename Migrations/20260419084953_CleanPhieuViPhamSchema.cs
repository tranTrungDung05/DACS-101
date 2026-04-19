using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACS.Migrations
{
    /// <inheritdoc />
    public partial class CleanPhieuViPhamSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHopDongs_HopDongs_HopDongIdHopDong",
                table: "ChiTietHopDongs");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHopDongs_PhuongTiens_PhuongTienIdPhuongTien",
                table: "ChiTietHopDongs");

            migrationBuilder.DropForeignKey(
                name: "FK_DuLieuGPS_ThietBiGPS_ThietBiGPSIdThietBi",
                table: "DuLieuGPS");

            migrationBuilder.DropForeignKey(
                name: "FK_HanhTrinhs_PhuongTiens_PhuongTienIdPhuongTien",
                table: "HanhTrinhs");

            migrationBuilder.DropForeignKey(
                name: "FK_HopDongs_KhachHangs_KhachHangMaCccd",
                table: "HopDongs");

            migrationBuilder.DropForeignKey(
                name: "FK_HopDongs_TaiKhoans_TaiKhoanIdTaiKhoan",
                table: "HopDongs");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuViPhams_HanhTrinhs_HanhTrinhIdHanhTrinh",
                table: "PhieuViPhams");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuViPhams_KhachHangs_KhachHangMaCccd",
                table: "PhieuViPhams");

            migrationBuilder.DropIndex(
                name: "IX_PhieuViPhams_HanhTrinhIdHanhTrinh",
                table: "PhieuViPhams");

            migrationBuilder.DropIndex(
                name: "IX_PhieuViPhams_KhachHangMaCccd",
                table: "PhieuViPhams");

            migrationBuilder.DropIndex(
                name: "IX_HopDongs_KhachHangMaCccd",
                table: "HopDongs");

            migrationBuilder.DropIndex(
                name: "IX_HopDongs_TaiKhoanIdTaiKhoan",
                table: "HopDongs");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietHopDongs_HopDongIdHopDong",
                table: "ChiTietHopDongs");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietHopDongs_PhuongTienIdPhuongTien",
                table: "ChiTietHopDongs");

            migrationBuilder.DropColumn(
                name: "HanhTrinhIdHanhTrinh",
                table: "PhieuViPhams");

            migrationBuilder.DropColumn(
                name: "KhachHangMaCccd",
                table: "PhieuViPhams");

            migrationBuilder.DropColumn(
                name: "KhachHangMaCccd",
                table: "HopDongs");

            migrationBuilder.DropColumn(
                name: "TaiKhoanIdTaiKhoan",
                table: "HopDongs");

            migrationBuilder.DropColumn(
                name: "HopDongIdHopDong",
                table: "ChiTietHopDongs");

            migrationBuilder.DropColumn(
                name: "PhuongTienIdPhuongTien",
                table: "ChiTietHopDongs");

            migrationBuilder.RenameColumn(
                name: "PhuongTienIdPhuongTien",
                table: "HanhTrinhs",
                newName: "IdPhuongTien");

            migrationBuilder.RenameIndex(
                name: "IX_HanhTrinhs_PhuongTienIdPhuongTien",
                table: "HanhTrinhs",
                newName: "IX_HanhTrinhs_IdPhuongTien");

            migrationBuilder.RenameColumn(
                name: "ThietBiGPSIdThietBi",
                table: "DuLieuGPS",
                newName: "IdThietBi");

            migrationBuilder.RenameIndex(
                name: "IX_DuLieuGPS_ThietBiGPSIdThietBi",
                table: "DuLieuGPS",
                newName: "IX_DuLieuGPS_IdThietBi");

            migrationBuilder.AlterColumn<string>(
                name: "MaCccd",
                table: "PhieuViPhams",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MaCccd",
                table: "HopDongs",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuViPhams_IdHanhTrinh",
                table: "PhieuViPhams",
                column: "IdHanhTrinh");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuViPhams_MaCccd",
                table: "PhieuViPhams",
                column: "MaCccd");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_IdTaiKhoan",
                table: "HopDongs",
                column: "IdTaiKhoan");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_MaCccd",
                table: "HopDongs",
                column: "MaCccd");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHopDongs_IdHopDong",
                table: "ChiTietHopDongs",
                column: "IdHopDong");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHopDongs_HopDongs_IdHopDong",
                table: "ChiTietHopDongs",
                column: "IdHopDong",
                principalTable: "HopDongs",
                principalColumn: "IdHopDong",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHopDongs_PhuongTiens_IdPhuongTien",
                table: "ChiTietHopDongs",
                column: "IdPhuongTien",
                principalTable: "PhuongTiens",
                principalColumn: "IdPhuongTien",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DuLieuGPS_ThietBiGPS_IdThietBi",
                table: "DuLieuGPS",
                column: "IdThietBi",
                principalTable: "ThietBiGPS",
                principalColumn: "IdThietBi");

            migrationBuilder.AddForeignKey(
                name: "FK_HanhTrinhs_PhuongTiens_IdPhuongTien",
                table: "HanhTrinhs",
                column: "IdPhuongTien",
                principalTable: "PhuongTiens",
                principalColumn: "IdPhuongTien",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HopDongs_KhachHangs_MaCccd",
                table: "HopDongs",
                column: "MaCccd",
                principalTable: "KhachHangs",
                principalColumn: "MaCccd",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HopDongs_TaiKhoans_IdTaiKhoan",
                table: "HopDongs",
                column: "IdTaiKhoan",
                principalTable: "TaiKhoans",
                principalColumn: "IdTaiKhoan",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuViPhams_HanhTrinhs_IdHanhTrinh",
                table: "PhieuViPhams",
                column: "IdHanhTrinh",
                principalTable: "HanhTrinhs",
                principalColumn: "IdHanhTrinh",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuViPhams_KhachHangs_MaCccd",
                table: "PhieuViPhams",
                column: "MaCccd",
                principalTable: "KhachHangs",
                principalColumn: "MaCccd",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHopDongs_HopDongs_IdHopDong",
                table: "ChiTietHopDongs");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHopDongs_PhuongTiens_IdPhuongTien",
                table: "ChiTietHopDongs");

            migrationBuilder.DropForeignKey(
                name: "FK_DuLieuGPS_ThietBiGPS_IdThietBi",
                table: "DuLieuGPS");

            migrationBuilder.DropForeignKey(
                name: "FK_HanhTrinhs_PhuongTiens_IdPhuongTien",
                table: "HanhTrinhs");

            migrationBuilder.DropForeignKey(
                name: "FK_HopDongs_KhachHangs_MaCccd",
                table: "HopDongs");

            migrationBuilder.DropForeignKey(
                name: "FK_HopDongs_TaiKhoans_IdTaiKhoan",
                table: "HopDongs");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuViPhams_HanhTrinhs_IdHanhTrinh",
                table: "PhieuViPhams");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuViPhams_KhachHangs_MaCccd",
                table: "PhieuViPhams");

            migrationBuilder.DropIndex(
                name: "IX_PhieuViPhams_IdHanhTrinh",
                table: "PhieuViPhams");

            migrationBuilder.DropIndex(
                name: "IX_PhieuViPhams_MaCccd",
                table: "PhieuViPhams");

            migrationBuilder.DropIndex(
                name: "IX_HopDongs_IdTaiKhoan",
                table: "HopDongs");

            migrationBuilder.DropIndex(
                name: "IX_HopDongs_MaCccd",
                table: "HopDongs");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietHopDongs_IdHopDong",
                table: "ChiTietHopDongs");

            migrationBuilder.RenameColumn(
                name: "IdPhuongTien",
                table: "HanhTrinhs",
                newName: "PhuongTienIdPhuongTien");

            migrationBuilder.RenameIndex(
                name: "IX_HanhTrinhs_IdPhuongTien",
                table: "HanhTrinhs",
                newName: "IX_HanhTrinhs_PhuongTienIdPhuongTien");

            migrationBuilder.RenameColumn(
                name: "IdThietBi",
                table: "DuLieuGPS",
                newName: "ThietBiGPSIdThietBi");

            migrationBuilder.RenameIndex(
                name: "IX_DuLieuGPS_IdThietBi",
                table: "DuLieuGPS",
                newName: "IX_DuLieuGPS_ThietBiGPSIdThietBi");

            migrationBuilder.AlterColumn<string>(
                name: "MaCccd",
                table: "PhieuViPhams",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "HanhTrinhIdHanhTrinh",
                table: "PhieuViPhams",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "KhachHangMaCccd",
                table: "PhieuViPhams",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MaCccd",
                table: "HopDongs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "KhachHangMaCccd",
                table: "HopDongs",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TaiKhoanIdTaiKhoan",
                table: "HopDongs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HopDongIdHopDong",
                table: "ChiTietHopDongs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PhuongTienIdPhuongTien",
                table: "ChiTietHopDongs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuViPhams_HanhTrinhIdHanhTrinh",
                table: "PhieuViPhams",
                column: "HanhTrinhIdHanhTrinh");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuViPhams_KhachHangMaCccd",
                table: "PhieuViPhams",
                column: "KhachHangMaCccd");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_KhachHangMaCccd",
                table: "HopDongs",
                column: "KhachHangMaCccd");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_TaiKhoanIdTaiKhoan",
                table: "HopDongs",
                column: "TaiKhoanIdTaiKhoan");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHopDongs_HopDongIdHopDong",
                table: "ChiTietHopDongs",
                column: "HopDongIdHopDong");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHopDongs_PhuongTienIdPhuongTien",
                table: "ChiTietHopDongs",
                column: "PhuongTienIdPhuongTien");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHopDongs_HopDongs_HopDongIdHopDong",
                table: "ChiTietHopDongs",
                column: "HopDongIdHopDong",
                principalTable: "HopDongs",
                principalColumn: "IdHopDong",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHopDongs_PhuongTiens_PhuongTienIdPhuongTien",
                table: "ChiTietHopDongs",
                column: "PhuongTienIdPhuongTien",
                principalTable: "PhuongTiens",
                principalColumn: "IdPhuongTien",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DuLieuGPS_ThietBiGPS_ThietBiGPSIdThietBi",
                table: "DuLieuGPS",
                column: "ThietBiGPSIdThietBi",
                principalTable: "ThietBiGPS",
                principalColumn: "IdThietBi");

            migrationBuilder.AddForeignKey(
                name: "FK_HanhTrinhs_PhuongTiens_PhuongTienIdPhuongTien",
                table: "HanhTrinhs",
                column: "PhuongTienIdPhuongTien",
                principalTable: "PhuongTiens",
                principalColumn: "IdPhuongTien",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HopDongs_KhachHangs_KhachHangMaCccd",
                table: "HopDongs",
                column: "KhachHangMaCccd",
                principalTable: "KhachHangs",
                principalColumn: "MaCccd",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HopDongs_TaiKhoans_TaiKhoanIdTaiKhoan",
                table: "HopDongs",
                column: "TaiKhoanIdTaiKhoan",
                principalTable: "TaiKhoans",
                principalColumn: "IdTaiKhoan",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuViPhams_HanhTrinhs_HanhTrinhIdHanhTrinh",
                table: "PhieuViPhams",
                column: "HanhTrinhIdHanhTrinh",
                principalTable: "HanhTrinhs",
                principalColumn: "IdHanhTrinh",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuViPhams_KhachHangs_KhachHangMaCccd",
                table: "PhieuViPhams",
                column: "KhachHangMaCccd",
                principalTable: "KhachHangs",
                principalColumn: "MaCccd");
        }
    }
}
