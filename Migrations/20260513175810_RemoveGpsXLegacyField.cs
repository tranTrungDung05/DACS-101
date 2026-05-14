using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DACS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGpsXLegacyField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
IF COL_LENGTH('DuLieuGPS', 'GpsX') IS NOT NULL
    ALTER TABLE [DuLieuGPS] DROP COLUMN [GpsX];
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
IF COL_LENGTH('DuLieuGPS', 'GpsX') IS NULL
    ALTER TABLE [DuLieuGPS] ADD [GpsX] float NULL;
");
        }
    }
}
