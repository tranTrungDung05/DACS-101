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
            migrationBuilder.Sql(
                @"
IF OBJECT_ID(N'[FK_ChiTietHopDongs_HopDongs_HopDongIdHopDong]', N'F') IS NOT NULL
    ALTER TABLE [ChiTietHopDongs] DROP CONSTRAINT [FK_ChiTietHopDongs_HopDongs_HopDongIdHopDong];
IF OBJECT_ID(N'[FK_ChiTietHopDongs_PhuongTiens_PhuongTienIdPhuongTien]', N'F') IS NOT NULL
    ALTER TABLE [ChiTietHopDongs] DROP CONSTRAINT [FK_ChiTietHopDongs_PhuongTiens_PhuongTienIdPhuongTien];
IF OBJECT_ID(N'[FK_DuLieuGPS_ThietBiGPS_ThietBiGPSIdThietBi]', N'F') IS NOT NULL
    ALTER TABLE [DuLieuGPS] DROP CONSTRAINT [FK_DuLieuGPS_ThietBiGPS_ThietBiGPSIdThietBi];
IF OBJECT_ID(N'[FK_HanhTrinhs_PhuongTiens_PhuongTienIdPhuongTien]', N'F') IS NOT NULL
    ALTER TABLE [HanhTrinhs] DROP CONSTRAINT [FK_HanhTrinhs_PhuongTiens_PhuongTienIdPhuongTien];
IF OBJECT_ID(N'[FK_HopDongs_KhachHangs_KhachHangMaCccd]', N'F') IS NOT NULL
    ALTER TABLE [HopDongs] DROP CONSTRAINT [FK_HopDongs_KhachHangs_KhachHangMaCccd];
IF OBJECT_ID(N'[FK_HopDongs_TaiKhoans_TaiKhoanIdTaiKhoan]', N'F') IS NOT NULL
    ALTER TABLE [HopDongs] DROP CONSTRAINT [FK_HopDongs_TaiKhoans_TaiKhoanIdTaiKhoan];
IF OBJECT_ID(N'[FK_PhieuViPhams_HanhTrinhs_HanhTrinhIdHanhTrinh]', N'F') IS NOT NULL
    ALTER TABLE [PhieuViPhams] DROP CONSTRAINT [FK_PhieuViPhams_HanhTrinhs_HanhTrinhIdHanhTrinh];
IF OBJECT_ID(N'[FK_PhieuViPhams_HanhTrinhs]', N'F') IS NOT NULL
    ALTER TABLE [PhieuViPhams] DROP CONSTRAINT [FK_PhieuViPhams_HanhTrinhs];
IF OBJECT_ID(N'[FK_PhieuViPhams_KhachHangs_KhachHangMaCccd]', N'F') IS NOT NULL
    ALTER TABLE [PhieuViPhams] DROP CONSTRAINT [FK_PhieuViPhams_KhachHangs_KhachHangMaCccd];
IF OBJECT_ID(N'[FK_PhieuViPhams_KhachHangs]', N'F') IS NOT NULL
    ALTER TABLE [PhieuViPhams] DROP CONSTRAINT [FK_PhieuViPhams_KhachHangs];

IF COL_LENGTH('HanhTrinhs', 'PhuongTienIdPhuongTien') IS NOT NULL AND COL_LENGTH('HanhTrinhs', 'IdPhuongTien') IS NULL
    EXEC sp_rename N'[HanhTrinhs].[PhuongTienIdPhuongTien]', N'IdPhuongTien', N'COLUMN';
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[HanhTrinhs]') AND name = N'IX_HanhTrinhs_PhuongTienIdPhuongTien')
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[HanhTrinhs]') AND name = N'IX_HanhTrinhs_IdPhuongTien')
    EXEC sp_rename N'[HanhTrinhs].[IX_HanhTrinhs_PhuongTienIdPhuongTien]', N'IX_HanhTrinhs_IdPhuongTien', N'INDEX';

IF COL_LENGTH('DuLieuGPS', 'ThietBiGPSIdThietBi') IS NOT NULL AND COL_LENGTH('DuLieuGPS', 'IdThietBi') IS NULL
    EXEC sp_rename N'[DuLieuGPS].[ThietBiGPSIdThietBi]', N'IdThietBi', N'COLUMN';
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[DuLieuGPS]') AND name = N'IX_DuLieuGPS_ThietBiGPSIdThietBi')
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[DuLieuGPS]') AND name = N'IX_DuLieuGPS_IdThietBi')
    EXEC sp_rename N'[DuLieuGPS].[IX_DuLieuGPS_ThietBiGPSIdThietBi]', N'IX_DuLieuGPS_IdThietBi', N'INDEX';

IF COL_LENGTH('PhieuViPhams', 'HanhTrinhIdHanhTrinh') IS NOT NULL AND COL_LENGTH('PhieuViPhams', 'IdHanhTrinh') IS NULL
    EXEC sp_rename N'[PhieuViPhams].[HanhTrinhIdHanhTrinh]', N'IdHanhTrinh', N'COLUMN';
IF COL_LENGTH('PhieuViPhams', 'KhachHangMaCccd') IS NOT NULL AND COL_LENGTH('PhieuViPhams', 'MaCccd') IS NULL
    EXEC sp_rename N'[PhieuViPhams].[KhachHangMaCccd]', N'MaCccd', N'COLUMN';

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[PhieuViPhams]') AND name = N'IX_PhieuViPhams_HanhTrinhIdHanhTrinh')
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[PhieuViPhams]') AND name = N'IX_PhieuViPhams_IdHanhTrinh')
    EXEC sp_rename N'[PhieuViPhams].[IX_PhieuViPhams_HanhTrinhIdHanhTrinh]', N'IX_PhieuViPhams_IdHanhTrinh', N'INDEX';
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[PhieuViPhams]') AND name = N'IX_PhieuViPhams_KhachHangMaCccd')
    AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[PhieuViPhams]') AND name = N'IX_PhieuViPhams_MaCccd')
    EXEC sp_rename N'[PhieuViPhams].[IX_PhieuViPhams_KhachHangMaCccd]', N'IX_PhieuViPhams_MaCccd', N'INDEX';

IF COL_LENGTH('HopDongs', 'KhachHangMaCccd') IS NOT NULL
    ALTER TABLE [HopDongs] DROP COLUMN [KhachHangMaCccd];
IF COL_LENGTH('HopDongs', 'TaiKhoanIdTaiKhoan') IS NOT NULL
    ALTER TABLE [HopDongs] DROP COLUMN [TaiKhoanIdTaiKhoan];
IF COL_LENGTH('ChiTietHopDongs', 'HopDongIdHopDong') IS NOT NULL
    ALTER TABLE [ChiTietHopDongs] DROP COLUMN [HopDongIdHopDong];
IF COL_LENGTH('ChiTietHopDongs', 'PhuongTienIdPhuongTien') IS NOT NULL
    ALTER TABLE [ChiTietHopDongs] DROP COLUMN [PhuongTienIdPhuongTien];

IF COL_LENGTH('HopDongs', 'MaCccd') IS NOT NULL
BEGIN
    UPDATE [HopDongs] SET [MaCccd] = N'' WHERE [MaCccd] IS NULL;
    ALTER TABLE [HopDongs] ALTER COLUMN [MaCccd] nvarchar(450) NOT NULL;
END
IF COL_LENGTH('PhieuViPhams', 'MaCccd') IS NOT NULL
    ALTER TABLE [PhieuViPhams] ALTER COLUMN [MaCccd] nvarchar(450) NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[PhieuViPhams]') AND name = N'IX_PhieuViPhams_IdHanhTrinh')
    CREATE INDEX [IX_PhieuViPhams_IdHanhTrinh] ON [PhieuViPhams] ([IdHanhTrinh]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[PhieuViPhams]') AND name = N'IX_PhieuViPhams_MaCccd')
    CREATE INDEX [IX_PhieuViPhams_MaCccd] ON [PhieuViPhams] ([MaCccd]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[HopDongs]') AND name = N'IX_HopDongs_IdTaiKhoan')
    CREATE INDEX [IX_HopDongs_IdTaiKhoan] ON [HopDongs] ([IdTaiKhoan]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[HopDongs]') AND name = N'IX_HopDongs_MaCccd')
    CREATE INDEX [IX_HopDongs_MaCccd] ON [HopDongs] ([MaCccd]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[ChiTietHopDongs]') AND name = N'IX_ChiTietHopDongs_IdHopDong')
    CREATE INDEX [IX_ChiTietHopDongs_IdHopDong] ON [ChiTietHopDongs] ([IdHopDong]);

IF OBJECT_ID(N'[FK_ChiTietHopDongs_HopDongs_IdHopDong]', N'F') IS NULL
    ALTER TABLE [ChiTietHopDongs] ADD CONSTRAINT [FK_ChiTietHopDongs_HopDongs_IdHopDong]
    FOREIGN KEY ([IdHopDong]) REFERENCES [HopDongs] ([IdHopDong]) ON DELETE CASCADE;
IF OBJECT_ID(N'[FK_ChiTietHopDongs_PhuongTiens_IdPhuongTien]', N'F') IS NULL
    ALTER TABLE [ChiTietHopDongs] ADD CONSTRAINT [FK_ChiTietHopDongs_PhuongTiens_IdPhuongTien]
    FOREIGN KEY ([IdPhuongTien]) REFERENCES [PhuongTiens] ([IdPhuongTien]) ON DELETE CASCADE;
IF OBJECT_ID(N'[FK_DuLieuGPS_ThietBiGPS_IdThietBi]', N'F') IS NULL AND COL_LENGTH('DuLieuGPS', 'IdThietBi') IS NOT NULL
    ALTER TABLE [DuLieuGPS] ADD CONSTRAINT [FK_DuLieuGPS_ThietBiGPS_IdThietBi]
    FOREIGN KEY ([IdThietBi]) REFERENCES [ThietBiGPS] ([IdThietBi]);
IF OBJECT_ID(N'[FK_HanhTrinhs_PhuongTiens_IdPhuongTien]', N'F') IS NULL
    ALTER TABLE [HanhTrinhs] ADD CONSTRAINT [FK_HanhTrinhs_PhuongTiens_IdPhuongTien]
    FOREIGN KEY ([IdPhuongTien]) REFERENCES [PhuongTiens] ([IdPhuongTien]) ON DELETE CASCADE;
IF OBJECT_ID(N'[FK_HopDongs_KhachHangs_MaCccd]', N'F') IS NULL
    ALTER TABLE [HopDongs] ADD CONSTRAINT [FK_HopDongs_KhachHangs_MaCccd]
    FOREIGN KEY ([MaCccd]) REFERENCES [KhachHangs] ([MaCccd]) ON DELETE CASCADE;
IF OBJECT_ID(N'[FK_HopDongs_TaiKhoans_IdTaiKhoan]', N'F') IS NULL
    ALTER TABLE [HopDongs] ADD CONSTRAINT [FK_HopDongs_TaiKhoans_IdTaiKhoan]
    FOREIGN KEY ([IdTaiKhoan]) REFERENCES [TaiKhoans] ([IdTaiKhoan]) ON DELETE CASCADE;
IF OBJECT_ID(N'[FK_PhieuViPhams_HanhTrinhs_IdHanhTrinh]', N'F') IS NULL
    ALTER TABLE [PhieuViPhams] ADD CONSTRAINT [FK_PhieuViPhams_HanhTrinhs_IdHanhTrinh]
    FOREIGN KEY ([IdHanhTrinh]) REFERENCES [HanhTrinhs] ([IdHanhTrinh]) ON DELETE CASCADE;
IF OBJECT_ID(N'[FK_PhieuViPhams_KhachHangs_MaCccd]', N'F') IS NULL
    ALTER TABLE [PhieuViPhams] ADD CONSTRAINT [FK_PhieuViPhams_KhachHangs_MaCccd]
    FOREIGN KEY ([MaCccd]) REFERENCES [KhachHangs] ([MaCccd]) ON DELETE CASCADE;
");
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
