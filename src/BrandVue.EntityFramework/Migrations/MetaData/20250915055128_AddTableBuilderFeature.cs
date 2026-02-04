using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddTableBuilderFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Feature Permissions
            migrationBuilder.InsertData(
                table: "Features",
                columns: new[] { "Name", "DocumentationUrl", "FeatureCode", "IsActive" },
                values: new object[] { "Table Builder", "", "table_builder", true }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "FeatureCode",
                keyValue: "table_builder"
            );
        }
    }
}
