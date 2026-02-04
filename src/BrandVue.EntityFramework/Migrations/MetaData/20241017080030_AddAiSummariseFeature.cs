#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddAiSummariseFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Features",
                columns: new[] { "Name", "DocumentationUrl", "FeatureCode", "IsActive" },
                values: new object[] { "AI Summarise", "https://docs.savanta.com/allvue/Content/AllVue/Generating_AI_Summaries_for_Open_Ends.html?Highlight=ai%20summari", "ai_summarise", true }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "FeatureCode",
                keyValue: "ai_summarise"
            );
        }
    }
}
