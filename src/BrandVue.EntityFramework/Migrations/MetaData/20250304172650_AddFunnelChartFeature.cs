using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddFunnelChartFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Features",
                columns: new[] { "Name", "DocumentationUrl", "FeatureCode", "IsActive" },
                values: new object[] { "Funnel chart", "", "funnel_chart", true }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "FeatureCode",
                keyValue: "funnel_chart"
            );
        }
    }
}
