using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintsToPermissionFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PermissionFeatures_Name_SystemKey",
                schema: "UserFeaturePermissions",
                table: "PermissionFeatures",
                columns: new[] { "Name", "SystemKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PermissionFeatures_Name_SystemKey",
                schema: "UserFeaturePermissions",
                table: "PermissionFeatures");
        }
    }
}
