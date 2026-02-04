#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    /// <inheritdoc />
    public partial class AddLlmInsightsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Features",
                columns: new[] { "Name", "DocumentationUrl", "FeatureCode", "IsActive" },
                values: new object[] { "Llm Insights", "", "llm_insights", true }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "FeatureCode",
                keyValue: "llm_insights"
            );
        }
    }

}
