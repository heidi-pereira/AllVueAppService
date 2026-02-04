using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class remapSigConfidenceLevelEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remap old enum values to new ones:
            // Old: 0 = 99, 1 = 98, 2 = 95, 3 = 90
            // New: 99 = 99, 98 = 98, 95 = 95, 90 = 90

            migrationBuilder.Sql(@"
                UPDATE [Reports].[SavedReports]
                SET [SigConfidenceLevel] = 
                    CASE [SigConfidenceLevel]
                        WHEN 0 THEN 99
                        WHEN 1 THEN 98
                        WHEN 2 THEN 95
                        WHEN 3 THEN 90
                        ELSE [SigConfidenceLevel]
                    END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the mapping if needed
            migrationBuilder.Sql(@"
                UPDATE [Reports].[SavedReports]
                SET [SigConfidenceLevel] = 
                    CASE [SigConfidenceLevel]
                        WHEN 99 THEN 0
                        WHEN 98 THEN 1
                        WHEN 95 THEN 2
                        WHEN 90 THEN 3
                        ELSE [SigConfidenceLevel]
                    END
            ");
        }
    }
}
