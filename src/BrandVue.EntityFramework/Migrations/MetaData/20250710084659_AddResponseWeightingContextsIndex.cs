using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddResponseWeightingContextsIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ResponseWeightingContexts_WeightingTargetId' AND object_id = OBJECT_ID('ResponseWeightingContexts'))
                BEGIN
                    DROP INDEX IX_ResponseWeightingContexts_WeightingTargetId ON ResponseWeightingContexts
                END");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseWeightingContexts_WeightingTargetId",
                table: "ResponseWeightingContexts",
                column: "WeightingTargetId",
                unique: true,
                filter: "[WeightingTargetId] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Id", "ProductShortCode", "SubProductId", "Context", "SubsetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ResponseWeightingContexts_WeightingTargetId' AND object_id = OBJECT_ID('ResponseWeightingContexts'))
                BEGIN
                    DROP INDEX IX_ResponseWeightingContexts_WeightingTargetId ON ResponseWeightingContexts
                END");
        }
    }
}
