using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACS.Migrations
{
    /// <inheritdoc />
    public partial class CleanupChiTietViPhamLegacyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('ChiTietViPhams', 'IdPhieuViPham') IS NOT NULL
                   AND COL_LENGTH('ChiTietViPhams', 'PhieuViPhamIdPhieuViPham') IS NOT NULL
                BEGIN
                    UPDATE [dbo].[ChiTietViPhams]
                    SET [PhieuViPhamIdPhieuViPham] = [IdPhieuViPham]
                    WHERE [PhieuViPhamIdPhieuViPham] = 0 AND [IdPhieuViPham] <> 0;
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('ChiTietViPhams', 'IdQuyDinh') IS NOT NULL
                   AND COL_LENGTH('ChiTietViPhams', 'QuyDinhIdQuyDinh') IS NOT NULL
                BEGIN
                    UPDATE [dbo].[ChiTietViPhams]
                    SET [QuyDinhIdQuyDinh] = [IdQuyDinh]
                    WHERE [QuyDinhIdQuyDinh] = 0 AND [IdQuyDinh] <> 0;
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('ChiTietViPhams', 'IdPhieuViPham') IS NOT NULL
                   OR COL_LENGTH('ChiTietViPhams', 'IdQuyDinh') IS NOT NULL
                BEGIN
                    DECLARE @pkName sysname;
                    SELECT @pkName = kc.name
                    FROM sys.key_constraints kc
                    JOIN sys.tables t ON t.object_id = kc.parent_object_id
                    WHERE kc.[type] = 'PK' AND t.name = 'ChiTietViPhams';

                    IF @pkName IS NOT NULL
                    BEGIN
                        EXEC('ALTER TABLE [dbo].[ChiTietViPhams] DROP CONSTRAINT [' + @pkName + ']');
                    END

                    IF EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE name = 'IX_ChiTietViPhams_PhieuViPhamIdPhieuViPham'
                          AND object_id = OBJECT_ID('dbo.ChiTietViPhams')
                    )
                    BEGIN
                        DROP INDEX [IX_ChiTietViPhams_PhieuViPhamIdPhieuViPham] ON [dbo].[ChiTietViPhams];
                    END

                    IF COL_LENGTH('ChiTietViPhams', 'IdPhieuViPham') IS NOT NULL
                    BEGIN
                        ALTER TABLE [dbo].[ChiTietViPhams] DROP COLUMN [IdPhieuViPham];
                    END

                    IF COL_LENGTH('ChiTietViPhams', 'IdQuyDinh') IS NOT NULL
                    BEGIN
                        ALTER TABLE [dbo].[ChiTietViPhams] DROP COLUMN [IdQuyDinh];
                    END
                END
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.key_constraints kc
                    JOIN sys.tables t ON t.object_id = kc.parent_object_id
                    WHERE kc.[type] = 'PK' AND t.name = 'ChiTietViPhams'
                )
                   AND COL_LENGTH('ChiTietViPhams', 'PhieuViPhamIdPhieuViPham') IS NOT NULL
                   AND COL_LENGTH('ChiTietViPhams', 'QuyDinhIdQuyDinh') IS NOT NULL
                   AND COL_LENGTH('ChiTietViPhams', 'ThoiGianXayRa') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[ChiTietViPhams]
                    ADD CONSTRAINT [PK_ChiTietViPhams]
                    PRIMARY KEY ([PhieuViPhamIdPhieuViPham], [QuyDinhIdQuyDinh], [ThoiGianXayRa]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('ChiTietViPhams', 'IdPhieuViPham') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[ChiTietViPhams] ADD [IdPhieuViPham] int NOT NULL DEFAULT (0);
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('ChiTietViPhams', 'IdQuyDinh') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[ChiTietViPhams] ADD [IdQuyDinh] int NOT NULL DEFAULT (0);
                END
                """);
        }
    }
}
