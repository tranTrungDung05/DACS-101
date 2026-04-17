using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChucVus",
                columns: table => new
                {
                    IdChucVu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenChucVu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChucVus", x => x.IdChucVu);
                });

            migrationBuilder.CreateTable(
                name: "KhachHangs",
                columns: table => new
                {
                    MaCccd = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgaySinh = table.Column<DateOnly>(type: "date", nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sdt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHangs", x => x.MaCccd);
                });

            migrationBuilder.CreateTable(
                name: "QuyDinhs",
                columns: table => new
                {
                    IdQuyDinh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenQuyDinh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MucPhat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyDinhs", x => x.IdQuyDinh);
                });

            migrationBuilder.CreateTable(
                name: "Quyens",
                columns: table => new
                {
                    IdQuyen = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenQuyen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaQuyen = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quyens", x => x.IdQuyen);
                });

            migrationBuilder.CreateTable(
                name: "ThietBiGPS",
                columns: table => new
                {
                    IdThietBi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaImei = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoaiThietBi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayKichHoat = table.Column<DateOnly>(type: "date", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThietBiGPS", x => x.IdThietBi);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoans",
                columns: table => new
                {
                    IdTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdChucVu = table.Column<int>(type: "int", nullable: false),
                    ChucVuIdChucVu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoans", x => x.IdTaiKhoan);
                    table.ForeignKey(
                        name: "FK_TaiKhoans_ChucVus_ChucVuIdChucVu",
                        column: x => x.ChucVuIdChucVu,
                        principalTable: "ChucVus",
                        principalColumn: "IdChucVu",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietQuyens",
                columns: table => new
                {
                    IdChucVu = table.Column<int>(type: "int", nullable: false),
                    IdQuyen = table.Column<int>(type: "int", nullable: false),
                    ChucVuIdChucVu = table.Column<int>(type: "int", nullable: false),
                    QuyenIdQuyen = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietQuyens", x => new { x.IdChucVu, x.IdQuyen });
                    table.ForeignKey(
                        name: "FK_ChiTietQuyens_ChucVus_ChucVuIdChucVu",
                        column: x => x.ChucVuIdChucVu,
                        principalTable: "ChucVus",
                        principalColumn: "IdChucVu",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietQuyens_Quyens_QuyenIdQuyen",
                        column: x => x.QuyenIdQuyen,
                        principalTable: "Quyens",
                        principalColumn: "IdQuyen",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhuongTiens",
                columns: table => new
                {
                    IdPhuongTien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BienSo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoaiPhuongTien = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HangSanXuat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MauSac = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    IdThietBi = table.Column<int>(type: "int", nullable: false),
                    ThietBiGPSIdThietBi = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhuongTiens", x => x.IdPhuongTien);
                    table.ForeignKey(
                        name: "FK_PhuongTiens_ThietBiGPS_ThietBiGPSIdThietBi",
                        column: x => x.ThietBiGPSIdThietBi,
                        principalTable: "ThietBiGPS",
                        principalColumn: "IdThietBi",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HopDongs",
                columns: table => new
                {
                    IdHopDong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NgayTao = table.Column<DateOnly>(type: "date", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    MaCccd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    TaiKhoanIdTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    KhachHangMaCccd = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HopDongs", x => x.IdHopDong);
                    table.ForeignKey(
                        name: "FK_HopDongs_KhachHangs_KhachHangMaCccd",
                        column: x => x.KhachHangMaCccd,
                        principalTable: "KhachHangs",
                        principalColumn: "MaCccd",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HopDongs_TaiKhoans_TaiKhoanIdTaiKhoan",
                        column: x => x.TaiKhoanIdTaiKhoan,
                        principalTable: "TaiKhoans",
                        principalColumn: "IdTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HanhTrinhs",
                columns: table => new
                {
                    IdHanhTrinh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NgayDi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayDen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TongQuangDuong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    IdPhuongTien = table.Column<int>(type: "int", nullable: false),
                    PhuongTienIdPhuongTien = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HanhTrinhs", x => x.IdHanhTrinh);
                    table.ForeignKey(
                        name: "FK_HanhTrinhs_PhuongTiens_PhuongTienIdPhuongTien",
                        column: x => x.PhuongTienIdPhuongTien,
                        principalTable: "PhuongTiens",
                        principalColumn: "IdPhuongTien",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietHopDongs",
                columns: table => new
                {
                    IdPhuongTien = table.Column<int>(type: "int", nullable: false),
                    IdHopDong = table.Column<int>(type: "int", nullable: false),
                    NgayThue = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayTra = table.Column<DateOnly>(type: "date", nullable: false),
                    GiaThue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhuongTienIdPhuongTien = table.Column<int>(type: "int", nullable: false),
                    HopDongIdHopDong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietHopDongs", x => new { x.IdPhuongTien, x.IdHopDong });
                    table.ForeignKey(
                        name: "FK_ChiTietHopDongs_HopDongs_HopDongIdHopDong",
                        column: x => x.HopDongIdHopDong,
                        principalTable: "HopDongs",
                        principalColumn: "IdHopDong",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietHopDongs_PhuongTiens_PhuongTienIdPhuongTien",
                        column: x => x.PhuongTienIdPhuongTien,
                        principalTable: "PhuongTiens",
                        principalColumn: "IdPhuongTien",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DuLieuGPS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdHanhTrinh = table.Column<int>(type: "int", nullable: false),
                    KinhDo = table.Column<decimal>(type: "decimal(11,8)", nullable: false),
                    ViDo = table.Column<decimal>(type: "decimal(11,8)", nullable: false),
                    TocDo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HanhTrinhIdHanhTrinh = table.Column<int>(type: "int", nullable: false),
                    ThietBiGPSIdThietBi = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuLieuGPS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DuLieuGPS_HanhTrinhs_HanhTrinhIdHanhTrinh",
                        column: x => x.HanhTrinhIdHanhTrinh,
                        principalTable: "HanhTrinhs",
                        principalColumn: "IdHanhTrinh",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DuLieuGPS_ThietBiGPS_ThietBiGPSIdThietBi",
                        column: x => x.ThietBiGPSIdThietBi,
                        principalTable: "ThietBiGPS",
                        principalColumn: "IdThietBi");
                });

            migrationBuilder.CreateTable(
                name: "PhieuViPhams",
                columns: table => new
                {
                    IdPhieuViPham = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NgayViPham = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TienPhat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    IdHanhTrinh = table.Column<int>(type: "int", nullable: false),
                    MaCccd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HanhTrinhIdHanhTrinh = table.Column<int>(type: "int", nullable: false),
                    KhachHangMaCccd = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuViPhams", x => x.IdPhieuViPham);
                    table.ForeignKey(
                        name: "FK_PhieuViPhams_HanhTrinhs_HanhTrinhIdHanhTrinh",
                        column: x => x.HanhTrinhIdHanhTrinh,
                        principalTable: "HanhTrinhs",
                        principalColumn: "IdHanhTrinh",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhieuViPhams_KhachHangs_KhachHangMaCccd",
                        column: x => x.KhachHangMaCccd,
                        principalTable: "KhachHangs",
                        principalColumn: "MaCccd");
                });

            migrationBuilder.CreateTable(
                name: "ChiTietViPhams",
                columns: table => new
                {
                    IdPhieuViPham = table.Column<int>(type: "int", nullable: false),
                    IdQuyDinh = table.Column<int>(type: "int", nullable: false),
                    ThoiGianXayRa = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PhieuViPhamIdPhieuViPham = table.Column<int>(type: "int", nullable: false),
                    QuyDinhIdQuyDinh = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietViPhams", x => new { x.IdPhieuViPham, x.IdQuyDinh });
                    table.ForeignKey(
                        name: "FK_ChiTietViPhams_PhieuViPhams_PhieuViPhamIdPhieuViPham",
                        column: x => x.PhieuViPhamIdPhieuViPham,
                        principalTable: "PhieuViPhams",
                        principalColumn: "IdPhieuViPham",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietViPhams_QuyDinhs_QuyDinhIdQuyDinh",
                        column: x => x.QuyDinhIdQuyDinh,
                        principalTable: "QuyDinhs",
                        principalColumn: "IdQuyDinh",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHopDongs_HopDongIdHopDong",
                table: "ChiTietHopDongs",
                column: "HopDongIdHopDong");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHopDongs_PhuongTienIdPhuongTien",
                table: "ChiTietHopDongs",
                column: "PhuongTienIdPhuongTien");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietQuyens_ChucVuIdChucVu",
                table: "ChiTietQuyens",
                column: "ChucVuIdChucVu");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietQuyens_QuyenIdQuyen",
                table: "ChiTietQuyens",
                column: "QuyenIdQuyen");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietViPhams_PhieuViPhamIdPhieuViPham",
                table: "ChiTietViPhams",
                column: "PhieuViPhamIdPhieuViPham");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietViPhams_QuyDinhIdQuyDinh",
                table: "ChiTietViPhams",
                column: "QuyDinhIdQuyDinh");

            migrationBuilder.CreateIndex(
                name: "IX_DuLieuGPS_HanhTrinhIdHanhTrinh",
                table: "DuLieuGPS",
                column: "HanhTrinhIdHanhTrinh");

            migrationBuilder.CreateIndex(
                name: "IX_DuLieuGPS_ThietBiGPSIdThietBi",
                table: "DuLieuGPS",
                column: "ThietBiGPSIdThietBi");

            migrationBuilder.CreateIndex(
                name: "IX_HanhTrinhs_PhuongTienIdPhuongTien",
                table: "HanhTrinhs",
                column: "PhuongTienIdPhuongTien");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_KhachHangMaCccd",
                table: "HopDongs",
                column: "KhachHangMaCccd");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_TaiKhoanIdTaiKhoan",
                table: "HopDongs",
                column: "TaiKhoanIdTaiKhoan");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuViPhams_HanhTrinhIdHanhTrinh",
                table: "PhieuViPhams",
                column: "HanhTrinhIdHanhTrinh");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuViPhams_KhachHangMaCccd",
                table: "PhieuViPhams",
                column: "KhachHangMaCccd");

            migrationBuilder.CreateIndex(
                name: "IX_PhuongTiens_BienSo",
                table: "PhuongTiens",
                column: "BienSo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhuongTiens_ThietBiGPSIdThietBi",
                table: "PhuongTiens",
                column: "ThietBiGPSIdThietBi");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_ChucVuIdChucVu",
                table: "TaiKhoans",
                column: "ChucVuIdChucVu");

            migrationBuilder.CreateIndex(
                name: "IX_ThietBiGPS_MaImei",
                table: "ThietBiGPS",
                column: "MaImei",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietHopDongs");

            migrationBuilder.DropTable(
                name: "ChiTietQuyens");

            migrationBuilder.DropTable(
                name: "ChiTietViPhams");

            migrationBuilder.DropTable(
                name: "DuLieuGPS");

            migrationBuilder.DropTable(
                name: "HopDongs");

            migrationBuilder.DropTable(
                name: "Quyens");

            migrationBuilder.DropTable(
                name: "PhieuViPhams");

            migrationBuilder.DropTable(
                name: "QuyDinhs");

            migrationBuilder.DropTable(
                name: "TaiKhoans");

            migrationBuilder.DropTable(
                name: "HanhTrinhs");

            migrationBuilder.DropTable(
                name: "KhachHangs");

            migrationBuilder.DropTable(
                name: "ChucVus");

            migrationBuilder.DropTable(
                name: "PhuongTiens");

            migrationBuilder.DropTable(
                name: "ThietBiGPS");
        }
    }
}
