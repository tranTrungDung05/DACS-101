using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACS.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingChiTietViPhamColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('PhieuViPhams', 'HanhTrinhIdHanhTrinh') IS NOT NULL
                   AND COL_LENGTH('PhieuViPhams', 'IdHanhTrinh') IS NULL
                BEGIN
                    EXEC sp_rename 'PhieuViPhams.HanhTrinhIdHanhTrinh', 'IdHanhTrinh', 'COLUMN';
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('PhieuViPhams', 'KhachHangMaCccd') IS NOT NULL
                   AND COL_LENGTH('PhieuViPhams', 'MaCccd') IS NULL
                BEGIN
                    EXEC sp_rename 'PhieuViPhams.KhachHangMaCccd', 'MaCccd', 'COLUMN';
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('ChiTietViPhams', 'KinhDo') IS NULL
                BEGIN
                    ALTER TABLE ChiTietViPhams
                    ADD KinhDo decimal(18,2) NOT NULL CONSTRAINT DF_ChiTietViPhams_KinhDo DEFAULT (0);
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('ChiTietViPhams', 'ViDo') IS NULL
                BEGIN
                    ALTER TABLE ChiTietViPhams
                    ADD ViDo decimal(18,2) NOT NULL CONSTRAINT DF_ChiTietViPhams_ViDo DEFAULT (0);
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('ChiTietViPhams', 'TocDoViPham') IS NULL
                BEGIN
                    ALTER TABLE ChiTietViPhams
                    ADD TocDoViPham decimal(18,2) NOT NULL CONSTRAINT DF_ChiTietViPhams_TocDoViPham DEFAULT (0);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('ChiTietViPhams', 'TocDoViPham') IS NOT NULL
                BEGIN
                    ALTER TABLE ChiTietViPhams DROP COLUMN TocDoViPham;
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('ChiTietViPhams', 'ViDo') IS NOT NULL
                BEGIN
                    ALTER TABLE ChiTietViPhams DROP COLUMN ViDo;
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('ChiTietViPhams', 'KinhDo') IS NOT NULL
                BEGIN
                    ALTER TABLE ChiTietViPhams DROP COLUMN KinhDo;
                END
                """);
        }
    }
}
