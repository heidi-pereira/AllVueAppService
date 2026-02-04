using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddFeatureUserManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Feature Permissions
            migrationBuilder.InsertData(
                table: "Features",
                columns: new[] { "Name", "DocumentationUrl", "FeatureCode", "IsActive" },
                values: new object[] { "User Management", "", "user_management", false }
            );

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "FeatureCode",
                keyValue: "user_management"
            );

        }
    }
}
